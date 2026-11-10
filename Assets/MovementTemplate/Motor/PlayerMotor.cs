using System;
using System.Collections.Generic;
using UnityEngine;

namespace MovementTemplate
{
    /// <summary>
    /// Kinematic character motor. Reads one <see cref="InputFrame"/> per fixed tick and runs a small
    /// state machine (ground, air, slide, vault, mantle, wall-run, ladder, climb, swim).
    /// Presentation (camera, animation) only reads the public properties and events.
    /// </summary>
    [DefaultExecutionOrder(0)]
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Setup")]
        public MovementProfile profile;
        public PlayerInputReader input;
        [Tooltip("Sets Time.fixedDeltaTime to 1/60 so the simulation runs at 60 Hz on every platform.")]
        public bool forceSixtyHertz = true;
        [Tooltip("Respawn at the start position when falling below this height.")]
        public float killY = -50f;

        // ---- Events (animation, audio and VFX subscribe to these) ----
        public event Action Jumped;
        public event Action<float> Landed;
        public event Action Footstep;
        public event Action<MoveMode, MoveMode> ModeChanged;

        // ---- Read-only state for presentation ----
        public CharacterController Controller { get; private set; }
        public MoveMode Mode { get; private set; } = MoveMode.Ground;
        public Stance Stance { get; private set; } = Stance.Standing;
        public InputFrame Frame { get; private set; }
        public float Yaw { get; private set; }
        public float Pitch { get; private set; }
        public float CameraRollTarget { get; set; }
        public int WallSide { get; set; }

        // ---- State shared with the individual states ----
        [NonSerialized] public Vector3 Velocity;
        public bool Grounded { get; private set; }
        public Vector3 GroundNormal { get; private set; } = Vector3.up;
        public float GroundAngle { get; private set; }
        public Collider GroundCollider { get; private set; }
        public MovingPlatform GroundPlatform { get; private set; }
        public float JumpBufferTimer { get; set; }
        public float CoyoteTimer { get; set; }
        public int WallJumpChain { get; set; }
        public Collider LastWallRunCollider { get; set; }
        public float WallRunCooldown { get; set; }
        public float ClimbCooldown { get; set; }
        public bool ForceCrouch { get; set; }
        public ClimbVolume CurrentClimb { get; private set; }
        public WaterVolume CurrentWater { get; private set; }
        public CollisionFlags LastFlags { get; private set; }

        public Vector3 PreviousPosition { get; private set; }
        public Vector3 CurrentPosition { get; private set; }

        public float HorizontalSpeed => MotorMath.Flat(Velocity).magnitude;
        public float EyeHeight => Controller.height - 0.15f;
        public float WaterDepth => CurrentWater != null ? CurrentWater.SurfaceY - transform.position.y : 0f;
        public bool HeadUnderwater => CurrentWater != null && transform.position.y + EyeHeight < CurrentWater.SurfaceY;

        /// <summary>Position to draw at, interpolated between the last two fixed ticks.</summary>
        public Vector3 RenderPosition
        {
            get
            {
                float a = Mathf.Clamp01((Time.time - Time.fixedTime) / Mathf.Max(Time.fixedDeltaTime, 0.0001f));
                return Vector3.Lerp(PreviousPosition, CurrentPosition, a);
            }
        }

        readonly Dictionary<MoveMode, MotorState> states = new Dictionary<MoveMode, MotorState>();
        MotorState current;
        int tick;
        Vector3 spawnPosition;
        float spawnYaw;
        float footstepDistance;

        readonly RaycastHit[] hitBuffer = new RaycastHit[16];
        readonly Collider[] overlapBuffer = new Collider[32];
        Collider cachedPlatformCollider;

        void Awake()
        {
            Controller = GetComponent<CharacterController>();
            if (forceSixtyHertz) Time.fixedDeltaTime = 1f / 60f;
            if (profile == null)
            {
                Debug.LogWarning("PlayerMotor has no MovementProfile; using defaults.", this);
                profile = ScriptableObject.CreateInstance<MovementProfile>();
            }
            if (input == null) input = GetComponent<PlayerInputReader>();

            Controller.radius = profile.capsuleRadius;
            Controller.slopeLimit = profile.slopeLimit;
            Controller.stepOffset = profile.stepOffset;
            Controller.minMoveDistance = 0f;
            SetHeight(profile.standHeight);

            states[MoveMode.Ground] = new GroundState(this);
            states[MoveMode.Air] = new AirState(this);
            states[MoveMode.Slide] = new SlideState(this);
            states[MoveMode.Vault] = new LedgeMoveState(this, MoveMode.Vault);
            states[MoveMode.Mantle] = new LedgeMoveState(this, MoveMode.Mantle);
            states[MoveMode.WallRun] = new WallRunState(this);
            states[MoveMode.Ladder] = new ClimbState(this, MoveMode.Ladder);
            states[MoveMode.Climb] = new ClimbState(this, MoveMode.Climb);
            states[MoveMode.Swim] = new SwimState(this);
            current = states[MoveMode.Ground];
        }

