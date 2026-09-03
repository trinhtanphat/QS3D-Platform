# Work claim — C02 quantity CSV output cardinality

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T12:15:00+07:00`
- Baseline main SHA: `f32e9ccded477ee5f567337745eb8bcce9f9a444`
- Implementation branch: `agent/c02-gpt56sol/issue-168-csv-output-cardinality`
- Integration batch: `TBD`
- Lane-Key: `c02-csv-output-cardinality-20260903`
- Canonical issue: `#168`

## Reserved scope
Bound aggregate CSV data-record cardinality before body generation so individually valid schedule rows cannot expand into an unbounded in-memory export.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityScheduleCsv.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityScheduleCsvOutputCardinalityModuleSmoke.cs`
- this coordination claim

## Excluded scope
CSV import (none exists), XLSX/IFC/BCF feature invention, Quantity arithmetic, BOQ/commercial math, Domain/Core persistence, UI, MCP, installer/release.

## Validation plan
1. deterministic RED at the real 100,000-record boundary;
2. exact boundary remains admitted and record 100,001 fails closed;
3. preflight aggregate output count using overflow-safe arithmetic before allocating/writing CSV body;
4. populated summaries count one record each and intentionally empty rows count one record each;
5. preserve ordering, formula-injection neutralization, canonical CRLF and #165 empty-row fidelity;
6. exact-head and exact-main CI GREEN before completion.

## Completion condition
Implementation merged to current main with fresh exact-head GREEN and exact-main GREEN evidence, then claim terminalized.
