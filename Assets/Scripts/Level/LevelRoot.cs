using System.Collections.Generic;
using UnityEngine;

namespace ThatGameJam.Level
{
    public class LevelRoot : MonoBehaviour
    {
        [SerializeField] private GameObject background;
        [SerializeField] private GameObject platform;
        [SerializeField] private GameObject scenery;
        [SerializeField] private List<GameObject> foregrounds = new List<GameObject>();

        public LevelDefinition Definition { get; private set; }
        public GameObject Background => background;
        public GameObject Platform => platform;
        public GameObject Scenery => scenery;
        public IReadOnlyList<GameObject> Foregrounds => foregrounds;

        internal void Bind(LevelDefinition definition, GameObject backgroundPart, GameObject platformPart, GameObject sceneryPart, IList<GameObject> foregroundParts)
        {
            Definition = definition;
            background = backgroundPart;
            platform = platformPart;
            scenery = sceneryPart;

            foregrounds.Clear();
            if (foregroundParts != null) foregrounds.AddRange(foregroundParts);
        }
    }
}
