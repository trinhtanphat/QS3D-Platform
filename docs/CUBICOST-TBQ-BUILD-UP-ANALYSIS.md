# Cubicost TBQ — Build-up Analysis parity

Updated: 2026-08-16 (UTC+7)
Issue: #22
Dependency stack: #15 -> #19 -> #21

## Public behavior being modeled

The official Glodon Asia TBQ User Guide documents **Build-up Analysis** as a Build-up Unit Rates analysis surface with these important rules:

- the analysis contains only rates adopted in bill items;
- existing rates can be modified in the analysis and those modifications are reflected in BQ and Build-up Unit Rates;
- `Check BQ Reversely` shows all bill items adopting the selected rate;
- users cannot add new rates from the Build-up Analysis tab.

QS3D implements the domain behavior only. It does not reproduce Glodon UI, assets, templates, binaries or private source.

## Shared workspace contract

`BuildUpAnalysisWorkspace` is an immutable-style snapshot built from canonical `CostRateBuildUp` records plus `BqRateAdoption` links.

Construction validates:

- unique rate IDs case-insensitively;
- every BQ adoption points to a known rate;
- duplicate BQ/rate adoption edges are rejected;
- null rates/adoptions fail closed.

Only rates referenced by at least one BQ adoption are exposed through `Rates`.

## Reverse checking

`CheckBqReversely(rateId)` returns deterministic bill-item codes for an adopted rate. A rate that is not part of the adopted-rate workspace is rejected rather than silently returning a misleading result.

## Existing-rate update only

`UpdateExisting(replacement)` has no add path. The replacement ID must already identify a BQ-adopted rate in the workspace.

A successful update returns `BuildUpAnalysisChange` containing:

- previous rate;
- replacement/current rate;
- affected BQ item codes;
- a new `BuildUpAnalysisWorkspace` snapshot.

The original workspace remains unchanged. This lets a consuming project/host apply BQ and Build-up persistence atomically in its own transaction boundary without putting storage side effects into Platform.

If the rate is unadopted or unknown, update fails. This explicitly preserves the public rule that users cannot add new rates in Build-up Analysis.

## Repository ownership

`QS3D-Platform` owns this filtering/reverse-reference/update contract because it is cost-domain logic. BricsCAD, AutoCAD and standalone hosts own persistence transactions, UI presentation and native workflows.

## Validation

`TbqBuildUpAnalysisParitySmoke` covers adopted-only filtering, deterministic reverse BQ lookup, immutable replacement, affected-BQ evidence, rejection of unadopted/new rates, duplicate rates, dangling adoptions and duplicate edges.

`scripts/check-cubicost-tbq-build-up-analysis.py` guards the contract and rejects native CAD/UI/file/network dependencies from the shared source.

Green Platform CI proves this host-neutral source only; it is not evidence of private Glodon internals or licensed CAD-host runtime behavior.
