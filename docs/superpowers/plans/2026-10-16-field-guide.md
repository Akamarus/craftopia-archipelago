# Field Guide implementation

Approved design: companion option C, followed by explicit implementation approval. Gameplay continues while open.

- [x] Expose bounded, sanitized received rows and check/goal state separately from legacy text; test snapshot replacement and reset.
- [x] Add read-only unlock rows from the same permissions used by enforcement, not inferred from item names.
- [x] Replace the legacy overlay with a centered parchment Field Guide, teal header, progress sidebar, Received/Unlocks/Session tabs, scrollable rows, and a small draggable badge. F8 opens/closes; cursor ownership is restored on close. Never change time scale.
- [x] Preserve transient/recipe notices, existing badge position configuration, session validation and delivery behavior.
- [x] Build Release, run notification and permission regression suites, review diff, stage the DLL and live-test instructions. Installation must wait if the game is running.

Scope: menu presentation only. No new checks, seed edits, reward grants, or save changes. Sidebar uses actual aggregate progress; region and tower claims from the prototype are not fabricated where the UI has no authoritative data.
