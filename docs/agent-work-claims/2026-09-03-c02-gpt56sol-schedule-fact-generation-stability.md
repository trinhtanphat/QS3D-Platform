# Work claim — C02 quantity schedule fact generation stability

- Status: `ACTIVE`
- Agent: `c02-gpt56sol-20260903-1939`
- Registered: `2026-09-03T19:39:00+07:00`
- Baseline main SHA: `2ecf8821f172b48ba4b5a3bcb570a7d9ec94fb58`
- Implementation branch: `agent/c02-gpt56sol-20260903-1939/issue-239-schedule-fact-generation`
- Lane-Key: `issue-239`
- Canonical issue: `#239`

## Reserved scope
Bind counted `QuantityScheduleProjector` fact inputs to one ordered immutable semantic generation so same-Count replacement, reorder, quantity, or CAD provenance drift fails closed while raw streaming enumerables remain single-pass.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantitySchedule.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityScheduleFactGenerationStabilitySmoke.cs`
- this coordination claim

## Excluded scope
BOQ/rate arithmetic, QuantityRule production, Domain/Persistence production, BricsCAD UI, MCP, installer/release.

## Validation contract
Deterministic TDD RED first; bounded replay Count observation; semantic ordered replay comparison; 100,000-entry boundary and existing schedule/provenance behavior preserved; fresh exact-head Platform CI GREEN before implementation merge; exact-main CI after merge.

## Completion condition
Update to `COMPLETED` only after implementation is merged and exact-main CI is green.
