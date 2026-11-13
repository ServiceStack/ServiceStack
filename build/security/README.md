# ServiceStack SBOM / VEX automation

Initial scope: ServiceStack.Common, ServiceStack.Interfaces, ServiceStack.OrmLite, ServiceStack.OrmLite.Sqlite and ServiceStack.Text. Add future package mappings to packages.json. Discovery is deliberately release-package driven; test/sample projects are not public products.

## Add a package and run

Add an entry to `build/security/packages.json`, using the public NuGet ID and repository-relative project path:

```json
{"id": "ServiceStack.Redis", "project": "ServiceStack.Redis/src/ServiceStack.Redis/ServiceStack.Redis.csproj"}
```

Then run from the repository root (or invoke the script by absolute path):

    ./build/security/generate.sh

The script creates a local Python virtual environment on first use and installs the pinned validator. Requires Python 3.10+ with venv support, .NET SDK 10 and network access for new generation. CycloneDX is pinned to 6.2.0 in the local tool manifest and restored only when generation is needed. Nothing is added to the ServiceStack libraries.

For each newly configured package without retained release evidence, the generator downloads its latest **stable published NuGet release**, derives its inventory from that exact archive, and initializes `security/README.md`, `.gitignore`, `vex-decisions.json` and `releases/<version>/`. It does not use an unreleased source version. Package entries require both `id` and `project`; IDs and project directories must be unique.

Rerunning verifies hashes, schemas and package identity for existing evidence and skips those packages. It does not check for newer NuGet releases, restore tools, resolve dependencies, rescan advisories or change timestamps for skipped packages. After the wrapper's initial dependency setup, an all-existing run works offline. Corrupt/incomplete evidence fails verification instead of being overwritten; fix it deliberately before retrying. Maintained documentation and reviewed VEX decisions are never overwritten.

Select one configured package, or explicitly add a later release:

    ./build/security/generate.sh --package ServiceStack.Redis
    ./build/security/generate.sh --package ServiceStack.Common --release 10.4.1

An explicit release must already be published, unless supplied in a staged archive folder. Existing evidence for that release is verified and skipped. Routine advisory monitoring uses `scan`, described below, rather than regeneration.

## Generate from staged archives and verify

The same generator accepts exact build/release artifacts, retaining the existing CI interface:

    ./build/security/generate.sh --package-directory build/staging
    build/security/.venv/bin/python build/security/security_artifacts.py verify --package-directory build/staging --require-clean

All configured packages must exist in the staged folder unless `--package` selects a subset. An existing release is verified against the original archive hash and skipped. Changed content under the same version fails; it cannot silently replace previous evidence. With `--output-root`, skip checks and generated folders use the output tree, independently of evidence already committed in source folders.

The generator reads actual packaged frameworks, restores isolated temporary consumer projects, invokes CycloneDX per framework, and combines precise dependency graphs into ONE schema-valid SBOM per package/release. It records archive SHA-256, repository/commit when present, declared dependency groups/ranges and frozen resolved graphs. Build-only SourceLink/reference-assembly packages are not introduced into consumer inventories. Missing graph nodes or unavailable advisory data fail generation. Release evidence is written only after generation succeeds.

For direct Python usage or CI setup:

    python3 -m pip install -r build/security/requirements.txt
    python3 -m unittest discover -s build/security/tests -v
    python3 build/security/security_artifacts.py generate

Vendored CycloneDX 1.6 schemas permit offline validation once Python dependencies are installed. The shell wrapper also forwards all generator options.

## CI output and promotion

Both `NuGet Pack` and `A Pre Release Pack` build and upload `ServiceStack Packages` without waiting for security generation. A successful pack run automatically triggers **Package Security Evidence** (`package-security.yml`). The security workflow downloads packages by the originating run ID. It uses current generator tooling with the exact pack commit's package mapping and reviewed decisions, keeping tooling fixes usable when regenerating older builds.

The security workflow uploads **ServiceStack Security** (the evidence tree) and **ServiceStack Security ZIP** (a ZIP plus SHA-256 checksum) on its own Actions run page. `source-run.json` records the repository, originating pack run/commit and security tooling revision. Generated output includes its `packages.json`, so verification and publishing use the same inventory scope. Both artifacts are retained for 90 days. Security failures do not delay or change the completed pack result; evidence is retained for reviewing advisory findings.

