# Agent work claim — Element Code share overflow

- Date: 2026-08-16
- Agent/session: `chatgpt-gpt56sol`
- Status: ACTIVE / STOP BEFORE MERGE
- Issue: #40
- Branch: `agent/chatgpt-gpt56sol/fix-element-share-overflow-20260816`
- Exact stacked base: `a482359b7d642d48aae062a8f6b1186ae74e26c6` (PR #39, Platform CI #134 SUCCESS)

## Scope

- `src/QS3D.Platform.Parity/TbqElementAnalysisParity.cs`
- `tests/QS3D.Platform.SmokeTests/TbqElementAnalysisParitySmoke.cs`
- this claim only

## Contract

Preserve current Element Code grouping, node filtering, checked aggregate totals, area handling and deterministic ordering. Correct only the representable-result overflow caused by multiplying a row cost by 100 before dividing by total cost.

Regression: one valid row with `Cost == decimal.MaxValue` and therefore `TotalCost == decimal.MaxValue` must return exactly `SharePercent == 100m` without overflow.

## Boundary

No vendor/native/UI/file/network changes. No direct main write, force-push or merge. Fresh exact-head Platform CI SUCCESS is required before PR_READY.