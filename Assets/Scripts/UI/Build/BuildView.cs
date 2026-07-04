using System;
using System.Collections.Generic;
using BrokenAnchor.Build;
using BrokenAnchor.Config;
using BrokenAnchor.Pieces;
using UnityEngine;
using UnityEngine.UI;

namespace BrokenAnchor.UI
{
    public class BuildView : MonoBehaviour
    {
        [SerializeField] private BuildController controller;
        [SerializeField] private RectTransform workspace;
        [SerializeField] private RectTransform connectionLayer;
        [SerializeField] private RectTransform ropeMountPoint;
        [SerializeField] private RectTransform materialPile;
        [SerializeField] private Text riskText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button rotateButton;
        [SerializeField] private Button flipButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button submitButton;

        private Action<AnchorBuildResult> submitCallback;
        private static readonly Color RopeMountIdleColor = new Color(1f, 0.67f, 0.08f, 0.42f);
        private static readonly Color RopeMountBorderColor = new Color(1f, 0.92f, 0.18f, 1f);
        private static readonly Color RopeMountTextColor = new Color(0.05f, 0.06f, 0.03f, 1f);

        public static BuildView Create(Transform parent)
        {
            var root = UIBuilder.CreateRect(parent, "BuildView", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var view = root.gameObject.AddComponent<BuildView>();
            UIBuilder.CreatePanel(root, "Background", new Color(0.035f, 0.09f, 0.105f, 1f));

            var title = UIBuilder.CreateText(root, "Title", "拼装船锚", 30, Color.white, TextAnchor.MiddleLeft);
            title.rectTransform.anchorMin = new Vector2(0.03f, 0.9f);
            title.rectTransform.anchorMax = new Vector2(0.3f, 0.98f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            view.materialPile = UIBuilder.CreateRect(root, "MaterialPile", new Vector2(0.03f, 0.15f), new Vector2(0.2f, 0.88f), Vector2.zero, Vector2.zero);
            view.materialPile.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.15f, 0.17f, 1f);

            var pileTitle = UIBuilder.CreateText(view.materialPile, "PileTitle", "材料堆", 20, Color.white, TextAnchor.MiddleCenter);
            pileTitle.rectTransform.anchorMin = new Vector2(0f, 0.86f);
            pileTitle.rectTransform.anchorMax = new Vector2(1f, 0.98f);
            pileTitle.rectTransform.offsetMin = Vector2.zero;
            pileTitle.rectTransform.offsetMax = Vector2.zero;

            view.workspace = UIBuilder.CreateRect(root, "Workspace", new Vector2(0.22f, 0.15f), new Vector2(0.76f, 0.88f), Vector2.zero, Vector2.zero);
            view.workspace.gameObject.AddComponent<Image>().color = new Color(0.11f, 0.16f, 0.16f, 1f);

            var gridText = UIBuilder.CreateText(view.workspace, "WorkspaceHint", "从旁边材料堆拖入拼装区；贴边组合后可旋转、翻转。把一个物品覆盖到上方挂点来连接绳子。", 18, new Color(0.63f, 0.72f, 0.7f, 0.75f), TextAnchor.LowerCenter);
            gridText.rectTransform.anchorMin = new Vector2(0f, 0f);
            gridText.rectTransform.anchorMax = new Vector2(1f, 0.12f);
            gridText.rectTransform.offsetMin = Vector2.zero;
            gridText.rectTransform.offsetMax = Vector2.zero;

            view.connectionLayer = UIBuilder.CreateRect(view.workspace, "ConnectionLayer", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            view.connectionLayer.SetAsFirstSibling();

            view.ropeMountPoint = UIBuilder.CreateRect(view.workspace, "RopeMountPoint", new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), new Vector2(-42f, -20f), new Vector2(42f, 20f));
            view.ropeMountPoint.gameObject.AddComponent<Image>().color = RopeMountIdleColor;
            var mountLabel = UIBuilder.CreateText(view.ropeMountPoint, "Label", "绳子挂点", 15, new Color(0.08f, 0.09f, 0.08f), TextAnchor.MiddleCenter);
            mountLabel.rectTransform.anchorMin = Vector2.zero;
            mountLabel.rectTransform.anchorMax = Vector2.one;
            mountLabel.rectTransform.offsetMin = Vector2.zero;
            mountLabel.rectTransform.offsetMax = Vector2.zero;
            ConfigureRopeMountPointVisual(view.ropeMountPoint);

            var side = UIBuilder.CreateRect(root, "RiskPanel", new Vector2(0.78f, 0.15f), new Vector2(0.97f, 0.88f), Vector2.zero, Vector2.zero);
            side.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.13f, 0.15f, 1f);

            var riskTitle = UIBuilder.CreateText(side, "RiskTitle", "风险提示", 21, Color.white, TextAnchor.MiddleLeft);
            riskTitle.rectTransform.anchorMin = new Vector2(0.08f, 0.86f);
            riskTitle.rectTransform.anchorMax = new Vector2(0.92f, 0.96f);
            riskTitle.rectTransform.offsetMin = Vector2.zero;
            riskTitle.rectTransform.offsetMax = Vector2.zero;

            view.riskText = UIBuilder.CreateText(side, "RiskText", "", 17, new Color(1f, 0.86f, 0.58f), TextAnchor.UpperLeft);
            view.riskText.rectTransform.anchorMin = new Vector2(0.08f, 0.36f);
            view.riskText.rectTransform.anchorMax = new Vector2(0.92f, 0.84f);
            view.riskText.rectTransform.offsetMin = Vector2.zero;
            view.riskText.rectTransform.offsetMax = Vector2.zero;

            view.statusText = UIBuilder.CreateText(root, "StatusText", "", 16, new Color(0.84f, 0.92f, 0.91f), TextAnchor.MiddleLeft);
            view.statusText.rectTransform.anchorMin = new Vector2(0.22f, 0.07f);
            view.statusText.rectTransform.anchorMax = new Vector2(0.76f, 0.13f);
            view.statusText.rectTransform.offsetMin = Vector2.zero;
            view.statusText.rectTransform.offsetMax = Vector2.zero;

            view.rotateButton = CreateActionButton(side, "RotateButton", "旋转", 0);
            view.flipButton = CreateActionButton(side, "FlipButton", "翻转", 1);
            view.clearButton = CreateActionButton(side, "ClearButton", "清空", 2);
            view.submitButton = CreateActionButton(side, "SubmitButton", "下锚", 3);
            view.controller = root.gameObject.AddComponent<BuildController>();
            view.BindGeneratedButtonClickEvents();
            return view;
        }

