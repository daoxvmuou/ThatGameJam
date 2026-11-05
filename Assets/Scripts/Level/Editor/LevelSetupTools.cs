#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThatGameJam.Intro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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

        [MenuItem("Tools/横版跳跃/⑤ 批量生成 7 个关卡配置", false, 5)]
        public static void CreateLevelsFromMaps()
        {
            EnsureFolder(DataFolder);
            EnsureFolder(LevelsFolder);

            LevelRegistry registry = LoadOrCreateAsset<LevelRegistry>(RegistryPath);

            var maps = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Arts/Prefab/Map" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(prefab => prefab != null)
                .OrderBy(prefab => ExtractNumber(prefab.name))
                .ToList();

            if (maps.Count == 0)
            {
                Debug.LogError("[关卡] 在 Assets/Arts/Prefab/Map 下没找到地图预制体。");
                return;
            }

            var registrySerialized = new SerializedObject(registry);
            registrySerialized.FindProperty("levels").ClearArray();
            registrySerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(registry);

            var lines = new List<string>();
            for (int i = 0; i < maps.Count; i++)
            {
                string path = $"{LevelsFolder}/Level_{i + 1:00}.asset";
                LevelDefinition definition = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                if (definition == null)
                {
                    definition = ScriptableObject.CreateInstance<LevelDefinition>();
                    AssetDatabase.CreateAsset(definition, path);
                }

                var serialized = new SerializedObject(definition);
                serialized.FindProperty("platform").objectReferenceValue = maps[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(definition);

                RegisterLevel(registry, definition);
                lines.Add($"Level_{i + 1:00}  →  {maps[i].name}");
            }

            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            AddHotkeysToGameScene();

            Debug.Log($"[关卡] 已生成并注册 {lines.Count} 个关卡\n  " + string.Join("\n  ", lines));
        }

        [MenuItem("Tools/横版跳跃/⑥ 接入关卡自动推进", false, 6)]
        public static void WireAutoAdvance()
        {
            if (!File.Exists(GameScenePath))
            {
                Debug.LogError($"[关卡] 找不到场景 {GameScenePath}，请先执行 ① 或 ③。");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            if (UnityEngine.Object.FindObjectOfType<LevelAutoAdvance>() == null)
            {
                var advanceGo = new GameObject("LevelAutoAdvance");
                advanceGo.AddComponent<LevelAutoAdvance>();
            }

            if (UnityEngine.Object.FindObjectOfType<LevelHotkeys>() == null)
            {
                var hotkeyGo = new GameObject("LevelHotkeys");
                hotkeyGo.AddComponent<LevelHotkeys>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, GameScenePath);
            AddSceneToBuildSettings();

            Debug.Log("[关卡] 已接入自动推进：走到地图右端进入下一关，第 7 关后回到第 1 关");
        }

        [MenuItem("Tools/横版跳跃/⑧ 整理 UI 布局", false, 8)]
        public static void LayoutGameUi()
        {
            const string uiPrefabPath = "Assets/Arts/Prefab/UI/UIMainPanel.prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(uiPrefabPath) == null)
            {
                Debug.LogError($"[UI] 找不到 UI 预制体 {uiPrefabPath}");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(uiPrefabPath);
            if (root == null) return;

            Transform topGroup = FindChildRecursive(root.transform, "TopButtonGroup");
            if (topGroup != null)
            {
                SetAnchored(topGroup.Find("RestartButton"), 0.06f, new Vector2(64f, 64f));
                SetAnchored(topGroup.Find("HomeButton"), 0.15f, new Vector2(64f, 64f));
                SetAnchored(topGroup.Find("SettingsButton"), 0.24f, new Vector2(64f, 64f));
                SetAnchored(topGroup.Find("HeartButton"), 0.33f, new Vector2(64f, 64f));
                SetAnchored(topGroup.Find("PlaybackBackwardButton"), 0.67f, new Vector2(64f, 64f));
                SetAnchored(topGroup.Find("PlayPauseButton"), 0.76f, new Vector2(64f, 64f));
                SetAnchored(topGroup.Find("PlaybackForwardButton"), 0.85f, new Vector2(64f, 64f));
                SetAnchored(topGroup.Find("MusicToggleButton"), 0.94f, new Vector2(64f, 64f));
            }

            Transform directionPad = FindChildRecursive(root.transform, "DirectionPad");
            if (directionPad != null)
            {
                var padRect = directionPad as RectTransform;
                if (padRect != null)
                {
                    padRect.anchorMin = new Vector2(0.14f, 0.24f);
                    padRect.anchorMax = new Vector2(0.14f, 0.24f);
                    padRect.pivot = new Vector2(0.5f, 0.5f);
                    padRect.anchoredPosition = Vector2.zero;
                    padRect.sizeDelta = new Vector2(200f, 180f);
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, uiPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[UI] 布局已整理：顶部按钮等距分布，方向键移到左下角");
        }

        static void SetAnchored(Transform target, float anchorX, Vector2 size)
        {
            if (target == null) return;

            var rect = target as RectTransform;
            if (rect == null) return;

            rect.anchorMin = new Vector2(anchorX, 0.5f);
            rect.anchorMax = new Vector2(anchorX, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        static Transform FindChildRecursive(Transform root, string name)
        {
            if (root.name == name) return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildRecursive(root.GetChild(i), name);
                if (found != null) return found;
            }

            return null;
        }

        [MenuItem("Tools/横版跳跃/⑦ 接入游戏 UI", false, 7)]
        public static void WireGameUi()
        {
            const string uiPrefabPath = "Assets/Arts/Prefab/UI/UIMainPanel.prefab";

            GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(uiPrefabPath);
            if (uiPrefab == null)
            {
                Debug.LogError($"[UI] 找不到 UI 预制体 {uiPrefabPath}");
                return;
            }

            if (!File.Exists(GameScenePath))
            {
                Debug.LogError($"[UI] 找不到场景 {GameScenePath}，请先执行 ① 或 ③。");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasGo = new GameObject("GameCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            }

            if (canvas.transform.Find("UIMainPanel") == null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(uiPrefab, canvas.transform);
                instance.name = "UIMainPanel";

                var rect = instance.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    rect.localScale = Vector3.one;
                }

                if (instance.GetComponent<GameHudBinder>() == null)
                {
                    instance.AddComponent<GameHudBinder>();
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, GameScenePath);
            AddSceneToBuildSettings();

            Debug.Log("[UI] 游戏 UI 已接入 Game 场景：GameCanvas + EventSystem + UIMainPanel（按钮已绑定功能）");
        }

        static void AddHotkeysToGameScene()
        {
            if (!File.Exists(GameScenePath)) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            if (UnityEngine.Object.FindObjectOfType<LevelHotkeys>() != null) return;

            var go = new GameObject("LevelHotkeys");
            go.AddComponent<LevelHotkeys>();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, GameScenePath);
        }

        static int ExtractNumber(string name)
        {
            int value = 0;
            bool found = false;
            foreach (char c in name)
            {
                if (c >= '0' && c <= '9')
                {
                    value = value * 10 + (c - '0');
                    found = true;
                }
                else if (found)
                {
                    break;
                }
            }
            return value;
        }

        static GameObject BuildPlayerPrefab(string path)
        {
            Sprite sprite = LoadFirstAsset<Sprite>("Assets/Arts/Image/Character/player", "t:Sprite");
            RuntimeAnimatorController controller = LoadFirstAsset<RuntimeAnimatorController>("Assets/Arts/Image/Character/player", "t:AnimatorController");

            var go = new GameObject("Player");
            go.tag = "Player";

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 10;

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
