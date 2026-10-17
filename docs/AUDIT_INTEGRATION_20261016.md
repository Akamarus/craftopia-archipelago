# Audit integration — v1.12.2 / regional revision 3

The [research audit](research/2026-10-16/README.md) is now represented by a versioned, sanitized progression catalog shipped in the APWorld: 253 recursively included item definitions, 10 towers/30 repair stages, nine age transitions and 77 resistance records. Disabled items remain identifiable as evidence but cannot supply an active crafting route. No player identity, inventory, shop stock or save state is included.

`AuditedProgression.py` supplies exact tower/age costs, rounded-up recipe batch costs and material capability nodes for the existing least-fixed-point evaluator. Profiles must explicitly provide gathering/trading alternatives, age access and station construction access. Missing evidence produces an unreachable node; a cycle cannot unlock itself. Station operating permissions are added separately. Quantities describe costs, not an inventory simulation or proof of renewable resources.

The new Geezah regional profile r3 uses audited ingredient IDs for its three tower stages and first age transition. Its original local-resource assumptions remain experimental. The 52-check pool and tower-plus-dungeon goal remain the same. Historical pilot profiles and active seed/save files are unchanged. Later ending regions remain rejected pending dungeon identities, location selection and resource/route verification.

The mod now recognizes three additional exact generic crafting-bench families:

| Family | Treatment |
|---|---|
| 20012 Potion Brewing Stand | Production: alchemy |
| 20013 Machine Factory | Ordinary manual crafting; vanilla age/material checks |
| 20020 Furniture Workbench | Ordinary manual crafting; vanilla age/material checks |

Automation retains its independent hook/permission. Unknown identities still fail closed before costs. These new family decisions are tested with representative station objects; the exact live prefab classes still need in-game confirmation. The unresolved generic generator is deliberately not made universally free.

Resistance values and durations are available as facts, not fabricated region access rules. Named-region boundaries, local overrides, equipment permission/obtainability and sustainable potion supply still need joining before late-game protection rules are enabled. Trade definitions likewise do not become free material sources.

## Verification

Production permission tests: 40 assertions passed. Release mod build: zero warnings/errors. The regional suite passed eight tests including 100 real AP 0.6.8 fills. Additional full-world and connector verification is recorded in STATUS.md after completion. An initial broad-suite run failed because its temporary directory was inaccessible; it was rerun using a workspace temporary directory.

## Reproduce catalog export

Run `tools/export_audited_progression.py CAPTURE RESEARCH_DIRECTORY OUTPUT_JSON`. It requires matching capture/audit/asset assembly hashes and writes only sanitized selected fields. Keep the source capture outside the distributable package. The output is `worlds/craftopia/progression_audit_20261016_r1.json`.

No installation or live seed replacement is included in this update. The prepared DLL and APWorld can be installed separately without resetting the current character.
