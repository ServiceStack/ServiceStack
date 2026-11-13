# ServiceStack.Server accepted feature improvements

No proposals from this first-pass analysis have been accepted. Existing implementation and historical plans are not retroactively classified as newly accepted work.

See [proposals](FEATURE-PROPOSALS.md), [existing capabilities](../FEATURES.md) and [acceptance workflow](../../FEATURES.md#planning-workflow).

For each explicitly accepted proposal, move its full record here with its stable ID and add:

- Decision reference, accepting maintainer and date.
- Agreed customer outcome, scope and exclusions.
- Owning project and participating projects.
- Implementation plan, compatibility/migration decisions and acceptance criteria.
- Status: Accepted, In progress, Implemented or Released; do not equate these states.
- Validation evidence, documentation changes and implementation revision.
- Release version and release-note link when confirmed.

After implementation, update FEATURES.md to describe the actual supported behavior and constraints. Keep this decision/implementation record for history.

## SERVER-829: safe archive retention

- Decision: user explicitly requested implementation on 2026-11-13 after investigation of [issue 829](https://github.com/ServiceStack/Issues/issues/829) and recommendation of provider-specific cleanup. This is a correction to the existing retention contract, accepted directly in this chat; no earlier proposal is being reclassified.
- Owner: ServiceStack.Server. Participant: ServiceStack.Jobs for the shared whole-month cutoff and configuration documentation.
- Scope: separate archive cleanup from schema teardown; delete expired completed/failed rows in shared tables; remove only whole expired PostgreSQL child partitions; discover failed-only archives; invalidate the local partition cache; preserve overlapping monthly archives in the shared SQLite path. Null/non-positive retention disables cleanup.
- Acceptance criteria: operational schema and rows survive; recent and boundary history survives; expired completed and failed history is removed; failed-only history is handled; cleanup is repeatable; PostgreSQL partitions can be recreated after cleanup. No queue, schedule or summary retention changes.
- Status: implemented in the working tree, based on `77ee02d63860ce13a1c5187acb9ba77633dfa77f`; not released.
- Validation: new `DbJobsRetentionTests` fixture, including isolated SQL Server, MariaDB, PostgreSQL and SQLite cases. Server and Jobs projects built successfully for net8.0 and net10.0 (existing warnings, no errors), using existing restored dependencies and `BuildProjectReferences=false`. An isolated net10.0 runner passed all 10 cases, including SQL Server 2025 with Microsoft.Data.SqlClient, MariaDB and PostgreSQL 17. The repository net10.0 jobs run passed all 10 retention cases and 147/148 jobs tests overall; `Every_failed_attempt_is_recorded` initially observed only the first attempt, then passed alone. Source inspection shows the existing summary status is published before the final attempt insert, allowing that assertion to race. This unrelated timing issue was not changed. The repository test project uses its existing System.Data.SqlClient provider, so both SQL Server client variants were exercised. MariaDB verifies the MySQL adapter, not Oracle MySQL 8 runtime parity. Whole-month SQLite boundary tests exercise the shared cleanup method with captured archive deletion; they do not delete actual archive files.
- Release: first published fixed package version remains unconfirmed.
