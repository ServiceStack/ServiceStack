# ServiceStack.Jobs feature proposals

Status: proposed only; no entry here authorizes implementation. Reviewed 2026-10-31.

See [existing capabilities](../FEATURES.md), [suite priorities](../../plan/FEATURE-PROPOSALS.md), and [acceptance workflow](../../FEATURES.md#planning-workflow).

## JOBS-001: Executable provider capability matrix and deployment checks

- Status: Proposed; acceptance not requested or granted.
- Type: Documentation, diagnostics and conformance improvement.
- Initial disposition: Investigate next.
- Potential value: High potential reduction in incorrect deployment assumptions for durable work.
- Relative effort: Small–medium; not a delivery estimate.
- Evidence confidence: High that providers have distinct ownership models; specific diagnostic gaps need endpoint/UI review.
- Customer evidence: Not yet validated against support, interviews or usage data.

### Existing baseline

SQLite BackgroundJobs and RDBMS DbJobs already share Admin contracts. Leases, node registration, health checks, queue controls and telemetry already exist; capability reporting is also present. This proposal improves the completeness and verifiability of those surfaces.

### Proposed increment

Publish a capability matrix covering ownership, failover, transaction enqueue, scheduling, queue limits, storage and recovery. Tie each guarantee to an executable conformance scenario. Extend existing startup/Admin diagnostics only where the matrix reveals missing explanations or unsupported configurations.

### Validation before acceptance

Reuse BackgroundJobsTests and DbJobsTests; add scenarios only for uncovered guarantees. Validate RDBMS-specific guarantees on the actual supported dialects, not just SQLite simulation. Explain local ownership clearly in the UI.

### Constraints and risks

Do not imply exactly-once side effects, distributed SQLite ownership or identical guarantees across providers. Avoid maintaining a second capability model independent of existing metadata.

### Evidence and ownership

- [ServiceStack.Jobs/BackgroundJobs.cs](../BackgroundJobs.cs)
- [ServiceStack.Jobs/AdminJobServices.cs](../AdminJobServices.cs)
- [ServiceStack.Server/DbJobs.cs](../../ServiceStack.Server/DbJobs.cs)
- [ServiceStack.Server/DbJobsAdminServices.cs](../../ServiceStack.Server/DbJobsAdminServices.cs)
- [ServiceStack/Jobs/JobsHealthCheck.cs](../../ServiceStack/Jobs/JobsHealthCheck.cs)
- Participating projects: [ServiceStack.Server](../../ServiceStack.Server/FEATURES.md), [ServiceStack](../../ServiceStack/FEATURES.md).

## Proposals owned by another project

- [SERVER-001: Durable outbound webhook delivery records and operations](../../ServiceStack.Server/plan/FEATURE-PROPOSALS.md) — participate without duplicating the canonical proposal.
- [CORE-003: Correlated operational view across requests and background work](../../ServiceStack/plan/FEATURE-PROPOSALS.md) — participate without duplicating the canonical proposal.

## Decision handling

When explicitly accepted, move the full proposal under the same stable ID into [FEATURE-IMPROVEMENTS.md](FEATURE-IMPROVEMENTS.md), record the decision/date and agreed scope, and leave a short link here. Rejected or deferred proposals remain here with their disposition. Prioritization is not acceptance.
