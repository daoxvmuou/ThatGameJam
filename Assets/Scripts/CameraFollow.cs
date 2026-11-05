using UnityEngine;
using ThatGameJam.Level;

namespace ThatGameJam
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [Header("目标")]
        [SerializeField] private Transform target;
        [SerializeField] private string targetTag = "Player";

        [Header("跟随")]
        [SerializeField] private Vector2 offset = new Vector2(0f, 1.5f);
        [SerializeField, Min(0f)] private float smoothTime = 0.2f;
        [SerializeField] private float lookAhead = 1.5f;

        [Header("边缘缩放")]
        [SerializeField] private bool zoomNearEdges = true;
        [SerializeField] private float edgeOrthoSize = 3.8f;
        [SerializeField] private float edgeZoomDistance = 4f;
        [SerializeField, Min(0f)] private float zoomSmoothTime = 0.35f;

        [Header("地图边界")]
        [SerializeField] private bool useLevelBounds = true;
        [SerializeField] private float boundsPadding = 0f;

        private Camera cam;
        private Bounds levelBounds;
        private bool hasBounds;
        private float baseOrthoSize = 5f;
        private Vector3 velocity;
        private float zoomVelocity;
        private float currentLookAhead;

        void Awake()
        {
            cam = GetComponent<Camera>();
            baseOrthoSize = cam.orthographicSize;
        }

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

        void LateUpdate()
        {
            if (cam == null) return;
            if (target == null) ResolveTarget();
            if (target == null) return;

            float targetSize = ResolveOrthoSize();
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetSize, ref zoomVelocity, zoomSmoothTime);

            currentLookAhead = Mathf.MoveTowards(currentLookAhead, ResolveLookAhead(), 8f * Time.deltaTime);

            Vector3 desired = new Vector3(
                target.position.x + offset.x + currentLookAhead,
                target.position.y + offset.y,
                transform.position.z);

            if (useLevelBounds && hasBounds) desired = ClampToBounds(desired);

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);

            if (useLevelBounds && hasBounds) transform.position = ClampToBounds(transform.position);
        }

        void OnLevelLoaded(LevelDefinition definition)
        {
            RecalculateBounds();
        }

        float ResolveOrthoSize()
        {
            if (!useLevelBounds || !hasBounds) return baseOrthoSize;

            float aspect = Mathf.Max(0.1f, cam.aspect);
            float fitByWidth = levelBounds.size.x / (2f * aspect);
            float fitByHeight = levelBounds.size.y / 2f;
            float size = Mathf.Min(baseOrthoSize, Mathf.Min(fitByWidth, fitByHeight));

            if (!zoomNearEdges || edgeZoomDistance <= 0f) return size;

            float halfWidth = size * aspect;
            float rightLimit = levelBounds.max.x - halfWidth;
            float leftLimit = levelBounds.min.x + halfWidth;
            float overflow = Mathf.Max(0f, target.position.x - rightLimit) + Mathf.Max(0f, leftLimit - target.position.x);
            float t = Mathf.Clamp01(overflow / edgeZoomDistance);

            return Mathf.Lerp(size, Mathf.Min(edgeOrthoSize, size), t);
        }

        Vector3 ClampToBounds(Vector3 position)
        {
            float halfHeight = cam.orthographicSize - boundsPadding;
            float halfWidth = halfHeight * Mathf.Max(0.1f, cam.aspect);

            float minX = levelBounds.min.x + halfWidth;
            float maxX = levelBounds.max.x - halfWidth;
            float minY = levelBounds.min.y + halfHeight;
            float maxY = levelBounds.max.y - halfHeight;

            position.x = minX <= maxX ? Mathf.Clamp(position.x, minX, maxX) : levelBounds.center.x;
            position.y = minY <= maxY ? Mathf.Clamp(position.y, minY, maxY) : levelBounds.center.y;

            return position;
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

        float ResolveLookAhead()
        {
            Rigidbody2D body = target.GetComponent<Rigidbody2D>();
            if (body == null) return 0f;
            return Mathf.Clamp(body.velocity.x / 6f, -1f, 1f) * lookAhead;
        }

        void ResolveTarget()
        {
            if (string.IsNullOrEmpty(targetTag)) return;
            GameObject found = GameObject.FindGameObjectWithTag(targetTag);
            if (found != null) target = found.transform;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
