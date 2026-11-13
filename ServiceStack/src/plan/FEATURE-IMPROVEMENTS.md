# Accepted feature improvements: suite index

Acceptance requires an explicit scope decision. Approval to catalog existing behavior and suggest ideas does not itself authorize implementation.

Accepted proposal records belong in each owning project's `plan/FEATURE-IMPROVEMENTS.md`; this file will link those records after explicit acceptance. Keep status, ownership and decision history in the canonical project record.

See the [project index](../FEATURES.md#project-index), [unaccepted proposals](FEATURE-PROPOSALS.md) and [acceptance workflow](../FEATURES.md#planning-workflow).

## Cross-project accepted work

- [REDIS-001: named GETEX/GETDEL/UNLINK commands and pattern cleanup](../../../ServiceStack.Redis/src/ServiceStack.Redis/plan/FEATURE-IMPROVEMENTS.md#command-group-decision-2026-11-10) — ServiceStack.Redis owns the implementation and decision record; ServiceStack.Interfaces supplies optional command contracts and GETEX options. Implemented in the working tree; release availability unconfirmed.

- [REDIS-001: hash-field expiration](../../../ServiceStack.Redis/src/ServiceStack.Redis/plan/FEATURE-IMPROVEMENTS.md#hash-field-expiration-decision-2026-11-11) — ServiceStack.Redis owns native/rich sync/async APIs, parsing and validation; ServiceStack.Interfaces supplies optional binary command contracts and a single-choice condition enum. Implemented in the working tree; release availability unconfirmed.

- [REDIS-004: Streams and consumer groups](../../../ServiceStack.Redis/src/ServiceStack.Redis/plan/FEATURE-IMPROVEMENTS.md#redis-004-typed-streams-and-consumer-group-operations) — ServiceStack.Redis owns implementation and decisions, including the accepted primitive pipelines and bounded bulk-append phase; ServiceStack.Interfaces supplies additive stream contracts/models and nil-reply metadata. Implemented in the working tree; release availability unconfirmed.

- [SERVER-829: safe archive retention](../ServiceStack.Server/plan/FEATURE-IMPROVEMENTS.md#server-829-safe-archive-retention) — ServiceStack.Server owns provider cleanup and regression coverage; ServiceStack.Jobs participates through shared monthly cutoff semantics. Accepted by the user on 2026-11-13; implemented in the working tree, release availability unconfirmed.
