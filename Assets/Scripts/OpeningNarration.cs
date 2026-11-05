using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ThatGameJam
{
    public class OpeningNarration : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Material glowMaterial;
        [SerializeField] private string title;
        [SerializeField] private string[] sentences;
        [SerializeField, Min(0.1f)] private float durationSeconds = 15f;
        [SerializeField, Min(0f)] private float fadeSeconds = 1f;

        private CanvasGroup group;
        private TextMeshProUGUI body;
        private float[] sentenceEndTimes;
        private float elapsed;

        public bool IsComplete { get; private set; }

        void Awake()
        {
            if (font == null || glowMaterial == null || sentences == null || sentences.Length == 0)
            {
                Debug.LogError("[Opening] Configure a font, glow material, and at least one sentence.", this);
                enabled = false;
                IsComplete = true;
                return;
            }

            sentenceEndTimes = new float[sentences.Length];
            float totalWeight = 0f;
            foreach (string sentence in sentences) totalWeight += Mathf.Max(30, sentence.Length);
            float accumulatedWeight = 0f;
            for (int i = 0; i < sentences.Length; i++)
            {
                accumulatedWeight += Mathf.Max(30, sentences[i].Length);
                sentenceEndTimes[i] = durationSeconds * accumulatedWeight / totalWeight;
            }

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            var background = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            background.transform.SetParent(transform, false);
            var backgroundRect = (RectTransform)background.transform;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = Color.black;

            if (!string.IsNullOrWhiteSpace(title))
            {
                CreateText("Title", title, 68f, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.94f));
                body = CreateText("Narration", sentences[0], 42f,
                    new Vector2(0.12f, 0.25f), new Vector2(0.88f, 0.7f));
            }
            else
            {
                body = CreateText("Narration", sentences[0], 48f,
                    new Vector2(0.12f, 0.2f), new Vector2(0.88f, 0.8f));
            }
        }

        TextMeshProUGUI CreateText(string objectName, string text, float size, Vector2 min, Vector2 max)
        {
            var item = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            item.transform.SetParent(transform, false);
            var rect = (RectTransform)item.transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var label = item.GetComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSharedMaterial = glowMaterial;
            label.text = text;
            label.color = Color.white;
            label.fontSize = size;
            label.enableAutoSizing = true;
            label.fontSizeMin = 20f;
            label.fontSizeMax = size;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = true;
            label.raycastTarget = false;
            return label;
        }

        void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= durationSeconds)
            {
                group.alpha = 0f;
                IsComplete = true;
                gameObject.SetActive(false);
                return;
            }

            float fade = fadeSeconds <= 0f
                ? 1f
                : Mathf.Min(1f, elapsed / fadeSeconds, (durationSeconds - elapsed) / fadeSeconds);
            group.alpha = fade;
            int sentenceIndex = 0;
            while (sentenceIndex < sentenceEndTimes.Length - 1 && elapsed >= sentenceEndTimes[sentenceIndex])
            {
                sentenceIndex++;
            }
            body.text = sentences[sentenceIndex];
        }
    }
}
