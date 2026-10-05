#!/usr/bin/env python3
"""Synchronize shared llms-py assets. No host data, custom UI or upstream writes."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import sys
import tempfile

SKIP = {'credentials', 'github_auth', 'browser', 'identity'}
ASSETS = ('prompts', 'examples', 'recipes')
CONFIGS = ('index.html', 'llms.json', 'providers.json', 'providers-extra.json')
NAME = re.compile(r'[a-z][a-z0-9_]*\Z')


def digest(data):
    return hashlib.sha256(data).hexdigest()


def regular_files(directory, exclude=()):
    if directory.is_symlink() or not directory.is_dir():
        raise ValueError(f'Expected a real directory: {directory}')
    for base, dirs, files in os.walk(directory, followlinks=False):
        base = Path(base)
        dirs[:] = sorted(d for d in dirs if (base / d).relative_to(directory).as_posix() not in exclude)
        for name in dirs + files:
            if (base / name).is_symlink():
                raise ValueError(f'Symlinks are not supported in synchronized assets: {base / name}')
        for name in sorted(files):
            path = base / name
            if not path.is_file():
                raise ValueError(f'Expected a regular asset file: {path}')
            yield path


def destination_path(home, relative):
    path = home / relative
    if not relative.startswith('chat/') or '..' in Path(relative).parts or Path(relative).is_absolute():
        raise ValueError(f'Invalid synchronized path: {relative}')
    for candidate in (path, *path.parents):
        if candidate == home:
            break
        if candidate.is_symlink():
            raise ValueError(f'Symlink in synchronized destination: {candidate}')
    return path


def safe_target(home, relative):
    path = destination_path(home, relative)
    if path.exists() and not path.is_file():
        raise ValueError(f'Asset destination is not a file: {path}')
    return path


def safe_directory(home, relative):
    path = destination_path(home, relative)
    if path.exists() and not path.is_dir():
        raise ValueError(f'Asset destination is not a directory: {path}')
    return path


def plan(home, source, mode=None):
    source = source.resolve()
    if source == (home / 'chat').resolve() or source == home.resolve():
        raise ValueError('The upstream package and C# destination must be different directories')
    files, inputs, roots, extensions = {}, {}, set(), set()

    def file(path, target):
        if any(parent.is_symlink() for parent in (path, *path.parents) if parent != source and source in parent.parents) or path.is_symlink() or not path.is_file():
            raise ValueError(f'Missing or non-regular source asset: {path}')
        content = path.read_bytes()
        if target in files and files[target] != content:
            raise ValueError(f'Conflicting shared asset mapping: {target}')
        files[target] = content
        inputs[path] = digest(content)

    def directory(path, target, exclude=()):
        roots.add(target)
        for item in regular_files(path, exclude):
            file(item, target + '/' + item.relative_to(path).as_posix())

    manifest_path = safe_target(home, 'chat/shared-assets.json')
    previous = json.loads(manifest_path.read_text()) if manifest_path.exists() else {}
    if previous and (previous.get('version') != 1 or not isinstance(previous.get('files'), dict)):
        raise ValueError('Invalid shared-assets.json; expected a sync-generated version 1 manifest')
    previous_extensions = set(previous.get('extensions', []))
    if any(not isinstance(e, str) or not NAME.fullmatch(e) or e in SKIP for e in previous_extensions):
        raise ValueError('Invalid extension ownership in shared-assets.json')

    if mode is None or mode == 'core':
        directory(source / 'ui', 'chat/ui', exclude=('tailwind',))
        for name in CONFIGS:
            file(source / name, 'chat/' + name)
    if mode != 'core':
        if mode is None:
            directory(source / 'extensions/app/themes', 'chat/themes')
            directory(source / 'extensions/agents/profiles', 'chat/profiles')
            candidates = sorted(p for p in (source / 'extensions').iterdir() if p.is_dir() and not p.name.startswith('.'))
        else:
            if not NAME.fullmatch(mode) or mode in SKIP:
                raise ValueError(f'Not a shared extension: {mode}')
            candidates = [source / 'extensions' / mode]
        for extension in candidates:
            if extension.name in SKIP:
                continue
            if extension.is_symlink() or not extension.is_dir() or not NAME.fullmatch(extension.name):
                raise ValueError(f'Invalid upstream extension directory: {extension}')
            directories = [('ui', ''), *((name, '/' + name) for name in ASSETS)]
            if not any((extension / name).is_dir() for name, _ in directories):
                if mode is not None:
                    raise ValueError(f'Extension has no shared assets: {extension.name}')
                continue
            target = 'chat/ext/' + extension.name
            roots.add(target)
            extensions.add(extension.name)
            for name, suffix in directories:
                if (extension / name).exists():
                    directory(extension / name, target + suffix)
        if mode is None:
            # Only extensions recorded by a previous sync are eligible for stale-root deletion.
            # Unknown C# extensions, custom UI and Identity/credentials stay untouched.
            roots.update('chat/ext/' + e for e in previous_extensions - extensions)

    def selected(path):
        return path in files or any(path == r or path.startswith(r + '/') for r in roots)

    hashes = {} if mode is None else {p: h for p, h in previous.get('files', {}).items() if not selected(p)}
    for path in hashes:
        safe_target(home, path)
    hashes.update({path: digest(content) for path, content in files.items()})
    if mode is not None:
        extensions |= previous_extensions
    manifest = {'version': 1, 'extensions': sorted(extensions), 'files': dict(sorted(hashes.items()))}
    files['chat/shared-assets.json'] = (json.dumps(manifest, indent=2) + '\n').encode()
    stale = set()
    for root in roots:
        directory_path = safe_directory(home, root)
        if directory_path.exists() or directory_path.is_symlink():
            for item in regular_files(directory_path):
                relative = item.relative_to(home).as_posix()
                if relative not in files:
                    stale.add(relative)
    writes = {}
    for relative, data in files.items():
        target = safe_target(home, relative)
        if not target.exists() or target.read_bytes() != data:
            writes[relative] = data
    for relative in stale:
        safe_target(home, relative)
    # Read all inputs before any destination change and reject an edit during collection.
    if any(digest(path.read_bytes()) != value for path, value in inputs.items()):
        raise ValueError('Upstream changed while collecting assets; run sync again')
    return writes, stale, roots, len(hashes)


def apply(home, writes, stale, roots):
    paths = set(writes) | stale
    before = {p: (home / p).read_bytes() if (home / p).exists() else None for p in paths}
    modified = []
    with tempfile.TemporaryDirectory(prefix='.chat-sync-', dir=home) as temp:
        stage = Path(temp)
        for relative, data in writes.items():
            candidate = stage / relative
            candidate.parent.mkdir(parents=True, exist_ok=True)
            candidate.write_bytes(data)
        try:
            # Publish the manifest last. Changed bytes get a fresh timestamp, regardless of
            # backdated source mtimes, so normal incremental embedded-resource builds see them.
            for relative in sorted(paths, key=lambda p: (p == 'chat/shared-assets.json', p)):
                target = safe_target(home, relative)
                current = target.read_bytes() if target.exists() else None
                if current != before[relative]:
                    raise ValueError(f'Destination changed during sync: {relative}')
                if relative in writes:
                    target.parent.mkdir(parents=True, exist_ok=True)
                    os.replace(stage / relative, target)
                else:
                    target.unlink()
                modified.append(relative)
        except BaseException:
            for relative in reversed(modified):
                target = home / relative
                if before[relative] is None:
                    target.unlink(missing_ok=True)
                else:
                    candidate = stage / 'restore'
                    candidate.write_bytes(before[relative])
                    os.replace(candidate, target)
            raise
    # Remove empty synchronized directories, never recurse into host/custom directories.
    for root in sorted(roots, reverse=True):
        path = safe_directory(home, root)
        if path.is_dir():
            for base, _, _ in os.walk(path, topdown=False):
                try:
                    Path(base).rmdir()
                except OSError:
                    pass


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    modes = parser.add_mutually_exclusive_group()
    modes.add_argument('--core', action='store_true')
    modes.add_argument('--extension')
    parser.add_argument('--check', action='store_true', help='Verify source parity without writing any files')
    parser.add_argument('--dry-run', action='store_true', help='List changes without writing any files')
    parser.add_argument('source', nargs='?', default=os.environ.get('LLMS', '../../../../llms/llms'))
    args = parser.parse_args()
    home = Path(__file__).resolve().parent
    source = Path(args.source)
    if not source.is_absolute():
        source = home / source
    lock = home / '.chat-sync.lock'
    acquired = False
    try:
        if not args.check and not args.dry_run:
            try:
                lock.mkdir()
            except FileExistsError:
                raise ValueError(f'Another sync is running, or an interrupted sync left {lock}. '
                                 'If no sync is running, remove that directory and run sync again.') from None
            acquired = True
        writes, stale, roots, count = plan(home, source, 'core' if args.core else args.extension)
        if args.dry_run or args.check:
            for p in sorted(writes):
                print('COPY ' + p)
            for p in sorted(stale):
                print('DELETE ' + p)
            print(f'{count} shared assets; {len(writes)} writes, {len(stale)} deletions')
            return 1 if args.check and (writes or stale) else 0
        apply(home, writes, stale, roots)
        print(f'Synced {count} shared assets; {len(writes)} writes, {len(stale)} deletions')
        return 0
    except (OSError, ValueError) as error:
        print(f'Sync failed: {error}', file=sys.stderr)
        return 1
    finally:
        if acquired:
            lock.rmdir()


if __name__ == '__main__':
    sys.exit(main())
