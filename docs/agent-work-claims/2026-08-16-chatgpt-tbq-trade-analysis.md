# Agent work claim — Cubicost TBQ Analysis by Trade

Date: 2026-08-16 (UTC+7)
Status: ACTIVE
Issue: #26
Agent/session: chatgpt-gpt56sol
Branch: `agent/chatgpt-gpt56sol/cubicost-tbq-trade-analysis-20260816`

## Baseline

Stacked on Resource Library PR #25 at `514e9a6d0427e11e1d0c2b85266e8579286c1f5d`, after #15 -> #19 -> #21 -> #23.

## Official evidence boundary

Official Glodon Asia TBQ documentation for Analysis by Trade states trade-code grouping, `Unclassified` for missing codes, CFA cost/m², current project/bill/element node viewing, explicit Refresh for latest adjusted-cost data, Export Excel and report-column UI behavior.

This lane reserves only host-neutral analysis/snapshot semantics. Excel generation and UI behavior remain consuming-host/report scope.

## Reserved scope

- `src/QS3D.Platform.Parity/TbqTradeAnalysisParity.cs`
- `tests/QS3D.Platform.SmokeTests/TbqTradeAnalysisParitySmoke.cs`
- minimum registration in `tests/QS3D.Platform.SmokeTests/Program.cs`
- `scripts/check-cubicost-tbq-trade-analysis.py`
- minimum `scripts/validate.sh` wiring
- `docs/CUBICOST-TBQ-TRADE-ANALYSIS.md`
- this claim file

## Boundary

Reuse canonical `TradeCostAnalysisService`; stable line IDs; current-node + descendants; `Unclassified`; CFA; explicit Refresh snapshot; fail closed on duplicate/null/negative/overflow. No native SDK, UI cloning, Excel/PDF/file/network I/O, direct `main` write or merge.

## Handoff

Open a stacked PR to #25 only after exact branch head is assembled. Use fresh pull-request CI as qualification and preserve dependency order.
