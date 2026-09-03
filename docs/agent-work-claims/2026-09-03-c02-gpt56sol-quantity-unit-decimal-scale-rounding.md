# Work claim — C02 exact decimal SI unit scaling

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T06:08:00Z`
- Baseline main SHA: `14a8fd326afa3ba185029dcf9c3796e2a18496f1`
- Issue: `#180`
- Lane-Key: `issue-180`
- Implementation branch: `agent/c02-gpt56sol-20260903-unit-scale-rounding/issue-180-exact-decimal-unit-scaling`
- Integration batch: `TBD`
- Ownership-Key: `quantity.units.decimal-scale-correct-rounding-v1`
- Runtime: `REMOTE_SAFE` deterministic vendor-neutral .NET; no licensed CAD runtime result required or claimed.

## Reserved scope
Correct-rounding and numeric safety of SI decimal scaling in `QuantityUnits` only.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityUnits.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityUnitDecimalScaleRoundingModuleSmoke.cs`
- this claim document

## Excluded scope
Quantity accumulator/schedule/CSV/BOQ, Domain/Persistence, BricsCAD UI/runtime, MCP, installer/release, and unrelated rule arithmetic. Existing QuantityRule unit-scale smokes may be run as compatibility evidence but are not reserved for production edits.

## Validation plan
1. TDD RED through public `QuantityUnits.ToCanonical` / `FromCanonical` using values where binary scale constants produce a one-ULP error versus exact decimal scaling.
2. Cover mm/cm, square/cubic prefixes, gram/tonne, zero/negative-zero, subnormal underflow, overflow, and reciprocal controls.
3. Implement exact decimal-rational scaling with one final binary64 nearest-even rounding while preserving fail-closed semantics.
4. Run focused smoke and broad Platform CI on the exact head; rerun QuantityRule unit-scale/product smokes for compatibility.
5. Refresh main, reconcile without force, require fresh exact-head GREEN before merge, then exact-main GREEN before marking terminal.

## Completion condition
Production fix and deterministic regression are merged into current `main`, exact-main CI is GREEN, and this claim is updated to `COMPLETED` with issue/PR/SHAs recorded.
