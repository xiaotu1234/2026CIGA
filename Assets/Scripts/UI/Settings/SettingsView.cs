using System;
using UnityEngine;
using UnityEngine.UI;

namespace BrokenAnchor.UI
{
    public class SettingsView : MonoBehaviour
    {
        private Button backButton;
        private Toggle debugToggle;
        private Slider volumeSlider;

        public static SettingsView Create(Transform parent)
        {
            var root = UIBuilder.CreateRect(parent, "SettingsView", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var view = root.gameObject.AddComponent<SettingsView>();
            UIBuilder.CreatePanel(root, "Dim", new Color(0f, 0f, 0f, 0.58f));

            var panel = UIBuilder.CreateRect(root, "Panel", new Vector2(0.32f, 0.24f), new Vector2(0.68f, 0.76f), Vector2.zero, Vector2.zero);
            var image = panel.gameObject.AddComponent<Image>();
            image.color = new Color(0.08f, 0.14f, 0.17f, 0.96f);

            var title = UIBuilder.CreateText(panel, "Title", "设置", 30, Color.white, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0f, 0.78f);
            title.rectTransform.anchorMax = new Vector2(1f, 0.95f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            var volumeLabel = UIBuilder.CreateText(panel, "VolumeLabel", "音量", 18, Color.white, TextAnchor.MiddleLeft);
            volumeLabel.rectTransform.anchorMin = new Vector2(0.12f, 0.58f);
            volumeLabel.rectTransform.anchorMax = new Vector2(0.36f, 0.67f);
            volumeLabel.rectTransform.offsetMin = Vector2.zero;
            volumeLabel.rectTransform.offsetMax = Vector2.zero;

            var sliderRect = UIBuilder.CreateRect(panel, "VolumeSlider", new Vector2(0.38f, 0.6f), new Vector2(0.86f, 0.66f), Vector2.zero, Vector2.zero);
            view.volumeSlider = sliderRect.gameObject.AddComponent<Slider>();
            view.volumeSlider.value = 0.8f;

            var debugLabel = UIBuilder.CreateText(panel, "DebugLabel", "调试显示", 18, Color.white, TextAnchor.MiddleLeft);
            debugLabel.rectTransform.anchorMin = new Vector2(0.12f, 0.43f);
            debugLabel.rectTransform.anchorMax = new Vector2(0.45f, 0.52f);
            debugLabel.rectTransform.offsetMin = Vector2.zero;
            debugLabel.rectTransform.offsetMax = Vector2.zero;

            var toggleRect = UIBuilder.CreateRect(panel, "DebugToggle", new Vector2(0.68f, 0.44f), new Vector2(0.78f, 0.52f), Vector2.zero, Vector2.zero);
            view.debugToggle = toggleRect.gameObject.AddComponent<Toggle>();

            var quality = UIBuilder.CreateText(panel, "Quality", "画面质量：原型占位", 18, new Color(0.78f, 0.86f, 0.86f), TextAnchor.MiddleLeft);
            quality.rectTransform.anchorMin = new Vector2(0.12f, 0.29f);
            quality.rectTransform.anchorMax = new Vector2(0.88f, 0.38f);
            quality.rectTransform.offsetMin = Vector2.zero;
            quality.rectTransform.offsetMax = Vector2.zero;

            view.backButton = UIBuilder.CreateButton(panel, "BackButton", "返回", null);
            view.backButton.GetComponent<RectTransform>().anchorMin = new Vector2(0.34f, 0.08f);
            view.backButton.GetComponent<RectTransform>().anchorMax = new Vector2(0.66f, 0.2f);
            view.backButton.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            view.backButton.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            return view;
        }

        public void Initialize(Action onBack)
        {
            backButton.onClick.AddListener(() => onBack());
        }
    }
}
