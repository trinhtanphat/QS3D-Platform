# C02 reservation — quantity schedule CAD source provenance fidelity

Status: COMPLETED
Lane-Key: issue-195
Issue: #195 (closed/completed)
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

## Completion evidence
- Claim-visible merge: `67414f7a6d2c4d38e2192030d57f4722b7a979c6` via PR #196; claim exact-head CI `33729526860` SUCCESS.
- Regression-only head: `b0861144e5bff8a2dc6c6fa09cfd7ea51c7741bb`; RED Platform CI `33729763075` FAILURE.
- Production exact head: `508a116f4080eef644ce86323c3816db2d40af37`; exact-head Platform CI `33730082732` SUCCESS.
- Production PR: #197.
- Production merge commit: `8a2b86a9a57bc44f7e8b6157cc6ffa42d523c511`.
- Exact-main Platform CI: `33730153410` SUCCESS on `8a2b86a9a57bc44f7e8b6157cc6ffa42d523c511`.
- Issue #195: closed/completed.

## Root cause and fix
The schedule projector enforced exact fact-to-element `CadReference` affinity but discarded that validated drawing/handle when constructing schedule rows. PR #197 preserves nullable source provenance on `QuantityScheduleRow`, retains the legacy 8-parameter constructor as a real compatibility overload, adds a source-aware overload with fail-closed validation, propagates source for populated and include-empty rows, and appends `SourceDrawingId,SourceHandle` after the prior thirteen CSV columns.

## Reservation terminalization
Production is merged and fresh exact-main CI is green. This claim is terminal and no longer reserves the listed paths.
