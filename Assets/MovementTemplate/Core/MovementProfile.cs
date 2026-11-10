using UnityEngine;

namespace MovementTemplate
{
    /// <summary>
    /// Every tunable movement and camera number lives here. Defaults are the Tactical preset.
    /// Create assets via Assets > Create > Movement Template > Movement Profile, or let the builder make them.
    /// </summary>
    [CreateAssetMenu(menuName = "Movement Template/Movement Profile", fileName = "MovementProfile")]
    public class MovementProfile : ScriptableObject
    {
        [Header("Ground")]
        public float walkSpeed = 3.5f;
        public float sprintSpeed = 6f;
        public float crouchSpeed = 2f;
        [Tooltip("Seconds to reach full speed.")] public float timeToTopSpeed = 0.30f;
        [Tooltip("Seconds to stop from full speed.")] public float timeToStop = 0.20f;
        [Tooltip("Sprint only works within this angle of forward.")] public float sprintAngleLimit = 45f;
        public float slopeLimit = 50f;
        public float stepOffset = 0.35f;
        public LayerMask groundMask = ~0;

        [Header("Stance")]
        public float standHeight = 1.8f;
        public float crouchHeight = 1.1f;
        public float capsuleRadius = 0.35f;
        public float stanceChangeSpeed = 10f;

        [Header("Air")]
        public float gravity = 20f;
        public float fallGravityMultiplier = 1.3f;
        public float terminalSpeed = 50f;
        [Range(0f, 1f)] public float airControl = 0.2f;
        public float jumpHeight = 1.0f;
        public float coyoteTime = 0.10f;
        public float jumpBuffer = 0.10f;
        [Tooltip("Extra gravity when jump is released early (variable jump height).")]
        public float jumpCutMultiplier = 2.5f;

        [Header("Slide")]
        public float slideMinSpeed = 4.5f;
        public float slideEntryBoost = 1.1f;
        public float slideFriction = 3f;
        public float slideMaxDuration = 0.8f;
        public float slideEndSpeed = 2.5f;
        public float slideJumpBoost = 1.1f;

        [Header("Vault")]
        public float vaultMaxHeight = 1.0f;
        public float vaultMaxDepth = 1.5f;
        public float vaultDuration = 0.45f;
        public float vaultMinSpeed = 1.5f;

        [Header("Mantle (automatic)")]
        [Tooltip("Ledges at or below this height are mantled automatically, including mid-jump.")]
        public float mantleMaxHeight = 1.5f;
        public float mantleReach = 0.7f;
        public float mantleDuration = 0.5f;

        [Header("Wall-run / wall-jump")]
        public float wallRunMinSpeed = 4f;
        public float wallRunMaxTime = 1.5f;
        public float wallRunSpeed = 6f;
        [Tooltip("Fraction of gravity applied while wall-running.")] public float wallRunGravityScale = 0.1f;
        public float wallCheckDistance = 0.6f;
        public float wallRunCooldown = 0.25f;
        public float wallJumpHorizontal = 7f;
        public float wallJumpVertical = 6f;
        public int wallJumpMaxChain = 2;

        [Header("Ladder / climb")]
        public float ladderSpeed = 3f;
        public float climbSpeed = 2.5f;

        [Header("Swim")]
        public float swimSpeed = 3f;
        public float swimVerticalSpeed = 2.5f;
        [Tooltip("Water depth (feet to surface) at which swimming starts.")] public float swimEnterDepth = 1.1f;
        public float swimExitDepth = 0.6f;
        [Tooltip("How far below the surface the feet float when idle.")] public float surfaceFloatDepth = 1.15f;
        public float waterDrag = 4f;

        [Header("Camera")]
        public float baseFov = 75f;
        public float sprintFovKick = 6f;
        public float slideFovKick = 10f;
        public float headBobScale = 1f;
        public float headBobFrequency = 1.7f;
        public float headBobAmplitude = 0.035f;
        public float landingDipScale = 1f;
        public float wallRunRoll = 15f;
        public float strafeRoll = 1.5f;
        public float tpsDistance = 3.2f;
        public float tpsShoulderOffset = 0.55f;
        public float tpsHeightOffset = 0.2f;

        public float JumpVelocity => Mathf.Sqrt(2f * gravity * jumpHeight);

        [ContextMenu("Apply Tactical Preset")]
        public void ApplyTactical()
        {
            walkSpeed = 3.5f; sprintSpeed = 6f; crouchSpeed = 2f;
            timeToTopSpeed = 0.30f; timeToStop = 0.20f;
            airControl = 0.2f; jumpHeight = 1.0f; gravity = 20f;
            coyoteTime = 0.10f; jumpBuffer = 0.10f;
            slideEntryBoost = 1.1f; slideMaxDuration = 0.8f;
            wallRunMaxTime = 1.5f; wallRunSpeed = 6f;
            baseFov = 75f; headBobScale = 1f;
        }

        [ContextMenu("Apply Arcade Preset")]
        public void ApplyArcade()
        {
            walkSpeed = 5f; sprintSpeed = 9f; crouchSpeed = 3.5f;
            timeToTopSpeed = 0.12f; timeToStop = 0.08f;
            airControl = 0.6f; jumpHeight = 1.4f; gravity = 24f;
            coyoteTime = 0.15f; jumpBuffer = 0.15f;
            slideEntryBoost = 1.3f; slideMaxDuration = 1.5f;
            wallRunMaxTime = 3f; wallRunSpeed = 9f;
            baseFov = 90f; headBobScale = 0.4f;
        }
    }
}
