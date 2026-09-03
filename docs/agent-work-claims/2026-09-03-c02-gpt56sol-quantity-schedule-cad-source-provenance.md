# C02 reservation — quantity schedule CAD source provenance fidelity

Status: ACTIVE
Lane-Key: issue-195
Issue: #195
Canonical owner/session: account:trinhtanphat|session:gpt56sol-c02-20260903-schedule-cad-source
Canonical carrier: agent/c02-gpt56sol-20260903-schedule-cad-source/issue-195-schedule-cad-source
Ownership-Key: quantity.schedule.cad-source-provenance-fidelity-v1
Baseline main SHA: 162d2e723d735f734f3cfecc49064ee2a7dc1cd3
Runtime: REMOTE_SAFE deterministic host-neutral .NET

Expected-Paths:
- src/QS3D.Platform.Quantity/QuantitySchedule.cs
- src/QS3D.Platform.Quantity/QuantityScheduleCsv.cs
- tests/QS3D.Platform.SmokeTests/QuantityScheduleCadSourceProvenanceModuleSmoke.cs
- tests/QS3D.Platform.SmokeTests/QuantityScheduleCsvCadSourceFidelityModuleSmoke.cs
- tests/QS3D.Platform.SmokeTests/QuantityScheduleCsvSummaryEvidenceFidelityModuleSmoke.cs
- tests/QS3D.Platform.SmokeTests/QuantityScheduleCsvProvenanceFidelityModuleSmoke.cs
- tests/QS3D.Platform.SmokeTests/QuantityScheduleCsvEmptyRowFidelityModuleSmoke.cs
- docs/agent-work-claims/2026-09-03-c02-gpt56sol-quantity-schedule-cad-source-provenance.md

## Reserved scope
Preserve the already-validated nullable semantic-element `CadReference` through `QuantityScheduleRow` and canonical CSV export, without reordering any existing CSV columns.

## Excluded scope
No Domain/Persistence implementation changes, Workspace UI, MCP runtime/transport, installer/release, native CAD behavior or QS3D-BricsCAD submodule pointer mutation.

## Validation plan
- deterministic RED proving source-backed projected schedule/export loses source drawing/handle today;
- direct-row backward compatibility via optional trailing source argument;
- null source fidelity and non-null deterministic DrawingId/handle export;
- preserve source-affinity fail-closed behavior from #108/#113;
- existing CSV provenance/evidence/empty/security/cardinality smokes;
- full authoritative Platform CI, fresh exact-head and exact-main evidence;
- self-review nullable provenance, constructor validation, positional compatibility, deterministic formatting and evidence immutability.

## Completion condition
Production carrier merged, exact-main Platform CI green, issue closed/completed and this reservation terminalized.
