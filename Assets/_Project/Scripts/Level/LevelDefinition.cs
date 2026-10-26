using UnityEngine;

namespace ThatGameJam.Level
{
    /// <summary>
    /// 关卡配置：只包含「背景 / 平台 / 外景 / 音乐」四项，不含其他属性。
    /// 资源文件名即关卡名（用于查找与展示）。
    /// </summary>
    [CreateAssetMenu(fileName = "Level_New", menuName = "横版跳跃/关卡配置", order = 0)]
    public class LevelDefinition : ScriptableObject
    {
        [Tooltip("背景 Prefab，位于关卡最底层（sortingOrder 由 LevelController 统一设置）")]
        [SerializeField] private GameObject background;

        [Tooltip("平台 Prefab，需自带 Collider2D")]
        [SerializeField] private GameObject platform;

        [Tooltip("外景 Prefab，位于背景与平台之间的装饰层")]
        [SerializeField] private GameObject scenery;

        [Tooltip("关卡背景音乐，加载关卡时循环播放")]
        [SerializeField] private AudioClip music;

        public GameObject Background => background;
        public GameObject Platform => platform;
        public GameObject Scenery => scenery;
        public AudioClip Music => music;
    }
}
