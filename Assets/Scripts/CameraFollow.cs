using UnityEngine;

namespace ThatGameJam
{
    [DisallowMultipleComponent]
    public class CameraFollow : MonoBehaviour
    {
        [Header("目标")]
        [SerializeField] private Transform target;
        [SerializeField] private string targetTag = "Player";

        [Header("跟随")]
        [SerializeField] private Vector2 offset = new Vector2(0f, 1.5f);
        [SerializeField, Min(0f)] private float smoothTime = 0.2f;
        [SerializeField] private float lookAhead = 1.5f;

        [Header("纵向限制")]
        [SerializeField] private bool clampVertical = true;
        [SerializeField] private float minY = -2f;
        [SerializeField] private float maxY = 4f;

        private Vector3 velocity;
        private float currentLookAhead;

        void LateUpdate()
        {
            if (target == null) ResolveTarget();
            if (target == null) return;

            currentLookAhead = Mathf.MoveTowards(currentLookAhead, ResolveLookAhead(), 8f * Time.deltaTime);

            float desiredY = target.position.y + offset.y;
            if (clampVertical) desiredY = Mathf.Clamp(desiredY, minY, maxY);

            Vector3 desired = new Vector3(target.position.x + offset.x + currentLookAhead, desiredY, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
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
