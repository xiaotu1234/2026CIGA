using BrokenAnchor.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BrokenAnchor.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private MainMenuView mainMenuPrefab;
        [SerializeField] private SettingsView settingsPrefab;
        [SerializeField] private LevelSelectView levelSelectPrefab;
        [SerializeField] private StartBriefingView startBriefingPrefab;
        [SerializeField] private BuildView buildPrefab;
        [SerializeField] private SimulationView simulationPrefab;
        [SerializeField] private ResultView resultPrefab;
        [SerializeField] private AudioClip bgmClip;

        private AudioSource backgroundMusicSource;

        private void Awake()
        {
            EnsureCamera();
            EnsureEventSystem();
            backgroundMusicSource = EnsureBackgroundMusic();
            AudioSettingsController.MusicVolumeChanged += OnMusicVolumeChanged;

            var canvas = CreateCanvas();
            var flow = gameObject.AddComponent<ViewFlowController>();
            var round = gameObject.AddComponent<RoundController>();

            var mainMenu = CreateView(mainMenuPrefab, canvas.transform, MainMenuView.Create, nameof(MainMenuView));
            var settings = CreateView(settingsPrefab, canvas.transform, SettingsView.Create, nameof(SettingsView));
            var levelSelect = CreateView(levelSelectPrefab, canvas.transform, LevelSelectView.Create, nameof(LevelSelectView));
            var briefing = CreateView(startBriefingPrefab, canvas.transform, StartBriefingView.Create, nameof(StartBriefingView));
            var build = CreateView(buildPrefab, canvas.transform, BuildView.Create, nameof(BuildView));
            var simulation = CreateView(simulationPrefab, canvas.transform, SimulationView.Create, nameof(SimulationView));
            var result = CreateView(resultPrefab, canvas.transform, ResultView.Create, nameof(ResultView));

            flow.Register(GameView.MainMenu, mainMenu.gameObject);
            flow.Register(GameView.Settings, settings.gameObject);
            flow.Register(GameView.LevelSelect, levelSelect.gameObject);
            flow.Register(GameView.StartBriefing, briefing.gameObject);
            flow.Register(GameView.Build, build.gameObject);
            flow.Register(GameView.Simulation, simulation.gameObject);
            flow.Register(GameView.Result, result.gameObject);

            mainMenu.Initialize(
                () => mainMenu.PlayStartButtonFlyThen(() => round.ShowLevelSelect()),
                () => flow.ShowSettings(),
                QuitGame);
            settings.Initialize(() => flow.HideSettings());
            levelSelect.Initialize(
                levelId =>
                {
                    if (levelId == 1)
                    {
                        mainMenu.ResetStartPresentation();
                        flow.Show(GameView.MainMenu);
                        mainMenu.PlayIntroComicThen(() => round.StartNewRound(levelId));
                        return;
                    }

                    round.StartNewRound(levelId);
                },
                () => round.UnlockAllLevels(),
                () =>
                {
                    mainMenu.ResetStartPresentation();
                    flow.Show(GameView.MainMenu);
                });
            briefing.Initialize(() => round.ShowBuild(), () => round.ShowMainMenu());
            result.Initialize(
                () => round.RestartCurrentRound(),
                () => round.ReplayBuild(),
                () => round.ShowMainMenu(),
                () => round.StartNextRound());
            round.Initialize(flow, levelSelect, briefing, build, simulation, result);

            mainMenu.ResetStartPresentation();
            flow.Show(GameView.MainMenu);
        }

        private void OnDestroy()
        {
            AudioSettingsController.MusicVolumeChanged -= OnMusicVolumeChanged;
        }

        private static T CreateView<T>(T prefab, Transform parent, System.Func<Transform, T> fallbackFactory, string viewName)
            where T : Component
        {
            if (prefab != null)
            {
                return Instantiate(prefab, parent, false);
            }

            Debug.LogWarning($"{viewName} prefab is not assigned on GameBootstrap. Falling back to generated UI.");
            return fallbackFactory(parent);
        }

        private AudioSource EnsureBackgroundMusic()
        {
            if (bgmClip == null)
            {
                Debug.LogWarning("BGM clip is not assigned on GameBootstrap.");
                return null;
            }

            var existingSource = GameObject.Find("BackgroundMusic")?.GetComponent<AudioSource>();
            if (existingSource != null)
            {
                existingSource.clip = bgmClip;
                existingSource.loop = true;
                AudioSettingsController.ApplyMusicVolume(existingSource);

                if (!existingSource.isPlaying)
                {
                    existingSource.Play();
                }

                return existingSource;
            }

            var musicGo = new GameObject("BackgroundMusic");
            DontDestroyOnLoad(musicGo);

            var source = musicGo.AddComponent<AudioSource>();
            source.clip = bgmClip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            AudioSettingsController.ApplyMusicVolume(source);
            source.Play();
            return source;
        }

        private void OnMusicVolumeChanged(float volume)
        {
            if (backgroundMusicSource != null)
            {
                backgroundMusicSource.volume = volume;
            }
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
