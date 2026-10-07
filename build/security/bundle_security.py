#!/usr/bin/env python3
"""Create a validated public ZIP with inventories, reviewed VEX and evidence hashes."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import zipfile
from security_artifacts import configurations, encoded, read_json, verify_release


def release_version(tag):
    match = re.fullmatch(r'v?(\d+)\.(\d+)(?:\.(\d+))?(-[A-Za-z0-9.-]+)?', tag)
    if not match:
        raise ValueError('Release tag must be vMAJOR.MINOR[.PATCH][-prerelease]')
    major, minor, patch, suffix = match.groups()
    return '.'.join(str(int(n)) for n in (major, minor, patch or '0')) + (suffix or '')


def bundle(root, output, version=None):
    items = configurations(root, root / 'packages.json')
    files = {}
    inventory = []
    for item in items:
        parent = item['directory'] / 'security/releases'
        releases = [parent / version] if version else sorted(parent.glob('*'))
        if not releases:
            raise ValueError(f'Missing release evidence: {item["id"]}')
        for release in releases:
            manifest = verify_release(release)
            if manifest['package'] != item['id'] or manifest['version'] != release.name:
                raise ValueError('Package mapping and release identity differ')
            inventory.append({k: manifest[k] for k in ('package', 'version', 'packageSha256', 'frameworks')})
            for path in sorted(release.rglob('*')):
                if path.is_file():
                    if path.is_symlink() or not path.resolve().is_relative_to(root.resolve()):
                        raise ValueError('Evidence must not link outside the archive')
                    files[path.relative_to(root).as_posix()] = path.read_bytes()
    for name in ('packages.json', 'source-run.json'):
        path = root / name
        if path.is_file():
            files[name] = path.read_bytes()
    files['index.json'] = encoded({'schemaVersion': 1, 'packages': inventory})
    files['README.md'] = b'''# ServiceStack package security evidence

Each package release folder contains a combined CycloneDX 1.6 SBOM, original
NuGet archive SHA-256, declared dependency ranges, resolved consumer graphs and
managed NuGet advisory observations. Dated VEX files are included only where
vulnerability-specific reviewed assessments exist. No VEX file means no reviewed
statements, not an all-clear. Audit observations are snapshots, not certifications.

Dependency ranges may resolve differently in downstream applications. Native
payload hashes do not establish embedded engine versions or advisory coverage.
Runtime/OS/framework components and first-party code vulnerabilities require
separate assessment. Consult each manifest's limitations. source-run.json identifies
whether evidence came from build artifacts or published packages for a release.

SHA256SUMS covers every enclosed evidence file. The adjacent ZIP.sha256 verifies
archive integrity; hashes are not a digital signature or authenticity attestation.
'''
    files['SHA256SUMS'] = ''.join(f'{hashlib.sha256(data).hexdigest()}  {name}\n' for name, data in sorted(files.items())).encode()
    output.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(output, 'x', compression=zipfile.ZIP_DEFLATED) as archive:
        for name, data in sorted(files.items()):
            archive.writestr(name, data)
    sha = hashlib.sha256(output.read_bytes()).hexdigest()
    output.with_suffix(output.suffix + '.sha256').write_text(f'{sha}  {output.name}\n')
    print(f'Bundled {len(inventory)} package releases: {output}')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path)
    parser.add_argument('--output', type=Path)
    parser.add_argument('--release')
    parser.add_argument('--normalize-tag')
    args = parser.parse_args()
    if args.normalize_tag:
        print(release_version(args.normalize_tag))
    elif args.root and args.output:
        bundle(args.root.resolve(), args.output, args.release)
    else:
        parser.error('--root and --output are required')


if __name__ == '__main__':
    main()
