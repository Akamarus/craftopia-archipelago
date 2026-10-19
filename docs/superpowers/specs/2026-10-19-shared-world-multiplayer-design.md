# Shared-world multiplayer proposal

Status: approved. Transport-independent progress and delivery foundations are implemented and tested. Native networking, runtime adapters, and live co-op acceptance remain incomplete; multiplayer is not yet advertised as supported.

## Intended experience

Players install the same mod and join a normal Craftopia shared world. Support two explicit modes: one Archipelago slot for the team, or one slot per player. Keep existing single-player enrollment and delivery behavior working. No shared filesystem, manual bridge launch, or new port-forwarding requirement should be part of the normal setup.

Confirmed user decisions:

- Tower repairs, age advancements, main dungeon clears, and soul-orb collection are team accomplishments.
- One player collecting an orb credits every enrolled slot in separate-slot mode.
- Enrolled players who were offline receive permanent world credit upon reconnecting.
- Individual missions remain personal in separate-slot mode.

## Recommended architecture and alternatives

Use a host-authoritative team journal. The host validates reports from enrolled participants, persists accomplishments, and synchronizes them to peers. Each machine retains its loopback-only mod-to-bridge connection. Separate-slot bridges connect to their own AP slots; in shared-slot mode only the host bridge connects to AP, with participant updates relayed through the game session.

Prefer Craftopia's existing multiplayer transport for peer messages, subject to a read-only inspection and a disposable connectivity probe before production integration. Do not assume a particular RPC API exists. If no suitable supported transport can be verified, stop the transport phase and review an alternative rather than silently expose the local bridge to the network.

Alternatives considered: a network-accessible central bridge adds address, authentication, and firewall setup; AP-server storage alone does not establish membership of a particular Craftopia world or authenticate game-session reports. Neither is the default proposal.

## Identity and enrollment

Scope all records to AP seed/team, Craftopia world identity, cooperative group identity, mode, protocol version, and catalog identity. A seed name alone is insufficient. Bind each enrolled character to its permitted slot. Separate slots must be in the same AP multiworld and use compatible region endpoints and check catalogs for the initial release.

The host creates the group through the Field Guide. Joining players explicitly enroll their character and slot. A player returning on a different connection retains the same enrollment. Offline membership is durable and never expires with presence heartbeats. For the initial release, new characters cannot enroll after team progress starts; returning enrolled characters can join a progressed world. This avoids silently importing old progress into a new slot.

Show host, mode, participating slots, and synchronization status in the Field Guide. Shared-slot guests do not need the AP password. Seed/slot changes retain the main-menu-only restriction. Multiplayer gameplay remains live when the guide opens, with local camera/input capture handled separately.

## Check ownership

| Activity | Shared slot | Separate slots |
| --- | --- | --- |
| Soul orb | One team check | Corresponding check in every enrolled slot |
| Tower repair stage | One team check per stage | Corresponding check in every enrolled slot |
| Age advancement | One team check per age | Corresponding check in every enrolled slot |
| Main dungeon clear | One team check per dungeon | Corresponding check in every enrolled slot |
| Individual mission/category | Any participant can complete the team check once | Only that character's slot |

Separate-slot completion releases the item placed at each slot's location to its AP-assigned recipient, who may be another player or game. It does not guarantee a local item for every collector.

Persist source identities, not translated names: orb ID, tower ID plus stage, age ID, dungeon ID, or mission ID. Event keys combine group/world identity, event family, and source identity. Duplicate observations and reconnects cannot create another check. Only enrolled, validated reports enter the journal. Preserve existing verified detection hooks; do not infer dungeon completion from proximity or collection from disappearance.

Offline catch-up replays durable completed-source state into each slot's own catalog and outbound check queue. Do not cap correctness state to a recent-event history size. Never submit nonexistent locations. A catalog mismatch blocks synchronization with a clear explanation.

## Received items

Shared slot:

