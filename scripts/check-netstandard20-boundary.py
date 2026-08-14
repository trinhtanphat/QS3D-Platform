#!/usr/bin/env python3
from __future__ import annotations

import pathlib
import re
import sys
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[1]

FORBIDDEN_SOURCE = {
    "ArgumentNullException.ThrowIfNull": "requires a newer BCL than netstandard2.0",
    "ArgumentException.ThrowIfNullOrEmpty": "requires a newer BCL than netstandard2.0",
    "ArgumentException.ThrowIfNullOrWhiteSpace": "requires a newer BCL than netstandard2.0",
    "double.IsFinite": "is not part of netstandard2.0; use the shared numeric guard",
    "float.IsFinite": "is not part of netstandard2.0; use an explicit finite guard",
    "Math.Clamp(": "is not part of netstandard2.0",
    ".TryAdd(": "Dictionary.TryAdd is not part of netstandard2.0; use ContainsKey/Add or an approved compatible API",
    "SHA256.HashData": "is not part of netstandard2.0; use SHA256.Create().ComputeHash",
    "Convert.ToHexString": "is not part of netstandard2.0; use an explicit invariant hexadecimal encoder",
    "CryptographicOperations.FixedTimeEquals": "is not part of the netstandard2.0 contract; keep modern crypto helpers in product adapters or use an approved compatible implementation",
    "System.Windows": "UI framework types must not enter shared Platform projects",
    "Microsoft.Win32": "Windows-specific APIs must not enter shared Platform projects",
    "System.Runtime.InteropServices.DllImport": "native P/Invoke must remain behind product adapters",
    "Autodesk.AutoCAD": "AutoCAD vendor types must not enter Platform",
    "Bricscad": "BricsCAD vendor types must not enter Platform",
    "Teigha": "Teigha/BricsCAD vendor types must not enter Platform",
    "OdDb": "ODA native database types must not enter Platform",
}

PROJECT_REF_RE = re.compile(r"<ProjectReference\s+Include=\"([^\"]+)\"")


def target_framework(project: pathlib.Path) -> str | None:
    try:
        tree = ET.parse(project)
    except ET.ParseError as exc:
        raise RuntimeError(f"invalid project XML {project}: {exc}") from exc
    value = tree.findtext(".//TargetFramework")
    return value.strip() if value else None


def netstandard_projects() -> list[pathlib.Path]:
    return sorted(
        project
        for project in (ROOT / "src").rglob("*.csproj")
        if target_framework(project) == "netstandard2.0"
    )


def check_source(project: pathlib.Path, failures: list[str]) -> None:
    directory = project.parent
    for source in sorted(directory.rglob("*.cs")):
        text = source.read_text(encoding="utf-8")
        for token, reason in FORBIDDEN_SOURCE.items():
            if token in text:
                rel = source.relative_to(ROOT)
                failures.append(f"{rel}: forbidden '{token}' ({reason})")


def check_project_references(project: pathlib.Path, frameworks: dict[pathlib.Path, str | None], failures: list[str]) -> None:
    text = project.read_text(encoding="utf-8")
    for match in PROJECT_REF_RE.finditer(text):
        reference = (project.parent / match.group(1).replace("\\", "/")).resolve()
        framework = frameworks.get(reference)
        if framework is None and not reference.exists():
            failures.append(f"{project.relative_to(ROOT)}: missing ProjectReference {match.group(1)}")
            continue
        if framework not in (None, "netstandard2.0"):
            failures.append(
                f"{project.relative_to(ROOT)}: netstandard2.0 project references {reference.relative_to(ROOT)} targeting {framework}"
            )


def main() -> int:
    all_projects = sorted((ROOT / "src").rglob("*.csproj"))
    frameworks = {project.resolve(): target_framework(project) for project in all_projects}
    shared = netstandard_projects()
    if not shared:
        print("FAILED: no netstandard2.0 shared projects found")
        return 1

    failures: list[str] = []
    for project in shared:
        check_source(project, failures)
        check_project_references(project, frameworks, failures)

    if failures:
        print("netstandard2.0 boundary FAILED")
        for failure in failures:
            print(f" - {failure}")
        return 1

    print(f"netstandard2.0 boundary PASS ({len(shared)} shared project(s))")
    return 0


if __name__ == "__main__":
    sys.exit(main())
