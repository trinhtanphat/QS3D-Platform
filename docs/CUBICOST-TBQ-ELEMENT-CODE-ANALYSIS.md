# Cubicost TBQ — Analysis by Element Code parity

Updated: 2026-08-16 (UTC+7)
Issue: #20
Dependencies: shared Cubicost parity #15, TBQ price-reference #19

## Public behavior being modeled

The public Glodon Asia TBQ User Guide lists **Analysis by Element Code**. Glodon's public project showcase also describes analyzing project costs by trade or element for detailed insight. Public secondary training/distributor material describes assigning element codes to BQ items and using an area/GFA value to obtain cost per square metre.

QS3D treats the official public material as the feature authority and the secondary material only as supporting evidence for the optional cost/m² presentation. This implementation does not reproduce proprietary UI, report templates, binaries, private source or private workflow details.

## Shared contract

`ElementCostLine` is a host-neutral cost-analysis input with:

- stable line ID;
- element code;
- non-negative cost;
- hierarchy/node path.

Blank element codes normalize to `Unclassified` so costs are not silently dropped from analysis.

`ElementCostAnalysisService.Analyze(...)`:

- validates unique input line identity case-insensitively;
- optionally scopes input to a selected hierarchy node and descendants;
- groups costs by element code case-insensitively;
- uses checked decimal accumulation;
- reports each element's cost, share of selected total, source-line count and optional cost/m²;
- reports selected total cost and optional total cost/m²;
- sorts output deterministically by element code.

An analysis area of zero is valid and produces `null` cost/m² rather than inventing a divisor. Negative area or negative cost fails before producing a result.

## Current-node scope

The node path is deliberately generic rather than coupled to TBQ UI trees. A consuming host can map its project/bill/element navigation to paths such as:

`Project/Bill-A/Element-1`

Selecting `Project/Bill-A` includes that exact node plus descendants while excluding similar siblings such as `Project/Bill-AB`.

## Repository ownership

`QS3D-Platform` owns aggregation, validation and deterministic cost-analysis semantics because they are independent of CAD APIs and UI frameworks.

Consuming hosts own presentation, export and native integration. Excel/PDF export and report rendering are intentionally outside this lane.

## Validation

`TbqElementAnalysisParitySmoke` covers:

- element aggregation and case-insensitive identity;
- `Unclassified` normalization;
- cost share and cost/m²;
- zero-area null behavior;
- hierarchy boundary filtering;
- duplicate line IDs, negative cost, negative area and null-line rejection.

`scripts/check-cubicost-tbq-element-analysis.py` protects the contract and rejects native CAD/UI/file/network dependencies from this shared source.

A green Platform CI proves only this host-neutral implementation. It is not evidence of Glodon private internals or licensed BricsCAD/AutoCAD runtime behavior.
