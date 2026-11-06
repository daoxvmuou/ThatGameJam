using UnityEngine;
using UnityEngine.UI;

namespace ThatGameJam
{
    [DisallowMultipleComponent]
    public sealed class UISettingPanelAudio : MonoBehaviour
    {
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        void Awake()
        {
            if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
            if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
            if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(SetSfxVolume);
        }

        void OnEnable()
        {
            AudioVolumeSettings.RefreshSfxVolume();

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(AudioVolumeSettings.MasterVolume);
            }

            if (musicVolumeSlider != null)
            {
                float volume = MusicPlayer.Instance != null ? MusicPlayer.Instance.Volume : 1f;
                musicVolumeSlider.SetValueWithoutNotify(volume);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(AudioVolumeSettings.SfxVolume);
            }
        }

        void SetMasterVolume(float volume)
        {
            AudioVolumeSettings.SetMasterVolume(volume);
        }

        void SetMusicVolume(float volume)
        {
            if (MusicPlayer.Instance != null) MusicPlayer.Instance.SetVolume(volume);
        }

        void SetSfxVolume(float volume)
        {
            AudioVolumeSettings.SetSfxVolume(volume);
        }
    }
}
