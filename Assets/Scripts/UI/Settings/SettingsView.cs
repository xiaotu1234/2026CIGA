using System;
using BrokenAnchor.Core;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace BrokenAnchor.UI
{
    [ExecuteAlways]
    public class SettingsView : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField] private Toggle debugToggle;
        [SerializeField, FormerlySerializedAs("volumeSlider")] private Slider musicVolumeSlider;
        [SerializeField] private Slider voiceVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        private Action onBack;

        private void OnEnable()
        {
            ResolveReferences();
            EnsureSliderVisuals();

            if (Application.isPlaying)
            {
                BindAudioSettings();
            }
        }

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

            view.musicVolumeSlider = CreateSliderRow(panel, "MusicVolume", "BGM", 0.62f);
            view.voiceVolumeSlider = CreateSliderRow(panel, "VoiceVolume", "语音", 0.5f);
            view.sfxVolumeSlider = CreateSliderRow(panel, "SfxVolume", "音效", 0.38f);

            var debugLabel = UIBuilder.CreateText(panel, "DebugLabel", "调试显示", 18, Color.white, TextAnchor.MiddleLeft);
            debugLabel.rectTransform.anchorMin = new Vector2(0.12f, 0.26f);
            debugLabel.rectTransform.anchorMax = new Vector2(0.45f, 0.35f);
            debugLabel.rectTransform.offsetMin = Vector2.zero;
            debugLabel.rectTransform.offsetMax = Vector2.zero;

            var toggleRect = UIBuilder.CreateRect(panel, "DebugToggle", new Vector2(0.68f, 0.27f), new Vector2(0.78f, 0.35f), Vector2.zero, Vector2.zero);
            view.debugToggle = toggleRect.gameObject.AddComponent<Toggle>();

            var quality = UIBuilder.CreateText(panel, "Quality", "画面质量：原型占位", 18, new Color(0.78f, 0.86f, 0.86f), TextAnchor.MiddleLeft);
            quality.rectTransform.anchorMin = new Vector2(0.12f, 0.16f);
            quality.rectTransform.anchorMax = new Vector2(0.88f, 0.25f);
            quality.rectTransform.offsetMin = Vector2.zero;
            quality.rectTransform.offsetMax = Vector2.zero;

            view.backButton = UIBuilder.CreateButton(panel, "BackButton", "返回", null);
            view.backButton.GetComponent<RectTransform>().anchorMin = new Vector2(0.34f, 0.03f);
            view.backButton.GetComponent<RectTransform>().anchorMax = new Vector2(0.66f, 0.13f);
            view.backButton.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            view.backButton.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            view.EnsureSliderVisuals();
            view.BindGeneratedButtonClickEvents();
            return view;
        }

        public void Initialize(Action onBack)
        {
            ResolveReferences();
            EnsureSliderVisuals();
            BindAudioSettings();
            this.onBack = onBack;
        }

        public void OnBackButtonClicked()
        {
            onBack?.Invoke();
        }

        private void BindGeneratedButtonClickEvents()
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
        }

        private void ResolveReferences()
        {
            backButton = backButton != null ? backButton : FindChildComponent<Button>("BackButton");
            debugToggle = debugToggle != null ? debugToggle : FindChildComponent<Toggle>("DebugToggle");
            musicVolumeSlider = musicVolumeSlider != null ? musicVolumeSlider : FindChildComponent<Slider>("MusicVolumeSlider");
            musicVolumeSlider = musicVolumeSlider != null ? musicVolumeSlider : FindChildComponent<Slider>("VolumeSlider");
            voiceVolumeSlider = voiceVolumeSlider != null ? voiceVolumeSlider : FindChildComponent<Slider>("VoiceVolumeSlider");
            sfxVolumeSlider = sfxVolumeSlider != null ? sfxVolumeSlider : FindChildComponent<Slider>("SfxVolumeSlider");
            CreateMissingRows();
        }

        private void BindAudioSettings()
        {
            BindSlider(musicVolumeSlider, AudioSettingsController.MusicVolume, AudioSettingsController.SetMusicVolume);
            BindSlider(voiceVolumeSlider, AudioSettingsController.VoiceVolume, AudioSettingsController.SetVoiceVolume);
            BindSlider(sfxVolumeSlider, AudioSettingsController.SfxVolume, AudioSettingsController.SetSfxVolume);
        }

        private static Slider CreateSliderRow(Transform panel, string namePrefix, string labelText, float centerY)
        {
            var label = UIBuilder.CreateText(panel, $"{namePrefix}Label", labelText, 18, Color.white, TextAnchor.MiddleLeft);
            label.rectTransform.anchorMin = new Vector2(0.12f, centerY - 0.035f);
            label.rectTransform.anchorMax = new Vector2(0.32f, centerY + 0.035f);
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;

            var sliderRect = UIBuilder.CreateRect(panel, $"{namePrefix}Slider", new Vector2(0.38f, centerY - 0.02f), new Vector2(0.86f, centerY + 0.02f), Vector2.zero, Vector2.zero);
            var slider = sliderRect.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;
            BuildSliderVisuals(slider);
            return slider;
        }

        private void CreateMissingRows()
        {
            var panel = FindChildTransform("Panel");
            if (panel == null)
            {
                return;
            }

            if (musicVolumeSlider == null)
            {
                musicVolumeSlider = CreateSliderRow(panel, "MusicVolume", "BGM", 0.62f);
            }

            if (voiceVolumeSlider == null)
            {
                voiceVolumeSlider = CreateSliderRow(panel, "VoiceVolume", "语音", 0.5f);
            }

            if (sfxVolumeSlider == null)
            {
                sfxVolumeSlider = CreateSliderRow(panel, "SfxVolume", "音效", 0.38f);
            }
        }

        private void EnsureSliderVisuals()
        {
            BuildSliderVisuals(musicVolumeSlider);
            BuildSliderVisuals(voiceVolumeSlider);
            BuildSliderVisuals(sfxVolumeSlider);
        }

        private static void BindSlider(Slider slider, float value, UnityEngine.Events.UnityAction<float> onChanged)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.RemoveListener(onChanged);
            slider.onValueChanged.AddListener(onChanged);
        }

        private static void BuildSliderVisuals(Slider slider)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.direction = Slider.Direction.LeftToRight;

            var background = EnsureImage(slider.transform, "Background", new Color(0.1f, 0.2f, 0.22f, 1f));
            var backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = new Vector2(0f, 0.32f);
            backgroundRect.anchorMax = new Vector2(1f, 0.68f);
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            var fillArea = EnsureRect(slider.transform, "Fill Area");
            fillArea.anchorMin = Vector2.zero;
            fillArea.anchorMax = Vector2.one;
            fillArea.offsetMin = new Vector2(8f, 0f);
            fillArea.offsetMax = new Vector2(-8f, 0f);

            var fill = EnsureImage(fillArea, "Fill", new Color(0.35f, 0.86f, 0.78f, 1f));
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0.32f);
            fillRect.anchorMax = new Vector2(1f, 0.68f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var handleArea = EnsureRect(slider.transform, "Handle Slide Area");
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(8f, 0f);
            handleArea.offsetMax = new Vector2(-8f, 0f);

            var handle = EnsureImage(handleArea, "Handle", new Color(0.92f, 0.98f, 0.92f, 1f));
            var handleRect = handle.rectTransform;
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(28f, 28f);
            handleRect.anchoredPosition = Vector2.zero;

            slider.targetGraphic = handle;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
        }

        private static RectTransform EnsureRect(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null)
            {
                return child.GetComponent<RectTransform>() ?? child.gameObject.AddComponent<RectTransform>();
            }

            var rect = UIBuilder.CreateRect(parent, childName, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return rect;
        }

        private static Image EnsureImage(Transform parent, string childName, Color color)
        {
            var rect = EnsureRect(parent, childName);
            var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
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

        private Transform FindChildTransform(string childName)
        {
            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
