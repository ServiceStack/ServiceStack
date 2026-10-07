#!/usr/bin/env python3
"""Artifact-derived, multi-target CycloneDX SBOMs and reviewed VEX for NuGet releases.

Runtime: Python 3.10+, .NET SDK 10 and pinned CycloneDX 6.2.0. No product dependency.
"""
import argparse
import copy
import datetime as dt
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile
import uuid
import urllib.request
import xml.etree.ElementTree as ET
from xml.sax.saxutils import escape
import zipfile

HERE = Path(__file__).resolve().parent
TOOL_VERSION = '6.2.0'
FORMAT = '1.6'
GENERATOR_VERSION = '1.1.1'
STATES = {'exploitable', 'in_triage', 'resolved', 'resolved_with_pedigree', 'false_positive', 'not_affected'}
JUSTIFICATIONS = {'code_not_present', 'code_not_reachable', 'requires_configuration', 'requires_dependency', 'requires_environment', 'protected_by_compiler', 'protected_at_runtime', 'protected_at_perimeter', 'protected_by_mitigating_control'}


def read_json(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))


def encoded(data):
    return (json.dumps(data, indent=2, ensure_ascii=False) + '\n').encode()


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def timestamp():
    return dt.datetime.now(dt.timezone.utc).isoformat(timespec='seconds').replace('+00:00', 'Z')


