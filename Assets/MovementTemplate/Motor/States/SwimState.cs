using UnityEngine;

namespace MovementTemplate
{
    /// <summary>Surface swimming and diving. Edges are climbed out of with the automatic mantle.</summary>
    public class SwimState : MotorState
    {
        public SwimState(PlayerMotor motor) : base(motor) { }

        public override void Enter(MoveMode from)
        {
            M.Velocity = new Vector3(M.Velocity.x * 0.5f, M.Velocity.y * 0.3f, M.Velocity.z * 0.5f);
            M.ForceCrouch = false;
            M.CameraRollTarget = 0f;
        }

        public override void Tick(float dt)
        {
            WaterVolume water = M.CurrentWater;
            if (water == null || M.WaterDepth < P.swimExitDepth)
            {
                M.SetMode(MoveMode.Air);
                return;
            }

            InputFrame f = M.Frame;
            float depth = M.WaterDepth;
            bool atSurface = depth <= P.surfaceFloatDepth + 0.25f;

            // Swim into an edge: climb out.
            if (atSurface && (f.Move.y > 0.3f || f.JumpHeld) && TryAutoLedge(true)) return;

            // Horizontal
            Vector3 wish = M.GetMoveDirection();
            float mag = Mathf.Clamp01(wish.magnitude);
            Vector3 target = mag > 0.01f ? wish.normalized * (mag * P.swimSpeed) : Vector3.zero;
            M.AccelerateHorizontal(target, P.swimSpeed * P.waterDrag * dt);

            // Vertical: jump = up, crouch = dive, otherwise float at the surface.
            float vertical = (f.JumpHeld ? 1f : 0f) - (f.CrouchHeld ? 1f : 0f);
            float floatY = water.SurfaceY - P.surfaceFloatDepth;
            float y = M.transform.position.y;
            float spring = Mathf.Clamp((floatY - y) * 3f, -2f, 2f);

            float targetVy;
            if (vertical > 0.5f) targetVy = y < floatY ? P.swimVerticalSpeed : spring;
            else if (vertical < -0.5f) targetVy = -P.swimVerticalSpeed;
            else targetVy = atSurface ? spring : 0f;

            M.Velocity.y = Mathf.MoveTowards(M.Velocity.y, targetVy, 12f * dt);
            M.MoveCharacter(M.Velocity * dt);
        }
    }
}
