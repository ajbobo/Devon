namespace Devon.Services;

using Devon.Models;

/// <summary>
/// Renders cutscenes to the console with color and pause support.
/// Supports skipping the rest of the cutscene by pressing Esc at any prompt.
/// </summary>
public class CutsceneRenderer : ICutsceneRenderer
{
    private readonly IConsole _console;
    private readonly IConditionEvaluator _evaluator;

    public CutsceneRenderer() : this(new SystemConsole(), new ConditionEvaluator()) { }

    public CutsceneRenderer(IConsole console, IConditionEvaluator evaluator)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    /// <summary>
    /// Plays the given cutscene, evaluating conditions and executing results per line.
    /// </summary>
    /// <param name="cutscene">The cutscene to play</param>
    /// <param name="state">The current game state (used for condition evaluation)</param>
    /// <param name="executor">The action executor (used to execute result actions)</param>
    /// <returns>True if the cutscene played to completion, false if it was skipped.</returns>
    public bool PlayCutscene(Cutscene cutscene, GameState state, IActionExecutor executor)
    {
        if (cutscene == null) throw new ArgumentNullException(nameof(cutscene));
        if (state == null) throw new ArgumentNullException(nameof(state));
        if (executor == null) throw new ArgumentNullException(nameof(executor));

        bool firstDisplay = true;
        bool skipped = false;

        foreach (var line in cutscene.Text)
        {
            // Condition evaluation: if a condition is present and it evaluates to false, skip this line
            if (!string.IsNullOrEmpty(line.Condition))
            {
                if (!_evaluator.Evaluate(line.Condition, state))
                {
                    continue;
                }
            }

            // Screen clear: either clear explicitly, or implicitly for first displayed line
            if (line.Clear || firstDisplay)
            {
                try { _console.Clear(); } catch { /* Ignore if console not available */ }
                firstDisplay = false;
            }

            // Set color if specified
            if (!string.IsNullOrEmpty(line.Color))
            {
                if (Enum.TryParse<ConsoleColor>(line.Color, true, out var color))
                {
                    _console.ForegroundColor = color;
                }
            }

            // Display the text
            _console.WriteLine(line.Text);
            _console.ResetColor();

            // Check for Esc after non-wait lines (if key is already pressed)
            if (!line.Wait && _console.KeyAvailable)
            {
                var key = _console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape)
                {
                    skipped = true;
                    // Still execute result if present even on skip
                    if (!string.IsNullOrEmpty(line.Result))
                    {
                        executor.Execute(line.Result, state);
                    }
                    break;
                }
            }

            // Wait for key press if requested
            if (line.Wait)
            {
                _console.WriteLine();
                var key = _console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape)
                {
                    skipped = true;
                    // Still execute result if present
                    if (!string.IsNullOrEmpty(line.Result))
                    {
                        executor.Execute(line.Result, state);
                    }
                    break;
                }
            }

            // Execute result actions after display and wait
            if (!string.IsNullOrEmpty(line.Result))
            {
                executor.Execute(line.Result, state);
            }
        }

        return !skipped;
    }
}