def run(args, cwd=None, env=None):
    result = subprocess.run([str(x) for x in args], cwd=cwd, env=env, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if result.returncode:
        raise ValueError(f'Command failed ({result.returncode}): {args[0]}\n{result.stdout}')
    return result.stdout


def generate_framework_bom(tool, project, tfm, temporary, cache):
    bom_path = temporary / (tfm + '.json')
    # CycloneDX discovers caches via NuGet settings, independently of the assets
    # packageFolders. Explicitly select the exact restore cache for staged releases.
    environment = {**os.environ, 'NUGET_PACKAGES': str(cache.resolve())}
    run(tool + [str(project), '--framework', tfm, '--disable-package-restore', '--output-format', 'Json',
                '--spec-version', FORMAT, '--output', str(temporary), '--filename', bom_path.name],
        cwd=HERE, env=environment)
    return read_json(bom_path)


def prop(name, value):
    return {'name': 'servicestack:' + name, 'value': str(value)}


def package_info(path):
    with zipfile.ZipFile(path) as archive:
        names = archive.namelist()
        specs = [n for n in names if '/' not in n and n.endswith('.nuspec')]
        if len(specs) != 1:
            raise ValueError(f'{path}: expected exactly one root nuspec')
        root = ET.fromstring(archive.read(specs[0]))
        metadata = next((n for n in root if n.tag.split('}')[-1] == 'metadata'), None)
        if metadata is None:
            raise ValueError(f'{path}: missing nuspec metadata')
        fields = {n.tag.split('}')[-1]: n for n in metadata}
        name, version = fields['id'].text, fields['version'].text
        if not re.fullmatch(r'[A-Za-z0-9_.-]+', name or '') or not re.fullmatch(r'[A-Za-z0-9_.+-]+', version or ''):
            raise ValueError('Unsafe or missing package identity')
        tfms = sorted({n.split('/')[1] for n in names if n.startswith(('lib/', 'ref/')) and len(n.split('/')) > 2 and n.split('/')[1].startswith('net')})
        framework_source = 'Packaged lib/ref directories'
        groups = []
        for node in metadata.iter():
            if node.tag.split('}')[-1] == 'group':
                groups.append({'targetFramework': node.get('targetFramework', ''), 'dependencies': [dict(x.attrib) for x in node if x.tag.split('}')[-1] == 'dependency']})
        content_files = sorted(n for n in names if n.startswith('contentFiles/') and not n.endswith('/'))
        if not tfms and content_files and not any(n.startswith(('lib/', 'ref/')) for n in names):
            # Content-only releases have compatibility groups, not assembly TFMs.
            tfms = sorted({g['targetFramework'] for g in groups if g['targetFramework']})
            framework_source = 'Declared nuspec compatibility groups for content-only archive'
        if not tfms or any(not re.fullmatch(r'net[a-z0-9.]+', tfm) for tfm in tfms):
            raise ValueError(f'{name}: unsupported or absent shipped target frameworks')
        repository = fields.get('repository')
        license_node = fields.get('license')
        licenses = []
        if license_node is not None and license_node.text:
            # Keep an expression supplied by the package; do not guess a license from authors.
            if license_node.get('type') == 'expression':
                licenses = [{'expression': license_node.text}]
            else:
                licenses = [{'license': {'name': 'Package license file: ' + license_node.text}}]
        elif fields.get('licenseUrl') is not None and fields['licenseUrl'].text:
            licenses = [{'license': {'name': 'Declared package license', 'url': fields['licenseUrl'].text}}]
        return {'id': name, 'version': version, 'frameworks': tfms, 'dependencyGroups': groups,
                'frameworkSource': framework_source, 'contentFiles': content_files,
                'repository': dict(repository.attrib) if repository is not None else {}, 'licenses': licenses,
                'authors': fields['authors'].text if 'authors' in fields else None}


def purl(name, version):
    # NuGet package names are case insensitive. Lower-case IDs make stable identities.
    return f'pkg:nuget/{name.lower()}@{version}'


def aggregate(info, documents, package_hash, created):
    """Keep each framework graph separate within ONE SBOM; identical purls share identity.

    bom-ref is framework-qualified because the same dependency can have different edges
    for different TFMs. It is deliberately distinct from the canonical NuGet purl.
    """
    root_ref = purl(info['id'], info['version'])
    components, edges = {}, {}
    variants = []
    for tfm, document in sorted(documents.items()):
        by_ref = {c['bom-ref']: c for c in document.get('components', [])}
        roots = [c for c in by_ref.values() if c.get('name', '').lower() == info['id'].lower() and c.get('version') == info['version']]
        if len(roots) != 1:
            raise ValueError(f'{info["id"]}/{tfm}: generator must contain exact packaged root once')
        graph = {d['ref']: d.get('dependsOn', []) for d in document.get('dependencies', [])}
        old_root = roots[0]['bom-ref']
        reachable, pending = set(), [old_root]
        while pending:
            old = pending.pop()
            if old in reachable:
                continue
            if old not in by_ref or old not in graph:
                raise ValueError(f'{tfm}: incomplete generator dependency graph at {old}')
            reachable.add(old)
            pending.extend(graph[old])
        refs = {old: purl(by_ref[old]['name'], by_ref[old]['version']) + '#tfm=' + tfm for old in reachable}
        variants.append(refs[old_root])
        for old in sorted(reachable):
            component = copy.deepcopy(by_ref[old])
            ref = refs[old]
            component['bom-ref'] = ref
            component['purl'] = purl(component['name'], component['version'])
            component.setdefault('properties', []).append(prop('target-framework', tfm))
            # Do not distribute private restore paths or tool-inferred local evidence.
            component.pop('evidence', None)
            components[ref] = component
            edges[ref] = sorted(refs[d] for d in graph[old])
    root_component = {'type': 'library', 'bom-ref': root_ref, 'name': info['id'], 'version': info['version'],
                      'purl': root_ref, 'supplier': {'name': 'ServiceStack, Inc.'},
                      'hashes': [{'alg': 'SHA-256', 'content': package_hash}],
                      'properties': [prop('target-frameworks', ';'.join(info['frameworks'])),
                                     prop('inventory-kind', 'Exact NuGet archive and resolved consumer baseline'),
                                     prop('declared-dependency-groups', json.dumps(info['dependencyGroups'], sort_keys=True)),
                                     prop('resolution-limits', 'Consumers can resolve other versions allowed by NuGet ranges; runtime/OS framework components are not inventoried.')]}
    if info['licenses']:
        root_component['licenses'] = info['licenses']
    if info['repository'].get('commit'):
        root_component['properties'].append(prop('package-source-commit', info['repository']['commit']))
    edges[root_ref] = sorted(variants)
    bom = {'$schema': 'http://cyclonedx.org/schema/bom-1.6.schema.json', 'bomFormat': 'CycloneDX', 'specVersion': FORMAT,
           'serialNumber': 'urn:uuid:' + str(uuid.uuid4()), 'version': 1,
           'metadata': {'timestamp': created, 'component': root_component,
                        'tools': {'components': [{'type': 'application', 'name': 'CycloneDX .NET', 'version': TOOL_VERSION},
                                                 {'type': 'application', 'name': 'ServiceStack security artifacts', 'version': GENERATOR_VERSION}]}},
           'components': [components[k] for k in sorted(components)],
           'dependencies': [{'ref': k, 'dependsOn': edges[k]} for k in sorted(edges)]}
    return bom


def is_native(name, data):
    lower = name.lower()
    if lower.endswith(('.so', '.dylib', '.a', '.lib')) or '.so.' in lower:
        return True
    if not lower.endswith(('.dll', '.exe')) or not data.startswith(b'MZ'):
        return False
    try:
        pe = int.from_bytes(data[0x3c:0x40], 'little')
        optional = pe + 24
        if data[pe:pe + 4] != b'PE\0\0':
            return False
        magic = int.from_bytes(data[optional:optional + 2], 'little')
        directory = optional + (112 if magic == 0x20b else 96)
        # PE data-directory entry 14 is the CLR header; absent means native binary.
        cli = directory + 14 * 8
        return int.from_bytes(data[cli:cli + 4], 'little') == 0
    except (IndexError, ValueError):
        return False


def native_inventory(bom, archives):
    """Inventory native files contained in resolved package archives (all RID variants).

    This is package-content evidence, not an assertion that every binary is loaded
    by every consumer. Embedded engine/version identification needs separate review.
    """
    records = []
    native_components = {}
    edges = {d['ref']: d['dependsOn'] for d in bom['dependencies']}
    packages = [bom['metadata']['component']] + bom['components']
    for component in packages:
        identity = (component['name'].lower(), component['version'])
        archive_path = archives.get(identity)
        if archive_path is None:
            raise ValueError(f'Cannot inventory native payloads: missing archive for {identity}')
        with zipfile.ZipFile(archive_path) as archive:
            for name in sorted(archive.namelist()):
                if not name.lower().endswith(('.dll', '.exe', '.so', '.dylib', '.a', '.lib')) and '.so.' not in name.lower():
                    continue
                data = archive.read(name)
                if not is_native(name, data):
                    continue
                hash_value = hashlib.sha256(data).hexdigest()
                ref = 'urn:servicestack:native:' + hashlib.sha256((component['purl'] + '/' + name).encode()).hexdigest()
                if ref not in native_components:
                    native_components[ref] = {'type': 'file', 'bom-ref': ref, 'name': name,
                        'hashes': [{'alg': 'SHA-256', 'content': hash_value}],
                        'properties': [prop('containing-package', component['purl']),
                                       prop('coverage', 'Native file contained in package; all architecture/RID payloads, not necessarily loaded'),
                                       prop('embedded-version-review', 'Required; file hash does not identify embedded engine version')]}
                    records.append({'package': component['purl'], 'path': name, 'sha256': hash_value})
                    edges[ref] = []
                if ref not in edges[component['bom-ref']]:
                    edges[component['bom-ref']].append(ref)
    bom['components'].extend(native_components[k] for k in sorted(native_components))
    bom['dependencies'] = [{'ref': k, 'dependsOn': sorted(set(edges[k]))} for k in sorted(edges)]
    return records


def content_inventory(bom, archive, paths):
    records = []
    root = bom['metadata']['component']['bom-ref']
    edge = next(d for d in bom['dependencies'] if d['ref'] == root)
    with zipfile.ZipFile(archive) as package:
        for name in paths:
            sha256 = hashlib.sha256(package.read(name)).hexdigest()
            ref = root + '#content=' + str(uuid.uuid5(uuid.NAMESPACE_URL, name))
            bom['components'].append({'type': 'file', 'bom-ref': ref, 'name': name,
                                      'hashes': [{'alg': 'SHA-256', 'content': sha256}],
                                      'properties': [prop('archive-path', name)]})
            bom['dependencies'].append({'ref': ref, 'dependsOn': []})
            edge['dependsOn'].append(ref)
            records.append({'path': name, 'sha256': sha256})
    return records


def validate_schema(document):
    try:
        from jsonschema import Draft7Validator
        from referencing import Registry, Resource
    except ImportError as error:
        raise ValueError('Install build/security/requirements.txt for schema validation') from error
    registry = Registry()
    for path in (HERE / 'schema').glob('*.json'):
        resource = Resource.from_contents(read_json(path))
        registry = registry.with_resource('http://cyclonedx.org/schema/' + path.name, resource)
    validator = Draft7Validator(read_json(HERE / 'schema/bom-1.6.schema.json'), registry=registry)
    errors = list(validator.iter_errors(document))
    if errors:
        raise ValueError('CycloneDX schema validation failed: ' + errors[0].message)


def validate_bom(bom):
    validate_schema(bom)
    if bom.get('bomFormat') != 'CycloneDX' or bom.get('specVersion') != FORMAT:
        raise ValueError('Expected CycloneDX 1.6')
    components = [bom['metadata']['component']] + bom.get('components', [])
    refs = [c['bom-ref'] for c in components]
    if len(set(refs)) != len(refs):
        raise ValueError('Duplicate bom-ref')
    graphs = {d['ref']: d.get('dependsOn', []) for d in bom.get('dependencies', [])}
    if len(graphs) != len(bom.get('dependencies', [])) or set(graphs) != set(refs):
        raise ValueError('Every component must have exactly one dependency record')
    for links in graphs.values():
        if any(ref not in refs for ref in links):
            raise ValueError('Dependency points to missing component')
    expected = set(bom['metadata']['component']['properties'][0]['value'].split(';'))
    observed = {p['value'] for c in components for p in c.get('properties', []) if p['name'] == 'servicestack:target-framework'}
    if expected != observed:
        raise ValueError('Framework coverage mismatch')


def render_vex(bom, decisions, created):
    if decisions.get('schemaVersion') != 1:
        raise ValueError('Unsupported VEX decisions schema')
    assessments = decisions.get('assessments')
    if not isinstance(assessments, list):
        raise ValueError('VEX assessments must be an array')
    package = bom['metadata']['component']
    statements = []
    for decision in assessments:
        required = ('id', 'sourceUrl', 'releaseVersion', 'state', 'detail', 'reviewer', 'reviewedAt', 'evidence')
        if any(not decision.get(k) for k in required):
            raise ValueError('VEX decision requires advisory, exact release, state, detail, reviewer/date and evidence')
        if decision['state'] not in STATES:
            raise ValueError('Unsupported CycloneDX VEX state')
        if not re.fullmatch(r'\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z', decision['reviewedAt']):
            raise ValueError('VEX reviewedAt must be a UTC timestamp')
        dt.datetime.fromisoformat(decision['reviewedAt'].replace('Z', '+00:00'))
        if not decision['sourceUrl'].startswith('https://') or not isinstance(decision['evidence'], list):
            raise ValueError('VEX source must be HTTPS and evidence an array')
        if decision['state'] == 'not_affected' and decision.get('justification') not in JUSTIFICATIONS:
            raise ValueError('not_affected requires an explicit supported justification')
        if decision['state'] in {'exploitable', 'resolved', 'resolved_with_pedigree'} and not decision.get('action'):
            raise ValueError('Affected/fixed decisions require corrective action')
        if decision['releaseVersion'] != package['version']:
            continue
        frameworks = decision.get('frameworks', [])
        shipped = package['properties'][0]['value'].split(';')
        if not frameworks or not set(frameworks).issubset(shipped):
            raise ValueError('VEX decision must explicitly name applicable shipped frameworks')
        affected = [c['bom-ref'] for c in bom['components'] if c.get('name', '').lower() == package['name'].lower()
                    and c.get('version') == package['version'] and any(p['name'] == 'servicestack:target-framework' and p['value'] in frameworks for p in c.get('properties', []))]
        analysis = {'state': decision['state'], 'detail': decision['detail'],
                    'firstIssued': decision['reviewedAt'], 'lastUpdated': decision['reviewedAt']}
        if decision.get('justification'):
            if decision['justification'] not in JUSTIFICATIONS:
                raise ValueError('Unsupported VEX justification')
            analysis['justification'] = decision['justification']
        vulnerability = {'id': decision['id'], 'source': {'url': decision['sourceUrl']},
                         'analysis': analysis, 'affects': [{'ref': ref} for ref in affected],
                         'properties': [prop('reviewer', decision['reviewer']), prop('evidence', json.dumps(decision['evidence']))]}
        if decision.get('action'):
            vulnerability['recommendation'] = decision['action']
        statements.append(vulnerability)
    if not statements:
        return None
    if len({v['id'] for v in statements}) != len(statements):
        raise ValueError('Use one decision per advisory/release with all applicable frameworks')
    # Self-contained snapshot: component identities and graph match the immutable SBOM.
    vex = copy.deepcopy(bom)
    vex['serialNumber'] = 'urn:uuid:' + str(uuid.uuid4())
    vex['metadata']['timestamp'] = created
    vex['metadata']['properties'] = [prop('document-kind', 'Reviewed vulnerability impact statements'),
                                    prop('sbom-serial-number', bom['serialNumber'])]
    vex['vulnerabilities'] = statements
    validate_schema(vex)
    return vex


def configurations(repo):
    config = read_json(HERE / 'packages.json')
    if config.get('schemaVersion') != 1:
        raise ValueError('Unsupported package configuration')
    items = []
    ids, projects = set(), set()
    for item in config['packages']:
        name, project = item['id'], Path(item['project'])
        if not re.fullmatch(r'[A-Za-z0-9_.-]+', name):
            raise ValueError('Unsafe package ID')
        if project.is_absolute() or '..' in project.parts or project.suffix != '.csproj':
            raise ValueError('Project must be a repository-relative .csproj path')
        directory = (repo / project).parent.resolve()
        if not directory.is_relative_to(repo.resolve()):
            raise ValueError('Project resolves outside repository')
        if name.lower() in ids or directory in projects:
            raise ValueError('Duplicate package ID or project directory')
        ids.add(name.lower())
        projects.add(directory)
        items.append({**item, 'directory': directory})
    return items


def locate_packages(directory):
    result = {}
    for path in sorted(directory.glob('*.nupkg')):
        if path.name.endswith('.symbols.nupkg'):
            continue
        info = package_info(path)
        key = info['id'].lower()
        if key in result:
            raise ValueError(f'Multiple versions/artifacts of {info["id"]} in input folder')
        result[key] = (path, info)
    return result


def output_directory(item, args):
    return args.output_root / Path(item['project']).parent if args.output_root else item['directory']


def initialize_security(item, args):
    directory = output_directory(item, args) / 'security'
    directory.mkdir(parents=True, exist_ok=True)
    source = item['directory'] / 'security/vex-decisions.json'
    decisions = source.read_bytes() if source.exists() else encoded({'schemaVersion': 1, 'assessments': []})
    files = {'README.md': (HERE / 'package-README.md').read_bytes(),
             '.gitignore': b'!releases/\n!releases/**\n', 'vex-decisions.json': decisions}
    for name, data in files.items():
        try:
            with (directory / name).open('xb') as destination:
                destination.write(data)
        except FileExistsError:
            pass  # Maintained policies and reviewed decisions belong to the maintainer.


def published_package(item, directory, version=None):
    name = item['id'].lower()
    base = 'https://api.nuget.org/v3-flatcontainer/' + name + '/'
    if version is None:
        with urllib.request.urlopen(base + 'index.json', timeout=60) as response:
            versions = json.load(response)['versions']
        stable = [value for value in versions if '-' not in value]
        if not stable:
            raise ValueError(f'No published stable version for {item["id"]}')
        version = stable[-1]  # NuGet flat-container versions are ascending.
    if not re.fullmatch(r'[A-Za-z0-9_.+-]+', version):
        raise ValueError('Unsafe published version')
    filename = name + '.' + version.lower() + '.nupkg'
    archive = directory / filename
    with urllib.request.urlopen(base + version.lower() + '/' + filename, timeout=60) as response:
        with archive.open('wb') as destination:
            shutil.copyfileobj(response, destination)
    info = package_info(archive)
    if info['id'].lower() != name or info['version'].lower() != version.lower():
        raise ValueError('Downloaded package identity differs from requested release')
    return archive, info


def generate_packages(items, args):
    staged = locate_packages(args.package_directory) if args.package_directory else None
    pending = []
    # Verify and skip retained evidence before any network access or tool restore.
    for item in items:
        parent = output_directory(item, args) / 'security/releases'
        if staged is not None:
            if item['id'].lower() not in staged:
                raise ValueError(f'Missing staged package: {item["id"]}')
            archive, info = staged[item['id'].lower()]
            if args.release and args.release != info['version']:
                raise ValueError('Staged package does not match --release')
            retained = [parent / info['version']] if (parent / info['version']).exists() else []
        else:
            archive, info = None, None
            retained = ([parent / args.release] if (parent / args.release).exists() else []) if args.release else sorted(parent.glob('*'))
        for release in retained:
            manifest = verify_release(release, archive, args.require_clean)
            if manifest['package'].lower() != item['id'].lower() or manifest['version'] != release.name:
                raise ValueError('Retained evidence differs from configured package/version')
            print(f'Skipped {item["id"]} {manifest["version"]}: existing evidence verified')
        if not retained:
            pending.append((item, archive, info))
    if not pending:
        print('No new packages/releases to generate.')
        return
    tool = [str(args.cyclonedx.resolve())] if args.cyclonedx else ['dotnet', 'tool', 'run', 'dotnet-CycloneDX', '--']
    if not args.cyclonedx:
        run(['dotnet', 'tool', 'restore'], cwd=HERE)
    if not run(tool + ['--version'], cwd=HERE).strip().startswith(TOOL_VERSION):
        raise ValueError('CycloneDX version differs from pinned version')
    with tempfile.TemporaryDirectory(prefix='servicestack-security-download-') as temporary:
        directory = Path(temporary)
        if staged is None:
            args.package_directory = directory
        for item, archive, info in pending:
            if archive is None:
                archive, info = published_package(item, directory, args.release)
            generate_one(item, archive, info, args, tool, timestamp())
            initialize_security(item, args)


def generate_one(item, archive, info, args, tool, created):
    release = output_directory(item, args) / 'security' / 'releases' / info['version']
    if release.exists():
        raise ValueError(f'Release evidence already exists: {release}; verify it or use a new release, never overwrite')
    package_hash = digest(archive)
    with tempfile.TemporaryDirectory(prefix='servicestack-sbom-') as temporary:
        temporary = Path(temporary)
        project = temporary / 'Consumer.csproj'
        project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
            + '<TargetFrameworks>' + ';'.join(info['frameworks']) + '</TargetFrameworks>'
            + '<AutomaticallyUseReferenceAssemblyPackages>false</AutomaticallyUseReferenceAssemblyPackages>'
            + '<NuGetAudit>true</NuGetAudit><NuGetAuditMode>all</NuGetAuditMode>'
            + '<RestoreEnablePackagePruning>false</RestoreEnablePackagePruning>'
            + '</PropertyGroup><ItemGroup><PackageReference Include="' + escape(info['id'])
            + '" Version="[' + escape(info['version']) + ']" /></ItemGroup></Project>')
        # Isolate from machine/global feeds and never execute package build targets.
        config = temporary / 'NuGet.Config'
        config.write_text('<configuration><packageSources><clear/><add key="release" value="'
            + escape(str(args.package_directory.resolve()), {'"': '&quot;'})
            + '"/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources>'
            + '<auditSources><clear/><add key="nuget.org" value="https://data.nuget.org/v3/index.json"/></auditSources></configuration>')
        cache = temporary / 'packages'
        restore_log = run(['dotnet', 'restore', project, '--configfile', config, '--packages', cache, '--force-evaluate'], cwd=temporary)
        if re.search(r'NU190[05]', restore_log):
            raise ValueError('Vulnerability source unavailable; restore audit coverage is incomplete')
        assets = read_json(temporary / 'obj/project.assets.json')
        documents = {}
        for tfm in info['frameworks']:
            documents[tfm] = generate_framework_bom(tool, project, tfm, temporary, cache)
        bom = aggregate(info, documents, package_hash, created)
        archives = {(info['id'].lower(), info['version']): archive}
        for key, library in assets['libraries'].items():
            if library['type'] == 'package':
                name, version = key.rsplit('/', 1)
                path = cache / library['path'] / f'{name.lower()}.{version.lower()}.nupkg'
                if not path.is_file():
                    raise ValueError(f'Package cache archive missing: {name}/{version}')
                archives[(name.lower(), version)] = path
        if digest(archives[(info['id'].lower(), info['version'])]) != package_hash:
            raise ValueError('Resolved root archive differs from staged package')
        native = native_inventory(bom, archives)
        content = content_inventory(bom, archive, info.get('contentFiles', [])) if info.get('frameworkSource', '').startswith('Declared') else []
        if content:
            bom['metadata']['component']['properties'].append(prop('framework-source', info['frameworkSource']))
        validate_bom(bom)
        audit = json.loads(run(['dotnet', 'package', 'list', '--project', project, '--no-restore', '--include-transitive', '--vulnerable', '--format', 'json'], cwd=temporary))
        # Strip paths before publishing the restore-based vulnerability observation.
        for entry in audit.get('projects', []):
            entry['path'] = info['id']
        if any(log.get('level', '').lower() == 'error' for log in audit.get('logs', [])):
            raise ValueError('Vulnerability scan did not complete successfully')
        resolution = {'frameworks': info['frameworks'], 'declaredDependencyGroups': info['dependencyGroups'],
                      'targets': {target: {name: node.get('dependencies', {}) for name, node in sorted(nodes.items()) if node.get('type') == 'package'} for target, nodes in sorted(assets['targets'].items())}}
        decisions_path = item['directory'] / 'security/vex-decisions.json'
        decisions = read_json(decisions_path) if decisions_path.exists() else {'schemaVersion': 1, 'assessments': []}
        vex = render_vex(bom, decisions, created)
        documents_out = {'sbom.cdx.json': encoded(bom), 'resolution.json': encoded(resolution), 'audit.json': encoded(audit)}
        manifest = {'schemaVersion': 1, 'package': info['id'], 'version': info['version'], 'created': created,
                    'packageSha256': package_hash, 'frameworks': info['frameworks'], 'packageRepository': info['repository'],
                    'generatorVersion': GENERATOR_VERSION, 'cycloneDxVersion': TOOL_VERSION, 'dotnetSdk': run(['dotnet', '--version']).strip(),
                    'files': {name: hashlib.sha256(data).hexdigest() for name, data in documents_out.items()},
                    'vex': 'No reviewed statements for this exact release' if vex is None else 'Reviewed statements in dated VEX snapshot',
                    'nativePayloads': native, 'contentPayloads': content, 'frameworkSource': info.get('frameworkSource', 'Packaged lib/ref directories'), 'limitations': ['Resolved dependency versions are a consumer baseline; declared ranges may resolve differently.',
                    'Framework/runtime/OS components and ServiceStack code vulnerabilities are outside NuGet advisory scan coverage.',
                    'Native payload hashes are inventoried; embedded engine/version identification and advisory assessment require review.']}
        if content:
            manifest['limitations'].append('Content-only package: framework coverage uses nuspec compatibility groups, not shipped assemblies. External template host/Visual Studio assembly references are not resolved or scanned.')
        # Write only once all generation/triage-input checks have succeeded.
        release.parent.mkdir(parents=True, exist_ok=True)
        stage = Path(tempfile.mkdtemp(prefix='.evidence-', dir=release.parent))
        try:
            for name, data in documents_out.items():
                (stage / name).write_bytes(data)
            (stage / 'manifest.json').write_bytes(encoded(manifest))
            if vex:
                (stage / 'vex').mkdir()
                (stage / 'vex' / (created.replace(':', '').replace('-', '') + '.cdx.json')).write_bytes(encoded(vex))
            stage.rename(release)
        finally:
            if stage.exists():
                shutil.rmtree(stage)
    print(f'{info["id"]} {info["version"]}: {len(info["frameworks"])} frameworks, {len(bom["components"])} components; {release}')


