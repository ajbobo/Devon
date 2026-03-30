namespace Devon.Services;

using Devon.Models;

/// <summary>
/// Parses and executes action command strings
/// </summary>
public class ActionExecutor : IActionExecutor
{
    private readonly ICutsceneRenderer _cutsceneRenderer;
    private readonly Dictionary<string, Cutscene> _cutscenes;

    public ActionExecutor(ICutsceneRenderer cutsceneRenderer, Dictionary<string, Cutscene> cutscenes)
    {
        _cutsceneRenderer = cutsceneRenderer ?? throw new ArgumentNullException(nameof(cutsceneRenderer));
        _cutscenes = cutscenes ?? throw new ArgumentNullException(nameof(cutscenes));
    }

    public void Execute(string? commands, GameState state)
    {
        if (string.IsNullOrWhiteSpace(commands))
            return;

        var commandList = commands.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var cmd in commandList)
        {
            ExecuteSingle(cmd.Trim(), state);
        }
    }

    private void ExecuteSingle(string cmd, GameState state)
    {
        // Expected format: Class.Operation(arg) e.g., Inventory.add(item)
        // We'll parse by finding first '(' and last ')', split by '.' before '('.
        var parenOpen = cmd.IndexOf('(');
        var parenClose = cmd.LastIndexOf(')');

        if (parenOpen < 0 || parenClose < 0 || parenClose <= parenOpen)
            throw new InvalidOperationException($"Invalid command format: {cmd}");

        var className = cmd[..parenOpen].Trim();
        var argString = cmd[(parenOpen + 1)..parenClose].Trim();
        // Remove quotes if present
        var arg = argString.Trim('"', '\'');

        // Determine method name and target
        var dotIndex = className.LastIndexOf('.');
        if (dotIndex < 0)
            throw new InvalidOperationException($"Invalid command format (missing dot): {cmd}");

        var classPart = className[..dotIndex];
        var methodPart = className[(dotIndex + 1)..];

        switch (classPart)
        {
            case "Inventory":
                ExecuteInventory(state.Player, methodPart, arg);
                break;
            case "Player":
                ExecutePlayer(state.Player, methodPart, arg);
                break;
            case "Room":
                ExecuteRoom(state.CurrentRoom, methodPart, arg);
                break;
            default:
                throw new InvalidOperationException($"Unknown target class: {classPart}");
        }
    }

    private void ExecuteInventory(Player player, string method, string arg)
    {
        if (method.Equals("add", StringComparison.OrdinalIgnoreCase))
        {
            player.AddItem(arg);
        }
        else if (method.Equals("remove", StringComparison.OrdinalIgnoreCase))
        {
            player.RemoveItem(arg);
        }
        else
        {
            throw new InvalidOperationException($"Unknown Inventory method: {method}");
        }
    }

    private void ExecutePlayer(Player player, string method, string arg)
    {
        if (method.Equals("addCondition", StringComparison.OrdinalIgnoreCase))
        {
            player.AddCondition(arg);
        }
        else if (method.Equals("removeCondition", StringComparison.OrdinalIgnoreCase))
        {
            player.RemoveCondition(arg);
        }
        else
        {
            throw new InvalidOperationException($"Unknown Player method: {method}");
        }
    }

    private void ExecuteRoom(Room? room, string method, string arg)
    {
        if (room == null)
            throw new InvalidOperationException("Cannot execute room command: no current room");

        if (method.Equals("addCondition", StringComparison.OrdinalIgnoreCase))
        {
            room.Conditions.Add(arg);
        }
        else if (method.Equals("removeCondition", StringComparison.OrdinalIgnoreCase))
        {
            room.Conditions.Remove(arg);
        }
        else if (method.Equals("startCutscene", StringComparison.OrdinalIgnoreCase))
        {
            if (_cutscenes.TryGetValue(arg, out var cutscene))
            {
                _cutsceneRenderer.PlayCutscene(cutscene);
            }
            else
            {
                throw new InvalidOperationException($"Cutscene '{arg}' not found");
            }
        }
        else
        {
            throw new InvalidOperationException($"Unknown Room method: {method}");
        }
    }
}
