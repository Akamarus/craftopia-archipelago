# Craftopia Archipelago

Unofficial [Archipelago](https://archipelago.gg/) integration for **Craftopia — Seamless World** on Windows PC.

> **Experimental alpha.** Geezah and Millewind runs have reached victory in controlled playtests. Full-game logic and general compatibility are still in development. Use a separate character/world and keep backups of your saves.

The current source prepares a **1.18.0 balance candidate through Brigandine Valley**. Public downloads may still contain the earlier alpha. See [candidate scope and validation](docs/VALLEY_RUNS.md).

## Start here

- **Candidate scope:** cumulative regional runs ending anywhere from Geezah through Brigandine Valley, with missions, Soul Orbs, mission categories, tower repairs, age evolutions and main dungeon checks.
- **Goal:** finish the ending region's tower **and** clear its main dungeon.
- **Install:** extract the Windows release ZIP, double-click **Setup.cmd**, and press Enter. Setup detects Craftopia, installs the verified loader, and handles manual or subscribed Workshop copies.
- **Connect:** F8 opens the Field Guide, including server/slot/password settings and character enrollment. A packaged build includes its own bridge runtime.
- **Availability:** **[Download the Windows alpha](https://github.com/Akamarus/craftopia-archipelago/releases)** — includes the compiled mod, portable bridge, APWorld and example YAML. No compiler or Python setup required. GitHub's green **Code → Download ZIP** button downloads source instead.
- **Still in development:** later regions, remaining acquisition requirements, route coverage and native multiplayer compatibility. New map markers and pause behavior await live acceptance.

[Installation and setup](docs/INSTALLATION.md) · [Roadmap](docs/ROADMAP.md) · [Testing and issue reports](docs/TESTING.md) · [Developer guide](docs/DEVELOPMENT.md)

Steam Workshop distribution is being tested privately; use the GitHub release until a public Workshop link is listed here.

## Choose your run

| Ending region | Areas included | Checks with Soul Orbs | Victory |
| --- | --- | ---: | --- |
| Geezah | Geezah | 119 | Tower complete + Ruin of Beginning |
| Millewind | Geezah and Millewind | 161 | Millewind tower complete + Old Garden of Undead |
| Yarden Mountain | Earlier regions and Yarden Mountain | 215 | Mountain tower + Ruin of Hunters |
| Yarden Ruin | Earlier regions and both Yarden towers | 219 | Ruin tower + Old Garden of Storms |
| Owatatsu | Earlier regions and Owatatsu | 275 | Owatatsu tower + Labyrinth of Torrent |
| Brigandine | All above and Brigandine Valley | 330 | Brigandine tower + Old Garden of Massacre |

These totals apply to the supplied regional configuration. Disabling Soul Orb checks gives 68 Geezah or 73 Millewind checks. Checks beyond the chosen ending region are excluded. Yarden and later endings are release-candidate content pending targeted live validation.

## Reviewed balance update

New seeds use the reviewed regional equipment thresholds (2/3/4/5/6/7), individual unlock-placement deadlines, and 206 additional equipment tier assignments. Life starts unlocked; Combat Support is a separate unlock. Tower repair recipes retain their existing requirements. Use the matching mod, bridge and APworld from the same build. Old seed equipment rules are preserved. See [1.18.0 notes](docs/RELEASE_v1.18.0.md).

## What is randomized?

**Checks:** selected individual missions, mission-category reward claims, one-time regional Soul Orb pickups, each tower repair stage, age evolutions and main dungeon clears. Respawning chests are excluded.

**Items:** progressive equipment permissions, production unlocks such as cooking and smelting, skill-tree unlocks, Skill Points and Soul Orb Currency. Equipment unlocks allow the relevant gear; they do not automatically put that gear in your inventory.

Regional logic uses vanilla Tamer capabilities. A separate Tamer mod is optional and is not required to complete a seed.

## In-game features

- Field Guide with connection controls, check totals, unlock information and received-item history.
- Item notifications and explanations when an AP unlock blocks an action.
- Remembered character/world enrollment for returning to the same seed.
- Persisted reward delivery and reconnect recovery.
- New in v1.15.0: selected regional Soul Orbs shown immediately on the full map, gold available and grey completed, with an optional hide-collected toggle.
- New in v1.15.0: the expanded Field Guide pauses single-player simulation and suppresses camera/player input. Multiplayer worlds remain live.

**The last two features have automated coverage but still need in-game visual/input verification.** See [map and pause notes](docs/ORB_MAP_AND_PAUSE.md).

## Current baseline and limits

| Component | Baseline |
| --- | --- |
| Current source candidate | v1.18.0-rc.1; not yet published |
| Archipelago | 0.6.8 |
| Game scope | Windows PC, Seamless World; observed game build 20260825.1206 |
| Loader | BepInEx 5, 64-bit Mono |
| Validated play mode | Single-player Craftopia connected to an AP server |

The project owner completed Geezah and Millewind playtests. That does not establish every possible seed, physical orb route, combat build or multiplayer scenario. Required progression is currently kept off optional regional orb checks because their individual traversal routes remain under review. The 48 presumed legacy equipment items remain outside active progression review.

[Regional rules](docs/REGIONAL_RUNS.md) · [Progression evidence](docs/VERIFIED_PROGRESSION_v1123.md) · [Detailed status history](docs/STATUS.md)

## Repository layout

| Path | Purpose |
| --- | --- |
| `worlds/craftopia/` | APWorld, options, catalogs and generation tests |
| `client/` | Bridge, connection host and delivery tests |
| `mod/CraftopiaArchipelago/` | BepInEx mod |
| `mod/*Tests/` | C# regression programs |
| `tools/` | Packaging, generation and evidence tools |
| `docs/` | Player guides, developer notes and historical research |

## Development disclosure

This project is developed with AI-assisted code and documentation under the project owner's direction. The owner guides design decisions, performs gameplay testing and accepts or rejects changes. Automated tests support that process; they do not replace in-game verification.

Game assemblies, saves, credentials, local configuration and delivery ledgers are not distributed in source. See the [developer guide](docs/DEVELOPMENT.md) for building and validation.
