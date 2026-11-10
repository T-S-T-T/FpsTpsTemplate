using UnityEngine;

namespace MovementTemplate
{
    /// <summary>Sprint-slide: entry boost, friction decay, downhill gain, jump to cancel.</summary>
    public class SlideState : MotorState
    {
        Vector3 direction;
        float speed;
        float timer;

        public SlideState(PlayerMotor motor) : base(motor) { }

        public override void Enter(MoveMode from)
        {
            Vector3 flat = MotorMath.Flat(M.Velocity);
            direction = flat.sqrMagnitude > 0.01f ? flat.normalized : M.transform.forward;
            speed = flat.magnitude * P.slideEntryBoost;
            timer = 0f;
            M.ForceCrouch = true;
        }

        public override void Exit(MoveMode to)
        {
            M.ForceCrouch = false;
        }

        public override void Tick(float dt)
        {
            timer += dt;

            if (!M.Grounded)
            {
                M.Velocity = direction * speed + Vector3.up * M.Velocity.y;
                M.SetMode(MoveMode.Air);
                return;
            }

            // Jump out of the slide, keeping the speed.
            if (M.JumpBufferTimer > 0f)
            {
                M.Velocity = direction * (speed * P.slideJumpBoost);
                M.DoJump(P.JumpVelocity);
                M.SetMode(MoveMode.Air);
                return;
            }

            // Friction, with extra speed when going downhill.
            speed -= P.slideFriction * dt;
            Vector3 downSlope = Vector3.ProjectOnPlane(Vector3.down, M.GroundNormal);
            float downhill = 0f;
            if (downSlope.sqrMagnitude > 0.0001f)
                downhill = Vector3.Dot(downSlope.normalized, direction);
            bool goingDownhill = downhill > 0.05f && M.GroundAngle > 3f;
            if (goingDownhill)
                speed += downhill * P.gravity * Mathf.Sin(M.GroundAngle * Mathf.Deg2Rad) * dt;

            bool ended = speed < P.slideEndSpeed
                         || !M.Frame.CrouchHeld
                         || (timer > P.slideMaxDuration && !goingDownhill);
            if (ended)
            {
                M.Velocity = direction * Mathf.Max(speed, 0f);
                M.SetMode(MoveMode.Ground);
                return;
            }

            Vector3 flat = direction * speed;
            Vector3 along = Vector3.ProjectOnPlane(flat, M.GroundNormal);
            if (along.sqrMagnitude > 0.0001f) along = along.normalized * flat.magnitude;
            M.Velocity = flat;
            M.MoveCharacter((along + Vector3.down * 3f) * dt);

            // Hit something: stop sliding.
            Vector3 actual = MotorMath.Flat(M.Controller.velocity);
            if (actual.magnitude < speed * 0.4f)
            {
                M.Velocity = actual;
                M.SetMode(MoveMode.Ground);
            }
        }
    }
}
