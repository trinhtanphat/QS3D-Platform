# Work claim — C02 quantity rule pre-evaluation admission

- Status: `COMPLETED`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T18:38:00+07:00`
- Baseline main SHA: `defe8b731bd5f578ecccd91db9c33d2c5e8568da`
- Implementation branch: `agent/c02-gpt56sol/issue-232-rule-pre-eval-admission`
- Integration batch: `issue-232`
- Lane-Key: `c02-quantity-rule-pre-evaluation-admission-20260903`
- Issue: `#232`
- Claim PR: `#233`
- Implementation PR: `#234`
- Regression SHA: `ac3d687e20eb25aa7541d6a2b411597e9bb6347a`
- Production SHA: `84882158aefc971bceed92a1b7497f804135ce08`
- Reconciled exact head: `afb0ff65d92541339dde6cbc61daf5a020ada304`
- Implementation merge commit: `948af5b6e9b892991612f34e3bd80eaa12d7c241`

## Reserved scope
C02 quantity-rule producer admission: once 100,000 facts are already admitted, deterministic non-skipping evaluation must reject before evaluating a definitely unreturnable fact. Preserve skip-missing semantics for rules that may still be skipped.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityRules.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityRulePreEvaluationAdmissionModuleSmoke.cs`
- this claim file

## Excluded scope
BOQ, quantity schedule, accumulator, Domain/Persistence, Workspace/UI, MCP, installer/release and native CAD adapters.

## Validation evidence
- Claim-only exact head `061b6efc22fd44add0801bba6d2a9bd3d3207aad`: CI `33750791672` SUCCESS; claim PR #233 merged as `653b21d300cdac8ff1a5a48de32df2e650b00690`.
- Deterministic regression-only exact head `ac3d687e20eb25aa7541d6a2b411597e9bb6347a`: CI `33750927102` FAILURE in authoritative validation, proving current code evaluated malformed input for the doomed 100001st fact.
- Production commit `84882158aefc971bceed92a1b7497f804135ce08`: adds the non-skipping pre-evaluation admission gate while retaining the existing post-evaluation gate for skip-missing semantics.
- Reconciled exact head `afb0ff65d92541339dde6cbc61daf5a020ada304`: CI `33751217412` SUCCESS.
- Implementation PR #234 merged with expected-head binding as `948af5b6e9b892991612f34e3bd80eaa12d7c241`.
- Exact implementation-main CI `33751307099` SUCCESS on `948af5b6e9b892991612f34e3bd80eaa12d7c241`.

## Completion
`QuantityRuleEngine.Evaluate` now fails closed before evaluating an impossible 100001st rule when `skipRuleWhenInputMissing:false`. Exactly 100,000 facts remain supported. For `skipRuleWhenInputMissing:true`, missing-input rules can still be skipped beyond the boundary and the existing post-evaluation gate rejects the next producing rule before insertion. Arithmetic, exact rounding, provenance, deterministic rule/element ordering, catalog indexing, and immutable result exposure are unchanged.

Self-review also corrected the regression to use fixed ascending `ElementId` GUIDs because evaluation orders elements by identifier; no nondeterministic RED evidence was reused.

## Runtime boundary
`REMOTE_SAFE` deterministic host-neutral .NET. No licensed BricsCAD runtime result is required or claimed.
