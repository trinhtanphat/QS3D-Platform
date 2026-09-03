# Work claim — C02 BOQ commercial input generation stability

- Status: `ACTIVE`
- Agent: `c02-gpt56sol-20260903-1954`
- Registered: `2026-09-03T19:54:00+07:00`
- Baseline main SHA: `4fd05d7499c0777c2afae75c40bd7e1b84dc4256`
- Implementation branch: `agent/c02-gpt56sol-20260903-1954/issue-243-boq-generation`
- Lane-Key: `issue-243`
- Canonical issue: `#243`

## Reserved scope
Bind counted BOQ unit-rate, quantity-summary, and BQ-line inputs to one ordered immutable commercial generation. Same-Count replacement/reorder/evidence/price/currency drift fails closed while raw streaming enumerables remain single-pass.

## Expected surfaces
- `src/QS3D.Platform.Quantity/BoqProjection.cs`
- `tests/QS3D.Platform.SmokeTests/BoqGenerationStabilitySmoke.cs`
- this coordination claim

## Excluded scope
QuantityRule production, Domain/Persistence production, BricsCAD UI, MCP, installer/release.

## Validation contract
Deterministic TDD RED first; bounded Count observation; ordered semantic replay; existing 100,000-entry admission, duplicate-key checks, exact decimal conversion, currency and total validation preserved; fresh exact-head Platform CI GREEN before implementation merge; exact-main CI and terminal claim closeout.
