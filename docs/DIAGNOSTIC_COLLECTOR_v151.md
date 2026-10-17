# Collector 1.5.1 performance repair

User reported severe lag after 1.5.0. Log confirms 1.5.0 loaded, no exception flood; game exited normally. Inspection found whole-scene FindObjectsOfType calls for world manager and towers twice per second, plus world lookup during flush and tower hooks. This is a likely performance contributor, not a profiled proof of the full cause.

Removed recurring tower enumeration. Tower observers register up to 128 instances; polling reads those references and removes destroyed instances. F9 still enumerates loaded towers for snapshots. Towers without observed callbacks are not sampled; snapshots remain complementary evidence. World manager discovery is cached with Unity destroyed-object validation; missing/replaced instances retry at most once per ten seconds. World selection baseline checks remain in place.

Validation: new cache regression test failed for missing helper, then passed 1,000 steady-state accesses with one discovery, throttled missing-world discovery and replacement. Existing CollectorTests pass. Release build: zero errors/warnings. All 20 hook signatures verified against installed game assembly. Live performance comparison remains pending. No AP, Tamer, save or ledger changes.
