# Work claim — Quantity unit conversion underflow

- Status: `ACTIVE`
- Agent: `c02-gpt56sol-20260903`
- Registered: `2026-09-03T02:45:00+07:00`
- Baseline main SHA: `9846549b41491b78cb593085698091d7a2af8c69`
- Implementation branch: `agent/c02-gpt56sol/issue-50-quantity-unit-underflow`
- Lane-Key: `c02-quantity-unit-underflow-20260903`
- Canonical issue: `#50`

## Reserved scope

Make unit conversion fail closed when a positive nonzero finite quantity becomes exact zero through scaling, preventing silent quantity/round-trip data loss.

## Expected surfaces

- `src/QS3D.Platform.Quantity/QuantityUnits.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityUnitUnderflowModuleSmoke.cs`
- this claim file

## Excluded scope

- Quantity rule multiplication
- CSV/XLSX/export formatting
- native CAD runtime
- CI/release infrastructure

## Validation plan

- prove RED for `ToCanonical` positive underflow
- prove RED for `FromCanonical` positive underflow
- preserve exact zero conversions and existing finite/overflow validation
- run exact-head repository CI before merge and exact-main CI after merge
