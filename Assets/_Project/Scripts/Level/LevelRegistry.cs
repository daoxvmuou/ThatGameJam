using System.Collections.Generic;
using UnityEngine;

namespace ThatGameJam.Level
{
    /// <summary>
    /// 关卡总表：记录关卡顺序，供 LevelController 按序号加载。
    /// 新增关卡时只需把 LevelDefinition 拖进列表。
    /// </summary>
    [CreateAssetMenu(fileName = "LevelRegistry", menuName = "横版跳跃/关卡总表", order = 1)]
    public class LevelRegistry : ScriptableObject
    {
        [SerializeField] private List<LevelDefinition> levels = new List<LevelDefinition>();

        public IReadOnlyList<LevelDefinition> Levels => levels;
        public int Count => levels.Count;

        public LevelDefinition Get(int index)
        {
            if (index < 0 || index >= levels.Count) return null;
            return levels[index];
        }

        /// <summary>按资源名查找关卡（LevelDefinition 的资源名即关卡名）。</summary>
        public LevelDefinition GetByName(string levelName)
        {
            if (string.IsNullOrEmpty(levelName)) return null;
            for (int i = 0; i < levels.Count; i++)
            {
                LevelDefinition level = levels[i];
                if (level != null && level.name == levelName) return level;
            }
            return null;
        }

        public int IndexOf(LevelDefinition level)
        {
            if (level == null) return -1;
            return levels.IndexOf(level);
        }
    }
}
