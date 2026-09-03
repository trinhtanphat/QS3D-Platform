# Work claim — C02 quantity schedule row summary affinity

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T12:06:00+07:00`
- Baseline main SHA: `845128e6923d1c4856696371510b3a2e6f09be65`
- Implementation branch: `agent/c02-gpt56sol/issue-162-schedule-row-summary-affinity`
- Integration batch: `TBD`
- Lane-Key: `c02-schedule-row-summary-affinity-20260903`
- Canonical issue: `#162`

## Reserved scope
Enforce row-local quantity evidence integrity at the public `QuantityScheduleRow` boundary: every summary attached to a per-element row must represent facts from exactly that one row element, while intentionally empty rows remain represented by an empty quantities collection.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantitySchedule.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityScheduleRowSummaryAffinityModuleSmoke.cs`
- this coordination claim

## Excluded scope
Quantity arithmetic/unit conversion, BOQ/commercial math, Domain/Core persistence, UI, MCP, installer/release, unrelated exports.

## Validation plan
1. deterministic RED proving `ElementCount > 1` and zero-fact summaries are currently accepted by a per-element row;
2. minimal production guard at row construction after safe materialization/null validation;
3. retain valid zero-valued fact-backed summaries and empty quantity collections;
4. run exact-head Platform CI and broader quantity smoke validation; reconcile latest main before merge.

## Completion condition
Implementation merged to current main with fresh exact-head GREEN and exact-main GREEN evidence, then claim terminalized.
