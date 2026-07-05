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
        [SerializeField] private RectTransform materialIconRoot;
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
            view.materialText.rectTransform.anchorMin = new Vector2(0.56f, 0.73f);
            view.materialText.rectTransform.anchorMax = new Vector2(0.9f, 0.78f);
            view.materialText.rectTransform.offsetMin = Vector2.zero;
            view.materialText.rectTransform.offsetMax = Vector2.zero;

            view.materialIconRoot = UIBuilder.CreateRect(root, "MaterialIconRoot", new Vector2(0.56f, 0.24f), new Vector2(0.9f, 0.72f), Vector2.zero, Vector2.zero);

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
            ConfigureMaterialPreviewLayout();
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

            materialText.text = "\u672c\u5c40\u635e\u5230\u7684\u6750\u6599\uff1a";
            PopulateMaterialIcons(materials);
        }

        private void PopulateMaterialIcons(IReadOnlyList<MaterialConfig> materials)
        {
            EnsureMaterialIconRoot();
            if (materialIconRoot == null)
            {
                return;
            }

            for (var i = materialIconRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(materialIconRoot.GetChild(i).gameObject);
            }

            if (materials == null || materials.Count == 0)
            {
                return;
            }

            const int maxRows = 6;
            const float gap = 8f;
            var rows = Mathf.Min(maxRows, materials.Count);
            var columns = Mathf.CeilToInt(materials.Count / (float)maxRows);
            var areaSize = materialIconRoot.rect.size;
            if (areaSize.x <= 0f || areaSize.y <= 0f)
            {
                areaSize = new Vector2(380f, 280f);
            }

            var cellHeight = (areaSize.y - gap * (rows - 1)) / rows;
            var cellSize = Mathf.Clamp(cellHeight * 1.5f, 42f, 78f);
            var targetShortSide = cellSize * 0.78f;
            var maxLongSide = cellSize * 1.05f;
            const float startX = 0f;
            const float startY = 0f;

            for (var i = 0; i < materials.Count; i++)
            {
                var material = materials[i];
                var iconRect = UIBuilder.CreateRect(materialIconRoot, "MaterialIcon_" + i, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
                iconRect.pivot = new Vector2(0f, 1f);
                iconRect.sizeDelta = new Vector2(cellSize, cellSize);
                iconRect.anchoredPosition = new Vector2(startX + (i / maxRows) * (cellSize + gap), startY - (i % maxRows) * (cellSize + gap));

                var prefab = material == null ? null : LoadPiecePrefab(material.prefabAssetPath);
                if (!TryCreatePrefabTextureIcon(iconRect, prefab, targetShortSide, maxLongSide) && !TryCreatePrefabSpriteIcon(iconRect, prefab, targetShortSide, maxLongSide))
                {
                    CreateFallbackIcon(iconRect, material, targetShortSide);
                }
            }
        }

        private static bool TryCreatePrefabTextureIcon(RectTransform parent, GameObject prefab, float targetShortSide, float maxLongSide)
        {
            if (prefab == null)
            {
                return false;
            }

            var prefabImage = prefab.GetComponentInChildren<RawImage>(true);
            if (prefabImage == null || prefabImage.texture == null)
            {
                return false;
            }

            var rect = CreateCenteredIconRect(parent, GetScaledIconSize(new Vector2(prefabImage.texture.width, prefabImage.texture.height), targetShortSide, maxLongSide));
            var icon = rect.gameObject.AddComponent<RawImage>();
            icon.texture = prefabImage.texture;
            icon.uvRect = prefabImage.uvRect;
            icon.color = prefabImage.color;
            icon.raycastTarget = false;
            return true;
        }

        private static bool TryCreatePrefabSpriteIcon(RectTransform parent, GameObject prefab, float targetShortSide, float maxLongSide)
        {
            if (prefab == null)
            {
                return false;
            }

            var prefabImage = prefab.GetComponent<Image>();
            if (prefabImage == null || prefabImage.sprite == null)
            {
                return false;
            }

            var spriteSize = prefabImage.sprite.rect.size;
            var rect = CreateCenteredIconRect(parent, GetScaledIconSize(spriteSize, targetShortSide, maxLongSide));
            var icon = rect.gameObject.AddComponent<Image>();
            icon.sprite = prefabImage.sprite;
            icon.color = prefabImage.color;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            return true;
        }

        private static void CreateFallbackIcon(RectTransform parent, MaterialConfig material, float targetShortSide)
        {
            var rect = CreateCenteredIconRect(parent, new Vector2(targetShortSide, targetShortSide));
            var icon = rect.gameObject.AddComponent<Image>();
            icon.color = material == null ? Color.white : material.color;
            icon.raycastTarget = false;
        }

        private static RectTransform CreateCenteredIconRect(RectTransform parent, Vector2 size)
        {
            var rect = UIBuilder.CreateRect(parent, "Icon", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        private static Vector2 GetScaledIconSize(Vector2 sourceSize, float targetShortSide, float maxLongSide)
        {
            if (sourceSize.x <= 0f || sourceSize.y <= 0f)
            {
                return new Vector2(targetShortSide, targetShortSide);
            }

            var shortSide = Mathf.Min(sourceSize.x, sourceSize.y);
            var scale = targetShortSide / shortSide;
            var scaledSize = sourceSize * scale;
            var longSide = Mathf.Max(scaledSize.x, scaledSize.y);
            if (longSide > maxLongSide)
            {
                scaledSize *= maxLongSide / longSide;
            }

            return scaledSize;
        }

        private void EnsureMaterialIconRoot()
        {
            if (materialIconRoot != null)
            {
                return;
            }

            var existing = FindChildComponent<RectTransform>("MaterialIconRoot");
            if (existing != null)
            {
                materialIconRoot = existing;
                return;
            }

            var root = transform as RectTransform;
            if (root == null)
            {
                return;
            }

            materialIconRoot = UIBuilder.CreateRect(root, "MaterialIconRoot", new Vector2(0.56f, 0.24f), new Vector2(0.9f, 0.72f), Vector2.zero, Vector2.zero);
        }

        private void ConfigureMaterialPreviewLayout()
        {
            if (materialText != null)
            {
                materialText.rectTransform.anchorMin = new Vector2(0.56f, 0.73f);
                materialText.rectTransform.anchorMax = new Vector2(0.9f, 0.78f);
                materialText.rectTransform.offsetMin = Vector2.zero;
                materialText.rectTransform.offsetMax = Vector2.zero;
            }

            EnsureMaterialIconRoot();
            if (materialIconRoot != null)
            {
                materialIconRoot.anchorMin = new Vector2(0.56f, 0.24f);
                materialIconRoot.anchorMax = new Vector2(0.9f, 0.72f);
                materialIconRoot.offsetMin = Vector2.zero;
                materialIconRoot.offsetMax = Vector2.zero;
            }
        }

        private static GameObject LoadPiecePrefab(string assetPath)
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(assetPath))
            {
                return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            }
#endif
            return null;
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
            materialIconRoot = materialIconRoot != null ? materialIconRoot : FindChildComponent<RectTransform>("MaterialIconRoot");
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
