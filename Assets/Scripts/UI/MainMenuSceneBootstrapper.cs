using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class MainMenuSceneBootstrapper : MonoBehaviour
{
    [SerializeField] private LevelCatalog levelCatalog;
    [SerializeField] private bool buildOnAwake = true;

    private static readonly Color SkyTop = new Color(0.47f, 0.82f, 1f, 1f);
    private static readonly Color SkyBottom = new Color(0.69f, 0.91f, 1f, 1f);
    private static readonly Color Grass = new Color(0.39f, 0.82f, 0.28f, 1f);
    private static readonly Color Panel = new Color(1f, 0.97f, 0.89f, 0.96f);
    private static readonly Color Ink = new Color(0.13f, 0.16f, 0.14f, 1f);
    private static readonly Color Accent = new Color(1f, 0.76f, 0.25f, 1f);
    private static readonly Color Green = new Color(0.24f, 0.72f, 0.38f, 1f);

    private void Awake()
    {
        if (buildOnAwake)
        {
            Build();
        }
    }

    public void Build()
    {
        EnsureCamera();
        EnsureEventSystem();

        Canvas canvas = CreateCanvas();
        Transform root = canvas.transform;
        CreateBackground(root);

        MainMenuNavigationController navigation = gameObject.AddComponent<MainMenuNavigationController>();
        GameObject splashPanel = BuildSplashPanel(root);
        GameObject mainPanel = BuildMainPanel(root, navigation);
        GameObject levelSelectPanel = BuildLevelSelectPanel(root, navigation);
        GameObject optionsPanel = BuildOptionsPanel(root, navigation);
        GameObject creditsPanel = BuildCreditsPanel(root, navigation);

        navigation.ConfigurePanels(splashPanel, mainPanel, levelSelectPanel, optionsPanel, creditsPanel);
    }

    private void EnsureCamera()
    {
        if (Camera.main != null || FindFirstObjectByType<Camera>() != null)
        {
            return;
        }

        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = SkyTop;
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
    }

    private Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        _ = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private void CreateBackground(Transform parent)
    {
        GameObject background = CreatePanel(parent, "PrototypeBackground");
        AddBand(background.transform, "SkyTop", SkyTop, new Vector2(0f, 0.35f), Vector2.one);
        AddBand(background.transform, "SkyBottom", SkyBottom, new Vector2(0f, 0.18f), new Vector2(1f, 0.36f));
        AddBand(background.transform, "Grass", Grass, Vector2.zero, new Vector2(1f, 0.2f));
    }

    private GameObject BuildSplashPanel(Transform parent)
    {
        GameObject panel = CreatePanel(parent, "SplashPanel");
        GameObject card = CreateCard(panel.transform, "SplashCard", new Vector2(780f, 340f), Panel);
        VerticalLayoutGroup layout = AddVerticalLayout(card, 22f, new RectOffset(42, 42, 36, 36), TextAnchor.MiddleCenter);

        TextMeshProUGUI title = CreateText(card.transform, "Unlucky Ducky", 76f, FontStyles.Bold, Ink, TextAlignmentOptions.Center);
        title.textWrappingMode = TextWrappingModes.NoWrap;
        CreateText(card.transform, "Un puzzle de patos, trampas y malas decisiones.", 28f, FontStyles.Normal, Ink, TextAlignmentOptions.Center);
        CreateText(card.transform, "Presiona cualquier tecla", 24f, FontStyles.Bold, Accent, TextAlignmentOptions.Center);

        layout.childForceExpandHeight = false;
        return panel;
    }

    private GameObject BuildMainPanel(Transform parent, MainMenuNavigationController navigation)
    {
        GameObject panel = CreatePanel(parent, "MainMenuPanel");

        GameObject titleBlock = CreateAnchored(panel.transform, "TitleBlock", new Vector2(120f, 210f), new Vector2(760f, 420f), new Vector2(0f, 0.5f));
        AddVerticalLayout(titleBlock, 40f, new RectOffset(0, 0, 0, 0), TextAnchor.MiddleLeft);
        CreateText(titleBlock.transform, "Unlucky\nDucky", 96f, FontStyles.Bold, Ink, TextAlignmentOptions.Left);
        CreateText(titleBlock.transform, "Una aventura in-quack-íble.", 30f, FontStyles.Normal, Ink, TextAlignmentOptions.Left);

        GameObject menuCard = CreateAnchoredCard(panel.transform, "MainActions", new Vector2(-190f, 0f), new Vector2(470f, 520f), new Vector2(1f, 0.5f));
        AddVerticalLayout(menuCard, 18f, new RectOffset(34, 34, 34, 34), TextAnchor.MiddleCenter);
        CreateText(menuCard.transform, "MENÚ", 38f, FontStyles.Bold, Ink, TextAlignmentOptions.Center);
        CreateButton(menuCard.transform, "Jugar", navigation.ShowLevelSelect, Green);
        CreateButton(menuCard.transform, "Opciones", navigation.ShowOptions, Accent);
        CreateButton(menuCard.transform, "Créditos", navigation.ShowCredits, Accent);
        CreateButton(menuCard.transform, "Salir", navigation.ExitGame, new Color(0.9f, 0.45f, 0.36f, 1f));

        return panel;
    }

    private GameObject BuildLevelSelectPanel(Transform parent, MainMenuNavigationController navigation)
    {
        GameObject panel = CreatePanel(parent, "LevelSelectPanel");
        GameObject card = CreateAnchoredCard(panel.transform, "LevelSelectCard", Vector2.zero, new Vector2(1120f, 560f), new Vector2(0.5f, 0.5f));
        AddVerticalLayout(card, 20f, new RectOffset(42, 42, 34, 34), TextAnchor.UpperCenter);
        CreateText(card.transform, "Seleccionar nivel", 44f, FontStyles.Bold, Ink, TextAlignmentOptions.Center);

        GameObject paginationRow = new GameObject("PaginationRow", typeof(RectTransform), typeof(LayoutElement));
        paginationRow.transform.SetParent(card.transform, false);
        SetPreferredSize(paginationRow, 980f, 170f);

        Button previousButton = CreateButton(paginationRow.transform, "<", null, Accent);
        PlaceInPaginationRow(previousButton.GetComponent<RectTransform>(), new Vector2(-431f, 0f), new Vector2(82f, 132f));

        GameObject gridFrame = new GameObject("LevelGridFrame", typeof(RectTransform), typeof(Image));
        gridFrame.transform.SetParent(paginationRow.transform, false);
        gridFrame.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.35f);
        PlaceInPaginationRow(gridFrame.GetComponent<RectTransform>(), Vector2.zero, new Vector2(690f, 170f));

        Button nextButton = CreateButton(paginationRow.transform, ">", null, Accent);
        PlaceInPaginationRow(nextButton.GetComponent<RectTransform>(), new Vector2(431f, 0f), new Vector2(82f, 132f));

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup));
        content.transform.SetParent(gridFrame.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        Stretch(contentRect);
        contentRect.offsetMin = new Vector2(24f, 24f);
        contentRect.offsetMax = new Vector2(-24f, -24f);

        GridLayoutGroup gridLayout = content.GetComponent<GridLayoutGroup>();
        gridLayout.padding = new RectOffset(0, 0, 0, 0);
        gridLayout.spacing = new Vector2(12f, 14f);
        gridLayout.cellSize = new Vector2(116f, 122f);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = LevelSelectController.ItemsPerPage;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        TextMeshProUGUI pageLabel = CreateText(card.transform, "Mundo 1 / 1", 24f, FontStyles.Bold, Ink, TextAlignmentOptions.Center);
        SetPreferredSize(pageLabel.gameObject, 320f, 38f);

        LevelSelectController levelSelect = panel.AddComponent<LevelSelectController>();
        levelSelect.Configure(levelCatalog, content.transform, previousButton, nextButton, pageLabel);
        CreateButton(card.transform, "Volver", navigation.ShowMainMenu, Accent);
        return panel;
    }

    private GameObject BuildOptionsPanel(Transform parent, MainMenuNavigationController navigation)
    {
        GameObject panel = CreatePanel(parent, "OptionsPanel");
        GameObject card = CreateAnchoredCard(panel.transform, "OptionsCard", Vector2.zero, new Vector2(720f, 620f), new Vector2(0.5f, 0.5f));
        AddVerticalLayout(card, 20f, new RectOffset(46, 46, 40, 40), TextAnchor.UpperCenter);
        CreateText(card.transform, "Opciones", 46f, FontStyles.Bold, Ink, TextAlignmentOptions.Center);
        CreateSliderRow(card.transform, "Musica", 0.75f);
        CreateSliderRow(card.transform, "Efectos", 0.85f);
        CreateToggleRow(card.transform, "Pantalla completa", true);
        Button resetButton = CreateButton(card.transform, "Reiniciar progreso (próximamente)", null, new Color(0.72f, 0.75f, 0.71f, 1f));
        resetButton.interactable = false;
        CreateButton(card.transform, "Volver", navigation.ShowMainMenu, Accent);
        return panel;
    }

    private GameObject BuildCreditsPanel(Transform parent, MainMenuNavigationController navigation)
    {
        GameObject panel = CreatePanel(parent, "CreditsPanel");
        GameObject card = CreateAnchoredCard(panel.transform, "CreditsCard", Vector2.zero, new Vector2(800f, 620f), new Vector2(0.5f, 0.5f));
        VerticalLayoutGroup layout = AddVerticalLayout(card, 24f, new RectOffset(64, 64, 54, 54), TextAnchor.UpperCenter);
        layout.childForceExpandWidth = false;

        TextMeshProUGUI title = CreateText(card.transform, "Créditos", 46f, FontStyles.Bold, Ink, TextAlignmentOptions.Center);
        SetPreferredSize(title.gameObject, 560f, 70f);

        TextMeshProUGUI credits = CreateText(card.transform, "Diseño y desarrollo\nTeam RoMaRi\nNicolás Rodríguez - Lead Level Designer\nFelipe Riva - Lead UX/UI and Game Designer\nRamiro Masseroni - Lead Programmer\n¡Gracias por jugar!", 30f, FontStyles.Normal, Ink, TextAlignmentOptions.Center);
        credits.lineSpacing = 8f;
        credits.enableAutoSizing = true;
        credits.fontSizeMin = 22f;
        credits.fontSizeMax = 30f;
        SetPreferredSize(credits.gameObject, 560f, 300f);

        Button backButton = CreateButton(card.transform, "Volver", navigation.ShowMainMenu, Accent);
        SetPreferredSize(backButton.gameObject, 360f, 72f);
        return panel;
    }

    private void CreateSliderRow(Transform parent, string label, float value)
    {
        GameObject row = new GameObject(label, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        row.GetComponent<LayoutElement>().preferredHeight = 54f;
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;

        TextMeshProUGUI text = CreateText(row.transform, label, 26f, FontStyles.Bold, Ink, TextAlignmentOptions.Left);
        text.GetComponent<LayoutElement>().preferredWidth = 180f;

        GameObject sliderObject = new GameObject("Slider", typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
        sliderObject.transform.SetParent(row.transform, false);
        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.SetValueWithoutNotify(value);
        slider.GetComponent<LayoutElement>().preferredWidth = 360f;

        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(slider.transform, false);
        Stretch(background.GetComponent<RectTransform>());
        background.GetComponent<RectTransform>().offsetMin = new Vector2(0f, 18f);
        background.GetComponent<RectTransform>().offsetMax = new Vector2(0f, -18f);
        background.GetComponent<Image>().color = new Color(0.82f, 0.86f, 0.78f, 1f);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(slider.transform, false);
        Stretch(fillArea.GetComponent<RectTransform>());
        fillArea.GetComponent<RectTransform>().offsetMin = new Vector2(0f, 18f);
        fillArea.GetComponent<RectTransform>().offsetMax = new Vector2(0f, -18f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        Stretch(fill.GetComponent<RectTransform>());
        fill.GetComponent<Image>().color = Green;

        slider.fillRect = fill.GetComponent<RectTransform>();
    }

    private void CreateToggleRow(Transform parent, string label, bool value)
    {
        Toggle toggle = new GameObject(label, typeof(RectTransform), typeof(Toggle), typeof(LayoutElement)).GetComponent<Toggle>();
        toggle.transform.SetParent(parent, false);
        toggle.isOn = value;
        toggle.GetComponent<LayoutElement>().preferredHeight = 54f;

        GameObject box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(toggle.transform, false);
        RectTransform boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0f, 0.5f);
        boxRect.anchorMax = new Vector2(0f, 0.5f);
        boxRect.pivot = new Vector2(0f, 0.5f);
        boxRect.sizeDelta = new Vector2(34f, 34f);
        boxRect.anchoredPosition = Vector2.zero;
        Image boxImage = box.GetComponent<Image>();
        boxImage.color = new Color(1f, 0.96f, 0.84f, 1f);

        GameObject checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkmark.transform.SetParent(box.transform, false);
        Stretch(checkmark.GetComponent<RectTransform>());
        checkmark.GetComponent<RectTransform>().offsetMin = new Vector2(8f, 8f);
        checkmark.GetComponent<RectTransform>().offsetMax = new Vector2(-8f, -8f);
        Image checkmarkImage = checkmark.GetComponent<Image>();
        checkmarkImage.color = Green;
        toggle.targetGraphic = boxImage;
        toggle.graphic = checkmarkImage;

        TextMeshProUGUI text = CreateText(toggle.transform, label, 26f, FontStyles.Bold, Ink, TextAlignmentOptions.Left);
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(52f, 0f);
    }

    private static GameObject CreatePanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        Stretch(panel.GetComponent<RectTransform>());
        return panel;
    }

    private static GameObject CreateCard(Transform parent, string name, Vector2 size, Color color)
    {
        return CreateAnchoredCard(parent, name, Vector2.zero, size, new Vector2(0.5f, 0.5f), color);
    }

    private static GameObject CreateAnchoredCard(Transform parent, string name, Vector2 position, Vector2 size, Vector2 anchor)
    {
        return CreateAnchoredCard(parent, name, position, size, anchor, Panel);
    }

    private static GameObject CreateAnchoredCard(Transform parent, string name, Vector2 position, Vector2 size, Vector2 anchor, Color color)
    {
        GameObject card = CreateAnchored(parent, name, position, size, anchor);
        Image image = card.AddComponent<Image>();
        image.color = color;
        return card;
    }

    private static GameObject CreateAnchored(Transform parent, string name, Vector2 position, Vector2 size, Vector2 anchor)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = anchor;
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;
        return gameObject;
    }

    private static void AddBand(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject band = new GameObject(name, typeof(RectTransform), typeof(Image));
        band.transform.SetParent(parent, false);
        RectTransform rectTransform = band.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        band.GetComponent<Image>().color = color;
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

    private static Button CreateButton(Transform parent, string label, UnityAction action, Color color)
    {
        GameObject buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<Image>().color = color;
        buttonObject.GetComponent<LayoutElement>().preferredHeight = 72f;

        Button button = buttonObject.GetComponent<Button>();

        if (action != null)
        {
            button.onClick.AddListener(action);
        }

        TextMeshProUGUI text = CreateText(buttonObject.transform, label, 30f, FontStyles.Bold, Ink, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        return button;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string value, float size, FontStyles style, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.GetComponent<LayoutElement>().preferredHeight = Mathf.Max(48f, size * 1.45f);
        return text;
    }

    private static void SetPreferredSize(GameObject target, float width, float height)
    {
        LayoutElement layoutElement = target.GetComponent<LayoutElement>();

        if (layoutElement == null)
        {
            layoutElement = target.AddComponent<LayoutElement>();
        }

        layoutElement.preferredWidth = width;
        layoutElement.preferredHeight = height;
    }

    private static void PlaceInPaginationRow(RectTransform rectTransform, Vector2 position, Vector2 size)
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
