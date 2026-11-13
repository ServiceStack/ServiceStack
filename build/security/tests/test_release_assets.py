import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch
import zipfile

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import release_assets
import ci_workflows
import test_security_ci as existing_tests


class ReleaseAssetsTests(unittest.TestCase):
    repo = 'ServiceStack/ServiceStack'
    commit = 'a' * 40

    def fixture(self, root):
        existing_tests.BundleTests.fixture(self, root)
        (root / 'source-run.json').write_text(json.dumps({'repository': self.repo, 'packRunId': '123', 'packCommit': self.commit}))
        release_assets.record_publication(root, self.repo, '789', '123', self.commit)
        return root

    def run_info(self, **changes):
        return {'id': 789, 'head_sha': self.commit, 'event': 'workflow_dispatch', 'conclusion': 'success',
                'path': '.github/workflows/nuget-push.yml', 'head_repository': {'full_name': self.repo}, **changes}

    def test_record_requires_matching_pack_provenance(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = self.fixture(Path(temporary))
            for values in [(self.repo, '789', '999', self.commit), ('other/repo', '789', '123', self.commit), (self.repo, '789', '123', 'b' * 40)]:
                with self.assertRaisesRegex(ValueError, 'differs'):
                    release_assets.record_publication(root, *values)

    def test_mixed_versions_and_wrong_publication_are_rejected(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = self.fixture(Path(temporary))
            with self.assertRaisesRegex(ValueError, 'provenance'):
                release_assets.validate_publication(root, self.repo, '1.0.0', self.run_info(head_sha='b' * 40))
            with self.assertRaisesRegex(ValueError, 'provenance'):
                release_assets.validate_publication(root, self.repo, '1.0.1', self.run_info())
            (root/'other/security/releases/2.0.0').mkdir(parents=True)
            with patch.object(release_assets, 'configurations', return_value=[{'id': 'A', 'directory': root/'src/Test'}, {'id': 'B', 'directory': root/'other'}]), patch.object(release_assets, 'verify_release', side_effect=[{'package':'A','version':'1.0.0'}, {'package':'B','version':'2.0.0'}]):
                with self.assertRaisesRegex(ValueError, 'mixed package versions'): release_assets.evidence_version(root)

    def test_attach_uses_existing_release_without_creating_or_editing_notes(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)/'evidence'
            root.mkdir(); self.fixture(root)
            with patch.object(release_assets, 'api', return_value=[{'tag_name': 'v1.0', 'draft': False}]), patch.object(release_assets, 'gh') as gh:
                release_assets.attach(root, self.repo, Path(temporary)/'zip')
                self.assertEqual(gh.call_count, 1)
                self.assertEqual(gh.call_args.args[:3], ('release', 'upload', 'v1.0'))
                archive = Path(temporary)/'zip/servicestack-security-1.0.0.zip'
                with zipfile.ZipFile(archive) as z:
                    self.assertIn('publication.json', z.namelist())
                    self.assertEqual(json.loads(z.read('publication.json'))['publishRunId'], '789')

    def test_missing_release_creates_versioned_draft_and_then_attaches(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = self.fixture(Path(temporary))
            with patch.object(release_assets, 'api', return_value=[]), patch.object(release_assets, 'gh') as gh:
                release_assets.attach(root, self.repo, root/'zip')
                create = gh.call_args_list[0].args
                self.assertEqual(create[:3], ('release','create','v1.0'))
                self.assertIn('--draft', create)
                self.assertEqual(create[create.index('--target')+1], self.commit)
                self.assertEqual(gh.call_args_list[1].args[:3], ('release','upload','v1.0'))

    def test_wrong_tag_blocks_before_any_github_mutation(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = self.fixture(Path(temporary))
            with patch.object(release_assets, 'gh') as gh:
                with self.assertRaisesRegex(ValueError, 'tag does not match'):
                    release_assets.attach(root, self.repo, root/'zip', 'v2.0')
                gh.assert_not_called()

    def test_matching_tags_are_normalized_and_ambiguous_tags_fail(self):
        self.assertEqual(release_assets.select_release_tag('10.6.0', [{'tag_name':'v10.6'},{'tag_name':'old-name'}]), 'v10.6')
        with self.assertRaisesRegex(ValueError, 'Multiple'):
            release_assets.select_release_tag('10.6.0', [{'tag_name':'v10.6'}, {'tag_name':'v10.6.0'}])

    def test_failed_attachment_does_not_hide_successful_package_publication(self):
        with patch.object(release_assets, 'api', return_value={'jobs':[{'name':'nuget-push','conclusion':'success'}]}):
            self.assertTrue(release_assets.successful_publication(self.run_info(conclusion='failure',status='completed'), self.repo))
        with patch.object(release_assets, 'api', return_value={'jobs':[{'name':'nuget-push','conclusion':'failure'}]}):
            self.assertFalse(release_assets.successful_publication(self.run_info(conclusion='failure',status='completed'), self.repo))
        self.assertFalse(release_assets.successful_publication(self.run_info(path='.github/workflows/pre-release-pack.yml'), self.repo))

    def test_fetch_matches_published_version_and_provenance(self):
        with tempfile.TemporaryDirectory() as temporary:
            destination = Path(temporary)/'fetched'
            def download(*args):
                self.fixture(Path(args[-1]))
            with patch.object(release_assets, 'api', return_value=self.run_info()), patch.object(release_assets, 'gh', side_effect=download):
                record = release_assets.fetch_publication(self.repo,'v1.0',destination,'789')
                self.assertEqual(record['publishRunId'],'789')
                self.assertTrue((destination/'source-run.json').exists())

    def test_missing_publication_fails_without_nuget_fallback(self):
        with tempfile.TemporaryDirectory() as temporary, patch.object(release_assets,'api',return_value={'workflow_runs':[]}):
            with self.assertRaisesRegex(ValueError,'No retained published security evidence'):
                release_assets.fetch_publication(self.repo,'v1.0',Path(temporary)/'out')

    def test_publish_waits_for_security_generation_but_not_for_invalid_evidence(self):
        with patch.object(ci_workflows, 'resolve_publish', side_effect=[ci_workflows.EvidenceUnavailable('pending'), {'pack_run_id':123}]), patch.object(ci_workflows.time, 'sleep') as sleep:
            result=ci_workflows.resolve_publish_wait(self.repo,self.commit,Path('/tmp/not-written'),wait_seconds=60)
            self.assertEqual(result['pack_run_id'],123)
            sleep.assert_called_once()
        with patch.object(ci_workflows, 'resolve_publish', side_effect=ValueError('invalid provenance')), patch.object(ci_workflows.time,'sleep') as sleep:
            with self.assertRaisesRegex(ValueError,'invalid provenance'):
                ci_workflows.resolve_publish_wait(self.repo,self.commit,Path('/tmp/not-written'),wait_seconds=60)
            sleep.assert_not_called()

    def test_publish_wait_is_bounded(self):
        with patch.object(ci_workflows, 'resolve_publish', side_effect=ci_workflows.EvidenceUnavailable('pending')), patch.object(ci_workflows.time,'monotonic',side_effect=[0,61]), patch.object(ci_workflows.time,'sleep') as sleep:
            with self.assertRaisesRegex(ci_workflows.EvidenceUnavailable,'pending'):
                ci_workflows.resolve_publish_wait(self.repo,self.commit,Path('/tmp/not-written'),wait_seconds=60)
            sleep.assert_not_called()

    def test_workflows_reuse_pack_evidence_and_record_only_after_push(self):
        repo=Path(__file__).resolve().parents[3]
        publish=(repo/'.github/workflows/nuget-push.yml').read_text()
        self.assertLess(publish.index('dotnet nuget push'),publish.index('release_assets.py record'))
        self.assertIn('needs: nuget-push',publish)
        release=(repo/'.github/workflows/release-security.yml').read_text()
        self.assertIn('release_assets.py fetch',release)
        self.assertNotIn('security_artifacts.py generate',release)
        self.assertNotIn('setup-dotnet',release)
        pack_security=(repo/'.github/workflows/package-security.yml').read_text()
        self.assertIn('workflows: ["NuGet Pack", "A Pre Release Pack"]',pack_security)
        self.assertIn('--package-directory build/staging',pack_security)


if __name__ == '__main__': unittest.main()
