# Work claim — C02 quantity accumulator generation stability

- Status: `COMPLETED`
- Agent: `c02-gpt56sol-20260903-1659`
- Registered: `2026-09-03T17:01:30+07:00`
- Baseline main SHA: `d1dd2f7d72239b99c3274166cf66a48950fee99f`
- Implementation branch: `agent/c02-gpt56sol-20260903-1659/issue-220-quantity-fact-generation`
- Lane-Key: `issue-220`
- Ownership-Key: `quantity.accumulator.fact-generation-stability-v1`
- Implementation PR: `#222`
- Implementation merge SHA: `c9a2975c360c469b99d9377eb11375879e5a73ed`
- Exact-head CI: `33742347173` — GREEN
- Exact-main CI: `33742525271` — GREEN

## Reserved scope
Harden counted `QuantityAccumulator` input admission against same-Count semantic generation drift while preserving raw streaming single-pass compatibility.

## Landed surfaces
- `src/QS3D.Platform.Quantity/QuantityModel.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityAccumulatorGenerationStabilitySmoke.cs`
- this claim file

## Verified behavior
Counted inputs are replay-validated against the complete ordered immutable `QuantityFact` semantics before aggregation. Replacement, reorder, quantity-value and CAD-provenance drift fail closed. Raw non-counted streaming inputs remain single-pass compatible. Existing cardinality, 100k admission, provenance, exact accumulation and null-fact checks remain covered by authoritative validation.

## TDD evidence
Regression head `57ec08dd87c5b7fafffc1e404f3169e0c20c0bab` failed CI run `33742176104` only after a clean Release build because same-count replacement was accepted. Production/self-review head `3a27cfb45c7ff0048e717a0c81f2b31e62a22d3e` passed CI run `33742347173`. Merge SHA `c9a2975c360c469b99d9377eb11375879e5a73ed` passed push CI run `33742525271`.

## Excluded scope
No Domain/Persistence, Workspace/UI, MCP/transport, release/install, Excel/IFC/BCF, BOQ, or QuantityRule changes.

## Runtime
`REMOTE_SAFE` deterministic host-neutral .NET. No licensed BricsCAD runtime evidence required or claimed.
