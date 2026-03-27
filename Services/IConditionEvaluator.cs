namespace Devon.Services;

/// <summary>
/// Evaluates condition expressions against a game state
/// </summary>
public interface IConditionEvaluator
{
    /// <summary>
    /// Evaluates a condition expression string (e.g., "HasItem(\"key\")") against the given game state
    /// </summary>
    bool Evaluate(string expression, Devon.Models.GameState state);
}
