# Work claim — C02 quantity rule product rounding

- Status: `COMPLETED`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T12:46:42+07:00`
- Baseline main SHA: `f10465dee4b50d445ed53999b9af7f5cd79e5ff7`
- Implementation branch: `agent/c02-gpt56sol-20260903-rule-product-rounding/issue-176-quantity-rule-exact-product`
- Integration batch: `PR #178`
- Lane-Key: `issue-176`
- Issue: `#176`

## Reserved scope
Correct IEEE-754 rounding of multi-factor products produced by `QuantityRuleEngine` from admitted binary64 inputs and canonical unit-scale factors.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityRules.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityRuleProductRoundingModuleSmoke.cs`
- this claim file

## Excluded scope
Domain/Persistence, QuantityAccumulator, QuantitySchedule/CSV, BOQ/commercial projection, BricsCAD adapters/UI, MCP, release/install.

## Validation evidence
- Regression-only SHA `d88dcb27b1550371ad9fc2d3971f81688183c783`: CI `33720378140` FAILURE after clean Release build, exactly on `2.8912663957675546` versus expected `2.891266395767554`.
- Production SHA `97d253058a536c4feea82312af534256f5bc3a45`: exact binary significand/exponent product with one final nearest-even rounding.
- Resource-safety hardening SHA `198fa84262154cba2387e38db1c4107088b5408a`: bounded extreme-underflow right-shift rounding.
- Final exact head `7db4d223b6cba4d99e1d390e9d38a3951b7a8e39`: CI `33720660762` SUCCESS including authoritative validation and strengthened product-rounding smoke.
- PR #178 merge commit `f37ffab03dbcd565aa58b6d1dbcb0f1c191353f8`.
- Exact-main push CI `33720774525` SUCCESS on `f37ffab03dbcd565aa58b6d1dbcb0f1c191353f8`.

## Completion
`QuantityRuleEngine` now multiplies the exact admitted binary64 significands and powers of two, then rounds once using round-to-nearest/ties-to-even. Regression permutations, exponent semantics, subnormal tie cases and far-underflow fail-closed behavior are pinned without weakening missing-input, unit-scale, output-cardinality, provenance or deterministic ordering contracts.
