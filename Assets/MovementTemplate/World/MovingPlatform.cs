using UnityEngine;

namespace MovementTemplate
{
    /// <summary>
    /// A platform that moves and/or spins on its own, and tells the motor how far it moved this tick.
    /// Runs before the motor so the player always sees this tick's movement.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class MovingPlatform : MonoBehaviour, IPlayerPusher
    {
        public PlayerInteraction interaction = PlayerInteraction.Carry;

        [Header("Motion")]
        [Tooltip("Ping-pong offset from the start position (world space).")]
        public Vector3 moveOffset = new Vector3(0f, 0f, 8f);
        public float moveDuration = 4f;
        public float pauseAtEnds = 0.5f;
        [Tooltip("Degrees per second around the world up axis.")]
        public float spinSpeed;

        Vector3 startPos;
        Vector3 prevPos;
        Quaternion prevRot;
        float phase;
        float pauseTimer;
        bool forward = true;
        float spinAngle;
        Quaternion startRot;

        public PlayerInteraction Interaction => interaction;
        public Vector3 Velocity { get; private set; }

        void Awake()
        {
            startPos = transform.position;
            startRot = transform.rotation;
            prevPos = startPos;
            prevRot = startRot;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            prevPos = transform.position;
            prevRot = transform.rotation;

            if (moveOffset.sqrMagnitude > 0.0001f && moveDuration > 0.01f)
            {
                if (pauseTimer > 0f)
                {
                    pauseTimer -= dt;
                }
                else
                {
                    phase += (forward ? 1f : -1f) * dt / moveDuration;
                    if (phase >= 1f) { phase = 1f; forward = false; pauseTimer = pauseAtEnds; }
                    else if (phase <= 0f) { phase = 0f; forward = true; pauseTimer = pauseAtEnds; }
                }
            }

            spinAngle += spinSpeed * dt;
            Vector3 pos = startPos + moveOffset * MotorMath.EaseInOut(phase);
            Quaternion rot = Quaternion.Euler(0f, spinAngle, 0f) * startRot;
            transform.SetPositionAndRotation(pos, rot);
            Physics.SyncTransforms();
            Velocity = (pos - prevPos) / Mathf.Max(dt, 0.0001f);
        }

        /// <summary>How far the point at world position p moved during the last tick.</summary>
        public Vector3 DeltaAt(Vector3 p)
        {
            Vector3 local = Quaternion.Inverse(prevRot) * (p - prevPos);
            Vector3 now = transform.position + transform.rotation * local;
            return now - p;
        }

        /// <summary>Yaw change (degrees) of the platform during the last tick.</summary>
        public float YawDelta()
        {
            float delta = (transform.rotation * Quaternion.Inverse(prevRot)).eulerAngles.y;
            return delta > 180f ? delta - 360f : delta;
        }
    }
}
