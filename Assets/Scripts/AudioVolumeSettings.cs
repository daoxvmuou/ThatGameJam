using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThatGameJam
{
    public static class AudioVolumeSettings
    {
        private static readonly Dictionary<AudioSource, float> sfxBaseVolumes = new Dictionary<AudioSource, float>();
        private static float masterVolume = 1f;
        private static float sfxVolume = 1f;
        private static bool muted;
        private static bool initialized;

        public static float MasterVolume => masterVolume;
        public static float SfxVolume => sfxVolume;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            if (initialized) return;

            initialized = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyMasterVolume();
        }

        public static void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            ApplyMasterVolume();
        }

        public static void ToggleMute()
        {
            muted = !muted;
            ApplyMasterVolume();
        }

        public static void SetSfxVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            ApplySfxVolume();
        }

        public static void RefreshSfxVolume()
        {
            ApplySfxVolume();
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplySfxVolume();
        }

        static void ApplyMasterVolume()
        {
            AudioListener.volume = muted ? 0f : masterVolume;
        }

        static void ApplySfxVolume()
        {
            AudioSource[] sources = Object.FindObjectsOfType<AudioSource>(true);
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = sources[i];
                if (source == null || source.GetComponent<MusicPlayer>() != null) continue;

                if (!sfxBaseVolumes.TryGetValue(source, out float baseVolume))
                {
                    baseVolume = source.volume;
                    sfxBaseVolumes.Add(source, baseVolume);
                }

                source.volume = baseVolume * sfxVolume;
            }

            List<AudioSource> destroyedSources = null;
            foreach (AudioSource source in sfxBaseVolumes.Keys)
            {
                if (source != null) continue;
                if (destroyedSources == null) destroyedSources = new List<AudioSource>();
                destroyedSources.Add(source);
            }

            if (destroyedSources == null) return;
            for (int i = 0; i < destroyedSources.Count; i++)
            {
                sfxBaseVolumes.Remove(destroyedSources[i]);
            }
        }
    }
}
