using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class UiPrefabMigration
{
    public const string GameplayCanvasPath = "Assets/Prefabs/UI/UI_GameplayCanvas.prefab";
    public const string MainMenuCanvasPath = "Assets/Prefabs/UI/UI_MainMenuCanvas.prefab";
    public const string EventSystemPath = "Assets/Prefabs/UI/UI_EventSystem.prefab";

    private const string InventorySlotPath = "Assets/Prefabs/UI/UI_PlaceableInventorySlot.prefab";
    private const string LevelSelectSlotPath = "Assets/Prefabs/UI/UI_LevelSelectSlot.prefab";
    private const string HudPath = "Assets/Prefabs/UI/UI_LevelHudPanel.prefab";
    private const string InventoryPath = "Assets/Prefabs/UI/UI_PlaceableInventoryPanel.prefab";
    private const string PausePath = "Assets/Prefabs/UI/PausedMenu.prefab";
    private const string VictoryPath = "Assets/Prefabs/UI/UI_VictoryScreen.prefab";
    private const string DefeatPath = "Assets/Prefabs/UI/UI_DefeatScreen.prefab";
    private const string MainBackgroundPath = "Assets/Prefabs/UI/Main Menu/UI_MainMenuBackground.prefab";
    private const string SplashPath = "Assets/Prefabs/UI/Main Menu/UI_SplashPanel.prefab";
    private const string MainPanelPath = "Assets/Prefabs/UI/Main Menu/UI_MainMenuPanel.prefab";
    private const string LevelSelectPath = "Assets/Prefabs/UI/Main Menu/UI_LevelSelectPanel.prefab";
    private const string OptionsPath = "Assets/Prefabs/UI/Main Menu/UI_OptionsPanel.prefab";
    private const string CreditsPath = "Assets/Prefabs/UI/Main Menu/UI_CreditsPanel.prefab";
    private const string PauseIconPath = "Assets/Art/UI/Icons/pause.png";
    private const string ResetIconPath = "Assets/Art/UI/Icons/reset.png";
    private const string MainLevelCatalogPath = "Assets/ScriptableObjects/Level Catalogs/MainLevelCatalog.asset";

    private static readonly Color SkyTop = new Color(0.47f, 0.82f, 1f, 1f);
    private static readonly Color SkyBottom = new Color(0.69f, 0.91f, 1f, 1f);
    private static readonly Color Grass = new Color(0.39f, 0.82f, 0.28f, 1f);
    private static readonly Color Panel = new Color(1f, 0.97f, 0.89f, 0.96f);
    private static readonly Color Ink = new Color(0.13f, 0.16f, 0.14f, 1f);
    private static readonly Color Accent = new Color(1f, 0.76f, 0.25f, 1f);
    private static readonly Color Green = new Color(0.24f, 0.72f, 0.38f, 1f);
    private static readonly Color Red = new Color(0.9f, 0.45f, 0.36f, 1f);
    private static readonly Color HudDark = new Color(0.08f, 0.1f, 0.09f, 0.88f);

    [MenuItem("Unlucky Ducky/UI/Generate Prefabs and Migrate Scenes")]
    public static void GenerateAllAndMigrate()
    {
        GenerateAllPrefabs();
        MigrateAllScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("UI prefab generation and scene migration completed.");
    }

    [MenuItem("Unlucky Ducky/UI/Generate Prefabs")]
    public static void GenerateAllPrefabs()
    {
        EnsureFolder("Assets/Prefabs/UI/Main Menu");
        EnsureFolder("Assets/Art/UI/Icons");
        GenerateIconAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        BuildInventorySlotPrefab();
        BuildLevelSelectSlotPrefab();
        BuildHudPrefab();
        BuildInventoryPrefab();
        BuildPausePrefab();
        BuildVictoryPrefab();
        BuildDefeatPrefab();
        BuildMainMenuBackgroundPrefab();
        BuildSplashPrefab();
        BuildMainPanelPrefab();
        BuildLevelSelectPrefab();
        BuildOptionsPrefab();
        BuildCreditsPrefab();
        BuildEventSystemPrefab();
        BuildGameplayCanvasPrefab();
        BuildMainMenuCanvasPrefab();
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Unlucky Ducky/UI/Migrate All Scenes")]
    public static void MigrateAllScenes()
    {
        GameObject gameplayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayCanvasPath);
        GameObject mainMenuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainMenuCanvasPath);
        GameObject eventSystemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EventSystemPath);

        if (gameplayPrefab == null || mainMenuPrefab == null || eventSystemPrefab == null)
        {
            throw new InvalidOperationException("Generate the UI prefabs before migrating scenes.");
        }

        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });

        for (int i = 0; i < sceneGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);

            if (path.Contains("/Settings/", StringComparison.Ordinal)
                || path.Contains("SceneTemplate", StringComparison.Ordinal))
            {
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            if (string.Equals(path, "Assets/Scenes/MainMenuScene.unity", StringComparison.Ordinal))
            {
                MigrateMainMenuScene(scene, mainMenuPrefab, eventSystemPrefab);
            }
            else if (path.EndsWith("Test_Scene.unity", StringComparison.Ordinal)
                || path.Contains("/World ", StringComparison.Ordinal))
            {
                MigrateGameplayScene(scene, gameplayPrefab, eventSystemPrefab);
            }

            EditorSceneManager.SaveScene(scene);
        }
    }

    private static void BuildInventorySlotPrefab()
    {
        GameObject root = CreateUiObject(
            "UI_PlaceableInventorySlot",
            null,
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement),
            typeof(HorizontalLayoutGroup),
            typeof(PlaceableInventorySlotView));

        Image background = root.GetComponent<Image>();
        background.color = Color.white;
        Button button = root.GetComponent<Button>();
        button.targetGraphic = background;
        LayoutElement slotLayout = root.GetComponent<LayoutElement>();
        slotLayout.preferredHeight = 92f;
        slotLayout.minHeight = 92f;

        HorizontalLayoutGroup contentLayout = root.GetComponent<HorizontalLayoutGroup>();
        contentLayout.padding = new RectOffset(10, 10, 8, 8);
        contentLayout.spacing = 10f;
        contentLayout.childAlignment = TextAnchor.MiddleCenter;
        contentLayout.childControlHeight = true;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandHeight = true;
        contentLayout.childForceExpandWidth = false;

        GameObject iconObject = CreateUiObject("Icon", root.transform, typeof(Image), typeof(LayoutElement));
        Image icon = iconObject.GetComponent<Image>();
        icon.preserveAspect = true;
        LayoutElement iconLayout = iconObject.GetComponent<LayoutElement>();
        iconLayout.minWidth = 68f;
        iconLayout.preferredWidth = 68f;
        iconLayout.preferredHeight = 68f;
        iconLayout.flexibleWidth = 0f;

        GameObject textBlockObject = CreateUiObject("Text", root.transform, typeof(VerticalLayoutGroup), typeof(LayoutElement));
        VerticalLayoutGroup textBlock = textBlockObject.GetComponent<VerticalLayoutGroup>();
        textBlock.spacing = 4f;
        textBlock.childAlignment = TextAnchor.MiddleLeft;
        textBlock.childControlHeight = true;
        textBlock.childControlWidth = true;
        textBlock.childForceExpandHeight = false;
        textBlock.childForceExpandWidth = true;
        LayoutElement textBlockLayout = textBlockObject.GetComponent<LayoutElement>();
        textBlockLayout.minWidth = 72f;
        textBlockLayout.preferredWidth = 88f;
        textBlockLayout.flexibleWidth = 1f;

        Text nameText = CreateLegacyText(textBlockObject.transform, "Name", "Objeto", 12, FontStyle.Bold, TextAnchor.LowerLeft);
        LayoutElement nameLayout = nameText.gameObject.AddComponent<LayoutElement>();
        nameLayout.preferredHeight = 22f;
        nameLayout.flexibleWidth = 1f;
        Text amountText = CreateLegacyText(textBlockObject.transform, "Amount", "1", 18, FontStyle.Normal, TextAnchor.UpperLeft);
        LayoutElement amountLayout = amountText.gameObject.AddComponent<LayoutElement>();
        amountLayout.preferredHeight = 28f;
        amountLayout.flexibleWidth = 1f;

        PlaceableInventorySlotView view = root.GetComponent<PlaceableInventorySlotView>();
        SetObject(view, "background", background);
        SetObject(view, "button", button);
        SetObject(view, "slotLayout", slotLayout);
        SetObject(view, "contentLayout", contentLayout);
        SetObject(view, "icon", icon);
        SetObject(view, "iconLayout", iconLayout);
        SetObject(view, "textBlockLayout", textBlockLayout);
        SetObject(view, "textBlock", textBlock);
        SetObject(view, "nameText", nameText);
        SetObject(view, "nameLayout", nameLayout);
        SetObject(view, "amountText", amountText);
        SetObject(view, "amountLayout", amountLayout);
        SavePrefab(root, InventorySlotPath);
    }

    private static void BuildLevelSelectSlotPrefab()
    {
        GameObject root = CreateUiObject(
            "UI_LevelSelectSlot",
            null,
            typeof(Image),
            typeof(Button),
            typeof(LevelSelectSlotView));
        Image image = root.GetComponent<Image>();
        image.color = new Color(1f, 0.96f, 0.84f, 1f);
        Button button = root.GetComponent<Button>();
        button.targetGraphic = image;
        TextMeshProUGUI label = CreateTmpText(
            root.transform,
            "Label",
            "1",
            30f,
            FontStyles.Bold,
            Ink,
            TextAlignmentOptions.Center,
            false);
        Stretch(label.rectTransform);
        label.rectTransform.offsetMin = new Vector2(10f, 8f);
        label.rectTransform.offsetMax = new Vector2(-10f, -8f);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;

        LevelSelectSlotView view = root.GetComponent<LevelSelectSlotView>();
        SetObject(view, "background", image);
        SetObject(view, "button", button);
        SetObject(view, "label", label);
        SavePrefab(root, LevelSelectSlotPath);
    }

    private static void BuildHudPrefab()
    {
        Sprite pauseIcon = AssetDatabase.LoadAssetAtPath<Sprite>(PauseIconPath);
        Sprite resetIcon = AssetDatabase.LoadAssetAtPath<Sprite>(ResetIconPath);
        GameObject root = CreateUiObject("UI_LevelHudPanel", null, typeof(LevelHudPanel));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = new Vector2(0f, 72f);

        TextMeshProUGUI title = CreateTmpText(
            root.transform,
            "Text (TMP)",
            "Mundo XX - Nivel YY",
            36f,
            FontStyles.Normal,
            Color.white,
            TextAlignmentOptions.Center,
            false);
        Place(title.rectTransform, new Vector2(-713f, 1f), new Vector2(375f, 50f));

        TextMeshProUGUI timer = CreateTmpText(
            root.transform,
            "PlanningTimerText",
            string.Empty,
            34f,
            FontStyles.Bold,
            Color.white,
            TextAlignmentOptions.Center,
            false);
        Place(timer.rectTransform, Vector2.zero, new Vector2(160f, 44f));
        timer.gameObject.SetActive(false);

        Button pause = CreateHudButton(root.transform, "PauseButton", new Vector2(-76f, -18f), pauseIcon);
        Button reset = CreateHudButton(root.transform, "ResetLevelButton", new Vector2(-18f, -18f), resetIcon);
        ResetLevelButtonController resetController = reset.gameObject.AddComponent<ResetLevelButtonController>();

        GameObject tooltipObject = CreateUiObject("HudTooltip", root.transform, typeof(Image));
        RectTransform tooltipRect = tooltipObject.GetComponent<RectTransform>();
        tooltipRect.anchorMin = Vector2.one;
        tooltipRect.anchorMax = Vector2.one;
        tooltipRect.pivot = Vector2.one;
        tooltipRect.sizeDelta = new Vector2(210f, 34f);
        tooltipRect.anchoredPosition = new Vector2(-18f, -76f);
        Image tooltipBackground = tooltipObject.GetComponent<Image>();
        tooltipBackground.color = new Color(0.08f, 0.1f, 0.09f, 0.92f);
        tooltipBackground.raycastTarget = false;
        TextMeshProUGUI tooltip = CreateTmpText(
            tooltipObject.transform,
            "Label",
            string.Empty,
            18f,
            FontStyles.Bold,
            Color.white,
            TextAlignmentOptions.Center,
            false);
        Stretch(tooltip.rectTransform);
        tooltipObject.SetActive(false);

        LevelHudPanel hud = root.GetComponent<LevelHudPanel>();
        SetObject(hud, "levelTitleText", title);
        SetObject(hud, "planningTimerText", timer);
        SetObject(hud, "tooltipRoot", tooltipRect);
        SetObject(hud, "tooltipText", tooltip);
        SetObject(hud, "pauseButton", pause);
        SetObject(hud, "resetLevelButton", resetController);

        ConfigureTooltipSource(pause.gameObject, hud, "Pausar el juego");
        ConfigureTooltipSource(reset.gameObject, hud, "Reiniciar el nivel");
        SavePrefab(root, HudPath);
    }

    private static void BuildInventoryPrefab()
    {
        PlaceableInventorySlotView slotPrefab = AssetDatabase
            .LoadAssetAtPath<GameObject>(InventorySlotPath)
            .GetComponent<PlaceableInventorySlotView>();
        GameObject root = CreateUiObject(
            "UI_PlaceableInventoryPanel",
            null,
            typeof(Image),
            typeof(VerticalLayoutGroup),
            typeof(PlaceableInventoryPanel));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(1f, 0.5f);
        rootRect.anchorMax = new Vector2(1f, 0.5f);
        rootRect.pivot = new Vector2(1f, 0.5f);
        rootRect.anchoredPosition = new Vector2(-18f, 0f);
        rootRect.sizeDelta = new Vector2(220f, 640f);

        Image background = root.GetComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.92f);
        VerticalLayoutGroup panelLayout = root.GetComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(10, 10, 10, 10);
        panelLayout.spacing = 8f;
        panelLayout.childControlHeight = true;
        panelLayout.childControlWidth = true;
        panelLayout.childForceExpandHeight = false;
        panelLayout.childForceExpandWidth = true;

        Text title = CreateLegacyText(
            root.transform,
            "Title",
            "OBJETOS DISPONIBLES",
            14,
            FontStyle.Bold,
            TextAnchor.MiddleLeft);
        LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 24f;

        GameObject slotsObject = CreateUiObject("Slots", root.transform, typeof(VerticalLayoutGroup), typeof(LayoutElement));
        VerticalLayoutGroup slotsLayout = slotsObject.GetComponent<VerticalLayoutGroup>();
        slotsLayout.spacing = 8f;
        slotsLayout.childControlHeight = true;
        slotsLayout.childControlWidth = true;
        slotsLayout.childForceExpandHeight = false;
        slotsLayout.childForceExpandWidth = true;
        LayoutElement slotsElement = slotsObject.GetComponent<LayoutElement>();
        slotsElement.minHeight = 0f;
        slotsElement.preferredHeight = 0f;
        slotsElement.flexibleHeight = 1f;

        Button startButton = CreateLegacyButton(root.transform, "StartExecutionButton", "PROBAR NIVEL", Color.white, 12);
        LayoutElement startLayout = startButton.gameObject.AddComponent<LayoutElement>();
        startLayout.preferredHeight = 40f;
        StartExecutionButtonController startController = startButton.gameObject.AddComponent<StartExecutionButtonController>();

        PlaceableInventoryPanel panel = root.GetComponent<PlaceableInventoryPanel>();
        SetObject(panel, "slotsRoot", slotsObject.GetComponent<RectTransform>());
        SetObject(panel, "slotsLayout", slotsLayout);
        SetObject(panel, "slotPrefab", slotPrefab);
        SetObject(panel, "startExecutionButton", startController);
        SetObject(panel, "panelRectTransform", rootRect);
        SetObject(panel, "panelLayout", panelLayout);
        SavePrefab(root, InventoryPath);
    }

    private static void BuildPausePrefab()
    {
        GameObject root = CreateUiObject("PausedMenu", null, typeof(PauseMenuManager));
        Stretch(root.GetComponent<RectTransform>());
        GameObject container = CreateUiObject("Container", root.transform);
        Stretch(container.GetComponent<RectTransform>());

        GameObject background = CreateUiObject("Image", container.transform, typeof(Image));
        Place(background.GetComponent<RectTransform>(), Vector2.zero, new Vector2(734f, 280f));
        background.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78431374f);

        GameObject actions = CreateUiObject("PauseActions", container.transform);
        Stretch(actions.GetComponent<RectTransform>());
        TextMeshProUGUI title = CreateTmpText(
            actions.transform,
            "Text",
            "Pausado",
            36f,
            FontStyles.Normal,
            Color.white,
            TextAlignmentOptions.Center,
            false);
        Place(title.rectTransform, new Vector2(0f, 160f), new Vector2(520f, 76f));

        Button resume = CreateTmpButton(actions.transform, "Resume", "Continuar", Color.white, Ink, 24f, 0f, FontStyles.Normal);
        Place(resume.GetComponent<RectTransform>(), new Vector2(0f, 72f), new Vector2(320f, 46f));
        Button reset = CreateTmpButton(actions.transform, "Reset level", "Reiniciar nivel", Accent, Ink, 24f, 0f, FontStyles.Normal);
        Place(reset.GetComponent<RectTransform>(), new Vector2(0f, 18f), new Vector2(320f, 46f));
        Button options = CreateTmpButton(actions.transform, "Options", "Opciones", Color.white, Ink, 24f, 0f, FontStyles.Normal);
        Place(options.GetComponent<RectTransform>(), new Vector2(0f, -36f), new Vector2(320f, 46f));
        Button menu = CreateTmpButton(actions.transform, "Go to menu", "Volver al menu", Color.white, Ink, 24f, 0f, FontStyles.Normal);
        Place(menu.GetComponent<RectTransform>(), new Vector2(0f, -90f), new Vector2(320f, 46f));

        GameObject optionsPanel = CreateUiObject(
            "OptionsPanel",
            container.transform,
            typeof(Image),
            typeof(VerticalLayoutGroup));
        Place(optionsPanel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(520f, 420f));
        optionsPanel.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.18f);
        VerticalLayoutGroup layout = optionsPanel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(48, 48, 36, 36);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        TextMeshProUGUI optionsTitle = CreateTmpText(
            optionsPanel.transform,
            "Title",
            "Opciones",
            42f,
            FontStyles.Bold,
            Color.white,
            TextAlignmentOptions.Center,
            true);
        SetPreferredSize(optionsTitle.gameObject, 420f, 64f);
        CreateSliderRow(optionsPanel.transform, "Musica", 0.75f, 420f, 46f, 160f, 220f, 22f, Color.white);
        CreateSliderRow(optionsPanel.transform, "Efectos", 0.85f, 420f, 46f, 160f, 220f, 22f, Color.white);
        CreateToggleRow(optionsPanel.transform, "Pantalla completa", true, 420f, 46f, 22f, Color.white);
        Button back = CreateTmpButton(optionsPanel.transform, "Back", "Volver", Accent, Ink, 24f, 0f, FontStyles.Normal);
        SetPreferredSize(back.gameObject, 320f, 48f);
        optionsPanel.SetActive(false);

        PauseMenuManager manager = root.GetComponent<PauseMenuManager>();
        SetObject(manager, "container", container);
        SetObject(manager, "pauseActions", actions);
        SetObject(manager, "optionsPanel", optionsPanel);
        SetObject(manager, "resumeButton", resume);
        SetObject(manager, "resetButton", reset);
        SetObject(manager, "optionsButton", options);
        SetObject(manager, "mainMenuButton", menu);
        SetObject(manager, "optionsBackButton", back);
        container.SetActive(false);
        SavePrefab(root, PausePath);
    }

    private static void BuildVictoryPrefab()
    {
        GameObject root = CreateUiObject("VictoryScreenManager", null, typeof(VictoryScreenManager));
        Stretch(root.GetComponent<RectTransform>());
        GameObject container = CreateModalContainer(root.transform, "VictoryScreenContainer");
        GameObject card = CreateModalCard(container.transform, "VictoryCard", new Vector2(620f, 520f));
        TextMeshProUGUI title = CreateTmpText(card.transform, "Title", "Victoria", 54f, FontStyles.Bold, Ink, TextAlignmentOptions.Center, false);
        Place(title.rectTransform, new Vector2(0f, 170f), new Vector2(500f, 68f));
        TextMeshProUGUI subtitle = CreateTmpText(card.transform, "Subtitle", "\u00a1Nivel completado!", 30f, FontStyles.Bold, Ink, TextAlignmentOptions.Center, false);
        Place(subtitle.rectTransform, new Vector2(0f, 104f), new Vector2(500f, 44f));
        Button continueButton = CreateTmpButton(card.transform, "Continue", "Continuar", Green, Ink, 26f);
        Place(continueButton.GetComponent<RectTransform>(), new Vector2(0f, 28f), new Vector2(420f, 62f));
        Button retryButton = CreateTmpButton(card.transform, "Retry", "Reintentar", Accent, Ink, 26f);
        Place(retryButton.GetComponent<RectTransform>(), new Vector2(0f, -52f), new Vector2(420f, 62f));
        Button menuButton = CreateTmpButton(card.transform, "MainMenu", "Volver al menu", Red, Ink, 26f);
        Place(menuButton.GetComponent<RectTransform>(), new Vector2(0f, -132f), new Vector2(420f, 62f));

        VictoryScreenManager manager = root.GetComponent<VictoryScreenManager>();
        SetObject(manager, "container", container);
        SetObject(manager, "continueButton", continueButton);
        SetObject(manager, "retryButton", retryButton);
        SetObject(manager, "mainMenuButton", menuButton);
        container.SetActive(false);
        SavePrefab(root, VictoryPath);
    }

    private static void BuildDefeatPrefab()
    {
        GameObject root = CreateUiObject("DefeatScreenManager", null, typeof(DefeatScreenManager));
        Stretch(root.GetComponent<RectTransform>());
        GameObject container = CreateModalContainer(root.transform, "DefeatScreenContainer");
        GameObject card = CreateModalCard(container.transform, "DefeatCard", new Vector2(560f, 380f));
        TextMeshProUGUI title = CreateTmpText(card.transform, "Title", "Derrota", 54f, FontStyles.Bold, Ink, TextAlignmentOptions.Center, false);
        Place(title.rectTransform, new Vector2(0f, 100f), new Vector2(460f, 68f));
        TextMeshProUGUI subtitle = CreateTmpText(card.transform, "Subtitle", "\u00a1El pato se pinch\u00f3!", 30f, FontStyles.Bold, Ink, TextAlignmentOptions.Center, false);
        Place(subtitle.rectTransform, new Vector2(0f, 38f), new Vector2(460f, 44f));
        Button retryButton = CreateTmpButton(card.transform, "Retry", "Reintentar", Accent, Ink, 26f);
        Place(retryButton.GetComponent<RectTransform>(), new Vector2(0f, -42f), new Vector2(400f, 62f));
        Button menuButton = CreateTmpButton(card.transform, "MainMenu", "Volver al menu", Red, Ink, 26f);
        Place(menuButton.GetComponent<RectTransform>(), new Vector2(0f, -122f), new Vector2(400f, 62f));

        DefeatScreenManager manager = root.GetComponent<DefeatScreenManager>();
        SetObject(manager, "container", container);
        SetObject(manager, "subtitleText", subtitle);
        SetObject(manager, "retryButton", retryButton);
        SetObject(manager, "mainMenuButton", menuButton);
        container.SetActive(false);
        SavePrefab(root, DefeatPath);
    }

    private static void BuildMainMenuBackgroundPrefab()
    {
        GameObject root = CreateUiObject("PrototypeBackground", null);
        Stretch(root.GetComponent<RectTransform>());
        AddBand(root.transform, "SkyTop", SkyTop, new Vector2(0f, 0.35f), Vector2.one);
        AddBand(root.transform, "SkyBottom", SkyBottom, new Vector2(0f, 0.18f), new Vector2(1f, 0.36f));
        AddBand(root.transform, "Grass", Grass, Vector2.zero, new Vector2(1f, 0.2f));
        SavePrefab(root, MainBackgroundPath);
    }

    private static void BuildSplashPrefab()
    {
        GameObject root = CreateFullPanel("SplashPanel");
        GameObject card = CreateCard(root.transform, "SplashCard", Vector2.zero, new Vector2(780f, 340f), new Vector2(0.5f, 0.5f), Panel);
        VerticalLayoutGroup layout = AddVerticalLayout(card, 22f, new RectOffset(42, 42, 36, 36), TextAnchor.MiddleCenter);
        TextMeshProUGUI title = CreateTmpText(card.transform, "Title", "Unlucky Ducky", 76f, FontStyles.Bold, Ink, TextAlignmentOptions.Center, true);
        title.textWrappingMode = TextWrappingModes.NoWrap;
        CreateTmpText(card.transform, "Subtitle", "Un puzzle de patos, trampas y malas decisiones.", 28f, FontStyles.Normal, Ink, TextAlignmentOptions.Center, true);
        CreateTmpText(card.transform, "Prompt", "Presiona cualquier tecla", 24f, FontStyles.Bold, Accent, TextAlignmentOptions.Center, true);
        layout.childForceExpandHeight = false;
        SavePrefab(root, SplashPath);
    }

    private static void BuildMainPanelPrefab()
    {
        GameObject root = CreateFullPanel("MainMenuPanel");
        GameObject titleBlock = CreateAnchored(root.transform, "TitleBlock", new Vector2(120f, 210f), new Vector2(760f, 420f), new Vector2(0f, 0.5f));
        AddVerticalLayout(titleBlock, 40f, new RectOffset(), TextAnchor.MiddleLeft);
        CreateTmpText(titleBlock.transform, "Title", "Unlucky\nDucky", 96f, FontStyles.Bold, Ink, TextAlignmentOptions.Left, true);
        CreateTmpText(titleBlock.transform, "Subtitle", "Una aventura in-quack-\u00edble.", 30f, FontStyles.Normal, Ink, TextAlignmentOptions.Left, true);

        GameObject menuCard = CreateCard(root.transform, "MainActions", new Vector2(-190f, 0f), new Vector2(470f, 520f), new Vector2(1f, 0.5f), Panel);
        VerticalLayoutGroup menuLayout = AddVerticalLayout(menuCard, 18f, new RectOffset(34, 34, 34, 34), TextAnchor.MiddleCenter);
        menuLayout.childForceExpandWidth = false;
        CreateTmpText(menuCard.transform, "MenuTitle", "MEN\u00da", 38f, FontStyles.Bold, Ink, TextAlignmentOptions.Center, true);
        SetPreferredSize(CreateTmpButton(menuCard.transform, "PlayButton", "Jugar", Green, Ink, 30f).gameObject, 360f, 72f);
        SetPreferredSize(CreateTmpButton(menuCard.transform, "OptionsButton", "Opciones", Accent, Ink, 30f).gameObject, 360f, 72f);
        SetPreferredSize(CreateTmpButton(menuCard.transform, "CreditsButton", "Cr\u00e9ditos", Accent, Ink, 30f).gameObject, 360f, 72f);
        SetPreferredSize(CreateTmpButton(menuCard.transform, "ExitButton", "Salir", Red, Ink, 30f).gameObject, 360f, 72f);
        SavePrefab(root, MainPanelPath);
    }

    private static void BuildLevelSelectPrefab()
    {
        LevelCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(MainLevelCatalogPath);
        GameObject slotAsset = AssetDatabase.LoadAssetAtPath<GameObject>(LevelSelectSlotPath);
        GameObject root = CreateFullPanel("LevelSelectPanel");
        LevelSelectController controller = root.AddComponent<LevelSelectController>();
        GameObject backgroundObject = CreateUiObject(
            "SelectorBackground",
            root.transform,
            typeof(Image),
            typeof(AspectRatioFitter));
        Stretch(backgroundObject.GetComponent<RectTransform>());
        Image selectorBackground = backgroundObject.GetComponent<Image>();
        selectorBackground.color = Color.white;
        selectorBackground.raycastTarget = false;
        AspectRatioFitter backgroundFitter = backgroundObject.GetComponent<AspectRatioFitter>();
        backgroundFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;

        GameObject card = CreateAnchored(root.transform, "LevelSelectCard", Vector2.zero, new Vector2(1120f, 560f), new Vector2(0.5f, 0.5f));
        AddVerticalLayout(card, 20f, new RectOffset(42, 42, 34, 34), TextAnchor.UpperCenter);
        CreateTmpText(card.transform, "Title", "Seleccionar nivel", 44f, FontStyles.Bold, Ink, TextAlignmentOptions.Center, true);

        GameObject paginationRow = CreateUiObject("PaginationRow", card.transform, typeof(LayoutElement));
        SetPreferredSize(paginationRow, 980f, 170f);
        Button previous = CreateTmpButton(paginationRow.transform, "PreviousPage", "<", Accent, Ink, 30f, 72f);
        Place(previous.GetComponent<RectTransform>(), new Vector2(-431f, 0f), new Vector2(82f, 132f));
        GameObject gridFrame = CreateUiObject("LevelGridFrame", paginationRow.transform, typeof(Image));
        gridFrame.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.35f);
        Place(gridFrame.GetComponent<RectTransform>(), Vector2.zero, new Vector2(690f, 170f));
        Button next = CreateTmpButton(paginationRow.transform, "NextPage", ">", Accent, Ink, 30f, 72f);
        Place(next.GetComponent<RectTransform>(), new Vector2(431f, 0f), new Vector2(82f, 132f));

        GameObject content = CreateUiObject("Content", gridFrame.transform, typeof(GridLayoutGroup));
        Stretch(content.GetComponent<RectTransform>());
        content.GetComponent<RectTransform>().offsetMin = new Vector2(24f, 24f);
        content.GetComponent<RectTransform>().offsetMax = new Vector2(-24f, -24f);
        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
        grid.spacing = new Vector2(12f, 14f);
        grid.cellSize = new Vector2(116f, 122f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = LevelSelectController.ItemsPerPage;
        grid.childAlignment = TextAnchor.MiddleCenter;

        LevelSelectSlotView[] slots = new LevelSelectSlotView[LevelSelectController.ItemsPerPage];
        for (int i = 0; i < slots.Length; i++)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(slotAsset);
            instance.transform.SetParent(content.transform, false);
            instance.name = $"LevelSlot{i + 1}";
            slots[i] = instance.GetComponent<LevelSelectSlotView>();
        }

        TextMeshProUGUI pageLabel = CreateTmpText(card.transform, "PageLabel", "Mundo 1 / 1", 24f, FontStyles.Bold, Ink, TextAlignmentOptions.Center, true);
        SetPreferredSize(pageLabel.gameObject, 320f, 38f);
        CreateTmpButton(card.transform, "BackButton", "Volver", Accent, Ink, 30f, 72f);

        SetObject(controller, "catalog", catalog);
        SetObjectArray(controller, "slots", slots);
        SetObject(controller, "previousPageButton", previous);
        SetObject(controller, "nextPageButton", next);
        SetObject(controller, "pageLabel", pageLabel);
        SetObject(controller, "selectorBackground", selectorBackground);
        SavePrefab(root, LevelSelectPath);
    }

    private static void BuildOptionsPrefab()
    {
        GameObject root = CreateFullPanel("OptionsPanel");
        GameObject card = CreateCard(root.transform, "OptionsCard", Vector2.zero, new Vector2(720f, 620f), new Vector2(0.5f, 0.5f), Panel);
        AddVerticalLayout(card, 20f, new RectOffset(46, 46, 40, 40), TextAnchor.UpperCenter);
        CreateTmpText(card.transform, "Title", "Opciones", 46f, FontStyles.Bold, Ink, TextAlignmentOptions.Center, true);
        CreateSliderRow(card.transform, "Musica", 0.75f, 0f, 54f, 180f, 360f, 26f, Ink);
        CreateSliderRow(card.transform, "Efectos", 0.85f, 0f, 54f, 180f, 360f, 26f, Ink);
        CreateToggleRow(card.transform, "Pantalla completa", true, 0f, 54f, 26f, Ink);
        Button reset = CreateTmpButton(card.transform, "ResetProgress", "Reiniciar progreso (pr\u00f3ximamente)", new Color(0.72f, 0.75f, 0.71f, 1f), Ink, 30f, 72f);
        reset.interactable = false;
        CreateTmpButton(card.transform, "BackButton", "Volver", Accent, Ink, 30f, 72f);
        SavePrefab(root, OptionsPath);
    }

    private static void BuildCreditsPrefab()
    {
        GameObject root = CreateFullPanel("CreditsPanel");
        GameObject card = CreateCard(root.transform, "CreditsCard", Vector2.zero, new Vector2(800f, 620f), new Vector2(0.5f, 0.5f), Panel);
        VerticalLayoutGroup layout = AddVerticalLayout(card, 24f, new RectOffset(64, 64, 54, 54), TextAnchor.UpperCenter);
        layout.childForceExpandWidth = false;
        TextMeshProUGUI title = CreateTmpText(card.transform, "Title", "Cr\u00e9ditos", 46f, FontStyles.Bold, Ink, TextAlignmentOptions.Center, true);
        SetPreferredSize(title.gameObject, 560f, 70f);
        TextMeshProUGUI credits = CreateTmpText(
            card.transform,
            "Credits",
            "Dise\u00f1o y desarrollo\nTeam RoMaRi\nNicol\u00e1s Rodr\u00edguez - Lead Level Designer\nFelipe Riva - Lead UX/UI and Game Designer\nRamiro Masseroni - Lead Programmer\n\u00a1Gracias por jugar!",
            30f,
            FontStyles.Normal,
            Ink,
            TextAlignmentOptions.Center,
            true);
        credits.lineSpacing = 8f;
        credits.enableAutoSizing = true;
        credits.fontSizeMin = 22f;
        credits.fontSizeMax = 30f;
        SetPreferredSize(credits.gameObject, 560f, 300f);
        Button back = CreateTmpButton(card.transform, "BackButton", "Volver", Accent, Ink, 30f, 72f);
        SetPreferredSize(back.gameObject, 360f, 72f);
        SavePrefab(root, CreditsPath);
    }

    private static void BuildGameplayCanvasPrefab()
    {
        GameObject hudAsset = AssetDatabase.LoadAssetAtPath<GameObject>(HudPath);
        GameObject inventoryAsset = AssetDatabase.LoadAssetAtPath<GameObject>(InventoryPath);
        GameObject pauseAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PausePath);
        GameObject victoryAsset = AssetDatabase.LoadAssetAtPath<GameObject>(VictoryPath);
        GameObject defeatAsset = AssetDatabase.LoadAssetAtPath<GameObject>(DefeatPath);
        GameObject root = CreateCanvas("UI_GameplayCanvas", new Vector2(800f, 600f), false);
        GameObject hud = AddNestedPrefab(hudAsset, root.transform);
        AddNestedPrefab(inventoryAsset, root.transform);
        GameObject pause = AddNestedPrefab(pauseAsset, root.transform);
        AddNestedPrefab(victoryAsset, root.transform);
        AddNestedPrefab(defeatAsset, root.transform);
        SetObject(hud.GetComponent<LevelHudPanel>(), "pauseMenuManager", pause.GetComponent<PauseMenuManager>());
        SavePrefab(root, GameplayCanvasPath);
    }

    private static void BuildMainMenuCanvasPrefab()
    {
        GameObject backgroundAsset = AssetDatabase.LoadAssetAtPath<GameObject>(MainBackgroundPath);
        GameObject splashAsset = AssetDatabase.LoadAssetAtPath<GameObject>(SplashPath);
        GameObject mainAsset = AssetDatabase.LoadAssetAtPath<GameObject>(MainPanelPath);
        GameObject levelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(LevelSelectPath);
        GameObject optionsAsset = AssetDatabase.LoadAssetAtPath<GameObject>(OptionsPath);
        GameObject creditsAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CreditsPath);
        GameObject root = CreateCanvas("UI_MainMenuCanvas", new Vector2(1920f, 1080f), true);
        MainMenuNavigationController navigation = root.AddComponent<MainMenuNavigationController>();
        AddNestedPrefab(backgroundAsset, root.transform);
        GameObject splash = AddNestedPrefab(splashAsset, root.transform);
        GameObject main = AddNestedPrefab(mainAsset, root.transform);
        GameObject level = AddNestedPrefab(levelAsset, root.transform);
        GameObject options = AddNestedPrefab(optionsAsset, root.transform);
        GameObject credits = AddNestedPrefab(creditsAsset, root.transform);

        SetObject(navigation, "splashPanel", splash);
        SetObject(navigation, "mainMenuPanel", main);
        SetObject(navigation, "levelSelectPanel", level);
        SetObject(navigation, "optionsPanel", options);
        SetObject(navigation, "creditsPanel", credits);
        SetObject(navigation, "playButton", FindButton(main.transform, "PlayButton"));
        SetObject(navigation, "optionsButton", FindButton(main.transform, "OptionsButton"));
        SetObject(navigation, "creditsButton", FindButton(main.transform, "CreditsButton"));
        SetObject(navigation, "exitButton", FindButton(main.transform, "ExitButton"));
        SetObject(navigation, "levelSelectBackButton", FindButton(level.transform, "BackButton"));
        SetObject(navigation, "optionsBackButton", FindButton(options.transform, "BackButton"));
        SetObject(navigation, "creditsBackButton", FindButton(credits.transform, "BackButton"));
        splash.SetActive(true);
        main.SetActive(false);
        level.SetActive(false);
        options.SetActive(false);
        credits.SetActive(false);
        SavePrefab(root, MainMenuCanvasPath);
    }

    private static void BuildEventSystemPrefab()
    {
        GameObject root = new GameObject("UI_EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        SavePrefab(root, EventSystemPath);
    }

    private static void MigrateGameplayScene(Scene scene, GameObject gameplayPrefab, GameObject eventSystemPrefab)
    {
        DestroySceneComponents<Canvas>(scene);
        DestroySceneComponents<EventSystem>(scene);
        DestroySceneComponents<VictoryScreenManager>(scene);
        DestroySceneComponents<DefeatScreenManager>(scene);
        DestroySceneComponents<PauseMenuManager>(scene);
        DestroySceneComponents<LevelHudPanel>(scene);
        DestroySceneComponents<PlaceableInventoryPanel>(scene);
        InstantiateInScene(gameplayPrefab, scene);
        InstantiateInScene(eventSystemPrefab, scene);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void MigrateMainMenuScene(Scene scene, GameObject mainMenuPrefab, GameObject eventSystemPrefab)
    {
        DestroySceneComponents<Canvas>(scene);
        DestroySceneComponents<EventSystem>(scene);
        DestroyObjectsNamed(scene, "MainMenuBootstrapper");
        EnsureMainMenuCamera(scene);
        InstantiateInScene(mainMenuPrefab, scene);
        InstantiateInScene(eventSystemPrefab, scene);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void EnsureMainMenuCamera(Scene scene)
    {
        Camera camera = FindComponentInScene<Camera>(scene);

        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            cameraObject.tag = "MainCamera";
            camera = cameraObject.GetComponent<Camera>();
        }

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = SkyTop;
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void GenerateIconAssets()
    {
        Texture2D pauseTexture = CreateClearTexture(64);
        FillRect(pauseTexture, 20, 14, 10, 36, Color.white);
        FillRect(pauseTexture, 34, 14, 10, 36, Color.white);
        pauseTexture.Apply();
        WriteTexture(pauseTexture, PauseIconPath);
        UnityEngine.Object.DestroyImmediate(pauseTexture);

        Texture2D resetTexture = CreateClearTexture(64);
        Vector2 center = new Vector2(32f, 32f);
        const float radius = 21f;
        const float tipDegrees = 35f;
        DrawArcClockwise(resetTexture, center, radius, 300f, tipDegrees, Color.white, 4);
        DrawArrowHead(resetTexture, PointOnCircle(center, radius, tipDegrees), ClockwiseTangent(tipDegrees), Color.white, 4);
        resetTexture.Apply();
        WriteTexture(resetTexture, ResetIconPath);
        UnityEngine.Object.DestroyImmediate(resetTexture);

        ConfigureSpriteImporter(PauseIconPath);
        ConfigureSpriteImporter(ResetIconPath);
    }

    private static GameObject CreateCanvas(string name, Vector2 referenceResolution, bool scaleWithScreen)
    {
        GameObject root = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        root.layer = 5;
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = scaleWithScreen
            ? CanvasScaler.ScaleMode.ScaleWithScreenSize
            : CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = scaleWithScreen ? 0.5f : 0f;
        return root;
    }

    private static Button CreateHudButton(Transform parent, string name, Vector2 position, Sprite sprite)
    {
        GameObject buttonObject = CreateUiObject(name, parent, typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.sizeDelta = new Vector2(48f, 48f);
        rect.anchoredPosition = position;
        Image background = buttonObject.GetComponent<Image>();
        background.color = HudDark;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;

        GameObject iconObject = CreateUiObject("Icon", buttonObject.transform, typeof(Image));
        Place(iconObject.GetComponent<RectTransform>(), Vector2.zero, new Vector2(30f, 30f));
        Image icon = iconObject.GetComponent<Image>();
        icon.sprite = sprite;
        icon.color = Color.white;
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        return button;
    }

    private static GameObject CreateModalContainer(Transform parent, string name)
    {
        GameObject container = CreateUiObject(name, parent, typeof(Image));
        Stretch(container.GetComponent<RectTransform>());
        Image overlay = container.GetComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.68f);
        overlay.raycastTarget = true;
        return container;
    }

    private static GameObject CreateModalCard(Transform parent, string name, Vector2 size)
    {
        GameObject card = CreateUiObject(name, parent, typeof(Image));
        Place(card.GetComponent<RectTransform>(), Vector2.zero, size);
        card.GetComponent<Image>().color = Panel;
        return card;
    }

    private static GameObject CreateFullPanel(string name)
    {
        GameObject panel = CreateUiObject(name, null);
        Stretch(panel.GetComponent<RectTransform>());
        return panel;
    }

    private static GameObject CreateCard(Transform parent, string name, Vector2 position, Vector2 size, Vector2 anchor, Color color)
    {
        GameObject card = CreateAnchored(parent, name, position, size, anchor);
        card.AddComponent<Image>().color = color;
        return card;
    }

    private static GameObject CreateAnchored(Transform parent, string name, Vector2 position, Vector2 size, Vector2 anchor)
    {
        GameObject target = CreateUiObject(name, parent);
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return target;
    }

    private static VerticalLayoutGroup AddVerticalLayout(GameObject target, float spacing, RectOffset padding, TextAnchor alignment)
    {
        VerticalLayoutGroup layout = target.AddComponent<VerticalLayoutGroup>();
        layout.padding = padding;
        layout.spacing = spacing;
        layout.childAlignment = alignment;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return layout;
    }

    private static TextMeshProUGUI CreateTmpText(
        Transform parent,
        string name,
        string value,
        float size,
        FontStyles style,
        Color color,
        TextAlignmentOptions alignment,
        bool addLayoutElement)
    {
        GameObject textObject = CreateUiObject(name, parent, typeof(TextMeshProUGUI));
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;

        if (addLayoutElement)
        {
            LayoutElement element = textObject.AddComponent<LayoutElement>();
            element.preferredHeight = Mathf.Max(48f, size * 1.45f);
        }

        return text;
    }

    private static Text CreateLegacyText(
        Transform parent,
        string name,
        string value,
        int size,
        FontStyle style,
        TextAnchor alignment)
    {
        GameObject textObject = CreateUiObject(name, parent, typeof(Text));
        Text text = textObject.GetComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.black;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private static Button CreateTmpButton(
        Transform parent,
        string name,
        string label,
        Color color,
        Color textColor,
        float fontSize,
        float preferredHeight = 0f,
        FontStyles fontStyle = FontStyles.Bold)
    {
        GameObject buttonObject = CreateUiObject(name, parent, typeof(Image), typeof(Button));
        Image image = buttonObject.GetComponent<Image>();
        image.color = color;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        TextMeshProUGUI text = CreateTmpText(
            buttonObject.transform,
            "Text",
            label,
            fontSize,
            fontStyle,
            textColor,
            TextAlignmentOptions.Center,
            false);
        Stretch(text.rectTransform);
        text.raycastTarget = false;

        if (preferredHeight > 0f)
        {
            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.preferredHeight = preferredHeight;
        }

        return button;
    }

    private static Button CreateLegacyButton(Transform parent, string name, string label, Color color, int fontSize)
    {
        GameObject buttonObject = CreateUiObject(name, parent, typeof(Image), typeof(Button));
        Image image = buttonObject.GetComponent<Image>();
        image.color = color;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        Text text = CreateLegacyText(buttonObject.transform, "Label", label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);
        return button;
    }

    private static void CreateSliderRow(
        Transform parent,
        string label,
        float value,
        float preferredWidth,
        float preferredHeight,
        float labelWidth,
        float sliderWidth,
        float fontSize,
        Color textColor)
    {
        GameObject row = CreateUiObject(label, parent, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        LayoutElement rowElement = row.GetComponent<LayoutElement>();
        rowElement.preferredWidth = preferredWidth;
        rowElement.preferredHeight = preferredHeight;
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;

        TextMeshProUGUI text = CreateTmpText(row.transform, "Label", label, fontSize, FontStyles.Bold, textColor, TextAlignmentOptions.Left, false);
        SetPreferredSize(text.gameObject, labelWidth, preferredHeight > 0f ? preferredHeight : 54f);
        GameObject sliderObject = CreateUiObject("Slider", row.transform, typeof(Slider), typeof(LayoutElement));
        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(value);
        sliderObject.GetComponent<LayoutElement>().preferredWidth = sliderWidth;

        GameObject background = CreateUiObject("Background", sliderObject.transform, typeof(Image));
        Stretch(background.GetComponent<RectTransform>());
        float inset = preferredHeight <= 46f ? 17f : 18f;
        background.GetComponent<RectTransform>().offsetMin = new Vector2(0f, inset);
        background.GetComponent<RectTransform>().offsetMax = new Vector2(0f, -inset);
        background.GetComponent<Image>().color = new Color(0.82f, 0.86f, 0.78f, 1f);
        GameObject fillArea = CreateUiObject("Fill Area", sliderObject.transform);
        Stretch(fillArea.GetComponent<RectTransform>());
        fillArea.GetComponent<RectTransform>().offsetMin = new Vector2(0f, inset);
        fillArea.GetComponent<RectTransform>().offsetMax = new Vector2(0f, -inset);
        GameObject fill = CreateUiObject("Fill", fillArea.transform, typeof(Image));
        Stretch(fill.GetComponent<RectTransform>());
        fill.GetComponent<Image>().color = Green;
        slider.fillRect = fill.GetComponent<RectTransform>();
    }

    private static void CreateToggleRow(
        Transform parent,
        string label,
        bool value,
        float preferredWidth,
        float preferredHeight,
        float fontSize,
        Color textColor)
    {
        GameObject toggleObject = CreateUiObject(label, parent, typeof(Toggle), typeof(LayoutElement));
        Toggle toggle = toggleObject.GetComponent<Toggle>();
        toggle.isOn = value;
        LayoutElement element = toggleObject.GetComponent<LayoutElement>();
        element.preferredWidth = preferredWidth;
        element.preferredHeight = preferredHeight;

        GameObject box = CreateUiObject("Box", toggleObject.transform, typeof(Image));
        RectTransform boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0f, 0.5f);
        boxRect.anchorMax = new Vector2(0f, 0.5f);
        boxRect.pivot = new Vector2(0f, 0.5f);
        float boxSize = preferredHeight <= 46f ? 32f : 34f;
        boxRect.sizeDelta = new Vector2(boxSize, boxSize);
        boxRect.anchoredPosition = Vector2.zero;
        Image boxImage = box.GetComponent<Image>();
        boxImage.color = new Color(1f, 0.96f, 0.84f, 1f);

        GameObject checkmark = CreateUiObject("Checkmark", box.transform, typeof(Image));
        Stretch(checkmark.GetComponent<RectTransform>());
        checkmark.GetComponent<RectTransform>().offsetMin = new Vector2(8f, 8f);
        checkmark.GetComponent<RectTransform>().offsetMax = new Vector2(-8f, -8f);
        Image checkmarkImage = checkmark.GetComponent<Image>();
        checkmarkImage.color = Green;
        toggle.targetGraphic = boxImage;
        toggle.graphic = checkmarkImage;

        TextMeshProUGUI text = CreateTmpText(toggleObject.transform, "Label", label, fontSize, FontStyles.Bold, textColor, TextAlignmentOptions.Left, false);
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(52f, 0f);
    }

    private static void AddBand(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject band = CreateUiObject(name, parent, typeof(Image));
        RectTransform rect = band.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        band.GetComponent<Image>().color = color;
    }

    private static GameObject CreateUiObject(string name, Transform parent, params Type[] components)
    {
        List<Type> types = new List<Type> { typeof(RectTransform) };
        types.AddRange(components);
        GameObject target = new GameObject(name, types.ToArray());
        target.layer = 5;

        if (parent != null)
        {
            target.transform.SetParent(parent, false);
        }

        return target;
    }

    private static GameObject AddNestedPrefab(GameObject asset, Transform parent)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        instance.transform.SetParent(parent, false);
        return instance;
    }

    private static Button FindButton(Transform root, string name)
    {
        Transform match = FindChildRecursive(root, name);
        return match != null ? match.GetComponent<Button>() : null;
    }

    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root.name == name)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindChildRecursive(root.GetChild(i), name);

            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static void ConfigureTooltipSource(GameObject target, LevelHudPanel hud, string message)
    {
        HudTooltipSource source = target.AddComponent<HudTooltipSource>();
        SetObject(source, "hud", hud);
        SetString(source, "message", message);
    }

    private static void SetPreferredSize(GameObject target, float width, float height)
    {
        LayoutElement element = target.GetComponent<LayoutElement>();

        if (element == null)
        {
            element = target.AddComponent<LayoutElement>();
        }

        element.preferredWidth = width;
        element.preferredHeight = height;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SavePrefab(GameObject root, string path)
    {
        EnsureFolder(Path.GetDirectoryName(path)?.Replace("\\", "/"));
        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void EnsureFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static void SetObject(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null)
        {
            throw new MissingFieldException(target.GetType().Name, propertyName);
        }

        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObjectArray<T>(UnityEngine.Object target, string propertyName, T[] values)
        where T : UnityEngine.Object
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        property.arraySize = values.Length;

        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetString(UnityEngine.Object target, string propertyName, string value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).stringValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void InstantiateInScene(GameObject prefab, Scene scene)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = prefab.name;
    }

    private static void DestroySceneComponents<T>(Scene scene) where T : Component
    {
        List<GameObject> targets = new List<GameObject>();
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            T[] components = roots[i].GetComponentsInChildren<T>(true);

            for (int j = 0; j < components.Length; j++)
            {
                GameObject target = components[j].gameObject;

                if (!targets.Contains(target))
                {
                    targets.Add(target);
                }
            }
        }

        for (int i = 0; i < targets.Count; i++)
        {
            UnityEngine.Object.DestroyImmediate(targets[i]);
        }
    }

    private static void DestroyObjectsNamed(Scene scene, string name)
    {
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = roots.Length - 1; i >= 0; i--)
        {
            Transform match = FindChildRecursive(roots[i].transform, name);

            if (match != null)
            {
                UnityEngine.Object.DestroyImmediate(match.gameObject);
            }
        }
    }

    private static T FindComponentInScene<T>(Scene scene) where T : Component
    {
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            T component = roots[i].GetComponentInChildren<T>(true);

            if (component != null)
            {
                return component;
            }
        }

        return null;
    }

    private static Texture2D CreateClearTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        Color[] pixels = new Color[size * size];
        Color clear = new Color(1f, 1f, 1f, 0f);

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = clear;
        }

        texture.SetPixels(pixels);
        return texture;
    }

    private static void WriteTexture(Texture2D texture, string path)
    {
        File.WriteAllBytes(Path.GetFullPath(path), texture.EncodeToPNG());
    }

    private static void ConfigureSpriteImporter(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 64f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static void DrawArcClockwise(Texture2D texture, Vector2 center, float radius, float startDegrees, float endDegrees, Color color, int thickness)
    {
        if (startDegrees < endDegrees)
        {
            startDegrees += 360f;
        }

        for (float angle = startDegrees; angle >= endDegrees; angle -= 1.5f)
        {
            Vector2 point = PointOnCircle(center, radius, angle);
            FillCircle(texture, Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y), thickness, color);
        }
    }

    private static void DrawArrowHead(Texture2D texture, Vector2 tip, Vector2 direction, Color color, int thickness)
    {
        const float length = 12f;
        const float angle = 36f;
        Vector2 back = -direction.normalized;
        DrawLine(texture, tip, tip + Rotate(back, angle) * length, color, thickness);
        DrawLine(texture, tip, tip + Rotate(back, -angle) * length, color, thickness);
    }

    private static Vector2 PointOnCircle(Vector2 center, float radius, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
    }

    private static Vector2 ClockwiseTangent(float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(radians), -Mathf.Cos(radians)).normalized;
    }

    private static Vector2 Rotate(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
    }

    private static void DrawLine(Texture2D texture, Vector2 start, Vector2 end, Color color, int thickness)
    {
        int steps = Mathf.CeilToInt(Vector2.Distance(start, end));

        for (int i = 0; i <= steps; i++)
        {
            Vector2 point = Vector2.Lerp(start, end, i / (float)steps);
            FillCircle(texture, Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y), thickness, color);
        }
    }

    private static void FillCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
    {
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= radius * radius)
                {
                    SetPixelSafe(texture, centerX + x, centerY + y, color);
                }
            }
        }
    }

    private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        for (int yy = y; yy < y + height; yy++)
        {
            for (int xx = x; xx < x + width; xx++)
            {
                SetPixelSafe(texture, xx, yy, color);
            }
        }
    }

    private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
    {
        if (x >= 0 && y >= 0 && x < texture.width && y < texture.height)
        {
            texture.SetPixel(x, y, color);
        }
    }
}
