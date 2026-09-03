# Work claim — C02 skip-missing factor-order determinism

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T16:48:07+07:00`
- Baseline main SHA: `f7606842f2eb6c5ece6071ffee9decc53dab95b8`
- Implementation branch: `agent/c02-gpt56sol-20260903-1631/issue-216-skip-missing-factor-order`
- Integration batch: `TBD`
- Lane-Key: `issue-216`
- Canonical issue: `#216`

## Reserved scope
C02 quantity-rule missing-input determinism: when `skipRuleWhenInputMissing` is enabled, missing-required-input detection must not depend on commutative factor declaration order or be preempted by parsing an unrelated factor first.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityRules.cs`
- focused deterministic Quantity rule smoke under `tests/QS3D.Platform.SmokeTests/`
- this coordination claim

## Excluded scope
Domain/Persistence, Workspace/UI, MCP/native CAD, release/install, BOQ/schedule/CSV behavior.

## Validation plan
- TDD RED proving reversed factor order changes skip-vs-throw outcome on current main;
- minimal production change that resolves all required-property presence before numeric parsing only in skip-missing mode;
- verify skip=false remains fail-closed and complete-input arithmetic remains deterministic;
- full Platform exact-head CI GREEN, merge, exact-main CI GREEN.

## Completion condition
Implementation is merged to current `main`, exact-main CI is GREEN, issue #216 is completed, claim is terminalized, and no required code remains off-main.
