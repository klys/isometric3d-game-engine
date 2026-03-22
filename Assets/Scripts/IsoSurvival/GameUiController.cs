using UnityEngine;
using UnityEngine.UI;

namespace IsoSurvival
{
    public class GameUiController : MonoBehaviour
    {
        private GameController controller;
        private Canvas canvas;
        private Font uiFont;

        private GameObject mainMenuPanel;
        private GameObject settingsPanel;
        private GameObject hudPanel;
        private GameObject pausePanel;
        private GameObject gameOverPanel;

        private Text inventoryText;
        private Text statusText;
        private Text settingsText;
        private Text selectionText;

        private float refreshTimer;

        public void Initialize(GameController gameController)
        {
            controller = gameController;
        }

        private void Update()
        {
            if (canvas == null)
            {
                return;
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f)
            {
                return;
            }

            refreshTimer = 0.15f;
            RefreshHud();
            RefreshSettings();
        }

        public void Build()
        {
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            var canvasObject = new GameObject("UI", typeof(RectTransform));
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasObject);

            mainMenuPanel = CreateFullScreenPanel("Main Menu", new Color(0.06f, 0.11f, 0.16f, 0.92f));
            settingsPanel = CreateWindow("Settings", new Vector2(460f, 260f), new Color(0.12f, 0.16f, 0.2f, 0.96f));
            hudPanel = CreateTransparentPanel("HUD");
            pausePanel = CreateFullScreenPanel("Pause", new Color(0f, 0f, 0f, 0.55f));
            gameOverPanel = CreateFullScreenPanel("Game Over", new Color(0.12f, 0f, 0f, 0.72f));

            BuildMainMenu();
            BuildSettings();
            BuildHud();
            BuildPause();
            BuildGameOver();
        }

        public void ShowMainMenu()
        {
            mainMenuPanel.SetActive(true);
            settingsPanel.SetActive(false);
            hudPanel.SetActive(false);
            pausePanel.SetActive(false);
            gameOverPanel.SetActive(false);
        }

        public void ShowHud()
        {
            mainMenuPanel.SetActive(false);
            settingsPanel.SetActive(false);
            hudPanel.SetActive(true);
            pausePanel.SetActive(false);
            gameOverPanel.SetActive(false);
        }

        public void SetPauseVisible(bool visible)
        {
            pausePanel.SetActive(visible);
        }

        public void ShowGameOver()
        {
            pausePanel.SetActive(false);
            gameOverPanel.SetActive(true);
        }

        public void RefreshHud()
        {
            if (inventoryText == null || controller == null)
            {
                return;
            }

            inventoryText.text =
                "Inventory\n" +
                "Plants: " + controller.Inventory.Get(CollectibleType.Plant) + "\n" +
                "Flowers: " + controller.Inventory.Get(CollectibleType.Flower) + "\n" +
                "Rocks: " + controller.Inventory.Get(CollectibleType.Rock) + "\n" +
                "Minerals: " + controller.Inventory.Get(CollectibleType.Mineral);

            var buildText = controller.Buildings.HasPendingPlacement
                ? "Build: " + controller.Buildings.PendingBuildingType.Value
                : "Build: none";

            statusText.text =
                "Wave " + controller.CurrentWave + "\n" +
                buildText + "\n" +
                "Humanoids alive: " + controller.Units.AliveCount;

            selectionText.text =
                "Controls\n" +
                "Left click: select\n" +
                "Shift + left click: multi-select\n" +
                "Right click: move / cancel build\n" +
                "1 House  2 Wall  3 Tower  Tab Cancel\n" +
                "WASD / Arrows move camera, wheel zoom, Esc pause";
        }

        public void RefreshSettings()
        {
            if (settingsText == null || controller == null)
            {
                return;
            }

            settingsText.text =
                "Seed: " + controller.MenuSettings.Seed + "\n" +
                "Starting humanoids: " + controller.MenuSettings.StartingHumanoids + "\n" +
                "Enemy intensity: " + controller.MenuSettings.EnemyIntensity.ToString("0.0") + "x\n" +
                "Wave interval: " + controller.MenuSettings.WaveIntervalSeconds.ToString("0") + "s";
        }

