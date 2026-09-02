# Work claim — C02 direct BOQ arithmetic integrity

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T03:49:31+07:00`
- Baseline main SHA: `bddb24cd322806143d850f9f3f0e6f004ab8947e`
- Implementation branch: `agent/c02-gpt56sol/issue-60-boq-direct-arithmetic`
- Integration batch: `TBD`
- Lane-Key: `c02-boq-direct-line-arithmetic-integrity-20260903`
- Canonical issue: `#60`

## Reserved scope
Validate arithmetic and decimal-evidence integrity for caller-supplied `BoqLine` values at the public `BoqProjection` boundary.

## Expected surfaces
- `src/QS3D.Platform.Quantity/BoqProjection.cs`
- `tests/QS3D.Platform.SmokeTests/BoqDirectLineIntegrityModuleSmoke.cs`
- this coordination claim

## Excluded scope
Quantity accumulation/rules/units, schedules/CSV beyond compatibility, UI/MCP/Core persistence, release/install.

## Validation plan
- TDD RED for stale caller-supplied line total
- TDD RED for direct unrepresentable double quantity bypass
- checked multiplication overflow fail-closed regression
- preserve currency guard and projector-generated lines
- use one shared invariant round-trip double→decimal conversion path
- exact-head hosted CI and post-merge exact-main CI

## Completion condition
Direct `BoqProjection` input enforces the same decimal representation and commercial arithmetic invariants as `BoqProjector`, exact-head CI is green, implementation is merged, and exact-main CI is verified.
