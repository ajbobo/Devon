namespace Devon.Services;

/// <summary>
/// Abstraction of console operations for testability.
/// </summary>
public interface IConsole
{
    void Clear();
    void Write(string? value = null);
    void WriteLine(string? value = null);
    ConsoleColor ForegroundColor { get; set; }
    void ResetColor();
    ConsoleKeyInfo ReadKey(bool intercept);
    bool KeyAvailable { get; }
    void SetCursorPosition(int left, int top);
    int CursorTop { get; }
    string? ReadLine();
}
