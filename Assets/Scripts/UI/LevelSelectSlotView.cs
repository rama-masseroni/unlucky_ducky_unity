using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LevelSelectSlotView : MonoBehaviour
{
    private static readonly Color PlayableColor = new Color(1f, 0.96f, 0.84f, 1f);
    private static readonly Color LockedColor = new Color(0.62f, 0.65f, 0.62f, 1f);

    [SerializeField] private Image background;
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI label;

    private LevelCatalogEntry entry;
    private UnityAction clickAction;
    private Sprite defaultSprite;
    private Color defaultColor;
    private bool defaultsCaptured;

    public Button Button => button;
    public LevelCatalogEntry Entry => entry;

    public void Bind(LevelCatalogEntry newEntry, int slotNumber, UnityAction onClick)
    {
        Bind(newEntry, slotNumber, null, newEntry != null && newEntry.IsPlayable, onClick);
    }

    public void Bind(
        LevelCatalogEntry newEntry,
        int slotNumber,
        WorldLevelSelectorAssets selectorAssets,
        UnityAction onClick)
    {
        Bind(
            newEntry,
            slotNumber,
            selectorAssets,
            newEntry != null && newEntry.IsPlayable,
            onClick);
    }

    public void Bind(
        LevelCatalogEntry newEntry,
        int slotNumber,
        WorldLevelSelectorAssets selectorAssets,
        bool isUnlocked,
        UnityAction onClick)
    {
        CaptureDefaults();
        entry = newEntry;
        gameObject.name = entry != null ? entry.DisplayName : $"Nivel {slotNumber}";
        gameObject.SetActive(entry != null);
        bool isPlayable = entry != null && entry.HasSceneName && isUnlocked;
        Sprite levelSprite = entry != null
            ? selectorAssets?.GetLevelSprite(entry.DisplayOrder, !isPlayable)
            : null;

        if (background != null)
        {
            background.sprite = levelSprite != null ? levelSprite : defaultSprite;
            background.preserveAspect = levelSprite != null;
            background.color = GetBackgroundColor(entry, isPlayable, selectorAssets, levelSprite);
        }

        if (label != null)
        {
            label.gameObject.SetActive(entry != null && levelSprite == null);
            label.text = entry != null ? entry.DisplayOrder.ToString() : "-";
        }

        if (button == null)
        {
            return;
        }

        if (clickAction != null)
        {
            button.onClick.RemoveListener(clickAction);
        }

        clickAction = onClick;
        button.transition = levelSprite != null
            ? Selectable.Transition.None
            : Selectable.Transition.ColorTint;
        button.interactable = isPlayable;

        if (clickAction != null)
        {
            button.onClick.AddListener(clickAction);
        }
    }

    private void CaptureDefaults()
    {
        if (defaultsCaptured || background == null)
        {
            return;
        }

        defaultSprite = background.sprite;
        defaultColor = background.color;
        defaultsCaptured = true;
    }

    private Color GetBackgroundColor(
        LevelCatalogEntry currentEntry,
        bool isPlayable,
        WorldLevelSelectorAssets selectorAssets,
        Sprite levelSprite)
    {
        if (currentEntry == null)
        {
            return defaultColor;
        }

        if (levelSprite == null)
        {
            return isPlayable ? PlayableColor : LockedColor;
        }

        bool usesTintedFallback = !isPlayable
            && selectorAssets != null
            && !selectorAssets.HasLockedLevelSprite(currentEntry.DisplayOrder);
        return usesTintedFallback ? selectorAssets.LockedFallbackTint : Color.white;
    }
}
