# Agent work claim — Cubicost TBQ Analysis by Element Code

Date: 2026-08-16 (UTC+7)
Status: ACTIVE
Issue: #20
Agent/session: chatgpt-gpt56sol
Branch: `agent/chatgpt-gpt56sol/cubicost-tbq-element-analysis-20260816`

## Baseline

This lane is stacked on TBQ 360-degree price/reference PR #19 at `8d9169b7bd4105d50011c10526e4ed539223d6eb`, which itself is stacked on shared Cubicost parity PR #15.

## Reserved scope

- `src/QS3D.Platform.Parity/TbqElementAnalysisParity.cs`
- `tests/QS3D.Platform.SmokeTests/TbqElementAnalysisParitySmoke.cs`
- minimum smoke registration in `tests/QS3D.Platform.SmokeTests/Program.cs`
- `scripts/check-cubicost-tbq-element-analysis.py`
- minimum `scripts/validate.sh` wiring
- `docs/CUBICOST-TBQ-ELEMENT-CODE-ANALYSIS.md`
- this claim file

## Boundary

Implement public clean-room element-code cost analysis only. No vendor source/UI/templates, no CAD SDK, no report/export implementation, no direct main write, no merge.

## Handoff

Open a stacked PR to the #19 branch, obtain exact PR CI and fix to green. Stop before merge and preserve dependency order #15 -> #19 -> this lane.
