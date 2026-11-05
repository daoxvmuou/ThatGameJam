using UnityEngine;

namespace ThatGameJam
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Animator))]
    public class PlayerController : MonoBehaviour
    {
        [Header("移动")]
        [SerializeField, Min(0f)] private float moveSpeed = 6f;
        [SerializeField, Min(0f)] private float jumpSpeed = 12f;
        [SerializeField, Min(0f)] private float fallMultiplier = 1.8f;
        [SerializeField, Min(0f)] private float jumpReleaseMultiplier = 2.2f;

        [Header("手感")]
        [SerializeField, Min(0f)] private float coyoteTime = 0.1f;
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;
        [SerializeField, Range(0f, 1f)] private float airControl = 0.85f;

        [Header("表现")]
        [SerializeField] private bool flipSprite = true;

        private static readonly int IsMove = Animator.StringToHash("IsMove");

        private Rigidbody2D body;
        private Animator animator;
        private SpriteRenderer spriteRenderer;
        private ContactFilter2D groundFilter;
        private float horizontal;
        private float coyoteCounter;
        private float bufferCounter;
        private bool jumpHeld;
        private bool controlEnabled = true;

        public bool IsGrounded { get; private set; }
        public bool ControlEnabled => controlEnabled;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            body.constraints |= RigidbodyConstraints2D.FreezeRotation;

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

            if (!controlEnabled || input == null || !input.isActiveAndEnabled)
            {
                horizontal = 0f;
                jumpHeld = false;
                bufferCounter = 0f;
                if (animator != null) animator.SetBool(IsMove, false);
                return;
            }

            horizontal = input.Horizontal;
            jumpHeld = input.JumpHeld;

            if (input.JumpPressed && Time.timeScale > 0f) bufferCounter = jumpBufferTime;
            else bufferCounter -= Time.deltaTime;

            if (animator != null) animator.SetBool(IsMove, Mathf.Abs(horizontal) > 0.01f);

            if (flipSprite && spriteRenderer != null && Mathf.Abs(horizontal) > 0.01f)
            {
                spriteRenderer.flipX = horizontal < 0f;
            }
        }

        void FixedUpdate()
        {
            Vector2 velocity = body.velocity;
            IsGrounded = body.IsTouching(groundFilter);

            float control = IsGrounded ? 1f : airControl;
            velocity.x = horizontal * moveSpeed * control;

            if (IsGrounded) coyoteCounter = coyoteTime;
            else coyoteCounter -= Time.fixedDeltaTime;

            if (bufferCounter > 0f && coyoteCounter > 0f)
            {
                velocity.y = jumpSpeed;
                bufferCounter = 0f;
                coyoteCounter = 0f;
            }
            else if (velocity.y < 0f)
            {
                velocity.y += Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
            }
            else if (velocity.y > 0f && !jumpHeld)
            {
                velocity.y += Physics2D.gravity.y * (jumpReleaseMultiplier - 1f) * Time.fixedDeltaTime;
            }

            body.velocity = velocity;
        }

        public void SetControlEnabled(bool enabled)
        {
            controlEnabled = enabled;
            if (!enabled)
            {
                horizontal = 0f;
                jumpHeld = false;
                bufferCounter = 0f;
                if (animator != null) animator.SetBool(IsMove, false);
                if (body != null) body.velocity = new Vector2(0f, body.velocity.y);
            }
        }

        public void Teleport(Vector2 position)
        {
            if (body == null) return;
            body.position = position;
            body.velocity = Vector2.zero;
            transform.position = position;
            coyoteCounter = 0f;
            bufferCounter = 0f;
        }

        void OnDisable()
        {
            horizontal = 0f;
            jumpHeld = false;
            bufferCounter = 0f;
            if (body != null) body.velocity = new Vector2(0f, body.velocity.y);
            if (animator != null) animator.SetBool(IsMove, false);
        }
    }
}
