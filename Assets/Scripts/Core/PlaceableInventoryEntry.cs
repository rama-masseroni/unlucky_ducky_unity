using System;
using UnityEngine;

[Serializable]
public class PlaceableInventoryEntry
{
    [SerializeField] private PlaceableDefinition definition;
    [SerializeField] private int amount = 1;

    public PlaceableDefinition Definition => definition;
    public int Amount => Mathf.Max(0, amount);

    public bool TryConsumeOne()
    {
        if (amount <= 0)
        {
            return false;
        }

        amount--;
        return true;
    }
}
