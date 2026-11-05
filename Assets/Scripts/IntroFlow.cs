using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ThatGameJam.Intro;
using ThatGameJam.Level;

namespace ThatGameJam
{
    [DisallowMultipleComponent]
    public class IntroFlow : MonoBehaviour
    {
        [Header("开场前的界面（按顺序播放）")]
        [SerializeField] private List<GameObject> panels = new List<GameObject>();
        [SerializeField] private string closeButtonName = "CloseButton";
        [SerializeField] private bool anyKeyToContinue = true;
        [SerializeField] private float autoCloseSeconds = 0f;

        [Header("开场动画")]
        [SerializeField] private IntroPlayer introPlayer;

        [Header("游戏内 UI")]
        [SerializeField] private string hudRootName = "UIMainPanel";

        [Header("音乐")]
        [SerializeField] private AudioClip bgm;
        [SerializeField, Range(0f, 1f)] private float bgmVolume = 1f;

        private GameObject panelInstance;
        private Transform hudRoot;
        private bool panelDismissed;

        void Awake()
        {
            hudRoot = FindHudRoot();
            SetHudVisible(false);
            PlayBgm();
        }

        void Start()
        {
            StartCoroutine(RunPanels());
        }

        void OnDestroy()
        {
            IntroPlayer.IntroFinished -= OnIntroFinished;
        }

        IEnumerator RunPanels()
        {
            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i] == null) continue;
                yield return ShowPanel(panels[i]);
            }

            StartIntro();
        }

        IEnumerator ShowPanel(GameObject prefab)
        {
            panelInstance = InstantiatePanel(prefab);
            panelDismissed = false;

            Button closeButton = FindButton(panelInstance.transform, closeButtonName);
            if (closeButton != null) closeButton.onClick.AddListener(CloseSettings);

            var narration = panelInstance.GetComponentInChildren<OpeningNarration>(true);
            if (narration != null)
            {
                while (!narration.IsComplete)
                {
                    if (anyKeyToContinue && Input.anyKeyDown) break;
                    yield return null;
                }
            }
            else
            {
                float remaining = autoCloseSeconds;
                while (!panelDismissed)
                {
                    if (anyKeyToContinue && Input.anyKeyDown) break;
                    if (autoCloseSeconds > 0f && (remaining -= Time.unscaledDeltaTime) <= 0f) break;
                    yield return null;
                }
            }

            if (panelInstance != null) Destroy(panelInstance);
            panelInstance = null;
        }

        GameObject InstantiatePanel(GameObject prefab)
        {
            bool isCanvasPrefab = prefab.GetComponent<Canvas>() != null;
            bool isNarration = prefab.GetComponent<OpeningNarration>() != null;

            if (isCanvasPrefab || isNarration)
            {
                GameObject instance = Instantiate(prefab);
                var canvas = instance.GetComponent<Canvas>();
                if (canvas != null) canvas.sortingOrder = 100;
                return instance;
            }

            Canvas hostCanvas = FindObjectOfType<Canvas>();
            Transform parent = hostCanvas != null ? hostCanvas.transform : transform;
            GameObject panel = Instantiate(prefab, parent);

            var rect = panel.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
            }

            return panel;
        }

        public void CloseSettings()
        {
            panelDismissed = true;
        }

        void StartIntro()
        {
            if (introPlayer != null)
            {
                IntroPlayer.IntroFinished += OnIntroFinished;
                introPlayer.gameObject.SetActive(true);
                introPlayer.Play();
                return;
            }

            SetHudVisible(true);

            if (LevelController.Instance != null && !LevelController.Instance.IsLoaded)
            {
                LevelController.Instance.Load(0);
            }
        }

        void OnIntroFinished()
        {
            IntroPlayer.IntroFinished -= OnIntroFinished;
            SetHudVisible(true);
        }

        void PlayBgm()
        {
            AudioClip clip = bgm != null ? bgm : (introPlayer != null ? introPlayer.Bgm : null);
            if (clip == null || MusicPlayer.Instance == null) return;
            MusicPlayer.Instance.Play(clip, bgmVolume);
        }

        void SetHudVisible(bool visible)
        {
            if (hudRoot != null) hudRoot.gameObject.SetActive(visible);
        }

        Transform FindHudRoot()
        {
            if (string.IsNullOrEmpty(hudRootName)) return null;

            Canvas[] canvases = FindObjectsOfType<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                Transform found = FindTransform(canvases[i].transform, hudRootName);
                if (found != null) return found;
            }

            return null;
        }

        static Transform FindTransform(Transform root, string name)
        {
            if (root.name == name) return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindTransform(root.GetChild(i), name);
                if (found != null) return found;
            }

            return null;
        }

        static Button FindButton(Transform root, string name)
        {
            if (root.name == name)
            {
                Button direct = root.GetComponent<Button>();
                if (direct != null) return direct;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Button found = FindButton(root.GetChild(i), name);
                if (found != null) return found;
            }

            return null;
        }
    }
}
