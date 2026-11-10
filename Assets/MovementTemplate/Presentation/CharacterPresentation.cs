using UnityEngine;

namespace MovementTemplate
{
    /// <summary>
    /// Shows the right body for the current perspective (arms only in first person, full body in third person),
    /// turns the body to face its movement, and keeps the visual interpolated between simulation ticks.
    /// </summary>
    [DefaultExecutionOrder(110)]
    public class CharacterPresentation : MonoBehaviour
    {
        [Header("References")]
        public PlayerMotor motor;
        public CameraRig cameraRig;
        [Tooltip("Parent of the third-person body. Moved and rotated here.")] public Transform visualRoot;
        public GameObject bodyRoot;
        [Tooltip("First-person arms (child of the camera).")] public GameObject armsRoot;

        [Header("Turning (third person)")]
        public float turnSpeed = 540f;
        [Tooltip("When standing still, turn to the camera once it is this far off the body's facing.")]
        public float idleTurnThreshold = 70f;

        float bodyYaw;

        void Start()
        {
            if (motor != null) bodyYaw = motor.Yaw;
        }

        void LateUpdate()
        {
            if (motor == null || visualRoot == null) return;
            float dt = Time.deltaTime;

            float tps = cameraRig != null ? cameraRig.TpsBlend : 0f;
            bool showBody = tps > 0.35f;
            if (bodyRoot != null && bodyRoot.activeSelf != showBody) bodyRoot.SetActive(showBody);
            if (armsRoot != null && armsRoot.activeSelf == showBody) armsRoot.SetActive(!showBody);

            float cameraYaw = motor.Yaw;
            if (tps < 0.5f)
            {
                bodyYaw = cameraYaw;
            }
            else
            {
                float target = bodyYaw;
                Vector3 velocity = MotorMath.Flat(motor.Velocity);
                switch (motor.Mode)
                {
                    case MoveMode.Ladder:
                    case MoveMode.Climb:
                        if (motor.CurrentClimb != null) target = YawOf(motor.CurrentClimb.Forward);
                        break;
                    case MoveMode.Vault:
                    case MoveMode.Mantle:
                        break;
                    default:
                        if (velocity.magnitude > 0.5f) target = YawOf(velocity);
                        else if (Mathf.Abs(Mathf.DeltaAngle(bodyYaw, cameraYaw)) > idleTurnThreshold) target = cameraYaw;
                        break;
                }
                bodyYaw = Mathf.MoveTowardsAngle(bodyYaw, target, turnSpeed * dt);
            }

            visualRoot.SetPositionAndRotation(motor.RenderPosition, Quaternion.Euler(0f, bodyYaw, 0f));
        }

        static float YawOf(Vector3 v)
        {
            return Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
        }

        void OnDrawGizmos()
        {
            // A forward arrow, so the character's front is clear in the Scene view.
            Transform t = visualRoot != null ? visualRoot : transform;
            Vector3 origin = t.position + Vector3.up * 0.05f;
            Vector3 tip = origin + t.forward * 1.2f;
            Gizmos.color = new Color(1f, 0.55f, 0.1f);
            Gizmos.DrawLine(origin, tip);
            Gizmos.DrawLine(tip, tip - t.forward * 0.25f + t.right * 0.15f);
            Gizmos.DrawLine(tip, tip - t.forward * 0.25f - t.right * 0.15f);
        }
    }
}
