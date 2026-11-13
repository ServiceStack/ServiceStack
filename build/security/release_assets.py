#!/usr/bin/env python3
"""Publish security ZIPs from the exact evidence verified before NuGet publication."""
import argparse
import json
import os
import re
from pathlib import Path
import shutil
import subprocess
import tempfile

from bundle_security import bundle, release_version
from ci_workflows import api, gh
from security_artifacts import configurations, encoded, read_json, timestamp, verify_release

ARTIFACT = 'ServiceStack Published Security'


def evidence_version(root):
    versions = set()
    items = configurations(root, root / 'packages.json')
    if not items:
        raise ValueError('Cannot publish an empty security inventory')
    for item in items:
        releases = sorted((item['directory'] / 'security/releases').glob('*'))
        if len(releases) != 1:
            raise ValueError(f'Expected one release per package: {item["id"]}')
        manifest = verify_release(releases[0])
        if manifest['package'] != item['id'] or manifest['version'] != releases[0].name:
            raise ValueError('Evidence identity differs from package mapping')
        versions.add(manifest['version'])
    if len(versions) != 1:
        raise ValueError('Release bundle contains mixed package versions')
    return versions.pop()


def record_publication(root, repo, publish_run_id, pack_run_id, commit):
    source = read_json(root / 'source-run.json')
    if (source.get('repository') != repo or str(source.get('packRunId')) != str(pack_run_id)
            or source.get('packCommit') != commit):
        raise ValueError('Security evidence differs from the published package build')
    version = evidence_version(root)
    (root / 'publication.json').write_bytes(encoded({
        'repository': repo, 'publishRunId': str(publish_run_id), 'packRunId': str(pack_run_id),
        'packCommit': commit, 'packageVersion': version, 'publishedAt': timestamp(),
    }))
    return version


def successful_publication(run, repo):
    if (run.get('event') != 'workflow_dispatch' or run.get('path') != '.github/workflows/nuget-push.yml'
            or run.get('head_repository', {}).get('full_name') != repo):
        return False
    # A failed attachment job must not hide an otherwise successful package publish.
    if run.get('conclusion') == 'success':
        return True
    if run.get('status') != 'completed':
        return False
    jobs = api(f'repos/{repo}/actions/runs/{run["id"]}/jobs?filter=latest&per_page=100')['jobs']
    return any(job['name'] == 'nuget-push' and job['conclusion'] == 'success' for job in jobs)


def validate_publication(root, repo, version, run):
    record = read_json(root / 'publication.json')
    source = read_json(root / 'source-run.json')
    if (not str(record.get('publishRunId', '')).isdigit()
            or not str(record.get('packRunId', '')).isdigit()
            or not re.fullmatch(r'[0-9a-f]{40}', record.get('packCommit', ''))
            or record.get('repository') != repo or source.get('repository') != repo
            or str(record.get('publishRunId')) != str(run['id'])
            or record.get('packageVersion') != version or record.get('packCommit') != run['head_sha']
            or record.get('packCommit') != source.get('packCommit')
            or str(record.get('packRunId')) != str(source.get('packRunId'))):
        raise ValueError('Publication provenance does not match the requested release/run')
    if evidence_version(root) != version:
        raise ValueError('Evidence package version differs from the release')
    return record


def fetch_publication(repo, tag, destination, run_id=None):
    version = release_version(tag)
    if destination.exists():
        raise ValueError('Security output directory already exists')
    if run_id:
        if not str(run_id).isdigit():
            raise ValueError('Publish run ID must be numeric')
        runs = [api(f'repos/{repo}/actions/runs/{run_id}')]
        if not successful_publication(runs[0], repo):
            raise ValueError('Selected run did not successfully publish NuGet packages')
        pages = [runs]
    else:
        pages = None
    for page in range(1, 11):
        runs = pages[0] if pages else api(f'repos/{repo}/actions/workflows/nuget-push.yml/runs?status=completed&per_page=100&page={page}')['workflow_runs']
        for run in runs:
            if not successful_publication(run, repo):
                continue
            with tempfile.TemporaryDirectory(prefix='security-release-') as temporary:
                temporary = Path(temporary)
                try:
                    gh('run', 'download', run['id'], '--repo', repo, '--name', ARTIFACT, '--dir', temporary)
                except subprocess.CalledProcessError:
                    if run_id:
                        raise ValueError('Published security artifact is unavailable or expired')
                    continue
                record = read_json(temporary / 'publication.json')
                if record.get('packageVersion') != version:
                    if run_id:
                        raise ValueError('Selected publish run has a different package version')
                    continue
                validate_publication(temporary, repo, version, run)
                shutil.copytree(temporary, destination)
                return record
        if pages or len(runs) < 100:
            break
    raise ValueError(f'No retained published security evidence for {version}. Publish with the updated NuGet Publish workflow, or restore the original verified evidence; do not substitute another release.')


def select_release_tag(version, releases):
    matching = []
    for release in releases:
        try:
            if release_version(release['tag_name']) == version:
                matching.append(release['tag_name'])
        except ValueError:
            continue
    if len(matching) > 1:
        raise ValueError('Multiple releases match this version; supply --tag explicitly')
    return matching[0] if matching else None


def attach(root, repo, output, tag=None):
    version = evidence_version(root)
    publication = read_json(root / 'publication.json')
    validate_publication(root, repo, version, {'id': publication.get('publishRunId'), 'head_sha': publication.get('packCommit')})
    if tag and release_version(tag) != version:
        raise ValueError('Release tag does not match the evidence version')
    if not tag:
        releases = []
        for page in range(1, 11):
            batch = api(f'repos/{repo}/releases?per_page=100&page={page}')
            releases.extend(batch)
            if len(batch) < 100:
                break
        tag = select_release_tag(version, releases)
        if not tag:
            tag = 'v' + (version[:-2] if re.fullmatch(r'\d+\.\d+\.0', version) else version)
            gh('release', 'create', tag, '--repo', repo, '--draft', '--title', tag,
               '--target', publication['packCommit'], '--notes',
               'Security evidence for the published NuGet packages is attached. Release notes pending.')
    else:
        gh('release', 'view', tag, '--repo', repo, '--json', 'tagName')
    archive = output / f'servicestack-security-{version}.zip'
    bundle(root, archive, version)
    gh('release', 'upload', tag, archive, archive.with_suffix('.zip.sha256'), '--repo', repo, '--clobber')
    print(f'Attached verified package-build security evidence to {tag}')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=['record', 'fetch', 'attach'])
    parser.add_argument('--repo', default=os.environ.get('GITHUB_REPOSITORY'))
    parser.add_argument('--root', type=Path, default=Path('build/security-output'))
    parser.add_argument('--output', type=Path, default=Path('build/security-archives'))
    parser.add_argument('--tag')
    parser.add_argument('--publish-run-id', default=None)
    parser.add_argument('--pack-run-id')
    parser.add_argument('--commit', default=os.environ.get('GITHUB_SHA'))
    args = parser.parse_args()
    if args.command == 'record':
        if not args.pack_run_id:
            parser.error('record requires --pack-run-id')
        record_publication(args.root.resolve(), args.repo, args.publish_run_id or os.environ['GITHUB_RUN_ID'], args.pack_run_id, args.commit)
    elif args.command == 'fetch':
        if not args.tag:
            parser.error('fetch requires --tag')
        fetch_publication(args.repo, args.tag, args.root, args.publish_run_id)
    else:
        attach(args.root.resolve(), args.repo, args.output, args.tag)


if __name__ == '__main__':
    main()
