> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# Craftopia Skill Tree Catalog — Static Metadata Analysis

**Source:** `mod/refs/AD__Overcraft.dll`
**Method:** Python `dnfile` metadata inspection only. Assembly was NOT executed.
**Date:** 2026-10-06
**Assembly hash (SHA-256):** `88db99ebe9fbf1296c74465b6f32467cdbc71711235f925532d9bcf6f2ccebea`

---

## 1. Skill Tree Definitions

### Core manager: `Oc.Skills.OcSkillManager`
- **Base type:** `SingletonMonoBehaviour<OcSkillManager>` (Unity MonoBehaviour singleton)
- **Key fields:**
  - `Oc.SoSkillDataList skillList` — the skill catalog (ScriptableObject)
  - `Oc.Skills.SoSkillLockData skillLockData` — lock conditions
  - `int[,] requiredSkillPoint` — 2D array: required SP per (category, tier)
  - `int[,] assignedSkillPoint` — 2D array: assigned SP per (category, tier)
  - `int[] reqSP_Basic`, `reqSP_Combat`, `reqSP_Magic`, `reqSP_Life`, `reqSP_Trick`, `reqSP_Tamer` — per-tier SP thresholds per category
  - `List<OcSkillLockCondition_Boss> _bossKillRequiredSkill`

### Skill catalog: `Oc.SoSkillDataList`
- **Base type:** `UnityEngine.ScriptableObject`
- **Fields:** `Oc.Skills.OcSkill[] all`
- **Methods:**
  - `OcSkill[] GetAll()`
  - `OcSkill[] GetAll(OcPlSkillCategory, OcPlSkillTier)`
  - `OcSkill[] GetImplemented()`
  - `OcSkill[] GetImplemented(OcPlSkillCategory, OcPlSkillTier)`
  - `OcSkill GetById(int)`
  - `OcSkill GetById_OrNull(int)`

### Per-skill: `Oc.Skills.OcSkill`
- **Base type:** `UnityEngine.ScriptableObject`
- **Key fields:**
  - `int category` (backing field for `OcPlSkillCategory Category` property)
  - `int tier` (backing field for `OcPlSkillTier Tier` property)
  - `int maxLevel` (max 6, per `OcPlSkillLevel`)
  - `int levelUpCost_lv1` through `levelUpCost_lv6` — SP cost per level
  - `Oc.Item.ItemData skillItem`
  - `ISkillLockCondition _skillLockCondition` — prerequisites
  - `bool _preLocked`
  - `Oc.Skills.SkillSaveData _ssData` — runtime state
  - `bool allowAll`, `allowBare`, `allow1HSword`, `allow2HSword`, `allowShield`, `allowBow`, `allowLance`, `allowMagicStaff`, `allowKatana`, `allowDualSword`, `allowGun` — weapon restrictions

---

## 2. Individual Skills

### Active skills: `Oc.OcPlActiveSkillType` (enum)
- **168 skills** (No01 through No168), values 1–168, sequential
- Plus `None=0` and `Max=169` (170 total entries)
- **Underlying type:** 1-byte (values stored as single bytes)

