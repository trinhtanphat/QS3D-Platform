# Work claim — C02 quantity readonly array exposure

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T08:55:00+07:00`
- Baseline main SHA: `ee98f7315f424e3d7141cee09806890021dff3d7`
- Implementation branch: `agent/c02-gpt56sol-20260903-0855/issue-127-readonly-array-exposure`
- Integration batch: `TBD`
- Lane-Key: `c02-quantity-readonly-array-exposure-20260903`

## Reserved scope
C02 immutable public collection exposure for validated quantity schedule and rule state in issue #127.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantitySchedule.cs`
- `src/QS3D.Platform.Quantity/QuantityRules.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityReadonlyCollectionExposureModuleSmoke.cs`
- this claim file

## Excluded scope
Quantity numeric algorithms, Domain/Core production, BricsCAD UI, MCP, release/install.

## Validation plan
Deterministic RED smoke proves the public `IReadOnlyList<T>` properties are runtime arrays that callers can cast and mutate after validation. Production wraps private ordered arrays in non-array read-only views; verify schedule rows/quantities and rule factors/catalog rules, ordering, compatibility, and no mutable backing escape. Fresh exact-head and exact-main CI required.

## Completion condition
Implementation merged to main with fresh exact-head GREEN CI and final main SHA verified.