using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Devon.Editor.ViewModels;

/// <summary>
/// Editable entry for a single line of text in a cutscene
/// </summary>
public partial class CutsceneTextEntry : ObservableObject
{
    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private string? _color;

    [ObservableProperty]
    private bool _wait = false;

    [ObservableProperty]
    private bool _clear = false;

    [ObservableProperty]
    private string? _condition;

    [ObservableProperty]
    private string? _result;
}
