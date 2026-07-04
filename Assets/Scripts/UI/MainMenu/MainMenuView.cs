using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BrokenAnchor.UI
{
    public class MainMenuView : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Animation startAnimationPlayer;
        [SerializeField] private AnimationClip startanimation;

        private Action onStart;
        private Action onSettings;
        private Action onQuit;
        private bool isStarting;

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

            view.startButton = CreateMenuButton(root, "StartButton", "开始游戏", 0);
            view.settingsButton = CreateMenuButton(root, "SettingsButton", "设置", 1);
            view.quitButton = CreateMenuButton(root, "QuitButton", "退出", 2);
            view.BindGeneratedButtonClickEvents();

            return view;
        }

        public void Initialize(Action onStart, Action onSettings, Action onQuit)
        {
            this.onStart = onStart;
            this.onSettings = onSettings;
            this.onQuit = onQuit;
        }

        public void OnStartButtonClicked()
        {
            if (isStarting)
            {
                return;
            }

            StartCoroutine(PlayStartAnimationThenStart());
        }

        public void OnSettingsButtonClicked()
        {
            onSettings?.Invoke();
        }

        public void OnQuitButtonClicked()
        {
            onQuit?.Invoke();
        }

        private IEnumerator PlayStartAnimationThenStart()
        {
            isStarting = true;
            SetButtonsInteractable(false);

            if (startAnimationPlayer != null && startanimation != null)
            {
                startAnimationPlayer.Stop();
                startAnimationPlayer.clip = startanimation;
                if (startAnimationPlayer.GetClip(startanimation.name) == null)
                {
                    startAnimationPlayer.AddClip(startanimation, startanimation.name);
                }

                startAnimationPlayer.Play(startanimation.name);
                yield return new WaitForSeconds(startanimation.length);
            }

            onStart?.Invoke();
            SetButtonsInteractable(true);
            isStarting = false;
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (startButton != null)
            {
                startButton.interactable = interactable;
            }

            if (settingsButton != null)
            {
                settingsButton.interactable = interactable;
            }

            if (quitButton != null)
            {
                quitButton.interactable = interactable;
            }
        }

        private void BindGeneratedButtonClickEvents()
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            quitButton.onClick.AddListener(OnQuitButtonClicked);
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
