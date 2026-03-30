# Devon

## Overview
Devon is a C# console application that is a text adventure game. The user is shown text that describes what they see in a room. They can then choose from
several options to tell the game what they want to do. 

## Options
In each room, the user will have the following options
- Look - Look at the room - Provides a description of the current room
- Take - Pick up an item in the room - Prompts the user for the item to pick up
- Use - Use an item from the player's inventory - Prompts the user for the item and then what to use it on
- Talk - Talk to something in the room - Prompts the user for what they wish to talk to and then what they want to say
- Inventory - Show the user a list of the items that are currently in their inventory
- North - Go north - Only available if the room is set to have a north exit
- South - Go south - Only available if the room is set to have a south exit
- East - Go east - Only available if the room is set to have an east exit
- West - Go west - Only available if the room is set to have a west exit

## Rooms
Rooms are defined as entries in a JSON file:
```json
{
    "rooms": [
        {
            "name": "<Room's Name>",
            "description": [ <-- Text is shown when the user chooses Look
                {
                    "text": "<The initial description of the room>",
                    "condition": "<check against the Room and/or player condition>" <-- optional
                }
            ],
            "onEntry": {
                "action": "<List of actions that occur the FIRST time a player enters a room>" <-- No result_text or conditions needed
            }
            "actions": {
                "north": {
                    "room": "<the name of the room to the north>",
                    "action": "<a list of actions that occur when the user goes this direction>",
                },
                "south": {
                    "room": "<the name of the room to the south>", <-- May not have an action
                },
                "east": "<the name of the room to the east, or null>", <-- Can be just the name of the next room
                "west": null,   <-- Indicates that the player can't go that direction
                "take": {
                    "item": "<the name of an item that can be taken>",
                    "result_text": "<text that is displayed to the user after the item is picked up>" <-- optional
                    <-- Take doesn't need an action field because it is always inventory.add(<item>)
                },
                "use": {
                    "item": "<the name of an item that can be used in the room>",
                    "target": "<the name of the valid target for the item>",
                    "result_text": "<the text that is displayed after the item is used>",
                    "action": "<a list of actions that occur after the result_text is displayed>"
                },
                "talk": {
                    "target": "<the name of the thing the user wants to talk to>",
                    "says": "<what the user needs to say>",
                    "result_text": "<the text that is displayed after the user says the right thing>",
                    "action": "<a list of actions that occur after the result_text is displayed>"
                }
            }
        }
    ],
    "cutscenes": [
        {
            "name": "<the name of the cutscene>",
            "text": [
                {
                    "color":"<the color of the text>",
                    "text":"<text to display>",
                    "wait": true <-- Optional - If true, the cutscene pauses until the user presses a key
                    "clear": true <-- Optional - If true, the screen is cleared BEFORE this line of text is displayed
                },
                {
                    "color":"<the color of the text>",
                    "text":"<text to display>",
                }
            ]
        }
    ]
}
```

## Actions
The "action" field in the JSON is a semicolon-separated list of the following commands:
- Inventory.add(<item_name>)
- Inventory.remove(<item_name>)
- Player.addCondition(<condition_name>)
- Player.removeCondition(<condition_name>)
- Room.addCondition(<condition_name>)
- Room.removeCondition(<condition_name>)
- Room.startCutscene(<cutscene_name>)
- Game.gameOver()
Examples:
- Inventory.add(knife)
- Player.addCondition(has_armor)
- Inventory.remove(rock);Inventory.add(ring)
- Room.addCondition(knocker_fell)

## Inventory
The user has an inventory that contains every item they have picked up so far in the game. Actions can be used to add or remove items to/from the inventory.
At any point in the game, the user can look at their inventory.

## Player conditions
Some actions result in changes to the player's condition. These changes are permanent from room-to-room (unless changed by another action later)

## Room conditions
Some actions result in changes to the current room's condition. These changes are permanent (unles changed by another action later).

## Condition checks
Some text descriptions and actions are based on whether or not certain conditions are met

## Room text
When the player enters a room, each object in the "description" is checked
1. If the object has a "condition" field, that field is evaluated. If there is no condition field, it is assumed to be "true"
2. If the condition is true, the text is displayed
3. If the room still has the item that was originally assigned to it, the player is told about it "You see a(n) <item_name> here"

## Cutscenes
When the Room.startCutscene() action is performed, the screen is cleared and the text for the cutscene is shown.
Each line of text can be a different color. Some lines of text may require the user to press a button to continue it is displayed.
It is also possible to define lines of text that require the screen to be cleared before they are displayed (this is implicitly true for the first text in a cutscene).

## Interface
When the player enters a room, the screen is cleared and the description of the room is shown. The player is then given a menu and they can select
the action that they want to take
+-----------------------------------------+
|Description of the room goes here        |
|What would you like to do?               |
|Look Take >Use< Talk Inventory           | <-- Use is currently selected
|North South East West                    |
+-----------------------------------------+

## Action prompts
When the user selects an action, they may be give prompts for more information:
|Action|Prompt1|Prompt2|
|Take|What would you like to pick up?|
|Use|What would you like to use?|What would you like to use the <itemname> on?|
|Talk|Who would you like to talk to?|What would you like to say?|
