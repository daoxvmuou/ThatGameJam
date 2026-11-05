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

        [Tooltip("前景 Prefab，渲染在玩家之上，用于遮挡")]
        [SerializeField] private GameObject foreground;

        [Tooltip("背景音乐，循环播放")]
        [SerializeField] private AudioClip music;

        public GameObject Background => background;
        public GameObject Platform => platform;
        public GameObject Scenery => scenery;
        public GameObject Foreground => foreground;
        public AudioClip Music => music;
    }
}
