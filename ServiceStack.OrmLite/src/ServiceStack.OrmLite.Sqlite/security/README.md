# Package security artifacts

One CycloneDX SBOM per release covers every target framework contained in the actual NuGet archive. Framework-specific bom-ref values preserve dependency applicability within the combined document; canonical NuGet purls identify the packages.

- releases/<version>/sbom.cdx.json: immutable artifact-derived inventory.
- releases/<version>/manifest.json: original package hash, framework coverage, generation details and evidence hashes.
- releases/<version>/resolution.json: declared ranges and frozen resolved dependency graphs.
- releases/<version>/audit.json: managed NuGet advisory observation at generation time, not a security certification.
- releases/<version>/vex/: dated, reviewed vulnerability impact statements, when available.
- releases/<version>/scans/: optional subsequent advisory observations; these never change the SBOM.
- vex-decisions.json: reviewed product-impact decisions used to render VEX; an empty array means no reviewed statements, not absence of vulnerabilities.

The initial 10.4.0 evidence was generated from the published NuGet package, not the current 10.4.1 source tree. Consumers can resolve other dependency versions allowed by declared ranges and need their own application inventory. NuGet scans do not assess ServiceStack code vulnerabilities, operating systems/runtimes or embedded native engine vulnerabilities. Native payloads are hashed where present; identify embedded versions and review advisories separately.

Run the shared build/security/security_artifacts.py commands from the repository root. See build/security/README.md for setup, generation, verification, rescanning, VEX decisions and release evidence promotion. Keep confidential investigation material outside this public folder.
