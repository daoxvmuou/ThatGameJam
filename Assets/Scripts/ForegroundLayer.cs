using UnityEngine;

namespace ThatGameJam
{
    [DisallowMultipleComponent]
    public class ForegroundLayer : MonoBehaviour
    {
        [SerializeField] private int sortingOrder = 20;
        [SerializeField] private bool applyToChildren = true;

        void Awake()
        {
            Apply();
        }

        void OnValidate()
        {
            Apply();
        }

        public void Apply()
        {
            if (applyToChildren)
            {
                SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
                for (int i = 0; i < renderers.Length; i++) renderers[i].sortingOrder = sortingOrder;
                return;
            }

            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.sortingOrder = sortingOrder;
        }
    }
}
