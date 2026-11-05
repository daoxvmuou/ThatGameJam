using UnityEngine;
using UnityEngine.UI;
using ThatGameJam.Intro;
using ThatGameJam.Level;

namespace ThatGameJam
{
    [DisallowMultipleComponent]
    public class IntroFlow : MonoBehaviour
    {
        [Header("设置界面")]
        [SerializeField] private GameObject settingsPanelPrefab;
        [SerializeField] private string closeButtonName = "CloseButton";

        [Header("开场动画")]
        [SerializeField] private IntroPlayer introPlayer;

        [Header("游戏内 UI")]
        [SerializeField] private string hudRootName = "UIMainPanel";

        private GameObject panelInstance;

        void Start()
        {
            ShowSettings();
        }

        public void ShowSettings()
        {
            if (settingsPanelPrefab == null)
            {
                StartIntro();
                return;
            }

            Canvas canvas = FindObjectOfType<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;

            panelInstance = Instantiate(settingsPanelPrefab, parent);
            panelInstance.name = "UISettingPanel";

            var rect = panelInstance.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
            }

            Button closeButton = FindButton(panelInstance.transform, closeButtonName);
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseSettings);
            }
            else
            {
                Debug.LogWarning($"[开场] 设置界面里找不到 {closeButtonName}", this);
            }

            SetHudVisible(false);
        }

        public void CloseSettings()
        {
            if (panelInstance != null)
            {
                Destroy(panelInstance);
                panelInstance = null;
            }

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
