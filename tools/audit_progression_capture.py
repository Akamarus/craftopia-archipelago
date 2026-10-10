"""Audit read-only schema-3/4 evidence; never infer reachability or write game state."""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path


def audit(raw):
    data = json.loads(raw)
    if type(data.get("schema_version")) is not int or data["schema_version"] not in (3, 4):
        raise ValueError("Expected diagnostic schema 3 or 4")
    for section in ("items", "ages", "craft_bench_map", "age_new_items", "missions", "skills"):
        if data.get(section + "_status") != "collected" or not isinstance(data.get(section), list):
            raise ValueError(f"Incomplete section: {section}")
    items = data["items"]
    sentinel_rows = sum(row.get("id") == 0 and type(row.get("id")) is int for row in items)
    items = [row for row in items if row.get("id") != 0]
    ids = [row["id"] for row in items]
    if any(type(value) is not int or value <= 0 for value in ids):
        raise ValueError("Invalid item ID")
    counts = Counter(ids)
    duplicates = sorted(key for key, count in counts.items() if count != 1)
    if duplicates:
        raise ValueError(f"Duplicate item IDs: {duplicates}")
    known = set(ids)
    recipes = []
    missing_materials, malformed_pairs = set(), []
    for row in items:
        materials = []
        for letter in "ABCDE":
            item_id, count = row.get(f"material{letter}_Id"), row.get(f"material{letter}_count")
            if type(item_id) is not int or type(count) is not int or item_id < 0 or count < 0:
                malformed_pairs.append([row["id"], letter])
            elif item_id and count:
                materials.append(item_id)
                if item_id not in known:
                    missing_materials.add(item_id)
            elif bool(item_id) != bool(count):
                malformed_pairs.append([row["id"], letter])
        if materials:
            recipes.append(row["id"])
    bench_ids = set()
    missing_bench_items, bench_mismatches = set(), []
    by_id = {row["id"]: row for row in items}
    for group in data["craft_bench_map"]:
        bench = group["workbench_id"]
        if type(bench) is not int or bench in bench_ids:
            raise ValueError("Invalid/duplicate bench group")
        bench_ids.add(bench)
        for item in group["item_ids"]:
            if item not in known:
                missing_bench_items.add(item)
            elif by_id[item]["workbenchId"] != bench:
                bench_mismatches.append([item, bench, by_id[item]["workbenchId"]])
    age_requirements = {item["item_id"] for age in data["ages"] for item in age["requireItems"]}
    report = {
        "source_sha256": hashlib.sha256(raw).hexdigest(),
        "captured_at": data["timestamp"],
        "collector_version": data["collector_version"],
        "collector_sha256": data["collector_sha256"],
        "game_assembly_sha256": data["game_assembly_sha256"],
        "counts": {key: len(data[key]) for key in ("items", "ages", "craft_bench_map", "age_new_items", "missions", "skills")},
        "unique_item_ids": len(known),
        "zero_id_sentinel_rows_excluded": sentinel_rows,
        "items_with_material_fields": len(recipes),
        "missing_material_ids": sorted(missing_materials),
        "malformed_material_pairs": malformed_pairs,
        "missing_bench_item_ids": sorted(missing_bench_items),
        "bench_assignment_mismatches": bench_mismatches,
        "missing_age_requirement_ids": sorted(age_requirements - known),
        "item_status_counts": dict(sorted(Counter(row["status"] for row in items).items())),
        "old_data_rows": sum(row["isOldData"] is True for row in items),
        "world_levels_in_age_rows": [row["worldLevel"] for row in data["ages"]],
        "limitations": [
            "Mixed raw status values may include disabled or non-player content; counts are not playable-item counts.",
            "Material fields describe raw dependencies, not all gathering/drop/merchant sources.",
            "Raw bench IDs are not yet bound to verified station types.",
            "Age-array indices, raw rank, rarity, and bench level are not assigned AP tiers.",
            "Mission base fields do not include every subclass-specific objective or prerequisite.",
            "No beatability, equipment eligibility, or multiplayer authority claim follows from this audit."
        ]
    }
    return report


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("capture", type=Path)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    report = json.dumps(audit(args.capture.read_bytes()), indent=2) + "\n"
    if args.output:
        args.output.write_text(report, encoding="utf-8")
    else:
        print(report, end="")
