# Craftopia Archipelago 1.18.2 — Regional Alpha

Ready-to-use Windows build for Craftopia Seamless World, with Archipelago **0.6.8** support. Runs can end in Geezah, Millewind, either Yarden region, Owatatsu or Brigandine Valley. This remains an experimental alpha.

## Download and install

- **Players:** download `Craftopia-Archipelago-1.18.2-Windows.zip`, extract it, close Craftopia and run **Install Bundled Build.cmd**. This installs the included mod and bridge even if an older Workshop copy exists. No Python or compiler setup is required.
- **Hosts:** install the attached `craftopia.apworld` into AP 0.6.8's `custom_worlds` folder and restart the generator. Use the attached `Craftopia.yaml`, edit the slot name and choose an ending region. It defaults to Brigandine; `geezah` is the short-run option. Keep `game: Craftopia` and `Craftopia:` capitalization.
- In game, press **F8 → Connection**, enter the room details and enroll a fresh character/world. Returning players reconnect to their existing slot and character without enrolling again.

Existing saves and AP records should be backed up. Do not erase delivery records during an upgrade. Workshop publication is separate from this GitHub release; use the bundled installer to select this version explicitly.

## Changes

- Fix APworld container loading and AP 0.6.8 generation compatibility.
- Allow ordinary progression to cross games in a multiworld. Opening cooking and smelting remain local. Regional placement deadlines apply only within the recipient's Craftopia world.
- Correct Universal Tracker Skill Point reconstruction and bound progression reachability caching.
- Grant a starting weapon matching the selected class in newly generated regional seeds. Bow includes arrows; Staff includes Magic Tier 1 and a skill point. Existing seeds retain their original behavior.
- Normalize the Staff starter's Magic unlock so it actually opens Magic Tier 1, including recovery of already recorded starter unlocks on reconnect.
- Avoid clearing Field Guide activity on repeated messages for the same session and show the active validation result instead of a misleading waiting message.
- Retain reviewed equipment tiers, tower recipe requirements, region goals and progression safeguards from the previous regional build.

## Validation

- APworld source suite: 89 tests passed before the preceding main-branch merge.
- Packaged generation matrix: 72 cases covering six endings, three starting modes and four player layouts; three additional 16-player generations passed.
- A full **50-player / 50-distinct-game** generation passed. Its actual server data contained every expected Craftopia item in the 119-item Geezah pool; opening cooking/smelting stayed local.
- Final audited APworld: Ionium AP 0.6.8 passed 253 core tests and 33 fuzz cases across all 11 variants. The preceding candidate also passed 360 fuzz cases; detailed artifact distinctions are in the generation audit.
- Starter transaction, unlock replay and enrollment/reload tests cover duplicate prevention. The owner confirmed the corrected Staff starter and session display work in game on October 28.

Generation success verifies the modeled logic, not every physical route or combat scenario. Native Craftopia shared-world co-op still needs host/guest testing; AP mixed-game rooms are a separate feature. Later-region and map/pause coverage remain incomplete. The broad historical client test runner is not claimed to pass.

See the repository README and installation guide for current setup instructions, and `docs/GENERATION_AUDIT_20261027.md` for detailed generation evidence.
