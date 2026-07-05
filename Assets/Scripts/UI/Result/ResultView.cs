using System;
using BrokenAnchor.Simulation;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace BrokenAnchor.UI
{
    public class ResultView : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text detailText;
        [SerializeField] private RawImage resultVideoImage;
        [SerializeField] private VideoPlayer resultVideoPlayer;
        [SerializeField] private VideoClip winVideoClip;
        [SerializeField] private VideoClip defeatVideoClip;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button backBuildButton;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button menuButton;

        private Action onRetry;
        private Action onBackBuild;
        private Action onNextLevel;
        private Action onMenu;
        private bool hasPendingResultVideo;
        private bool pendingResultVideoSuccess;

        public static ResultView Create(Transform parent)
        {
            var root = UIBuilder.CreateRect(parent, "ResultView", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var view = root.gameObject.AddComponent<ResultView>();
            UIBuilder.CreatePanel(root, "Background", new Color(0.04f, 0.09f, 0.1f, 1f));

            view.titleText = UIBuilder.CreateText(root, "Title", "", 42, Color.white, TextAnchor.MiddleCenter);
            view.titleText.rectTransform.anchorMin = new Vector2(0.16f, 0.7f);
            view.titleText.rectTransform.anchorMax = new Vector2(0.84f, 0.86f);
            view.titleText.rectTransform.offsetMin = Vector2.zero;
            view.titleText.rectTransform.offsetMax = Vector2.zero;

            view.detailText = UIBuilder.CreateText(root, "Detail", "", 22, new Color(0.86f, 0.93f, 0.9f), TextAnchor.UpperLeft);
            view.detailText.rectTransform.anchorMin = new Vector2(0.24f, 0.34f);
            view.detailText.rectTransform.anchorMax = new Vector2(0.76f, 0.66f);
            view.detailText.rectTransform.offsetMin = Vector2.zero;
            view.detailText.rectTransform.offsetMax = Vector2.zero;

            view.retryButton = CreateButton(root, "RetryButton", "重新挑战", 0);
            view.backBuildButton = CreateButton(root, "BackBuildButton", "返回拼装", 1);
            view.nextLevelButton = CreateButton(root, "NextLevelButton", "下一关", 2);
            view.menuButton = CreateButton(root, "MenuButton", "主菜单", 3);
            view.BindGeneratedButtonClickEvents();
            return view;
        }

        public void Initialize(Action onRetry, Action onBackBuild, Action onMenu, Action onNextLevel)
        {
            ResolveReferences();
            this.onRetry = onRetry;
            this.onBackBuild = onBackBuild;
            this.onMenu = onMenu;
            this.onNextLevel = onNextLevel;
            nextLevelButton.onClick.RemoveListener(OnNextLevelButtonClicked);
            nextLevelButton.onClick.AddListener(OnNextLevelButtonClicked);
            SetNextLevelButtonVisible(false);
        }

        public void OnRetryButtonClicked()
        {
            onRetry?.Invoke();
        }

        public void OnBackBuildButtonClicked()
        {
            onBackBuild?.Invoke();
        }

        public void OnNextLevelButtonClicked()
        {
            onNextLevel?.Invoke();
        }

        public void OnMenuButtonClicked()
        {
            onMenu?.Invoke();
        }

        public void Bind(SimulationResult result)
        {
            Bind(result, false);
        }

        public void Bind(SimulationResult result, bool canGoToNextLevel)
        {
            ResolveReferences();
            QueueResultVideo(result.success);
            SetNextLevelButtonVisible(result.success && canGoToNextLevel);

            if (result.success)
            {
                titleText.text = result.narrowSuccess ? "勉强成功" : "稳住了";
                titleText.color = result.narrowSuccess ? new Color(1f, 0.82f, 0.38f) : new Color(0.52f, 0.96f, 0.72f);
            }
            else
            {
                titleText.text = "失败";
                titleText.color = new Color(1f, 0.42f, 0.34f);
            }

            var text =
                $"船是否进入危险区：{(result.shipEnteredDangerZone ? "是" : "否")}\n" +
                $"危险区剩余距离：{result.remainingDistance:0.0} m\n" +
                $"船锚主体损伤：{result.anchorDamage * 100f:0}%\n\n" +
                "原因：\n";

            for (var i = 0; i < result.reasons.Count; i++)
            {
                text += $"- {result.reasons[i]}\n";
            }

            detailText.text = text;
        }

        private void OnEnable()
        {
            if (!hasPendingResultVideo)
            {
                return;
            }

            ResolveReferences();
            PlayResultVideo(pendingResultVideoSuccess);
        }

        private void OnDisable()
        {
            if (resultVideoPlayer == null)
            {
                return;
            }

            resultVideoPlayer.Stop();
        }

        private void QueueResultVideo(bool success)
        {
            hasPendingResultVideo = true;
            pendingResultVideoSuccess = success;

            if (!isActiveAndEnabled)
            {
                return;
            }

            PlayResultVideo(success);
        }

        private void PlayResultVideo(bool success)
        {
            if (resultVideoPlayer == null)
            {
                Debug.LogWarning("ResultView prefab is missing ResultVideoPlayer. Result video will be hidden.");
                return;
            }

            var clip = success ? winVideoClip : defeatVideoClip;
            if (clip == null)
            {
                Debug.LogWarning(success
                    ? "ResultView prefab is missing win video clip. Result video will be hidden."
                    : "ResultView prefab is missing defeat video clip. Result video will be hidden.");
                resultVideoPlayer.Stop();
                if (resultVideoImage != null)
                {
                    resultVideoImage.enabled = false;
                }

                return;
            }

            resultVideoPlayer.Stop();
            resultVideoPlayer.clip = clip;
            resultVideoPlayer.isLooping = true;
            resultVideoPlayer.playOnAwake = false;
            resultVideoPlayer.waitForFirstFrame = true;

            if (resultVideoPlayer.targetTexture != null)
            {
                resultVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
            }

            ClearResultVideoTexture();
            if (resultVideoImage != null)
            {
                resultVideoImage.texture = resultVideoPlayer.targetTexture;
                resultVideoImage.enabled = true;
            }

            resultVideoPlayer.Play();
        }

        private void ClearResultVideoTexture()
        {
            var targetTexture = resultVideoPlayer == null ? null : resultVideoPlayer.targetTexture;
            if (targetTexture == null)
            {
                return;
            }

            var previous = RenderTexture.active;
            RenderTexture.active = targetTexture;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = previous;
        }

        private void BindGeneratedButtonClickEvents()
        {
            retryButton.onClick.AddListener(OnRetryButtonClicked);
            backBuildButton.onClick.AddListener(OnBackBuildButtonClicked);
            nextLevelButton.onClick.AddListener(OnNextLevelButtonClicked);
            menuButton.onClick.AddListener(OnMenuButtonClicked);
        }

        private void ResolveReferences()
        {
            titleText = titleText != null ? titleText : FindChildComponent<Text>("Title");
            detailText = detailText != null ? detailText : FindChildComponent<Text>("Detail");
            resultVideoImage = resultVideoImage != null ? resultVideoImage : FindChildComponent<RawImage>("ResultVideoPlayer");
            resultVideoPlayer = resultVideoPlayer != null ? resultVideoPlayer : FindChildComponent<VideoPlayer>("ResultVideoPlayer");
            retryButton = retryButton != null ? retryButton : FindChildComponent<Button>("RetryButton");
            backBuildButton = backBuildButton != null ? backBuildButton : FindChildComponent<Button>("BackBuildButton");
            menuButton = menuButton != null ? menuButton : FindChildComponent<Button>("MenuButton");
            nextLevelButton = nextLevelButton != null ? nextLevelButton : FindChildComponent<Button>("NextLevelButton");
            if (nextLevelButton == null)
            {
                nextLevelButton = CreateButton(transform, "NextLevelButton", "下一关", 2);
                CopyButtonStyle(menuButton, nextLevelButton);
            }
        }

        private void SetNextLevelButtonVisible(bool visible)
        {
            if (nextLevelButton == null)
            {
                return;
            }

            nextLevelButton.gameObject.SetActive(visible);
            ApplyButtonLayout(visible);
        }

        private void ApplyButtonLayout(bool includeNextLevel)
        {
            var buttonCount = includeNextLevel ? 4 : 3;
            SetButtonSlot(retryButton, 0, buttonCount);
            SetButtonSlot(backBuildButton, 1, buttonCount);

            if (includeNextLevel)
            {
                SetButtonSlot(nextLevelButton, 2, buttonCount);
                SetButtonSlot(menuButton, 3, buttonCount);
                return;
            }

            SetButtonSlot(menuButton, 2, buttonCount);
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

        private static Button CreateButton(Transform root, string name, string text, int index)
        {
            var button = UIBuilder.CreateButton(root, name, text, null);
            SetButtonSlot(button, index, 3);
            return button;
        }

        private static void SetButtonSlot(Button button, int index, int count)
        {
            if (button == null)
            {
                return;
            }

            const float buttonWidth = 0.14f;
            const float buttonGap = 0.04f;
            var totalWidth = count * buttonWidth + (count - 1) * buttonGap;
            var start = (1f - totalWidth) * 0.5f;
            var minX = start + index * (buttonWidth + buttonGap);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(minX, 0.16f);
            rect.anchorMax = new Vector2(minX + buttonWidth, 0.24f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void CopyButtonStyle(Button source, Button target)
        {
            if (source == null || target == null)
            {
                return;
            }

            var sourceImage = source.targetGraphic as Image;
            var targetImage = target.targetGraphic as Image;
            if (sourceImage != null && targetImage != null)
            {
                targetImage.sprite = sourceImage.sprite;
                targetImage.type = sourceImage.type;
                targetImage.color = sourceImage.color;
                targetImage.pixelsPerUnitMultiplier = sourceImage.pixelsPerUnitMultiplier;
            }

            target.colors = source.colors;
            target.transition = source.transition;

            var sourceText = source.GetComponentInChildren<Text>(true);
            var targetText = target.GetComponentInChildren<Text>(true);
            if (sourceText == null || targetText == null)
            {
                return;
            }

            targetText.font = sourceText.font;
            targetText.fontSize = sourceText.fontSize;
            targetText.fontStyle = sourceText.fontStyle;
            targetText.color = sourceText.color;
            targetText.alignment = sourceText.alignment;
            targetText.horizontalOverflow = sourceText.horizontalOverflow;
            targetText.verticalOverflow = sourceText.verticalOverflow;
        }
    }
}
