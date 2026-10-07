#!/usr/bin/env python3
"""Resolve exact package/security Actions runs; no package publishing operations."""
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile

PACK_PATHS = {'.github/workflows/nuget-pack.yml', '.github/workflows/pre-release-pack.yml'}


def gh(*arguments):
    result = subprocess.run(['gh', *map(str, arguments)], check=True, capture_output=True, text=True)
    return result.stdout


def api(path):
    return json.loads(gh('api', path))


def validate_pack(run, repo, commit=None, stable=False):
    if (run.get('conclusion') != 'success' or run.get('event') != 'workflow_dispatch'
            or run.get('head_repository', {}).get('full_name') != repo
            or run.get('path') not in PACK_PATHS):
        raise ValueError('Expected a successful package build from this repository')
    if stable and run['path'] != '.github/workflows/nuget-pack.yml':
        raise ValueError('NuGet publishing requires a stable NuGet Pack run')
    if commit and run['head_sha'] != commit:
        raise ValueError('Package build commit differs from the publishing commit')
    return run


def source_run(repo, run_id):
    if not str(run_id).isdigit():
        raise ValueError('Pack run ID must be numeric')
    return validate_pack(api(f'repos/{repo}/actions/runs/{run_id}'), repo)


def write_outputs(values):
    with Path(os.environ['GITHUB_OUTPUT']).open('a') as output:
        for key, value in values.items():
            if '\n' in str(value) or '\r' in str(value):
                raise ValueError('Invalid workflow output')
            output.write(f'{key}={value}\n')


def resolve_publish(repo, commit, destination, run_id=None):
    if run_id:
        pack = validate_pack(source_run(repo, run_id), repo, commit, stable=True)
    else:
        runs = api(f'repos/{repo}/actions/workflows/nuget-pack.yml/runs?status=success&head_sha={commit}&per_page=100')['workflow_runs']
        candidates = [r for r in runs if r.get('event') == 'workflow_dispatch' and r.get('head_sha') == commit]
        if not candidates:
            raise ValueError('No successful NuGet Pack run for this commit')
        pack = validate_pack(candidates[0], repo, commit, stable=True)
    # workflow_run head_sha is the workflow's default-branch revision, so matching
    # security runs by commit alone is insufficient. Inspect artifact provenance.
    for page in range(1, 11):
        runs = api(f'repos/{repo}/actions/workflows/package-security.yml/runs?status=success&per_page=100&page={page}')['workflow_runs']
        for run in runs:
            if run.get('conclusion') != 'success' or run.get('head_repository', {}).get('full_name') != repo:
                continue
            with tempfile.TemporaryDirectory(prefix='security-publish-') as temporary:
                temporary = Path(temporary)
                try:
                    gh('run', 'download', run['id'], '--repo', repo, '--name', 'ServiceStack Security', '--dir', temporary)
                except subprocess.CalledProcessError:
                    continue  # Expired or unavailable artifact: look for a retained regeneration.
                provenance = json.loads((temporary / 'source-run.json').read_text())
                if (str(provenance.get('packRunId')) != str(pack['id'])
                        or provenance.get('packCommit') != commit or provenance.get('repository') != repo):
                    continue
                if destination.exists():
                    raise ValueError('Security output directory already exists')
                shutil.copytree(temporary, destination)
                return {'pack_run_id': pack['id'], 'security_run_id': run['id'], 'pack_commit': commit}
        if len(runs) < 100:
            break
    raise ValueError(f'No retained successful security evidence for NuGet Pack run {pack["id"]}; run Package Security Evidence with that pack_run_id first')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=['source', 'publish'])
    parser.add_argument('--run-id')
    parser.add_argument('--repo', default=os.environ.get('GITHUB_REPOSITORY'))
    parser.add_argument('--commit', default=os.environ.get('GITHUB_SHA'))
    parser.add_argument('--destination', type=Path, default=Path('build/security-output'))
    args = parser.parse_args()
    if args.command == 'source':
        run = source_run(args.repo, args.run_id)
        write_outputs({'pack_run_id': run['id'], 'pack_commit': run['head_sha'], 'pack_url': run['html_url']})
    else:
        write_outputs(resolve_publish(args.repo, args.commit, args.destination, args.run_id))


if __name__ == '__main__':
    main()
