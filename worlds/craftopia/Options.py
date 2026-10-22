"""Craftopia world options (Archipelago 0.6.8 API).

Full gameplay design: configurable victory, check categories, starting
options, and multiplayer modes.
"""
from dataclasses import dataclass

from Options import Toggle, Choice, Range, PerGameCommonOptions


# --- Victory conditions (independent; at least one must be enabled) ---

class GoalBoss(Toggle):
    """Require defeating the configured boss for victory.
    
    NOT YET SUPPORTED: No boss locations are generated. This option will
    cause generation to fail until the boss catalog is implemented.
    """
    display_name = "Goal: Boss"
    default = False


class GoalBossChoice(Choice):
    """Which boss must be defeated (if GoalBoss enabled)."""
    display_name = "Goal Boss"
    # Values populated from verified boss catalog; placeholder options.
    # The generator validates against the actual catalog.
    option_any_final_boss = 0
    default = 0


class GoalAge(Toggle):
    """Require reaching the configured age for victory."""
    display_name = "Goal: Age"
    default = False


class GoalAgeTarget(Range):
    """Target age (if GoalAge enabled). Age indices are game-defined."""
    display_name = "Goal Age Target"
    range_start = 1
    range_end = 10
    default = 5


class GoalObjectives(Toggle):
    """Require completing N distinct objectives for victory."""
    display_name = "Goal: Objective Count"
    default = True


class GoalObjectiveCount(Range):
    """Number of distinct objectives required (if GoalObjectives enabled)."""
    display_name = "Goal Objective Count"
    range_start = 1
    range_end = 200
    default = 30


# --- Starting options ---

class StartingCombat(Choice):
    """Starting combat category."""
    display_name = "Starting Combat Category"
    option_player_choice = 0  # Player selects at game start
    option_random_category = 1  # "random" is reserved by Archipelago's Choice type.
    option_all_basic = 2      # All basic categories unlocked
    default = 0


# --- Check category toggles (all enabled by default) ---

class EnableMissionChecks(Toggle):
    """Enable mission/achievement completion checks."""
    display_name = "Enable Mission Checks"
    default = True


class EnableAgeChecks(Toggle):
    """Enable age advancement checks."""
    display_name = "Enable Age Checks"
    default = True


class EnableSoulOrbChecks(Toggle):
    """Reserved for soul orb replacement. Unavailable until durable currency delivery is implemented."""
    display_name = "Enable Soul Orb Checks"
    default = False


class EnableChestChecks(Toggle):
    """Enable overworld chest checks."""
    display_name = "Enable Chest Checks"
    default = True


class EnableChallengeChecks(Toggle):
    """Enable challenge reward checks."""
    display_name = "Enable Challenge Checks"
    default = True


class EnableAnubisChecks(Toggle):
    """Enable Anubis award checks."""
    display_name = "Enable Anubis Checks"
    default = True


class EnableShopChecks(Toggle):
    """Enable one-time shop purchase checks."""
    display_name = "Enable Shop Checks"
    default = True


class EnableBossChecks(Toggle):
    """Enable boss/dungeon milestone checks."""
    display_name = "Enable Boss Checks"
    default = True


# --- Multiplayer ---

class MultiplayerMode(Choice):
    """How multiple players share the Craftopia world."""
    display_name = "Multiplayer Mode"
    option_separate_slots = 0  # Each player has their own AP slot
    option_shared_slot = 1     # One AP slot for the whole team
    default = 0


# --- Rewards ---

class SkillPointsPerCheck(Range):
    """How many skill points each check grants (shared-slot: per team)."""
    display_name = "Skill Points Per Check"
    range_start = 1
    range_end = 5
    default = 1


class MissionCatalogVersion(Choice):
    """Mission catalog version for instance-based ID mapping.

    - legacy: Class-based mapping (existing seeds, 53 locations).
    - 20261007_r1: Instance-based mapping from runtime evidence
      (125 observed missions, AP IDs 1000+). Requires new seed;
      do not mix with legacy seeds.
    """
    display_name = "Mission Catalog Version"
    option_legacy = 0
    option_20261007_r1 = 1
    default = 0


class LogicMode(Choice):
    """Candidate retains experimental flat logic. Verified requires reviewed
    prerequisites for every active mission; currently unavailable and rejected.
    Opening is a separate 11-mission first-age pilot, requiring instance mode.
    A captured recipe catalog alone does not establish beatability.
    """
    display_name = "Progression Logic"
    option_candidate = 0
    option_verified = 1
    option_opening = 2
    # Full captured mission pool with partial, explicitly experimental logic.
    option_expedition = 3
    option_geezah_pilot = 4
    option_regional = 5
    default = 0


class EndingRegion(Choice):
    """Regional runs end after the selected region's tower and main dungeon.
    Geezah through Brigandine Valley endings are available.
    Later-region combat and completion behavior still need targeted live validation.
    Only used with regional progression logic.
    """
    display_name = "Ending Region"
    option_geezah = 0
    option_millewind = 1
    option_yarden_mountain = 2
    option_yarden_ruin = 3
    option_owatatsu = 4
    option_brigandine = 5
    default = 0


@dataclass
class CraftopiaOptions(PerGameCommonOptions):
    # Victory (at least one must be enabled; validated at generation)
    goal_boss: GoalBoss
    goal_boss_choice: GoalBossChoice
    goal_age: GoalAge
    goal_age_target: GoalAgeTarget
    goal_objectives: GoalObjectives
    goal_objective_count: GoalObjectiveCount
    # Starting
    starting_combat: StartingCombat
    # Check categories
    enable_mission_checks: EnableMissionChecks
    # Catalog version (instance-based mapping is opt-in)
    mission_catalog_version: MissionCatalogVersion
    logic_mode: LogicMode
    ending_region: EndingRegion
    enable_age_checks: EnableAgeChecks
    enable_soul_orb_checks: EnableSoulOrbChecks
    enable_chest_checks: EnableChestChecks
    enable_challenge_checks: EnableChallengeChecks
    enable_anubis_checks: EnableAnubisChecks
    enable_shop_checks: EnableShopChecks
    enable_boss_checks: EnableBossChecks
    # Multiplayer
    multiplayer_mode: MultiplayerMode
    # Rewards
    skill_points_per_check: SkillPointsPerCheck
