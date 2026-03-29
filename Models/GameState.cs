namespace Devon.Models;

/// <summary>
/// Holds the complete state of the game
/// </summary>
public class GameState
{
    public Player Player { get; set; } = new();
    public Dictionary<string, Room> Rooms { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Room? CurrentRoom { get; set; }

    /// <summary>
    /// Finds a room by name, returns null if not found
    /// </summary>
    public Room? GetRoom(string name)
    {
        return Rooms.TryGetValue(name, out var room) ? room : null;
    }
}
