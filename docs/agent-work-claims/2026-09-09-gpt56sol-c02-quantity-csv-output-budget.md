# Work claim — Quantity Schedule CSV bounded output

- Status: `COMPLETED`
- Agent: `gpt56sol-c02-20260909-quantity-csv-output-budget`
- Registered: `2026-09-09T13:09:50+07:00`
- Baseline main SHA: `94b83326aa882fd4ae54738cba36a308bc39ace7`
- Implementation branch: `agent/gpt56sol-c02-20260909/quantity-csv-output-budget`
- Integration batch: `PR #299 / merge 728aa5779e3b1b0a30dae641359d95688228f50f`

## Reserved scope
Bound `QuantityScheduleCsv.Write` output growth before final string publication while preserving deterministic CSV fidelity, spreadsheet formula neutralization, provenance and existing cardinality semantics.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityScheduleCsv.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityScheduleCsvOutputBudgetModuleSmoke.cs`
- `tests/QS3D.Platform.SmokeTests/Program.cs` only if registration is required by the current smoke harness
- this claim file for terminal status only

## Reproduced defect
`QuantityScheduleCsv.Write` validated output record cardinality but then materialized the entire CSV in an unbounded `StringBuilder` and returned `output.ToString()`. Semantically valid long names/codes/provenance across admitted records could therefore amplify managed memory/output without a byte budget. Existing formula neutralization and deterministic ordering did not bound this resource surface.

## Completed implementation
`QuantityScheduleCsv.Write` now enforces a 16 MiB total emitted UTF-8 budget, reserves each complete field before append, directly emits normalized CRLF and doubled quotes without whole-field replacement copies, preserves spreadsheet-active-text neutralization, deterministic ordering, invariant quantity formatting, provenance and empty-row semantics, and uses checked netstandard2.0-safe UTF-8 byte accounting.

## Verification
- RED test-only head: `8ee43bd0506aa3938d17dcc895c938cac9a70abd`; CI `34318253147` failed exactly because the over-budget valid CSV returned successfully.
- First production attempt `7223f840f9f5d17f7ae8997149c4be43f99d8553`; CI `34318406581` exposed a netstandard2.0 API incompatibility in byte counting. No tests were weakened.
- Corrected exact head: `bfb3f19e9b34608febb415ce90d56b20a8ccb64f`; CI `34318534912` completed SUCCESS.
- PR #299 merged with expected-head guard to Platform `main` as `728aa5779e3b1b0a30dae641359d95688228f50f`, and exact main was verified at that SHA before this terminal update.

## Excluded scope
BOQ arithmetic, quantity-rule math, XLSX/IFC/BCF, native CAD/runtime, persistence, release/signing, and parent-repository submodule integration remain excluded from this upstream implementation claim.

## Completion condition
Satisfied for the upstream Platform implementation. Parent `QS3D-BricsCAD` submodule integration remains a separate Reservation-v2 carrier and must not reuse this claim.
