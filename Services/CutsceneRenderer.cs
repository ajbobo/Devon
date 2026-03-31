namespace Devon.Services;

using Devon.Models;

/// <summary>
/// Renders cutscenes to the console with color and pause support.
/// Supports skipping the rest of the cutscene by pressing Esc at any prompt.
/// </summary>
public class CutsceneRenderer : ICutsceneRenderer
{
    private readonly IConsole _console;

    public CutsceneRenderer() : this(new SystemConsole()) { }

    public CutsceneRenderer(IConsole console)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
    }

    public void PlayCutscene(Cutscene cutscene)
    {
        if (cutscene == null) throw new ArgumentNullException(nameof(cutscene));

        // According to spec: first line is implicitly preceded by a clear
        bool isFirstLine = true;

        foreach (var line in cutscene.Text)
        {
            // Clear screen before this line if requested or if it's the first line (implicit clear)
            if (line.Clear || isFirstLine)
            {
                try { _console.Clear(); } catch { /* Ignore if console not available */ }
                isFirstLine = false;
            }

            // Set color if specified
            if (!string.IsNullOrEmpty(line.Color))
            {
                if (Enum.TryParse<ConsoleColor>(line.Color, true, out var color))
                {
                    _console.ForegroundColor = color;
                }
                // If parsing fails, keep default color
            }

            // Display the text
            _console.WriteLine(line.Text);

            // Reset color after each line to avoid color bleeding
            _console.ResetColor();

            // Check for Esc after non-wait lines (if key is already pressed)
            if (!line.Wait && _console.KeyAvailable)
            {
                var key = _console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape)
                {
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
                    break;
                }
            }
        }
    }
}
