#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Platform.Parity/TbqBuildUpAnalysisParity.cs"
SMOKE = ROOT / "tests/QS3D.Platform.SmokeTests/TbqBuildUpAnalysisParitySmoke.cs"
DOC = ROOT / "docs/CUBICOST-TBQ-BUILD-UP-ANALYSIS.md"
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
    print("Cubicost TBQ Build-up Analysis: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
doc = DOC.read_text(encoding="utf-8")

for token in (
    "public sealed class BuildUpAnalysisWorkspace",
    "public sealed class BuildUpAnalysisChange",
    "public IReadOnlyList<string> CheckBqReversely",
    "public BuildUpAnalysisChange UpdateExisting",
    "adoptedRateIds.Contains(rate.Id)",
    "cannot add or update a rate that is not already adopted in BQ",
    "new BuildUpAnalysisWorkspace(nextRates, _adoptions)",
):
    require(source, token, "Build-up Analysis contract")

for token in (
    "IncludesOnlyBqAdoptedRates",
    "UpdatesExistingAndReturnsAffectedBq",
    "ValidationFailsClosed",
    "workspace.UpdateExisting",
    "workspace.CheckBqReversely",
):
    require(smoke, token, "Build-up Analysis smoke")

for token in (
    "Build-up Analysis",
    "only rates adopted in bill items",
    "Check BQ Reversely",
    "cannot add new rates",
    "QS3D-Platform",
):
    require(doc, token, "Build-up Analysis documentation")

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
    forbid(source, token, "shared Build-up Analysis boundary")

if errors:
    print("Cubicost TBQ Build-up Analysis: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("Cubicost TBQ Build-up Analysis: PASS")
