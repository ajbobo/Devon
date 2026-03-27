namespace Devon.Models;

public class Player
{
    public List<string> Inventory { get; private set; } = new();
    public HashSet<string> Conditions { get; private set; } = new();

    public void AddItem(string item)
    {
        if (!Inventory.Contains(item))
            Inventory.Add(item);
    }

    public bool RemoveItem(string item)
    {
        return Inventory.Remove(item);
    }

    public bool HasItem(string item)
    {
        return Inventory.Contains(item);
    }

    public void AddCondition(string condition)
    {
        Conditions.Add(condition);
    }

    public void RemoveCondition(string condition)
    {
        Conditions.Remove(condition);
    }

    public bool HasCondition(string condition)
    {
        return Conditions.Contains(condition);
    }
}
