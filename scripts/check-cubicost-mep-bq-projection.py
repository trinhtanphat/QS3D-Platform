#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Platform.Parity/MepBqCostProjection.cs"
SMOKE = ROOT / "tests/QS3D.Platform.SmokeTests/MepBqCostProjectionModuleSmoke.cs"
errors = []

for label, path in (("source", SOURCE), ("smoke", SMOKE)):
    if not path.exists():
        errors.append(f"missing {label}: {path.relative_to(ROOT)}")

if not errors:
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    for token in (
        "MepBqMeasurementBasis",
        "MepBqMappingProfile",
        "MepBqMappingStatus.Ambiguous",
        "MepBqProjectionService",
        "MepBqCostProjectionService",
        'MepBqMeasurementBasis.Count => "ea"',
        'MepBqMeasurementBasis.Length => "m"',
        'MepBqMeasurementBasis.Area => "m2"',
        'MepBqMeasurementBasis.Volume => "m3"',
        "library.Find(match.ItemCode)",
        "StringComparer.OrdinalIgnoreCase.Equals(rate.Currency, currency)",
        "Multiple exact rates match BQ item",
        "ContributedQuantity",
    ):
        if token not in source:
            errors.append(f"source missing required token {token!r}")

    for token in (
        "specific CHW mapping must outrank generic pipe mapping",
        "MepBqMappingStatus.Ambiguous",
        "wrongUnitLibrary",
        "duplicateRates",
        'Price(projected, rates, "USD")',
        "497.5m",
    ):
        if token not in smoke:
            errors.append(f"smoke missing required token {token!r}")

    for forbidden in (
        "Autodesk.",
        "Bricscad.",
        "Teigha.",
        "ODA",
        "ObjectId",
        "DBObject",
        "Solid3d",
        "System.Windows",
        "HttpClient",
        "File.Write",
        "Directory.Create",
    ):
        if forbidden in source:
            errors.append(f"source contains forbidden vendor/UI/storage/network token {forbidden!r}")

if errors:
    print("Cubicost MEP-to-BQ projection guard: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("Cubicost MEP-to-BQ projection guard: PASS")
