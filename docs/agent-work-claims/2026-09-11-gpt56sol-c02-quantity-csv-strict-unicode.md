# Work claim — Quantity Schedule CSV strict Unicode fidelity

- Status: `ACTIVE`
- Agent: `gpt56sol-c02-20260911-quantity-csv-strict-unicode`
- Registered: `2026-09-11T07:20:00+07:00`
- Baseline main SHA: `addac93f62530a326baa5f162dfd0d6ad7769a1b`
- Implementation branch: `agent/gpt56sol-c02-20260911/quantity-csv-strict-unicode`
- Integration batch: `TBD`

## Reserved scope
Fail closed when Quantity Schedule CSV fields contain malformed UTF-16 instead of silently budgeting/emitting replacement-character semantics, while preserving deterministic CSV quoting, CRLF, formula neutralization, provenance and the existing 16 MiB output ceiling.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityScheduleCsv.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityScheduleCsvUnicodeFidelityModuleSmoke.cs`
- existing Quantity Schedule CSV smokes only if compatibility needs a direct regression update
- this claim file for terminal status only

## Reproduced defect
Current `QuantityScheduleCsv.CountUtf8Bytes` treats unpaired high/low surrogates as three UTF-8 bytes for U+FFFD. `Write` therefore accepts malformed semantic/provenance text and returns a .NET string containing the malformed code unit; any normal UTF-8 publication replaces it, silently changing field identity/evidence bytes instead of failing closed.

## Validation plan
1. RED-first executable smoke for unpaired high/low surrogates in semantic and provenance CSV fields, plus valid supplementary Unicode fidelity.
2. Minimal strict UTF-16/UTF-8 admission in the same byte-accounting path before append; keep checked byte accounting and 16 MiB ceiling.
3. Run all Quantity Schedule CSV safety/cardinality/provenance/evidence/output-budget smokes plus full Platform smoke/build.
4. Self-review formula neutralization, normalized CRLF, quote expansion, deterministic ordering, numeric `R` formatting, count limits and output-budget boundaries.

## Completion condition
Implementation is reviewed and merged to Platform `main`, exact-main CI is green, this claim is terminalized, then the consuming QS3D-BricsCAD submodule pin is advanced through its own Reservation-v2 carrier.