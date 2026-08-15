#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
FILES = {
    "mep": ROOT / "src/QS3D.Platform.Parity/MepCoordinationParity.cs",
    "cost": ROOT / "src/QS3D.Platform.Parity/CostLifecycleParity.cs",
    "review": ROOT / "src/QS3D.Platform.Parity/RecognitionReviewParity.cs",
    "smoke": ROOT / "tests/QS3D.Platform.SmokeTests/CubicostSharedParitySmoke.cs",
    "plan": ROOT / "docs/CUBICOST-QS3D-FEATURE-MASTER-PLAN.md",
}
errors = []

for label, path in FILES.items():
    if not path.exists():
        errors.append(f"missing {label}: {path.relative_to(ROOT)}")

if errors:
    print("Cubicost shared parity guard: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

texts = {label: path.read_text(encoding="utf-8") for label, path in FILES.items()}

required = {
    "mep": [
        "MepRecognitionProfile",
        "MepRecognitionStatus.Ambiguous",
        "MepQuantityService",
        "ClashDetectionService",
        "AxisAlignedBox",
    ],
    "cost": [
        "CostRateBuildUp",
        "HistoricalCostCatalog",
        "BqLibraryCatalog",
        "Duplicate incoming BQ item code",
        "TenderEvaluationService",
        "ProgressClaimService",
        "TimePhasedCostService",
    ],
    "review": [
        "CadIdentificationProfile",
        "BeamSizeReadMode",
        "CoordinationIssue",
        "CoordinationIssueStatus",
        "CadReference?",
    ],
    "smoke": [
        "Ambiguous",
        "MepAggregation",
        "ClashDetection",
        "RateBuildUpAndBenchmark",
        "TenderAndProgress",
        "TimePhasedCost",
        "IdentificationAndIssueReview",
    ],
    "plan": [
        "QS3D-Platform",
        "QS3D-BricsCAD",
        "QS3D-AutoCAD",
        "QS3D-CAD",
        "FORMAT_SCOPE",
        "SERVICE_SCOPE",
        "LOCAL_ONLY",
    ],
}

for label, tokens in required.items():
    for token in tokens:
        if token not in texts[label]:
            errors.append(f"{label}: missing required token {token!r}")

vendor_tokens = (
    "Bricscad.",
    "Autodesk.",
    "Teigha.",
    "BrxMgd",
    "AcDb",
    "ObjectId",
    "DBObject",
    "Solid3d",
    "PaletteSet",
)
for label in ("mep", "cost", "review"):
    for token in vendor_tokens:
        if token in texts[label]:
            errors.append(f"{label}: vendor-specific token forbidden in Platform source: {token!r}")

if "netstandard2.0" not in (ROOT / "src/QS3D.Platform.Parity/QS3D.Platform.Parity.csproj").read_text(encoding="utf-8"):
    errors.append("Parity project must remain netstandard2.0")

if errors:
    print("Cubicost shared parity guard: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("Cubicost shared parity guard: PASS")
