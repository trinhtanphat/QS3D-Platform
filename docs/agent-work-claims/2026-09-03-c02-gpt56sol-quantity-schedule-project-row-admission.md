# Work claim — C02 quantity schedule project-row admission

- Status: `ACTIVE`
- Agent: `c02-gpt56sol-20260903-1030`
- Registered: `2026-09-03T10:45:00+07:00`
- Baseline main SHA: `712c323b6e1e524d455d718931d8a79f6c541efc`
- Implementation branch: `agent/c02-gpt56sol-20260903-1030/issue-147-project-row-admission`
- Lane-Key: `c02-quantity-schedule-project-row-admission-20260903`
- Canonical issue: `#147`

## Reserved scope
Fail fast when `QuantityScheduleProjector.Project(..., includeElementsWithoutQuantities: true)` is guaranteed to exceed the 100,000-row schedule ceiling from project element cardinality, before materializing facts or constructing rows.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantitySchedule.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityScheduleProjectRowAdmissionModuleSmoke.cs`
- this coordination claim

## Excluded scope
SemanticProject/Core cardinality, quantity arithmetic, `includeElementsWithoutQuantities=false` rejection based solely on project size, UI/MCP, release/install, native CAD.

## Validation plan
- regression proves >100,000 include-empty project fails before facts enumeration;
- exact 100,000 include-empty remains admitted;
- false mode remains compatible for large projects with small/no facts;
- preserve current fact materialization, provenance, row ordering and fail-closed semantics;
- fresh exact-head and exact-main CI evidence.
