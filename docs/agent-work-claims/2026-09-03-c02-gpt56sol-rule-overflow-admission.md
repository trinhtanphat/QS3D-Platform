# Work claim — C02 quantity rule final-overflow admission

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T16:34:17+07:00`
- Baseline main SHA: `d21e58f5aa8e57c43c47a86726638522b73e5d08`
- Implementation branch: `agent/c02-gpt56sol-20260903-1631/issue-211-rule-overflow-admission`
- Integration batch: `TBD`
- Lane-Key: `issue-211`
- Canonical issue: `#211`

## Reserved scope
C02 quantity-rule numeric/resource safety: reject mathematically certain final binary64 overflow before exact-rational rounding performs avoidable large BigInteger shifts/allocations.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityRules.cs`
- focused deterministic Quantity rule regression under `tests/QS3D.Platform.SmokeTests/`
- this coordination claim

## Excluded scope
Domain/Persistence, Workspace/UI, MCP/native CAD, release/install, BOQ/schedule/CSV behavior except shared smoke-runner registration if required.

## Validation plan
- TDD RED on current main proving admitted overflowing Count-factor product reaches expensive rounding work before overflow;
- minimal production gate after exact `highestBinaryExponent` calculation;
- exact finite boundary and existing product/underflow/decimal-scale smokes;
- full Platform validation on exact implementation head, merge only on GREEN, then exact-main CI.

## Completion condition
Implementation is merged to current `main`, exact-main CI is GREEN, claim is terminalized, and no required code remains off-main.
