# Long-running AI.Chat agents

AI.Chat can now keep complex agent work running across many model and tool exchanges without tying the
job to one browser request. You can leave or refresh an active chat and return to its persisted state.
A temporary browser connection failure no longer discards the run.

## What is improved

- Long tasks continue in resumable execution slices instead of failing after ten tool iterations.
- Progress, elapsed time, context usage, cancellation, failures, and tool-approval waits survive page
  refreshes because the database records the run.
- Partial assistant output is isolated from completed history, so an interrupted stream cannot damage
  earlier messages.
- Very long conversations automatically reduce the context sent to the model while keeping the full
  original chat available for review.
- The chat view initially loads the first 20 and latest 100 messages. The large middle is fetched only
  when requested, keeping rendering responsive even for thousands of messages.
- The sidebar uses the authoritative database message count and a small preview instead of downloading
  every conversation.
- Server-sent events normally deliver lower-latency updates. If SSE is unavailable through a proxy or
  host, the browser automatically falls back to reliable long polling.
- Text-only and strict providers receive a compatible copy of the conversation, reducing failures from
  old multipart attachments, orphaned tool results, or malformed legacy tool history.

## Context reduction

The context indicator shows how much of the selected model's window is in use. At about 80%, AI.Chat
summarizes older working context, preserves current instructions and a recent verbatim tail, and then
continues the same run. The complete visible conversation is not deleted. Status such as `Reducing
context · … · part 3/8` reports real compaction progress.

The compact button uses the same hardened service but creates a new child chat. This is useful when you
want a clean continuation while retaining the original thread unchanged.

## Waiting and cancellation

The pending response shows time since the last received activity, not total chat age. New streamed
content resets that timer. The longer-than-expected warning therefore appears only when a run has been
quiet for an extended period. You can cancel a run at any time; cancellation is persisted and propagated
to the active provider/tool operation.

## Configuration

No configuration is required. The default is:

```json
{
  "defaults": {
    "events": {
      "transport": "auto"
    }
  }
}
```

`auto` tries SSE in the browser and falls back to long polling if the connection does not become or
remain healthy. To disable SSE while keeping the same API routes, set `transport` to `long-poll`.