        private void BuildMainMenu()
        {
            var title = CreateText(mainMenuPanel.transform, "Iso Survival Engine", 42, TextAnchor.MiddleCenter);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 0.72f);
            titleRect.anchorMax = new Vector2(0.5f, 0.72f);
            titleRect.sizeDelta = new Vector2(500f, 80f);
            titleRect.anchoredPosition = Vector2.zero;

            CreateMenuButton(mainMenuPanel.transform, "Play", new Vector2(0f, 20f), controller.StartGame);
            CreateMenuButton(mainMenuPanel.transform, "Settings", new Vector2(0f, -50f), ToggleSettings);
            CreateMenuButton(mainMenuPanel.transform, "Exit", new Vector2(0f, -120f), controller.ExitGame);
        }

        private void BuildSettings()
        {
            settingsText = CreateText(settingsPanel.transform, string.Empty, 20, TextAnchor.UpperLeft);
            settingsText.rectTransform.anchorMin = new Vector2(0.08f, 0.3f);
            settingsText.rectTransform.anchorMax = new Vector2(0.6f, 0.88f);
            settingsText.rectTransform.offsetMin = Vector2.zero;
            settingsText.rectTransform.offsetMax = Vector2.zero;

            CreateSmallButton(settingsPanel.transform, "Seed", new Vector2(120f, 70f), controller.RandomizeSeed);
            CreateSmallButton(settingsPanel.transform, "Hum +", new Vector2(120f, 20f), delegate { controller.AdjustStartingHumanoids(1); });
            CreateSmallButton(settingsPanel.transform, "Hum -", new Vector2(250f, 20f), delegate { controller.AdjustStartingHumanoids(-1); });
            CreateSmallButton(settingsPanel.transform, "Diff +", new Vector2(120f, -30f), delegate { controller.AdjustEnemyIntensity(0.25f); });
            CreateSmallButton(settingsPanel.transform, "Diff -", new Vector2(250f, -30f), delegate { controller.AdjustEnemyIntensity(-0.25f); });
            CreateSmallButton(settingsPanel.transform, "Wave +", new Vector2(120f, -80f), delegate { controller.AdjustWaveInterval(5f); });
            CreateSmallButton(settingsPanel.transform, "Wave -", new Vector2(250f, -80f), delegate { controller.AdjustWaveInterval(-5f); });
            CreateSmallButton(settingsPanel.transform, "Close", new Vector2(185f, -125f), ToggleSettings);
        }

        private void BuildHud()
        {
            inventoryText = CreateText(hudPanel.transform, string.Empty, 18, TextAnchor.UpperLeft);
            inventoryText.rectTransform.anchorMin = new Vector2(0.79f, 0.62f);
            inventoryText.rectTransform.anchorMax = new Vector2(0.98f, 0.95f);
            inventoryText.rectTransform.offsetMin = Vector2.zero;
            inventoryText.rectTransform.offsetMax = Vector2.zero;

            statusText = CreateText(hudPanel.transform, string.Empty, 20, TextAnchor.UpperLeft);
            statusText.rectTransform.anchorMin = new Vector2(0.02f, 0.72f);
            statusText.rectTransform.anchorMax = new Vector2(0.22f, 0.95f);
            statusText.rectTransform.offsetMin = Vector2.zero;
            statusText.rectTransform.offsetMax = Vector2.zero;

            selectionText = CreateText(hudPanel.transform, string.Empty, 16, TextAnchor.LowerLeft);
            selectionText.rectTransform.anchorMin = new Vector2(0.02f, 0.03f);
            selectionText.rectTransform.anchorMax = new Vector2(0.38f, 0.24f);
            selectionText.rectTransform.offsetMin = Vector2.zero;
            selectionText.rectTransform.offsetMax = Vector2.zero;

            CreateHudButton("House", new Vector2(0.44f, 0.06f), delegate { controller.Buildings.QueuePlacement(BuildingType.House); });
            CreateHudButton("Wall", new Vector2(0.54f, 0.06f), delegate { controller.Buildings.QueuePlacement(BuildingType.Wall); });
            CreateHudButton("Tower", new Vector2(0.64f, 0.06f), delegate { controller.Buildings.QueuePlacement(BuildingType.Tower); });
            CreateHudButton("Cancel", new Vector2(0.74f, 0.06f), controller.Buildings.CancelPlacement);
        }

        private void BuildPause()
        {
            var pauseText = CreateText(pausePanel.transform, "Paused", 40, TextAnchor.MiddleCenter);
            pauseText.rectTransform.anchorMin = new Vector2(0.5f, 0.65f);
            pauseText.rectTransform.anchorMax = new Vector2(0.5f, 0.65f);
            pauseText.rectTransform.sizeDelta = new Vector2(260f, 60f);

            CreateMenuButton(pausePanel.transform, "Resume", new Vector2(0f, -20f), delegate { controller.SetPaused(false); });
            CreateMenuButton(pausePanel.transform, "Main Menu", new Vector2(0f, -90f), controller.ReturnToMainMenu);
        }

        private void BuildGameOver()
        {
            var title = CreateText(gameOverPanel.transform, "Game Over", 44, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0.5f, 0.66f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 0.66f);
            title.rectTransform.sizeDelta = new Vector2(320f, 70f);

            var subtitle = CreateText(gameOverPanel.transform, "All humanoids were lost.", 24, TextAnchor.MiddleCenter);
            subtitle.rectTransform.anchorMin = new Vector2(0.5f, 0.57f);
            subtitle.rectTransform.anchorMax = new Vector2(0.5f, 0.57f);
            subtitle.rectTransform.sizeDelta = new Vector2(420f, 44f);

            CreateMenuButton(gameOverPanel.transform, "Back To Menu", new Vector2(0f, -60f), controller.ReturnToMainMenu);
        }

        private void ToggleSettings()
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }

        private GameObject CreateTransparentPanel(string panelName)
        {
            var panel = new GameObject(panelName, typeof(RectTransform));
            panel.transform.SetParent(canvas.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return panel;
        }

        private GameObject CreateFullScreenPanel(string panelName, Color color)
        {
            var panel = CreateTransparentPanel(panelName);
            var image = panel.AddComponent<Image>();
            image.color = color;
            return panel;
        }

        private GameObject CreateWindow(string panelName, Vector2 size, Color color)
        {
            var panel = new GameObject(panelName, typeof(RectTransform));
            panel.transform.SetParent(canvas.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            var image = panel.AddComponent<Image>();
            image.color = color;
            panel.SetActive(false);
            return panel;
        }

        private void CreateMenuButton(Transform parent, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
        {
            var button = CreateButton(parent, label, new Vector2(220f, 56f), anchoredPosition, 24);
            button.onClick.AddListener(action);
        }

        private void CreateSmallButton(Transform parent, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
        {
            var button = CreateButton(parent, label, new Vector2(110f, 36f), anchoredPosition, 18);
            button.onClick.AddListener(action);
        }

        private void CreateHudButton(string label, Vector2 anchor, UnityEngine.Events.UnityAction action)
        {
            var buttonObject = new GameObject(label + " Button", typeof(RectTransform));
            buttonObject.transform.SetParent(hudPanel.transform, false);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(88f, 36f);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.1f, 0.16f, 0.22f, 0.88f);

            var button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(action);

            var labelText = CreateText(buttonObject.transform, label, 16, TextAnchor.MiddleCenter);
            labelText.rectTransform.anchorMin = Vector2.zero;
            labelText.rectTransform.anchorMax = Vector2.one;
            labelText.rectTransform.offsetMin = Vector2.zero;
            labelText.rectTransform.offsetMax = Vector2.zero;
        }

        private Button CreateButton(Transform parent, string label, Vector2 size, Vector2 anchoredPosition, int fontSize)
        {
            var buttonObject = new GameObject(label + " Button", typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.16f, 0.25f, 0.34f, 0.95f);

            var button = buttonObject.AddComponent<Button>();

            var labelText = CreateText(buttonObject.transform, label, fontSize, TextAnchor.MiddleCenter);
            labelText.rectTransform.anchorMin = Vector2.zero;
            labelText.rectTransform.anchorMax = Vector2.one;
            labelText.rectTransform.offsetMin = Vector2.zero;
            labelText.rectTransform.offsetMax = Vector2.zero;

            return button;
        }

        private Text CreateText(Transform parent, string content, int fontSize, TextAnchor alignment)
        {
            var textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<Text>();
            text.font = uiFont;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.text = content;
            return text;
        }
    }
}