**Complete list:**
| # | Name | # | Name | # | Name |
|---|------|---|------|---|------|
| 1 | ComboStrike | 57 | CompanionPlants | 113 | PetHealOwner |
| 2 | Thunder | 58 | Tuneup | 114 | PetHealParty |
| 3 | PetAttack | 59 | WarSong | 115 | Roar |
| 4 | Taunt | 60 | NutrientAbsorption | 116 | JumpShock |
| 5 | Generic_Berserk | 61 | ExplosionSignal | 117 | DualShockWave |
| 6 | CureCond | 62 | Tipping | 118 | 2HSwordDebuff |
| 7 | Rampage | 63 | MultiCraft | 119 | LancePull |
| 8 | ShieldRun | 64 | LargeScaleDevelopment | 120 | DualMidDamage |
| 9 | JumpFallStrike | 65 | Fertilize | 121 | DualPenetrate |
| 10 | BowSniping | 66 | Generic_GenerateWater | 122 | KTBackStep |
| 11 | MultiWay | 67 | GenerateCampfire | 123 | Lance360 |
| 12 | StormArrow | 68 | Cyclone | 124 | 2HSwordDebuffDmg |
| 13 | RapidFire | 69 | AreaHeal | 125 | KTCharge360 |
| 14 | Guillotine | 70 | MagicBlade | 126 | Lance120AoE |
| 15 | MoveSpeedUp | 71 | GreatEscape | 127 | KTAvoidSlash |
| 16 | Stealth | 72 | LightningFairy | 128 | DualHeavyDamage |
| 17 | Resurrection | 73 | DecoyDoll | 129 | 2HSwordHeavyDmg |
| 18 | FakeDeath | 74 | Warp | 130 | LanceAccumulateDamage |
| 19 | Siphone | 75 | Necromance | 131 | QuickReload |
| 20 | Heal | 76 | MagicShield | 132 | StandingAim |
| 21 | Blaze | 77 | StormRun | 133 | HighGripHold |
| 22 | HitMoney | 78 | UnlimitedBlade | 134 | FMJ |
| 23 | PetBomb | 79 | ChaoticDevelopment | 135 | HP |
| 24 | IceBeam | 80 | HyperResurrection | 136 | SpreadShot |
| 25 | IaiCombo | 81 | NativeAlchemy | 137 | MetalJet |
| 26 | Slash | 82 | FreshAlchemy | 138 | BackStepShot |
| 27 | Meteor | 83 | RunicRapidFire | 139 | BackStepGrenade |
| 28 | WindEdge | 84 | SwordSmallCombo | 140 | BackStepBarrel |
| 29 | SwordDance | 85 | FullSwing | 141 | FireBall |
| 30 | SpearCharge | 86 | Fasting | 142 | Holy |
| 31 | TrackingStrike | 87 | LifeSiphon | 143 | FireStorm |
| 32 | Explosion | 88 | Mantra | 144 | FireBlade |
| 33 | IceCrystal | 89 | Encourage | 145 | Driftwood |
| 34 | IaiSlash | 90 | Prayer | 146 | Satyros |
| 35 | TimeStop | 91 | Miasma | 147 | Reaper |
| 36 | Generic_GeneratePotion | 92 | GravityField | 148 | LizardGunner |
| 37 | GatherersFrenzy | 93 | Protection | 149 | Griffon |
| 38 | Generic_NoHunger | 94 | Lance_MultiStab | 150 | NobleFrog |
| 39 | Generic_NoArrowConsumption | 95 | PetInactiveWaitCommand | 151 | BoneDragon |
| 40 | Generic_GenerateMeat | 96 | PetActiveFollowCommand | 152 | Fenrir |
| 41 | Generic_BackwaterFormation | 97 | PetInactiveFollowCommand | 153 | KingMono |
| 42 | DUMMY | 98 | HealPet | 154 | LizardExec |
| 43 | Werewolf | 99 | CheerPet | 155 | Dryad |
| 44 | TrickBox | 100 | ResurrectPet | 156 | SandDragon |
| 45 | SunBlessing | 101 | OrderOfCourser | 157 | Worm |
| 46 | SelfExplosion | 102 | ReturnOrder | 158 | DemonDragon |
| 47 | Levitating | 103 | ArmyOfTheKing | 159 | StructuralMechanics |
| 48 | Giant | 104 | SupportInstructions | 160 | Dragon |
| 49 | LuckOfFish | 105 | PetPosionAttack | 161 | WoodCutter |
| 50 | Sharpen | 106 | PetHeal | 162 | MagicDrill |
| 51 | JudgementShot | 107 | StrongAtk | 163 | SnakeGiant |
| 52 | FlashStrike | 108 | AoEAtk | 164 | WormPoison |
| 53 | ExecutionStrike | 109 | ParalysisAttack | 165 | Hydra |
| 54 | ComboStrike | 110 | ChillAttack | 166 | WhaleDesart |
| 55 | Excalibur | 111 | StunAttack | 167 | DarkAnubis |
| 56 | SacrificeDinner | 112 | PartyBuff | 168 | Goddess |

### Passive skills: `Oc.OcPlPassiveSkillType` (enum)
- **112 skills** (No01 through No112), values 1–112, sequential
- Plus `None=0` and `Max=113` (114 total entries)

