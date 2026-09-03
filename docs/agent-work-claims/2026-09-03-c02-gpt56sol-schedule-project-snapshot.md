# C02 reservation — quantity schedule project snapshot

Status: ACTIVE
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

## Completion condition
Production carrier merged, exact-main Platform CI green, issue closed/completed and reservation terminalized.
