# Package security artifacts

One CycloneDX SBOM per release covers every target framework in the actual NuGet archive. Framework-specific graph references preserve dependency applicability in the combined document.

- `releases/<version>/sbom.cdx.json`: immutable artifact-derived inventory.
- `releases/<version>/manifest.json`: original archive hash, framework coverage, tool versions and evidence hashes.
- `releases/<version>/resolution.json`: declared dependency ranges and frozen resolved graphs.
- `releases/<version>/audit.json`: managed NuGet advisory observation at generation time.
- `releases/<version>/vex/`: dated reviewed impact statements, when available.
- `releases/<version>/scans/`: subsequent advisory observations.
- `vex-decisions.json`: reviewed decisions; empty assessments mean no reviewed statements.

Run `build/security/generate.sh` from the repository root to initialize newly configured packages. Existing evidence is verified and skipped. Use `--package <ID> --release <version>` to add an explicit release. See `build/security/README.md` for staged release generation, scans and VEX review.

Evidence describes the archived NuGet release, not necessarily the current source tree. Consumers can resolve other versions permitted by dependency ranges. NuGet advisory scans do not assess first-party code, operating systems, runtimes or embedded native engines. Native payload hashes are inventoried; embedded version identification and advisory review need separate assessment. Keep confidential investigation material outside this public folder.
