#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using OneMoreTurn.Presentation.UI;

namespace OneMoreTurn.Presentation.Editor
{
    /// <summary>
    /// Editor utility to set up the game scene.
    /// </summary>
    public static class SceneSetup
    {
        [MenuItem("One More Turn/Setup Game Scene")]
        public static void SetupScene()
        {
            // Create GameManager
            var gameManagerGO = new GameObject("GameManager");
            gameManagerGO.AddComponent<GameManager>();

            // Create Canvas
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();

            // Create UIManager
            var uiManagerGO = new GameObject("UIManager");
            uiManagerGO.transform.SetParent(canvasGO.transform, false);
            var uiManager = uiManagerGO.AddComponent<UIManager>();

            // Create Main Menu
            var mainMenu = CreateMainMenu(canvasGO.transform);

            // Create Draft UI
            var draftUI = CreateDraftUI(canvasGO.transform);

            // Create Game UI
            var gameUI = CreateGameUI(canvasGO.transform);

            // Create Game Over UI
            var gameOverUI = CreateGameOverUI(canvasGO.transform);

            // Wire up UIManager via SerializedObject
            var so = new SerializedObject(uiManager);
            so.FindProperty("_mainMenuUI").objectReferenceValue = mainMenu;
            so.FindProperty("_draftUI").objectReferenceValue = draftUI;
            so.FindProperty("_gameUI").objectReferenceValue = gameUI;
            so.FindProperty("_gameOverUI").objectReferenceValue = gameOverUI;
            so.ApplyModifiedProperties();

            // Create EventSystem if not exists
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Create Prefabs folder
            if (!AssetDatabase.IsValidFolder("Assets/Game/Presentation/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/Game/Presentation", "Prefabs");
            }

            // Create prefabs
            CreateModifierItemPrefab();
            CreateDraftOptionPrefab();

            Debug.Log("Scene setup complete! Assign prefabs in GameUI and DraftUI inspectors.");
            Selection.activeGameObject = canvasGO;
        }

        private static MainMenuUI CreateMainMenu(Transform parent)
        {
            var go = CreatePanel(parent, "MainMenu", new Color(0.1f, 0.1f, 0.15f, 1f));
            var mainMenu = go.AddComponent<MainMenuUI>();

            // Title
            var title = CreateText(go.transform, "Title", "ONE MORE TURN", 48, TextAnchor.MiddleCenter);
            SetRectTransform(title, new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), Vector2.zero, new Vector2(600, 80));

            // Loading text
            var loading = CreateText(go.transform, "LoadingText", "Loading...", 24, TextAnchor.MiddleCenter);
            SetRectTransform(loading, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 40));

