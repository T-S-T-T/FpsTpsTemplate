using UnityEngine;

namespace MovementTemplate
{
    /// <summary>Which movement state the motor is currently in.</summary>
    public enum MoveMode
    {
        Ground,
        Air,
        Slide,
        Vault,
        Mantle,
        WallRun,
        Ladder,
        Climb,
        Swim
    }

    public enum Stance
    {
        Standing,
        Crouching
    }

    public enum PerspectiveMode
    {
        FirstPerson,
        ThirdPerson
    }

    /// <summary>How a moving platform or prop affects the player.</summary>
    public enum PlayerInteraction
    {
        /// <summary>Player rides it (velocity and rotation are transferred).</summary>
        Carry,
        /// <summary>Player is shoved out of the way when it moves into them.</summary>
        Push,
        /// <summary>Solid, but never moves the player.</summary>
        Ignore
    }

    /// <summary>
    /// Everything the motor reads from input for one simulation tick.
    /// Keeping this a plain struct lets it be recorded, replayed or sent over a network later.
    /// </summary>
    public struct InputFrame
    {
        public int Tick;
        public Vector2 Move;
        public bool JumpPressed;
        public bool JumpHeld;
        public bool SprintHeld;
        public bool CrouchHeld;
        public bool CrouchPressed;
        public bool InteractPressed;
    }

    /// <summary>Static helpers shared by several states.</summary>
    public static class MotorMath
    {
        public static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        public static float EaseInOut(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        /// <summary>Frame-rate independent exponential smoothing factor.</summary>
        public static float Damp(float rate, float dt)
        {
            return 1f - Mathf.Exp(-rate * dt);
        }
    }
}
