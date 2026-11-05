using System.Collections.Generic;
using UnityEngine;

namespace ThatGameJam.Level
{
    [CreateAssetMenu(fileName = "Level_New", menuName = "横版跳跃/关卡配置", order = 0)]
    public class LevelDefinition : ScriptableObject
    {
        [Tooltip("背景 Prefab，位于关卡最底层")]
        [SerializeField] private GameObject background;

        [Tooltip("平台 Prefab，需自带 Collider2D")]
        [SerializeField] private GameObject platform;

        [Tooltip("外景 Prefab，位于背景与平台之间")]
        [SerializeField] private GameObject scenery;

        [Tooltip("前景 Prefab 列表，渲染在玩家之上，列表越靠后越靠前")]
        [SerializeField] private List<GameObject> foregrounds = new List<GameObject>();

        [Tooltip("背景音乐，循环播放")]
        [SerializeField] private AudioClip music;

        public GameObject Background => background;
        public GameObject Platform => platform;
        public GameObject Scenery => scenery;
        public IReadOnlyList<GameObject> Foregrounds => foregrounds;
        public AudioClip Music => music;

        public int ForegroundCount => foregrounds != null ? foregrounds.Count : 0;
    }
}
