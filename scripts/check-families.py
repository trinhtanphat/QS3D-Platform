#!/usr/bin/env python3
from __future__ import annotations
import pathlib, sys
ROOT = pathlib.Path(__file__).resolve().parents[1]
PROJECT = ROOT / "src/QS3D.Platform.Families/QS3D.Platform.Families.csproj"
MODEL = ROOT / "src/QS3D.Platform.Families/FamilySchemaModel.cs"
REGISTRY = ROOT / "src/QS3D.Platform.Families/FamilyMigrationRegistry.cs"
STEPS = ROOT / "src/QS3D.Platform.Families/FamilyMigrationSteps.cs"
SMOKE = ROOT / "tests/QS3D.Platform.SmokeTests/FamilyVersionSmoke.cs"
SMOKE_PROJECT = ROOT / "tests/QS3D.Platform.SmokeTests/QS3D.Platform.SmokeTests.csproj"
failures: list[str] = []
for path in (PROJECT, MODEL, REGISTRY, STEPS, SMOKE, SMOKE_PROJECT):
    if not path.is_file(): failures.append(f"missing {path.relative_to(ROOT)}")
if PROJECT.is_file() and "<TargetFramework>netstandard2.0</TargetFramework>" not in PROJECT.read_text(encoding="utf-8"):
    failures.append("Families project must target netstandard2.0")
if SMOKE_PROJECT.is_file() and "QS3D.Platform.Families" not in SMOKE_PROJECT.read_text(encoding="utf-8"):
    failures.append("Platform smoke project must reference Families")
if MODEL.is_file():
    text = MODEL.read_text(encoding="utf-8")
    for token in ("QuantityDimension", "FamilySchemaDefinition", "FamilySchemaValidator", "ApplyDefaults"):
        if token not in text: failures.append(f"FamilySchemaModel missing {token}")
if REGISTRY.is_file():
    text = REGISTRY.read_text(encoding="utf-8")
    for token in ("ToVersion !=", "Implicit family downgrade", "ApplyDefaults"):
        if token not in text: failures.append(f"FamilyMigrationRegistry missing {token}")
if failures:
    print("Platform family schema gate FAILED", file=sys.stderr)
    for failure in failures: print(f"- {failure}", file=sys.stderr)
    raise SystemExit(1)
print("Platform family schema gate PASS")