            // Start button
            var startBtn = CreateButton(go.transform, "StartButton", "Start Game", new Color(0.2f, 0.6f, 0.2f, 1f));
            SetRectTransform(startBtn, new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), Vector2.zero, new Vector2(200, 50));

            // Quit button
            var quitBtn = CreateButton(go.transform, "QuitButton", "Quit", new Color(0.6f, 0.2f, 0.2f, 1f));
            SetRectTransform(quitBtn, new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f), Vector2.zero, new Vector2(200, 50));

            // Wire up via SerializedObject
            var so = new SerializedObject(mainMenu);
            so.FindProperty("_titleText").objectReferenceValue = title.GetComponent<Text>();
            so.FindProperty("_loadingText").objectReferenceValue = loading.GetComponent<Text>();
            so.FindProperty("_startGameButton").objectReferenceValue = startBtn.GetComponent<Button>();
            so.FindProperty("_quitButton").objectReferenceValue = quitBtn.GetComponent<Button>();
            so.ApplyModifiedProperties();

            return mainMenu;
        }

        private static DraftUI CreateDraftUI(Transform parent)
        {
            var go = CreatePanel(parent, "DraftUI", new Color(0.1f, 0.12f, 0.18f, 1f));
            go.SetActive(false);
            var draftUI = go.AddComponent<DraftUI>();

            // Title
            var title = CreateText(go.transform, "Title", "Choose Your Modifiers", 36, TextAnchor.MiddleCenter);
            SetRectTransform(title, new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f), Vector2.zero, new Vector2(600, 60));

            // Instructions
            var instructions = CreateText(go.transform, "Instructions", "Select 3 modifiers", 24, TextAnchor.MiddleCenter);
            SetRectTransform(instructions, new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(400, 40));

            // Options container (horizontal layout)
            var container = new GameObject("OptionsContainer");
            container.transform.SetParent(go.transform, false);
            var containerRect = container.AddComponent<RectTransform>();
            SetRectTransform(container, new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f), Vector2.zero, new Vector2(900, 400));
            var layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // Wire up
            var so = new SerializedObject(draftUI);
            so.FindProperty("_titleText").objectReferenceValue = title.GetComponent<Text>();
            so.FindProperty("_instructionText").objectReferenceValue = instructions.GetComponent<Text>();
            so.FindProperty("_optionsContainer").objectReferenceValue = container.transform;
            so.ApplyModifiedProperties();

            return draftUI;
        }

        private static GameUI CreateGameUI(Transform parent)
        {
            var go = CreatePanel(parent, "GameUI", new Color(0.08f, 0.1f, 0.14f, 1f));
            go.SetActive(false);
            var gameUI = go.AddComponent<GameUI>();

            // Top bar - Turn and Scores
            var topBar = new GameObject("TopBar");
            topBar.transform.SetParent(go.transform, false);
            SetRectTransform(topBar, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -50), new Vector2(0, 80));
            topBar.AddComponent<HorizontalLayoutGroup>().padding = new RectOffset(20, 20, 10, 10);

            var turnText = CreateText(topBar.transform, "TurnText", "Turn 1", 28, TextAnchor.MiddleLeft);
            var atRiskText = CreateText(topBar.transform, "AtRiskScore", "At Risk: 0", 24, TextAnchor.MiddleCenter);
            var bankedText = CreateText(topBar.transform, "BankedScore", "Banked: 0", 24, TextAnchor.MiddleCenter);
            var totalText = CreateText(topBar.transform, "TotalScore", "Total: 0", 28, TextAnchor.MiddleRight);

            // Risk meter
            var riskPanel = new GameObject("RiskPanel");
            riskPanel.transform.SetParent(go.transform, false);
            SetRectTransform(riskPanel, new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), Vector2.zero, new Vector2(500, 80));

            var riskSlider = CreateSlider(riskPanel.transform, "RiskSlider");
            SetRectTransform(riskSlider, new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(400, 30));

            var riskText = CreateText(riskPanel.transform, "RiskText", "Risk: 0%", 24, TextAnchor.MiddleCenter);
            SetRectTransform(riskText, new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f), Vector2.zero, new Vector2(200, 30));

            // Push button
            var pushBtn = CreateButton(go.transform, "PushButton", "Push (0/2)", new Color(0.7f, 0.5f, 0.1f, 1f));
            SetRectTransform(pushBtn, new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(180, 45));

            // Turn breakdown
            var breakdownPanel = new GameObject("BreakdownPanel");
            breakdownPanel.transform.SetParent(go.transform, false);
            SetRectTransform(breakdownPanel, new Vector2(0.2f, 0.4f), new Vector2(0.2f, 0.4f), Vector2.zero, new Vector2(250, 150));
            var breakdownLayout = breakdownPanel.AddComponent<VerticalLayoutGroup>();
            breakdownLayout.childAlignment = TextAnchor.UpperLeft;

            var baseGain = CreateText(breakdownPanel.transform, "BaseGain", "Base: +0", 18, TextAnchor.MiddleLeft);
            var pushBonus = CreateText(breakdownPanel.transform, "PushBonus", "Push: x1.0", 18, TextAnchor.MiddleLeft);
            var finalGain = CreateText(breakdownPanel.transform, "FinalGain", "Gain: +0", 20, TextAnchor.MiddleLeft);
            var riskChange = CreateText(breakdownPanel.transform, "RiskChange", "Risk: +0%", 18, TextAnchor.MiddleLeft);

            // Modifiers container (right side)
            var modContainer = new GameObject("ModifierContainer");
            modContainer.transform.SetParent(go.transform, false);
            SetRectTransform(modContainer, new Vector2(0.85f, 0.5f), new Vector2(0.85f, 0.5f), Vector2.zero, new Vector2(250, 400));
            var modLayout = modContainer.AddComponent<VerticalLayoutGroup>();
            modLayout.spacing = 10;
            modLayout.childForceExpandHeight = false;

            // Bottom buttons
            var btnPanel = new GameObject("ButtonPanel");
            btnPanel.transform.SetParent(go.transform, false);
            SetRectTransform(btnPanel, new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(700, 120));
            var btnLayout = btnPanel.AddComponent<HorizontalLayoutGroup>();
            btnLayout.spacing = 15;
            btnLayout.childAlignment = TextAnchor.MiddleCenter;

            var bank25 = CreateButton(btnPanel.transform, "Bank25", "Bank 25%", new Color(0.3f, 0.3f, 0.5f, 1f));
            var bank50 = CreateButton(btnPanel.transform, "Bank50", "Bank 50%", new Color(0.3f, 0.3f, 0.5f, 1f));
            var oneMore = CreateButton(btnPanel.transform, "OneMoreTurn", "ONE MORE TURN", new Color(0.2f, 0.5f, 0.2f, 1f));
            var cashOut = CreateButton(btnPanel.transform, "CashOut", "CASH OUT", new Color(0.5f, 0.4f, 0.1f, 1f));

            // Wire up GameUI
            var so = new SerializedObject(gameUI);
            so.FindProperty("_turnText").objectReferenceValue = turnText.GetComponent<Text>();
            so.FindProperty("_atRiskScoreText").objectReferenceValue = atRiskText.GetComponent<Text>();
            so.FindProperty("_bankedScoreText").objectReferenceValue = bankedText.GetComponent<Text>();
            so.FindProperty("_totalScoreText").objectReferenceValue = totalText.GetComponent<Text>();
            so.FindProperty("_riskSlider").objectReferenceValue = riskSlider.GetComponent<Slider>();
            so.FindProperty("_riskText").objectReferenceValue = riskText.GetComponent<Text>();
            so.FindProperty("_pushButton").objectReferenceValue = pushBtn.GetComponent<Button>();
            so.FindProperty("_pushButtonText").objectReferenceValue = pushBtn.GetComponentInChildren<Text>();
            so.FindProperty("_breakdownPanel").objectReferenceValue = breakdownPanel;
            so.FindProperty("_baseGainText").objectReferenceValue = baseGain.GetComponent<Text>();
            so.FindProperty("_pushBonusText").objectReferenceValue = pushBonus.GetComponent<Text>();
            so.FindProperty("_finalGainText").objectReferenceValue = finalGain.GetComponent<Text>();
            so.FindProperty("_riskChangeText").objectReferenceValue = riskChange.GetComponent<Text>();
            so.FindProperty("_modifierContainer").objectReferenceValue = modContainer.transform;
            so.FindProperty("_bank25Button").objectReferenceValue = bank25.GetComponent<Button>();
            so.FindProperty("_bank50Button").objectReferenceValue = bank50.GetComponent<Button>();
            so.FindProperty("_oneMoreTurnButton").objectReferenceValue = oneMore.GetComponent<Button>();
            so.FindProperty("_cashOutButton").objectReferenceValue = cashOut.GetComponent<Button>();
            so.ApplyModifiedProperties();

            return gameUI;
        }

        private static GameOverUI CreateGameOverUI(Transform parent)
        {
            var go = CreatePanel(parent, "GameOverUI", new Color(0, 0, 0, 0.85f));
            go.SetActive(false);
            var gameOverUI = go.AddComponent<GameOverUI>();

            // Background image for color changes
            var bg = go.GetComponent<Image>();

            // Title
            var title = CreateText(go.transform, "Title", "GAME OVER", 48, TextAnchor.MiddleCenter);
            SetRectTransform(title, new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), Vector2.zero, new Vector2(500, 80));

            // Message
            var message = CreateText(go.transform, "Message", "", 24, TextAnchor.MiddleCenter);
            SetRectTransform(message, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(600, 60));

            // Score
            var score = CreateText(go.transform, "Score", "Final Score: 0", 36, TextAnchor.MiddleCenter);
            SetRectTransform(score, new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.4f), Vector2.zero, new Vector2(400, 60));

            // Buttons
            var playAgain = CreateButton(go.transform, "PlayAgain", "Play Again", new Color(0.2f, 0.5f, 0.2f, 1f));
            SetRectTransform(playAgain, new Vector2(0.4f, 0.2f), new Vector2(0.4f, 0.2f), Vector2.zero, new Vector2(180, 50));

            var mainMenuBtn = CreateButton(go.transform, "MainMenu", "Main Menu", new Color(0.4f, 0.4f, 0.4f, 1f));
            SetRectTransform(mainMenuBtn, new Vector2(0.6f, 0.2f), new Vector2(0.6f, 0.2f), Vector2.zero, new Vector2(180, 50));

            // Wire up
            var so = new SerializedObject(gameOverUI);
            so.FindProperty("_titleText").objectReferenceValue = title.GetComponent<Text>();
            so.FindProperty("_messageText").objectReferenceValue = message.GetComponent<Text>();
            so.FindProperty("_scoreText").objectReferenceValue = score.GetComponent<Text>();
            so.FindProperty("_backgroundImage").objectReferenceValue = bg;
            so.FindProperty("_playAgainButton").objectReferenceValue = playAgain.GetComponent<Button>();
            so.FindProperty("_mainMenuButton").objectReferenceValue = mainMenuBtn.GetComponent<Button>();
            so.ApplyModifiedProperties();

            return gameOverUI;
        }

        private static void CreateModifierItemPrefab()
        {
            var go = new GameObject("ModifierItem");
            var item = go.AddComponent<ModifierItemUI>();

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(230, 80);

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 5, 5);
            layout.childForceExpandHeight = false;

            // Name
            var nameText = CreateText(go.transform, "Name", "Modifier Name", 16, TextAnchor.MiddleLeft);

            // Description
            var descText = CreateText(go.transform, "Description", "Description", 12, TextAnchor.MiddleLeft);
            descText.GetComponent<Text>().color = new Color(0.7f, 0.7f, 0.7f);

            // Sacrifice buttons (horizontal)
            var btnRow = new GameObject("SacrificeButtons");
            btnRow.transform.SetParent(go.transform, false);
            var btnLayout = btnRow.AddComponent<HorizontalLayoutGroup>();
            btnLayout.spacing = 5;

            var sacRisk = CreateButton(btnRow.transform, "SacRisk", "-10%", new Color(0.3f, 0.5f, 0.3f, 1f), 12);
            var sacScore = CreateButton(btnRow.transform, "SacScore", "+50", new Color(0.5f, 0.5f, 0.3f, 1f), 12);

            // Wire up
            var so = new SerializedObject(item);
            so.FindProperty("_nameText").objectReferenceValue = nameText.GetComponent<Text>();
            so.FindProperty("_descriptionText").objectReferenceValue = descText.GetComponent<Text>();
            so.FindProperty("_sacrificeRiskButton").objectReferenceValue = sacRisk.GetComponent<Button>();
            so.FindProperty("_sacrificeScoreButton").objectReferenceValue = sacScore.GetComponent<Button>();
            so.FindProperty("_sacrificeRiskText").objectReferenceValue = sacRisk.GetComponentInChildren<Text>();
            so.FindProperty("_sacrificeScoreText").objectReferenceValue = sacScore.GetComponentInChildren<Text>();
            so.ApplyModifiedProperties();

            // Save prefab
            PrefabUtility.SaveAsPrefabAsset(go, "Assets/Game/Presentation/Prefabs/ModifierItem.prefab");
            Object.DestroyImmediate(go);
        }

        private static void CreateDraftOptionPrefab()
        {
            var go = new GameObject("DraftOption");
            var option = go.AddComponent<DraftOptionUI>();

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.25f, 1f);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(170, 350);

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 15, 15);
            layout.spacing = 10;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperCenter;

            // Rarity
            var rarityText = CreateText(go.transform, "Rarity", "Common", 14, TextAnchor.MiddleCenter);

            // Name
            var nameText = CreateText(go.transform, "Name", "Modifier", 18, TextAnchor.MiddleCenter);

            // Description
            var descText = CreateText(go.transform, "Description", "Description goes here", 14, TextAnchor.UpperCenter);
            var descRect = descText.GetComponent<RectTransform>();
            descRect.sizeDelta = new Vector2(150, 150);
            descText.GetComponent<Text>().color = new Color(0.8f, 0.8f, 0.8f);

            // Select button
            var selectBtn = CreateButton(go.transform, "Select", "SELECT", new Color(0.2f, 0.5f, 0.2f, 1f));

            // Wire up
            var so = new SerializedObject(option);
            so.FindProperty("_nameText").objectReferenceValue = nameText.GetComponent<Text>();
            so.FindProperty("_descriptionText").objectReferenceValue = descText.GetComponent<Text>();
            so.FindProperty("_rarityText").objectReferenceValue = rarityText.GetComponent<Text>();
            so.FindProperty("_background").objectReferenceValue = bg;
            so.FindProperty("_selectButton").objectReferenceValue = selectBtn.GetComponent<Button>();
            so.ApplyModifiedProperties();

            // Save prefab
            PrefabUtility.SaveAsPrefabAsset(go, "Assets/Game/Presentation/Prefabs/DraftOption.prefab");
            Object.DestroyImmediate(go);
        }

        #region Helpers

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private static GameObject CreateText(Transform parent, string name, string text, int fontSize, TextAnchor alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.fontSize = fontSize;
            txt.alignment = alignment;
            txt.color = Color.white;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return go;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Color color, int fontSize = 18)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150, 40);

            var img = go.AddComponent<Image>();
            img.color = color;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var textGo = CreateText(go.transform, "Text", label, fontSize, TextAnchor.MiddleCenter);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return go;
        }

        private static GameObject CreateSlider(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();

            // Background
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(go.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f);

            // Fill area
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            // Fill
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillArea.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = new Color(0.8f, 0.3f, 0.3f);

            var slider = go.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.interactable = false;

            return go;
        }

        private static void SetRectTransform(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var rect = go.GetComponent<RectTransform>();
            if (rect == null) rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;
        }

        #endregion
    }
}
#endif
