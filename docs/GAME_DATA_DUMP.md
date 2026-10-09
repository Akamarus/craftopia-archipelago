> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# Craftopia Game Data Dump (static IL inspection)

**Source:** `mod/refs/AD__Overcraft.dll`
**Method:** dnfile 0.18.0 metadata inspection only. No execution, no modification.
**Assembly SHA-256:** `88DB99EBE9FBF1296C74465B6F32467CDBC71711235F925532D9BCF6F2CCEBEA`
**Date:** 2026-10-06

All names and signatures below are exact as they appear in metadata.
Items marked **[NOT DETERMINABLE]** could not be resolved statically.

---

## 1. Concrete subclasses of `Oc.Missions.Mission`

**Count: 53 concrete mission classes** (not 142 — see note).

All extend `Oc.Missions.Mission`1<T>` or `Oc.Missions.SingletonMission`1<T>`
via generic instantiation (TypeSpec base). `Oc.Missions.Mission` itself is
abstract and extends `Oc.OcCsvScriptableObject`.

| # | Full type name |
|---|---|
| 1 | `Oc.Missions.AxeRepairMission` |
| 2 | `Oc.Missions.BossKillAscension` |
| 3 | `Oc.Missions.BossKillWorldLevel` |
| 4 | `Oc.Missions.BreadMission` |
| 5 | `Oc.Missions.BuildingMission` |
| 6 | `Oc.Missions.CategoryCollectMission` |
| 7 | `Oc.Missions.ChestRefineMaterialMission` |
| 8 | `Oc.Missions.ChestRefineStoneMission` |
| 9 | `Oc.Missions.ChestRefineTargetMission` |
| 10 | `Oc.Missions.ChestStorageMission` |
| 11 | `Oc.Missions.DiscoverDungeonMission` |
| 12 | `Oc.Missions.DismantlingMission` |
| 13 | `Oc.Missions.EatMission` |
| 14 | `Oc.Missions.EnchantFragmentMission` |
| 15 | `Oc.Missions.EnemyHitMission` |
| 16 | `Oc.Missions.EnemyHitRaceMission` |
| 17 | `Oc.Missions.EnemyKillMission` |
| 18 | `Oc.Missions.EnemyKillMission_withLevel` |
| 19 | `Oc.Missions.GliderWindZoneMission` |
| 20 | `Oc.Missions.GoToHellMission` |
| 21 | `Oc.Missions.HeadShotMission` |
| 22 | `Oc.Missions.HeadShotTargetMission` |
| 23 | `Oc.Missions.HervestMission` |
| 24 | `Oc.Missions.ItemCollectMission` |
| 25 | `Oc.Missions.ItemCollectRaceMission` |
| 26 | `Oc.Missions.ItemCostMission` |
| 27 | `Oc.Missions.ItemCraftMission` |
| 28 | `Oc.Missions.ItemEnchantMission` |
| 29 | `Oc.Missions.ItemEquipMission` |
| 30 | `Oc.Missions.ItemRefineMission` |
| 31 | `Oc.Missions.ItemSoldMission` |
| 32 | `Oc.Missions.KillRaceMission` |
| 33 | `Oc.Missions.LowHealthKillMission` |
| 34 | `Oc.Missions.ManaCostRaseMission` |
| 35 | `Oc.Missions.MapTransformMission` |
| 36 | `Oc.Missions.MissionCategroyClearMission` |
| 37 | `Oc.Missions.MissionPageMission` |
| 38 | `Oc.Missions.ObjectDestroyRaceMission` |
| 39 | `Oc.Missions.ObjectHitMission` |
| 40 | `Oc.Missions.OpenFullMapMission` |
| 41 | `Oc.Missions.PetMonoMission` |
| 42 | `Oc.Missions.PlayerBurnMission` |
| 43 | `Oc.Missions.RotateBuildingMission` |
| 44 | `Oc.Missions.SeedingMission` |
| 45 | `Oc.Missions.SkillHealMission` |
| 46 | `Oc.Missions.SkillLevelUpMission` |
| 47 | `Oc.Missions.SoulOrbExchangeMission` |
| 48 | `Oc.Missions.StaticalDataMission` |
| 49 | `Oc.Missions.StaticalDataRaceMission` |
| 50 | `Oc.Missions.TalkToAnubisMission` |
| 51 | `Oc.Missions.WateringMission` |
| 52 | `Oc.Missions.WeaponEnchantMission` |
| 53 | `Oc.Missions.WorldLevelUpMission` |

