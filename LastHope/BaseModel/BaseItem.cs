using Last_Hope.Engine;

namespace Last_Hope.BaseModel;

public abstract class BaseItem
{
    public string Name { get; protected set; }
    public string Description { get; protected set; }
    public int MaxCount { get; protected set; }
    public int CurrentCount { get; protected set; }

    protected BaseItem(string name, string description, int maxCount, int startingCount)
    {
        Name = name;
        Description = description;
        MaxCount = maxCount;
        CurrentCount = startingCount;
    }

    public virtual bool CanUse(BasePlayer player)
    {
        return CurrentCount > 0;
    }

    public virtual bool Use(BasePlayer player)
    {
        if (CanUse(player))
        {
            OnUse(player);
            CurrentCount--;
            return true;
        }
        return false;
    }

    protected abstract void OnUse(BasePlayer player);

    public virtual void AddItem(int amount)
    {
        CurrentCount += amount;
        if (CurrentCount > MaxCount)
        {
            CurrentCount = MaxCount;
        }
    }
}