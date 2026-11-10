using UnityEngine;

namespace MovementTemplate
{
    /// <summary>
    /// Base for ladders and climbable surfaces. A trigger box marks where the player may climb.
    /// The volume's blue (forward) axis points INTO the wall, so the player climbs while facing along it.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public abstract class ClimbVolume : MonoBehaviour
    {
        [Tooltip("True = move sideways as well as up and down (climbable wall). False = ladder.")]
        public bool freeStrafe;
        public float speedMultiplier = 1f;

        Collider cachedCollider;

        public Collider Trigger
        {
            get
            {
                if (cachedCollider == null) cachedCollider = GetComponent<Collider>();
                return cachedCollider;
            }
        }

        public Vector3 Forward
        {
            get
            {
                Vector3 f = transform.forward;
                f.y = 0f;
                return f.sqrMagnitude < 0.0001f ? Vector3.forward : f.normalized;
            }
        }

        public float TopY => Trigger.bounds.max.y;
        public float BottomY => Trigger.bounds.min.y;

        protected virtual void Reset()
        {
            var box = GetComponent<BoxCollider>();
            box.isTrigger = true;
        }

        protected virtual void Awake()
        {
            Trigger.isTrigger = true;
        }

        /// <summary>Closest point on the volume's centre plane to a world position, flattened to the XZ plane.</summary>
        public Vector3 PlanePoint(Vector3 worldPos)
        {
            Vector3 c = Trigger.bounds.center;
            Vector3 fwd = Forward;
            // Wall face = the volume's far side along Forward.
            float half = Mathf.Abs(Vector3.Dot(Trigger.bounds.extents, new Vector3(Mathf.Abs(fwd.x), 0f, Mathf.Abs(fwd.z))));
            Vector3 facePoint = c + fwd * half;
            float along = Vector3.Dot(worldPos - facePoint, fwd);
            return worldPos - fwd * along;
        }
    }
}
