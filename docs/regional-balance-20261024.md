# Regional balance revision 20261024_r1

New regional seeds use the player's provisional progression workbook and these combat permission thresholds:

| Region | Equipment tier |
| --- | --- |
| Geezah | 2 |
| Millewind | 3 |
| Yarden Mountain | 4 |
| Yarden Ruin | 5 |
| Owatatsu | 6 |
| Brigandine | 7 |

Weapon families are alternative routes. Staff routes also require Magic and a skill point. Higher weapon tiers satisfy lower thresholds. This does not require every weapon family or an armor set, and does not gate physical travel. Tower submissions retain their material, production and age dependencies without an additional combat tier gate.

Each progressive receipt has its own latest placement region from the workbook. The pool is trimmed to the selected ending region. Life starts unlocked; uncertain Water Processing and Vacuum Gun entries remain outside these regional pools. Existing Soul Orb progression-placement restrictions remain in force.

New regional seeds use equipment_reviewed_20261024_r1 with equipment protocol 3 (mod 1.18.0). Tier numbers have not been renumbered. Of the 213 user equipment assignments, 206 are active; seven explicitly excluded legacy/unavailable/temporary entries remain excluded. Combat support has its own tier-one unlock placed in Geezah. Existing native craft ages still apply. Older seeds retain their original equipment catalogs. This revision changes newly generated seeds, not existing seed placements.

Validation: five new balance tests pass, including 24 actual AP fills across all six endpoints with Soul Orb checks on/off. Another 79 selected world regressions ran: 78 passed directly; the bridge roundtrip hit a sandbox temporary-directory permission error and passed when rerun with a workspace temporary directory. The larger hundred-seed stress suites were not completed. Independent review identified the higher-tier Gun edge case, now covered and fixed. This is generation validation, not a new live playthrough.
