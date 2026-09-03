# Work claim — C02 quantity rule output cardinality

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T09:41:00+07:00`
- Baseline main SHA: `2756e0570245377fa63491dee862f0d1691afce5`
- Implementation branch: `agent/c02-gpt56sol-20260903-0939/issue-137-rule-output-cardinality`
- Integration batch: `TBD`
- Lane-Key: `c02-quantity-rule-output-cardinality-20260903`
- Issue: `#137`

## Reserved scope
C02 evaluated QuantityFact output cardinality in QuantityRuleEngine.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityRules.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityRuleOutputCardinalityModuleSmoke.cs`
- this claim file

## Excluded scope
Domain project cardinality policy, accumulator numeric logic, BricsCAD UI, MCP, release/install, persistence.

## Validation plan
- deterministic RED proving the producer can cross the downstream-supported 100,000-fact boundary;
- production guard rejects before adding fact 100,001 while exactly 100,000 remains accepted;
- skipped missing-input rules do not consume the fact budget;
- preserve deterministic order, arithmetic/provenance and readonly-result behavior;
- exact-head CI GREEN, merge, exact-main GREEN.

## Completion condition
QuantityRuleEngine bounds produced facts consistently with the quantity pipeline's supported fact ceiling without weakening existing behavior.