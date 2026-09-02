# Work claim — BOQ duplicate quantity summary safety

- Status: `ACTIVE`
- Agent: `c02-gpt56sol-20260903`
- Registered: `2026-09-03T02:49:00+07:00`
- Baseline main SHA: `fffbce3c8b26cc8946bae9f843dc7ea673827af3`
- Implementation branch: `agent/c02-gpt56sol/issue-52-boq-duplicate-summary`
- Lane-Key: `c02-boq-duplicate-summary-20260903`
- Canonical issue: `#52`

## Reserved scope

Reject ambiguous duplicate aggregate quantity keys before commercial BOQ projection so totals cannot silently double-charge the same `(Code, Dimension)` evidence.

## Expected surfaces

- `src/QS3D.Platform.Quantity/BoqProjection.cs`
- `tests/QS3D.Platform.SmokeTests/BoqDuplicateQuantitySummaryModuleSmoke.cs`
- this claim file

## Excluded scope

- Quantity accumulation
- unit conversion
- CSV/XLSX export
- native CAD runtime
- CI/release infrastructure

## Validation plan

- prove RED from duplicate `(Code, Dimension)` summaries with one matching rate
- reject duplicates before commercial arithmetic
- preserve unique keys, missing-rate policy, duplicate-rate checks and deterministic ordering
- require exact-head CI GREEN before merge
