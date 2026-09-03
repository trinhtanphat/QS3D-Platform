# Work claim — C02 BOQ code/dimension affinity

- Status: `ACTIVE`
- Agent: `gpt56sol-c02-20260903`
- Registered: `2026-09-03T22:08:10+07:00`
- Baseline main SHA: `2941727f0f973ddc4819553404fb29c8f7d3ea71`
- Implementation branch: `agent/gpt56sol-c02-20260903/issue-262-boq-code-dimension-affinity`
- Integration batch: `TBD`
- Lane-Key: `issue-262`

## Reserved scope
C02 commercial/BOQ semantic identity integrity. Reject reuse of one quantity code across multiple dimensions at direct BOQ-line, quantity-summary, and unit-rate ingestion boundaries.

## Expected surfaces
- `src/QS3D.Platform.Quantity/BoqProjection.cs`
- `tests/QS3D.Platform.SmokeTests/BoqCodeDimensionAffinityModuleSmoke.cs`
- this claim file

## Excluded scope
No Domain/Persistence production, Workspace/UI, MCP, release/installer, schedule production, or unrelated arithmetic changes.

## Validation plan
- deterministic regression-only RED first;
- cover direct `BoqProjection`, `BoqProjector` quantity summaries, and rates;
- preserve exact arithmetic, currency, missing-rate behavior, generation stability, duplicate same-key diagnostics, bounds, ordering, and netstandard2.0 compatibility;
- exact-head hosted Platform CI GREEN before merge.

## Completion condition
Implementation merged to current main, exact-main validation checked, claim marked COMPLETED, and issue #262 closed completed.
