#!/usr/bin/env python3
from __future__ import annotations

import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
INMEMORY = ROOT / "src" / "QS3D.Platform.InMemory"
TESTS = ROOT / "tests" / "QS3D.Platform.SmokeTests"

required_sources = (
    "InMemoryCadHost.cs",
    "InMemoryViewportService.cs",
    "InMemorySnapService.cs",
    "InMemorySpatialSelectionService.cs",
    "InMemoryAdvancedServices.cs",
    "InMemoryXrefService.cs",
    "InMemoryLayoutService.cs",
    "InMemoryPlotService.cs",
)
required_tests = (
    "InMemoryViewportSnapModuleSmoke.cs",
    "InMemorySpatialSelectionModuleSmoke.cs",
    "InMemoryAdvancedServicesRegistryModuleSmoke.cs",
    "InMemoryDocumentServicesModuleSmoke.cs",
)

failures: list[str] = []
for name in required_sources:
    if not (INMEMORY / name).is_file():
        failures.append(f"missing src/QS3D.Platform.InMemory/{name}")
for name in required_tests:
    if not (TESTS / name).is_file():
        failures.append(f"missing tests/QS3D.Platform.SmokeTests/{name}")

plot = INMEMORY / "InMemoryPlotService.cs"
if plot.is_file() and "new CadPlotResult(false" not in plot.read_text(encoding="utf-8", errors="replace"):
    failures.append("reference plot service must remain non-producing")
registry = INMEMORY / "InMemoryAdvancedServices.cs"
if registry.is_file() and "ConditionalWeakTable" not in registry.read_text(encoding="utf-8", errors="replace"):
    failures.append("document-scoped reference services must use a weak registry")

if failures:
    print("Platform reference services gate FAILED", file=sys.stderr)
    for failure in failures:
        print(f"- {failure}", file=sys.stderr)
    raise SystemExit(1)

print("Platform reference services gate PASS")
