using System;
using System.Collections.Generic;
using BrokenAnchor.Config;
using UnityEngine;
using UnityEngine.UI;

namespace BrokenAnchor.UI
{
    public class LevelSelectView : MonoBehaviour
    {
        [SerializeField] private RectTransform levelGrid;
        [SerializeField] private Button levelButtonPrefab;
        [SerializeField] private Text progressText;
        [SerializeField] private Button unlockAllButton;
        [SerializeField] private Button backButton;

        private Action<int> onLevelSelected;
        private Action onUnlockAll;
        private Action onBack;
        private bool generatedButtonEventsBound;

        public static LevelSelectView Create(Transform parent)
        {
            var root = UIBuilder.CreateRect(parent, "LevelSelectView", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var view = root.gameObject.AddComponent<LevelSelectView>();
            UIBuilder.CreatePanel(root, "Background", new Color(0.035f, 0.085f, 0.1f, 1f));

            var title = UIBuilder.CreateText(root, "Title", "选择关卡", 42, Color.white, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0.18f, 0.82f);
            title.rectTransform.anchorMax = new Vector2(0.82f, 0.92f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            view.progressText = UIBuilder.CreateText(root, "ProgressText", "", 22, new Color(0.82f, 0.92f, 0.9f), TextAnchor.MiddleCenter);
            view.progressText.rectTransform.anchorMin = new Vector2(0.2f, 0.74f);
            view.progressText.rectTransform.anchorMax = new Vector2(0.8f, 0.81f);
            view.progressText.rectTransform.offsetMin = Vector2.zero;
            view.progressText.rectTransform.offsetMax = Vector2.zero;

            view.levelGrid = UIBuilder.CreateRect(root, "LevelGrid", new Vector2(0.2f, 0.28f), new Vector2(0.8f, 0.72f), Vector2.zero, Vector2.zero);
            var grid = view.levelGrid.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(170f, 86f);
            grid.spacing = new Vector2(18f, 18f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.childAlignment = TextAnchor.MiddleCenter;

            view.backButton = UIBuilder.CreateButton(root, "BackButton", "返回主菜单", null);
            view.backButton.GetComponent<RectTransform>().anchorMin = new Vector2(0.2f, 0.14f);
            view.backButton.GetComponent<RectTransform>().anchorMax = new Vector2(0.36f, 0.22f);
            view.backButton.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            view.backButton.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            view.unlockAllButton = UIBuilder.CreateButton(root, "UnlockAllButton", "一键解锁全部", null);
            view.unlockAllButton.GetComponent<RectTransform>().anchorMin = new Vector2(0.62f, 0.14f);
            view.unlockAllButton.GetComponent<RectTransform>().anchorMax = new Vector2(0.8f, 0.22f);
            view.unlockAllButton.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            view.unlockAllButton.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            view.BindGeneratedButtonClickEvents();
            return view;
        }

        public void Initialize(Action<int> onLevelSelected, Action onUnlockAll, Action onBack)
        {
            ResolveReferences();
            this.onLevelSelected = onLevelSelected;
            this.onUnlockAll = onUnlockAll;
            this.onBack = onBack;
            BindGeneratedButtonClickEvents();
        }

        public void Bind(IReadOnlyList<LevelConfig> levels, int highestUnlockedLevel)
        {
            ResolveReferences();
            ClearLevelButtons();

            if (progressText != null)
            {
                progressText.text = $"已解锁至第 {highestUnlockedLevel} 关";
            }

            for (var i = 0; i < levels.Count; i++)
            {
                var level = levels[i];
                if (level == null)
                {
                    continue;
                }

                CreateLevelButton(level, level.levelId <= highestUnlockedLevel);
            }
        }

        public void OnUnlockAllButtonClicked()
        {
            onUnlockAll?.Invoke();
        }

        public void OnBackButtonClicked()
        {
            onBack?.Invoke();
        }

        private void CreateLevelButton(LevelConfig level, bool unlocked)
        {
            if (levelButtonPrefab == null)
            {
                Debug.LogWarning("LevelSelectView prefab is missing LevelButton prefab. Level buttons will be hidden.");
                return;
            }

            var button = Instantiate(levelButtonPrefab, levelGrid, false);
            button.gameObject.name = $"LevelButton_{level.levelId}";
            button.onClick.RemoveAllListeners();
            button.interactable = unlocked;

            var label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = unlocked ? $"第 {level.levelId} 关" : $"第 {level.levelId} 关\n未解锁";
            }

            var selectedLevelId = level.levelId;
            button.onClick.AddListener(() => onLevelSelected?.Invoke(selectedLevelId));
        }

        private void ClearLevelButtons()
        {
            if (levelGrid == null)
            {
                return;
            }

            for (var i = levelGrid.childCount - 1; i >= 0; i--)
            {
                Destroy(levelGrid.GetChild(i).gameObject);
            }
        }

        private void BindGeneratedButtonClickEvents()
        {
            if (generatedButtonEventsBound || unlockAllButton == null || backButton == null)
            {
                return;
            }

            unlockAllButton.onClick.AddListener(OnUnlockAllButtonClicked);
            backButton.onClick.AddListener(OnBackButtonClicked);
            generatedButtonEventsBound = true;
        }

        private void ResolveReferences()
        {
            levelGrid = levelGrid != null ? levelGrid : FindChildComponent<RectTransform>("LevelGrid");
            progressText = progressText != null ? progressText : FindChildComponent<Text>("ProgressText");
            unlockAllButton = unlockAllButton != null ? unlockAllButton : FindChildComponent<Button>("UnlockAllButton");
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
