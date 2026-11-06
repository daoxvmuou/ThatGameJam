using UnityEngine;

namespace ThatGameJam
{
    [DisallowMultipleComponent]
    public class MusicPlayer : MonoBehaviour
    {
        public static MusicPlayer Instance { get; private set; }

        private AudioSource source;
        private float musicVolume = 1f;
        private float trackVolume = 1f;

        public AudioClip Current => source != null ? source.clip : null;
        public bool IsPlaying => source != null && source.isPlaying;
        public float Volume => musicVolume;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;

            var go = new GameObject("MusicPlayer");
            go.AddComponent<MusicPlayer>();
            DontDestroyOnLoad(go);
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
        }

        public void Play(AudioClip clip, float volume = 1f)
        {
            if (source == null) return;

            if (clip == null)
            {
                Stop();
                return;
            }

            trackVolume = Mathf.Clamp01(volume);

            if (source.clip == clip && source.isPlaying)
            {
                ApplyVolume();
                return;
            }

            source.clip = clip;
            ApplyVolume();
            source.Play();
        }

        public void Stop()
        {
            if (source == null) return;
            source.Stop();
            source.clip = null;
        }

        public void SetVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            ApplyVolume();
        }

        void ApplyVolume()
        {
            if (source != null) source.volume = trackVolume * musicVolume;
        }
    }
}