def verify_release(release, archive=None, require_clean=False):
    manifest = read_json(release / 'manifest.json')
    if archive and digest(archive) != manifest['packageSha256']:
        raise ValueError(f'{release}: supplied archive hash differs')
    for name, expected in manifest['files'].items():
        if Path(name).name != name or digest(release / name) != expected:
            raise ValueError(f'{release}: evidence changed: {name}')
    bom = read_json(release / 'sbom.cdx.json')
    validate_bom(bom)
    root = bom['metadata']['component']
    if root['name'] != manifest['package'] or root['version'] != manifest['version']:
        raise ValueError('Manifest and SBOM package identity differ')
    info = package_info(archive) if archive else None
    if info and (info['id'] != manifest['package'] or info['version'] != manifest['version'] or info['frameworks'] != manifest['frameworks']):
        raise ValueError('Package identity/framework coverage differs from manifest')
    if require_clean:
        audit = read_json(release / 'audit.json')
        findings = [p for project in audit.get('projects', []) for framework in project.get('frameworks', [])
                    for kind in ('topLevelPackages', 'transitivePackages') for p in framework.get(kind, []) if p.get('vulnerabilities')]
        if findings:
            raise ValueError(f'{manifest["package"]}: {len(findings)} dependency advisory entries require triage; release gate blocks by default')
    refs = {root['bom-ref']} | {c['bom-ref'] for c in bom['components']}
    for path in sorted((release / 'vex').glob('*.cdx.json')):
        vex = read_json(path)
        validate_bom(vex)
        if vex['metadata']['component'] != root or vex['components'] != bom['components']:
            raise ValueError('VEX component snapshot differs from SBOM')
        for vulnerability in vex.get('vulnerabilities', []):
            if not vulnerability.get('affects') or any(x['ref'] not in refs for x in vulnerability['affects']):
                raise ValueError('VEX has unresolved product references')
    return manifest


