using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EcoSort.Gameplay;

namespace EcoSort.Editor
{
    /// <summary>
    /// Configures Blender-modeled cozy park assets (benches, footpath, lamp posts, flower bushes,
    /// signposts, curved half-moon bin stage, and animated walking NPCs) with URP materials and
    /// looping walk animations, then applies the park environment and half-moon bin layout across
    /// all EcoSort VR scenes while preserving existing trees, rocks, skybox, UI, and gameplay logic.
    /// </summary>
    [InitializeOnLoad]
    public static class EcoSortParkBuilder
    {
        private const string ModelsDir = "Assets/Models/Park";
        private const string ParkMatsDir = "Assets/Materials/Park";
        private const string ParkPrefabsDir = "Assets/Prefabs/Park";
        private const string ScenesDir = "Assets/Scenes/ScenesOfTheProject";
        private const string MarkerPath = "Library/EcoSortParkApplied_v1.marker";
        private const string ReportPath = "Library/EcoSortParkBuildReport.txt";

        private static readonly string[] AllSceneNames =
        {
            "MainMenu",
            "TutorialLevel",
            "Level1",
            "Level2",
            "Level3",
            "Level4",
            "Level5",
            "Level6",
            "Level7",
            "Level8",
            "Level9",
            "LevelCompletedDemo"
        };

        static EcoSortParkBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                    return;

