namespace Devon.Models;

public class Player
{
    public HashSet<string> Inventory { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Conditions { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

    public void AddItem(string item)
    {
        Inventory.Add(item); // HashSet.Add automatically ignores duplicates
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
