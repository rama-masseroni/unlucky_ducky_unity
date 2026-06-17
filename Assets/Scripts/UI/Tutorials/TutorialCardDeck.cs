using System;
using UnityEngine;

[Serializable]
public sealed class TutorialCard
{
    [SerializeField] private string title;
    [TextArea(2, 5)]
    [SerializeField] private string body;
    [SerializeField] private Sprite icon;

    public string Title => title;
    public string Body => body;
    public Sprite Icon => icon;
}

[CreateAssetMenu(fileName = "TutorialCardDeck", menuName = "Unlucky Ducky/Tutorials/Card Deck")]
public sealed class TutorialCardDeck : ScriptableObject
{
    [SerializeField] private TutorialCard[] cards;

    public int Count => cards != null ? cards.Length : 0;

    public TutorialCard GetCard(int index)
    {
        if (cards == null || index < 0 || index >= cards.Length)
        {
            return null;
        }

        return cards[index];
    }
}
