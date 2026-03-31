namespace Devon.Services;

/// <summary>
/// Wraps System.Console to implement IConsole.
/// </summary>
public class SystemConsole : IConsole
{
    public void Clear() => Console.Clear();

    public void Write(string? value = null) => Console.Write(value);

    public void WriteLine(string? value = null) => Console.WriteLine(value);

    public ConsoleColor ForegroundColor
    {
        get => Console.ForegroundColor;
        set => Console.ForegroundColor = value;
    }

    public void ResetColor() => Console.ResetColor();

    public ConsoleKeyInfo ReadKey(bool intercept) => Console.ReadKey(intercept);

    public bool KeyAvailable => Console.KeyAvailable;

    public void SetCursorPosition(int left, int top) => Console.SetCursorPosition(left, top);

    public int CursorTop => Console.CursorTop;

    public string? ReadLine() => Console.ReadLine();
}
