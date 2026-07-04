using BrokenAnchor.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BrokenAnchor.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private MainMenuView mainMenuPrefab;

        private void Awake()
        {
            EnsureCamera();
            EnsureEventSystem();

            var canvas = CreateCanvas();
            var flow = gameObject.AddComponent<ViewFlowController>();
            var round = gameObject.AddComponent<RoundController>();

            var mainMenu = CreateMainMenu(canvas.transform);
            var settings = SettingsView.Create(canvas.transform);
            var briefing = StartBriefingView.Create(canvas.transform);
            var build = BuildView.Create(canvas.transform);
            var simulation = SimulationView.Create(canvas.transform);
            var result = ResultView.Create(canvas.transform);

            flow.Register(GameView.MainMenu, mainMenu.gameObject);
            flow.Register(GameView.Settings, settings.gameObject);
            flow.Register(GameView.StartBriefing, briefing.gameObject);
            flow.Register(GameView.Build, build.gameObject);
            flow.Register(GameView.Simulation, simulation.gameObject);
            flow.Register(GameView.Result, result.gameObject);

            mainMenu.Initialize(
                () => round.StartNewRound(),
                () => flow.ShowSettings(),
                QuitGame);
            settings.Initialize(() => flow.HideSettings());
            briefing.Initialize(() => round.ShowBuild(), () => round.ShowMainMenu());
            result.Initialize(() => round.StartNewRound(), () => round.ReplayBuild(), () => round.ShowMainMenu());
            round.Initialize(flow, briefing, build, simulation, result);

            flow.Show(GameView.MainMenu);
        }

        private MainMenuView CreateMainMenu(Transform parent)
        {
            if (mainMenuPrefab != null)
            {
                return Instantiate(mainMenuPrefab, parent);
            }

            Debug.LogWarning("MainMenuView prefab is not assigned on GameBootstrap. Falling back to generated UI.");
            return MainMenuView.Create(parent);
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null)
            {
                Camera.main.orthographic = true;
                Camera.main.orthographicSize = 5f;
                Camera.main.backgroundColor = new Color(0.03f, 0.08f, 0.12f);
                return;
            }

            var cameraGo = new GameObject("Main Camera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.backgroundColor = new Color(0.03f, 0.08f, 0.12f);
            cameraGo.AddComponent<AudioListener>();
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static Canvas CreateCanvas()
        {
            var canvasGo = new GameObject("RootCanvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            Debug.Log("Quit requested in editor.");
#else
            Application.Quit();
#endif
        }
    }
}
