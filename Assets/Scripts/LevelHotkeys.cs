using UnityEngine;
using ThatGameJam.Level;

namespace ThatGameJam
{
    [DisallowMultipleComponent]
    public class LevelHotkeys : MonoBehaviour
    {
        [SerializeField] private KeyCode[] levelKeys =
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
            KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7
        };

        void Update()
        {
            LevelController controller = LevelController.Instance;
            if (controller == null) return;

            for (int i = 0; i < levelKeys.Length; i++)
            {
                if (Input.GetKeyDown(levelKeys[i]))
                {
                    if (i < controller.LevelCount) controller.Load(i);
                    else Debug.LogWarning($"[关卡] 序号 {i} 超出关卡数量 {controller.LevelCount}");
                    return;
                }
            }
        }
    }
}
