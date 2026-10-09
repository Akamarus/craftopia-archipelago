# Craftopia Archipelago

An experimental native [Archipelago](https://archipelago.gg/) integration for Craftopia: a Python APWorld, a bridge client, and a BepInEx mod.

**Current source baseline: v1.8.9. Status updated October 9, 2026.** This is a development snapshot, not a complete or generally validated randomizer release.

## Verified in the game

Controlled Windows tests on October 8, 2026 verified:

- Explicit enrollment of a fresh character, bound to the character, slot, and session.
- Completing a mission sends a check and delivers its AP Skill Point exactly once.
- Points accumulate and survive saving and restarting, both unspent and spent on skills.
- Restarting the bridge restores delivery history without duplicating the tested rewards.
- Tamer learning is blocked before the AP unlock is received.
- A real mission reward delivers the Tamer unlock, permitting Novice Trainer to be learned with normal skill-point spending.
- The learned skill and Tamer permission survive restarting the game.

The tests earned two separate Skill Point rewards. Learning the first skill completed another mission and legitimately awarded the second point. Reload validation correctly counted two spent points with zero remaining.

See [current status and evidence summary](docs/STATUS.md) for the limits of these results.

## What is randomized today

- Mission checks: legacy mode groups missions into 53 classes; opt-in `20261007_r1` uses 125 individually identified missions observed in one runtime capture.
- The item pool includes Skill Points, skill-tree unlocks, and production-group unlocks. Tamer's complete delivery/enforcement path has been tested in-game; production enforcement still needs runtime verification.
- Current supported goal generation uses objective counts. Boss and age goals remain unsupported at generation. Catalog presence does not establish that every mission is reachable.

Equipment progression, additional check categories, reward replacement, material/money bundles, and multiplayer need further implementation or verification. Do not infer support from an option or an older design document alone.

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

This source snapshot does not include a ready-to-install APWorld package or a general-purpose enrollment UI. Enrollment configuration is deliberately explicit and intended for a verified new disposable character. See [development testing](docs/DEVELOPMENT.md) before enabling delivery. Existing saves and historical reward recovery need separate review; do not reset a ledger or erase a hold to make a test proceed.

## Verification baseline

The v1.8.9 development build passed **340 C# assertions**, with zero compiler warnings/errors. Real AP 0.6.8 generation produced the 125-location test seed. The whole Python suite was not rerun for v1.8.9; some older tests and documentation retain obsolete assumptions. These results are recorded observations, not a claim that every included test currently passes on every platform.

Published source excludes game binaries, build outputs, private saves, delivery ledgers, local configuration, and raw runtime logs. No binary release is published with this initial source snapshot.
