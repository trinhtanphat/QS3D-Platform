# Work claim — C02 quantity rule generation stability

- Status: `COMPLETED`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T21:35:00+07:00`
- Baseline main SHA: `074074e7517b64ac303205b1c58992a3793151cd`
- Implementation branch: `agent/c02-gpt56sol-20260903/issue-251-rule-generation-stability`
- Integration batch: `issue-251`
- Lane-Key: `issue-251`
- Issue: `#251`
- Regression SHA: `4679f6c65b2d4cb7a1634e0329ab54d6846d6a1e`
- Production SHA: `905c88be559059726bf7e2f4927e628be16457d7`
- Final implementation head: `a13911c8050ea17739c96b068d48e2268845bb35`
- Implementation PR: `#253`
- Merge commit: `2e07f4e421354422f74e93933d4c66586eaa13eb`

## Reserved scope
Harden caller-controlled counted quantity-rule factor/catalog inputs against same-cardinality semantic generation replacement/reordering while preserving single-pass behavior for unknown-count streaming inputs.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityRules.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityRuleGenerationStabilityModuleSmoke.cs`
- this claim file

## Excluded scope
BOQ, quantity schedule, accumulator, Domain/Persistence, Workspace/UI, MCP, release/installer, native CAD adapters.

## Validation evidence
- Claim-only head `9cf7c4271baa64da94a1066e86ef8138f61bd9e0`: Platform CI `33767788093` SUCCESS before reservation merge.
- Regression-only head `4679f6c65b2d4cb7a1634e0329ab54d6846d6a1e`: Platform CI `33768000990` FAILURE after a clean build because same-count factor replacement was accepted unexpectedly.
- Final implementation head `a13911c8050ea17739c96b068d48e2268845bb35`: Platform CI `33768410893` SUCCESS.
- Implementation merge commit `2e07f4e421354422f74e93933d4c66586eaa13eb`: exact-main push CI `33768505013` SUCCESS.

## Completion
Counted quantity-rule factor and catalog sources now require stable ordered semantic generations around materialization; replacement/reordering fails closed, existing Count limits remain enforced, and raw streaming inputs remain single-pass.
