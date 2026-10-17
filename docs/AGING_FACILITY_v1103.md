# Aging Facility mapping fix — v1.10.3

F9 capture craftopia_diag_20261016_145958_a11a8e711e1940a190a71b9eefac918d proves Aging Facility item416 is Oc.OcBldg_CraftingBench, family20206. Missing family mapping caused station identity unavailable before costs. Map this exact class/family to equipment_care, consistent with existing OcBldg_Aging and Aging UI policy. Unknown families still deny. No unlocks granted or cost paths changed.

Regression test reproduced the missing mapping, then all 34 production assertions passed. Equipment suite passed 6843 assertions plus session/adapter checks. Release build: zero warnings/errors. Live craft test pending Equipment Care unlock and restart.

Runtime recipe evidence: Grease2458 uses workbench20206 and Animal Flesh2050 x2; Paper2457 uses Water811; Water uses cooking family20015. User reports Millewind repair needs paper and grease. This adds Equipment Care to the reported tower progression dependency; generation logic remains to be updated.
