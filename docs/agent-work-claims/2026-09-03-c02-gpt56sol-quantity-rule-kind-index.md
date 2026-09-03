# Work claim — C02 quantity rule kind index

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T17:39:00+07:00`
- Baseline main SHA: `38806c291fd0c5324f620a2d9ced30267e6e2c21`
- Implementation branch: `agent/c02-gpt56sol/issue-228-rule-kind-index`
- Integration batch: `issue-228`
- Lane-Key: `c02-quantity-rule-kind-index-20260903`
- Issue: `#228`

## Reserved scope
C02 quantity-rule catalog lookup/evaluation resource safety: eliminate repeated full-catalog scans and per-element rule-view allocations while preserving deterministic ordered rule semantics and immutable/public read-only exposure.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityRules.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityRuleKindIndexModuleSmoke.cs`
- this claim file

## Excluded scope
BOQ materialization (`BoqProjection.cs`, issue #85), quantity schedule, accumulator, Domain project persistence/state, Workspace/UI, MCP, installer/release, native CAD adapters.

## Validation plan
- TDD regression first: repeated `ForKind` lookup must reuse a stable read-only per-kind view while retaining deterministic ordering and mutation resistance.
- Exercise high-cardinality `QuantityRuleEngine.Evaluate` against repeated same-kind elements and existing output limits.
- Run targeted smoke and authoritative hosted validation on the exact candidate head; reconcile latest main without force push; merge only after fresh GREEN.

## Completion condition
Implementation and regression are merged into current `main`, exact-main CI is acceptable, issue #228 is completed, and this claim is terminalized without weakening existing cardinality/numeric/provenance guards.
