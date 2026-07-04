using System;
using System.Collections.Generic;
using BrokenAnchor.Config;
using UnityEngine;
using UnityEngine.UI;

namespace BrokenAnchor.UI
{
    public class StartBriefingView : MonoBehaviour
    {
        [SerializeField] private Text briefingText;
        [SerializeField] private Text materialText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button backButton;

        private Action onStartBuild;
        private Action onBack;

        public static StartBriefingView Create(Transform parent)
        {
            var root = UIBuilder.CreateRect(parent, "StartBriefingView", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var view = root.gameObject.AddComponent<StartBriefingView>();
            UIBuilder.CreatePanel(root, "Background", new Color(0.04f, 0.11f, 0.14f, 1f));

            var title = UIBuilder.CreateText(root, "Title", "开局局势", 36, Color.white, TextAnchor.MiddleLeft);
            title.rectTransform.anchorMin = new Vector2(0.08f, 0.82f);
            title.rectTransform.anchorMax = new Vector2(0.54f, 0.92f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            view.briefingText = UIBuilder.CreateText(root, "BriefingText", "", 22, new Color(0.84f, 0.92f, 0.91f), TextAnchor.UpperLeft);
            view.briefingText.rectTransform.anchorMin = new Vector2(0.08f, 0.36f);
            view.briefingText.rectTransform.anchorMax = new Vector2(0.5f, 0.78f);
            view.briefingText.rectTransform.offsetMin = Vector2.zero;
            view.briefingText.rectTransform.offsetMax = Vector2.zero;

            view.materialText = UIBuilder.CreateText(root, "MaterialText", "", 20, new Color(0.95f, 0.88f, 0.68f), TextAnchor.UpperLeft);
            view.materialText.rectTransform.anchorMin = new Vector2(0.56f, 0.32f);
            view.materialText.rectTransform.anchorMax = new Vector2(0.9f, 0.78f);
            view.materialText.rectTransform.offsetMin = Vector2.zero;
            view.materialText.rectTransform.offsetMax = Vector2.zero;

            view.startButton = UIBuilder.CreateButton(root, "StartBuildButton", "开始拼装", null);
            view.startButton.GetComponent<RectTransform>().anchorMin = new Vector2(0.68f, 0.14f);
            view.startButton.GetComponent<RectTransform>().anchorMax = new Vector2(0.88f, 0.22f);
            view.startButton.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            view.startButton.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            view.backButton = UIBuilder.CreateButton(root, "BackButton", "返回主菜单", null);
            view.backButton.GetComponent<RectTransform>().anchorMin = new Vector2(0.08f, 0.14f);
            view.backButton.GetComponent<RectTransform>().anchorMax = new Vector2(0.28f, 0.22f);
            view.backButton.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            view.backButton.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            view.BindGeneratedButtonClickEvents();
            return view;
        }

        public void Initialize(Action onStartBuild, Action onBack)
        {
            ResolveReferences();
            this.onStartBuild = onStartBuild;
            this.onBack = onBack;
        }

        public void OnStartBuildButtonClicked()
        {
            onStartBuild?.Invoke();
        }

        public void OnBackButtonClicked()
        {
            onBack?.Invoke();
        }

        public void Bind(LevelConfig level, IReadOnlyList<MaterialConfig> materials)
        {
            ResolveReferences();
            briefingText.text =
                $"船重：{level.shipWeight:0} kg\n" +
                $"海况：{level.seaState}\n" +
                $"水深：{level.waterDepth:0} m\n" +
                $"海底：{level.seabedType}\n" +
                $"危险区距离：{level.dangerZoneDistance:0} m\n" +
                $"稳船目标：{level.stableDuration:0} s";

            var lines = "本局捞到的材料：\n";
            var displayOrder = new List<string>();
            var counts = new Dictionary<string, int>();
            for (var i = 0; i < materials.Count; i++)
            {
                var displayName = materials[i].displayName.Replace("\n", " / ");
                if (!counts.ContainsKey(displayName))
                {
                    counts.Add(displayName, 0);
                    displayOrder.Add(displayName);
                }

                counts[displayName]++;
            }

            for (var i = 0; i < displayOrder.Count; i++)
            {
                var displayName = displayOrder[i];
                var count = counts[displayName];
                lines += count > 1 ? $"- {displayName} x{count}\n" : $"- {displayName}\n";
            }

            materialText.text = lines;
        }

        private void BindGeneratedButtonClickEvents()
        {
            startButton.onClick.AddListener(OnStartBuildButtonClicked);
            backButton.onClick.AddListener(OnBackButtonClicked);
        }

        private void ResolveReferences()
        {
            briefingText = briefingText != null ? briefingText : FindChildComponent<Text>("BriefingText");
            materialText = materialText != null ? materialText : FindChildComponent<Text>("MaterialText");
            startButton = startButton != null ? startButton : FindChildComponent<Button>("StartBuildButton");
            backButton = backButton != null ? backButton : FindChildComponent<Button>("BackButton");
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
    }
}
