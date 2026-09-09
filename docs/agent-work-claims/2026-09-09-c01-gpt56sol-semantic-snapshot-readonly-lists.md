# Work claim — C01 semantic snapshot read-only lists

- Status: `ACTIVE`
- Agent: `gpt56sol-c01`
- Registered: `2026-09-09T07:57:00+07:00`
- Baseline main SHA: `7788e981f71543854ee75d8cf2fee7d5f5993a18`
- Implementation branch: `agent/gpt56sol-c01-20260909-snapshot-readonly/issue-295-semantic-snapshot-readonly-lists`
- Integration batch: `TBD`
- Lane-Key: `issue-295`
- Canonical issue: `#295`

## Reserved scope
Make persistence snapshot list surfaces actually immutable after construction. `SnapshotGuard.Copy<T>` must not publish a mutable array behind `IReadOnlyList<T>` for `SemanticProjectSnapshot` collections or `ElementSnapshot.GeneratedReferences`.

## Expected surfaces
- `src/QS3D.Platform.Persistence/SemanticSnapshotModel.cs`
- `tests/QS3D.Platform.SmokeTests/SemanticSnapshotReadonlyListsModuleSmoke.cs`
- `tests/QS3D.Platform.SmokeTests/SemanticSnapshotReadonlyListsSmokeRegistration.cs`
- this claim file

## Excluded scope
- `src/QS3D.Platform.Domain/SemanticModel.cs` and SemanticElement read-only views owned by #292
- `ElementSnapshot.Properties` immutability completed by #289
- bounded/count-drift materialization completed by #94
- Quantity/estimating, CAD/native adapters, MCP, installer/release, unrelated persistence behavior

## Validation plan
- deterministic RED proving non-empty top-level snapshot lists and generated-reference lists are mutable through a down-cast on baseline
- minimal production read-only publication preserving detached copy, insertion order, indexed access, cardinality/count-drift/null-entry guards
- exact-head Platform build/smoke CI
- self-review castability, allocation, compatibility, nullability, ordering and mutation leakage

## Completion condition
Implementation and regression are merged through reviewed PR only after fresh exact-tree Platform CI GREEN; claim is then terminal and final Platform main SHA is verified.
