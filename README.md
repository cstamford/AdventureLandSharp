# AdventureLandSharp

This is a headless C# client for [AdventureLand](https://adventure.land/). The design intent is to provide an API for developers to create their own logic. It is not intended to be plug-and-play for non-technical users.

## Getting Started

If you just want to dive right in, start by reading `AdventureLandSharp/Example/*.cs`. There are three key components:

1. SessionCoordinator: This handles deciding which sessions should be running at any given time. The example coordinator will cycle through characters on a timer.
2. Session: This handles one individual session, where a session is defined as the whole 'interaction' between the client and the AL server. The example session will establish a connection, create and simulate a character, and handle reconnections if the connection drops.
3. Character: This handles the simulation logic of the character. The example character will roam the world, drinking potions as appropriate, and attacking anything in range.

A note on socket APIs: Many of them are scaffolded, but not yet implemented. You may find yourself needing to extend `AdventureLandSharp.Core.SocketApi`.

There is a full game implementation in `AdventureLandSharp.SecretSauce/*.cs`. Make sure to edit every variable starting with CREDENTIAL_ if you want it to run.

1. On architecture/thread safety: Each character runs on its own thread. You can access a character's data safely using the OnTick. Characters communicate through the event bus, not directly, to simplify thread safety.
2. On TODOs/hardcoded: there are some instances where things such as character names are hardcoded. Make sure to explore the code. Pro-tip: search for "mato" and you'll probably find everything.

There are some helper scripts for the vanilla JS client in `js_reference/*`, mostly related to crafting. You need to run `npm build` to do the TypeScript -> JavaScript conversion.

## Project Structure

1. **AdventureLandSharp**: This is a headless client per the above description.
2. **AdventureLandSharp.Core**: This project contains all of the core functionality. Things like the socket communication/persistence layer, or the HTTP communication layer, parsing game data, or implementations of various key algorithms like pathfinding.
3. **AdventureLandSharp.WebAPI**: This implements a basic REST API over some of the core functionality. 

## Contributing

PRs are welcome. Please follow the existing style.