- Equipment, production, and skill unlocks synchronize to the team.
- Skill-point entitlements apply once to each enrolled character, with reconnect catch-up.
- Physical items and currency are delivered once to the team through an explicit claim by an enrolled character. Reserving a claim is persisted before dispatch; it remains assigned through disconnects and is not automatically reassigned after an ambiguous delivery.
- An AP received-item index plus session identity is the delivery key. Repeated packets do not create additional grants.

Separate slots retain personal unlocks and item delivery. Shared world ages do not grant missing personal AP production or equipment permissions. A teammate may use their own unlocked abilities to help the group; the initial implementation does not attempt to prohibit ordinary item trading.

Do not promise atomic exactly-once game inventory mutation across a crash. Extend the existing receipt/recovery mechanism: ambiguous grants enter recovery hold, with no blind retry or second claimant. Per-character skill entitlements likewise use durable acknowledgments rather than current unspent point balance.

## Goals and generation

Separate-slot victory evaluates each slot's configured requirements using its personal checks and synchronized team milestones. Shared-slot victory is reported once by the authoritative bridge. Region-limited seeds keep their existing location bounds.

Maintain single-player generation unchanged. Expose cooperative generation only after the required protocol and delivery paths pass validation; removing existing shared-mode refusal checks alone is not support. The first cooperative profile requires matching region scope and catalog versions. Progression logic must remain independently solvable for each separate slot, so it does not depend on an absent teammate's unlocks.

## Persistence and failure handling

Host writes precede acknowledgment. Peers retain accepted event identities and pending local reports. On reconnect, reconcile a versioned snapshot and pending reports. AP outages delay submission without losing world progress. Host loss pauses cooperative synchronization and shared-slot delivery; the first release requires the same host to return and does not automatically elect a replacement.

World/seed mismatch, stale authority, invalid source identity, unregistered character, incompatible catalog, corrupt journal, and unsupported protocol refuse mutation and display actionable status. Messages have explicit types, bounded payload sizes, and session identity checks. Never transmit AP passwords in peer messages or log credentials. Do not trust a player's display name as identity.

## Existing foundation and gaps

`client/craftopia_bridge.py` and `client/test_multiplayer.py` contain earlier shared-slot entitlement and event-sharing code. The separate-slot registry is local to a machine, keyed too broadly by seed, prunes presence, and retains only 500 events. It is not a cross-PC synchronization mechanism.

Modern currency, soul-orb, milestone, enrollment, and regional-generation paths have single-player assumptions or explicit shared-mode rejection. They must be integrated deliberately. Historical multiplayer documentation is reference material, not proof of native co-op support.

## Implementation sequence

1. Build transport-independent group identity, durable membership, accomplishment journal, deduplication, and per-slot catch-up. Test using separate directories to prevent accidental shared-filesystem dependence.
2. Extend receipt ownership and entitlement rules for shared slot; preserve separate-slot behavior and crash recovery.
3. Inspect native networking and verify host/guest identity, message delivery, disconnect, and rejoin with a disposable probe. Select concrete transport hooks only from observed APIs.
4. Integrate host/guest synchronization, existing detection hooks, enrollment UI, compatibility checks, and goal reporting.
5. Enable a compatible cooperative seed profile and test two actual game clients in both modes before distributing multiplayer support.

## Acceptance criteria

Automated tests must cover duplicate reports, simultaneous claims, cross-world/seed isolation, personal missions, offline catch-up beyond 500 events, host restart, AP outage, out-of-order/repeated messages, invalid membership, stale authority, catalog mismatch, and ambiguous-delivery holds. Existing single-player enrollment, orb, currency, milestone, goal, and installer checks must still pass.

Live acceptance requires two PCs or independently running supported game clients: host and guest collect an orb, perform each milestone family, complete a personal mission, receive unlocks and consumables, disconnect/rejoin, restart the host, and complete the goal. Verify separate-slot personal permissions and shared-slot one-time team delivery. Preserve existing saves and AP records; use disposable cooperative characters/worlds for destructive recovery cases. Automated simulations alone do not establish native co-op readiness.
