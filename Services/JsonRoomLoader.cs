using Devon.Models;
using System.Reflection;
using System.Text.Json;

namespace Devon.Services;

/// <summary>
/// Loads rooms from an embedded JSON resource
/// </summary>
public class JsonRoomLoader : IJsonRoomLoader
{
    private readonly string _resourceName;
    private readonly IConditionEvaluator _conditionEvaluator;

    public JsonRoomLoader(string? resourceName = null, IConditionEvaluator? conditionEvaluator = null)
    {
        // Default resource name: Devon.rooms.json (assuming root namespace Devon)
        _resourceName = resourceName ?? "Devon.rooms.json";
        _conditionEvaluator = conditionEvaluator ?? new ConditionEvaluator();
    }

    public async Task<IReadOnlyDictionary<string, Room>> LoadRoomsAsync()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();

        var fullResourceName = resourceNames.FirstOrDefault(r => r.EndsWith("rooms.json", StringComparison.OrdinalIgnoreCase));
        if (fullResourceName == null)
            throw new FileNotFoundException($"Embedded resource 'rooms.json' not found. Available: {string.Join(", ", resourceNames)}");

        using var stream = assembly.GetManifestResourceStream(fullResourceName) ?? throw new InvalidOperationException($"Could not open resource stream for {fullResourceName}");

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var doc = await JsonSerializer.DeserializeAsync<JsonDocument>(stream);
        var roomsDict = new Dictionary<string, Room>(StringComparer.OrdinalIgnoreCase);

        if (!doc.RootElement.TryGetProperty("rooms", out JsonElement roomsElement))
            throw new InvalidOperationException("JSON missing 'rooms' array");

        foreach (var roomElem in roomsElement.EnumerateArray())
        {
            var room = ParseRoom(roomElem);
            roomsDict[room.Name] = room;
        }

        return roomsDict;
    }

    private Room ParseRoom(JsonElement roomElem)
    {
        var room = new Room
        {
            Name = roomElem.GetProperty("name").GetString() ?? throw new InvalidOperationException("Room missing name")
        };

        // Description array
        if (roomElem.TryGetProperty("description", out JsonElement descElem) && descElem.ValueKind == JsonValueKind.Array)
        {
            foreach (var descItem in descElem.EnumerateArray())
            {
                var rd = new RoomDescription
                {
                    Text = descItem.GetProperty("text").GetString() ?? "",
                    Condition = descItem.TryGetProperty("condition", out JsonElement condElem) ? condElem.GetString() : null
                };
                room.Description.Add(rd);
            }
        }

        // Items array (optional)
        if (roomElem.TryGetProperty("items", out JsonElement itemsElem) && itemsElem.ValueKind == JsonValueKind.Array)
        {
            foreach (var itemElem in itemsElem.EnumerateArray())
            {
                room.Items.Add(itemElem.GetString() ?? "");
            }
        }

        // Actions dictionary
        if (roomElem.TryGetProperty("actions", out JsonElement actionsElem) && actionsElem.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in actionsElem.EnumerateObject())
            {
                var actionKey = prop.Name; // e.g., "north", "take", "use", "talk"
                var actionDto = ParseActionDto(prop.Value);
                var roomAction = ConvertToRoomAction(actionKey, actionDto);
                room.Actions[actionKey] = roomAction;
            }
        }

        // Room initial conditions (will be applied after first description)
        if (roomElem.TryGetProperty("conditions", out JsonElement condsElem) && condsElem.ValueKind == JsonValueKind.Array)
        {
            foreach (var condElem in condsElem.EnumerateArray())
            {
                room.InitialConditions.Add(condElem.GetString() ?? "");
            }
        }

        return room;
    }

    private record ActionDto(
        string? Room,
        string? Item,
        string? Target,
        string? Says,
        string? ResultText,
        string? ActionCommands,
        string? Condition
    );

    private ActionDto ParseActionDto(JsonElement elem)
    {
        string? room = null;
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
                switch (prop.Name.ToLowerInvariant())
                {
                    case "room":
                        room = prop.Value.GetString();
                        break;
                    case "item":
                        item = prop.Value.GetString();
                        break;
                    case "target":
                        target = prop.Value.GetString();
                        break;
                    case "says":
                        says = prop.Value.GetString();
                        break;
                    case "result_text":
                        resultText = prop.Value.GetString();
                        break;
                    case "action":
                        actionCommands = prop.Value.GetString();
                        break;
                    case "condition":
                        condition = prop.Value.GetString();
                        break;
                }
            }
        }
        else if (elem.ValueKind == JsonValueKind.String)
        {
            // Simple string value means just room name (e.g., "east": "RoomName")
            room = elem.GetString();
        }

        return new ActionDto(room, item, target, says, resultText, actionCommands, condition);
    }

    private RoomAction ConvertToRoomAction(string key, ActionDto dto)
    {
        RoomAction action;

        // Determine action type based on key
        if (key.Equals("north", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("south", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("east", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("west", StringComparison.OrdinalIgnoreCase))
        {
            // Exit action
            action = new ExitAction
            {
                TargetRoom = dto.Room ?? throw new InvalidOperationException($"Exit action '{key}' missing 'room' field"),
                ResultText = dto.ResultText,
                ActionCommands = dto.ActionCommands,
                Condition = dto.Condition
            };
        }
        else if (key.Equals("take", StringComparison.OrdinalIgnoreCase))
        {
            // Take action
            action = new TakeAction
            {
                Item = dto.Item ?? throw new InvalidOperationException("Take action missing 'item'"),
                ResultText = dto.ResultText,
                ActionCommands = dto.ActionCommands,
                Condition = dto.Condition
            };
        }
        else if (key.Equals("use", StringComparison.OrdinalIgnoreCase))
        {
            // Use action
            action = new UseAction
            {
                Item = dto.Item ?? throw new InvalidOperationException("Use action missing 'item'"),
                Target = dto.Target ?? throw new InvalidOperationException("Use action missing 'target'"),
                ResultText = dto.ResultText,
                ActionCommands = dto.ActionCommands,
                Condition = dto.Condition
            };
        }
        else if (key.Equals("talk", StringComparison.OrdinalIgnoreCase))
        {
            // Talk action
            action = new TalkAction
            {
                Target = dto.Target ?? throw new InvalidOperationException("Talk action missing 'target'"),
                Says = dto.Says ?? throw new InvalidOperationException("Talk action missing 'says'"),
                ResultText = dto.ResultText,
                ActionCommands = dto.ActionCommands,
                Condition = dto.Condition
            };
        }
        else
        {
            throw new InvalidOperationException($"Unknown action key: {key}");
        }

        return action;
    }
}
