> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# Craftopia Archipelago — Goal Event Contract

**Version:** 1
**Date:** 2026-10-06

## Overview

Victory requires ALL enabled conditions to be met with actual evidence.
No condition is satisfied by Victory item possession alone.

## Event Types

### Boss Defeat
```json
{
  "event_type": "boss_defeat",
  "boss_id": "string",  // Stable boss identifier from verified catalog
  "timestamp": "ISO-8601"
}
```

The `boss_id` must match `slot_data["goal_boss_choice"]` (or be the configured
boss). A generic "Victory" item does NOT satisfy a boss goal.

### Age Advancement
```json
{
  "event_type": "age_advance",
  "age_level": 5,  // Integer world level
  "timestamp": "ISO-8601"
}
```

The `age_level` must be >= `slot_data["goal_age_target"]`.

### Objective Completion
```json
{
  "event_type": "objective_complete",
  "objective_id": "string",  // Stable: "<category>:<class>:<game_id>"
  "timestamp": "ISO-8601"
}
```

Each distinct `objective_id` counts once. The bridge tracks the set of
completed objective IDs. The count must reach `slot_data["goal_objective_count"]`.

**NOT counted**: Received Skill Point items. A Skill Point is a reward, not
proof of objective completion.

## Slot Data (World → Bridge)

```json
{
  "goal_boss": true,
  "goal_boss_choice": "boss_id_or_0",
  "goal_age": false,
  "goal_age_target": 5,
  "goal_objectives": true,
  "goal_objective_count": 30
}
```

At least one of `goal_boss`, `goal_age`, `goal_objectives` must be true.
The generator rejects configurations with all false.

## Bridge State

The bridge maintains:
- `boss_defeated`: bool (with boss_id)
- `max_age_reached`: int
- `completed_objectives`: set of objective_id strings

## Completion Predicate

```python
def is_complete():
    if goal_boss and not boss_defeated:
        return False
    if goal_age and max_age_reached < goal_age_target:
        return False
    if goal_objectives and len(completed_objectives) < goal_objective_count:
        return False
    return True  # All enabled conditions met
```

**Critical**: If NO goals are enabled (invalid config), `is_complete()` returns
False. There is no `all([])` shortcut to victory.

## Event Persistence

All goal events are persisted to the ledger before StatusUpdate is sent.
On bridge restart, events are replayed to restore state.

## Mod → Bridge Messages

- `{"cmd": "boss_defeat", "boss_id": "..."}`
- `{"cmd": "age_advance", "age_level": N}`
- `{"cmd": "objective_complete", "objective_id": "...", "category": "..."}`

The mod sends these when it detects the actual game events.
The bridge validates and records them.

## Victory Item

The "Victory" item is placed on a goal location for AP protocol compliance,
but POSSESSION DOES NOT TRIGGER COMPLETION. The bridge sends StatusUpdate
only when the event-based predicate is satisfied.

