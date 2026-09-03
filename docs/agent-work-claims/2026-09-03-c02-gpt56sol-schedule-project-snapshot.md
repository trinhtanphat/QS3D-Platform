# C02 reservation — quantity schedule project snapshot

Status: COMPLETED
Lane-Key: issue-203
Issue: #203
Canonical owner/session: account:trinhtanphat|session:gpt56sol-c02-20260903-schedule-project-snapshot
Canonical carrier: agent/c02-gpt56sol-20260903-schedule-project-snapshot/issue-203-schedule-project-snapshot
Ownership-Key: quantity.schedule.project-snapshot-v1
Baseline main SHA: 6f14bd79e311f52a56c03f9be4943642255b451b
Runtime: REMOTE_SAFE deterministic host-neutral .NET

Expected-Paths:
- src/QS3D.Platform.Quantity/QuantitySchedule.cs
- tests/QS3D.Platform.SmokeTests/QuantityScheduleProjectSnapshotModuleSmoke.cs
- docs/agent-work-claims/2026-09-03-c02-gpt56sol-schedule-project-snapshot.md

## Reserved scope
Make `QuantityScheduleProjector.Project` operate on one immutable project projection snapshot captured before executing caller-controlled quantity-fact enumeration. Prevent hostile enumeration from injecting later project state into the in-flight schedule or making the output-row admission check stale.

## Excluded scope
No Domain/Persistence implementation changes, Workspace UI, MCP transport/runtime, installer/release, BricsCAD native behavior, BOQ schema changes or QS3D-BricsCAD submodule pointer mutation.

## Validation plan
- deterministic hostile enumerable adds a second project element during fact enumeration;
- in-flight output remains bound to entry snapshot and does not contain the injected element;
- captured family/floor/zone/source provenance is stable through the same mutation boundary;
- existing hostile Count/cardinality validation remains fail-closed;
- deterministic order, empty-row behavior and 100k output bound remain unchanged;
- exact-head and exact-main authoritative Platform CI;
- self-review snapshot depth, mutable reference leakage, compatibility, ordering, maximum-record arithmetic and evidence fidelity.

## Completion evidence
- Reservation claim PR #204 merged after exact-head Platform CI 33731889533 GREEN; claim was visible on main at `c225de1ca6012d35ce65cb14180032868261d654`.
- Deterministic RED: Platform CI 33732085682 on `6d8155ee01bd621d69d72e96c5a4e93130831a3a`; Platform build succeeded with 0 warnings / 0 errors, then the new smoke failed because live source provenance changed during hostile fact enumeration.
- Exact production head GREEN: Platform CI 33732325679 on `f3e713c4a54a07dfe94f56ce4886f1388f011e06`.
- Production PR: #205.
- Production merge commit: `2e3f282555c8c45bb1ce921bb8e5332d2e567c87`.
- Exact production-main GREEN: Platform CI 33732408050 on `2e3f282555c8c45bb1ce921bb8e5332d2e567c87`.
- Issue #203 closed as completed after exact-main verification.

## Completion condition
Satisfied: production carrier merged, exact-main Platform CI green, issue closed/completed and reservation terminalized by this claim-only closeout.
