import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
from types import SimpleNamespace
import zipfile

spec = importlib.util.spec_from_file_location('security_artifacts', Path(__file__).resolve().parents[1] / 'security_artifacts.py')
security = importlib.util.module_from_spec(spec)
spec.loader.exec_module(security)


class ArtifactTests(unittest.TestCase):
    def setUp(self):
        self.info = {'id': 'ServiceStack.Test', 'version': '1.0.0', 'frameworks': ['net472', 'net10.0'], 'licenses': [], 'repository': {}, 'dependencyGroups': []}
        self.created = '2026-10-07T00:00:00Z'
        self.documents = {'net472': self.graph('System.Memory', '4.6.3'), 'net10.0': self.graph('System.Diagnostics.DiagnosticSource', '10.0.0')}
        self.bom = security.aggregate(self.info, self.documents, 'a' * 64, self.created)

    def graph(self, name, version):
        return {'components': [
            {'type': 'library', 'bom-ref': 'root', 'name': 'ServiceStack.Test', 'version': '1.0.0'},
            {'type': 'library', 'bom-ref': 'dep', 'name': name, 'version': version},
            {'type': 'library', 'bom-ref': 'build-only', 'name': 'Not.Shipped', 'version': '1.0.0'},
        ], 'dependencies': [{'ref': 'root', 'dependsOn': ['dep']}, {'ref': 'dep', 'dependsOn': []}, {'ref': 'build-only', 'dependsOn': []}]}

    def test_framework_graphs_do_not_leak_dependencies(self):
        security.validate_bom(self.bom)
        components = {c['bom-ref']: c for c in self.bom['components']}
        graph = {d['ref']: d['dependsOn'] for d in self.bom['dependencies']}
        for tfm, name in [('net472', 'System.Memory'), ('net10.0', 'System.Diagnostics.DiagnosticSource')]:
            ref = 'pkg:nuget/servicestack.test@1.0.0#tfm=' + tfm
            self.assertEqual([components[x]['name'] for x in graph[ref]], [name])
        self.assertNotIn('Not.Shipped', [c['name'] for c in components.values()])

    def test_same_identity_preserves_different_framework_edges(self):
        documents = {tfm: self.graph('Shared.Dependency', '2.0.0') for tfm in self.info['frameworks']}
        documents['net472']['components'].append({'type': 'library', 'bom-ref': 'extra', 'name': 'Legacy.Dependency', 'version': '1.0.0'})
        documents['net472']['dependencies'][1]['dependsOn'] = ['extra']
        documents['net472']['dependencies'].append({'ref': 'extra', 'dependsOn': []})
        bom = security.aggregate(self.info, documents, 'a' * 64, self.created)
        security.validate_bom(bom)
        graph = {d['ref']: d['dependsOn'] for d in bom['dependencies']}
        self.assertEqual(graph['pkg:nuget/shared.dependency@2.0.0#tfm=net10.0'], [])
        self.assertEqual(len(graph['pkg:nuget/shared.dependency@2.0.0#tfm=net472']), 1)

    def test_unresolved_edges_and_wrong_root_rejected(self):
        self.documents['net472']['dependencies'][0]['dependsOn'] = ['missing']
        with self.assertRaisesRegex(ValueError, 'incomplete'):
            security.aggregate(self.info, self.documents, 'a' * 64, self.created)
        wrong = {**self.info, 'version': '2.0.0'}
        with self.assertRaisesRegex(ValueError, 'exact packaged root'):
            security.aggregate(wrong, {'net10.0': self.documents['net10.0']}, 'a' * 64, self.created)

    def test_missing_framework_rejected(self):
        self.bom['metadata']['component']['properties'][0]['value'] += ';net8.0'
        with self.assertRaisesRegex(ValueError, 'coverage'):
            security.validate_bom(self.bom)

    def decision(self):
        return {'id': 'TEST-ADVISORY', 'sourceUrl': 'https://example.org/advisory', 'releaseVersion': '1.0.0',
                'frameworks': ['net472'], 'state': 'not_affected', 'justification': 'code_not_reachable',
                'detail': 'Synthetic test only.', 'reviewer': 'Test reviewer', 'reviewedAt': self.created, 'evidence': ['test evidence']}

    def test_empty_decisions_do_not_create_all_clear_vex(self):
        self.assertIsNone(security.render_vex(self.bom, {'schemaVersion': 1, 'assessments': []}, self.created))

    def test_vex_exact_release_framework_and_review(self):
        decision = self.decision()
        document = security.render_vex(self.bom, {'schemaVersion': 1, 'assessments': [decision]}, self.created)
        security.validate_bom(document)
        self.assertEqual(document['vulnerabilities'][0]['affects'], [{'ref': 'pkg:nuget/servicestack.test@1.0.0#tfm=net472'}])
        decision['releaseVersion'] = '2.0.0'
        self.assertIsNone(security.render_vex(self.bom, {'schemaVersion': 1, 'assessments': [decision]}, self.created))
        for field in ('reviewer', 'evidence', 'justification'):
            decision = self.decision()
            del decision[field]
            with self.assertRaises(ValueError):
                security.render_vex(self.bom, {'schemaVersion': 1, 'assessments': [decision]}, self.created)

    def test_native_file_hashes_inventory(self):
        with tempfile.TemporaryDirectory() as temporary:
            archives = {}
            for c in [self.bom['metadata']['component']] + self.bom['components']:
                key = (c['name'].lower(), c['version'])
                if key in archives:
                    continue
                path = Path(temporary) / (c['name'] + '.zip')
                with zipfile.ZipFile(path, 'w') as archive:
                    archive.writestr('runtimes/linux-x64/native/libtest.so', b'native test')
                archives[key] = path
            records = security.native_inventory(self.bom, archives)
            security.validate_bom(self.bom)
            self.assertEqual(len(records), 3)
            self.assertTrue(all(len(x['sha256']) == 64 for x in records))

    def test_frameworks_come_from_archive(self):
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / 'test.nupkg'
            with zipfile.ZipFile(path, 'w') as archive:
                archive.writestr('test.nuspec', '<package><metadata><id>ServiceStack.Test</id><version>1.0.0</version></metadata></package>')
                archive.writestr('lib/net472/Test.dll', b'assembly')
                archive.writestr('lib/net10.0/Test.dll', b'assembly')
            self.assertEqual(security.package_info(path)['frameworks'], ['net10.0', 'net472'])

    def test_content_only_archive_uses_declared_compatibility_and_hashes_files(self):
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / 'test.nupkg'
            with zipfile.ZipFile(path, 'w') as archive:
                archive.writestr('test.nuspec', '<package><metadata><id>ServiceStack.Test</id><version>1.0.0</version><dependencies><group targetFramework="net10.0" /></dependencies></metadata></package>')
                archive.writestr('contentFiles/any/any/test.tt', b'template')
            info = security.package_info(path)
            self.assertEqual(info['frameworks'], ['net10.0'])
            self.assertTrue(info['frameworkSource'].startswith('Declared'))
            records = security.content_inventory(self.bom, path, info['contentFiles'])
            security.validate_bom(self.bom)
            self.assertEqual(records[0]['sha256'], security.hashlib.sha256(b'template').hexdigest())

    def test_content_only_archive_without_compatibility_is_rejected(self):
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / 'test.nupkg'
            with zipfile.ZipFile(path, 'w') as archive:
                archive.writestr('test.nuspec', '<package><metadata><id>ServiceStack.Test</id><version>1.0.0</version></metadata></package>')
                archive.writestr('contentFiles/any/any/test.tt', b'template')
            with self.assertRaisesRegex(ValueError, 'unsupported or absent'):
                security.package_info(path)

    def test_manifest_detects_modified_evidence(self):
        with tempfile.TemporaryDirectory() as temporary:
            release = Path(temporary)
            data = security.encoded(self.bom)
            (release / 'sbom.cdx.json').write_bytes(data)
            (release / 'manifest.json').write_text(json.dumps({'package': 'ServiceStack.Test', 'version': '1.0.0', 'files': {'sbom.cdx.json': security.hashlib.sha256(data).hexdigest()}}))
            security.verify_release(release)
            (release / 'sbom.cdx.json').write_bytes(data + b' ')
            with self.assertRaisesRegex(ValueError, 'evidence changed'):
                security.verify_release(release)


