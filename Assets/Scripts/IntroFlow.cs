using UnityEngine;
using UnityEngine.UI;
using ThatGameJam.Intro;
using ThatGameJam.Level;

namespace ThatGameJam
{
    [DisallowMultipleComponent]
    public class IntroFlow : MonoBehaviour
    {
        [Header("开场前界面")]
        [SerializeField] private GameObject settingsPanelPrefab;
        [SerializeField] private string closeButtonName = "CloseButton";
        [SerializeField] private bool anyKeyToContinue = true;
        [SerializeField] private float autoCloseSeconds = 0f;

        [Header("开场动画")]
        [SerializeField] private IntroPlayer introPlayer;

        [Header("游戏内 UI")]
        [SerializeField] private string hudRootName = "UIMainPanel";

        private GameObject panelInstance;
        private Transform hudRoot;

        void Awake()
        {
            hudRoot = FindHudRoot();
            SetHudVisible(false);
        }

        void Start()
        {
            ShowSettings();
        }

        void Update()
        {
            if (panelInstance == null) return;
            if (!anyKeyToContinue) return;
            if (!Input.anyKeyDown) return;

            CloseSettings();
        }

        void OnDestroy()
        {
            CancelInvoke();
            IntroPlayer.IntroFinished -= OnIntroFinished;
        }

        public void ShowSettings()
        {
            if (settingsPanelPrefab == null)
            {
                StartIntro();
                return;
            }

            bool isCanvasPrefab = settingsPanelPrefab.GetComponent<Canvas>() != null;
            Canvas hostCanvas = FindObjectOfType<Canvas>();

            if (isCanvasPrefab)
            {
                panelInstance = Instantiate(settingsPanelPrefab);
                var prefabCanvas = panelInstance.GetComponent<Canvas>();
                if (prefabCanvas != null) prefabCanvas.sortingOrder = 100;
            }
            else
            {
                Transform parent = hostCanvas != null ? hostCanvas.transform : transform;
                panelInstance = Instantiate(settingsPanelPrefab, parent);

                var rect = panelInstance.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    rect.localScale = Vector3.one;
                }
            }

            panelInstance.name = "OpeningCanvas";

            Button closeButton = FindButton(panelInstance.transform, closeButtonName);
            if (closeButton != null) closeButton.onClick.AddListener(CloseSettings);

            if (autoCloseSeconds > 0f) Invoke(nameof(CloseSettings), autoCloseSeconds);
        }

        public void CloseSettings()
        {
            if (panelInstance == null) return;

            CancelInvoke();
            Destroy(panelInstance);
            panelInstance = null;

            StartIntro();
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
