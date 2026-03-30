using CommunityToolkit.Mvvm.ComponentModel;
using Devon.Models;
using System.Collections.ObjectModel;

namespace Devon.Editor.ViewModels;

/// <summary>
/// ViewModel for editing a single cutscene
/// </summary>
public partial class CutsceneEditorViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private ObservableCollection<CutsceneTextEntry> _textLines = new();

    [ObservableProperty]
    private CutsceneTextEntry? _selectedTextLine;

    public void LoadFromCutscene(Cutscene cutscene)
    {
        Name = cutscene.Name;
        TextLines.Clear();
        foreach (var line in cutscene.Text)
        {
            TextLines.Add(new CutsceneTextEntry
            {
                Text = line.Text,
                Color = line.Color,
                Wait = line.Wait,
                Clear = line.Clear
            });
        }
    }

    public void ApplyToCutscene(Cutscene cutscene)
    {
        cutscene.Name = Name;
        cutscene.Text.Clear();
        foreach (var entry in TextLines)
        {
            cutscene.Text.Add(new CutsceneText
            {
                Text = entry.Text,
                Color = entry.Color,
                Wait = entry.Wait,
                Clear = entry.Clear
            });
        }
    }
}
