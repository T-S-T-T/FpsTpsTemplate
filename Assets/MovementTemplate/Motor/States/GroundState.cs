using UnityEngine;

namespace MovementTemplate
{
    /// <summary>Walking, sprinting and crouching on the ground.</summary>
    public class GroundState : MotorState
    {
        public GroundState(PlayerMotor motor) : base(motor) { }

        public override void Enter(MoveMode from)
        {
            M.ForceCrouch = false;
            M.CameraRollTarget = 0f;
        }

        public override void Tick(float dt)
        {
            if (TryEnterSwim()) return;
            if (TryEnterClimb()) return;

            if (!M.Grounded)
            {
                M.SetMode(MoveMode.Air);
                return;
            }

            InputFrame f = M.Frame;

            if (M.JumpBufferTimer > 0f)
            {
                M.DoJump(P.JumpVelocity);
                M.SetMode(MoveMode.Air);
                return;
            }

            if (f.CrouchPressed && M.HorizontalSpeed >= P.slideMinSpeed)
            {
                M.SetMode(MoveMode.Slide);
                return;
            }

            if (TryAutoLedge(false)) return;

            // ---- Horizontal movement ----
            Vector3 wish = M.GetMoveDirection();
            float inputMag = Mathf.Clamp01(wish.magnitude);
            bool crouching = M.Stance == Stance.Crouching || f.CrouchHeld;
            bool sprinting = !crouching && f.SprintHeld && f.Move.y > 0.1f
                             && Vector2.Angle(Vector2.up, f.Move) <= P.sprintAngleLimit;

            float topSpeed = crouching ? P.crouchSpeed : (sprinting ? P.sprintSpeed : P.walkSpeed);
            Vector3 target = inputMag > 0.01f ? wish.normalized * (inputMag * topSpeed) : Vector3.zero;
            Vector3 flat = MotorMath.Flat(M.Velocity);

            float stopRate = P.sprintSpeed / Mathf.Max(0.01f, P.timeToStop);
            float rate = inputMag > 0.01f ? topSpeed / Mathf.Max(0.01f, P.timeToTopSpeed) : stopRate;
            if (flat.magnitude > target.magnitude + 0.01f) rate = Mathf.Max(rate, stopRate * 0.6f);
            M.AccelerateHorizontal(target, rate * dt);

            // ---- Move along the ground surface ----
            Vector3 flatNow = MotorMath.Flat(M.Velocity);
            Vector3 along = Vector3.ProjectOnPlane(flatNow, M.GroundNormal);
            if (along.sqrMagnitude > 0.0001f) along = along.normalized * flatNow.magnitude;
            M.Velocity.y = 0f;
            M.MoveCharacter((along + Vector3.down * 3f) * dt);

            // Do not keep pushing into walls.
            Vector3 actual = MotorMath.Flat(M.Controller.velocity);
            if (actual.sqrMagnitude < flatNow.sqrMagnitude * 0.25f)
            {
                M.Velocity.x = actual.x;
                M.Velocity.z = actual.z;
            }

            if (flatNow.magnitude > 0.5f)
                M.TickFootsteps(dt, sprinting ? 2.8f : (crouching ? 1.4f : 2.0f));
        }
    }
}
