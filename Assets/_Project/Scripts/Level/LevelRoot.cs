using UnityEngine;

namespace ThatGameJam.Level
{
    public class LevelRoot : MonoBehaviour
    {
        [SerializeField] private GameObject background;
        [SerializeField] private GameObject platform;
        [SerializeField] private GameObject scenery;

        public LevelDefinition Definition { get; private set; }
        public GameObject Background => background;
        public GameObject Platform => platform;
        public GameObject Scenery => scenery;

        internal void Bind(LevelDefinition definition, GameObject backgroundPart, GameObject platformPart, GameObject sceneryPart)
        {
            Definition = definition;
            background = backgroundPart;
            platform = platformPart;
            scenery = sceneryPart;
        }
    }
}
