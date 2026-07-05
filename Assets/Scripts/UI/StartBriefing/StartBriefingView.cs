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
            var areaSize = materialIconRoot.rect.size;
            if (areaSize.x <= 0f || areaSize.y <= 0f)
            {
                areaSize = new Vector2(380f, 280f);
            }

            var cellHeight = (areaSize.y - gap * (rows - 1)) / rows;
            var cellSize = Mathf.Clamp(cellHeight * 1.5f, 42f, 78f);
            var targetShortSide = cellSize * 0.78f;
            var maxLongSide = cellSize * 1.05f;
            var rowStep = cellSize + gap;
            var icons = new List<MaterialIconSpec>(materials.Count);
            var columnWidths = new List<float>();

            for (var i = 0; i < materials.Count; i++)
            {
                var icon = BuildMaterialIconSpec(materials[i], targetShortSide, maxLongSide);
                icons.Add(icon);

                var column = i / maxRows;
                while (columnWidths.Count <= column)
                {
                    columnWidths.Add(0f);
                }

                columnWidths[column] = Mathf.Max(columnWidths[column], icon.size.x);
            }

            var columnOffsets = new List<float>(columnWidths.Count);
            var cursorX = 0f;
            for (var i = 0; i < columnWidths.Count; i++)
            {
                columnOffsets.Add(cursorX);
                cursorX += columnWidths[i] + gap;
            }

            for (var i = 0; i < icons.Count; i++)
            {
                var column = i / maxRows;
                var row = i % maxRows;
                CreateMaterialIcon(icons[i], i, new Vector2(columnOffsets[column], -row * rowStep));
            }
        }

        private struct MaterialIconSpec
        {
            public bool useRawImage;
            public Texture texture;
            public Rect uvRect;
            public Sprite sprite;
            public Color color;
            public Vector2 size;
        }

        private static MaterialIconSpec BuildMaterialIconSpec(MaterialConfig material, float targetShortSide, float maxLongSide)
        {
            var prefab = material == null ? null : LoadPiecePrefab(material.prefabAssetPath);

            if (TryBuildPrefabTextureIcon(prefab, targetShortSide, maxLongSide, out var textureIcon))
            {
                return textureIcon;
            }

            if (TryBuildPrefabSpriteIcon(prefab, targetShortSide, maxLongSide, out var spriteIcon))
            {
                return spriteIcon;
            }

            return new MaterialIconSpec
            {
                useRawImage = false,
                color = material == null ? Color.white : material.color,
                size = new Vector2(targetShortSide, targetShortSide)
            };
        }

        private static bool TryBuildPrefabTextureIcon(GameObject prefab, float targetShortSide, float maxLongSide, out MaterialIconSpec icon)
        {
            icon = default;
            if (prefab == null)
            {
                return false;
            }

            var prefabImage = prefab.GetComponentInChildren<RawImage>(true);
            if (prefabImage == null || prefabImage.texture == null)
            {
                return false;
            }

            icon = new MaterialIconSpec
            {
                useRawImage = true,
                texture = prefabImage.texture,
                uvRect = prefabImage.uvRect,
                color = prefabImage.color,
                size = GetScaledIconSize(new Vector2(prefabImage.texture.width, prefabImage.texture.height), targetShortSide, maxLongSide)
            };
            return true;
        }

        private static bool TryBuildPrefabSpriteIcon(GameObject prefab, float targetShortSide, float maxLongSide, out MaterialIconSpec icon)
        {
            icon = default;
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
            icon = new MaterialIconSpec
            {
                useRawImage = false,
                sprite = prefabImage.sprite,
                color = prefabImage.color,
                size = GetScaledIconSize(spriteSize, targetShortSide, maxLongSide)
            };
            return true;
        }

        private void CreateMaterialIcon(MaterialIconSpec iconSpec, int index, Vector2 anchoredPosition)
        {
            var rect = UIBuilder.CreateRect(materialIconRoot, "MaterialIcon_" + index, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = iconSpec.size;
            rect.anchoredPosition = anchoredPosition;

            if (iconSpec.useRawImage)
            {
                var rawIcon = rect.gameObject.AddComponent<RawImage>();
                rawIcon.texture = iconSpec.texture;
                rawIcon.uvRect = iconSpec.uvRect;
                rawIcon.color = iconSpec.color;
                rawIcon.raycastTarget = false;
                return;
            }

            var imageIcon = rect.gameObject.AddComponent<Image>();
            imageIcon.sprite = iconSpec.sprite;
            imageIcon.color = iconSpec.color;
            imageIcon.preserveAspect = true;
            imageIcon.raycastTarget = false;
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
