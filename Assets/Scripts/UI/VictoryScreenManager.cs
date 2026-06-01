using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VictoryScreenManager : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenuScene";

    private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.68f);
    private static readonly Color CardColor = new Color(1f, 0.97f, 0.89f, 0.96f);
    private static readonly Color InkColor = new Color(0.13f, 0.16f, 0.14f, 1f);
    private static readonly Color AccentColor = new Color(1f, 0.76f, 0.25f, 1f);
    private static readonly Color GreenColor = new Color(0.24f, 0.72f, 0.38f, 1f);
    private static readonly Color RedColor = new Color(0.9f, 0.45f, 0.36f, 1f);

    [SerializeField] private GameObject container;

    private string nextSceneName;
    private Action<string> continueAction;

    public bool IsVisible => container != null && container.activeSelf;
    public string NextSceneName => nextSceneName;

    public static VictoryScreenManager FindOrCreate()
    {
        VictoryScreenManager existing = FindFirstObjectByType<VictoryScreenManager>();

        if (existing != null)
        {
            return existing;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("VictoryScreenCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        GameObject managerObject = new GameObject("VictoryScreenManager", typeof(RectTransform), typeof(VictoryScreenManager));
        managerObject.transform.SetParent(canvas.transform, false);
        Stretch(managerObject.GetComponent<RectTransform>());
        return managerObject.GetComponent<VictoryScreenManager>();
    }

    private void Awake()
    {
        EnsureLayout();
        Hide();
    }

    public void Show(string sceneName, Action<string> onContinue)
    {
        nextSceneName = sceneName;
        continueAction = onContinue;
        EnsureLayout();
        container.SetActive(true);
        container.transform.SetAsLastSibling();
        Time.timeScale = 0f;
    }

    public void ContinueButton()
    {
        Time.timeScale = 1f;
        Hide();

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            SceneManager.LoadScene(MainMenuSceneName);
            return;
        }

        if (continueAction != null)
        {
            continueAction.Invoke(nextSceneName);
            return;
        }

        SceneManager.LoadScene(nextSceneName);
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

        container = new GameObject("VictoryScreenContainer", typeof(RectTransform), typeof(Image)).gameObject;
        container.transform.SetParent(transform, false);
        Stretch(container.GetComponent<RectTransform>());

        Image overlay = container.GetComponent<Image>();
        overlay.color = OverlayColor;
        overlay.raycastTarget = true;

        GameObject card = new GameObject("VictoryCard", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(container.transform, false);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(620f, 520f);

        Image cardImage = card.GetComponent<Image>();
        cardImage.color = CardColor;

        TextMeshProUGUI title = CreateText(card.transform, "Victoria", 54f, FontStyles.Bold, InkColor);
        Place(title.rectTransform, new Vector2(0f, 170f), new Vector2(500f, 68f));

        TextMeshProUGUI subtitle = CreateText(card.transform, "\u00a1Nivel completado!", 30f, FontStyles.Bold, InkColor);
        Place(subtitle.rectTransform, new Vector2(0f, 104f), new Vector2(500f, 44f));

        Button continueButton = CreateButton(card.transform, "Continuar", ContinueButton, GreenColor);
        Place(continueButton.GetComponent<RectTransform>(), new Vector2(0f, 28f), new Vector2(420f, 62f));

        Button retryButton = CreateButton(card.transform, "Reintentar", RetryButton, AccentColor);
        Place(retryButton.GetComponent<RectTransform>(), new Vector2(0f, -52f), new Vector2(420f, 62f));

        Button menuButton = CreateButton(card.transform, "Volver al menu", ReturnToMainMenuButton, RedColor);
        Place(menuButton.GetComponent<RectTransform>(), new Vector2(0f, -132f), new Vector2(420f, 62f));
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

    private static void SetPreferredSize(GameObject gameObject, float width, float height)
    {
        LayoutElement layoutElement = gameObject.GetComponent<LayoutElement>();

        if (layoutElement == null)
        {
            layoutElement = gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.preferredWidth = width;
        layoutElement.preferredHeight = height;
    }
}