        void Start()
        {
            spawnPosition = transform.position;
            spawnYaw = transform.eulerAngles.y;
            Yaw = spawnYaw;
            PreviousPosition = CurrentPosition = transform.position;
        }

        void Update()
        {
            if (input == null) return;
            if (Cursor.lockState != CursorLockMode.Locked) return; // cursor released (Esc): do not turn the camera
            Vector2 look = input.ReadLook(Time.deltaTime);
            Yaw += look.x;
            Pitch = Mathf.Clamp(Pitch - look.y, -85f, 85f);
            transform.rotation = Quaternion.Euler(0f, Yaw, 0f);
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            tick++;
            Frame = input != null ? input.Consume(tick) : default;
            PreviousPosition = transform.position;

            // Timers
            JumpBufferTimer = Frame.JumpPressed ? profile.jumpBuffer : Mathf.Max(0f, JumpBufferTimer - dt);
            WallRunCooldown = Mathf.Max(0f, WallRunCooldown - dt);
            ClimbCooldown = Mathf.Max(0f, ClimbCooldown - dt);

            bool wasGrounded = Grounded;
            ProbeGround();
            if (Grounded)
            {
                CoyoteTimer = profile.coyoteTime;
                WallJumpChain = 0;
                LastWallRunCollider = null;
            }
            else
            {
                CoyoteTimer = Mathf.Max(0f, CoyoteTimer - dt);
            }
            ScanVolumes();
            CarryWithPlatform();

            current.Tick(dt);

            if (Mode != MoveMode.Vault && Mode != MoveMode.Mantle) ResolvePushers();
            UpdateStance(dt);

            if (transform.position.y < killY) Respawn();
            CurrentPosition = transform.position;
            _ = wasGrounded;
        }

        // =====================================================================
        // State machine
        // =====================================================================

        public void SetMode(MoveMode mode)
        {
            if (mode == Mode) return;
            MoveMode from = Mode;
            current.Exit(mode);
            Mode = mode;
            current = states[mode];
            current.Enter(from);
            ModeChanged?.Invoke(from, mode);
        }

        // =====================================================================
        // Helpers used by states
        // =====================================================================

        public MotorState GetState(MoveMode mode) { return states[mode]; }