To regenerate from retained build outputs, select **Package Security Evidence → Run workflow** and enter the successful pack run ID, or use:

    gh workflow run package-security.yml -f pack_run_id=123456789

Use the run ID in the pack run URL, not its job ID. Re-running an existing security Actions run also uses the original source run. A fresh manual dispatch uses the selected workflow revision's tooling. Source runs must be successful manually dispatched builds in this repository. Expired package artifacts need restoration or a new build; the workflow never silently substitutes public packages.

NuGet Publish selects the latest successful NuGet Pack for its commit (or the optional `pack_run_id` input). It locates successful security evidence by **source pack run ID and archive provenance**, downloads packages from that exact pack run and verifies all package hashes, schemas and framework coverage. If matching retained security evidence is unavailable, publishing fails with instructions to run security generation first. Package Security Evidence and NuGet Publish use `--report-advisories`: recorded dependency findings produce warning annotations and a job summary, but do not fail evidence generation or block publication of other fixes. Package hashes, schemas, framework coverage, source-run provenance and complete audit data remain required; integrity/verification failures still block publication. No blanket `continue-on-error` bypass is used. `--require-clean` remains an explicit opt-in for callers that want an advisory-free gate.

Verify downloaded evidence with:

    python3 build/security/security_artifacts.py verify --repo build/security-output --package-config build/security-output/packages.json --package-directory build/staging --report-advisories

Promote each artifact's `security/releases/<version>` folder to its matching package source folder without overwriting previous evidence. Commit those exact files rather than regenerating from another source revision. The workflow does not automatically commit source changes.

## Public release downloads

Release security assets now come from the original pack evidence, without downloading packages from NuGet again:

1. **NuGet Pack** builds and uploads packages; **Package Security Evidence** generates their SBOM/VEX evidence independently.
2. Run **NuGet Publish**. It waits up to 20 minutes for matching security evidence if generation is still running, then verifies the exact packages against that evidence before pushing them.
3. After a successful push, the publish job adds `publication.json` and uploads **ServiceStack Published Security**, retaining the original inventories and source provenance for 90 days.
4. A separate `release-assets` job compresses that evidence and attaches it to the matching GitHub release. If no matching release exists, it creates a **versioned draft** (for example, `v10.6` for package version `10.6.0`) with placeholder notes and the assets already attached. It does not publish the draft or alter existing release notes.
5. Complete the draft's release notes and publish it when ready. The **Release Security Download** workflow can also reattach the same retained evidence when the release is published or manually dispatched.

This avoids the NuGet publication/indexing race. Release-time jobs do not restore dependencies or silently generate a different graph. An attachment failure leaves the package publication and its evidence intact; rerun the failed attachment job or use the manual release workflow.

The public assets are:

- `servicestack-security-<version>.zip`
- `servicestack-security-<version>.zip.sha256`

The ZIP includes all configured package SBOMs, manifests, resolutions, audit snapshots, dated reviewed VEX when present, an index, source/publication provenance and `SHA256SUMS`. Unrendered VEX decisions and confidential company documents are excluded. Native/template-host coverage limitations remain explicit in the manifests. Recorded advisories remain visible; inventory publication is not a certification or an advisory-free assertion.

Anyone can download these assets from the published release's **Assets** section, for example:

    https://github.com/ServiceStack/ServiceStack/releases/download/v10.6/servicestack-security-10.6.0.zip

This example URL exists only after the matching draft has been published with its assets. Tags such as `v10.6` map to `10.6.0`; `v10.6.1` maps to `10.6.1`. Existing alternate tag formats are matched by normalized version. Multiple matching releases require an explicit tag rather than guessing.

To reattach an existing release's ZIP from its original publish evidence:

    gh workflow run release-security.yml -f release_tag=v10.6

Optionally select an exact NuGet Publish run with `-f publish_run_id=123456789`. The resolver verifies the publishing workflow/repository, successful package-publish job, package version, pack run, source commit and evidence integrity. It also accepts runs where package publishing succeeded but the later attachment job failed. Expired or missing evidence fails explicitly; there is no automatic fallback to nuget.org or a different release. Older releases created before **ServiceStack Published Security** was introduced need their original evidence restored or deliberate separate generation and verification.

