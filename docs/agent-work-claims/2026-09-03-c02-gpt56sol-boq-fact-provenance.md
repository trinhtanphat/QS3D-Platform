# C02 reservation — BOQ fact-count provenance fidelity

Status: ACTIVE
Lane-Key: issue-199
Issue: #199
Canonical owner/session: account:trinhtanphat|session:gpt56sol-c02-20260903-boq-fact-evidence
Canonical carrier: agent/c02-gpt56sol-20260903-boq-fact-evidence/issue-199-boq-fact-evidence
Ownership-Key: quantity.boq.fact-count-provenance-v1
Baseline main SHA: c29fb1a83a69d8be3efb90b96f1e079e2893f8f1
Runtime: REMOTE_SAFE deterministic host-neutral .NET

Expected-Paths:
- src/QS3D.Platform.Quantity/BoqProjection.cs
- tests/QS3D.Platform.SmokeTests/BoqFactProvenanceModuleSmoke.cs
- tests/QS3D.Platform.SmokeTests/BoqElementProvenanceModuleSmoke.cs
- docs/agent-work-claims/2026-09-03-c02-gpt56sol-boq-fact-provenance.md

## Reserved scope
Preserve known `QuantitySummary.FactCount` through BOQ projection without fabricating evidence for legacy direct `BoqLine` construction. Retain the existing public constructor as a real binary-compatible overload.

## Excluded scope
No Domain/Persistence implementation changes, Workspace UI, MCP transport/runtime, installer/release, native CAD behavior, CSV schema changes, or QS3D-BricsCAD submodule pointer mutation.

## Validation plan
- deterministic regression proving two summaries differing only in FactCount currently collapse at BOQ line evidence surface;
- projected exact FactCount and ElementCount;
- legacy constructor remains available and marks FactCount unknown rather than inferred;
- source/binary constructor-shape guard;
- invalid known fact/element combinations fail closed; legitimate zero-valued fact-backed quantities remain valid;
- existing BOQ totals, precision/overflow, hostile-input, duplicate and currency smokes;
- fresh exact-head and exact-main authoritative Platform CI;
- self-review arithmetic invariants, compatibility, nullability and evidence semantics.

## Completion condition
Production carrier merged, exact-main Platform CI green, issue closed/completed and reservation terminalized.
