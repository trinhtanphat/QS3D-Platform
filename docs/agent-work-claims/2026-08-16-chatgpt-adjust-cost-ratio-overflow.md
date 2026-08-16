# Adjust Cost representable ratio-overflow fix — claim

- Status: ACTIVE / implementation pushed; awaiting exact-head CI.
- Issue: #36.
- Agent/session: `chatgpt-gpt56sol`.
- Baseline: `675fd29ee0e75d4bcbf09e2b7f128be249e4ba12` (PR #35 head; Platform CI #132 SUCCESS).
- Branch: `agent/chatgpt-gpt56sol/fix-adjust-cost-ratio-overflow-20260816`.

## Defect

`CostAdjustmentService.ByRatio` multiplied by the percentage factor before dividing by 100. That can overflow a decimal intermediate even when the final adjusted total is representable.

## Fix boundary

- preserve the original checked multiply-then-divide path when it is representable;
- on `OverflowException` only, retry as divide-then-multiply;
- final non-representable results still fail closed through checked arithmetic;
- ordinary ratio validation, result/delta semantics and `ToTarget` stay unchanged;
- smoke covers `decimal.MaxValue @ -50%` and `(decimal.MaxValue / 2) @ +50%`, plus the existing normal 10% case.

No native CAD, UI, vendor, file/network or main write is in scope. Stop before merge.