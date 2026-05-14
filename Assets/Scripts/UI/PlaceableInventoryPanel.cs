using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class PlaceableInventoryPanel : MonoBehaviour
{
    [SerializeField] private PlaceableInventorySet inventorySet;
    [SerializeField] private RectTransform slotsRoot;
    [SerializeField] private string title = "OBJETOS";
    [SerializeField] private Vector2 panelSize = new Vector2(180f, 360f);
    [SerializeField] private Vector2 panelOffset = new Vector2(-18f, 0f);
    [SerializeField] private BuildModePlacementController placementController;

    private readonly List<PlaceableInventorySlotView> slotViews = new List<PlaceableInventorySlotView>();
    private PlaceableInventorySlotView selectedSlot;

    public PlaceableDefinition SelectedDefinition => selectedSlot != null ? selectedSlot.Definition : null;

    private void Awake()
    {
        EnsurePanelLayout();

        if (placementController == null)
        {
            placementController = FindFirstObjectByType<BuildModePlacementController>();
        }
    }

    private void Start()
    {
        Rebuild();
    }

    public void SetInventorySet(PlaceableInventorySet newInventorySet)
    {
        inventorySet = newInventorySet;
        Rebuild();
    }

    public void Rebuild()
    {
        if (slotsRoot == null)
        {
            return;
        }

        ClearSlots();

        if (inventorySet == null)
        {
            return;
        }

        IReadOnlyList<PlaceableInventoryEntry> entries = inventorySet.Entries;

        for (int i = 0; i < entries.Count; i++)
        {
            PlaceableInventoryEntry entry = entries[i];

            if (entry == null || entry.Definition == null)
            {
                continue;
            }

            PlaceableInventorySlotView slotView = PlaceableInventorySlotView.Create(slotsRoot, entry, placementController);
            slotView.Clicked.AddListener(SelectSlot);
            slotViews.Add(slotView);
        }
    }

    private void SelectSlot(PlaceableInventorySlotView slotView)
    {
        selectedSlot = slotView;

        for (int i = 0; i < slotViews.Count; i++)
        {
            slotViews[i].SetSelected(slotViews[i] == selectedSlot);
        }
    }

    private void ClearSlots()
    {
        slotViews.Clear();

        for (int i = slotsRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(slotsRoot.GetChild(i).gameObject);
        }
    }

    private void EnsurePanelLayout()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();

        if (rectTransform == null)
        {
            rectTransform = gameObject.AddComponent<RectTransform>();
        }

        rectTransform.anchorMin = new Vector2(1f, 0.5f);
        rectTransform.anchorMax = new Vector2(1f, 0.5f);
        rectTransform.pivot = new Vector2(1f, 0.5f);
        rectTransform.sizeDelta = panelSize;
        rectTransform.anchoredPosition = panelOffset;

        Image panelImage = GetComponent<Image>();

        if (panelImage == null)
        {
            panelImage = gameObject.AddComponent<Image>();
        }

        panelImage.color = new Color(1f, 1f, 1f, 0.92f);

        VerticalLayoutGroup panelLayout = GetComponent<VerticalLayoutGroup>();

        if (panelLayout == null)
        {
            panelLayout = gameObject.AddComponent<VerticalLayoutGroup>();
        }

        panelLayout.padding = new RectOffset(10, 10, 10, 10);
        panelLayout.spacing = 8f;
        panelLayout.childControlHeight = false;
        panelLayout.childControlWidth = true;
        panelLayout.childForceExpandHeight = false;
        panelLayout.childForceExpandWidth = true;

        if (slotsRoot == null)
        {
            CreateTitle(transform);
            slotsRoot = CreateSlotsRoot(transform);
        }
    }

    private void CreateTitle(Transform parent)
    {
        GameObject titleObject = new GameObject("Title", typeof(RectTransform), typeof(Text));
        titleObject.transform.SetParent(parent, false);

        Text titleText = titleObject.GetComponent<Text>();
        titleText.text = title;
        titleText.font = GetBuiltInFont();
        titleText.fontSize = 14;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleLeft;
        titleText.color = Color.black;

        LayoutElement layoutElement = titleObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 24f;
    }

    private Font GetBuiltInFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private RectTransform CreateSlotsRoot(Transform parent)
    {
        GameObject slotsObject = new GameObject("Slots", typeof(RectTransform), typeof(VerticalLayoutGroup));
        slotsObject.transform.SetParent(parent, false);

        VerticalLayoutGroup layout = slotsObject.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        LayoutElement layoutElement = slotsObject.AddComponent<LayoutElement>();
        layoutElement.flexibleHeight = 1f;

        return slotsObject.GetComponent<RectTransform>();
    }
}
