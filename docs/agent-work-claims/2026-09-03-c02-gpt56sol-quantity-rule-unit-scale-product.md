# Work claim — C02 quantity rule unit-scale balanced product

- Status: `COMPLETED`
- Agent: `c02-gpt56sol-20260903-1030`
- Registered: `2026-09-03T10:35:36+07:00`
- Baseline main SHA: `a66001e8f2ec4570aa48a4294876894379dda0bd`
- Implementation branch: `agent/c02-gpt56sol-20260903-1030/issue-143-unit-scale-product`
- Integration batch: `issue-143`
- Lane-Key: `c02-quantity-rule-unit-scale-product-20260903`
- Canonical issue: `#143`
- TDD RED head: `a712052221c666e391ad05969d07c28d3ae21f2e`
- TDD RED CI: `33712092438` FAILURE at the expected per-factor tonne conversion
- Final implementation head: `982058cee4b170a9175cdc0559b9a9f8e6cb7a46`
- Exact-head CI: `33712291803` SUCCESS
- Implementation merge: `033ed84a3f8f00f89932a7167e890a0d38b17f82`
- Exact-main CI: `33712341363` SUCCESS

## Reserved scope
Make quantity-rule unit scaling participate in the same balanced high-dynamic-range product as multiplier/raw factors so individually overflowing/underflowing canonical conversions do not reject a mathematically finite representable final quantity.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityRules.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityRuleUnitScaleProductModuleSmoke.cs`
- this coordination claim

## Excluded scope
`QuantityUnits.cs` and standalone unit-conversion semantics remained unchanged to avoid collision with historical claim #50; BOQ arithmetic, Domain/Core persistence, UI/MCP, release/install, native CAD, IFC/BCF feature invention were also excluded.

## Validation evidence
- regression proved false overflow before production fix for `1e308 tonne * 1e-308`;
- regression covers positive subnormal cubic-millimeter input balanced by a large multiplier;
- regression covers compensation by another factor and factor permutation equality;
- standalone unit conversions still fail closed on individual overflow/underflow;
- genuine final rule overflow/underflow still fail closed;
- fresh final candidate and exact merge-main CI are green.

## Completion condition
Satisfied: deterministic RED captured, production fix stayed inside the non-colliding QuantityRules surface, fresh candidate CI passed, implementation merged, and exact-main CI passed.
