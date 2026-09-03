# Work claim — C02 schedule pre-snapshot admission

- Status: `ACTIVE`
- Agent: `c02-gpt56sol-20260903-1533`
- Registered: `2026-09-03T15:42:00+07:00`
- Baseline main SHA: `dca425ef91965375835d1edf7ad8edb78a3da30f`
- Implementation branch: `agent/c02-gpt56sol-20260903-1533/issue-207-schedule-pre-snapshot-admission`
- Integration batch: `TBD`
- Lane-Key: `issue-207`
- Ownership-Key: `quantity.schedule.include-empty-pre-snapshot-admission-v1`

## Reserved scope
Restore the include-empty `QuantityScheduleProjector` row-ceiling admission check before project snapshot allocation. Preserve issue #203 snapshot semantics for admitted requests and do not reopen issue #155 sparse-mode policy.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantitySchedule.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityScheduleProjectRowAdmissionModuleSmoke.cs`
- this claim file

## Excluded scope
Sparse-mode project materialization policy (#155, closed not planned), Domain/Persistence, UI, MCP, installer/release, Quantity arithmetic, CSV schema.

## Validation plan
- TDD regression proves a 100,001-element include-empty request rejects before allocation proportional to element count and before facts enumeration.
- Exact 100,000 include-empty remains accepted.
- Sparse include-empty=false remains behaviorally unchanged.
- Run authoritative Platform CI on exact regression head (RED), production head (GREEN), then exact-main after merge.

## Completion condition
Production fix and deterministic regression are merged through reviewed PR with fresh exact-head Platform CI GREEN, exact-main CI GREEN, and this claim is terminalized only after final-main verification.
