# ServiceStack.Aws accepted feature improvements

Reviewed 2026-11-11. **No product proposals from this analysis have been accepted.** The maintainer requested the same review/catalog/planning exercise as ServiceStack.Blazor. That authorizes documentation, not implementation of AWS product recommendations.

Existing adapters and historical security fixes are mapped in [FEATURES.md](../FEATURES.md); they are not retroactively classified as newly accepted roadmap items. Reproduced concerns and candidate increments belong in [FEATURE-PROPOSALS.md](FEATURE-PROPOSALS.md).

## Acceptance record

After explicit acceptance of a bounded scope, move the entire proposal here under the same `AWS-###` ID and record:

- Accepting maintainer, date and decision/request reference.
- Customer outcome, agreed scope/exclusions, canonical owner and participating projects.
- Source baseline, implementation plan, compatibility/migration choices and acceptance criteria.
- Status: Accepted → In progress → Implemented → Released, recorded separately.
- Actual test evidence: SDK versions, target runtimes, fakes/emulators versus real services, regions/providers and IAM/resource scope; never equate a fake with AWS parity.
- Documentation/example changes and implementation revision; published version/tag/release-note evidence only when confirmed.

Leave a decision link in proposals and update the capability inventory when behavior changes. Cross-project participants link to the canonical owner. Deferred/rejected ideas retain their reason in proposals. See [catalog maintenance](../FEATURES.md#catalog-maintenance) and the [suite workflow](../../../../ServiceStack/src/FEATURES.md#planning-workflow).
