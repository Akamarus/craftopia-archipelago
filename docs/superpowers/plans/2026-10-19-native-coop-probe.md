# Native Co-op Probe Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Build a disposable, opt-in host/guest hello-and-echo diagnostic over Craftopia's existing Mirror connection.

**Architecture:** Separate BepInEx probe assembly, with a pure bounded protocol core and a thin Mirror adapter. Both endpoints explicitly enable the probe; only the guest initiates, and the host replies only to a valid probe hello. No AP or save mutations.

**Tech Stack:** C#, netstandard2.1 probe, net8.0 console tests, installed compile-only Mirror/Unity/BepInEx references.

**Spec:** `docs/superpowers/specs/2026-10-19-shared-world-multiplayer-design.md`, implementation sequence step 3. Evidence: `docs/COOPERATIVE_NETWORK_AUDIT.md`.

## Global Constraints

- No shared filesystem, manual bridge launch, or new port-forwarding requirement should be part of normal setup.
- Never transmit AP passwords in peer messages or log credentials.
- Preserve existing saves and AP records; use disposable cooperative characters/worlds for recovery tests.
- This probe does not enable production multiplayer, journal synchronization, enrollment, or item delivery.
- Existing single-player code and multiplayer refusal guards remain unchanged.

## Review Focus

- Unmodded peers: default-disabled probe sends nothing; explicitly armed guest sessions require a matching host probe.
- Occupied message ID: refuse registration without replacing the existing delegate.
- Reconnect/session change: discard old nonce and connection binding; never accept an old reply.
- Malformed payload: bounded fixed-size decoding, no attacker-controlled allocation or exception escaping callbacks.
- Host local client: never treat loopback success as a remote guest acceptance test.

### Task 1: Bounded diagnostic protocol

**Files:** Create `mod/CoopProbe/ProbeProtocol.cs`, `mod/CoopProbeTests/CoopProbeTests.csproj`, `mod/CoopProbeTests/Program.cs`.

**Interfaces:** `ProbeFrame(byte kind, ushort version, Guid nonce)`; `ProbeProtocol.Encode(ProbeFrame): byte[]`; `ProbeProtocol.TryDecode(byte[], out ProbeFrame): bool`. Exact wire size 23 bytes: four-byte ASCII CAPD marker, kind byte (1 hello, 2 echo), little-endian protocol ushort 1, 16 Guid bytes. Empty nonce invalid. This diagnostic protocol is not the production progress protocol.

- [x] Write tests: both kinds round-trip; lengths 0/22/24 rejected; wrong magic/version/kind and empty nonce rejected; input never mutated.
- [x] Run `dotnet run --project mod/CoopProbeTests` and verify missing implementation fails.
- [x] Implement the fixed-size codec; reject before allocating on invalid input.
- [x] Run tests and verify all pass; commit the task files.

### Task 2: Opt-in Mirror adapter

**Files:** Create `mod/CoopProbe/CoopProbe.csproj`, `mod/CoopProbe/ProbePlugin.cs`, `mod/CoopProbe/ProbeSession.cs`; extend `mod/CoopProbeTests/Program.cs` with linked pure session core tests.

**Interfaces:** `ProbeSession.Arm(Guid nonce)`, `ProbeSession.Reset()`, `ProbeSession.AcceptEcho(Guid nonce): bool`. Adapter owns actual connection reference, checks authentication before sending/handling, and resets on connection replacement or shutdown. Session core accepts an echo once only for the armed nonce.

- [x] Write failing tests for matching echo once, stale nonce, reset, replacement nonce, and unarmed rejection; run tests to confirm failure.
- [x] Implement session core and rerun tests.
- [x] Add a separate plugin, disabled by default, with config `Enabled=false` and `Initiate=false`. Require explicit enablement on both machines and Initiate on guest only. Send one hello per authenticated remote session, no retries/broadcast. Timeout after 10 seconds using unscaled time; log one concise result.
- [x] Use one namespaced Mirror message struct with explicit fixed-size reader/writer delegates. Verify actual payload length before decoding; handlers reject invalid content without invoking game mutation. Check both handler tables before installation; remember delegate ownership and refuse collisions. Register with authentication required. Poll lifecycle at most four times per second; never assign global network callbacks. Clear/re-register owned handlers after session teardown as necessary.
- [x] Host echoes only valid hellos from authenticated nonlocal connections, limited to one reply per connection per second. Log role, protocol, connection number and outcome only. Do not log address, device ID, authentication data, passwords or raw packets.
- [x] Build `dotnet build mod/CoopProbe/CoopProbe.csproj -c Release` against local private-false references. Run pure tests; inspect that probe has no AP bridge, receipt or save references. Commit.

### Task 3: Evidence and isolated live validation

**Files:** Create `docs/COOPERATIVE_PROBE.md`; update audit with measured results only.

- [x] Document installation/removal of the separate probe, config, matching endpoint requirement, and the risk of manually initiating against an unmodded host. Do not package it in the public release or Workshop update.
- [x] Run codec/session tests and build again; obtain independent branch review, resolving actionable findings.
- [ ] If two supported game clients are available, use disposable characters/worlds to verify disabled silence, guest hello/host echo, guest reconnect, host restart, and mismatched protocol rejection. Confirm no AP checks/items/saves are mutated by the probe. Record client versions, transport and outcomes.
- [x] If a second client is unavailable, mark live validation pending and request a host/guest session; do not claim network readiness from automated or host-loopback tests.
- [x] Record remaining production gates: safe capability discovery, enrollment identity and save acknowledgments. Commit documentation. Do not push/install/release production multiplayer as part of this plan.

## Scope review

This plan covers only the design's native connectivity probe. Durable journals and delivery are already implemented independently. Production integration and cooperative seed enablement follow after observed live transport results and resolution of identity/discovery gates. Execution remains inline with one independent final review, as previously selected.

Live execution is pending: user confirmed a second PC/player exists but is unavailable now. The build, automated verification, review, and private test package are complete; no installation or public distribution performed.
