using System;
using System.Collections;
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
        [SerializeField] private Text countdownText;
        [SerializeField] private RectTransform selectedItemInfoPanel;
        [SerializeField] private Text selectedItemInfoText;
        [SerializeField] private Text riskText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button rotateButton;
        [SerializeField] private Button flipButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button submitButton;

        private Action<AnchorBuildResult> submitCallback;
        private LevelConfig level;
        private Coroutine countdownRoutine;
        private float countdownRemaining;
        private bool submitTriggered;

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

            view.countdownText = UIBuilder.CreateText(root, "CountdownText", "", 40, Color.red, TextAnchor.MiddleCenter);
            view.countdownText.rectTransform.anchorMin = new Vector2(0.35f, 0.9f);
            view.countdownText.rectTransform.anchorMax = new Vector2(0.65f, 0.98f);
            view.countdownText.rectTransform.offsetMin = Vector2.zero;
            view.countdownText.rectTransform.offsetMax = Vector2.zero;
            StyleCountdownText(view.countdownText);

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
            view.ropeMountPoint.gameObject.AddComponent<Image>().color = new Color(0.92f, 0.68f, 0.28f, 0.75f);
            var mountLabel = UIBuilder.CreateText(view.ropeMountPoint, "Label", "绳子挂点", 15, new Color(0.08f, 0.09f, 0.08f), TextAnchor.MiddleCenter);
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

            view.riskText = UIBuilder.CreateText(side, "RiskText", "", 17, new Color(1f, 0.86f, 0.58f), TextAnchor.UpperLeft);
            view.riskText.rectTransform.anchorMin = new Vector2(0.08f, 0.28f);
            view.riskText.rectTransform.anchorMax = new Vector2(0.92f, 0.68f);
            view.riskText.rectTransform.offsetMin = Vector2.zero;
            view.riskText.rectTransform.offsetMax = Vector2.zero;

            view.selectedItemInfoPanel = UIBuilder.CreateRect(side, "SelectedItemInfoPanel", new Vector2(0.08f, 0.7f), new Vector2(0.92f, 0.84f), Vector2.zero, Vector2.zero);
            view.selectedItemInfoPanel.gameObject.AddComponent<Image>().color = new Color(0.92f, 0.95f, 0.9f, 0.95f);
            view.selectedItemInfoText = UIBuilder.CreateText(view.selectedItemInfoPanel, "SelectedItemInfoText", "", 16, Color.black, TextAnchor.UpperLeft);
            view.selectedItemInfoText.rectTransform.anchorMin = Vector2.zero;
            view.selectedItemInfoText.rectTransform.anchorMax = Vector2.one;
            view.selectedItemInfoText.rectTransform.offsetMin = new Vector2(10f, 6f);
            view.selectedItemInfoText.rectTransform.offsetMax = new Vector2(-10f, -6f);

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

        public void Bind(LevelConfig level, IReadOnlyList<MaterialConfig> materials, Action<AnchorBuildResult> onSubmit)
        {
            this.level = level;
            submitCallback = onSubmit;
            submitTriggered = false;
            countdownRemaining = Mathf.Max(0f, level == null ? 0f : level.buildTimeSeconds);
            ResolveReferences();
            EnsureRuntimeTextReferences();
            StyleCountdownText(countdownText);
            RemovePrefabAnchorPiecePlaceholders();
            InitializeController();
            controller.PopulateMaterialPile(materials);
            UpdateCountdownText();
            if (isActiveAndEnabled)
            {
                StartCountdownIfNeeded();
            }
        }

        public void ResumeEditing()
        {
            submitTriggered = false;
            UpdateCountdownText();
            if (isActiveAndEnabled)
            {
                StartCountdownIfNeeded();
            }
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
            SubmitBuildFromView();
        }

        private void InitializeController()
        {
            controller.Initialize(transform as RectTransform, workspace, connectionLayer, ropeMountPoint, materialPile, riskText, statusText, selectedItemInfoText, new AttachConfig(), result => submitCallback?.Invoke(result));
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
            countdownText = countdownText != null ? countdownText : FindChildComponent<Text>("CountdownText");
            selectedItemInfoPanel = selectedItemInfoPanel != null ? selectedItemInfoPanel : FindChildComponent<RectTransform>("SelectedItemInfoPanel");
            selectedItemInfoText = selectedItemInfoText != null ? selectedItemInfoText : FindChildComponent<Text>("SelectedItemInfoText");
            riskText = riskText != null ? riskText : FindChildComponent<Text>("RiskText");
            statusText = statusText != null ? statusText : FindChildComponent<Text>("StatusText");
            rotateButton = rotateButton != null ? rotateButton : FindChildComponent<Button>("RotateButton");
            flipButton = flipButton != null ? flipButton : FindChildComponent<Button>("FlipButton");
            clearButton = clearButton != null ? clearButton : FindChildComponent<Button>("ClearButton");
            submitButton = submitButton != null ? submitButton : FindChildComponent<Button>("SubmitButton");
        }

        private void EnsureRuntimeTextReferences()
        {
            var root = transform as RectTransform;
            if (countdownText == null && root != null)
            {
                countdownText = UIBuilder.CreateText(root, "CountdownText", "", 40, Color.red, TextAnchor.MiddleCenter);
                countdownText.rectTransform.anchorMin = new Vector2(0.35f, 0.9f);
                countdownText.rectTransform.anchorMax = new Vector2(0.65f, 0.98f);
                countdownText.rectTransform.offsetMin = Vector2.zero;
                countdownText.rectTransform.offsetMax = Vector2.zero;
                StyleCountdownText(countdownText);
            }

            if (selectedItemInfoText == null)
            {
                var parent = riskText == null ? root : riskText.transform.parent as RectTransform;
                if (parent != null)
                {
                    selectedItemInfoPanel = EnsureSelectedItemInfoPanel(parent);
                    selectedItemInfoText = UIBuilder.CreateText(selectedItemInfoPanel, "SelectedItemInfoText", "", 16, Color.black, TextAnchor.UpperLeft);
                }
            }

            var fallbackParent = riskText == null ? root : riskText.transform.parent as RectTransform;
            EnsureSelectedItemInfoPanel(fallbackParent);
            StyleSelectedItemInfoText();
        }

        private static void StyleCountdownText(Text text)
        {
            if (text == null)
            {
                return;
            }

            text.fontSize = 40;
            text.color = Color.red;
            text.alignment = TextAnchor.MiddleCenter;

            var outline = text.GetComponent<Outline>();
            if (outline == null)
            {
                outline = text.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = true;
        }

        private RectTransform EnsureSelectedItemInfoPanel(RectTransform parent)
        {
            if (parent == null)
            {
                return selectedItemInfoPanel;
            }

            if (selectedItemInfoPanel == null)
            {
                selectedItemInfoPanel = UIBuilder.CreateRect(parent, "SelectedItemInfoPanel", new Vector2(0.08f, 0.3f), new Vector2(0.92f, 0.43f), Vector2.zero, Vector2.zero);
                if (selectedItemInfoText != null)
                {
                    selectedItemInfoPanel.SetSiblingIndex(selectedItemInfoText.transform.GetSiblingIndex());
                }
            }

            var image = selectedItemInfoPanel.GetComponent<Image>();
            if (image == null)
            {
                image = selectedItemInfoPanel.gameObject.AddComponent<Image>();
            }

            image.color = new Color(0.92f, 0.95f, 0.9f, 0.95f);
            ApplySelectedItemInfoPanelLayout();
            return selectedItemInfoPanel;
        }

        private void ApplySelectedItemInfoPanelLayout()
        {
            if (selectedItemInfoPanel == null)
            {
                return;
            }

            selectedItemInfoPanel.anchorMin = new Vector2(0.08f, 0.7f);
            selectedItemInfoPanel.anchorMax = new Vector2(0.92f, 0.84f);
            selectedItemInfoPanel.offsetMin = Vector2.zero;
            selectedItemInfoPanel.offsetMax = Vector2.zero;
        }

        private void StyleSelectedItemInfoText()
        {
            if (selectedItemInfoText == null)
            {
                return;
            }

            selectedItemInfoText.fontSize = 16;
            selectedItemInfoText.color = Color.black;
            selectedItemInfoText.alignment = TextAnchor.UpperLeft;

            if (selectedItemInfoPanel != null && selectedItemInfoText.transform.parent == selectedItemInfoPanel)
            {
                selectedItemInfoText.rectTransform.anchorMin = Vector2.zero;
                selectedItemInfoText.rectTransform.anchorMax = Vector2.one;
                selectedItemInfoText.rectTransform.offsetMin = new Vector2(10f, 6f);
                selectedItemInfoText.rectTransform.offsetMax = new Vector2(-10f, -6f);
            }
        }

        private void OnEnable()
        {
            StartCountdownIfNeeded();
        }

        private void OnDisable()
        {
            StopCountdown();
        }

        private void StartCountdownIfNeeded()
        {
            if (submitTriggered || level == null || countdownRemaining <= 0f || countdownRoutine != null)
            {
                return;
            }

            countdownRoutine = StartCoroutine(BuildCountdown());
        }

        private IEnumerator BuildCountdown()
        {
            UpdateCountdownText();
            while (countdownRemaining > 0f && !submitTriggered)
            {
                countdownRemaining = Mathf.Max(0f, countdownRemaining - Time.deltaTime);
                UpdateCountdownText();
                yield return null;
            }

            countdownRoutine = null;
            if (!submitTriggered)
            {
                SubmitBuildFromView();
            }
        }

        private void SubmitBuildFromView()
        {
            if (submitTriggered)
            {
                return;
            }

            submitTriggered = true;
            StopCountdown();
            controller?.Submit();
        }

        private void StopCountdown()
        {
            if (countdownRoutine == null)
            {
                return;
            }

            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }

        private void UpdateCountdownText()
        {
            if (countdownText == null)
            {
                return;
            }

            var seconds = Mathf.CeilToInt(Mathf.Max(0f, countdownRemaining));
            countdownText.text = $"拼装倒计时 {seconds / 60:00}:{seconds % 60:00}";
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
