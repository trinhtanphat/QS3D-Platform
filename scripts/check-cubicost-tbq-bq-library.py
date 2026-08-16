#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Platform.Parity/TbqBqLibraryParity.cs"
SMOKE = ROOT / "tests/QS3D.Platform.SmokeTests/TbqBqLibraryParitySmoke.cs"
DOC = ROOT / "docs/CUBICOST-TBQ-BQ-LIBRARY.md"
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
    print("Cubicost TBQ BQ Library: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
doc = DOC.read_text(encoding="utf-8")
program = PROGRAM.read_text(encoding="utf-8")

for token in (
    "public enum BqLibraryNodeKind",
    "Category",
    "Subcategory",
    "Heading",
    "Bill",
    "public sealed class TbqBqLibraryWorkspace",
    "public static TbqBqLibraryWorkspace Create",
    "public TbqBqLibraryWorkspace AddContainer",
    "public TbqBqLibraryWorkspace AddBill",
    "public TbqBqLibraryWorkspace ImportFromProject",
    "BqLibraryItem? BillItem",
    "bill nodes cannot contain child nodes",
    "Duplicate project bill item code",
):
    require(source, token, "BQ Library contract")

for token in (
    "CreatesNamedHierarchyAndImportsProjectBills",
    "SnapshotsRemainIndependent",
    "ValidationFailsClosed",
    "PROJECT:B-001",
):
    require(smoke, token, "BQ Library smoke")

require(program, "(\"TBQ BQ Library hierarchy\", TbqBqLibraryParitySmoke.Run)", "smoke registration")

for token in (
    "BQ Library",
    "New BQ Library",
    "categories",
    "subcategories",
    "headings",
    "bills",
    "Import from Project",
    "no mandatory parent-kind",
    "QS3D-Platform",
):
    require(doc, token, "BQ Library documentation")

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
    forbid(source, token, "shared BQ Library boundary")

if errors:
    print("Cubicost TBQ BQ Library: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("Cubicost TBQ BQ Library: PASS")
