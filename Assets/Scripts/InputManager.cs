using UnityEngine;

namespace ThatGameJam
{
    [DisallowMultipleComponent]
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        public bool MovementUsed { get; private set; }
        public bool JumpUsed { get; private set; }

        public float Horizontal =>
            (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) -
            (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);

        public bool JumpPressed =>
            Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow);

        public bool JumpHeld =>
            Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.UpArrow);

        public bool InteractPressed => Input.GetKeyDown(KeyCode.E);

        public void RegisterMovement()
        {
            MovementUsed = true;
        }

        public void RegisterJump()
        {
            JumpUsed = true;
        }

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
