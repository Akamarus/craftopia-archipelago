# Private co-op connection test

This is a **diagnostic**, not multiplayer AP support. It sends a small hello from a guest and an echo from the host over Craftopia's existing connection. It does not connect to AP, enroll players, submit checks, grant items, or write game saves. It creates only its BepInEx configuration and log entries.

The plugin is disabled by default. Both PCs must use the same probe build and a compatible Craftopia/BepInEx installation. Do not initiate this test against a host that does not have the enabled probe: Craftopia may disconnect the guest for sending an unknown message. There is no automatic mod detection yet.

## Install on both PCs

1. Close Craftopia. Use your existing working BepInEx setup; do not install a second loader alongside a Workshop loader.
2. Put **only `CraftopiaCoopProbe.dll`** in a new `BepInEx/plugins/CraftopiaCoopProbe` folder in that installation. Keep the existing AP plugin unchanged.
3. Launch the game once, then close it. This creates `BepInEx/config/com.akamarus.craftopia.coopprobe.cfg` with the probe disabled.
4. On the host, set `[Probe]` values `Enabled = true` and `Initiate = false`.
5. On the guest, set `Enabled = true` and `Initiate = true`.
6. Restart both games. Use disposable characters and a disposable shared world, with AP disconnected for this transport-only test. Have the host open the world, then join normally from the guest.

No new address, port, AP password, shared folder, or separate bridge process is needed for the probe.

## What to look for

In each PC's BepInEx log:

- Guest: `guest protocol 1: hello sent`, followed by `guest protocol 1: echo accepted`.
- Host: `host protocol 1: echo sent to connection ...`.
- If there is no valid reply within ten seconds: `guest protocol 1: timed out; no retry`.
- A handler/serializer collision stops the probe without overwriting another mod's handler. An adapter failure also stops it. Do not treat either as a successful test.

It sends one hello per authenticated guest connection. Reconnecting creates a new nonce; late or duplicate echoes are ignored. The host never initiates, never broadcasts, and replies at most once per second per authenticated remote connection. Opening the game alone cannot prove host/guest connectivity.

## Live test record

Live validation is **pending**. The user has a second PC/player available later, not during this build. For that session, record both game/probe versions, the connection method (Steam/EOS/UDP), and both logs for:

| Test | Expected | Status |
| --- | --- | --- |
| Both probes disabled | Normal connection, no probe messages | Pending |
| Matching enabled probes | One hello and accepted echo | Pending |
| Guest leaves and rejoins | New hello/echo, no stale reply accepted | Pending |
| Host closes and restarts the world | Guest reconnects and succeeds again | Pending |
| Controlled protocol mismatch, both handlers installed | No accepted echo, guest times out | Pending |

The mismatch case needs a deliberately built test variant; don't send arbitrary messages or remove the host mod to simulate it. A malformed low-level Mirror packet can still disconnect a peer even though this probe validates its own fixed-size content.

Verify AP check/item totals remain unchanged. The game may save ordinary gameplay normally; the probe has no game-save APIs. Remove the probe after testing: close the game, remove its plugin folder, and optionally remove its named config file. Do not remove the AP mod or AP records.

## Developer verification

```
dotnet run --project mod/CoopProbeTests
dotnet build mod/CoopProbe/CoopProbe.csproj -c Release
```

The tests compile the protocol, session, and transport adapter against a small behavioral Mirror double. They check wire bytes, bounds, authentication gates, connection changes, timeouts, collisions, handler ownership, rate limiting, and adjacent frames in a batch. The plugin build compiles against the local real Mirror/BepInEx/Unity references, all marked `Private=false`. No game assemblies ship with the probe. These checks do not replace the two-client tests above.

Production integration still needs safe capability discovery, durable enrollment identity, save acknowledgments, and full AP synchronization/delivery acceptance. Existing multiplayer guards and public packages remain unchanged.

## Build validation — 2026-10-19

- Probe: 67 assertions passed, covering the pure codec/session and simulated adapter behavior.
- Existing cooperative progress/delivery: all 40 tests passed.
- Release build against real installed references: zero warnings and errors. The Mirror reference hash matches the audited installation.
- Independent review of `d8d4972..830e70c`: no actionable findings. Reviewer independently ran the 67 assertions and verified Mirror batching/serialization IL.
- Private ZIP contains only the probe DLL, these instructions, and its SHA256 checksum. Nothing has been installed, pushed, or published.

Implementation decisions: the adapter reads exactly 23 bytes from Mirror's shared batch reader, leaving following messages intact; requiring the entire reader to be 23 bytes would break batching. The adapter lives separately from the Unity plugin to exercise it with a behavioral double; that double can miss runtime differences, so live validation remains mandatory. Review deliberately leaves real Steam/EOS/UDP delivery, safe production mod discovery, persistent enrollment/save integration, and installed distribution behavior to their later gates. None is claimed as working here.
