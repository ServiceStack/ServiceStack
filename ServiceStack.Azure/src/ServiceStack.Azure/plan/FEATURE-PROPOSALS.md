# ServiceStack.Azure feature proposals

Reviewed 2026-11-11 at source HEAD `101038b4ba5a65650e02491e98c7c8a4e1d46f5f`. **Proposed only; no entry authorizes implementation.** See the [existing inventory and validation](../FEATURES.md), [acceptance ledger](FEATURE-IMPROVEMENTS.md) and [suite workflow](../../../../ServiceStack/src/FEATURES.md#planning-workflow).

These priorities reflect inspected code, deterministic local observations and fit with existing adapters. Customer demand, incident prevalence, throughput gains and commercial value have not been measured. Effort is relative design scope, not a release schedule. SDK fakes do not establish real Azure behavior or released availability.

## Existing capabilities checked first

| Existing capability | Proposal boundary |
| --- | --- |
| Azure Table conditional AddEntity, ETag counter retries, logical expiration, async cache APIs and regex timeouts | Improve expired-row semantics, conditional cleanup and bounded enumeration; do not propose atomic insertion, async caching or regex protection from scratch. |
| Block and append VFS, SDK-paged listings, trailing-slash folder deletion, path canonicalization and injected BlobContainerClient | Improve directory conformance and native async/append behavior. Blob pagination, append storage and SDK credential injection are not entirely absent. |
| Service Bus queue processors, PeekLock, explicit completion, SDK lock-renewal defaults, retries/DLQ routing and publish filters | Correct adapter settlement/lifetime boundaries and expose deliberate configuration. Automatic lock renewal is not absent from the underlying processor. |
| Stable MessageId mapping and a publish filter over ServiceBusMessage | A caller can set SDK fields; that does not establish session-aware queues/processors or transactional application side effects. |
| Azure Speech pull streams, configurable SpeechConfig, RecognizeOnceAsync and result/error mapping | Cancellation and multi-utterance recording support are increments to an existing provider. |

## Recommended investigation order

| ID | Customer outcome | Order / effort | Confidence and limits |
| --- | --- | --- | --- |
| [AZURE-001](#azure-001-explicit-service-bus-settlement-and-message-context) | Keep retry/DLQ transfer failures from being treated as completed input | Investigate first; medium | Failed DLQ publish followed by input completion and metadata replacement reproduced locally |
| [AZURE-002](#azure-002-race-safe-table-cache-expiry-and-replacement) | Preserve fresh cache values and treat expired rows consistently | Investigate first; medium | Stale cleanup deletion and expired Add/Replace behavior reproduced with TableClient fakes |
| [AZURE-003](#azure-003-injected-clients-and-passwordless-provisioning-options) | Use managed identity and pre-provisioned resources across adapters | Strong additive capability; medium, client injection first | Connection-string-only Table/MQ public construction and eager resource creation verified in source; RBAC/identity fixtures needed |
| [AZURE-004](#azure-004-consistent-blob-vfs-and-native-async-io) | Use predictable directories and cancellable cloud file operations | Correctness first, async/append phases next; medium–large | Nested depth/enumerator issues reproduced; append and remote cancellation paths remain source-based |
| [AZURE-005](#azure-005-restartable-service-bus-lifetimes-and-isolated-handlers) | Stop/restart/drain safely and handle concurrent messages independently | Investigate alongside AZURE-001; medium | Retained-client disposal reproduced with a fake; handler sharing/recovery-task issues require stress tests |
| [AZURE-006](#azure-006-session-aware-ordered-service-bus-queues) | Process related messages in deliberate per-session order | Validate ordering workloads first; medium–large | Ordinary queue processors verified; a session-aware high-level policy was not located |
| [AZURE-007](#azure-007-partition-aware-cache-keys-and-bounded-bulk-operations) | Bound cache scans/round trips and support deliberate key isolation | Validate workloads first; medium–large, partition filters first | Fixed empty partition, direct RowKey mapping, client-side patterns and sequential bulk methods verified |
| [AZURE-008](#azure-008-cancellable-speech-and-complete-recording-transcription) | Stop recognition predictably and transcribe more than one utterance | Next for speech users; medium–large, cancellation/test seam first | Unused token and RecognizeOnceAsync verified; native/paid speech behavior was not tested |

Start with AZURE-001/002 and the lifecycle correctness phase of AZURE-005. AZURE-003 provides the clearest broadly useful new configuration capability; session support and full-recording speech should follow confirmed workloads. Priority is not acceptance, and each phase can be selected independently.

## AZURE-001: Explicit Service Bus settlement and message context

- **Status:** Proposed. **Owner:** ServiceStack.Azure. **Participants:** core MessageHandler owners for any optional processing-outcome capability; applications retain business idempotency.
- **Value:** Preserve input messages when retry/DLQ transfer fails and retain application metadata across processing.
- **Effort/confidence:** Medium. The isolated runner exercised the real generic handler plus Azure worker/client with a scripted failed DLQ publish; the worker still called CompleteMessageAsync. No real broker loss rate was measured.

### Existing behavior and evidence

[ServiceBusMqWorker](../Messaging/ServiceBusMqWorker.cs) deserializes the envelope, replaces Meta with lock/queue keys, installs a no-op Ack callback, invokes the shared handler and then completes the input. Core [MessageHandler](../../../../ServiceStack/src/ServiceStack/Messaging/MessageHandler.cs) can catch/log an exception from its exception handler and return normally. Azure [Nak](../Messaging/ServiceBusMqClient.cs) publishes a retry/DLQ copy before Ack. Consequently a transfer failure can be swallowed before the worker decides to complete. Manual Ack also removes its pending callback before awaiting actual completion, so a failed settlement loses that local handle.

Existing finally cleanup and runtime-type allowlists must be retained. ServiceStack's application `.dlq` queue is not the broker's native dead-letter subqueue. Microsoft's [settlement contract](https://learn.microsoft.com/en-us/azure/service-bus-messaging/message-transfers-locks-settlement) distinguishes completion, abandon/dead-letter and lock-loss/uncertain outcomes; logging a failure does not settle it safely.

### Bounded proposal

1. Give the worker an explicit provider-local outcome for success, successful retry/DLQ transfer, failed transfer and unknown settlement. Complete only after the required outcome is confirmed; preserve retryability/lock behavior when transfer fails. Prefer a wrapper/context before changing required shared interfaces.
2. Merge application metadata with reserved lock context rather than discarding it. Define collision rules and prevent stale lock tokens from becoming reusable authority in republished envelopes or diagnostics.
3. Make pending manual-ack ownership explicit across success/failure/disposal. Define malformed/unsupported envelope handling without relaxing global type validation or silently acknowledging poison messages.
4. Keep current application DLQ routing compatible by default. Native abandon/dead-letter or broker transactions can be separately accepted options after their semantics are agreed; do not silently reinterpret RetryCount.

### Acceptance criteria if selected

- Handler, retry publish, DLQ publish, response publish and completion can each fail independently. Failed required transfers never lead to an unqualified successful completion; unknown outcomes remain distinguishable.
- Metadata survives processing/retry without allowing reserved lock-key spoofing. Null/invalid/unsupported envelopes follow a deliberate policy.
- Lock cleanup remains bounded; completion failures and duplicate Ack/Nak cannot cause competing settlements. Retain the current protection against synchronous callback completion deadlocks.
- SDK fakes cover the full matrix. Opt-in Service Bus tests validate actual transfer, lock expiry and redelivery. At-least-once transport never implies exactly-once business effects.

### Alternatives and compatibility

Applications can use native SDK processors and explicit settlement today. An SDK transaction might combine broker operations but cannot roll back an arbitrary database write. Any shared outcome contract belongs to core; do not duplicate a generic outbox/idempotency framework here.

## AZURE-002: Race-safe Table cache expiry and replacement

- **Status:** Proposed. **Owner:** ServiceStack.Azure's Table cache. **Participants:** shared cache owners only for optional new capabilities.
- **Value:** Avoid deleting a fresh concurrent value and align Add/Replace with logical cache validity.
- **Effort/confidence:** Medium. Three async fake-client cases reproduced stale cleanup deletion, expired-row Add conflict and expired-row Replace success. Sync paths share the source pattern but were not separately raced.

### Existing behavior and evidence

[GetEntry/GetEntryAsync](../Storage/AzureTableCacheClient.cs) detect expiration then call Remove, which rereads and deletes with ETag.All. A fresh row arriving between the expiry observation and deletion can therefore be removed. [AddAsync](../Storage/AzureTableCacheClientAsync.cs) already uses an atomic AddEntity and returns false on conflict, including a logically expired row. Replace reads without checking expiry, then uses wildcard ETag. Counters already use row ETags and retry conflicts; these strengths should be preserved.

ExpiryDate currently has a 200ms grace, and counter retries use a 30-second window. [Azure conditional updates](https://learn.microsoft.com/en-us/dotnet/api/azure.data.tables.tableclient.updateentityasync?view=azure-dotnet) provide the building block; they do not make a multi-step read/delete sequence conditional automatically.

### Bounded proposal

- Make cleanup conditional on the observed row ETag, treating a changed row as evidence to reread or stop. Do not reread a fresh row and then delete it unconditionally.
- Define expired-row Add/Replace semantics, including zero-duration entries and clock boundaries. Reclaim/replace an expired row through a bounded compare-and-swap loop; distinguish conflict/missing state from transport/auth errors.
- Reuse a cancellation-aware retry/budget policy for cache conflicts; do not replace the existing ETag counter algorithm with unconditional writes. Ensure no stale expired snapshot can reset a counter or new value.
- Review UTC conversion and the grace interval explicitly. Preserve serialized values, type metadata, no-expiry values and existing date fields unless a migration is separately accepted.

### Acceptance criteria if selected

- A fresh replacement survives stale lazy/sweep cleanup. Concurrent adds on missing/expired rows have one winner under the agreed semantics; Replace cannot revive an expired/missing row unexpectedly.
- Sync/async cases cover expired/fresh/no-expiry rows, zero/false/null/polymorphic values, failed conditions, counter races, cancellation and bounded retry exhaustion.
- SDK fakes and Azurite test local expression/ETag behavior; opt-in real Table tests validate service concurrency/date behavior. Do not infer Cosmos DB Table API parity from storage tests.

### Alternatives and compatibility

An initial safe phase can stop deleting expired rows on reads and leave conditional cleanup to sweeps, at the cost of retention. Callers can use another cache for stronger workloads. Logical expiry remains application-enforced; this proposal does not add automatic server TTL or a new distributed lease primitive.

## AZURE-003: Injected clients and passwordless provisioning options

- **Status:** Proposed. **Owner:** ServiceStack.Azure. **Participants:** core/Extensions only if optional host-registration recipes require them.
- **Value:** Use managed identity, configured SDK options and pre-provisioned resources without giving runtime applications management rights unnecessarily.
- **Effort/confidence:** Medium. Constructor/resource-creation behavior verified in source; deployment demand and minimum-role examples need validation.

### Existing behavior and evidence

[Blob providers](../Storage/AzureBlobVirtualFiles.cs) already accept BlobContainerClient, allowing caller-selected credentials and SDK configuration. They still synchronously create the container. [Table cache](../Storage/AzureTableCacheClient.cs) accepts a connection string and creates its table. [MQ factory](../Messaging/ServiceBusMqMessageFactory.cs) creates ServiceBusClient/AdministrationClient from a connection string and checks/creates queues during start and sender/receiver acquisition. The SDK [TableClient](https://learn.microsoft.com/en-us/dotnet/api/azure.data.tables.tableclient?view=azure-dotnet) already supports client construction with TokenCredential; the missing increment is adapter configuration, not an Azure authentication implementation.

### Bounded proposal

1. Add backward-compatible injected TableClient and Service Bus client/admin-factory paths with explicit owned/borrowed lifetimes. This also enables supported SDK fakes without reflection-based construction.
2. Add explicit existing-resource versus create-if-missing modes. In data-only mode, avoid administration calls on each publish/receive; fail clearly when a resource is absent. Preserve current convenience behavior as a deliberate default where compatible.
3. Provide asynchronous initialization/factory APIs with cancellation instead of hiding resource creation in constructors for new usage. Keep existing constructors working.
4. Document endpoint/TokenCredential composition and least-privilege examples separately for Blob, Tables and Service Bus. Accept application-selected credentials; avoid forcing Azure.Identity or inventing a new credential store.

### Acceptance criteria if selected

- Pre-provisioned data-only operation performs no create/admin checks. Fake clients capture this; opt-in Azure tests verify actual runtime versus provisioning roles.
- Borrowed clients remain usable after adapter disposal; owned clients are disposed once. Startup cancellation/failure leaves no half-started processor set.
- Existing connection-string examples work across all supported targets. SDK retry/transport/options can be supplied without shared-interface changes or credential logging.

### Alternatives and compatibility

Apps can already inject BlobContainerClient and use raw SDKs for Table/MQ. Start with recipes if those solve demand. SpeechConfig can also already be supplied; passwordless speech support is a separate SDK/provider decision, not automatically covered by these storage/MQ constructors.

## AZURE-004: Consistent Blob VFS and native async I/O

- **Status:** Proposed. **Owner:** ServiceStack.Azure. **Participants:** Common/core for optional VFS contract gaps; core FilesUploadFeature for integration validation.
- **Value:** Make VFS traversal consistent and stop synchronous cloud work from hiding behind async entry points.
- **Effort/confidence:** Medium–large in independent phases. Block directory depth/enumeration issues reproduced; native async and append limits/lifetimes need additional tests.

### Existing behavior and evidence

[Block](../Storage/AzureBlobVirtualFiles.cs) and [append](../Storage/AzureAppendBlobVirtualFiles.cs) listings already use SDK Pageable and trailing-slash folder deletion. [Directory depth](../Storage/AzureBlobVirtualDirectory.cs) compares slash counts against maxDepth without subtracting the starting directory depth; the nested direct-file case failed in the probe. Both directory GetEnumerator methods throw, and append directory LastModified is unimplemented.

The inherited [WriteFileAsync](../../../../ServiceStack/src/ServiceStack.Common/VirtualPath/AbstractVirtualPathProviderBase.cs) calls synchronous WriteFile and ignores its token. File reads/refresh use synchronous SDK calls; inherited partial-copy helpers still exist. Append writes delete/create then send one block, and append-file LastModified prefers creation time. Microsoft's [Append Block contract](https://learn.microsoft.com/en-us/rest/api/storageservices/append-block) includes service-version-dependent limits and append conditions; one SDK call is not a complete arbitrary-sized transfer policy.

### Bounded proposal

1. Correct non-root depth semantics, directory enumeration and normalized directory/root handling for both providers. Define append directory timestamp behavior and document any change to CreatedOn-based file LastModified.
2. Override the existing WriteFileAsync contract with native SDK async upload and cancellation; add provider-specific async listing/read/delete methods where existing contracts lack them. Keep streaming memory bounded and preserve caller/response stream ownership. Do not introduce Task.Run wrappers as native I/O support.
3. As a separate append phase, support bounded chunking using current SDK/service limits, explicit create-if-missing behavior and optional append-position/ETag conditions. Explain partial append outcomes and preserve caller-owned input streams. Delete/create/append replacement cannot be called atomic.

### Acceptance criteria if selected

- Run the same root/nested glob/depth/enumerator/path cases for block/append providers, including empty/root destructive operations and similarly named sibling paths. Multiple pages already work and remain covered.
- Cancellation before dispatch and during upload/list/delete stops further work; large/nonseekable streams do not require whole-file buffering. Test failure cleanup and input/response ownership.
- Append tests cover new/existing blobs, several blocks, retry ambiguity and concurrent append conditions. Azurite coverage is labeled separately from opt-in Azure service/version tests.
- FilesUploadFeature integration retains access validation and error propagation. No required new IVirtualFiles members are necessary for an initial native WriteFileAsync override.

### Alternatives and compatibility

Raw Blob clients already expose transfer options, ETags, ranges and leases. Prefer a provider-local capability or recipe where sufficient. Presigned access, blob version management and resumable multipart workflows require separate acceptance; never treat path normalization as an authorization boundary.

## AZURE-005: Restartable Service Bus lifetimes and isolated handlers

- **Status:** Proposed. **Owner:** ServiceStack.Azure. **Participants:** core generic-handler owners only if their contract changes; application delegates must retain their documented concurrency obligations.
- **Value:** Make lifecycle state truthful and avoid sharing mutable per-message state between concurrent deliveries.
- **Effort/confidence:** Medium. Stop/client retention was probed with a fake and no registered handlers; concurrency and live restart remain unvalidated.

### Existing behavior and evidence

[StopQueues](../Messaging/ServiceBusMqMessageFactory.cs) stops/disposes processors/senders/receivers and the readonly retained ServiceBusClient, clears dictionaries, but leaves workers accumulated. Factory Dispose only changes status. Missing-queue recovery runs a detached Task with no lifecycle cancellation. Start lacks a repeated-start guard.

AddQueueHandler creates one IMessageHandler per processor while setting MaxConcurrentCalls to the requested thread count. Core [MessageHandler](../../../../ServiceStack/src/ServiceStack/Messaging/MessageHandler.cs) is documented as single-threaded and stores MqClient/counters. Do not infer safe handler reentrancy from thread-safe Azure SDK clients. Existing SDK [processor options](https://learn.microsoft.com/en-us/dotnet/api/azure.messaging.servicebus.servicebusprocessoroptions?view=azure-dotnet) include prefetch and lock renewal; expose them carefully rather than rebuilding renewal from scratch.

### Bounded proposal

- Separate restartable Stop from terminal Dispose, or recreate all owned clients on restart under a documented lifecycle. Serialize Start/Stop/Dispose, make repeated calls deliberate, clear stale worker state and report partial startup failure truthfully.
- Track/cancel/await recovery tasks so they cannot restart after shutdown. Bound drain/stop through new async lifetime methods while retaining synchronous wrappers.
- Allocate an independent handler per concurrent unit or serialize access to a single handler. Avoid sharing mutable handler state while preserving intended application concurrency and statistics.
- Expose provider options for prefetch, SDK renewal budget and error callbacks with validated combinations. Align manual receiver/ack cleanup with AZURE-001 and borrowed-client ownership with AZURE-003.

### Acceptance criteria if selected

- Start/Start, Start/Stop/Start, Dispose-before-Start, failed startup and racing recovery/shutdown release each owned resource once and preserve borrowed ones.
- No missing-queue recovery starts after stop/dispose. State/stats reflect active workers rather than cumulative stale registrations.
- A stress fixture proves each message uses independent handler context; failures do not corrupt another message's Ack/Nak path. Include lock loss and cancellation during drain.
- Live Service Bus restart/renewal tests supplement fakes; no claim of infinitely renewable locks or guaranteed rollback on shutdown.

### Alternatives and compatibility

Use SDK processors directly or recreate the whole adapter per host lifecycle today. A minimal correctness release can fix lifecycle/handler allocation first; fully async publishing/batching is a separately selectable extension, not an implied requirement to rewrite IMessageProducer.

## AZURE-006: Session-aware ordered Service Bus queues

- **Status:** Proposed, validate ordering workloads first. **Owner:** ServiceStack.Azure. **Participants:** none for provider-specific options; core messaging only for an optional shared capability.
- **Value:** Process related messages in order while allowing independent groups to progress concurrently.
- **Effort/confidence:** Medium–large. Queue-only non-session processors and existing field filters verified; no session workload was service-tested.

### Existing behavior and evidence

[PublishMessageFilter](../Messaging/ServiceBusMqMessageProducer.cs) can already set ServiceBusMessage fields, and MessageId maps from IMessage.Id. However [queue creation/processing](../Messaging/ServiceBusMqMessageFactory.cs) uses default CreateQueueOptions and CreateProcessor, not session-enabled policies and CreateSessionProcessor. A SessionId property on an application's authentication DTO is not a broker-session implementation. Microsoft's [session model](https://learn.microsoft.com/en-us/azure/service-bus-messaging/message-sessions) supplies the actual ordering/ownership contract.

### Bounded proposal

- Add opt-in session queue/processor options and a message-to-SessionId mapping, preserving group identity through retries and replies. Validate missing IDs and topology mismatch before starting consumption.
- Define MaxConcurrentSessions, per-session concurrency, lock-renewal/drain behavior and retry ordering. A retry published at the tail must not silently overtake earlier required work.
- Keep standard queues unchanged. Existing non-session queues cannot simply be converted in place; require an explicit new-entity/migration plan. Broker duplicate-detection settings/stable MessageId can be a separately selected option.

### Acceptance criteria if selected

- Test multiple sessions, ordered work within one session, failure/redelivery, retry/DLQ policy, session lock loss and graceful shutdown.
- SDK request filters retain session/reply metadata in every publish path. Live session-enabled fixtures establish observed behavior; a local fake alone is insufficient.
- Make ordering scope explicit. Do not promise global ordering, business exactly-once effects or atomicity across external stores.

### Alternatives and compatibility

Raw SDK session consumers are viable. Topic/subscription routing, scheduling and temporary request/reply queues are separate increments; avoid combining them into an unbounded messaging rewrite.

## AZURE-007: Partition-aware cache keys and bounded bulk operations

- **Status:** Proposed, validate workload/cost first. **Owner:** ServiceStack.Azure. **Participants:** shared cache owners only for optional capability results; applications choose tenancy/isolation policy.
- **Value:** Reduce unnecessary scans/round trips and make key limits/isolation explicit before larger cache deployments.
- **Effort/confidence:** Medium–large; partition-scoped queries and validation are smaller initial phases. Source behavior is clear; throughput/cost gains were not benchmarked.

### Existing behavior and evidence

[Table cache](../Storage/AzureTableCacheClient.cs) uses a fixed empty PartitionKey and caller key directly as RowKey. Key queries have no server filter/projection, then run glob/regex locally. Bulk Set/Get/Remove are sequential conveniences, and cleanup scans keys then rereads rows. [Async pattern removal](../Storage/AzureTableCacheClientAsync.cs) materializes all matched keys before removal. Async iterators suppress the EnumeratorCancellation warning rather than forwarding consumer-enumerator cancellation automatically; explicit method tokens do reach SDK queries.

TableCacheEntry.Data is a single string. Its StringLength annotation/comment does not validate the service's actual string-property/key limits; the [Table data model](https://learn.microsoft.com/en-us/rest/api/storageservices/understanding-the-table-service-data-model) distinguishes property limits from the whole entity. Arbitrary cache-key and payload support must not be inferred from the generic ICacheClient signature.

### Bounded proposal

1. Add explicit optional namespace/partition/key mapping with compatibility-safe defaults. Scope server queries to owned partitions and request only necessary columns. Validate keys/payloads early against current service rules; define how invalid/long keys are encoded or rejected.
2. Preserve original-key semantics for pattern operations if encoded keys are offered. Hash-only keys cannot transparently provide arbitrary globs; document tradeoffs and migration/old-client coexistence.
3. Stream cleanup/removal in bounded windows and expose page/budget controls where justified. Forward both explicit and enumerator cancellation. Keep regex match timeouts and add an overall scan budget where requested.
4. Add opt-in bounded-concurrency bulk methods or same-partition entity transactions where service constraints fit. Report partial/unknown outcomes truthfully; do not label existing SetAll atomic or promise a batch-read operation the service does not supply.

### Acceptance criteria if selected

- Other namespaces/partitions are excluded from queries and destructive operations under the selected ownership model. Test colliding keys, invalid characters, Unicode, long values and payload boundaries.
- Default key addressing remains compatible. Migration, FlushAll/FlushOnDispose scope and mixed old/new clients are documented before rollout.
- Cancellation and scan limits work across empty/multiple pages; memory is bounded independently of match count. Benchmark representative hot-partition and large-table workloads before performance claims.
- Azurite and opt-in Azure fixtures cover request/service limits separately. Shared-table tenancy is not inferred from a prefix alone.

### Alternatives and compatibility

Dedicated tables and raw SDK queries solve some isolation needs already; Redis or another provider may fit high-throughput/large-value workloads better. An IDistributedCache adapter, arbitrary value chunking and a general Table ORM are separate proposals requiring demand and schema decisions.

## AZURE-008: Cancellable speech and complete recording transcription

- **Status:** Proposed. **Owner:** ServiceStack.Azure's AzureSpeechToText. **Participants:** shared speech/ServiceStack.AI owners only for optional cross-provider result/job capabilities; VFS owners for optional async-read gaps.
- **Value:** Respect cancellation and avoid presenting one recognized phrase as a complete multi-utterance recording.
- **Effort/confidence:** Medium–large; cancellation/test seam first, full recordings second. Source/API semantics verified; no native Speech, codec or paid service behavior was executed.

### Existing behavior and evidence

[AzureSpeechToText](../AI/AzureSpeechToText.cs) accepts but never reads its cancellation token, opens VFS content synchronously, invokes RecognizeOnceAsync and parses transcript/raw response/confidence. InitAsync is a no-op. The SDK [RecognizeOnceAsync contract](https://learn.microsoft.com/en-us/dotnet/api/microsoft.cognitiveservices.speech.speechrecognizer.recognizeonceasync?view=azure-dotnet) handles one utterance; the local docs describe recorded audio without explaining that boundary. SpeechConfig is already caller-configurable, and compressed audio/codec setup already exists.

### Bounded proposal

1. Introduce an injectable recognizer/session adapter preserving the current constructor. Add pre-cancellation checks, an overall deadline and cancellation that stops/drains native recognition and releases pull-stream resources. Merely abandoning the await does not stop native work.
2. Add an explicit full-recording mode based on continuous recognition, collecting ordered utterances through session completion. Preserve existing single-utterance behavior as a deliberate mode; do not silently redefine transcript/raw-response/confidence semantics.
3. Make result JSON ownership/disposal and malformed/empty result handling deterministic. Bound aggregate output/memory and define confidence aggregation, partial results, NoMatch and service cancellation distinctly from caller cancellation.
4. Document native SDK/platform/codec prerequisites and test supported recording formats. Long-running batch jobs, diarization and streaming event APIs are separate future scopes.

### Acceptance criteria if selected

- Fakes cover caller cancellation before/during open/recognition, timeout, completion races and exact disposal of recognizer/audio/stream resources. Retain BinaryAudioStreamReader disposal behavior.
- Multi-utterance tests include silence-separated phrases, partial failure, empty audio and large input/output. Single mode remains compatible; recording mode does not stop after the first phrase.
- Native fixtures validate actual codec/platform behavior; opt-in Azure tests validate recognition lifecycle and error mapping with explicit resource/charge scope.
- Shared ISpeechToText implementers remain compatible. Document whether cancellation can stop remote billing/work rather than promising it.

### Alternatives and compatibility

Use the Speech SDK directly or another existing provider for full recordings today. A documentation-only first phase can clarify the single-utterance boundary. Generic speech job/stream contracts require a shared-owner decision, not required Azure-only methods on every provider.

## Deferred and externally owned opportunities

- Topic/subscription pub-sub, temporary request/reply queues, scheduled sends and native Service Bus transactions merit separate customer evidence. Existing publish filters/raw SDKs are alternatives; queue support is not evidence those high-level workflows already exist.
- Native async publish/batch APIs and bounded producer backpressure can follow lifecycle/settlement correctness. Do not equate a SendMessageAsync wrapper blocked synchronously with a fully async MQ path.
- Blob version/presigned access, Key Vault settings, Azure OpenAI, Cosmos DB repositories, telemetry and Microsoft IDistributedCache are not automatically accepted by this review. Consult their actual owners/recipes first.
- Retain existing runtime-type allowlists, regex timeouts, lock cleanup and path normalization. Historical security remediation is existing source behavior, not newly accepted product work.

## Decision handling

After explicit acceptance, move the complete bounded record to [FEATURE-IMPROVEMENTS.md](FEATURE-IMPROVEMENTS.md), preserving its AZURE ID. Record the decision/date, customer outcome, selected phases, owner, migration, criteria, actual validation and status. Leave a decision link here and index cross-project participation at the canonical owner. Keep source, validated behavior and released availability separate.
