# Work claim — C02 quantity calculated result readonly exposure

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T09:33:00+07:00`
- Baseline main SHA: `273a1bc990ec3404ead996b8f91e2b5d6790687d`
- Implementation branch: `agent/c02-gpt56sol-20260903-0933/issue-134-calculated-result-readonly`
- Integration batch: `TBD`
- Lane-Key: `c02-quantity-calculated-result-readonly-20260903`
- Issue: `#134`

## Reserved scope
C02 immutable public result exposure for calculated quantity summaries and rule-evaluated quantity facts.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityModel.cs`
- `src/QS3D.Platform.Quantity/QuantityRules.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityCalculatedResultReadonlyModuleSmoke.cs`
- `tests/QS3D.Platform.SmokeTests/Program.cs`
- this claim file

## Excluded scope
BOQ/schedule readonly work already completed, numeric algorithms, Domain/Core production, BricsCAD UI, MCP, release/install.

## Validation plan
- deterministic RED proving both advertised `IReadOnlyList<T>` results expose mutable backing collections;
- production fix preserving ordering, identity, numeric/provenance semantics and public signatures;
- focused smoke and broad exact-head Platform CI;
- self-review empty results, mutation attempts, ordering and downstream compatibility;
- merge only on fresh exact-head GREEN, then verify exact-main GREEN.

## Completion condition
Both public calculated quantity result collections are non-mutable through concrete backing-storage casts, with deterministic regression evidence and exact-main GREEN.