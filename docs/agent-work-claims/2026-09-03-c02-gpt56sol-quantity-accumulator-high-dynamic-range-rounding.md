# Work claim — C02 quantity accumulator high-dynamic-range rounding

- Status: `COMPLETED`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T12:37:38+07:00`
- Baseline main SHA: `ce513d07ab7da9b5962db567cd84b229764ba042`
- Implementation branch: `agent/c02-gpt56sol-20260903-quantity-sum-ulp/issue-172-quantity-accumulator-exact-sum`
- Integration batch: `PR #174`
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

## Validation evidence
- Regression-only SHA `10066e369291faa7bf9eca189ce6c1613f9c021d`: CI `33719741262` FAILURE after successful build, exactly on `3.25E+160` versus expected `3.2500000000000004E+160`.
- Production SHA `d2fc6e8eae102eab912bf70adb2ac72df33840bc`: replaced sorted Kahan accumulation with exact binary64-unit accumulation plus one final ties-to-even rounding.
- Final exact head `873159a6c47d416649deebdbe26b5b7d819c89fd`: CI `33719946488` SUCCESS including authoritative validation, Release build and deterministic smokes.
- PR #174 merge commit `0627bb304e70ede0aa54e45ad16b8d8d23b05fd3`.
- Exact-main push CI `33720056565` SUCCESS on `0627bb304e70ede0aa54e45ad16b8d8d23b05fd3`.

## Completion
`QuantityAccumulator` now sums every admitted non-negative finite binary64 fact exactly in integer `2^-1074` units and rounds once using round-to-nearest/ties-to-even. The regression, classic recoverable contribution, permutations, subnormal/normal boundary, tie cases and true overflow are pinned while fact/element counts, provenance, cardinality and immutable outputs remain unchanged.
