# Work claim — semantic snapshot property ordering

- Status: `ACTIVE`
- Agent: `c01-gpt56sol`
- Registered: `2026-09-15T03:36:07+07:00`
- Baseline main SHA: `8b0206d3b74e7bbb3c02ed71ac4c5aeef47d5efd`
- Canonical issue: `#304`
- Implementation branch: `agent/c01-gpt56sol/issue-304-semantic-snapshot-property-order`
- Integration batch: `TBD`

## Reserved scope
C01 persistence determinism for semantic snapshot property materialization. Canonicalize persisted `ElementSnapshot.Properties` independently of live-domain insertion history.

## Expected surfaces
- `src/QS3D.Platform.Persistence/SemanticSnapshotModel.cs`
- `src/QS3D.Platform.Persistence/SemanticSnapshotService.cs` only if required by the root-cause fix
- `tests/QS3D.Platform.SmokeTests/SemanticSnapshotPropertyOrderDeterminismModuleSmoke.cs`
- `tests/QS3D.Platform.SmokeTests/Program.cs` registration only if required
- direct claim/validation metadata for issue #304

## Excluded scope
- live `SemanticElement` dictionary semantics unless a regression proves the persistence-boundary fix cannot be made safely
- Quantity/BOQ/CSV/C02 surfaces
- CAD host/runtime behavior
- parent `QS3D-BricsCAD` carrier #7078

## Validation plan
- RED regression: two logically equivalent elements with the same IDs/property values but opposite insertion order must materialize the same Ordinal property-key sequence
- targeted Persistence smoke
- full Platform smoke/preflight/build
- self-review duplicate normalized keys, nullability, materialization drift, compatibility, allocations and ordering
- fresh exact-head CI before integration

## Completion condition
Issue #304 is implemented on its dedicated branch, fresh exact-head Platform CI is green, implementation is integrated into current Platform `main`, and the final main SHA is verified.