        public void SetYaw(float yaw)
        {
            Yaw = yaw;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        public Vector3 GetMoveDirection()
        {
            Vector2 m = Frame.Move;
            Vector3 dir = transform.right * m.x + transform.forward * m.y;
            dir.y = 0f;
            return dir;
        }

        public CollisionFlags MoveCharacter(Vector3 displacement)
        {
            LastFlags = Controller.Move(displacement);
            if ((LastFlags & CollisionFlags.Above) != 0 && Velocity.y > 0f) Velocity.y = 0f;
            return LastFlags;
        }

        public void ApplyGravity(float dt, float scale = 1f, bool allowJumpCut = false)
        {
            float g = profile.gravity * scale;
            if (Velocity.y < 0f) g *= profile.fallGravityMultiplier;
            else if (allowJumpCut && !Frame.JumpHeld) g *= profile.jumpCutMultiplier;
            Velocity.y = Mathf.Max(Velocity.y - g * dt, -profile.terminalSpeed);
        }

        /// <summary>Accelerates the horizontal velocity toward a target, leaving vertical speed alone.</summary>
        public void AccelerateHorizontal(Vector3 target, float maxDelta)
        {
            Vector3 flat = MotorMath.Flat(Velocity);
            flat = Vector3.MoveTowards(flat, MotorMath.Flat(target), maxDelta);
            Velocity.x = flat.x;
            Velocity.z = flat.z;
        }

        public void RaiseJumped() { Jumped?.Invoke(); }
        public void RaiseLanded(float fallSpeed) { Landed?.Invoke(fallSpeed); }

        public void TickFootsteps(float dt, float stride)
        {
            footstepDistance += HorizontalSpeed * dt;
            if (footstepDistance >= stride)
            {
                footstepDistance = 0f;
                Footstep?.Invoke();
            }
        }

        /// <summary>Leave the ground with a jump (adds platform velocity so jumps off moving platforms carry over).</summary>
        public void DoJump(float velocity)
        {
            Velocity.y = velocity;
            if (GroundPlatform != null && GroundPlatform.Interaction == PlayerInteraction.Carry)
                Velocity += GroundPlatform.Velocity;
            JumpBufferTimer = 0f;
            CoyoteTimer = 0f;
            Grounded = false;
            RaiseJumped();
        }

        public void Teleport(Vector3 position, float yaw)
        {
            Controller.enabled = false;
            transform.position = position;
            Controller.enabled = true;
            Velocity = Vector3.zero;
            Yaw = yaw;
            Pitch = 0f;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            PreviousPosition = CurrentPosition = position;
            SetMode(MoveMode.Air);
        }

        public void Respawn() { Teleport(spawnPosition, spawnYaw); }

        // ---- Queries ----

        bool IsSelf(Collider c)
        {
            return c == Controller || c.transform.IsChildOf(transform);
        }

        public bool RaycastClosest(Vector3 origin, Vector3 dir, float dist, out RaycastHit best)
        {
            int n = Physics.RaycastNonAlloc(origin, dir, hitBuffer, dist, profile.groundMask, QueryTriggerInteraction.Ignore);
            return PickClosest(n, out best);
        }

        public bool SphereCastClosest(Vector3 origin, float radius, Vector3 dir, float dist, out RaycastHit best)
        {
            int n = Physics.SphereCastNonAlloc(origin, radius, dir, hitBuffer, dist, profile.groundMask, QueryTriggerInteraction.Ignore);
            return PickClosest(n, out best);
        }

        bool PickClosest(int count, out RaycastHit best)
        {
            best = default;
            float bestDist = float.MaxValue;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit h = hitBuffer[i];
                if (h.collider == null || IsSelf(h.collider)) continue;
                if (h.distance < bestDist)
                {
                    bestDist = h.distance;
                    best = h;
                    found = true;
                }
            }
            return found;
        }

