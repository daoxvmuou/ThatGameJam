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

        [Header("地面检测")]
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private float groundCheckDistance = 0.15f;
        [SerializeField, Range(0f, 1f)] private float minGroundNormalY = 0.45f;

        private Rigidbody2D body;
        private Animator animator;
        private SpriteRenderer spriteRenderer;
        private Collider2D bodyCollider;
        private readonly Collider2D[] groundHits = new Collider2D[8];
        private readonly RaycastHit2D[] groundCastHits = new RaycastHit2D[8];
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
            bodyCollider = GetComponent<Collider2D>();
            body.constraints |= RigidbodyConstraints2D.FreezeRotation;
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
            IsGrounded = CheckGrounded();

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

        bool CheckGrounded()
        {
            if (bodyCollider == null) return false;

            Bounds bounds = bodyCollider.bounds;
            if (GroundCast(bounds)) return true;
            return GroundOverlap(bounds);
        }

        bool GroundCast(Bounds bounds)
        {
            float width = Mathf.Max(0.05f, bounds.size.x * 0.85f);
            float height = Mathf.Max(0.05f, bounds.size.y * 0.25f);
            Vector2 origin = new Vector2(bounds.center.x, bounds.min.y + height * 0.5f + 0.01f);

            int count = Physics2D.BoxCastNonAlloc(origin, new Vector2(width, height), 0f, Vector2.down, groundCastHits, groundCheckDistance, groundLayers);
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = groundCastHits[i];
                if (!IsGroundCollider(hit.collider)) continue;
                if (hit.normal.y >= minGroundNormalY) return true;
                if (hit.distance <= 0.001f && hit.point.y <= bounds.min.y + 0.05f) return true;
            }

            return false;
        }

        bool GroundOverlap(Bounds bounds)
        {
            Vector2 center = new Vector2(bounds.center.x, bounds.min.y - groundCheckDistance * 0.5f);
            Vector2 size = new Vector2(Mathf.Max(0.05f, bounds.size.x * 0.9f), groundCheckDistance);

            int count = Physics2D.OverlapBoxNonAlloc(center, size, 0f, groundHits, groundLayers);
            for (int i = 0; i < count; i++)
            {
                if (IsGroundCollider(groundHits[i])) return true;
            }

            return false;
        }

        bool IsGroundCollider(Collider2D candidate)
        {
            if (candidate == null || candidate == bodyCollider || candidate.isTrigger) return false;
            if (candidate.attachedRigidbody != null && candidate.attachedRigidbody == body) return false;
            return true;
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
