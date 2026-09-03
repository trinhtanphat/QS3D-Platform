# Work claim — C02 schedule cross-row code/dimension affinity

- Status: `ACTIVE`
- Agent: `gpt56sol-c02`
- Registered: `2026-09-04T02:38:00+07:00`
- Baseline main SHA: `224d61000e5292fdb9d0158a460e86a8c3b55ffb`
- Implementation branch: `agent/gpt56sol-c02/issue-276-schedule-cross-row-dimension-affinity`
- Integration batch: `TBD`
- Lane-Key: `issue-276`

## Reserved scope
C02 Quantity schedule aggregate code/dimension affinity across element rows and the deterministic regression for issue #276.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantitySchedule.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityScheduleCrossRowDimensionAffinityModuleSmoke.cs`
- this claim file

## Excluded scope
No Domain/Persistence production, BricsCAD UI, MCP, installer/release, BOQ arithmetic, CSV schema, or unrelated quantity-rule production.

## Validation plan
- deterministic regression proving cross-row same-code/different-dimension rejection;
- valid same-code/same-dimension multi-element and distinct-code schedules;
- projector reproduction with different-element facts;
- focused smoke, solution build, repository preflight, fresh exact-head hosted CI.

## Completion condition
Production and regression are merged to latest upstream `main`, fresh exact-head CI is green, and the claim is closed only after the merged main SHA is verified.
