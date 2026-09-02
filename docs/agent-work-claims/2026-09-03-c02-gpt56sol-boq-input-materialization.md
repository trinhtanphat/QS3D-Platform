# Work claim — C02 BOQ input materialization

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T05:34:00+07:00`
- Baseline main SHA: `946fb433377582059660d19ce5115ea96504c1e4`
- Implementation branch: `agent/c02-gpt56sol-20260903-0534/issue-85-boq-input-materialization`
- Integration batch: `TBD`
- Lane-Key: `c02-boq-input-materialization-20260903`
- Issue: `#85`

## Reserved scope
C02 Quantity/Estimating commercial projection input safety: bounded materialization and cardinality-stability checks for BOQ lines, quantity summaries and unit rates.

## Expected surfaces
- `src/QS3D.Platform.Quantity/BoqProjection.cs`
- `tests/QS3D.Platform.SmokeTests/BoqInputMaterializationModuleSmoke.cs`
- this claim file

## Excluded scope
Workspace UI, MCP, release/install, Core persistence, native CAD adapters, unrelated Quantity/Excel/IFC/BCF code.

## Validation plan
- TDD RED smoke against current production behavior.
- Validate oversized advertised Count, conflicting Count evidence, post-traversal Count drift, null entries, exact 100000-entry boundary, deterministic ordering and unchanged commercial arithmetic.
- Run authoritative hosted CI on the exact candidate head and merge only when fresh GREEN.

## Completion condition
Implementation and deterministic regression are merged to current `main`, exact-main evidence is acceptable, and this claim is marked `COMPLETED`.
