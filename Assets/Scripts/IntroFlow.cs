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

            SetHudVisible(false);

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
            SetHudVisible(true);

            if (introPlayer != null)
            {
                introPlayer.gameObject.SetActive(true);
                introPlayer.Play();
                return;
            }

            if (LevelController.Instance != null && !LevelController.Instance.IsLoaded)
            {
                LevelController.Instance.Load(0);
            }
        }

        void SetHudVisible(bool visible)
        {
            if (string.IsNullOrEmpty(hudRootName)) return;

            Canvas canvas = FindObjectOfType<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;
            Transform hud = FindTransform(parent, hudRootName);
            if (hud != null) hud.gameObject.SetActive(visible);
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
