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
    private ObservableCollection<Cutscene> _cutscenes = new();

    [ObservableProperty]
    private Cutscene? _selectedCutscene;

    [ObservableProperty]
    private RoomEditorViewModel? _roomEditor;

    [ObservableProperty]
    private CutsceneEditorViewModel? _cutsceneEditor;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    partial void OnSelectedRoomChanged(Room? value)
    {
        if (value != null)
        {
            var editor = new RoomEditorViewModel();
            editor.LoadFromRoom(value);
            RoomEditor = editor;
        }
        else
        {
            RoomEditor = null;
        }
    }

    partial void OnSelectedCutsceneChanged(Cutscene? value)
    {
        if (value != null)
        {
            var editor = new CutsceneEditorViewModel();
            editor.LoadFromCutscene(value);
            CutsceneEditor = editor;
        }
        else
        {
            CutsceneEditor = null;
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
            RoomEditor = null;
            StatusMessage = $"Deleted room: {roomToDelete.Name}";
        }
    }

    [RelayCommand]
    private void NewCutscene()
    {
        var newCutscene = new Cutscene { Name = "New Cutscene" };
        Cutscenes.Add(newCutscene);
        SelectedCutscene = newCutscene;
        StatusMessage = "Created new cutscene";
    }

    [RelayCommand]
    private void DeleteCutscene()
    {
        if (SelectedCutscene != null)
        {
            var cutsceneToDelete = SelectedCutscene;
            Cutscenes.Remove(cutsceneToDelete);
            SelectedCutscene = null;
            CutsceneEditor = null;
            StatusMessage = $"Deleted cutscene: {cutsceneToDelete.Name}";
        }
    }

    [RelayCommand]
    private void AddCutsceneText()
    {
        if (CutsceneEditor != null)
        {
            CutsceneEditor.TextLines.Add(new CutsceneTextEntry { Text = "New line", Color = null, Wait = false, Clear = false });
            StatusMessage = "Added text line";
        }
    }

    [RelayCommand]
    private void RemoveCutsceneText()
    {
        if (CutsceneEditor != null && CutsceneEditor.SelectedTextLine != null)
        {
            var line = CutsceneEditor.SelectedTextLine;
            CutsceneEditor.TextLines.Remove(line);
            CutsceneEditor.SelectedTextLine = null;
            StatusMessage = "Removed text line";
        }
    }

    [RelayCommand]
    private void AddDescription()
    {
        if (RoomEditor != null)
        {
            RoomEditor.Descriptions.Add(new RoomDescriptionEntry { Text = "New description", Condition = "" });
            StatusMessage = "Added description";
        }
    }

    [RelayCommand]
    private void RemoveDescription()
    {
        if (RoomEditor != null && RoomEditor.SelectedDescription != null)
        {
            var desc = RoomEditor.SelectedDescription;
            RoomEditor.Descriptions.Remove(desc);
            RoomEditor.SelectedDescription = null;
            StatusMessage = "Removed description";
        }
    }

    [RelayCommand]
    private void AddCondition()
    {
        if (RoomEditor != null)
        {
            RoomEditor.Conditions.Add(new ConditionEntry { Name = "new_condition" });
            StatusMessage = "Added condition";
        }
    }

    [RelayCommand]
    private void RemoveCondition()
    {
        if (RoomEditor != null && RoomEditor.SelectedCondition != null)
        {
            var cond = RoomEditor.SelectedCondition;
            RoomEditor.Conditions.Remove(cond);
            RoomEditor.SelectedCondition = null;
            StatusMessage = "Removed condition";
        }
    }

    [RelayCommand]
    private void AddAction()
    {
        if (RoomEditor != null)
        {
            var action = new RoomActionEntry
            {
                Key = "north",
                Type = RoomActionEntryType.Exit,
                TargetRoom = "",
                ResultText = null,
                ActionCommands = null,
                Condition = null
            };
            RoomEditor.Actions.Add(action);
            RoomEditor.SelectedAction = action;
            StatusMessage = "Added action";
        }
    }

    [RelayCommand]
    private void RemoveAction()
    {
        if (RoomEditor != null && RoomEditor.SelectedAction != null)
        {
            var action = RoomEditor.SelectedAction;
            RoomEditor.Actions.Remove(action);
            RoomEditor.SelectedAction = null;
            StatusMessage = "Removed action";
        }
    }

    [RelayCommand]
    private void SaveChanges()
    {
        if (RoomEditor != null && SelectedRoom != null)
        {
            RoomEditor.ApplyToRoom(SelectedRoom);
            StatusMessage = $"Saved changes to: {SelectedRoom.Name}";
        }
        else if (CutsceneEditor != null && SelectedCutscene != null)
        {
            CutsceneEditor.ApplyToCutscene(SelectedCutscene);
            StatusMessage = $"Saved changes to cutscene: {SelectedCutscene.Name}";
        }
    }

    [RelayCommand]
    private async Task SaveAllAsync()
    {
        try
        {
            var data = new
            {
                rooms = Rooms.Select(r => ConvertRoomToJson(r)).ToList(),
                cutscenes = Cutscenes.Select(c => ConvertCutsceneToJson(c)).ToList()
            };
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(data, options);
            await File.WriteAllTextAsync(_roomsJsonPath, json);
            StatusMessage = $"Saved {Rooms.Count} rooms and {Cutscenes.Count} cutscenes to {_roomsJsonPath}";
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
            Cutscenes.Clear();

            if (doc.RootElement.TryGetProperty("rooms", out JsonElement roomsElement))
            {
                foreach (var roomElem in roomsElement.EnumerateArray())
                {
                    var room = ParseRoom(roomElem);
                    Rooms.Add(room);
                }
            }

            if (doc.RootElement.TryGetProperty("cutscenes", out JsonElement cutscenesElement))
            {
                foreach (var cutsceneElem in cutscenesElement.EnumerateArray())
                {
                    var cutscene = ParseCutscene(cutsceneElem);
                    Cutscenes.Add(cutscene);
                }
            }

            StatusMessage = $"Loaded {Rooms.Count} rooms and {Cutscenes.Count} cutscenes from {_roomsJsonPath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading: {ex.Message}";
        }
    }

    private Cutscene ParseCutscene(JsonElement cutsceneElem)
    {
        var cutscene = new Cutscene
        {
            Name = cutsceneElem.GetProperty("name").GetString() ?? "Unnamed Cutscene"
        };

        if (cutsceneElem.TryGetProperty("text", out JsonElement textElem) && textElem.ValueKind == JsonValueKind.Array)
        {
            foreach (var textItem in textElem.EnumerateArray())
            {
                cutscene.Text.Add(new CutsceneText
                {
                    Text = textItem.GetProperty("text").GetString() ?? "",
                    Color = textItem.TryGetProperty("color", out JsonElement colorElem) ? colorElem.GetString() : null,
                    Wait = textItem.TryGetProperty("wait", out JsonElement waitElem) && waitElem.ValueKind == JsonValueKind.True,
                    Clear = textItem.TryGetProperty("clear", out JsonElement clearElem) && clearElem.ValueKind == JsonValueKind.True
                });
            }
        }

        return cutscene;
    }

    private object ConvertCutsceneToJson(Cutscene cutscene)
    {
        return new
        {
            name = cutscene.Name,
            text = cutscene.Text.Select(t => new
            {
                text = t.Text,
                color = t.Color,
                wait = t.Wait,
                clear = t.Clear
            }).ToList()
        };
    }

    // Existing ParseRoom and ConvertRoomToJson methods remain unchanged
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

        if (roomElem.TryGetProperty("onEntry", out JsonElement onEntryElem) && onEntryElem.ValueKind == JsonValueKind.Object)
        {
            if (onEntryElem.TryGetProperty("action", out JsonElement actionElem))
            {
                room.OnEntry = actionElem.GetString();
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
            "north" or "south" or "east" or "west" or "up" or "down" or "left" or "center" or "right" => new ExitAction
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
            onEntry = room.OnEntry != null ? new { action = room.OnEntry } : null,
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