        public bool CapsuleBlocked(Vector3 bottom, Vector3 top, float radius)
        {
            int n = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, overlapBuffer, profile.groundMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (!IsSelf(overlapBuffer[i])) return true;
            }
            return false;
        }

        public bool HasHeadroom(float height)
        {
            float r = Controller.radius;
            Vector3 p = transform.position;
            Vector3 bottom = p + Vector3.up * (r + 0.03f);
            Vector3 top = p + Vector3.up * Mathf.Max(height - r, r + 0.03f);
            return !CapsuleBlocked(bottom, top, r * 0.95f);
        }

        /// <summary>True if a standing capsule fits with its feet at the given position.</summary>
        public bool StandingCapsuleFitsAt(Vector3 feet)
        {
            float r = Controller.radius;
            Vector3 bottom = feet + Vector3.up * (r + 0.05f);
            Vector3 top = feet + Vector3.up * (profile.standHeight - r);
            return !CapsuleBlocked(bottom, top, r * 0.95f);
        }

        /// <summary>Casts sideways/forward from chest height to find a wall.</summary>
        public bool ProbeWall(Vector3 dir, float distance, out RaycastHit hit)
        {
            Vector3 origin = transform.position + Vector3.up * (Controller.height * 0.55f);
            return RaycastClosest(origin, dir.normalized, distance + Controller.radius, out hit);
        }

        // =====================================================================
        // Per-tick probing
        // =====================================================================

        void ProbeGround()
        {
            float r = Controller.radius;
            Vector3 origin = transform.position + Vector3.up * (r + 0.05f);
            bool hit = SphereCastClosest(origin, r * 0.95f, Vector3.down, 0.22f, out RaycastHit h);
            bool wasOnGround = Grounded;
            Grounded = false;
            GroundCollider = null;
            GroundPlatform = null;

            if (hit && Velocity.y <= 0.1f)
            {
                float angle = Vector3.Angle(h.normal, Vector3.up);
                if (angle <= profile.slopeLimit + 1f)
                {
                    Grounded = true;
                    GroundNormal = h.normal;
                    GroundAngle = angle;
                    GroundCollider = h.collider;
                    if (h.collider != cachedPlatformCollider)
                    {
                        cachedPlatformCollider = h.collider;
                        cachedPlatform = h.collider.GetComponentInParent<MovingPlatform>();
                    }
                    GroundPlatform = cachedPlatform;
                }
            }
            if (!Grounded) GroundNormal = Vector3.up;
            _ = wasOnGround;
        }

        MovingPlatform cachedPlatform;

        void ScanVolumes()
        {
            float r = Controller.radius;
            Vector3 p = transform.position;
            Vector3 bottom = p + Vector3.up * r;
            Vector3 top = p + Vector3.up * Mathf.Max(Controller.height - r, r);
            int n = Physics.OverlapCapsuleNonAlloc(bottom, top, r, overlapBuffer, ~0, QueryTriggerInteraction.Collide);

            CurrentClimb = null;
            CurrentWater = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                Collider c = overlapBuffer[i];
                if (!c.isTrigger) continue;
                if (c.TryGetComponent(out ClimbVolume cv))
                {
                    float d = (MotorMath.Flat(c.bounds.center) - MotorMath.Flat(p)).sqrMagnitude;
                    if (d < bestDist) { bestDist = d; CurrentClimb = cv; }
                }
                else if (c.TryGetComponent(out WaterVolume wv))
                {
                    if (CurrentWater == null || wv.SurfaceY > CurrentWater.SurfaceY) CurrentWater = wv;
                }
            }
        }

        void CarryWithPlatform()
        {
            if (!Grounded || GroundPlatform == null) return;
            if (GroundPlatform.Interaction != PlayerInteraction.Carry) return;
            if (Mode == MoveMode.Vault || Mode == MoveMode.Mantle) return;

            Vector3 delta = GroundPlatform.DeltaAt(transform.position);
            if (delta.sqrMagnitude > 0f) Controller.Move(delta);
            float yaw = GroundPlatform.YawDelta();
            if (Mathf.Abs(yaw) > 0.0001f)
            {
                Yaw += yaw;
                transform.rotation = Quaternion.Euler(0f, Yaw, 0f);
            }
        }

        void ResolvePushers()
        {
            float r = Controller.radius;
            Vector3 p = transform.position;
            Vector3 bottom = p + Vector3.up * (r + 0.02f);
            Vector3 top = p + Vector3.up * Mathf.Max(Controller.height - r, r + 0.02f);
            int n = Physics.OverlapCapsuleNonAlloc(bottom, top, r, overlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider col = overlapBuffer[i];
                if (IsSelf(col)) continue;
                IPlayerPusher pusher = col.GetComponentInParent<IPlayerPusher>();
                if (pusher == null || pusher.Interaction != PlayerInteraction.Push) continue;

                if (Physics.ComputePenetration(Controller, transform.position, transform.rotation,
                        col, col.transform.position, col.transform.rotation,
                        out Vector3 dir, out float dist))
                {
                    Controller.Move(dir * (dist + 0.01f));
                    float push = Vector3.Dot(pusher.Velocity, dir);
                    if (push > 0f) Velocity += dir * (push * 0.5f);
                }
            }
        }

        void UpdateStance(float dt)
        {
            bool inCrouchableMode = Mode == MoveMode.Ground || Mode == MoveMode.Air;
            bool wantCrouch = ForceCrouch || (Frame.CrouchHeld && inCrouchableMode);
            float target = wantCrouch ? profile.crouchHeight : profile.standHeight;
            float h = Controller.height;
            float step = profile.stanceChangeSpeed * dt;

            if (target > h)
            {
                float newH = Mathf.MoveTowards(h, target, step);
                if (HasHeadroom(newH)) h = newH;
            }
            else
            {
                h = Mathf.MoveTowards(h, target, step);
            }

            if (!Mathf.Approximately(h, Controller.height)) SetHeight(h);
            Stance = h < (profile.standHeight + profile.crouchHeight) * 0.5f ? Stance.Crouching : Stance.Standing;
        }

        public void SetHeight(float h)
        {
            Controller.height = h;
            Controller.center = new Vector3(0f, h * 0.5f, 0f);
        }

        void OnDrawGizmosSelected()
        {
            if (Controller == null) return;
            Gizmos.color = Grounded ? Color.green : Color.red;
            float r = Controller.radius;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * (r + 0.05f - 0.22f), r * 0.95f);
        }
    }
}
