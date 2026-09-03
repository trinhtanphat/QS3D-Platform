# Work claim — C02 QuantityAccumulator code/dimension affinity

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T22:34:00+07:00`
- Baseline main SHA: `a9a86c029f774b089043fe893f0f7e42bb86c64b`
- Implementation branch: `agent/c02-gpt56sol/issue-267-quantity-accumulator-code-dimension-affinity`
- Integration batch: `TBD`
- Lane-Key: `issue-267`
- Canonical issue: `#267`

## Reserved scope
Fail closed in `QuantityAccumulator.Summarize` when direct/deserialized quantity facts reuse one semantic quantity code across multiple dimensions, while preserving deterministic exact accumulation and provenance behavior.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityModel.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityAccumulatorCodeDimensionAffinityModuleSmoke.cs`
- this coordination claim

## Excluded scope
QuantitySchedule/BOQ production code, Domain/Persistence, Workspace UI, MCP, release/install, unrelated Excel/IFC/BCF surfaces.

## Validation plan
- deterministic TDD RED for same-code mixed-dimension facts;
- verify same-element and cross-element ambiguity, ordering independence, valid same-dimension aggregation and distinct-code compatibility;
- run targeted smoke plus repository CI on exact head;
- self-review exact accumulation, Count/generation stability, provenance checks, 100,000 ceiling and deterministic ordering.

## Completion condition
Implementation is merged through the reviewed PR path, current main contains the fix and regression, and fresh exact-main CI is green.
