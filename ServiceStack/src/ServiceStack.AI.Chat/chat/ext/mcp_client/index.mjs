// Shared MCP client UI. Python is the source of truth; sync.sh copies this file verbatim.
import { ref, computed, inject, onMounted, onUnmounted, watch, nextTick } from 'vue'

async function request(ctx, path, method = 'GET', body) {
    const response = await fetch(`${ctx.ai.base}/ext/mcp_client/${path}`, {
        method, credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json', 'X-Mcp-Client': '1' },
        body: body == null ? undefined : JSON.stringify(body)
    })
    const data = await response.json()
    if (!response.ok) throw new Error(data.responseStatus?.message || data.error?.message || 'MCP request failed')
    return data
}

const buttonClass = 'inline-flex min-h-9 items-center justify-center gap-1.5 px-3 py-1.5 text-sm font-medium leading-5 whitespace-nowrap cursor-pointer transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-500 disabled:cursor-not-allowed disabled:opacity-50'

const JsonBlock = {
    props: { text: String },
    template: `<div class="group relative mt-2 max-w-full">
        <button type="button" class="absolute right-2 top-2 z-10 rounded-md border border-slate-200 bg-white/95 px-2 py-1 text-[11px] font-medium text-slate-600 shadow-sm hover:text-slate-900 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-500 dark:border-gray-700 dark:bg-slate-800 dark:text-slate-200 dark:hover:text-white" @click="copy">{{ copied ? 'Copied' : 'Copy' }}</button>
        <div class="max-h-80 max-w-full overflow-auto rounded-lg border border-slate-200 bg-slate-50 px-3 py-2.5 font-mono text-xs leading-5 text-slate-800 dark:border-gray-700 dark:bg-slate-900 dark:text-slate-100"><div class="w-max min-w-full whitespace-pre pr-10" v-html="$utils.highlightJson(text || '')"></div></div>
    </div>`,
    setup(props) {
        const copied = ref(false)
        async function copy() {
            try {
                await navigator.clipboard.writeText(props.text || '')
                copied.value = true
                setTimeout(() => { copied.value = false }, 1200)
            } catch { /* The JSON remains selectable if clipboard access is unavailable. */ }
        }
        return { copied, copy }
    }
}

const Approval = {
    components: { JsonBlock },
    props: { thread: Object, tool: Object, output: Object },
    template: `<section class="box-border bg-[var(--background)] px-5 py-[18px] text-sm leading-relaxed text-[var(--assistant-text)] max-sm:p-3.5" aria-label="Remote tool approval">
        <p v-if="error" class="my-2.5 break-words text-amber-700 dark:text-amber-400" role="alert">{{ error }}</p>
        <template v-if="approval">
            <header class="flex items-start justify-between gap-4 max-sm:flex-wrap"><div><span class="text-xs font-semibold tracking-wider text-[var(--tw-prose-captions)] uppercase">MCP tool request</span><h3 class="my-1 text-base font-semibold text-[var(--heading)]">{{ approval.title }}</h3><p v-if="approval.description" class="text-sm text-[var(--tw-prose-captions)]">{{ approval.description }}</p></div><span class="shrink-0 whitespace-nowrap rounded-full border px-2 py-0.5 text-xs text-[var(--tw-prose-captions)] border-[var(--assistant-border)]">{{ statusLabel(approval.status) }}</span></header>
            <p class="mt-3 mb-4 text-xs text-[var(--tw-prose-captions)]">Connection: {{ approval.sourceMetadata?.serverId }}</p>
            <template v-if="approval.status === 'pending'">
                <p>Review the arguments sent to this server. You can edit them before approving.</p>
                <label class="mt-3.5 mb-1.5 block text-sm font-semibold" for="mcp-approval-args">Arguments</label>
                <textarea id="mcp-approval-args" v-model="args" rows="7" class="mcp-approval-json min-h-[140px] resize-y focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/20 block max-h-80 w-full overflow-auto rounded-lg border bg-[var(--secondary-bg)] px-3.5 py-3 font-mono text-xs leading-relaxed whitespace-pre-wrap break-words text-[var(--assistant-text)] border-[var(--assistant-border)]" spellcheck="false" :disabled="busy" />
                <div class="mt-3.5 flex flex-wrap items-center gap-2"><button class="${buttonClass}" :class="$styles.primaryButton" type="button" :disabled="busy" @click="decide('approve')">{{ busy ? 'Working…' : 'Approve and run' }}</button><button class="${buttonClass}" :class="$styles.secondaryButton" type="button" :disabled="busy" @click="decide('approve', true)">Always approve this tool</button><button class="${buttonClass}" :class="$styles.secondaryButton" type="button" :disabled="busy" @click="decide('reject')">Reject</button></div>
                <p class="mt-2 text-xs text-[var(--tw-prose-captions)]">Future calls to this tool run without asking, for any arguments. Remove the badge beside the tool in Connections to revoke.</p>
            </template>
            <div v-else-if="approval.status === 'outcome_unknown'" class="rounded-lg border border-amber-500/40 bg-amber-500/5 px-4 py-3.5" role="alert"><strong class="mb-1 block">The remote result is uncertain</strong><p class="mb-3">The request may have reached the server. Check the remote service before continuing; this action will never replay it.</p><button class="${buttonClass}" :class="$styles.primaryButton" type="button" :disabled="busy" @click="decide('reconcile')">{{ busy ? 'Continuing…' : 'I checked — continue without replay' }}</button></div>
            <p v-else-if="approval.status === 'executing'" class="rounded-lg border border-amber-500/40 bg-amber-500/5 px-4 py-3.5">The request is running. If its result cannot be confirmed, you will be asked to review it before continuing.</p>
            <p v-if="approval.error && approval.status !== 'outcome_unknown'" class="my-2.5 break-words text-amber-700 dark:text-amber-400" role="alert">{{ approval.error === 'access_denied' ? 'Access was denied. For an OAuth connection, open MCP Connections → Connection options and reauthorize, then grant the requested access at your provider.' : approval.error }}</p>
            <details class="mt-3.5"><summary class="cursor-pointer text-xs text-[var(--tw-prose-captions)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-500">Input schema</summary><JsonBlock :text="schemaJson" /></details>
            <details v-if="approval.effectiveArgs" open class="mt-3.5"><summary class="cursor-pointer text-xs text-[var(--tw-prose-captions)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-500">Sent arguments</summary><JsonBlock :text="sentArgsJson" /></details>
            <details v-if="approval.result !== null && approval.result !== undefined" class="mt-3.5"><summary class="cursor-pointer text-xs text-[var(--tw-prose-captions)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-500">Tool response</summary><JsonBlock :text="resultJson" /></details>
            <template v-for="(resource, index) in approval.result?.resources || []" :key="index"><img v-if="resource.type === 'image_url'" :src="resource.image_url.url" alt="Remote tool image" class="max-w-full" /><audio v-if="resource.type === 'audio_url'" :src="resource.audio_url.url" controls aria-label="Remote tool audio" /></template>
        </template>
        <JsonBlock v-else :text="fallbackJson" />
    </section>`,
    setup(props) {
        const ctx = inject('ctx'), approval = ref(null), args = ref('{}'), error = ref(''), busy = ref(false)
        async function refresh() {
            if (!props.thread?.id) return
            try {
                const rows = await request(ctx, `approvals/${props.thread.id}`)
                approval.value = rows.find(x => x.toolCallId === props.tool?.id && x.source === 'mcp_client')
                args.value = JSON.stringify(approval.value?.effectiveArgs || approval.value?.proposedArgs || {}, null, 2)
            } catch (e) { error.value = e.message }
        }
        async function decide(action, alwaysApprove = false) {
            busy.value = true; error.value = ''
            try {
                const body = action === 'approve' ? { args: JSON.parse(args.value), alwaysApprove }
                    : action === 'reconcile' ? { decision: 'continue_without_replay' } : { reason: 'User declined the remote call' }
                await request(ctx, `approvals/${approval.value.id}/${action}`, 'POST', body)
                await refresh()
            } catch (e) { error.value = e.message }
            finally { busy.value = false }
        }
        onMounted(refresh); watch(() => props.thread?.status, refresh)
        function displayResult(result) {
            if (result && typeof result === 'object') { const { resources, ...rest } = result; return JSON.stringify(rest, null, 2) }
            return formatJson(result)
        }
        function formatJson(value) {
            if (typeof value === 'string') {
                try { return JSON.stringify(JSON.parse(value), null, 2) } catch { return value }
            }
            return JSON.stringify(value ?? {}, null, 2)
        }
        const schemaJson = computed(() => formatJson(approval.value?.schema || {}))
        const sentArgsJson = computed(() => formatJson(approval.value?.effectiveArgs || {}))
        const resultJson = computed(() => displayResult(approval.value?.result))
        const fallbackJson = computed(() => formatJson(props.output?.content ?? props.tool?.function?.arguments ?? {}))
        const statusLabel = status => ({ pending: 'Needs approval', executing: 'Running', outcome_unknown: 'Needs review', completed: 'Completed', failed: 'Failed', rejected: 'Rejected' }[status] || status)
        return { approval, args, error, busy, decide, schemaJson, sentArgsJson, resultJson, fallbackJson, statusLabel }
    }
}

