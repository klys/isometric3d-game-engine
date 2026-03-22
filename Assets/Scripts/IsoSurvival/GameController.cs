using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace IsoSurvival
{
    public class GameController : MonoBehaviour
    {
        private const string CameraName = "Isometric Camera";

        private Camera mainCamera;
        private IsometricCameraController cameraController;
        private GameUiController ui;
        private ProceduralWorldSystem world;
        private UnitsSystem units;
        private BuildingsSystem buildings;
        private WaveSystem waves;

        private GameSettings activeSettings;
        private GameSettings menuSettings;

        public GameState State { get; private set; }
        public Inventory Inventory { get; private set; }
        public int CurrentWave { get; private set; }

        public Camera MainCamera => mainCamera;
        public ProceduralWorldSystem World => world;
        public UnitsSystem Units => units;
        public BuildingsSystem Buildings => buildings;
        public WaveSystem Waves => waves;
        public GameSettings ActiveSettings => activeSettings;
        public GameSettings MenuSettings => menuSettings;
        public bool IsSimulationRunning => State == GameState.Playing;

        private void Awake()
        {
            menuSettings = CreateDefaultSettings();
            activeSettings = menuSettings.Clone();
            Inventory = new Inventory();

            EnsureSceneServices();
            CreateSystems();

            State = GameState.MainMenu;
            ui.Build();
            ui.ShowMainMenu();
        }

        private void Update()
        {
            if (State == GameState.Playing && Input.GetKeyDown(KeyCode.Escape))
            {
                SetPaused(true);
            }
            else if (State == GameState.Paused && Input.GetKeyDown(KeyCode.Escape))
            {
                SetPaused(false);
            }
        }

        public void StartGame()
        {
            ResetSession();

            activeSettings = menuSettings.Clone();
            activeSettings.Clamp();
            if (activeSettings.Seed == 0)
            {
                activeSettings.Seed = DateTime.Now.Millisecond + DateTime.Now.Second * 1000;
            }

            Inventory.Reset(
                activeSettings.StartingPlants,
                activeSettings.StartingFlowers,
                activeSettings.StartingRocks,
                activeSettings.StartingMinerals);

            world.BeginSession(activeSettings);
            buildings.BeginSession();
            units.BeginSession(activeSettings.StartingHumanoids);
            waves.BeginSession();
            cameraController.FocusOn(world.TileToWorld(Vector2Int.zero));

            CurrentWave = 0;
            State = GameState.Playing;
            Time.timeScale = 1f;
            ui.ShowHud();
        }

        public void ReturnToMainMenu()
        {
            ResetSession();
            State = GameState.MainMenu;
            Time.timeScale = 1f;
            ui.ShowMainMenu();
        }

        public void SetPaused(bool paused)
        {
            if (State != GameState.Playing && State != GameState.Paused)
            {
                return;
            }

            State = paused ? GameState.Paused : GameState.Playing;
            Time.timeScale = paused ? 0f : 1f;
            ui.SetPauseVisible(paused);
        }

        public void TriggerGameOver()
        {
            if (State == GameState.GameOver)
            {
                return;
            }

            State = GameState.GameOver;
            Time.timeScale = 0f;
            ui.ShowGameOver();
        }

        public void RegisterWaveStarted(int waveNumber)
        {
            CurrentWave = waveNumber;
        }

        public void AdjustStartingHumanoids(int delta)
        {
            menuSettings.StartingHumanoids = Mathf.Clamp(menuSettings.StartingHumanoids + delta, 1, 12);
            ui.RefreshSettings();
        }

        public void AdjustEnemyIntensity(float delta)
        {
            menuSettings.EnemyIntensity = Mathf.Clamp(menuSettings.EnemyIntensity + delta, 0.5f, 3f);
            ui.RefreshSettings();
        }

        public void AdjustWaveInterval(float delta)
        {
            menuSettings.WaveIntervalSeconds = Mathf.Clamp(menuSettings.WaveIntervalSeconds + delta, 10f, 90f);
            ui.RefreshSettings();
        }

        public void RandomizeSeed()
        {
            menuSettings.Seed = UnityEngine.Random.Range(1, 999999);
            ui.RefreshSettings();
        }

        public void ExitGame()
        {
            Application.Quit();
        }

        public IDamageable FindNearestDamageable(Vector3 position, float maxDistance)
        {
            IDamageable nearest = units.FindNearestLiving(position, maxDistance);
            IDamageable building = buildings.FindNearestLiving(position, maxDistance);

            if (nearest == null)
            {
                return building;
            }

            if (building == null)
            {
                return nearest;
            }

            var nearestDistance = Vector3.Distance(position, nearest.AimPoint);
            var buildingDistance = Vector3.Distance(position, building.AimPoint);
            return nearestDistance <= buildingDistance ? nearest : building;
        }

        public void NotifyHumanoidDied()
        {
            if (units.AliveCount <= 0)
            {
                TriggerGameOver();
            }
        }

        private void ResetSession()
        {
            waves.ClearSession();
            buildings.ClearSession();
            units.ClearSession();
            world.ClearSession();
            CurrentWave = 0;
        }

        private void EnsureSceneServices()
        {
            EnsureEventSystem();
            EnsureCamera();
            EnsureLighting();
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private void EnsureCamera()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraObject = new GameObject(CameraName);
                mainCamera = cameraObject.AddComponent<Camera>();
                mainCamera.tag = "MainCamera";
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = new Color(0.74f, 0.86f, 0.95f);
            }

            cameraController = mainCamera.gameObject.GetComponent<IsometricCameraController>();
            if (cameraController == null)
            {
                cameraController = mainCamera.gameObject.AddComponent<IsometricCameraController>();
            }
        }

        private void EnsureLighting()
        {
            if (FindObjectOfType<Light>() != null)
            {
                return;
            }

            var lightObject = new GameObject("Directional Light");
            var lightComponent = lightObject.AddComponent<Light>();
            lightComponent.type = LightType.Directional;
            lightComponent.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(42f, -30f, 0f);
        }

        private void CreateSystems()
        {
            world = gameObject.AddComponent<ProceduralWorldSystem>();
            units = gameObject.AddComponent<UnitsSystem>();
            buildings = gameObject.AddComponent<BuildingsSystem>();
            waves = gameObject.AddComponent<WaveSystem>();
            ui = gameObject.AddComponent<GameUiController>();

            world.Initialize(this);
            units.Initialize(this);
            buildings.Initialize(this);
            waves.Initialize(this);
            ui.Initialize(this);
        }

        private static GameSettings CreateDefaultSettings()
        {
            return new GameSettings
            {
                Seed = 0,
                StartingHumanoids = 4,
                ChunkSize = 10,
                ChunkViewRadius = 3,
                TileWorldSize = 1.6f,
                WaveIntervalSeconds = 28f,
                EnemyIntensity = 1f,
                StartingPlants = 16,
                StartingFlowers = 10,
                StartingRocks = 20,
                StartingMinerals = 8
            };
        }
    }

    public class Inventory
    {
        private readonly System.Collections.Generic.Dictionary<CollectibleType, int> amounts =
            new System.Collections.Generic.Dictionary<CollectibleType, int>();

        public void Reset(int plants, int flowers, int rocks, int minerals)
        {
            amounts[CollectibleType.Plant] = plants;
            amounts[CollectibleType.Flower] = flowers;
            amounts[CollectibleType.Rock] = rocks;
            amounts[CollectibleType.Mineral] = minerals;
        }

        public void Add(CollectibleType type, int amount)
        {
            if (type == CollectibleType.None || amount <= 0)
            {
                return;
            }

            amounts[type] = Get(type) + amount;
        }

        public int Get(CollectibleType type)
        {
            int value;
            return amounts.TryGetValue(type, out value) ? value : 0;
        }

        public bool CanAfford(BuildingDefinition definition)
        {
            return Get(CollectibleType.Plant) >= definition.Plants &&
                   Get(CollectibleType.Flower) >= definition.Flowers &&
                   Get(CollectibleType.Rock) >= definition.Rocks &&
                   Get(CollectibleType.Mineral) >= definition.Minerals;
        }

        public bool Spend(BuildingDefinition definition)
        {
            if (!CanAfford(definition))
            {
                return false;
            }

            amounts[CollectibleType.Plant] = Get(CollectibleType.Plant) - definition.Plants;
            amounts[CollectibleType.Flower] = Get(CollectibleType.Flower) - definition.Flowers;
            amounts[CollectibleType.Rock] = Get(CollectibleType.Rock) - definition.Rocks;
            amounts[CollectibleType.Mineral] = Get(CollectibleType.Mineral) - definition.Minerals;
            return true;
        }
    }

    public class IsometricCameraController : MonoBehaviour
    {
        private Vector3 focusPoint;
        private Camera attachedCamera;

        private const float MinZoom = 8f;
        private const float MaxZoom = 28f;

        private void Awake()
        {
            attachedCamera = GetComponent<Camera>();
            attachedCamera.orthographic = true;
            attachedCamera.orthographicSize = 15f;
            attachedCamera.nearClipPlane = 0.1f;
            attachedCamera.farClipPlane = 300f;
        }

        private void LateUpdate()
        {
            var rotation = Quaternion.Euler(35f, 45f, 0f);
            transform.rotation = rotation;

            var groundForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            var groundRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            var moveSpeed = 16f * Time.unscaledDeltaTime;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                focusPoint += groundForward * moveSpeed;
            }

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                focusPoint -= groundForward * moveSpeed;
            }

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                focusPoint -= groundRight * moveSpeed;
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                focusPoint += groundRight * moveSpeed;
            }

            var scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.001f)
            {
                attachedCamera.orthographicSize = Mathf.Clamp(attachedCamera.orthographicSize - scroll * 1.5f, MinZoom, MaxZoom);
            }

            transform.position = focusPoint - transform.forward * 28f;
        }

        public void FocusOn(Vector3 position)
        {
            focusPoint = position;
        }
    }
}
