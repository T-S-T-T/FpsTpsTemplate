using UnityEngine;

namespace MovementTemplate
{
    /// <summary>
    /// Put this on any wall the player may wall-run on. Walls without it are never runnable.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WallRunSurface : MonoBehaviour
    {
        [Tooltip("Scales the profile's maximum wall-run time on this wall.")]
        public float maxTimeMultiplier = 1f;
        [Tooltip("Scales the profile's wall-run speed on this wall.")]
        public float speedMultiplier = 1f;
    }
}
