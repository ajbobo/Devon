namespace Devon.Services;

using Devon.Models;

/// <summary>
/// Main game controller
/// </summary>
public class Game
{
    private readonly IConditionEvaluator _conditionEvaluator;
    private readonly IJsonRoomLoader _roomLoader;
    private readonly ICutsceneLoader _cutsceneLoader;
    private MenuRenderer? _menuRenderer;
    private ActionInvoker? _actionInvoker;
    private IActionExecutor? _actionExecutor;

    private GameState _state = new();
    private readonly IConsole _console;

    public Game() : this(new SystemConsole())
    {
    }

    public Game(IConsole console)
    {
        _conditionEvaluator = new ConditionEvaluator();
        _roomLoader = new JsonRoomLoader("Devon.rooms.json", _conditionEvaluator);
        _cutsceneLoader = new JsonCutsceneLoader();
        _console = console ?? throw new System.ArgumentNullException(nameof(console));
    }

    public void Run()
    {
        InitializeGame();

        bool quit = false;
        while (!quit)
        {
            try
            {
                var actionKey = _menuRenderer!.GetActionFromPlayer(_state.CurrentRoom!, _state.Player);

                if (actionKey == "quit")
                {
                    quit = true;
                    try { _console.Clear(); } catch { }
                    _console.WriteLine("Thanks for playing!");
                    break;
                }

                ProcessAction(actionKey);
            }
            catch (GameOverException)
            {
                quit = true;
                try { _console.Clear(); } catch { }
                _console.WriteLine("Game Over!");
                _console.WriteLine();
                _console.WriteLine("Thanks for playing!");
            }
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

            // Load cutscenes from the cutscene loader
            var cutscenes = _cutsceneLoader.LoadCutscenesAsync().GetAwaiter().GetResult();
            foreach (var kvp in cutscenes)
            {
                _state.Cutscenes[kvp.Key] = kvp.Value;
            }

            // Set starting room - first room in the dictionary (or could have "start" field in JSON)
            // For now, pick the one named "Entrance" or first
            _state.CurrentRoom = _state.Rooms.Values.FirstOrDefault(r => r.Name.Equals("Entrance", StringComparison.OrdinalIgnoreCase))
                              ?? _state.Rooms.Values.First();

            // Initialize services that need cutscenes
            var cutsceneRenderer = new CutsceneRenderer(_console);
            _actionExecutor = new ActionExecutor(cutsceneRenderer, _state.Cutscenes);
            _menuRenderer = new MenuRenderer(_conditionEvaluator, _console);
            _actionInvoker = new ActionInvoker(_actionExecutor, _conditionEvaluator, _console);

            // Execute onEntry for the starting room
            ExecuteRoomOnEntry(_state.CurrentRoom);

            _console.WriteLine("Devon - Text Adventure");
            _console.WriteLine("======================");
            _console.WriteLine();
        }
        catch (Exception ex)
        {
            _console.WriteLine($"Error loading game: {ex.Message}");
            _console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }

    private void ExecuteRoomOnEntry(Room room)
    {
        if (!string.IsNullOrEmpty(room.OnEntry) && !_state.Player.VisitedRooms.Contains(room.Name))
        {
            _state.Player.VisitedRooms.Add(room.Name);
            _actionExecutor!.Execute(room.OnEntry, _state);
        }
    }

    private void ProcessAction(string actionKey)
    {
        var room = _state.CurrentRoom;
        if (room == null) return;

        switch (actionKey.ToLowerInvariant())
        {
            case "north" or "south" or "east" or "west" or "up" or "down" or "left" or "center" or "right":
                HandleExit(actionKey, room);
                break;

            case "take":
                _actionInvoker!.HandleTake(room, _state.Player);
                break;

            case "use":
                _actionInvoker!.HandleUse(room, _state.Player);
                break;

            case "talk":
                _actionInvoker!.HandleTalk(room, _state.Player);
                break;

            case "inventory":
                _actionInvoker!.ShowInventory(_state.Player);
                break;
        }
    }

    private void HandleExit(string direction, Room currentRoom)
    {
        if (!currentRoom.TryGetAction(direction, out var action) || action is not ExitAction exitAction)
        {
            _console.WriteLine("You can't go that way.");
            WaitForKey();
            return;
        }

        // Check condition on exit action
        var state = new GameState { CurrentRoom = currentRoom, Player = _state.Player };
        if (!string.IsNullOrEmpty(exitAction.Condition))
        {
            if (!_conditionEvaluator.Evaluate(exitAction.Condition, state))
            {
                _console.WriteLine("You can't go that way.");
                WaitForKey();
                return;
            }
        }

        // Show result text before executing actions? Or after? Spec says action occurs when user goes direction.
        // The result_text is displayed to the user; we can show it before changing rooms
        if (!string.IsNullOrEmpty(exitAction.ResultText))
        {
            _console.WriteLine(exitAction.ResultText);
        }

        // Execute any associated commands (these might affect the current room before leaving or the new room)
        _actionExecutor!.Execute(exitAction.ActionCommands, state);

        // Find the target room
        var targetRoomName = exitAction.TargetRoom;
        var targetRoom = _state.GetRoom(targetRoomName);
        if (targetRoom == null)
        {
            _console.WriteLine($"ERROR: Room '{targetRoomName}' not found.");
            WaitForKey();
            return;
        }

        // Switch to target room
        _state.CurrentRoom = targetRoom;
        ExecuteRoomOnEntry(targetRoom);
    }

    private void WaitForKey()
    {
        _console.WriteLine();
        _console.ReadKey(true);
    }
}
