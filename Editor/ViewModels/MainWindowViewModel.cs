using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Devon.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace Devon.Editor.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private string _roomsJsonPath = "rooms.json";

    [ObservableProperty]
    private ObservableCollection<Room> _rooms = new();

    [ObservableProperty]
    private Room? _selectedRoom;

    [ObservableProperty]
    private RoomEditorViewModel? _editor;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    partial void OnSelectedRoomChanged(Room? value)
    {
        if (value != null)
        {
            Editor = new RoomEditorViewModel();
            Editor.LoadFromRoom(value);
        }
        else
        {
            Editor = null;
        }
    }

    [RelayCommand]
    private void NewRoom()
    {
        var newRoom = new Room { Name = "New Room" };
        Rooms.Add(newRoom);
        SelectedRoom = newRoom;
        StatusMessage = "Created new room";
    }

    [RelayCommand]
    private void DeleteRoom()
    {
        if (SelectedRoom != null)
        {
            var roomToDelete = SelectedRoom;
            Rooms.Remove(roomToDelete);
            SelectedRoom = null;
            Editor = null;
            StatusMessage = $"Deleted room: {roomToDelete.Name}";
        }
    }

    [RelayCommand]
    private void SaveChanges()
    {
        if (Editor != null && SelectedRoom != null)
        {
            Editor.ApplyToRoom(SelectedRoom);
            StatusMessage = $"Saved changes to: {SelectedRoom.Name}";
        }
    }

    [RelayCommand]
    private async Task SaveAllAsync()
    {
        try
        {
            var data = new { rooms = Rooms.Select(r => ConvertRoomToJson(r)).ToList() };
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(data, options);
            await File.WriteAllTextAsync(_roomsJsonPath, json);
            StatusMessage = $"Saved {Rooms.Count} rooms to {_roomsJsonPath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
        }
    }

    [RelayCommand]
    private void LoadRooms()
    {
        try
        {
            if (!File.Exists(_roomsJsonPath))
            {
                StatusMessage = $"File not found: {_roomsJsonPath}";
                return;
            }

            var json = File.ReadAllText(_roomsJsonPath);
            var doc = JsonDocument.Parse(json);

            Rooms.Clear();

            if (doc.RootElement.TryGetProperty("rooms", out JsonElement roomsElement))
            {
                foreach (var roomElem in roomsElement.EnumerateArray())
                {
                    var room = ParseRoom(roomElem);
                    Rooms.Add(room);
                }
            }

            StatusMessage = $"Loaded {Rooms.Count} rooms from {_roomsJsonPath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading: {ex.Message}";
        }
    }

    // Collection modification commands that operate on the Editor
    [RelayCommand]
    private void AddDescription()
    {
        if (Editor != null)
        {
            Editor.Descriptions.Add(new RoomDescriptionEntry { Text = "New description", Condition = "" });
            StatusMessage = "Added description";
        }
    }

    [RelayCommand]
    private void AddCondition()
    {
        if (Editor != null)
        {
            Editor.Conditions.Add(new ConditionEntry { Name = "new condition" });
            StatusMessage = "Added condition";
        }
    }

    [RelayCommand]
    private void AddAction()
    {
        if (Editor != null)
        {
            var action = new RoomActionEntry
            {
                Key = "north",
                Type = RoomActionEntryType.Exit,
                TargetRoom = "Target Room Name"
            };
            Editor.Actions.Add(action);
            Editor.SelectedAction = action;
            StatusMessage = "Added action";
        }
    }

    [RelayCommand]
    private void RemoveAction()
    {
        if (Editor != null && Editor.SelectedAction != null)
        {
            var action = Editor.SelectedAction;
            Editor.Actions.Remove(action);
            Editor.SelectedAction = null;
            StatusMessage = $"Removed action: {action.Key}";
        }
    }

    [RelayCommand]
    private void RemoveDescription()
    {
        if (Editor != null && Editor.SelectedDescription != null)
        {
            var desc = Editor.SelectedDescription;
            Editor.Descriptions.Remove(desc);
            Editor.SelectedDescription = null;
            StatusMessage = "Removed description";
        }
    }

    [RelayCommand]
    private void RemoveCondition()
    {
        if (Editor != null && Editor.SelectedCondition != null)
        {
            var cond = Editor.SelectedCondition;
            Editor.Conditions.Remove(cond);
            Editor.SelectedCondition = null;
            StatusMessage = $"Removed condition: {cond.Name}";
        }
    }

    private Room ParseRoom(JsonElement roomElem)
    {
        var room = new Room
        {
            Name = roomElem.GetProperty("name").GetString() ?? "Unnamed"
        };

        if (roomElem.TryGetProperty("description", out JsonElement descElem) && descElem.ValueKind == JsonValueKind.Array)
        {
            foreach (var descItem in descElem.EnumerateArray())
            {
                room.Description.Add(new RoomDescription
                {
                    Text = descItem.GetProperty("text").GetString() ?? "",
                    Condition = descItem.TryGetProperty("condition", out JsonElement cond) ? cond.GetString() : null
                });
            }
        }

        if (roomElem.TryGetProperty("conditions", out JsonElement condsElem) && condsElem.ValueKind == JsonValueKind.Array)
        {
            foreach (var cond in condsElem.EnumerateArray())
            {
                room.Conditions.Add(cond.GetString() ?? "");
            }
        }

        if (roomElem.TryGetProperty("actions", out JsonElement actionsElem) && actionsElem.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in actionsElem.EnumerateObject())
            {
                var action = ParseAction(prop.Name, prop.Value);
                room.Actions[prop.Name] = action;
            }
        }

        return room;
    }

    private RoomAction ParseAction(string key, JsonElement elem)
    {
        string? targetRoom = null;
        string? item = null;
        string? target = null;
        string? says = null;
        string? resultText = null;
        string? actionCommands = null;
        string? condition = null;

        if (elem.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in elem.EnumerateObject())
            {
                var name = prop.Name.ToLowerInvariant();
                var value = prop.Value.GetString();
                switch (name)
                {
                    case "room":
                        targetRoom = value;
                        break;
                    case "item":
                        item = value;
                        break;
                    case "target":
                        target = value;
                        break;
                    case "says":
                        says = value;
                        break;
                    case "result_text":
                        resultText = value;
                        break;
                    case "action":
                        actionCommands = value;
                        break;
                    case "condition":
                        condition = value;
                        break;
                }
            }
        }
        else if (elem.ValueKind == JsonValueKind.String)
        {
            targetRoom = elem.GetString();
        }

        return key.ToLowerInvariant() switch
        {
            "north" or "south" or "east" or "west" or "up" or "down" => new ExitAction
            {
                TargetRoom = targetRoom ?? "",
                ResultText = resultText,
                ActionCommands = actionCommands,
                Condition = condition
            },
            "take" => new TakeAction
            {
                Item = item ?? "",
                ResultText = resultText,
                ActionCommands = actionCommands,
                Condition = condition
            },
            "use" => new UseAction
            {
                Item = item ?? "",
                Target = target ?? "",
                ResultText = resultText,
                ActionCommands = actionCommands,
                Condition = condition
            },
            "talk" => new TalkAction
            {
                Target = target ?? "",
                Says = says ?? "",
                ResultText = resultText,
                ActionCommands = actionCommands,
                Condition = condition
            },
            _ => throw new NotSupportedException($"Unknown action type: {key}")
        };
    }

    private object ConvertRoomToJson(Room room)
    {
        return new
        {
            name = room.Name,
            description = room.Description.Select(d => new
            {
                text = d.Text,
                condition = d.Condition
            }).ToList(),
            conditions = room.Conditions.ToList(),
            actions = room.Actions.ToDictionary(kvp => kvp.Key, kvp => ConvertActionToJson(kvp.Value))
        };
    }

    private object ConvertActionToJson(RoomAction action)
    {
        return action switch
        {
            ExitAction exit => new
            {
                room = exit.TargetRoom,
                result_text = exit.ResultText,
                action = exit.ActionCommands,
                condition = exit.Condition
            },
            TakeAction take => new
            {
                item = take.Item,
                result_text = take.ResultText,
                action = take.ActionCommands,
                condition = take.Condition
            },
            UseAction use => new
            {
                item = use.Item,
                target = use.Target,
                result_text = use.ResultText,
                action = use.ActionCommands,
                condition = use.Condition
            },
            TalkAction talk => new
            {
                target = talk.Target,
                says = talk.Says,
                result_text = talk.ResultText,
                action = talk.ActionCommands,
                condition = talk.Condition
            },
            _ => new { }
        };
    }
}
