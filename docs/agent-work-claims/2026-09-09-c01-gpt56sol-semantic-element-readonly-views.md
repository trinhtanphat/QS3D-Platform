# Work claim — C01 SemanticElement read-only view encapsulation

- Status: `ACTIVE`
- Agent: `c01-gpt56sol`
- Registered: `2026-09-09T05:52:00+07:00`
- Baseline main SHA: `897309d542dfa17e5071244cce67cb3f5a027cbb`
- Implementation branch: `agent/c01-gpt56sol/issue-292-semantic-element-readonly-views`
- Integration batch: `TBD`
- Lane-Key: `c01-semantic-element-readonly-views-20260909`
- Canonical issue: `#292`

## Reserved scope
Prevent callers from mutating `SemanticElement` backing generated-reference/property collections by down-casting advertised read-only views, preserving validation and mutation-method authority.

## Expected surfaces
- `src/QS3D.Platform.Domain/SemanticModel.cs`
- `tests/QS3D.Platform.SmokeTests/SemanticElementInvariantModuleSmoke.cs`
- this claim file

## Excluded scope
Persistence snapshot implementation (#289), Quantity/estimating, CAD adapters/native runtime, MCP, installer/release and unrelated Domain/Persistence behavior.

## Validation plan
- deterministic RED regression proving mutable down-cast of generated references and properties on baseline
- minimal genuine read-only wrappers preserving current public API types and validated mutation methods
- full authoritative Platform CI on exact candidate
- self-review invalid-reference injection, blank/property normalization bypass, allocation/compatibility and serialization capture behavior

## Completion condition
Implementation/regressions merged through reviewed PR after fresh exact-head Platform CI GREEN; claim terminalized and final Platform main SHA verified.