**Complete list:**
| # | Name | # | Name |
|---|------|---|------|
| 1 | GenMP | 57 | FastWalk |
| 2 | ReduceMP | 58 | Endurance |
| 3 | Shield | 59 | SpinStrike |
| 4 | Dual | 60 | ChargeStrike |
| 5 | TwoHand | 61 | StrengthenBash |
| 6 | Parry | 62 | StrongChargeShot |
| 7 | AirJump | 63 | Counter |
| 8 | JumpArchery | 64 | KatanaMastery |
| 9 | MartialArts | 65 | RuneStream |
| 10 | Forester | 66 | SkilledAttacker |
| 11 | BareForage | 67 | BackStab |
| 12 | Crouch_NoHit | 68 | Forester2 |
| 13 | Jump_NoHit | 69 | Woodchopper |
| 14 | EarnMoneyFromBuilding | 70 | StoneMiner |
| 15 | HealOnAttackEnemy | 71 | GoldRush |
| 16 | FastSwim | 72 | Dummy |
| 17 | LastManStanding | 73 | MiracleOfNature |
| 18 | StrongHeadshot | 74 | LuckyDiver |
| 19 | HigherCritProb | 75 | TreasureHunter |
| 20 | HigherCritDmg | 76 | AntiFlamingDamage |
| 21 | EfficientAxe | 77 | SecretIngredient |
| 22 | EfficientPickle | 78 | Contamination |
| 23 | PerfectLander | 79 | DecreaseCraftingTime |
| 24 | TwinFish | 80 | CommercialMind |
| 25 | Thrifty | 81 | DecreaseItemCoolDown |
| 26 | LightEater | 82 | VehicleDurability |
| 27 | Gourmet | 83 | MartialArtsWithNoHelm |
| 28 | HungryBeast | 84 | MartialArtsWithNoBody |
| 29 | DeathInsurance | 85 | HellResistance |
| 30 | EfficientSwim | 86 | SneakAttack |
| 31 | AirDash | 87 | SkilledRepairer |
| 32 | Coordination | 88 | GatheringLife |
| 33 | HealOnBuff | 89 | ManaBlessing |
| 34 | MPHealOnMagicGuard | 90 | MagicGrasp |
| 35 | Driver | 91 | FastCast |
| 36 | SkillCooldownReduction | 92 | CarpetBombing |
| 37 | FishAnywhere | 93 | AntiMagmaDamage |
| 38 | Carnivore | 94 | IncreasePetExSmall |
| 39 | EvasionUpOnEvade | 95 | CaptureMaster |
| 40 | EvasionUpOnDash | 96 | Veterinarian |
| 41 | ThrowBuilding | 97 | IncreasePetAtkSmall |
| 42 | HealOnGlider | 98 | IncreasePetHpSmall |
| 43 | EfficientGlider | 99 | IncreasePetExLarge |
| 44 | DefUpOnGlider | 100 | IncreasePetAtkLarge |
| 45 | EfficientHillclimbJump | 101 | IncreasePetHpLarge |
| 46 | EfficientWallJump | 102 | IncreaseMaxNumOfPet1 |
| 47 | GreatJump | 103 | IncreaseMaxNumOfPet2 |
| 48 | Gluttony | 104 | CredentialOfRider |
| 49 | EnemyDropRate | 105 | Sleep |
| 50 | Dummy_50 | 106 | PetAtkUp |
| 51 | MPRegeneration | 107 | PetDefUp |
| 52 | HPRegeneration | 108 | PetLifeUp |
| 53 | BiggerInventory | 109 | ShieldDebuff |
| 54 | ItemDropRate | 110 | TonboSlash |
| 55 | BetterJump | 111 | BonusNeutralizer |
| 56 | Sturdy | 112 | AncientGolem |

**Total skills:** 280 (168 active + 112 passive)

---

## 3. Skill Categories

### `Oc.OcPlSkillCategory` (enum, values verified)
| Value | Name | Notes |
|-------|------|-------|
| 0 | None | — |
| 1 | Basic | — |
| 2 | Combat | — |
| 3 | Magic | — |
| 4 | Life | — |
| 5 | Trick | — |
| 6 | Tamer | Pet-related skills |
| 7 | PetOnly | Pet-only skills (not for player) |
| 8 | Max | Sentinel |

### Category distinction
- Categories are distinguished by the `OcPlSkillCategory` enum value on each `OcSkill.category` field.
- The `OcSkillManager` tracks SP separately per category via `reqSP_Basic`, `reqSP_Combat`, `reqSP_Magic`, `reqSP_Life`, `reqSP_Trick`, `reqSP_Tamer` (each `int[]`, indexed by tier).
- **UI:** `Oc.Skills.OcUI_SkillTree` has `skillTree_Category` (`OcUI_SkillTree_Category[]`) and `tamerTabGray` (Tamer tab can be greyed/locked).

