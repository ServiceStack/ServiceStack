# ServiceStack.OpenApi.Microsoft feature proposals

Status: proposed only; no entry here authorizes implementation. Reviewed 2026-10-31.

See [existing capabilities](../FEATURES.md), [suite priorities](../../plan/FEATURE-PROPOSALS.md), and [acceptance workflow](../../FEATURES.md#planning-workflow).

## OPENAPI-001: Shared OpenAPI integration conformance fixtures

- Status: Proposed; acceptance not requested or granted.
- Type: Compatibility and developer experience improvement.
- Initial disposition: Investigate next.
- Potential value: High potential across three modern OpenAPI adapters; reduces duplicated fixes and package-selection confusion.
- Relative effort: Medium; not a delivery estimate.
- Evidence confidence: High that adapter implementations overlap; individual output defects are not established by this inventory.
- Customer evidence: Not yet validated against support, interviews or usage data.

### Existing baseline

The Microsoft, newer Swashbuckle and older ASP.NET Core adapters each generate ServiceStack schemas and operations. They intentionally use different dependency lines. Swagger 2 is a separate legacy surface.

### Proposed increment

Build common DTO/route fixtures and normalize document output for comparison across adapters. Cover naming, nullability, generics, exclusion attributes, binary uploads, auth requirements and empty responses. Publish an exact package/TFM selection table. Extract shared policy only after intentional adapter differences are documented.

### Validation before acceptance

Validate generated documents using tooling compatible with each declared spec version. Exercise representative client generation and protected-schema visibility. Run each adapter in an isolated host/dependency graph.

### Constraints and risks

Do not force Swagger 2 and OpenAPI 3 documents to be identical or co-load incompatible packages. Microsoft.OpenApi model versions and serializer behavior limit code sharing.

### Evidence and ownership

- [ServiceStack.OpenApi.Microsoft/OpenApiMetadata.cs](../OpenApiMetadata.cs)
- [ServiceStack.OpenApi.Swashbuckle/OpenApiMetadata.cs](../../ServiceStack.OpenApi.Swashbuckle/OpenApiMetadata.cs)
- [ServiceStack.AspNetCore.OpenApi/OpenApiMetadata.cs](../../ServiceStack.AspNetCore.OpenApi/OpenApiMetadata.cs)
- Participating projects: [ServiceStack.OpenApi.Swashbuckle](../../ServiceStack.OpenApi.Swashbuckle/FEATURES.md), [ServiceStack.AspNetCore.OpenApi](../../ServiceStack.AspNetCore.OpenApi/FEATURES.md), [ServiceStack.Api.OpenApi](../../ServiceStack.Api.OpenApi/FEATURES.md).

## Proposals owned by another project

- [CORE-001: API contract snapshots and compatibility reports](../../ServiceStack/plan/FEATURE-PROPOSALS.md) — participate without duplicating the canonical proposal.

## Decision handling

When explicitly accepted, move the full proposal under the same stable ID into [FEATURE-IMPROVEMENTS.md](FEATURE-IMPROVEMENTS.md), record the decision/date and agreed scope, and leave a short link here. Rejected or deferred proposals remain here with their disposition. Prioritization is not acceptance.
