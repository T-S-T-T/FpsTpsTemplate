using UnityEngine;

namespace MovementTemplate
{
    /// <summary>Base class for every movement state. One small class per state.</summary>
    public abstract class MotorState
    {
        protected readonly PlayerMotor M;
        protected MovementProfile P => M.profile;

        protected MotorState(PlayerMotor motor)
        {
            M = motor;
        }

        public virtual void Enter(MoveMode from) { }
        public virtual void Exit(MoveMode to) { }
        public abstract void Tick(float dt);

        // ---- Transitions shared by several states ----

        protected bool TryEnterSwim()
        {
            if (M.CurrentWater == null || M.WaterDepth < P.swimEnterDepth) return false;
            M.SetMode(MoveMode.Swim);
            return true;
        }

        protected bool TryEnterClimb()
        {
            if (M.ClimbCooldown > 0f) return false;
            ClimbVolume volume = M.CurrentClimb;
            if (volume == null) return false;

            bool facing = Vector3.Dot(M.transform.forward, volume.Forward) > 0.5f;
            bool intent = M.Frame.InteractPressed || M.Frame.Move.y > 0.5f;
            if (!facing || !intent) return false;

            M.SetMode(volume.freeStrafe ? MoveMode.Climb : MoveMode.Ladder);
            return true;
        }

        /// <summary>
        /// Automatic vault or mantle: if the player is moving into a ledge at or below the mantle
        /// threshold, start the move. Works on the ground, mid-jump and when swimming to an edge.
        /// </summary>
        protected bool TryAutoLedge(bool airborne)
        {
            Vector3 dir = M.GetMoveDirection();
            if (dir.sqrMagnitude < 0.04f) return false;
            dir.Normalize();

            float minHeight = airborne ? 0.1f : P.stepOffset + 0.05f;
            if (!LedgeProbe.TryFind(M, dir, P.mantleMaxHeight, minHeight, out LedgeProbe.Info info)) return false;

            bool vault = info.CanVault
                         && info.Height <= P.vaultMaxHeight
                         && M.HorizontalSpeed >= P.vaultMinSpeed;

            var state = (LedgeMoveState)M.GetState(vault ? MoveMode.Vault : MoveMode.Mantle);
            state.Prepare(info, dir);
            M.SetMode(vault ? MoveMode.Vault : MoveMode.Mantle);
            return true;
        }
    }
}
