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

            var pile = UIBuilder.CreateRect(root, "MaterialPile", new Vector2(0.03f, 0.15f), new Vector2(0.2f, 0.88f), Vector2.zero, Vector2.zero);
            pile.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.15f, 0.17f, 1f);

            var pileTitle = UIBuilder.CreateText(pile, "PileTitle", "材料堆", 20, Color.white, TextAnchor.MiddleCenter);
            pileTitle.rectTransform.anchorMin = new Vector2(0f, 0.86f);
            pileTitle.rectTransform.anchorMax = new Vector2(1f, 0.98f);
            pileTitle.rectTransform.offsetMin = Vector2.zero;
            pileTitle.rectTransform.offsetMax = Vector2.zero;

            var workspace = UIBuilder.CreateRect(root, "Workspace", new Vector2(0.22f, 0.15f), new Vector2(0.76f, 0.88f), Vector2.zero, Vector2.zero);
            workspace.gameObject.AddComponent<Image>().color = new Color(0.11f, 0.16f, 0.16f, 1f);

            var gridText = UIBuilder.CreateText(workspace, "WorkspaceHint", "从旁边材料堆拖入拼装区；贴边组合后可旋转、翻转。把一个物品覆盖到上方挂点来连接绳子。", 18, new Color(0.63f, 0.72f, 0.7f, 0.75f), TextAnchor.LowerCenter);
            gridText.rectTransform.anchorMin = new Vector2(0f, 0f);
            gridText.rectTransform.anchorMax = new Vector2(1f, 0.12f);
            gridText.rectTransform.offsetMin = Vector2.zero;
            gridText.rectTransform.offsetMax = Vector2.zero;

            var connectionLayer = UIBuilder.CreateRect(workspace, "ConnectionLayer", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            connectionLayer.SetAsFirstSibling();

            var ropeMountPoint = UIBuilder.CreateRect(workspace, "RopeMountPoint", new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), new Vector2(-42f, -20f), new Vector2(42f, 20f));
            ropeMountPoint.gameObject.AddComponent<Image>().color = new Color(0.92f, 0.68f, 0.28f, 0.75f);
            var mountLabel = UIBuilder.CreateText(ropeMountPoint, "Label", "绳子挂点", 15, new Color(0.08f, 0.09f, 0.08f), TextAnchor.MiddleCenter);
            mountLabel.rectTransform.anchorMin = Vector2.zero;
            mountLabel.rectTransform.anchorMax = Vector2.one;
            mountLabel.rectTransform.offsetMin = Vector2.zero;
            mountLabel.rectTransform.offsetMax = Vector2.zero;

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
            var clearButton = CreateActionButton(side, "ClearButton", "清空", 2);
            var submitButton = CreateActionButton(side, "SubmitButton", "下锚", 3);

            view.controller = root.gameObject.AddComponent<BuildController>();
            view.controller.Initialize(root, workspace, connectionLayer, ropeMountPoint, pile, riskText, statusText, new AttachConfig(), result => view.submitCallback?.Invoke(result));
            rotateButton.onClick.AddListener(view.controller.RotateSelected);
            flipButton.onClick.AddListener(view.controller.FlipSelected);
            clearButton.onClick.AddListener(view.controller.ClearBuild);
            submitButton.onClick.AddListener(view.controller.Submit);
            return view;
        }

        public void Bind(IReadOnlyList<MaterialConfig> materials, Action<AnchorBuildResult> onSubmit)
        {
            submitCallback = onSubmit;
            controller.PopulateMaterialPile(materials);
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
