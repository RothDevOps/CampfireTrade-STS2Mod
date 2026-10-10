# Missing Trade button: diagnostic procedure

The cause of the reported client-specific failure is not confirmed. Reinstallation,
matching mod files and a fresh profile did not resolve it for the reporting players.
The diagnostic build records initialization, option generation, resources and button
creation without changing the multiplayer option order or suppressing exceptions.

## Reproduce locally

1. Use the same game, BaseLib and Campfire Trade versions on every client.
2. Enable only BaseLib and Campfire Trade and start a multiplayer run.
3. Reach a Rest Site before choosing any option. Confirm that the affected player can
   see the normal options and whether Trade is missing entirely or only its image.
4. Exit the game and collect the complete game log from that session, including
   startup messages and errors. Search for `CampfireTrade` to locate the diagnostics,
   but preserve surrounding game/BaseLib errors too.
5. Collect a working player's log from the same run as a comparison. Record the OS,
   game version, language, enabled mods and which player hosted.

Do not delete profiles or saves as a troubleshooting step.

## Read the evidence

| Log evidence | Next investigation |
| --- | --- |
| No `Initializing` entry | Mod loader errors, enabled mod, manifest and DLL loading. |
| `Initialization failed while ...` | The logged stage and full exception; initialization did not complete. |
| `build=` differs between clients | DLLs were built differently even if their assembly version is identical. |
| `Initialization complete` but no `Added Trade` during multiplayer | Generate patch execution; compare patch owners and surrounding errors. |
| `Skipping Trade ... solo run` during multiplayer | Run state/player count on that client. |
| `Trade already exists` with another type | Another mod uses the same `TRADE` option identifier. |
| `Rest site option generation failed` | An exception occurred while generating options; inspect the stack trace. |
| Local options omit `TRADE` despite `Added Trade` | Another patch changed the returned options; compare Harmony patch owners. |
| `Trade asset before preload ... exists=False` | PCK mounting/export/import issue for the logged resource path. |
| Localization reports missing keys | Localization files did not load; inspect PCK and mod loader messages. |
| `Trade button initialization failed` | Button resource/text initialization exception; inspect the stack trace. |
| Local options include Trade but no room/button logs | Room preloading/creation failed before the UI initialized. |
| `Trade button ready` and room lists `TRADE` | Investigate visibility/layout and other UI modifications. |

`visible=True` records the button's own visibility flag at initialization; it does
not prove the button is visible on screen after animations or parent visibility changes.
Game languages other than English/German are not by themselves proof of a failure:
the inspected game version loads English localization as a fallback.

## Manual validation before release

- Compare working and affected clients in a two- or three-player run, including
  different host roles. A successful local build alone does not reproduce this bug.
- Verify Trade is present before selection and test accept, decline and cancel.
- Confirm solo runs hide Trade unless the solo debug option is enabled.
- All artifact distribution and Steam Workshop publishing remain manual owner tasks.
