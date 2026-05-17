public class PlaceableInventoryRuntimeEntry
{
    private readonly PlaceableInventoryRuntime owner;
    private int amount;

    public PlaceableInventoryRuntimeEntry(
        PlaceableInventoryRuntime owner,
        PlaceableDefinition definition,
        int amount)
    {
        this.owner = owner;
        Definition = definition;
        InitialAmount = UnityEngine.Mathf.Max(0, amount);
        this.amount = InitialAmount;
    }

    public PlaceableDefinition Definition { get; }
    public int InitialAmount { get; }
    public int Amount => amount;

    public bool TryConsumeOne()
    {
        if (amount <= 0)
        {
            return false;
        }

        amount--;
        owner.NotifyChanged();
        return true;
    }
}