async function registerTools(ctx) {
    const response = await fetch(`${ctx.ai.base}/ext/tools`, { credentials: 'same-origin' })
    if (!response.ok) return
    const catalog = await response.json()
    if (ctx.state.tool) {
        ctx.state.tool.groups = catalog.groups
        ctx.state.tool.definitions = catalog.definitions
    }
    ctx.setToolCallBodies(Object.fromEntries(catalog.definitions
        .filter(x => x.function?.name?.startsWith('mcp_'))
        .map(x => [x.function.name, { component: Approval, autoExpand: ({ thread }) => thread?.status?.startsWith('Approval required') }])))
}

const Connections = {
    template: `<section class="rounded-lg shadow-sm border border-gray-200 dark:border-gray-800 mt-3 box-border bg-[var(--background)] p-6 text-sm leading-relaxed text-[var(--assistant-text)] max-sm:p-4" aria-label="MCP connections">
      <header class="mb-6 flex items-center justify-between gap-5 max-sm:flex-wrap max-sm:items-start max-sm:gap-3.5">
        <div><h2 class="text-lg font-semibold text-[var(--heading)]">{{ editor ? (editingId ? 'Connection settings' : 'Add connection') : 'MCP connections' }}</h2><p class="mt-1 text-sm text-[var(--tw-prose-captions)]">{{ editor ? 'Connect a service to use its tools in chat.' : 'Connect your services, then choose their tools for a conversation.' }}</p></div>
        <div v-if="!editor" class="flex flex-wrap items-center gap-2">
          <button class="${buttonClass.replace('px-3 py-1.5', 'size-9 px-2')}" :class="$styles.secondaryButton" type="button" :disabled="busy || loading" @click="reload" title="Refresh connections" aria-label="Refresh connections">
              <svg class="size-9" viewBox="0 0 24 24"><path d="M0 0h24v24H0z" fill="none" /><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 0 0 4.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 0 1-15.357-2m15.357 2H15" /></svg>
          </button>
          <button v-if="connections.length" class="${buttonClass}" :class="$styles.secondaryButton" type="button" :disabled="busy || loading || !connections.some(c => !c.disabled)" @click="connectAll">{{ activeAction === 'connect-all' ? 'Connecting…' : 'Connect all' }}</button>
          <button v-if="config.canEdit" class="${buttonClass}" :class="$styles.primaryButton" type="button" :disabled="busy || loading" @click="edit()">＋ Add connection</button>
        </div>
      </header>
      <div v-if="error" class="mb-5 rounded-lg border border-amber-500/40 bg-amber-500/5 px-4 py-3.5 break-words [&_button]:mt-2.5" role="alert"><p>{{ error }}</p><button v-if="connectionIssue && !editor" type="button" class="${buttonClass}" :class="$styles.secondaryButton" @click="edit(connectionIssue)">Edit connection</button></div>
      <p v-if="notice && !error" class="mb-4 text-sm text-[var(--assistant-text)]" role="status">{{ notice }}</p>
      <p v-if="loading" class="p-8 text-center text-[var(--tw-prose-captions)]" role="status">Loading connections…</p>
      <form v-if="editor" @submit.prevent="save" class="mx-auto max-w-[560px] [&_fieldset]:min-w-0 [&_fieldset]:border-0 [&_fieldset]:p-0" aria-label="Edit MCP connection">
        <p v-if="restoreTools" class="mb-5 rounded-lg border border-amber-500/40 bg-amber-500/5 px-4 py-3 text-sm">Saving will allow all tools discovered by this server. You may need to authenticate again. For a Bearer connection, re-enter your token below.</p>
        <fieldset :disabled="busy">
          <div class="grid gap-4">
            <label class="flex flex-col gap-1.5 text-sm font-medium">Name<input class="h-10 w-full rounded-md border px-3 py-2 text-[var(--assistant-text)] placeholder:text-[var(--tw-prose-captions)] focus:outline-none focus:ring-2 focus:ring-blue-500/30" :class="[$styles.bgInput, $styles.borderInput]" ref="nameInput" v-model="editor.displayName" required placeholder="e.g. GitHub" autocomplete="off" /></label>
            <label class="flex flex-col gap-1.5 text-sm font-medium">Server URL<input class="h-10 w-full rounded-md border px-3 py-2 text-[var(--assistant-text)] placeholder:text-[var(--tw-prose-captions)] focus:outline-none focus:ring-2 focus:ring-blue-500/30" :class="[$styles.bgInput, $styles.borderInput]" v-model="editor.endpoint" type="url" required placeholder="https://example.com/mcp" autocomplete="off" spellcheck="false" /></label>
            <label class="flex flex-col gap-1.5 text-sm font-medium">Authentication<select class="h-10 w-full rounded-md border px-3 py-2 text-[var(--assistant-text)] placeholder:text-[var(--tw-prose-captions)] focus:outline-none focus:ring-2 focus:ring-blue-500/30" :class="[$styles.bgInput, $styles.borderInput]" v-model="editor.auth.mode"><option value="anonymous">None</option><option value="bearer">Bearer token / personal access token</option><option value="user_oauth">OAuth</option></select></label>
            <label v-if="editor.auth.mode === 'bearer'" class="flex flex-col gap-1.5 text-sm font-medium">Bearer token<input class="h-10 w-full rounded-md border px-3 py-2 text-[var(--assistant-text)] placeholder:text-[var(--tw-prose-captions)] focus:outline-none focus:ring-2 focus:ring-blue-500/30" :class="[$styles.bgInput, $styles.borderInput]" v-model="bearerToken" type="password" :required="!editingId || restoreTools" autocomplete="new-password" :placeholder="restoreTools ? 'Re-enter your token to reconnect' : editingId ? 'Leave blank to keep your saved token' : 'Paste your token'" spellcheck="false" /><small class="text-xs font-normal leading-relaxed text-[var(--tw-prose-captions)]">{{ editingId ? 'Enter a new token to replace it. Changes to connection settings may require a new token.' : 'Paste the token without the “Bearer” prefix. Saved on the server.' }}</small></label>
            <template v-if="editor.auth.mode === 'user_oauth'">
              <p class="text-[var(--tw-prose-captions)]">Register an OAuth App with your provider using this callback URL: <br><code class="break-all">{{ oauthRedirectUri || 'Configure a public HTTPS MCP OAuth callback URL on this host.' }}</code></p>
              <label class="flex flex-col gap-1.5 text-sm font-medium">Client ID<input class="h-10 w-full rounded-md border px-3 py-2 text-[var(--assistant-text)] placeholder:text-[var(--tw-prose-captions)] focus:outline-none focus:ring-2 focus:ring-blue-500/30" :class="[$styles.bgInput, $styles.borderInput]" v-model="editor.oauthClientId" required autocomplete="off" /></label>
              <label class="flex flex-col gap-1.5 text-sm font-medium">Client secret <span class="text-xs font-normal text-[var(--tw-prose-captions)]">Required by GitHub</span><input class="h-10 w-full rounded-md border px-3 py-2 text-[var(--assistant-text)] placeholder:text-[var(--tw-prose-captions)] focus:outline-none focus:ring-2 focus:ring-blue-500/30" :class="[$styles.bgInput, $styles.borderInput]" v-model="oauthClientSecret" type="password" :required="isGitHubOAuth(editor.endpoint) && !editingId" autocomplete="new-password" :placeholder="editingId ? 'Leave blank to keep the saved secret' : 'Paste the client secret if required'" spellcheck="false" /><small class="text-xs font-normal leading-relaxed text-[var(--tw-prose-captions)]">Saved on the server, never in config.json. Re-enter it if you change this connection.</small></label>
              <label v-if="issuerChoices.length > 1 && !manualIssuer" class="flex flex-col gap-1.5 text-sm font-medium">Sign-in provider<select class="h-10 w-full rounded-md border px-3 py-2 text-[var(--assistant-text)]" :class="[$styles.bgInput, $styles.borderInput]" v-model="selectedIssuer" required><option value="" disabled>Choose a provider</option><option v-for="issuer in issuerChoices" :key="issuer" :value="issuer">{{ issuer }}</option></select><small class="text-xs font-normal leading-relaxed text-[var(--tw-prose-captions)]">This server offers more than one provider. Choose the one where you registered your OAuth App.</small></label>
              <details class="rounded-lg border border-[var(--assistant-border)] px-3 py-2 text-sm"><summary class="cursor-pointer text-[var(--tw-prose-captions)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-500">Advanced OAuth settings</summary><div class="mt-3 grid gap-4">
                <label class="flex flex-col gap-1.5 font-medium">Scopes <span class="text-xs font-normal text-[var(--tw-prose-captions)]">Optional</span><input class="h-10 w-full rounded-md border px-3 py-2 text-[var(--assistant-text)] focus:outline-none focus:ring-2 focus:ring-blue-500/30" :class="[$styles.bgInput, $styles.borderInput]" v-model="scopes" placeholder="tools.read, tools.write" /><small class="text-xs font-normal leading-relaxed text-[var(--tw-prose-captions)]">Fallback scopes, used only if the MCP server does not advertise any. Separate with commas.</small></label>
                <label class="flex flex-col gap-1.5 font-medium">Issuer URL <span class="text-xs font-normal text-[var(--tw-prose-captions)]">Optional override</span><input class="h-10 w-full rounded-md border px-3 py-2 text-[var(--assistant-text)] focus:outline-none focus:ring-2 focus:ring-blue-500/30" :class="[$styles.bgInput, $styles.borderInput]" v-model="manualIssuer" type="url" placeholder="Discovered automatically" autocomplete="off" spellcheck="false" /><small class="text-xs font-normal leading-relaxed text-[var(--tw-prose-captions)]">Only needed if this server does not publish sign-in provider discovery.</small></label>
              </div></details>
            </template>
          </div>
        </fieldset>
        <footer class="mt-6 flex items-center justify-between gap-4 border-t pt-5 max-sm:flex-wrap border-[var(--assistant-border)]"><small class="text-xs text-[var(--tw-prose-captions)]">New tool calls ask for approval until you always approve a tool.</small><div class="flex flex-wrap items-center gap-2"><button class="${buttonClass}" :class="$styles.secondaryButton" type="button" :disabled="busy" @click="cancelEdit">Cancel</button><button class="${buttonClass}" :class="$styles.primaryButton" type="submit" :disabled="busy">{{ busy ? 'Connecting…' : editingId ? 'Save & connect' : 'Connect' }}</button></div></footer>
      </form>
      <template v-else-if="!loading">
        <div v-if="!connections.length" class="rounded-lg border border-dashed px-4 pt-8 pb-10 text-center border-[var(--assistant-border)]">
          <svg class="mx-auto mb-3.5 size-[30px] text-[var(--tw-prose-captions)]" viewBox="0 0 384 512"><path d="M0 0h384v512H0z" fill="none" /><path fill="currentColor" d="M320 32a32 32 0 0 0-64 0v96h64Zm48 128H16a16 16 0 0 0-16 16v32a16 16 0 0 0 16 16h16v32a160.07 160.07 0 0 0 128 156.8V512h64v-99.2A160.07 160.07 0 0 0 352 256v-32h16a16 16 0 0 0 16-16v-32a16 16 0 0 0-16-16M128 32a32 32 0 0 0-64 0v96h64Z" /></svg>
          <h3 class="text-sm font-semibold text-[var(--heading)]">No connections yet</h3><p class="mx-auto mt-1.5 max-w-[360px] text-sm text-[var(--tw-prose-captions)]">{{ config.canEdit ? 'Add an MCP server to bring tools from your services into chat.' : 'Your administrator manages the connections available to you.' }}</p>
        </div>
        <div v-else class="divide-y divide-[var(--assistant-border)] border-t border-[var(--assistant-border)]">
          <article v-for="connection in connections" :key="connection.id" class="py-4 last:pb-0">
            <div class="flex items-center gap-3.5 max-sm:flex-wrap max-sm:gap-2.5">
              <div class="grid size-[38px] shrink-0 place-items-center rounded-lg border bg-[var(--assistant-bg)] text-[17px] font-semibold max-sm:size-9 border-[var(--assistant-border)]" aria-hidden="true">{{ connection.name.slice(0, 1).toUpperCase() }}</div>
              <div class="min-w-0 flex-1 max-sm:basis-[calc(100%_-_50px)]"><h3 class="break-words text-sm font-semibold text-[var(--heading)]">{{ connection.name }} <span v-if="connection.scope !== 'personal'" class="ml-1 rounded-sm border px-1 py-px text-[10px] font-normal border-[var(--assistant-border)]">Shared</span></h3><p class="text-[var(--tw-prose-captions)] mt-0.5 truncate text-xs" :title="endpointLabel(connection)">{{ endpointLabel(connection) }}</p></div>
              <span class="whitespace-nowrap text-xs text-[var(--tw-prose-captions)] flex items-center" ><span aria-hidden="true" class="mr-1 text-[9px]" :class="connection.state === 'ready' ? 'text-green-500' : ['degraded', 'error', 'auth_required', 'unsupported'].includes(connection.state) ? 'text-amber-500' : ''">●</span> {{ connection.disabled ? 'Disabled' : stateLabel(connection.state) }}</span>
              <div class="flex flex-wrap items-center gap-2">
                <button v-if="connection.state !== 'ready'" class="${buttonClass}" :class="$styles.secondaryButton" type="button" :disabled="busy" @click="act(connection, 'connect')">{{ activeAction === connection.id + ':connect' ? 'Connecting…' : connection.disabled ? 'Enable & connect' : 'Connect' }}</button>
                <button v-if="connection.state === 'ready'" class="${buttonClass}" :class="$styles.secondaryButton" type="button" :disabled="busy" :aria-expanded="expandedTools === connection.id" @click="showTools(connection)">{{ activeAction === connection.id + ':tools' ? 'Loading…' : expandedTools === connection.id ? 'Hide tools' : 'Tools' }}</button>
                <button v-if="connection.scope === 'personal' && config.canEdit" class="${buttonClass}" :class="$styles.secondaryButton" type="button" :disabled="busy" @click="edit(connection.id)">Settings</button>
              </div>
            </div>
            <div v-if="signIn?.id === connection.id" class="mt-4 flex flex-wrap items-center justify-between gap-4 rounded-lg border border-blue-200 bg-blue-50/70 p-4 dark:border-blue-500/30 dark:bg-blue-500/10" role="status">
              <div class="min-w-0"><p class="text-sm font-semibold text-[var(--heading)]">Authorize {{ connection.name }}</p><p class="mt-1 text-xs leading-relaxed text-[var(--tw-prose-captions)]">Open the provider's sign-in page. When you're done, return here to load its tools.</p></div>
              <div class="flex flex-wrap items-center gap-2"><a class="${buttonClass} rounded-md bg-blue-600 text-white hover:bg-blue-700 focus-visible:outline-blue-500 dark:bg-blue-500 dark:hover:bg-blue-600" :href="signIn.url" target="_blank" rel="noopener noreferrer">Open sign-in <svg class="size-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true"><path d="M13 5h6v6m0-6-9 9" stroke-linecap="round" stroke-linejoin="round"/><path d="M19 13v6H5V5h6" stroke-linecap="round" stroke-linejoin="round"/></svg></a><button class="${buttonClass}" :class="$styles.secondaryButton" type="button" :disabled="busy" @click="act(connection, 'refresh')">{{ activeAction === connection.id + ':refresh' ? 'Checking…' : 'Load tools' }}</button></div>
            </div>
            <details class="mt-3 text-xs text-[var(--tw-prose-captions)]"><summary class="w-fit cursor-pointer focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-500">Connection options</summary><div class="mt-3 flex flex-wrap items-center gap-2">
              <button class="${buttonClass}" :class="$styles.secondaryButton" type="button" :disabled="busy" @click="act(connection, 'refresh')" v-if="!connection.disabled">Refresh tools</button>
              <button v-if="connection.accountMode === 'user_oauth' && !connection.disabled" class="${buttonClass}" :class="$styles.secondaryButton" type="button" :disabled="busy" @click="act(connection, 'connect')">Reauthorize</button>
              <button v-if="!connection.disabled" class="${buttonClass}" :class="$styles.secondaryButton" type="button" :disabled="busy" @click="act(connection, 'disconnect')">Disable</button>
              <button v-if="['user_oauth', 'bearer'].includes(connection.accountMode)" class="${buttonClass}" :class="$styles.secondaryButton" type="button" :disabled="busy" @click="removing = connection.id; removalKind = 'credentials'">Clear saved credentials</button>
              <button v-if="connection.scope === 'personal' && config.canEdit" class="${buttonClass} rounded-md border border-[var(--assistant-border)] bg-[var(--background)] text-red-600 hover:bg-red-50 dark:text-red-400 dark:hover:bg-red-950/30" type="button" :disabled="busy" @click="removing = connection.id; removalKind = 'connection'">Remove connection</button>
            </div></details>
            <div v-if="removing === connection.id" class="mt-3.5 rounded-lg border border-amber-500/40 bg-amber-500/5 px-4 py-3.5 break-words [&>div]:mt-2.5" role="alert"><p>{{ removalKind === 'credentials' ? connection.accountMode === 'user_oauth' ? 'Clear this account’s sign-in? You can authorize it again; the OAuth App client secret stays saved.' : 'Clear saved credentials? You will need to authenticate again.' : 'Remove this connection and its saved credentials?' }}</p><div class="flex flex-wrap items-center gap-2"><button class="${buttonClass} rounded-md border border-[var(--assistant-border)] bg-[var(--background)] text-red-600 hover:bg-red-50 dark:text-red-400 dark:hover:bg-red-950/30" type="button" :disabled="busy" @click="confirmRemove(connection)">{{ removalKind === 'credentials' ? 'Clear credentials' : 'Remove connection' }}</button><button class="${buttonClass}" :class="$styles.secondaryButton" type="button" :disabled="busy" @click="removing = null">Cancel</button></div></div>
            <div v-if="expandedTools === connection.id && catalogs[connection.id]" class="mt-4 border-t pt-4 border-[var(--assistant-border)]">
              <div class="flex flex-wrap items-center gap-2">
                <label class="min-w-[180px] flex-1"><span class="sr-only">Search tools</span><input class="h-10 w-full rounded-md border border-[var(--assistant-border)] bg-[var(--background)] px-3 py-2 text-[var(--assistant-text)] placeholder:text-[var(--tw-prose-captions)] focus-visible:outline-2 focus-visible:outline-[var(--ring)]" v-model="toolSearch" type="search" placeholder="Search tools…" /></label>
                <span class="whitespace-nowrap text-xs text-[var(--tw-prose-captions)]">{{ toolSearch ? filteredTools(connection).length + ' of ' : '' }}{{ catalogs[connection.id].length }} tools</span>
                <button v-if="filteredTools(connection).length" class="${buttonClass}" :class="$styles.secondaryButton" type="button" @click="toggleFiltered(connection)">{{ allFilteredSelected(connection) ? 'Deselect results' : 'Select all results' }}</button>
              </div>
              <p class="text-[var(--tw-prose-captions)] py-2.5 text-xs">Choose tools to include in your conversation.</p>
              <div class="max-h-[360px] overflow-auto overscroll-contain">
                <div v-if="!catalogs[connection.id].length && hasHiddenTools(connection)" class="flex flex-wrap items-center gap-2 py-3 text-xs text-[var(--tw-prose-captions)]">
                  <span>A saved tool filter hides this connection's tools.</span>
                  <button class="${buttonClass}" :class="$styles.secondaryButton" type="button" @click="showAllTools(connection)">Show discovered tools</button>
                </div>
                <p v-else-if="!filteredTools(connection).length" class="py-2.5 text-xs text-[var(--tw-prose-captions)]">{{ catalogs[connection.id].length ? 'No matching tools.' : 'No tools available from this connection.' }}</p>
                <div v-for="tool in filteredTools(connection)" :key="tool.name" class="flex items-start gap-3 border-t px-1 py-3 hover:bg-[var(--assistant-bg)] border-[var(--assistant-border)]">
                  <input :id="'mcp-tool-' + connection.id + '-' + tool.name" type="checkbox" class="mt-0.5 size-4 shrink-0 rounded border-gray-300 text-blue-600 focus:ring-blue-500 dark:border-gray-600 dark:bg-gray-800" :checked="isSelected(tool)" @change="select(tool)" />
                  <div class="min-w-0 flex-1"><div class="flex flex-wrap items-center gap-2"><label :for="'mcp-tool-' + connection.id + '-' + tool.name" class="cursor-pointer"><strong class="break-words text-sm font-medium">{{ tool.remoteName }}</strong></label><span v-if="tool.alwaysApproved" class="mcp-approved-badge inline-flex items-center gap-1 whitespace-nowrap rounded-full border border-green-500/50 bg-green-500/10 py-0.5 pr-1 pl-2 text-xs font-medium leading-none text-green-700 dark:text-green-300">always approved <button class="grid size-[18px] cursor-pointer place-items-center rounded-full text-base leading-none hover:bg-green-500/20 focus-visible:bg-green-500/20 disabled:cursor-default disabled:opacity-50" type="button" :disabled="busy" :aria-label="'Revoke always approval for ' + tool.remoteName" :title="'Revoke always approval for ' + tool.remoteName" @click="revokeGrant(connection, tool)">×</button></span></div><label :for="'mcp-tool-' + connection.id + '-' + tool.name" class="mt-0.5 line-clamp-2 cursor-pointer text-xs leading-relaxed text-[var(--tw-prose-captions)]">{{ tool.description }}</label></div>
                </div>
              </div>
            </div>
          </article>
        </div>
      </template>
    </section>`,
    setup() {
        const ctx = inject('ctx'), connections = ref([]), catalogs = ref({}), error = ref(''), busy = ref(false), signIn = ref(null)
        const config = ref({ servers: [], canEdit: false }), editor = ref(null), editingId = ref(null), removing = ref(null)
        const bearerToken = ref(''), oauthClientSecret = ref(''), oauthRedirectUri = ref(''), restoreTools = ref(false), nameInput = ref(null), notice = ref(''), expandedTools = ref(null), toolSearch = ref(''), activeAction = ref(''), removalKind = ref('connection')
        const scopes = ref(''), loading = ref(true), connectionIssue = ref(null)
        const issuerChoices = ref([]), selectedIssuer = ref(''), manualIssuer = ref(''), originalEndpoint = ref(''), issuerChoiceEndpoint = ref('')
        const isGitHubOAuth = endpoint => {
            try { return new URL(endpoint).hostname === 'api.githubcopilot.com' } catch { return false }
        }
        watch(() => editor.value?.endpoint, endpoint => {
            if (endpoint !== issuerChoiceEndpoint.value) { issuerChoices.value = []; selectedIssuer.value = '' }
        })
        const stateLabel = state => ({ ready: 'Connected', disconnected: 'Not connected', auth_required: 'Sign-in required', connecting: 'Connecting', error: 'Needs attention', degraded: 'Needs attention', unsupported: 'Unsupported' }[state] || String(state || '').replaceAll('_', ' '))
        async function refresh() {
            config.value = await request(ctx, 'config.json')
            oauthRedirectUri.value = config.value.oauthRedirectUri || ''
            connections.value = await request(ctx, 'connections')
        }
        async function reload() {
            busy.value = true; error.value = ''; notice.value = ''; connectionIssue.value = null
            try { await refresh(); editor.value = null; removing.value = null }
            catch (e) { error.value = e.message }
            finally { busy.value = false }
        }
        function edit(id) {
            bearerToken.value = ''; oauthClientSecret.value = ''; restoreTools.value = false; error.value = ''; notice.value = ''; connectionIssue.value = null; editingId.value = id || null
            editor.value = id ? JSON.parse(JSON.stringify(config.value.servers.find(s => s.id === id)))
                : { id: '', displayName: '', endpoint: '', auth: { mode: 'anonymous' }, allowedTools: ['*'], deniedTools: [], includeInAll: false }
            editor.value.auth ??= { mode: 'anonymous' }
            if (id && !editor.value.displayName) editor.value.displayName = id
            scopes.value = (editor.value.oauthScopes || []).join(', ')
            issuerChoices.value = []; selectedIssuer.value = ''; manualIssuer.value = ''
            originalEndpoint.value = editor.value.endpoint || ''; issuerChoiceEndpoint.value = ''
            nextTick(() => nameInput.value?.focus())
        }
        async function revokeGrant(connection, tool) {
            busy.value = true; error.value = ''; notice.value = ''
            try {
                await request(ctx, `connections/${encodeURIComponent(connection.id)}/approval-grants/revoke`, 'POST', { tool: tool.remoteName })
                catalogs.value[connection.id] = catalogs.value[connection.id].map(x => x.name === tool.name
                    ? { ...x, alwaysApproved: false, requiresApproval: true } : x)
                notice.value = `Approval revoked for ${tool.remoteName}.`
            } catch (e) { error.value = e.message }
            finally { busy.value = false }
        }
        function cancelEdit() { editor.value = null; bearerToken.value = ''; oauthClientSecret.value = ''; restoreTools.value = false; error.value = ''; connectionIssue.value = null }
        function endpointLabel(connection) {
            const endpoint = config.value.servers.find(s => s.id === connection.id)?.endpoint
            try { return endpoint ? new URL(endpoint).host : connection.scope === 'personal' ? 'Personal connection' : 'Managed by your administrator' } catch { return 'MCP server' }
        }
        function filteredTools(connection) {
            const query = toolSearch.value.trim().toLowerCase()
            return (catalogs.value[connection.id] || []).filter(t => !query || `${t.remoteName} ${t.description || ''}`.toLowerCase().includes(query))
        }
        function hasHiddenTools(connection) {
            if (connection.scope !== 'personal' || !config.value.canEdit) return false
            const server = config.value.servers.find(s => s.id === connection.id)
            return !!server && (!server.allowedTools?.length) && !server.deniedTools?.includes('*')
        }
        function showAllTools(connection) {
            if (!hasHiddenTools(connection)) return
            edit(connection.id)
            editor.value.allowedTools = ['*']
            restoreTools.value = true
        }
        async function confirmRemove(connection) {
            if (removalKind.value === 'credentials') { await act(connection, 'credentials', 'DELETE'); if (!error.value) removing.value = null }
            else await remove(connection.id)
        }
        const split = value => value.split(',').map(x => x.trim()).filter(Boolean)
        async function persist(servers, onSaved = () => { }) {
            await request(ctx, 'config.json', 'POST', { servers, revision: config.value.revision })
            onSaved()
            editor.value = null; removing.value = null; catalogs.value = {}; expandedTools.value = null; signIn.value = null
            await refresh(); await registerTools(ctx)
        }
        async function save() {
            busy.value = true; error.value = ''; notice.value = ''; connectionIssue.value = null
            let savedId = null
            try {
                const server = { ...editor.value }
                server.displayName = server.displayName.trim()
                server.endpoint = server.endpoint.trim()
                if (!server.displayName) throw new Error('Enter a name for this connection.')
                const endpoint = new URL(server.endpoint)
                if (!['https:', 'http:'].includes(endpoint.protocol)) throw new Error('Enter an HTTP or HTTPS server URL.')
                if (!editingId.value) {
                    // IDs are an implementation detail. Keep them stable once created and avoid all visible IDs.
                    // The backend adds mcp_ to group and tool names; connection IDs are unprefixed.
                    const slug = server.displayName.toLowerCase().replace(/[^a-z0-9]+/g, '_').replace(/^_+|_+$/g, '')
                    const base = (/^[a-z]/.test(slug) ? slug : slug ? `server_${slug}` : 'server').slice(0, 24)
                    const ids = new Set([...config.value.servers, ...connections.value].map(s => s.id))
                    server.id = base
                    for (let suffix = 2; ids.has(server.id); suffix++) server.id = `${base}_${suffix}`
                }
                if (server.auth.mode === 'user_oauth') {
                    server.oauthScopes = split(scopes.value)
                    if (manualIssuer.value.trim()) server.oauthIssuer = manualIssuer.value.trim()
                    else if (server.endpoint !== originalEndpoint.value || !server.oauthIssuer) {
                        if (issuerChoiceEndpoint.value !== server.endpoint) {
                            let discovery
                            try { discovery = await request(ctx, 'oauth/issuers', 'POST', { endpoint: server.endpoint }) }
                            catch (e) { throw new Error(`Could not discover this server's sign-in provider: ${e.message} Enter an issuer URL in Advanced OAuth settings if your server does not support discovery.`) }
                            issuerChoices.value = discovery.issuers || []
                            issuerChoiceEndpoint.value = server.endpoint
                        }
                        if (issuerChoices.value.length === 1) server.oauthIssuer = issuerChoices.value[0]
                        else if (issuerChoices.value.length > 1) {
                            if (!selectedIssuer.value) throw new Error('Choose a sign-in provider for this server.')
                            server.oauthIssuer = selectedIssuer.value
                        } else throw new Error('This server did not advertise a sign-in provider. Enter an issuer URL in Advanced OAuth settings.')
                    }
                } else { delete server.oauthClientId; delete server.oauthIssuer; delete server.oauthScopes }
                if (server.auth.mode === 'user_oauth' && !oauthRedirectUri.value)
                    throw new Error('OAuth needs a public callback URL. Configure the MCP OAuth callback URL on this host and restart it.')
                if (server.auth.mode === 'user_oauth' && isGitHubOAuth(server.endpoint) && !oauthClientSecret.value
                    && (!editingId.value || JSON.stringify(config.value.servers.find(s => s.id === editingId.value)) !== JSON.stringify(server)))
                    throw new Error('Enter the GitHub OAuth App client secret for this connection.')
                await persist([...config.value.servers.filter(s => s.id !== editingId.value), server], () => { savedId = server.id })
                if (server.auth.mode === 'bearer' && bearerToken.value) {
                    const token = bearerToken.value.trim()
                    bearerToken.value = ''
                    await request(ctx, `connections/${encodeURIComponent(server.id)}/credentials`, 'POST', { token })
                }
                if (server.auth.mode === 'user_oauth' && oauthClientSecret.value) {
                    const secret = oauthClientSecret.value
                    oauthClientSecret.value = ''
                    await request(ctx, `connections/${encodeURIComponent(server.id)}/oauth-client-secret`, 'POST', { secret })
                }
                const result = await request(ctx, `connections/${encodeURIComponent(server.id)}/connect`, 'POST', {})
                if (result.authorizationUrl) {
                    const url = new URL(result.authorizationUrl)
                    if (!['https:', 'http:'].includes(url.protocol)) throw new Error('Unsupported sign-in URL')
                    signIn.value = { id: server.id, url: url.href }
                } else {
                    catalogs.value[server.id] = await request(ctx, `connections/${encodeURIComponent(server.id)}/tools`)
                }
                await refresh(); await registerTools(ctx)
                notice.value = result.authorizationUrl ? '' : `${server.displayName} connected. Choose its tools below.`
            } catch (e) {
                if (savedId) { try { await refresh() } catch { /* Keep the original connection error. */ } }
                connectionIssue.value = savedId
                error.value = savedId ? `Connection saved, but could not connect. ${e.message}` : e.message
            }
            finally { busy.value = false }
        }
        async function remove(id) {
            busy.value = true; error.value = ''; notice.value = ''; connectionIssue.value = null
            try { await persist(config.value.servers.filter(s => s.id !== id)); notice.value = 'Connection removed.' }
            catch (e) { error.value = e.message }
            finally { busy.value = false }
        }
        async function act(connection, action, method = 'POST') {
            busy.value = true; error.value = ''; notice.value = ''; connectionIssue.value = null; activeAction.value = `${connection.id}:${action}`
            try {
                const result = await request(ctx, `connections/${encodeURIComponent(connection.id)}/${action}`, method, {})
                if (result.authorizationUrl) {
                    const url = new URL(result.authorizationUrl)
                    if (!['https:', 'http:'].includes(url.protocol)) throw new Error('Unsupported sign-in URL')
                    signIn.value = { id: connection.id, url: url.href }
                }
                delete catalogs.value[connection.id]; expandedTools.value = null
                if (!result.authorizationUrl) signIn.value = null
                await refresh(); await registerTools(ctx)
                notice.value = result.authorizationUrl ? '' : action === 'connect' ? `${connection.name} connected.` : action === 'refresh' ? 'Tools refreshed.' : action === 'credentials' ? 'Saved credentials cleared.' : `${connection.name} disabled. Connect all will skip it.`
            } catch (e) { error.value = e.message; connectionIssue.value = connection.scope === 'personal' ? connection.id : null }
            finally { busy.value = false; activeAction.value = '' }
        }
        async function connectAll() {
            busy.value = true; error.value = ''; notice.value = ''; connectionIssue.value = null; activeAction.value = 'connect-all'
            try {
                const results = await request(ctx, 'connections/connect-all', 'POST', {})
                catalogs.value = {}; expandedTools.value = null
                await refresh(); await registerTools(ctx)
                const connected = results.filter(x => x.connected).length
                const failed = results.filter(x => !x.connected)
                notice.value = results.length ? `${connected} of ${results.length} enabled connections connected.` : 'No enabled connections to connect.'
                if (failed.length) error.value = `${notice.value} ${failed.map(x => `${x.name}: ${x.error}`).join(' ')}`
            } catch (e) { error.value = e.message }
            finally { busy.value = false; activeAction.value = '' }
        }
        async function showTools(connection) {
            if (expandedTools.value === connection.id) { expandedTools.value = null; return }
            busy.value = true; error.value = ''; activeAction.value = `${connection.id}:tools`
            try {
                catalogs.value[connection.id] = await request(ctx, `connections/${encodeURIComponent(connection.id)}/tools`)
                expandedTools.value = connection.id; toolSearch.value = ''
                await registerTools(ctx)
            } catch (e) { error.value = e.message }
            finally { busy.value = false; activeAction.value = '' }
        }
        function isSelected(tool) { return ctx.prefs.onlyTools?.includes(tool.name) === true }
        function selectedToolNames() {
            // Switching from All to an explicit list keeps the currently available local tools.
            return ctx.prefs.onlyTools ?? ctx.state.tool.definitions.filter(x => !x.function?.name?.startsWith('mcp_')).map(x => x.function.name)
        }
        function select(tool) {
            const selected = selectedToolNames()
            ctx.setPrefs({ onlyTools: selected.includes(tool.name) ? selected.filter(x => x !== tool.name) : [...new Set([...selected, tool.name])] })
        }
        function allFilteredSelected(connection) {
            const results = filteredTools(connection)
            return results.length > 0 && results.every(isSelected)
        }
        function toggleFiltered(connection) {
            const names = new Set(filteredTools(connection).map(tool => tool.name))
            if (!names.size) return
            const selected = selectedToolNames()
            const allSelected = [...names].every(name => selected.includes(name))
            ctx.setPrefs({
                onlyTools: allSelected
                    ? selected.filter(name => !names.has(name))
                    : [...new Set([...selected, ...names])]
            })
        }
        let oauthChannel
        onMounted(async () => {
            if (typeof BroadcastChannel !== 'undefined') {
                try { oauthChannel = new BroadcastChannel('ai-chat-mcp-oauth') } catch { /* Manual Load tools remains available. */ }
                if (oauthChannel) oauthChannel.onmessage = async ({ data }) => {
                    if (data?.type === 'failed' && signIn.value) {
                        signIn.value = null
                        error.value = data.phase === 'tools'
                            ? `Sign-in succeeded, but tools could not load (${data.errorCode || 'connection_error'}). Try Refresh tools.`
                            : `Sign-in could not finish (${data.errorCode || 'connection_error'}). Check the OAuth App credentials and try again.`
                        await refresh()
                        return
                    }
                    if (data?.type !== 'authorized' || data.serverId !== signIn.value?.id || busy.value) return
                    try {
                        error.value = ''; notice.value = ''
                        await refresh()
                        let connected = connections.value.find(c => c.id === data.serverId)
                        if (!connected) return
                        // The callback may land on a different host instance; discover there if needed.
                        if (connected.state !== 'ready') {
                            await act(connected, 'refresh')
                            connected = connections.value.find(c => c.id === data.serverId)
                        } else signIn.value = null
                        if (!error.value && connected?.state === 'ready') {
                            await showTools(connected)
                            if (!error.value) notice.value = `${connected.name} connected. Choose its tools below.`
                        }
                    } catch (e) { error.value = e.message }
                }
            }
            try { await refresh() } catch (e) { error.value = e.message } finally { loading.value = false }
        })
        onUnmounted(() => oauthChannel?.close())
        return { connections, catalogs, error, busy, signIn, act, showTools, select, isSelected, config, editor, editingId, removing, scopes, issuerChoices, selectedIssuer, manualIssuer, edit, save, remove, reload, loading, stateLabel, connectionIssue, bearerToken, oauthClientSecret, oauthRedirectUri, isGitHubOAuth, restoreTools, nameInput, notice, expandedTools, toolSearch, activeAction, removalKind, cancelEdit, endpointLabel, filteredTools, hasHiddenTools, showAllTools, allFilteredSelected, toggleFiltered, confirmRemove, connectAll, revokeGrant }
    }
}

