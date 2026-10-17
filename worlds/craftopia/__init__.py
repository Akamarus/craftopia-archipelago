"""Craftopia Archipelago world (0.6.8).

Full gameplay design:
- Locations: missions, ages, soul orbs, chests, challenges, Anubis awards,
  shops, boss milestones. Each category independently toggleable.
- Items: progressive combat tiers, production unlocks, skill trees,
  skill points, filler bundles, Victory.
- Victory: configurable boss + age + objective count (any nonempty
  combination; ALL enabled conditions required).
- Multiplayer: separate slots or shared team slot.

ID mappings are initialized at class definition time (module level), not
in stage_assert_generate, so the AutoWorld metaclass sees them.
"""
from typing import Dict, List

from BaseClasses import Tutorial, CollectionState
from worlds.AutoWorld import World, WebWorld

from .Data import build_item_table, build_location_table, load_mission_data
from .Items import CraftopiaItem, CraftopiaLocation, ITEM_CLASSIFICATION
from .Options import CraftopiaOptions
from .Progression import ProgressionModel, load_reviewed_profile


class CraftopiaWebWorld(WebWorld):
    theme = "ocean"
    tutorials = [Tutorial(
        "Multiworld Setup Guide",
        "A guide to setting up the Craftopia randomizer.",
        "English",
        "setup_en.md",
        "setup/en",
        ["CraftopiaDev"],
    )]


# Module-level ID mappings, initialized at import time.
_item_table = build_item_table()
_missions = load_mission_data()
_location_table = build_location_table(_missions)

# Instance-based catalog (runtime evidence, versioned).
# Loaded lazily to avoid import cost when not used.
_instance_location_table = None
def _get_instance_table():
    global _instance_location_table
    if _instance_location_table is None:
        from .mission_id_mapper import get_id_map
        id_map = get_id_map()
        # Build location table entries for instance mode.
        # Each mission gets its frozen AP ID and name.
        _instance_location_table = {}
        for mission_id, info in id_map.items():
            _instance_location_table[info["ap_location_name"]] = info["ap_location_id"]
    return _instance_location_table

_item_name_to_id = {n: d.ap_id for n, d in _item_table.items()}
_location_name_to_id = {n: d.ap_id for n, d in _location_table.items()}
# Merge instance IDs (1000+) into the module-level table.
# These are stable and don't collide with legacy 1-53.
_instance_ids = _get_instance_table()
_location_name_to_id.update(_instance_ids)
from .SoulOrbs import load_catalog as _load_orbs, validate_catalog as _validate_orbs
for _orb in _validate_orbs(_load_orbs()):
    if _orb['ap_location_id'] in _location_name_to_id.values() or _orb['ap_location_name'] in _location_name_to_id:
        raise ValueError('Soul orb location ID/name collision')
    _location_name_to_id[_orb['ap_location_name']] = _orb['ap_location_id']
from .Milestones import load_catalog as _load_milestones, validate_catalog as _validate_milestones, key as _milestone_key
for _row in _validate_milestones(_load_milestones()):
    if _row['ap_location_id'] in _location_name_to_id.values() or _row['ap_location_name'] in _location_name_to_id:
        raise ValueError('Milestone location ID/name collision')
    _location_name_to_id[_row['ap_location_name']] = _row['ap_location_id']
_class_to_location = {d.mission_class: n for n, d in _location_table.items()}
_game_id_to_location = {d.game_id: n for n, d in _location_table.items()
                        if d.game_id is not None}
# Add numeric mission ID -> AP location name for instance mode.
# (Bridge uses this via slot_data game_id_to_location.)
from .mission_id_mapper import get_id_map as _get_id_map
for _mid, _info in _get_id_map().items():
    _game_id_to_location[_mid] = _info["ap_location_name"]
    _game_id_to_location[str(_mid)] = _info["ap_location_name"]


