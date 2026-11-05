using UnityEngine;
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