**Mission IDs:** **[NOT DETERMINABLE statically]** — Mission IDs are
data-driven, not per-class constants. The base class `Oc.Missions.Mission`
has an instance `int id` field and `public int get_ID()`. Zero of the 53
concrete subclasses define a static literal ID field. IDs are populated at
runtime from CSV/ScriptableObject data (`_msData`). There is no static
mission-ID-to-class mapping in IL.

**Note on "142":** The hook inventory's 142 does not match the 53 concrete
`Mission` subclasses in metadata. The difference is unresolved — it may
count `*Proc` handler classes, data-file entries, or another assembly.
The 53 above are the verifiable `Oc.Missions.Mission` derivatives.

**Related (not Mission subclasses):** `*Proc` classes (e.g.
`Oc.Missions.MissionItemCraftProc`, `Oc.Missions.MissionEnemyKill`) extend
`Oc.Missions.MissionProc` (abstract, extends `System.Object`) and implement
runtime progress handling.

**`Mission.TryGetReward`:** `public bool TryGetReward()` (0 params, bool return).
This is the hook target used by the PoC.

---

## 2. `Oc.OcEmType` enum (int → name)

Underlying type: `ushort`. 492 values. Complete list:

| Value | Name |
|-------|------|
| 0 | Bear |
| 1 | Cow |
| 2 | Deer |
| 3 | Elephant |
| 4 | LoneWolf |
| 5 | Worm |
| 6 | Seagull |
| 7 | Dragon |
| 8 | Lumberjack |
| 9 | Boar |
| 10 | Buffalo |
| 11 | Camel |
| 12 | Chicken |
| 13 | Crocodile |
| 14 | Fox |
| 15 | Sheep |
| 16 | Giraffe |
| 17 | Wolf |
| 18 | Storm_BigWolf |
| 19 | Storm_BigChicken |
| 20 | GoblinShaman |
| 21 | GoblinBird |
| 22 | Pig |
| 23 | GoblinHealer |
| 24 | GoblinWarrior |
| 25 | Barrel |
| 26 | Gorilla |
| 27 | Ride_Balloon |
| 28 | Ride_Hoverboard |
| 29 | Ride_Bike |
| 30 | Mono |
| 31 | Ride_Heli |
| 32 | Ride_Car |
| 33 | Ride_FrameWalker |
| 34 | Ride_Plane |
| 35 | Ride_Pz4 |
| 36 | Reaper |
| 37 | GolemAncient |
| 38 | GolemCrystal |
| 39 | GolemLava |
| 40 | GolemAdaman |
| 41 | GolemStone |
| 42 | Treant |
| 43 | Phoenix |
| 44 | Griffon |
| 45 | DragonBone |
| 46 | Kraken |
| 47 | SkeletonGreen |
| 48 | SkeletonOrange |
| 49 | SkeletonPurple |
| 50 | CircleTarget |
| 51 | IronBall |
| 52 | NPC_Mob |
| 53 | NPC_Bandit |
| 54 | NPC_Quest |
| 55 | NPC_Merchant_CropExchange |
| 56 | NPC_Reception |
| 57 | NPC_Reception_CH_Goal |
| 58 | NPC_Reception_CH_Golf |
| 59 | NPC_Reception_CH_Race |
| 60 | NPC_Reception_CH_Shoot |
| 61 | NPC_Mob_Anubis |
| 62 | Ride_Hoverboard_Wood |
| 63 | FixedArtillery_Fire |
| 64 | NPC_Mob_Child_01 |
| 65 | NPC_Mob_Child_02 |
| 66 | NPC_Mob_Man_01 |
| 67 | NPC_Mob_Man_02 |
| 68 | NPC_Mob_Man_03 |
| 69 | NPC_Mob_Man_04 |
| 70 | NPC_Mob_Man_05 |
| 71 | NPC_Mob_Man_06 |
| 72 | NPC_Mob_Man_07 |
| 73 | NPC_Mob_Man_08 |
| 74 | NPC_Mob_Man_09 |
| 75 | NPC_Mob_Man_10 |
| 76 | NPC_Mob_Woman_01 |
| 77 | NPC_Mob_Woman_02 |
| 78 | NPC_Mob_Woman_03 |
| 79 | NPC_Mob_Woman_04 |
| 80 | LizardAxe |
| 81 | LizardSword |
| 82 | NPC_Tutorial_Woman_01 |
| 83 | NPC_Tutorial_Woman_02 |
| 84 | NPC_Tutorial_Woman_03 |
| 85 | NPC_Tutorial_Man_04 |
| 86 | NPC_Tutorial_Man_05 |
| 87 | NPC_Tutorial_Man_06 |
| 88 | NPC_Tutorial_Man_07 |
| 89 | NPC_Tutorial_Man_08 |
| 90 | NPC_Tutorial_Man_09 |
| 91 | NPC_Tutorial_Man_10 |
| 92 | GoblinHunter |
| 93 | Werewolf |
| 94 | LizardExecutioner |
| 95 | MonoKing |
| 96 | MonoPrince |
| 97 | OrcWarrior |
| 98 | Cockatrice |
| 99 | TurtleGiant |
| 100 | TurtleAlligator |
| 101 | LizardGunner |
| 102 | LizardGunner_A |
| 103 | GoblinSoldier |
| 104 | GoblinSoldier_Red |
| 105 | GoblinSoldier_Sword |
| 106 | Fort_WhaleDesart_Ally |
| 107 | Bat |
| 108 | Cyclops |
| 109 | MonoRed |
| 110 | MonoYellow |
| 111 | MonoGreen |
| 112 | TreeRoot |
| 113 | GoblinKid |
| 114 | MonoPurple |
| 115 | DryadFlower |
| 116 | SnakeGiant |
| 117 | Penguin |
| 118 | BearWhite |
| 119 | TigerIce |
| 120 | JackOLantern |
| 121 | TurretMachineGun |
| 122 | Bee |
| 123 | BeeHive |
| 124 | ChickPhoenix |
| 125 | Cassowary |
| 126 | GriffonAutumnal |
| 127 | MonoMetal |
| 128 | CowBomb |
| 129 | NPC_Boss_AnubisDark |
| 130 | Sandbag |
| 131 | Scarecrow |
| 132 | Decoy |
| 133 | BallSnow |
| 134 | Firecracker |
| 135 | LizardAxe_G2 |
| 136 | GoblinShaman_G2 |
| 137 | LumberjackWild |
| 138 | Hydra |
| 139 | DisplayBrowser |
| 140 | DragonDemons |
| 141 | NPC_Reception_CH_Pin |
| 142 | Horse |
| 143 | NPC_Paladin |
| 144 | Cockatrice_G2 |
| 145 | Satyros |
| 146 | BallCannon |
| 147 | TurretMachineGunGuard_Ally |
| 148 | Ride_Raft |
| 149 | Ride_Boat |
| 150 | Ride_HoverBike |
| 151 | NPC_Mob_Ambassador_01 |
| 152 | NPC_Mob_Ambassador_02 |
| 153 | NPC_Mob_Ambassador_03 |
| 154 | NPC_Mob_Ambassador_04 |
| 155 | NPC_Mob_Ambassador_05 |
| 156 | NPC_Mob_Ambassador_06 |
| 157 | NPC_Mob_Ambassador_07 |
| 158 | NPC_Tutorial_Woman_05 |
| 159 | NPC_Quest_BossDrop |
| 160 | NPC_Quest_Enchant |
| 161 | NPC_Quest_Fish |
| 162 | NPC_Quest_NaturalResource |
| 163 | NPC_Quest_Treasure |
| 164 | NPC_Merchant |
| 165 | NPC_Event_01 |
| 166 | NPC_Event_02 |
| 167 | NPC_Event_03 |
| 168 | GoblinHunter_Titan |
| 169 | NPC_SoulOrb |
| 170 | BallTrap |
| 171 | TurretMachineGun_Ally |
| 172 | Ride_Car_Kart_Blue |
| 173 | Ride_Car_Kart_Red |
| 174 | Ride_Car_Kart_Yellow |
| 175 | Dummy_175 |
| 176 | Dummy_176 |
| 177 | Dummy_177 |
| 178 | NPC_Farmer_01 |
| 179 | NPC_Mob_Woman_05 |
| 180 | NPC_Mob_Woman_06 |
| 181 | NPC_Mob_Woman_07 |
| 182 | NPC_Mob_Woman_08 |
| 183 | NPC_Mob_Woman_09 |
| 184 | NPC_Mob_Woman_10 |
| 185 | GoblinBig_Shield |
| 186 | GoblinBig_Axe |
| 187 | Goblin_Sword |
| 188 | Goblin_Archer |
| 189 | Goblin_Nohand |
| 190 | GoblinBig_Shield_Light |
| 191 | GoblinBig_Axe_Light |
| 192 | Goblin_Sword_Light |
| 193 | Goblin_Archer_Light |
| 194 | Wizard |
| 195 | Gigantes_Archer |
| 196 | Gigantes_Halberd |
| 197 | Skeleton_1HAxeShield |
| 198 | Skeleton_Bow |
| 199 | Skeleton_Pirate_Captain |
| 200 | Skeleton_Pirate_Sailor |
| 201 | NPC_ExtractCrystal |
| 202 | Bat_Raid_Raid |
| 203 | GoblinBig_Shield_Raid |
| 204 | GoblinBig_Axe_Raid |
| 205 | Goblin_Sword_Raid |
| 206 | Goblin_Archer_Raid |
| 207 | Goblin_Nohand_Raid |
| 208 | NobleFrog |
| 209 | NPC_SubQuest_01 |
| 210 | NPC_SubQuest_02 |
| 211 | NPC_SubQuest_03 |
| 212 | NPC_SubQuest_04 |
| 213 | NPC_SubQuest_05 |
| 214 | NPC_SubQuest_06 |
| 215 | NPC_SubQuest_07 |
| 216 | NPC_SubQuest_08 |
| 217 | NPC_SubQuest_09 |
| 218 | NPC_SubQuest_10 |
| 219 | NPC_SubQuest_11 |
| 220 | NPC_SubQuest_12 |
| 221 | NPC_SubQuest_13 |
| 222 | NPC_SubQuest_14 |
| 223 | NPC_SubQuest_15 |
| 224 | NPC_SubQuest_16 |
| 225 | NPC_SubQuest_17 |
| 226 | NPC_SubQuest_18 |
| 227 | NPC_SubQuest_19 |
| 228 | NPC_SubQuest_20 |
| 229 | NPC_Boss_AnubisDark_True |
| 230 | WhaleDesart |
| 231 | Humanoid_Bandit_1HSword |
| 232 | Humanoid_Bandit_1HSword_Strong |
| 233 | Humanoid_Bandit_2HSword |
| 234 | Humanoid_Bandit_2HSword_Strong |
| 235 | Humanoid_Bandit_Lance |
| 236 | Humanoid_Bandit_Lance_Strong |
| 237 | Humanoid_Bandit_Archer |
| 238 | Humanoid_Bandit_Archer_Strong |
| 239 | Humanoid_Raider_1H |
| 240 | Humanoid_Raider_1H_Strong |
| 241 | Humanoid_Raider_2H |
| 242 | Humanoid_Raider_2H_Strong |
| 243 | Humanoid_Raider_Lance |
| 244 | Humanoid_Raider_Lance_Strong |
| 245 | Humanoid_Raider_Archer |
| 246 | Humanoid_Raider_Archer_Strong |
| 247 | Humanoid_Raider_Gun |
| 248 | Humanoid_Raider_Gun_Strong |
| 249 | Humanoid_Viking_1H |
| 250 | Humanoid_Viking_1H_Strong |
| 251 | Humanoid_Viking_Lance |
| 252 | Humanoid_Viking_Lance_Strong |
| 253 | Humanoid_Viking_Archer |
| 254 | Humanoid_Viking_Archer_Strong |
| 255 | Humanoid_Viking_2H |
| 256 | Humanoid_Viking_2H_Strong |
| 257 | NPC_ItemInherit |
| 258 | Wizard_Poison |
| 259 | Wizard_Thunder |
| 260 | Wizard_Ice |
| 261 | Wizard_Fire |
| 262 | Goblin_Sword_Poison |
| 263 | Goblin_Sword_Thunder |
| 264 | Goblin_Sword_Ice |
| 265 | Goblin_Sword_Fire |
| 266 | Goblin_Archer_Poison |
| 267 | Goblin_Archer_Thunder |
| 268 | Goblin_Archer_Ice |
| 269 | Goblin_Archer_Fire |
| 270 | Goblin_Nohand_Poison |
| 271 | Goblin_Nohand_Thunder |
| 272 | Goblin_Nohand_Ice |
| 273 | Goblin_Nohand_Fire |
| 274 | GoblinBig_Shield_Poison |
| 275 | GoblinBig_Shield_Thunder |
| 276 | GoblinBig_Shield_Ice |
| 277 | GoblinBig_Axe_Poison |
| 278 | GoblinBig_Axe_Thunder |
| 279 | GoblinBig_Axe_Ice |
| 280 | NPC_Scholar_MQuest01 |
| 281 | GoblinBig_Shield_Fire |
| 282 | GoblinBig_Axe_Fire |
| 283 | NPC_1_Guidance_01 |
| 284 | NPC_1_Guidance_02 |
| 285 | NPC_1_Guidance_03 |
| 286 | NPC_1_Guidance_04 |
| 287 | NPC_1_Guidance_05 |
| 288 | NPC_1_Guidance_06 |
| 289 | NPC_1_Guidance_07 |
| 290 | NPC_1_Guidance_08 |
| 291 | NPC_1_Guidance_09 |
| 292 | NPC_1_Guidance_10 |
| 293 | NPC_2_Guidance_01 |
| 294 | NPC_2_Guidance_02 |
| 295 | NPC_2_Guidance_03 |
| 296 | NPC_2_Guidance_04 |
| 297 | NPC_2_Guidance_05 |
| 298 | NPC_2_Guidance_06 |
| 299 | NPC_2_Guidance_07 |
| 300 | NPC_2_Guidance_08 |
| 301 | NPC_2_Guidance_09 |
| 302 | NPC_2_Guidance_10 |
| 303 | NPC_3_1_Guidance_01 |
| 304 | NPC_3_1_Guidance_02 |
| 305 | NPC_3_1_Guidance_03 |
| 306 | NPC_3_1_Guidance_04 |
| 307 | NPC_3_1_Guidance_05 |
| 308 | NPC_3_1_Guidance_06 |
| 309 | NPC_3_1_Guidance_07 |
| 310 | NPC_3_1_Guidance_08 |
| 311 | NPC_3_1_Guidance_09 |
| 312 | NPC_3_1_Guidance_10 |
| 313 | NPC_3_2_Guidance_01 |
| 314 | NPC_3_2_Guidance_02 |
| 315 | NPC_3_2_Guidance_03 |
| 316 | NPC_3_2_Guidance_04 |
| 317 | NPC_3_2_Guidance_05 |
| 318 | NPC_3_2_Guidance_06 |
| 319 | NPC_3_2_Guidance_07 |
| 320 | NPC_3_2_Guidance_08 |
| 321 | NPC_3_2_Guidance_09 |
| 322 | NPC_3_2_Guidance_10 |
| 323 | NPC_4_Guidance_01 |
| 324 | NPC_4_Guidance_02 |
| 325 | NPC_4_Guidance_03 |
| 326 | NPC_4_Guidance_04 |
| 327 | NPC_4_Guidance_05 |
| 328 | NPC_4_Guidance_06 |
| 329 | NPC_4_Guidance_07 |
| 330 | NPC_4_Guidance_08 |
| 331 | NPC_4_Guidance_09 |
| 332 | NPC_4_Guidance_10 |
| 333 | NPC_5_Guidance_01 |
| 334 | NPC_5_Guidance_02 |
| 335 | NPC_5_Guidance_03 |
| 336 | NPC_5_Guidance_04 |
| 337 | NPC_5_Guidance_05 |
| 338 | NPC_5_Guidance_06 |
| 339 | NPC_5_Guidance_07 |
| 340 | NPC_5_Guidance_08 |
| 341 | NPC_5_Guidance_09 |
| 342 | NPC_5_Guidance_10 |
| 343 | NPC_6_Guidance_01 |
| 344 | NPC_6_Guidance_02 |
| 345 | NPC_6_Guidance_03 |
| 346 | NPC_6_Guidance_04 |
| 347 | NPC_6_Guidance_05 |
| 348 | NPC_6_Guidance_06 |
| 349 | NPC_6_Guidance_07 |
| 350 | NPC_6_Guidance_08 |
| 351 | NPC_6_Guidance_09 |
| 352 | NPC_6_Guidance_10 |
| 353 | MonoIce |
| 354 | NPC_SubQuest_21 |
| 355 | NPC_SubQuest_22 |
| 356 | NPC_SubQuest_23 |
| 357 | NPC_SubQuest_24 |
| 358 | NPC_SubQuest_25 |
| 359 | NPC_SubQuest_26 |
| 360 | NPC_SubQuest_27 |
| 361 | NPC_SubQuest_28 |
| 362 | NPC_SubQuest_29 |
| 363 | NPC_SubQuest_30 |
| 364 | NPC_SubQuest_31 |
| 365 | NPC_SubQuest_32 |
| 366 | NPC_SubQuest_33 |
| 367 | NPC_SubQuest_34 |
| 368 | NPC_SubQuest_35 |
| 369 | NPC_SubQuest_36 |
| 370 | NPC_SubQuest_37 |
| 371 | NPC_SubQuest_38 |
| 372 | NPC_SubQuest_39 |
| 373 | NPC_SubQuest_40 |
| 374 | NPC_SubQuest_41 |
| 375 | NPC_SubQuest_42 |
| 376 | NPC_SubQuest_43 |
| 377 | NPC_SubQuest_44 |
| 378 | NPC_SubQuest_45 |
| 379 | NPC_SubQuest_46 |
| 380 | NPC_SubQuest_47 |
| 381 | NPC_SubQuest_48 |
| 382 | NPC_SubQuest_49 |
| 383 | NPC_SubQuest_50 |
| 384 | NPC_SubQuest_51 |
| 385 | NPC_SubQuest_52 |
| 386 | NPC_SubQuest_53 |
| 387 | NPC_SubQuest_54 |
| 388 | NPC_SubQuest_55 |
| 389 | NPC_SubQuest_56 |
| 390 | NPC_SubQuest_57 |
| 391 | NPC_SubQuest_58 |
| 392 | NPC_SubQuest_59 |
| 393 | NPC_SubQuest_60 |
| 394 | NPC_SubQuest_61 |
| 395 | NPC_SubQuest_62 |
| 396 | NPC_SubQuest_63 |
| 397 | NPC_SubQuest_64 |
| 398 | NPC_SubQuest_65 |
| 399 | NPC_SubQuest_66 |
| 400 | NPC_SubQuest_67 |
| 401 | NPC_SubQuest_68 |
| 402 | NPC_SubQuest_69 |
| 403 | NPC_SubQuest_70 |
| 404 | NPC_Scholar_MQuest02 |
| 405 | NPC_Scholar_MQuest03 |
| 406 | NPC_Scholar_MQuest04 |
| 407 | NPC_Scholar_MQuest05 |
| 408 | NPC_Scholar_MQuest06 |
| 409 | NPC_Scholar_MQuest07 |
| 410 | NPC_Scholar_MQuest08 |
| 411 | ride_MotocrossBike |
| 412 | Ride_PTBoat |
| 413 | Ride_AttackHeli |
| 414 | Frog |
| 415 | Ride_HoverboardShark |
| 416 | Ride_Robot |
| 417 | GolemStone_Goblin |
| 418 | SandDragon |
| 419 | Ride_RailCart |
| 420 | NPC_SubQuest_71 |
| 421 | NPC_SubQuest_72 |
| 422 | NPC_SubQuest_73 |
| 423 | NPC_SubQuest_74 |
| 424 | NPC_SubQuest_75 |
| 425 | NPC_SubQuest_76 |
| 426 | NPC_SubQuest_77 |
| 427 | NPC_SubQuest_78 |
| 428 | NPC_SubQuest_79 |
| 429 | NPC_SubQuest_80 |
| 430 | NPC_SubQuest_81 |
| 431 | NPC_SubQuest_82 |
| 432 | NPC_SubQuest_83 |
| 433 | NPC_SubQuest_84 |
| 434 | NPC_SubQuest_85 |
| 435 | NPC_SubQuest_86 |
| 436 | NPC_SubQuest_87 |
| 437 | NPC_SubQuest_88 |
| 438 | NPC_SubQuest_89 |
| 439 | NPC_SubQuest_90 |
| 440 | WormPoison |
| 441 | Watercraft_Goblin |
| 442 | Tank_Goblin |
| 443 | Aircraft_Goblin |
| 444 | C4Bomb |
| 445 | Flak_Goblin |
| 446 | Humanoid_Viking_Magician |
| 447 | Humanoid_Viking_Magician_Strong |
| 448 | Humanoid_Bandit_Magician |
| 449 | Humanoid_Bandit_Magician_Strong |
| 450 | Humanoid_Japanese_Sword |
| 451 | Humanoid_Japanese_Sword_Strong |
| 452 | Humanoid_Japanese_Ninja |
| 453 | Humanoid_Japanese_Ninja_Strong |
| 454 | Humanoid_Japanese_Lance |
| 455 | Humanoid_Japanese_Lance_Strong |
| 456 | BallonBomb |
| 457 | Monobis |
| 458 | Ride_Ballon_Mono |
| 459 | NPC_WhaleFortress |
| 460 | Whale_Mini |
| 461 | Ride_HoverCar |
| 462 | Ride_Missile |
| 463 | SprinkerRobo |
| 464 | SowingRobo |
| 465 | FarmingRobo |
| 466 | Ride_Tractor |
| 467 | Ride_Robot_Farm |
| 468 | NPC_WhaleFortress_Man_01 |
| 469 | NPC_SubQuest_91 |
| 470 | NPC_SubQuest_92 |
| 471 | NPC_SubQuest_93 |
| 472 | NPC_SubQuest_94 |
| 473 | NPC_SubQuest_95 |
| 474 | NPC_SubQuest_96 |
| 475 | NPC_SubQuest_97 |
| 476 | NPC_SubQuest_98 |
| 477 | NPC_SubQuest_99 |
| 478 | NPC_SubQuest_100 |
| 479 | NPC_Boss_Goddess_First |
| 480 | CampfireFlying |
| 481 | NPC_Boss_Goddess_Third |
| 482 | NPC_TalkableMono |
| 483 | FarmingRobo_FullAuto |
| 484 | NPC_TalkableMono_NormalWorld |
| 485 | Ride_Cultivator |
| 486 | TurretMachineGunGuard_Ally_Spawnable |
| 487 | Ride_CowBike |
| 488 | CultivatorRobo |
| 489 | MonoPlane |
| 490 | DragonCyber |
| 491 | Max |

