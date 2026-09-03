# Work claim — C02 quantity rule generation stability

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T21:35:00+07:00`
- Baseline main SHA: `074074e7517b64ac303205b1c58992a3793151cd`
- Implementation branch: `agent/c02-gpt56sol-20260903/issue-251-rule-generation-stability`
- Integration batch: `issue-251`
- Lane-Key: `issue-251`
- Issue: `#251`

## Reserved scope
Harden caller-controlled counted quantity-rule factor/catalog inputs against same-cardinality semantic generation replacement/reordering while preserving single-pass behavior for unknown-count streaming inputs.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityRules.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityRuleGenerationStabilityModuleSmoke.cs`
- this claim file

## Excluded scope
BOQ, quantity schedule, accumulator, Domain/Persistence, Workspace/UI, MCP, release/installer, native CAD adapters.

## Validation plan
- deterministic RED smoke proving same-count factor and catalog semantic drift is currently accepted;
- production fix with ordered semantic replay only when Count evidence exists;
- regression for replacement, reorder, Count evidence stability and unknown-count single-pass semantics;
- fresh exact-head Platform CI before merge; exact-main CI after merge.

## Completion condition
Implementation is merged to current main with exact-tree GREEN CI and this claim is terminalized as `COMPLETED`.
