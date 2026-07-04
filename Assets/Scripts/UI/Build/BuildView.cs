using System;
using System.Collections.Generic;
using BrokenAnchor.Build;
using BrokenAnchor.Config;
using UnityEngine;
using UnityEngine.UI;

namespace BrokenAnchor.UI
{
    public class BuildView : MonoBehaviour
    {
        private BuildController controller;
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

            var tray = UIBuilder.CreateRect(root, "MaterialTray", new Vector2(0.03f, 0.15f), new Vector2(0.2f, 0.88f), Vector2.zero, Vector2.zero);
            tray.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.15f, 0.17f, 1f);

            var workspace = UIBuilder.CreateRect(root, "Workspace", new Vector2(0.22f, 0.15f), new Vector2(0.76f, 0.88f), Vector2.zero, Vector2.zero);
            workspace.gameObject.AddComponent<Image>().color = new Color(0.11f, 0.16f, 0.16f, 1f);

            var gridText = UIBuilder.CreateText(workspace, "WorkspaceHint", "拖拽材料贴边组合；点击材料后可旋转、翻转、设为绑点。", 18, new Color(0.63f, 0.72f, 0.7f, 0.75f), TextAnchor.LowerCenter);
            gridText.rectTransform.anchorMin = new Vector2(0f, 0f);
            gridText.rectTransform.anchorMax = new Vector2(1f, 0.12f);
            gridText.rectTransform.offsetMin = Vector2.zero;
            gridText.rectTransform.offsetMax = Vector2.zero;

            var connectionLayer = UIBuilder.CreateRect(workspace, "ConnectionLayer", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            connectionLayer.SetAsFirstSibling();

            var side = UIBuilder.CreateRect(root, "RiskPanel", new Vector2(0.78f, 0.15f), new Vector2(0.97f, 0.88f), Vector2.zero, Vector2.zero);
            side.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.13f, 0.15f, 1f);

            var riskTitle = UIBuilder.CreateText(side, "RiskTitle", "风险提示", 21, Color.white, TextAnchor.MiddleLeft);
            riskTitle.rectTransform.anchorMin = new Vector2(0.08f, 0.86f);
            riskTitle.rectTransform.anchorMax = new Vector2(0.92f, 0.96f);
            riskTitle.rectTransform.offsetMin = Vector2.zero;
            riskTitle.rectTransform.offsetMax = Vector2.zero;

            var riskText = UIBuilder.CreateText(side, "RiskText", "", 17, new Color(1f, 0.86f, 0.58f), TextAnchor.UpperLeft);
            riskText.rectTransform.anchorMin = new Vector2(0.08f, 0.36f);
            riskText.rectTransform.anchorMax = new Vector2(0.92f, 0.84f);
            riskText.rectTransform.offsetMin = Vector2.zero;
            riskText.rectTransform.offsetMax = Vector2.zero;

            var statusText = UIBuilder.CreateText(root, "StatusText", "", 16, new Color(0.84f, 0.92f, 0.91f), TextAnchor.MiddleLeft);
            statusText.rectTransform.anchorMin = new Vector2(0.22f, 0.07f);
            statusText.rectTransform.anchorMax = new Vector2(0.76f, 0.13f);
            statusText.rectTransform.offsetMin = Vector2.zero;
            statusText.rectTransform.offsetMax = Vector2.zero;

            var rotateButton = CreateActionButton(side, "RotateButton", "旋转", 0);
            var flipButton = CreateActionButton(side, "FlipButton", "翻转", 1);
            var tieButton = CreateActionButton(side, "TieButton", "设为绑点", 2);
            var clearButton = CreateActionButton(side, "ClearButton", "清空", 3);
            var submitButton = CreateActionButton(side, "SubmitButton", "下锚", 4);

            view.controller = root.gameObject.AddComponent<BuildController>();
            view.controller.Initialize(workspace, connectionLayer, tray, riskText, statusText, new AttachConfig(), result => view.submitCallback?.Invoke(result));
            rotateButton.onClick.AddListener(view.controller.RotateSelected);
            flipButton.onClick.AddListener(view.controller.FlipSelected);
            tieButton.onClick.AddListener(view.controller.SetRopeTiePoint);
            clearButton.onClick.AddListener(view.controller.ClearBuild);
            submitButton.onClick.AddListener(view.controller.Submit);
            return view;
        }

        public void Bind(IReadOnlyList<MaterialConfig> materials, Action<AnchorBuildResult> onSubmit)
        {
            submitCallback = onSubmit;
            controller.ClearBuild();
            controller.PopulateMaterialTray(materials);
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
