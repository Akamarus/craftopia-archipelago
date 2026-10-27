# Starter kits implementation plan

Approved behavior: grant the starting class weapon once per fresh enrolled character; Bow also gets 30 Branch Arrows; Staff gets Magic access and one skill point. No auto-equip. Existing seed contracts remain unchanged.

Architecture: new regional slot protocol; bridge persists the chosen starter and Magic receipt. A local inventory receipt store reuses the currency intent/save-proof transaction with item-specific identity. Fresh enrollment creates stores; missing or interrupted stores hold rather than redeliver. SP uses the existing transactional grant pipeline with a reserved index. Runtime runs on the game thread after enrollment validation.

- [x] Add regression tests for frozen kit contents, tier eligibility, protocol and starter replay.
- [x] Generalize inventory delivery/save proof with unchanged Soul Orb defaults; test identity, duplicates, full inventory and restart ambiguity.
- [x] Integrate fresh enrollment, session binding, inventory delivery and starter SP. Preserve older sessions.
- [x] Build mod/bridge/APworld, run focused regressions, review changes, document live test limitations.

Mappings: Sword Stone Sword44; Bow Branch Bow92 plus Branch Arrow181 x30; Shield Wooden Shield116; Lance Pitchfork1914; Two-handed Zweihander1702; Katana Bokken6430; Staff Crutch6392. These all have tier1 in the reviewed catalog. Shield remains the existing defensive starting choice rather than secretly granting a second weapon family.

Validation: starter transaction/runtime, bridge starter tests, 21 delivery-repair tests, 89 currency assertions, 23 unlock assertions, 16 transaction assertions, five regional balance tests passed. Release build has zero warnings/errors. Exact packaged APworld and Akamarus YAML generated with AP 0.6.8; can_beat_game true, goal IDs [3002,4100], starter_kit_protocol=1. Fresh-context review identified arrow-consumption save retry edge; bounded timeout and consumption regression added. Portable runtime imports passed; packaged socket smoke test interrupted after hanging. Live starter delivery remains unverified. Installed DLL and two bridge files locally with backup and matching hashes; not published.
