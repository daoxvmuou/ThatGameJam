using UnityEngine;
using UnityEngine.UI;
using ThatGameJam.Level;

namespace ThatGameJam
{
    [DisallowMultipleComponent]
    public class GameHudBinder : MonoBehaviour
    {
        [SerializeField] private bool bindRestart = true;
        [SerializeField] private bool bindHome = true;
        [SerializeField] private bool bindPause = true;
        [SerializeField] private bool bindMusic = true;

        private bool paused;

        void Start()
        {
            if (bindRestart) Bind("RestartButton", OnRestart);
            if (bindHome) Bind("HomeButton", OnHome);
            if (bindPause) Bind("PlayPauseButton", OnTogglePause);
            if (bindMusic) Bind("MusicToggleButton", OnToggleMusic);
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        void OnRestart()
        {
            if (paused) OnTogglePause();
            if (LevelController.Instance != null) LevelController.Instance.Restart();
        }

        void OnHome()
        {
            if (paused) OnTogglePause();
            if (LevelController.Instance != null) LevelController.Instance.Load(0);
        }

        void OnTogglePause()
        {
            paused = !paused;
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
        }

        void OnToggleMusic()
        {
            AudioVolumeSettings.ToggleMute();
        }

        void Bind(string buttonName, UnityEngine.Events.UnityAction action)
        {
            Button button = FindButton(transform, buttonName);
            if (button == null)
            {
                Debug.LogWarning($"[UI] 找不到按钮 {buttonName}", this);
                return;
            }

            button.onClick.AddListener(action);
        }

        static Button FindButton(Transform root, string name)
        {
            if (root.name == name)
            {
                Button direct = root.GetComponent<Button>();
                if (direct != null) return direct;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Button found = FindButton(root.GetChild(i), name);
                if (found != null) return found;
            }

            return null;
        }
    }
}
