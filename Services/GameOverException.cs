namespace Devon.Services;

/// <summary>
/// Exception thrown when the game should end (via Game.gameOver() action)
/// </summary>
public class GameOverException : Exception
{
    public GameOverException() : base("Game over")
    {
    }
}
