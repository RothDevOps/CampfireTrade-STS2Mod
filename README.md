# Campfire Trade

Campfire Trade is a Slay the Spire 2 mod that adds a new Rest Site option: **Trade**.

The Trade option lets a player request a card from another player during a multiplayer run. 
The selected player receives a trade request and can either accept or decline it. 
If accepted, the requested card is removed from the target player's deck and added to the initiating player's deck.

## Features:
- Adds a new Trade option at Rest Sites.
- Allows selecting another player in the run.
- Allows browsing that player's deck and choosing a card to request.
- Sends an accept/decline request to the target player.
- Transfers the requested card if the target player accepts.

## Multiplayer
This mod is intended for multiplayer runs.
All players in the lobby should have the same version of the mod installed. 
The Rest Site option is synchronized across clients, so mismatched mod versions may cause incorrect behavior.

## Localization
Current supported languages:
- english
- german

## Requirements
- BaseLib

## Installation
### Workshop installation
Best installed via the workshop: TODO: Add workshop link
### Manual installation
Install Requirements (BaseLib)

Place the published mod files in your Slay the Spire 2 mods folder.
The installed mod should include files similar to:

`CampfireTrade.dll`

`CampfireTrade.pck`

`CampfireTrade.json`

## Local build setup
This repository does not commit `Directory.Build.props`, because that file contains machine-specific paths.

To set up the project locally:

1. Copy `Directory.Build.props.template`.
2. Rename the copy to `Directory.Build.props`.
3. Edit the paths inside `Directory.Build.props`:
   - `GodotPath`: path to your MegaDot/Godot 4.5.1 Mono executable.
   - `Sts2Path`: path to your local Slay the Spire 2 install folder.

## Current Mod Status
This mod is doing what it says though it is still in early development.
There are still improvements to be made and I'm also keen to read your comments and suggestions on this mod.

## Compatibility
This mod changes Rest Site behavior by adding an additional Rest Site option. 
It may conflict with other mods that heavily modify Rest Site option generation or multiplayer Rest Site synchronization.

## License
See the repository license file for details.