Reattaching packages the same retained evidence and replaces the named ZIP/checksum assets. To update reviewed VEX or regenerate a package graph, run the appropriate generation/review workflow and preserve its provenance separately; a release download rerun does not perform a fresh vulnerability assessment. Preserve an independent long-term backup beyond the Actions retention window.

Automatic completion/manual triggers require workflow definitions on the default branch. Publishing a GitHub release using a workflow's `GITHUB_TOKEN` generally does not trigger another workflow, but assets are already attached by NuGet Publish in this flow. Neither package publication nor a release workflow is dispatched by editing these files locally.

## Advisory monitoring

    python3 build/security/security_artifacts.py scan
    python3 build/security/security_artifacts.py scan --release 10.4.0 --package ServiceStack.Common

The daily/manual `security-core.yml` scan is independent of the publication workflows. It deliberately fails when retained release graphs match advisories, preserving the review signal; that failure does not by itself gate NuGet publication. A fixed application dependency does not rewrite a historical release graph or clear its recorded findings.

Scan checks every retained release by default. It restores the EXACT recorded versions, confirms the graph has not changed and verifies the public root archive hash before querying current NuGet advisories. It never overwrites the release SBOM. Private rebuilds need their original feed; the first implementation refuses them rather than substituting a public package.

Use --scan-output <directory> for ephemeral scheduled results. security-core.yml runs tests/schema checks on relevant changes and rescans retained core releases daily and on manual dispatch. It uploads fresh reports even if a finding causes failure. GitHub workflow failure provides the review signal; no automatic public vulnerability disclosure or VEX assertion occurs.

Check native payload/engine and first-party code advisories separately. A clean NuGet result only means no advisory matches in that data source for the resolved managed package baseline.

## Reviewed VEX

Leave assessments empty until there is a real vulnerability-specific decision. Each entry in a package's vex-decisions.json requires: id, HTTPS sourceUrl, exact releaseVersion, explicit frameworks, state, detail, reviewer, reviewedAt (UTC YYYY-MM-DDTHH:MM:SSZ), and a nonempty evidence array. Use safe public evidence references and role names where appropriate; keep exploit details/confidential work elsewhere.

CycloneDX states: exploitable, in_triage, resolved, resolved_with_pedigree, false_positive, not_affected. not_affected additionally requires a supported justification. Affected/fixed decisions require action describing mitigation/fix/upgrade guidance. One entry per advisory/release can list multiple frameworks. There is no automatic conclusion from scanner results.

    python3 build/security/security_artifacts.py vex --release 10.4.0 --package ServiceStack.Text

This validates existing SBOM evidence and renders a dated self-contained VEX snapshot scoped to exact framework variants. Empty decisions produce no VEX file. Previous snapshots remain available; the most recent is the latest assessment. The manifest's VEX description is a creation-time observation, not a dynamic latest-VEX pointer.

## Limitations and next scope

NuGet dependency ranges allow downstream consumers to choose other versions. Framework-qualified bom-ref values represent graph instances; repeated canonical purls across frameworks are intentional. Native archive payloads include alternative RID/architecture files that may not be loaded by every deployment. Embedded software version/license identification needs human augmentation before claiming complete native vulnerability coverage. There is no blanket Microsoft-package exemption, CRA conformity assertion, automatic finding closure or signed authenticity attestation.

Future work: expand package mapping, integrate native engine identity/advisory review, add supported-release selection tied to an approved support schedule, automate evidence promotion to a release PR and permanent archive, and configure public versioned download URLs. This implementation generates actual SBOMs and supports reviewed VEX, but does not invent an LTS policy or vulnerability findings.

Content-only packages with no `lib/` or `ref/` assets use explicit supported `net*` compatibility groups from their nuspec. Their manifests identify this source of framework coverage, and shipped content files are inventoried with SHA-256 hashes. External template hosts and assembly references in templates require separate review; they are not NuGet dependency scan results. Packages without identifiable supported compatibility groups still fail rather than inventing a framework.
