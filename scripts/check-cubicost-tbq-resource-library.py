#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Platform.Parity/TbqResourceLibraryParity.cs"
SMOKE = ROOT / "tests/QS3D.Platform.SmokeTests/TbqResourceLibraryParitySmoke.cs"
DOC = ROOT / "docs/CUBICOST-TBQ-RESOURCE-LIBRARY.md"
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
    print("Cubicost TBQ Resource Library: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
doc = DOC.read_text(encoding="utf-8")
program = PROGRAM.read_text(encoding="utf-8")

for token in (
    "public sealed class TbqResourceLibrary",
    "public sealed class ResourceLibraryBatchImportResult",
    "public static TbqResourceLibrary ImportFromProject",
    "public ResourceLibraryBatchImportResult BatchImport",
    "new Dictionary<string, CostRateBuildUp>(StringComparer.OrdinalIgnoreCase)",
    "_rateById.TryGetValue(requestedId, out var rate)",
    "Duplicate Resource Library batch request id",
    "requires at least one explicit rate selection",
    "selected.Sort",
):
    require(source, token, "Resource Library contract")

for token in (
    "CreatesLibraryFromProjectAndImportsExplicitBatch",
    "PreservesRateDetailsWithoutMutation",
    "ValidationFailsClosed",
    "library.BatchImport",
    "PROJECT-HISTORY-01",
):
    require(smoke, token, "Resource Library smoke")

require(program, "(\"TBQ Resource Library batch import\", TbqResourceLibraryParitySmoke.Run)", "smoke registration")

for token in (
    "Batch Import from RL",
    "Resource Library",
    "Import from project",
    "explicit selection",
    "no fuzzy",
    "QS3D-Platform",
):
    require(doc, token, "Resource Library documentation")

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
    "Levenshtein",
    "Similarity",
):
    forbid(source, token, "shared Resource Library boundary")

if errors:
    print("Cubicost TBQ Resource Library: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("Cubicost TBQ Resource Library: PASS")
