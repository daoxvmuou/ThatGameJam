using UnityEngine;
using UnityEngine.UI;
using ThatGameJam.Level;

namespace ThatGameJam
{
    [DisallowMultipleComponent]
    public sealed class UIEndPanel : MonoBehaviour
    {
        void Awake()
        {
            Bind("RestartButton", OnRestart);
            Bind("QuitButton", OnQuit);

            Button resumeButton = FindButton(transform, "ResumeButton");
            if (resumeButton != null) resumeButton.onClick.AddListener(OnResume);
            Button homeButton = FindButton(transform, "HomeButton");
            if (homeButton != null) homeButton.onClick.AddListener(OnHome);
        }

        void OnResume()
        {
            IntroFlow introFlow = FindObjectOfType<IntroFlow>();
            if (introFlow == null)
            {
                Debug.LogWarning("[结局] 找不到 IntroFlow，无法返回标题页。", this);
                return;
            }

            Destroy(gameObject);
            introFlow.ReturnToTitleScreen();
        }

        void OnRestart()
        {
            LevelController levelController = LevelController.Instance;
            if (levelController == null)
            {
                Debug.LogWarning("[结局] 找不到 LevelController，无法重开关卡。", this);
                return;
            }

            Destroy(gameObject);
            levelController.Load(0);
        }

        void OnHome()
        {
            IntroFlow introFlow = FindObjectOfType<IntroFlow>();
            if (introFlow == null)
            {
                Debug.LogWarning("[结局] 找不到 IntroFlow，无法播放开场动画。", this);
                return;
            }

            Destroy(gameObject);
            introFlow.PlayOpeningAnimation();
        }

        void OnQuit()
        {
            Application.Quit();
        }

        void Bind(string buttonName, UnityEngine.Events.UnityAction action)
        {
            Button button = FindButton(transform, buttonName);
            if (button == null)
            {
                Debug.LogWarning($"[结局] 找不到按钮 {buttonName}", this);
                return;
            }

            button.onClick.AddListener(action);
        }

        static Button FindButton(Transform root, string buttonName)
        {
            if (root.name == buttonName)
            {
                Button button = root.GetComponent<Button>();
                if (button != null) return button;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Button button = FindButton(root.GetChild(i), buttonName);
                if (button != null) return button;
            }

            return null;
        }
    }
}
