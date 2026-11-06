using UnityEngine;
using UnityEngine.UI;

namespace ThatGameJam
{
    [DisallowMultipleComponent]
    public sealed class UIMainPanel : MonoBehaviour
    {
        [SerializeField] private Image playPauseImage;
        [SerializeField] private Sprite playSprite;
        [SerializeField] private Sprite pauseSprite;
        [SerializeField] private Image musicImage;
        [SerializeField] private Sprite musicOnSprite;
        [SerializeField] private Sprite musicOffSprite;
        [SerializeField] private Sprite heartPopupSprite;
        [SerializeField] private GameObject settingsPanelPrefab;

        private GameObject heartPopupObject;
        private GameObject settingsPanelInstance;
        private AudioSource clickAudioSource;

        void Awake()
        {
            PrepareClickAudio();
            BindClickSounds(transform);
            CreateHeartPopup();
            PrepareSettingsPanel();

            FindButton(transform, "HeartButton")?.onClick.AddListener(ToggleHeartPopup);
            FindButton(transform, "SettingsButton")?.onClick.AddListener(OpenSettings);
        }

        public void TogglePlaybackSprite()
        {
            if (playPauseImage == null || playSprite == null || pauseSprite == null) return;

            playPauseImage.sprite = playPauseImage.sprite == playSprite ? pauseSprite : playSprite;
        }

        public void ToggleMusicSprite()
        {
            if (musicImage == null || musicOnSprite == null || musicOffSprite == null) return;

            musicImage.sprite = musicImage.sprite == musicOnSprite ? musicOffSprite : musicOnSprite;
        }

        public void ToggleHeartPopup()
        {
            if (heartPopupObject == null) return;

            heartPopupObject.SetActive(!heartPopupObject.activeSelf);
        }

        public void OpenSettings()
        {
            if (settingsPanelInstance == null) PrepareSettingsPanel();
            if (settingsPanelInstance != null)
            {
                settingsPanelInstance.transform.SetAsLastSibling();
                settingsPanelInstance.SetActive(true);
            }
        }

        public void CloseSettings()
        {
            if (settingsPanelInstance != null) settingsPanelInstance.SetActive(false);
        }

        void PrepareClickAudio()
        {
            AudioClip clip = Resources.Load<AudioClip>("Sound/Thatgamejam 音效1(1)");
            if (clip == null)
            {
                Debug.LogWarning("[UI] 找不到点击音效 Sound/Thatgamejam 音效1(1)", this);
                return;
            }

            clip.LoadAudioData();
            clickAudioSource = gameObject.AddComponent<AudioSource>();
            clickAudioSource.playOnAwake = false;
            clickAudioSource.spatialBlend = 0f;
            clickAudioSource.ignoreListenerPause = true;
            clickAudioSource.clip = clip;
            AudioVolumeSettings.RefreshSfxVolume();
        }

        void BindClickSounds(Transform root)
        {
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].onClick.RemoveListener(PlayClickSound);
                buttons[i].onClick.AddListener(PlayClickSound);
            }
        }

        void PlayClickSound()
        {
            if (clickAudioSource != null) clickAudioSource.PlayOneShot(clickAudioSource.clip);
        }

        void CreateHeartPopup()
        {
            if (heartPopupSprite == null) return;

            heartPopupObject = new GameObject("HeartPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            heartPopupObject.transform.SetParent(transform, false);

            RectTransform rect = heartPopupObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsFirstSibling();

            Image image = heartPopupObject.GetComponent<Image>();
            image.sprite = heartPopupSprite;
            image.raycastTarget = false;
            heartPopupObject.SetActive(false);
        }

        void PrepareSettingsPanel()
        {
            if (settingsPanelInstance != null) return;

            Transform parent = transform.parent;
            if (parent != null)
            {
                Transform existingPanel = parent.Find("UISettingPanel");
                if (existingPanel != null) settingsPanelInstance = existingPanel.gameObject;
            }

            if (settingsPanelInstance == null)
            {
                if (settingsPanelPrefab == null) return;

                UnityEngine.Object instance = parent == null
                    ? UnityEngine.Object.Instantiate((UnityEngine.Object)settingsPanelPrefab)
                    : UnityEngine.Object.Instantiate((UnityEngine.Object)settingsPanelPrefab, parent, false);
                settingsPanelInstance = instance as GameObject;
                if (settingsPanelInstance == null) return;
            }

            BindClickSounds(settingsPanelInstance.transform);
            Button closeButton = FindButton(settingsPanelInstance.transform, "CloseButton");
            if (closeButton != null) closeButton.onClick.AddListener(CloseSettings);

            settingsPanelInstance.SetActive(false);
        }

        static Button FindButton(Transform root, string buttonName)
        {
            if (root.name == buttonName)
            {
                Button button = root.GetComponent<Button>();
                if (button != null) return button;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Button button = FindButton(root.GetChild(i), buttonName);
                if (button != null) return button;
            }

            return null;
        }
    }
}
