# C02 reservation — quantity schedule CSV provenance fidelity

Status: COMPLETED
Lane-Key: issue-188
Issue: #188 (closed/completed)
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

## Completion evidence
- Regression-only head: `04d9b90fd1b7f71473c875eb641b8f5aad2e7f7f`
- RED Platform CI: `33724196653` FAILURE on authoritative validation
- Production exact head: `89421f69fdc00b074fed8763bbf4fea4ff496147`
- Exact-head Platform CI: `33724317468` SUCCESS
- Production PR: #189
- Production merge commit: `0233e7083bd745b7f00787086c3b7f54c5c7a9cc`
- Exact-main Platform CI: `33724419251` SUCCESS on `0233e7083bd745b7f00787086c3b7f54c5c7a9cc`
- Issue #188: closed/completed

## Root cause and fix
`QuantityScheduleRow` retains element kind, family identity/name and optional floor/zone identity after projector affinity validation, but canonical CSV previously emitted only element ID/name plus quantity fields. PR #189 preserves the original six columns as a positional prefix and appends `ElementKind`, `FamilyId`, `FamilyName`, `FloorId`, `ZoneId`; family free text uses the existing spreadsheet-active-text neutralization, nullable location is blank, and GUIDs use deterministic `D` format.

## Reservation terminalization
The production carrier is merged and exact-main CI is green. This claim is terminal and no longer reserves the listed paths.
