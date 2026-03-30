using Devon.Models;

namespace Devon.Services;

/// <summary>
/// Handles user prompts for Take/Use/Talk actions and invokes them
/// </summary>
public class ActionInvoker
{
    private readonly IActionExecutor _executor;
    private readonly IConditionEvaluator _conditionEvaluator;

    public ActionInvoker(IActionExecutor executor, IConditionEvaluator conditionEvaluator)
    {
        _executor = executor;
        _conditionEvaluator = conditionEvaluator;
    }

    /// <summary>
    /// Handles a Take action from the current room
    /// </summary>
    public void HandleTake(Room room, Player player)
    {
        if (!room.TryGetAction("take", out var takeAction) || takeAction is not TakeAction take)
        {
            Console.WriteLine("There's nothing to take here.");
            WaitForKey();
            return;
        }

        var item = take.Item;
        if (!room.Items.Contains(item))
        {
            Console.WriteLine("That item is no longer available.");
            WaitForKey();
            return;
        }

        // Remove from room, add to inventory
        room.Items.Remove(item);
        player.AddItem(item); // Always add to inventory

        if (!string.IsNullOrEmpty(take.ResultText))
        {
            Console.WriteLine(take.ResultText);
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
        Console.Write("What would you like to use? ");
        var itemInput = Console.ReadLine()?.Trim();

        // Validate that the room has a use action and the input matches the required item
        if (!room.TryGetAction("use", out var useAction) || useAction is not UseAction use || !string.Equals(itemInput, use.Item, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("That's not something you can use here.");
            WaitForKey();
            return;
        }

        // Check if the use action's condition is satisfied
        var state = new GameState { CurrentRoom = room, Player = player };
        if (!_conditionEvaluator.Evaluate(use.Condition ?? "true", state))
        {
            Console.WriteLine("You can't use that now.");
            WaitForKey();
            return;
        }

        // Check that the player actually has the item in inventory
        if (!player.HasItem(use.Item))
        {
            Console.WriteLine($"You don't have a {use.Item}.");
            WaitForKey();
            return;
        }

        // Prompt for target
        Console.Write($"What would you like to use it on? ");
        var targetInput = Console.ReadLine()?.Trim();

        // If the action specifies a target, it must match exactly
        if (!string.IsNullOrEmpty(use.Target) && !string.Equals(targetInput, use.Target, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"That doesn't work.");
            WaitForKey();
            return;
        }

        // Execute the action
        if (!string.IsNullOrEmpty(use.ResultText))
        {
            Console.WriteLine(use.ResultText);
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
        Console.Write("Who would you like to talk to? ");
        var targetInput = Console.ReadLine()?.Trim();

        // Check if there is a talk action for this target
        if (room.TryGetAction("talk", out var talkAction) && talkAction is TalkAction talk)
        {
            var state = new GameState { CurrentRoom = room, Player = player };
            if (!_conditionEvaluator.Evaluate(talk.Condition ?? "true", state))
            {
                Console.WriteLine("You can't talk to that now.");
                WaitForKey();
                return;
            }

            if (!string.Equals(targetInput, talk.Target, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("That person isn't here.");
                WaitForKey();
                return;
            }

            // Prompt for what to say
            Console.Write("What would you like to say? ");
            var saysInput = Console.ReadLine()?.Trim();

            if (!string.Equals(saysInput, talk.Says, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("The NPC doesn't respond to that.");
                WaitForKey();
                return;
            }

            // Correct response
            if (!string.IsNullOrEmpty(talk.ResultText))
            {
                Console.WriteLine(talk.ResultText);
            }

            _executor.Execute(talk.ActionCommands, state);
        }
        else
        {
            // No talk action defined - generic response
            Console.WriteLine($"You talk to {targetInput}, but there's no response.");
        }

        WaitForKey();
    }

    /// <summary>
    /// Handles Inventory display
    /// </summary>
    public void ShowInventory(Player player)
    {
        Console.Clear();
        Console.WriteLine("You are carrying:");
        if (player.Inventory.Count == 0)
        {
            Console.WriteLine("  Nothing.");
        }
        else
        {
            foreach (var item in player.Inventory)
            {
                Console.WriteLine($"  - {item}");
            }
        }
        Console.WriteLine();
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
            Console.WriteLine(exitAction.ResultText);
            WaitForKey();
        }

        // Execute commands (which may modify conditions, inventory, etc)
        executor.Execute(exitAction.ActionCommands, state);

        // Actually, based on spec: "action: <a list of actions that occur when the user goes this direction>"
        // These happen when transitioning. So we do them now, then move.
        // The state's CurrentRoom will be updated after this method returns, so ensure commands reference the old room if needed.

        // WaitForKey occurs after displaying result text, but we may also want to show the new room next.
    }

    private static void WaitForKey()
    {
        Console.WriteLine();
        Console.ReadKey(true);
    }
}
