# Work claim — C02 quantity accumulator replay Count bound

- Status: `ACTIVE`
- Agent: `c02-gpt56sol-20260903-1659`
- Registered: `2026-09-03T17:09:15+07:00`
- Baseline main SHA: `4b96688005d8c10e0b68ddfc0c57c71356ef91db`
- Implementation branch: `agent/c02-gpt56sol-20260903-1659/issue-224-replay-count-bound`
- Lane-Key: `issue-224`
- Ownership-Key: `quantity.accumulator.replay-count-amplification-v1`

## Reserved scope
Bound caller-controlled Count observations during counted QuantityAccumulator semantic replay without weakening generation-stability admission.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityModel.cs` — `RequireStableFactGeneration` only.
- `tests/QS3D.Platform.SmokeTests/QuantityAccumulatorGenerationStabilitySmoke.cs` — deterministic bounded-Count regression.
- this claim file.

## Excluded scope
No schedule/BOQ/rules, Domain/Persistence, Workspace/UI, MCP/transport, release/install.

## Validation plan
- RED regression proving current replay over-reads Count.
- GREEN bounded Count access while replacement/reorder/provenance drift still fail closed.
- stable counted + raw streaming controls.
- full authoritative Platform validation and fresh exact-head CI.

## Completion condition
Implementation is merged from a fresh GREEN exact head, current main contains it, push CI is GREEN, and the claim is terminalized.
