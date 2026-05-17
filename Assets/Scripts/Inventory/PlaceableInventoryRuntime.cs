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

    public void NotifyChanged()
    {
        Changed?.Invoke();
    }
}
