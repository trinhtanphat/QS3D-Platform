# Work claim — C02 BOQ zero-rate decimal short-circuit

- Status: `COMPLETED`
- Agent: `c02-gpt56sol-20260903-1133`
- Registered: `2026-09-03T11:38:46+07:00`
- Baseline main SHA: `0db3fdffb34c240b449a2df845f2a492fdf7aae9`
- Implementation branch: `agent/c02-gpt56sol-20260903-1133/issue-151-zero-rate-decimal`
- Integration batch: `TBD`
- Lane-Key: `c02-boq-zero-rate-decimal-short-circuit-20260903`
- Canonical issue: `#151`

## Reserved scope
Correct BOQ commercial arithmetic so an exact zero unit-rate does not spuriously require an otherwise non-decimal-representable finite quantity to round-trip through `decimal` before producing the exact zero total.

## Expected surfaces
- `src/QS3D.Platform.Quantity/BoqProjection.cs`
- `tests/QS3D.Platform.SmokeTests/BoqZeroRateDecimalRangeModuleSmoke.cs`
- this coordination claim

## Excluded scope
Quantity rule/unit arithmetic, CSV export (separately reserved), Domain/Core persistence, UI/MCP, release/install, native CAD, unrelated parity code.

## Validation evidence
- Regression-only RED head: `d2f325d6c05af151b44bc2aae7cbc0dbab1f0bba`.
- RED CI: `33715869276` — authoritative validation failed on current production.
- Production/final exact-head: `134c6da0be216b52d5977831f75b234d4b77af85`.
- Exact-head CI: `33715961291` — SUCCESS.
- Implementation PR: `#153`.
- Implementation merge commit: `367aec8fdda6e5c2471bf2596a781502d08a3b02`.
- Exact implementation-main CI: `33716013849` — SUCCESS.

## Completion condition
Satisfied: exact-zero rates bypass only the unnecessary decimal quantity conversion, positive-rate fidelity remains fail-closed, both projector/direct regressions pass, implementation merged, and exact implementation-main CI is green.
