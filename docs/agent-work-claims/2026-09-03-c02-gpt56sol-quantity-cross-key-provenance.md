# Work claim — C02 quantity cross-key provenance

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T08:46:00+07:00`
- Baseline main SHA: `f6d8c4004ff5add2a852cd2e0b270b3d58d6c2b4`
- Implementation branch: `agent/c02-gpt56sol-20260903-0846/issue-124-cross-key-provenance`
- Integration batch: `TBD`
- Lane-Key: `c02-quantity-accumulator-cross-key-provenance-20260903`

## Reserved scope
C02 direct quantity aggregation provenance consistency across quantity keys for issue #124.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityModel.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityAccumulatorCrossKeyProvenanceModuleSmoke.cs`
- `tests/QS3D.Platform.SmokeTests/Program.cs`
- this claim file

## Excluded scope
Domain/Core production, BricsCAD UI, MCP, release/install, unrelated quantity behavior.

## Validation plan
Deterministic RED smoke proving same-element conflicting CAD provenance is admitted across different quantity keys; production fail-closed validation across the whole aggregation; matching-null/non-null and different-element compatibility checks; broad smoke and fresh exact-head/exact-main CI.

## Completion condition
Implementation merged to main with fresh exact-head GREEN CI and final main SHA verified.