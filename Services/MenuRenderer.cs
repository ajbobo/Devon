using Devon.Models;

namespace Devon.Services;

/// <summary>
/// Renders the room description and menu, handles user input for action selection
/// </summary>
public class MenuRenderer
{
    private readonly IConditionEvaluator _conditionEvaluator;

    public MenuRenderer(IConditionEvaluator conditionEvaluator)
    {
        _conditionEvaluator = conditionEvaluator;
    }

    /// <summary>
    /// Displays the current room and returns the player's selected action key
    /// </summary>
    public string GetActionFromPlayer(Room currentRoom, Player player)
    {
        try
        {
            Console.Clear();
        }
        catch { /* Ignore if console not available */ }

        // Display room description
        Console.WriteLine($"== {currentRoom.Name} ==");
        Console.WriteLine();

        foreach (var desc in currentRoom.Description)
        {
            if (_conditionEvaluator.Evaluate(desc.Condition ?? "true", new GameState { CurrentRoom = currentRoom, Player = player }))
            {
                Console.WriteLine(desc.Text);
            }
        }
        Console.WriteLine();

        // Display items in room (only those still available)
        var availableItems = currentRoom.Items;
        if (availableItems.Count > 0)
        {
            Console.WriteLine("You see here:");
            foreach (var item in availableItems)
            {
                Console.WriteLine($"  - {item}");
            }
            Console.WriteLine();
        }

        // Build menu options
        var options = BuildMenuOptions(currentRoom, player);

        if (options.Count == 0)
        {
            Console.WriteLine("There are no available actions. (Press any key to continue, or Esc to quit)");
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Escape) return "quit";
            return "quit"; // No actions means no progress, exit
        }

        // Get selection via arrow keys
        int selectedIndex = 0;
        int menuStartRow = Console.CursorTop;

        while (true)
        {
            // Render the menu with current selection
            RenderMenu(options, selectedIndex, menuStartRow);

            var keyInfo = Console.ReadKey(true);
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
        var directionKeys = new[] { "north", "south", "east", "west" };
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

        // Use action - only show if use action exists, is accessible, and player has the required item
        if (currentRoom.TryGetAction("use", out var useAction) && useAction is UseAction use && IsActionAvailable(useAction, currentRoom, player) && player.Inventory.Contains(use.Item))
        {
            options.Add(new MenuOption("Use", "use"));
        }

        // Talk action - always available if talk action exists and condition met
        if (currentRoom.TryGetAction("talk", out var talkAction) && talkAction != null && IsActionAvailable(talkAction, currentRoom, player))
        {
            options.Add(new MenuOption("Talk", "talk"));
        }

        // Inventory always available
        options.Add(new MenuOption("Inventory", "inventory"));

        return options;
    }

    private void RenderMenu(List<MenuOption> options, int selectedIndex, int startRow)
    {
        Console.SetCursorPosition(0, startRow);
        Console.WriteLine("What would you like to do?");
        for (int i = 0; i < options.Count; i++)
        {
            bool isSelected = (i == selectedIndex);
            var prefix = isSelected ? "> " : "  ";
            var suffix = isSelected ? " <" : "  ";
            var line = $"  {prefix}{options[i].Label}{suffix}";
            Console.WriteLine(line);
        }
        // Clear any leftover lines if options decreased
        int currentRow = Console.CursorTop;
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
