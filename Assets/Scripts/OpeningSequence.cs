using System.Collections;
using UnityEngine;

namespace ThatGameJam
{
    public class OpeningSequence : MonoBehaviour
    {
        [SerializeField] private OpeningNarration[] narrations;

        private OpeningNarration current;
        private float previousTimeScale;
        private bool playing;

        void Start()
        {
            if (narrations == null || narrations.Length == 0)
            {
                Debug.LogError("[Opening] No narration prefabs are configured.", this);
                enabled = false;
                return;
            }

            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            playing = true;
            StartCoroutine(PlaySequence());
        }

        IEnumerator PlaySequence()
        {
            foreach (OpeningNarration narration in narrations)
            {
                if (narration == null) continue;

                current = Instantiate(narration);
                while (current != null && !current.IsComplete) yield return null;

                if (current != null) Destroy(current.gameObject);
                current = null;
            }

            RestoreTimeScale();
        }

        void OnDisable()
        {
            if (playing)
            {
                StopAllCoroutines();
                if (current != null) Destroy(current.gameObject);
                current = null;
                RestoreTimeScale();
            }
        }

        void RestoreTimeScale()
        {
            Time.timeScale = previousTimeScale;
            playing = false;
        }
    }
}