class CraftopiaWorld(World):
    """Archipelago world for Craftopia (Pocketpair)."""
    game = "Craftopia"
    web = CraftopiaWebWorld()
    options_dataclass = CraftopiaOptions
    options: CraftopiaOptions

    item_name_to_id = _item_name_to_id
    location_name_to_id = _location_name_to_id
    class_to_location = _class_to_location
    game_id_to_location = _game_id_to_location

    def __init__(self, multiworld, player: int):
        super().__init__(multiworld, player)
        self.item_table = _item_table
        self.location_table = _location_table
        self.progression_model = None
        self.opening_profile = None
        self.regional_profile = None

    def selected_soul_orbs(self):
        if self.options.logic_mode.value == 5:
            from .RegionalRuns import geezah_orbs
            return geezah_orbs()
        return _validate_orbs(_load_orbs())

    def generate_early(self):
        if self.options.logic_mode.value in (4, 5):
            if (self.multiworld.players != 1 or self.options.multiplayer_mode.value != 0 or
                    self.options.mission_catalog_version.value != 1 or not self.options.enable_mission_checks.value or
                    not self.options.enable_age_checks.value or not self.options.enable_boss_checks.value):
                raise ValueError('Regional runs require one player, instance missions, age and dungeon checks')
            if self.options.logic_mode.value == 5:
                from .RegionalRuns import build_run_profile
                self.regional_profile = build_run_profile(self.options.ending_region.current_key, self.item_table)
            else:
                from .Regional import load_profile, build_regional_profile
                self.regional_profile = build_regional_profile(load_profile(),self.item_table)
            self.opening_profile = self.regional_profile
            self.progression_model = ProgressionModel(self.opening_profile,self.item_table,allow_experimental=True)
        if self.options.enable_soul_orb_checks.value and self.options.logic_mode.value not in (3, 5):
            raise ValueError('Soul Orbs currently require the experimental expedition profile; traversal is unverified.')
        if self.options.logic_mode.value == 3:
            if self.options.mission_catalog_version.value != 1 or self.options.multiplayer_mode.value != 0:
                raise ValueError('Expedition requires instance missions and separate slots')
            from .Expedition import build_profile
            self.opening_profile = build_profile(self.item_table)
            self.progression_model = ProgressionModel(self.opening_profile, self.item_table,
                                                      allow_experimental=True)
        if self.options.logic_mode.value == 2:
            if self.options.mission_catalog_version.value != 1 or self.options.multiplayer_mode.value != 0:
                raise ValueError('Opening profile requires instance missions and separate slots')
            import json
            from pathlib import Path
            self.opening_profile = json.loads(Path(__file__).with_name('opening_20261014_r1.json').read_text())
            self.progression_model = ProgressionModel(self.opening_profile, self.item_table)
        if self.options.logic_mode.value == 1:
            if self.options.mission_catalog_version.value != 1:
                raise ValueError('Verified progression requires numeric mission instances')
            self.progression_model = ProgressionModel(load_reviewed_profile(), self.item_table)

    def create_item(self, name: str) -> CraftopiaItem:
        if name == "Completed Objective":
            return CraftopiaItem(name, ITEM_CLASSIFICATION["progression"], None, self.player)
        idef = self.item_table[name]
        return CraftopiaItem(
            name,
            ITEM_CLASSIFICATION[idef.classification],
            idef.ap_id,
            self.player,
        )

    def create_regions(self):
        from BaseClasses import Region
        menu = Region("Menu", self.player, self.multiworld)
        self.multiworld.regions.append(menu)

        overworld = Region("Overworld", self.player, self.multiworld)
        self.multiworld.regions.append(overworld)
        menu.connect(overworld)

        # Generate locations for each enabled category.
        # Missions are the verified category; others are gated by options
        # and will be populated as their catalogs are verified.
        #
        # Catalog version determines the location set:
        # - legacy (0): class-based, 53 locations (existing seeds).
        # - 20261007_r1 (1): instance-based, 125 locations from runtime evidence.
        use_instance = (self.options.mission_catalog_version.value == 1)
        if self.options.enable_mission_checks.value:
            if use_instance:
                # Instance mode: 125 distinct missions from runtime catalog.
                from .mission_id_mapper import get_id_map
                id_map = get_id_map()
                for mission_id in sorted(id_map.keys()):
                    if self.opening_profile is not None and str(mission_id) not in self.opening_profile['missions']:
                        continue
                    info = id_map[mission_id]
                    loc_name = info["ap_location_name"]
                    loc = CraftopiaLocation(
                        self.player,
                        loc_name,
                        info["ap_location_id"],
                        overworld,
                    )
                    loc.game_id = mission_id
                    loc.mission_class = info["class"]
                    loc.check_category = "mission"
                    loc.catalog_version = "20261007_r1"
                    overworld.locations.append(loc)
                    event = CraftopiaLocation(self.player, "Objective: " + loc_name, None, overworld)
                    event.access_rule = lambda state, check=loc: check.can_reach(state)
                    event.place_locked_item(self.create_item("Completed Objective"))
                    overworld.locations.append(event)
            else:
                # Legacy mode: class-based, 53 locations.
                for loc_name, loc_def in self.location_table.items():
                    loc = CraftopiaLocation(
                    self.player,
                    loc_name,
                    loc_def.ap_id,
                    overworld,
                    )
                    loc.game_id = loc_def.game_id
                    loc.mission_class = loc_def.mission_class
                    loc.check_category = "mission"
                    overworld.locations.append(loc)
                    # A local logic event models completing this mission type;
                    # it is independent of the randomized reward at its check.
                    event = CraftopiaLocation(self.player, "Objective: " + loc_name, None, overworld)
                    event.access_rule = lambda state, check=loc: check.can_reach(state)
                    event.place_locked_item(self.create_item("Completed Objective"))
                    overworld.locations.append(event)

        if self.options.enable_soul_orb_checks.value:
            for row in self.selected_soul_orbs():
                loc=CraftopiaLocation(self.player,row['ap_location_name'],row['ap_location_id'],overworld)
                loc.check_category='soul_orb'
                loc.orb_guid=row['guid']
                overworld.locations.append(loc)
            # Observed positions only: no verified traversal rules. This is why
            # the option is restricted to the explicitly experimental profile.

        if getattr(self, 'regional_profile', None) is not None:
            for row in self.regional_profile['milestones']:
                loc=CraftopiaLocation(self.player,row['ap_location_name'],row['ap_location_id'],overworld)
                loc.check_category='milestone'
                overworld.locations.append(loc)

        # TODO: chest, challenge, Anubis, shop
        # locations as their catalogs are verified. Each will set
        # loc.check_category accordingly.

    def create_items(self):
        from BaseClasses import Region
        try:
            overworld = self.multiworld.get_region("Overworld", self.player)
        except KeyError:
            return
        locations = [loc.name for loc in overworld.locations if loc.address is not None]
        if not locations:
            return

        # Validate victory configuration: at least one goal must be enabled.
        if getattr(self, 'regional_profile', None) is None and not (self.options.goal_boss.value or
                self.options.goal_age.value or
                self.options.goal_objectives.value):
            raise Exception(
                "Craftopia: at least one victory condition must be enabled "
                "(Goal Boss, Goal Age, or Goal Objective Count).")

        # Item pool: progression items + skill points + filler.
        # Skill tree unlocks: 8 items (Magic, Life, Trick, Tamer, Combat T2-T5).
        # Combat tiers must be in order: T2 before T3 before T4 before T5.
        skill_tree_items = [
            "Skill Tree: Magic",
            "Skill Tree: Life",
            "Skill Tree: Trick",
            "Skill Tree: Tamer",
            "Skill Tree: Combat Tier 2",
            "Skill Tree: Combat Tier 3",
            "Skill Tree: Combat Tier 4",
            "Skill Tree: Combat Tier 5",
        ]
        if self.opening_profile is not None:
            pool = self.opening_profile['pool']
            currency=['Soul Orb Currency'] * sum(getattr(l,'check_category',None)=='soul_orb' for l in overworld.locations)
            if len(pool)+len(currency) > len(locations): raise ValueError('Opening pool exceeds location count')
            skill_points = 0
            for name in pool + currency + ['Skill Point'] * (len(locations) - len(pool) - len(currency)):
                item = self.create_item(name)
                if self.options.logic_mode.value == 5 and name == 'Skill Point':
                    skill_points += 1
                    # Tamer needs three points and the added Heal route needs one. Surplus still
                    # grants points, but may fill AP-excluded locations.
                    if skill_points > 4:
                        item.classification = ITEM_CLASSIFICATION['filler']
                self.multiworld.itempool.append(item)
            return
        for item_name in skill_tree_items:
            self.multiworld.itempool.append(self.create_item(item_name))

        # Production unlocks: 10 binary groups.
        production_items = [
            "Production: smelting",
            "Production: cooking",
            "Production: farming",
            "Production: livestock_fishing",
            "Production: alchemy",
            "Production: recycling",
            "Production: water_processing",
            "Production: equipment_care",
            "Production: automation",
            "Production: creature_combining",
        ]
        for item_name in production_items:
            self.multiworld.itempool.append(self.create_item(item_name))

        # Remaining locations: skill points only.
        # Bundles are EXCLUDED until the complete delivery path exists
        # (structured item/quantity contract + mod transactional handler).
        # See REVIEW_FOR_MUSE.md section 3.
        n_progression = len(skill_tree_items) + len(production_items)
        n_remaining = len(locations) - n_progression
        if n_remaining < 0:
            raise Exception(
                f"Craftopia: not enough locations ({len(locations)}) for "
                f"progression items ({n_progression}).")
        # Fill remaining with Skill Points (the verified grant mechanism).
        for i in range(n_remaining):
            self.multiworld.itempool.append(self.create_item("Skill Point"))

    def set_rules(self):
        try:
            overworld = self.multiworld.get_region("Overworld", self.player)
        except KeyError:
            return
        locations = [loc for loc in overworld.locations if loc.address is not None and getattr(loc,'check_category',None)=='mission']
        n_locations = len(locations)

        model = getattr(self, 'progression_model', None)
        if model is not None:
            model.require_coverage([loc.game_id for loc in locations])
            # Attach rules to actual network locations. Their local objective
            # events already delegate to these locations' reachability.
            for loc in locations:
                loc.access_rule = lambda state, mid=loc.game_id: model.can_complete(
                    mid, lambda name: state.count(name, self.player))

        if n_locations == 0:
            raise Exception(
                "Craftopia: zero locations generated. Enable at least one "
                "check category to generate a playable seed.")

        if getattr(self, 'regional_profile', None) is not None:
            from .RegionalRuns import apply_early_placement_rules
            profile=self.regional_profile
            milestone_model=ProgressionModel({**profile,'missions':profile['milestone_rules']},
                                             self.item_table,allow_experimental=True)
            network=[loc for loc in overworld.locations if loc.address is not None]
            for loc in network:
                if loc.check_category=='milestone':
                    loc.access_rule=lambda state,mid=loc.address: milestone_model.can_complete(
                        mid,lambda name: state.count(name,self.player))
            from worlds.generic.Rules import add_item_rule
            for loc in network:
                if loc.check_category == 'soul_orb':
                    # Field membership is verified, individual traversal is not.
                    # Optional exploration cannot hold an essential reward.
                    add_item_rule(loc, lambda item: not item.advancement)
                    loc.access_rule = lambda state: model.can_complete(67,
                        lambda name: state.count(name, self.player))
            apply_early_placement_rules(self, network, profile)
            goals=[loc for loc in network if loc.address in profile['goal_location_ids']]
            if not goals or len(goals) != len(profile['goal_location_ids']):
                raise ValueError('Regional victory requires every selected goal location')
            self.multiworld.completion_condition[self.player]=lambda state: all(loc.can_reach(state) for loc in goals)
            return

        # Validate victory configuration: at least one goal must be enabled.
        if not (self.options.goal_boss.value or
                self.options.goal_age.value or
                self.options.goal_objectives.value):
            raise Exception(
                "Craftopia: at least one victory condition must be enabled "
                "(Goal Boss, Goal Age, or Goal Objective Count).")

        # Completion is event-based, not item-based.
        # The bridge tracks actual boss/age/objective events and sends
        # StatusUpdate when ALL enabled conditions are met.
        # The Victory item is placed for AP protocol compliance, but
        # possession alone does not satisfy the predicate here either.
        #
        # For the AP `completion_condition` (used by the generator to verify
        # beatability), we model the actual event dependencies:
        #
        # - Boss goal: Requires a boss location to exist. Currently no boss
        #   locations are generated (catalog pending). Reject if enabled.
        # - Age goal: Requires an age location to exist. Currently none.
        #   Reject if enabled.
        # - Objective goal: Requires N distinct checkable locations.
        #
        # The bridge enforces the event-based predicate at runtime.
        # See docs/GOAL_EVENT_CONTRACT.md.

        if self.options.goal_boss.value:
            # No boss locations are generated yet. The boss catalog and
            # detection hooks are pending runtime verification.
            raise Exception(
                "Craftopia: Goal Boss is not yet supported (no boss locations "
                "generated). Disable Goal Boss or wait for boss catalog.")

        if self.options.goal_age.value:
            # No age locations are generated yet.
            raise Exception(
                "Craftopia: Goal Age is not yet supported (no age locations "
                "generated). Disable Goal Age or wait for age system.")

        if self.options.goal_objectives.value:
            required = self.options.goal_objective_count.value
            achievable = n_locations  # Each location is a potential objective
            if required > achievable:
                raise Exception(
                    f"Craftopia: goal objective count ({required}) exceeds "
                    f"achievable locations ({achievable}). Reduce the count "
                    f"or enable more check categories.")

        # Local event count matches the runtime objective catalog.
        required = self.options.goal_objective_count.value
        self.multiworld.completion_condition[self.player] = \
            lambda state: state.has("Completed Objective", self.player, required)

    def get_filler_item_name(self) -> str:
        return "Skill Point"

    def fill_slot_data(self) -> Dict:
        """Data sent to the client on connect.

        Routing maps are MODE-SPECIFIC to prevent cross-catalog leaks:
        - legacy: class_to_location active; game_id_to_location only for
          legacy numeric IDs (no instance IDs).
        - instance: game_id_to_location maps numeric IDs to instance
          locations only; class_to_location is empty (no class fallback).
        """
        is_instance = (self.options.mission_catalog_version.value == 1)
        is_regional = getattr(self, 'regional_profile', None) is not None
        is_expedition = getattr(getattr(self.options, 'logic_mode', None), 'value', 0) in (3,4,5)
        is_regional_run = getattr(getattr(self.options, 'logic_mode', None), 'value', 0) == 5
        if is_instance:
            # Instance mode: numeric IDs only, no class fallback.
            from .mission_id_mapper import get_id_map
            id_map = get_id_map()
            game_id_map = {}
            for mid, info in id_map.items():
                game_id_map[str(mid)] = info["ap_location_name"]
                game_id_map[mid] = info["ap_location_name"]
            class_map = {}  # Empty: no class fallback in instance mode.
        else:
            # Legacy mode: class-based; numeric fallback only for legacy IDs.
            # Filter out instance IDs (1000+) to prevent cross-catalog leak.
            class_map = self.class_to_location
            game_id_map = {
                k: v for k, v in self.game_id_to_location.items()
                if self.location_name_to_id.get(v, 0) < 1000
            }
        active_names = {loc.name for loc in self.multiworld.get_locations(self.player) if loc.address is not None}
        game_id_map = {key: name for key, name in game_id_map.items() if name in active_names}
        return {
            **({'milestone_schema':1,'milestone_catalog':'milestones_20261016_r1',
                'milestone_locations':{_milestone_key(r['family'],r['source_id'],r['stage']):r['ap_location_id']
                                       for r in self.regional_profile['milestones']},
                'goal_location_ids':self.regional_profile['goal_location_ids'],
                'selected_region':self.regional_profile.get('region','geezah')} if is_regional else {}),
            **({'regional_run_schema':1, 'ending_region':self.regional_profile['region'],
                'included_regions':self.regional_profile['included_regions'],
                'goal_policy':self.regional_profile['goal_policy'],
                'early_item_locations':self.regional_profile['early_item_locations']}
               if is_regional_run else {}),
            **({'soul_orb_catalog':'soul_orbs_20261015_r1','soul_orb_schema':1,
                'soul_orb_currency_protocol':1,'soul_orb_access':('regional_optional_unverified_traversal' if self.options.logic_mode.value == 5 else 'unverified_candidates'),
                'soul_orb_locations':{str(r['guid']):r['ap_location_id'] for r in self.selected_soul_orbs()}}
               if self.options.enable_soul_orb_checks.value else {}),
            **({'equipment_catalog': ('equipment_loot_20261016_r2'
                    if self.regional_profile.get('profile_id') in ('regional_geezah_20261016_r4', 'regional_geezah_20261016_r5')
                    else 'equipment_loot_20261016_r1') if is_regional_run else 'equipment_20261010_r1',
                'equipment_protocol': 2 if is_regional_run else 1,
                'progression_profile': self.opening_profile['profile_id']} if self.opening_profile else {}),
            **({'equipment_tier_steps': self.opening_profile['equipment_tier_steps'],
                'tamer_scope': 'vanilla', 'logic_evidence': 'partial_experimental'}
               if is_expedition else {}),
            "logic_mode": "regional" if is_regional_run else "geezah_pilot" if is_regional else "expedition" if is_expedition else "opening" if self.opening_profile else "verified" if getattr(self, 'progression_model', None) is not None else "candidate",
            "class_to_location": class_map,
            "game_id_to_location": game_id_map,
            "objective_locations": {
                name: f"location:{ap_id}"
                for name, ap_id in self.location_name_to_id.items()
                # Filter by active catalog: legacy (1-53) vs instance (1000+).
                if name in active_names and (
                    (self.options.mission_catalog_version.value == 1 and 1000 <= ap_id < 2000)
                    or (self.options.mission_catalog_version.value == 0 and ap_id < 1000)
                )
            } if self.options.enable_mission_checks.value else {},
            "skill_points_per_check": self.options.skill_points_per_check.value,
            # Victory configuration
            "goal_boss": self.options.goal_boss.value,
            "goal_boss_choice": self.options.goal_boss_choice.value,
            "goal_age": self.options.goal_age.value,
            "goal_age_target": self.options.goal_age_target.value,
            "goal_objectives": self.options.goal_objectives.value,
            "goal_objective_count": self.options.goal_objective_count.value,
            # Starting
            "starting_combat": self.options.starting_combat.value,
            # Check categories
            "enable_mission_checks": self.options.enable_mission_checks.value,
            "enable_age_checks": self.options.enable_age_checks.value,
            "enable_soul_orb_checks": self.options.enable_soul_orb_checks.value,
            "enable_chest_checks": self.options.enable_chest_checks.value,
            "enable_challenge_checks": self.options.enable_challenge_checks.value,
            "enable_anubis_checks": self.options.enable_anubis_checks.value,
            "enable_shop_checks": self.options.enable_shop_checks.value,
            "enable_boss_checks": self.options.enable_boss_checks.value,
            # Multiplayer
            "multiplayer_mode": self.options.multiplayer_mode.value,
            # Catalog version (bridge routing depends on this).
            # Schema is per-mode: legacy=0, instance=1.
            # (v1.7.0/v1.7.1 emitted schema=1 for legacy; the bridge
            # accepts that historical pair for compatibility.)
            "mission_catalog_version": (
                "20261007_r1"
                if self.options.mission_catalog_version.value == 1
                else "legacy"
            ),
            "mission_catalog_schema": (
                1 if self.options.mission_catalog_version.value == 1 else 0
            ),
        }
