namespace Devon.Models;

/// <summary>
/// Represents a room description entry that may have a conditional display
/// </summary>
public class RoomDescription
{
    public string Text { get; set; } = string.Empty;
    public string? Condition { get; set; } // Condition expression string
}

/// <summary>
/// Abstract base for different action types that can occur in a room
/// </summary>
public abstract class RoomAction
{
    public string? Condition { get; set; } // Optional condition expression that must be true for this action to be available
    public string? ResultText { get; set; }
    public string? ActionCommands { get; set; } // Semicolon-separated commands like "Inventory.add(key);Room.addCondition(door_open)"
}

/// <summary>
/// Exit action: leads to another room in a direction
/// </summary>
public class ExitAction : RoomAction
{
    public string TargetRoom { get; set; } = string.Empty;
}

/// <summary>
/// Take action: player can pick up an item from the room
/// </summary>
public class TakeAction : RoomAction
{
    public string Item { get; set; } = string.Empty;
}

/// <summary>
/// Use action: player uses an item on a target
/// </summary>
public class UseAction : RoomAction
{
    public string Item { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
}

/// <summary>
/// Talk action: player talks to an NPC with specific dialogue
/// </summary>
public class TalkAction : RoomAction
{
    public string Target { get; set; } = string.Empty;
    public string Says { get; set; } = string.Empty; // Required phrase
}

public class Room
{
    public string Name { get; set; } = string.Empty;
    public List<RoomDescription> Description { get; set; } = new();
    public List<string> Items { get; set; } = new(); // Items available in this room
    public Dictionary<string, RoomAction> Actions { get; set; } = new(); // "north", "take", "use", "talk", etc.
    public HashSet<string> Conditions { get; set; } = new(); // Room's persistent conditions

    /// <summary>
    /// Gets an action by its type key (e.g., "north", "take", "use", "talk")
    /// </summary>
    public bool TryGetAction(string key, out RoomAction? action)
    {
        return Actions.TryGetValue(key, out action);
    }
}
