# Work claim — C02 quantity schedule cardinality

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T04:34:00+07:00`
- Baseline main SHA: `c43f90640c9b7e876e26cad67fb026d52a2bdfd8`
- Implementation branch: `agent/c02-gpt56sol/issue-75-schedule-cardinality`
- Integration batch: `issue-75`
- Lane-Key: `issue-75`

## Reserved scope
Bound caller-controlled quantity schedule row/summary/fact materialization so hostile or unexpectedly large enumerables fail closed before unbounded allocation/traversal.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantitySchedule.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityScheduleCardinalityModuleSmoke.cs`
- smoke registration only if required by the existing test harness

## Excluded scope
Workspace/UI, MCP transport/runtime, Core persistence, release/install, Quantity arithmetic/BOQ semantics outside schedule cardinality.

## Validation plan
- deterministic exact-limit and limit+1 coverage;
- hostile count/enumerator mismatch where a count-bearing collection is supplied;
- empty and normal schedule/projector behavior;
- full hosted Platform smoke/CI on the exact implementation head.

## Completion condition
The exact implementation head is green, merged through PR review, and the resulting exact `main` SHA is verified with no lost concurrent work.
