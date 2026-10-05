# Decision Studio (Jev)

`JevExtension` installs the frozen shared Decision Studio UI and `/ext/jev` routes. Decisions use one raw OpenRouter decision request, separate from chat completions and durable agents. Enable OpenRouter and configure its API key to execute decisions; editing and importing recipes remain available without it. `PublishExtension.Enabled` and a connected per-user publisher account are required to publish or star community recipes.

Data lives below `App_Data/chat/user/<identity>/jev`: portable `recipes/*.json`, readable `history/*.md`, the recipe/run index, initialization receipt, and private import/publication journals. A single host process must own this App_Data root. Back up the whole directory, including dotfiles and pending publication receipts, before moving or upgrading it. Filenames preserve Unicode and Python case folding; saved names are metadata, not implicit filename renames. Revision and replacement controls prevent stale writes. Expired runs become interrupted and are never automatically retried as paid work.

## Moving Python profiles

The current Python file format is supported. Stop the source and destination hosts, back up both profiles, then copy the source user's entire `jev` directory to the destination identity's `jev` directory. Keep private journals and history with their originating user. Publisher credentials are separate: reconnect that user through Publisher settings rather than copying an operator's account. Restart and review the recipes/history before running or publishing.

AI.Chat does not read the older `jev.sqlite` format. First open that profile in the current Python application so its existing migration writes the portable recipe/history files and initialization receipt, then stop it and copy the resulting directory as above. For individual recipes, use Python's Export recipe JSON and import the JSON file or its URL in Decision Studio; this transfers documented actual-run examples, but not private history or publication ownership. Preserve the original database and backup until the migrated profile is checked. Do not delete an initialization receipt to force migration or merge two users' profiles.

A rollback to a binary without these extensions requires restoring its matching pre-migration data backup. New recipe/index identities, journal states, and publication receipts must not be handed to an old binary as an assumed compatible downgrade.

## Host network boundaries

Generic JSON URL imports are unauthenticated, capped at 512 KiB and six response attempts, and every redirect passes `ChatFeature.ValidateDownloadUrl`. Configure that policy for the host's permitted outbound destinations. Default transports disable cookies and automatic redirects. Test handler factories must do the same. Decisions have a 2 MiB response limit, 45-second request and 55-second execution bounds with no hidden paid retries. Publisher calls retain an immutable user account for the entire operation; local locks are released before network waits. Pending publications retry their exact saved payload after a lost response or local edit. External revision conflicts archive the reservation and reviewed payload for inspection.

`DecisionHandlerFactory` and `DownloadHandlerFactory` provide isolated transport seams for tests and host integration. Decision Studio settings never replace the host's Identity authentication or filesystem policy.