**Boss-relevant entries** (for dungeon boss mapping): `NPC_Boss_AnubisDark` (129),
`NPC_Boss_AnubisDark_True` (229), `NPC_Boss_Goddess_First` (479),
`NPC_Boss_Goddess_Third` (481), `DragonDemons` (140), `Hydra` (138),
`MonoKing` (95), `Reaper` (36), `Kraken` (46), `Phoenix` (43).
Boss identity for AP mapping must be confirmed at runtime via
`OnDungeonBossKill(int)` — the int parameter's meaning (EmType vs dungeon ID)
is **[NOT DETERMINABLE]** statically.

---

## 3. `Oc.OcDungeonID`

`Oc.OcDungeonID` is a **struct** (extends `System.ValueType`), not an enum.
Single instance field: `int dungeonID`. No static constants, no enum values.

Related types:
- `Oc.DungeonIDGetter` — class with `int Get()` method and `_dungeonID` field
- `Oc.PosToDungeonIDGetter` — class

**Dungeon ID constants:** **[NOT DETERMINABLE]** — dungeon IDs are runtime
`int` values, not static constants in IL. Mapping dungeon IDs to names
requires runtime observation or data-file inspection.

---

## 4. `Oc.Achievements` — 17 types

| Full type name | Base |
|---|---|
| `Oc.Achievements.OcAchievement` | `Oc.OcCsvScriptableObject` |
| `Oc.Achievements.OcAchievementManager` | (singleton pattern) |
| `Oc.Achievements.OcAchievementManager.<>c` | `System.Object` (compiler-generated) |
| `Oc.Achievements.OcAchievementManager.<>c__DisplayClass11_0` | `System.Object` (compiler-generated) |
| `Oc.Achievements.OcAchievementManager.<>c__DisplayClass8_0` | `System.Object` (compiler-generated) |
| `Oc.Achievements.OcAchievementManager.<>c__DisplayClass8_1` | `System.Object` (compiler-generated) |
| `Oc.Achievements.OcAchievementSaveData` | `System.Object` |
| `Oc.Achievements.OcAchievement_BuildCount` | `Oc.Achievements.OcAchievement` |
| `Oc.Achievements.OcAchievement_DungeonClear` | `Oc.Achievements.OcAchievement` |
| `Oc.Achievements.OcAchievement_ItemCraftCount` | `Oc.Achievements.OcAchievement` |
| `Oc.Achievements.OcAchievement_ItemTookCount` | `Oc.Achievements.OcAchievement` |
| `Oc.Achievements.OcAchievement_KillCount` | `Oc.Achievements.OcAchievement` |
| `Oc.Achievements.OcAchievement_KillCount_WithLevel` | `Oc.Achievements.OcAchievement_KillCount` |
| `Oc.Achievements.OcAchievement_RestoreTower` | `Oc.Achievements.OcAchievement` |
| `Oc.Achievements.OcAchievement_TargetCount` | `Oc.Achievements.OcAchievement` |
| `Oc.Achievements.OcAchievement_ItemCraftCount.<>c` | `System.Object` (compiler-generated) |
| `Oc.Achievements.OcAchievement_ItemTookCount.<>c` | `System.Object` (compiler-generated) |

