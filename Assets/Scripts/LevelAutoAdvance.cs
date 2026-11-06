using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using ThatGameJam.Level;

namespace ThatGameJam
{
    [DisallowMultipleComponent]
    public class LevelAutoAdvance : MonoBehaviour
    {
        [Header("触发")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private float rightMargin = 0.5f;
        [SerializeField] private float fallbackHalfWidth = 20f;

        [Header("流程")]
        [SerializeField] private bool loopToFirstLevel = true;

        [Header("结局")]
        [SerializeField] private GameObject blackScreenPrefab;
        [SerializeField] private GameObject endScreenPrefab;
        [SerializeField] private GameObject endMenuPrefab;
        [SerializeField] private bool showEndScreenOnLastLevel = true;
        [SerializeField] private bool stopPlayerAtEnd = true;
        [SerializeField] private float blackScreenDuration = 6f;
        [SerializeField] private string endingVideoResourcePath = "Video/结尾动画";
        [SerializeField] private float endScreenDuration = 6f;
        [SerializeField] private float videoFadeDuration = 1f;

        private Bounds levelBounds;
        private bool hasBounds;
        private Transform player;
        private bool advancing;

        void OnEnable()
        {
            LevelController.LevelLoaded += OnLevelLoaded;
        }

        void OnDisable()
        {
            LevelController.LevelLoaded -= OnLevelLoaded;
        }

        void Start()
        {
            if (LevelController.Instance != null && LevelController.Instance.IsLoaded) RecalculateBounds();
        }

        void Update()
        {
            if (advancing) return;

            if (player == null) ResolvePlayer();
            if (player == null) return;

            float rightEdge = hasBounds ? levelBounds.max.x - rightMargin : fallbackHalfWidth - rightMargin;
            if (player.position.x >= rightEdge) Advance();
        }

        void OnLevelLoaded(LevelDefinition definition)
        {
            advancing = false;
            player = null;
            RecalculateBounds();
        }

        void Advance()
        {
            LevelController controller = LevelController.Instance;
            if (controller == null) return;

            int next = controller.CurrentIndex + 1;
            if (next >= controller.LevelCount)
            {
                if (showEndScreenOnLastLevel && endScreenPrefab != null)
                {
                    ShowEndScreen();
                    return;
                }

                if (!loopToFirstLevel)
                {
                    advancing = true;
                    controller.Unload();
                    return;
                }
                next = 0;
            }

            advancing = true;
            player = null;
            controller.Load(next);
        }

        void ShowEndScreen()
        {
            advancing = true;

            if (stopPlayerAtEnd)
            {
                ResolvePlayer();
                if (player != null)
                {
                    PlayerController controller = player.GetComponent<PlayerController>();
                    if (controller != null) controller.SetControlEnabled(false);
                }
            }

            StartCoroutine(PlayEndingSequence());
        }

        IEnumerator PlayEndingSequence()
        {
            if (MusicPlayer.Instance != null)
            {
                AudioClip endingMusic = Resources.Load<AudioClip>("Sound/bA大调，结束配乐");
                MusicPlayer.Instance.Play(endingMusic);
            }

            GameObject blackCover = CreateBlackCover();
            GameObject blackScreen = blackScreenPrefab != null ? Instantiate(blackScreenPrefab) : null;
            if (blackScreenDuration > 0f) yield return new WaitForSecondsRealtime(blackScreenDuration);
            if (blackScreen != null) Destroy(blackScreen);

            yield return PlayEndingVideo();

            GameObject endScreen = endScreenPrefab != null ? Instantiate(endScreenPrefab) : null;
            OpeningNarration narration = endScreen != null ? endScreen.GetComponentInChildren<OpeningNarration>(true) : null;
            if (narration != null)
            {
                while (narration != null && !narration.IsComplete) yield return null;
            }
            else if (endScreenDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(endScreenDuration);
            }

            if (endMenuPrefab != null)
            {
                if (endScreen != null) Destroy(endScreen);
                if (blackCover != null) Destroy(blackCover);
                Instantiate(endMenuPrefab);
            }

        }

        IEnumerator PlayEndingVideo()
        {
            VideoClip clip = Resources.Load<VideoClip>(endingVideoResourcePath);
            if (clip == null)
            {
                Debug.LogWarning($"[结局] 找不到视频资源：Resources/{endingVideoResourcePath}", this);
                yield break;
            }

            int width = clip.width > 0 ? (int)clip.width : 1920;
            int height = clip.height > 0 ? (int)clip.height : 1080;
            RenderTexture renderTexture = new RenderTexture(width, height, 0);
            GameObject canvasRoot = new GameObject("EndingVideoCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;

            GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backgroundObject.transform.SetParent(canvasRoot.transform, false);
            RectTransform backgroundRect = (RectTransform)backgroundObject.transform;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            backgroundObject.GetComponent<Image>().color = Color.black;

            GameObject videoObject = new GameObject("Video", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            videoObject.transform.SetParent(canvasRoot.transform, false);
            RectTransform videoRect = (RectTransform)videoObject.transform;
            videoRect.anchorMin = Vector2.zero;
            videoRect.anchorMax = Vector2.one;
            videoRect.offsetMin = Vector2.zero;
            videoRect.offsetMax = Vector2.zero;
            RawImage videoImage = videoObject.GetComponent<RawImage>();
            videoImage.texture = renderTexture;
            videoImage.color = new Color(1f, 1f, 1f, 0f);

            VideoPlayer player = gameObject.AddComponent<VideoPlayer>();
            bool prepared = false;
            bool finished = false;
            bool failed = false;
            player.playOnAwake = false;
            player.source = VideoSource.VideoClip;
            player.clip = clip;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = renderTexture;
            player.audioOutputMode = VideoAudioOutputMode.Direct;
            player.isLooping = false;
            player.skipOnDrop = false;
            player.prepareCompleted += source => prepared = true;
            player.loopPointReached += source => finished = true;
            player.errorReceived += (source, message) =>
            {
                failed = true;
                Debug.LogError($"[结局] 视频播放失败：{message}", this);
            };

            player.Prepare();
            float prepareStart = Time.unscaledTime;
            while (!prepared && !failed && Time.unscaledTime - prepareStart < 10f)
            {
                yield return null;
            }

            if (prepared && !failed)
            {
                player.Play();
                yield return FadeVideo(videoImage, 0f, 1f);
                while (!finished && !failed) yield return null;
                if (!failed) yield return FadeVideo(videoImage, 1f, 0f);
            }

            player.Stop();
            Destroy(player);
            Destroy(canvasRoot);
            renderTexture.Release();
            Destroy(renderTexture);
        }

        GameObject CreateBlackCover()
        {
            GameObject canvasObject = new GameObject("EndingBlackCover", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            background.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = (RectTransform)background.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = Color.black;
            return canvasObject;
        }

        IEnumerator FadeVideo(RawImage image, float from, float to)
        {
            if (videoFadeDuration <= 0f)
            {
                image.color = new Color(1f, 1f, 1f, to);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < videoFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / videoFadeDuration));
                image.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }
        }

        void ResolvePlayer()
        {
            if (string.IsNullOrEmpty(playerTag)) return;
            GameObject found = GameObject.FindGameObjectWithTag(playerTag);
            if (found != null) player = found.transform;
        }

        void RecalculateBounds()
        {
            hasBounds = false;

            LevelController controller = LevelController.Instance;
            if (controller == null || controller.Root == null) return;

            Renderer[] renderers = controller.Root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            levelBounds = bounds;
            hasBounds = true;
        }

        void OnDrawGizmosSelected()
        {
            if (!hasBounds) return;
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3(levelBounds.max.x - rightMargin, levelBounds.center.y, 0f);
            Gizmos.DrawWireCube(center, new Vector3(0.2f, levelBounds.size.y, 0f));
        }
    }
}
