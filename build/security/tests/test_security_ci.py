import hashlib
from contextlib import redirect_stderr, redirect_stdout
import io
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch
from urllib.error import HTTPError
import zipfile

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import bundle_security
import ci_workflows
import scan_latest
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

    def test_publication_workflows_report_advisories_without_disabling_integrity_checks(self):
        repo = Path(__file__).resolve().parents[3]
        for name in ('nuget-push.yml', 'package-security.yml'):
            with self.subTest(workflow=name):
                workflow = (repo / '.github/workflows' / name).read_text()
                verify = [line for line in workflow.splitlines() if 'security_artifacts.py verify ' in line]
                self.assertEqual(len(verify), 1)
                self.assertIn('--report-advisories', verify[0])
                self.assertNotIn('--require-clean', verify[0])
                self.assertNotIn('continue-on-error', workflow)
        monitor = (repo / '.github/workflows/security-core.yml').read_text()
        self.assertIn('scan_latest.py --scan-output build/security-scans', monitor)
        self.assertNotIn('continue-on-error', monitor)

    def test_missing_security_run_blocks_publish(self):
        with tempfile.TemporaryDirectory() as temporary, patch.object(ci_workflows, 'api', side_effect=[self.pack(), {'workflow_runs': []}]):
            with self.assertRaisesRegex(ValueError, 'run Package Security Evidence'):
                ci_workflows.resolve_publish('ServiceStack/ServiceStack', 'a' * 40, Path(temporary) / 'out', '123')


class LatestScanTests(unittest.TestCase):
    package_url = 'https://api.nuget.org/v3-flatcontainer/servicestack/10.5.0/servicestack.10.5.0.nupkg'

    def test_latest_release_scans_exact_published_package_and_preserves_findings(self):
        package = io.BytesIO()
        with zipfile.ZipFile(package, 'w') as archive:
            archive.writestr('ServiceStack.nuspec', '<package><metadata><id>ServiceStack</id><version>10.5.0</version></metadata></package>')
            archive.writestr('lib/net10.0/_._', '')
        data = package.getvalue()

        def dotnet(args, cwd):
            if args[1] == 'restore':
                self.assertIn('Version="[10.5.0]"', (cwd / 'Consumer.csproj').read_text())
                library = 'servicestack/10.5.0'
                cache = args[args.index('--packages') + 1] / library
                cache.mkdir(parents=True)
                (cache / 'servicestack.10.5.0.nupkg').write_bytes(data)
                (cwd / 'obj').mkdir()
                (cwd / 'obj/project.assets.json').write_text(json.dumps({
                    'libraries': {'ServiceStack/10.5.0': {'path': library}},
                    'targets': {'net10.0': {'ServiceStack/10.5.0': {'type': 'package'}}},
                }))
                return ''
            self.assertIn('--include-transitive', args)
            return json.dumps({'projects': [{'path': str(cwd), 'frameworks': [{'framework': 'net10.0',
                'transitivePackages': [{'vulnerabilities': [{'severity': 'High'}]}]}]}]})

        with tempfile.TemporaryDirectory() as temporary, redirect_stdout(io.StringIO()), redirect_stderr(io.StringIO()) as errors:
            argv = ['scan_latest.py', '--package', 'ServiceStack', '--scan-output', temporary]
            with patch.object(sys, 'argv', argv), patch.object(security_artifacts, 'run', side_effect=dotnet), \
                    patch.object(scan_latest.urllib.request, 'urlopen', side_effect=[io.BytesIO(b'{"tag_name":"v10.5"}'), io.BytesIO(data)]) as fetch:
                self.assertEqual(scan_latest.main(), 1)
            self.assertEqual(fetch.call_args_list[0].args[0].full_url, scan_latest.LATEST_RELEASE)
            self.assertEqual(fetch.call_args_list[1].args[0], self.package_url)
            report, = (Path(temporary) / 'ServiceStack/10.5.0').glob('*.json')
            snapshot = json.loads(report.read_text())
            self.assertEqual(snapshot['packageSha256'], hashlib.sha256(data).hexdigest())
            self.assertEqual(snapshot['audit']['projects'][0]['path'], 'ServiceStack')
            self.assertIn('advisories require review', errors.getvalue())

    def test_missing_published_version_fails_without_falling_back(self):
        missing = HTTPError(self.package_url, 404, 'Not Found', {}, None)
        argv = ['scan_latest.py', '--package', 'ServiceStack']
        with patch.object(sys, 'argv', argv), patch.object(scan_latest.urllib.request, 'urlopen',
                side_effect=[io.BytesIO(b'{"tag_name":"v10.5"}'), missing]) as fetch, \
                redirect_stdout(io.StringIO()), redirect_stderr(io.StringIO()) as errors:
            self.assertEqual(scan_latest.main(), 1)
        self.assertEqual(fetch.call_count, 2)
        self.assertEqual(fetch.call_args.args[0], self.package_url)
        self.assertIn('ServiceStack 10.5.0: HTTP Error 404', errors.getvalue())


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
