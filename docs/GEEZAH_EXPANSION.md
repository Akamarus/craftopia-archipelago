# Geezah expanded playtest — r5

The regional profile grows from52 to117 checks when soul orbs are enabled:
52 individual missions (previously38),9 existing category claims,3 tower repairs,
1 age evolution,1 main dungeon clear, and51 native Geezah soul orbs.
Victory remains tower stage3 AND Ruin of Beginning.

## Added missions

| IDs | Activity | Logic |
|---|---|---|
|34|Killing headshot on a cow|Bow1, arrows, local wildlife|
|61|Collect3 Animal Flesh|Local wildlife|
|73|Craft20 ingots|Copper smelting|
|80,81|Craft Branch Bow and Branch Arrow|Bow1 for bow; workbench and chicken feathers for arrows|
|82|Craft Copper Shield|Blacksmith, Shield2, leather|
|87,93|Discover dungeon, open map|Geezah field access|
|88|Defeat dungeon boss|Ruin of Beginning combat route|
|99,544|Learn a skill, heal using a skill|Magic tree and1 Skill Point|
|519|Craft Old Rag|Workbench and straw|
|529,530|Craft flour and bread|Cooking Pot, Cooking and Farming|

Evidence: frozen mission_catalog_20261007_r1.json exact proc targets;
installed assembly88db99ebe9fbf1296c74465b6f32467cdbc71711235f925532d9bcf6f2ccebea;
recipe and drop exports under docs/research/2026-10-16 and follow-up directories.
Cow and chicken availability use the existing local-wildlife assumption.
Magic and two progressive Shield copies are added; four Skill Points are marked
progression to cover Tamer3 plus Heal1. Remaining Skill Points are filler.

## Soul orb boundary and limitation

The native field algorithm selects51 of439 canonical orbs. Exact derivation,
source hashes, field weighting, boundary checks, positions and exclusions are in
research/geezah-orb-expansion.json. Runtime subset is geezah_orbs_20261016_r1.json.
No nearest-tower approximation is used. Native-unassigned orbs are excluded.
Selected orb pickups use existing AP replacement and currency delivery contracts;
unselected orbs remain vanilla. The pool includes51 Soul Orb Currency rewards.

Individual routes are not yet playtested. All51 are OPTIONAL for victory and may
hold only non-advancement rewards. The provisional access rule uses the glider
and cooking/updraft capability; it does not prove physical reachability. This
restriction prevents an unverified orb route from holding required progression.

No respawning chest checks or later-region checks are enabled. Category28 could
be added later as a separate versioned milestone catalog update; this seed adds
its two individual cooking missions, not its category claim. Armory missions,
Detector and later refining missions remain excluded pending support/age fit.