def scan_release(item, release, args):
    """Rescan exact recorded package versions, refusing a silently changed graph."""
    manifest = verify_release(release)
    resolution = read_json(release / 'resolution.json')
    created = timestamp()
    with tempfile.TemporaryDirectory(prefix='servicestack-rescan-') as temporary:
        temporary = Path(temporary)
        project = temporary / 'Consumer.csproj'
        xml = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFrameworks>' + ';'.join(manifest['frameworks']) + '</TargetFrameworks>'
        xml += '<AutomaticallyUseReferenceAssemblyPackages>false</AutomaticallyUseReferenceAssemblyPackages><NuGetAudit>true</NuGetAudit><NuGetAuditMode>all</NuGetAuditMode><RestoreEnablePackagePruning>false</RestoreEnablePackagePruning></PropertyGroup>'
        for tfm, nodes in resolution['targets'].items():
            if tfm not in manifest['frameworks']:
                raise ValueError('Unsupported framework/RID in frozen resolution')
            xml += '<ItemGroup Condition="' + "'$(TargetFramework)' == '" + tfm + "'" + '">'
            for key in sorted(nodes):
                name, version = key.rsplit('/', 1)
                xml += '<PackageReference Include="' + escape(name) + '" Version="[' + escape(version) + ']" />'
            xml += '</ItemGroup>'
        project.write_text(xml + '</Project>')
        config = temporary / 'NuGet.Config'
        config.write_text('<configuration><packageSources><clear/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources><auditSources><clear/><add key="nuget.org" value="https://data.nuget.org/v3/index.json"/></auditSources></configuration>')
        log = run(['dotnet', 'restore', project, '--configfile', config, '--force-evaluate'], cwd=temporary)
        if re.search(r'NU190[05]', log):
            raise ValueError('Vulnerability source unavailable; scan is incomplete')
        restored = read_json(temporary / 'obj/project.assets.json')
        for tfm, expected in resolution['targets'].items():
            actual = {key for key, node in restored['targets'][tfm].items() if node.get('type') == 'package'}
            if actual != set(expected):
                raise ValueError(f'{tfm}: frozen dependency graph changed during rescan')
        root_key = next(key for key in restored['libraries'] if key.lower() == (manifest['package'] + '/' + manifest['version']).lower())
        library = restored['libraries'][root_key]
        cached = next((Path(folder) / library['path'] / (manifest['package'].lower() + '.' + manifest['version'].lower() + '.nupkg') for folder in restored['packageFolders'] if (Path(folder) / library['path']).exists()), None)
        if cached is None or digest(cached) != manifest['packageSha256']:
            raise ValueError('Public root package does not match recorded artifact; private builds need their original feed')
        audit = json.loads(run(['dotnet', 'package', 'list', '--project', project, '--no-restore', '--include-transitive', '--vulnerable', '--format', 'json'], cwd=temporary))
        if any(x.get('level', '').lower() == 'error' for x in audit.get('logs', [])):
            raise ValueError('Vulnerability scan failed')
        for entry in audit.get('projects', []):
            entry['path'] = manifest['package']
        snapshot = {'package': manifest['package'], 'version': manifest['version'], 'scannedAt': created, 'packageSha256': manifest['packageSha256'], 'audit': audit,
                    'limitations': manifest['limitations']}
        folder = args.scan_output / manifest['package'] / manifest['version'] if args.scan_output else release / 'scans'
        folder.mkdir(parents=True, exist_ok=True)
        destination = folder / (created.replace(':', '').replace('-', '') + '.json')
        with destination.open('xb') as output:
            output.write(encoded(snapshot))
        findings = [p for entry in audit.get('projects', []) for framework in entry.get('frameworks', []) for kind in ('topLevelPackages', 'transitivePackages') for p in framework.get(kind, []) if p.get('vulnerabilities')]
        print(f'{manifest["package"]} {manifest["version"]}: scan recorded, {len(findings)} advisory entries')
        if findings:
            raise ValueError(f'New/current advisories require review: {destination}')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=['generate', 'verify', 'vex', 'scan'])
    parser.add_argument('--repo', type=Path, default=HERE.parent.parent)
    parser.add_argument('--package-directory', type=Path, help='Staged .nupkg directory (exact release artifacts)')
    parser.add_argument('--package', action='append', help='Configured package ID; repeat to select a subset')
    parser.add_argument('--cyclonedx', type=Path, help='Optional pinned CLI executable (version checked)')
    parser.add_argument('--output-root', type=Path, help='Generate a repo-relative evidence tree here instead of source folders')
    parser.add_argument('--scan-output', type=Path, help='Write fresh audit snapshots here instead of package security folders')
    parser.add_argument('--release', help='Exact release for generation, VEX revisions or scans; generation otherwise initializes latest stable for new packages')
    parser.add_argument('--require-clean', action='store_true', help='Block any dependency advisories pending human triage')
    args = parser.parse_args()
    try:
        items = configurations(args.repo.resolve())
        if args.package:
            selected = {x.lower() for x in args.package}
            if selected - {x['id'].lower() for x in items}:
                raise ValueError('Unknown configured package selection')
            items = [x for x in items if x['id'].lower() in selected]
        if args.release and not re.fullmatch(r'[A-Za-z0-9_.+-]+', args.release):
            raise ValueError('Unsafe release version')
        if args.command == 'scan':
            failures = []
            for item in items:
                parent = item['directory'] / 'security/releases'
                releases = [parent / args.release] if args.release else sorted(parent.glob('*'))
                if not releases:
                    raise ValueError(f'No release evidence for {item["id"]}')
                for release in releases:
                    try:
                        scan_release(item, release, args)
                    except (ValueError, OSError, KeyError) as error:
                        failures.append(str(error))
            if failures:
                raise ValueError('Rescan failures/findings: ' + '; '.join(failures))
            return
        if args.command == 'vex':
            if not args.release or not re.fullmatch(r'[A-Za-z0-9_.+-]+', args.release):
                raise ValueError('VEX requires an exact safe --release version')
            for item in items:
                release = item['directory'] / 'security/releases' / args.release
                verify_release(release)
                created = timestamp()
                vex = render_vex(read_json(release / 'sbom.cdx.json'), read_json(item['directory'] / 'security/vex-decisions.json'), created)
                if vex:
                    destination = release / 'vex' / (created.replace(':', '').replace('-', '') + '.cdx.json')
                    destination.parent.mkdir(exist_ok=True)
                    with destination.open('xb') as output:
                        output.write(encoded(vex))
                    print(f'Wrote reviewed VEX: {destination}')
                else:
                    print(f'{item["id"]}: no reviewed VEX statements for {args.release}')
            return
        if args.command == 'generate':
            generate_packages(items, args)
            return
        if not args.package_directory:
            raise ValueError('--package-directory is required for verify')
        packages = locate_packages(args.package_directory)
        for item in items:
            if item['id'].lower() not in packages:
                raise ValueError(f'Missing staged package: {item["id"]}')
            archive, info = packages[item['id'].lower()]
            release = item['directory'] / 'security/releases' / info['version']
            verify_release(release, archive, args.require_clean)
            print(f'Verified {info["id"]} {info["version"]}')
    except (ValueError, OSError, KeyError, zipfile.BadZipFile, ET.ParseError) as error:
        print(f'Security artifacts: {error}', file=sys.stderr)
        sys.exit(1)


if __name__ == '__main__':
    main()
