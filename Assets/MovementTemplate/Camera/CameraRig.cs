using System;
using UnityEngine;

namespace MovementTemplate
{
    /// <summary>
    /// First- and third-person camera in one component. The motor only knows yaw and pitch, so switching
    /// perspective never touches movement. Effects (bob, roll, FOV kick, landing dip) read the motor.
    /// No Cinemachine needed.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(100)]
    public class CameraRig : MonoBehaviour
    {
        [Header("References")]
        public PlayerMotor motor;
        public PlayerInputReader input;

        [Header("Perspective")]
        public PerspectiveMode startMode = PerspectiveMode.FirstPerson;
        public float blendTime = 0.25f;
        public int shoulderSide = 1;
        public LayerMask tpsCollisionMask = ~0;
        public float tpsCollisionRadius = 0.25f;

        [Header("Accessibility")]
        [Tooltip("Turns off head bob, roll and FOV kick.")] public bool reduceMotion;
        [Tooltip("Added to the profile's base FOV (player FOV slider).")] public float fovOffset;
        public bool underwaterFog = true;

        public event Action<PerspectiveMode> PerspectiveChanged;

        public PerspectiveMode Perspective { get; private set; }
        /// <summary>0 = fully first person, 1 = fully third person.</summary>
        public float TpsBlend => MotorMath.EaseInOut(blend);

        Camera cam;
        float blend;
        float eyeHeight;
        float roll;
        float bobTime;
        float bobAmount;
        float dip;
        float dipVelocity;
        float tpsDistance;

        bool savedFog;
        Color savedFogColor;
        FogMode savedFogMode;
        float savedFogDensity;

        void Awake()
        {
            cam = GetComponent<Camera>();
            Perspective = startMode;
            blend = Perspective == PerspectiveMode.ThirdPerson ? 1f : 0f;
            savedFog = RenderSettings.fog;
            savedFogColor = RenderSettings.fogColor;
            savedFogMode = RenderSettings.fogMode;
            savedFogDensity = RenderSettings.fogDensity;
        }

        void OnEnable()
        {
            if (input != null)
            {
                input.SwitchPerspectivePressed += TogglePerspective;
                input.ShoulderSwapPressed += SwapShoulder;
            }
            if (motor != null) motor.Landed += OnLanded;
        }

        void OnDisable()
        {
            if (input != null)
            {
                input.SwitchPerspectivePressed -= TogglePerspective;
                input.ShoulderSwapPressed -= SwapShoulder;
            }
            if (motor != null) motor.Landed -= OnLanded;
            RestoreFog();
        }

        public void TogglePerspective()
        {
            SetPerspective(Perspective == PerspectiveMode.FirstPerson ? PerspectiveMode.ThirdPerson : PerspectiveMode.FirstPerson);
        }

        public void SetPerspective(PerspectiveMode mode)
        {
            if (mode == Perspective) return;
            Perspective = mode;
            PerspectiveChanged?.Invoke(mode);
        }

        public void SwapShoulder()
        {
            shoulderSide = -shoulderSide;
        }

        void OnLanded(float fallSpeed)
        {
            if (reduceMotion || motor.profile == null) return;
            dipVelocity -= Mathf.Clamp(fallSpeed, 0f, 20f) * 0.35f * motor.profile.landingDipScale;
        }

