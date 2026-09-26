#!/usr/bin/env python3
"""Upgrade pinned PackageReferences to the latest stable version in their current major.

Usage: python3 upgrade-net10.py [ROOT] [--dry-run]
ROOT defaults to the repository root (the parent of this script's build directory)
and is searched recursively for .csproj files, excluding .git, bin, obj and node_modules. Requires Python 3 and internet.

Versions are resolved per package from NuGet.org's V3 API at execution time:
https://learn.microsoft.com/en-us/nuget/api/package-base-address-resource
All package IDs are checked, including third-party packages. Each reference stays
within its current major: for example, 7.0.2 can become 7.1.0, but never 8.0.0.
Only numeric stable versions are considered (the API includes unlisted versions).
Prereleases, floating versions, version ranges, MSBuild properties and centrally
managed versions are left unchanged. NuGet.config/private feeds are not used.
Edit BLACKLIST below to preserve specific package versions, or use "*" to skip
all versions of a package. Both Version attributes and child elements are
supported. No restore is run.
"""

import argparse
import json
import os
from pathlib import Path
import re
import sys
from urllib.parse import quote
from urllib.request import urlopen
import xml.etree.ElementTree as ET


# Package IDs are case-insensitive. Entries apply in every project/target framework.
# Pin only the legacy versions so newer references can still receive upgrades.
# To skip a package entirely, use {"*"} instead of a set of pinned versions.
BLACKLIST = {
    "Grpc.AspNetCore.Server": {"2.71.0"},  # net6.0 compatibility
    "protobuf-net.Grpc.AspNetCore": {"1.2.2"},  # net6.0 compatibility
}

STABLE = re.compile(r"\d+(?:\.\d+){0,3}\Z")
# Match whole XML tags so '>' inside a quoted Condition is not a terminator.
TAG_BODY = r'''(?:[^>"']|"[^"]*"|'[^']*')*'''
REFERENCE = re.compile(
    r"<PackageReference\b" + TAG_BODY + r"/\s*>"
    r"|<PackageReference\b" + TAG_BODY + r">.*?</PackageReference\s*>",
    re.DOTALL,
)
ATTRIBUTE = re.compile(r'''\b(Include|Update|Version)\s*=\s*(["'])(.*?)\2''', re.DOTALL)
VERSION_ELEMENT = re.compile(r"<Version\s*>\s*(\S+?)\s*</Version\s*>")
IGNORED_XML = re.compile(r"<!--.*?-->|<!\[CDATA\[.*?\]\]>", re.DOTALL)
EXCLUDED = {".git", "bin", "obj", "node_modules"}


def version_key(version):
    parts = tuple(map(int, version.split(".")))
    return parts + (0,) * (4 - len(parts))


def is_blacklisted(package, version):
    for name, versions in BLACKLIST.items():
        if name.lower() == package.lower():
            return "*" in versions or any(version_key(v) == version_key(version) for v in versions)
    return False


def references(text):
    """Return eligible package IDs, versions and precise text replacement spans."""
    ET.fromstring(text)  # Reject malformed XML before planning any writes.
    masked = IGNORED_XML.sub(lambda m: " " * len(m[0]), text)
    for match in REFERENCE.finditer(masked):
        tag = re.match(r"<PackageReference\b" + TAG_BODY + r">", match[0])[0]
        attrs = {m[1]: m for m in ATTRIBUTE.finditer(tag)}
        identity = attrs.get("Include") or attrs.get("Update")
        if identity is None:
            continue
        version = attrs.get("Version")
        group = 3
        if version is None:
            version = VERSION_ELEMENT.search(match[0])
            group = 1
        if version is not None and STABLE.fullmatch(version[group]):
            if is_blacklisted(identity[3], version[group]):
                continue
            start, end = version.span(group)
            yield identity[3], version[group], match.start() + start, match.start() + end


def get_json(url):
    with urlopen(url, timeout=30) as response:
        return json.load(response)


def latest_versions(packages):
    index = get_json("https://api.nuget.org/v3/index.json")
    base = next(r["@id"] for r in index["resources"]
                if r["@type"] == "PackageBaseAddress/3.0.0")
    result = {}
    for package in sorted(packages):
        data = get_json(f"{base.rstrip('/')}/{quote(package, safe='')}/index.json")
        versions = [v for v in data["versions"] if STABLE.fullmatch(v)]
        for major in sorted(packages[package]):
            candidates = [v for v in versions if version_key(v)[0] == major]
            if not candidates:
                raise ValueError(f"No stable {major}.x version found for {package}")
            result[package, major] = max(candidates, key=version_key)
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("root", nargs="?", type=Path, default=Path(__file__).resolve().parent.parent)
    parser.add_argument("--dry-run", action="store_true", help="Show upgrades without changing files")
    args = parser.parse_args()
    root = args.root.resolve()
    if not root.is_dir():
        parser.error(f"Not a directory: {root}")

    projects = []
    scanned = 0
    for directory, dirs, files in os.walk(root):
        dirs[:] = sorted(d for d in dirs if d not in EXCLUDED)
        for name in sorted(files):
            path = Path(directory) / name
            if path.suffix.lower() != ".csproj" or path.is_symlink():
                continue
            scanned += 1
            # Decode bytes directly to preserve UTF-8 BOMs and CRLF newlines.
            text = path.read_bytes().decode("utf-8")
            refs = list(references(text))
            if refs:
                projects.append((path, text, refs))

    packages = {}
    for _, _, refs in projects:
        for package, current, *_ in refs:
            packages.setdefault(package.lower(), set()).add(version_key(current)[0])
    if not packages:
        print(f"No eligible pinned package references found in {scanned} projects.")
        return
    print(f"Checking {len(packages)} packages in {scanned} projects against NuGet.org...", flush=True)
    # Resolve every package before writing so a lookup failure cannot partially upgrade files.
    latest = latest_versions(packages)
    changed_files = changed_refs = 0
    for path, original, refs in projects:
        updated = original
        for package, current, start, end in reversed(refs):
            target = latest[package.lower(), version_key(current)[0]]
            if version_key(target) <= version_key(current):
                continue
            print(f"{path.relative_to(root)}: {package} {current} -> {target}")
            updated = updated[:start] + target + updated[end:]
            changed_refs += 1
        if updated != original:
            if not args.dry_run:
                path.write_bytes(updated.encode("utf-8"))
            changed_files += 1
    action = "Would update" if args.dry_run else "Updated"
    print(f"{action} {changed_refs} references in {changed_files} files.")


if __name__ == "__main__":
    try:
        main()
    except (OSError, ValueError, KeyError, StopIteration, ET.ParseError) as error:
        print(f"Error: {error}", file=sys.stderr)
        sys.exit(1)
