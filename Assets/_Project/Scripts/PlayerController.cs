using UnityEngine;

namespace ThatGameJam
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField, Min(0f)] private float jumpSpeed = 8f;

        private Rigidbody2D body;
        private ContactFilter2D groundFilter;
        private float horizontal;
        private bool jumpRequested;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.constraints |= RigidbodyConstraints2D.FreezeRotation;

            // 只将朝上的非触发器接触视为地面，排除墙壁和天花板。
            groundFilter = new ContactFilter2D
            {
                useTriggers = false,
                useNormalAngle = true,
                minNormalAngle = 45f,
                maxNormalAngle = 135f
            };
        }

        void Update()
        {
            InputManager input = InputManager.Instance;
            if (input == null || !input.isActiveAndEnabled)
            {
                horizontal = 0f;
                jumpRequested = false;
                return;
            }

            horizontal = input.Horizontal;
            // 保留按键，直到下一个物理帧处理，避免漏掉短按。
            jumpRequested |= input.JumpPressed && Time.timeScale > 0f;
        }

        void FixedUpdate()
        {
            Vector2 velocity = body.velocity;
            velocity.x = horizontal * moveSpeed;

            if (jumpRequested && velocity.y <= 0.01f && body.IsTouching(groundFilter))
            {
                velocity.y = jumpSpeed;
            }

            body.velocity = velocity;
            jumpRequested = false;
        }

        void OnDisable()
        {
            horizontal = 0f;
            jumpRequested = false;
            if (body != null) body.velocity = new Vector2(0f, body.velocity.y);
        }
    }
}
