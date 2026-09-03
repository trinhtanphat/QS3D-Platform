# Work claim — C02 quantity CSV empty-row fidelity

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T12:10:00+07:00`
- Baseline main SHA: `d076fb509f97097ac12224910c319d3aed51418c`
- Implementation branch: `agent/c02-gpt56sol/issue-165-csv-empty-row-fidelity`
- Integration batch: `TBD`
- Lane-Key: `c02-csv-empty-row-fidelity-20260903`
- Canonical issue: `#165`

## Reserved scope
Preserve valid intentionally empty `QuantityScheduleRow` records across CSV export instead of silently dropping the row when `Quantities.Count == 0`.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityScheduleCsv.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityScheduleCsvEmptyRowFidelityModuleSmoke.cs`
- this coordination claim

## Excluded scope
CSV import (none exists), Quantity arithmetic, BOQ/commercial math, Domain/Core persistence, UI, MCP, installer/release.

## Validation plan
1. deterministic RED from a canonical projector result using `includeElementsWithoutQuantities: true`;
2. emit exactly one row-preservation record for an empty schedule row with blank quantity fields, no fake zero/code;
3. retain existing populated-row ordering, formula-injection neutralization and canonical CRLF;
4. verify truly empty schedules remain header-only and mixed empty/populated schedules preserve both elements;
5. exact-head and exact-main CI GREEN before completion.

## Completion condition
Implementation merged to current main with fresh exact-head GREEN and exact-main GREEN evidence, then claim terminalized.
