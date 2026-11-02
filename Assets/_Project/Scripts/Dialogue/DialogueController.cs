using System;
using TMPro;
using UnityEngine;

namespace ThatGameJam.Dialogue
{
    [DisallowMultipleComponent]
    public class DialogueController : MonoBehaviour
    {
        [Serializable]
        private class Script
        {
            public Conversation[] conversations;
        }

        [Serializable]
        private class Conversation
        {
            public int number;
            public string frogName;
            public Line[] lines;
        }

        [Serializable]
        private class Line
        {
            public bool player;
            public string text;
        }

        [Header("剧本与角色")]
        [SerializeField] private TextAsset script;
        [SerializeField] private PlayerController player;
        [SerializeField, Min(0.1f)] private float interactionDistance = 2.5f;

        [Header("现有气泡")]
        [SerializeField] private GameObject playerBubble;
        [SerializeField] private TMP_Text playerText;
        [SerializeField] private GameObject frogBubble;
        [SerializeField] private TMP_Text frogText;
        [SerializeField] private SpriteRenderer frogPortrait;
        [SerializeField] private Camera viewCamera;

        private Script dialogueScript;
        private FrogDialogue currentFrog;
        private Conversation currentConversation;
        private int nextLine;
        private bool isTalking;
        private bool playerWasEnabled;
        private float bubbleWidth;

        void Awake()
        {
            HideBubbles();
            if (script == null || player == null || playerBubble == null || playerText == null ||
                frogBubble == null || frogText == null || frogPortrait == null || viewCamera == null)
            {
                Debug.LogError("[对话] 请配置剧本、Player、两个气泡及其 Text、青蛙头像和摄像机。", this);
                enabled = false;
                return;
            }

            try
            {
                dialogueScript = JsonUtility.FromJson<Script>(script.text);
            }
            catch (ArgumentException exception)
            {
                Debug.LogError("[对话] 剧本格式错误：" + exception.Message, this);
            }

            if (dialogueScript == null || dialogueScript.conversations == null)
            {
                Debug.LogError("[对话] 剧本中没有对话数据。", this);
                enabled = false;
                return;
            }

            bubbleWidth = Mathf.Max(playerBubble.GetComponent<SpriteRenderer>().sprite.bounds.size.x,
                frogBubble.GetComponent<SpriteRenderer>().sprite.bounds.size.x);
        }

        void Update()
        {
            if (isTalking && (currentFrog == null || !currentFrog.isActiveAndEnabled || player == null))
            {
                EndConversation();
            }

            InputManager input = InputManager.Instance;
            if (Time.timeScale <= 0f || input == null || !input.isActiveAndEnabled || !input.InteractPressed)
                return;

            if (isTalking) ShowNextLine();
            else TryStartConversation();
        }

        void LateUpdate()
        {
            if (!isTalking || viewCamera == null) return;

            // 两个世界空间气泡并排放在视野底部，窄屏时一起缩小。
            float width = viewCamera.orthographicSize * viewCamera.aspect * 2f;
            float scale = Mathf.Min(0.5f, width * 0.44f / bubbleWidth);
            float depth = -viewCamera.transform.position.z;
            playerBubble.transform.position = viewCamera.ViewportToWorldPoint(new Vector3(0.26f, 0.16f, depth));
            frogBubble.transform.position = viewCamera.ViewportToWorldPoint(new Vector3(0.74f, 0.16f, depth));
            playerBubble.transform.localScale = new Vector3(scale, scale, 1f);
            frogBubble.transform.localScale = new Vector3(scale, scale, 1f);
        }

        void TryStartConversation()
        {
            if (player == null || !player.isActiveAndEnabled) return;

            FrogDialogue nearest = null;
            float nearestDistance = interactionDistance * interactionDistance;
            foreach (FrogDialogue frog in FindObjectsOfType<FrogDialogue>())
            {
                if (!frog.isActiveAndEnabled || frog.Sprite == null) continue;
                float distance = ((Vector2)(frog.transform.position - player.transform.position)).sqrMagnitude;
                if (distance <= nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = frog;
                }
            }

            if (nearest == null) return;
            Conversation conversation = Array.Find(dialogueScript.conversations,
                item => item.number == nearest.DialogueNumber);
            if (conversation == null || conversation.lines == null || conversation.lines.Length == 0) return;

            currentFrog = nearest;
            currentConversation = conversation;
            nextLine = 0;
            isTalking = true;
            playerWasEnabled = player.enabled;
            player.enabled = false;
            playerText.text = string.Empty;
            frogText.text = string.Empty;
            frogPortrait.sprite = currentFrog.Sprite;
            Vector2 size = frogPortrait.sprite.bounds.size;
            float portraitScale = Mathf.Min(2.2f / size.x, 2.4f / size.y);
            frogPortrait.transform.localScale = new Vector3(portraitScale, portraitScale, 1f);
            playerBubble.SetActive(true);
            frogBubble.SetActive(true);
            ShowNextLine();
            LateUpdate();
        }

        void ShowNextLine()
        {
            if (nextLine >= currentConversation.lines.Length)
            {
                EndConversation();
                return;
            }

            Line line = currentConversation.lines[nextLine++];
            // 只更新发言者，另一方保留上一句，两个气泡始终同时显示。
            if (line.player) playerText.text = line.text;
            else frogText.text = line.text;
        }

        void EndConversation()
        {
            if (isTalking && player != null) player.enabled = playerWasEnabled;
            isTalking = false;
            currentFrog = null;
            currentConversation = null;
            HideBubbles();
        }

        void HideBubbles()
        {
            if (playerBubble != null) playerBubble.SetActive(false);
            if (frogBubble != null) frogBubble.SetActive(false);
        }

        void OnDisable()
        {
            EndConversation();
        }
    }
}