### Tiers: `Oc.OcPlSkillTier` (enum, values verified)
| Value | Name |
|-------|------|
| 0 | None |
| 1 | Tier1 |
| 2 | Tier2 |
| 3 | Tier3 |
| 4 | Tier4 |
| 5 | Tier5 |
| 6 | Max |

### Levels: `Oc.OcPlSkillLevel` (enum, values verified)
| Value | Name |
|-------|------|
| 0 | None |
| 1–6 | Lv1–Lv6 |
| 7 | Max |

---

## 4. `OcPlSkillCtrl` — Skill Point API

**Class:** `Oc.OcPlSkillCtrl` (extends `System.Object`; plain class held by player)

### Skill point methods (signatures decoded from metadata)
```
int get_SkillPoint()
void set_SkillPoint(int value)
void SetSkillPoint(int value)
void AddSkillPoint(int value)        // <-- the verified grant method
void CostSkillPoint(int value)
```

### Skill level methods
```
int getPassiveSkillLevel(Oc.OcPlPassiveSkillType type)
int getActiveSkillLevel(Oc.OcPlActiveSkillType type, bool unknown)
void setPassiveSkill(Oc.OcPlPassiveSkillType type, int level)
void setActiveSkill(Oc.OcPlActiveSkillType type, int level)
int GetEquipmentSkillLevel(Oc.OcPlPassiveSkillType type)  // overload
int GetEquipmentSkillLevel(Oc.OcPlActiveSkillType type)   // overload
void ResetAllSkills()
bool CanUseActiveSkill()
bool TryUseActiveSkill()
```

### Per-skill value getters
The class has ~500 `get_*` methods for per-skill parameters, e.g.:
- `float get_Thunder_Dmg()`, `int get_PetAttack_Count()`, `float get_PetAttack_AtkRate()`
- `float get_Heal_Rate()`, `float get_Blaze_MotDmg()`, etc.
- Pattern: `GetValue(int[], OcPlActiveSkillType)`, `GetIntValue(int[], OcPlActiveSkillType)`

### Key fields
- `int _SkillPoint` — unspent skill points
- `int[] _PassiveSkillLv` — passive skill levels (indexed by enum value)
- `int[] _ActiveSkillLv` — active skill levels (indexed by enum value)
- `Oc.OcPl _Pl` — owning player reference

---

## 5. Skill Prerequisites

### Lock condition types (`Oc.Skills.ISkillLockCondition`)
| Class | Prerequisite field | Meaning |
|-------|-------------------|---------|
| `OcSkillLockCondition_Skill` | `int _preSkillId` | Requires another skill learned |
| `OcSkillLockCondition_Boss` | `OcEmType emType` | Requires boss kill |
| `OcSkillLockCondition_Item` | `int _itemId` | Requires item possession |
| `OcSkillLockCondition_Mission` | `int _missionId` | Requires mission completion |
| `OcSkillLockCondition_Quest` | `int _questId` | Requires quest completion |

### How prerequisites attach
- `OcSkill._skillLockCondition` (`ISkillLockCondition`) — set via `SetSkillLockCondition()`
- `OcSkill._preLocked` (bool)
- `OcSkill.get_IsLocked()`, `get_ConditionLocked()`, `get_ConditionText()` — query lock state
- `SoSkillLockData._conditionList` — master list of conditions

### Tier gating
- `OcSkillManager.CanAssignSP(OcPlSkillCategory, OcPlSkillTier)` → bool
- `OcSkillManager.CanAssignSPByCategory(OcPlSkillCategory, OcPlSkillTier)` → bool
- `OcSkillManager.GetGreatestTierLearned(OcPlSkillCategory)` → `OcPlSkillTier`
- Per-category per-tier SP thresholds in `reqSP_*` arrays

---

## 6. Key Manager APIs (decoded signatures)

### `Oc.Skills.OcSkillManager`
```
void GameStartCheckSkillPoint()                                    // the validator
void GameStartCheckSkillUnlock()
void BossSkillUnlock()
bool TryLearnSkill(int, int, bool)
bool TryLearnSkillByMission(OcSkill)
bool TryAssignSkillPoint(OcSkill, int, bool)
bool TryDecreaseSkillLevel(OcSkill, byref int)
OcSkill GetSkill(int id)
OcSkill[] GetSkills(OcPlSkillCategory, OcPlSkillTier)
List<OcSkill> GetAllLearnedSkills()
List<OcSkill> GetAllActiveSkill()
bool get_IsAnySPAssigned()
int get_CurrentAssignedSP()
int GetCurrentAssignedSP(OcPlSkillCategory, OcPlSkillTier)
int GetCurrentAssignedSPByCategory(OcPlSkillCategory, OcPlSkillTier)
bool CanAssignSP(OcPlSkillCategory, OcPlSkillTier)
bool CanAssignSPByCategory(OcPlSkillCategory, OcPlSkillTier)
OcPlSkillTier GetGreatestTierLearned(OcPlSkillCategory)
void LearnAllSkillsToMax()
void ResetAllSkills()
void UnlockSkillTree()
```