        public void Bind(IReadOnlyList<MaterialConfig> materials, Action<AnchorBuildResult> onSubmit)
        {
            submitCallback = onSubmit;
            ResolveReferences();
            RemovePrefabAnchorPiecePlaceholders();
            InitializeController();
            controller.PopulateMaterialPile(materials);
        }

        public void OnRotateButtonClicked()
        {
            controller?.RotateSelected();
        }

        public void OnFlipButtonClicked()
        {
            controller?.FlipSelected();
        }

        public void OnClearButtonClicked()
        {
            controller?.ClearBuild();
        }

        public void OnSubmitButtonClicked()
        {
            controller?.Submit();
        }

        private void InitializeController()
        {
            controller.Initialize(transform as RectTransform, workspace, connectionLayer, ropeMountPoint, materialPile, riskText, statusText, new AttachConfig(), result => submitCallback?.Invoke(result));
        }

        private void BindGeneratedButtonClickEvents()
        {
            rotateButton.onClick.AddListener(OnRotateButtonClicked);
            flipButton.onClick.AddListener(OnFlipButtonClicked);
            clearButton.onClick.AddListener(OnClearButtonClicked);
            submitButton.onClick.AddListener(OnSubmitButtonClicked);
        }

        private void ResolveReferences()
        {
            controller = controller != null ? controller : GetComponent<BuildController>();
            if (controller == null)
            {
                controller = gameObject.AddComponent<BuildController>();
            }

            workspace = workspace != null ? workspace : FindChildComponent<RectTransform>("Workspace");
            connectionLayer = connectionLayer != null ? connectionLayer : FindChildComponent<RectTransform>("ConnectionLayer");
            ropeMountPoint = ropeMountPoint != null ? ropeMountPoint : FindChildComponent<RectTransform>("RopeMountPoint");
            materialPile = materialPile != null ? materialPile : FindChildComponent<RectTransform>("MaterialPile");
            riskText = riskText != null ? riskText : FindChildComponent<Text>("RiskText");
            statusText = statusText != null ? statusText : FindChildComponent<Text>("StatusText");
            rotateButton = rotateButton != null ? rotateButton : FindChildComponent<Button>("RotateButton");
            flipButton = flipButton != null ? flipButton : FindChildComponent<Button>("FlipButton");
            clearButton = clearButton != null ? clearButton : FindChildComponent<Button>("ClearButton");
            submitButton = submitButton != null ? submitButton : FindChildComponent<Button>("SubmitButton");
            ConfigureRopeMountPointVisual(ropeMountPoint);
        }

