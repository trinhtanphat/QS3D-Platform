# C02 reservation — quantity schedule CSV summary evidence fidelity

Status: ACTIVE
Lane-Key: issue-191
Issue: #191
Canonical owner/session: account:trinhtanphat|session:gpt56sol-c02-20260903-schedule-csv-evidence
Canonical carrier: agent/c02-gpt56sol-20260903-schedule-csv-evidence/issue-191-schedule-csv-evidence
Ownership-Key: quantity.schedule.csv.summary-evidence-fidelity-v1
Baseline main SHA: 892c423c8e0102041d5793d86a35e71d0ee866b2
Runtime: REMOTE_SAFE deterministic host-neutral .NET

Expected-Paths:
- src/QS3D.Platform.Quantity/QuantityScheduleCsv.cs
- tests/QS3D.Platform.SmokeTests/QuantityScheduleCsvSummaryEvidenceFidelityModuleSmoke.cs
- tests/QS3D.Platform.SmokeTests/QuantityScheduleCsvProvenanceFidelityModuleSmoke.cs
- tests/QS3D.Platform.SmokeTests/QuantityScheduleCsvEmptyRowFidelityModuleSmoke.cs
- docs/agent-work-claims/2026-09-03-c02-gpt56sol-quantity-schedule-csv-summary-evidence.md

## Reserved scope
Preserve `QuantitySummary.FactCount` and `ElementCount` in canonical schedule CSV without changing the existing eleven positional columns from issue #188. Empty schedule rows must retain blank evidence fields.

## Excluded scope
No Domain/Persistence, Workspace UI, MCP runtime/transport, installer/release, or unrelated Quantity changes. No QS3D-BricsCAD submodule pointer mutation in this reservation.

## Validation plan
- deterministic behavioral regression proving schedules that differ only in fact cardinality no longer collapse to identical CSV;
- empty-row blank evidence fields;
- existing CSV provenance/security/cardinality smokes;
- full Platform authoritative validation and fresh exact-head CI;
- self-review deterministic ordering, invariant integer formatting, formula-injection boundary, CRLF/quoting and compatibility.

## Completion condition
Production carrier merged, fresh exact-main Platform CI green, issue closed/completed, and this reservation terminalized with exact evidence.
