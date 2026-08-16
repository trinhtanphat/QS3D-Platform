#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Platform.Parity/TbqTradeAnalysisParity.cs"
SMOKE = ROOT / "tests/QS3D.Platform.SmokeTests/TbqTradeAnalysisParitySmoke.cs"
DOC = ROOT / "docs/CUBICOST-TBQ-TRADE-ANALYSIS.md"
PROGRAM = ROOT / "tests/QS3D.Platform.SmokeTests/Program.cs"
errors = []


def require(text, token, label):
    if token not in text:
        errors.append(f"{label}: missing {token!r}")


def forbid(text, token, label):
    if token in text:
        errors.append(f"{label}: forbidden {token!r}")


for path in (SOURCE, SMOKE, DOC, PROGRAM):
    if not path.exists():
        errors.append(f"missing required file: {path.relative_to(ROOT)}")

if errors:
    print("Cubicost TBQ Analysis by Trade: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
doc = DOC.read_text(encoding="utf-8")
program = PROGRAM.read_text(encoding="utf-8")

for token in (
    "public sealed class TradeAnalysisLine",
    "public sealed class TradeAnalysisSnapshot",
    "public sealed class TradeAnalysisWorkspace",
    "public TradeAnalysisSnapshot? Current",
    "public TradeAnalysisSnapshot Refresh",
    "TradeCostAnalysisService.Analyze",
    "Duplicate trade-analysis line id",
    "IsWithinNode",
    "Unclassified",
):
    require(source, token, "trade analysis contract")

for token in (
    "RefreshScopesCurrentNodeAndCalculatesCfa",
    "SnapshotChangesOnlyOnExplicitRefresh",
    "ValidationFailsClosed",
    "decimal.MaxValue",
):
    require(smoke, token, "trade analysis smoke")

require(program, "(\"TBQ Analysis by Trade\", TbqTradeAnalysisParitySmoke.Run)", "smoke registration")

for token in (
    "Analysis by Trade",
    "Unclassified",
    "CFA",
    "Refresh",
    "current node",
    "Export Excel",
    "QS3D-Platform",
):
    require(doc, token, "trade analysis documentation")

for token in (
    "Bricscad.",
    "Autodesk.",
    "Teigha.",
    "Solid3d",
    "DBObject",
    "System.Windows",
    "Microsoft.Win32",
    "System.IO",
    "File.",
    "HttpClient",
    "ClosedXML",
    "EPPlus",
):
    forbid(source, token, "shared trade analysis boundary")

if errors:
    print("Cubicost TBQ Analysis by Trade: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("Cubicost TBQ Analysis by Trade: PASS")
