using UnityEngine;
using ThatGameJam.Level;

namespace ThatGameJam
{
    [DisallowMultipleComponent]
    public class PlayerSpawner : MonoBehaviour
    {
        [Header("玩家")]
        [SerializeField] private GameObject playerPrefab;

        [Header("出生位置")]
        [SerializeField] private bool spawnAtLevelLeftBottom = true;
        [SerializeField] private float horizontalInset = 1.5f;
        [SerializeField] private float verticalInset = 2f;
        [SerializeField] private Vector2 spawnOffset = new Vector2(0f, 4f);

        [Header("掉落重生")]
        [SerializeField] private bool respawnOnFall = true;
        [SerializeField] private float killY = -12f;

        private Bounds levelBounds;

        public GameObject Player { get; private set; }

        void OnEnable()
        {
            LevelController.LevelLoaded += OnLevelLoaded;
            LevelController.LevelUnloaded += OnLevelUnloaded;
        }

        void OnDisable()
        {
            LevelController.LevelLoaded -= OnLevelLoaded;
            LevelController.LevelUnloaded -= OnLevelUnloaded;
        }

        void Start()
        {
            if (Player == null && LevelController.Instance != null && LevelController.Instance.IsLoaded)
            {
                RecalculateBounds();
                Spawn();
            }
        }

        void Update()
        {
            if (!respawnOnFall || Player == null) return;
            if (Player.transform.position.y < killY) Spawn();
        }

        void OnLevelLoaded(LevelDefinition definition)
        {
            RecalculateBounds();
            Spawn();
        }

        void OnLevelUnloaded(LevelDefinition definition)
        {
            Despawn();
        }

        public void Spawn()
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[玩家] 未配置玩家预制体。", this);
                return;
            }

            Despawn();

            Vector3 position = ResolveSpawnPosition();
            Player = Instantiate(playerPrefab, position, Quaternion.identity);
            Player.name = "Player";
        }

        Vector3 ResolveSpawnPosition()
        {
            if (spawnAtLevelLeftBottom)
            {
                return new Vector3(levelBounds.min.x + horizontalInset, levelBounds.max.y + verticalInset, 0f);
            }

            return transform.position + (Vector3)spawnOffset;
        }

        void RecalculateBounds()
        {
            LevelController controller = LevelController.Instance;
            if (controller == null || controller.Root == null)
            {
                levelBounds = new Bounds(transform.position, Vector3.zero);
                return;
            }

            Collider2D[] colliders = controller.Root.GetComponentsInChildren<Collider2D>();
            if (colliders.Length > 0)
            {
                Bounds bounds = colliders[0].bounds;
                for (int i = 1; i < colliders.Length; i++) bounds.Encapsulate(colliders[i].bounds);
                levelBounds = bounds;
                return;
            }

            Renderer[] renderers = controller.Root.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                levelBounds = bounds;
                return;
            }

            levelBounds = new Bounds(transform.position, Vector3.zero);
        }

        public void Despawn()
        {
            if (Player == null) return;
            Destroy(Player);
            Player = null;
        }
    }
}
