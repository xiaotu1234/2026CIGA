using System.Collections.Generic;
using UnityEngine;

namespace BrokenAnchor.Core
{
    public enum GameView
    {
        MainMenu,
        Settings,
        LevelSelect,
        StartBriefing,
        Build,
        Simulation,
        Result
    }

    public class ViewFlowController : MonoBehaviour
    {
        private readonly Dictionary<GameView, GameObject> views = new Dictionary<GameView, GameObject>();
        private GameView previousView = GameView.MainMenu;

        public void Register(GameView view, GameObject root)
        {
            views[view] = root;
            root.SetActive(false);
        }

        public void Show(GameView view)
        {
            foreach (var pair in views)
            {
                pair.Value.SetActive(pair.Key == view);
            }

            if (view != GameView.Settings)
            {
                previousView = view;
            }
        }

        public void ShowLevelSelectOverMainMenu()
        {
            foreach (var pair in views)
            {
                pair.Value.SetActive(pair.Key == GameView.MainMenu || pair.Key == GameView.LevelSelect);
            }

            if (views.TryGetValue(GameView.LevelSelect, out var levelSelect))
            {
                levelSelect.transform.SetAsLastSibling();
            }

            previousView = GameView.LevelSelect;
        }

        public void ShowSettings()
        {
            if (views.ContainsKey(GameView.Settings))
            {
                views[GameView.Settings].SetActive(true);
            }
        }

        public void HideSettings()
        {
            if (views.ContainsKey(GameView.Settings))
            {
                views[GameView.Settings].SetActive(false);
            }

            if (previousView == GameView.LevelSelect)
            {
                ShowLevelSelectOverMainMenu();
                return;
            }

            Show(previousView);
        }
    }
}
