using CommunityToolkit.Mvvm.ComponentModel;
using Devon.Models;
using System.Collections.ObjectModel;

namespace Devon.Editor.ViewModels;

/// <summary>
/// ViewModel for editing a single room
/// </summary>
public partial class RoomEditorViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private ObservableCollection<RoomDescriptionEntry> _descriptions = new();

    [ObservableProperty]
    private RoomDescriptionEntry? _selectedDescription;

    [ObservableProperty]
    private ObservableCollection<ItemEntry> _items = new();

    [ObservableProperty]
    private ItemEntry? _selectedItem;

    [ObservableProperty]
    private ObservableCollection<ConditionEntry> _conditions = new();

    [ObservableProperty]
    private ConditionEntry? _selectedCondition;

    [ObservableProperty]
    private ObservableCollection<RoomActionEntry> _actions = new();

    [ObservableProperty]
    private RoomActionEntry? _selectedAction;

    public void LoadFromRoom(Room room)
    {
        Name = room.Name;
        Descriptions.Clear();
        foreach (var d in room.Description)
        {
            Descriptions.Add(new RoomDescriptionEntry
            {
                Text = d.Text,
                Condition = d.Condition ?? string.Empty
            });
        }
        Items.Clear();
        foreach (var item in room.Items)
            Items.Add(new ItemEntry { Name = item });
        Conditions.Clear();
        foreach (var cond in room.Conditions)
            Conditions.Add(new ConditionEntry { Name = cond });
        Actions.Clear();
        foreach (var kvp in room.Actions)
        {
            Actions.Add(CreateRoomActionEntry(kvp.Key, kvp.Value));
        }
    }

    public void ApplyToRoom(Room room)
    {
        room.Name = Name;
        room.Description.Clear();
        foreach (var d in Descriptions)
        {
            room.Description.Add(new RoomDescription
            {
                Text = d.Text,
                Condition = string.IsNullOrWhiteSpace(d.Condition) ? null : d.Condition
            });
        }
        room.Items.Clear();
        foreach (var item in Items)
            room.Items.Add(item.Name);
        room.Conditions.Clear();
        foreach (var cond in Conditions)
            room.Conditions.Add(cond.Name);
        room.Actions.Clear();
        foreach (var actionEditor in Actions)
        {
            var action = ConvertToRoomAction(actionEditor);
            room.Actions[actionEditor.Key] = action;
        }
    }

    private RoomActionEntry CreateRoomActionEntry(string key, RoomAction action)
    {
        return action switch
        {
            ExitAction exit => new RoomActionEntry
            {
                Key = key,
                Type = RoomActionEntryType.Exit,
                TargetRoom = exit.TargetRoom,
                ResultText = exit.ResultText,
                ActionCommands = exit.ActionCommands,
                Condition = exit.Condition
            },
            TakeAction take => new RoomActionEntry
            {
                Key = key,
                Type = RoomActionEntryType.Take,
                Item = take.Item,
                ResultText = take.ResultText,
                ActionCommands = take.ActionCommands,
                Condition = take.Condition
            },
            UseAction use => new RoomActionEntry
            {
                Key = key,
                Type = RoomActionEntryType.Use,
                Item = use.Item,
                Target = use.Target,
                ResultText = use.ResultText,
                ActionCommands = use.ActionCommands,
                Condition = use.Condition
            },
            TalkAction talk => new RoomActionEntry
            {
                Key = key,
                Type = RoomActionEntryType.Talk,
                Target = talk.Target,
                Says = talk.Says,
                ResultText = talk.ResultText,
                ActionCommands = talk.ActionCommands,
                Condition = talk.Condition
            },
            _ => throw new NotSupportedException($"Unknown action type: {action.GetType()}")
        };
    }

    private RoomAction ConvertToRoomAction(RoomActionEntry editor)
    {
        return editor.Type switch
        {
            RoomActionEntryType.Exit => new ExitAction
            {
                TargetRoom = editor.TargetRoom ?? "",
                ResultText = editor.ResultText,
                ActionCommands = editor.ActionCommands,
                Condition = editor.Condition
            },
            RoomActionEntryType.Take => new TakeAction
            {
                Item = editor.Item ?? "",
                ResultText = editor.ResultText,
                ActionCommands = editor.ActionCommands,
                Condition = editor.Condition
            },
            RoomActionEntryType.Use => new UseAction
            {
                Item = editor.Item ?? "",
                Target = editor.Target ?? "",
                ResultText = editor.ResultText,
                ActionCommands = editor.ActionCommands,
                Condition = editor.Condition
            },
            RoomActionEntryType.Talk => new TalkAction
            {
                Target = editor.Target ?? "",
                Says = editor.Says ?? "",
                ResultText = editor.ResultText,
                ActionCommands = editor.ActionCommands,
                Condition = editor.Condition
            },
            _ => throw new NotSupportedException($"Unknown action editor type: {editor.Type}")
        };
    }
}

public partial class RoomDescriptionEntry : ObservableObject
{
    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private string _condition = string.Empty;
}

public partial class ItemEntry : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;
}

public partial class ConditionEntry : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;
}

public enum RoomActionEntryType
{
    Exit,
    Take,
    Use,
    Talk
}

public partial class RoomActionEntry : ObservableObject
{
    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private RoomActionEntryType _type;

    // Exit action
    [ObservableProperty]
    private string? _targetRoom;

    // Take action
    [ObservableProperty]
    private string? _item;

    // Use action
    [ObservableProperty]
    private string? _target;

    // Talk action
    [ObservableProperty]
    private string? _says;

    // Common fields
    [ObservableProperty]
    private string? _resultText;

    [ObservableProperty]
    private string? _actionCommands;

    [ObservableProperty]
    private string? _condition;

    // Collection of valid action keys for dropdown binding
    public ObservableCollection<string> ValidKeys { get; } = new()
    {
        "north", "south", "east", "west",
        "take", "use", "talk"
    };

    partial void OnKeyChanged(string value)
    {
        // Auto-update Type based on the selected action key
        Type = value switch
        {
            "north" or "south" or "east" or "west" => RoomActionEntryType.Exit,
            "take" => RoomActionEntryType.Take,
            "use" => RoomActionEntryType.Use,
            "talk" => RoomActionEntryType.Talk,
            _ => RoomActionEntryType.Exit
        };
    }
}
