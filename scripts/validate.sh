#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

python3 scripts/preflight.py
dotnet build QS3D.Platform.sln -c Release
dotnet run --project tests/QS3D.Platform.SmokeTests/QS3D.Platform.SmokeTests.csproj -c Release --no-build

echo "QS3D Platform validation PASS"