                if (!File.Exists(MarkerPath) && File.Exists(Path.Combine(ModelsDir, "Park_Pathway.fbx")))
                {
                    Debug.Log("[EcoSortParkBuilder] Auto-applying cozy park environment & half-moon bin layout to all scenes...");
                    ApplyParkToAllScenesInternal(forceReimport: true);
                }
            };
        }

        [MenuItem("EcoSort/Apply Cozy Park & Half-Moon Bins To All Scenes")]
        public static void ApplyParkToAllScenes()
        {
            ApplyParkToAllScenesInternal(forceReimport: true);
        }

        private static void ApplyParkToAllScenesInternal(bool forceReimport)
        {
            var report = new StringBuilder();
            report.AppendLine($"=== EcoSortParkBuilder Run: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");

            try
            {
                EnsureFolders();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                // 1. Create URP Lit materials for park & NPC models and enrich park grass tone
                Dictionary<string, Material> mats = CreateOrUpdateParkMaterials();
                EnhanceParkFloorMaterial();
                report.AppendLine($"Materials ready: {mats.Count}");

                // 2. Configure FBX importers (remap URP materials + enable looping Walk clip on NPCs)
                if (forceReimport)
                {
                    ConfigureAllFbxImporters(mats, report);
                }

                // 3. Create looping AnimatorControllers for the 3 NPC variants
                var ctrlA = CreateNpcAnimatorController("Park_NPC_VisitorA.fbx", $"{ParkPrefabsDir}/ParkNpcAnim_A.controller", report);
                var ctrlB = CreateNpcAnimatorController("Park_NPC_VisitorB.fbx", $"{ParkPrefabsDir}/ParkNpcAnim_B.controller", report);
                var ctrlC = CreateNpcAnimatorController("Park_NPC_VisitorC.fbx", $"{ParkPrefabsDir}/ParkNpcAnim_C.controller", report);

                // 4. Build reusable park & NPC prefabs
                GameObject crescentPrefab = BuildCrescentPlatformPrefab();
                GameObject benchPrefab = BuildBenchPrefab();
                GameObject lampPrefab = BuildLampPostPrefab();
                GameObject bushPrefab = BuildSimplePropPrefab("Park_FlowerBush.fbx", "Park_FlowerBush");
                GameObject signPrefab = BuildSimplePropPrefab("Park_Signpost.fbx", "Park_Signpost");
                GameObject pathwayModel = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsDir}/Park_Pathway.fbx");

                GameObject npcAPrefab = BuildNpcPrefab("Park_NPC_VisitorA.fbx", "Park_NPC_VisitorA", ctrlA);
                GameObject npcBPrefab = BuildNpcPrefab("Park_NPC_VisitorB.fbx", "Park_NPC_VisitorB", ctrlB);
                GameObject npcCPrefab = BuildNpcPrefab("Park_NPC_VisitorC.fbx", "Park_NPC_VisitorC", ctrlC);

                GameObject parkEnvPrefab = BuildParkEnvironmentPrefab(
                    pathwayModel, benchPrefab, lampPrefab, bushPrefab, signPrefab,
                    npcAPrefab, npcBPrefab, npcCPrefab
                );
                report.AppendLine($"ParkEnvironment prefab built: {(parkEnvPrefab != null ? "OK" : "FAILED")}");

                AssetDatabase.SaveAssets();

                // 5. Remember active scene (save if dirty), apply park + half-moon bins to all 12 scenes
                Scene activeBefore = SceneManager.GetActiveScene();
                string originalScenePath = activeBefore.path;
                if (activeBefore.IsValid() && activeBefore.isDirty)
                {
                    EditorSceneManager.SaveOpenScenes();
                }
                int scenesUpdated = 0;

                foreach (string sceneName in AllSceneNames)
                {
                    string scenePath = $"{ScenesDir}/{sceneName}.unity";
                    if (!File.Exists(scenePath))
                    {
                        report.AppendLine($"  SKIP missing scene: {scenePath}");
                        continue;
                    }

                    Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    string summary = UpdateOpenScene(scene, sceneName, parkEnvPrefab, crescentPrefab);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    scenesUpdated++;
                    report.AppendLine($"  [{sceneName}] {summary}");
                }

                // Restore previously open scene
                if (!string.IsNullOrEmpty(originalScenePath) && File.Exists(originalScenePath))
                {
                    EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                File.WriteAllText(MarkerPath, DateTime.Now.ToString("O"));
                report.AppendLine($"SUCCESS: Updated {scenesUpdated} scenes.");
                Debug.Log($"[EcoSortParkBuilder] Successfully applied cozy park & half-moon bin layout to {scenesUpdated} scenes!");
            }
            catch (Exception ex)
            {
                report.AppendLine("ERROR: " + ex.ToString());
                Debug.LogError("[EcoSortParkBuilder] Failed: " + ex);
            }
            finally
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
                File.WriteAllText(ReportPath, report.ToString());
            }
        }

        private static void EnsureFolders()
        {
            CreateFolderIfMissing("Assets", "Models");
            CreateFolderIfMissing("Assets/Models", "Park");
            CreateFolderIfMissing("Assets", "Materials");
            CreateFolderIfMissing("Assets/Materials", "Park");
            CreateFolderIfMissing("Assets", "Prefabs");
            CreateFolderIfMissing("Assets/Prefabs", "Park");
        }

        private static void CreateFolderIfMissing(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
                AssetDatabase.CreateFolder(parent, name);
        }

        #region Materials

        private static Dictionary<string, Material> CreateOrUpdateParkMaterials()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
                urpLit = Shader.Find("Standard");

            var specs = new (string name, Color color, float smoothness, float metallic, Color? emission)[]
            {
                ("Mat_Park_WoodWarm",       new Color(0.62f, 0.37f, 0.19f), 0.28f, 0.00f, null),
                ("Mat_Park_WoodDark",       new Color(0.35f, 0.21f, 0.12f), 0.24f, 0.00f, null),
                ("Mat_Park_WoodLight",      new Color(0.76f, 0.56f, 0.34f), 0.30f, 0.00f, null),
                ("Mat_Park_CastIron",       new Color(0.16f, 0.20f, 0.23f), 0.52f, 0.65f, null),
                ("Mat_Park_Brass",          new Color(0.84f, 0.66f, 0.24f), 0.68f, 0.78f, null),
                ("Mat_Park_StoneLight",     new Color(0.87f, 0.83f, 0.75f), 0.15f, 0.00f, null),
                ("Mat_Park_StoneTrim",      new Color(0.65f, 0.62f, 0.56f), 0.12f, 0.00f, null),
                ("Mat_Park_StoneBase",      new Color(0.55f, 0.53f, 0.49f), 0.12f, 0.00f, null),
                ("Mat_Park_PathGravel",     new Color(0.89f, 0.79f, 0.62f), 0.10f, 0.00f, null),
                ("Mat_Park_PathStep",       new Color(0.78f, 0.73f, 0.64f), 0.16f, 0.00f, null),
                ("Mat_Park_LampGlassGlow",  new Color(1.00f, 0.94f, 0.72f), 0.85f, 0.00f, new Color(1.00f, 0.82f, 0.38f) * 2.8f),
                ("Mat_Park_BushGreen",      new Color(0.22f, 0.56f, 0.24f), 0.18f, 0.00f, null),
                ("Mat_Park_BushGreenLight", new Color(0.34f, 0.68f, 0.29f), 0.18f, 0.00f, null),
                ("Mat_Park_FlowerYellow",   new Color(0.99f, 0.83f, 0.16f), 0.35f, 0.00f, null),
                ("Mat_Park_FlowerPink",     new Color(0.96f, 0.42f, 0.56f), 0.35f, 0.00f, null),
                ("Mat_Park_FlowerBlue",     new Color(0.38f, 0.66f, 0.97f), 0.35f, 0.00f, null),
                ("Mat_Park_FlowerWhite",    new Color(0.98f, 0.98f, 0.95f), 0.35f, 0.00f, null),
                ("Mat_Park_SignBoard",      new Color(0.94f, 0.90f, 0.78f), 0.26f, 0.00f, null),
                // NPC palette
                ("Mat_NPC_Skin",            new Color(0.96f, 0.78f, 0.63f), 0.28f, 0.00f, null),
                ("Mat_NPC_SkinTan",         new Color(0.83f, 0.60f, 0.43f), 0.28f, 0.00f, null),
                ("Mat_NPC_Cheeks",          new Color(0.95f, 0.56f, 0.56f), 0.22f, 0.00f, null),
                ("Mat_NPC_Eyes",            new Color(0.08f, 0.08f, 0.11f), 0.75f, 0.00f, null),
                ("Mat_NPC_HairDark",        new Color(0.19f, 0.13f, 0.10f), 0.24f, 0.00f, null),
                ("Mat_NPC_HairBrown",       new Color(0.45f, 0.27f, 0.14f), 0.24f, 0.00f, null),
                ("Mat_NPC_ShirtCoral",      new Color(0.92f, 0.36f, 0.32f), 0.22f, 0.00f, null),
                ("Mat_NPC_ShirtYellow",     new Color(0.96f, 0.76f, 0.18f), 0.22f, 0.00f, null),
                ("Mat_NPC_ShirtMint",       new Color(0.24f, 0.69f, 0.50f), 0.22f, 0.00f, null),
                ("Mat_NPC_ShirtCream",      new Color(0.95f, 0.93f, 0.87f), 0.20f, 0.00f, null),
                ("Mat_NPC_PantsBlue",       new Color(0.20f, 0.34f, 0.60f), 0.18f, 0.00f, null),
                ("Mat_NPC_PantsTeal",       new Color(0.16f, 0.45f, 0.49f), 0.18f, 0.00f, null),
                ("Mat_NPC_PantsKhaki",      new Color(0.73f, 0.62f, 0.44f), 0.18f, 0.00f, null),
                ("Mat_NPC_HatRed",          new Color(0.85f, 0.24f, 0.24f), 0.26f, 0.00f, null),
                ("Mat_NPC_HatBeanie",       new Color(0.32f, 0.48f, 0.87f), 0.16f, 0.00f, null),
                ("Mat_NPC_HatStraw",        new Color(0.91f, 0.79f, 0.48f), 0.18f, 0.00f, null),
                ("Mat_NPC_Shoes",           new Color(0.23f, 0.19f, 0.17f), 0.34f, 0.00f, null),
                ("Mat_NPC_Backpack",        new Color(0.65f, 0.35f, 0.17f), 0.24f, 0.00f, null),
            };

            var dict = new Dictionary<string, Material>();
            foreach (var s in specs)
            {
                string path = $"{ParkMatsDir}/{s.name}.mat";
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    mat = new Material(urpLit) { name = s.name };
                    AssetDatabase.CreateAsset(mat, path);
                }
                else
                {
                    mat.shader = urpLit;
                }

                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", s.color);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", s.color);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", s.smoothness);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", s.smoothness);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", s.metallic);

                if (s.emission.HasValue)
                {
                    mat.EnableKeyword("_EMISSION");
                    if (mat.HasProperty("_EmissionColor"))
                        mat.SetColor("_EmissionColor", s.emission.Value);
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                else
                {
                    mat.DisableKeyword("_EMISSION");
                    if (mat.HasProperty("_EmissionColor"))
                        mat.SetColor("_EmissionColor", Color.black);
                }

                EditorUtility.SetDirty(mat);
                dict[s.name] = mat;
            }

            AssetDatabase.SaveAssets();
            return dict;
        }

        private static void EnhanceParkFloorMaterial()
        {
            string floorMatPath = "Assets/Materials/EcoSort/Mat_CozyParkFloor.mat";
            Material floorMat = AssetDatabase.LoadAssetAtPath<Material>(floorMatPath);
            if (floorMat != null)
            {
                // Fresh, warm cozy park meadow green (replaces pale gray-mint so gravel path & stone plaza pop beautifully!)
                Color meadowGreen = new Color(0.56f, 0.77f, 0.49f, 1f);
                if (floorMat.HasProperty("_BaseColor")) floorMat.SetColor("_BaseColor", meadowGreen);
                if (floorMat.HasProperty("_Color")) floorMat.SetColor("_Color", meadowGreen);
                if (floorMat.HasProperty("_Smoothness")) floorMat.SetFloat("_Smoothness", 0.12f);
                EditorUtility.SetDirty(floorMat);
            }
        }

        #endregion

        #region FBX Importers & Animator Controllers

        private static void ConfigureAllFbxImporters(Dictionary<string, Material> mats, StringBuilder report)
        {
            string[] fbxFiles =
            {
                "Park_CrescentPlatform.fbx",
                "Park_Bench.fbx",
                "Park_LampPost.fbx",
                "Park_Pathway.fbx",
                "Park_FlowerBush.fbx",
                "Park_Signpost.fbx",
                "Park_NPC_VisitorA.fbx",
                "Park_NPC_VisitorB.fbx",
                "Park_NPC_VisitorC.fbx",
            };

            foreach (string fname in fbxFiles)
            {
                string fbxPath = $"{ModelsDir}/{fname}";
                ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
                if (importer == null)
                {
                    report.AppendLine($"  WARN: Could not load ModelImporter for {fbxPath}");
                    continue;
                }

                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.globalScale = 1f;
                importer.useFileScale = true;
                importer.importCameras = false;
                importer.importLights = false;

                // Remap only material slots actually present in this FBX to our clean URP Lit materials
                var embeddedMats = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Material>().ToList();
                if (embeddedMats.Count > 0)
                {
                    foreach (Material em in embeddedMats)
                    {
                        if (em != null && mats.TryGetValue(em.name, out Material urpMat))
                        {
                            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), em.name), urpMat);
                        }
                    }
                }
                else
                {
                    foreach (var kv in mats)
                    {
                        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
                    }
                }

                bool isNpc = fname.Contains("NPC");
                if (isNpc)
                {
                    importer.animationType = ModelImporterAnimationType.Generic;
                    importer.importAnimation = true;
                    importer.SaveAndReimport();

                    // Re-fetch to configure default clips with looping enabled
                    importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
                    if (importer != null)
                    {
                        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
                        if (clips != null && clips.Length > 0)
                        {
                            for (int i = 0; i < clips.Length; i++)
                            {
                                clips[i].name = "Walk";
                                clips[i].loopTime = true;
                                clips[i].loopPose = true;
                            }
                            importer.clipAnimations = clips;
                            importer.SaveAndReimport();
                            report.AppendLine($"  Configured NPC FBX {fname}: {clips.Length} looping Walk clip(s)");
                        }
                        else
                        {
                            report.AppendLine($"  WARN: No animation clips found in {fname}");
                        }
                    }
                }
                else
                {
                    importer.animationType = ModelImporterAnimationType.None;
                    importer.importAnimation = false;
                    importer.SaveAndReimport();
                    report.AppendLine($"  Configured Static FBX {fname}");
                }
            }
        }

        private static RuntimeAnimatorController CreateNpcAnimatorController(string fbxName, string controllerPath, StringBuilder report)
        {
            string fbxPath = $"{ModelsDir}/{fbxName}";
            AnimationClip walkClip = AssetDatabase.LoadAllAssetsAtPath(fbxPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));

            if (File.Exists(controllerPath))
                AssetDatabase.DeleteAsset(controllerPath);

            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            AnimatorState walkState = sm.AddState("Walk");
            walkState.motion = walkClip;
            walkState.speed = 1f;
            sm.defaultState = walkState;

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            report.AppendLine($"  AnimatorController {Path.GetFileName(controllerPath)} -> clip={(walkClip != null ? walkClip.name : "NONE")}");
            return ctrl;
        }

        #endregion

        #region Prefab Builders

        private static GameObject BuildCrescentPlatformPrefab()
        {
            GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsDir}/Park_CrescentPlatform.fbx");
            GameObject root = new GameObject("BinsStagePlatform_Crescent");

            if (src != null)
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(src);
                model.name = "CrescentStageMesh";
                model.transform.SetParent(root.transform, false);

                // Add MeshCollider on each mesh renderer so dropped trash rests solidly on the curved deck
                foreach (MeshFilter mf in model.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf.sharedMesh != null)
                    {
                        MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                        mc.sharedMesh = mf.sharedMesh;
                    }
                }
            }

            string path = $"{ParkPrefabsDir}/Park_CrescentPlatform.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildBenchPrefab()
        {
            GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsDir}/Park_Bench.fbx");
            GameObject root = new GameObject("Park_Bench");

            if (src != null)
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(src);
                model.name = "BenchMesh";
                model.transform.SetParent(root.transform, false);
            }

            BoxCollider col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.44f, -0.05f);
            col.size = new Vector3(1.66f, 0.88f, 0.64f);

            string path = $"{ParkPrefabsDir}/Park_Bench.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildLampPostPrefab()
        {
            GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsDir}/Park_LampPost.fbx");
            GameObject root = new GameObject("Park_LampPost");

            if (src != null)
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(src);
                model.name = "LampMesh";
                model.transform.SetParent(root.transform, false);
            }

            // Warm cozy lantern Point Light (shadows disabled for solid 72-90 FPS on Quest VR)
            GameObject lightGO = new GameObject("WarmLanternLight");
            lightGO.transform.SetParent(root.transform, false);
            lightGO.transform.localPosition = new Vector3(0f, 2.52f, 0f);

            Light pt = lightGO.AddComponent<Light>();
            pt.type = LightType.Point;
            pt.lightmapBakeType = LightmapBakeType.Realtime;
            pt.color = new Color(1.00f, 0.86f, 0.58f, 1f);
            pt.intensity = 2.8f;
            pt.range = 7.5f;
            pt.shadows = LightShadows.None;
            pt.renderMode = LightRenderMode.Auto;

            string path = $"{ParkPrefabsDir}/Park_LampPost.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildSimplePropPrefab(string fbxName, string prefabName)
        {
            GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsDir}/{fbxName}");
            GameObject root = new GameObject(prefabName);

            if (src != null)
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(src);
                model.name = prefabName + "_Mesh";
                model.transform.SetParent(root.transform, false);
            }

            string path = $"{ParkPrefabsDir}/{prefabName}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildNpcPrefab(string fbxName, string prefabName, RuntimeAnimatorController ctrl)
        {
            GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsDir}/{fbxName}");
            GameObject root = new GameObject(prefabName);

            Animator anim = null;
            if (src != null)
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(src);
                model.name = "VisualRig";
                model.transform.SetParent(root.transform, false);

                anim = model.GetComponent<Animator>();
                if (anim == null)
                    anim = model.AddComponent<Animator>();

                anim.runtimeAnimatorController = ctrl;
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            ParkNpcWalker walker = root.AddComponent<ParkNpcWalker>();
            SetPrivateField(walker, "npcAnimator", anim);

            string path = $"{ParkPrefabsDir}/{prefabName}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildParkEnvironmentPrefab(
            GameObject pathwayModel,
            GameObject benchPrefab,
            GameObject lampPrefab,
            GameObject bushPrefab,
            GameObject signPrefab,
            GameObject npcAPrefab,
            GameObject npcBPrefab,
            GameObject npcCPrefab)
        {
            GameObject root = new GameObject("EcoParkEnvironment");
            Vector3 ringCenter = new Vector3(0f, 0f, 0.40f);

            // 1. Stone Plaza + Circular Park Sendero + Forest Trail Extensions
            if (pathwayModel != null)
            {
                GameObject pathObj = (GameObject)PrefabUtility.InstantiatePrefab(pathwayModel);
                pathObj.name = "Park_Pathway";
                pathObj.transform.SetParent(root.transform, false);
                pathObj.transform.localPosition = Vector3.zero;
            }

            // 2. Cozy Park Benches (6 benches along the outer side of the ring sendero, facing inward!)
            GameObject benchesRoot = new GameObject("Park_Benches");
            benchesRoot.transform.SetParent(root.transform, false);

            var benchSpecs = new (string name, float deg, float radius)[]
            {
                ("Bench_FrontLeft",   -15f, 5.45f),
                ("Bench_FrontRight",   24f, 5.45f),
                ("Bench_RightFront",   58f, 5.45f),
                ("Bench_RightBack",   105f, 5.45f),
                ("Bench_LeftBack",   -105f, 5.45f),
                ("Bench_BackCenter", -155f, 5.45f),
            };

            foreach (var b in benchSpecs)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(benchPrefab);
                inst.name = b.name;
                inst.transform.SetParent(benchesRoot.transform, false);
                Vector3 pos = RingPoint(ringCenter, b.deg, b.radius);
                inst.transform.localPosition = pos;
                Vector3 inward = (ringCenter - pos);
                inward.y = 0f;
                inst.transform.localRotation = Quaternion.LookRotation(inward.normalized, Vector3.up);
            }

            // 3. Cozy Glowing Park Lamp Posts (6 vintage lanterns alternating with benches!)
            GameObject lampsRoot = new GameObject("Park_LampPosts");
            lampsRoot.transform.SetParent(root.transform, false);

            var lampSpecs = new (string name, float deg, float radius)[]
            {
                ("Lamp_FrontLeft",   -34f, 5.15f),
                ("Lamp_FrontRight",   40f, 5.15f),
                ("Lamp_RightBack",    96f, 5.20f),
                ("Lamp_BackRight",   142f, 5.20f),
                ("Lamp_BackLeft",   -135f, 5.20f),
                ("Lamp_LeftBack",    -96f, 5.20f),
            };

            foreach (var l in lampSpecs)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(lampPrefab);
                inst.name = l.name;
                inst.transform.SetParent(lampsRoot.transform, false);
                Vector3 pos = RingPoint(ringCenter, l.deg, l.radius);
                inst.transform.localPosition = pos;
                Vector3 inward = (ringCenter - pos);
                inward.y = 0f;
                inst.transform.localRotation = Quaternion.LookRotation(inward.normalized, Vector3.up);
            }

            // 4. Flower Bushes framing benches, lamps, and plaza corners
            GameObject bushesRoot = new GameObject("Park_FlowerBushes");
            bushesRoot.transform.SetParent(root.transform, false);

            var bushSpecs = new (Vector3 pos, float rotY, float scale)[]
            {
                (new Vector3(-2.55f, 0f,  2.72f),  25f, 0.95f),
                (new Vector3( 2.55f, 0f,  2.72f), -35f, 0.95f),
                (new Vector3(-3.22f, 0f,  0.65f),  80f, 1.05f),
                (new Vector3( 3.22f, 0f,  0.65f), -70f, 1.05f),
                (new Vector3(-2.35f, 0f, -1.72f), 140f, 0.95f),
                (new Vector3( 2.35f, 0f, -1.72f), 210f, 0.95f),
                (new Vector3(-2.55f, 0f,  5.55f),  15f, 1.15f),
                (new Vector3( 3.35f, 0f,  5.12f), -45f, 1.12f),
                (new Vector3( 5.15f, 0f,  2.35f),  65f, 1.10f),
                (new Vector3( 5.30f, 0f, -2.05f), 120f, 1.10f),
                (new Vector3(-5.30f, 0f, -2.05f), 240f, 1.10f),
                (new Vector3(-1.12f, 0f, -4.92f), 175f, 1.12f),
            };

            for (int i = 0; i < bushSpecs.Length; i++)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(bushPrefab);
                inst.name = $"FlowerBush_{i + 1:00}";
                inst.transform.SetParent(bushesRoot.transform, false);
                inst.transform.localPosition = bushSpecs[i].pos;
                inst.transform.localRotation = Quaternion.Euler(0f, bushSpecs[i].rotY, 0f);
                inst.transform.localScale = Vector3.one * bushSpecs[i].scale;
            }

            // 5. Two Rustic Wooden Park Signposts framing the front garden entrance
            GameObject signsRoot = new GameObject("Park_Signposts");
            signsRoot.transform.SetParent(root.transform, false);

            var signSpecs = new (string name, Vector3 pos, float rotY)[]
            {
                ("Signpost_FrontLeft",  new Vector3(-3.05f, 0f, 3.22f), 208f),
                ("Signpost_FrontRight", new Vector3( 3.05f, 0f, 3.22f), 152f),
            };
            foreach (var s in signSpecs)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(signPrefab);
                inst.name = s.name;
                inst.transform.SetParent(signsRoot.transform, false);
                inst.transform.localPosition = s.pos;
                inst.transform.localRotation = Quaternion.Euler(0f, s.rotY, 0f);
            }

            // 6. 4 Friendly Low-Poly Animated NPCs strolling along two separate lanes of the outer sendero
            // (Inner lane R=4.08m clockwise, Outer lane R=4.64m counter-clockwise -> never collide, never near player!)
            GameObject npcsRoot = new GameObject("Park_WalkingNPCs");
            npcsRoot.transform.SetParent(root.transform, false);

            var npcSpecs = new (string name, GameObject prefab, float radius, bool cw, float startDeg, float speed)[]
            {
                ("ParkNPC_01_CoralCap",    npcAPrefab, 4.08f, true,  -30f, 0.82f),
                ("ParkNPC_02_YellowBeanie",npcBPrefab, 4.64f, false,  38f, 0.88f),
                ("ParkNPC_03_MintSunHat",  npcCPrefab, 4.08f, true,  145f, 0.78f),
                ("ParkNPC_04_Stroller",    npcBPrefab, 4.64f, false,-140f, 0.85f),
            };

            foreach (var n in npcSpecs)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(n.prefab);
                inst.name = n.name;
                inst.transform.SetParent(npcsRoot.transform, false);

                ParkNpcWalker walker = inst.GetComponent<ParkNpcWalker>();
                if (walker != null)
                {
                    walker.Configure(ringCenter, n.radius, n.cw, n.startDeg, n.speed);
                    EditorUtility.SetDirty(walker);
                }
            }

            string path = $"{ParkPrefabsDir}/ParkEnvironment.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static Vector3 RingPoint(Vector3 center, float degFromPlusZ, float radius)
        {
            float rad = degFromPlusZ * Mathf.Deg2Rad;
            return new Vector3(
                center.x + radius * Mathf.Sin(rad),
                0f,
                center.z + radius * Mathf.Cos(rad)
            );
        }

        #endregion

        #region Scene Application (Park + Half-Moon Bins)

        private static string UpdateOpenScene(Scene scene, string sceneName, GameObject parkEnvPrefab, GameObject crescentPrefab)
        {
            // 1. Replace any previous EcoParkEnvironment instance (idempotent!)
            foreach (GameObject rootObj in scene.GetRootGameObjects())
            {
                if (rootObj.name == "EcoParkEnvironment")
                {
                    UnityEngine.Object.DestroyImmediate(rootObj);
                }
            }

            GameObject parkInst = (GameObject)PrefabUtility.InstantiatePrefab(parkEnvPrefab, scene);
            parkInst.name = "EcoParkEnvironment";
            parkInst.transform.position = Vector3.zero;
            parkInst.transform.rotation = Quaternion.identity;

            // 2. Warm cozy park lighting touch on Directional Light
            Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    l.color = new Color(1.00f, 0.96f, 0.89f, 1f);
                    l.intensity = 1.30f;
                    EditorUtility.SetDirty(l);
                }
            }

            // 3. Arrange recycling bins into a spacious Half-Moon (Media Luna) arc facing the player
            GameObject gameplayRoot = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "Gameplay3DRoot");
            int binsArranged = 0;

            if (gameplayRoot != null)
            {
                List<EcoBin> recyclingBins = gameplayRoot
                    .GetComponentsInChildren<EcoBin>(true)
                    .Where(b => b != null && b.gameObject.name.StartsWith("Bin_"))
                    .OrderBy(b => b.transform.localPosition.x)
                    .ToList();

                if (recyclingBins.Count > 0)
                {
                    float[] angles = GetHalfMoonAngles(recyclingBins.Count);
                    const float arcCenterZ = 0.10f;
                    const float arcRadius = 2.15f;
                    const float binBaseY = 0.56f;

                    for (int i = 0; i < recyclingBins.Count; i++)
                    {
                        float deg = angles[i];
                        float rad = deg * Mathf.Deg2Rad;

                        Transform bt = recyclingBins[i].transform;
                        bt.localPosition = new Vector3(
                            arcRadius * Mathf.Sin(rad),
                            binBaseY,
                            arcCenterZ + arcRadius * Mathf.Cos(rad)
                        );
                        // Bin front faces -Z locally, so rotY = +deg aims directly back at (0, binBaseY, arcCenterZ)!
                        bt.localRotation = Quaternion.Euler(0f, deg, 0f);
                        EditorUtility.SetDirty(bt);
                        binsArranged++;
                    }

                    // Replace old straight rectangular BinsStagePlatform with curved Blender Crescent Stage
                    var toDestroy = new List<GameObject>();
                    foreach (Transform child in gameplayRoot.transform)
                    {
                        if (child.name == "BinsStagePlatform" || child.name == "BinsStagePlatform_Crescent")
                        {
                            toDestroy.Add(child.gameObject);
                        }
                    }
                    foreach (GameObject oldStage in toDestroy)
                    {
                        UnityEngine.Object.DestroyImmediate(oldStage);
                    }

                    GameObject crescentInst = (GameObject)PrefabUtility.InstantiatePrefab(crescentPrefab);
                    crescentInst.name = "BinsStagePlatform_Crescent";
                    crescentInst.transform.SetParent(gameplayRoot.transform, false);
                    crescentInst.transform.localPosition = new Vector3(0f, 0f, arcCenterZ);
                    crescentInst.transform.localRotation = Quaternion.identity;
                    crescentInst.transform.localScale = Vector3.one;
                }
            }

            // 4. Gently move InGameHUDCanvas back to z=2.72 so it never overlaps the center apex of the crescent bins (z=2.25)
            GameObject hudObj = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "InGameHUDCanvas");
            if (hudObj != null)
            {
                RectTransform hudRt = hudObj.GetComponent<RectTransform>();
                if (hudRt != null)
                {
                    Vector3 ap = hudRt.anchoredPosition3D;
                    hudRt.anchoredPosition3D = new Vector3(ap.x, Mathf.Max(ap.y, 1.54f), 2.74f);
                    EditorUtility.SetDirty(hudRt);
                }
            }

            return $"Park applied | Bins in half-moon: {binsArranged}";
        }

        private static float[] GetHalfMoonAngles(int count)
        {
            switch (count)
            {
                case 1: return new[] { 0f };
                case 2: return new[] { -26f, 26f };
                case 3: return new[] { -32f, 0f, 32f };
                case 4: return new[] { -45f, -15f, 15f, 45f };
                case 5: return new[] { -56f, -28f, 0f, 28f, 56f };
                case 6: return new[] { -62.5f, -37.5f, -12.5f, 12.5f, 37.5f, 62.5f };
                default:
                    float[] res = new float[count];
                    for (int i = 0; i < count; i++)
                        res[i] = Mathf.Lerp(-65f, 65f, count > 1 ? (float)i / (count - 1) : 0.5f);
                    return res;
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null) return;
            var field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public
            );
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        #endregion
    }
}
