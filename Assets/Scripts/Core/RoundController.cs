using System.Collections.Generic;
using BrokenAnchor.Build;
using BrokenAnchor.Config;
using BrokenAnchor.Simulation;
using BrokenAnchor.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace BrokenAnchor.Core
{
    public class RoundController : MonoBehaviour
    {
        private ViewFlowController flow;
        private LevelSelectView levelSelectView;
        private StartBriefingView briefingView;
        private BuildView buildView;
        private SimulationView simulationView;
        private ResultView resultView;
        private List<LevelConfig> levels;
        private LevelConfig level;
        private List<MaterialConfig> materials;
        private AnchorBuildResult currentBuild;

        public void Initialize(
            ViewFlowController flow,
            LevelSelectView levelSelectView,
            StartBriefingView briefingView,
            BuildView buildView,
            SimulationView simulationView,
            ResultView resultView)
        {
            this.flow = flow;
            this.levelSelectView = levelSelectView;
            this.briefingView = briefingView;
            this.buildView = buildView;
            this.simulationView = simulationView;
            this.resultView = resultView;
            levels = PrototypeCatalog.CreateLevels();
        }

        public void ShowLevelSelect()
        {
            RefreshLevelSelect();
            flow.Show(GameView.LevelSelect);
        }

        public void StartNewRound(int levelId)
        {
            level = PrototypeCatalog.CreateLevel(levelId);
            materials = PrototypeCatalog.CreateMaterials(level);
            currentBuild = null;
            briefingView.Bind(level, materials);
            buildView.Bind(level, materials, OnBuildSubmitted);
            flow.Show(GameView.StartBriefing);
        }

        public void RestartCurrentRound()
        {
            StartNewRound(level == null ? 1 : level.levelId);
        }

        public void UnlockAllLevels()
        {
            LevelProgress.UnlockAll(MaxLevelId);
            RefreshLevelSelect();
        }

        public void ShowMainMenu()
        {
            ReloadActiveScene();
        }

        private static void ReloadActiveScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadScene(activeScene.buildIndex);
                return;
            }

#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(activeScene.path))
            {
                EditorSceneManager.LoadScene(activeScene.path);
                return;
            }
#endif

            SceneManager.LoadScene(activeScene.name);
        }

        public void ShowBriefing()
        {
            flow.Show(GameView.StartBriefing);
        }

        public void ShowBuild()
        {
            if (currentBuild != null)
            {
                buildView.ResumeEditing();
            }

            flow.Show(GameView.Build);
        }

        public void ReplayBuild()
        {
            if (currentBuild != null)
            {
                buildView.ResumeEditing();
                flow.Show(GameView.Build);
            }
        }

        private void OnBuildSubmitted(AnchorBuildResult build)
        {
            currentBuild = build;
            flow.Show(GameView.Simulation);
            simulationView.Run(build, level, OnSimulationFinished);
        }

        private void OnSimulationFinished(SimulationResult result)
        {
            if (result.success && level != null)
            {
                LevelProgress.MarkLevelCompleted(level.levelId, MaxLevelId);
            }

            resultView.Bind(result);
            flow.Show(GameView.Result);
        }

        private int MaxLevelId
        {
            get
            {
                var max = 1;
                if (levels == null)
                {
                    return max;
                }

                for (var i = 0; i < levels.Count; i++)
                {
                    if (levels[i] != null && levels[i].levelId > max)
                    {
                        max = levels[i].levelId;
                    }
                }

                return max;
            }
        }

        private void RefreshLevelSelect()
        {
            if (levels == null || levels.Count == 0)
            {
                levels = PrototypeCatalog.CreateLevels();
            }

            levelSelectView.Bind(levels, LevelProgress.HighestUnlockedLevel);
        }
    }
}
