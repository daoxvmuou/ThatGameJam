using System;
using UnityEngine;

namespace ThatGameJam.Level
{
    [DisallowMultipleComponent]
    public class LevelController : MonoBehaviour
    {
        public static LevelController Instance { get; private set; }

        public static event Action<LevelDefinition> LevelLoaded;
        public static event Action<LevelDefinition> LevelUnloaded;

        [Header("关卡数据")]
        [SerializeField] private LevelRegistry registry;

        [Header("挂载点")]
        [SerializeField] private Transform levelParent;

        [Header("音乐")]
        [SerializeField] private AudioSource musicSource;

        [Header("启动行为")]
        [SerializeField] private bool loadFirstLevelOnStart = true;

        [Header("层级顺序")]
        [SerializeField] private bool applySortingOrder = true;
        [SerializeField] private int backgroundSortingOrder = -100;
        [SerializeField] private int scenerySortingOrder = -50;
        [SerializeField] private int platformSortingOrder = 0;

        public LevelDefinition Current { get; private set; }
        public int CurrentIndex { get; private set; } = -1;
        public LevelRoot Root { get; private set; }
        public LevelRegistry Registry => registry;
        public int LevelCount => registry != null ? registry.Count : 0;
        public bool IsLoaded => Root != null;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[关卡] 场景中存在多个 LevelController，仅保留第一个。", this);
            }
            else
            {
                Instance = this;
            }

            if (levelParent == null) levelParent = transform;
        }

        void Start()
        {
            if (loadFirstLevelOnStart && Current == null && LevelCount > 0)
            {
                Load(0);
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool Load(int index)
        {
            if (registry == null)
            {
                Debug.LogError("[关卡] LevelRegistry 未配置，无法加载关卡。", this);
                return false;
            }

            LevelDefinition definition = registry.Get(index);
            if (definition == null)
            {
                Debug.LogError($"[关卡] 序号 {index} 在关卡总表中不存在。", this);
                return false;
            }

            return LoadInternal(definition, index);
        }

        public bool Load(LevelDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogError("[关卡] 关卡配置为空。", this);
                return false;
            }

            int index = registry != null ? registry.IndexOf(definition) : -1;
            return LoadInternal(definition, index);
        }

        public bool Load(string levelName)
        {
            if (registry == null)
            {
                Debug.LogError("[关卡] LevelRegistry 未配置，无法按名字加载关卡。", this);
                return false;
            }

            LevelDefinition definition = registry.GetByName(levelName);
            if (definition == null)
            {
                Debug.LogError($"[关卡] 找不到名为 {levelName} 的关卡。", this);
                return false;
            }

            return LoadInternal(definition, registry.IndexOf(definition));
        }

        public void Restart()
        {
            if (Current == null)
            {
                Debug.LogWarning("[关卡] 当前没有已加载的关卡，Restart 忽略。", this);
                return;
            }

            LoadInternal(Current, CurrentIndex);
        }

        public void Unload()
        {
            LevelDefinition previous = Current;

            StopMusic();

            if (Root != null)
            {
                if (Application.isPlaying) Destroy(Root.gameObject);
                else DestroyImmediate(Root.gameObject);
            }

            Root = null;
            Current = null;
            CurrentIndex = -1;

            if (previous != null) LevelUnloaded?.Invoke(previous);
        }

        bool LoadInternal(LevelDefinition definition, int index)
        {
            Unload();

            GameObject host = new GameObject("Level_" + definition.name);
            host.transform.SetParent(levelParent, false);

            LevelRoot root = host.AddComponent<LevelRoot>();

            GameObject backgroundPart = SpawnPart(definition.Background, host.transform, "Background", backgroundSortingOrder);
            GameObject sceneryPart = SpawnPart(definition.Scenery, host.transform, "Scenery", scenerySortingOrder);
            GameObject platformPart = SpawnPart(definition.Platform, host.transform, "Platform", platformSortingOrder);

            root.Bind(definition, backgroundPart, platformPart, sceneryPart);

            Root = root;
            Current = definition;
            CurrentIndex = index;

            PlayMusic(definition.Music);

            LevelLoaded?.Invoke(definition);
            return true;
        }

        GameObject SpawnPart(GameObject prefab, Transform parent, string partName, int sortingOrder)
        {
            if (prefab == null) return null;

            GameObject instance = Instantiate(prefab, parent);
            instance.name = partName;

            if (applySortingOrder)
            {
                SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
                if (renderer != null) renderer.sortingOrder = sortingOrder;
            }

            return instance;
        }

        void PlayMusic(AudioClip clip)
        {
            if (musicSource == null) musicSource = gameObject.GetComponent<AudioSource>();
            if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();

            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.clip = clip;

            if (clip != null) musicSource.Play();
            else musicSource.Stop();
        }

        void StopMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
                musicSource.clip = null;
            }
        }
    }
}
