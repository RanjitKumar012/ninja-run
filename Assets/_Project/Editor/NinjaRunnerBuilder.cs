using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace NinjaRunner.EditorTools
{
    /// <summary>
    /// Editor-only tool that builds the complete Ninja Runner scene: materials, meshes,
    /// prefabs (road segment, coin, 4 obstacles), the ninja, lighting, post-processing,
    /// UI and all Inspector references.
    ///
    /// Menu:  Ninja Runner > 1. Import TextMeshPro Essentials   (first time only)
    ///        Ninja Runner > 2. Build Game Scene
    ///
    /// Everything it creates is a normal Unity asset / GameObject, so you can open and
    /// edit every prefab, material and component afterwards.
    /// </summary>
    public static class NinjaRunnerBuilder
    {
        private const string Root = "Assets/_Project";
        private const string ScenePath = Root + "/Scenes/NinjaRunner.unity";

        // ====================================================================
        // Menu items
        // ====================================================================

        [MenuItem("Ninja Runner/1. Import TextMeshPro Essentials", priority = 1)]
        public static void ImportTmpEssentials()
        {
            if (TmpEssentialsPresent())
            {
                Debug.Log("[Ninja Runner] TextMeshPro Essentials are already in the project.");
                return;
            }

            var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui/package.json");
            if (info == null)
            {
                Debug.LogError("[Ninja Runner] Could not find the com.unity.ugui package.");
                return;
            }

            string package = Path.Combine(info.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            AssetDatabase.ImportPackage(package, false);
            Debug.Log("[Ninja Runner] Importing TextMeshPro Essentials... then run 'Ninja Runner > 2. Build Game Scene'.");
        }

        [MenuItem("Ninja Runner/2. Build Game Scene", priority = 2)]
        public static void BuildGameScene()
        {
            if (!TmpEssentialsPresent())
            {
                ImportTmpEssentials();
                Debug.LogWarning("[Ninja Runner] TextMeshPro Essentials were missing and are being imported. " +
                                 "Run 'Ninja Runner > 2. Build Game Scene' again in a few seconds.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // New scene first: opening a scene unloads unused assets, so create assets afterwards.
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            CreateFolders();
            var m = new MaterialLibrary();
            Mesh cone = CreateConeMesh("Cone", 24);
            Mesh pyramid = CreateConeMesh("Pyramid", 4);

            // ---- Prefabs (built in the scene, saved as assets, then removed from the scene) ----
            BuildRoadSegmentPrefab(m, pyramid);
            BuildCoinPrefab(m);
            BuildCratesPrefab(m);
            BuildStoneBarrierPrefab(m);
            BuildRoadblockPrefab(m);
            BuildSpikeTrapPrefab(m, pyramid);
            AssetDatabase.SaveAssets();

            RoadSegment segmentPrefab = LoadPrefab<RoadSegment>("Environment/RoadSegment");
            Coin coinPrefab = LoadPrefab<Coin>("Collectibles/Coin");
            Obstacle[] obstaclePrefabs =
            {
                LoadPrefab<Obstacle>("Obstacles/WoodenCrates"),
                LoadPrefab<Obstacle>("Obstacles/StoneBarrier"),
                LoadPrefab<Obstacle>("Obstacles/Roadblock"),
                LoadPrefab<Obstacle>("Obstacles/SpikeTrap")
            };

            GameManager gameManager = new GameObject("GameManager").AddComponent<GameManager>();
            PlayerController player = BuildPlayer(m);
            Camera cam = SetupCamera(player.transform);
            SetupLighting(m);
            BuildBackground(m, cone, player.transform);
            BuildSpawners(player, segmentPrefab, coinPrefab, obstaclePrefabs);
            SetupPostProcessing();
            AudioManager audio = BuildAudioManager(player);
            BuildUI(player);

            ConfigurePlayerSettings();

            // ---- Save ----
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = player.gameObject;
            Debug.Log("[Ninja Runner] Scene built and saved to " + ScenePath +
                      ". Press Play! Controls: A/D or Left/Right arrows, Esc = pause, Space = start/restart.");

            // keep references "used" for clarity in the Inspector
            _ = gameManager; _ = cam; _ = audio;
        }

        private static bool TmpEssentialsPresent() => AssetDatabase.FindAssets("t:TMP_Settings").Length > 0;

        private static T LoadPrefab<T>(string relativePath) where T : Component
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/{relativePath}.prefab");
            if (go == null)
            {
                Debug.LogError($"[Ninja Runner] Prefab not found: {relativePath}");
                return null;
            }
            T component = go.GetComponent<T>();
            if (component == null) Debug.LogError($"[Ninja Runner] Prefab {relativePath} has no {typeof(T).Name} component");
            return component;
        }

        private static void CreateFolders()
        {
            foreach (string folder in new[]
                     {
                         "Scenes", "Materials", "Meshes", "Prefabs", "Prefabs/Obstacles",
                         "Prefabs/Collectibles", "Prefabs/Environment", "Settings"
                     })
            {
                EnsureFolder($"{Root}/{folder}");
            }
        }

        // ====================================================================
        // Materials
        // ====================================================================

        private class MaterialLibrary
        {
            public readonly Material StonePath, StoneDark, LanePaint, Moss, Stone;
            public readonly Material Wood, WoodDark, ToriiRed, ToriiBlack;
            public readonly Material NinjaBlack, NinjaRed, Skin, Steel, BeltRed, Gold, GoldDark;
            public readonly Material Blossom, BlossomDeep, Trunk, PagodaWall, PagodaRoof;
            public readonly Material Mountain, Snow, Cloud, LanternLight, Iron, BarrierWhite;

            public MaterialLibrary()
            {
                StonePath = Mat("StonePath", Hex("8C8A86"), 0.15f);
                StoneDark = Mat("StoneDark", Hex("5B5957"), 0.1f);
                Stone = Mat("Stone", Hex("A19C93"), 0.1f);
                LanePaint = Mat("LanePaint", Hex("E8DFC8"), 0.2f);
                Moss = Mat("Moss", Hex("5E7D4A"), 0.05f);

                Wood = Mat("Wood", Hex("9A6A3C"), 0.2f);
                WoodDark = Mat("WoodDark", Hex("5C3B22"), 0.2f);
                ToriiRed = Mat("ToriiRed", Hex("C8281E"), 0.35f);
                ToriiBlack = Mat("ToriiBlack", Hex("1E1A1A"), 0.3f);

                NinjaBlack = Mat("NinjaBlack", Hex("15161A"), 0.35f);
                NinjaRed = Mat("NinjaRed", Hex("D7141E"), 0.4f);
                BeltRed = Mat("NinjaBelt", Hex("6B1015"), 0.3f);
                Skin = Mat("Skin", Hex("F0C8A0"), 0.3f);
                Steel = Mat("Steel", Hex("C9CED6"), 0.85f, 0.9f);
                Iron = Mat("Iron", Hex("3A3D42"), 0.5f, 0.7f);
                Gold = Mat("Gold", Hex("FFC82E"), 0.8f, 1f, Hex("FFB000") * 0.6f);
                GoldDark = Mat("GoldDark", Hex("D08A10"), 0.7f, 1f, Hex("B06000") * 0.4f);
                BarrierWhite = Mat("BarrierWhite", Hex("F2EEE6"), 0.3f);

                Blossom = Mat("CherryBlossom", Hex("F7B6C8"), 0.1f);
                BlossomDeep = Mat("CherryBlossomDeep", Hex("EE8FAD"), 0.1f);
                Trunk = Mat("Trunk", Hex("4A2F25"), 0.1f);
                PagodaWall = Mat("PagodaWall", Hex("EFE3CC"), 0.2f);
                PagodaRoof = Mat("PagodaRoof", Hex("2E3440"), 0.4f);

                Mountain = Mat("Mountain", Hex("6E7A99"), 0.05f);
                Snow = Mat("Snow", Hex("F4F4FA"), 0.3f);
                Cloud = Mat("Cloud", Hex("FFFFFF"), 0f, 0f, Hex("FFE6EE") * 0.35f);
                LanternLight = Mat("LanternLight", Hex("FFD9A0"), 0.2f, 0f, Hex("FF9A3C") * 3f);
            }
        }

        private static Material Mat(string name, Color color, float smoothness, float metallic = 0f, Color? emission = null)
        {
            string path = $"{Root}/Materials/{name}.mat";
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(lit);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != lit)
            {
                mat.shader = lit;
            }

            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            mat.enableInstancing = true;

            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color c);
            return c;
        }

        // ====================================================================
        // Procedural meshes (low-poly cone / pyramid with flat shading)
        // ====================================================================

        private static Mesh CreateConeMesh(string name, int sides)
        {
            string path = $"{Root}/Meshes/{name}.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool isNew = mesh == null;
            if (isNew) mesh = new Mesh();
            mesh.Clear();
            mesh.name = name;

            var verts = new List<Vector3>();
            var tris = new List<int>();
            Vector3 apex = new Vector3(0f, 1f, 0f);
            float startAngle = sides == 4 ? Mathf.PI / 4f : 0f; // square pyramid aligned to axes

            void AddTri(Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0f) (b, c) = (c, b);
                int i = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c);
                tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            }

            for (int s = 0; s < sides; s++)
            {
                float a0 = startAngle + s * Mathf.PI * 2f / sides;
                float a1 = startAngle + (s + 1) * Mathf.PI * 2f / sides;
                Vector3 p0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * 0.5f;
                Vector3 p1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * 0.5f;
                Vector3 mid = (p0 + p1) * 0.5f;

                AddTri(apex, p0, p1, mid.normalized + Vector3.up * 0.5f); // side
                AddTri(Vector3.zero, p0, p1, Vector3.down);               // base
            }

            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            if (isNew) AssetDatabase.CreateAsset(mesh, path);
            else EditorUtility.SetDirty(mesh);
            return mesh;
        }

        // ====================================================================
        // Small helpers for building objects
        // ====================================================================

        private static Transform Empty(string name, Transform parent, Vector3 localPos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            return t;
        }

        private static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 pos,
            Vector3 scale, Material mat, Vector3 euler = default, bool castShadows = true)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>()); // visuals only; colliders are added on purpose
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }

        private static GameObject MeshPart(Mesh mesh, string name, Transform parent, Vector3 pos, Vector3 scale,
            Material mat, Vector3 euler = default, bool castShadows = true)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }

        private static T SavePrefab<T>(GameObject go, string path) where T : Component
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<T>();
        }

        // --- set private [SerializeField] fields exactly like dragging them in the Inspector ---

        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Ninja Runner] Field '{field}' not found on {target.GetType().Name}"); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (value != null && p.objectReferenceValue == null)
                Debug.LogError($"[Ninja Runner] Could not assign '{field}' on {target.GetType().Name} ({value.GetType().Name})");
        }

        private static void SetRefs(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Ninja Runner] Field '{field}' not found on {target.GetType().Name}"); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Ninja Runner] Field '{field}' not found on {target.GetType().Name}"); return; }
            p.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // ====================================================================
        // Road segment prefab (20 m of stone road + Japanese scenery)
        // ====================================================================

        private static RoadSegment BuildRoadSegmentPrefab(MaterialLibrary m, Mesh pyramid)
        {
            const float L = 20f;
            var root = new GameObject("RoadSegment");
            Transform t = root.transform;

            // Stone pathway with two lanes
            Transform road = Empty("Road", t, Vector3.zero);
            Part(PrimitiveType.Cube, "StonePath", road, new Vector3(0f, -0.2f, L / 2f), new Vector3(4.8f, 0.4f, L), m.StonePath, castShadows: false);
            for (int i = 0; i < 5; i++)
                Part(PrimitiveType.Cube, "TileSeam", road, new Vector3(0f, 0.003f, 2f + i * 4f), new Vector3(4.8f, 0.01f, 0.06f), m.StoneDark, castShadows: false);
            for (int i = 0; i < 4; i++)
                Part(PrimitiveType.Cube, "LaneDash", road, new Vector3(0f, 0.006f, 2.5f + i * 5f), new Vector3(0.09f, 0.01f, 2.4f), m.LanePaint, castShadows: false);
            Part(PrimitiveType.Cube, "CurbLeft", road, new Vector3(-2.55f, 0.1f, L / 2f), new Vector3(0.35f, 0.35f, L), m.StoneDark);
            Part(PrimitiveType.Cube, "CurbRight", road, new Vector3(2.55f, 0.1f, L / 2f), new Vector3(0.35f, 0.35f, L), m.StoneDark);
            Part(PrimitiveType.Cube, "GroundLeft", road, new Vector3(-32.7f, -0.25f, L / 2f), new Vector3(60f, 0.3f, L), m.Moss, castShadows: false);
            Part(PrimitiveType.Cube, "GroundRight", road, new Vector3(32.7f, -0.25f, L / 2f), new Vector3(60f, 0.3f, L), m.Moss, castShadows: false);

            // Stone lanterns (toro) on both sides
            Transform lanterns = Empty("Lanterns", t, Vector3.zero);
            foreach (float z in new[] { 5f, 15f })
            foreach (float x in new[] { -3.4f, 3.4f })
                BuildLantern(lanterns, new Vector3(x, 0f, z), m, pyramid);

            // Cherry blossom trees (randomly scaled/rotated each time the segment is reused)
            Transform trees = Empty("CherryTrees", t, Vector3.zero);
            var treeList = new List<Transform>
            {
                BuildCherryTree(trees, new Vector3(-6.5f, 0f, 4f), m),
                BuildCherryTree(trees, new Vector3(-10f, 0f, 14f), m),
                BuildCherryTree(trees, new Vector3(7f, 0f, 9f), m),
                BuildCherryTree(trees, new Vector3(11f, 0f, 17f), m)
            };

            // Optional: torii gate over the road, pagodas on each side
            GameObject torii = BuildToriiGate(t, new Vector3(0f, 0f, L / 2f), m);
            GameObject pagodaL = BuildPagoda(t, new Vector3(-17f, 0f, L / 2f), m, pyramid, "PagodaLeft");
            GameObject pagodaR = BuildPagoda(t, new Vector3(17f, 0f, L / 2f), m, pyramid, "PagodaRight");

            RoadSegment segment = root.AddComponent<RoadSegment>();
            SetFloat(segment, "length", L);
            SetRefs(segment, "toriiGates", new Object[] { torii });
            SetRefs(segment, "pagodas", new Object[] { pagodaL, pagodaR });
            SetRefs(segment, "randomizedProps", treeList.ToArray());

            return SavePrefab<RoadSegment>(root, $"{Root}/Prefabs/Environment/RoadSegment.prefab");
        }

        private static void BuildLantern(Transform parent, Vector3 pos, MaterialLibrary m, Mesh pyramid)
        {
            Transform l = Empty("StoneLantern", parent, pos);
            Part(PrimitiveType.Cube, "Base", l, new Vector3(0f, 0.075f, 0f), new Vector3(0.45f, 0.15f, 0.45f), m.Stone);
            Part(PrimitiveType.Cylinder, "Post", l, new Vector3(0f, 0.6f, 0f), new Vector3(0.16f, 0.45f, 0.16f), m.Stone);
            Part(PrimitiveType.Cube, "Platform", l, new Vector3(0f, 1.06f, 0f), new Vector3(0.42f, 0.06f, 0.42f), m.StoneDark);
            Part(PrimitiveType.Cube, "Light", l, new Vector3(0f, 1.25f, 0f), new Vector3(0.3f, 0.32f, 0.3f), m.LanternLight, castShadows: false);
            Part(PrimitiveType.Cube, "Cap", l, new Vector3(0f, 1.43f, 0f), new Vector3(0.48f, 0.05f, 0.48f), m.StoneDark);
            MeshPart(pyramid, "Roof", l, new Vector3(0f, 1.45f, 0f), new Vector3(0.75f, 0.3f, 0.75f), m.Stone);
            Part(PrimitiveType.Sphere, "Finial", l, new Vector3(0f, 1.78f, 0f), Vector3.one * 0.1f, m.Stone);
        }

        private static Transform BuildCherryTree(Transform parent, Vector3 pos, MaterialLibrary m)
        {
            Transform tree = Empty("CherryTree", parent, pos);
            Part(PrimitiveType.Cylinder, "Trunk", tree, new Vector3(0f, 1.4f, 0f), new Vector3(0.35f, 1.4f, 0.35f), m.Trunk);
            Part(PrimitiveType.Cylinder, "BranchA", tree, new Vector3(0.45f, 2.6f, 0f), new Vector3(0.16f, 0.7f, 0.16f), m.Trunk, new Vector3(0f, 0f, -38f));
            Part(PrimitiveType.Cylinder, "BranchB", tree, new Vector3(-0.4f, 2.5f, 0.1f), new Vector3(0.15f, 0.6f, 0.15f), m.Trunk, new Vector3(0f, 0f, 35f));
            Part(PrimitiveType.Sphere, "Blossom1", tree, new Vector3(0f, 3.5f, 0f), Vector3.one * 2.6f, m.Blossom);
            Part(PrimitiveType.Sphere, "Blossom2", tree, new Vector3(1.1f, 3.1f, 0.4f), Vector3.one * 1.8f, m.BlossomDeep);
            Part(PrimitiveType.Sphere, "Blossom3", tree, new Vector3(-1.1f, 3.2f, -0.3f), Vector3.one * 1.9f, m.Blossom);
            Part(PrimitiveType.Sphere, "Blossom4", tree, new Vector3(0.2f, 3.1f, -1.1f), Vector3.one * 1.7f, m.BlossomDeep);
            Part(PrimitiveType.Sphere, "Blossom5", tree, new Vector3(0.1f, 4.3f, 0.3f), Vector3.one * 1.5f, m.Blossom);
            return tree;
        }

        private static GameObject BuildToriiGate(Transform parent, Vector3 pos, MaterialLibrary m)
        {
            Transform g = Empty("ToriiGate", parent, pos);
            foreach (float x in new[] { -3.1f, 3.1f })
            {
                Part(PrimitiveType.Cylinder, "Pillar", g, new Vector3(x, 2.6f, 0f), new Vector3(0.38f, 2.6f, 0.38f), m.ToriiRed);
                Part(PrimitiveType.Cube, "PillarBase", g, new Vector3(x, 0.18f, 0f), new Vector3(0.5f, 0.36f, 0.5f), m.ToriiBlack);
            }
            Part(PrimitiveType.Cube, "Nuki", g, new Vector3(0f, 4.0f, 0f), new Vector3(7.2f, 0.28f, 0.3f), m.ToriiRed);
            Part(PrimitiveType.Cube, "Shimaki", g, new Vector3(0f, 4.75f, 0f), new Vector3(8.0f, 0.28f, 0.45f), m.ToriiRed);
            Part(PrimitiveType.Cube, "Kasagi", g, new Vector3(0f, 5.05f, 0f), new Vector3(8.8f, 0.34f, 0.6f), m.ToriiBlack);
            Part(PrimitiveType.Cube, "KasagiTipL", g, new Vector3(-4.45f, 5.2f, 0f), new Vector3(0.6f, 0.3f, 0.6f), m.ToriiBlack, new Vector3(0f, 0f, -18f));
            Part(PrimitiveType.Cube, "KasagiTipR", g, new Vector3(4.45f, 5.2f, 0f), new Vector3(0.6f, 0.3f, 0.6f), m.ToriiBlack, new Vector3(0f, 0f, 18f));
            Part(PrimitiveType.Cube, "Gakuzuka", g, new Vector3(0f, 4.38f, 0f), new Vector3(0.25f, 0.5f, 0.2f), m.ToriiRed);
            Part(PrimitiveType.Cube, "Plaque", g, new Vector3(0f, 4.38f, -0.12f), new Vector3(0.55f, 0.75f, 0.08f), m.ToriiBlack);
            return g.gameObject;
        }

        private static GameObject BuildPagoda(Transform parent, Vector3 pos, MaterialLibrary m, Mesh pyramid, string name)
        {
            Transform p = Empty(name, parent, pos);
            Part(PrimitiveType.Cube, "Platform", p, new Vector3(0f, 0.3f, 0f), new Vector3(6.5f, 0.6f, 6.5f), m.Stone);
            for (int i = 0; i < 4; i++)
            {
                float w = 3.8f - i * 0.65f;
                float baseY = 0.6f + i * 2.1f;
                Part(PrimitiveType.Cube, $"Wall{i}", p, new Vector3(0f, baseY + 0.8f, 0f), new Vector3(w, 1.6f, w), m.PagodaWall);
                Part(PrimitiveType.Cube, $"Beam{i}", p, new Vector3(0f, baseY + 1.55f, 0f), new Vector3(w + 0.2f, 0.18f, w + 0.2f), m.ToriiRed);
                MeshPart(pyramid, $"Roof{i}", p, new Vector3(0f, baseY + 1.6f, 0f), new Vector3(w * 1.45f + 1.6f, 0.95f, w * 1.45f + 1.6f), m.PagodaRoof);
            }
            Part(PrimitiveType.Cylinder, "Spire", p, new Vector3(0f, 9.6f, 0f), new Vector3(0.14f, 1.3f, 0.14f), m.GoldDark);
            return p.gameObject;
        }

        // ====================================================================
        // Coin & obstacle prefabs
        // ====================================================================

        private static Coin BuildCoinPrefab(MaterialLibrary m)
        {
            var root = new GameObject("Coin");
            var col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.6f;

            Transform visual = Empty("Visual", root.transform, Vector3.zero);
            Part(PrimitiveType.Cylinder, "Disc", visual, Vector3.zero, new Vector3(0.75f, 0.05f, 0.75f), m.Gold, new Vector3(90f, 0f, 0f));
            Part(PrimitiveType.Cylinder, "Emblem", visual, Vector3.zero, new Vector3(0.48f, 0.06f, 0.48f), m.GoldDark, new Vector3(90f, 0f, 0f));

            Coin coin = root.AddComponent<Coin>();
            SetRef(coin, "visual", visual);
            return SavePrefab<Coin>(root, $"{Root}/Prefabs/Collectibles/Coin.prefab");
        }

        private static Obstacle NewObstacle(string name, Vector3 colliderSize, out Transform root)
        {
            var go = new GameObject(name);
            root = go.transform;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = colliderSize;
            box.center = new Vector3(0f, colliderSize.y / 2f, 0f);
            Obstacle o = go.AddComponent<Obstacle>();
            SetFloat(o, "length", colliderSize.z);
            return o;
        }

        private static Obstacle BuildCratesPrefab(MaterialLibrary m)
        {
            NewObstacle("WoodenCrates", new Vector3(1.25f, 2.0f, 1.25f), out Transform r);
            Transform big = Empty("Crate", r, new Vector3(0f, 0.65f, 0f));
            Part(PrimitiveType.Cube, "Box", big, Vector3.zero, Vector3.one * 1.3f, m.Wood);
            Part(PrimitiveType.Cube, "BandTop", big, new Vector3(0f, 0.45f, 0f), new Vector3(1.33f, 0.12f, 1.33f), m.WoodDark);
            Part(PrimitiveType.Cube, "BandBottom", big, new Vector3(0f, -0.45f, 0f), new Vector3(1.33f, 0.12f, 1.33f), m.WoodDark);
            Transform small = Empty("SmallCrate", r, new Vector3(0.1f, 1.73f, 0.05f));
            small.localEulerAngles = new Vector3(0f, 22f, 0f);
            Part(PrimitiveType.Cube, "Box", small, Vector3.zero, Vector3.one * 0.85f, m.Wood);
            Part(PrimitiveType.Cube, "Band", small, Vector3.zero, new Vector3(0.88f, 0.1f, 0.88f), m.WoodDark);
            return SavePrefab<Obstacle>(r.gameObject, $"{Root}/Prefabs/Obstacles/WoodenCrates.prefab");
        }

        private static Obstacle BuildStoneBarrierPrefab(MaterialLibrary m)
        {
            NewObstacle("StoneBarrier", new Vector3(1.8f, 1.1f, 0.9f), out Transform r);
            Part(PrimitiveType.Cube, "Block", r, new Vector3(0f, 0.45f, 0f), new Vector3(1.8f, 0.9f, 0.9f), m.Stone);
            Part(PrimitiveType.Cube, "Cap", r, new Vector3(0f, 0.99f, 0f), new Vector3(1.95f, 0.18f, 1.05f), m.StoneDark);
            Part(PrimitiveType.Cube, "Moss", r, new Vector3(0f, 0.05f, 0f), new Vector3(1.85f, 0.1f, 0.95f), m.Moss);
            return SavePrefab<Obstacle>(r.gameObject, $"{Root}/Prefabs/Obstacles/StoneBarrier.prefab");
        }

        private static Obstacle BuildRoadblockPrefab(MaterialLibrary m)
        {
            NewObstacle("Roadblock", new Vector3(1.9f, 1.3f, 0.5f), out Transform r);
            foreach (float x in new[] { -0.8f, 0.8f })
            {
                Part(PrimitiveType.Cube, "Post", r, new Vector3(x, 0.65f, 0f), new Vector3(0.14f, 1.3f, 0.14f), m.WoodDark);
                Part(PrimitiveType.Cube, "Foot", r, new Vector3(x, 0.05f, 0f), new Vector3(0.2f, 0.1f, 0.7f), m.WoodDark);
            }
            foreach (float y in new[] { 0.7f, 1.1f })
                for (int i = 0; i < 4; i++)
                    Part(PrimitiveType.Cube, "Stripe", r, new Vector3(-0.71f + i * 0.475f, y, 0f),
                        new Vector3(0.475f, 0.22f, 0.1f), (i + (y > 1f ? 1 : 0)) % 2 == 0 ? m.ToriiRed : m.BarrierWhite);
            return SavePrefab<Obstacle>(r.gameObject, $"{Root}/Prefabs/Obstacles/Roadblock.prefab");
        }

        private static Obstacle BuildSpikeTrapPrefab(MaterialLibrary m, Mesh pyramid)
        {
            NewObstacle("SpikeTrap", new Vector3(1.8f, 0.85f, 1.4f), out Transform r);
            Part(PrimitiveType.Cube, "Plate", r, new Vector3(0f, 0.06f, 0f), new Vector3(1.8f, 0.12f, 1.4f), m.Iron);
            foreach (float x in new[] { -0.55f, 0f, 0.55f })
            foreach (float z in new[] { -0.35f, 0.35f })
                MeshPart(pyramid, "Spike", r, new Vector3(x, 0.12f, z), new Vector3(0.34f, 0.75f, 0.34f), m.Steel);
            return SavePrefab<Obstacle>(r.gameObject, $"{Root}/Prefabs/Obstacles/SpikeTrap.prefab");
        }

        // ====================================================================
        // The ninja
        // ====================================================================

        private static PlayerController BuildPlayer(MaterialLibrary m)
        {
            var go = new GameObject("Player");
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 0.95f, 0f);
            capsule.radius = 0.32f;
            capsule.height = 1.8f;

            PlayerController controller = go.AddComponent<PlayerController>();
            go.AddComponent<CoinCollector>();

            Transform model = Empty("Model", go.transform, Vector3.zero);   // leans when switching lanes
            Transform rig = Empty("Rig", model, Vector3.zero);                // pivot at the feet
            Transform body = Empty("Body", rig, new Vector3(0f, 0.95f, 0f)); // bobs while running

            Part(PrimitiveType.Capsule, "Torso", body, new Vector3(0f, 0.38f, 0f), new Vector3(0.5f, 0.42f, 0.34f), m.NinjaBlack);
            Part(PrimitiveType.Cylinder, "Belt", body, new Vector3(0f, 0.04f, 0f), new Vector3(0.53f, 0.06f, 0.37f), m.BeltRed);
            Part(PrimitiveType.Sphere, "Head", body, new Vector3(0f, 0.98f, 0f), Vector3.one * 0.4f, m.NinjaBlack);
            Part(PrimitiveType.Cube, "EyeSlit", body, new Vector3(0f, 1.0f, 0.17f), new Vector3(0.26f, 0.08f, 0.08f), m.Skin);
            Part(PrimitiveType.Sphere, "EyeL", body, new Vector3(-0.06f, 1.0f, 0.205f), Vector3.one * 0.05f, m.ToriiBlack);
            Part(PrimitiveType.Sphere, "EyeR", body, new Vector3(0.06f, 1.0f, 0.205f), Vector3.one * 0.05f, m.ToriiBlack);
            Part(PrimitiveType.Cylinder, "Headband", body, new Vector3(0f, 1.08f, 0f), new Vector3(0.43f, 0.035f, 0.43f), m.NinjaRed);
            Part(PrimitiveType.Sphere, "Knot", body, new Vector3(0f, 1.08f, -0.21f), Vector3.one * 0.09f, m.NinjaRed);

            Transform tails = Empty("HeadbandTails", body, new Vector3(0f, 1.08f, -0.22f));
            Part(PrimitiveType.Cube, "TailL", tails, new Vector3(-0.05f, 0f, -0.17f), new Vector3(0.05f, 0.03f, 0.34f), m.NinjaRed, new Vector3(0f, 10f, 0f));
            Part(PrimitiveType.Cube, "TailR", tails, new Vector3(0.05f, 0f, -0.17f), new Vector3(0.05f, 0.03f, 0.34f), m.NinjaRed, new Vector3(0f, -10f, 0f));

            Transform katana = Empty("Katana", body, new Vector3(0f, 0.45f, -0.21f));
            katana.localEulerAngles = new Vector3(0f, 0f, 35f);
            Part(PrimitiveType.Cube, "Blade", katana, new Vector3(0f, 0.22f, 0f), new Vector3(0.045f, 0.9f, 0.025f), m.Steel);
            Part(PrimitiveType.Cube, "Guard", katana, new Vector3(0f, -0.25f, 0f), new Vector3(0.14f, 0.03f, 0.08f), m.GoldDark);
            Part(PrimitiveType.Cube, "Handle", katana, new Vector3(0f, -0.4f, 0f), new Vector3(0.05f, 0.27f, 0.05f), m.NinjaRed);

            Transform leftArm = BuildLimb(body, "LeftArm", new Vector3(-0.31f, 0.72f, 0f), 0.14f, 0.3f, m.NinjaBlack, m.Skin, false);
            Transform rightArm = BuildLimb(body, "RightArm", new Vector3(0.31f, 0.72f, 0f), 0.14f, 0.3f, m.NinjaBlack, m.Skin, false);
            Transform leftLeg = BuildLimb(rig, "LeftLeg", new Vector3(-0.13f, 0.92f, 0f), 0.17f, 0.42f, m.NinjaBlack, m.Iron, true);
            Transform rightLeg = BuildLimb(rig, "RightLeg", new Vector3(0.13f, 0.92f, 0f), 0.17f, 0.42f, m.NinjaBlack, m.Iron, true);

            NinjaAnimator anim = rig.gameObject.AddComponent<NinjaAnimator>();
            SetRef(anim, "body", body);
            SetRef(anim, "leftArm", leftArm);
            SetRef(anim, "rightArm", rightArm);
            SetRef(anim, "leftLeg", leftLeg);
            SetRef(anim, "rightLeg", rightLeg);
            SetRef(anim, "headbandTails", tails);

            SetRef(controller, "model", model);
            return controller;
        }

        private static Transform BuildLimb(Transform parent, string name, Vector3 pivot, float thickness, float halfLength,
            Material cloth, Material end, bool isLeg)
        {
            Transform limb = Empty(name, parent, pivot);
            Part(PrimitiveType.Capsule, "Segment", limb, new Vector3(0f, -halfLength - 0.02f, 0f),
                new Vector3(thickness, halfLength, thickness), cloth);
            if (isLeg)
                Part(PrimitiveType.Cube, "Foot", limb, new Vector3(0f, -halfLength * 2f - 0.04f, 0.06f), new Vector3(0.15f, 0.07f, 0.26f), end);
            else
                Part(PrimitiveType.Sphere, "Hand", limb, new Vector3(0f, -halfLength * 2f - 0.02f, 0f), Vector3.one * 0.12f, end);
            return limb;
        }

        // ====================================================================
        // Camera, lighting, sky, background, post-processing
        // ====================================================================

        private static Camera SetupCamera(Transform player)
        {
            Camera cam = Camera.main;
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 700f;
            cam.clearFlags = CameraClearFlags.Skybox;

            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;

            CameraFollow follow = cam.gameObject.AddComponent<CameraFollow>();
            SetRef(follow, "target", player);
            // Start in the menu view (in front of the ninja); CameraFollow swings behind on "Tap to Play".
            cam.transform.position = player.position + new Vector3(-1.5f, 0f, 0f) + new Vector3(1.5f, 1.9f, 3.6f);
            cam.transform.LookAt(player.position + new Vector3(-1.5f, 0f, 0f) + new Vector3(0.25f, 1.15f, 0f));
            return cam;
        }

        private static void SetupLighting(MaterialLibrary m)
        {
            Light sun = Object.FindAnyObjectByType<Light>();
            sun.name = "Sun";
            sun.color = Hex("FFD2A8");
            sun.intensity = 1.4f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(28f, -150f, 0f); // low evening sun ahead of the runner

            var sky = AssetDatabase.LoadAssetAtPath<Material>($"{Root}/Materials/Sky.mat");
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(sky, $"{Root}/Materials/Sky.mat");
            }
            sky.SetFloat("_SunSize", 0.06f);
            sky.SetFloat("_SunSizeConvergence", 4f);
            sky.SetFloat("_AtmosphereThickness", 1.3f);
            sky.SetColor("_SkyTint", Hex("FFA8C0"));
            sky.SetColor("_GroundColor", Hex("6E5E6A"));
            sky.SetFloat("_Exposure", 1.25f);
            EditorUtility.SetDirty(sky);

            RenderSettings.skybox = sky;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("D8C4E0");
            RenderSettings.ambientEquatorColor = Hex("B89AA8");
            RenderSettings.ambientGroundColor = Hex("5A4E50");

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.0055f;
            RenderSettings.fogColor = Hex("F2C6C8");
        }

        private static void BuildBackground(MaterialLibrary m, Mesh cone, Transform player)
        {
            var bg = new GameObject("Background");
            bg.AddComponent<BackgroundFollower>();
            SetRef(bg.GetComponent<BackgroundFollower>(), "target", player);

            // Mountains (with snow caps) far ahead and to the sides
            var mountains = new (Vector3 pos, float width, float height)[]
            {
                (new Vector3(30f, -2f, 380f), 320f, 150f),  // the big one, Fuji-style
                (new Vector3(-180f, -2f, 320f), 200f, 95f),
                (new Vector3(210f, -2f, 330f), 220f, 105f),
                (new Vector3(-320f, -2f, 260f), 220f, 80f),
                (new Vector3(340f, -2f, 250f), 220f, 85f),
                (new Vector3(-110f, -2f, 420f), 180f, 110f),
            };
            Transform mt = Empty("Mountains", bg.transform, Vector3.zero);
            foreach (var (pos, w, h) in mountains)
            {
                Transform peak = Empty("Mountain", mt, pos);
                MeshPart(cone, "Rock", peak, Vector3.zero, new Vector3(w, h, w), m.Mountain, castShadows: false);
                MeshPart(cone, "Snow", peak, new Vector3(0f, h * 0.72f, 0f), new Vector3(w * 0.29f, h * 0.29f, w * 0.29f), m.Snow, castShadows: false);
            }

            // Soft clouds
            Transform clouds = Empty("Clouds", bg.transform, Vector3.zero);
            var rnd = new System.Random(42);
            for (int i = 0; i < 9; i++)
            {
                var c = Empty("Cloud", clouds, new Vector3(-180f + i * 45f + rnd.Next(-15, 15), 55f + rnd.Next(0, 30), 150f + rnd.Next(0, 180)));
                for (int k = 0; k < 4; k++)
                    Part(PrimitiveType.Sphere, "Puff", c, new Vector3(k * 9f - 13f, rnd.Next(-2, 3), rnd.Next(-4, 4)),
                        new Vector3(18f + rnd.Next(0, 10), 7f + rnd.Next(0, 4), 12f), m.Cloud, castShadows: false);
            }
        }

        private static void SetupPostProcessing()
        {
            string path = $"{Root}/Settings/PostProcessProfile.asset";
            AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var bloom = profile.Add<Bloom>();
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.8f);
            bloom.scatter.Override(0.65f);

            var tonemapping = profile.Add<Tonemapping>();
            tonemapping.mode.Override(TonemappingMode.ACES);

            var color = profile.Add<ColorAdjustments>();
            color.postExposure.Override(0.15f);
            color.contrast.Override(12f);
            color.saturation.Override(14f);

            var vignette = profile.Add<Vignette>();
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.45f);

            foreach (VolumeComponent component in profile.components)
            {
                component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            EditorUtility.SetDirty(profile);

            var go = new GameObject("PostProcessing");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        // ====================================================================
        // Spawners, audio
        // ====================================================================

        private static void BuildSpawners(PlayerController player, RoadSegment segment, Coin coin, Obstacle[] obstacles)
        {
            Transform world = new GameObject("World").transform;

            var road = new GameObject("RoadSpawner").AddComponent<RoadSpawner>();
            road.transform.SetParent(world);
            SetRef(road, "player", player.transform);
            SetRef(road, "segmentPrefab", segment);

            var obstacleSpawner = new GameObject("ObstacleSpawner").AddComponent<ObstacleSpawner>();
            obstacleSpawner.transform.SetParent(world);
            SetRef(obstacleSpawner, "player", player.transform);
            SetRef(obstacleSpawner, "playerController", player);
            SetRefs(obstacleSpawner, "obstaclePrefabs", obstacles);

            var coinSpawner = new GameObject("CoinSpawner").AddComponent<CoinSpawner>();
            coinSpawner.transform.SetParent(world);
            SetRef(coinSpawner, "player", player.transform);
            SetRef(coinSpawner, "playerController", player);
            SetRef(coinSpawner, "obstacleSpawner", obstacleSpawner);
            SetRef(coinSpawner, "coinPrefab", coin);
        }

        private static AudioManager BuildAudioManager(PlayerController player)
        {
            var go = new GameObject("AudioManager");
            AudioManager audio = go.AddComponent<AudioManager>();

            AudioSource NewSource(string name)
            {
                var s = new GameObject(name).AddComponent<AudioSource>();
                s.transform.SetParent(go.transform);
                s.playOnAwake = false;
                s.spatialBlend = 0f; // 2D sound
                return s;
            }

            SetRef(audio, "musicSource", NewSource("Music"));
            SetRef(audio, "sfxSource", NewSource("SFX"));
            SetRef(audio, "musicClip", Clip("music_loop"));
            SetRef(audio, "coinClip", Clip("coin"));
            SetRef(audio, "laneSwitchClip", Clip("whoosh"));
            SetRef(audio, "crashClip", Clip("crash"));
            SetRef(audio, "clickClip", Clip("click"));
            SetRef(audio, "player", player);
            SetRef(audio, "coinCollector", player.GetComponent<CoinCollector>());
            return audio;
        }

        private static AudioClip Clip(string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Root}/Audio/{name}.wav");
            if (clip == null) Debug.LogWarning($"[Ninja Runner] Audio clip '{name}.wav' not found in {Root}/Audio.");
            return clip;
        }

        // ====================================================================
        // UI
        // ====================================================================

        private static readonly Color Red = new Color(0.82f, 0.1f, 0.09f, 1f);
        private static readonly Color Cream = new Color(1f, 0.94f, 0.86f, 1f);
        private static readonly Color GoldText = new Color(1f, 0.8f, 0.25f, 1f);

        private static void BuildUI(PlayerController player)
        {
            // Canvas that scales for any phone/PC screen
            var canvasGO = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            canvasGO.AddComponent<ResponsiveCanvasScaler>(); // portrait: match width, landscape: match height
            Transform canvas = canvasGO.transform;

            UIManager ui = canvasGO.AddComponent<UIManager>();

            // ---------------- HUD ----------------
            RectTransform hud = Stretch(UIObject("HUD", canvas));

            RectTransform coinBox = UIObject("CoinCounter", hud);
            Place(coinBox, new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(330f, 110f));
            AddImage(coinBox, new Color(0f, 0f, 0f, 0.45f));
            RectTransform coinIcon = UIObject("CoinIcon", coinBox);
            Place(coinIcon, new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(72f, 72f), new Vector2(0f, 0.5f));
            AddImage(coinIcon, new Color(1f, 0.78f, 0.15f), "UI/Skin/Knob.psd");
            TMP_Text coinText = Label("CoinText", coinBox, "0", 64, Color.white, TextAlignmentOptions.MidlineLeft);
            Place(coinText.rectTransform, new Vector2(0f, 0.5f), new Vector2(110f, 0f), new Vector2(210f, 100f), new Vector2(0f, 0.5f));

            RectTransform scoreBox = UIObject("Score", hud);
            Place(scoreBox, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(400f, 130f));
            AddImage(scoreBox, new Color(0f, 0f, 0f, 0.45f));
            TMP_Text scoreCaption = Label("Caption", scoreBox, "SCORE", 30, new Color(1f, 1f, 1f, 0.7f), TextAlignmentOptions.Center);
            Place(scoreCaption.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(380f, 40f));
            TMP_Text scoreText = Label("ScoreText", scoreBox, "0", 62, Color.white, TextAlignmentOptions.Center);
            Place(scoreText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(380f, 80f), new Vector2(0.5f, 0f));

            Button pause = MakeButton("PauseButton", hud, "II", new Vector2(1f, 1f), new Vector2(-40f, -40f),
                new Vector2(120f, 120f), new Color(0f, 0f, 0f, 0.45f), 54);
            UnityEventTools.AddPersistentListener(pause.onClick, ui.OnPauseButton);

            Button left = MakeButton("LeftButton", hud, "<", new Vector2(0f, 0f), new Vector2(50f, 60f),
                new Vector2(250f, 250f), new Color(1f, 1f, 1f, 0.22f), 130, "UI/Skin/Knob.psd");
            AddPointerDown(left.gameObject, ui.OnLeftButton);

            Button right = MakeButton("RightButton", hud, ">", new Vector2(1f, 0f), new Vector2(-50f, 60f),
                new Vector2(250f, 250f), new Color(1f, 1f, 1f, 0.22f), 130, "UI/Skin/Knob.psd");
            AddPointerDown(right.gameObject, ui.OnRightButton);

            // ---------------- Start screen (Subway-Surfers style: see the ninja, tap anywhere) ----------------
            RectTransform start = Stretch(UIObject("StartScreen", canvas));

            // Invisible full-screen button: a tap/click anywhere starts the run.
            RectTransform tapArea = Stretch(UIObject("TapToPlayArea", start));
            Image tapImage = AddImage(tapArea, new Color(0f, 0f, 0f, 0f));
            Button tap = tapArea.gameObject.AddComponent<Button>();
            tap.targetGraphic = tapImage;
            tap.transition = Selectable.Transition.None;
            tap.navigation = new Navigation { mode = Navigation.Mode.None };
            UnityEventTools.AddPersistentListener(tap.onClick, ui.OnPlayButton);

            // Soft dark band behind the title (top) and the prompt (bottom) for readability.
            RectTransform topShade = UIObject("TopShade", start);
            topShade.anchorMin = new Vector2(0f, 1f); topShade.anchorMax = new Vector2(1f, 1f);
            topShade.pivot = new Vector2(0.5f, 1f); topShade.anchoredPosition = Vector2.zero; topShade.sizeDelta = new Vector2(0f, 470f);
            AddImage(topShade, new Color(0.05f, 0.01f, 0.04f, 0.45f)).raycastTarget = false;
            RectTransform bottomShade = UIObject("BottomShade", start);
            bottomShade.anchorMin = Vector2.zero; bottomShade.anchorMax = new Vector2(1f, 0f);
            bottomShade.pivot = new Vector2(0.5f, 0f); bottomShade.anchoredPosition = Vector2.zero; bottomShade.sizeDelta = new Vector2(0f, 400f);
            AddImage(bottomShade, new Color(0.05f, 0.01f, 0.04f, 0.45f)).raycastTarget = false;

            TMP_Text title = Label("Title", start, "NINJA RUN", 150, Cream, TextAlignmentOptions.Center);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1040f, 190f));
            RectTransform band = UIObject("RedBand", start);
            Place(band, new Vector2(0.5f, 1f), new Vector2(0f, -255f), new Vector2(700f, 14f));
            AddImage(band, Red).raycastTarget = false;
            TMP_Text subtitle = Label("Subtitle", start, "ENDLESS RUNNER", 48, new Color(1f, 0.5f, 0.45f), TextAlignmentOptions.Center);
            Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(900f, 70f));
            TMP_Text menuBest = Label("BestText", start, "BEST  0", 46, GoldText, TextAlignmentOptions.Center);
            Place(menuBest.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -360f), new Vector2(800f, 70f));

            TMP_Text tapText = Label("TapToPlayText", start, "TAP TO PLAY", 92, Color.white, TextAlignmentOptions.Center);
            Place(tapText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 220f), new Vector2(1000f, 130f));
            tapText.gameObject.AddComponent<UIPulse>();
            TMP_Text controls = Label("Controls", start,
                "Switch lane:  A / D,  Arrow keys  or  < >  buttons      Pause:  Esc",
                32, new Color(1f, 1f, 1f, 0.8f), TextAlignmentOptions.Center);
            Place(controls.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(1060f, 90f));

            // ---------------- Pause screen ----------------
            RectTransform pauseScreen = Stretch(UIObject("PauseScreen", canvas));
            AddImage(pauseScreen, new Color(0f, 0f, 0f, 0.6f));
            Center(Label("Title", pauseScreen, "PAUSED", 130, Cream, TextAlignmentOptions.Center), 0f, 260f, 900f, 180f);
            Button resume = MakeButton("ResumeButton", pauseScreen, "RESUME", new Vector2(0.5f, 0.5f), new Vector2(0f, 40f),
                new Vector2(500f, 160f), Red, 70);
            UnityEventTools.AddPersistentListener(resume.onClick, ui.OnResumeButton);
            Button pauseRestart = MakeButton("RestartButton", pauseScreen, "RESTART", new Vector2(0.5f, 0.5f), new Vector2(0f, -160f),
                new Vector2(500f, 160f), new Color(0.22f, 0.22f, 0.27f), 70);
            UnityEventTools.AddPersistentListener(pauseRestart.onClick, ui.OnRestartButton);

            // ---------------- Game Over screen ----------------
            RectTransform over = Stretch(UIObject("GameOverScreen", canvas));
            AddImage(over, new Color(0.08f, 0f, 0f, 0.65f));
            Center(Label("Title", over, "GAME OVER", 140, new Color(1f, 0.3f, 0.25f), TextAlignmentOptions.Center), 0f, 500f, 1040f, 200f);
            RectTransform card = UIObject("ResultsCard", over);
            Place(card, new Vector2(0.5f, 0.5f), new Vector2(0f, 140f), new Vector2(820f, 540f));
            AddImage(card, new Color(0f, 0f, 0f, 0.5f));
            TMP_Text finalScore = Label("FinalScore", card, "SCORE  0", 78, Color.white, TextAlignmentOptions.Center);
            Place(finalScore.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(780f, 110f));
            TMP_Text finalCoins = Label("FinalCoins", card, "COINS  0", 58, GoldText, TextAlignmentOptions.Center);
            Place(finalCoins.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(780f, 90f));
            TMP_Text best = Label("BestScore", card, "BEST  0", 50, new Color(1f, 1f, 1f, 0.8f), TextAlignmentOptions.Center);
            Place(best.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -265f), new Vector2(780f, 80f));
            TMP_Text newBest = Label("NewBest", card, "NEW BEST!", 56, GoldText, TextAlignmentOptions.Center);
            Place(newBest.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -370f), new Vector2(780f, 90f));
            Button restart = MakeButton("RestartButton", over, "RESTART", new Vector2(0.5f, 0.5f), new Vector2(0f, -280f),
                new Vector2(580f, 175f), Red, 80);
            UnityEventTools.AddPersistentListener(restart.onClick, ui.OnRestartButton);
            Center(Label("Hint", over, "or press Space", 38, new Color(1f, 1f, 1f, 0.7f), TextAlignmentOptions.Center), 0f, -420f, 800f, 70f);

            // ---------------- Wire UIManager ----------------
            SetRef(ui, "startPanel", start.gameObject);
            SetRef(ui, "hudPanel", hud.gameObject);
            SetRef(ui, "pausePanel", pauseScreen.gameObject);
            SetRef(ui, "gameOverPanel", over.gameObject);
            SetRef(ui, "coinText", coinText);
            SetRef(ui, "scoreText", scoreText);
            SetRef(ui, "menuBestText", menuBest);
            SetRef(ui, "finalScoreText", finalScore);
            SetRef(ui, "finalCoinsText", finalCoins);
            SetRef(ui, "bestScoreText", best);
            SetRef(ui, "newBestLabel", newBest.gameObject);
            SetRef(ui, "player", player);

            hud.gameObject.SetActive(false);
            pauseScreen.gameObject.SetActive(false);
            over.gameObject.SetActive(false);

            // Event system (lets UI buttons receive clicks and touches)
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        private static RectTransform UIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static RectTransform Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>Anchors to one point of the parent (pivot defaults to the same point).</summary>
        private static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot ?? anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        private static void Center(TMP_Text text, float x, float y, float w, float h) =>
            Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(w, h));

        private static Image AddImage(RectTransform rt, Color color, string builtinSprite = "UI/Skin/UISprite.psd")
        {
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(builtinSprite);
            img.type = builtinSprite.Contains("UISprite") ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            return img;
        }

        private static TMP_Text Label(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align)
        {
            RectTransform rt = UIObject(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = FontStyles.Bold;
            t.raycastTarget = false;
            return t;
        }

        private static Button MakeButton(string name, Transform parent, string label, Vector2 anchor, Vector2 position,
            Vector2 size, Color color, float fontSize, string sprite = "UI/Skin/UISprite.psd")
        {
            RectTransform rt = UIObject(name, parent);
            Place(rt, anchor, position, size);
            Image img = AddImage(rt, color, sprite);

            Button b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.navigation = new Navigation { mode = Navigation.Mode.None }; // arrow keys are for lanes, not UI
            ColorBlock colors = b.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.selectedColor = Color.white;
            b.colors = colors;

            TMP_Text text = Label("Label", rt, label, fontSize, Color.white, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            return b;
        }

        /// <summary>Lane buttons react on finger DOWN (not on release) so they feel instant on mobile.</summary>
        private static void AddPointerDown(GameObject go, UnityEngine.Events.UnityAction action)
        {
            var trigger = go.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            UnityEventTools.AddVoidPersistentListener(entry.callback, action);
            trigger.triggers.Add(entry);
        }

        // ====================================================================
        // Player settings (for the Android build)
        // ====================================================================

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.productName = "Ninja Run";
            PlayerSettings.companyName = "Akhil";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.akhil.ninjarun");
        }
    }
}
