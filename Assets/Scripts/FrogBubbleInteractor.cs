using UnityEngine;
using ThatGameJam.Dialogue;

namespace ThatGameJam
{
    [DisallowMultipleComponent]
    public class FrogBubbleInteractor : MonoBehaviour
    {
        [Header("气泡预制体")]
        [SerializeField] private GameObject frogBubblePrefab;
        [SerializeField] private GameObject playerBubblePrefab;

        [Header("交互")]
        [SerializeField] private float interactDistance = 2.5f;
        [SerializeField] private float autoCloseDistance = 4f;
        [SerializeField] private float bubbleScale = 0.45f;
        [SerializeField] private Vector2 playerViewport = new Vector2(0.26f, 0.16f);
        [SerializeField] private Vector2 frogViewport = new Vector2(0.74f, 0.16f);

        [Header("场景引用")]
        [SerializeField] private Transform player;
        [SerializeField] private Camera viewCamera;

        private GameObject frogBubble;
        private GameObject playerBubble;
        private Transform currentFrog;
        private bool showing;

        public bool IsShowing => showing;

        void Awake()
        {
            if (viewCamera == null) viewCamera = Camera.main;
            Hide();
        }

        void Update()
        {
            if (player == null) ResolvePlayer();
            if (player == null || viewCamera == null) return;

            if (showing && currentFrog != null)
            {
                float distance = Vector2.Distance(player.position, currentFrog.position);
                if (distance > autoCloseDistance) Hide();
            }

            InputManager input = InputManager.Instance;
            if (input == null || !input.isActiveAndEnabled) return;
            if (Time.timeScale <= 0f) return;
            if (!input.InteractPressed) return;

            if (showing) Hide();
            else TryShow();
        }

        void LateUpdate()
        {
            if (!showing || viewCamera == null) return;

            float depth = -viewCamera.transform.position.z;

            if (playerBubble != null)
            {
                playerBubble.transform.position = viewCamera.ViewportToWorldPoint(new Vector3(playerViewport.x, playerViewport.y, depth));
                playerBubble.transform.localScale = new Vector3(bubbleScale, bubbleScale, 1f);
            }

            if (frogBubble != null)
            {
                frogBubble.transform.position = viewCamera.ViewportToWorldPoint(new Vector3(frogViewport.x, frogViewport.y, depth));
                frogBubble.transform.localScale = new Vector3(bubbleScale, bubbleScale, 1f);
            }
        }

        void OnDisable()
        {
            Hide();
        }

        public void TryShow()
        {
            Transform frog = FindNearestFrog();
            if (frog == null) return;

            currentFrog = frog;

            if (frogBubble == null && frogBubblePrefab != null) frogBubble = Instantiate(frogBubblePrefab);
            if (playerBubble == null && playerBubblePrefab != null) playerBubble = Instantiate(playerBubblePrefab);

            if (frogBubble != null)
            {
                frogBubble.name = "FrogBubble";
                frogBubble.SetActive(true);
            }

            if (playerBubble != null)
            {
                playerBubble.name = "PlayerBubble";
                playerBubble.SetActive(true);
            }

            showing = true;
            LateUpdate();
        }

        public void Hide()
        {
            showing = false;
            currentFrog = null;

            if (frogBubble != null) frogBubble.SetActive(false);
            if (playerBubble != null) playerBubble.SetActive(false);
        }

        Transform FindNearestFrog()
        {
            Transform best = null;
            float bestDistance = interactDistance * interactDistance;

            FrogDialogue[] marked = FindObjectsOfType<FrogDialogue>();
            for (int i = 0; i < marked.Length; i++)
            {
                FrogDialogue frog = marked[i];
                if (frog == null || !frog.isActiveAndEnabled) continue;

                float distance = ((Vector2)(frog.transform.position - player.position)).sqrMagnitude;
                if (distance > bestDistance) continue;

                bestDistance = distance;
                best = frog.transform;
            }

            if (best != null) return best;

            foreach (Transform candidate in FindObjectsOfType<Transform>())
            {
                if (candidate == null || candidate == transform) continue;
                if (!candidate.gameObject.name.StartsWith("Frog")) continue;
                if (candidate.GetComponent<SpriteRenderer>() == null) continue;

                float distance = ((Vector2)(candidate.position - player.position)).sqrMagnitude;
                if (distance > bestDistance) continue;

                bestDistance = distance;
                best = candidate;
            }

            return best;
        }

        void ResolvePlayer()
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }
    }
}
