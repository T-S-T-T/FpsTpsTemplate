using UnityEngine;

namespace MovementTemplate
{
    /// <summary>Implemented by anything that can carry or shove the player.</summary>
    public interface IPlayerPusher
    {
        PlayerInteraction Interaction { get; }
        Vector3 Velocity { get; }
    }
}
