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
        [SerializeField] private Button skipStartAnimationButton;
        [SerializeField] private Animation startAnimationPlayer;
        [SerializeField] private AnimationClip startanimation;
        [SerializeField] private float startButtonFlyEndTime = 0.66f;
        [SerializeField] private float comicStartTime = 0.68f;

        private Action onStart;
        private Action onSettings;
        private Action onQuit;
        private Action startAnimationCompleteCallback;
        private bool isStarting;
        private Coroutine startRoutine;
        private bool buttonClickEventsBound;

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
            view.skipStartAnimationButton = CreateSkipStartAnimationButton(root);
            view.BindGeneratedButtonClickEvents();
            view.SetSkipStartAnimationButtonVisible(false);

            return view;
        }

        public void Initialize(Action onStart, Action onSettings, Action onQuit)
        {
            ResolveReferences();
            this.onStart = onStart;
            this.onSettings = onSettings;
            this.onQuit = onQuit;
            BindGeneratedButtonClickEvents();
            SetSkipStartAnimationButtonVisible(false);
        }

        public void OnStartButtonClicked()
        {
            if (isStarting)
            {
                return;
            }

            onStart?.Invoke();
        }

        public void PlayStartAnimationThen(Action onComplete)
        {
            PlayIntroComicThen(onComplete);
        }

        public void PlayStartButtonFlyThen(Action onComplete)
        {
            if (isStarting)
            {
                return;
            }

            startAnimationCompleteCallback = onComplete;
            startRoutine = StartCoroutine(RunStartButtonFlyThen(onComplete));
        }

        public void PlayIntroComicThen(Action onComplete)
        {
            if (isStarting)
            {
                return;
            }

            startAnimationCompleteCallback = onComplete;
            startRoutine = StartCoroutine(RunIntroComicThen(onComplete));
        }

        public void ResetStartPresentation()
        {
            if (startRoutine != null)
            {
                StopCoroutine(startRoutine);
                startRoutine = null;
            }

            isStarting = false;
            startAnimationCompleteCallback = null;
            SampleStartAnimation(0f);
            SetButtonsInteractable(true);
            SetSkipStartAnimationButtonVisible(false);
        }

        public void OnSkipStartAnimationButtonClicked()
        {
            if (!isStarting)
            {
                return;
            }

            if (startRoutine != null)
            {
                StopCoroutine(startRoutine);
                startRoutine = null;
            }

            if (startAnimationPlayer != null)
            {
                startAnimationPlayer.Stop();
            }

            CompleteStartTransition();
        }

        public void OnSettingsButtonClicked()
        {
            onSettings?.Invoke();
        }

        public void OnQuitButtonClicked()
        {
            onQuit?.Invoke();
        }

        private IEnumerator RunStartButtonFlyThen(Action onComplete)
        {
            isStarting = true;
            SetButtonsInteractable(false);
            SetSkipStartAnimationButtonVisible(false);

            if (startAnimationPlayer != null && startanimation != null)
            {
                yield return PlayStartAnimationSegment(0f, Mathf.Min(startButtonFlyEndTime, startanimation.length));
            }

            startRoutine = null;
            CompleteStartTransition(onComplete);
        }

        private IEnumerator RunIntroComicThen(Action onComplete)
        {
            isStarting = true;
            SetButtonsInteractable(false);
            SetSkipStartAnimationButtonVisible(true);

            if (startAnimationPlayer != null && startanimation != null)
            {
                yield return PlayStartAnimationSegment(Mathf.Min(comicStartTime, startanimation.length), startanimation.length);
            }

            startRoutine = null;
            CompleteStartTransition(onComplete);
        }

        private IEnumerator PlayStartAnimationSegment(float fromTime, float toTime)
        {
            PrepareStartAnimationClip();

            var clipName = startanimation.name;
            var state = startAnimationPlayer[clipName];
            state.time = fromTime;
            state.speed = 1f;
            startAnimationPlayer.Play(clipName);

            var duration = Mathf.Max(0f, toTime - fromTime);
            yield return new WaitForSeconds(duration);

            startAnimationPlayer.Stop();
            SampleStartAnimation(toTime);
        }

        private void PrepareStartAnimationClip()
        {
            startAnimationPlayer.Stop();
            startAnimationPlayer.clip = startanimation;
            if (startAnimationPlayer.GetClip(startanimation.name) == null)
            {
                startAnimationPlayer.AddClip(startanimation, startanimation.name);
            }
        }

        private void SampleStartAnimation(float time)
        {
            if (startAnimationPlayer == null || startanimation == null)
            {
                return;
            }

            PrepareStartAnimationClip();
            var state = startAnimationPlayer[startanimation.name];
            state.enabled = true;
            state.time = Mathf.Clamp(time, 0f, startanimation.length);
            startAnimationPlayer.Sample();
            state.enabled = false;
        }

        private void CompleteStartTransition(Action onComplete = null)
        {
            if (!isStarting)
            {
                return;
            }

            SetSkipStartAnimationButtonVisible(false);
            (onComplete ?? startAnimationCompleteCallback)?.Invoke();
            startAnimationCompleteCallback = null;
            SetButtonsInteractable(true);
            isStarting = false;
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (startButton != null) startButton.interactable = interactable;
            if (settingsButton != null) settingsButton.interactable = interactable;
            if (quitButton != null) quitButton.interactable = interactable;
        }

        private void SetSkipStartAnimationButtonVisible(bool visible)
        {
            if (skipStartAnimationButton != null)
            {
                skipStartAnimationButton.gameObject.SetActive(visible);
            }
        }

        private void BindGeneratedButtonClickEvents()
        {
            if (buttonClickEventsBound)
            {
                return;
            }

            startButton.onClick.AddListener(OnStartButtonClicked);
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            quitButton.onClick.AddListener(OnQuitButtonClicked);
            skipStartAnimationButton.onClick.AddListener(OnSkipStartAnimationButtonClicked);
            buttonClickEventsBound = true;
        }

        private void ResolveReferences()
        {
            startButton = startButton != null ? startButton : FindChildComponent<Button>("StartButton");
            settingsButton = settingsButton != null ? settingsButton : FindChildComponent<Button>("SettingsButton");
            quitButton = quitButton != null ? quitButton : FindChildComponent<Button>("QuitButton");
            skipStartAnimationButton = skipStartAnimationButton != null ? skipStartAnimationButton : FindChildComponent<Button>("SkipStartAnimationButton");
            if (skipStartAnimationButton == null)
            {
                skipStartAnimationButton = CreateSkipStartAnimationButton(transform);
            }

            startAnimationPlayer = startAnimationPlayer != null ? startAnimationPlayer : GetComponent<Animation>();
        }

        private T FindChildComponent<T>(string childName) where T : Component
        {
            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                {
                    return child.GetComponent<T>();
                }
            }

            return null;
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

        private static Button CreateSkipStartAnimationButton(Transform root)
        {
            var button = UIBuilder.CreateButton(root, "SkipStartAnimationButton", "跳过人生", null);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.87f, 0.89f);
            rect.anchorMax = new Vector2(0.96f, 0.96f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return button;
        }
    }
}
