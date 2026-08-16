#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Platform.Parity/TbqElementAnalysisParity.cs"
SMOKE = ROOT / "tests/QS3D.Platform.SmokeTests/TbqElementAnalysisParitySmoke.cs"
DOC = ROOT / "docs/CUBICOST-TBQ-ELEMENT-CODE-ANALYSIS.md"
errors = []


def require(text, token, label):
    if token not in text:
        errors.append(f"{label}: missing {token!r}")


def forbid(text, token, label):
    if token in text:
        errors.append(f"{label}: forbidden {token!r}")


for path in (SOURCE, SMOKE, DOC):
    if not path.exists():
        errors.append(f"missing required file: {path.relative_to(ROOT)}")

if errors:
    print("Cubicost TBQ element-code analysis: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
doc = DOC.read_text(encoding="utf-8")

for token in (
    "public sealed class ElementCostLine",
    "public sealed class ElementCostSummary",
    "public sealed class ElementCostAnalysisResult",
    "public static class ElementCostAnalysisService",
    'string.IsNullOrWhiteSpace(value) ? "Unclassified"',
    "checked(total + line.Cost)",
    "CostPerM2",
    "IsWithinNode",
    "StringComparer.OrdinalIgnoreCase",
):
    require(source, token, "element analysis contract")

for token in (
    "AggregatesByElementAndArea",
    "FiltersCurrentNodeDeterministically",
    "ValidationFailsClosed",
    'new ElementCostLine("L4", null, 50m',
    "ElementCostAnalysisService.Analyze",
):
    require(smoke, token, "element analysis smoke")

for token in (
    "Analysis by Element Code",
    "Unclassified",
    "cost/m²",
    "QS3D-Platform",
    "public",
):
    require(doc, token, "element analysis documentation")

for token in (
    "Bricscad.",
    "Autodesk.",
    "Teigha.",
    "Solid3d",
    "DBObject",
    "System.Windows",
    "Microsoft.Win32",
    "File.",
    "HttpClient",
):
    forbid(source, token, "vendor-neutral analysis boundary")

if errors:
    print("Cubicost TBQ element-code analysis: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("Cubicost TBQ element-code analysis: PASS")
