namespace Devon.Services;

using Devon.Models;

/// <summary>
/// Renders cutscenes to the console with color and pause support
/// </summary>
public class CutsceneRenderer : ICutsceneRenderer
{
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
                try { Console.Clear(); } catch { /* Ignore if console not available */ }
                isFirstLine = false;
            }

            // Set color if specified
            if (!string.IsNullOrEmpty(line.Color))
            {
                if (Enum.TryParse<ConsoleColor>(line.Color, true, out var color))
                {
                    Console.ForegroundColor = color;
                }
                // If parsing fails, keep default color
            }

            // Display the text
            Console.WriteLine(line.Text);

            // Reset color after each line to avoid color bleeding
            Console.ResetColor();

            // Wait for key press if requested
            if (line.Wait)
            {
                Console.WriteLine();
                Console.ReadKey(true);
            }
        }
    }
}
