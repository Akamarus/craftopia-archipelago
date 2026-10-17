# Equipment evidence and permission checks

**Goal:** Turn the captured item metadata into a reproducible, strictly validated equipment permission catalog, without changing existing seeds.

**Architecture:** A deterministic exporter records explicit decisions for observed item IDs. Recipe age proposes the permission tier; loot-only combat equipment remains unsupported. A pure C# evaluator checks catalog identity, metadata, ownership and age without mutating inventory. Game hooks must precede costs/transfers and preserve vanilla checks.

**Tech stack:** Python, C# netstandard2.1/net8 tests, HarmonyX, installed Unity Mono evidence.

- [x] Test exporter rejection of malformed/duplicate evidence and explicit unsupported loot.
- [x] Export a sanitized versioned catalog with source hashes and no character/world identifiers.
- [x] Test and implement immutable C# catalog parsing and permission decisions; unknown IDs, inconsistent metadata and failures deny when enabled, disabled mode preserves vanilla.
- [x] Implement runtime integration: whole-loadout extraction at the confirmation callback, explicit test-session activation, and non-destructive pre-existing gear admission policy. See `docs/EQUIPMENT_HOOKS_v192.md`. Runtime behavior remains to be tested; full use enforcement and playable seed activation are not claimed.
- [x] Run the relevant suites, obtain independent review, and record exact runtime limitations in `docs/EQUIPMENT_CATALOG_20261010.md`.

Equipment items remain excluded from the generated pool until safe hook coverage and the prerequisite profile exist. The existing test seed and its ownership/history are not edited to manufacture equipment tests.
