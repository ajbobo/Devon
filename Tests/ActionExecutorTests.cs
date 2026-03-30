using Devon.Models;
using Devon.Services;
using Xunit;

namespace Devon.Tests;

public class ActionExecutorTests
{
    private readonly IActionExecutor _actionExecutor;

    public ActionExecutorTests()
    {
        var cutsceneRenderer = new CutsceneRenderer();
        var cutscenes = new Dictionary<string, Cutscene>(StringComparer.OrdinalIgnoreCase);
        _actionExecutor = new ActionExecutor(cutsceneRenderer, cutscenes);
    }

    [Fact]
    public void Execute_GameGameOver_ThrowsGameOverException()
    {
        // Arrange
        var state = new GameState();

        // Act & Assert
        var exception = Assert.Throws<GameOverException>(() =>
            _actionExecutor.Execute("Game.gameOver()", state));
        Assert.Equal("Game over", exception.Message);
    }

    [Fact]
    public void Execute_GameGameOverCaseInsensitive_ThrowsGameOverException()
    {
        // Arrange
        var state = new GameState();

        // Act & Assert
        var exception = Assert.Throws<GameOverException>(() =>
            _actionExecutor.Execute("Game.GAMEOVER()", state));
        Assert.Equal("Game over", exception.Message);
    }

    [Fact]
    public void Execute_GameUnknownMethod_ThrowsInvalidOperationException()
    {
        // Arrange
        var state = new GameState();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _actionExecutor.Execute("Game.unknownMethod()", state));
        Assert.Contains("Unknown Game method", exception.Message);
    }

    [Fact]
    public void Execute_GameGameOverWithExtraSpaces_ThrowsGameOverException()
    {
        // Arrange
        var state = new GameState();

        // Act & Assert
        var exception = Assert.Throws<GameOverException>(() =>
            _actionExecutor.Execute("Game.gameOver( )", state));
        Assert.Equal("Game over", exception.Message);
    }

    [Fact]
    public void Execute_GameGameOverWithArg_StillThrowsGameOverException()
    {
        // Even though the spec doesn't have args, parser will strip what's in parens
        // This test ensures we ignore any arg passed
        var state = new GameState();

        var exception = Assert.Throws<GameOverException>(() =>
            _actionExecutor.Execute("Game.gameOver(something)", state));
        Assert.Equal("Game over", exception.Message);
    }
}
