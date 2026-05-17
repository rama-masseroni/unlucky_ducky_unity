using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class LevelHudPanel : MonoBehaviour
{
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private ResetLevelButtonController resetLevelButton;
    [SerializeField] private Vector2 resetButtonSize = new Vector2(48f, 48f);
    [SerializeField] private Vector2 resetButtonOffset = new Vector2(-18f, -18f);

    private void Awake()
    {
        if (gameStateManager == null)
        {
            gameStateManager = GameStateManager.FindOrCreate();
        }

        EnsureLayout();

        if (resetLevelButton == null)
        {
            resetLevelButton = CreateResetButton(transform);
        }

        resetLevelButton.SetGameStateManager(gameStateManager);
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

    private ResetLevelButtonController CreateResetButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("ResetLevelButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonTransform = buttonObject.GetComponent<RectTransform>();
        buttonTransform.anchorMin = new Vector2(1f, 1f);
        buttonTransform.anchorMax = new Vector2(1f, 1f);
        buttonTransform.pivot = new Vector2(1f, 1f);
        buttonTransform.sizeDelta = resetButtonSize;
        buttonTransform.anchoredPosition = resetButtonOffset;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.92f);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelTransform = labelObject.GetComponent<RectTransform>();
        labelTransform.anchorMin = Vector2.zero;
        labelTransform.anchorMax = Vector2.one;
        labelTransform.offsetMin = Vector2.zero;
        labelTransform.offsetMax = Vector2.zero;

        Text label = labelObject.GetComponent<Text>();
        label.text = "R";
        label.font = GetBuiltInFont();
        label.fontSize = 20;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.black;

        return buttonObject.AddComponent<ResetLevelButtonController>();
    }

    private Font GetBuiltInFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
