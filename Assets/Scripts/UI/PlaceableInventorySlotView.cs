using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class PlaceableInventorySlotView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private PlaceableDefinition definition;
    private PlaceableInventoryEntry inventoryEntry;
    private BuildModePlacementController placementController;
    private int amount;
    private Image background;
    private Text amountText;
    private bool isDragging;

    public UnityEvent<PlaceableInventorySlotView> Clicked { get; } = new UnityEvent<PlaceableInventorySlotView>();

    public PlaceableDefinition Definition => definition;
    public int Amount => amount;

    public static PlaceableInventorySlotView Create(
        Transform parent,
        PlaceableInventoryEntry entry,
        BuildModePlacementController placementController)
    {
        GameObject slotObject = new GameObject(entry.Definition.DisplayName, typeof(RectTransform), typeof(Image), typeof(Button));
        slotObject.transform.SetParent(parent, false);

        Image slotBackground = slotObject.GetComponent<Image>();
        slotBackground.color = Color.white;

        Button button = slotObject.GetComponent<Button>();
        button.interactable = entry.Amount > 0;

        LayoutElement layoutElement = slotObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 72f;

        PlaceableInventorySlotView slotView = slotObject.AddComponent<PlaceableInventorySlotView>();
        slotView.definition = entry.Definition;
        slotView.inventoryEntry = entry;
        slotView.placementController = placementController;
        slotView.amount = entry.Amount;
        slotView.background = slotBackground;
        slotView.Build();
        button.onClick.AddListener(slotView.NotifyClicked);

        return slotView;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanStartDrag())
        {
            return;
        }

        isDragging = true;
        Clicked.Invoke(this);
        placementController.BeginDrag(definition);
        placementController.UpdateDrag(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || placementController == null)
        {
            return;
        }

        placementController.UpdateDrag(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging || placementController == null)
        {
            return;
        }

        isDragging = false;
        bool placed = placementController.EndDrag();

        if (placed && inventoryEntry.TryConsumeOne())
        {
            amount = inventoryEntry.Amount;
            RefreshAmount();
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (background == null)
        {
            return;
        }

        background.color = isSelected
            ? new Color(0.86f, 0.92f, 1f, 1f)
            : Color.white;
    }

    private void Build()
    {
        HorizontalLayoutGroup layout = gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 6, 6);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;

        CreateIcon();
        CreateTextBlock();
    }

    private void NotifyClicked()
    {
        Clicked.Invoke(this);
    }

    private bool CanStartDrag()
    {
        return amount > 0
            && definition != null
            && definition.Prefab != null
            && placementController != null;
    }

    private void CreateIcon()
    {
        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(transform, false);

        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.sprite = definition.Icon;
        iconImage.preserveAspect = true;
        iconImage.color = definition.Icon != null ? Color.white : new Color(0.2f, 0.2f, 0.2f, 1f);

        LayoutElement layoutElement = iconObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 42f;
        layoutElement.preferredHeight = 42f;
    }

    private void CreateTextBlock()
    {
        GameObject textBlockObject = new GameObject("Text", typeof(RectTransform), typeof(VerticalLayoutGroup));
        textBlockObject.transform.SetParent(transform, false);

        VerticalLayoutGroup layout = textBlockObject.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 2f;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        LayoutElement blockLayout = textBlockObject.AddComponent<LayoutElement>();
        blockLayout.flexibleWidth = 1f;

        CreateText(textBlockObject.transform, definition.DisplayName, 12, FontStyle.Bold);
        amountText = CreateText(textBlockObject.transform, amount.ToString(), 16, FontStyle.Normal);
    }

    private Text CreateText(Transform parent, string content, int fontSize, FontStyle fontStyle)
    {
        GameObject textObject = new GameObject(content, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.text = content;
        text.font = GetBuiltInFont();
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.black;

        LayoutElement layoutElement = textObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = fontSize + 6f;

        return text;
    }

    private void RefreshAmount()
    {
        if (amountText != null)
        {
            amountText.text = amount.ToString();
        }

        Button button = GetComponent<Button>();

        if (button != null)
        {
            button.interactable = amount > 0;
        }
    }

    private Font GetBuiltInFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
