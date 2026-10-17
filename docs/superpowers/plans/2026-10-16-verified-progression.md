# Verified progression implementation plan

**Goal:** Apply verified acquisition, crafting and protection evidence to the existing progression model and generate an isolated Geezah test seed.

**Architecture:** Extend existing data-driven capability nodes and the versioned equipment policy. Preserve frozen seeds. Requirements are repeatable access capabilities, not simulated inventory. Only Geezah is an approved ending; later-region protection capabilities do not imply verified geographic access.

**Tech stack:** Python AP 0.6.8 world, C# BepInEx enforcement, JSON evidence catalogs.

**Spec:** User-approved four-part scope in this conversation; docs/REGIONAL_RUNS.md and docs/research/2026-10-16-closure/README.md.

## Constraints and review focus

- The 48 presumed legacy IDs stay excluded; duplicate display names do not share rules.
- Preserve existing seed contracts and all prior dirty work; no live deployment or save mutation.
- No recipe cycle may create access to itself. Production permissions and station construction are separate.
- Resistance routes require source/recipe access, age and equipment permissions where applicable; do not stack incompatible equipment or consumable buffs.
- Unknown region identities/material sources remain unreachable rather than free.

## Tasks

- [x] Add failing tests and implement reusable audited stage/age and environmental capability builders. Exercise missing sources, recipe cycles, heat/cold separation, legacy exclusions and unsupported protection.
- [x] Replace Geezah handwritten construction dependencies with audited ingredient/station chains; retain explicit local gathering evidence. Test denied smelting, farming and equipment routes.
- [x] Review and implement individually justified loot equipment rules under a new contract if the evidence supports them. Keep old catalogs unchanged and verify Python/C# parity.
- [x] Run focused suites, regression tests, real AP fills and final review; package an isolated Geezah seed with tower plus Ruin of Beginning victory. Write one focused live acceptance checklist.

## Execution notes

User has authorized execution; proceed without another approval loop. Existing shared checkout contains the complete prior implementation, so preserve it and make scoped edits rather than creating a checkout that omits that state. Independent agents are reviewing evidence only. Do not commit unrelated pre-existing work.

Verification: 63 world tests passed including 100 real fills; 5 new capability tests are included. Equipment 6909 assertions and focused bridge31 tests passed. Mod v1.12.3 builds with zero warnings/errors. New seed65472869102660608961 generated with52 checks and both goal locations. Independent dependency review found no actionable defects. Broader client suite status recorded in delivery notes.

Ruling: broad equipment source evidence is not a reviewed tier decision. This pass adds only the locally placed Goblin Bow alongside the existing Club; other verified sources remain candidates for later individual balance review. Later-region capability infrastructure is implemented without advertising unverified regional endings.
