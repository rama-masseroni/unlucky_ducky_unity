using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    public GameObject container;

    private const string TitleName = "Text";
    private const string ResumeButtonName = "Resume";
    private const string ResetButtonName = "Reset level";
    private const string OptionsButtonName = "Options";
    private const string MainMenuButtonName = "Go to menu";
    private const string OptionsPanelName = "OptionsPanel";

    private static readonly Color ButtonColor = new Color(1f, 1f, 1f, 1f);
    private static readonly Color ButtonTextColor = new Color(0.196f, 0.196f, 0.196f, 1f);
    private static readonly Color AccentColor = new Color(1f, 0.76f, 0.25f, 1f);
    private static readonly Color GreenColor = new Color(0.24f, 0.72f, 0.38f, 1f);

    private GameObject optionsPanel;

    private void Awake()
    {
        if (container == null)
        {
            container = gameObject;
        }

        EnsurePauseMenuUpgrade();
        ShowPauseActions();

        if (container != null)
        {
            container.SetActive(false);
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Pause();
        }
    }

    public void ResumeButton()
    {
        if (container != null)
        {
            container.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    public void PauseButton()
    {
        Pause();
    }

    private void Pause()
    {
        if (container != null)
        {
            container.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void ReturnToMainMenuButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene");
    }

    public void ResetLevelButton()
    {
        Time.timeScale = 1f;
        GameStateManager gameStateManager = GameStateManager.FindOrCreate();

        if (gameStateManager != null)
        {
            gameStateManager.ResetCurrentLevel();
        }
    }

    public void OptionsButton()
    {
        SetPauseActionsActive(false);

        if (optionsPanel != null)
        {
            optionsPanel.SetActive(true);
        }
    }

    public void BackToPauseButton()
    {
        ShowPauseActions();
    }

    private void EnsurePauseMenuUpgrade()
    {
        if (container == null)
        {
            return;
        }

        RectTransform title = FindChildRect(TitleName);
        Place(title, new Vector2(0f, 160f), new Vector2(520f, 76f));

        Button resumeButton = FindChildButton(ResumeButtonName);
        ConfigureExistingButton(resumeButton, "Continuar", ResumeButton, new Vector2(0f, 72f));

        Button resetButton = EnsureButton(ResetButtonName, "Reiniciar nivel", ResetLevelButton, new Vector2(0f, 18f), AccentColor);
        ConfigureButtonRect(resetButton, new Vector2(0f, 18f));

        Button optionsButton = EnsureButton(OptionsButtonName, "Opciones", OptionsButton, new Vector2(0f, -36f), ButtonColor);
        ConfigureButtonRect(optionsButton, new Vector2(0f, -36f));

        Button mainMenuButton = FindChildButton(MainMenuButtonName);
        ConfigureExistingButton(mainMenuButton, "Volver al menu", ReturnToMainMenuButton, new Vector2(0f, -90f));

        EnsureOptionsPanel();
    }

    private void EnsureOptionsPanel()
    {
        if (container == null)
        {
            return;
        }

        Transform existing = container.transform.Find(OptionsPanelName);
        optionsPanel = existing != null ? existing.gameObject : CreateOptionsPanel(container.transform);
        optionsPanel.SetActive(false);
    }

    private GameObject CreateOptionsPanel(Transform parent)
    {
        GameObject panel = new GameObject(OptionsPanelName, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        panel.transform.SetParent(parent, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(520f, 420f);

        Image background = panel.GetComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.18f);
        background.raycastTarget = true;

        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(48, 48, 36, 36);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI title = CreateText(panel.transform, "Opciones", 42f, FontStyles.Bold, Color.white);
        SetPreferredSize(title.gameObject, 420f, 64f);
        CreateSliderRow(panel.transform, "Musica", 0.75f);
        CreateSliderRow(panel.transform, "Efectos", 0.85f);
        CreateToggleRow(panel.transform, "Pantalla completa", true);
        Button backButton = CreateButton(panel.transform, "Volver", BackToPauseButton, AccentColor);
        SetPreferredSize(backButton.gameObject, 320f, 48f);
        return panel;
    }

    private void ShowPauseActions()
    {
        SetPauseActionsActive(true);

        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }
    }

    private void SetPauseActionsActive(bool active)
    {
        SetChildActive(TitleName, active);
        SetChildActive(ResumeButtonName, active);
        SetChildActive(ResetButtonName, active);
        SetChildActive(OptionsButtonName, active);
        SetChildActive(MainMenuButtonName, active);
    }

    private Button EnsureButton(string name, string label, UnityEngine.Events.UnityAction action, Vector2 anchoredPosition, Color color)
    {
        Button button = FindChildButton(name);

        if (button == null)
        {
            button = CreateButton(container.transform, label, action, color);
            button.gameObject.name = name;
        }
        else
        {
            SetButtonLabel(button, label);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
            SetButtonColor(button, color);
        }

        ConfigureButtonRect(button, anchoredPosition);
        return button;
    }

    private Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action, Color color)
    {
        GameObject buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        Button button = buttonObject.GetComponent<Button>();
        Image image = buttonObject.GetComponent<Image>();
        image.color = color;
        image.type = Image.Type.Sliced;
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        TextMeshProUGUI text = CreateText(buttonObject.transform, label, 24f, FontStyles.Normal, ButtonTextColor);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private void ConfigureExistingButton(Button button, string label, UnityEngine.Events.UnityAction action, Vector2 anchoredPosition)
    {
        if (button == null)
        {
            EnsureButton(label, label, action, anchoredPosition, ButtonColor);
            return;
        }

        SetButtonLabel(button, label);
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
        SetButtonColor(button, ButtonColor);
        ConfigureButtonRect(button, anchoredPosition);
    }

    private static void ConfigureButtonRect(Button button, Vector2 anchoredPosition)
    {
        if (button == null)
        {
            return;
        }

        Place(button.GetComponent<RectTransform>(), anchoredPosition, new Vector2(320f, 46f));

        LayoutElement layoutElement = button.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.preferredWidth = 320f;
            layoutElement.preferredHeight = 46f;
        }
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

    private void CreateSliderRow(Transform parent, string label, float value)
    {
        GameObject row = new GameObject(label, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        SetPreferredSize(row, 420f, 46f);

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;

        TextMeshProUGUI text = CreateText(row.transform, label, 22f, FontStyles.Bold, Color.white);
        SetPreferredSize(text.gameObject, 160f, 42f);
        text.alignment = TextAlignmentOptions.Left;

        Slider slider = new GameObject("Slider", typeof(RectTransform), typeof(Slider), typeof(LayoutElement)).GetComponent<Slider>();
        slider.transform.SetParent(row.transform, false);
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(value);
        SetPreferredSize(slider.gameObject, 220f, 42f);

        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(slider.transform, false);
        Stretch(background.GetComponent<RectTransform>());
        background.GetComponent<RectTransform>().offsetMin = new Vector2(0f, 17f);
        background.GetComponent<RectTransform>().offsetMax = new Vector2(0f, -17f);
        background.GetComponent<Image>().color = new Color(0.82f, 0.86f, 0.78f, 1f);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(slider.transform, false);
        Stretch(fillArea.GetComponent<RectTransform>());
        fillArea.GetComponent<RectTransform>().offsetMin = new Vector2(0f, 17f);
        fillArea.GetComponent<RectTransform>().offsetMax = new Vector2(0f, -17f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        Stretch(fill.GetComponent<RectTransform>());
        fill.GetComponent<Image>().color = GreenColor;
        slider.fillRect = fill.GetComponent<RectTransform>();
    }

    private void CreateToggleRow(Transform parent, string label, bool value)
    {
        Toggle toggle = new GameObject(label, typeof(RectTransform), typeof(Toggle), typeof(LayoutElement)).GetComponent<Toggle>();
        toggle.transform.SetParent(parent, false);
        toggle.isOn = value;
        SetPreferredSize(toggle.gameObject, 420f, 46f);

        GameObject box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(toggle.transform, false);
        RectTransform boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0f, 0.5f);
        boxRect.anchorMax = new Vector2(0f, 0.5f);
        boxRect.pivot = new Vector2(0f, 0.5f);
        boxRect.sizeDelta = new Vector2(32f, 32f);
        boxRect.anchoredPosition = Vector2.zero;
        Image boxImage = box.GetComponent<Image>();
        boxImage.color = ButtonColor;

        GameObject checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkmark.transform.SetParent(box.transform, false);
        Stretch(checkmark.GetComponent<RectTransform>());
        checkmark.GetComponent<RectTransform>().offsetMin = new Vector2(8f, 8f);
        checkmark.GetComponent<RectTransform>().offsetMax = new Vector2(-8f, -8f);
        Image checkmarkImage = checkmark.GetComponent<Image>();
        checkmarkImage.color = GreenColor;
        toggle.targetGraphic = boxImage;
        toggle.graphic = checkmarkImage;

        TextMeshProUGUI text = CreateText(toggle.transform, label, 22f, FontStyles.Bold, Color.white);
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(52f, 0f);
        text.alignment = TextAlignmentOptions.Left;
    }

    private Button FindChildButton(string childName)
    {
        if (container == null)
        {
            return null;
        }

        Transform child = container.transform.Find(childName);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private RectTransform FindChildRect(string childName)
    {
        if (container == null)
        {
            return null;
        }

        Transform child = container.transform.Find(childName);
        return child != null ? child.GetComponent<RectTransform>() : null;
    }

    private void SetChildActive(string childName, bool active)
    {
        if (container == null)
        {
            return;
        }

        Transform child = container.transform.Find(childName);

        if (child != null)
        {
            child.gameObject.SetActive(active);
        }
    }

    private static void SetButtonLabel(Button button, string label)
    {
        if (button == null)
        {
            return;
        }

        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);

        if (text != null)
        {
            text.text = label;
            text.fontSize = 24f;
            text.color = ButtonTextColor;
            text.alignment = TextAlignmentOptions.Center;
        }
    }

    private static void SetButtonColor(Button button, Color color)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();

        if (image != null)
        {
            image.color = color;
        }
    }

    private static void Place(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
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
