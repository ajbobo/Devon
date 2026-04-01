# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Devon is a C# text adventure game with:
- **Devon**: .NET 8 console application (the game)
- **Devon.Editor**: WPF editor application for editing room JSON
- **Devon.Tests**: xUnit test project

## Common Commands

**Build solutions:**
```bash
dotnet build Devon.sln
```

**Run the game:**
```bash
dotnet run --project Devon
```

**Run tests:**
```bash
dotnet test
```

**Run a single test:**
```bash
dotnet test --filter "FullyQualifiedName~TestName"
```

**Edit room data:**
- Run the Editor: `dotnet run --project Editor`
- Or edit `rooms.json` directly

## High-Level Architecture

### Data Model (`Models/`)
- **Room**: Represents a game location with description, items, conditions, and actions
- **RoomAction** (abstract): Base for ExitAction, TakeAction, UseAction, TalkAction
- **Player**: Inventory (HashSet) and Conditions (HashSet)
- **GameState**: CurrentRoom, Player, and Rooms dictionary

### Services (`Services/`)
- **Game**: Main controller; runs game loop, processes actions
- **MenuRenderer**: Displays room description, builds action menu, handles user selection
- **ActionInvoker**: Executes Take/Use/Talk/Inventory actions
- **ActionExecutor**: Parses and executes command strings (e.g., `Inventory.add(item);Room.addCondition(cond)`)
- **ConditionEvaluator**: Evaluates condition expressions (e.g., `AND(Player.hasItem(key), Room.hasCondition(door_open))`)
- **JsonRoomLoader**: Loads rooms from embedded `rooms.json` resource

### JSON Room Format (`rooms.json`)
Rooms are defined with:
- `name`: Room identifier
- `description`: Array of `{text, condition}` entries shown when entering
- `conditions`: Room conditions initially set
- `actions`: Dictionary keyed by direction (`north`/`south`/`east`/`west`/`up`/`down`) or action type (`take`/`use`/`talk`)
  - Exit actions: `{room, result_text, action, condition}`
  - Take actions: `{item, result_text, action, condition}` — defines an item
  - Use actions: `{item, target, result_text, action, condition}`
  - Talk actions: `{target, says, result_text, action, condition}`

**Important**: Items are NOT separately listed; they are auto-derived from `take` actions during loading.

### Editor (`Editor/`)
WPF MVVM application for visually editing `rooms.json`. The Items section was removed — items come from take actions only.

### Key Design Points

- **Case-insensitive**: Collections use `StringComparer.OrdinalIgnoreCase` (Inventory, Conditions, Room.Items, etc.)
- **Condition evaluation**: Functions like `Player.hasItem(item)`, `Player.hasCondition(cond)`, `Room.hasCondition(cond)`, `Room.hasItem(item)` used in JSON condition strings
- **Action menu**: Built dynamically based on available exits, take/use/talk actions, and player state
- **Take validation**: A take action only appears in the menu if the referenced item is in `room.Items`
- **Item display**: Items are NOT listed separately; they are shown through conditional description entries (e.g., `"<blue sword>"` with condition `!Player.hasItem(sword)`)

### Testing
Tests use xUnit. Primary coverage is in `Tests/ConditionEvaluatorTests.cs`. Run all tests with `dotnet test`.

## Notes for Modifications

- When adding new room items, define a `take` action; the item name is automatically added to `room.Items` at load time.
- When changing condition evaluation logic, update both `ConditionEvaluator.cs` and its tests.
- The Editor and Game share the same Models and Services; keep them in sync.

## Rules for Github

- Do NOT commit or push to Github automatically
- You will be told when to commit and push