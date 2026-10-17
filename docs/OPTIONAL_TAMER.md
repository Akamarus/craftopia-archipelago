# Optional Tamer Combat companion

Tamer Combat can be installed as a separate BepInEx plugin alongside Craftopia Archipelago. Archipelago does not require it, bundle it, or enable it automatically. This is an experimental compatibility setup, not a verified multiplayer or generation option.

The locally tested candidate is Tamer Combat v0.9.0, SHA-256 `d4b5957f2fa61b606e160e1d9fe81277f0a9e92ce5109972e582667d588f1198`. The original binary is not redistributed here; source is not included in this repository.

With the game closed, put one copy in its own plugin folder. Keep the existing `com.jack.craftopia.tamercombat.cfg` and pet-tuning configuration. Its `[General] Enabled` switch controls patch installation at startup; switching it off requires a game restart. Individual modules also have switches. Removing/disabling the plugin is not a promise to revert pet progression already saved.

The existing configuration changes pet combat, following, experience, summon count, mounted combat, ascension and some skill effects. These can affect difficulty and progression. Enabling the companion does not grant the AP Tamer item or add excluded/unimplemented skills to the AP catalog. Archipelago remains responsible for its own learning permissions and point accounting. There is currently no AP-aware gate around every companion ability; do not claim that companion effects are locked by the AP Tamer item.

October 14 setup: the original companion was restored with its existing settings alongside v1.9.3 after backing up the current save/configuration. Joint startup, pet behavior and overlapping skill effects still require runtime checks. The continuing test character already owns the AP Tamer unlock.

Next compatibility checks: both plugins start without patch errors; Blacksmith repair works; owned Tamer learning retains normal point costs; summon/follow/recall and representative pet combat work; subsequent save/load preserves existing totals. Unknown interactions must be recorded rather than treated as a full compatibility pass.

Startup check: runtime identifies the restored DLL as v0.9.0 (the retained configuration header said v0.8.0). All eight Tamer modules reported patches applied, 165 pet overrides loaded, and Archipelago v1.9.3 verified its hooks and bound the expected session. No Error/FATAL/Exception matched in the inspected startup log. This establishes startup coexistence, not gameplay compatibility.

Gameplay follow-up: tester confirmed pet following, G recall and engagement when the player attacks. Copper Sword crafting/equipping also passed with both mods loaded. No Error/FATAL/Exception matched in the inspected session log. Advanced abilities, ascension, multiplayer, and behavior before AP Tamer ownership remain unverified.
