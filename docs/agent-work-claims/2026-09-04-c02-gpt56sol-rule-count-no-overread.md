# Work claim — C02 quantity rule known-Count no-overread

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-04T06:37:00+07:00`
- Baseline main SHA: `2c950185bcb932fa1b9f26478ed3837f947dad07`
- Implementation branch: `agent/c02-gpt56sol/issue-279-rule-count-no-overread`
- Integration batch: `issue-279`
- Lane-Key: `issue-279`

## Reserved scope
Fail closed when an authoritative-count quantity-rule factor/catalog source yields beyond its advertised Count, before accessing `IEnumerator.Current` for the overrun item.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityRules.cs`
- one focused deterministic smoke under `tests/QS3D.Platform.SmokeTests/`
- smoke registration only if required

## Excluded scope
Quantity schedule carriers, Core persistence, Workspace/UI, MCP, installer/release.

## Validation plan
- TDD RED with a counted hostile enumerable whose overrun `Current` access throws;
- exact Count, Count=0, underreported/overreported/changing/conflicting Count;
- unknown-count streaming and maximum-entry compatibility;
- preserve semantic generation replay and deterministic catalog ordering;
- exact-head hosted Platform CI.

## Completion condition
Fresh exact-head GREEN, reviewed PR merge, claim closeout and exact resulting main SHA verification.