**7 functional achievement types** (excluding base, manager, save data,
compiler-generated):
`BuildCount`, `DungeonClear`, `ItemCraftCount`, `ItemTookCount`,
`KillCount` (incl. `WithLevel` variant), `RestoreTower`, `TargetCount`.

---

## 5. `Oc.OcPlSkillCtrl.AddSkillPoint` — confirmed

```csharp
public void AddSkillPoint(int)
public void SetSkillPoint(int)
```

Both are public instance methods on `Oc.OcPlSkillCtrl`. `AddSkillPoint(int)`
takes a single `int` (skill points to add) and returns `void`.

---

## 6. `Oc.Missions.OcMissionManager` — confirmed signatures

```csharp
public Oc.Missions.Mission[] GetMissionLists()
public System.Collections.Generic.IEnumerable<Oc.Missions.Mission> GetClearedMissions()
public Oc.Missions.Mission GetMission(int)
private void OnDungeonBossKill(int)
public int GetTotalSkillPointTaken()
```

**Important:** `OnDungeonBossKill(int)` is **private**, not public. External
callers (including Harmony patches) can still target it, but it is not part
of the public API. The meaning of its `int` parameter (EmType value vs
dungeon ID vs other) is **[NOT DETERMINABLE]** statically.

`TryGetReward` is **not** on `OcMissionManager` — it is
`public bool TryGetReward()` on the `Oc.Missions.Mission` base class.

