# Reviewed equipment runtime and GitHub update

User-approved basis: apply workbook equipment assignments to the mod and update GitHub after regional APworld balance. Existing catalog identities and saved seeds must retain their rules.

1. Add runtime regression coverage for exact per-item tiers, native age gates, exclusions, metadata mismatches and independent old catalogs; add bridge and APworld contract tests.
2. Create a versioned overlay for the 206 eligible reviewed rows. Keep seven explicitly excluded legacy/unavailable/temporary rows excluded. Add Combat Support (AssistEquip) as an append-only AP item, with one tier-one receipt in Geezah; retain native craft ages. Keep existing numbering and sparse schedules, extending a schedule only if a reviewed tier cannot otherwise be reached. New regional seeds select the new contract. Old seeds remain unchanged.
3. Build and test mod, bridge, APworld and packages; independent review; document and push the scoped work to the existing feature branch on GitHub. Do not publish Workshop or overwrite a running installation.

Ruling: the workbook assigns all 19 combat-support rows tier one but does not specify a placement deadline. Use one Geezah unlock so the newly supported family is usable without inventing later progression. Native ages still apply. Cost if wrong: timing may need adjustment after playtesting.

Ruling: preserve the current tier numbering used by the newly approved regional thresholds; copper-first renumbering remains a separate coordinated rebalance, avoiding silently shifting the user's workbook tiers.

Ruling: preserve unrelated pending Tamer work and mod fixes. The new build includes the already-present mod fixes, but commit only this task's files plus explicitly separated existing hunks where necessary. GitHub push is authorized by this request; no merge or public release is inferred.

Progress: runtime catalog boundaries passed for all 206 items; excluded IDs tested explicitly. Independent review found one stale APworld protocol assertion, fixed after observing the failure. Protocol-3 bridge test found the old capability upper bound; corrected after RED and verified GREEN. Armory production tests (47) and milestone suites (67) passed. No Tamer files are included.
