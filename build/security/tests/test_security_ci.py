import hashlib
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch
import zipfile

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import bundle_security
import ci_workflows
import security_artifacts


class WorkflowTests(unittest.TestCase):
    def pack(self, **changes):
        return {'id': 123, 'conclusion': 'success', 'event': 'workflow_dispatch',
                'head_repository': {'full_name': 'ServiceStack/ServiceStack'},
                'path': '.github/workflows/nuget-pack.yml', 'head_sha': 'a' * 40, **changes}

    def test_source_requires_successful_same_repository_pack(self):
        self.assertEqual(ci_workflows.validate_pack(self.pack(), 'ServiceStack/ServiceStack')['id'], 123)
        for changes in ({'conclusion': 'failure'}, {'event': 'pull_request'},
                        {'head_repository': {'full_name': 'someone/fork'}}, {'path': 'other.yml'}):
            with self.assertRaises(ValueError):
                ci_workflows.validate_pack(self.pack(**changes), 'ServiceStack/ServiceStack')

    def test_publish_rejects_prerelease_pack_and_wrong_commit(self):
        for changes in ({'path': '.github/workflows/pre-release-pack.yml'}, {'head_sha': 'b' * 40}):
            with self.assertRaises(ValueError):
                ci_workflows.validate_pack(self.pack(**changes), 'ServiceStack/ServiceStack', 'a' * 40, stable=True)

    def test_publish_pairs_by_source_run_not_security_workflow_commit(self):
        pack = self.pack()
        candidates = [{'id': n, 'conclusion': 'success', 'head_repository': pack['head_repository'], 'head_sha': 'b' * 40} for n in (456, 455)]
        def api(path):
            return pack if path.endswith('/123') else {'workflow_runs': candidates}
        def download(*args):
            run_id = args[2]
            path = Path(args[-1])
            (path / 'source-run.json').write_text(json.dumps({'repository': 'ServiceStack/ServiceStack',
                'packRunId': '999' if run_id == 456 else '123', 'packCommit': 'a' * 40}))
            (path / 'packages.json').write_text('{}')
        with tempfile.TemporaryDirectory() as temporary, patch.object(ci_workflows, 'api', side_effect=api), patch.object(ci_workflows, 'gh', side_effect=download):
            destination = Path(temporary) / 'evidence'
            outputs = ci_workflows.resolve_publish('ServiceStack/ServiceStack', 'a' * 40, destination, '123')
            self.assertEqual(outputs['security_run_id'], 455)
            self.assertEqual(outputs['pack_run_id'], 123)
            self.assertTrue((destination / 'packages.json').is_file())

    def test_missing_security_run_blocks_publish(self):
        with tempfile.TemporaryDirectory() as temporary, patch.object(ci_workflows, 'api', side_effect=[self.pack(), {'workflow_runs': []}]):
            with self.assertRaisesRegex(ValueError, 'run Package Security Evidence'):
                ci_workflows.resolve_publish('ServiceStack/ServiceStack', 'a' * 40, Path(temporary) / 'out', '123')


class BundleTests(unittest.TestCase):
    def test_release_tags_map_to_exact_versions(self):
        for tag, version in [('v10.4', '10.4.0'), ('v10.4.1', '10.4.1'), ('10.4.1-rc.1', '10.4.1-rc.1')]:
            self.assertEqual(bundle_security.release_version(tag), version)
        for tag in ('latest', '--clobber', '../10.4', 'v10.4\n'):
            with self.assertRaises(ValueError):
                bundle_security.release_version(tag)

    def fixture(self, root):
        (root / 'packages.json').write_text(json.dumps({'schemaVersion': 1, 'packages': [{'id': 'ServiceStack.Test', 'project': 'src/Test/Test.csproj'}]}))
        release = root / 'src/Test/security/releases/1.0.0'
        release.mkdir(parents=True)
        info = {'id': 'ServiceStack.Test', 'version': '1.0.0', 'frameworks': ['net10.0'], 'licenses': [], 'repository': {}, 'dependencyGroups': []}
        graph = {'components': [{'type': 'library', 'bom-ref': 'root', 'name': 'ServiceStack.Test', 'version': '1.0.0'}],
                 'dependencies': [{'ref': 'root', 'dependsOn': []}]}
        bom = security_artifacts.aggregate(info, {'net10.0': graph}, 'a' * 64, '2026-10-07T00:00:00Z')
        data = security_artifacts.encoded(bom)
        (release / 'sbom.cdx.json').write_bytes(data)
        manifest = {'package': 'ServiceStack.Test', 'version': '1.0.0', 'packageSha256': 'a' * 64, 'frameworks': ['net10.0'],
                    'files': {'sbom.cdx.json': hashlib.sha256(data).hexdigest()}}
        (release / 'manifest.json').write_text(json.dumps(manifest))
        # Unrendered decisions are inputs, not part of public release inventories.
        (root / 'src/Test/security/vex-decisions.json').write_text('private input must not be copied')
        return release

    def test_zip_contains_validated_evidence_and_checksums_only(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary) / 'evidence'
            root.mkdir()
            self.fixture(root)
            output = Path(temporary) / 'security.zip'
            bundle_security.bundle(root, output, '1.0.0')
            with zipfile.ZipFile(output) as archive:
                self.assertFalse(any('vex-decisions' in n for n in archive.namelist()))
                for line in archive.read('SHA256SUMS').decode().splitlines():
                    sha, name = line.split('  ', 1)
                    self.assertEqual(hashlib.sha256(archive.read(name)).hexdigest(), sha)
                self.assertEqual(len(json.loads(archive.read('index.json'))['packages']), 1)
            self.assertEqual(output.with_suffix('.zip.sha256').read_text().split()[0], hashlib.sha256(output.read_bytes()).hexdigest())

    def test_tampered_or_missing_evidence_is_not_bundled(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            release = self.fixture(root)
            (release / 'sbom.cdx.json').write_text('{}')
            with self.assertRaisesRegex(ValueError, 'evidence changed'):
                bundle_security.bundle(root, root / 'security.zip')
            with self.assertRaises(OSError):
                bundle_security.bundle(root, root / 'security.zip', '2.0.0')
            self.assertFalse((root / 'security.zip').exists())


if __name__ == '__main__':
    unittest.main()
