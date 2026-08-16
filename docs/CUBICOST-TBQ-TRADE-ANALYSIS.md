# Cubicost TBQ — Analysis by Trade parity

Updated: 2026-08-16 (UTC+7)
Issue: #26
Dependency stack: #15 -> #19 -> #21 -> #23 -> #25

## Official behavior modeled

The official Glodon Asia TBQ User Guide documents **Analysis by Trade** with these shared behaviors:

- bill items carry a trade code before analysis;
- items without trade codes appear as **Unclassified**;
- users enter **CFA** and cost per m² CFA is calculated;
- switching project or bill/element nodes changes the analysis scope;
- adjusted cost is not reflected instantly: users click **Refresh** to retrieve the latest analysis data;
- **Export Excel**, hide/unhide columns and expand/fold are report/UI operations.

QS3D implements the analysis and snapshot semantics in `QS3D-Platform`. Excel file generation, native UI state and cursor integration belong to consuming hosts/report adapters.

## Shared contract

`TradeAnalysisLine` carries a stable line ID, trade code, non-negative cost and generic hierarchy path. Blank trade codes normalize to `Unclassified`.

`TradeAnalysisWorkspace.Refresh(...)` validates the complete input set, then scopes lines to the requested current node plus descendants. Path matching is case-insensitive and boundary-aware so selecting `Project/Bill-A` does not silently include sibling `Project/Bill-A2`.

The workspace delegates trade aggregation and CFA calculations to the existing canonical `TradeCostAnalysisService`; this lane does not introduce a second cost formula.

## CFA

CFA must be non-negative. When CFA is positive, each row and the total expose cost per m². CFA of zero produces null cost/m² rather than dividing by zero or inventing a default area.

## Explicit Refresh snapshot

`Current` changes only after an explicit `Refresh(...)`. Mutating/replacing caller input after a refresh does not silently recalculate the current snapshot. This directly models the documented behavior that analysis is not normally updated instantly after cost adjustment and requires Refresh for latest data.

The returned `TradeAnalysisSnapshot` contains:

- current node path;
- CFA;
- deterministic trade summary rows;
- total cost and total cost/m²;
- source-line count.

Duplicate stable line IDs, null lines, negative cost/CFA and decimal overflow fail closed.

## Report / Excel boundary

The official workflow offers **Export Excel** and report-column presentation. Platform exposes deterministic snapshot rows suitable for a report adapter, but performs no Excel/PDF/file I/O and does not clone vendor column menus or expansion UI.

## Validation

`TbqTradeAnalysisParitySmoke` covers current-node/descendant scoping, sibling exclusion, `Unclassified`, deterministic trade aggregation, CFA calculations, zero-CFA behavior, explicit Refresh staleness and fail-closed duplicate/overflow paths.

`scripts/check-cubicost-tbq-trade-analysis.py` guards source/smoke/docs/registration and rejects native CAD/UI/file/report-package dependencies from the shared source surface.

Green Platform CI proves this host-neutral contract only. It does not claim private Glodon implementation or native CAD runtime behavior.
