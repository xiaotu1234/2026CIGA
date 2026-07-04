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
        private Text jointDebugText;
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

            view.jointDebugText = UIBuilder.CreateText(side, "JointDebugText", "", 14, new Color(1f, 0.9f, 0.62f), TextAnchor.UpperLeft);
            view.jointDebugText.rectTransform.anchorMin = new Vector2(0.1f, 0.05f);
            view.jointDebugText.rectTransform.anchorMax = new Vector2(0.9f, 0.3f);
            view.jointDebugText.rectTransform.offsetMin = Vector2.zero;
            view.jointDebugText.rectTransform.offsetMax = Vector2.zero;
            view.jointDebugText.horizontalOverflow = HorizontalWrapMode.Wrap;
            view.jointDebugText.verticalOverflow = VerticalWrapMode.Truncate;

            var progressLabel = UIBuilder.CreateText(root, "ShipDangerProgressLabel", "\u8ddd\u79bb\u5371\u9669\u533a", 18, new Color(0.92f, 0.96f, 0.94f), TextAnchor.MiddleRight);
            progressLabel.rectTransform.anchorMin = new Vector2(0.34f, 0.915f);
            progressLabel.rectTransform.anchorMax = new Vector2(0.47f, 0.965f);
            progressLabel.rectTransform.offsetMin = Vector2.zero;
            progressLabel.rectTransform.offsetMax = Vector2.zero;

            view.progress = CreateProgressSlider(root, "ShipDangerProgress", new Vector2(0.48f, 0.925f), new Vector2(0.72f, 0.955f));

            view.controller = root.gameObject.AddComponent<SimulationController>();
            view.controller.Initialize(view.playArea, view.ship, view.anchor, view.rope, view.stageText, view.metricText, view.jointDebugText, view.progress, null);
            return view;
        }

        public void Run(AnchorBuildResult build, LevelConfig level, Action<SimulationResult> onFinished)
        {
            ResolveReferences();
            ClearAnchorChildren();

            controller.Initialize(playArea, ship, anchor, rope, stageText, metricText, jointDebugText, progress, onFinished);
            controller.StartSimulation(build, level);
        }

        private void ClearAnchorChildren()
        {
            foreach (Transform child in playArea)
            {
                if (child.name.StartsWith("Piece_") || child.name.StartsWith("Joint_"))
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private void ResolveReferences()
        {
            controller = controller != null ? controller : GetComponent<SimulationController>();
            if (controller == null)
            {
                controller = gameObject.AddComponent<SimulationController>();
            }

            playArea = playArea != null ? playArea : FindChildComponent<RectTransform>("PlayArea");
            ship = ship != null ? ship : FindChildComponent<RectTransform>("Ship");
            anchor = anchor != null ? anchor : FindChildComponent<RectTransform>("Anchor");
            rope = rope != null ? rope : FindChildComponent<RectTransform>("Rope");
            stageText = stageText != null ? stageText : FindChildComponent<Text>("StageText");
            metricText = metricText != null ? metricText : FindChildComponent<Text>("MetricText");
            jointDebugText = jointDebugText != null ? jointDebugText : FindChildComponent<Text>("JointDebugText");
            EnsureJointDebugText();
            progress = progress != null ? progress : FindChildComponent<Slider>("DangerProgress");
            progress = progress != null ? progress : FindChildComponent<Slider>("ShipDangerProgressBg");
        }

        private void EnsureJointDebugText()
        {
            if (jointDebugText != null)
            {
                ConfigureJointDebugText(jointDebugText);
                return;
            }

            var metricsPanel = FindChildComponent<RectTransform>("MetricsPanel");
            if (metricsPanel == null)
            {
                return;
            }

            jointDebugText = UIBuilder.CreateText(metricsPanel, "JointDebugText", "", 14, new Color(1f, 0.9f, 0.62f), TextAnchor.UpperLeft);
            ConfigureJointDebugText(jointDebugText);
        }

        private static void ConfigureJointDebugText(Text text)
        {
            text.rectTransform.anchorMin = new Vector2(0.1f, 0.05f);
            text.rectTransform.anchorMax = new Vector2(0.9f, 0.3f);
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
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

        private void InitializeController(Action<SimulationResult> onFinished)
        {
            controller.Initialize(playArea, ship, anchor, rope, stageText, metricText, jointDebugText, progress, onFinished);
        }

        private static void CreateBand(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var band = UIBuilder.CreateRect(parent, name, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            band.gameObject.AddComponent<Image>().color = color;
        }

        private static RectTransform CreateObject(Transform parent, string name, Vector2 size, Color color)
        {
            var obj = UIBuilder.CreateRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            obj.gameObject.AddComponent<Image>().color = color;
            return obj;
        }

        private static Slider CreateProgressSlider(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var bg = UIBuilder.CreateRect(parent, name + "Bg", anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            bg.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.22f, 0.24f, 1f);

            var fill = UIBuilder.CreateRect(bg, name + "Fill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            fill.gameObject.AddComponent<Image>().color = new Color(0.92f, 0.23f, 0.13f, 1f);

            var handle = UIBuilder.CreateRect(bg, name + "ShipHandle", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-20f, -18f), new Vector2(20f, 18f));
            handle.gameObject.AddComponent<Image>().color = new Color(0.78f, 0.86f, 0.82f, 1f);
            var icon = UIBuilder.CreateText(handle, "Icon", "\u8239", 16, new Color(0.05f, 0.12f, 0.14f), TextAnchor.MiddleCenter);
            icon.rectTransform.anchorMin = Vector2.zero;
            icon.rectTransform.anchorMax = Vector2.one;
            icon.rectTransform.offsetMin = Vector2.zero;
            icon.rectTransform.offsetMax = Vector2.zero;

            var slider = bg.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.fillRect = fill;
            slider.handleRect = handle;
            return slider;
        }
    }
}
