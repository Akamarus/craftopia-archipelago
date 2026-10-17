# Diagnostic Collector 1.4.0

Built for read-only runtime evidence collection. This collector does not grant rewards, submit Archipelago checks, change permissions, or edit game saves or delivery ledgers. Its own diagnostic files are written to `diagnostics/` beside the installed collector DLL. Install only one active copy of DiagnosticCollector.

## Capture

1. Load the test character and world, then press F9 for a baseline snapshot.
2. Play normally. When convenient, open a chest, collect a soul orb, claim a quest/challenge reward, visit a shop or Anubis, clear a dungeon, or advance an age. There is no need to reach every system in one session.
3. Press F9 after these interactions. Save and quit normally.
4. On a later load, press F9 again so identities and persistent state can be compared.

F9 writes a uniquely named schema-5 JSON snapshot. Observations of selected game methods are automatically saved about every two seconds and on collector shutdown. Keep both the snapshot and observation JSON files for review.

## Coverage and evidence boundaries

Snapshots retain the existing mission, skill, recipe, item and age capture and add loaded quest, achievement, challenge, shop, Anubis and dungeon data; nearby active chest and soul-orb fields; selected enemy fields; and skill lock-condition fields. Mission raw fields are included for prerequisite analysis. Vanilla pet creation is observed; custom Tamer progression is not a dependency.

The collector observes 13 methods related to chest/orb actions, mission/quest/challenge rewards, purchases, Anubis exchanges, age changes, dungeon clears, enemy kill-count updates and pet creation. A method returning is **not proof that a reward was granted or a new objective completed**. Before/after fields help interpretation; failed attempts and load-time updates must be distinguished during review.

Only loaded assets and active scene objects are captured. Missing, unavailable and truncated sections are explicit. This does not enumerate every object in the game or prove that missions are reachable. Dynamic reward getters are not executed. Reflection reads fields, not arbitrary properties or enumerators; scene positions and asset names use read-only Unity accessors.

The world comparison fingerprint is not an authoritative global identity: copied worlds can match. Observation context is normally a cached manager candidate, labeled with cache age; it may refer to the preceding world briefly during transitions. The age-change observer uses its actual manager instance.

## Bounds

Field projections cap depth, node count, string length and collection sizes. Snapshot sections cap rows at 1,024. Observations cap the pending queue at 256, the session at 10,000 events and session output at 64 MiB. Dropped events and limit status are reported. Failed writes retain queued events; removal occurs only after a successful write and rename. A hard crash can lose the most recent buffered observations. These files are diagnostic evidence, not a transaction journal.

## Verification

Collector tests cover inherited fields, scalar arrays, missing fields, excluding getters and arbitrary object formatting, bounded projection, numeric JSON and queue retention/overflow behavior. The Release build completed with zero warnings and errors. Static signature verification found all 13 target methods in the installed game assembly. Live Harmony execution and in-game capture still require verification; static checks do not establish runtime success.
