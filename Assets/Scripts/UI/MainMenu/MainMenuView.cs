using System;
using UnityEngine;
using UnityEngine.UI;

namespace BrokenAnchor.UI
{
    public class MainMenuView : MonoBehaviour
    {
        public static MainMenuView Create(Transform parent)
        {
            var root = UIBuilder.CreateRect(parent, "MainMenuView", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var view = root.gameObject.AddComponent<MainMenuView>();
            UIBuilder.CreatePanel(root, "Background", new Color(0.03f, 0.09f, 0.12f, 1f));

            var title = UIBuilder.CreateText(root, "Title", "断锚求生", 54, new Color(0.94f, 0.98f, 0.92f), TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0.15f, 0.62f);
            title.rectTransform.anchorMax = new Vector2(0.85f, 0.84f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            var subtitle = UIBuilder.CreateText(root, "Subtitle", "拼出一只临时船锚，在急流里拖住船。", 22, new Color(0.75f, 0.86f, 0.84f), TextAnchor.MiddleCenter);
            subtitle.rectTransform.anchorMin = new Vector2(0.15f, 0.54f);
            subtitle.rectTransform.anchorMax = new Vector2(0.85f, 0.62f);
            subtitle.rectTransform.offsetMin = Vector2.zero;
            subtitle.rectTransform.offsetMax = Vector2.zero;

            view.startButton = CreateMenuButton(root, "StartButton", "开始", 0);
            view.settingsButton = CreateMenuButton(root, "SettingsButton", "设置", 1);
            view.quitButton = CreateMenuButton(root, "QuitButton", "退出", 2);

            return view;
        }

        private Button startButton;
        private Button settingsButton;
        private Button quitButton;

        public void Initialize(Action onStart, Action onSettings, Action onQuit)
        {
            startButton.onClick.AddListener(() => onStart());
            settingsButton.onClick.AddListener(() => onSettings());
            quitButton.onClick.AddListener(() => onQuit());
        }

        private static Button CreateMenuButton(Transform root, string name, string text, int index)
        {
            var button = UIBuilder.CreateButton(root, name, text, null);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.39f, 0.42f - index * 0.1f);
            rect.anchorMax = new Vector2(0.61f, 0.49f - index * 0.1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return button;
        }
    }
}
