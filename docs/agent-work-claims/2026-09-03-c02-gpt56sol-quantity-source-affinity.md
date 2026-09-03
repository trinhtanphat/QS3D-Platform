# Work claim — C02 quantity schedule source affinity

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T07:33:00+07:00`
- Baseline main SHA: `695e15ba30d7bc76b95f5f7447d12805836a6b19`
- Implementation branch: `agent/c02-gpt56sol-20260903-0733/issue-108-quantity-source-affinity`
- Integration batch: `TBD`
- Lane-Key: `c02-quantity-schedule-source-affinity-20260903`

## Reserved scope
C02 Quantity schedule projection provenance validation for issue #108.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantitySchedule.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityScheduleSourceAffinityModuleSmoke.cs`
- this claim file

## Excluded scope
Domain/Core production, BricsCAD UI, MCP, release/install, unrelated quantity behavior.

## Validation plan
Deterministic RED smoke for stale CAD source reference; production fail-closed validation; full smoke/CI on exact head.

## Completion condition
Implementation merged to main with fresh exact-head GREEN and current main SHA verified.
