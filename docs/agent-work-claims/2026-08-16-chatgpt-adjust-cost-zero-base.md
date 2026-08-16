# Agent work claim — Adjust Cost zero-base target ratio

Date: 2026-08-16 (UTC+7)
Status: ACTIVE
Issue: #30
Agent/session: chatgpt-gpt56sol
Branch: `agent/chatgpt-gpt56sol/fix-adjust-cost-zero-base-20260816`

## Baseline

Stacked on BQ Library PR #29 exact head `cbad2754fa74f18153a1015ec23ed6e06172ab30`, after #15 -> #19 -> #21 -> #23 -> #25 -> #27.

## Verified defect

`CostAdjustmentService.ToTarget(0m, positiveTarget)` previously returned a synthetic `RatioPercent = 100m`. No finite percentage applied to a zero original total can produce a positive target, so that result is mathematically invalid and unsafe for downstream planning.

## Reserved scope

- `src/QS3D.Platform.Parity/CostLifecycleParity.cs`
- `tests/QS3D.Platform.SmokeTests/CubicostSharedParitySmoke.cs`
- this claim file

## Fix boundary

- keep `ToTarget(0, 0)` valid with ratio 0%;
- reject `ToTarget(0, positive)` with `InvalidOperationException`;
- preserve normal positive-base target calculation and `ByRatio` behavior;
- do not infer or implement TBQ `Mk Up Ratio` combination rules without authoritative formula evidence;
- no native SDK/UI/file/network behavior and no direct `main` write or merge.

## Validation

Fresh pull-request CI on the exact branch head must pass authoritative Platform validation before PR_READY.