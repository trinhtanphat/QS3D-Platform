# Work claim — C02 schedule counted-source no-overread

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-04T06:50:00+07:00`
- Baseline main SHA: `2384462c139547f7e3f7065fca5fe69d8055c7d1`
- Implementation branch: `agent/c02-gpt56sol/issue-282-schedule-count-no-overread`
- Integration batch: `issue-282`
- Lane-Key: `issue-282`

## Reserved scope
Fail closed when authoritative-count schedule row, quantity summary, quantity fact, or generic schedule materializer inputs yield beyond advertised Count, before accessing `IEnumerator.Current` for the overrun item.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantitySchedule.cs`
- focused existing schedule cardinality/generation smoke(s) under `tests/QS3D.Platform.SmokeTests/`
- smoke registration only if required

## Excluded scope
BOQ/commercial projection, Quantity arithmetic/rules outside schedule materialization, Core persistence, Workspace/UI, MCP, installer/release.

## Validation plan
- TDD RED with counted hostile schedule inputs whose N+1 `Current` access is observable;
- Count=0, exact Count, under/overreported/conflicting/changing Count and 100,000-entry ceiling;
- preserve unknown-count bounded single-pass behavior;
- preserve schedule row/summary/fact generation replay, provenance and deterministic ordering;
- fresh exact-head hosted Platform CI.

## Completion condition
Fresh exact-head GREEN, reviewed PR merge, claim closeout and resulting main SHA verification.
