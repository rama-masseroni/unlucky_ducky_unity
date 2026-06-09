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

    public Button Button => button;
    public LevelCatalogEntry Entry => entry;

    public void Bind(LevelCatalogEntry newEntry, int slotNumber, UnityAction onClick)
    {
        entry = newEntry;
        gameObject.name = entry != null ? entry.DisplayName : $"Nivel {slotNumber}";
        bool isPlayable = entry != null && entry.IsPlayable;

        if (background != null)
        {
            background.color = isPlayable ? PlayableColor : LockedColor;
        }

        if (label != null)
        {
            label.text = entry != null ? slotNumber.ToString() : "-";
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
        button.interactable = isPlayable;

        if (clickAction != null)
        {
            button.onClick.AddListener(clickAction);
        }
    }
}
