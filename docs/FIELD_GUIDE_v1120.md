# Field Guide — v1.12.0

Replaces the plain AP overlay with the approved Field Guide direction: a parchment book with a teal header and progress sidebar, plus a compact draggable gameplay badge.

- F8, or the badge's + button, opens the guide. F8, Escape, or × closes it.
- Gameplay continues. This menu never changes time scale or invokes pause.
- Received shows scrollable bridge history, newest first, and identifies truncated history. It does not list completed checks.
- Unlocks shows equipment tiers, production groups and skill permissions from the live enforcement model, including starter skills.
- Session shows slot, bridge connection, aggregate goal/check state and delivery/validation status.
- The sidebar and badge show completed/total checks. An unknown denominator is shown as ?.
- Drag the badge's text area while the cursor is visible; its position is saved using the existing HudX/HudY settings.
- Opening acquires the cursor; closing, focus loss and teardown restore its previous state. Normal game menus may also manage the cursor; this needs a live check.
- Existing selected-recipe explanations and short notices appear near the lower center.

Textures and text styles are cached. Long history row measurements are cached per snapshot/width; only visible rows are drawn. Compile-only Unity IMGUI/Input/TextRendering references are supplied by the game at runtime and must not be packaged with the plugin.

No seed, check pool, grant logic, save, server setting, or optional tamer behavior changes. Aggregate progress is authoritative; the prototype's sample region/tower annotations are deliberately not presented as live facts.

## Verification

The added received-row test failed before implementation, then passed. Notification tests cover plain text, bounds, drag lifecycle, snapshot reset, goal/count state, and exact unlock projection. UnlockMirrorTests pass all 23 assertions. Release builds with zero warnings/errors. Independent code review found no important issues.

Live acceptance pending: F8 in gameplay and crafting menus; all three tabs; scroll a long history; drag/reload badge position; close and return to normal mouse look; confirm enemies/time continue, UI clicks do not trigger unwanted game actions, and locked-recipe notices remain readable. Test at the player's usual resolution before calling visual acceptance complete.