### `Oc.Skills.OcSkill`
```
int get_ID()
OcPlSkillCategory get_Category()
OcPlSkillTier get_Tier()
OcPlActiveSkillType get_activeSkillType()
OcPlPassiveSkillType get_passiveSkillType()
string get_SkillName()
int get_MaxLevel()
int get_Level() / void set_Level(int)
int get_AssignedSP()
int GetLevelUpCost(int level)
void Init() / void Init(OcSkill)
bool get_IsLocked() / get_IsLearned() / get_IsMaxedOut()
void ApplySp(int)
void TryLevelUp()
void Reset()
SkillSaveData get_SaveData() / void set_SaveData(SkillSaveData)
```

### `Oc.Skills.SkillSaveData` (save format)
```
int id          // skill ID (enum value)
int level       // current level (0-6)
int assignedSP  // SP invested
```

---

## 7. What's NOT Determinable Statically

The following require runtime evidence (game running, save inspection, or asset extraction):

1. **Per-skill category/tier assignments.** The `OcSkill.category` and `OcSkill.tier` values are populated from the `SoSkillDataList` ScriptableObject asset at runtime. The DLL contains the data structures but not the asset data. **Cannot determine which of the 280 skills belongs to which category/tier without runtime inspection.**

2. **Per-skill lock conditions.** The `_skillLockCondition` for each skill is configured in the ScriptableObject asset. **Cannot determine prerequisites statically.**

3. **SP costs per skill.** `levelUpCost_lv1` through `levelUpCost_lv6` are per-skill asset data. **Not in DLL.**

4. **Tier unlock thresholds.** The `reqSP_*` arrays are populated at runtime (likely from asset or code). **Values not in DLL metadata.**

5. **Skill IDs.** `OcSkill.get_ID()` return values — likely match enum values but **not verified statically**.

6. **Which skills are "implemented".** `GetImplemented()` filters the list; the `IsImplemented` flag is asset data. **Not determinable statically.**

7. **`Oc.SoSkill` (balance parameters).** Contains ~400 fields of per-skill tuning values (damage, ranges, etc.). These are field *definitions*; **actual values are in the asset, not the DLL.**

8. **Mission/quest/boss IDs for lock conditions.** The `_missionId`, `_questId`, `emType` values per skill are asset data.

---

## 8. Specific Runtime Evidence Needed

To complete the catalog for randomizer design:

1. **Dump `SoSkillDataList.all` at runtime:** For each of the 280 skills, record: `ID`, `Category`, `Tier`, `MaxLevel`, `levelUpCost_lv1..6`, `IsImplemented`, lock condition type + ID.

2. **Dump `reqSP_*` arrays:** The per-category per-tier SP thresholds that gate tier unlocks.

3. **Dump `skillLockData.ConditionList`:** All lock conditions with their parameters.

4. **Verify skill ID ↔ enum mapping:** Confirm `OcSkill.ID` equals the `OcPlActiveSkillType`/`OcPlPassiveSkillType` enum value.

5. **Check `GetGreatestTierLearned` behavior:** How tier progression unlocks work in practice.

6. **Tamer category:** Which skills are in Tamer (6) vs PetOnly (7), and how the Tamer tab lock (`tamerTabGray`) works.

---

## Summary for Randomizer Design

| Aspect | Status |
|--------|--------|
| Total skills | ✅ 280 verified (168 active + 112 passive) |
| Skill names | ✅ All 280 enum names extracted |
| Categories | ✅ 8 categories with numeric values |
| Tiers | ✅ 5 tiers with numeric values |
| Levels | ✅ 6 levels with numeric values |
| Skill point API | ✅ `AddSkillPoint(int)` confirmed |
| Prerequisite types | ✅ 5 lock condition types identified |
| Per-skill category/tier | ❌ Requires runtime dump |
| Per-skill costs | ❌ Requires runtime dump |
| Per-skill prerequisites | ❌ Requires runtime dump |
| Tier unlock thresholds | ❌ Requires runtime dump |

