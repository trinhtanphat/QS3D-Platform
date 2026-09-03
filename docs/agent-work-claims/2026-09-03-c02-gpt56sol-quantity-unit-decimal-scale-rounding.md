# Work claim — C02 exact decimal SI unit scaling

- Status: `COMPLETED`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T06:08:00Z`
- Baseline main SHA: `14a8fd326afa3ba185029dcf9c3796e2a18496f1`
- Issue: `#180`
- Lane-Key: `issue-180`
- Implementation branch: `agent/c02-gpt56sol-20260903-unit-scale-rounding/issue-180-exact-decimal-unit-scaling`
- Implementation PR: `#182`
- Regression-only RED head: `e132957b7a8bc78ae366665dd41ddf5443b60861`
- RED CI: `33721813278` — `FAILURE`
- Production head: `aba0da9332ebd210fd30f7fd95a16453c9c481a0`
- Exact-head CI: `33722002385` — `SUCCESS`
- Merge commit: `6abef77e254b5a29b077a6e2d9fb4a8b43a2e4cc`
- Exact-main CI: `33722076151` — `SUCCESS`
- Ownership-Key: `quantity.units.decimal-scale-correct-rounding-v1`
- Runtime: `REMOTE_SAFE` deterministic vendor-neutral .NET; no licensed CAD runtime result required or claimed.

## Reserved scope
Correct-rounding and numeric safety of SI decimal scaling in `QuantityUnits` only.

## Landed surfaces
- `src/QS3D.Platform.Quantity/QuantityUnits.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityUnitDecimalScaleRoundingModuleSmoke.cs`
- this claim document

## Result
SI prefix scaling no longer treats rounded binary decimal constants as exact. Supported decimal scales are represented as integer powers of ten, using one division for negative powers and one multiplication for positive powers. Existing non-negative finite admission, positive-zero normalization, finite overflow refusal and positive-nonzero underflow refusal remain intact.

The regression pins exact bit outcomes across mm/cm, square/cubic subunits and gram, plus tonne/zero/overflow/underflow controls. Broad authoritative Platform CI, including existing QuantityRule unit-scale/product smokes, passed on the production head and exact merged main.

## Excluded scope
Quantity accumulator/schedule/CSV/BOQ, Domain/Persistence, BricsCAD UI/runtime, MCP, installer/release, and unrelated rule arithmetic.

## Completion condition
Satisfied: implementation is reachable from `main` at merge `6abef77e254b5a29b077a6e2d9fb4a8b43a2e4cc` and exact-main CI `33722076151` is GREEN.