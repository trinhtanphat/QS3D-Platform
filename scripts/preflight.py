#!/usr/bin/env python3
from __future__ import annotations

import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
SRC = ROOT / "src"

SHARED_PROJECTS = [
    "QS3D.Platform.Geometry",
    "QS3D.Platform.Domain",
    "QS3D.Platform.Cad.Abstractions",
    "QS3D.Platform.Application",
    "QS3D.Platform.Quantity",
    "QS3D.Platform.Diagnostics",
    "QS3D.Platform.Persistence",
]

FORBIDDEN_SOURCE_TOKENS = [
    "BrxMgd",
    "TD_Mgd",
    "Autodesk.AutoCAD",
    "Bricscad.ApplicationServices",
    "Bricscad.DatabaseServices",
    "Teigha.DatabaseServices",
    "Teigha.Runtime",
    "OpenDesignAlliance",
    "OdDb",
    "System.Windows.Controls",
    "PresentationFramework",
]

FORBIDDEN_BINARY_SUFFIXES = {".dll", ".exe", ".lib", ".a", ".so", ".dylib", ".pdb", ".nupkg"}
errors: list[str] = []


def fail(message: str) -> None:
    errors.append(message)


for project in SHARED_PROJECTS:
    path = SRC / project / f"{project}.csproj"
    if not path.is_file():
        fail(f"missing shared project: {path.relative_to(ROOT)}")
        continue
    text = path.read_text(encoding="utf-8")
    if "<TargetFramework>netstandard2.0</TargetFramework>" not in text:
        fail(f"{path.relative_to(ROOT)} must target netstandard2.0 for BricsCAD V25/net48 consumption")

for path in SRC.rglob("*"):
    if not path.is_file():
        continue
    if path.suffix.lower() in FORBIDDEN_BINARY_SUFFIXES:
        fail(f"committed binary is forbidden in Platform source: {path.relative_to(ROOT)}")
    if path.suffix.lower() not in {".cs", ".csproj", ".props", ".targets"}:
        continue
    text = path.read_text(encoding="utf-8", errors="replace")
    for token in FORBIDDEN_SOURCE_TOKENS:
        if token in text:
            fail(f"vendor/UI token {token!r} leaked into {path.relative_to(ROOT)}")

in_memory = SRC / "QS3D.Platform.InMemory" / "QS3D.Platform.InMemory.csproj"
if not in_memory.is_file() or "<TargetFramework>net8.0</TargetFramework>" not in in_memory.read_text(encoding="utf-8"):
    fail("QS3D.Platform.InMemory must remain the net8.0 non-production adapter")

for source in [
    "InMemoryCadHost.cs",
    "InMemoryViewportService.cs",
    "InMemorySnapService.cs",
    "InMemorySpatialSelectionService.cs",
    "InMemoryAdvancedServices.cs",
]:
    if not (SRC / "QS3D.Platform.InMemory" / source).is_file():
        fail(f"missing deterministic in-memory reference surface: src/QS3D.Platform.InMemory/{source}")

smoke = ROOT / "tests" / "QS3D.Platform.SmokeTests" / "QS3D.Platform.SmokeTests.csproj"
if not smoke.is_file():
    fail("missing deterministic Platform smoke project")
else:
    smoke_text = smoke.read_text(encoding="utf-8")
    for required in [
        "QS3D.Platform.Domain",
        "QS3D.Platform.Cad.Abstractions",
        "QS3D.Platform.Quantity",
        "QS3D.Platform.Diagnostics",
        "QS3D.Platform.Persistence",
        "QS3D.Platform.InMemory",
    ]:
        if required not in smoke_text:
            fail(f"smoke project must reference {required}")

for regression in [
    "InMemoryViewportSnapModuleSmoke.cs",
    "InMemorySpatialSelectionModuleSmoke.cs",
    "InMemoryAdvancedServicesRegistryModuleSmoke.cs",
    "PersistenceSnapshotModuleSmoke.cs",
]:
    if not (ROOT / "tests" / "QS3D.Platform.SmokeTests" / regression).is_file():
        fail(f"missing Platform regression module: tests/QS3D.Platform.SmokeTests/{regression}")

if not (ROOT / "PLANNING.md").is_file():
    fail("PLANNING.md is required at repository root")

if errors:
    print("QS3D Platform preflight FAILED", file=sys.stderr)
    for error in errors:
        print(f"- {error}", file=sys.stderr)
    raise SystemExit(1)

print("QS3D Platform preflight PASS")
print(f"checked {len(SHARED_PROJECTS)} shared netstandard2.0 projects, deterministic reference surfaces and vendor-neutral boundaries")
