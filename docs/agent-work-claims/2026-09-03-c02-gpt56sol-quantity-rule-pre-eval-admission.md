# Work claim — C02 quantity rule pre-evaluation admission

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T18:38:00+07:00`
- Baseline main SHA: `defe8b731bd5f578ecccd91db9c33d2c5e8568da`
- Implementation branch: `agent/c02-gpt56sol/issue-232-rule-pre-eval-admission`
- Integration batch: `issue-232`
- Lane-Key: `c02-quantity-rule-pre-evaluation-admission-20260903`
- Issue: `#232`

## Reserved scope
C02 quantity-rule producer admission: once 100,000 facts are already admitted, deterministic non-skipping evaluation must reject before evaluating a definitely unreturnable fact. Preserve skip-missing semantics for rules that may still be skipped.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityRules.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityRulePreEvaluationAdmissionModuleSmoke.cs`
- this claim file

## Excluded scope
BOQ, quantity schedule, accumulator, Domain/Persistence, Workspace/UI, MCP, installer/release and native CAD adapters.

## Validation plan
1. Add deterministic module-initializer regression on an exact implementation head proving current production evaluates malformed input for a doomed 100001st non-skipping fact instead of failing cardinality first.
2. Move/adapt the producer admission gate so `skipRuleWhenInputMissing:false` fails before `TryEvaluate` at the ceiling while preserving `skipRuleWhenInputMissing:true` skipped-rule behavior.
3. Run focused smoke plus authoritative hosted Platform validation on the exact candidate head.
4. Reconcile latest main, require fresh exact-head GREEN, merge, and verify exact-main GREEN before terminalizing this claim.

## Runtime boundary
`REMOTE_SAFE` deterministic host-neutral .NET. No licensed BricsCAD runtime result is required or claimed.
