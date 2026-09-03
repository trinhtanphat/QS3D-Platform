# C02 reservation — quantity schedule CSV provenance fidelity

Status: ACTIVE
Lane-Key: issue-188
Issue: #188
Canonical owner/session: account:trinhtanphat|session:gpt56sol-c02-20260903-schedule-csv-provenance
Canonical carrier: agent/c02-gpt56sol-20260903-schedule-csv-provenance/issue-188-schedule-csv-provenance
Ownership-Key: quantity.schedule.csv.provenance-fidelity-v1
Runtime: REMOTE_SAFE deterministic host-neutral .NET

Expected-Paths:
- src/QS3D.Platform.Quantity/QuantityScheduleCsv.cs
- tests/QS3D.Platform.SmokeTests/QuantityScheduleCsvProvenanceFidelityModuleSmoke.cs
- tests/QS3D.Platform.SmokeTests/QuantityScheduleCsvEmptyRowFidelityModuleSmoke.cs
- tests/QS3D.Platform.SmokeTests/QuantityScheduleCsvSecurityModuleSmoke.cs
- docs/agent-work-claims/2026-09-03-c02-gpt56sol-quantity-schedule-csv-provenance.md

## Reservation / collision check
No active C02 implementation PR was found on fresh Platform main `9705fc47b8bd2e0e6fe8829506eaba013d9e63f0`. The abandoned sparse-materialization claim #155/#156 is explicitly not-planned and is not reused. This carrier does not touch Domain/Persistence, UI, MCP, release, or other lane production code.

## Root cause
`QuantityScheduleRow` retains element kind, family identity/name and optional floor/zone identity after projector affinity validation, but `QuantityScheduleCsv.Write` emits only element ID/name plus quantity fields. Canonical CSV export therefore destroys validated row provenance. Issue #165's stated empty-row contract included element kind, but its landed smoke/schema omitted it.

## Contract
Keep the existing first six CSV columns/order for compatibility and append deterministic provenance columns. Preserve null location as blank, GUID `D` formatting, spreadsheet neutralization for family name, CRLF/quoting, record ceiling, empty-row behavior and deterministic ordering.
