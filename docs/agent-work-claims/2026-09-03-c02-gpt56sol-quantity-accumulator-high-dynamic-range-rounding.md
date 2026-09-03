# Work claim — C02 quantity accumulator high-dynamic-range rounding

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T12:37:38+07:00`
- Baseline main SHA: `ce513d07ab7da9b5962db567cd84b229764ba042`
- Implementation branch: `agent/c02-gpt56sol-20260903-quantity-sum-ulp/issue-172-quantity-accumulator-exact-sum`
- Integration batch: `TBD`
- Lane-Key: `issue-172`
- Issue: `#172`

## Reserved scope
Correctly-rounded deterministic accumulation of finite non-negative quantity facts in `QuantityAccumulator` across extreme dynamic range.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityModel.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityAccumulatorHighDynamicRangeRoundingModuleSmoke.cs`
- this claim file

## Excluded scope
Domain/Persistence, QuantitySchedule/CSV, BOQ/commercial policy, BricsCAD adapters/UI, MCP, release/install, XLSX/IFC/BCF.

## Validation plan
- TDD RED through real `QuantityAccumulator.Summarize` using `[1e-60, 1.5e160, 1.75e160]` and exact expected IEEE-754 double `3.2500000000000004e160`.
- Preserve ordinary sums, `1e16 + 1 + 1`, permutation determinism, counts/provenance/cardinality/readonly semantics and true-overflow failure.
- Run repository smoke/build CI on exact head, reconcile latest main, merge only after fresh exact-head GREEN, then verify exact-main CI.

## Completion condition
Production accumulator returns deterministic correctly-rounded representable totals for the regression and controls without weakening fail-closed overflow or any existing provenance/cardinality contract, and the implementation is verified on current `main`.
