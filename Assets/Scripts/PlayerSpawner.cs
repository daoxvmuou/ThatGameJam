using UnityEngine;
using ThatGameJam.Level;

namespace ThatGameJam
{
    [DisallowMultipleComponent]
    public class PlayerSpawner : MonoBehaviour
    {
        [Header("玩家")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Vector2 spawnOffset = new Vector2(0f, 4f);

        [Header("掉落重生")]
        [SerializeField] private bool respawnOnFall = true;
        [SerializeField] private float killY = -12f;

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

            Vector3 position = transform.position + (Vector3)spawnOffset;
            Player = Instantiate(playerPrefab, position, Quaternion.identity);
            Player.name = "Player";
        }

        public void Despawn()
        {
            if (Player == null) return;
            Destroy(Player);
            Player = null;
        }
    }
}
