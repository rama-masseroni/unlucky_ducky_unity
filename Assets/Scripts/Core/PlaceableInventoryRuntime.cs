using System;
using System.Collections.Generic;

public class PlaceableInventoryRuntime
{
    private readonly List<PlaceableInventoryRuntimeEntry> entries = new List<PlaceableInventoryRuntimeEntry>();

    public PlaceableInventoryRuntime(PlaceableInventorySet inventorySet)
    {
        if (inventorySet == null)
        {
            return;
        }

        IReadOnlyList<PlaceableInventoryEntry> authoredEntries = inventorySet.Entries;

        for (int i = 0; i < authoredEntries.Count; i++)
        {
            PlaceableInventoryEntry entry = authoredEntries[i];

            if (entry == null || entry.Definition == null)
            {
                continue;
            }

            entries.Add(new PlaceableInventoryRuntimeEntry(this, entry.Definition, entry.Amount));
        }
    }

    public event Action Changed;

    public IReadOnlyList<PlaceableInventoryRuntimeEntry> Entries => entries;

    public bool AllItemsUsed
    {
        get
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Amount > 0)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public bool AllRequiredPlanningItemsUsed
    {
        get
        {
            for (int i = 0; i < entries.Count; i++)
            {
                PlaceableInventoryRuntimeEntry entry = entries[i];

                if (entry.Definition != null
                    && entry.Definition.RequiresPlacementBeforeExecution
                    && entry.Amount > 0)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public void NotifyChanged()
    {
        Changed?.Invoke();
    }

    public PlaceableInventoryRuntimeEntry FindEntry(PlaceableDefinition definition)
    {
        if (definition == null)
        {
            return null;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Definition == definition)
            {
                return entries[i];
            }
        }

        return null;
    }

    public PlaceableInventoryRuntimeEntry FindFirstEntryByUseMode(PlaceableUseMode useMode)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Definition != null && entries[i].Definition.UseMode == useMode)
            {
                return entries[i];
            }
        }

        return null;
    }

    public bool TryReturnOne(PlaceableDefinition definition)
    {
        PlaceableInventoryRuntimeEntry entry = FindEntry(definition);
        return entry != null && entry.TryReturnOne();
    }
}
