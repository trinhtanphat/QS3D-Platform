# Agent work claim — Cubicost TBQ Resource Library batch import

Date: 2026-08-16 (UTC+7)
Status: ACTIVE
Issue: #24
Agent/session: chatgpt-gpt56sol
Branch: `agent/chatgpt-gpt56sol/cubicost-tbq-resource-library-20260816`

## Baseline

Stacked on Build-up Analysis PR #23 at `841db29bafd7b65dc34f3c79beb342f2ad7e398f`, after #15 -> #19 -> #21.

## Public evidence boundary

Public Glodon TBQ material lists `Batch Import from RL`; the current TBQ product page describes the Resource Library as storing build-up rate details, and public TBQC training shows `Import from project` followed by `Batch Import from RL` in Build-up Unit Rates.

Public material retrieved for this lane does not specify a fuzzy/automatic matching algorithm. The implementation therefore reserves only explicit, exact resource-rate selection and must not guess hidden vendor behavior.

## Reserved scope

- `src/QS3D.Platform.Parity/TbqResourceLibraryParity.cs`
- `tests/QS3D.Platform.SmokeTests/TbqResourceLibraryParitySmoke.cs`
- minimum registration in `tests/QS3D.Platform.SmokeTests/Program.cs`
- `scripts/check-cubicost-tbq-resource-library.py`
- minimum `scripts/validate.sh` wiring
- `docs/CUBICOST-TBQ-RESOURCE-LIBRARY.md`
- this claim file

## Boundary

Host-neutral cost-domain logic only. Reuse `CostRateBuildUp`; deterministic explicit batch selection; duplicate/missing/null input fail-closed; no file/network/native SDK/UI/persistence side effects; no fuzzy matching; no direct `main` write or merge.

## Handoff

Open a stacked PR to #23 only after exact branch CI passes. Preserve dependency order and stop before merge.
