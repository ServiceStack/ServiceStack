# ServiceStack.Blazor feature proposals

Reviewed 2026-11-11 at source HEAD `101038b4ba5a65650e02491e98c7c8a4e1d46f5f`. **Proposed only: no product implementation is accepted by this analysis.** See the [existing inventory and validation limits](../FEATURES.md), [accepted-improvements ledger](FEATURE-IMPROVEMENTS.md) and [decision workflow](../../../../ServiceStack/src/FEATURES.md#planning-workflow).

Priorities describe engineering fit and concrete source evidence. Customer reach, support frequency, performance gains and commercial value have not been measured. Effort is relative scope, not a delivery estimate. A reproduced UI/cache concern below is not a claim of a deployed server authorization bypass.

## Existing behavior checked before proposing work

| Existing capability | Boundary for recommendations |
| --- | --- |
| AutoQuery grid CRUD, filtering, sorting, paging, CSV/API URLs and templated columns/toolbars/forms | Do not propose basic CRUD grids, filters, exports or custom forms as new. |
| ApiPrefs/ColumnSettings persisted in local storage; FilterViews displays/removes active filters | Saved preferences already exist; named views, cross-device storage or full query serialization would be distinct extensions. |
| Metadata create/edit/embedded-field forms, multipart uploads, patch/reset mapping and ResponseStatus errors | Improve lifecycle, validation integration and editing safety; do not describe form generation, patch support or upload support as missing. |
| Autocomplete keyboard/paste behavior, modal lookup and some ARIA markup | Extend remote-data and accessibility behavior; do not propose all keyboard support or all lookup UI as new. |
| Clone-capable server gateways, HTTP client factories and WASM auth state | Preserve both transports and current registration; server/Identity implementations live in core/Extensions. |
| Per-component render-action queues, encoded dump helpers, local-return-URL and role/permission checks | Existing security work is a regression baseline. Process-static lookup/config/metadata state needs separate scrutiny. |
| Server/WASM galleries and bUnit package references | Samples and commented prerender tasks do not establish automated interaction, accessibility or render-mode coverage. |

## Recommended investigation order

| ID | Customer outcome | Order / relative effort | Confidence |
| --- | --- | --- | --- |
| [BLAZOR-001](#blazor-001-scope-and-bound-ui-lookup-state) | Keep lookup/UI metadata state isolated between apps, users and circuits | Investigate first; medium, with a small lookup-only first phase | Cross-client label reuse reproduced; deployed exposure depends on app data/identity boundaries |
| [BLAZOR-002](#blazor-002-make-auth-state-transitions-explicit-and-testable) | Reflect logout and identity changes reliably in WASM UI | Investigate first; small–medium | Stale authenticated UI state reproduced with a fake successful logout |
| [BLAZOR-003](#blazor-003-cancellable-component-requests-and-stale-result-protection) | Stop obsolete work and show the newest query result | Next; medium | Token forwarding and result-generation gaps verified in source; races need component tests |
| [BLAZOR-004](#blazor-004-accessible-overlays-comboboxes-and-keyboard-lifetimes) | Operate forms/lookups entirely by keyboard with predictable focus | Next; medium | Existing keyboard/ARIA baseline inspected; browser/assistive-technology gaps need validation |
| [BLAZOR-005](#blazor-005-bounded-cancellable-uploads-with-accurate-status) | Upload with clear limits, progress, cancellation and truthful completion | Next; medium | Buffering, fixed standalone limit and completion flow verified in source |
| [BLAZOR-006](#blazor-006-optional-data-providers-and-virtualized-result-windows) | Keep grids and option pickers responsive with larger datasets | Demand/benchmark gate; medium–large, split provider and virtualization phases | Collection rendering/local options verified; no measured bottleneck or customer demand supplied |
| [BLAZOR-007](#blazor-007-explicit-render-mode-and-prerender-initialization-contract) | Initialize consistently across prerendered and interactive hosting | Next after state/cancellation foundations; medium | Initialization/storage boundaries verified; full render matrix not executed |
| [BLAZOR-008](#blazor-008-generated-form-edit-state-and-unsaved-change-guards) | Avoid losing edits and compose validation with generated forms | Demand validation; medium | Original/edited dictionaries and callbacks exist; shared dirty/guard lifecycle not located |

Start by validating and fixing the reproduced state issues, then establish component/browser regression fixtures with the request and focus work. Virtualization should follow measurements on representative grids. This order is not product acceptance and does not commit all candidates to one release.

## BLAZOR-001: Scope and bound UI lookup state

- **Status:** Proposed. **Owner:** ServiceStack.Blazor. **Participants:** ServiceStack.Client if shared metadata-cache behavior changes; core/Extensions for host identity context.
- **Value:** Prevent unrelated users/apps from displaying another context's cached labels and give long-lived Server apps bounded cache lifetime. This is an isolation/correctness improvement to existing lookup functionality.
- **Effort/confidence:** Medium overall; lookup-only DI adapter is a bounded first phase. High confidence in cross-client key reuse; production data sensitivity and tenant selection need maintainer validation.

### Existing behavior and evidence

[LookupValues](../LookupValues.cs) holds static nested dictionaries keyed by model, record ID and label field. [LookupInput](../Components/Tailwind/LookupInput.razor.cs) reads/writes them. A temporary net10.0 probe inserted a label and retrieved it through another API client's base URL without a fetch. No deployment/customer incident is asserted.

Metadata is already cached: [BlazorUtils.ApiAppMetadataAsync](../BlazorUtils.cs) calls the Client [ApiCacheAsync](../../../../ServiceStack/src/ServiceStack.Client/JsonApiClientUtils.cs) cache, whose inspected generic storage is keyed by response type. BlazorConfig.Instance is also global. A new cache must not merely wrap these lifetimes and assume isolation is solved.

### Bounded proposal

1. Add a scoped/injectable lookup cache with explicit API authority and application-supplied identity/tenant namespace, bounded entries/TTL and invalidation. Use it in built-in lookup components; shared public lookup caching must be explicit.
2. Specify sign-in/sign-out/tenant-switch invalidation with BLAZOR-002. Deduplicate concurrent fetches safely and define whether cancelling one waiter cancels a shared fetch.
3. Document the static helper migration/compatibility path. Add a context-specific metadata provider or explicit metadata injection path first; changing the generic Client cache needs its owning project's participation and a separately agreed scope.

### Acceptance criteria if selected

- Two circuits/users, two API authorities and matching model IDs cannot consume each other's private labels; public-sharing opt-in is tested separately.
- Boundaries hold for concurrent fetch/update/invalidate, auth changes, missing labels, failed queries and eviction. Logs contain no private labels, credentials or tenant payloads by default.
- Server and WASM lifetimes are documented; explicitly cached data is invalidated or partitioned correctly on identity changes.
- Existing lookup UX and explicit AppMetadata usage continue to work. Tests use deterministic fake clients and real scoped DI containers.

### Alternatives and compatibility

An app can currently render labels directly in row DTOs or avoid cached lookups. Preserve these options. Removing all static APIs outright would break applications; keeping component defaults process-global would fail the intended isolation outcome. Maintainers must select an explicit transition policy rather than hide a new global singleton behind DI.

## BLAZOR-002: Make auth state transitions explicit and testable

- **Status:** Proposed. **Owner:** ServiceStack.Blazor's WASM provider. **Participants:** core ServiceStack/Extensions only for coordinated server/Identity validation; do not duplicate their providers here.
- **Value:** Keep navigation, EnsureAccess and metadata-driven UI consistent with the current sign-in/sign-out state.
- **Effort/confidence:** Small–medium. A stale-state path is reproduced; browser/session expiration and multi-tab behavior still need validation.

### Existing behavior and evidence

[BlazorWasmAuthenticationStateProvider](../ServiceStackAuthenticationStateProvider.cs) already implements auth probing, SignInAsync, LogoutAsync and notifications. After SignInAsync, a fake HTTP handler returning success for logout left both IsAuthenticated and GetAuthenticationStateAsync's ClaimsPrincipal authenticated. Logout currently clears errors while [ApiResult.ClearErrors](../../../../ServiceStack/src/ServiceStack.Client/ApiResult.cs) does not clear Response. This is UI state behavior; server API authorization remains a separate enforcement boundary. Existing [role/permission checks](../AuthBlazorComponentBase.cs) are not a substitute for invalidating the provider's cached identity.

### Bounded proposal

- Define authenticated/anonymous/loading/refresh outcomes and a refresh/invalidate API. Clear cached identity on confirmed logout, notify after the transition and prevent older auth requests from restoring a superseded user.
- Coalesce initialization checks and define failed-logout/offline behavior explicitly. Preserve server errors rather than report a successful logout when the session could not be ended.
- Coordinate identity/cache invalidation with BLAZOR-001. Cross-tab sign-out can be a later opt-in phase; do not persist bearer tokens or private session objects in browser storage as a shortcut.

### Acceptance criteria if selected

- Sign-in → successful logout yields an anonymous principal and IsAuthenticated=false; a subsequent auth check follows the agreed refresh policy.
- Failed logout, expired sessions, overlapping refresh/sign-in/logout, missing optional claim values and user switching have deterministic tests and notification ordering.
- Existing provider aliases and application overrides remain compatible or receive explicit migration guidance; Server/Identity recipes identify the owning provider.

### Alternatives and compatibility

Applications may already use ASP.NET Core Identity or a custom AuthenticationStateProvider. Continue to support them. Do not broaden this proposal into replacing server authentication, token refresh protocols or mandatory cross-tab infrastructure.

## BLAZOR-003: Cancellable component requests and stale-result protection

- **Status:** Proposed. **Owner:** ServiceStack.Blazor. **Participants:** Client/core only where an existing transport contract cannot carry the token.
- **Value:** Fast filter/navigation changes should display the newest requested state and stop work for disposed components.
- **Effort/confidence:** Medium. The missing component-level token/result ownership is source-confirmed; exact races are not runtime-reproduced in this pass.

### Existing behavior and evidence

[BlazorComponentBase](../BlazorComponentBase.cs) and [managed request helpers](../BlazorUtils.cs) invoke existing gateway/client operations without public token overloads. The underlying [IServiceGateway async contract](../../../../ServiceStack/src/ServiceStack.Interfaces/IServiceGateway.cs) and [JsonApiClient](../../../../ServiceStack/src/ServiceStack.Client/JsonApiClient.cs) already support cancellation; this is not a proposal to invent async HTTP support.

[AutoQueryGrid.SearchAsync](../Components/Tailwind/AutoQueryGrid.razor.cs) deduplicates a last-query marker, assigns the awaited response and invokes DataLoaded without an observed request-generation check. Parameter changes, navigation and first-render preferences can trigger additional loads. [ModalLookup](../Components/Tailwind/ModalLookup.razor.cs) has similar query work. Autocomplete is currently local; remote option fetching belongs to BLAZOR-006.

### Bounded proposal

- Add backward-compatible token-aware component/helper entry points and scoped request lifetimes; forward tokens through HTTP and in-process paths while preserving gateway cloning/error hooks.
- Cancel superseded grid/lookup queries, assign results/errors only for the current generation and suppress callbacks/renders after disposal.
- Make query fingerprinting include effective ConfigureQuery mutations; a failed request must not permanently suppress a valid retry. Define loading/empty/error states explicitly.

### Acceptance criteria if selected

- A slow query A completing after B cannot overwrite B's rows/error/selection or fire a stale DataLoaded callback.
- Navigation/disposal/refresh cancellation closes the component lifetime without reusing disposed JS references or turning expected cancellation into an error toast.
- Tests cover failure/retry, repeated parameters, custom query mutations and both fake gateway and HTTP transports. Mutation cancellation never claims that the server rolled back a dispatched write.

### Alternatives and compatibility

Apps can serialize requests or wrap a custom component today. Preserve old virtual overrides and tokenless calls, and assess overload ambiguity. Do not add automatic retries for create/update/delete requests as part of cancellation work.

## BLAZOR-004: Accessible overlays, comboboxes and keyboard lifetimes

- **Status:** Proposed. **Owner:** ServiceStack.Blazor. **Participant:** core ServiceStack owns the companion JS file.
- **Value:** Make dialogs, lookup selection and generated CRUD workflows predictable for keyboard and assistive-technology users.
- **Effort/confidence:** Medium, phased by component. Source observations are concrete; this review did not perform a browser or accessibility conformance audit.

### Existing behavior and evidence

[Autocomplete](../Components/Tailwind/Autocomplete.razor.cs) already handles navigation keys and paste; [markup](../Components/Tailwind/Autocomplete.razor) includes combobox/listbox roles. Its aria-controls literal does not match the listbox ID expression, and aria-expanded is static. [ModalDialog](../Components/Tailwind/ModalDialog.razor) and [SlideOver](../Components/Tailwind/SlideOver.razor) have dialog markup/transitions; a unified focus-trap/restore lifecycle was not located.

[AutoQueryGrid](../Components/Tailwind/AutoQueryGrid.razor.cs) registers JS key navigation and disposes its DotNetObjectReference. The companion [servicestack-blazor.js](../../../../ServiceStack/src/ServiceStack/js/servicestack-blazor.js) already exposes disposeKeyNav; the inspected component does not call that removal helper. The gap is ownership and integration, not absence of any JS cleanup mechanism.

### Bounded proposal and validation

- Correct dynamic IDs/states and implement per-instance initial focus, focus containment/restore, Escape handling and background-interaction policy for modal overlays. Align with the [W3C combobox](https://www.w3.org/WAI/ARIA/apg/patterns/combobox/) and [modal-dialog](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/) patterns; ARIA markup alone does not establish accessible behavior.
- Pair JS registration/removal with component lifetime, including multiple grids, nested dialogs, repeated navigation and disconnected circuits. Avoid introducing a process-global component callback registry.
- Add focused component tests plus browser keyboard/focus tests for Tab/Shift+Tab, arrow keys, selection, dismissal, duplicate instances and focus restoration. Include representative manual screen-reader checks and document their limits; no blanket compliance claim follows from an automated scan.

### Alternatives and compatibility

Developers can supply custom create/edit forms and overlay controls. Preserve CSS/fragment customization. Assess native dialog or a small shared focus primitive before adding a large UI dependency; companion-script changes require core participation and compatible asset deployment.

## BLAZOR-005: Bounded cancellable uploads with accurate status

- **Status:** Proposed. **Owner:** ServiceStack.Blazor. **Participants:** Client for any transport progress hook, core ServiceStack for existing FilesUploadFeature metadata/validation.
- **Value:** Keep larger uploads responsive, bounded and truthful about success/failure without replacing the server's file security policy.
- **Effort/confidence:** Medium. Buffering/limit/status differences are source-confirmed; browser memory, throughput and failure cases were not measured.

### Existing behavior and evidence

[AutoFormBase](../Components/AutoFormBase.cs) already submits multipart file streams using server upload metadata or 50 MiB/100-file defaults. [FileInput](../Components/Tailwind/FileInput.razor.cs) handles selection/preview. Standalone [FileUpload](../Components/Tailwind/FileUpload.razor.cs) uses a fixed 15 MiB limit, copies the selected file into MemoryStream and reports text stages. After an unsuccessful ApiResult it records errorStatus but still sets “Upload complete!” and invokes OnUploadComplete. Do not describe all existing upload paths as fully buffered or without limits.

### Bounded proposal

1. Expose explicit size/count/accept policies, reconcile standalone defaults with applicable metadata, and make success/error/cancel callbacks/state distinct. Server validation remains authoritative.
2. Forward cancellation and dispose file/multipart resources consistently; remove unnecessary standalone whole-file buffering where supported. Report byte progress only when the chosen browser/transport path can measure it accurately.
3. Keep chunked/resumable uploads, direct cloud credentials and automatic upload retry outside the first scope. Those need server protocol/security decisions.

### Acceptance criteria if selected

- Failed/denied/oversize uploads cannot present success or fire a success-only callback. Clarify backward compatibility of existing OnUploadComplete semantics.
- Cancel/dispose during reading or sending stops UI updates and releases streams; unknown server write outcomes are documented.
- Tests cover zero-byte files, same-file reselection, multiple files, metadata limits, fake stream failures, server validation and supported HTTP/gateway modes. Profile memory before promising large-file scalability.

### Alternatives and compatibility

Continue supporting custom form templates and standard InputFile/MultipartFormDataContent. Limit improvements to this package's UX/lifecycle; server storage, content scanning, access roles and URL authorization remain Server/application responsibilities.

## BLAZOR-006: Optional data providers and virtualized result windows

- **Status:** Proposed, gated on real workload measurements. **Owner:** ServiceStack.Blazor. **Participants:** Server only if a separately justified paging contract is needed.
- **Value:** Browse larger query results or remote option sets without materializing/rendering everything at once.
- **Effort/confidence:** Medium–large. Collection-based rendering and local autocomplete are verified; performance/customer value are hypotheses.

### Existing behavior and evidence

[AutoQueryGrid](../Components/Tailwind/AutoQueryGrid.razor.cs) already pages server results with Skip/Take/total counts; [DataGrid](../Components/Tailwind/DataGrid.razor) renders Items with foreach and keys rows by GetHashCode. [Autocomplete](../Components/Tailwind/Autocomplete.razor.cs) accepts Options/Match and grows the locally displayed window. No built-in ItemsProvider/Virtualize or cancellable remote-options delegate was located in the inspected package. Do not propose server pagination itself as new.

### Bounded proposal

- Define an optional provider request/result containing query state, window, total information, stable row identity and cancellation. Keep existing collection and page-based APIs as defaults.
- First add a remote autocomplete/options provider with debounce/latest-result rules from BLAZOR-003; assess whether it can share query ownership without over-generalizing the contract.
- Prototype opt-in row virtualization only after render/DOM measurements. Use the framework's [virtualization/provider model](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/virtualization?view=aspnetcore-10.0); preserve custom templates, responsive columns, paging fallbacks and selection by stable key.

### Acceptance criteria if selected

- Publish reproducible render/DOM/allocation measurements for representative local and AutoQuery datasets before claiming improvement.
- Sorting/filter changes invalidate windows, late/cancelled loads cannot replace current data, key collisions cannot alias rows and selected records survive a window refresh by identity.
- Test keyboard/focus/scroll behavior with custom row heights and both Server/WASM latency; document unsupported layouts. Start with existing offset paging, not a promise of cheap deep paging or a new keyset server engine.

### Alternatives and compatibility

Existing pagination may already be sufficient. A custom grid or third-party virtualized component can be used through app composition. Avoid shipping a second general-purpose grid engine without evidence that the provider adapter is insufficient.

## BLAZOR-007: Explicit render-mode and prerender initialization contract

- **Status:** Proposed. **Owner:** ServiceStack.Blazor. **Participants:** core/Extensions for server client/Identity initialization; template owners for examples.
- **Value:** Reduce duplicate loads and JS initialization failures when an application transitions from prerendered HTML to an interactive component.
- **Effort/confidence:** Medium. Source lifecycle/storage behavior is inspected; no full hosting-mode matrix was executed.

### Existing behavior and evidence

[UiComponentBase](../Components/UiComponentBase.cs) already queues JS render actions per instance. [AutoQueryGrid](../Components/Tailwind/AutoQueryGrid.razor.cs) fetches metadata during initialization and queries during parameter changes/after-render preference loading. [LocalStorage](../LocalStorage.cs) delegates to JS and components inject concrete storage helpers even though BlazorConfig has a UseLocalStorage setting. Gallery/template sources demonstrate hosting integration; they do not establish seamless static-only rendering or every render-mode transition for each component.

### Bounded proposal

- Document/component-test an explicit initialization sequence: metadata readiness, prerender-safe defaults, browser storage availability, first interactive query and JS registration/disposal. Handle unavailable/corrupt storage without silently sharing a new static cache.
- Add optional state handoff/adapters where needed, using [framework prerendered state persistence](https://learn.microsoft.com/en-us/aspnet/core/blazor/state-management/prerendered-state-persistence?view=aspnetcore-10.0). Gate version-specific integration to its supported targets and preserve net6/net8 behavior; do not assume the net10 enhanced-navigation contract applies everywhere.
- Persist only explicitly allowed data. Authentication tokens, private sessions and arbitrary user metadata must not be embedded in prerendered browser-visible state. Resolve BLAZOR-001 lifetimes first.

### Acceptance criteria if selected

- A documented grid/form subset is exercised under supported interactive Server/WASM modes, with prerender on/off and applicable enhanced navigation. Tests assert query counts, first load, auth changes and teardown.
- Components handle blocked storage and disconnected JS predictably. Static-only read-only output and interactive CRUD requirements are documented separately.
- Include runnable examples using actual owning registration APIs. Keep MAUI/Hybrid support unclaimed until a dedicated host fixture exists.

### Alternatives and compatibility

Apps can supply AppMetadata/preferences explicitly, disable prerender per component or delay UI until interactive. Recipes may solve the need without new package APIs. Preserve application-controlled hosting policy and avoid broad framework-version upgrades as an implicit requirement.

## BLAZOR-008: Generated form edit state and unsaved-change guards

- **Status:** Proposed. **Owner:** ServiceStack.Blazor. **Participants:** applications/server DTO owners for optional conflict information.
- **Value:** Avoid accidental loss of edits and make generated fields compose naturally with application validation workflows.
- **Effort/confidence:** Medium. Existing model dictionaries/callbacks/patch behavior are confirmed; demand and the final edit-state contract need validation.

### Existing behavior and evidence

[AutoFormBase](../Components/AutoFormBase.cs) retains OriginalModelDictionary and ModelDictionary and computes changed/reset fields for patch submission. [AutoCreateForm](../Components/Tailwind/AutoCreateForm.razor.cs) and [AutoEditForm](../Components/Tailwind/AutoEditForm.razor.cs) initialize them during parameter updates; markup uses HTML form submission. [TextInputBase](../Components/TextInputBase.cs) already has validation-state/EditContext hooks, and AutoFormFields can be embedded in custom forms. A package-level dirty-state/close-guard lifecycle was not located. This is not a proposal to add all validation from scratch.

### Bounded proposal

- Expose explicit dirty/valid/submitting state, before-close/navigation decisions, reset/revert and successful-save baseline updates. Make guards opt-in and support asynchronous app decisions.
- Define an EditContext/ValidationMessageStore adapter for dictionary/metadata models and server ResponseStatus errors where it removes application glue. Reuse the framework's [form validation primitives](https://learn.microsoft.com/en-us/aspnet/core/blazor/forms/validation?view=aspnetcore-10.0).
- Prevent repeated submit/parameter refresh from unintentionally losing local edits. Display conflict errors when an existing API returns them; do not add server concurrency tokens, optimistic locking or draft persistence implicitly.

### Acceptance criteria if selected

- Test edit/revert/save/validation-fail/cancel transitions, navigation/overlay dismissal, file selections, nullable reset semantics and parameter refresh while dirty.
- Preserve custom FormLayout, Save/Delete/Error callbacks, AutoSave=false and embedded fields. Dirty-state calculation must account for mutable/nested values rather than assume shallow dictionary equality is sufficient.
- Browser tests verify focus/error announcements with BLAZOR-004. Draft storage, if later selected, needs a separate sensitive-data/identity/expiry design.

### Alternatives and compatibility

Custom create/edit fragments and AutoFormFields already allow app-owned EditForm workflows. Prefer documented composition if an adapter plus a close hook meets the need; avoid replacing all existing forms with a new mandatory state engine.

## Deferred or externally owned ideas

- Named/shared query views, multi-row bulk actions and end-user dashboards need customer workflow evidence; existing preferences/filter views are the baseline.
- Generic metadata-cache redesign is owned with ServiceStack.Client; Blazor-specific consumption/identity integration is captured in BLAZOR-001 without duplicating a generic cache-engine proposal.
- New database query operators, server keyset paging, optimistic concurrency enforcement, storage providers and resumable upload protocols require Server/application ownership, not UI-only claims.
- New Bootstrap parity, MAUI support, offline mutation replay, AI-generated component code and a complete design-system rewrite are not recommended merely because they were not found here. The existing source, templates and tool ownership need separate justification.

## Decision handling

After explicit acceptance, move the full bounded proposal to [FEATURE-IMPROVEMENTS.md](FEATURE-IMPROVEMENTS.md) under the same ID, retaining the baseline/evidence and recording scope, owner, decision/date, migration, tests and status. Leave a link here. Participant projects link to the canonical record; they do not duplicate it. Source presence, validated behavior and released availability remain distinct.