---

## 7. Item ID candidates

- `Oc.OcItemID` — **class** (extends `System.Object`), not an enum.
  - `public int get_ItemID()`
  - `public Oc.Item.ItemData get_ItemData()`
  - `public string get_ItemName()`
  - `public int Get()`
  - Fields: `int _id`, `Oc.Item.ItemData _itemData`
- `Oc.Item.StItemID` — **[NOT FOUND]** in this assembly.
- No `ItemID` enum or static item-ID constants were found. Item IDs are
  **data-driven** (`int _id` populated at runtime).

**Grantable items:** **[NOT DETERMINABLE statically]** — beyond skill points
(`AddSkillPoint(int)`), no static item catalog exists in IL. Item grants
would require runtime `ItemData` construction or data-file IDs.

---

## Summary of static unknowns

| Item | Status |
|---|---|
| Per-class mission ID constants | None exist; IDs are data-driven via `Mission.id` |
| Mission ID → class mapping | Requires runtime CSV/data inspection |
| `OnDungeonBossKill(int)` parameter meaning | Requires runtime observation |
| Dungeon ID → name mapping | Requires runtime or data files |
| Static item ID catalog | None exists; IDs are data-driven |
| Why hook inventory says 142 missions | Unresolved; 53 verifiable in metadata |

---

## Inspection scripts

Ad-hoc dnfile scripts used (in `/tmp`, not preserved):
- TypeDef/TypeRef/MethodDef/Field/Constant table traversal
- ECMA-335 signature blob parser (hand-written)
- Generic-instantiation (TypeSpec) base resolution for `Mission`1<T>` subclasses

