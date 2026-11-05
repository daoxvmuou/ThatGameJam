using UnityEngine;

namespace ThatGameJam.Level
{
    public class LevelRoot : MonoBehaviour
    {
        [SerializeField] private GameObject background;
        [SerializeField] private GameObject platform;
        [SerializeField] private GameObject scenery;
        [SerializeField] private GameObject foreground;

        public LevelDefinition Definition { get; private set; }
        public GameObject Background => background;
        public GameObject Platform => platform;
        public GameObject Scenery => scenery;
        public GameObject Foreground => foreground;

        internal void Bind(LevelDefinition definition, GameObject backgroundPart, GameObject platformPart, GameObject sceneryPart, GameObject foregroundPart)
        {
            Definition = definition;
            background = backgroundPart;
            platform = platformPart;
            scenery = sceneryPart;
            foreground = foregroundPart;
        }
    }
}
