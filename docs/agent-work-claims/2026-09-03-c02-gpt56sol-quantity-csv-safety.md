# Work claim — Quantity CSV spreadsheet-safety hardening

- Status: `ACTIVE`
- Agent: `c02-gpt56sol-20260903`
- Registered: `2026-09-03T00:34:25+07:00`
- Baseline main SHA: `b9592366909df8e8d44ef01cf574507bf96787d9`
- Implementation branch: `agent/c02-gpt56sol/quantity-csv-safety-20260903`
- Integration batch: `integration/c02-quantity-csv-safety-20260903`

## Reserved scope

Harden host-neutral quantity schedule CSV export against spreadsheet formula injection and related deterministic CSV safety regressions without changing quantity calculation semantics.

## Expected surfaces

- `src/QS3D.Platform.Quantity/QuantityScheduleCsv.cs`
- deterministic quantity CSV smoke coverage under `tests/QS3D.Platform.SmokeTests/`
- smoke registration only if required by the existing harness
- this claim file for terminal status

## Excluded scope

- quantity rule arithmetic and unit conversion unless a directly coupled CSV defect requires it
- parity/cost lifecycle code under `src/QS3D.Platform.Parity/`
- native BricsCAD/AutoCAD runtime behavior
- release/install/CI infrastructure

## Validation plan

- prove current CSV output leaves formula-leading untrusted text executable in spreadsheet consumers
- cover `=`, `+`, `-`, `@`, leading whitespace before formula markers, quoting, embedded quotes/newlines and benign values
- preserve invariant-culture numeric output, canonical units and deterministic row ordering
- run focused smoke and repository validation available to the session
- self-review for compatibility and data-integrity effects

## Completion condition

Untrusted textual CSV cells are deterministically neutralized for spreadsheet consumers, benign CSV semantics remain stable, regression coverage passes, the implementation is integrated through the repository PR path, and the claim is marked terminal after exact-main verification.
