using UnityEngine;

namespace MovementTemplate
{
    /// <summary>Jumping and falling, with coyote time, jump buffering, wall-jump and wall-run entry.</summary>
    public class AirState : MotorState
    {
        public AirState(PlayerMotor motor) : base(motor) { }

        public override void Enter(MoveMode from)
        {
            M.ForceCrouch = false;
        }

        public override void Tick(float dt)
        {
            if (TryEnterSwim()) return;
            if (TryEnterClimb()) return;

            // Landing
            if (M.Grounded && M.Velocity.y <= 0.1f)
            {
                M.RaiseLanded(Mathf.Max(0f, -M.Velocity.y));
                M.Velocity.y = 0f;
                M.SetMode(MoveMode.Ground);
                return;
            }

            // Jump requests while airborne
            if (M.JumpBufferTimer > 0f)
            {
                if (M.CoyoteTimer > 0f) M.DoJump(P.JumpVelocity);
                else TryWallJump();
            }

            if (TryAutoLedge(true)) return;
            if (TryStartWallRun()) return;

            // Air control: steer without gaining speed.
            Vector3 wish = M.GetMoveDirection();
            float mag = Mathf.Clamp01(wish.magnitude);
            if (mag > 0.05f)
            {
                float topSpeed = Mathf.Max(M.HorizontalSpeed, P.walkSpeed);
                float rate = (P.sprintSpeed / Mathf.Max(0.05f, P.timeToTopSpeed)) * P.airControl;
                M.AccelerateHorizontal(wish.normalized * (mag * topSpeed), rate * dt);
            }

            M.ApplyGravity(dt, 1f, true);
            M.MoveCharacter(M.Velocity * dt);

            if ((M.LastFlags & CollisionFlags.Sides) != 0)
            {
                Vector3 actual = M.Controller.velocity;
                M.Velocity.x = actual.x;
                M.Velocity.z = actual.z;
            }
        }

        bool TryWallJump()
        {
            if (M.WallJumpChain >= P.wallJumpMaxChain) return false;

            Transform t = M.transform;
            for (int i = 0; i < 4; i++)
            {
                Vector3 d = i == 0 ? t.right : (i == 1 ? -t.right : (i == 2 ? t.forward : -t.forward));
                if (!M.ProbeWall(d, P.wallCheckDistance, out RaycastHit hit)) continue;
                if (Mathf.Abs(hit.normal.y) > 0.3f) continue;

                Vector3 n = MotorMath.Flat(hit.normal).normalized;
                Vector3 along = Vector3.ProjectOnPlane(MotorMath.Flat(M.Velocity), n) * 0.5f;
                M.Velocity = n * P.wallJumpHorizontal + along + Vector3.up * P.wallJumpVertical;
                M.WallJumpChain++;
                M.JumpBufferTimer = 0f;
                M.RaiseJumped();
                return true;
            }
            return false;
        }

        bool TryStartWallRun()
        {
            if (M.WallRunCooldown > 0f) return false;
            if (M.HorizontalSpeed < P.wallRunMinSpeed) return false;
            if (M.Frame.Move.y < 0.1f) return false;
            // Not right above the floor.
            if (M.RaycastClosest(M.transform.position + Vector3.up * 0.1f, Vector3.down, 0.6f, out RaycastHit floor)) return false;

            Vector3 velocityDir = MotorMath.Flat(M.Velocity).normalized;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 d = M.transform.right * side;
                if (!M.ProbeWall(d, P.wallCheckDistance, out RaycastHit hit)) continue;
                if (Mathf.Abs(hit.normal.y) > 0.3f) continue;

                WallRunSurface surface = hit.collider.GetComponentInParent<WallRunSurface>();
                if (surface == null) continue;                       // only tagged walls are runnable
                if (hit.collider == M.LastWallRunCollider) continue; // same wall blocked until grounded
                if (Mathf.Abs(Vector3.Dot(velocityDir, hit.normal)) > 0.7f) continue; // not head-on

                var run = (WallRunState)M.GetState(MoveMode.WallRun);
                run.Begin(hit, surface, side);
                M.SetMode(MoveMode.WallRun);
                return true;
            }
            return false;
        }
    }
}
