using System;
using BrokenAnchor.Build;
using BrokenAnchor.Config;
using BrokenAnchor.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace BrokenAnchor.UI
{
    public class SimulationView : MonoBehaviour
    {
        private SimulationController controller;
        private RectTransform playArea;
        private RectTransform ship;
        private RectTransform anchor;
        private RectTransform rope;
        private Text stageText;
        private Text metricText;
        private Slider progress;

        public static SimulationView Create(Transform parent)
        {
            var root = UIBuilder.CreateRect(parent, "SimulationView", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var view = root.gameObject.AddComponent<SimulationView>();
            UIBuilder.CreatePanel(root, "Background", new Color(0.02f, 0.08f, 0.13f, 1f));

            var title = UIBuilder.CreateText(root, "Title", "下锚模拟", 30, Color.white, TextAnchor.MiddleLeft);
            title.rectTransform.anchorMin = new Vector2(0.04f, 0.91f);
            title.rectTransform.anchorMax = new Vector2(0.35f, 0.98f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            view.playArea = UIBuilder.CreateRect(root, "PlayArea", new Vector2(0.04f, 0.13f), new Vector2(0.72f, 0.88f), Vector2.zero, Vector2.zero);
            view.playArea.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.15f, 0.22f, 1f);

            CreateBand(view.playArea, "WaterSurface", new Vector2(0f, 0.6f), new Vector2(1f, 0.615f), new Color(0.4f, 0.82f, 0.94f, 0.95f));
            CreateBand(view.playArea, "Seabed", new Vector2(0f, 0.08f), new Vector2(1f, 0.14f), new Color(0.48f, 0.37f, 0.22f, 1f));
            CreateBand(view.playArea, "DangerZone", new Vector2(0.88f, 0f), new Vector2(0.93f, 1f), new Color(0.9f, 0.18f, 0.13f, 0.65f));

            view.ship = CreateObject(view.playArea, "Ship", new Vector2(120f, 48f), new Color(0.78f, 0.82f, 0.78f));
            view.anchor = CreateObject(view.playArea, "Anchor", new Vector2(76f, 76f), new Color(0.45f, 0.5f, 0.53f));
            view.rope = UIBuilder.CreateRect(view.playArea, "Rope", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            view.rope.gameObject.AddComponent<Image>().color = new Color(0.92f, 0.78f, 0.48f, 1f);
            view.rope.SetAsFirstSibling();

            var side = UIBuilder.CreateRect(root, "MetricsPanel", new Vector2(0.75f, 0.13f), new Vector2(0.96f, 0.88f), Vector2.zero, Vector2.zero);
            side.gameObject.AddComponent<Image>().color = new Color(0.07f, 0.12f, 0.15f, 1f);

            view.stageText = UIBuilder.CreateText(side, "StageText", "", 26, Color.white, TextAnchor.MiddleLeft);
            view.stageText.rectTransform.anchorMin = new Vector2(0.1f, 0.78f);
            view.stageText.rectTransform.anchorMax = new Vector2(0.9f, 0.92f);
            view.stageText.rectTransform.offsetMin = Vector2.zero;
            view.stageText.rectTransform.offsetMax = Vector2.zero;

            view.metricText = UIBuilder.CreateText(side, "MetricText", "", 19, new Color(0.86f, 0.93f, 0.9f), TextAnchor.UpperLeft);
            view.metricText.rectTransform.anchorMin = new Vector2(0.1f, 0.32f);
            view.metricText.rectTransform.anchorMax = new Vector2(0.9f, 0.72f);
            view.metricText.rectTransform.offsetMin = Vector2.zero;
            view.metricText.rectTransform.offsetMax = Vector2.zero;

            var sliderRect = UIBuilder.CreateRect(side, "DangerProgress", new Vector2(0.1f, 0.18f), new Vector2(0.9f, 0.24f), Vector2.zero, Vector2.zero);
            view.progress = sliderRect.gameObject.AddComponent<Slider>();
            view.progress.minValue = 0f;
            view.progress.maxValue = 1f;

            view.controller = root.gameObject.AddComponent<SimulationController>();
            view.controller.Initialize(view.playArea, view.ship, view.anchor, view.rope, view.stageText, view.metricText, view.progress, null);
            return view;
        }

        public void Run(AnchorBuildResult build, LevelConfig level, Action<SimulationResult> onFinished)
        {
            controller.Initialize(playArea, ship, anchor, rope, stageText, metricText, progress, onFinished);
            controller.StartSimulation(build, level);
        }

        public void OnEnable()
        {
        }

        private static RectTransform CreateObject(Transform parent, string name, Vector2 size, Color color)
        {
            var rect = UIBuilder.CreateRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rect.sizeDelta = size;
            rect.gameObject.AddComponent<Image>().color = color;
            var label = UIBuilder.CreateText(rect, "Label", name, 16, Color.black, TextAnchor.MiddleCenter);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            return rect;
        }

        private static void CreateBand(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var rect = UIBuilder.CreateRect(parent, name, min, max, Vector2.zero, Vector2.zero);
            rect.gameObject.AddComponent<Image>().color = color;
        }
    }
}
