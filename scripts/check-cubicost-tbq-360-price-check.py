#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Platform.Parity/TbqPriceReferenceParity.cs"
SMOKE = ROOT / "tests/QS3D.Platform.SmokeTests/TbqPriceReferenceParitySmoke.cs"
DOC = ROOT / "docs/CUBICOST-TBQ-360-PRICE-CHECK.md"
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
    print("Cubicost TBQ 360-degree price check: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
doc = DOC.read_text(encoding="utf-8")

for token in (
    "public enum CostReferenceUsage",
    "public sealed class CostRateReferenceGraph",
    "public CostRateReferenceState GetReferenceState",
    "public IReadOnlyList<string> CheckLinkingRates",
    "public IReadOnlyList<string> CheckBqReversely",
    "public IReadOnlyList<CostRateNode> FindRatesNotAdoptedInBq",
    'CostReferenceUsage.Bq | CostReferenceUsage.UnitRate => "BQ+UR"',
    "Composition parent must be a unit rate",
    "Composition component must be a basic rate",
):
    require(source, token, "shared price-reference contract")

for token in (
    "ReferenceMarksAndReverseChecks",
    "IntegrityFailsClosed",
    'Equal("BQ", graph.GetReferenceState("ur-001").ReferenceMark)',
    'Equal("BQ+UR", graph.GetReferenceState("MAT-001").ReferenceMark)',
    'Equal("UR", graph.GetReferenceState("lab-001").ReferenceMark)',
    'graph.CheckLinkingRates("mat-001")',
    'graph.CheckBqReversely("UR-001")',
):
    require(smoke, token, "price-reference smoke")

for token in (
    "360-Degree Price Check",
    "Check Linking Rate",
    "Check BQ Reversely",
    "BQ+UR",
    "QS3D-Platform",
):
    require(doc, token, "price-reference documentation")

for token in (
    "Bricscad.",
    "Autodesk.",
    "Teigha.",
    "Solid3d",
    "DBObject",
    "System.Windows",
    "Microsoft.Win32",
):
    forbid(source, token, "vendor-neutral boundary")

if errors:
    print("Cubicost TBQ 360-degree price check: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("Cubicost TBQ 360-degree price check: PASS")
