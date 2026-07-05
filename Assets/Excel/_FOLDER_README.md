# Assets/Excel

## Purpose

Stores the local configuration workbook and the helper tool used to export it
to prototype JSON config files.

## Allowed

- `excel.xlsx`: the source configuration workbook.
- `update_config.py`: workbook-to-JSON export script.
- `update_config.bat`: Windows launcher for teammates.
- Short usage notes for the export workflow.

## Not Allowed

- Runtime gameplay code.
- Generated Unity assets unrelated to configuration export.
- Temporary personal copies of the workbook.

## Notes

- Item weights in the workbook are treated as kilograms and are written to
  `weightKg` directly.
- The export target is `Assets/Resources_Prototype/TestData/`.
