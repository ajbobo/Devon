using Devon.Models;

namespace Devon.Services;

/// <summary>
/// Main game controller
/// </summary>
public class Game
{
    private readonly IConditionEvaluator _conditionEvaluator;
    private readonly IJsonRoomLoader _roomLoader;
    private readonly MenuRenderer _menuRenderer;
    private readonly ActionInvoker _actionInvoker;
    private readonly IActionExecutor _actionExecutor;

    private GameState _state = new();

    public Game()
    {
        _conditionEvaluator = new ConditionEvaluator();
        _roomLoader = new JsonRoomLoader("Devon.rooms.json", _conditionEvaluator);
        _menuRenderer = new MenuRenderer(_conditionEvaluator);
        _actionExecutor = new ActionExecutor();
        _actionInvoker = new ActionInvoker(_actionExecutor, _conditionEvaluator);
    }

    public void Run()
    {
        InitializeGame();

        bool quit = false;
        while (!quit)
        {
            var actionKey = _menuRenderer.GetActionFromPlayer(_state.CurrentRoom!, _state.Player);

            if (actionKey == "quit")
            {
                quit = true;
                try { Console.Clear(); } catch { }
                Console.WriteLine("Thanks for playing!");
                break;
            }

            ProcessAction(actionKey);
        }
    }

    private void InitializeGame()
    {
        try
        {
            // Synchronous load (blocking)
            var rooms = _roomLoader.LoadRoomsAsync().GetAwaiter().GetResult();
            foreach (var kvp in rooms)
            {
                _state.Rooms[kvp.Key] = kvp.Value;
            }

            // Set starting room - first room in the dictionary (or could have "start" field in JSON)
            // For now, pick the one named "Entrance" or first
            _state.CurrentRoom = _state.Rooms.Values.FirstOrDefault(r => r.Name.Equals("Entrance", StringComparison.OrdinalIgnoreCase))
                              ?? _state.Rooms.Values.First();

            Console.WriteLine("Devon - Text Adventure");
            Console.WriteLine("======================");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading game: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }

    private void ProcessAction(string actionKey)
    {
        var room = _state.CurrentRoom;
        if (room == null) return;

        switch (actionKey.ToLowerInvariant())
        {
            case "north" or "south" or "east" or "west":
                HandleExit(actionKey, room);
                break;

            case "take":
                _actionInvoker.HandleTake(room, _state.Player);
                break;

            case "use":
                _actionInvoker.HandleUse(room, _state.Player);
                break;

            case "talk":
                _actionInvoker.HandleTalk(room, _state.Player);
                break;

            case "inventory":
                _actionInvoker.ShowInventory(_state.Player);
                break;
        }
    }

    private void HandleExit(string direction, Room currentRoom)
    {
        if (!currentRoom.TryGetAction(direction, out var action) || action is not ExitAction exitAction)
        {
            Console.WriteLine("You can't go that way.");
            WaitForKey();
            return;
        }

        // Check condition on exit action
        var state = new GameState { CurrentRoom = currentRoom, Player = _state.Player };
        if (!string.IsNullOrEmpty(exitAction.Condition))
        {
            if (!_conditionEvaluator.Evaluate(exitAction.Condition, state))
            {
                Console.WriteLine("You can't go that way.");
                WaitForKey();
                return;
            }
        }

        // Show result text before executing actions? Or after? Spec says action occurs when user goes direction.
        // The result_text is displayed to the user; we can show it before changing rooms
        if (!string.IsNullOrEmpty(exitAction.ResultText))
        {
            Console.WriteLine(exitAction.ResultText);
        }

        // Execute any associated commands (these might affect the current room before leaving or the new room)
        _actionExecutor.Execute(exitAction.ActionCommands, state);

        // Find the target room
        var targetRoomName = exitAction.TargetRoom;
        var targetRoom = _state.GetRoom(targetRoomName);
        if (targetRoom == null)
        {
            Console.WriteLine($"ERROR: Room '{targetRoomName}' not found.");
            WaitForKey();
            return;
        }

        _state.CurrentRoom = targetRoom;
    }

    private static void WaitForKey()
    {
        Console.WriteLine();
        Console.ReadKey(true);
    }
}
