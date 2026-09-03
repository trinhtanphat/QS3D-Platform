# Work claim — C01 domain element location affinity

- Status: `ACTIVE`
- Agent: `c01-gpt56sol`
- Registered: `2026-09-03T07:32:45+07:00`
- Baseline main SHA: `4292c2186dc5c8fbe8394908a168609f3dd7a008`
- Implementation branch: `agent/c01-gpt56sol-20260903-0732/issue-106-domain-element-location-affinity`
- Integration batch: `TBD`
- Lane-Key: `c01-domain-element-location-affinity-20260903`

## Reserved scope
C01 Domain project admission semantics for `SemanticProject.AddElement`: non-null FloorId/ZoneId must belong to the same project before element insertion.

## Expected surfaces
- `src/QS3D.Platform.Domain/SemanticModel.cs`
- `tests/QS3D.Platform.SmokeTests/SemanticProjectLocationAffinityModuleSmoke.cs`
- this claim file

## Excluded scope
Persistence schema/serialization, Quantity, BricsCAD UI/runtime, MCP, release/install infrastructure, and unrelated domain refactors.

## Validation plan
- deterministic regression proving missing floor and missing zone are rejected;
- prove rejected admission does not partially insert the element;
- preserve null location and existing family/kind admission behavior;
- full authoritative Platform CI on exact candidate head.

## Completion condition
Implementation merged through reviewed PR, exact candidate CI GREEN, merge commit verified on current main, and post-merge evidence recorded.
