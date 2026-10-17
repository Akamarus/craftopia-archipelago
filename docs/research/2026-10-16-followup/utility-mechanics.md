# Bucket, tower and generator mechanics

Read-only review of the same installed assembly/capture used by this follow-up. Decompiled method exports are retained in workspace `evidence/research-followup-20261016/`; no game or mod file was modified.

## Buckets

Captured item1919, Bucket (empty), is status1, bench20002, raw age0 (displayed Age1). Its recorded recipe produces10 from Log746 x1 and Stone698 x1. This supports the user's early-bucket account.

`Oc.AsPl_AttackBucketEmpty.move` casts for a water surface when the attack's shoot event fires. A successful host-side hit grants filled bucket839; exit consumes the equipped empty bucket when the hit succeeded. The action has no age or tool-tier condition. It establishes filling at a recognized water surface, not that every visually watery location necessarily uses that surface system.

`Oc.AsPl_Bucket.enter` grants empty bucket1919 only when host-side random value is below0.3. Its exit consumes the equipped bucket after a shoot event unless infinite ammo is enabled. Thus this **pouring action** does not support a guaranteed returned bucket. The random grant is in enter, so it is not a precise claim about the outcome of every interrupted action either.

Captured Water811 is a separate recipe: bench20015, raw age1 (displayed Age2), output10 from filled bucket839 x1. Do not apply the pouring action's 30% behavior to crafting. Return behavior for recipe consumption remains unresolved in this review.

## Tower age evolution

`Oc.Item.UI.OcUI_OpenWorldTowerUI.OnOpenUI` shows the age-evolution button when the current `OcBldg_WorldHeritage` is not `IsUnderConstruction`. It does not select a permitted age by tower identity.

`Oc.OcUI_AgeEvolution.CanEvo` checks that the current evolution row exists and is enabled, then required gold and inventory materials (with an explicit debug bypass). `TryEvoWorldLevel` charges those costs and evolves. Together these methods support use of a completed tower for successive available age transitions, subject to the current row's costs. They do not make later-region materials accessible merely because the tower UI is available.

## Generator scope

`Oc.OcBldg_ItemGenerator` loads its serialized `generateItemID` into the crafting item. Its parameterless `StartProcessReq` requests10 queued units. That is not evidence of ten batteries per completion. Its per-item time is based on the generated item's craft time and process-speed modifiers.

`Oc.OcZone_GeneratorRunning.StateLoop` starts the process after mounting/start animation and disposes/stops processing when mounting ends. This supports a mounting-dependent production loop. The actual generator prefab's output ID, eligible mounted entities, and practical production rate were not resolved here; do not infer these solely from the generic class name.
