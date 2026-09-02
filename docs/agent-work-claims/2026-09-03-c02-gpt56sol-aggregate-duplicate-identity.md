# Work claim — C02 aggregate duplicate identity safety

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T03:43:02+07:00`
- Baseline main SHA: `750aa7a95f1c868838d4432b0e1bbba606ed3c7b`
- Implementation branch: `agent/c02-gpt56sol/issue-57-aggregate-duplicate-identity`
- Integration batch: `TBD`
- Lane-Key: `c02-aggregate-container-duplicate-identity-20260903`
- Canonical issue: `#57`

## Reserved scope
Fail closed on duplicate aggregate identities at public Quantity schedule and BOQ container boundaries so direct ingestion/deserialization cannot duplicate exported evidence or commercial totals.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantitySchedule.cs`
- `src/QS3D.Platform.Quantity/BoqProjection.cs`
- `tests/QS3D.Platform.SmokeTests/AggregateDuplicateIdentityModuleSmoke.cs`
- this coordination claim

## Excluded scope
Quantity rule arithmetic, unit conversion, MCP/UI/Core persistence, release/install, unrelated export formats.

## Validation plan
- TDD RED for duplicate `(Code, Dimension)` summaries within one schedule row
- TDD RED for duplicate `ElementId` schedule rows
- TDD RED for duplicate `(Code, Dimension)` BOQ lines and inflated totals
- explicit null-entry behavior and valid deterministic ordering regression
- exact-head hosted CI and post-merge exact-main CI

## Completion condition
All three public container boundaries reject ambiguous duplicate identities before export/summing, valid ordering remains deterministic, exact-head CI is green, implementation is merged to current `main`, and exact-main CI is verified.
