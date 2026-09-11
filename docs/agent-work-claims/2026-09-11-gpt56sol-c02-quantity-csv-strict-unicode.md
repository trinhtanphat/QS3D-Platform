# Work claim — Quantity Schedule CSV strict Unicode fidelity

- Status: `COMPLETED`
- Agent: `gpt56sol-c02-20260911-quantity-csv-strict-unicode`
- Registered: `2026-09-11T07:20:00+07:00`
- Baseline main SHA: `addac93f62530a326baa5f162dfd0d6ad7769a1b`
- Implementation branch: `agent/gpt56sol-c02-20260911/quantity-csv-strict-unicode`
- Integration batch: `PR #302 / merge e5cc3a825efeb8b921bc06ed7d02caa611878934`

## Reserved scope
Fail closed when Quantity Schedule CSV fields contain malformed UTF-16 instead of silently budgeting/emitting replacement-character semantics, while preserving deterministic CSV quoting, CRLF, formula neutralization, provenance and the existing 16 MiB output ceiling.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityScheduleCsv.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityScheduleCsvUnicodeFidelityModuleSmoke.cs`
- this claim file for terminal status only

## Reproduced defect and fix
`QuantityScheduleCsv.CountUtf8Bytes` treated unpaired high/low surrogates as three UTF-8 bytes for U+FFFD. The implementation now rejects malformed UTF-16 in the existing pre-append byte-accounting path while preserving valid surrogate pairs and all existing CSV fidelity/safety contracts.

## Verification
- RED test-only head: `18e69efc30e284489a215e80591f02188a1b5e1c`; Platform CI `34546695767` failed in authoritative validation with production untouched.
- Final implementation head: `d29329a8d64320bf1663842ce91669265c543e48`; Platform CI `34546841779` completed SUCCESS.
- PR #302 merged with expected-head guard to Platform `main` as `e5cc3a825efeb8b921bc06ed7d02caa611878934`.
- Self-review removed unrelated formatting churn before the final GREEN head.

## Completion condition
Satisfied for upstream Platform implementation. The consuming QS3D-BricsCAD submodule integration remains a separate Reservation-v2 carrier and must independently qualify its exact parent-repository head.