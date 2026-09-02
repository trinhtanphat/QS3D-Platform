# Work claim — C02 final positive quantity-rule underflow

- Status: `COMPLETED`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T03:54:03+07:00`
- Baseline main SHA: `bf81fc654f0b6d95c5c8210dea3dc5dbd585404f`
- Implementation branch: `agent/c02-gpt56sol/issue-63-rule-final-underflow`
- Integration batch: `TBD`
- Lane-Key: `c02-rule-final-positive-underflow-20260903`
- Canonical issue: `#63`
- Implementation merge: `5c60cae7faa3667d8f27b14d5066ddde5f9e3fc8`

## Reserved scope
Fail closed when a quantity-rule product of strictly-positive factors has a mathematically positive final value but cannot be represented as a nonzero `double`.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityRules.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityRuleFinalUnderflowModuleSmoke.cs`
- this coordination claim

## Excluded scope
Unit conversion, BOQ/commercial arithmetic, schedules/CSV, UI/MCP/Core persistence, release/install.

## Validation plan
- TDD RED for two positive length factors whose Area product is below double range
- preserve explicit-zero annihilation
- preserve representable positive subnormal results
- preserve permutation/order determinism from prior multiplication hardening
- deterministic invariant diagnostics
- exact-head and exact-main CI

## Completion condition
Positive final underflow can no longer silently become a zero quantity, legitimate zero and representable subnormal values remain supported, implementation merges, and exact-main CI is green.
