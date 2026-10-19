# In-game connection and character setup

Open the Field Guide with **F8**, then select **Connection**.

1. At the main menu, enter the Archipelago server address and port, your exact slot name, and the password if the room uses one.
2. Choose a starting weapon preference if your seed permits choosing one. Seeds with fixed starting equipment keep their own rules.
3. Select **Connect**. The mod starts its included bridge in the background and reports the server's response.
4. Create and load a fresh character and world. Open Connection again and select **Start this seed with this character**, then confirm.

The mod checks the character and world again when you confirm. No F9 capture, configuration editing, or enrollment restart is needed. Later visits to the same seed reuse the saved binding; connect at the main menu before loading the character. Select another seed or slot only at the main menu. The expanded guide pauses single-player gameplay in v1.15.0; multiplayer worlds remain live.

Server and slot are remembered. Passwords are masked; **Remember password** is optional and stores Windows-user-protected data rather than plaintext. Turning it off removes the saved password. Passwords are not passed on command lines or written to bridge logs.

Disconnecting does not remove progression restrictions, reset deliveries, or erase enrollment. A different character, progressed world, conflicting saved history, or incomplete write cannot be made fresh through this screen. Keep the original session files for recovery if a hold appears.

## Distribution

The Windows package includes `CraftopiaArchipelago.dll` and a sibling `bridge` directory with its isolated CPython runtime, bridge client, and matching catalogs. No separate Python installation or manually launched bridge is needed. Preserve that directory layout. Runtime provenance, hashes, and redistribution licenses are included.

For development, `Connection.PythonExecutable` can point to an absolute Python executable path. Normal players leave it blank. `Connection.Managed=false` retains the earlier manually launched bridge workflow and its original files; managed sessions use separate identities and do not automatically migrate existing manual sessions.

Build the portable bridge using the AP Python environment:

```text
python tools/package_managed_bridge.py --overwrite --verify
```

The owner confirmed connection, enrollment and returning after restart without re-enrollment. Map and pause behavior added in v1.15.0 still awaits live acceptance. Automated tests cover handshake/refusal, credential handling, profile isolation, repeated confirmation, persisted approval, and stale character/world snapshots.
