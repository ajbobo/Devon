using Devon.Models;

namespace Devon.Services;

/// <summary>
/// Renders the room description and menu, handles user input for action selection
/// </summary>
public class MenuRenderer
{
    private readonly IConditionEvaluator _conditionEvaluator;
    private readonly IConsole _console;

    public MenuRenderer(IConditionEvaluator conditionEvaluator, IConsole console)
    {
        _conditionEvaluator = conditionEvaluator;
        _console = console;
    }

    /// <summary>
    /// Displays the current room and returns the player's selected action key
    /// </summary>
    public string GetActionFromPlayer(Room currentRoom, Player player)
    {
        try
        {
            _console.Clear();
        }
        catch { /* Ignore if console not available */ }

        // Display room description
        _console.WriteLine($"== {currentRoom.Name} ==");
        _console.WriteLine();

        foreach (var desc in currentRoom.Description)
        {
            if (_conditionEvaluator.Evaluate(desc.Condition ?? "true", new GameState { CurrentRoom = currentRoom, Player = player }))
            {
                _console.WriteLine(desc.Text);
            }
        }
        _console.WriteLine();

        // Apply initial conditions after first description is shown
        if (currentRoom.InitialConditions.Count > 0)
        {
            foreach (var cond in currentRoom.InitialConditions)
            {
                currentRoom.Conditions.Add(cond);
            }
            currentRoom.InitialConditions.Clear();
        }

        // Build menu options
        var options = BuildMenuOptions(currentRoom, player);

        if (options.Count == 0)
        {
            _console.WriteLine("There are no available actions. (Press any key to continue, or Esc to quit)");
            var key = _console.ReadKey(true);
            if (key.Key == ConsoleKey.Escape) return "quit";
            return "quit"; // No actions means no progress, exit
        }

        // Get selection via arrow keys
        int selectedIndex = 0;
        int menuStartRow = _console.CursorTop;

        while (true)
        {
            // Render the menu with current selection
            RenderMenu(options, selectedIndex, menuStartRow);

            var keyInfo = _console.ReadKey(true);
            var key = keyInfo.Key;

            switch (key)
            {
                case ConsoleKey.UpArrow:
                    selectedIndex = (selectedIndex - 1 + options.Count) % options.Count;
                    break;
                case ConsoleKey.DownArrow:
                    selectedIndex = (selectedIndex + 1) % options.Count;
                    break;
                case ConsoleKey.Enter:
                    return options[selectedIndex].Key;
                case ConsoleKey.Escape:
                    return "quit";
            }
        }
    }

    private List<MenuOption> BuildMenuOptions(Room currentRoom, Player player)
    {
        var options = new List<MenuOption>();

        // Direction exits
        var directionKeys = new[] { "north", "south", "east", "west", "up", "down", "left", "center", "right" };
        foreach (var dir in directionKeys)
        {
            if (currentRoom.TryGetAction(dir, out var action) && IsActionAvailable(action, currentRoom, player))
            {
                options.Add(new MenuOption($"{char.ToUpper(dir[0])}{dir[1..]}", dir));
            }
        }

        // Take action - only show if take action exists, is available (condition), and the specific item is present in the room
        if (currentRoom.TryGetAction("take", out var takeAction) && takeAction is TakeAction take && IsActionAvailable(takeAction, currentRoom, player) && currentRoom.Items.Contains(take.Item))
        {
            options.Add(new MenuOption("Take", "take"));
        }

        // Use action - always available if player has items
        if (player.Inventory.Count > 0)
        {
            options.Add(new MenuOption("Use", "use"));
        }

        // Talk action - always available (like Inventory)
        options.Add(new MenuOption("Talk", "talk"));

        // Inventory always available
        options.Add(new MenuOption("Inventory", "inventory"));

        return options;
    }

    private void RenderMenu(List<MenuOption> options, int selectedIndex, int startRow)
    {
        _console.SetCursorPosition(0, startRow);
        _console.WriteLine("What would you like to do?");
        for (int i = 0; i < options.Count; i++)
        {
            bool isSelected = (i == selectedIndex);
            var prefix = isSelected ? "> " : "  ";
            var suffix = isSelected ? " <" : "  ";
            var line = $"  {prefix}{options[i].Label}{suffix}";
            _console.WriteLine(line);
        }
        // Clear any leftover lines if options decreased
        int currentRow = _console.CursorTop;
        // (Simplified: no explicit clear of old lines, okay for now)
    }

    private bool IsActionAvailable(RoomAction action, Room room, Player player)
    {
        if (string.IsNullOrEmpty(action.Condition))
            return true;

        var state = new GameState { CurrentRoom = room, Player = player };
        return _conditionEvaluator.Evaluate(action.Condition, state);
    }

    private record MenuOption(string Label, string Key);
}
