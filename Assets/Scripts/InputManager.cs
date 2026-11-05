using UnityEngine;

namespace ThatGameJam
{
    [DisallowMultipleComponent]
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        public float Horizontal =>
            (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);

        public bool JumpPressed => Input.GetKeyDown(KeyCode.W);
        public bool InteractPressed => Input.GetKeyDown(KeyCode.E);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            if (Instance == null)
            {
                new GameObject(nameof(InputManager)).AddComponent<InputManager>();
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
