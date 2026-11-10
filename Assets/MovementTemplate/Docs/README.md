# Movement Template (FPS / TPS)

A drop-in movement layer for Unity 6 (6000.6.8f1, URP, Input System). One kinematic motor, one camera rig that
switches between first and third person, and a greybox test arena with a station for every ability.

## Quickstart (5 minutes)

1. Let Unity finish importing and compiling. Fix any red errors in the Console first (see "Known limits").
2. Check **Edit > Project Settings > Player > Active Input Handling** is **Input System Package (New)** or **Both**.
3. Run **Tools > Movement Template > Build Everything**. This creates:
   - `Profiles/Tactical.asset` and `Profiles/Arcade.asset`
   - `Player/Player.prefab` (grey box character, first-person arms, camera)
   - `Samples/TestArena.unity`
4. Open `Samples/TestArena.unity` and press Play.

To use the player in your own scene, drag `Player/Player.prefab` in. It brings its own camera, so delete the scene's Main Camera.

## Controls

| Action | Keyboard and mouse | Gamepad |
| --- | --- | --- |
| Move / look | WASD / mouse | Left stick / right stick |
| Jump, wall-jump | Space | A |
| Sprint (hold) | Left Shift | Left stick click |
| Crouch, slide (hold) | C or Left Ctrl | B |
| Interact (ladder, let go of climb) | E | X |
| Swim up / dive | Space / Left Ctrl | A / B |
| Switch FPS / TPS | V | D-pad up |
| Shoulder swap (TPS) | Q | D-pad left |
| Debug overlay / presets | F1 / F2, F3 | |
| Release cursor | Esc | |

Mantle and vault are automatic: move into a ledge at or below the mantle height (1.5 m by default), on the ground or mid-jump.
Slide: sprint, then press crouch. Wall-run: be airborne next to a green wall while sprinting forward.

## How it is organised

| Folder | What is in it |
| --- | --- |
| `Core` | Enums, `InputFrame`, `MovementProfile` (every tunable number) |
| `Input` | `PlayerInputReader` (keyboard, mouse, gamepad; touch UI can write to its External fields) |
| `Motor` | `PlayerMotor`, one small class per state in `States/`, `LedgeProbe` |
| `Camera` | `CameraRig` (FPS/TPS blend, bob, roll, FOV kick, landing dip, underwater fog) |
| `Presentation` | `CharacterPresentation`, `GreyboxAnimationDriver`, `DebugOverlay` |
| `World` | `WallRunSurface`, `LadderVolume`, `ClimbSurface`, `WaterVolume`, `MovingPlatform`, `PushableProp` |
| `Editor` | The builder menu |

Data flow: input is sampled into an `InputFrame` each 60 Hz tick, the motor turns state plus frame into new state,
and the camera and character only read the result (interpolated between ticks).

## Level setup

- **Wall-run:** add `WallRunSurface` to a wall. Walls without it are never runnable.
- **Ladder:** add a trigger box with `LadderVolume`. Its **blue (forward) axis points into the wall**, and the box should touch the wall face.
- **Climbable wall:** same, with `ClimbSurface`.
- **Water:** a trigger box with `WaterVolume`. The top face is the surface.
- **Moving platform:** add `MovingPlatform`. Set Interaction to Carry (rides), Push (shoves the player) or Ignore (solid only).
- **Physics prop:** add a Rigidbody and `PushableProp`.

## Tuning

Select `Tactical` or `Arcade` and edit it while playing, or press F2 / F3 in play mode to switch. Create more with
**Assets > Create > Movement Template > Movement Profile**. The mantle height is `mantleMaxHeight`.

## Replacing the character and animations

The grey box is a placeholder. Its front is marked by an orange nose block and an orange chest plate, plus an orange
arrow gizmo in the Scene view. For a real game:

1. Replace the contents of `Visual/Body` in the prefab with your model (keep `Visual` as the parent).
2. Delete or disable `GreyboxAnimationDriver` and drive your own Animator from `PlayerMotor` properties and events
   (`Mode`, `Stance`, `HorizontalSpeed`, `Grounded`, `Velocity`, `Jumped`, `Landed`, `Footstep`, `ModeChanged`).
3. Replace `FpsArms` (child of the camera) with your own arms.

## Stamina and other optional features

Stamina is off in v1. Sprint, climb and swim are single places in the states, so a game that wants stamina can add a
resource check there without touching the rest of the motor.

## Known limits

- Written without access to a Unity compiler. The first import may show compile errors; send them over and they get fixed.
- First-person arms are drawn by the main camera, so they can clip into walls. A URP camera-stack overlay is the usual fix.
- Touch controls: the input reader has External fields for a virtual stick and look drag, but the on-screen UI is not built yet.
- No `AnimationSet` ScriptableObject yet; see "Replacing the character".
- Netcode: the motor reads one `InputFrame` per fixed tick, but look is applied per rendered frame and physics queries are not
  deterministic, so prediction and rollback will need tolerance and some rework.
- Single assembly (no asmdefs yet).
