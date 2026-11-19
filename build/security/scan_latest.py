#!/usr/bin/env python3
"""Scan NuGet packages at the version of the latest ServiceStack GitHub release."""
import argparse
import json
import os
from pathlib import Path
import sys
import tempfile
import urllib.request
from xml.etree.ElementTree import ParseError
from zipfile import BadZipFile

from bundle_security import release_version
from security_artifacts import (
    HERE, audit_project, configurations, digest, published_package, record_scan,
    restore_package_project, timestamp,
)

REPO = HERE.parent.parent
LATEST_RELEASE = 'https://api.github.com/repos/ServiceStack/ServiceStack/releases/latest'


def latest_tag():
    headers = {'Accept': 'application/vnd.github+json'}
    if token := os.environ.get('GH_TOKEN'):
        headers['Authorization'] = f'Bearer {token}'
    request = urllib.request.Request(LATEST_RELEASE, headers=headers)
    with urllib.request.urlopen(request, timeout=60) as response:
        return json.load(response)['tag_name']


def scan_package(item, version, temporary, output):
    downloads = temporary / 'downloads'
    cache = temporary / 'packages'
    consumer = temporary / item['id']
    consumer.mkdir()
    archive, info = published_package(item, downloads, version)
    created = timestamp()
    project = consumer / 'Consumer.csproj'
    assets = restore_package_project(info, project, downloads, cache)
    root = f'{info["id"]}/{version}'.lower()
    for framework in info['frameworks']:
        if root not in {key.lower() for key in assets['targets'][framework]}:
            raise ValueError(f'{framework}: restore did not resolve the exact published package')
    libraries = {key.lower(): library for key, library in assets['libraries'].items()}
    if root not in libraries:
        raise ValueError('Restore metadata is missing the published package')
    package_hash = digest(archive)
    if digest(cache / libraries[root]['path'] / archive.name) != package_hash:
        raise ValueError('Restored root archive differs from the NuGet.org download')
    audit = audit_project(project, info['id'])
    if {framework['framework'] for entry in audit['projects'] for framework in entry['frameworks']} != set(info['frameworks']):
        raise ValueError('Audit framework coverage differs from the published package')
    manifest = {
        'package': info['id'], 'version': version, 'packageSha256': package_hash,
        'limitations': [
            'Resolved dependency versions are a consumer baseline; declared ranges may resolve differently.',
            'Native payloads, framework/runtime/OS components and ServiceStack code vulnerabilities require separate review.',
        ],
    }
    record_scan(audit, manifest, created, output / info['id'] / version)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--package', action='append', help='Configured package ID; repeat to select a subset')
    parser.add_argument('--scan-output', type=Path, default=REPO / '.build/security-scans',
                        help='Report directory (default: .build/security-scans in the repository)')
    args = parser.parse_args()

    try:
        items = configurations(REPO, packages=args.package)
        if not items:
            raise ValueError('No packages configured for advisory scanning')
        tag = latest_tag()
        version = release_version(tag)
        print(f'Scanning {len(items)} package(s) on NuGet.org at {version} from GitHub release {tag}', flush=True)
        failures = []
        with tempfile.TemporaryDirectory(prefix='servicestack-latest-scan-') as directory:
            temporary = Path(directory)
            (temporary / 'downloads').mkdir()
            for item in items:
                try:
                    scan_package(item, version, temporary, args.scan_output)
                except (ValueError, OSError, KeyError, BadZipFile, ParseError) as error:
                    failures.append(f'{item["id"]} {version}: {error}')
        if failures:
            raise ValueError('Scan failures/findings:\n' + '\n'.join(failures))
    except (ValueError, OSError, KeyError) as error:
        print(f'Security advisories: {error}', file=sys.stderr)
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main())
