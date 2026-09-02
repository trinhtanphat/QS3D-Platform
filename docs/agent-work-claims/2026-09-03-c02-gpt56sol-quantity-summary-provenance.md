# Work claim — C02 QuantitySummary provenance invariants

- Status: `COMPLETED`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T04:05:00+07:00`
- Baseline main SHA: `5c60cae7faa3667d8f27b14d5066ddde5f9e3fc8`
- Implementation branch: `agent/c02-gpt56sol/issue-66-summary-provenance`
- Integration batch: `TBD`
- Lane-Key: `c02-quantity-summary-provenance-20260903`
- Canonical issue: `#66`
- Implementation merge: `1ff38ad7bcb5974055e6a1b5936bc7a54fe9d6eb`
- Exact-main CI: `33683688667` SUCCESS

## Reserved scope
Reject public `QuantitySummary` instances whose fact/element counts cannot correspond to any real set of quantity facts, preventing impossible provenance from reaching schedules, CSV, or commercial projection.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityModel.cs`
- `tests/QS3D.Platform.SmokeTests/QuantitySummaryProvenanceModuleSmoke.cs`
- this coordination claim

## Excluded scope
Quantity-rule arithmetic, unit conversion, BOQ rate arithmetic, UI/MCP/Core persistence, release/install.

## Validation plan
- TDD RED for `elementCount > factCount`
- TDD RED for zero facts with nonzero element count or nonzero quantity
- preserve empty zero summary
- preserve zero-valued summaries backed by facts
- preserve normal accumulator output
- authoritative exact-head CI and exact-main verification

## Completion condition
Impossible summary provenance fails closed, legitimate accumulator/direct summaries remain compatible, implementation merges, and exact-main CI is green.
