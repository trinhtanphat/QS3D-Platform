# Work claim — Quantity Schedule CSV bounded output

- Status: `ACTIVE`
- Agent: `gpt56sol-c02-20260909-quantity-csv-output-budget`
- Registered: `2026-09-09T13:09:50+07:00`
- Baseline main SHA: `94b83326aa882fd4ae54738cba36a308bc39ace7`
- Implementation branch: `agent/gpt56sol-c02-20260909/quantity-csv-output-budget`
- Integration batch: `TBD`

## Reserved scope
Bound `QuantityScheduleCsv.Write` output growth before final string publication while preserving deterministic CSV fidelity, spreadsheet formula neutralization, provenance and existing cardinality semantics.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityScheduleCsv.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityScheduleCsvOutputBudgetModuleSmoke.cs`
- `tests/QS3D.Platform.SmokeTests/Program.cs` only if registration is required by the current smoke harness
- this claim file for terminal status only

## Reproduced defect
`QuantityScheduleCsv.Write` validates output record cardinality but then materializes the entire CSV in an unbounded `StringBuilder` and returns `output.ToString()`. Semantically valid long names/codes/provenance across admitted records can therefore amplify managed memory/output without a byte budget. Existing formula neutralization and deterministic ordering do not bound this resource surface.

## Excluded scope
BOQ arithmetic, quantity-rule math, XLSX/IFC/BCF, native CAD/runtime, persistence, release/signing, and parent-repository submodule integration are excluded from this upstream implementation claim.

## Validation plan
1. RED-first deterministic smoke proving an admitted schedule can exceed a bounded UTF-8 CSV output contract on current source.
2. Minimal production implementation that enforces an explicit total UTF-8 output budget before appending beyond it, avoids whole-field replacement amplification where practical, and preserves exact RFC-style quoting/CRLF and spreadsheet-safety semantics.
3. Existing CSV safety/cardinality/provenance/empty-row/evidence smokes plus full Platform smoke/CI.
4. Self-review deterministic ordering, Unicode byte accounting, quote/newline expansion, formula-prefix expansion, overflow, and no partial returned output.
5. Final reviewed integration and exact-main verification before marking this claim completed.

## Completion condition
The bounded CSV behavior and regression coverage are merged to Platform `main`, exact-main CI is green, and the claim is terminalized without weakening existing CSV semantics.