// Use the same Tools-page extension point and persisted extension preferences as MCP Server.
let connectionsScope
const ConnectionsToolPageHeader = {
    components: { Connections },
    template: `<section class="mb-6 flex flex-col items-stretch" aria-label="MCP connection management">
        <button type="button" class="inline-flex cursor-pointer self-start items-center gap-2 text-sm font-medium text-[var(--tw-prose-captions)] hover:text-[var(--heading)] focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-[var(--ring)]" :aria-expanded="expanded" aria-controls="mcp-connections-panel" @click="toggleExpanded">
            <svg aria-hidden="true" viewBox="0 0 24 24" fill="currentColor" class="size-5" :class="{ 'rotate-90': expanded }"><path d="M10 17l5-5-5-5v10z"/></svg>
            MCP Connections
        </button>
        <div v-if="expanded" id="mcp-connections-panel"><Connections /></div>
    </section>`,
    setup() {
        const expanded = computed(() => connectionsScope?.prefs.expanded === true)
        function toggleExpanded() { connectionsScope?.setPrefs({ expanded: !expanded.value }) }
        return { expanded, toggleExpanded }
    }
}

export default {
    install(ctx) {
        connectionsScope = ctx.scope('mcp_client')
        // The shared URL resolver prefixes relative paths. This extension keeps supported
        // inline private media usable without modifying synchronized core UI.
        const resolveUrl = ctx.resolveUrl?.bind(ctx)
        if (resolveUrl) ctx.resolveUrl = url => /^data:(image\/(png|jpeg|gif|webp)|audio\/(mpeg|wav|ogg));base64,/.test(url)
            ? url : resolveUrl(url)
        ctx.tools?.setToolPageHeaders({ mcp_client: ConnectionsToolPageHeader })
    },
    async load(ctx) { await registerTools(ctx) }
}
