using System;
using BrokenAnchor.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace BrokenAnchor.UI
{
    public class ResultView : MonoBehaviour
    {
        private Text titleText;
        private Text detailText;
        private Button retryButton;
        private Button backBuildButton;
        private Button menuButton;

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
            view.menuButton = CreateButton(root, "MenuButton", "主菜单", 2);
            return view;
        }

        public void Initialize(Action onRetry, Action onBackBuild, Action onMenu)
        {
            retryButton.onClick.AddListener(() => onRetry());
            backBuildButton.onClick.AddListener(() => onBackBuild());
            menuButton.onClick.AddListener(() => onMenu());
        }

        public void Bind(SimulationResult result)
        {
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

        private static Button CreateButton(Transform root, string name, string text, int index)
        {
            var button = UIBuilder.CreateButton(root, name, text, null);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.25f + index * 0.18f, 0.16f);
            rect.anchorMax = new Vector2(0.39f + index * 0.18f, 0.24f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return button;
        }
    }
}
