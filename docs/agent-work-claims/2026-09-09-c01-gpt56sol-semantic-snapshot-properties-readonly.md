# Work claim — C01 semantic snapshot property immutability

- Status: `ACTIVE`
- Agent: `c01-gpt56sol`
- Registered: `2026-09-09T05:47:00+07:00`
- Baseline main SHA: `5c1b650e475af3c823ae38aaf1a4a85f41985457`
- Implementation branch: `agent/c01-gpt56sol/issue-289-semantic-snapshot-properties-readonly`
- Integration batch: `TBD`
- Lane-Key: `c01-semantic-snapshot-properties-readonly-20260909`
- Canonical issue: `#289`

## Reserved scope
Make `ElementSnapshot.Properties` actually immutable after snapshot construction so callers cannot down-cast the advertised `IReadOnlyDictionary<string,string>` to a mutable dictionary and alter persisted snapshot state after validation/materialization.

## Expected surfaces
- `src/QS3D.Platform.Persistence/SemanticSnapshotModel.cs`
- `tests/QS3D.Platform.SmokeTests/SemanticSnapshotMaterializationModuleSmoke.cs`
- this claim file

## Excluded scope
Quantity/estimating, CAD adapters/native runtime, BricsCAD repository code, MCP, installer/release, unrelated Domain/Persistence behavior.

## Validation plan
- deterministic RED regression proving `ElementSnapshot.Properties` exposes a mutable dictionary on baseline
- minimal production read-only wrapper/copy preserving ordinal identity and detached-copy behavior
- platform build/smoke validation on exact candidate
- self-review castability, nullability, cardinality/generation checks, allocation/compatibility, ordering and mutation leakage

## Completion condition
Implementation and regression are merged through reviewed PR with exact-tree Platform CI GREEN, claim updated terminal, and final platform main SHA verified.
