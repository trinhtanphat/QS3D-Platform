# Cost percentage arithmetic overflow — claim

- Status: ACTIVE / implementation pushed; awaiting exact-head CI.
- Issue: #38.
- Agent/session: `chatgpt-gpt56sol`.
- Baseline: `9f5663fa39a4261dd2e53daf0fccf128f591e9ff` (PR #37 head; Platform CI #133 SUCCESS).
- Branch: `agent/chatgpt-gpt56sol/fix-cost-percentage-overflow-20260816`.

## Defects

Representable cost results could fail because overhead, profit and progress retention multiplied by their percentage before division by 100.

## Fix boundary

- central internal `CostPercentageMath.Of` preserves checked multiply-then-divide fast behavior and retries divide-then-multiply only after intermediate overflow;
- `CostRateBuildUp` overhead/profit, `CostAdjustmentService.ByRatio`, and `ProgressClaimService` retention share the same bounded arithmetic contract;
- final non-representable results still fail closed;
- ordinary validation and business semantics are unchanged;
- smoke covers large representable overhead, profit, retention/net plus all prior ordinary/Adjust Cost cases.

No native CAD, UI, vendor, file/network or main write is in scope. Stop before merge.