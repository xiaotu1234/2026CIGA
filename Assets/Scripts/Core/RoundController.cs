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
        private StartBriefingView briefingView;
        private BuildView buildView;
        private SimulationView simulationView;
        private ResultView resultView;
        private LevelConfig level;
        private List<MaterialConfig> materials;
        private AnchorBuildResult currentBuild;

        public void Initialize(
            ViewFlowController flow,
            StartBriefingView briefingView,
            BuildView buildView,
            SimulationView simulationView,
            ResultView resultView)
        {
            this.flow = flow;
            this.briefingView = briefingView;
            this.buildView = buildView;
            this.simulationView = simulationView;
            this.resultView = resultView;
            StartNewRound();
        }

        public void StartNewRound()
        {
            level = PrototypeCatalog.CreateLevel();
            materials = PrototypeCatalog.CreateMaterials(level);
            currentBuild = null;
            briefingView.Bind(level, materials);
            buildView.Bind(level, materials, OnBuildSubmitted);
            flow.Show(GameView.StartBriefing);
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
            resultView.Bind(result);
            flow.Show(GameView.Result);
        }
    }
}
