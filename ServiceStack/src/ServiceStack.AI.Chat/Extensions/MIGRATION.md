# Migration, backups and validation

The 2026-10-05 migration implements the 175 frozen inputs listed in the upstream
`docs/AI_CHAT_MIGRATION_INVENTORY.json`. Nine subsequent upstream UI edits are excluded and preserved.
Shared assets are copied verbatim; Git, Jev and OpenAI auth are registered backend extensions, not
features activated solely by copying JavaScript. The initial release fixture audited 282 shared embedded
assets, including the unchanged main chat selector. Subsequent full syncs consume the current llms-py
UI verbatim and generate `chat/shared-assets.json`; the packaged test validates that current manifest
rather than retaining old source hashes. Full synchronization preserves upstream and host-owned data.

## Storage and upgrade

Stop the owning host, back up its database using the database engine's supported consistent backup
mechanism, and back up the complete App_Data root and separately configured project directories.
Retain file permissions and protect credentials/backups. If using `NamedConnection`, back up that
connection's chat data as well as any host authentication/API-key database. Do not copy an actively
written SQLite database or restore individual recipe files over a running host.

Start one upgraded host process per App_Data root. Additive OrmLite migration creates missing columns
and the Projects creation/reservation tables; Gemini identity backfill validates duplicates before
rewriting document scopes. Saved imports and existing remote names remain associated with their owner,
source and manifest. Duplicate identities require operator reconciliation before claiming a successful
upgrade. Projects keep IDs, history and archive membership; creation operations reconcile restart state.
Run identity/explorer/import checks before re-enabling background work.

Jev portable recipes, history, submission receipts, counters and journals travel together under each
user's `jev` directory. Migrate legacy Python SQLite-only profiles with the current Python application
first, then copy the whole portable directory while hosts are stopped. Individual JSON import omits
private history/publication ownership. Reconnect publisher accounts explicitly. Subscription migration
never reads an operator's Codex profile automatically; preserve this application's issued client and
stable host identity together. Read the extension READMEs before copying grants.

## Rollback

A binary-only downgrade is not certified. Older Gemini identity algorithms and older Projects/Jev
writers do not understand all new reservations/journals. Stop the upgraded host and restore the
matching **complete** pre-upgrade database and file backup before starting the previous binary. This
restores the old schema, scopes, IDs and counters together; any work since the backup is lost. Keep
post-upgrade data separately if it must later be reconciled. Remote Git commits, uploaded Gemini copies
and publications are external state: restoring local storage does not undo them. Review those receipts
and remote state before resuming writes; never blindly repeat a paid decision or uncertain publication.

Tests cover additive schema/backfill, restart recovery, captured workspaces, unchanged history, and a
stopped-host backup/restore path. They do not certify every older binary against the new schema.

## Verification

From the downstream monorepo (use disposable data and fixture accounts):

```sh
dotnet build ServiceStack/src/ServiceStack.AI.Chat/ServiceStack.AI.Chat.csproj -f net8.0
dotnet build ServiceStack/src/ServiceStack.AI.Chat/ServiceStack.AI.Chat.csproj -f net10.0 -t:Rebuild --no-dependencies
dotnet test ServiceStack/tests/ServiceStack.AiTests/ServiceStack.AiTests.csproj --filter 'FullyQualifiedName~AiChat&FullyQualifiedName!~Real_CSharp_to_isolated_publisher'
dotnet test ServiceStack/tests/ServiceStack.AiTests/ServiceStack.AiTests.csproj --filter FullyQualifiedName~IdentityChatAuthTests
```

The explicit publisher test requires the isolated publisher fixture, not the production catalog.
Explicit MariaDB/PostgreSQL/SQL Server fixtures require configured local engines and disposable owned
records. The regular suite exercises real C# routes and Chromium under empty and `/chat` prefixes,
user isolation, cookie/API-key/RequiredRole boundaries, named storage and default/disabled modes.
Existing provider, durable-agent, MCP/API-tool and PDF checks remain part of regression validation.

Controlled OAuth tests use a signed fake issuer and never perform live authorization or paid
inference. Before claiming a deployment can use ChatGPT plan inference, verify its actual account,
registration, callback behavior and inference eligibility against the current OpenAI documentation.
Hosted automatic OAuth is not implemented; the available workflow uses the complete manual callback.
