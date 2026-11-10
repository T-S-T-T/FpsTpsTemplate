using UnityEngine;

namespace MovementTemplate
{
    /// <summary>Running along a wall tagged with <see cref="WallRunSurface"/>.</summary>
    public class WallRunState : MotorState
    {
        Vector3 wallNormal;
        Collider wallCollider;
        WallRunSurface surface;
        int side;
        float timer;
        Vector3 runDir;
        float runSpeed;

        public WallRunState(PlayerMotor motor) : base(motor) { }

        /// <summary>Called by the air state right before switching to this state.</summary>
        public void Begin(RaycastHit hit, WallRunSurface wall, int sideSign)
        {
            wallNormal = MotorMath.Flat(hit.normal).normalized;
            wallCollider = hit.collider;
            surface = wall;
            side = sideSign;
        }

        public override void Enter(MoveMode from)
        {
            timer = 0f;
            Vector3 flatVelocity = MotorMath.Flat(M.Velocity);
            runDir = Vector3.Cross(wallNormal, Vector3.up);
            if (Vector3.Dot(runDir, flatVelocity) < 0f) runDir = -runDir;
            runDir.Normalize();
            runSpeed = Mathf.Max(P.wallRunSpeed * surface.speedMultiplier, flatVelocity.magnitude);
            M.Velocity = runDir * runSpeed + Vector3.up * Mathf.Clamp(M.Velocity.y, -1f, 2f);
            M.WallSide = side;
            // Wall on the right (+1) tilts the camera left, which is a positive roll.
            M.CameraRollTarget = side * P.wallRunRoll;
        }

        public override void Exit(MoveMode to)
        {
            M.CameraRollTarget = 0f;
            M.WallSide = 0;
            M.WallRunCooldown = P.wallRunCooldown;
            M.LastWallRunCollider = wallCollider;
        }

        public override void Tick(float dt)
        {
            timer += dt;

            bool wallOk = M.ProbeWall(-wallNormal, P.wallCheckDistance + 0.2f, out RaycastHit hit)
                          && Mathf.Abs(hit.normal.y) < 0.3f
                          && hit.collider.GetComponentInParent<WallRunSurface>() != null;
            if (wallOk)
            {
                wallNormal = MotorMath.Flat(hit.normal).normalized;
                Vector3 tangent = Vector3.Cross(wallNormal, Vector3.up);
                if (Vector3.Dot(tangent, runDir) < 0f) tangent = -tangent;
                runDir = tangent.normalized;
                wallCollider = hit.collider;
            }

            // Jump off the wall.
            if (M.JumpBufferTimer > 0f)
            {
                M.Velocity = wallNormal * P.wallJumpHorizontal + runDir * (runSpeed * 0.6f) + Vector3.up * P.wallJumpVertical;
                M.WallJumpChain = 1;
                M.JumpBufferTimer = 0f;
                M.RaiseJumped();
                M.SetMode(MoveMode.Air);
                return;
            }

            float maxTime = P.wallRunMaxTime * surface.maxTimeMultiplier;
            if (M.Grounded && timer > 0.1f)
            {
                M.SetMode(MoveMode.Ground);
                return;
            }
            if (!wallOk || timer > maxTime || M.Frame.CrouchHeld || M.Frame.Move.y < 0.1f)
            {
                M.SetMode(MoveMode.Air);
                return;
            }

            // Blocked by a wall ahead (corner)?
            Vector3 chest = M.transform.position + Vector3.up * (M.Controller.height * 0.55f);
            if (M.RaycastClosest(chest, runDir, M.Controller.radius + 0.4f, out RaycastHit ahead)
                && Vector3.Dot(ahead.normal, runDir) < -0.5f)
            {
                M.SetMode(MoveMode.Air);
                return;
            }

            float vy = Mathf.Max(M.Velocity.y - P.gravity * P.wallRunGravityScale * dt, -3f);
            M.Velocity = new Vector3(runDir.x * runSpeed, vy, runDir.z * runSpeed);
            M.MoveCharacter((M.Velocity - wallNormal) * dt);
        }
    }
}
