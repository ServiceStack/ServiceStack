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

The pack workflow uses --output-root build/security-output to create the same repository-relative folder layout as a separate artifact. The push workflow downloads that evidence and verifies original package hashes, schema, dependency references and framework coverage before publishing. --require-clean blocks advisory entries until triaged; this first implementation does not automatically suppress them using VEX.

The initial committed evidence is for published 10.4.0. For each future release, download the ServiceStack Security artifact from the same successful pack run. Verify it with:

    python3 build/security/security_artifacts.py verify --repo build/security-output --package-directory build/staging --require-clean

Promote each artifact's security/releases/<version> directory to its matching package source folder, without overwriting an existing release. Commit the exact generated files; do not regenerate from main after publishing. The artifact tree preserves package paths to make this copy unambiguous. This promotion can be handled by a release-bot PR later; it is not an automatic source commit in this implementation.

CI evidence retention is 90 days. That is a transfer window, not a permanent CRA archive. Keep committed release records and an independently backed-up long-term release archive. Export the same SBOM/VEX as customer/release assets as appropriate; package publishing does not automatically publish public evidence download URLs.

## Advisory monitoring

    python3 build/security/security_artifacts.py scan
    python3 build/security/security_artifacts.py scan --release 10.4.0 --package ServiceStack.Common

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
