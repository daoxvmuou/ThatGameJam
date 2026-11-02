using UnityEngine;

namespace ThatGameJam.Dialogue
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public class FrogDialogue : MonoBehaviour
    {
        [SerializeField, Range(1, 21)] private int dialogueNumber = 1;

        public int DialogueNumber => dialogueNumber;
        public Sprite Sprite => GetComponent<SpriteRenderer>().sprite;
    }
}
