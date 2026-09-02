# Work claim — Quantity rule product order safety

- Status: `ACTIVE`
- Agent: `c02-gpt56sol-20260903`
- Registered: `2026-09-03T02:38:00+07:00`
- Baseline main SHA: `83ba5124fbd7743f2692ae2092af5bf6dc48e7a8`
- Implementation branch: `agent/c02-gpt56sol/issue-48-quantity-rule-product-order`
- Lane-Key: `c02-quantity-rule-product-order-20260903`
- Canonical issue: `#48`

## Reserved scope

Remove declaration-order-dependent false overflow/underflow from quantity rule factor multiplication while preserving final-range fail-closed semantics.

## Expected surfaces

- `src/QS3D.Platform.Quantity/QuantityRules.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityRuleFactorOrderDeterminismModuleSmoke.cs`
- this claim file for terminal status

## Excluded scope

- CSV/XLSX/export formatting
- QuantityAccumulator
- native CAD runtime
- CI/release infrastructure

## Validation plan

- prove RED with equivalent factor permutations containing extreme finite magnitudes
- cover false overflow and false underflow cases without weakening genuine final overflow rejection
- preserve rule dimensions, missing-input semantics and factor exponents
- run exact-head repository CI before merge

## Completion condition

Regression is GREEN, exact-head CI is GREEN, canonical PR merges to `main`, exact-main CI is verified, and the claim is marked terminal.
