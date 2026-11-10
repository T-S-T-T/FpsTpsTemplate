using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MovementTemplate.EditorTools
{
    /// <summary>
    /// Builds the greybox test arena: one station per ability. All geometry is plain cubes.
    /// Spawn is at the origin facing +Z. Floor top is y = 0.
    /// </summary>
    public static class TestArenaBuilder
    {
        const string ScenePath = MovementTemplateBuilder.Root + "/Samples/TestArena.unity";

        static Material floorMat, blockMat, wallRunMat, noRunMat, ladderMat, climbMat, waterMat, platformMat, propMat;

        [MenuItem("Tools/Movement Template/3 Create Test Arena Scene")]
        public static void CreateTestArena()
        {
            MovementTemplateBuilder.CreateProfiles();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(MovementTemplateBuilder.PlayerPrefabPath) == null)
                MovementTemplateBuilder.CreatePlayerPrefab();

            floorMat = MovementTemplateBuilder.GetMaterial("Floor", new Color(0.33f, 0.35f, 0.38f));
            blockMat = MovementTemplateBuilder.GetMaterial("Block", new Color(0.7f, 0.7f, 0.73f));
            wallRunMat = MovementTemplateBuilder.GetMaterial("WallRunGreen", new Color(0.25f, 0.7f, 0.35f));
            noRunMat = MovementTemplateBuilder.GetMaterial("NotRunnableRed", new Color(0.75f, 0.25f, 0.25f));
            ladderMat = MovementTemplateBuilder.GetMaterial("LadderYellow", new Color(0.95f, 0.8f, 0.2f));
            climbMat = MovementTemplateBuilder.GetMaterial("ClimbTeal", new Color(0.2f, 0.65f, 0.7f));
            waterMat = MovementTemplateBuilder.GetMaterial("Water", new Color(0.1f, 0.45f, 0.8f));
            platformMat = MovementTemplateBuilder.GetMaterial("Platform", new Color(0.55f, 0.35f, 0.8f));
            propMat = MovementTemplateBuilder.GetMaterial("Prop", new Color(0.9f, 0.55f, 0.2f));

            MovementTemplateBuilder.EnsureFolder(MovementTemplateBuilder.Root + "/Samples");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // The player brings its own camera.
            var defaultCamera = Camera.main;
            if (defaultCamera != null) Object.DestroyImmediate(defaultCamera.gameObject);

            var arena = new GameObject("Arena").transform;
            BuildFloorAndPool(arena);
            BuildRamps(arena);
            BuildLedges(arena);
            BuildVaults(arena);
            BuildWallRun(arena);
            BuildLadderAndClimb(arena);
            BuildPlatforms(arena);
            BuildTunnelAndGap(arena);

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MovementTemplateBuilder.PlayerPrefabPath);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = new Vector3(0f, 0.1f, 0f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Movement Template: test arena saved to " + ScenePath + ". Press Play.");
        }

        // ------------------------------------------------------------ geometry helpers

        static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material material, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = size;
            if (euler.HasValue) go.transform.rotation = Quaternion.Euler(euler.Value);
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        static GameObject Volume(Transform parent, string name, Vector3 center, Vector3 size, System.Type component)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            box.isTrigger = true;
            go.AddComponent(component);
            return go;
        }

        static void Label(Transform parent, string text, Vector3 position)
        {
            var go = new GameObject("Label: " + text);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.characterSize = 0.25f;
            mesh.fontSize = 48;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = Color.white;
        }

        // ------------------------------------------------------------ stations

        static void BuildFloorAndPool(Transform parent)
        {
            var group = new GameObject("Floor and Pool").transform;
            group.SetParent(parent, false);

            // Floor in four pieces around a 12 x 12 pool pit at x 18..30, z -24..-12. Pieces are 4 m thick.
            Box(group, "Floor West", new Vector3(-21f, -2f, 0f), new Vector3(78f, 4f, 120f), floorMat);
            Box(group, "Floor East", new Vector3(45f, -2f, 0f), new Vector3(30f, 4f, 120f), floorMat);
            Box(group, "Floor North", new Vector3(24f, -2f, 24f), new Vector3(12f, 4f, 72f), floorMat);
            Box(group, "Floor South", new Vector3(24f, -2f, -42f), new Vector3(12f, 4f, 36f), floorMat);
            Box(group, "Pool Bottom", new Vector3(24f, -3.5f, -18f), new Vector3(12f, 1f, 12f), floorMat);

            // Water: top face at y = -0.2.
            var water = Volume(group, "Water Volume", new Vector3(24f, -1.6f, -18f), new Vector3(12f, 2.8f, 12f), typeof(WaterVolume));
            water.name = "Water Volume (surface y -0.2)";
            var surface = Box(group, "Water Surface (visual)", new Vector3(24f, -0.2f, -18f), new Vector3(12f, 0.02f, 12f), waterMat);
            Object.DestroyImmediate(surface.GetComponent<Collider>());
            Label(group, "POOL: swim, dive, climb out at the edge", new Vector3(24f, 3f, -12f));
        }

        static void BuildRamps(Transform parent)
        {
            var group = new GameObject("Ramps and Stairs").transform;
            group.SetParent(parent, false);
            float[] angles = { 15f, 30f, 45f, 60f };
            for (int i = 0; i < angles.Length; i++)
            {
                float a = angles[i] * Mathf.Deg2Rad;
                float length = 6f;
                float x = -20f + i * 6f;
                float y = (length * 0.5f) * Mathf.Sin(a) + 0.2f * Mathf.Cos(a) - 0.05f;
                Box(group, "Ramp " + angles[i] + " deg", new Vector3(x, y, 15f), new Vector3(4f, 0.4f, length), blockMat, new Vector3(-angles[i], 0f, 0f));
                Label(group, angles[i] + " deg", new Vector3(x, 1f, 11f));
            }
            for (int i = 0; i < 10; i++)
                Box(group, "Stair " + i, new Vector3(6f, 0.1f + i * 0.2f, 14f + i * 0.4f), new Vector3(3f, 0.2f + i * 0.2f, 0.4f), blockMat);
            Label(group, "STAIRS", new Vector3(6f, 1f, 11f));
        }

        static void BuildLedges(Transform parent)
        {
            var group = new GameObject("Ledges").transform;
            group.SetParent(parent, false);
            float[] heights = { 0.5f, 1.0f, 1.5f, 2.0f };
            for (int i = 0; i < heights.Length; i++)
            {
                float x = -18f + i * 8f;
                Box(group, "Ledge " + heights[i] + " m", new Vector3(x, heights[i] * 0.5f, 32f), new Vector3(4f, heights[i], 3f), blockMat);
                Label(group, heights[i] + " m" + (heights[i] > 1.5f ? " (too high to mantle)" : ""), new Vector3(x, heights[i] + 1f, 29f));
            }
        }

        static void BuildVaults(Transform parent)
        {
            var group = new GameObject("Vaults").transform;
            group.SetParent(parent, false);
            float[] depths = { 0.3f, 1.0f, 2.2f };
            for (int i = 0; i < depths.Length; i++)
            {
                float x = 18f + i * 6f;
                Box(group, "Vault block depth " + depths[i], new Vector3(x, 0.45f, 32f), new Vector3(4f, 0.9f, depths[i]), blockMat);
                Label(group, depths[i] < 1.6f ? "VAULT (" + depths[i] + " m deep)" : "too deep: mantles", new Vector3(x, 2f, 29f));
            }
        }

        static void BuildWallRun(Transform parent)
        {
            var group = new GameObject("Wall-run corridor").transform;
            group.SetParent(parent, false);

            // Path: 4 straight segments, then 3 segments turning 12 degrees each. Walls 3 m either side.
            Vector3 point = new Vector3(0f, 0f, 45f);
            float yaw = 0f;
            for (int i = 0; i < 7; i++)
            {
                if (i >= 4) yaw += 12f;
                Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                Vector3 right = new Vector3(dir.z, 0f, -dir.x);
                Vector3 center = point + dir * 3f;
                bool broken = i == 2; // a gap in the left wall
                for (int side = -1; side <= 1; side += 2)
                {
                    if (broken && side == -1) continue;
                    var wall = Box(group, "Wall-run wall " + i + (side < 0 ? " L" : " R"),
                        center + right * (3f * side) + Vector3.up * 3f, new Vector3(0.5f, 6f, 6f), wallRunMat, new Vector3(0f, yaw, 0f));
                    wall.AddComponent<WallRunSurface>();
                }
                point += dir * 6f;
            }
            Label(group, "WALL-RUN: jump, hold forward and sprint next to a green wall", new Vector3(0f, 7f, 43f));

            // Look-alike wall that is NOT tagged: must not be runnable.
            Box(group, "Untagged wall (not runnable)", new Vector3(12f, 3f, 57f), new Vector3(0.5f, 6f, 24f), noRunMat);
            Label(group, "RED: not runnable", new Vector3(12f, 7f, 43f));
        }

        static void BuildLadderAndClimb(Transform parent)
        {
            var group = new GameObject("Ladder and Climb").transform;
            group.SetParent(parent, false);

            // Ladder tower: south face at z = -12. Volume forward (+Z) points into the tower.
            Box(group, "Ladder Tower", new Vector3(-30f, 3f, -10f), new Vector3(4f, 6f, 4f), blockMat);
            Volume(group, "Ladder Volume", new Vector3(-30f, 3f, -12.5f), new Vector3(1.2f, 6f, 1f), typeof(LadderVolume));
            Box(group, "Ladder rail L", new Vector3(-30.5f, 3f, -12.05f), new Vector3(0.06f, 6f, 0.06f), ladderMat);
            Box(group, "Ladder rail R", new Vector3(-29.5f, 3f, -12.05f), new Vector3(0.06f, 6f, 0.06f), ladderMat);
            for (int i = 0; i < 20; i++)
                Box(group, "Rung " + i, new Vector3(-30f, 0.3f + i * 0.3f, -12.05f), new Vector3(1f, 0.05f, 0.05f), ladderMat);
            Label(group, "LADDER: face it and hold forward (or E)", new Vector3(-30f, 7.5f, -12f));

            // Climbable wall: south face at z = -10.5. Volume forward (+Z) points into the wall.
            Box(group, "Climb Wall", new Vector3(-40f, 3f, -10f), new Vector3(8f, 6f, 1f), climbMat);
            Volume(group, "Climb Surface", new Vector3(-40f, 3f, -11f), new Vector3(7f, 6f, 1f), typeof(ClimbSurface));
            Label(group, "CLIMB WALL: hold forward, move sideways", new Vector3(-40f, 7.5f, -11f));
        }

        static void BuildPlatforms(Transform parent)
        {
            var group = new GameObject("Platforms and Props").transform;
            group.SetParent(parent, false);

            // Carry platforms (player rides them).
            var slider = Box(group, "Carry: sliding platform", new Vector3(0f, 0.15f, -20f), new Vector3(4f, 0.4f, 4f), platformMat);
            var sliderPlatform = slider.AddComponent<MovingPlatform>();
            sliderPlatform.moveOffset = new Vector3(0f, 0f, -14f);
            Label(group, "CARRY: sliding", new Vector3(0f, 2f, -16f));

            var elevator = Box(group, "Carry: elevator", new Vector3(8f, 0.15f, -20f), new Vector3(4f, 0.4f, 4f), platformMat);
            var elevatorPlatform = elevator.AddComponent<MovingPlatform>();
            elevatorPlatform.moveOffset = new Vector3(0f, 4f, 0f);
            Label(group, "CARRY: elevator", new Vector3(8f, 6.5f, -20f));

            var spinner = Box(group, "Carry: spinning disc", new Vector3(14f, 0.15f, -4f), new Vector3(6f, 0.4f, 6f), platformMat);
            var spinPlatform = spinner.AddComponent<MovingPlatform>();
            spinPlatform.moveOffset = Vector3.zero;
            spinPlatform.spinSpeed = 30f;
            Label(group, "CARRY: spinning", new Vector3(14f, 2f, -4f));

            // Push block: shoves the player out of the way. Ignore block: solid but never moves the player.
            var pusher = Box(group, "Push: moving block", new Vector3(-8f, 1f, -30f), new Vector3(2f, 2f, 2f), noRunMat);
            var pushPlatform = pusher.AddComponent<MovingPlatform>();
            pushPlatform.interaction = PlayerInteraction.Push;
            pushPlatform.moveOffset = new Vector3(8f, 0f, 0f);
            pushPlatform.moveDuration = 2.5f;
            Label(group, "PUSH: shoves you", new Vector3(-8f, 3.5f, -30f));

            var ignorer = Box(group, "Ignore: moving block", new Vector3(-8f, 1f, -36f), new Vector3(2f, 2f, 2f), blockMat);
            var ignorePlatform = ignorer.AddComponent<MovingPlatform>();
            ignorePlatform.interaction = PlayerInteraction.Ignore;
            ignorePlatform.moveOffset = new Vector3(8f, 0f, 0f);
            ignorePlatform.moveDuration = 2.5f;
            Label(group, "IGNORE: solid only", new Vector3(-8f, 3.5f, -36f));

            // Physics props.
            for (int i = 0; i < 3; i++)
            {
                var prop = Box(group, "Push prop " + i, new Vector3(-4f + i * 1.5f, 0.5f, -45f), Vector3.one, propMat);
                var body = prop.AddComponent<Rigidbody>();
                body.mass = 20f;
                prop.AddComponent<PushableProp>().interaction = PlayerInteraction.Push;
            }
            Label(group, "PROPS (Push)", new Vector3(-2f, 2f, -45f));
        }

        static void BuildTunnelAndGap(Transform parent)
        {
            var group = new GameObject("Tunnel and Gap").transform;
            group.SetParent(parent, false);

            // Crouch-only tunnel: 1.3 m clearance (standing height is 1.8, crouch is 1.1).
            Box(group, "Tunnel roof", new Vector3(36f, 1.45f, 15f), new Vector3(3f, 0.3f, 8f), blockMat);
            Box(group, "Tunnel wall L", new Vector3(34.35f, 0.75f, 15f), new Vector3(0.3f, 1.5f, 8f), blockMat);
            Box(group, "Tunnel wall R", new Vector3(37.65f, 0.75f, 15f), new Vector3(0.3f, 1.5f, 8f), blockMat);
            Label(group, "CROUCH TUNNEL", new Vector3(36f, 3f, 10f));

            // Tight gap: 1.0 m wide between two walls.
            Box(group, "Gap wall L", new Vector3(43.5f, 1.5f, 15f), new Vector3(2f, 3f, 0.6f), blockMat);
            Box(group, "Gap wall R", new Vector3(46.5f, 1.5f, 15f), new Vector3(2f, 3f, 0.6f), blockMat);
            Label(group, "TIGHT GAP (1 m)", new Vector3(45f, 3.8f, 12f));
        }
    }
}
