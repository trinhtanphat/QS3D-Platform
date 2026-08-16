# Agent work claim — Cubicost TBQ 360-degree price check

Date: 2026-08-16 (UTC+7)
Status: ACTIVE
Issue: #18
Agent/session: chatgpt-gpt56sol
Branch: `agent/chatgpt-gpt56sol/cubicost-tbq-360-price-check-20260816`

## Baseline

This lane is intentionally stacked on the open shared Cubicost parity PR #15 at `a5778f4abcf3b5c308c5d6854040dbc0c3082390`. Platform `main` at registration was `7d20229ac12b6d41c90f75f08cc361ee76372635`.

## Reserved scope

- `src/QS3D.Platform.Parity/TbqPriceReferenceParity.cs`
- `tests/QS3D.Platform.SmokeTests/TbqPriceReferenceParitySmoke.cs`
- minimum smoke registration in `tests/QS3D.Platform.SmokeTests/Program.cs`
- `scripts/check-cubicost-tbq-360-price-check.py`
- minimum validation wiring in `scripts/validate.sh`
- `docs/CUBICOST-TBQ-360-PRICE-CHECK.md`
- this claim file

## Behavior

Implement only the public, clean-room TBQ reference-check contract: BQ/UR marks, Check Linking Rate, Check BQ Reversely and rates-not-adopted-in-BQ review. No vendor UI/assets/source, no native CAD types, no printing/import workflow, no direct main write.

## Handoff rule

Validate the exact task branch, refresh PR #15 before PR creation, and stop before merge. If #15 changes incompatibly, reconcile on this task branch rather than overwriting upstream work.
