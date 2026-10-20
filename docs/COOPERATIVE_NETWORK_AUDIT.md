# Native multiplayer transport audit

Inspected 2026-10-19 using Mono.Cecil metadata and IL reads of the installed game. No game assemblies were executed, no game was launched, and no installed files were changed. This establishes candidate APIs, not successful multiplayer operation.

## Evidence

Installed assemblies under `Craftopia_Data/Managed`:

| Assembly | SHA256 |
| --- | --- |
| Mirror.dll | 93F3F1C110B5A0B6A05B5DC5DAB76AF86E30396622E0DEBA6D56A5E5687E7878 |
| AD__Overcraft.dll | 88DB99EBE9FBF1296C74465B6F32467CDBC71711235F925532D9BCF6F2CCEBEA |

Local raw inspection output is in ignored `artifacts/multiplayer-network-audit/`. Do not distribute game DLLs with the probe.

## Findings and consequences

- Craftopia uses Mirror, with `UniRMI.CraftopiaNetworkManager` and `UniRMI.CraftopiaAuthenticator`. Existing AP code already references Mirror. Steam/EOS/UDP entry points exist on `Oc.Network.OcNetMng`; a Mirror-level adapter could potentially share those transports, subject to live tests.
- `NetworkServer.RegisterHandler<T>(Action<NetworkConnectionToClient,T>, bool)` and `NetworkClient.RegisterHandler<T>(Action<T>, bool)` support authentication-required handlers. Use `true`; do not intercept or alter game authentication.
- `NetworkConnection.isAuthenticated` is public. `OnConnectedEvent` alone is insufficient: it fires at transport connection, before completed authentication.
- `Reader<T>.read` and `Writer<T>.write` are public static delegates. Explicit serializers are possible without weaving game assemblies. `Channels.Reliable` is 0.
- Message IDs are the low 16 bits of a stable hash of the message type's full name. RegisterHandler replaces an occupied handler after a warning. Check both handler tables for collisions before registration; never replace an existing game/mod handler.
- Unknown message IDs are **not harmless**. Both client and server `OnTransportData` disconnect after `UnpackAndInvoke` fails. Do not broadcast a mod hello to arbitrary players. An initial disposable probe requires explicitly enabled matching installations on both endpoints. Production capability discovery remains unresolved.
- Client shutdown clears networking state. The UniRMI manager exposes start/stop server, client-connect, stop-client, and server-disconnect methods. Registration must survive a new session without duplicate hooks, and teardown must remove only handlers owned by this mod.
- `OcNetMng.OnDisconnectForServer` obtains `OcPlMaster` through `connection.identity.GetComponent<OcPlMaster>()`. This is evidence of connection-to-player-object association, not proof of persistent character ownership.
- `OcNetMng.GetDeviceId` reads `connection.authenticationData` as a string. Do not use it as a secret or assume it proves durable character ownership.
- AP's current `Plugin.GetCharacterGuid()` returns the GUID captured at load validation. Neither a connection ID nor `OcPl.GetPlayerId()` has been proven equivalent. Stable enrollment authentication must be established separately before accepting checks, claims, or grants.

## Next gate

Build an isolated, disabled-by-default hello/echo probe. It must not submit AP checks, deliver items, change saves, enroll characters, expose the bridge, or remove multiplayer guards. Verify connection/rejoin behavior with two supported game clients on disposable saves. Host loopback or simulated traffic alone does not establish native co-op readiness.

Before production integration, resolve safe capability discovery, durable enrollment ownership, reliable save acknowledgments, and payload batching for full offline progress snapshots. Keep multiplayer unsupported in public distribution until the design's live acceptance matrix passes.
