# AdventureLandSharp

This is a headless C# client for [AdventureLand](https://adventure.land/). The design intent is to provide an API for developers to create their own logic. It is not intended to be plug-and-play for non-technical users.

## Getting Started

If you just want to dive right in, start by reading `AdventureLandSharp/Example/*.cs`. There are three key components:

* `SessionCoordinator`: This handles deciding which sessions should be running at any given time. The example coordinator will cycle through characters on a timer.
* `Session`: This handles one individual session, where a session is defined as the whole 'interaction' between the client and the AL server. The example session will establish a connection, create and simulate a character, and handle reconnections if the connection drops.
* `Character`: This handles the simulation logic of the character. The example character will roam the world, drinking potions as appropriate, and attacking anything in range.

A note on socket APIs: Many of them are scaffolded, but not yet implemented. You may find yourself needing to extend `AdventureLandSharp.Core.SocketApi`.

### Project Structure

* `AdventureLandSharp`: This is a headless client per the above description.
* `AdventureLandSharp.Core`: This project contains all of the core functionality. Things like the socket communication/persistence layer, or the HTTP communication layer, parsing game data, or implementations of various key algorithms like pathfinding.
* `AdventureLandSharp.Data`: For now, mostly empty tool to perform data processing. Architecturally intended to be a codegen layer from gamedata.js -> C# bindings for all data.
* `AdventureLandSharp.SecretSauce`: Full game implementation I used for my characters. See below for more info.
* `AdventureLandSharp.Test`: Unit tests.
* `AdventureLandSharp.WebAPI`: This implements a basic REST API over some of the core functionality.
* `js_reference`: TypeScript helpers for vanilla JS client, mostly related to crafting. You need to run `npm build` to do the TypeScript -> JavaScript conversion.

### Enviroment Variables

* `ADVENTURELAND_LOG_DIR`: Obvious. Just the path to the directory you want logs stored. File name is in `log_yyyy-MM-dd_HH-mm-ss.txt`.
* `ADVENTURELAND_LOG_LEVEL`: Log level for the file above.
* `ADVENTURELAND_LOG_LEVEL_CONSOLE`: Log level for console / stdout.

Consult `AdventureLandSharp.Core\Util\Log.cs` for valid log levels. Log level is passed here verbatim.

## SecretSauce (full game implementation)

This is the full implementation of my character's logic. To start, I recommend checking:

* `Session.cs`: each character has their own session which controls lifetime of the character.
* `SessionCoordinator.cs`: coordinator determines which characters/sessions should run, and constructs them.
* `SessionEventBus.cs`: event bus enables communication between characters.
* `Character/CharacterBase*.cs`: base class, shared code for all characters.
* `Classes/*.cs`: one or more for each character class.
* `Tactics/HighValue_PinkGoblin.cs`: small example showing how advanced combat code can be written.
* `Strategy/HighValue_PhoenixScout.cs`: small example showing how advanced travel code can be written.

### External Dependencies

* InfluxDB: storing metrics.
* Redis: reading config files. See Redis below.

### Redis Configuration

After setting up a Redis server, you will want to use database 1 (production) and database 2 (local server flow).

1. Create the `v2_config` key with the following structure, one line per character:

```json
[
    "Arcanomato",
    "Ragemato",
    "Seamato",
    "Sneakmato"
]
```

2. Create one key per character (example: `v2_config_arcanomato`):

```json
{
    "partyLeader": "Arcanomato",
    "partyLeaderFollowDist": null,
    "partyLeaderAssist": null,
    "destroyItemsData": [],
    "keepItemsData": [
        "goldbooster",
        "luckbooster",
        "xpbooster",
        "tracker"
    ],
    "sellItemsData": [
        "cclaw",
        "hpamulet",
        "hpbelt",
        "mushroomstaff",
        "ringsj",
        "slimestaff",
        "stinger",
        "vitearring",
        "vitscroll",
        "whiteegg"
    ],
    "targetsData": {
        "cutebee": 100,
        "goblin": 100,
        "goldenbat": 100,
        "rgoo": 50,
        "snowman": 50,
        "wabbit": 50,
        "bgoo": 35,
        "greenjr": 30,
        "jr": 30,
        "fvampire": 28,
        "mvampire": 27,
        "phoenix": 26,
        "frog": 25,
        "squig": 6,
        "squigtoad": 5,
        "crab": 4,
        "crabxx": -1,
        "dragold": -1,
        "franky": -1,
        "grinch": -1,
        "icegolem": -1,
        "mrgreen": -1,
        "mrpumpkin": -1,
        "pinkgoo": -1,
        "tiger": -1,
        "puppy1": -1,
        "puppy2": -1,
        "puppy3": -1,
        "puppy4": -1,
        "kitty1": -1,
        "kitty2": -1,
        "kitty3": -1,
        "kitty4": -1,
        "target": -1,
        "target_a500": -1,
        "target_a750": -1,
        "target_r500": -1,
        "target_r750": -1,
        "target_ar900": -1,
        "target_ar500red": -1
    },
    "healthPotion": "hpot1",
    "manaPotion": "mpot1",
    "elixir": "elixirint2",
    "tradeTargetsItems": [
        "Seamato"
    ],
    "tradeTargetsGold": [
        "Seamato"
    ],
    "shouldLoot": true,
    "shouldUsePassiveRestore": false,
    "shouldHuntPriorityMobs": true,
    "shouldAcceptMagiport": true,
    "shouldDoEvents": true,
    "blendTargets": [
        "phoenix",
        "kitty1",
        "kitty2",
        "kitty3",
        "kitty4",
        "kitty5"
    ]
}
```

Non-obvious config explanations:

* `partyLeaderFollowDist`: Clamp valid positions within (ex: 150) units of the leader.
* `partyLeaderAssist`: Only attack this person's target when they're around.
* `shouldUsePassiveRestore`: Whether to ever use the 4-second CD recovery abilities.
* `shouldHuntPriorityMobs`: When a character passes a mob, they annouce it to the other characters. If another character spots a mob that you have flagged as a priority, should you go to it?
* `blendTargets`: List of valid monsters for you to blend (e.g. steal the skin of).

### How do I expand sockets?

* Cross-reference with AL server code.
* Check log output. Verbose will dump any unrecognised socket events and VeryVerbose will dump everything.

### Other Notes

* Make sure to edit every variable starting with CREDENTIAL_ if you want it to run.
* Architecture/thread safety: Each character runs on its own thread. You can access a character's data safely using the OnTick. Characters communicate through the event bus, not directly, to simplify thread safety.
* TODOs/hardcoded: there are some instances where things such as character names are hardcoded. Make sure to explore the code. Pro-tip: search for "mato" and you'll probably find everything.
* Search for "bank" and "bank_b" and make sure to update these to reflect your character's current bank access (1/2/3 floors).
* Config updates live, so you can easily edit using something like Another Redis Desktop Manager to log characters on/off and tweak their config without a restart.
* Code has been tested most on Debian 12 / x64 / .NET 9 preview.
* `smap_data.json` is extracted dumped from the running server. If you search the Discord, you might find instructions, but honestly just search for `smap` in the server code and dump the obvious. It will work without, but start-up will take longer and paths will generate with fewer shortcuts.
