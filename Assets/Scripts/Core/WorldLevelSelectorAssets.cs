using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WorldLevelSelectorAssets",
    menuName = "Unlucky Ducky/UI/World Level Selector Assets")]
public class WorldLevelSelectorAssets : ScriptableObject
{
    [SerializeField] private Sprite background;
    [SerializeField] private Sprite previousPage;
    [SerializeField] private Sprite nextPage;
    [SerializeField] private Sprite backButton;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color lockedFallbackTint = new Color(0.42f, 0.42f, 0.42f, 1f);
    [SerializeField] private List<LevelSelectorSpriteSet> levelSprites = new List<LevelSelectorSpriteSet>();

    public Sprite Background => background;
    public Sprite PreviousPage => previousPage;
    public Sprite NextPage => nextPage;
    public Sprite BackButton => backButton;
    public Color TextColor => textColor;
    public Color LockedFallbackTint => lockedFallbackTint;

    public Sprite GetLevelSprite(int displayOrder, bool locked)
    {
        for (int i = 0; i < levelSprites.Count; i++)
        {
            LevelSelectorSpriteSet spriteSet = levelSprites[i];

            if (spriteSet != null && spriteSet.DisplayOrder == displayOrder)
            {
                return locked && spriteSet.Locked != null ? spriteSet.Locked : spriteSet.Normal;
            }
        }

        return null;
    }

    public bool HasLockedLevelSprite(int displayOrder)
    {
        for (int i = 0; i < levelSprites.Count; i++)
        {
            LevelSelectorSpriteSet spriteSet = levelSprites[i];

            if (spriteSet != null && spriteSet.DisplayOrder == displayOrder)
            {
                return spriteSet.Locked != null;
            }
        }

        return false;
    }
}

[Serializable]
public class LevelSelectorSpriteSet
{
    [Min(1)]
    [SerializeField] private int displayOrder = 1;
    [SerializeField] private Sprite normal;
    [SerializeField] private Sprite locked;

    public int DisplayOrder => displayOrder;
    public Sprite Normal => normal;
    public Sprite Locked => locked;
}
