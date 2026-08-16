# Cost benchmark representable large-value overflow — claim

- Status: ACTIVE / implementation pushed; awaiting exact-head CI.
- Issue: #32.
- Agent/session: `chatgpt-gpt56sol`.
- Baseline: `08671d236a112d15852e7f52cc199ad81828bc31` (PR #31 head; Platform CI #130 SUCCESS).
- Branch: `agent/chatgpt-gpt56sol/fix-cost-benchmark-large-average-20260816`.

## Defect

`CostBenchmarkService.Analyze` summed all non-negative unit costs before division and added the two middle values before dividing for an even median. Multiple valid values near `decimal.MaxValue` could therefore overflow an intermediate even when the requested average/median was representable.

## Fix boundary

- incremental average: `average += (value - average) / count`;
- even median: `low + (high - low) / 2`;
- normal filtering, candidate deviation, ordering and result contract remain unchanged;
- deterministic smoke covers two `decimal.MaxValue` samples yielding exact `decimal.MaxValue` average and median.

No native CAD, UI, file/network, vendor behavior or main write is in scope. Stop before merge.