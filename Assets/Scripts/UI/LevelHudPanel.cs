using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class LevelHudPanel : MonoBehaviour
{
    private const string ResetTooltip = "Reiniciar el nivel";
    private const string PauseTooltip = "Pausar el juego";
    private const string PauseButtonName = "PauseButton";
    private const string ResetButtonName = "ResetLevelButton";
    private const string TooltipName = "HudTooltip";
    private const string ButtonIconName = "Icon";

    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private PauseMenuManager pauseMenuManager;
    [SerializeField] private TextMeshProUGUI levelTitleText;
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private Button pauseButton;
    [SerializeField] private ResetLevelButtonController resetLevelButton;
    [SerializeField] private Vector2 resetButtonSize = new Vector2(48f, 48f);
    [SerializeField] private Vector2 resetButtonOffset = new Vector2(-18f, -18f);
    [SerializeField] private Vector2 pauseButtonSize = new Vector2(48f, 48f);
    [SerializeField] private Vector2 pauseButtonOffset = new Vector2(-76f, -18f);

    private static Sprite pauseIconSprite;
    private static Sprite resetIconSprite;

    private RectTransform tooltipRoot;

    private void Awake()
    {
        if (gameStateManager == null)
        {
            gameStateManager = GameStateManager.FindOrCreate();
        }

        if (pauseMenuManager == null)
        {
            pauseMenuManager = FindFirstObjectByType<PauseMenuManager>();
        }

        EnsureLayout();
        ResolveLevelTitleText();
        EnsureTooltip();

        pauseButton = ResolveButton(pauseButton, PauseButtonName);
        resetLevelButton = ResolveResetButton(resetLevelButton);

        if (pauseButton == null)
        {
            pauseButton = CreatePauseButton(transform);
        }

        if (resetLevelButton == null)
        {
            resetLevelButton = CreateResetButton(transform);
        }

        ConfigureHudButton(pauseButton, HudIconKind.Pause, PauseTooltip);
        ConfigureHudButton(resetLevelButton != null ? resetLevelButton.GetComponent<Button>() : null, HudIconKind.Reset, ResetTooltip);
        resetLevelButton.SetGameStateManager(gameStateManager);
        RefreshLevelTitle();
    }

    private void Start()
    {
        RefreshLevelTitle();
    }

    public void RefreshLevelTitle()
    {
        if (levelTitleText == null)
        {
            return;
        }

        LevelDefinition levelDefinition = gameStateManager != null ? gameStateManager.CurrentLevelDefinition : null;

        if (TryGetLevelNumbers(levelDefinition, out int worldNumber, out int levelNumber))
        {
            levelTitleText.text = $"Mundo {worldNumber} - Nivel {levelNumber}";
            return;
        }

        if (TryGetLevelNumbers(SceneManager.GetActiveScene().name, out worldNumber, out levelNumber))
        {
            levelTitleText.text = $"Mundo {worldNumber} - Nivel {levelNumber}";
            return;
        }

        if (levelDefinition != null)
        {
            levelTitleText.text = levelDefinition.LevelName;
        }
    }

    private void HandlePauseButtonClicked()
    {
        if (pauseMenuManager == null)
        {
            pauseMenuManager = FindFirstObjectByType<PauseMenuManager>();
        }

        if (pauseMenuManager != null)
        {
            pauseMenuManager.PauseButton();
        }
    }

    private void ShowTooltip(string message, RectTransform source)
    {
        if (tooltipText == null || source == null)
        {
            return;
        }

        RectTransform targetTooltipRoot = tooltipRoot != null ? tooltipRoot : tooltipText.rectTransform;
        tooltipText.text = message;
        targetTooltipRoot.gameObject.SetActive(true);

        targetTooltipRoot.anchoredPosition = new Vector2(
            source.anchoredPosition.x - source.sizeDelta.x * 0.5f,
            source.anchoredPosition.y - source.sizeDelta.y - 10f);
    }

    private void HideTooltip()
    {
        if (tooltipText != null)
        {
            RectTransform targetTooltipRoot = tooltipRoot != null ? tooltipRoot : tooltipText.rectTransform;
            targetTooltipRoot.gameObject.SetActive(false);
        }
    }

    private void EnsureLayout()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(0f, 72f);
    }

    private void ResolveLevelTitleText()
    {
        if (levelTitleText != null)
        {
            return;
        }

        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null || texts[i].gameObject.name == TooltipName)
            {
                continue;
            }

            if (texts[i].text.Contains("Mundo") || texts[i].gameObject.name == "Text (TMP)")
            {
                levelTitleText = texts[i];
                return;
            }
        }

        if (texts.Length > 0)
        {
            levelTitleText = texts[0];
        }
    }

    private void EnsureTooltip()
    {
        if (tooltipText != null)
        {
            tooltipRoot = tooltipText.transform.parent is RectTransform parentRect ? parentRect : tooltipText.rectTransform;
            tooltipRoot.gameObject.SetActive(false);
            return;
        }

        GameObject tooltipObject = new GameObject(TooltipName, typeof(RectTransform), typeof(Image));
        tooltipObject.transform.SetParent(transform, false);

        tooltipRoot = tooltipObject.GetComponent<RectTransform>();
        tooltipRoot.anchorMin = new Vector2(1f, 1f);
        tooltipRoot.anchorMax = new Vector2(1f, 1f);
        tooltipRoot.pivot = new Vector2(1f, 1f);
        tooltipRoot.sizeDelta = new Vector2(210f, 34f);
        tooltipRoot.anchoredPosition = new Vector2(-18f, -76f);

        Image background = tooltipObject.GetComponent<Image>();
        background.color = new Color(0.08f, 0.1f, 0.09f, 0.92f);
        background.raycastTarget = false;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(tooltipObject.transform, false);

        RectTransform labelTransform = labelObject.GetComponent<RectTransform>();
        labelTransform.anchorMin = Vector2.zero;
        labelTransform.anchorMax = Vector2.one;
        labelTransform.offsetMin = Vector2.zero;
        labelTransform.offsetMax = Vector2.zero;

        tooltipText = labelObject.GetComponent<TextMeshProUGUI>();
        tooltipText.text = string.Empty;
        tooltipText.fontSize = 18f;
        tooltipText.fontStyle = FontStyles.Bold;
        tooltipText.color = Color.white;
        tooltipText.alignment = TextAlignmentOptions.Center;
        tooltipText.raycastTarget = false;
        tooltipText.textWrappingMode = TextWrappingModes.NoWrap;
        tooltipText.overflowMode = TextOverflowModes.Ellipsis;
        tooltipObject.SetActive(false);
    }

    private static bool TryGetLevelNumbers(LevelDefinition levelDefinition, out int worldNumber, out int levelNumber)
    {
        worldNumber = 0;
        levelNumber = 0;

        if (levelDefinition == null)
        {
            return false;
        }

        return TryGetLevelNumbers(levelDefinition.LevelId, out worldNumber, out levelNumber) ||
            TryGetLevelNumbers(levelDefinition.name, out worldNumber, out levelNumber);
    }

    private static bool TryGetLevelNumbers(string levelId, out int worldNumber, out int levelNumber)
    {
        worldNumber = 0;
        levelNumber = 0;

        if (string.IsNullOrWhiteSpace(levelId))
        {
            return false;
        }

        string[] parts = levelId.Split('_');

        if (parts.Length < 3)
        {
            return false;
        }

        return int.TryParse(parts[1], out worldNumber) &&
            int.TryParse(parts[2], out levelNumber);
    }

    private Button CreatePauseButton(Transform parent)
    {
        Button button = CreateHudButton(parent, PauseButtonName, pauseButtonSize, pauseButtonOffset);
        button.onClick.AddListener(HandlePauseButtonClicked);
        return button;
    }

    private ResetLevelButtonController CreateResetButton(Transform parent)
    {
        Button button = CreateHudButton(parent, ResetButtonName, resetButtonSize, resetButtonOffset);
        return button.gameObject.AddComponent<ResetLevelButtonController>();
    }

    private Button CreateHudButton(Transform parent, string objectName, Vector2 size, Vector2 offset)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonTransform = buttonObject.GetComponent<RectTransform>();
        buttonTransform.anchorMin = new Vector2(1f, 1f);
        buttonTransform.anchorMax = new Vector2(1f, 1f);
        buttonTransform.pivot = new Vector2(1f, 1f);
        buttonTransform.sizeDelta = size;
        buttonTransform.anchoredPosition = offset;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.08f, 0.1f, 0.09f, 0.88f);

        return buttonObject.GetComponent<Button>();
    }

    private Button ResolveButton(Button current, string childName)
    {
        if (current != null)
        {
            return current;
        }

        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private ResetLevelButtonController ResolveResetButton(ResetLevelButtonController current)
    {
        if (current != null)
        {
            return current;
        }

        Transform child = transform.Find(ResetButtonName);

        if (child == null)
        {
            return null;
        }

        ResetLevelButtonController controller = child.GetComponent<ResetLevelButtonController>();
        return controller != null ? controller : child.gameObject.AddComponent<ResetLevelButtonController>();
    }

    private void ConfigureHudButton(Button button, HudIconKind iconKind, string tooltip)
    {
        if (button == null)
        {
            return;
        }

        Image background = button.GetComponent<Image>();

        if (background != null)
        {
            background.color = new Color(0.08f, 0.1f, 0.09f, 0.88f);
        }

        EnsureButtonIcon(button, iconKind);
        HideTextLabels(button);

        RectTransform buttonTransform = button.GetComponent<RectTransform>();
        EventTrigger trigger = button.GetComponent<EventTrigger>();

        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        trigger.triggers.Clear();
        AddTooltipEvent(trigger, EventTriggerType.PointerEnter, () => ShowTooltip(tooltip, buttonTransform));
        AddTooltipEvent(trigger, EventTriggerType.PointerExit, HideTooltip);
    }

    private static void HideTextLabels(Button button)
    {
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);

        if (label != null)
        {
            label.gameObject.SetActive(false);
        }

        Text legacyLabel = button.GetComponentInChildren<Text>(true);

        if (legacyLabel != null)
        {
            legacyLabel.gameObject.SetActive(false);
        }
    }

    private static void EnsureButtonIcon(Button button, HudIconKind iconKind)
    {
        Transform iconTransform = button.transform.Find(ButtonIconName);
        Image iconImage;

        if (iconTransform == null)
        {
            GameObject iconObject = new GameObject(ButtonIconName, typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(button.transform, false);
            iconImage = iconObject.GetComponent<Image>();
        }
        else
        {
            iconImage = iconTransform.GetComponent<Image>();

            if (iconImage == null)
            {
                iconImage = iconTransform.gameObject.AddComponent<Image>();
            }
        }

        RectTransform iconRect = iconImage.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(30f, 30f);

        iconImage.color = Color.white;
        iconImage.raycastTarget = false;
        iconImage.sprite = iconKind == HudIconKind.Reset ? GetResetIconSprite() : GetPauseIconSprite();
        iconImage.preserveAspect = true;
    }

    private static Sprite GetResetIconSprite()
    {
        if (resetIconSprite == null)
        {
            Texture2D texture = CreateClearTexture(64);
            Color color = Color.white;
            Vector2 center = new Vector2(32f, 32f);
            float radius = 21f;
            float arrowTipDegrees = 35f;
            DrawArcClockwise(texture, center, radius, 300f, arrowTipDegrees, color, 4);
            DrawArrowHead(texture, PointOnCircle(center, radius, arrowTipDegrees), ClockwiseTangent(arrowTipDegrees), color, 4);
            texture.Apply();
            resetIconSprite = Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 64f);
        }

        return resetIconSprite;
    }

    private static Sprite GetPauseIconSprite()
    {
        if (pauseIconSprite == null)
        {
            Texture2D texture = CreateClearTexture(64);
            Color color = Color.white;
            FillRect(texture, 20, 14, 10, 36, color);
            FillRect(texture, 34, 14, 10, 36, color);
            texture.Apply();
            pauseIconSprite = Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 64f);
        }

        return pauseIconSprite;
    }

    private static Texture2D CreateClearTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color clear = new Color(1f, 1f, 1f, 0f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, clear);
            }
        }

        return texture;
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
        const float arrowLength = 12f;
        const float arrowAngleDegrees = 36f;
        Vector2 back = -direction.normalized;
        DrawLine(texture, tip, tip + Rotate(back, arrowAngleDegrees) * arrowLength, color, thickness);
        DrawLine(texture, tip, tip + Rotate(back, -arrowAngleDegrees) * arrowLength, color, thickness);
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
        if (x < 0 || y < 0 || x >= texture.width || y >= texture.height)
        {
            return;
        }

        texture.SetPixel(x, y, color);
    }

    private static void AddTooltipEvent(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = eventType
        };
        entry.callback.AddListener(_ => action.Invoke());
        trigger.triggers.Add(entry);
    }

    private enum HudIconKind
    {
        Pause,
        Reset
    }
}
