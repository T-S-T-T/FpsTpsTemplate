using UnityEditor;
using UnityEngine;

namespace MovementTemplate.EditorTools
{
    /// <summary>
    /// Menu: Tools > Movement Template. Creates the two feel profiles, the Player prefab (grey box
    /// character with a marked front, first-person arms, camera) and the test arena scene.
    /// </summary>
    public static class MovementTemplateBuilder
    {
        public const string Root = "Assets/MovementTemplate";
        public const string ProfilesFolder = Root + "/Profiles";
        public const string PlayerFolder = Root + "/Player";
        public const string MaterialsFolder = Root + "/Samples/Materials";
        public const string PlayerPrefabPath = PlayerFolder + "/Player.prefab";

        [MenuItem("Tools/Movement Template/Build Everything")]
        public static void BuildEverything()
        {
            CreateProfiles();
            CreatePlayerPrefab();
            TestArenaBuilder.CreateTestArena();
        }

        // ---------------------------------------------------------------- profiles

        [MenuItem("Tools/Movement Template/1 Create Profiles")]
        public static void CreateProfiles()
        {
            EnsureFolder(ProfilesFolder);
            GetOrCreateProfile("Tactical", false);
            GetOrCreateProfile("Arcade", true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static MovementProfile GetOrCreateProfile(string name, bool arcade)
        {
            string path = ProfilesFolder + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<MovementProfile>(path);
            if (existing != null) return existing;

            EnsureFolder(ProfilesFolder);
            var profile = ScriptableObject.CreateInstance<MovementProfile>();
            if (arcade) profile.ApplyArcade(); else profile.ApplyTactical();
            AssetDatabase.CreateAsset(profile, path);
            return profile;
        }

        // ---------------------------------------------------------------- player prefab

        [MenuItem("Tools/Movement Template/2 Create Player Prefab")]
        public static void CreatePlayerPrefab()
        {
            CreateProfiles();
            EnsureFolder(PlayerFolder);

            var tactical = GetOrCreateProfile("Tactical", false);
            var arcade = GetOrCreateProfile("Arcade", true);

            Material grey = GetMaterial("Grey", new Color(0.62f, 0.63f, 0.66f));
            Material light = GetMaterial("LightGrey", new Color(0.82f, 0.83f, 0.85f));
            Material front = GetMaterial("FrontMarker", new Color(1f, 0.5f, 0.05f));

            // ---- Root with the motor ----
            var root = new GameObject("Player");
            var controller = root.AddComponent<CharacterController>();
            controller.height = tactical.standHeight;
            controller.radius = tactical.capsuleRadius;
            controller.center = new Vector3(0f, tactical.standHeight * 0.5f, 0f);
            controller.slopeLimit = tactical.slopeLimit;
            controller.stepOffset = tactical.stepOffset;

            var reader = root.AddComponent<PlayerInputReader>();
            var motor = root.AddComponent<PlayerMotor>();
            motor.profile = tactical;
            motor.input = reader;

            // ---- Third-person grey box body (front is +Z) ----
            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            var body = new GameObject("Body").transform;
            body.SetParent(visual, false);

            Transform torso = Pivot(body, "TorsoPivot", new Vector3(0f, 0.9f, 0f));
            Part(torso, "Torso", PrimitiveType.Cube, new Vector3(0f, 0.3f, 0f), new Vector3(0.5f, 0.6f, 0.28f), grey);
            Part(torso, "ChestPlate (front marker)", PrimitiveType.Cube, new Vector3(0f, 0.32f, 0.15f), new Vector3(0.3f, 0.25f, 0.03f), front);
            Part(torso, "Head", PrimitiveType.Cube, new Vector3(0f, 0.75f, 0f), new Vector3(0.3f, 0.3f, 0.3f), light);
            Part(torso, "Nose (front marker)", PrimitiveType.Cube, new Vector3(0f, 0.74f, 0.19f), new Vector3(0.1f, 0.1f, 0.12f), front);

            Transform leftArm = Pivot(torso, "LeftArmPivot", new Vector3(-0.33f, 0.55f, 0f));
            Part(leftArm, "LeftArm", PrimitiveType.Cube, new Vector3(0f, -0.27f, 0f), new Vector3(0.14f, 0.54f, 0.14f), grey);
            Transform rightArm = Pivot(torso, "RightArmPivot", new Vector3(0.33f, 0.55f, 0f));
            Part(rightArm, "RightArm", PrimitiveType.Cube, new Vector3(0f, -0.27f, 0f), new Vector3(0.14f, 0.54f, 0.14f), grey);

            Transform leftLeg = Pivot(body, "LeftLegPivot", new Vector3(-0.13f, 0.9f, 0f));
            Part(leftLeg, "LeftLeg", PrimitiveType.Cube, new Vector3(0f, -0.45f, 0f), new Vector3(0.18f, 0.9f, 0.18f), grey);
            Transform rightLeg = Pivot(body, "RightLegPivot", new Vector3(0.13f, 0.9f, 0f));
            Part(rightLeg, "RightLeg", PrimitiveType.Cube, new Vector3(0f, -0.45f, 0f), new Vector3(0.18f, 0.9f, 0.18f), grey);

            // ---- Camera with first-person arms ----
            var cameraObject = new GameObject("Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, tactical.standHeight - 0.15f, 0f);
            var cam = cameraObject.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.fieldOfView = tactical.baseFov;
            cameraObject.AddComponent<AudioListener>();
            var rig = cameraObject.AddComponent<CameraRig>();
            rig.motor = motor;
            rig.input = reader;

            var arms = new GameObject("FpsArms").transform;
            arms.SetParent(cameraObject.transform, false);
            Transform fpsLeft = Pivot(arms, "FpsLeftArm", new Vector3(-0.24f, -0.26f, 0.18f));
            fpsLeft.localRotation = Quaternion.Euler(0f, 6f, 0f);
            Part(fpsLeft, "Arm", PrimitiveType.Cube, new Vector3(0f, 0f, 0.3f), new Vector3(0.1f, 0.1f, 0.6f), grey);
            Part(fpsLeft, "Hand", PrimitiveType.Cube, new Vector3(0f, 0f, 0.62f), new Vector3(0.12f, 0.12f, 0.14f), light);
            Transform fpsRight = Pivot(arms, "FpsRightArm", new Vector3(0.24f, -0.26f, 0.18f));
            fpsRight.localRotation = Quaternion.Euler(0f, -6f, 0f);
            Part(fpsRight, "Arm", PrimitiveType.Cube, new Vector3(0f, 0f, 0.3f), new Vector3(0.1f, 0.1f, 0.6f), grey);
            Part(fpsRight, "Hand", PrimitiveType.Cube, new Vector3(0f, 0f, 0.62f), new Vector3(0.12f, 0.12f, 0.14f), light);

            // ---- Presentation scripts ----
            var presentation = root.AddComponent<CharacterPresentation>();
            presentation.motor = motor;
            presentation.cameraRig = rig;
            presentation.visualRoot = visual;
            presentation.bodyRoot = body.gameObject;
            presentation.armsRoot = arms.gameObject;

            var animation = root.AddComponent<GreyboxAnimationDriver>();
            animation.motor = motor;
            animation.bodyRoot = body;
            animation.torso = torso;
            animation.leftArm = leftArm;
            animation.rightArm = rightArm;
            animation.leftLeg = leftLeg;
            animation.rightLeg = rightLeg;
            animation.fpsArmsRoot = arms;
            animation.fpsLeftArm = fpsLeft;
            animation.fpsRightArm = fpsRight;

            var overlay = root.AddComponent<DebugOverlay>();
            overlay.motor = motor;
            overlay.cameraRig = rig;
            overlay.presets = new[] { tactical, arcade };

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Movement Template: Player prefab saved to " + PlayerPrefabPath);
        }

        // ---------------------------------------------------------------- helpers

        public static Transform Pivot(Transform parent, string name, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        /// <summary>A visual-only primitive (its collider is removed so it cannot block the character controller).</summary>
        public static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var collider = go.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        public static Material GetMaterial(string name, Color color)
        {
            string path = MaterialsFolder + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            EnsureFolder(MaterialsFolder);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader);
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            string leaf = path.Substring(slash + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
