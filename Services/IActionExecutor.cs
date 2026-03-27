namespace Devon.Services;

using Devon.Models;

/// <summary>
/// Executes action command strings (e.g., "Inventory.add(key);Room.addCondition(door_open)")
/// </summary>
public interface IActionExecutor
{
    /// <summary>
    /// Executes the given command string in the context of the game state
    /// Commands are semicolon-separated: Class.Operation(arg)
    /// </summary>
    void Execute(string? commands, GameState state);
}
