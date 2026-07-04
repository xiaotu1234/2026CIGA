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
        private BuildController controller;
        private RectTransform workspace;
        private RectTransform connectionLayer;
        private RectTransform ropeMountPoint;
        private RectTransform materialPile;
        private Text riskText;
        private Text statusText;
        private Button rotateButton;
        private Button flipButton;
        private Button clearButton;
        private Button submitButton;

        private Action<AnchorBuildResult> submitCallback;

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

            var pileTitle = UIBuilder.CreateText(view.materialPile, "PileTitle", "材料", 21, Color.white, TextAnchor.MiddleCenter);
            pileTitle.rectTransform.anchorMin = new Vector2(0.05f, 0.92f);
            pileTitle.rectTransform.anchorMax = new Vector2(0.95f, 0.99f);
            pileTitle.rectTransform.offsetMin = Vector2.zero;
            pileTitle.rectTransform.offsetMax = Vector2.zero;

            view.workspace = UIBuilder.CreateRect(root, "Workspace", new Vector2(0.22f, 0.15f), new Vector2(0.76f, 0.88f), Vector2.zero, Vector2.zero);
            view.workspace.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.12f, 0.14f, 1f);

            var workspaceLabel = UIBuilder.CreateText(view.workspace, "WorkspaceLabel", "工作区", 18, new Color(0.78f, 0.88f, 0.86f), TextAnchor.MiddleCenter);
            workspaceLabel.rectTransform.anchorMin = new Vector2(0.35f, 0.94f);
            workspaceLabel.rectTransform.anchorMax = new Vector2(0.65f, 0.99f);
            workspaceLabel.rectTransform.offsetMin = Vector2.zero;
            workspaceLabel.rectTransform.offsetMax = Vector2.zero;

            view.connectionLayer = UIBuilder.CreateRect(view.workspace, "ConnectionLayer", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            view.ropeMountPoint = UIBuilder.CreateRect(view.workspace, "RopeMountPoint", new Vector2(0.45f, 0.88f), new Vector2(0.55f, 0.95f), Vector2.zero, Vector2.zero);

            view.connectionLayer.gameObject.AddComponent<CanvasRenderer>();

            var side = UIBuilder.CreateRect(root, "SidePanel", new Vector2(0.77f, 0.15f), new Vector2(0.97f, 0.88f), Vector2.zero, Vector2.zero);
            side.gameObject.AddComponent<Image>().color = new Color(0.07f, 0.12f, 0.15f, 1f);

            var actionLabel = UIBuilder.CreateText(side, "ActionLabel", "操作", 21, Color.white, TextAnchor.MiddleCenter);
            actionLabel.rectTransform.anchorMin = new Vector2(0.08f, 0.9f);
            actionLabel.rectTransform.anchorMax = new Vector2(0.92f, 0.98f);
            actionLabel.rectTransform.offsetMin = Vector2.zero;
            actionLabel.rectTransform.offsetMax = Vector2.zero;

            view.rotateButton = CreateActionButton(side, "RotateButton", "旋转", 0);
            view.flipButton = CreateActionButton(side, "FlipButton", "翻转", 1);
            view.clearButton = CreateActionButton(side, "ClearButton", "清空", 2);
            view.submitButton = CreateActionButton(side, "SubmitButton", "下锚", 3);

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

            view.controller = root.gameObject.AddComponent<BuildController>();
            view.controller.Initialize(root, view.workspace, view.connectionLayer, view.ropeMountPoint, view.materialPile, view.riskText, view.statusText, new AttachConfig(), result => view.submitCallback?.Invoke(result));
            view.rotateButton.onClick.AddListener(view.controller.RotateSelected);
            view.flipButton.onClick.AddListener(view.controller.FlipSelected);
            view.clearButton.onClick.AddListener(view.controller.ClearBuild);
            view.submitButton.onClick.AddListener(view.controller.Submit);
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
            ResolveReferences();
            controller.RotateSelected();
        }

        public void OnFlipButtonClicked()
        {
            ResolveReferences();
            controller.FlipSelected();
        }

        public void OnClearButtonClicked()
        {
            ResolveReferences();
            controller.ClearBuild();
        }

        public void OnSubmitButtonClicked()
        {
            ResolveReferences();
            controller.Submit();
        }

        private void InitializeController()
        {
            controller.Initialize(transform as RectTransform, workspace, connectionLayer, ropeMountPoint, materialPile, riskText, statusText, new AttachConfig(), result => submitCallback?.Invoke(result));
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
    }
}
