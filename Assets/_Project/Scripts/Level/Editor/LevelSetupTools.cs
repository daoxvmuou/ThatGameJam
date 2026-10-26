#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ThatGameJam.Level.EditorTools
{
    public static class LevelSetupTools
    {
        const string ProjectRoot = "Assets/_Project";
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
