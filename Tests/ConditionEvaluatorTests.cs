using Devon.Models;
using Devon.Services;
using Xunit;

namespace Devon.Tests;

public class ConditionEvaluatorTests
{
    private readonly IConditionEvaluator _evaluator = new ConditionEvaluator();

    [Fact]
    public void EmptyCondition_ReturnsTrue()
    {
        var state = new GameState();
        bool result = _evaluator.Evaluate(string.Empty, state);
        Assert.True(result);
    }

    [Fact]
    public void TrueLiteral_ReturnsTrue()
    {
        var state = new GameState();
        bool result = _evaluator.Evaluate("true", state);
        Assert.True(result);
    }

    [Fact]
    public void FalseLiteral_ReturnsFalse()
    {
        var state = new GameState();
        bool result = _evaluator.Evaluate("false", state);
        Assert.False(result);
    }

    [Fact]
    public void HasItem_WithItemInInventory_ReturnsTrue()
    {
        var player = new Player();
        player.AddItem("key");
        var state = new GameState { Player = player };
        bool result = _evaluator.Evaluate("Player.hasItem(key)", state);
        Assert.True(result);
    }

    [Fact]
    public void HasItem_WithItemNotInInventory_ReturnsFalse()
    {
        var player = new Player();
        var state = new GameState { Player = player };
        bool result = _evaluator.Evaluate("Player.hasItem(sword)", state);
        Assert.False(result);
    }

    [Fact]
    public void HasCondition_PlayerHasCondition_ReturnsTrue()
    {
        var player = new Player();
        player.AddCondition("has_armor");
        var state = new GameState { Player = player };
        bool result = _evaluator.Evaluate("Player.hasCondition(has_armor)", state);
        Assert.True(result);
    }

    [Fact]
    public void NotOperator_WorksCorrectly()
    {
        var player = new Player();
        player.AddCondition("has_armor");
        var state = new GameState { Player = player };
        bool result = _evaluator.Evaluate("NOT(Player.hasCondition(has_armor))", state);
        Assert.False(result);
    }

    [Fact]
    public void RoomHasCondition_WithConditionPresent_ReturnsTrue()
    {
        var room = new Room { Conditions = new() };
        room.Conditions.Add("lit");
        var player = new Player();
        var state = new GameState { Player = player, CurrentRoom = room };
        bool result = _evaluator.Evaluate("Room.hasCondition(lit)", state);
        Assert.True(result);
    }

    [Fact]
    public void AndOperator_AllTrue_ReturnsTrue()
    {
        var player = new Player();
        player.AddItem("key");
        player.AddCondition("has_armor");
        var room = new Room { Conditions = new() };
        room.Conditions.Add("lit");
        var state = new GameState { Player = player, CurrentRoom = room };
        bool result = _evaluator.Evaluate("AND(Player.hasItem(key), Player.hasCondition(has_armor))", state);
        Assert.True(result);
    }

    [Fact]
    public void AndOperator_OneFalse_ReturnsFalse()
    {
        var player = new Player();
        player.AddItem("key");
        var room = new Room { Conditions = new() };
        room.Conditions.Add("lit");
        var state = new GameState { Player = player, CurrentRoom = room };
        bool result = _evaluator.Evaluate("AND(Player.hasItem(key), Player.hasCondition(has_armor))", state);
        Assert.False(result); // has_armor not set
    }

    [Fact]
    public void OrOperator_OneTrue_ReturnsTrue()
    {
        var player = new Player();
        player.AddItem("key");
        var room = new Room { Conditions = new() };
        room.Conditions.Add("lit");
        var state = new GameState { Player = player, CurrentRoom = room };
        bool result = _evaluator.Evaluate("OR(Player.hasItem(sword), Room.hasCondition(lit))", state);
        Assert.True(result); // room has lit
    }

    [Fact]
    public void OrOperator_AllFalse_ReturnsFalse()
    {
        var player = new Player();
        var room = new Room { Conditions = new() };
        var state = new GameState { Player = player, CurrentRoom = room };
        bool result = _evaluator.Evaluate("OR(Player.hasItem(sword), Room.hasCondition(lit))", state);
        Assert.False(result);
    }

    [Fact]
    public void ComplexNestedExpression_Works()
    {
        var player = new Player();
        player.AddItem("key");
        player.AddCondition("has_armor");
        var room = new Room { Conditions = new() };
        room.Conditions.Add("lit");
        var state = new GameState { Player = player, CurrentRoom = room };

        // NOT(AND(Player.hasItem(key), Player.hasCondition(has_armor)))
        bool result = _evaluator.Evaluate("NOT(AND(Player.hasItem(key), Player.hasCondition(has_armor)))", state);
        Assert.False(result); // both are true, so Not(true) = false
    }

    [Fact]
    public void AndWithLiterals_Works()
    {
        var state = new GameState();
        bool result = _evaluator.Evaluate("And(true, true)", state);
        Assert.True(result);
        result = _evaluator.Evaluate("And(true, false)", state);
        Assert.False(result);
    }

    [Fact]
    public void OrWithLiterals_Works()
    {
        var state = new GameState();
        bool result = _evaluator.Evaluate("Or(false, false)", state);
        Assert.False(result);
        result = _evaluator.Evaluate("Or(false, true)", state);
        Assert.True(result);
    }

    [Fact]
    public void NotWithLiteral_Works()
    {
        var state = new GameState();
        bool result = _evaluator.Evaluate("Not(true)", state);
        Assert.False(result);
        result = _evaluator.Evaluate("Not(false)", state);
        Assert.True(result);
    }

    [Fact]
    public void RoomHasItem_WithItemInRoom_ReturnsTrue()
    {
        var room = new Room { Items = new() };
        room.Items.Add("key");
        var player = new Player();
        var state = new GameState { Player = player, CurrentRoom = room };
        bool result = _evaluator.Evaluate("Room.hasItem(key)", state);
        Assert.True(result);
    }

    [Fact]
    public void RoomHasItem_WithItemNotInRoom_ReturnsFalse()
    {
        var room = new Room { Items = new() };
        var player = new Player();
        var state = new GameState { Player = player, CurrentRoom = room };
        bool result = _evaluator.Evaluate("Room.hasItem(sword)", state);
        Assert.False(result);
    }

    [Fact]
    public void RoomHasItem_WithMultiWordItem_Works()
    {
        var room = new Room { Items = new() };
        room.Items.Add("brass key");
        var player = new Player();
        var state = new GameState { Player = player, CurrentRoom = room };
        bool result = _evaluator.Evaluate("Room.hasItem(brass key)", state);
        Assert.True(result);
    }

    [Fact]
    public void RoomHasItem_WithSpaceButNotExactMatch_ReturnsFalse()
    {
        var room = new Room { Items = new() };
        room.Items.Add(" brass key "); // With spaces (trimmed on add)
        var player = new Player();
        var state = new GameState { Player = player, CurrentRoom = room };
        bool result = _evaluator.Evaluate("Room.hasItem(brass key)", state);
        Assert.False(result); // "brass key" is the name after trim
    }
}
