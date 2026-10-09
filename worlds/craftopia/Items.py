"""Craftopia items and locations for Archipelago 0.6.8."""
from BaseClasses import Item, Location, ItemClassification


class CraftopiaItem(Item):
    game = "Craftopia"


class CraftopiaLocation(Location):
    game = "Craftopia"
    # game_id: the Mission.get_ID() this location corresponds to (may be None).
    # mission_class: the verified IL type name (stable key).
    game_id: int = None
    mission_class: str = ""


ITEM_CLASSIFICATION = {
    "progression": ItemClassification.progression,
    "useful": ItemClassification.useful,
    "filler": ItemClassification.filler,
    "trap": ItemClassification.trap,
}
