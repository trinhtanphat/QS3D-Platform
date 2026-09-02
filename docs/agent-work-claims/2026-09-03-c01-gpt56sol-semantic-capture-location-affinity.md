# Work claim — C01 semantic capture location affinity

- Status: `ACTIVE`
- Agent: `c01-gpt56sol`
- Registered: `2026-09-03T06:34:00+07:00`
- Baseline main SHA: `670d33f4576afd56af9835d57751c2a71e8746b4`
- Implementation branch: `agent/c01-gpt56sol/issue-97-semantic-capture-location-affinity`
- Integration batch: `TBD`
- Lane-Key: `c01-semantic-capture-location-affinity-20260903`
- Canonical issue: `#97`

## Reserved scope
Fail closed when semantic snapshot capture observes FloorId/ZoneId references that are not members of the captured project.

## Expected surfaces
- `src/QS3D.Platform.Persistence/SemanticSnapshotService.cs`
- `tests/QS3D.Platform.SmokeTests/PersistenceSnapshotModuleSmoke.cs`
- this claim file

## Excluded scope
Quantity/estimating, BricsCAD UI/native runtime, MCP, release/installer, unrelated domain/persistence behavior.

## Validation plan
- deterministic RED capture/restore round-trip regression
- valid floor/zone and null location compatibility
- exact-head platform CI and self-review of identity/cross-reference semantics

## Completion condition
Fix and regression merged after fresh exact-head GREEN, final main verified.
