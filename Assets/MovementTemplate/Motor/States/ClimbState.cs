using UnityEngine;

namespace MovementTemplate
{
    /// <summary>
    /// Ladders (up and down only) and climbable surfaces (up, down and sideways).
    /// Tops out into an automatic mantle, or a small hop if there is no ledge.
    /// </summary>
    public class ClimbState : MotorState
    {
        readonly MoveMode mode;
        ClimbVolume volume;

        public ClimbState(PlayerMotor motor, MoveMode mode) : base(motor)
        {
            this.mode = mode;
        }

        public override void Enter(MoveMode from)
        {
            volume = M.CurrentClimb;
            M.Velocity = Vector3.zero;
            M.ForceCrouch = false;
            M.CameraRollTarget = 0f;
        }

        public override void Exit(MoveMode to)
        {
            M.ClimbCooldown = Mathf.Max(M.ClimbCooldown, 0.3f);
        }

        public override void Tick(float dt)
        {
            volume = M.CurrentClimb;
            if (volume == null)
            {
                M.SetMode(MoveMode.Air);
                return;
            }

            InputFrame f = M.Frame;
            Vector2 mv = f.Move;
            Vector3 fwd = volume.Forward;
            Vector3 right = Vector3.Cross(Vector3.up, fwd);

            // Leave: jump off the back, let go, or step off at the bottom.
            if (M.JumpBufferTimer > 0f)
            {
                M.Velocity = -fwd * 4f + Vector3.up * (P.JumpVelocity * 0.8f);
                M.JumpBufferTimer = 0f;
                M.ClimbCooldown = 0.4f;
                M.RaiseJumped();
                M.SetMode(MoveMode.Air);
                return;
            }
            if (f.InteractPressed)
            {
                M.Velocity = Vector3.zero;
                M.ClimbCooldown = 0.4f;
                M.SetMode(MoveMode.Air);
                return;
            }
            if (M.Grounded && mv.y < -0.1f)
            {
                M.SetMode(MoveMode.Ground);
                return;
            }

            // Top-out: mantle onto the ledge once it is within reach, or hop over an open top.
            if (mv.y > 0.1f)
            {
                if (LedgeProbe.TryFind(M, fwd, P.mantleMaxHeight, 0.1f, out LedgeProbe.Info info))
                {
                    var mantle = (LedgeMoveState)M.GetState(MoveMode.Mantle);
                    mantle.Prepare(info, fwd);
                    M.SetMode(MoveMode.Mantle);
                    return;
                }
                if (M.transform.position.y >= volume.TopY - 0.1f)
                {
                    M.Velocity = fwd * 2.5f + Vector3.up * 4f;
                    M.SetMode(MoveMode.Air);
                    return;
                }
            }

            float speed = (mode == MoveMode.Ladder ? P.ladderSpeed : P.climbSpeed) * volume.speedMultiplier;
            Vector3 velocity = Vector3.up * (mv.y * speed);
            if (volume.freeStrafe) velocity += right * (mv.x * speed);

            // Stay lined up with the ladder and the wall face.
            Vector3 pos = M.transform.position;
            Bounds b = volume.Trigger.bounds;
            Vector3 correction = Vector3.zero;
            if (!volume.freeStrafe)
            {
                float lateral = Vector3.Dot(pos - b.center, right);
                correction -= right * (lateral * 6f);
            }
            float halfDepth = Mathf.Abs(fwd.x) * b.extents.x + Mathf.Abs(fwd.z) * b.extents.z;
            Vector3 face = b.center + fwd * halfDepth;
            float wantDepth = Vector3.Dot(face - pos, fwd) - (M.Controller.radius + 0.05f);
            correction += fwd * (Mathf.Clamp(wantDepth, -0.5f, 0.5f) * 8f);

            M.Velocity = velocity;
            M.MoveCharacter((velocity + correction) * dt);
        }
    }
}
