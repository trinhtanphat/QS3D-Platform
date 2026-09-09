# Work claim — C01 persistence read-only lists

- Status: `ACTIVE`
- Agent: `gpt56sol-c01`
- Registered: `2026-09-09T07:57:00+07:00`
- Baseline main SHA: `7788e981f71543854ee75d8cf2fee7d5f5993a18`
- Implementation branch: `agent/gpt56sol-c01-20260909-snapshot-readonly/issue-295-semantic-snapshot-readonly-lists`
- Integration batch: `PR #297`
- Lane-Key: `issue-295`
- Canonical issue: `#295`

## Reserved scope
Make C01 persistence list surfaces actually immutable after construction. `SnapshotGuard.Copy<T>` must not publish mutable arrays behind `IReadOnlyList<T>` for `SemanticProjectSnapshot` collections or `ElementSnapshot.GeneratedReferences`; `ProjectContainerManifest.Payloads` must likewise not publish its sorted mutable backing array after manifest validation.

## Expected surfaces
- `src/QS3D.Platform.Persistence/SemanticSnapshotModel.cs`
- `src/QS3D.Platform.Persistence/ProjectContainerManifest.cs`
- `tests/QS3D.Platform.SmokeTests/SemanticSnapshotReadonlyListsModuleSmoke.cs`
- `tests/QS3D.Platform.SmokeTests/ProjectContainerManifestReadonlyPayloadsModuleSmoke.cs`
- this claim file

## Excluded scope
- `src/QS3D.Platform.Domain/SemanticModel.cs` and SemanticElement read-only views owned by #292
- `ElementSnapshot.Properties` immutability completed by #289
- bounded/count-drift snapshot materialization completed by #94
- project-container cardinality/admission behavior already completed by earlier C01 carriers
- Quantity/estimating, CAD/native adapters, MCP, installer/release, unrelated persistence behavior

## Validation evidence / plan
- deterministic test-only RED head `a95ff14116d4db0de36452a78d9ddfadc37a9b5b`: Platform CI `34297554027` built the solution with 0 warnings / 0 errors, then failed in `ProjectContainerManifestReadonlyPayloadsModuleSmoke` because indexed mutation did not throw `NotSupportedException`
- minimal production read-only publication preserving detached copy, insertion/sorted ordering, indexed access, cardinality/count-drift/null-entry guards and normalized manifest identity
- fresh exact-head Platform build/smoke CI after production fix
- self-review castability, allocation, compatibility, nullability, ordering and mutation leakage

## Completion condition
Implementation and regression are merged through reviewed PR only after fresh exact-tree Platform CI GREEN; claim is then terminal and final Platform main SHA is verified.
