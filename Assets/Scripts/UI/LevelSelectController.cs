using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelectController : MonoBehaviour
{
    public const int ItemsPerPage = 5;

    [SerializeField] private LevelCatalog catalog;
    [SerializeField] private LevelSelectSlotView[] slots;
    [SerializeField] private Button previousPageButton;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private TextMeshProUGUI pageLabel;
    [Header("World selector visuals")]
    [SerializeField] private Image selectorBackground;
    [SerializeField] private Image previousPageImage;
    [SerializeField] private Image nextPageImage;
    [SerializeField] private Image backButtonImage;
    [SerializeField] private TextMeshProUGUI titleLabel;

    private readonly List<Button> createdLevelButtons = new List<Button>();
    private readonly List<LevelCatalogPage> pages = new List<LevelCatalogPage>();
    private readonly List<LevelCatalogEntry> orderedEntries = new List<LevelCatalogEntry>();
    private int currentPageIndex;
    private int totalPages = 1;
    private SelectorVisualDefaults visualDefaults;

    public static Action<string> SceneLoadOverride { get; set; }
    public IReadOnlyList<Button> CreatedLevelButtons => createdLevelButtons;
    public int CurrentPageIndex => currentPageIndex;
    public int TotalPages => totalPages;

    private void Awake()
    {
        CaptureVisualDefaults();
        BindPaginationButtons();
    }

    private void Start()
    {
        Rebuild();
    }

    public void Configure(
        LevelCatalog levelCatalog,
        LevelSelectSlotView[] authoredSlots,
        Button previousButton,
        Button nextButton,
        TextMeshProUGUI paginationLabel)
    {
        catalog = levelCatalog;
        slots = authoredSlots;
        previousPageButton = previousButton;
        nextPageButton = nextButton;
        pageLabel = paginationLabel;
        BindPaginationButtons();
        Rebuild();
    }

    public void Rebuild()
    {
        createdLevelButtons.Clear();
        pages.Clear();
        orderedEntries.Clear();

        if (catalog != null)
        {
            orderedEntries.AddRange(catalog.GetOrderedEntries());
            BuildPages(orderedEntries);
        }

        totalPages = Mathf.Max(1, pages.Count);
        currentPageIndex = Mathf.Clamp(currentPageIndex, 0, totalPages - 1);
        IReadOnlyList<LevelCatalogEntry> entries = pages.Count > 0
            ? pages[currentPageIndex].Entries
            : Array.Empty<LevelCatalogEntry>();
        WorldLevelSelectorAssets selectorAssets = pages.Count > 0
            ? pages[currentPageIndex].WorldDefinition?.LevelSelectorAssets
            : null;

        ApplyWorldVisuals(selectorAssets);

        for (int i = 0; i < ItemsPerPage; i++)
        {
            LevelCatalogEntry entry = i < entries.Count ? entries[i] : null;
            BindSlot(i, entry, selectorAssets);
        }

        UpdatePaginationControls();
    }

    public void ShowPreviousPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            Rebuild();
        }
    }

    public void ShowNextPage()
    {
        if (currentPageIndex < totalPages - 1)
        {
            currentPageIndex++;
            Rebuild();
        }
    }

    public void LoadLevel(LevelCatalogEntry entry)
    {
        if (!LevelProgressService.IsUnlocked(entry, orderedEntries))
        {
            return;
        }

        if (SceneLoadOverride != null)
        {
            SceneLoadOverride.Invoke(entry.SceneName);
            return;
        }

        SceneManager.LoadScene(entry.SceneName);
    }

    private void BindSlot(int index, LevelCatalogEntry entry, WorldLevelSelectorAssets selectorAssets)
    {
        if (slots == null || index >= slots.Length || slots[index] == null)
        {
            return;
        }

        UnityAction action = entry != null ? () => LoadLevel(entry) : null;
        bool isUnlocked = LevelProgressService.IsUnlocked(entry, orderedEntries);
        slots[index].Bind(entry, index + 1, selectorAssets, isUnlocked, action);

        if (entry != null && slots[index].Button != null)
        {
            createdLevelButtons.Add(slots[index].Button);
        }
    }

    private void BuildPages(List<LevelCatalogEntry> entries)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            LevelCatalogEntry entry = entries[i];

            if (entry == null)
            {
                continue;
            }

            WorldDefinition worldDefinition = entry.WorldDefinition;

            if (pages.Count == 0 || !pages[pages.Count - 1].Matches(worldDefinition, entry.WorldLabel))
            {
                pages.Add(new LevelCatalogPage(worldDefinition, entry.WorldLabel));
            }

            pages[pages.Count - 1].Entries.Add(entry);
        }
    }

    private void BindPaginationButtons()
    {
        if (previousPageButton != null)
        {
            previousPageButton.onClick.RemoveListener(ShowPreviousPage);
            previousPageButton.onClick.AddListener(ShowPreviousPage);
        }

        if (nextPageButton != null)
        {
            nextPageButton.onClick.RemoveListener(ShowNextPage);
            nextPageButton.onClick.AddListener(ShowNextPage);
        }
    }

    private void UpdatePaginationControls()
    {
        if (previousPageButton != null)
        {
            previousPageButton.interactable = currentPageIndex > 0;
        }

        if (nextPageButton != null)
        {
            nextPageButton.interactable = currentPageIndex < totalPages - 1;
        }

        if (pageLabel != null)
        {
            string label = pages.Count > currentPageIndex
                ? pages[currentPageIndex].DisplayName
                : $"Mundo {currentPageIndex + 1}";
            pageLabel.text = $"{label} / {totalPages}";
        }
    }

    private void CaptureVisualDefaults()
    {
        if (visualDefaults != null)
        {
            return;
        }

        visualDefaults = new SelectorVisualDefaults(
            selectorBackground,
            previousPageImage,
            nextPageImage,
            backButtonImage,
            titleLabel,
            pageLabel);
    }

    private void ApplyWorldVisuals(WorldLevelSelectorAssets selectorAssets)
    {
        CaptureVisualDefaults();

        ApplySprite(selectorBackground, selectorAssets?.Background, visualDefaults.Background);
        ApplySprite(previousPageImage, selectorAssets?.PreviousPage, visualDefaults.PreviousPage);
        ApplySprite(nextPageImage, selectorAssets?.NextPage, visualDefaults.NextPage);
        ApplySprite(backButtonImage, selectorAssets?.BackButton, visualDefaults.BackButton);

        if (titleLabel != null)
        {
            titleLabel.color = selectorAssets != null ? selectorAssets.TextColor : visualDefaults.TitleColor;
        }

        if (pageLabel != null)
        {
            pageLabel.color = selectorAssets != null ? selectorAssets.TextColor : visualDefaults.PageColor;
        }
    }

    private static void ApplySprite(Image image, Sprite themedSprite, ImageDefaults defaults)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = themedSprite != null ? themedSprite : defaults.Sprite;
        image.color = themedSprite != null ? Color.white : defaults.Color;
    }

    private sealed class LevelCatalogPage
    {
        public LevelCatalogPage(WorldDefinition worldDefinition, string worldLabel)
        {
            WorldDefinition = worldDefinition;
            WorldLabel = worldLabel;
        }

        public WorldDefinition WorldDefinition { get; }
        public string WorldLabel { get; }
        public string DisplayName => WorldDefinition != null ? WorldDefinition.WorldName : WorldLabel;
        public List<LevelCatalogEntry> Entries { get; } = new List<LevelCatalogEntry>();

        public bool Matches(WorldDefinition worldDefinition, string worldLabel)
        {
            if (WorldDefinition != null || worldDefinition != null)
            {
                return WorldDefinition == worldDefinition;
            }

            return string.Equals(WorldLabel, worldLabel, StringComparison.Ordinal);
        }
    }

    private sealed class SelectorVisualDefaults
    {
        public SelectorVisualDefaults(
            Image background,
            Image previousPage,
            Image nextPage,
            Image backButton,
            TextMeshProUGUI title,
            TextMeshProUGUI page)
        {
            Background = new ImageDefaults(background);
            PreviousPage = new ImageDefaults(previousPage);
            NextPage = new ImageDefaults(nextPage);
            BackButton = new ImageDefaults(backButton);
            TitleColor = title != null ? title.color : Color.white;
            PageColor = page != null ? page.color : Color.white;
        }

        public ImageDefaults Background { get; }
        public ImageDefaults PreviousPage { get; }
        public ImageDefaults NextPage { get; }
        public ImageDefaults BackButton { get; }
        public Color TitleColor { get; }
        public Color PageColor { get; }
    }

    private readonly struct ImageDefaults
    {
        public ImageDefaults(Image image)
        {
            Sprite = image != null ? image.sprite : null;
            Color = image != null ? image.color : Color.white;
        }

        public Sprite Sprite { get; }
        public Color Color { get; }
    }
}
