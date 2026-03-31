using Devon.Models;

namespace Devon.Services;

/// <summary>
/// Handles user prompts for Take/Use/Talk actions and invokes them
/// </summary>
public class ActionInvoker
{
    private readonly IActionExecutor _executor;
    private readonly IConditionEvaluator _conditionEvaluator;
    private readonly IConsole _console;

    public ActionInvoker(IActionExecutor executor, IConditionEvaluator conditionEvaluator, IConsole console)
    {
        _executor = executor;
        _conditionEvaluator = conditionEvaluator;
        _console = console;
    }

    /// <summary>
    /// Handles a Take action from the current room
    /// </summary>
    public void HandleTake(Room room, Player player)
    {
        if (!room.TryGetAction("take", out var takeAction) || takeAction is not TakeAction take)
        {
            _console.WriteLine("There's nothing to take here.");
            WaitForKey();
            return;
        }

        var item = take.Item;
        if (!room.Items.Contains(item))
        {
            _console.WriteLine("That item is no longer available.");
            WaitForKey();
            return;
        }

        // Remove from room, add to inventory
        room.Items.Remove(item);
        player.AddItem(item); // Always add to inventory

        if (!string.IsNullOrEmpty(take.ResultText))
        {
            _console.WriteLine(take.ResultText);
        }

        // Execute any additional commands (could modify conditions, etc.)
        _executor.Execute(take.ActionCommands, new GameState { CurrentRoom = room, Player = player });

        WaitForKey();
    }

    /// <summary>
    /// Handles a Use action - prompts for item and target
    /// </summary>
    public void HandleUse(Room room, Player player)
    {
        // Always prompt for which item to use
        _console.Write("What would you like to use? ");
        var itemInput = _console.ReadLine()?.Trim(); // Using Console.ReadLine directly as it's not easily mockable

        // Validate that the room has a use action and the input matches the required item
        if (!room.TryGetAction("use", out var useAction) || useAction is not UseAction use || !string.Equals(itemInput, use.Item, StringComparison.OrdinalIgnoreCase))
        {
            _console.WriteLine("That's not something you can use here.");
            WaitForKey();
            return;
        }

        // Check if the use action's condition is satisfied
        var state = new GameState { CurrentRoom = room, Player = player };
        if (!_conditionEvaluator.Evaluate(use.Condition ?? "true", state))
        {
            _console.WriteLine("You can't use that now.");
            WaitForKey();
            return;
        }

        // Check that the player actually has the item in inventory
        if (!player.HasItem(use.Item))
        {
            _console.WriteLine($"You don't have a {use.Item}.");
            WaitForKey();
            return;
        }

        // Prompt for target
        _console.Write($"What would you like to use it on? ");
        var targetInput = _console.ReadLine()?.Trim();

        // If the action specifies a target, it must match exactly
        if (!string.IsNullOrEmpty(use.Target) && !string.Equals(targetInput, use.Target, StringComparison.OrdinalIgnoreCase))
        {
            _console.WriteLine($"That doesn't work.");
            WaitForKey();
            return;
        }

        // Execute the action
        if (!string.IsNullOrEmpty(use.ResultText))
        {
            _console.WriteLine(use.ResultText);
        }

        state = new GameState { CurrentRoom = room, Player = player };
        _executor.Execute(use.ActionCommands, state);

        WaitForKey();
    }

    /// <summary>
    /// Handles a Talk action - prompts for target and what to say
    /// Always available, regardless of talk action definition in room
    /// </summary>
    public void HandleTalk(Room room, Player player)
    {
        // Always prompt for target (who to talk to)
        _console.Write("Who would you like to talk to? ");
        var targetInput = _console.ReadLine()?.Trim();

        // Check if there is a talk action for this target
        if (room.TryGetAction("talk", out var talkAction) && talkAction is TalkAction talk)
        {
            var state = new GameState { CurrentRoom = room, Player = player };
            if (!_conditionEvaluator.Evaluate(talk.Condition ?? "true", state))
            {
                _console.WriteLine("You can't talk to that now.");
                WaitForKey();
                return;
            }

            if (!string.Equals(targetInput, talk.Target, StringComparison.OrdinalIgnoreCase))
            {
                _console.WriteLine("That person isn't here.");
                WaitForKey();
                return;
            }

            // Prompt for what to say
            _console.Write("What would you like to say? ");
            var saysInput = _console.ReadLine()?.Trim();

            if (!string.Equals(saysInput, talk.Says, StringComparison.OrdinalIgnoreCase))
            {
                _console.WriteLine("The NPC doesn't respond to that.");
                WaitForKey();
                return;
            }

            // Correct response
            if (!string.IsNullOrEmpty(talk.ResultText))
            {
                _console.WriteLine(talk.ResultText);
            }

            _executor.Execute(talk.ActionCommands, state);
        }
        else
        {
            // No talk action defined - generic response
            _console.WriteLine($"You talk to {targetInput}, but there's no response.");
        }

        WaitForKey();
    }

    /// <summary>
    /// Handles Inventory display
    /// </summary>
    public void ShowInventory(Player player)
    {
        _console.Clear();
        _console.WriteLine("You are carrying:");
        if (player.Inventory.Count == 0)
        {
            _console.WriteLine("  Nothing.");
        }
        else
        {
            foreach (var item in player.Inventory)
            {
                _console.WriteLine($"  - {item}");
            }
        }
        _console.WriteLine();
        WaitForKey();
    }

    /// <summary>
    /// Handles movement to a new room
    /// </summary>
    public void TransitionToRoom(Room fromRoom, Room toRoom, ExitAction exitAction, GameState state, ActionExecutor executor)
    {
        // Note: exitAction is specific to the direction taken
        // The action may have result text and commands to execute upon entering the new room
        // We should execute commands AFTER entering? The spec says action occurs when user goes direction.
        // Typically: execute commands, then change room.

        if (!string.IsNullOrEmpty(exitAction.ResultText))
        {
            _console.WriteLine(exitAction.ResultText);
            WaitForKey();
        }

        // Execute commands (which may modify conditions, inventory, etc)
        executor.Execute(exitAction.ActionCommands, state);

        // Actually, based on spec: "action: <a list of actions that occur when the user goes this direction>"
        // These happen when transitioning. So we do them now, then move.
        // The state's CurrentRoom will be updated after this method returns, so ensure commands reference the old room if needed.

        // WaitForKey occurs after displaying result text, but we may also want to show the new room next.
    }

    private void WaitForKey()
    {
        _console.WriteLine();
        _console.ReadKey(true);
    }
}
