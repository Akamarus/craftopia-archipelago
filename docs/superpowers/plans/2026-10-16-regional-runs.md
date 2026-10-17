# Regional runs implementation plan

Approved scope: implement regional run settings and progression dependencies, starting with Geezah. Existing pilot and running session remain unchanged.

1. Add regression tests for a separate `regional` mode, `ending_region: geezah`, tower AND dungeon goal, complete selected-rule coverage and no outside-region routing.
2. Add an explicit regional profile, including the omitted mission-screen and category-reward tutorials. Model tower material capabilities, first age, copper ingots and dependent stations. Keep geographical access separate from enemy difficulty. Mark remaining resource/combat assumptions experimental.
3. Restrict Smelting and Cooking to opening checks reachable with no randomized permissions. Compose placement rules; reject impossible exclusions rather than granting items. Preserve all historical profiles and frozen IDs.
4. Test at least 100 real AP 0.6.8 fills, full progression spheres and all selected checks, exclusion failures and old profiles. Verify bridge victory requires AP confirmation of both final tower stage and dungeon.
5. Package a new APWorld and example YAML, document readiness limits and results, and review the implementation. No live seed replacement or game restart needed.

Only Geezah is offered as an ending region in this version. Later endings require an identified dungeon, explicit selected checks and resource/survival dependencies. Soul-orb region membership and environmental protection remain unverified, so this change does not enable them by proximity guesses. Multiplayer milestone enrollment remains unsupported.
