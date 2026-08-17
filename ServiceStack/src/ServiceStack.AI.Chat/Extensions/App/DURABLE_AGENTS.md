# Durable agent architecture

This is the C# implementation of AI.Chat's durable long-running agent loop. It intentionally uses
asynchronous work in the web process, not a dedicated OS thread or child process. The RDBMS is the
authority, so execution can later move to ServiceStack Background Jobs or another worker without
changing the conversation or run model.

## Representations and invariants

Four representations must remain separate:

1. `ChatMessage` is the canonical, append-oriented conversation and audit history.
2. `ChatThread.Messages` is a compatibility projection for older code. It is kept synchronized but
   is not used for normal list, paging, or context-window reads.
3. `ContextSnapshot.Summary` plus canonical rows after `ToSequence` is the bounded model context.
   Compaction never deletes or rewrites the visible conversation.
4. A provider payload is a deep copy of that context. Provider-specific cleanup and repair must not
   mutate checkpoint identities or canonical history.

Assistant tool calls and their contiguous tool results are an atomic logical group. Paging,
compaction, and provider repair should not split this group. Server timestamps are monotonic message
identities; sequence is the stable RDBMS cursor.

## Additive schema migration

`ChatDb.InitSchema()` creates and additively migrates:

- `AgentRun`: queue state, progress, budgets, context usage, leases, errors, and lifecycle timestamps.
- `AgentStep`: ordered slice evidence and the unique `run:{runId}:step:{sequence}` idempotency key.
- `ChatMessage`: canonical JSON messages with thread sequence, timestamp, tool correlation, provenance,
  token estimate, and active-branch marker.
- `ContextSnapshot`: versioned, non-destructive summaries with exact canonical sequence coverage.

It also retains `ChatThread.StreamingMessage` and `ChatThread.ContextTokens`. `CreateTableIfNotExists`
and `AddMissingColumns` make startup safe against existing installations; no destructive rebuild or
manual migration is required. Legacy `ChatThread.Messages` is backfilled into `ChatMessage` on first
use. Explicit edit/redo/delete operations deactivate the prior branch and create a new active branch.

## Run lifecycle

Posting `threads/{id}/chat` validates ownership, persists the user turn, rejects another active run,
inserts a queued `AgentRun`, wakes the scheduler, and returns immediately. `AgentScheduler` then:

1. claims eligible rows with a compare-and-set from `queued` to `running`;
2. assigns an owner and renewable lease;
3. runs at most `defaults.agent.maxConcurrency` asynchronous slices (default 2);
4. creates an `AgentStep` before each slice;
5. completes a run on a final model response, or requeues it when the per-slice tool limit is reached;
6. persists failures/cancellation and wakes connected clients; and
7. requeues interrupted `running` rows at startup and graceful shutdown.

The coordinator only polls while work is active. When idle, it blocks on an in-memory semaphore and
performs no once-per-second queue query. Every enqueue and slice completion signals it.

`ChatLimits.MaxIterations` is a slice boundary for a projected durable context. Stateless API calls
still receive the original terminal maximum-iterations error because they have no durable checkpoint
from which to resume. `metadata.maxSteps` is the overall run budget and defaults to 250 slices.

Interactive tool approval changes the run to `waiting_approval`. Continuing an approved batch
requeues that same run instead of inventing a new user message.

## Checkpointing

`ChatContext.ProjectedContext` distinguishes model projection from canonical history. At slice start,
the context records every persisted timestamp. Assistant tool-call and tool-result messages receive
monotonic timestamps before filters run. `OnChatToolAsync` appends only timestamps not in that identity
set and annotates them with `RunId` and `StepId`.

This identity reconciliation is deliberately not positional: provider normalization may merge or
remove projected messages. The provider receives `currentChat.Clone()`, so stripping `_sequence`,
timestamps, usage, or unsupported content cannot corrupt the worker's checkpointable list.

The streaming assistant message remains in `ChatThread.StreamingMessage` until committed. An aborted
stream can therefore lose only its partial response, never the conversation.

## Context accounting and compaction

Before each slice, the worker builds context from the latest snapshot and canonical tail and stores
the approximate token count plus the model's configured `limit.context` on the run. Automatic
compaction normally starts at 80%; when metadata has no model limit the fallback is 80,000 tokens.

Thread metadata can override:

- `compactThreshold`
- `compactChunkTokens` (default 60,000, minimum 8,000)
- `compactRecentMessages` (default 12, minimum 4)

Automatic and manual compaction use `CompactMessagesAsync`. The service preserves leading
system/developer instructions and a recent verbatim tail, keeps tool groups together, bounds huge
resource values, summarizes in bounded batches, validates text-only output and real size reduction,
and hierarchically reduces for at most four passes. Internal summarization uses `NoHistory` and
`NoStore`, so it cannot pollute the thread or accounting history.

Automatic compaction writes a new `ContextSnapshot` and continues the same run. Manual compaction
creates a completed child thread, leaving the original conversation intact.

Generated summaries carry an internal projection marker. On a later compaction they are folded into
the next summary instead of being mistaken for authoritative leading instructions, preventing a
long-lived run from accumulating an ever-growing chain of summaries. The marker is stripped from
provider payloads and manual child-thread history.

## Provider compatibility

Provider preparation is projection-only. UI fields (`timestamp`, `_sequence`, `streaming`, model and
usage metadata) are removed from outbound copies. Text-only models receive string content; multipart
attachments and top-level tool resources become compact textual placeholders.

Strict GLM projections additionally move system context to the beginning, merge adjacent ordinary
roles, preserve complete tool exchanges, remove duplicate tool-call IDs, turn orphan tool results
into ordinary context, discard incomplete tool envelopes while retaining useful prose, and add a
neutral continuation turn when recovering an interrupted assistant state. Canonical history is never
silently rewritten by these repairs.

## UI retrieval and updates

Thread list queries omit the large compatibility message/tool blobs and return one-message previews
plus authoritative `messageCount`. A selected thread receives at most 20 messages from the beginning
and 100 from the end, separately bounded to 128 KiB and 384 KiB. `messageWindow` exposes stable ranges,
and `threads/{id}/messages?before=&after=&take=&maxBytes=` pages through the omitted middle.

Update routes are fixed:

- `GET threads/{id}/updates/stream` — SSE with connected/thread/heartbeat events.
- `GET threads/{id}/updates` — long-poll fallback.

Only transport behavior is configurable at `defaults.events`. Missing configuration defaults to
`auto`; the browser attempts SSE and falls back to long polling after its configured health threshold.
Setting `transport` to `long-poll` makes the SSE route return unavailable while retaining the fixed URL.

Refresh signatures are derived from canonical message count/sequence plus streaming and terminal
state. Sidebar, selected-window, SSE, and long-poll reads therefore never select `ChatThread.Messages`.
Frequent status/context writes use partial-column updates, and checkpoint reconciliation queries only
the incoming timestamps; neither path scales its RDBMS work with the size of the legacy JSON blob.

## Operational boundary

The claim is safe for one or more schedulers sharing the database because each candidate is updated
only while still queued. For a high-scale multi-process deployment, retain the same schema and add
dialect-specific row locking/skip-locked claims plus expired-lease recovery. Do not replace the
append-oriented message and snapshot model with process-local state.
