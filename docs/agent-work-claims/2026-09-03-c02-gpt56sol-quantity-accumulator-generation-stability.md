# Work claim — C02 quantity accumulator generation stability

- Status: `ACTIVE`
- Agent: `c02-gpt56sol-20260903-1659`
- Registered: `2026-09-03T17:01:30+07:00`
- Baseline main SHA: `d1dd2f7d72239b99c3274166cf66a48950fee99f`
- Implementation branch: `agent/c02-gpt56sol-20260903-1659/issue-220-quantity-fact-generation`
- Integration batch: `TBD`
- Lane-Key: `issue-220`
- Ownership-Key: `quantity.accumulator.fact-generation-stability-v1`

## Reserved scope
Harden counted `QuantityAccumulator` input admission against same-Count semantic generation drift while preserving raw streaming single-pass compatibility.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityModel.cs` — `QuantityAccumulator.MaterializeFacts` and narrowly-scoped semantic replay helpers.
- `tests/QS3D.Platform.SmokeTests/QuantityAccumulatorGenerationStabilitySmoke.cs` — deterministic replacement/reorder regressions and stable/streaming controls.
- `scripts/preflight-quantity-accumulator-generation-stability.py` — direct source/regression guard if required.
- this claim file.

## Excluded scope
No Domain/Persistence, Workspace/UI, MCP/transport, release/install, Excel/IFC/BCF, BOQ, or QuantityRule changes.

## Validation plan
- RED focused smoke on current main proving same-Count replacement/reorder is currently accepted.
- GREEN focused smoke after production fix.
- Existing quantity accumulator/cardinality/provenance/order/high-dynamic-range smokes.
- Full `QS3D.Platform.SmokeTests` Release build/run and hosted exact-head CI.
- Refresh current main before merge and reject stale-head CI.

## Completion condition
Exact implementation head is fresh against current main, relevant hosted CI is GREEN, PR merges without collision, current main contains the implementation, and post-merge CI is verified.
