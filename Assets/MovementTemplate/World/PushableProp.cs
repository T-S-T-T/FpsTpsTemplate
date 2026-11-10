using UnityEngine;

namespace MovementTemplate
{
    /// <summary>
    /// Physics prop. With Push it shoves the player out of the way (and hands over some of its velocity);
    /// with Ignore it is solid but never moves the player.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PushableProp : MonoBehaviour, IPlayerPusher
    {
        public PlayerInteraction interaction = PlayerInteraction.Push;

        Rigidbody body;

        public PlayerInteraction Interaction => interaction;
        public Vector3 Velocity => body != null ? body.linearVelocity : Vector3.zero;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
        }
    }
}
