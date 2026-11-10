using UnityEngine;

namespace MovementTemplate
{
    /// <summary>
    /// Scripted vault or mantle. The character controller is switched off while the position follows
    /// an eased curve, then switched back on. One instance per mode.
    /// </summary>
    public class LedgeMoveState : MotorState
    {
        readonly MoveMode mode;
        LedgeProbe.Info info;
        Vector3 direction;
        Vector3 start;
        float t;
        float duration;
        float entrySpeed;

        public LedgeMoveState(PlayerMotor motor, MoveMode mode) : base(motor)
        {
            this.mode = mode;
        }

        public void Prepare(LedgeProbe.Info ledge, Vector3 moveDirection)
        {
            info = ledge;
            direction = moveDirection;
        }

        public override void Enter(MoveMode from)
        {
            start = M.transform.position;
            t = 0f;
            duration = mode == MoveMode.Vault ? P.vaultDuration : P.mantleDuration;
            entrySpeed = Mathf.Max(M.HorizontalSpeed, P.walkSpeed);
            M.ForceCrouch = false;
            M.Velocity = Vector3.zero;
            M.Controller.enabled = false;
        }

        public override void Exit(MoveMode to)
        {
            M.Controller.enabled = true;
            M.Velocity = mode == MoveMode.Vault
                ? new Vector3(direction.x, 0f, direction.z) * entrySpeed
                : Vector3.zero;
        }

        public override void Tick(float dt)
        {
            t += dt / Mathf.Max(0.05f, duration);
            float k = Mathf.Clamp01(t);
            Vector3 pos;

            if (mode == MoveMode.Vault)
            {
                Vector3 end = info.Landing;
                float h = MotorMath.EaseInOut(k);
                float apex = info.TopY + 0.05f;
                float y = k < 0.5f
                    ? Mathf.Lerp(start.y, apex, MotorMath.EaseInOut(k * 2f))
                    : Mathf.Lerp(apex, end.y, MotorMath.EaseInOut((k - 0.5f) * 2f));
                pos = new Vector3(Mathf.Lerp(start.x, end.x, h), y, Mathf.Lerp(start.z, end.z, h));
            }
            else
            {
                Vector3 end = info.Target;
                float v = MotorMath.EaseInOut(Mathf.Clamp01(k / 0.6f));
                float h = MotorMath.EaseInOut(Mathf.Clamp01((k - 0.4f) / 0.6f));
                pos = new Vector3(Mathf.Lerp(start.x, end.x, h), Mathf.Lerp(start.y, end.y, v), Mathf.Lerp(start.z, end.z, h));
            }

            M.transform.position = pos;
            if (t >= 1f) M.SetMode(MoveMode.Ground);
        }
    }
}
