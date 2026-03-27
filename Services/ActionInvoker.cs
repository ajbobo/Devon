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
        if (!room.TryGetAction("use", out var useAction) || useAction is not UseAction use)
        {
            Console.WriteLine("You can't use anything here.");
            WaitForKey();
            return;
        }

        // Check if player has the required item
        if (!player.HasItem(use.Item))
        {
            Console.WriteLine($"You don't have a {use.Item}.");
            WaitForKey();
            return;
        }

        // Prompt for target - either the specified target or any valid target
        Console.Write($"What would you like to use the {use.Item} on? ");
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

        var state = new GameState { CurrentRoom = room, Player = player };
        _executor.Execute(use.ActionCommands, state);

        WaitForKey();
    }

    /// <summary>
    /// Handles a Talk action - prompts for target and what to say
    /// </summary>
    public void HandleTalk(Room room, Player player)
    {
        // For talk, we need to find the talk action that matches the target. However the spec says talk action is defined with target and says.
        // In a room, there could be multiple talk actions? The spec shows actions.talk as a single entry.
        // We'll assume one talk action per room for now. The target is the NPC to talk to.
        if (!room.TryGetAction("talk", out var talkAction) || talkAction is not TalkAction talk)
        {
            Console.WriteLine("There's nobody to talk to.");
            WaitForKey();
            return;
        }

        // Check condition on talk action (availability checked before showing, but double-check)
        var state = new GameState { CurrentRoom = room, Player = player };
        if (!_conditionEvaluator.Evaluate(talk.Condition ?? "true", state))
        {
            Console.WriteLine("You can't talk to that now.");
            WaitForKey();
            return;
        }

        // Prompt: "What would you like to say to <target>?"
        Console.Write($"What would you like to say to the {talk.Target}? ");
        var whatTheySay = Console.ReadLine()?.Trim();

        if (!string.Equals(whatTheySay, talk.Says, StringComparison.OrdinalIgnoreCase))
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

        WaitForKey();
    }

    /// <summary>
    /// Handles Inventory display
    /// </summary>
    public void ShowInventory(Player player)
    {
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
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
    }
}
