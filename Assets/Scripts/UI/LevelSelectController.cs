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
    [SerializeField] private Transform contentRoot;
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

    private void Start()
    {
        Rebuild();
    }

    public void Configure(LevelCatalog levelCatalog, Transform levelContentRoot)
    {
        Configure(levelCatalog, levelContentRoot, null, null, null);
    }

    public void Configure(
        LevelCatalog levelCatalog,
        Transform levelContentRoot,
        Button previousButton,
        Button nextButton,
        TextMeshProUGUI paginationLabel)
    {
        catalog = levelCatalog;
        contentRoot = levelContentRoot;
        previousPageButton = previousButton;
        nextPageButton = nextButton;
        pageLabel = paginationLabel;
        BindPaginationButtons();
        Rebuild();
    }

    public void Rebuild()
    {
        Clear();

        if (catalog == null || contentRoot == null)
        {
            UpdatePaginationControls();
            return;
        }

        BuildPages(catalog.GetOrderedEntries());
        totalPages = Mathf.Max(1, pages.Count);
        currentPageIndex = Mathf.Clamp(currentPageIndex, 0, totalPages - 1);
        IReadOnlyList<LevelCatalogEntry> entries = pages.Count > 0
            ? pages[currentPageIndex].Entries
            : Array.Empty<LevelCatalogEntry>();

        for (int i = 0; i < ItemsPerPage; i++)
        {
            LevelCatalogEntry entry = i < entries.Count ? entries[i] : null;
            CreateLevelButton(entry, i + 1);
        }

        UpdatePaginationControls();
    }

    public void ShowPreviousPage()
    {
        if (currentPageIndex <= 0)
        {
            return;
        }

        currentPageIndex--;
        Rebuild();
    }

    public void ShowNextPage()
    {
        if (currentPageIndex >= totalPages - 1)
        {
            return;
        }

        currentPageIndex++;
        Rebuild();
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

    private void Clear()
    {
        createdLevelButtons.Clear();

        if (contentRoot == null)
        {
            return;
        }

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            GameObject child = contentRoot.GetChild(i).gameObject;

            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }

    private void BuildPages(List<LevelCatalogEntry> entries)
    {
        pages.Clear();

        for (int i = 0; i < entries.Count; i++)
        {
            LevelCatalogEntry entry = entries[i];

            if (entry == null)
            {
                continue;
            }

            string worldLabel = entry.WorldLabel;

            if (pages.Count == 0 || !string.Equals(pages[pages.Count - 1].WorldLabel, worldLabel, StringComparison.Ordinal))
            {
                pages.Add(new LevelCatalogPage(worldLabel));
            }

            pages[pages.Count - 1].Entries.Add(entry);
        }
    }

    private void CreateLevelButton(LevelCatalogEntry entry, int slotNumber)
    {
        string slotName = entry != null ? entry.DisplayName : $"Nivel {slotNumber}";
        GameObject buttonObject = new GameObject(slotName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(contentRoot, false);

        bool isPlayable = entry != null && entry.IsPlayable;
        Image image = buttonObject.GetComponent<Image>();
        image.color = isPlayable
            ? new Color(1f, 0.96f, 0.84f, 1f)
            : new Color(0.62f, 0.65f, 0.62f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.interactable = isPlayable;

        if (entry != null)
        {
            button.onClick.AddListener(CreateLoadAction(entry));
        }

        TextMeshProUGUI label = CreateText(buttonObject.transform, GetSlotLabel(entry, slotNumber), 30f, FontStyles.Bold);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(10f, 8f);
        label.rectTransform.offsetMax = new Vector2(-10f, -8f);
        label.alignment = TextAlignmentOptions.Center;

        if (entry != null)
        {
            createdLevelButtons.Add(button);
        }
    }

    private UnityAction CreateLoadAction(LevelCatalogEntry entry)
    {
        return () => LoadLevel(entry);
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
            string label = pages.Count > currentPageIndex ? pages[currentPageIndex].WorldLabel : $"Mundo {currentPageIndex + 1}";
            pageLabel.text = $"{label} / {totalPages}";
        }
    }

    private static TextMeshProUGUI CreateText(Transform parent, string textValue, float fontSize, FontStyles fontStyle)
    {
        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = textValue;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = new Color(0.13f, 0.16f, 0.14f, 1f);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static string GetSlotLabel(LevelCatalogEntry entry, int slotNumber)
    {
        if (entry == null)
        {
            return "-";
        }

        return slotNumber.ToString();
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
