# Work claim — C02 quantity-summary generation stability

- Status: `ACTIVE`
- Agent: `c02-gpt56sol-20260903-2032`
- Registered: `2026-09-03T20:32:00+07:00`
- Baseline main SHA: `3221f1b64801c7e0552b6205c4e26ef39ee3156d`
- Implementation branch: `agent/c02-gpt56sol-20260903-2032/issue-247-quantity-summary-generation`
- Lane-Key: `issue-247`
- Canonical issue: `#247`

## Reserved scope
Bind counted `QuantityScheduleRow` quantity-summary input to one ordered immutable semantic generation. Same-Count replacement/reorder/value/evidence drift must fail closed while raw streaming enumerables remain single-pass.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantitySchedule.cs`
- direct deterministic smoke under `tests/QS3D.Platform.SmokeTests/`
- this coordination claim

## Excluded scope
BOQ production, Domain/Persistence production, BricsCAD UI, MCP, installer/release, unrelated quantity rules.

## Validation plan
Deterministic TDD RED first; preserve 100,000-entry admission, negative/conflicting Count rejection and raw streaming single-pass behavior; replay counted summaries comparing code, quantity dimension/value, FactCount and ElementCount; fresh exact-head Platform CI GREEN before implementation merge; exact-main verification and terminal claim closeout.

## Completion condition
Implementation is merged to current protected `main`, exact-head required CI is GREEN, current-main SHA is recorded, and this claim is terminalized.
