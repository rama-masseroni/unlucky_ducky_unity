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

    private readonly List<Button> createdLevelButtons = new List<Button>();
    private readonly List<LevelCatalogPage> pages = new List<LevelCatalogPage>();
    private int currentPageIndex;
    private int totalPages = 1;

    public static Action<string> SceneLoadOverride { get; set; }
    public IReadOnlyList<Button> CreatedLevelButtons => createdLevelButtons;
    public int CurrentPageIndex => currentPageIndex;
    public int TotalPages => totalPages;

    private void Awake()
    {
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

        if (catalog != null)
        {
            BuildPages(catalog.GetOrderedEntries());
        }

        totalPages = Mathf.Max(1, pages.Count);
        currentPageIndex = Mathf.Clamp(currentPageIndex, 0, totalPages - 1);
        IReadOnlyList<LevelCatalogEntry> entries = pages.Count > 0
            ? pages[currentPageIndex].Entries
            : Array.Empty<LevelCatalogEntry>();

        for (int i = 0; i < ItemsPerPage; i++)
        {
            LevelCatalogEntry entry = i < entries.Count ? entries[i] : null;
            BindSlot(i, entry);
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
        if (entry == null || !entry.HasSceneName)
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

    private void BindSlot(int index, LevelCatalogEntry entry)
    {
        if (slots == null || index >= slots.Length || slots[index] == null)
        {
            return;
        }

        UnityAction action = entry != null ? () => LoadLevel(entry) : null;
        slots[index].Bind(entry, index + 1, action);

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

            if (pages.Count == 0
                || !string.Equals(pages[pages.Count - 1].WorldLabel, entry.WorldLabel, StringComparison.Ordinal))
            {
                pages.Add(new LevelCatalogPage(entry.WorldLabel));
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
                ? pages[currentPageIndex].WorldLabel
                : $"Mundo {currentPageIndex + 1}";
            pageLabel.text = $"{label} / {totalPages}";
        }
    }

    private sealed class LevelCatalogPage
    {
        public LevelCatalogPage(string worldLabel)
        {
            WorldLabel = worldLabel;
        }

        public string WorldLabel { get; }
        public List<LevelCatalogEntry> Entries { get; } = new List<LevelCatalogEntry>();
    }
}
