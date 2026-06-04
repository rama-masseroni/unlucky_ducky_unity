using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DefeatScreenManager : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenuScene";
    private const string DefaultSubtitle = "\u00a1El pato se pinch\u00f3!";
    private const string PlanningTimeoutSubtitle = "Se acab\u00f3 el tiempo de planeaci\u00f3n";

    private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.68f);
    private static readonly Color CardColor = new Color(1f, 0.97f, 0.89f, 0.96f);
    private static readonly Color InkColor = new Color(0.13f, 0.16f, 0.14f, 1f);
    private static readonly Color AccentColor = new Color(1f, 0.76f, 0.25f, 1f);
    private static readonly Color RedColor = new Color(0.9f, 0.45f, 0.36f, 1f);

    [SerializeField] private GameObject container;
    [SerializeField] private TextMeshProUGUI subtitleText;

    public bool IsVisible => container != null && container.activeSelf;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterDeathHandler()
    {
        PlayerDuckController.DeathScreenHandler = ShowForPlayerDeath;
        GameStateManager.PlanningTimeoutHandler = ShowForPlanningTimeout;
    }

    public static bool ShowForPlayerDeath(PlayerDuckController _)
    {
        DefeatScreenManager manager = FindOrCreate();

        if (manager == null)
        {
            return false;
        }

        manager.Show(DefaultSubtitle);
        return true;
    }

    public static bool ShowForPlanningTimeout(string message)
    {
        DefeatScreenManager manager = FindOrCreate();

        if (manager == null)
        {
            return false;
        }

        manager.Show(string.IsNullOrWhiteSpace(message) ? PlanningTimeoutSubtitle : message);
        return true;
    }

    public static DefeatScreenManager FindOrCreate()
    {
        DefeatScreenManager existing = FindFirstObjectByType<DefeatScreenManager>();

        if (existing != null)
        {
            return existing;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("DefeatScreenCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        GameObject managerObject = new GameObject("DefeatScreenManager", typeof(RectTransform), typeof(DefeatScreenManager));
        managerObject.transform.SetParent(canvas.transform, false);
        Stretch(managerObject.GetComponent<RectTransform>());
        return managerObject.GetComponent<DefeatScreenManager>();
    }

    private void Awake()
    {
        EnsureLayout();
        Hide();
    }

    public void Show()
    {
        Show(DefaultSubtitle);
    }

    public void Show(string subtitle)
    {
        EnsureLayout();
        if (subtitleText != null)
        {
            subtitleText.text = string.IsNullOrWhiteSpace(subtitle) ? DefaultSubtitle : subtitle;
        }

        container.SetActive(true);
        container.transform.SetAsLastSibling();
        Time.timeScale = 0f;
    }

    public void RetryButton()
    {
        Time.timeScale = 1f;
        Hide();
        GameStateManager gameStateManager = GameStateManager.FindOrCreate();

        if (gameStateManager != null)
        {
            gameStateManager.ResetCurrentLevel();
        }
    }

    public void ReturnToMainMenuButton()
    {
        Time.timeScale = 1f;
        Hide();
        SceneManager.LoadScene(MainMenuSceneName);
    }

    private void Hide()
    {
        if (container != null)
        {
            container.SetActive(false);
        }
    }

    private void EnsureLayout()
    {
        if (container != null)
        {
            return;
        }

        container = new GameObject("DefeatScreenContainer", typeof(RectTransform), typeof(Image)).gameObject;
        container.transform.SetParent(transform, false);
        Stretch(container.GetComponent<RectTransform>());

        Image overlay = container.GetComponent<Image>();
        overlay.color = OverlayColor;
        overlay.raycastTarget = true;

        GameObject card = new GameObject("DefeatCard", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(container.transform, false);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(560f, 380f);

        Image cardImage = card.GetComponent<Image>();
        cardImage.color = CardColor;

        TextMeshProUGUI title = CreateText(card.transform, "Derrota", 54f, FontStyles.Bold, InkColor);
        Place(title.rectTransform, new Vector2(0f, 100f), new Vector2(460f, 68f));

        subtitleText = CreateText(card.transform, DefaultSubtitle, 30f, FontStyles.Bold, InkColor);
        Place(subtitleText.rectTransform, new Vector2(0f, 38f), new Vector2(460f, 44f));

        Button retryButton = CreateButton(card.transform, "Reintentar", RetryButton, AccentColor);
        Place(retryButton.GetComponent<RectTransform>(), new Vector2(0f, -42f), new Vector2(400f, 62f));

        Button menuButton = CreateButton(card.transform, "Volver al menu", ReturnToMainMenuButton, RedColor);
        Place(menuButton.GetComponent<RectTransform>(), new Vector2(0f, -122f), new Vector2(400f, 62f));
    }

    private static Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action, Color color)
    {
        GameObject buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = color;
        image.type = Image.Type.Sliced;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        TextMeshProUGUI text = CreateText(buttonObject.transform, label, 26f, FontStyles.Bold, InkColor);
        Stretch(text.rectTransform);
        text.raycastTarget = false;
        return button;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string value, float size, FontStyles style, Color color)
    {
        GameObject textObject = new GameObject("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void Place(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size)
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
    }
}
