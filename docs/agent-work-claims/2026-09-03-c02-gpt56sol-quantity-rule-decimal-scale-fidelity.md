# Work claim — C02 quantity-rule exact decimal scale fidelity

- Status: `COMPLETED`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T06:14:00Z`
- Baseline main SHA: `efcf1cd7ae3106173e156c5f5fd46308046dcfaa`
- Issue: `#184`
- Lane-Key: `issue-184`
- Implementation branch: `agent/c02-gpt56sol-20260903-rule-decimal-scale/issue-184-rule-decimal-scale-fidelity`
- Implementation PR: `#186`
- Regression-only RED head: `f61c5bee0e8baff755aab39ffbfbfd09702d5e62`
- RED CI: `33722484677` — `FAILURE`
- Production commit: `62aeef45442806a9a8d7808ccdfe2174d4a78f64`
- Production CI: `33722796453` — `SUCCESS`
- Strengthened exact head: `44fb8da1f7686fedee70315632aec72ecf4eb6f5`
- Exact-head CI: `33722888017` — `SUCCESS`
- Merge commit: `1d66adf3f08156d855a43d3fc6a9e2743aeb2e14`
- Exact-main CI: `33722947899` — `SUCCESS`
- Ownership-Key: `quantity.rules.exact-decimal-scale-rational-product-v1`
- Runtime: `REMOTE_SAFE` deterministic vendor-neutral .NET; no licensed CAD runtime evidence required or claimed.

## Reserved scope
Preserve exact SI decimal-rational scale semantics through `QuantityRuleEngine` product accumulation and final binary64 rounding without reintroducing transient per-factor overflow/underflow.

## Landed surfaces
- `src/QS3D.Platform.Quantity/QuantityRules.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityRuleDecimalScaleFidelityModuleSmoke.cs`
- this claim document

## Result
Quantity rules no longer materialize SI decimal scales as rounded binary64 reciprocals before exact-product accumulation. Raw factors and multiplier remain exact dyadics; unit decimal powers are carried separately as exact powers of two/five, and the combined positive rational is rounded nearest-even once to binary64 at the final result.

The focused regression pins single-factor parity with `QuantityUnits` across mm/cm, square/cubic subunits and gram/tonne, plus parsed-one, exponent-2, factor-permutation and `1e308 tonne × 1e-308` compensated-product cases. Broad CI also reran the prior #143 transient overflow/underflow compensation and #176 exact-product/subnormal smokes.

## Safety / compatibility
- zero results remain normalized and do not allocate rational rounding work;
- final overflow and underflow remain fail-closed;
- far-underflow exits before oversized rounding shifts;
- normal/subnormal transitions and ties use exact quotient/remainder nearest-even rounding;
- output dimensions, provenance, missing-input behavior, APIs and deterministic rule ordering are unchanged;
- decimal powers are structurally bounded by valid quantity dimensions/factor exponents.

## Excluded scope
`QuantityUnits` public semantics from #180, accumulator/schedule/CSV/BOQ, Domain/Persistence, BricsCAD UI/runtime, MCP, installer/release.

## Completion condition
Satisfied: implementation is reachable from `main` at merge `1d66adf3f08156d855a43d3fc6a9e2743aeb2e14`, and exact-main CI `33722947899` is GREEN.