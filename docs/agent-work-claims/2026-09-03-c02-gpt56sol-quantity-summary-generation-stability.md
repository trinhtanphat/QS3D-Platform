# Work claim — C02 quantity-summary generation stability

- Status: `COMPLETED`
- Agent: `c02-gpt56sol-20260903-2032`
- Registered: `2026-09-03T20:32:00+07:00`
- Baseline main SHA: `3221f1b64801c7e0552b6205c4e26ef39ee3156d`
- Implementation branch: `agent/c02-gpt56sol-20260903-2032/issue-247-quantity-summary-generation`
- Lane-Key: `issue-247`
- Canonical issue: `#247`
- Regression-only head: `948e7fa5722de6e1f434f7a31652f1b1137307bd`
- Regression RED: Platform CI `33761892992`
- Final implementation head: `922281b63cc1c5d4b06e5085a967e0ffeaa338a8`
- Exact-head GREEN: Platform CI `33762101969`
- Implementation merge: `53b2ce99d31cae1896406c2cb07c4e984dcbc3af`

## Reserved scope
Bind counted `QuantityScheduleRow` quantity-summary input to one ordered immutable semantic generation. Same-Count replacement/reorder/value/evidence drift fails closed while raw streaming enumerables remain single-pass.

## Delivered
`QuantityScheduleMaterializer.MaterializeStableQuantitySummaries` preserves existing cardinality admission, replays counted inputs in original order, and compares Code, Quantity (dimension/value), FactCount and ElementCount before the schedule row accepts the snapshot. Uncounted streaming enumerables remain one-pass.

## Validation
The regression-only carrier failed authoritative validation on exact head `948e7fa5722de6e1f434f7a31652f1b1137307bd` in CI run `33761892992`. The production head `922281b63cc1c5d4b06e5085a967e0ffeaa338a8` passed fresh exact-head Platform CI run `33762101969` before merge. PR #249 merged with expected-head binding as `53b2ce99d31cae1896406c2cb07c4e984dcbc3af`.

## Excluded scope
BOQ production, Domain/Persistence production, BricsCAD UI, MCP, installer/release, unrelated quantity rules.

## Runtime
`REMOTE_SAFE` host-neutral .NET. No licensed CAD runtime evidence is required or claimed.
