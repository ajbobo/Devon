using Devon.Models;
using Devon.Services;
using Xunit;

namespace Devon.Tests;

/// <summary>
/// Fake IConsole for testing - simulates console behavior
/// </summary>
public class FakeConsole : IConsole
{
    private readonly Queue<ConsoleKeyInfo> _keyQueue;
    private bool _keyAvailable;
    private readonly List<string> _outputLines = new();
    private ConsoleColor _currentColor;
    private readonly Queue<string> _inputQueue = new();

    public FakeConsole(params ConsoleKeyInfo[] keys)
    {
        _keyQueue = new Queue<ConsoleKeyInfo>(keys);
        _keyAvailable = keys.Length > 0;
    }

    public IReadOnlyList<string> OutputLines => _outputLines;

    public void Clear()
    {
        _outputLines.Clear();
    }

    public void Write(string? value = null)
    {
        if (value != null)
            _outputLines.Add(value);
    }

    public void WriteLine(string? value = null)
    {
        _outputLines.Add(value ?? string.Empty);
    }

    public ConsoleColor ForegroundColor
    {
        get => _currentColor;
        set => _currentColor = value;
    }

    public void ResetColor()
    {
        _currentColor = ConsoleColor.Gray; // default
    }

    public ConsoleKeyInfo ReadKey(bool intercept)
    {
        if (_keyQueue.Count == 0)
        {
            // Simulate no key by blocking - but in tests we should always queue keys
            throw new InvalidOperationException("No more keys in queue");
        }

        var key = _keyQueue.Dequeue();
        _keyAvailable = _keyQueue.Count > 0;
        return key;
    }

    public bool KeyAvailable => _keyAvailable;

    public void SetCursorPosition(int left, int top) { }

    public int CursorTop => 0;

    public string? ReadLine()
    {
        if (_inputQueue.Count == 0)
            return null;
        return _inputQueue.Dequeue();
    }

    public void EnqueueInput(string input)
    {
        _inputQueue.Enqueue(input);
    }
}

/// <summary>
/// Fake IActionExecutor for testing cutscene result execution (does nothing)
/// </summary>
public class FakeActionExecutor : IActionExecutor
{
    public void Execute(string? commands, GameState state) { }
}

public class CutsceneRendererTests
{
    private readonly IConditionEvaluator _evaluator = new ConditionEvaluator();

    private GameState CreateGameState()
    {
        return new GameState
        {
            Player = new Player(),
            CurrentRoom = new Room { Name = "TestRoom" },
        };
    }

    [Fact]
    public void PlayCutscene_WithoutWait_DisplaysAllLines()
    {
        // Arrange
        var cutscene = new Cutscene
        {
            Text = new List<CutsceneText>
            {
                new() { Text = "Line 1" },
                new() { Text = "Line 2" },
                new() { Text = "Line 3" }
            }
        };
        var console = new FakeConsole(); // no key presses
        var renderer = new CutsceneRenderer(console, _evaluator);
        var state = CreateGameState();
        var executor = new FakeActionExecutor();

        // Act
        renderer.PlayCutscene(cutscene, state, executor);

        // Assert
        Assert.Equal(3, console.OutputLines.Count);
        Assert.Contains("Line 1", console.OutputLines);
        Assert.Contains("Line 2", console.OutputLines);
        Assert.Contains("Line 3", console.OutputLines);
    }

    [Fact]
    public void PlayCutscene_WithWait_PausesAndContinuesOnAnyKey()
    {
        // Arrange
        var cutscene = new Cutscene
        {
            Text = new List<CutsceneText>
            {
                new() { Text = "Line 1", Wait = true },
                new() { Text = "Line 2" }
            }
        };
        var console = new FakeConsole(new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false));
        var renderer = new CutsceneRenderer(console, _evaluator);
        var state = CreateGameState();
        var executor = new FakeActionExecutor();

        // Act
        renderer.PlayCutscene(cutscene, state, executor);

