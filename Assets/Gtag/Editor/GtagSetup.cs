#if UNITY_EDITOR
using System.Collections.Generic;
using Photon.VR;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Gtag.EditorTools
{
    /// <summary>
    /// Enforces Linear + Vulkan (no OpenGL) on every editor load, and builds the stump / computer /
    /// leaderboard into the open scene via  Gtag > Setup Scene.
    /// Unity Cloud: run "Gtag > Setup Scene" once, save, commit. (Also callable with
    /// -executeMethod Gtag.EditorTools.GtagSetup.BatchSetup)
    /// </summary>
    [InitializeOnLoad]
    public static class GtagSetup
    {
        static GtagSetup() { EditorApplication.delayCall += EnforceSettings; }

        [MenuItem("Gtag/Enforce Linear + Vulkan")]
        public static void EnforceSettings()
        {
            bool changed = false;
            if (PlayerSettings.colorSpace != ColorSpace.Linear) { PlayerSettings.colorSpace = ColorSpace.Linear; changed = true; }

            // Android / Quest: Vulkan only, no GLES
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            var android = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            if (android.Length != 1 || android[0] != GraphicsDeviceType.Vulkan)
            { PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan }); changed = true; }

            // Windows PC: Direct3D11 (no OpenGLCore)
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            var win = PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64);
            if (win.Length == 0 || win[0] != GraphicsDeviceType.Direct3D11 || System.Array.IndexOf(win, GraphicsDeviceType.OpenGLCore) >= 0)
            { PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 }); changed = true; }

            if (PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
            { PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP); changed = true; }
            if (PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64)
            { PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64; changed = true; }

            if (changed) { AssetDatabase.SaveAssets(); Debug.Log("[Gtag] Enforced Linear color space + Vulkan (Android) / D3D11 (Windows)."); }
        }

        public static void BatchSetup()
        {
            EnforceSettings();
            SetupScene();
            EditorSceneManager.SaveOpenScenes();
        }

        [MenuItem("Gtag/Setup Scene")]
        public static void SetupScene()
        {
            var scene = SceneManager.GetActiveScene();

            // --- one-copy guards on the PhotonVR objects ---
            foreach (var m in Object.FindObjectsByType<PhotonVRManager>(FindObjectsSortMode.None))
                if (m.GetComponent<GtagSingletonGuard>() == null) m.gameObject.AddComponent<GtagSingletonGuard>().key = "PhotonVRManager";
            foreach (var sp in Object.FindObjectsByType<Photon.VR.Player.PlayerSpawner>(FindObjectsSortMode.None))
                if (sp.GetComponent<GtagSingletonGuard>() == null) { var g = sp.gameObject.AddComponent<GtagSingletonGuard>(); g.key = "PlayerSpawner"; }

            // --- stump ---
            var root = GameObject.Find("GtagStump");
            if (root != null) Object.DestroyImmediate(root);   // rebuild cleanly, never duplicate
            root = new GameObject("GtagStump");
            root.transform.position = new Vector3(0, 0, 6);

            var stump = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stump.name = "stump";
            stump.transform.SetParent(root.transform, false);
            stump.transform.localScale = new Vector3(3f, 0.5f, 3f);
            stump.transform.localPosition = new Vector3(0, 0.5f, 0);
            Object.DestroyImmediate(stump.GetComponent<Collider>());
            stump.AddComponent<MeshCollider>().convex = false;

            var spawn = new GameObject("StumpSpawnManager").AddComponent<GtagStumpSpawn>();
            spawn.transform.SetParent(root.transform, false);
            spawn.stump = stump.transform;
            spawn.offset = new Vector3(0, 1.2f, 0);

            // --- computer ---
            var comp = new GameObject("Computer");
            comp.transform.SetParent(root.transform, false);
            comp.transform.localPosition = new Vector3(0, 1.0f, 1.6f);
            comp.transform.localRotation = Quaternion.Euler(0, 180, 0);

            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Panel"; panel.transform.SetParent(comp.transform, false);
            panel.transform.localScale = new Vector3(2.4f, 1.8f, 0.05f);
            Object.DestroyImmediate(panel.GetComponent<Collider>());

            var screen = MakeText("Screen", comp.transform, new Vector3(0, 0.62f, -0.04f), 0.55f, 2.2f, 0.6f);
            var computer = comp.AddComponent<GtagComputer>();
            computer.screen = screen;

            string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            for (int i = 0; i < letters.Length; i++)
            {
                int row = i / 12, col = i % 12;
                MakeKey(comp.transform, letters[i].ToString(), new Vector3(-1.0f + col * 0.18f, 0.18f - row * 0.2f, -0.06f), GtagKeyType.Letter, letters[i].ToString(), Color.white);
            }
            MakeKey(comp.transform, "DEL", new Vector3(0.65f, -0.5f, -0.06f), GtagKeyType.Delete, "", Color.red);
            MakeKey(comp.transform, "OK", new Vector3(0.95f, -0.5f, -0.06f), GtagKeyType.Confirm, "", Color.green);
            Color[] cols = { Color.red, new Color(1, .5f, 0), Color.yellow, Color.green, Color.cyan, Color.blue, new Color(.6f, 0, 1), Color.magenta, Color.white, Color.black };
            for (int i = 0; i < cols.Length; i++)
                MakeKey(comp.transform, "", new Vector3(-0.9f + i * 0.2f, -0.72f, -0.06f), GtagKeyType.Colour, "", cols[i]);

            // --- leaderboard ---
            var lb = new GameObject("Leaderboard");
            lb.transform.SetParent(root.transform, false);
            lb.transform.localPosition = new Vector3(3.2f, 1.6f, 1.6f);
            lb.transform.localRotation = Quaternion.Euler(0, 160, 0);
            var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.name = "Back"; back.transform.SetParent(lb.transform, false);
            back.transform.localScale = new Vector3(2.4f, 2.6f, 0.05f);
            Object.DestroyImmediate(back.GetComponent<Collider>());
            var lbText = MakeText("Text", lb.transform, new Vector3(0, 0, -0.04f), 0.45f, 2.2f, 2.4f);
            lbText.alignment = TextAlignmentOptions.TopLeft;
            lb.AddComponent<GtagLeaderboard>().board = lbText;

            // --- gorilla model hookup ---
            HookModel();

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Gtag] Scene set up. Save the scene (Ctrl+S) and commit it for Unity Cloud.");
        }

        static TextMeshPro MakeText(string name, Transform parent, Vector3 pos, float size, float w, float h)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var t = go.AddComponent<TextMeshPro>();
            t.fontSize = size * 10; t.color = Color.white; t.alignment = TextAlignmentOptions.TopLeft;
            t.rectTransform.sizeDelta = new Vector2(w, h);
            t.enableAutoSizing = false; t.textWrappingMode = TextWrappingModes.NoWrap;
            return t;
        }

        static void MakeKey(Transform parent, string label, Vector3 pos, GtagKeyType type, string letter, Color colour)
        {
            var k = GameObject.CreatePrimitive(PrimitiveType.Cube);
            k.name = "Key_" + (label == "" ? type.ToString() : label);
            k.transform.SetParent(parent, false);
            k.transform.localPosition = pos;
            k.transform.localScale = new Vector3(0.15f, 0.15f, 0.08f);
            var col = k.GetComponent<BoxCollider>(); col.isTrigger = true; col.size = new Vector3(1, 1, 2.5f);
            var key = k.AddComponent<GtagComputerKey>(); key.type = type; key.letter = letter; key.colour = colour;
            var r = k.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.SetColor("_BaseColor", type == GtagKeyType.Letter ? Color.gray : colour);
            r.sharedMaterial = mat;
            if (label != "") { var t = MakeText("Label", k.transform, new Vector3(0, 0, -0.6f), 0.18f, 1f, 1f); t.alignment = TextAlignmentOptions.Center; t.text = label; t.color = Color.black; }
        }

        static void HookModel()
        {
            string[] guids = AssetDatabase.FindAssets("Gorilla_IK_Rig t:Model", new[] { "Assets/Gtag/Models" });
            if (guids.Length == 0)
            {
                Debug.LogWarning("[Gtag] No gorilla model found. Export Gorilla_IK_Rig.blend to FBX (Assets/Gtag/Models/Gorilla_IK_Rig.fbx) - Unity Cloud build machines don't have Blender, so a raw .blend will NOT import there. Then run Gtag > Setup Scene again.");
                return;
            }
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
            string path = "Assets/Resources/PhotonVR/Player.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var old = prefab.transform.Find("GorillaModel");
                if (old != null) Object.DestroyImmediate(old.gameObject);   // replace, never duplicate
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(model, prefab.transform);
                inst.name = "GorillaModel";
                var gm = prefab.GetComponent<GtagPlayerModel>() ?? prefab.AddComponent<GtagPlayerModel>();
                gm.modelRoot = inst.transform;
                var rends = new List<Renderer>(inst.GetComponentsInChildren<SkinnedMeshRenderer>(true));
                gm.tintRenderers = rends.ToArray();
                gm.headTarget = FindByName(inst.transform, "head", "Head_IK", "head_target");
                gm.leftHandTarget = FindByName(inst.transform, "hand.L", "LeftHand_IK", "hand_ik.L", "Hand_L", "L_hand");
                gm.rightHandTarget = FindByName(inst.transform, "hand.R", "RightHand_IK", "hand_ik.R", "Hand_R", "R_hand");
                // hide the template's default body/head/hands on remote players (the gorilla replaces them)
                var pvr = prefab.GetComponent<Photon.VR.Player.PhotonVRPlayer>();
                if (pvr != null) { foreach (var t in new[] { pvr.Head, pvr.Body }) if (t) foreach (var r in t.GetComponentsInChildren<Renderer>(true)) r.enabled = false; }
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }

        static Transform FindByName(Transform root, params string[] names)
        {
            foreach (var n in names)
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (string.Equals(t.name, n, System.StringComparison.OrdinalIgnoreCase)) return t;
            return null;
        }
    }
}
#endif
