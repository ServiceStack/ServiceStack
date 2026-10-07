#!/usr/bin/env python3
"""Offline schema, integrity and reviewed-decision validation of package evidence."""
from pathlib import Path
import sys
from security_artifacts import configurations, read_json, render_vex, timestamp, verify_release


def main():
    repo = Path(__file__).resolve().parents[2]
    count = 0
    for item in configurations(repo):
        security = item['directory'] / 'security'
        decisions = read_json(security / 'vex-decisions.json')
        releases = sorted((security / 'releases').glob('*'))
        if not releases:
            raise ValueError(f'No checked-in release evidence for {item["id"]}')
        for release in releases:
            manifest = verify_release(release)
            if manifest['package'] != item['id'] or manifest['version'] != release.name:
                raise ValueError(f'Unexpected identity in {release}')
            # Dry-run only. Never manufacture or publish assertions during validation.
            render_vex(read_json(release / 'sbom.cdx.json'), decisions, timestamp())
            count += 1
    print(f'Validated {count} checked-in release evidence sets and VEX decision files')


if __name__ == '__main__':
    try:
        main()
    except (ValueError, OSError, KeyError) as error:
        print(f'Security evidence: {error}', file=sys.stderr)
        sys.exit(1)