        private void RemovePrefabAnchorPiecePlaceholders()
        {
            var pieces = GetComponentsInChildren<AnchorPiece>(true);
            for (var i = pieces.Length - 1; i >= 0; i--)
            {
                Destroy(pieces[i].gameObject);
            }
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

        private static Button CreateActionButton(Transform parent, string name, string text, int index)
        {
            var button = UIBuilder.CreateButton(parent, name, text, null);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.12f, 0.25f - index * 0.06f);
            rect.anchorMax = new Vector2(0.88f, 0.3f - index * 0.06f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return button;
        }

        private static void ConfigureRopeMountPointVisual(RectTransform mount)
        {
            if (mount == null)
            {
                return;
            }

            var image = mount.GetComponent<Image>();
            if (image != null)
            {
                image.color = RopeMountIdleColor;
                image.raycastTarget = false;
            }

            mount.SetAsLastSibling();
            EnsureLine(mount, "MountBorderTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -4f), Vector2.zero, RopeMountBorderColor);
            EnsureLine(mount, "MountBorderBottom", Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 4f), RopeMountBorderColor);
            EnsureLine(mount, "MountBorderLeft", Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(4f, 0f), RopeMountBorderColor);
            EnsureLine(mount, "MountBorderRight", new Vector2(1f, 0f), Vector2.one, new Vector2(-4f, 0f), Vector2.zero, RopeMountBorderColor);
            EnsureLine(mount, "MountCrossHorizontal", new Vector2(0.12f, 0.5f), new Vector2(0.88f, 0.5f), new Vector2(0f, -2f), new Vector2(0f, 2f), new Color(1f, 0.98f, 0.52f, 0.95f));
            EnsureLine(mount, "MountCrossVertical", new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.88f), new Vector2(-2f, 0f), new Vector2(2f, 0f), new Color(1f, 0.98f, 0.52f, 0.95f));

            var label = FindDirectChild<Text>(mount, "Label");
            if (label != null)
            {
                label.text = "绳子挂点\n检测区";
                label.fontSize = 14;
                label.color = RopeMountTextColor;
                label.raycastTarget = false;
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = Vector2.zero;
                label.rectTransform.offsetMax = Vector2.zero;
            }
        }

        private static Image EnsureLine(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var child = parent.Find(name) as RectTransform;
            if (child == null)
            {
                child = UIBuilder.CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
                child.gameObject.AddComponent<Image>();
            }

            child.anchorMin = anchorMin;
            child.anchorMax = anchorMax;
            child.offsetMin = offsetMin;
            child.offsetMax = offsetMax;
            var image = child.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static T FindDirectChild<T>(Transform parent, string childName) where T : Component
        {
            var child = parent == null ? null : parent.Find(childName);
            return child == null ? null : child.GetComponent<T>();
        }
    }
}
