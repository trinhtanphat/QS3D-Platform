# Work claim — C02 quantity rule unit-scale balanced product

- Status: `ACTIVE`
- Agent: `c02-gpt56sol-20260903-1030`
- Registered: `2026-09-03T10:35:36+07:00`
- Baseline main SHA: `a66001e8f2ec4570aa48a4294876894379dda0bd`
- Implementation branch: `agent/c02-gpt56sol-20260903-1030/issue-143-unit-scale-product`
- Integration batch: `TBD`
- Lane-Key: `c02-quantity-rule-unit-scale-product-20260903`
- Canonical issue: `#143`

## Reserved scope
Make quantity-rule unit scaling participate in the same balanced high-dynamic-range product as multiplier/raw factors so individually overflowing/underflowing canonical conversions do not reject a mathematically finite representable final quantity.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityRules.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityRuleUnitScaleProductModuleSmoke.cs`
- this coordination claim

## Excluded scope
`QuantityUnits.cs` and standalone unit-conversion semantics are explicitly excluded because active claim #50 owns that surface; BOQ arithmetic, Domain/Core persistence, UI/MCP, release/install, native CAD, IFC/BCF feature invention are also excluded.

## Validation plan
- deterministic RED for `1e308 tonne * 1e-308` yielding finite canonical mass instead of per-factor overflow;
- deterministic RED for positive subnormal cubic-millimeter input balanced by a large multiplier yielding positive representable canonical volume instead of per-factor underflow;
- obtain the existing canonical unit scale without modifying `QuantityUnits.cs`, then balance raw value and scale with the multiplier;
- preserve standalone `QuantityUnits.ToCanonical/FromCanonical` overflow/underflow rejection;
- preserve factor-order determinism, exponent behavior, zero products, missing/invalid input handling and genuine final overflow/underflow rejection;
- authoritative exact-head CI and exact-main verification.

## Completion condition
Regression is RED on pre-fix source, production fix balances unit scale without weakening standalone conversion safety or colliding with #50, fresh candidate CI is green, implementation is merged, claim is terminalized, and exact-main CI is green.
