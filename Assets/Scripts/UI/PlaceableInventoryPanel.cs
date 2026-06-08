using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class PlaceableInventoryPanel : MonoBehaviour
{
    private const float TitleHeight = 24f;
    private const float StartButtonHeight = 40f;
    private const float BaseSlotHeight = 92f;
    private const float BaseSlotSpacing = 8f;
    private static readonly Vector2 MinimumPanelSize = new Vector2(220f, 640f);

    [SerializeField] private PlaceableInventorySet inventorySet;
    [SerializeField] private RectTransform slotsRoot;
    [SerializeField] private string title = "OBJETOS";
    [SerializeField] private Vector2 panelSize = new Vector2(220f, 640f);
    [SerializeField] private Vector2 panelOffset = new Vector2(-18f, 0f);
    [SerializeField] private BuildModePlacementController placementController;
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private StartExecutionButtonController startExecutionButton;

    private readonly List<PlaceableInventorySlotView> slotViews = new List<PlaceableInventorySlotView>();
    private PlaceableInventorySlotView selectedSlot;
    private PlaceableInventoryRuntime runtimeInventory;
    private RectTransform panelRectTransform;
    private Vector2 defaultAnchoredPosition;
    private bool hasDefaultAnchoredPosition;

    public PlaceableDefinition SelectedDefinition => selectedSlot != null ? selectedSlot.Definition : null;

    private void Awake()
    {
        if (gameStateManager == null)
        {
            gameStateManager = GameStateManager.FindOrCreate();
        }

        if (gameStateManager != null && inventorySet != null)
        {
            gameStateManager.SetFallbackInventorySet(inventorySet);
        }

        EnsurePanelLayout();

        if (placementController == null)
        {
            placementController = FindFirstObjectByType<BuildModePlacementController>();
        }

        if (startExecutionButton != null)
        {
            startExecutionButton.SetGameStateManager(gameStateManager);
        }

    }

    private void Start()
    {
        UseRuntimeInventory();
        Rebuild();
    }

    private void OnEnable()
    {
        if (gameStateManager == null)
        {
            gameStateManager = GameStateManager.FindOrCreate();
        }

        gameStateManager.PhaseChanged += HandlePhaseChanged;

    }

    private void OnDisable()
    {
        if (gameStateManager != null)
        {
            gameStateManager.PhaseChanged -= HandlePhaseChanged;
        }

        if (runtimeInventory != null)
        {
            runtimeInventory.Changed -= HandleInventoryChanged;
        }
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

        if (runtimeInventory == null)
        {
            return;
        }

        List<PlaceableInventoryRuntimeEntry> entries = GetVisibleEntries();
        SlotLayout slotLayout = CalculateSlotLayout(entries.Count);
        ApplySlotsRootLayout(slotLayout);

        for (int i = 0; i < entries.Count; i++)
        {
            PlaceableInventoryRuntimeEntry entry = entries[i];

            PlaceableInventorySlotView slotView = PlaceableInventorySlotView.Create(
                slotsRoot,
                entry,
                placementController,
                slotLayout.SlotHeight,
                slotLayout.Scale);
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

        panelRectTransform = rectTransform;
        rectTransform.anchorMin = new Vector2(1f, 0.5f);
        rectTransform.anchorMax = new Vector2(1f, 0.5f);
        rectTransform.pivot = new Vector2(1f, 0.5f);
        rectTransform.sizeDelta = ResolvePanelSize();
        rectTransform.anchoredPosition = panelOffset;
        RememberDefaultAnchoredPosition();

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
        panelLayout.childControlHeight = true;
        panelLayout.childControlWidth = true;
        panelLayout.childForceExpandHeight = false;
        panelLayout.childForceExpandWidth = true;

        if (slotsRoot == null)
        {
            CreateTitle(transform);
            slotsRoot = CreateSlotsRoot(transform);
            startExecutionButton = CreateStartExecutionButton(transform);
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
        layoutElement.preferredHeight = TitleHeight;
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
        layout.spacing = BaseSlotSpacing;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        LayoutElement layoutElement = slotsObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 0f;
        layoutElement.preferredHeight = 0f;
        layoutElement.flexibleHeight = 1f;

        return slotsObject.GetComponent<RectTransform>();
    }

    private StartExecutionButtonController CreateStartExecutionButton(Transform parent)
    {
        GameObject buttonObject = CreatePanelButton(parent, "StartExecutionButton", "PROBAR NIVEL");

        StartExecutionButtonController controller = buttonObject.AddComponent<StartExecutionButtonController>();
        controller.SetGameStateManager(gameStateManager);
        return controller;
    }

    private GameObject CreatePanelButton(Transform parent, string objectName, string labelText)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = Color.white;

        LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = StartButtonHeight;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelTransform = labelObject.GetComponent<RectTransform>();
        labelTransform.anchorMin = Vector2.zero;
        labelTransform.anchorMax = Vector2.one;
        labelTransform.offsetMin = Vector2.zero;
        labelTransform.offsetMax = Vector2.zero;

        Text label = labelObject.GetComponent<Text>();
        label.text = labelText;
        label.font = GetBuiltInFont();
        label.fontSize = 12;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.black;

        return buttonObject;
    }

    private void UseRuntimeInventory()
    {
        if (runtimeInventory != null)
        {
            runtimeInventory.Changed -= HandleInventoryChanged;
        }

        if (gameStateManager != null && gameStateManager.Inventory != null)
        {
            runtimeInventory = gameStateManager.Inventory;
            runtimeInventory.Changed += HandleInventoryChanged;
            return;
        }

        runtimeInventory = new PlaceableInventoryRuntime(inventorySet);
        runtimeInventory.Changed += HandleInventoryChanged;
    }

    private List<PlaceableInventoryRuntimeEntry> GetVisibleEntries()
    {
        IReadOnlyList<PlaceableInventoryRuntimeEntry> runtimeEntries = runtimeInventory.Entries;
        List<PlaceableInventoryRuntimeEntry> entries = new List<PlaceableInventoryRuntimeEntry>();

        for (int i = 0; i < runtimeEntries.Count; i++)
        {
            PlaceableInventoryRuntimeEntry entry = runtimeEntries[i];

            if (entry != null && entry.Definition != null)
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    private SlotLayout CalculateSlotLayout(int entryCount)
    {
        if (entryCount <= 0)
        {
            return new SlotLayout(BaseSlotHeight, BaseSlotSpacing, 1f);
        }

        float availableHeight = GetAvailableSlotsHeight();
        float desiredHeight = entryCount * BaseSlotHeight + Mathf.Max(0, entryCount - 1) * BaseSlotSpacing;
        float scale = desiredHeight > 0f ? Mathf.Min(1f, availableHeight / desiredHeight) : 1f;
        return new SlotLayout(BaseSlotHeight * scale, BaseSlotSpacing * scale, scale);
    }

    private float GetAvailableSlotsHeight()
    {
        RectTransform rectTransform = panelRectTransform != null ? panelRectTransform : GetComponent<RectTransform>();
        float height = rectTransform != null && rectTransform.rect.height > 0f
            ? rectTransform.rect.height
            : panelSize.y;
        VerticalLayoutGroup panelLayout = GetComponent<VerticalLayoutGroup>();
        float padding = panelLayout != null ? panelLayout.padding.top + panelLayout.padding.bottom : 0f;
        float panelSpacing = panelLayout != null ? panelLayout.spacing * 2f : 0f;
        return Mathf.Max(0f, height - padding - TitleHeight - StartButtonHeight - panelSpacing);
    }

    private Vector2 ResolvePanelSize()
    {
        Vector2 resolvedSize = new Vector2(
            Mathf.Max(panelSize.x, MinimumPanelSize.x),
            Mathf.Max(panelSize.y, MinimumPanelSize.y));
        RectTransform parentRect = transform.parent as RectTransform;

        if (parentRect != null && parentRect.rect.height > 0f)
        {
            resolvedSize.y = Mathf.Min(resolvedSize.y, Mathf.Max(0f, parentRect.rect.height - 36f));
        }

        return resolvedSize;
    }

    private void ApplySlotsRootLayout(SlotLayout slotLayout)
    {
        if (slotsRoot == null)
        {
            return;
        }

        VerticalLayoutGroup layout = slotsRoot.GetComponent<VerticalLayoutGroup>();

        if (layout != null)
        {
            layout.spacing = slotLayout.Spacing;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
        }
    }

    public bool ContainsScreenPoint(Vector2 screenPosition)
    {
        RectTransform rectTransform = panelRectTransform != null ? panelRectTransform : GetComponent<RectTransform>();
        return rectTransform != null && RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition);
    }

    public void SetDynamicPlanningCameraInset(bool active, float insetPixels)
    {
        RectTransform rectTransform = panelRectTransform != null ? panelRectTransform : GetComponent<RectTransform>();

        if (rectTransform == null)
        {
            return;
        }

        panelRectTransform = rectTransform;
        RememberDefaultAnchoredPosition();
        rectTransform.anchoredPosition = active
            ? defaultAnchoredPosition + Vector2.left * Mathf.Max(0f, insetPixels)
            : defaultAnchoredPosition;
    }

    public bool TryReturnOne(PlaceableDefinition definition)
    {
        return runtimeInventory != null && runtimeInventory.TryReturnOne(definition);
    }

    private void HandlePhaseChanged(LevelPhase phase)
    {
        bool planning = phase == LevelPhase.Planning;

        for (int i = 0; i < slotViews.Count; i++)
        {
            PlaceableDefinition definition = slotViews[i].Definition;
            bool allowInteraction = definition != null
                && definition.UseMode == PlaceableUseMode.ExecutionClickToDestroyTile
                ? phase == LevelPhase.Execution
                : planning;

            slotViews[i].SetInteractionAllowed(allowInteraction);
        }
    }

    private void RememberDefaultAnchoredPosition()
    {
        if (hasDefaultAnchoredPosition || panelRectTransform == null)
        {
            return;
        }

        defaultAnchoredPosition = panelRectTransform.anchoredPosition;
        hasDefaultAnchoredPosition = true;
    }

    private void HandleInventoryChanged()
    {
        for (int i = 0; i < slotViews.Count; i++)
        {
            slotViews[i].RefreshFromRuntimeEntry();
        }
    }

    private readonly struct SlotLayout
    {
        public SlotLayout(float slotHeight, float spacing, float scale)
        {
            SlotHeight = slotHeight;
            Spacing = spacing;
            Scale = scale;
        }

        public float SlotHeight { get; }
        public float Spacing { get; }
        public float Scale { get; }
    }
}
