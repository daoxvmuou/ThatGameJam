using UnityEngine;

namespace ThatGameJam
{
    [DisallowMultipleComponent]
    public class MusicPlayer : MonoBehaviour
    {
        public static MusicPlayer Instance { get; private set; }

        private AudioSource source;

        public AudioClip Current => source != null ? source.clip : null;
        public bool IsPlaying => source != null && source.isPlaying;

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

            if (source.clip == clip && source.isPlaying)
            {
                source.volume = volume;
                return;
            }

            source.clip = clip;
            source.volume = volume;
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
            if (source != null) source.volume = Mathf.Clamp01(volume);
        }
    }
}
