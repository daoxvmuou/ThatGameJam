using UnityEngine;

namespace ThatGameJam.Level
{
    /// <summary>
    /// 关卡实例根节点：由 LevelController 在加载时创建，持有三个部件实例的引用。
    /// 其他模块（玩家、相机等）需要定位关卡内容时从这里取，不要用 Find。
    /// </summary>
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
