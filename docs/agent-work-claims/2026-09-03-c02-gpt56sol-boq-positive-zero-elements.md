# Work claim — C02 positive BOQ quantity with zero elements

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T04:17:00+07:00`
- Baseline main SHA: `c95cbdb611e1f40a3385bdb236ddbcbb916727e0`
- Implementation branch: `agent/c02-gpt56sol/issue-72-boq-zero-elements`
- Integration batch: `TBD`
- Lane-Key: `c02-boq-positive-zero-elements-20260903`
- Canonical issue: `#72`

## Reserved scope
Reject direct/imported BOQ lines whose quantity is strictly positive but whose element provenance count is zero.

## Expected surfaces
- `src/QS3D.Platform.Quantity/BoqProjection.cs`
- `tests/QS3D.Platform.SmokeTests/BoqElementProvenanceModuleSmoke.cs`
- this coordination claim

## Excluded scope
Quantity aggregation, unit conversion, rules, UI/MCP/Core persistence, release/install.

## Validation plan
- TDD RED for positive quantity with zero elements
- preserve empty zero line with zero elements and zero total
- preserve zero-valued lines backed by elements
- preserve total/currency/decimal-integrity gates and deterministic ordering
- authoritative exact-head CI and exact-main verification

## Completion condition
Positive payable BOQ evidence can no longer claim zero contributing elements, legitimate zero cases remain compatible, implementation merges, and exact-main CI is green.
