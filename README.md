# Craftopia Archipelago

Unofficial [Archipelago](https://archipelago.gg/) integration for **Craftopia — Seamless World** on Windows PC.

[Download 1.18.2](https://github.com/Akamarus/craftopia-archipelago/releases/tag/v1.18.2) · [Setup guide](docs/INSTALLATION.md) · [Release notes](docs/RELEASE_v1.18.2.md) · [Report an issue](https://github.com/Akamarus/craftopia-archipelago/issues)

**Experimental alpha — regional runs through Brigandine Valley.** Play Craftopia alongside other games in an Archipelago multiworld. Complete missions, collect Soul Orbs, repair towers and clear dungeons to send items; receive equipment permissions, production unlocks, skill trees and progression rewards in return.

## Play in three steps

1. **Install:** download `Craftopia-Archipelago-1.18.2-Windows.zip` from the release above and extract it. With Craftopia closed, double-click **Install Bundled Build.cmd**. It installs this release even if you subscribe to an older Workshop copy. No compiler or Python installation is required.
2. **Generate:** give your host the included **craftopia.apworld** and your edited **Craftopia.yaml**. The host needs **Archipelago 0.6.8**. Set your slot name and ending region; choose `geezah` for a short first run. The supplied template defaults to `brigandine`. Keep `game: Craftopia` and `Craftopia:` capitalized exactly.
3. **Connect:** launch Craftopia, press **F8 → Connection**, and enter the server address, slot and password. Create a fresh character/world, then select **Start this seed with this character**. Enrollment is remembered when returning to that run.

Use a separate character/world and back up your saves and AP records. GitHub's **Code → Download ZIP** contains source code, not the playable release. See [installation](docs/INSTALLATION.md) for returning to a run, uninstalling and troubleshooting.

**Steam Workshop:** [subscription](https://steamcommunity.com/sharedfiles/filedetails/?id=3823110766) is optional. Workshop and GitHub updates are published separately. Use **Install Bundled Build.cmd** for this exact GitHub build; **Setup.cmd** instead automatically selects a downloaded Workshop copy when one is present.

## Choose your run

| Ending region | Areas included | Checks with Soul Orbs | Victory |
| --- | --- | ---: | --- |
| Geezah | Geezah | 119 | Tower complete + Ruin of Beginning |
| Millewind | Geezah and Millewind | 161 | Millewind tower complete + Old Garden of Undead |
| Yarden Mountain | Earlier regions and Yarden Mountain | 215 | Mountain tower + Ruin of Hunters |
| Yarden Ruin | Earlier regions and both Yarden towers | 219 | Ruin tower + Old Garden of Storms |
| Owatatsu | Earlier regions and Owatatsu | 275 | Owatatsu tower + Labyrinth of Torrent |
| Brigandine | All above and Brigandine Valley | 330 | Brigandine tower + Old Garden of Massacre |

These totals apply to the supplied regional configuration. Disabling Soul Orb checks gives 68 Geezah or 73 Millewind checks. Checks beyond the chosen ending region are excluded. Later endings remain experimental; route and combat coverage is incomplete.

## What's new in 1.18.2

- Archipelago **0.6.8** packaging and mixed-game generation fixes. Cooking and smelting remain local opening unlocks; ordinary progression can appear in other games. Placement deadlines apply to items placed in your own Craftopia world.
- New regional seeds grant the selected class's starting weapon. Bow also receives arrows; Staff receives **Magic Tier 1 and a skill point**. Reconnect/reload delivery is recorded to prevent duplicate starter rewards.
- Corrected Staff's Magic unlock and Field Guide session display. The owner confirmed the updated starter flow works in game.
- Reviewed equipment tiers and regional requirements remain in place. Existing seeds keep their original rules; starter kits require a newly generated seed.

Generation validation includes a completed **50-player test with 50 different games**, an audit of all 119 Craftopia items in its Geezah slot, and AP 0.6.8 Ionium tests. These validate generation and modeled reachability, not every possible gameplay route. See [release notes](docs/RELEASE_v1.18.2.md) and the [generation audit](docs/GENERATION_AUDIT_20261027.md).

## What is randomized?

**Checks:** selected individual missions, mission-category reward claims, one-time regional Soul Orb pickups, each tower repair stage, age evolutions and main dungeon clears. Respawning chests are excluded.

**Items:** progressive equipment permissions, production unlocks such as cooking and smelting, skill-tree unlocks, Skill Points and Soul Orb Currency. Equipment unlocks allow the relevant gear; you still craft or find it. The starting weapon kit is the exception.

Regional logic uses vanilla Tamer capabilities. A separate Tamer mod is optional and is not required to complete a seed.

## In-game features

- Field Guide with connection controls, check totals, unlock information and received-item history.
- Item notifications and explanations when an AP unlock blocks an action.
- Remembered character/world enrollment for returning to the same seed.
- Persisted reward delivery and reconnect recovery.
- selected regional Soul Orbs shown immediately on the full map, gold available and grey completed, with an optional hide-collected toggle.
- the expanded Field Guide pauses single-player simulation and suppresses camera/player input. Multiplayer worlds remain live.

**The last two features have automated coverage but still need in-game visual/input verification.** See [map and pause notes](docs/ORB_MAP_AND_PAUSE.md).

## Current baseline and limits

| Component | Baseline |
| --- | --- |
| Public release | v1.18.2 (experimental regional alpha) |
| Archipelago | 0.6.8 |
| Game scope | Windows PC, Seamless World; observed game build 20260825.1206 |
| Loader | BepInEx 5, 64-bit Mono |
| Validated play mode | Single-player Craftopia connected to an AP server |

The project owner completed Geezah and Millewind playtests. That does not establish every possible seed, physical orb route, combat build or multiplayer scenario. Required progression is currently kept off optional regional orb checks because their individual traversal routes remain under review. The 48 presumed legacy equipment items remain outside active progression review.

[Regional rules](docs/REGIONAL_RUNS.md) · [Progression evidence](docs/VERIFIED_PROGRESSION_v1123.md) · [Detailed status history](docs/STATUS.md)

## Development and support

[Roadmap](docs/ROADMAP.md) · [Testing and issue reports](docs/TESTING.md) · [Developer guide](docs/DEVELOPMENT.md)

Native Craftopia host/guest co-op is still under development. Support for an AP room containing different games does not establish shared-world co-op compatibility.

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
