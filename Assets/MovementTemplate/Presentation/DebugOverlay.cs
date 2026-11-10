using UnityEngine;
using UnityEngine.InputSystem;

namespace MovementTemplate
{
    /// <summary>
    /// On-screen debug readout. F1 toggles it. F2 / F3 switch between the profiles in the Presets list
    /// at runtime so you can compare Tactical and Arcade feel instantly.
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        public PlayerMotor motor;
        public CameraRig cameraRig;
        public MovementProfile[] presets;
        public bool visible = true;

        float smoothedFps = 60f;
        GUIStyle style;

        void Update()
        {
            smoothedFps = Mathf.Lerp(smoothedFps, 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f), 0.05f);
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.f1Key.wasPressedThisFrame) visible = !visible;
            if (presets != null && motor != null)
            {
                if (kb.f2Key.wasPressedThisFrame && presets.Length > 0 && presets[0] != null) motor.profile = presets[0];
                if (kb.f3Key.wasPressedThisFrame && presets.Length > 1 && presets[1] != null) motor.profile = presets[1];
            }
        }

        void OnGUI()
        {
            if (!visible || motor == null) return;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true };
                style.normal.textColor = Color.white;
            }

            string text =
                $"<b>State</b>  {motor.Mode}   <b>Stance</b>  {motor.Stance}\n" +
                $"<b>Speed</b>  {motor.HorizontalSpeed:0.00} m/s   <b>Vertical</b>  {motor.Velocity.y:0.00}\n" +
                $"<b>Grounded</b>  {motor.Grounded}   <b>Ground angle</b>  {motor.GroundAngle:0}\n" +
                $"<b>Wall jumps used</b>  {motor.WallJumpChain}   <b>Water depth</b>  {motor.WaterDepth:0.00}\n" +
                $"<b>Profile</b>  {(motor.profile != null ? motor.profile.name : "none")}\n" +
                $"<b>View</b>  {(cameraRig != null ? cameraRig.Perspective.ToString() : "-")}   {smoothedFps:0} fps\n" +
                "F1 overlay   F2/F3 presets   V view   Q shoulder   Esc cursor";

            GUI.Box(new Rect(10, 10, 440, 130), GUIContent.none);
            GUI.Label(new Rect(18, 14, 430, 126), text, style);
        }
    }
}
