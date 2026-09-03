# Work claim — C02 BOQ zero-rate decimal short-circuit

- Status: `ACTIVE`
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

## Validation plan
- deterministic regression first for `double.MaxValue` and positive sub-decimal-range quantities at `0m` unit rate;
- cover both direct `BoqProjection` line validation and `BoqProjector.Project`;
- prove positive rates still fail closed when exact double→decimal fidelity is impossible;
- run focused smoke plus full Platform validation/CI;
- self-review decimal overflow, zero/negative-zero, currency, duplicate/materialization and deterministic ordering semantics.

## Completion condition
Implementation is merged from a fresh exact-head GREEN candidate, exact-main CI is GREEN, issue is closed, and this claim is terminalized with the merge evidence.
