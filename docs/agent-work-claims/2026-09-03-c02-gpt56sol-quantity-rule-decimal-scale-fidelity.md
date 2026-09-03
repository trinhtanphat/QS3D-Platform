# Work claim — C02 quantity-rule exact decimal scale fidelity

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T06:14:00Z`
- Baseline main SHA: `efcf1cd7ae3106173e156c5f5fd46308046dcfaa`
- Issue: `#184`
- Lane-Key: `issue-184`
- Implementation branch: `agent/c02-gpt56sol-20260903-rule-decimal-scale/issue-184-rule-decimal-scale-fidelity`
- Ownership-Key: `quantity.rules.exact-decimal-scale-rational-product-v1`
- Runtime: `REMOTE_SAFE` deterministic vendor-neutral .NET; no licensed CAD runtime evidence required or claimed.

## Reserved scope
Preserve exact SI decimal-rational scale semantics through `QuantityRuleEngine` product accumulation and final binary64 rounding without reintroducing transient per-factor overflow/underflow.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityRules.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityRuleDecimalScaleFidelityModuleSmoke.cs`
- this claim document

## Excluded scope
`QuantityUnits` public semantics from #180, accumulator/schedule/CSV/BOQ, Domain/Persistence, BricsCAD UI/runtime, MCP, installer/release.

## Deterministic defect
On the #180 main tree, one factor with raw `69789978031.23123` millimeters should agree with standalone exact conversion `69789978.03123122` (`0x4190A3A4681FFB14`). The current rule engine first materializes `ToCanonical(1d, Millimeter)` as a rounded binary64 reciprocal and then multiplies, producing `69789978.03123124` (`0x4190A3A4681FFB15`).

## Design
Keep each parsed raw factor and multiplier as exact dyadic components as today. Represent the unit scale separately as a decimal power. Split powers of ten into powers of two plus powers of five; fold powers of two into the binary exponent and powers of five into an exact BigInteger numerator/denominator. Round the resulting positive rational to binary64 nearest-even once at the end. This preserves balanced high-dynamic-range compensation and factor-order determinism while removing rounded scale constants from rule arithmetic.

## Validation plan
1. Regression-only RED through `QuantityRuleEngine.Evaluate` for single-factor parity with `QuantityUnits.ToCanonical`.
2. Cover mm/cm/mm2/cm2/mm3/cm3/g/tonne, factor exponent 1–3, multiple-factor permutations, multiplier compensation, zero, subnormal, final overflow/underflow.
3. Preserve existing QuantityRule unit-scale product, exact-product rounding, catalog/materialization and broad Platform smokes.
4. Self-review rational exponent bounds, BigInteger growth, tie-to-even, subnormal/min-normal/max-finite boundaries, deterministic ordering, error paths and compatibility.
5. Fresh exact-head CI GREEN before merge; exact-main GREEN before terminal claim.
