# Work claim — C02 schedule code/dimension affinity

- Status: `COMPLETED`
- Agent: `gpt56sol-c02-20260903`
- Registered: `2026-09-03T22:01:45+07:00`
- Baseline main SHA: `dabc7c99f7811245e58f194af36cf77a00404ee9`
- Implementation branch: `agent/gpt56sol-c02-20260903/issue-259-schedule-code-dimension-affinity`
- Integration batch: `PR #261`
- Lane-Key: `issue-259`

## Reserved scope
C02 Quantity / schedule/export integrity only. Reject ambiguous reuse of one quantity code for multiple dimensions within a public `QuantityScheduleRow` ingestion boundary.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantitySchedule.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityScheduleCodeDimensionAffinityModuleSmoke.cs`
- this claim file

## Excluded scope
No Domain/Persistence production, Workspace/UI, MCP, release/installer, BOQ pricing arithmetic, or unrelated feature code.

## Validation evidence
- regression-only head `fc465d35dee1408f740c8e46211ab46c7a6f911a`: Platform CI `33770366258` RED on deterministic acceptance of same-code/different-dimension summaries;
- production head `9eb2cefb54c6bbac128aad3f7ddac901596626c5`: netstandard2.0 guard correctly rejected `Dictionary.TryAdd` in CI `33770517277`;
- compatibility-repaired exact head `140eb1a76be97b0c1a9052a8e422361e5d74e323`: Platform CI `33770728300` GREEN;
- implementation PR #261 merged as `2941727f0f973ddc4819553404fb29c8f7d3ea71` and remained in the subsequent protected-main ancestry.

## Completion
Implementation is merged; code/dimension ambiguity now fails closed while valid distinct codes and existing generation/evidence guards remain intact. Issue #259 may be closed `completed` after this closeout lands.
