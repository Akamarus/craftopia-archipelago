# Craftopia Archipelago

An experimental native [Archipelago](https://archipelago.gg/) integration for Craftopia: a Python APWorld, a bridge client, and a BepInEx mod.

**Current development source: v1.12.3 experimental. Updated October 16, 2026.** This is a development snapshot, not a complete or generally validated randomizer release.

The expanded Geezah regional playtest has **117 checks**: 52 missions, 51 native-region soul orbs, nine mission-category claims, three tower repairs, one age evolution, and Ruin of Beginning. Victory requires **Geezah tower completion AND Ruin of Beginning**. Magic and Shield unlocks support the new healing and crafting missions. See [Geezah expansion](docs/GEEZAH_EXPANSION.md) and [regional progression](docs/REGIONAL_RUNS.md).

Individual orb routes still need live testing, so required progression items cannot be placed on those optional checks. Respawning chests and later-region checks are excluded from this profile. The separate experimental expedition mode retains its larger candidate pool.

The source includes the Field Guide menu, durable orb/currency delivery, mission-category/tower/age/dungeon observations, audited recipe dependencies, and explicit reviewed loot tiers. The 48 presumed legacy equipment IDs remain outside active progression review. See [current status](docs/STATUS.md) for verification and remaining limits.

## Current playtest: Geezah

The regional profile (`logic_mode: regional`, `ending_region: geezah`) supports a short run within Geezah. Enable soul-orb checks to use the full expanded pool.

| Check type | Count |
| --- | ---: |
| Individual missions | 52 |
| Geezah soul orbs | 51 |
| Mission-category completion claims | 9 |
| Tower repair stages | 3 |
| Agricultural Age evolution | 1 |
| Ruin of Beginning clear | 1 |
| **Total** | **117** |

The new missions cover bow crafting and use, copper shields, ingot production, flour and bread, healing magic, and dungeon exploration. The item pool includes progressive equipment permissions, production unlocks, Magic and Tamer skill-tree unlocks, Skill Points, and Soul Orb Currency.

Smelting and Cooking are restricted to opening mission locations. The 51 orb locations use the game's native region boundaries; their individual traversal routes remain experimental. These optional orb checks cannot contain required progression items. Disabling orb checks produces the 66-check mission/milestone variant.

**Victory requires both the final Geezah tower repair and clearing Ruin of Beginning.** Completing either alone does not finish the regional run. Later regional endings are not enabled yet.

## Implemented features and limits

- **In-game Field Guide:** compact status display, expandable received-item history, check totals, and unlock feedback while gameplay continues.
- **Progression enforcement:** equipment tiers, skill-tree permissions, production groups, and audited crafting dependencies. Selected loot weapons have explicit reviewed tiers.
- **Durable delivery:** character/session binding, persisted reward tracking, reconnect handling, and soul-orb replacement with AP currency delivery.
- **Milestone checks:** selected mission-category claims, tower repairs, age evolution, and the main dungeon clear.
- **Optional Tamer use:** regional logic stays within vanilla Tamer capabilities; a separate Tamer mod is not required.

Earlier controlled tests exercised mission rewards, permission enforcement, soul-orb delivery, save/reload behavior, tower repairs, and dungeon completion. The new 117-check profile still needs a complete live playthrough. Automated beatability is a check of the model, not proof that every physical route works.

Other profiles remain available for development: legacy mission-class logic, the larger experimental expedition candidate pool, and the historical dungeon-only Geezah pilot. Their scope and goal behavior differ from the current regional profile. Respawning chest checks are excluded; multiplayer and unverified later-region progression are not part of this Geezah playtest. The 48 presumed legacy equipment items remain outside active progression review.

See [current status](docs/STATUS.md), [expansion evidence](docs/GEEZAH_EXPANSION.md), and [audited progression](docs/VERIFIED_PROGRESSION_v1123.md) for details.

## Repository layout

| Directory | Contents |
| --- | --- |
| `worlds/craftopia/` | Native APWorld, options, observed catalogs, generation tests |
| `client/` | Native bridge, durable delivery state, tests; older PoC client retained for reference |
| `mod/CraftopiaArchipelago/` | BepInEx mod and enrollment, validation, transaction, and permission logic |
| `mod/*Tests/` | C# regression programs |
| `mod/DiagnosticCollector/` | Read-only runtime catalog collector source |
| `docs/` | Current status plus historical design and implementation notes |

## Development requirements

- Archipelago **0.6.8** source/runtime and its dependencies, separately installed.
- .NET SDK capable of building `netstandard2.1`; regression programs target .NET 8.
- Python and `websockets` for the native bridge. The controlled Windows run used Python 3.13.5.
- Your own Craftopia installation and compatible BepInEx 5/HarmonyX libraries. Controlled tests used Unity 2022.3.62 and HarmonyX assembly version 2.9.0.0.

Game and third-party reference DLLs are **not included**. The project files enumerate the required assemblies in `mod/refs/`; obtain those from your local installation and BepInEx. Do not publish that directory.

```sh
dotnet build mod/CraftopiaArchipelago/CraftopiaArchipelago.csproj -c Release
python -m pip install websockets
python client/craftopia_bridge.py --help
```

The repository contains source and packaging tools, not a published ready-to-install binary release or a general-purpose enrollment UI. Enrollment configuration is deliberately explicit and intended for a verified new disposable character. See [development testing](docs/DEVELOPMENT.md) before enabling delivery. Existing saves and historical reward recovery need separate review; do not reset a ledger or erase a hold to make a test proceed.

## Generate and package the regional playtest

Set `CRAFTOPIA_AP_SOURCE` to your Archipelago 0.6.8 source checkout and use a Python environment with its dependencies installed. From the repository root:

```sh
python tools/generate_regional_example.py outputs/geezah --seed 2026101605
python tools/package_apworld.py outputs/craftopia.apworld
```

The example player configuration enables all 117 checks. Generation and packaging do not install the mod, start a server, enroll a character, or replace an active session. Use a separate fresh character/world and session for a new playtest; retain earlier saves separately. Follow [development testing](docs/DEVELOPMENT.md) for enrollment and delivery setup.

## Verification baseline

The expanded regional profile passed **64 world tests**, including **100 randomized fills**, and **68 focused bridge tests**. These verify modeled logic and delivery contracts; they do not establish every physical orb route. All **15 C# regression programs** passed, and the Release mod build completed with zero warnings/errors. The legacy standalone AP wire and multiplayer test runners stall; an all-client-suite pass is not claimed. See [current status](docs/STATUS.md).

Published source excludes game binaries, build outputs, private saves, delivery ledgers, local configuration, and raw runtime logs. No binary release is published with this source update.

Historical dungeon-only pilot: [scope and live-test instructions](docs/GEEZAH_PILOT.md). For the current run, use the regional profile above.