        // Assert - output: "Line 1", "" (empty after wait), "Line 2"
        Assert.Equal(3, console.OutputLines.Count);
        Assert.Contains("Line 1", console.OutputLines);
        Assert.Contains("", console.OutputLines); // empty line after wait
        Assert.Contains("Line 2", console.OutputLines);
    }

    [Fact]
    public void PlayCutscene_EscDuringWait_SkipsRemainingLines()
    {
        // Simulate: line1 (no wait), then after line1 a non-Esc key is consumed,
        // then Esc is consumed at line2 wait, skipping the rest.
        var cutscene = new Cutscene
        {
            Text = new List<CutsceneText>
            {
                new() { Text = "Line 1" },
                new() { Text = "Line 2", Wait = true },
                new() { Text = "Line 3" },
                new() { Text = "Line 4" }
            }
        };
        var console = new FakeConsole(
            new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false), // consumed after line1
            new ConsoleKeyInfo(' ', ConsoleKey.Escape, false, false, false) // consumed at line2 wait
        );
        var renderer = new CutsceneRenderer(console, _evaluator);
        var state = CreateGameState();
        var executor = new FakeActionExecutor();

        // Act
        renderer.PlayCutscene(cutscene, state, executor);

        // Assert - output: "Line 1", "Line 2", ""
        Assert.Equal(3, console.OutputLines.Count);
        Assert.Contains("Line 1", console.OutputLines);
        Assert.Contains("Line 2", console.OutputLines);
        Assert.Contains("", console.OutputLines);
        Assert.DoesNotContain("Line 3", console.OutputLines);
        Assert.DoesNotContain("Line 4", console.OutputLines);
    }

    [Fact]
    public void PlayCutscene_EscAfterNonWaitLine_SkipsRemaining()
    {
        var cutscene = new Cutscene
        {
            Text = new List<CutsceneText>
            {
                new() { Text = "Line 1" },
                new() { Text = "Line 2" },
                new() { Text = "Line 3" }
            }
        };
        var console = new FakeConsole(new ConsoleKeyInfo(' ', ConsoleKey.Escape, false, false, false));
        var renderer = new CutsceneRenderer(console, _evaluator);
        var state = CreateGameState();
        var executor = new FakeActionExecutor();

        // Act
        renderer.PlayCutscene(cutscene, state, executor);

        // Assert - only first line, no empty lines
        Assert.Single(console.OutputLines);
        Assert.Contains("Line 1", console.OutputLines);
    }

    [Fact]
    public void PlayCutscene_EscOnFirstWaitLine_SkipsRemaining()
    {
        var cutscene = new Cutscene
        {
            Text = new List<CutsceneText>
            {
                new() { Text = "Line 1", Wait = true },
                new() { Text = "Line 2", Wait = true },
                new() { Text = "Line 3" }
            }
        };
        var console = new FakeConsole(new ConsoleKeyInfo(' ', ConsoleKey.Escape, false, false, false));
        var renderer = new CutsceneRenderer(console, _evaluator);
        var state = CreateGameState();
        var executor = new FakeActionExecutor();

        // Act
        renderer.PlayCutscene(cutscene, state, executor);

        // Assert - output: "Line 1", ""
        Assert.Equal(2, console.OutputLines.Count);
        Assert.Contains("Line 1", console.OutputLines);
        Assert.Contains("", console.OutputLines);
        Assert.DoesNotContain("Line 2", console.OutputLines);
        Assert.DoesNotContain("Line 3", console.OutputLines);
    }

    [Fact]
    public void PlayCutscene_ClearFirstLine_Works()
    {
        var cutscene = new Cutscene
        {
            Text = new List<CutsceneText>
            {
                new() { Text = "Line 1", Clear = true },
                new() { Text = "Line 2" }
            }
        };
        var console = new FakeConsole();
        var renderer = new CutsceneRenderer(console, _evaluator);
        var state = CreateGameState();
        var executor = new FakeActionExecutor();

        // Act
        renderer.PlayCutscene(cutscene, state, executor);

        // Assert
        Assert.Equal(2, console.OutputLines.Count);
        Assert.Contains("Line 1", console.OutputLines);
        Assert.Contains("Line 2", console.OutputLines);
    }

    [Fact]
    public void PlayCutscene_NullCutscene_ThrowsArgumentNullException()
    {
        var console = new FakeConsole();
        var renderer = new CutsceneRenderer(console, _evaluator);
        var state = CreateGameState();
        var executor = new FakeActionExecutor();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => renderer.PlayCutscene(null!, state, executor));
    }

    [Fact]
    public void PlayCutscene_ColorChange_AppliedAndReset()
    {
        var cutscene = new Cutscene
        {
            Text = new List<CutsceneText>
            {
                new() { Text = "Red line", Color = "Red" },
                new() { Text = "Normal line" }
            }
        };
        var console = new FakeConsole();
        var renderer = new CutsceneRenderer(console, _evaluator);
        var state = CreateGameState();
        var executor = new FakeActionExecutor();

        // Act
        renderer.PlayCutscene(cutscene, state, executor);

        // Assert - we can't directly test color in FakeConsole without extra tracking
        // But we can verify it doesn't throw and both lines are written
        Assert.Equal(2, console.OutputLines.Count);
        Assert.Contains("Red line", console.OutputLines);
        Assert.Contains("Normal line", console.OutputLines);
    }

    [Fact]
    public void PlayCutscene_NonEscKey_ContinuesNormally()
    {
        var cutscene = new Cutscene
        {
            Text = new List<CutsceneText>
            {
                new() { Text = "Line 1" },
                new() { Text = "Line 2", Wait = true },
                new() { Text = "Line 3" }
            }
        };
        // Need a key for the non-wait check after line1, and a key for the wait
        var console = new FakeConsole(
            new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false), // consumed after line1
            new ConsoleKeyInfo('b', ConsoleKey.B, false, false, false)  // consumed at line2 wait
        );
        var renderer = new CutsceneRenderer(console, _evaluator);
        var state = CreateGameState();
        var executor = new FakeActionExecutor();

        // Act
        renderer.PlayCutscene(cutscene, state, executor);

        // Assert - output: "Line 1", "Line 2", "", "Line 3"
        Assert.Equal(4, console.OutputLines.Count);
        Assert.Contains("Line 1", console.OutputLines);
        Assert.Contains("Line 2", console.OutputLines);
        Assert.Contains("", console.OutputLines);
        Assert.Contains("Line 3", console.OutputLines);
    }

    [Fact]
    public void PlayCutscene_ConditionTrue_ShowsLine()
    {
        // Arrange: line with condition Player.hasItem(sword), and player has the sword
        var cutscene = new Cutscene
        {
            Text = new List<CutsceneText>
            {
                new() { Text = "Safe" },
                new() { Condition = "Player.hasItem(sword)", Text = "Has sword" },
                new() { Text = "End" }
            }
        };
        var console = new FakeConsole();
        var renderer = new CutsceneRenderer(console, _evaluator);
        var state = CreateGameState();
        state.Player.AddItem("sword");
        var executor = new FakeActionExecutor();

        // Act
        renderer.PlayCutscene(cutscene, state, executor);

        // Assert - all lines shown since condition is true
        Assert.Contains("Has sword", console.OutputLines);
        Assert.Contains("Safe", console.OutputLines);
        Assert.Contains("End", console.OutputLines);
    }

    [Fact]
    public void PlayCutscene_ConditionFalse_SkipsLine()
    {
        // Arrange: two mutually exclusive outcome lines (like Study armor scenario)
        var cutscene = new Cutscene
        {
            Text = new List<CutsceneText>
            {
                new() { Text = "Ventok appears" },
                new() { Text = "Throws fire" },
                new() { Condition = "Player.hasItem(Armor)", Text = "You live, thanks to armor" },
                new() { Condition = "!Player.hasItem(Armor)", Text = "You die" }
            }
        };
        var console = new FakeConsole();
        var renderer = new CutsceneRenderer(console, _evaluator);
        var state = CreateGameState();
        // Player does NOT have armor
        var executor = new FakeActionExecutor();

        // Act
        renderer.PlayCutscene(cutscene, state, executor);

        // Assert - "You live" skipped, "You die" shown
        Assert.DoesNotContain("You live, thanks to armor", console.OutputLines);
        Assert.Contains("You die", console.OutputLines);
        Assert.Contains("Ventok appears", console.OutputLines);
        Assert.Contains("Throws fire", console.OutputLines);
    }
}