class GenerationTests(unittest.TestCase):
    def arguments(self, **overrides):
        return SimpleNamespace(package_directory=None, output_root=None, release=None,
                               require_clean=False, cyclonedx=None, **overrides)

    def item(self, directory, name='ServiceStack.Test'):
        return {'id': name, 'project': name + '/' + name + '.csproj', 'directory': directory}

    def existing(self, item, version='1.0.0'):
        release = item['directory'] / 'security/releases' / version
        release.mkdir(parents=True)
        return release

    def test_cyclonedx_uses_exact_restore_cache_without_changing_parent_environment(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            cache = directory / 'isolated-packages'
            (directory / 'net10.0.json').write_text('{}')
            with patch.dict(security.os.environ, {'NUGET_PACKAGES': '/wrong-global-cache', 'CACHE_REGRESSION': 'preserved'}), patch.object(security, 'run') as run:
                security.generate_framework_bom(['dotnet-CycloneDX'], directory / 'Consumer.csproj', 'net10.0', directory, cache)
                environment = run.call_args.kwargs['env']
                self.assertEqual(environment['NUGET_PACKAGES'], str(cache.resolve()))
                self.assertEqual(environment['CACHE_REGRESSION'], 'preserved')
                self.assertEqual(security.os.environ['NUGET_PACKAGES'], '/wrong-global-cache')
                self.assertIn('--disable-package-restore', run.call_args.args[0])

    def test_existing_packages_skip_download_and_tools(self):
        with tempfile.TemporaryDirectory() as temporary:
            item = self.item(Path(temporary))
            release = self.existing(item)
            with patch.object(security, 'verify_release', return_value={'package': item['id'], 'version': release.name}) as verify, patch.object(security, 'run') as run, patch.object(security, 'published_package') as download:
                security.generate_packages([item], self.arguments())
                verify.assert_called_once_with(release, None, False)
                run.assert_not_called()
                download.assert_not_called()

    def test_only_new_package_is_generated_and_rerun_skips_it(self):
        with tempfile.TemporaryDirectory() as temporary:
            items = [self.item(Path(temporary) / name, name) for name in ('Old', 'New')]
            self.existing(items[0])
            def verify(release, *args):
                return {'package': release.parents[2].name, 'version': release.name}
            def generate(item, archive, info, *args):
                self.existing(item, info['version'])
            with patch.object(security, 'verify_release', side_effect=verify), patch.object(security, 'run', return_value=security.TOOL_VERSION), patch.object(security, 'published_package', return_value=(Path(temporary) / 'new.nupkg', {'version': '2.0.0'})) as download, patch.object(security, 'generate_one', side_effect=generate) as build:
                security.generate_packages(items, self.arguments())
                security.generate_packages(items, self.arguments())
                self.assertEqual(download.call_count, 1)
                self.assertEqual(build.call_count, 1)
                self.assertEqual(build.call_args.args[0]['id'], 'New')
                self.assertEqual(security.read_json(items[1]['directory'] / 'security/vex-decisions.json')['assessments'], [])

    def test_initialization_preserves_maintained_files(self):
        with tempfile.TemporaryDirectory() as temporary:
            item = self.item(Path(temporary))
            security.initialize_security(item, self.arguments())
            decisions = item['directory'] / 'security/vex-decisions.json'
            decisions.write_text('reviewed decisions must survive')
            readme = item['directory'] / 'security/README.md'
            readme.write_text('Maintained package documentation')
            security.initialize_security(item, self.arguments())
            self.assertEqual(decisions.read_text(), 'reviewed decisions must survive')
            self.assertEqual(readme.read_text(), 'Maintained package documentation')

    def test_tampered_retained_evidence_blocks_generation(self):
        with tempfile.TemporaryDirectory() as temporary:
            item = self.item(Path(temporary))
            self.existing(item)
            with patch.object(security, 'verify_release', side_effect=ValueError('evidence changed')), patch.object(security, 'run') as run:
                with self.assertRaisesRegex(ValueError, 'evidence changed'):
                    security.generate_packages([item], self.arguments())
                run.assert_not_called()

    def test_staged_existing_release_verifies_original_archive(self):
        with tempfile.TemporaryDirectory() as temporary:
            item = self.item(Path(temporary))
            release = self.existing(item)
            archive = Path(temporary) / 'test.nupkg'
            args = self.arguments()
            args.package_directory = Path(temporary)
            with patch.object(security, 'locate_packages', return_value={item['id'].lower(): (archive, {'version': '1.0.0'})}), patch.object(security, 'verify_release', side_effect=ValueError('supplied archive hash differs')) as verify, patch.object(security, 'run') as run:
                with self.assertRaisesRegex(ValueError, 'archive hash differs'):
                    security.generate_packages([item], args)
                verify.assert_called_once_with(release, archive, False)
                run.assert_not_called()

    def test_explicit_release_generates_for_existing_package(self):
        with tempfile.TemporaryDirectory() as temporary:
            item = self.item(Path(temporary))
            self.existing(item)
            args = self.arguments()
            args.release = '2.0.0'
            with patch.object(security, 'run', return_value=security.TOOL_VERSION), patch.object(security, 'published_package', return_value=(Path(temporary) / 'test.nupkg', {'version': '2.0.0'})) as download, patch.object(security, 'generate_one'), patch.object(security, 'initialize_security'):
                security.generate_packages([item], args)
                self.assertEqual(download.call_args.args[2], '2.0.0')


if __name__ == '__main__':
    unittest.main()
