# Work claim — C02 quantity accumulator cardinality

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T04:39:00+07:00`
- Baseline main SHA: `a58eb102127fbe2a58d01ada650cfa24ab0128e3`
- Implementation branch: `agent/c02-gpt56sol/issue-78-accumulator-cardinality`
- Integration batch: `issue-78`
- Lane-Key: `issue-78`

## Reserved scope
Bound direct `QuantityAccumulator.Summarize` caller-controlled fact enumeration before LINQ grouping/materialization.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityModel.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityAccumulatorCardinalityModuleSmoke.cs`
- smoke registration only if required

## Excluded scope
Quantity schedule implementation reserved by issue #75, Quantity rules/catalog, Workspace/UI, MCP, Core persistence, installer/release.

## Validation plan
- TDD hostile streamed limit+1 RED;
- exact-limit and Count/enumerator drift coverage;
- null fact and normal deterministic accumulation regression;
- exact-head hosted Platform CI.

## Completion condition
Fresh exact-head CI GREEN, reviewed PR merge, exact resulting main SHA verified, claim closed only after integration.
