using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using ThatGameJam.Level;

namespace ThatGameJam.Intro
{
    [DisallowMultipleComponent]
    public class IntroPlayer : MonoBehaviour
    {
        public static event Action IntroFinished;

        [Header("资源")]
        [SerializeField] private VideoClip videoClip;
        [SerializeField] private AudioClip bgm;
        [SerializeField] private string fallbackVideoFolder = "Video";

        [Header("音频")]
        [SerializeField] private float bgmVolume = 1f;
        [SerializeField] private bool useVideoAudio = false;

        [Header("流程")]
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool loadLevelAfterIntro = true;
        [SerializeField] private int levelIndex = 0;

        [Header("跳过")]
        [SerializeField] private KeyCode[] skipKeys = { KeyCode.Escape };

        [Header("画面")]
        [SerializeField] private Color backgroundColor = Color.black;

        VideoPlayer player;
        RenderTexture renderTexture;
        GameObject canvasRoot;
        public AudioClip Bgm => bgm;
        bool started;
        bool finished;
        float playStartTime;
        float requestTime;
        float introDuration = 3f;

        public bool IsPlaying => started && !finished;

        void Start()
        {
            if (playOnStart) Play();
        }

        void Update()
        {
            if (!started || finished) return;
            if (SkipKeyPressed())
            {
                Finish();
                return;
            }
            CheckDuration();
        }

        void OnDestroy()
        {
            Cleanup();
        }

        public void Play()
        {
            if (started || finished) return;

            if (videoClip == null && !string.IsNullOrEmpty(fallbackVideoFolder))
            {
                var clips = Resources.LoadAll<VideoClip>(fallbackVideoFolder);
                if (clips != null && clips.Length > 0) videoClip = clips[0];
            }

            if (videoClip == null)
            {
                Debug.LogWarning("[开场] 未配置视频，直接进入关卡。", this);
                Finish();
                return;
            }

            introDuration = videoClip.length > 0.1 ? (float)videoClip.length : 3f;
            requestTime = Time.unscaledTime;

            BuildUI();
            BuildPlayer();
            BuildAudio();

            started = true;
        }

        public void Replay()
        {
            Cleanup();
            started = false;
            finished = false;
            playStartTime = 0f;
            requestTime = 0f;
            enabled = true;
            Play();
        }

        public void Skip()
        {
            Finish();
        }

        void BuildUI()
        {
            canvasRoot = new GameObject("IntroCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;

            var backgroundGo = new GameObject("Background", typeof(Image));
            backgroundGo.transform.SetParent(canvasRoot.transform, false);
            var background = backgroundGo.GetComponent<Image>();
            background.color = backgroundColor;
            Stretch(background.rectTransform);

            int width = videoClip.width > 0 ? (int)videoClip.width : 1920;
            int height = videoClip.height > 0 ? (int)videoClip.height : 1080;
            renderTexture = new RenderTexture(width, height, 0);

            var videoGo = new GameObject("Video", typeof(RawImage));
            videoGo.transform.SetParent(canvasRoot.transform, false);
            var rawImage = videoGo.GetComponent<RawImage>();
            rawImage.texture = renderTexture;
            rawImage.color = Color.white;
            Stretch(rawImage.rectTransform);

            var fitter = videoGo.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = (float)width / height;
        }

        void BuildPlayer()
        {
            player = gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.source = VideoSource.VideoClip;
            player.clip = videoClip;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = renderTexture;
            player.audioOutputMode = useVideoAudio ? VideoAudioOutputMode.Direct : VideoAudioOutputMode.None;
            player.isLooping = false;
            player.skipOnDrop = false;
            player.errorReceived += OnVideoError;

            player.Prepare();
            player.prepareCompleted += OnPrepared;
        }

        void OnPrepared(VideoPlayer source)
        {
            source.prepareCompleted -= OnPrepared;
            if (finished) return;
            source.Play();
            playStartTime = Time.unscaledTime;
        }

        void OnVideoError(VideoPlayer source, string message)
        {
            Debug.LogError($"[开场] 视频播放失败：{message}", this);
        }

        void BuildAudio()
        {
            if (bgm == null) return;
            if (MusicPlayer.Instance != null) MusicPlayer.Instance.Play(bgm, bgmVolume);
        }

        void CheckDuration()
        {
            if (playStartTime <= 0f)
            {
                if (Time.unscaledTime - requestTime > 5f)
                {
                    Debug.LogWarning("[开场] 视频加载超时，直接进入关卡。", this);
                    Finish();
                }
                return;
            }

            if (Time.unscaledTime - playStartTime >= introDuration) Finish();
        }

        bool SkipKeyPressed()
        {
            if (skipKeys == null) return false;
            for (int i = 0; i < skipKeys.Length; i++)
            {
                if (Input.GetKeyDown(skipKeys[i])) return true;
            }
            return false;
        }

        void Finish()
        {
            if (finished) return;
            finished = true;

            Cleanup();

            IntroFinished?.Invoke();

            if (loadLevelAfterIntro && LevelController.Instance != null && !LevelController.Instance.IsLoaded)
            {
                LevelController.Instance.Load(levelIndex);
            }

            enabled = false;
        }

        void Cleanup()
        {
            if (player != null)
            {
                player.errorReceived -= OnVideoError;
                player.prepareCompleted -= OnPrepared;
                player.Stop();
                Destroy(player);
                player = null;
            }

            if (canvasRoot != null)
            {
                Destroy(canvasRoot);
                canvasRoot = null;
            }

            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
                renderTexture = null;
            }
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
