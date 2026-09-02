# Work claim — C02 BOQ double/decimal fidelity

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T03:34:37+07:00`
- Baseline main SHA: `e41ece7ec00194591c36df29e82eec61a0c67f5b`
- Implementation branch: `agent/c02-gpt56sol/issue-54-boq-double-decimal-fidelity`
- Integration batch: `TBD`
- Lane-Key: `c02-boq-double-decimal-fidelity-20260903`
- Canonical issue: `#54`

## Reserved scope
C02 quantity/commercial projection precision: preserve invariant round-trip quantity fidelity when projecting canonical `double` quantities into decimal BOQ totals.

## Expected surfaces
- `src/QS3D.Platform.Quantity/BoqProjection.cs`
- direct Quantity deterministic smoke/regression for BOQ decimal conversion
- this coordination claim

## Excluded scope
Workspace/UI, MCP transport, Core persistence, release/install, unrelated quantity rules or exports.

## Validation plan
- deterministic RED regression against current production conversion
- targeted Quantity smoke
- repository validation / hosted CI on exact implementation head
- self-review decimal min/max, zero, underflow/overflow, culture, multiplication overflow, deterministic behavior

## Completion condition
Production conversion preserves invariant round-trip decimal fidelity when representable, fails closed otherwise, exact-head CI is green, implementation is merged to current `main`, and the merge/main SHA is verified.
