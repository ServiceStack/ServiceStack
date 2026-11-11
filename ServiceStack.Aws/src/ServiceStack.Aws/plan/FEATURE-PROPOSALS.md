# ServiceStack.Aws feature proposals

Reviewed 2026-11-11 at source HEAD `101038b4ba5a65650e02491e98c7c8a4e1d46f5f`. **Proposed only; no entry authorizes implementation.** See the [existing inventory and verification](../FEATURES.md), [acceptance ledger](FEATURE-IMPROVEMENTS.md) and [suite workflow](../../../../ServiceStack/src/FEATURES.md#planning-workflow).

Investigation priorities reflect concrete implementation gaps and fit with the existing adapters. Customer demand, prevalence, throughput gains and commercial value were not measured. Effort is relative scope, not a release schedule. Fake-client observations below do not establish a production incident, AWS service parity or a distributed delivery guarantee.

## Existing capabilities checked first

| Existing implementation | Proposal boundary |
| --- | --- |
| PocoDynamo sync/async CRUD, IAsyncEnumerable query/scan, index/projection expressions, typed conditional updates and raw IAmazonDynamoDB access | Async support, pagination and conditions are not entirely missing. Typed transaction/page-outcome facades are different increments. |
| Exception retries, backoff, batch limits and resubmission of unprocessed keys/items | Improve cancellation, one-operation budgets and partial outcomes; do not propose retry support from scratch. |
| Logical DynamoDB cache expiry, expiry sweeps, counters and Add/Replace APIs | Existence checks are not atomic conditions; native TTL schema/epoch support is not established by ExpiryDate or GetTimeToLive. |
| S3 metadata/ETags, conditional refresh, range streaming, sync writes/deletes and modern-target async enumeration | Focus on complete/safe bulk traversal and lifetime controls; do not propose all S3 streaming, ETags or async enumeration as new. |
| R2 upload overrides, content buffering and rethrown upload failures | Preserve compatibility and failure propagation; do not reintroduce a supposedly “successful” swallowed upload. |
| SQS batching, visibility operations, long polling, redrive policies, hooks and in-memory tests | These are an existing foundation. Confirmed outcomes, bounded pending work, FIFO semantics and automatic visibility renewal are separate concerns. |
| Transcribe vocabulary/job submission/polling, tokens and output parsing | Improve workflow boundaries and terminal handling; this is not a new speech-provider proposal. |

## Recommended investigation order

| ID | Customer outcome | Order / effort | Confidence and limits |
| --- | --- | --- | --- |
| [AWS-001](#aws-001-complete-prefix-safe-cancellable-s3-bulk-operations) | Enumerate/delete the intended complete file set | Investigate first; medium, split correctness from advanced transfers | Single-page sync listing and sibling-prefix deletion reproduced with SDK fakes |
| [AWS-002](#aws-002-atomic-dynamodb-cache-conditions-and-expiry) | Make cache Add/Replace and expired-entry cleanup race-safe | Investigate first; medium, TTL opt-in later | Concurrent AddAsync success/unconditional writes reproduced; other races need targeted tests |
| [AWS-003](#aws-003-one-cancellable-retry-budget-for-pocodynamo-operations) | Stop cancelled/throttled operations predictably | Investigate first; medium | Cancellation converted to TimeoutException reproduced; sustained throttling/batch progress need service validation |
| [AWS-004](#aws-004-explicit-sqs-batch-outcomes-and-bounded-publishing) | Know what was accepted, failed or remains queued | Next; medium–large, confirmations before scheduling/heartbeat extensions | Callback/dequeue behavior reproduced; production acceptance/duplicate outcomes remain unknown |
| [AWS-005](#aws-005-explicit-sqs-fifo-queue-and-message-support) | Use deliberate group ordering and stable deduplication IDs | Next if ordering demand is confirmed; medium–large | Group/deduplication loss through the buffer reproduced; no live FIFO matrix executed |
| [AWS-006](#aws-006-typed-dynamodb-conditionals-and-transactions) | Compose atomic model changes with explicit conflict outcomes | Strong additive capability; medium–large, conditionals first | SDK escape hatch and typed Update conditions already exist; typed transaction facade not located |
| [AWS-007](#aws-007-typed-query-pages-and-read-cost-controls) | Resume queries and understand their read cost without scanning invisibly | Validate workloads first; medium | Existing paging/raw request controls confirmed; a new typed page report is an ergonomic addition |
| [AWS-008](#aws-008-bounded-transcribe-jobs-and-correct-result-retrieval) | Bound speech workflows and fetch the correct output reliably | Next for speech users; medium | Polling/exception/output paths inspected; no paid transcription or vocabulary calls executed |

Start with data-loss/concurrency/cancellation concerns and deterministic tests. Typed transactions and FIFO are the strongest new service-facing capabilities after their prerequisites. Rank is not acceptance; this does not commit every candidate to the next release.

## AWS-001: Complete prefix-safe cancellable S3 bulk operations

- **Status:** Proposed. **Owner:** ServiceStack.Aws. **Participants:** Common/core only for optional VFS capability contracts; core FilesUploadFeature consumers validate integration.
- **Value:** Avoid incomplete file discovery/cleanup and deletion of similarly named sibling paths. Improve the existing storage adapter rather than adding another provider.
- **Effort/confidence:** Medium. Two behaviors are reproduced with fake SDK responses; real S3/R2 limits, access errors and versioned-bucket behavior remain unverified.

### Existing behavior and evidence

[S3VirtualFiles](../IO/S3VirtualFiles.cs) synchronous EnumerateFiles issues one ListObjects request without a continuation loop. Its modern-target async enumeration already follows ListObjectsV2 tokens. DeleteFolder passes the sanitized directory string directly as Prefix. A probe returned only the first scripted page synchronously but both asynchronously; another DeleteFolder("reports") probe selected `reports/a.txt` and sibling `reports-archive/b.txt`.

DeleteFiles already batches requests and upload errors propagate. However async folder/bucket cleanup materializes keys and invokes synchronous deletion without forwarding the caller token through that phase. S3 [listing semantics](https://docs.aws.amazon.com/AmazonS3/latest/API/API_ListObjectsV2.html) distinguish key prefixes from directory boundaries and expose continuation state. Path canonicalization is not an authorization boundary or a substitute for delimiter-aware matching.

### Bounded proposal

1. Make complete sync/async enumeration agree across all pages, with progress/continuation validation. Define folder semantics explicitly: exact marker object versus descendants under `path/`; sibling prefixes must not match. Keep whole-bucket clearing explicit.
2. Stream keys into bounded delete batches, propagate cancellation through listing/deletion and report partial progress/failure without claiming rollback. Preserve SDK per-object failure information, including exceptions carrying partial results.
3. Audit response/stream ownership in full/range reads, refresh and R2 buffering; retain the existing disposed range response and upload rethrow protections. Define target-specific async capability without adding required IVirtualFiles members.

### Acceptance criteria if selected

- More than one listing page, empty pages, continuation failures, `a`/`ab`/`a/` keys, root/empty paths and normalized separators produce deliberate results.
- Cancellation between and during pages/batches stops further dispatch; memory stays bounded by the chosen batch/window, not total bucket size.
- Fake tests cover partial delete errors, permissions, stream disposal and cancellation. Separate opt-in S3/R2 contract fixtures establish actual provider behavior before parity claims.
- Validate the older FileStorage/S3FileStorageProvider separately; a fix to S3VirtualFiles does not automatically fix that abstraction.

### Alternatives and compatibility

Callers can use raw SDK paginators and explicit `folder/` prefixes today. Keep IVirtualFiles defaults compatible where safe; document changes that correct over-broad destructive behavior. Multipart transfers, presigned URLs, lifecycle policies and version-history deletion require separate scope decisions.

## AWS-002: Atomic DynamoDB cache conditions and expiry

- **Status:** Proposed. **Owner:** ServiceStack.Aws's DynamoDB cache adapter. **Participant:** shared cache owners only if an optional new result contract is justified.
- **Value:** Make distributed Add/Replace reflect the state at the write, and prevent cleanup of an expired snapshot from deleting a fresh replacement.
- **Effort/confidence:** Medium. Two coordinated AddAsync calls were reproduced returning true with unconditional PutItem; expired-read/sweep races are source-backed follow-up cases, not yet reproduced against DynamoDB.

### Existing behavior and evidence

[DynamoDbCacheClient](../DynamoDb/DynamoDbCacheClient.cs) and [async cache](../DynamoDb/DynamoDbCacheClient.Async.cs) read an entry before Add/Replace and call unconditional CacheSet. Reads detect ExpiryDate and issue deletion; sweeps scan then delete IDs. The numeric zero/false existence fix already exists and must be preserved. FlushAll deletes/recreates the cache table.

Logical expiry is already supported. Native DynamoDB TTL was not located in this provider. AWS documents [delayed TTL cleanup](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/TTL.html) and [handling expired records/conditional writes](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/ttl-expired-items.html); background TTL removal cannot define immediate cache validity.

### Bounded proposal

- Encode absent/present-and-unexpired conditions on the write itself; define expired-entry semantics and update value/deadline together. Return a condition miss distinctly from authentication, throttling or uncertain transport failures.
- Make expired-read and sweep removal conditional on the observed expiry/version, or safely stop deleting on reads. A fresh concurrent value must survive. Preserve counter representation and existing JSON/type conventions.
- As a separately accepted phase, add an opt-in numeric Unix-seconds TTL attribute and schema enablement/migration guidance. Continue application-side expiry checks; do not silently reinterpret existing DateTime data or require broader IAM on every operation.
- Document destructive FlushAll and offer a separately scoped namespace/key-based strategy if demand justifies it. Do not change an entire shared table's retention policy implicitly.

### Acceptance criteria if selected

- Concurrent Add yields one winner; Replace cannot recreate an absent/expired entry under the agreed semantics. Exercise no-expiry/absolute/relative TTL, failed conditions, zero/false/null payloads and counters.
- A stale expired snapshot/sweep cannot delete a freshly replaced row; cancellation and uncertain writes preserve truthful outcomes.
- SDK fakes and DynamoDB Local cover expression/concurrency behavior; opt-in AWS tests cover TTL configuration, IAM and delayed cleanup. Table names/schema migration and old-client coexistence are explicit.

### Alternatives and compatibility

Applications can already use SDK conditional writes or a different cache provider. A Microsoft IDistributedCache adapter is a separate feature, not implicitly accepted because one exists in ServiceStack.Redis. Avoid changing shared cache interfaces or promising transactional behavior from existing BatchWrite convenience calls.

## AWS-003: One cancellable retry budget for PocoDynamo operations

- **Status:** Proposed. **Owner:** ServiceStack.Aws. **Participants:** none required for a provider-local retry/options phase.
- **Value:** Bound resource use and shutdown latency during cancellation, throttling and persistent unprocessed batches.
- **Effort/confidence:** Medium. A cancelled callback was retried by ExecAsync and surfaced as TimeoutException in the probe. End-to-end throttling and lost-response behavior still require validation.

### Existing behavior and evidence

[PocoDynamo.Support](../DynamoDb/PocoDynamo.Support.cs) already has exception retry classification, backoff, ExceptionFilter and MaxRetryOnExceptionTimeout. ExecAsync catches cancellation with other exceptions and delays without the caller token. Batch get/write loops resubmit unprocessed items, but a successful response with persistent unprocessed work can restart fresh SDK-call retry windows repeatedly. Many [async methods](../DynamoDb/PocoDynamo.Async.cs) forward tokens to SDK calls; this does not make the enclosing retry loop cancellation-aware.

### Bounded proposal

- Add a backward-compatible operation context/options path with a monotonic overall budget, cancellation-aware backoff and terminal caller cancellation. Preserve caller token identity.
- Apply the same budget to exception retries, unprocessed-key/item loops and table readiness waits. Distinguish service refusal, permanent validation/auth errors, transient throttling and unknown write outcomes.
- Define SDK-retry versus wrapper-retry responsibilities to avoid multiplying attempts unexpectedly. Preserve useful ExceptionFilter hooks without exposing secret request data.
- Add structured partial progress for optional batch APIs while keeping existing void/list contracts compatible. Treat nullable SDK v4 response collections deliberately rather than assuming every successful response initializes them.

### Acceptance criteria if selected

- Pre-cancelled, mid-request and during-backoff operations terminate promptly with the original token; timeout is distinct from caller cancellation.
- Persistent unprocessed responses cannot run indefinitely; only remaining entries are resubmitted, with bounded delay and explicit partial/unknown results.
- Tests cover empty/partial SDK responses, throttling, permanent errors, table polling and cancellation across supported targets. Unknown non-idempotent writes are not blindly classified as safe to replay.

### Alternatives and compatibility

Applications can configure SDK retry behavior and call raw APIs. Keep those controls and existing exception filters. Prefer optional overloads/adapters to adding required IPocoDynamo members that break third-party implementations; do not turn a deadline into a promise of rollback.

## AWS-004: Explicit SQS batch outcomes and bounded publishing

- **Status:** Proposed. **Owner:** ServiceStack.Aws. **Participants:** core messaging owners only for optional confirmation capabilities; app handlers own business idempotency.
- **Value:** Distinguish enqueued, accepted, failed and unknown publishes and bound producer backlog under service failures.
- **Effort/confidence:** Medium–large. Callback/dequeue mapping was reproduced with a partial fake response; no live delivery-loss rate or AWS production outcome was measured.

### Existing behavior and evidence

[SqsMqBuffer](../Sqs/SqsMqBuffer.cs) already batches sends/deletes/visibility changes and exposes ErrorHandler. Entries leave the pending queue before dispatch. A fake partial failure produced a true dispatch boolean, one error callback and an empty pending buffer. [SqsMqMessageProducer](../Sqs/SqsMqMessageProducer.cs) offers OnPublishedCallback after its buffer send; that is not necessarily server confirmation. AWS [SendMessageBatch](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_SendMessageBatch.html) can contain per-entry failures even when the HTTP call succeeds.

### Bounded proposal

1. Introduce optional per-entry outcomes/confirmation with stable request correlation through buffer, flush, SDK partial response and exception paths. Preserve diagnostic callbacks while making accepted versus merely queued status explicit.
2. Define bounded retry/dead-letter reporting for retryable failed entries; permanent SenderFault entries must not loop. Unknown outcomes require application idempotency and must not be presented as rejected safely.
3. Add opt-in pending-message/byte limits, wait/fail-fast policy and cancellation-aware async publishing/drain. Existing flush thresholds are not capacity limits. Preserve current nonbuffered and synchronous APIs.

### Acceptance criteria if selected

- Mixed success/permanent/transient/unknown entries retain their original identities and receive exactly one terminal library outcome under the chosen API; this is not exactly-once business processing.
- Failed entries cannot disappear with only an unobserved callback when confirmation mode is selected. Buffer capacity, shutdown/drain and concurrent producers are deterministic under failure.
- SDK fakes cover partial failures and failed sends/deletes/visibility changes. Dedicated opt-in AWS tests validate receipt handles, redrive, throttling and lost responses.
- Long-running-handler visibility renewal can be a later explicit phase: ChangeVisibility already exists, but automatic heartbeat lifetime/maximums and receipt ownership need their own agreement.

### Alternatives and compatibility

Callers can use nonbuffered mode/raw SDK and existing filters today. Do not silently redefine IMessageProducer.Publish or OnPublishedCallback as a durable confirmation. SQS remains an at-least-once workflow requiring application-level idempotency; generic idempotency infrastructure should link to the core owner's proposal rather than be duplicated here.

## AWS-005: Explicit SQS FIFO queue and message support

- **Status:** Proposed. **Owner:** ServiceStack.Aws. **Participants:** none for provider-specific queue/message options; core messaging only if an optional generic contract is needed.
- **Value:** Let applications deliberately choose per-group ordering and stable deduplication identities while keeping standard queues unchanged.
- **Effort/confidence:** Medium–large; preserve fields first, queue lifecycle/worker ordering second. Buffered field loss was reproduced; complete FIFO behavior was not service-tested.

### Existing behavior and evidence

The underlying SDK and [send filters](../Sqs/SqsMqMessageProducer.cs) already expose message request fields. However [EntriesToSend](../Sqs/SqsMqBuffer.cs) copies body/delay/attributes without group/deduplication fields. A fake captured those supplied IDs as absent from the outgoing batch. [Queue-name normalization](../Sqs/SqsQueueName.cs), [definitions](../Sqs/SqsQueueDefinition.cs) and [queue manager](../Sqs/SqsQueueManager.cs) were not found to provide a complete named FIFO policy; normal dot replacement also needs review for the `.fifo` suffix.

### Bounded proposal

- Add explicit queue kind/options and preserve required suffix/attributes for FIFO main/priority/dead-letter/temp policies. Never rename or recreate existing standard queues automatically.
- Carry caller-selected MessageGroupId and stable MessageDeduplicationId through both buffered/nonbuffered sends, retries and replies. Avoid generating a fresh deduplication ID on every retry.
- Define worker scheduling/prefetch/ack behavior within groups before claiming ordering. Multiple workers and batches must not execute side effects out of order within a group.
- Use [AWS FIFO group/deduplication semantics](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_SendMessageBatch.html) as a service contract; deduplication is not a guarantee that a handler's side effects execute once.

### Acceptance criteria if selected

- Request filters retain metadata in every path; standard queue requests stay compatible and invalid option combinations fail clearly.
- Test mixed groups, stable retry IDs, batch partial failure, queue attribute mismatch, redrive and shutdown with in-flight groups.
- A real FIFO contract fixture demonstrates observed ordering and duplicate handling. Keep fake coverage labeled simulation; scope live resource cleanup explicitly before running it.

### Alternatives and compatibility

Raw AWS SDK and application-owned FIFO consumers remain viable. Field preservation is smaller than complete FIFO MQ support and should be documented separately if selected first. Do not alter existing queue names or promise cross-group/global order.

## AWS-006: Typed DynamoDB conditionals and transactions

- **Status:** Proposed. **Owner:** ServiceStack.Aws/PocoDynamo. **Participants:** auth/cache consumers only for separately accepted adoption; no automatic provider migrations.
- **Value:** Express compare-and-set and atomic multi-record updates with model converters and deliberate conflict/idempotency outcomes.
- **Effort/confidence:** Medium–large. SDK support and typed Update conditions exist; a typed transaction facade was not located in the inspected IPocoDynamo/client implementation.

### Existing behavior and evidence

[UpdateExpression](../DynamoDb/UpdateExpression.cs) already provides Condition/AddConditionExpression and [UpdateItemAsync](../DynamoDb/PocoDynamo.Async.cs) returns false for a conditional miss. Raw IAmazonDynamoDB is exposed by [IPocoDynamo](../DynamoDb/IPocoDynamo.cs), so applications can already use transactions directly. PutItem/batch conveniences are not multi-item transactions. [Auth repository uniqueness](../DynamoDb/DynamoDbAuthRepository.cs) currently checks before writing; a transaction building block alone would not retroactively fix that race.

### Bounded proposal

1. Add typed conditional Put/Delete helpers/results reusing metadata/converters and explicit field aliases; preserve raw request escape hatches.
2. Design optional TransactWrite/TransactGet builders with condition checks, client request tokens and per-action conflict/cancellation details. Follow the [AWS transaction contract](https://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_TransactWriteItems.html), validating current service limits rather than copying historical numbers into interfaces.
3. Keep transaction retry/idempotency policy distinct from ordinary writes and SDK retries. Schema creation, sequence allocation and request conversion must not cause hidden side effects before an atomic operation.

### Acceptance criteria if selected

- Validate all actions before dispatch; all-or-nothing behavior and conflict results are exercised with fake responses, DynamoDB Local and an opt-in AWS fixture.
- Preserve keys, aliases, numbers, sets, binary values and custom converters. Stable idempotency tokens are retained across a logical retry; changed payloads do not reuse a token silently.
- Define unsupported combinations and duplicate-item actions locally, preserve unknown outcomes and support caller cancellation under AWS-003's budget model.
- Existing IPocoDynamo implementations remain compatible via optional facades/capabilities or an explicitly accepted migration.

### Alternatives and compatibility

Raw SDK transactions are the immediate alternative; examples may be sufficient before public builders are justified. Auth uniqueness reservation rows, cache conditions and new application transaction workflows need separate acceptance and migration plans. Do not label BatchWrite retries atomic or introduce a distributed transaction coordinator.

## AWS-007: Typed query pages and read-cost controls

- **Status:** Proposed, demand/workload validation first. **Owner:** ServiceStack.Aws. **Participants:** core ServiceStack for optional AutoQuery Data integration, applications for external cursor security policy.
- **Value:** Resume bounded queries, expose what was scanned/consumed and avoid accidental expensive full scans behind convenient abstractions.
- **Effort/confidence:** Medium. Existing pagination and raw controls were verified; convenience and cost benefits are hypotheses until benchmarked with representative access patterns.

### Existing behavior and evidence

[QueryExpression/ScanExpression](../DynamoDb/QueryExpression.cs) inherit SDK request controls, including cursor/capacity options. [PocoDynamo loops](../DynamoDb/PocoDynamo.cs) and [async enumeration](../DynamoDb/PocoDynamo.Async.cs) already follow LastEvaluatedKey. Converter overloads/raw SDK can capture response metadata. [DynamoDbQueryDataSource](../DynamoDb/DynamoDbQueryDataSource.cs) already supports disabling scans; offset/count behavior may read more than the returned rows. Therefore “add pagination/scan controls” would overstate the missing functionality.

### Bounded proposal

- Add optional typed single-page results with items, continuation key, returned/scanned counts and requested consumed-capacity metadata; retain streaming/list APIs as convenience layers.
- Offer explicit maximum pages/evaluated items/time/capacity policy where service metadata permits enforcement, with partial-progress results. State whether a budget is checked before dispatch or after a billed page returns.
- Provide a cursor serialization recipe bound to table/index/query shape. Public cursor integrity/confidentiality is application policy; do not emit arbitrary key values in logs or treat a cursor as authorization.

### Acceptance criteria if selected

- Empty filtered pages with continuation state are resumable; limits distinguish evaluated versus returned items and capacity metadata absence is not reported as zero cost.
- Snapshot caller requests or document mutation; interleaved enumerations cannot corrupt continuation/query state. Test projection/index/consistency/custom-converter combinations.
- Benchmark query versus scan behavior and AutoQuery count/offset paths before promising performance gains. Test cancellation and budgets with AWS-003.

### Alternatives and compatibility

Raw SDK responses, existing converter callbacks and allowScans=false already solve many advanced cases. Prefer docs/adapters if they suffice. A page envelope does not create a new index, cheap random-access offset paging, point-in-time snapshot or server keyset query engine.

## AWS-008: Bounded Transcribe jobs and correct result retrieval

- **Status:** Proposed. **Owner:** ServiceStack.Aws's AwsSpeechToText. **Participants:** shared speech contract/ServiceStack.AI owners only for optional cross-provider job capabilities; VFS owners for async retrieval support.
- **Value:** Make long-running jobs, vocabulary initialization and transcript retrieval fail/stop predictably and honor distinct input/output configuration.
- **Effort/confidence:** Medium. Existing polling, exception and result paths were inspected; no real Transcribe job, IAM policy or vocabulary operation was executed.

### Existing behavior and evidence

[AwsSpeechToText](../AI/AwsSpeechToText.cs) already implements vocabulary recreation, job submission/status polling, request/name/key/media filters, language selection, output bucket selection and TranscriptResult parsing. Tokens reach SDK polling and delays. However the loops have no intrinsic overall deadline; vocabulary deletion catches all exceptions; the S3-VFS output branch uses synchronous AssertFile/OpenRead and a copy without the caller token. OutputBucketName can differ from Bucket, while the inspected result-path extraction uses Bucket. These are provider-specific gaps, not evidence that all speech APIs lack cancellation.

### Bounded proposal

- Add optional overall timeout/polling policy, terminal status/error classification and cancellation-aware output retrieval. Only suppress the specific missing-vocabulary case; preserve permission/configuration/cancellation failures.
- Resolve output bucket/key from the actual job result/configuration with deliberate URI parsing and resource ownership. Support distinct input/output buckets without substring-based path assumptions.
- Make SDK interactions injectable through an interface/adapter while preserving the existing constructor, enabling deterministic job/vocabulary/storage tests.
- An optional start/status/resume handle is a later scope if demand is confirmed. Cancelling a local await must not be represented as undoing a submitted remote job or guaranteeing zero charges.

### Acceptance criteria if selected

- Fake tests cover pending/ready/completed/failed states, cancellation in every stage, deadline exhaustion, permission errors and malformed/missing transcript data.
- Distinct output bucket/key, encoded paths, configured/custom job names, response-stream disposal and transcript/confidence/raw response behavior remain compatible.
- Any opt-in AWS contract fixture defines resource/charge boundaries and cleanup explicitly. Cross-provider job APIs require a separate shared-contract decision.

### Alternatives and compatibility

Applications can use the Transcribe SDK directly or wrap ISpeechToText with their own lifecycle policy. Preserve that route and avoid changing required generic speech methods for an AWS-only workflow. Streaming transcription, EventBridge orchestration and automatic remote-job cleanup are separate features.

## Deferred and externally owned opportunities

- Auth username/email uniqueness could adopt conditional reservation rows/transactions after AWS-006; existing indexes and prechecks are not proof of serialized registration. Design index/data migration and compatibility before proposing a rewrite.
- Native TTL/on-demand/schema settings should reuse existing CreateTableFilter/SDK controls before adding a broad provisioning framework. Infrastructure drift, backups, KMS and IAM are wider deployment concerns.
- S3 multipart/presigned/versioned-object features, DynamoDB Streams consumers, SES/SSM/Secrets Manager providers, telemetry and IDistributedCache need separate customer/scope evidence. Their absence in this catalog is not automatic roadmap acceptance.
- SDK IAM/credential-chain support already exists below injected clients. Do not invent a second credential store or place secrets in new diagnostics/planning examples.

## Decision handling

After explicit acceptance, move the complete bounded proposal to [FEATURE-IMPROVEMENTS.md](FEATURE-IMPROVEMENTS.md) under its original ID. Record the request/date, customer outcome, owner, migration, acceptance criteria, actual validation and status; leave a link here. Participants link to the canonical owner instead of duplicating proposals. Keep implementation, validated behavior and released availability separate.
