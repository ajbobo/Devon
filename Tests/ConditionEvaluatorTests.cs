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
        bool result = _evaluator.Evaluate("HasItem(\"key\")", state);
        Assert.True(result);
    }

    [Fact]
    public void HasItem_WithItemNotInInventory_ReturnsFalse()
    {
        var player = new Player();
        var state = new GameState { Player = player };
        bool result = _evaluator.Evaluate("HasItem(\"sword\")", state);
        Assert.False(result);
    }

    [Fact]
    public void HasCondition_PlayerHasCondition_ReturnsTrue()
    {
        var player = new Player();
        player.AddCondition("has_armor");
        var state = new GameState { Player = player };
        bool result = _evaluator.Evaluate("HasCondition(\"has_armor\")", state);
        Assert.True(result);
    }

    [Fact]
    public void NotOperator_WorksCorrectly()
    {
        var player = new Player();
        player.AddCondition("has_armor");
        var state = new GameState { Player = player };
        bool result = _evaluator.Evaluate("Not(HasCondition(\"has_armor\"))", state);
        Assert.False(result);
    }

    [Fact]
    public void RoomHasCondition_WithConditionPresent_ReturnsTrue()
    {
        var room = new Room { Conditions = new() };
        room.Conditions.Add("lit");
        var player = new Player();
        var state = new GameState { Player = player, CurrentRoom = room };
        bool result = _evaluator.Evaluate("RoomHasCondition(\"lit\")", state);
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
        bool result = _evaluator.Evaluate("And(HasItem(\"key\"), HasCondition(\"has_armor\"))", state);
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
        bool result = _evaluator.Evaluate("And(HasItem(\"key\"), HasCondition(\"has_armor\"))", state);
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
        bool result = _evaluator.Evaluate("Or(HasItem(\"sword\"), RoomHasCondition(\"lit\"))", state);
        Assert.True(result); // room has lit
    }

    [Fact]
    public void OrOperator_AllFalse_ReturnsFalse()
    {
        var player = new Player();
        var room = new Room { Conditions = new() };
        var state = new GameState { Player = player, CurrentRoom = room };
        bool result = _evaluator.Evaluate("Or(HasItem(\"sword\"), RoomHasCondition(\"lit\"))", state);
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

        // Not(And(HasItem("key"), HasCondition("has_armor")))
        bool result = _evaluator.Evaluate("Not(And(HasItem(\"key\"), HasCondition(\"has_armor\")))", state);
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
}
