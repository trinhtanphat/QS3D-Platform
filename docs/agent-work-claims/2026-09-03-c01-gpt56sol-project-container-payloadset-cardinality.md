# Work claim — C01 project container payload-set cardinality

- Status: `ACTIVE`
- Agent: `c01-gpt56sol`
- Registered: `2026-09-03T19:30:00+07:00`
- Baseline main SHA: `f2a6af9a881587cecea6293d08e46a536a2c0d79`
- Implementation branch: `agent/c01-gpt56sol-20260903-1930/issue-122-payloadset-cardinality`
- Integration batch: `TBD`
- Lane-Key: `issue-122`

## Reserved scope
C01 Persistence validation safety for caller-provided `ProjectContainerManifestValidator.ValidatePayloadSet` dictionaries: bounded enumeration and stable cardinality evidence before normalized payload/hash validation.

## Expected surfaces
- `src/QS3D.Platform.Persistence/ProjectContainerManifest.cs`
- `tests/QS3D.Platform.SmokeTests/ProjectContainerPayloadSetCardinalityModuleSmoke.cs`
- this claim file

## Excluded scope
Manifest constructor admission already completed in #118, container schema/wire format, payload hashing algorithm, Quantity, BricsCAD UI/runtime, MCP, release/install infrastructure, and unrelated persistence refactors.

## Validation plan
- deterministic RED for oversized advertised Count before traversal;
- enumeration overrun, Count/enumeration mismatch, and post-traversal Count drift;
- preserve normalized duplicate-name, unexpected/missing payload, byte-length/hash validation semantics;
- full authoritative Platform CI on exact candidate head.

## Completion condition
Implementation merged through reviewed PR, exact candidate CI GREEN, merge commit verified on current main, and post-merge evidence recorded.