        void LateUpdate()
        {
            if (motor == null || motor.profile == null) return;
            float dt = Time.deltaTime;
            MovementProfile p = motor.profile;

            // Perspective blend
            float blendTarget = Perspective == PerspectiveMode.ThirdPerson ? 1f : 0f;
            blend = Mathf.MoveTowards(blend, blendTarget, dt / Mathf.Max(0.01f, blendTime));
            float t = MotorMath.EaseInOut(blend);

            // Smoothed eye height (changes when crouching or sliding)
            if (eyeHeight <= 0f) eyeHeight = motor.EyeHeight;
            eyeHeight = Mathf.Lerp(eyeHeight, motor.EyeHeight, MotorMath.Damp(14f, dt));

            Vector3 root = motor.RenderPosition;
            Quaternion yawRotation = Quaternion.Euler(0f, motor.Yaw, 0f);

            // Roll (wall-run tilt plus a touch of strafe lean)
            float rollTarget = 0f;
            if (!reduceMotion)
            {
                rollTarget = motor.CameraRollTarget;
                if (motor.Mode == MoveMode.Ground && input != null) rollTarget -= input.MoveValue.x * p.strafeRoll;
            }
            roll = Mathf.Lerp(roll, rollTarget, MotorMath.Damp(8f, dt));
            Quaternion lookRotation = Quaternion.Euler(motor.Pitch, motor.Yaw, roll);

            // Head bob (first person) and landing dip
            float speed = motor.HorizontalSpeed;
            bool bobbing = motor.Mode == MoveMode.Ground && !reduceMotion && speed > 0.5f;
            float bobTarget = bobbing ? p.headBobAmplitude * p.headBobScale * Mathf.Clamp01(speed / Mathf.Max(0.1f, p.sprintSpeed)) : 0f;
            bobAmount = Mathf.Lerp(bobAmount, bobTarget, MotorMath.Damp(10f, dt));
            if (bobbing) bobTime += dt * speed * p.headBobFrequency;
            Vector3 bob = yawRotation * new Vector3(Mathf.Cos(bobTime) * bobAmount * 0.5f, Mathf.Sin(bobTime * 2f) * bobAmount, 0f);

            dipVelocity += (-dip * 160f - dipVelocity * 14f) * dt;
            dip += dipVelocity * dt;

            Vector3 fpsPosition = root + Vector3.up * (eyeHeight + dip) + bob;

            // Third person: over-the-shoulder with collision
            Vector3 pivot = root + Vector3.up * (eyeHeight + p.tpsHeightOffset)
                            + yawRotation * Vector3.right * (p.tpsShoulderOffset * shoulderSide);
            Vector3 backDirection = Quaternion.Euler(motor.Pitch, motor.Yaw, 0f) * Vector3.back;
            float wantedDistance = p.tpsDistance;
            if (motor.SphereCastClosest(pivot, tpsCollisionRadius, backDirection, wantedDistance, out RaycastHit hit))
                wantedDistance = Mathf.Max(0.2f, hit.distance - 0.05f);
            if (tpsDistance <= 0f) tpsDistance = wantedDistance;
            tpsDistance = wantedDistance < tpsDistance ? wantedDistance : Mathf.Lerp(tpsDistance, wantedDistance, MotorMath.Damp(6f, dt));
            Vector3 tpsPosition = pivot + backDirection * tpsDistance;

            transform.SetPositionAndRotation(Vector3.Lerp(fpsPosition, tpsPosition, t), lookRotation);

            // FOV: base plus a kick that grows with speed while sprinting or sliding
            float fovTarget = p.baseFov + fovOffset;
            if (!reduceMotion)
            {
                float speedFactor = Mathf.Clamp01((speed - p.walkSpeed) / Mathf.Max(0.1f, p.sprintSpeed - p.walkSpeed));
                float kick = motor.Mode == MoveMode.Slide ? p.slideFovKick : p.sprintFovKick;
                if (motor.Mode == MoveMode.Ground || motor.Mode == MoveMode.Slide || motor.Mode == MoveMode.Air || motor.Mode == MoveMode.WallRun)
                    fovTarget += kick * speedFactor;
            }
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fovTarget, MotorMath.Damp(8f, dt));

            UpdateUnderwater();
        }

        void UpdateUnderwater()
        {
            if (!underwaterFog) return;
            WaterVolume water = motor.CurrentWater;
            if (water != null && motor.HeadUnderwater)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Exponential;
                RenderSettings.fogColor = water.underwaterFogColor;
                RenderSettings.fogDensity = water.underwaterFogDensity;
            }
            else
            {
                RestoreFog();
            }
        }

        void RestoreFog()
        {
            RenderSettings.fog = savedFog;
            RenderSettings.fogColor = savedFogColor;
            RenderSettings.fogMode = savedFogMode;
            RenderSettings.fogDensity = savedFogDensity;
        }
    }
}
