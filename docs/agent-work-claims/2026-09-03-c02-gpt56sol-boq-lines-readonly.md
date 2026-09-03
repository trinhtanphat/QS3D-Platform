# Work claim — C02 BOQ lines readonly commercial total

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T09:05:00+07:00`
- Baseline main SHA: `746b7707d31d1339e8aeaee3305cdeefb54afb3b`
- Implementation branch: `agent/c02-gpt56sol-20260903-0905/issue-130-boq-lines-readonly`
- Integration batch: `TBD`
- Lane-Key: `c02-boq-lines-readonly-commercial-total-20260903`

## Reserved scope
C02 immutable BOQ projection line evidence after commercial-total validation in issue #130.

## Expected surfaces
- `src/QS3D.Platform.Quantity/BoqProjection.cs`
- `tests/QS3D.Platform.SmokeTests/BoqProjectionReadonlyLinesModuleSmoke.cs`
- this claim file

## Excluded scope
Quantity formulas, decimal conversion semantics, Domain/Core production, BricsCAD UI/runtime, MCP, installer/release.

## Validation plan
Deterministic RED smoke proves `BoqProjection.Lines` is a castable backing array whose post-construction replacement can diverge from the stored validated `Total`. Production exposes an immutable read-only view while preserving line order/object identity, currency/line-total validation and aggregate arithmetic. Fresh exact-head and exact-main CI required.

## Completion condition
Implementation merged to main with fresh exact-head GREEN CI and final main SHA verified.
