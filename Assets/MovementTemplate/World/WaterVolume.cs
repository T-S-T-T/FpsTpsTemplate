using UnityEngine;

namespace MovementTemplate
{
    /// <summary>Trigger box whose top face is the water surface.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public class WaterVolume : MonoBehaviour
    {
        public Color underwaterFogColor = new Color(0.1f, 0.3f, 0.45f, 1f);
        public float underwaterFogDensity = 0.08f;

        Collider cachedCollider;

        public Collider Trigger
        {
            get
            {
                if (cachedCollider == null) cachedCollider = GetComponent<Collider>();
                return cachedCollider;
            }
        }

        public float SurfaceY => Trigger.bounds.max.y;

        void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        void Awake()
        {
            Trigger.isTrigger = true;
        }

        public bool ContainsXZ(Vector3 p)
        {
            Bounds b = Trigger.bounds;
            return p.x >= b.min.x && p.x <= b.max.x && p.z >= b.min.z && p.z <= b.max.z;
        }
    }
}
