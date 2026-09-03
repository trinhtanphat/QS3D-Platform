# Work claim — C02 quantity schedule fact generation stability

- Status: `COMPLETED`
- Agent: `c02-gpt56sol-20260903-1939`
- Registered: `2026-09-03T19:39:00+07:00`
- Baseline main SHA: `2ecf8821f172b48ba4b5a3bcb570a7d9ec94fb58`
- Implementation branch: `agent/c02-gpt56sol-20260903-1939/issue-239-schedule-fact-generation`
- Lane-Key: `issue-239`
- Canonical issue: `#239`
- Implementation PR: `#241`
- TDD RED commit: `c5e03cf3f7b169449d90488840956a42222d82be`
- Production commit: `9e5403f3cd77212982fc397f9f4929c6130a2778`
- Final implementation head: `7c420e56d9061c25f8f4c8b306caefcb9aa72951`
- Implementation merge commit: `6bc6bb765d2852da8133b11e19661a9c177934e5`

## Completed scope
Counted `QuantityScheduleProjector` fact inputs are now bound to one ordered immutable semantic generation. Same-Count replacement, reorder, quantity drift, and CAD provenance drift fail closed; raw streaming enumerables remain single-pass.

## Changed implementation surfaces
- `src/QS3D.Platform.Quantity/QuantitySchedule.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityScheduleFactGenerationStabilitySmoke.cs`
- this coordination claim

## Excluded scope preserved
BOQ/rate arithmetic, QuantityRule production, Domain/Persistence production, BricsCAD UI, MCP, installer/release.

## Validation evidence
- TDD RED: Platform CI `33756816235` on exact regression head `c5e03cf3f7b169449d90488840956a42222d82be` — authoritative validation failed as expected before the production fix.
- Fresh exact implementation head: Platform CI `33757087827` on `7c420e56d9061c25f8f4c8b306caefcb9aa72951` — `SUCCESS`, authoritative validation GREEN.
- Exact implementation main: Platform CI `33757286975` on merge SHA `6bc6bb765d2852da8133b11e19661a9c177934e5` — `SUCCESS`.

## Regression contract
Coverage pins same-Count replacement, same-Count reorder, CAD provenance drift, stable counted input arithmetic/evidence, bounded Count observations, and single-pass compatibility for raw streaming facts. Existing 100,000-entry admission and project snapshot/source-affinity behaviors remain under the broader authoritative suite.

## Runtime
`REMOTE_SAFE` host-neutral .NET. No licensed/native CAD runtime claim.
