using UnityEngine;

namespace MovementTemplate
{
    /// <summary>
    /// Placeholder procedural animation for the grey box character and the first-person arms.
    /// Each game replaces this with its own animation (see README). Reads only motor state.
    /// Angles are "forward swing" in degrees: positive swings the limb forward.
    /// </summary>
    [DefaultExecutionOrder(120)]
    public class GreyboxAnimationDriver : MonoBehaviour
    {
        public PlayerMotor motor;

        [Header("Third-person body")]
        [Tooltip("Scaled vertically when crouching.")] public Transform bodyRoot;
        public Transform torso;
        public Transform leftArm;
        public Transform rightArm;
        public Transform leftLeg;
        public Transform rightLeg;

        [Header("First-person arms")]
        public Transform fpsArmsRoot;
        public Transform fpsLeftArm;
        public Transform fpsRightArm;

        float phase;
        float lArm, rArm, lLeg, rLeg, torsoPitch, torsoRoll, heightScale = 1f;
        Vector3 fpsBasePosition;
        Quaternion fpsLeftBase = Quaternion.identity, fpsRightBase = Quaternion.identity;
        Vector3 fpsOffset;
        bool fpsCached;

        void Start()
        {
            CacheFpsBase();
        }

        void CacheFpsBase()
        {
            if (fpsCached || fpsArmsRoot == null) return;
            fpsBasePosition = fpsArmsRoot.localPosition;
            if (fpsLeftArm != null) fpsLeftBase = fpsLeftArm.localRotation;
            if (fpsRightArm != null) fpsRightBase = fpsRightArm.localRotation;
            fpsCached = true;
        }

        static float Smooth(float current, float target, float dt)
        {
            return Mathf.Lerp(current, target, MotorMath.Damp(14f, dt));
        }

        void LateUpdate()
        {
            if (motor == null || motor.profile == null) return;
            CacheFpsBase();

            float dt = Time.deltaTime;
            MovementProfile p = motor.profile;
            float speed = motor.HorizontalSpeed;
            float n = Mathf.Clamp01(speed / Mathf.Max(0.1f, p.sprintSpeed));

            // Phase speed depends on what the character is doing.
            switch (motor.Mode)
            {
                case MoveMode.Ladder:
                case MoveMode.Climb:
                    phase += dt * (Mathf.Abs(motor.Velocity.y) + speed) * 3f;
                    break;
                case MoveMode.Swim:
                    phase += dt * (4f + speed * 2f);
                    break;
                default:
                    if (speed > 0.1f) phase += dt * (2.5f + speed * 1.6f);
                    break;
            }
            float s = Mathf.Sin(phase);

            float tLArm = 0f, tRArm = 0f, tLLeg = 0f, tRLeg = 0f, tPitch = 0f, tRoll = 0f;
            float swing = 55f * n;

            switch (motor.Mode)
            {
                case MoveMode.Ground:
                    tLLeg = s * swing; tRLeg = -s * swing;
                    tLArm = -s * swing * 0.8f; tRArm = s * swing * 0.8f;
                    tPitch = 14f * n;
                    break;
                case MoveMode.Air:
                    bool rising = motor.Velocity.y > 0f;
                    tLLeg = 30f; tRLeg = rising ? -10f : 5f;
                    tLArm = tRArm = rising ? 50f : 80f;
                    break;
                case MoveMode.Slide:
                    tPitch = -40f;
                    tLLeg = 75f; tRLeg = 60f;
                    tLArm = tRArm = -35f;
                    break;
                case MoveMode.WallRun:
                    tRoll = -motor.WallSide * 15f;
                    tLLeg = s * 45f; tRLeg = -s * 45f;
                    tLArm = motor.WallSide < 0 ? 80f : -s * 30f;
                    tRArm = motor.WallSide > 0 ? 80f : s * 30f;
                    break;
                case MoveMode.Ladder:
                case MoveMode.Climb:
                    tLArm = 110f + 40f * s; tRArm = 110f - 40f * s;
                    tLLeg = 30f * s; tRLeg = -30f * s;
                    break;
                case MoveMode.Vault:
                    tLArm = tRArm = 150f;
                    tLLeg = 60f; tRLeg = -20f;
                    tPitch = 20f;
                    break;
                case MoveMode.Mantle:
                    tLArm = tRArm = 165f;
                    tLLeg = 45f; tRLeg = 20f;
                    break;
                case MoveMode.Swim:
                    tLArm = 20f + 70f * s; tRArm = 20f - 70f * s;
                    tLLeg = 15f * Mathf.Sin(phase * 3f); tRLeg = -tLLeg;
                    tPitch = 25f;
                    break;
            }

            lArm = Smooth(lArm, tLArm, dt); rArm = Smooth(rArm, tRArm, dt);
            lLeg = Smooth(lLeg, tLLeg, dt); rLeg = Smooth(rLeg, tRLeg, dt);
            torsoPitch = Smooth(torsoPitch, tPitch, dt);
            torsoRoll = Smooth(torsoRoll, tRoll, dt);
            heightScale = Smooth(heightScale, motor.Controller.height / Mathf.Max(0.1f, p.standHeight), dt);

            // Third-person body
            if (bodyRoot != null) bodyRoot.localScale = new Vector3(1f, heightScale, 1f);
            if (torso != null) torso.localRotation = Quaternion.Euler(torsoPitch, 0f, torsoRoll);
            SetLimb(leftArm, lArm); SetLimb(rightArm, rArm);
            SetLimb(leftLeg, lLeg); SetLimb(rightLeg, rLeg);

            // First-person arms: same motion, smaller, plus a position offset per move.
            if (fpsArmsRoot != null)
            {
                Vector3 targetOffset = Vector3.zero;
                switch (motor.Mode)
                {
                    case MoveMode.Ground: targetOffset = new Vector3(0f, -0.05f * n, 0.02f * n); break;
                    case MoveMode.Slide: targetOffset = new Vector3(0f, -0.12f, -0.02f); break;
                    case MoveMode.Air: targetOffset = new Vector3(0f, 0.02f, 0f); break;
                    case MoveMode.Vault:
                    case MoveMode.Mantle: targetOffset = new Vector3(0f, 0.1f, 0.15f); break;
                    case MoveMode.WallRun: targetOffset = new Vector3(motor.WallSide * -0.05f, -0.03f, 0f); break;
                }
                fpsOffset = Vector3.Lerp(fpsOffset, targetOffset, MotorMath.Damp(10f, dt));
                float bob = motor.Mode == MoveMode.Ground ? Mathf.Sin(phase * 2f) * 0.012f * n : 0f;
                fpsArmsRoot.localPosition = fpsBasePosition + fpsOffset + new Vector3(0f, bob, 0f);

                if (fpsLeftArm != null) fpsLeftArm.localRotation = fpsLeftBase * Quaternion.Euler(-lArm * 0.35f, 0f, 0f);
                if (fpsRightArm != null) fpsRightArm.localRotation = fpsRightBase * Quaternion.Euler(-rArm * 0.35f, 0f, 0f);
            }
        }

        static void SetLimb(Transform limb, float forwardAngle)
        {
            if (limb != null) limb.localRotation = Quaternion.Euler(-forwardAngle, 0f, 0f);
        }
    }
}
