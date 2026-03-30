namespace Devon.Models;

/// <summary>
/// Represents a cutscene - a scripted sequence of text lines with optional formatting
/// </summary>
public class Cutscene
{
    public string Name { get; set; } = string.Empty;
    public List<CutsceneText> Text { get; set; } = new();
}

/// <summary>
/// Represents a single line of text in a cutscene
/// </summary>
public class CutsceneText
{
    public string Text { get; set; } = string.Empty;
    public string? Color { get; set; }  // Optional color name (e.g., "yellow", "red")
    public bool Wait { get; set; } = false;  // If true, pause for key press after displaying
    public bool Clear { get; set; } = false; // If true, clear screen before displaying this line
}
