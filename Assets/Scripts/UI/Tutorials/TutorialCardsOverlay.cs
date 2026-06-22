using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public sealed class TutorialCardsOverlay : MonoBehaviour
{
    [SerializeField] private TutorialCardDeck deck;
    [SerializeField] private Text titleText;
    [SerializeField] private Text bodyText;
    [SerializeField] private Text progressText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button closeButton;

    private int currentIndex;

    public void ShowFromStart()
    {
        currentIndex = 0;
        gameObject.SetActive(true);
        Refresh();
    }

    private void Awake()
    {
        if (!HasRequiredReferences())
        {
            Debug.LogError("TutorialCardsOverlay requires an inspector-authored deck and UI references.", this);
            gameObject.SetActive(false);
            return;
        }

        previousButton.onClick.AddListener(ShowPrevious);
        nextButton.onClick.AddListener(ShowNext);
        closeButton.onClick.AddListener(Close);
        Refresh();
    }

    private void OnDestroy()
    {
        if (previousButton != null)
        {
            previousButton.onClick.RemoveListener(ShowPrevious);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(ShowNext);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
        }
    }

    private void ShowPrevious()
    {
        if (deck == null)
        {
            return;
        }

        currentIndex = Mathf.Max(0, currentIndex - 1);
        Refresh();
    }

    private void ShowNext()
    {
        if (deck == null)
        {
            return;
        }

        currentIndex = Mathf.Min(deck.Count - 1, currentIndex + 1);
        Refresh();
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }

    private void Refresh()
    {
        if (deck == null || deck.Count == 0)
        {
            gameObject.SetActive(false);
            return;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, deck.Count - 1);
        TutorialCard card = deck.GetCard(currentIndex);

        titleText.text = card != null ? card.Title : string.Empty;
        bodyText.text = card != null ? card.Body : string.Empty;
        progressText.text = $"{currentIndex + 1}/{deck.Count}";

        if (iconImage != null)
        {
            iconImage.sprite = card != null ? card.Icon : null;
            iconImage.gameObject.SetActive(iconImage.sprite != null);
        }

        previousButton.interactable = currentIndex > 0;
        nextButton.interactable = currentIndex < deck.Count - 1;
    }

    private bool HasRequiredReferences()
    {
        return deck != null
            && titleText != null
            && bodyText != null
            && progressText != null
            && previousButton != null
            && nextButton != null
            && closeButton != null;
    }

}
