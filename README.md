# Devon

Devon is a C# text adventure game with an integrated room editor. The game is built on a data-driven architecture where rooms, items, and interactions are defined in JSON, while a WPF editor provides a visual interface for creating and modifying game content.

## Features

- **Text-based adventure gameplay** with menu-driven interaction
- **Room-based exploration** with directional exits (north, south, east, west, up, down)
- **Inventory system** for collecting and using items
- **Condition system** for tracking player and room states
- **Visual Editor** for designing rooms without hand-editing JSON
- **Extensible action system** supporting take, use, talk, and exit actions

## Project Structure

```
Devon/
├── Devon/              # Main console game project
├── Editor/             # WPF editor application
├── Tests/              # xUnit tests
├── Models/             # Shared data models
├── Services/           # Game logic and JSON loading
├── rooms.json          # Room definitions (embedded in game)
└── CLAUDE.md           # Claude Code assistant guidance
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Building

```bash
# Clone the repository (if not already done)
git clone <repository-url>
cd Devon

# Build the entire solution
dotnet build Devon.sln

# Or build individual projects
dotnet build Devon/Devon.csproj
dotnet build Editor/Devon.Editor.csproj
```

## Running

### Console Game

```bash
dotnet run --project Devon
```

The game will load rooms from `rooms.json` (embedded resource) and start in the first room or a room named "Entrance" if present.

### Editor

```bash
dotnet run --project Editor
```

The editor allows you to:
- Create, edit, and delete rooms
- Add/remove room descriptions with conditions
- Define actions (exits, take, use, talk)
- Save all rooms to `rooms.json`

**Note:** The Editor no longer has a separate Items section. Items are defined implicitly through "take" actions.

## Room JSON Format

Rooms are defined in `rooms.json` with the following structure:

```json
{
  "rooms": [
    {
      "name": "Room Name",
      "description": [
        { "text": "You see a dark cave.", "condition": null },
        { "text": "<glowing sword>", "condition": "!Player.hasItem(sword)" }
      ],
      "conditions": ["room_condition_name"],
      "actions": {
        "north": {
          "room": "Next Room",
          "result_text": "You walk north.",
          "action": "Room.addCondition(entered_north)",
          "condition": "Room.hasCondition(gate_unlocked)"
        },
        "take": {
          "item": "Sword",
          "result_text": "You pick up the sword.",
          "action": null,
          "condition": null
        },
        "use": {
          "item": "key",
          "target": "door",
          "result_text": "You unlock the door.",
          "action": "Room.addCondition(door_open)"
        },
        "talk": {
          "target": "guard",
          "says": "hello",
          "result_text": "The guard nods.",
          "action": "Player.addCondition(guard_friendly)"
        }
      }
    }
  ]
}
```

### Key Points

- **Items**: Not separately listed. They are derived from `take` actions. The item name in the `take` action automatically appears in `room.Items` when the JSON is loaded.
- **Conditions**: Used to control when descriptions, actions, or menu options appear.
- **Action commands**: Semicolon-separated list of commands executed as side effects.
- **String comparisons**: Case-insensitive throughout the game.

## Action Commands

The `action` field in JSON supports these commands:

- `Inventory.add(item)`
- `Inventory.remove(item)`
- `Player.addCondition(cond)`
- `Player.removeCondition(cond)`
- `Room.addCondition(cond)`
- `Room.removeCondition(cond)`

Multiple commands can be chained with semicolons:
```
Inventory.remove(rock);Inventory.add(ring);Room.addCondition(transformed)
```

## Condition Functions

Condition expressions support these functions:

- `Player.hasItem(item)` - Player has the specified item
- `Player.hasCondition(cond)` - Player has the specified condition
- `Room.hasCondition(cond)` - Current room has the specified condition
- `Room.hasItem(item)` - Current room contains the specified item

Conditions can be combined using `AND()`, `OR()`, `NOT()`, or negated with `!`:
```
!Player.hasItem(sword)
AND(Player.hasItem(key), Room.hasCondition(door_unlocked))
```

The `NOT()` combinator is also available:
```
NOT(Player.hasItem(sword))
```

## Gameplay

When you enter a room:
1. The room description is displayed (conditional lines are evaluated)
2. A menu of available actions is shown
3. You select an action with arrow keys + Enter

The menu dynamically includes:
- Direction exits that are defined and meet their conditions
- "Take" if a take action exists, is available, and the item is in the room
- "Use" if the player has any items in inventory
- "Talk" if a talk action exists and its condition is met
- "Inventory" to view carried items

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage (if coverlet is configured)
dotnet test --collect:"XPlat Code Coverage"
```

Tests are located in `Tests/` and use xUnit.

## Architecture

### Models
- `Room`, `RoomDescription`, `RoomAction` (and subclasses: ExitAction, TakeAction, UseAction, TalkAction)
- `Player`
- `GameState`

### Services
- `Game`: Main loop and action coordination
- `MenuRenderer`: Room display and menu handling
- `ActionInvoker`: Executes user-selected actions
- `ActionExecutor`: Parses and runs command strings
- `ConditionEvaluator`: Evaluates condition expressions using a custom parser
- `JsonRoomLoader`: Deserializes rooms from embedded JSON

### Design Patterns
- MVVM in the Editor (CommunityToolkit.Mvvm)
- Strategy pattern for room actions
- Observer-like condition evaluation

## Development Notes

- Items are **never** separately listed in JSON or the editor; they come from `take` actions only.
- All string comparisons are case-insensitive (collections use `StringComparer.OrdinalIgnoreCase`).
- Room items are validated during "take" to ensure the item is actually present.
- The editor's "items" section was removed to enforce this single-source-of-truth design.

## License

(Add license information here if applicable)
