# Work claim — C02 quantity rule product rounding

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T12:46:42+07:00`
- Baseline main SHA: `f10465dee4b50d445ed53999b9af7f5cd79e5ff7`
- Implementation branch: `agent/c02-gpt56sol-20260903-rule-product-rounding/issue-176-quantity-rule-exact-product`
- Integration batch: `TBD`
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

## Validation plan
- TDD RED through real `QuantityRuleEngine.Evaluate` using three legal `Each` factors whose current sequential-mantissa product is one ULP high.
- Preserve zero/missing-input semantics, factor exponents, unit-scale compensation, final overflow/underflow failure, output cardinality, provenance and deterministic rule ordering.
- Self-review exact-product resource behavior at legal factor cardinality and use bounded-by-input deterministic precision state.
- Require fresh exact-head CI GREEN, merge, exact-main CI GREEN, then terminalize claim.

## Completion condition
Rule products reflect one final nearest-even rounding of the exact admitted binary64 factors without weakening existing safety or cardinality contracts, and current main carries the verified implementation.
