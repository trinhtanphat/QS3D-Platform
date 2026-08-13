#!/usr/bin/env python3
from __future__ import annotations
import pathlib, sys
ROOT = pathlib.Path(__file__).resolve().parents[1]
paths = [
    ROOT / "src/QS3D.Platform.Parity/QS3D.Platform.Parity.csproj",
    ROOT / "src/QS3D.Platform.Parity/GoldenParityModel.cs",
    ROOT / "src/QS3D.Platform.Parity/GoldenParityRunner.cs",
    ROOT / "tests/QS3D.Platform.SmokeTests/ParityModuleSmoke.cs",
]
failures = [f"missing {path.relative_to(ROOT)}" for path in paths if not path.is_file()]
solution = ROOT / "QS3D.Platform.sln"
smoke = ROOT / "tests/QS3D.Platform.SmokeTests/QS3D.Platform.SmokeTests.csproj"
if solution.is_file() and "QS3D.Platform.Parity" not in solution.read_text(encoding="utf-8"): failures.append("Parity missing from solution")
if smoke.is_file() and "QS3D.Platform.Parity" not in smoke.read_text(encoding="utf-8"): failures.append("Parity missing from smoke references")
project = paths[0]
if project.is_file() and "netstandard2.0" not in project.read_text(encoding="utf-8"): failures.append("Parity must target netstandard2.0")
if failures:
    print("Platform parity gate FAILED", file=sys.stderr)
    for failure in failures: print(f"- {failure}", file=sys.stderr)
    raise SystemExit(1)
print("Platform parity gate PASS")
