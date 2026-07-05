#!/usr/bin/env python3
"""Export Assets/Excel/excel.xlsx into prototype JSON config files."""

from __future__ import annotations

import argparse
import json
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Dict, List

try:
    from openpyxl import load_workbook
except ImportError:
    print("[config] Missing dependency: openpyxl", file=sys.stderr)
    print("[config] Install it with: python -m pip install openpyxl", file=sys.stderr)
    sys.exit(2)


REQUIRED_SHEETS = ("item", "global", "level", "Sheet1")


def clean(value: Any) -> Any:
    if value is None:
        return None
    if isinstance(value, str):
        return value.strip()
    if isinstance(value, float) and value.is_integer():
        return int(value)
    return value


def load_json(path: Path) -> Dict[str, Any]:
    with path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def save_json(path: Path, data: Dict[str, Any]) -> None:
    path.write_text(
        json.dumps(data, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


def display_path(path: Path, project_root: Path) -> str:
    try:
        return str(path.resolve().relative_to(project_root.resolve())).replace("\\", "/")
    except ValueError:
        return str(path.resolve())


def ensure_required_files(excel_path: Path, output_dir: Path) -> None:
    if not excel_path.exists():
        raise FileNotFoundError(f"Workbook not found: {excel_path}")
    lock_path = excel_path.with_name(f"~${excel_path.name}")
    if lock_path.exists():
        raise PermissionError(
            "Workbook appears to be open in Excel. Save and close it, then run this tool again."
        )
    if not output_dir.exists():
        raise FileNotFoundError(f"Output directory not found: {output_dir}")
    for name in (
        "item_config.json",
        "level_config.json",
        "global_config.json",
        "sheet1_config.json",
    ):
        path = output_dir / name
        if not path.exists():
            raise FileNotFoundError(f"Template JSON not found: {path}")


def ensure_required_sheets(workbook: Any) -> None:
    missing = [sheet for sheet in REQUIRED_SHEETS if sheet not in workbook.sheetnames]
    if missing:
        raise ValueError(f"Workbook missing required sheet(s): {', '.join(missing)}")


def export_items(workbook: Any, output_dir: Path, source_workbook: str, now: str) -> int:
    data = load_json(output_dir / "item_config.json")
    items: List[Dict[str, Any]] = []
    sheet = workbook["item"]

    for row in sheet.iter_rows(min_row=2, values_only=True):
        if not any(value is not None for value in row):
            continue
        item_id = clean(row[0] if len(row) > 0 else None)
        if item_id is None:
            continue
        items.append(
            {
                "itemId": item_id,
                "itemName": clean(row[1] if len(row) > 1 else None),
                "materialName": clean(row[2] if len(row) > 2 else None),
                "prefab": clean(row[3] if len(row) > 3 else None),
                "weightKg": clean(row[4] if len(row) > 4 else None),
                "defense": clean(row[5] if len(row) > 5 else None),
                "health": clean(row[6] if len(row) > 6 else None),
                "sizeFactorReference": clean(row[7] if len(row) > 7 else None),
                "guaranteedSpawnCount": clean(row[8] if len(row) > 8 else None),
                "randomWeight": clean(row[9] if len(row) > 9 else None),
                "unlockLevel": clean(row[10] if len(row) > 10 else None),
            }
        )

    data["sourceWorkbook"] = source_workbook
    data["generatedAt"] = now
    data["note"] = "Item source weight is stored in kilograms and used directly by gameplay and UI."
    data["sourceSheet"] = "item"
    data["items"] = items
    save_json(output_dir / "item_config.json", data)
    return len(items)


def export_levels(
    values_workbook: Any,
    formulas_workbook: Any,
    output_dir: Path,
    source_workbook: str,
    now: str,
) -> int:
    data = load_json(output_dir / "level_config.json")
    levels: List[Dict[str, Any]] = []
    values_sheet = values_workbook["level"]
    formulas_sheet = formulas_workbook["level"]

    for row_index in range(2, values_sheet.max_row + 1):
        values = [clean(values_sheet.cell(row_index, col).value) for col in range(1, 14)]
        if not any(value is not None for value in values):
            continue
        if values[0] is None:
            continue

        speed_formula = formulas_sheet.cell(row_index, 4).value
        levels.append(
            {
                "levelId": values[0],
                "shipWeightDisplay": values[1],
                "initialDistance": values[2],
                "shipSpeedDisplay": values[3],
                "basePullForce": values[4],
                "baseItemCount": values[5],
                "minRandomItemCount": values[6],
                "minTotalItemWeight": values[7],
                "recommendedWeightKg": values[8],
                "itemWeightCoefficient": values[9],
                "stormLevel": values[10],
                "buildTimeSeconds": values[11],
                "waterSurfaceTensionForce": values[12],
                "shipSpeedFormula": speed_formula
                if isinstance(speed_formula, str) and speed_formula.startswith("=")
                else None,
            }
        )

    data["sourceWorkbook"] = source_workbook
    data["generatedAt"] = now
    data["sourceSheet"] = "level"
    data["levels"] = levels
    save_json(output_dir / "level_config.json", data)
    return len(levels)


def export_entries(
    workbook: Any,
    output_dir: Path,
    json_name: str,
    sheet_name: str,
    first_row: int,
    source_workbook: str,
    now: str,
) -> int:
    data = load_json(output_dir / json_name)
    sheet = workbook[sheet_name]
    entries = data.get("entries", [])

    for index, entry in enumerate(entries, start=first_row):
        entry["value"] = clean(sheet.cell(index, 2).value)

    data["sourceWorkbook"] = source_workbook
    data["generatedAt"] = now
    data["sourceSheet"] = sheet_name
    data["parameters"] = {
        entry["key"]: entry.get("value")
        for entry in entries
        if entry.get("key")
    }
    save_json(output_dir / json_name, data)
    return len(entries)


def main() -> int:
    script_dir = Path(__file__).resolve().parent
    project_root = script_dir.parents[1]

    parser = argparse.ArgumentParser(
        description="Export the local Excel workbook into prototype JSON config files."
    )
    parser.add_argument(
        "--excel",
        type=Path,
        default=script_dir / "excel.xlsx",
        help="Path to the source workbook. Defaults to Assets/Excel/excel.xlsx.",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=project_root / "Assets" / "Resources_Prototype" / "TestData",
        help="Directory that contains the prototype JSON config files.",
    )
    args = parser.parse_args()

    excel_path = args.excel.resolve()
    output_dir = args.output.resolve()
    ensure_required_files(excel_path, output_dir)

    values_workbook = load_workbook(excel_path, data_only=True, read_only=True)
    formulas_workbook = load_workbook(excel_path, data_only=False, read_only=True)
    ensure_required_sheets(values_workbook)
    ensure_required_sheets(formulas_workbook)

    now = datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")
    source_workbook = display_path(excel_path, project_root)

    item_count = export_items(values_workbook, output_dir, source_workbook, now)
    level_count = export_levels(values_workbook, formulas_workbook, output_dir, source_workbook, now)
    global_count = export_entries(
        values_workbook,
        output_dir,
        "global_config.json",
        "global",
        2,
        source_workbook,
        now,
    )
    sheet1_count = export_entries(
        values_workbook,
        output_dir,
        "sheet1_config.json",
        "Sheet1",
        1,
        source_workbook,
        now,
    )

    print(f"[config] Source: {source_workbook}")
    print(f"[config] Output: {display_path(output_dir, project_root)}")
    print(f"[config] Items: {item_count}")
    print(f"[config] Levels: {level_count}")
    print(f"[config] Global entries: {global_count}")
    print(f"[config] Sheet1 entries: {sheet1_count}")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:
        print(f"[config] Export failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
