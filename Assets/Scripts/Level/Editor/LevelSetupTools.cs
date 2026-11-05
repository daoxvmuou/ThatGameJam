#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThatGameJam.Intro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace ThatGameJam.Level.EditorTools
{
    public static class LevelSetupTools
    {
        const string ProjectRoot = "Assets";
        const string DataFolder = ProjectRoot + "/Data";
        const string LevelsFolder = DataFolder + "/Levels";
        const string RegistryPath = DataFolder + "/LevelRegistry.asset";
        const string ScenesFolder = ProjectRoot + "/Scenes";
        const string GameScenePath = ScenesFolder + "/Game.unity";

        [MenuItem("Tools/横版跳跃/① 初始化关卡系统", false, 1)]
        public static void Initialize()
        {
            EnsureFolder(ProjectRoot);
            EnsureFolder(ProjectRoot + "/Scripts");
            EnsureFolder(ProjectRoot + "/Scripts/Level");
            EnsureFolder(DataFolder);
            EnsureFolder(LevelsFolder);
            EnsureFolder(ScenesFolder);

            LevelRegistry registry = LoadOrCreateAsset<LevelRegistry>(RegistryPath);
            LevelDefinition sample = LoadOrCreateAsset<LevelDefinition>(LevelsFolder + "/Level_01.asset");
            RegisterLevel(registry, sample);

            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();

            CreateGameScene(registry);

            AssetDatabase.Refresh();
            Debug.Log("[关卡] 初始化完成\n" +
                      $"  · 关卡总表：{RegistryPath}\n" +
                      $"  · 示例关卡：{LevelsFolder}/Level_01.asset\n" +
                      $"  · 玩法场景：{GameScenePath}");
        }

        [MenuItem("Tools/横版跳跃/② 新建关卡配置", false, 2)]
        public static void CreateLevelAsset()
        {
            EnsureFolder(LevelsFolder);
            EnsureFolder(DataFolder);

            LevelRegistry registry = LoadOrCreateAsset<LevelRegistry>(RegistryPath);

            string path = AssetDatabase.GenerateUniqueAssetPath(LevelsFolder + "/Level_New.asset");
            LevelDefinition definition = ScriptableObject.CreateInstance<LevelDefinition>();
            AssetDatabase.CreateAsset(definition, path);

            RegisterLevel(registry, definition);
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = definition;
            EditorGUIUtility.PingObject(definition);
            Debug.Log($"[关卡] 已创建并注册关卡配置：{path}");
        }

        [MenuItem("Tools/横版跳跃/③ 重建 Game 场景结构", false, 3)]
        public static void RebuildGameScene()
        {
            EnsureFolder(ScenesFolder);
            LevelRegistry registry = LoadOrCreateAsset<LevelRegistry>(RegistryPath);

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = File.Exists(GameScenePath)
                ? EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera camera = UnityEngine.Object.FindObjectOfType<Camera>();
            if (camera == null)
            {
                var cameraGo = new GameObject("Main Camera");
                cameraGo.tag = "MainCamera";
                camera = cameraGo.AddComponent<Camera>();
                cameraGo.AddComponent<AudioListener>();
                cameraGo.transform.position = new Vector3(0f, 0f, -10f);
            }
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.08f, 0.12f, 1f);

            LevelController controller = UnityEngine.Object.FindObjectOfType<LevelController>();
            if (controller == null)
            {
                var controllerGo = new GameObject("LevelController");
                controller = controllerGo.AddComponent<LevelController>();
            }

            Transform anchor = controller.transform.Find("LevelRoot");
            if (anchor == null)
            {
                var anchorGo = new GameObject("LevelRoot");
                anchorGo.transform.SetParent(controller.transform, false);
                anchor = anchorGo.transform;
            }

            var controllerSerialized = new SerializedObject(controller);
            controllerSerialized.FindProperty("registry").objectReferenceValue = registry;
            controllerSerialized.FindProperty("levelParent").objectReferenceValue = anchor;
            controllerSerialized.FindProperty("loadFirstLevelOnStart").boolValue = false;
            controllerSerialized.ApplyModifiedPropertiesWithoutUndo();

            IntroPlayer intro = UnityEngine.Object.FindObjectOfType<IntroPlayer>(true);
            if (intro == null)
            {
                var introGo = new GameObject("IntroPlayer");
                intro = introGo.AddComponent<IntroPlayer>();
            }

            var introSerialized = new SerializedObject(intro);
            SerializedProperty videoProperty = introSerialized.FindProperty("videoClip");
            if (videoProperty != null && videoProperty.objectReferenceValue == null)
            {
                string[] videoGuids = AssetDatabase.FindAssets("t:VideoClip", new[] { "Assets/Resources/Video" });
                if (videoGuids.Length > 0)
                {
                    string videoPath = AssetDatabase.GUIDToAssetPath(videoGuids[0]);
                    videoProperty.objectReferenceValue = AssetDatabase.LoadAssetAtPath<VideoClip>(videoPath);
                }
                introSerialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, GameScenePath);
            AddSceneToBuildSettings();

            Selection.activeGameObject = intro.gameObject;
            EditorGUIUtility.PingObject(intro.gameObject);
            Debug.Log("[关卡] Game 场景结构已重建：Main Camera / LevelController / LevelRoot / IntroPlayer");
        }

        [MenuItem("Tools/横版跳跃/④ 生成玩家预制体并接入场景", false, 4)]
        public static void CreatePlayerPrefabAndWire()
        {
            const string prefabsFolder = "Assets/Prefabs";
            const string playerPrefabPath = prefabsFolder + "/Player.prefab";

            EnsureFolder(prefabsFolder);

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath);
            if (playerPrefab == null) playerPrefab = BuildPlayerPrefab(playerPrefabPath);

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = File.Exists(GameScenePath)
                ? EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera camera = UnityEngine.Object.FindObjectOfType<Camera>();
            if (camera != null && camera.GetComponent<CameraFollow>() == null)
            {
                camera.gameObject.AddComponent<CameraFollow>();
            }

            PlayerSpawner spawner = UnityEngine.Object.FindObjectOfType<PlayerSpawner>();
            if (spawner == null)
            {
                var spawnerGo = new GameObject("PlayerSpawner");
                spawner = spawnerGo.AddComponent<PlayerSpawner>();
            }

            var serialized = new SerializedObject(spawner);
            SerializedProperty prefabProperty = serialized.FindProperty("playerPrefab");
            if (prefabProperty != null) prefabProperty.objectReferenceValue = playerPrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, GameScenePath);
            AddSceneToBuildSettings();

            Selection.activeObject = playerPrefab;
            EditorGUIUtility.PingObject(playerPrefab);
            Debug.Log("[关卡] 玩家预制体已生成，PlayerSpawner 与 CameraFollow 已接入 Game 场景");
        }

        static GameObject BuildPlayerPrefab(string path)
        {
            Sprite sprite = LoadFirstAsset<Sprite>("Assets/Arts/Image/Character/player", "t:Sprite");
            RuntimeAnimatorController controller = LoadFirstAsset<RuntimeAnimatorController>("Assets/Arts/Image/Character/player", "t:AnimatorController");

            var go = new GameObject("Player");
            go.tag = "Player";

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;

            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 3f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var collider = go.AddComponent<BoxCollider2D>();
            if (sprite != null)
            {
                collider.size = sprite.bounds.size;
                collider.offset = sprite.bounds.center;
            }

            var animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            go.AddComponent<PlayerController>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            AssetDatabase.SaveAssets();
            return prefab;
        }

        static T LoadFirstAsset<T>(string folder, string filter) where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets(filter, new[] { folder });
            if (guids == null || guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        static void CreateGameScene(LevelRegistry registry)
        {
            if (File.Exists(GameScenePath))
            {
                Debug.Log($"[关卡] 场景已存在，跳过创建：{GameScenePath}");
                AddSceneToBuildSettings();
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.08f, 0.12f, 1f);
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);
            cameraGo.AddComponent<AudioListener>();

            var controllerGo = new GameObject("LevelController");
            var controller = controllerGo.AddComponent<LevelController>();

            var anchor = new GameObject("LevelRoot");
            anchor.transform.SetParent(controllerGo.transform, false);

            var serialized = new SerializedObject(controller);
            serialized.FindProperty("registry").objectReferenceValue = registry;
            serialized.FindProperty("levelParent").objectReferenceValue = anchor.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, GameScenePath);
            AddSceneToBuildSettings();
        }

        static void AddSceneToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Any(s => s.path == GameScenePath)) return;

            scenes.Add(new EditorBuildSettingsScene(GameScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[关卡] 已把 {GameScenePath} 加入 Build Settings。");
        }

        static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void RegisterLevel(LevelRegistry registry, LevelDefinition definition)
        {
            var serialized = new SerializedObject(registry);
            SerializedProperty list = serialized.FindProperty("levels");

            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == definition) return;
            }

            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = definition;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            string name = Path.GetFileName(folder);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name)) return;

            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
