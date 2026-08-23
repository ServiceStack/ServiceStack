import { ref, computed, watch, nextTick, onMounted, onBeforeUnmount } from 'vue'
import { CheckBox } from './explorer.mjs'

let ext = null
let ctx = null
export function initAssistants(scope, context, components={}) {
    ext = scope
    ctx = context
    if (components.GeminiModelSelector)
        AssistantsPanel.components.GeminiModelSelector = components.GeminiModelSelector
}

const PROMPTS = {
    documentation: `# Role
You are a documentation guide. Help users understand and successfully use the documented product.

# Method
- Identify the user's intended outcome, not merely the terms in the question.
- Give the shortest complete answer that enables progress.
- Include prerequisites before procedural steps.
- Preserve documented names, commands, option names, paths, and identifiers exactly.
- When multiple approaches are documented, recommend the simplest applicable approach and briefly mention alternatives.
- State dependencies on product version, platform, or configuration clearly.
- Do not describe undocumented features or imply that an example is officially supported unless the documentation says so.

# Response
For how-to questions, state the recommended approach, list the steps in order, include a minimal supported example when useful, and mention material caveats or next steps. For conceptual questions, explain the concept plainly and connect it to the user's likely goal.`,
    troubleshooting: `# Role
You are a technical support troubleshooter. Help users diagnose and resolve problems using documented product behavior and troubleshooting guidance.

# Method
- Identify the symptom, environment, product version, and relevant configuration from the conversation.
- If one essential detail is missing, ask one focused diagnostic question instead of presenting many speculative fixes.
- Distinguish documented causes from possible causes.
- Start with the safest, least disruptive diagnostic check.
- Present troubleshooting steps in a deliberate order, stating what result to look for and what it means.
- Preserve error messages, commands, paths, setting names, and API identifiers exactly.
- Warn before any destructive, irreversible, security-sensitive, or production-impacting step.
- Do not claim a cause is confirmed unless the available evidence establishes it.
- Do not repeat steps the user has already completed.

# Response
When appropriate, use the headings **Likely cause**, **Try this**, and **If it still fails**. End with the next useful diagnostic detail to collect or the documented escalation path.`,
    support: `# Role
You are a friendly and practical customer support Assistant.

# Method
- Acknowledge the customer's goal or problem briefly without excessive apology.
- Explain the applicable documented policy or process in plain language.
- Give the clearest next action the customer can take.
- Ask only for information required to determine the applicable documented answer.
- Never request passwords, secret keys, payment-card details, authentication codes, or unnecessary personal information.
- Do not claim access to accounts, orders, billing systems, tickets, or customer records.
- Do not promise refunds, credits, delivery dates, exceptions, or outcomes unless the documentation explicitly guarantees them.
- When the request requires an employee or another system, explain the documented handoff path and what information the customer should prepare.

# Response
Lead with the answer or next action. Keep policy explanations concise, respectful, and unambiguous.`,
    developer: `# Role
You are a developer and API documentation Assistant. Provide technically precise answers grounded in the documented APIs and examples.

# Method
- Determine the relevant language, framework, runtime, package, and version when they affect the answer.
- Preserve documented type names, members, routes, parameters, casing, and command syntax exactly.
- Prefer the current documented API when the applicable version is known.
- Do not invent classes, methods, options, overloads, packages, or command flags.
- Do not present pseudocode as working code; clearly label conceptual examples.
- Reuse documented conventions and patterns.
- Include imports, registration, configuration, and prerequisites needed to make an example usable.
- Keep examples minimal and focused on the question.
- Explain why an approach works and mention important lifecycle, security, or compatibility constraints.
- When documents describe different versions, identify the difference instead of combining incompatible APIs.

# Response
Give the direct technical answer first, followed by a minimal code example when useful. Use fenced code blocks with an appropriate language identifier.`,
    product: `# Role
You are a product advisor. Help users determine whether the documented product or feature is suitable for their needs.

# Method
- Identify the user's goal, constraints, environment, and decision criteria.
- If the request is underspecified, ask one focused question that materially affects the recommendation.
- Recommend only capabilities and configurations supported by the documentation.
- Distinguish documented product facts from your reasoned fit assessment.
- Explain relevant trade-offs, limitations, prerequisites, and operational implications.
- Do not invent pricing, availability, roadmap commitments, service levels, performance figures, compatibility, or competitive claims.
- Do not disparage alternatives.
- If the documents do not support a confident recommendation, explain what information is missing.

# Response
When helpful, use the headings **Recommendation**, **Why**, and **Considerations**.`,
    onboarding: `# Role
You are an onboarding guide. Help users reach their first successful outcome with the documented product.

# Method
- Determine the outcome the user wants and their current progress.
- Break the journey into small, ordered milestones.
- Begin with prerequisites and the minimum viable setup.
- Give one coherent recommended path instead of listing every possible option.
- After each important step, provide a simple way to verify success.
- Explain unfamiliar terms briefly when first used.
- Introduce advanced configuration only when it is needed for the user's goal.
- Do not assume setup succeeded merely because instructions were provided.
- If the user encounters an error, switch to focused troubleshooting.

# Response
Keep the user oriented by stating what they are doing, the next step, how they will know it worked, and what to do afterward.`,
    policy: `# Role
You are a policy and procedures explainer. Provide precise, neutral explanations of the supplied policies and documented procedures.

# Method
- Identify which policy, version, jurisdiction, product, role, or effective period applies.
- Preserve distinctions such as must, may, should, prohibited, eligible, and required.
- Separate what the policy explicitly says from any plain-language explanation.
- Do not infer exceptions, permissions, obligations, deadlines, or guarantees that are not documented.
- When documents conflict or appear superseded, describe the conflict and ask the user to confirm which version applies.
- For procedures, list the required steps, prerequisites, responsible party, and documented escalation path.
- Do not present the answer as legal, medical, tax, or financial advice.
- For high-impact decisions, encourage confirmation with the responsible organization or a qualified professional.

# Response
State the applicable rule first, then explain it in plain language. Include qualifications and exceptions that materially affect the answer.`,
}
const DEFAULT_BUTTON = { size:50, iconSize:26, background:'', iconColor:'#ffffff', borderColor:'', borderWidth:0, borderRadius:50, shadow:'medium', iconDataUri:'' }
const BUTTON_COLOR_FIELDS = [
    { key:'borderColor', label:'Border color' },
    { key:'background', label:'Button background' },
    { key:'iconColor', label:'Icon color' },
]

const defaults = () => ({
    model: '',
    identity: { title: 'Ask our assistant', description: 'Answers grounded in our documentation.', welcome: 'Hi! How can I help you today?', suggestions: ['What can you help me with?'] },
    scope: {},
    behavior: { template: 'documentation', systemPrompt: PROMPTS.documentation, grounded: true, citations: true, responseStyle: 'balanced', openMode: '', keyboardShortcut: false, fallback: "I couldn't find that in the available documents.", notice: 'Conversations may be reviewed to improve support.' },
    appearance: { theme: 'auto', colors: {}, fonts: {}, position: 'bottom-right', icon: 'sparkles', button: {...DEFAULT_BUTTON}, panelSize: 'standard' },
    hosting: { allowedOrigins: [], requestsPerMinute: 30 },
})
const clone = value => JSON.parse(JSON.stringify(value))
const SCOPE_FIELDS = ['category', 'docType', 'status', 'locale', 'product', 'versions', 'tags']
const colorGroup = (label, fields) => ({ label, fields:fields.map(([key,name]) => ({ key, label:name })) })
const BUBBLE_COLOR_GROUPS = [
    colorGroup('Assistant', [['assistant-bg','Background'], ['assistant-border','Border'], ['assistant-text','Text']]),
    colorGroup('User', [['user-bg','Background'], ['user-border','Border'], ['user-text','Text']]),
]
const LEFT_COLOR_GROUPS = [
    colorGroup('Backgrounds', [['accent-bg','Accent'], ['panel-bg','Panel'], ['conversation-bg','Conversation']]),
    colorGroup('Borders', [['panel-border','Panel'], ['focus-border','Focus']]),
]
const RIGHT_COLOR_GROUPS = [
    colorGroup('Text colors', [['primary-text','Primary'], ['muted-text','Muted'], ['link-text','Link'], ['error-text','Error'], ['warning-text','Warning']]),
]
const LOWER_COLOR_COLUMNS = [LEFT_COLOR_GROUPS, RIGHT_COLOR_GROUPS]
const SYSTEM_FONT = "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', 'Noto Sans', Arial, sans-serif, 'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol', 'Noto Color Emoji'"
const MONO_FONT = "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', 'Courier New', monospace"
const FONT_PRESETS = { light:SYSTEM_FONT, dark:SYSTEM_FONT, nord:SYSTEM_FONT, matrix:MONO_FONT, 'soft-pink':SYSTEM_FONT }
const LAUNCHER_ICONS = {
    sparkles: `<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 24 24"><path d="M0 0h24v24H0z" fill="none"/><path fill="currentColor" d="m21.45 11.11l-3-1.5l-2.68-1.34l-.03-.03l-1.34-2.68l-1.5-3c-.34-.68-1.45-.68-1.79 0l-1.5 3l-1.34 2.68l-.03.03l-2.68 1.34l-3 1.5c-.34.17-.55.52-.55.89s.21.72.55.89l3 1.5l2.68 1.34l.03.03l1.34 2.68l1.5 3c.17.34.52.55.89.55s.72-.21.89-.55l1.5-3l1.34-2.68l.03-.03l2.68-1.34l3-1.5c.34-.17.55-.52.55-.89s-.21-.72-.55-.89Z"/></svg>`,
    chat: `<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 24 24"><path d="M0 0h24v24H0z" fill="none"/><path fill="currentColor" d="M12 3c5.5 0 10 3.58 10 8s-4.5 8-10 8c-1.24 0-2.43-.18-3.53-.5C5.55 21 2 21 2 21c2.33-2.33 2.7-3.9 2.75-4.5C3.05 15.07 2 13.13 2 11c0-4.42 4.5-8 10-8"/></svg>`,
    help: `<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 24 24"><path d="M0 0h24v24H0z" fill="none"/><path fill="currentColor" d="M10 19h3v3h-3zm2-17c5.35.22 7.68 5.62 4.5 9.67c-.83 1-2.17 1.66-2.83 2.5C13 15 13 16 13 17h-3c0-1.67 0-3.08.67-4.08c.66-1 2-1.59 2.83-2.25C15.92 8.43 15.32 5.26 12 5a3 3 0 0 0-3 3H6a6 6 0 0 1 6-6"/></svg>`,
}
const THEME_PRESETS = {
    auto:  { 'accent-bg':'#2563eb', 'panel-bg':'#ffffff', 'conversation-bg':'#f8fafc', 'assistant-bg':'#ffffff', 'assistant-border':'#dbe2ea', 'assistant-text':'#172033', 'user-bg':'#e8f0ff', 'user-border':'#bfdbfe', 'user-text':'#172033', 'primary-text':'#172033', 'muted-text':'#64748b', 'panel-border':'#dbe2ea', 'link-text':'#2563eb', 'focus-border':'#93c5fd', 'error-text':'#dc2626', 'warning-text':'#d97706' },
    light: { 'accent-bg':'#2563eb', 'panel-bg':'#ffffff', 'conversation-bg':'#f8fafc', 'assistant-bg':'#ffffff', 'assistant-border':'#dbe2ea', 'assistant-text':'#172033', 'user-bg':'#e8f0ff', 'user-border':'#bfdbfe', 'user-text':'#172033', 'primary-text':'#172033', 'muted-text':'#64748b', 'panel-border':'#dbe2ea', 'link-text':'#2563eb', 'focus-border':'#93c5fd', 'error-text':'#dc2626', 'warning-text':'#d97706' },
    dark:  { 'accent-bg':'#2563eb', 'panel-bg':'#0f172a', 'conversation-bg':'#111827', 'assistant-bg':'#1f2937', 'assistant-border':'#374151', 'assistant-text':'#f3f4f6', 'user-bg':'#1d4ed8', 'user-border':'#3b82f6', 'user-text':'#ffffff', 'primary-text':'#f3f4f6', 'muted-text':'#9ca3af', 'panel-border':'#334155', 'link-text':'#60a5fa', 'focus-border':'#93c5fd', 'error-text':'#f87171', 'warning-text':'#fbbf24' },
    nord:  { 'accent-bg':'#5e81ac', 'panel-bg':'#2e3440', 'conversation-bg':'#2e3440', 'assistant-bg':'#4c566a', 'assistant-border':'#434c5e', 'assistant-text':'#eceff4', 'user-bg':'#5e81ac', 'user-border':'#81a1c1', 'user-text':'#eceff4', 'primary-text':'#eceff4', 'muted-text':'#d8dee9', 'panel-border':'#4c566a', 'link-text':'#8fbcbb', 'focus-border':'#81a1c1', 'error-text':'#bf616a', 'warning-text':'#ebcb8b' },
    matrix:{ 'accent-bg':'#0d542b', 'panel-bg':'#000000', 'conversation-bg':'#020a04', 'assistant-bg':'#000000', 'assistant-border':'#008236', 'assistant-text':'#86efac', 'user-bg':'#052e16', 'user-border':'#166534', 'user-text':'#4ade80', 'primary-text':'#4ade80', 'muted-text':'#15803d', 'panel-border':'#166534', 'link-text':'#4ade80', 'focus-border':'#22c55e', 'error-text':'#f87171', 'warning-text':'#facc15' },
    'soft-pink':{ 'accent-bg':'#ec4899', 'panel-bg':'#ffffff', 'conversation-bg':'#fdf2f8', 'assistant-bg':'#fce7f3', 'assistant-border':'#f9a8d4', 'assistant-text':'#831843', 'user-bg':'#f1f5f9', 'user-border':'#cbd5e1', 'user-text':'#1e293b', 'primary-text':'#831843', 'muted-text':'#9d174d', 'panel-border':'#fbcfe8', 'link-text':'#ec4899', 'focus-border':'#f472b6', 'error-text':'#e11d48', 'warning-text':'#d97706' },
}

const DeleteAssistantDialog = {
    props: {
        open: Boolean,
        busy: Boolean,
        loading: Boolean,
        summary: Object,
        modelValue: String,
    },
    emits: ['update:modelValue', 'close', 'confirm'],
    template: `
      <Teleport to="body">
        <div v-if="open" class="fixed inset-0 flex items-center justify-center p-4" style="z-index:220">
          <div class="fixed inset-0 bg-black/60" @click="close"></div>
          <div class="relative flex max-h-[calc(100vh-2rem)] w-full max-w-xl flex-col overflow-hidden rounded-xl border bg-white shadow-2xl dark:bg-gray-900"
            :class="$styles.chromeBorder" role="dialog" aria-modal="true" aria-labelledby="delete-assistant-title">
            <div class="flex items-start gap-3 border-b px-5 py-4" :class="$styles.chromeBorder">
              <div class="mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-full bg-red-100 text-red-600 dark:bg-red-950 dark:text-red-400">
                <svg class="size-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 9v4m0 4h.01M10.3 3.7 2.4 17.4A2 2 0 0 0 4.1 20h15.8a2 2 0 0 0 1.7-3L13.7 3.7a2 2 0 0 0-3.4 0Z"/></svg>
              </div>
              <div>
                <h3 id="delete-assistant-title" class="font-semibold" :class="$styles.heading">
                  Permanently delete {{ summary?.name || 'this Assistant' }}?
                </h3>
                <p class="mt-1 text-sm" :class="$styles.muted">
                  Its deployment, configuration, customer history, and public widget ID will be removed. This cannot be undone.
                </p>
              </div>
            </div>

            <div class="overflow-y-auto px-5 py-4">
              <div v-if="loading" class="flex items-center justify-center gap-2 py-10 text-sm" :class="$styles.muted">
                <svg class="size-4 animate-spin" viewBox="0 0 24 24" fill="none"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 0 1 8-8V0A12 12 0 0 0 0 12h4Z"/></svg>
                Checking deployment and conversation history…
              </div>
              <template v-else-if="summary">
                <div class="overflow-hidden rounded-lg border text-sm" :class="$styles.chromeBorder">
                  <div class="flex items-center justify-between gap-4 border-b px-3 py-2.5" :class="$styles.chromeBorder">
                    <span>Assistant configuration and widget ID</span><b>1</b>
                  </div>
                  <div class="flex items-center justify-between gap-4 border-b px-3 py-2.5" :class="$styles.chromeBorder">
                    <span>Published widget deployment</span><b>{{ summary.published ? 1 : 0 }}</b>
                  </div>
                  <div class="flex items-center justify-between gap-4 border-b px-3 py-2.5" :class="$styles.chromeBorder">
                    <span>Customer conversations</span><b>{{ Number(summary.conversations || 0).toLocaleString() }}</b>
                  </div>
                  <div class="flex items-center justify-between gap-4 px-3 py-2.5">
                    <span>Conversation messages</span><b>{{ Number(summary.messages || 0).toLocaleString() }}</b>
                  </div>
                </div>

                <div class="mt-5">
                  <h4 class="text-sm font-semibold">{{ summary.published ? 'Websites that will stop working' : 'Recorded referring websites' }}</h4>
                  <p class="mt-1 text-xs" :class="$styles.muted">
                    Unique referring domains recorded from this Assistant's customer conversations.
                  </p>
                  <div v-if="summary.referrers?.length" class="mt-2 max-h-48 overflow-y-auto rounded-lg border" :class="$styles.chromeBorder">
                    <div v-for="site in summary.referrers" :key="site.domain"
                      class="flex items-start justify-between gap-4 border-b px-3 py-2.5 last:border-b-0" :class="$styles.chromeBorder">
                      <div class="min-w-0">
                        <div class="truncate text-sm font-medium" :title="site.domain">{{ site.domain }}</div>
                        <div class="text-xs" :class="$styles.muted">
                          {{ Number(site.conversationCount || 0).toLocaleString() }} conversation{{ site.conversationCount === 1 ? '' : 's' }}
                        </div>
                      </div>
                      <div class="shrink-0 text-right text-xs" :class="$styles.muted">
                        <span class="block">Last used</span>
                        <time :datetime="site.lastUsedAt" :title="site.lastUsedAt">{{ formatLastUsed(site.lastUsedAt) }}</time>
                      </div>
                    </div>
                  </div>
                  <p v-else class="mt-2 rounded-lg border px-3 py-3 text-sm" :class="[$styles.chromeBorder, $styles.muted]">
                    No referring websites have been recorded.
                  </p>
                  <p v-if="summary.unknownReferrerConversations" class="mt-2 text-xs" :class="$styles.muted">
                    {{ summary.unknownReferrerConversations }} conversation{{ summary.unknownReferrerConversations === 1 ? '' : 's' }} had no recorded website.
                  </p>
                </div>

                <label for="delete-assistant-confirmation" class="mt-5 block text-sm font-medium">
                  Type <strong>{{ summary.name }}</strong> to confirm
                </label>
                <input id="delete-assistant-confirmation" type="text" :value="modelValue"
                  @input="$emit('update:modelValue', $event.target.value)" :disabled="busy"
                  autocomplete="off" spellcheck="false" class="mt-2 block w-full rounded-md px-3 py-2 text-sm"
                  :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]">
              </template>
            </div>

            <div class="flex justify-end gap-2 border-t px-5 py-3" :class="$styles.chromeBorder">
              <button type="button" @click="close" :disabled="busy"
                class="rounded-md border px-3 py-1.5 text-sm transition-colors disabled:opacity-50" :class="$styles.secondaryButton">Cancel</button>
              <button type="button" @click="$emit('confirm')"
                :disabled="busy || loading || !summary || modelValue !== summary.name"
                class="rounded-md border border-red-600 bg-red-600 px-4 py-1.5 text-sm font-semibold text-white transition-colors hover:border-red-700 hover:bg-red-700 disabled:cursor-not-allowed disabled:opacity-40">
                {{ busy ? 'Deleting everything…' : 'Delete Assistant' }}
              </button>
            </div>
          </div>
        </div>
      </Teleport>
    `,
    setup(props, { emit }) {
        function close() { if (!props.busy) emit('close') }
        function onKey(event) { if (event.key === 'Escape' && props.open) close() }
        function formatLastUsed(value) {
            if (!value) return 'Unknown'
            const date = new Date(value)
            return Number.isNaN(date.getTime()) ? String(value) : date.toLocaleString()
        }
        onMounted(() => document.addEventListener('keydown', onKey))
        onBeforeUnmount(() => document.removeEventListener('keydown', onKey))
        return { close, formatLastUsed }
    },
}

export const AssistantsPanel = {
    components: { CheckBox, DeleteAssistantDialog },
    props: { storeId: [String, Number], facets: Object, routeAssistant: String,
        routeConversations: String, routeConversation: String },
    emits: ['count', 'navigate'],
    template: `
      <div data-tag="AssistantsPanel" class="space-y-5 [&_button]:transition-all [&_button]:duration-150 [&_button:not(:disabled)]:cursor-pointer [&_button:disabled]:cursor-not-allowed">
        <div class="flex flex-wrap items-start justify-between gap-3">
          <div><h2 class="text-lg font-semibold">Website Assistants</h2><p class="text-sm mt-1" :class="$styles.muted">Publish a document-grounded assistant as an isolated Shadow DOM widget.</p></div>
          <button type="button" @click="newAssistant" class="px-3 py-1.5 rounded-md text-sm font-semibold" :class="$styles.primaryButton">New assistant</button>
        </div>
        <ErrorSummary v-if="error" :status="error" />
        <DeleteAssistantDialog :open="deleteOpen" :busy="deleteBusy" :loading="deleteLoading"
          :summary="deleteSummary" v-model="deleteConfirmation"
          @close="closeDelete" @confirm="deletePermanently" />
        <div class="mb-8 grid lg:grid-cols-[15rem_minmax(0,1fr)] gap-5 items-start">
          <aside class="rounded-lg border overflow-hidden" :class="$styles.chromeBorder">
            <div class="px-3 py-2 text-xs font-semibold border-b bg-gray-50 dark:bg-gray-900" :class="$styles.chromeBorder">Saved assistants</div>
            <button v-for="item in items" :key="item.id" type="button" @click="selectAssistant(item)"
              class="w-full text-left px-3 py-2.5 border-b last:border-b-0 hover:bg-gray-50 dark:hover:bg-gray-800" :class="[$styles.chromeBorder, selected?.id === item.id ? 'bg-blue-50 dark:bg-blue-900/20' : '']">
              <span class="block text-sm font-medium truncate">{{ item.name }}</span>
              <span class="flex items-center gap-1.5 mt-1 text-xs" :class="$styles.muted"><span class="size-1.5 rounded-full" :class="item.enabled == 0 ? 'bg-orange-400' : item.published ? 'bg-green-500' : 'bg-gray-400'"></span>{{ item.enabled == 0 ? 'Archived' : item.published ? 'Published' : 'Draft' }}</span>
            </button>
            <p v-if="!loading && !items.length" class="px-3 py-5 text-sm" :class="$styles.muted">No assistants yet.</p>
          </aside>

          <div v-if="editing" class="space-y-5 min-w-0">
            <div class="flex flex-wrap items-center justify-between gap-3 rounded-lg border px-4 py-3" :class="$styles.chromeBorder">
              <div class="min-w-0">
                <input type="text" v-model="name" :disabled="archived" placeholder="Assistant name" class="w-72 max-w-full disabled:opacity-60" :class="$styles.secondaryButton">
                <p class="text-xs mt-1" :class="$styles.muted">Internal name; visitors see the display title below.</p>
              </div>
              <div class="flex flex-wrap gap-2">
                <button v-if="selected" type="button" @click="toggleConversations" class="px-3 py-1.5 text-sm rounded-md" :class="$styles.secondaryButton">
                  {{ showConversations ? 'Hide Conversations' : 'View Conversations' }} ({{ selected.conversationCount || 0 }})
                </button>
                <button v-if="!archived && !selected?.published" type="button" @click="save(false)" :disabled="busy || !canSaveDraft" class="px-3 py-1.5 text-sm rounded-md disabled:opacity-50" :class="$styles.secondaryButton">Save draft</button>
                <button v-if="!archived" type="button" @click="save(true)" :disabled="busy || !canPublish" class="px-3 py-1.5 text-sm rounded-md disabled:opacity-50" :class="$styles.primaryButton">{{ selected?.published ? 'Update published' : 'Publish' }}</button>
              </div>
            </div>

            <div v-if="archived" class="rounded-lg border border-amber-300 bg-amber-50 px-4 py-3 text-sm text-amber-900 dark:border-amber-800 dark:bg-amber-950/30 dark:text-amber-200">
              This Assistant is archived, offline, and read-only. Restore it to an unpublished draft before editing or publishing it.
            </div>

            <div v-if="showConversations && selected" class="rounded-lg border overflow-hidden" :class="$styles.chromeBorder">
              <div class="px-4 py-3 border-b flex justify-between" :class="$styles.chromeBorder"><div><h3 class="font-semibold">Customer conversations</h3><p class="text-xs" :class="$styles.muted">Retained so support teams can review questions and improve coverage.</p></div><button type="button" @click="loadConversations" title="Refresh conversations" aria-label="Refresh conversations" class="self-start rounded p-1 transition-colors hover:bg-gray-100 dark:hover:bg-gray-800" :class="$styles.muted"><svg class="size-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M20 11a8.1 8.1 0 0 0-15.5-2M4 4v5h5M4 13a8.1 8.1 0 0 0 15.5 2M20 20v-5h-5" /></svg></button></div>
              <div class="grid md:grid-cols-[18rem_minmax(0,1fr)] min-h-72 md:h-[32rem] md:max-h-[calc(100vh-14rem)]">
                <div class="min-h-0 max-h-64 md:max-h-none overflow-y-auto border-r" :class="$styles.chromeBorder">
                  <button v-for="c in conversations" :key="c.id" @click="openConversation(c)" class="block w-full text-left px-3 py-2 border-b hover:bg-gray-50 dark:hover:bg-gray-800" :class="[$styles.chromeBorder, conversation?.id === c.id ? 'bg-blue-50 dark:bg-blue-900/20' : '']"><span class="block text-sm truncate">{{ c.title || 'New conversation' }}</span><span class="block text-xs truncate" :class="$styles.muted">{{ c.origin || 'Unknown origin' }} · {{ c.userMessageCount || 0 }} user message{{ c.userMessageCount === 1 ? '' : 's' }}</span></button>
                  <p v-if="!conversations.length" class="p-4 text-sm" :class="$styles.muted">No customer conversations yet.</p>
                </div>
                <div ref="conversationPane" class="relative min-h-0 max-h-[32rem] md:max-h-none overflow-y-auto overscroll-contain p-4 space-y-3">
                  <template v-if="conversation">
                    <div class="flex items-start justify-between gap-3 mb-3">
                      <div class="min-w-0 text-xs break-words" :class="$styles.muted">{{ conversation.pageUrl || conversation.origin || 'Unknown page' }}</div>
                      <div v-if="conversationTurns.length" @keydown.esc="conversationNavOpen = false" class="relative shrink-0">
                        <button type="button" @click="conversationNavOpen = !conversationNavOpen" :aria-expanded="conversationNavOpen" title="Show all user messages" class="inline-flex items-center gap-1 whitespace-nowrap text-xs text-blue-600 dark:text-blue-400">
                          <svg v-if="conversationNavOpen" class="size-3 shrink-0" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 0 1 1.06.02L10 11.168l3.71-3.938a.75.75 0 1 1 1.08 1.04l-4.25 4.5a.75.75 0 0 1-1.08 0l-4.25-4.5a.75.75 0 0 1 .02-1.06Z" clip-rule="evenodd" /></svg>
                          <svg v-else class="size-3 shrink-0" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 0 1 .02-1.06L11.168 10 7.23 6.29a.75.75 0 1 1 1.04-1.08l4.5 4.25a.75.75 0 0 1 0 1.08l-4.5 4.25a.75.75 0 0 1-1.06-.02Z" clip-rule="evenodd" /></svg>
                          <span>{{ conversationTurns.length }} user message{{ conversationTurns.length === 1 ? '' : 's' }}</span>
                        </button>
                        <div v-if="conversationNavOpen" class="absolute z-20 right-0 top-full mt-1 w-80 max-w-[calc(100vw-3rem)] max-h-64 overflow-y-auto rounded-md border bg-white py-1 shadow-lg dark:bg-gray-900" :class="$styles.chromeBorder">
                          <button v-for="(turn,i) in conversationTurns" :key="turn.userId" type="button" @click="jumpToConversationResponse(turn.targetId)" :title="turn.label" class="block w-full px-3 py-2 text-left text-xs hover:bg-gray-50 dark:hover:bg-gray-800"><span class="mr-1" :class="$styles.muted">{{ i + 1 }}.</span>{{ turn.label }}</button>
                        </div>
                      </div>
                    </div>
                    <div v-for="m in conversation.messages" :key="m.id" :data-conversation-message-id="m.id" class="flex" :class="m.role === 'user' ? 'justify-end' : ''">
                      <div class="max-w-[88%] rounded-xl px-3 py-2 text-sm" :class="m.role === 'user' ? 'bg-blue-100 text-gray-900' : 'bg-gray-100 dark:bg-gray-800'">
                        <div v-if="m.role === 'assistant'" v-html="$fmt.markdown(m.content)" class="prose prose-sm max-w-none dark:prose-invert break-words"></div>
                        <div v-else class="whitespace-pre-wrap break-words">{{ m.content }}</div>
                        <div v-if="m.citations?.length" class="mt-2 text-xs"><a v-for="x in m.citations" :href="x.url" target="_blank" class="block text-blue-600 hover:underline">{{ x.title }}</a></div>
                      </div>
                    </div>
                  </template>
                  <p v-else class="text-sm" :class="$styles.muted">Select a conversation to review it.</p>
                </div>
              </div>
            </div>

            <div v-else class="grid xl:grid-cols-[minmax(0,1fr)_22rem] gap-5 items-start">
              <div class="space-y-5">
                <fieldset :disabled="archived" class="space-y-5 disabled:opacity-70">
                <section class="rounded-lg border p-4 space-y-3" :class="$styles.chromeBorder"><h3 class="font-semibold">Identity</h3>
                  <div>
                    <label class="block text-xs font-semibold">Display title</label>
                    <input type="text" v-model="config.identity.title" class="mt-1 w-full rounded-md" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]">
                  </div>
                  <div>
                    <label class="block text-xs font-semibold">Description</label>
                    <input type="text" v-model="config.identity.description" class="mt-1 w-full rounded-md" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]">
                  </div>
                  <div>
                    <label class="block text-xs font-semibold">Welcome message</label>
                    <textarea v-model="config.identity.welcome" rows="2" class="mt-1 w-full rounded-md" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]"></textarea>
                  </div>
                  <div>
                    <div class="flex items-center justify-between gap-2">
                      <label class="block text-xs font-semibold">Suggested questions</label>
                      <button type="button" @click="addSuggestion()" :disabled="config.identity.suggestions.length >= 6" title="Add suggested question" class="size-7 grid place-items-center rounded-md border border-transparent text-lg leading-none text-gray-400 hover:text-gray-700 hover:border-gray-300 focus-visible:border-gray-300 dark:text-gray-500 dark:hover:text-gray-200 dark:hover:border-gray-600 dark:focus-visible:border-gray-600 disabled:opacity-40">+</button>
                    </div>
                    <div class="mt-1 space-y-2">
                      <div v-for="(question,i) in config.identity.suggestions" :key="i" class="flex items-center gap-2">
                        <input type="text" v-model="config.identity.suggestions[i]" :data-suggestion-index="i" @keydown.enter.prevent="addSuggestion(i)" placeholder="Suggested question" class="min-w-0 flex-1 rounded-md" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]">
                        <button type="button" @click="removeSuggestion(i)" title="Remove suggested question" class="size-7 shrink-0 grid place-items-center rounded-md border border-transparent text-base leading-none text-gray-400 hover:text-gray-700 hover:border-gray-300 focus-visible:border-gray-300 dark:text-gray-500 dark:hover:text-gray-200 dark:hover:border-gray-600 dark:focus-visible:border-gray-600" aria-label="Remove suggested question">×</button>
                      </div>
                    </div>
                    <span class="block mt-1 text-xs" :class="$styles.muted">Press Enter or + to add another question. Up to 6 questions.</span>
                  </div>
                </section>

                <section class="rounded-lg border p-4 space-y-3" :class="$styles.chromeBorder">
                  <div>
                    <h3 class="font-semibold">Document scope</h3>
                    <p class="text-xs" :class="$styles.muted">These filters are enforced by the server and cannot be changed by the host website.</p>
                  </div>
                  <div class="grid sm:grid-cols-2 gap-3">
                    <div v-for="field in scopeFields" :key="field">
                      <label class="block text-xs font-semibold">{{ field }}</label>
                      <select v-model="config.scope[field]" class="mt-1 w-full rounded-md" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]">
                        <option value="">Any value</option>
                        <option v-for="x in facetOptions(field)" :value="x.value">{{ x.value }} ({{ x.count }})</option>
                      </select>
                    </div>
                  </div>
                  <p class="text-xs font-mono break-all" :class="$styles.muted">{{ scopeSummary }}</p>
              </section>

                <section class="rounded-lg border p-4 space-y-3" :class="$styles.chromeBorder"><h3 class="font-semibold">Behavior</h3>
                  <div>
                    <label class="block text-xs font-semibold mb-1">Gemini model</label>
                    <GeminiModelSelector v-model="config.model" default-text="Default (server configured)" help-text="Select a Gemini model for this Assistant" />
                    <span class="block mt-1 text-xs" :class="$styles.muted">Leave unset to use the server's default Assistant model.</span>
                  </div>
                  <div class="grid sm:grid-cols-2 gap-3">
                    <div>
                      <label class="block text-xs font-semibold">When to open</label>
                      <select v-model="config.behavior.openMode" class="mt-1 w-full rounded-md" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]">
                        <option value="">Only when initiated</option>
                        <option value="page-load">After page load</option>
                        <option value="page-bottom">After reaching bottom of page</option>
                      </select>
                    </div>
                    <div class="flex flex-col justify-end pb-1">
                      <label class="inline-flex items-center gap-2 text-sm"><CheckBox v-model="config.behavior.keyboardShortcut"/> Open with Ctrl/⌘+K</label>
                      <span class="mt-1 text-xs" :class="$styles.muted">The shortcut opens and focuses the Assistant.</span>
                    </div>
                  </div>
                  <div class="grid sm:grid-cols-2 gap-3">
                    <div>
                      <label class="block text-xs font-semibold">Template</label>
                      <select v-model="config.behavior.template" @change="applyTemplate" class="mt-1 w-full rounded-md" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]">
                        <option value="documentation">Documentation guide</option>
                        <option value="troubleshooting">Technical troubleshooter</option>
                        <option value="support">Customer support</option>
                        <option value="developer">Developer/API assistant</option>
                        <option value="product">Product advisor</option>
                        <option value="onboarding">Onboarding guide</option>
                        <option value="policy">Policy and procedures</option>
                      </select>
                    </div>
                    <div>
                      <label class="block text-xs font-semibold">Response style</label>
                      <select v-model="config.behavior.responseStyle" class="mt-1 w-full rounded-md" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]">
                        <option value="concise">Concise</option>
                        <option value="balanced">Balanced</option>
                        <option value="detailed">Detailed</option>
                      </select>
                    </div>  
                  </div>
                  <div>
                    <label class="block text-xs font-semibold">System prompt</label>
                    <textarea v-model="config.behavior.systemPrompt" rows="10" class="mt-1 w-full rounded-md" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]"></textarea>
                    <span class="block mt-1 font-normal" :class="$styles.muted">Specialist instructions combined with server-owned RAG and safety rules. Stored server-side and never included in the widget JavaScript.</span>
                  </div>
                  <div class="flex flex-wrap gap-5">
                    <label class="inline-flex items-center gap-2 text-sm"><CheckBox v-model="config.behavior.grounded"/> Require grounded answers</label>
                    <label class="inline-flex items-center gap-2 text-sm"><CheckBox v-model="config.behavior.citations"/> Include citations</label>
                  </div>
                  <div>
                    <label class="block text-xs font-semibold">Fallback message</label>
                    <input type="text" v-model="config.behavior.fallback" class="mt-1 w-full rounded-md" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]">
                  </div>
                  <div>
                    <label class="block text-xs font-semibold">Conversation notice</label>
                    <input type="text" v-model="config.behavior.notice" placeholder="Leave blank to hide" class="mt-1 w-full rounded-md" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]">
                  </div>
                </section>

                <section class="rounded-lg border p-4 space-y-4" :class="$styles.chromeBorder">
                  <div class="flex items-center justify-between gap-3">
                    <div>
                      <h3 class="font-semibold">Appearance</h3>
                      <p class="text-xs mt-0.5" :class="$styles.muted">Each theme is a preset. Override its CSS variables below.</p>
                    </div>
                    <button v-if="hasAppearanceOverrides" type="button" @click="resetAppearance" class="text-xs underline hover:text-blue-600 dark:hover:text-blue-400" :class="$styles.muted">Reset theme appearance</button>
                  </div>
                  <div class="grid sm:grid-cols-2 gap-3">
                    <div>
                      <label class="text-xs font-semibold">Theme</label>
                      <select v-model="config.appearance.theme" class="mt-1 w-full rounded-md" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]">
                        <option value="auto">Auto</option>
                        <option value="light">Light</option>
                        <option value="dark">Dark</option>
                        <option value="nord">Nord</option>
                        <option value="matrix">Matrix</option>
                        <option value="soft-pink">Soft Pink</option>
                      </select>
                    </div>
                    <div>
                      <label class="text-xs font-semibold">Panel size</label>
                      <select v-model="config.appearance.panelSize" class="mt-1 w-full rounded-md" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]">
                        <option value="compact">Compact</option>
                        <option value="standard">Standard</option>
                      </select>
                    </div>
                  </div>
                  <div class="border-t pt-3" :class="$styles.chromeBorder">
                    <div v-if="config.appearance.theme === 'auto'" class="rounded-md bg-gray-50 dark:bg-gray-900 px-3 py-3 text-sm" :class="$styles.muted">
                      Auto follows <code>prefers-color-scheme</code> and uses the configured Light or Dark appearance. This preview currently resolves to <strong>{{ resolvedTheme }}</strong>.
                    </div>
                    <div v-else class="space-y-5">
                      <div class="grid sm:grid-cols-2 gap-x-6 gap-y-5">
                        <div v-for="group in bubbleColorGroups" :key="group.label" class="space-y-3">
                          <h4 class="text-xs font-semibold uppercase tracking-wide" :class="$styles.muted">{{ group.label }}</h4>
                          <div v-for="color in group.fields" :key="color.key" class="text-xs">
                            <span class="font-semibold">{{ color.label }}</span>
                            <span class="ml-1 font-mono font-normal" :class="$styles.muted">--{{ color.key }}</span>
                            <div class="mt-1 flex items-center gap-2">
                              <input type="color" :value="colorValue(color.key)" @input="setColor(color.key, $event.target.value)" :aria-label="'Choose ' + color.label + ' color'" class="size-9 shrink-0 rounded border cursor-pointer" :class="$styles.chromeBorder">
                              <input type="text" :value="colorValue(color.key)" @change="setColorText(color.key, $event)" @keydown.enter.prevent="$event.target.blur()" maxlength="7" pattern="#[0-9a-fA-F]{6}" spellcheck="false" :aria-label="color.label + ' hex color'" class="min-w-0 w-24 rounded-md border px-2 py-1.5 text-xs font-mono font-normal bg-white dark:bg-gray-900" :class="$styles.chromeBorder">
                              <button v-if="hasColorOverride(color.key)" type="button" @click="resetColor(color.key)" class="text-xs underline" :class="$styles.muted">reset</button>
                            </div>
                          </div>
                        </div>
                      </div>
                      <div class="grid sm:grid-cols-2 gap-x-6"><div v-for="(column, columnIndex) in lowerColorColumns" :key="columnIndex" class="space-y-5">
                        <div v-for="group in column" :key="group.label" class="space-y-3">
                          <h4 class="text-xs font-semibold uppercase tracking-wide" :class="$styles.muted">{{ group.label }}</h4>
                          <div v-for="color in group.fields" :key="color.key" class="text-xs">
                            <span class="font-semibold">{{ color.label }}</span>
                            <span class="ml-1 font-mono font-normal" :class="$styles.muted">--{{ color.key }}</span><div class="mt-1 flex items-center gap-2">
                            <input type="color" :value="colorValue(color.key)" @input="setColor(color.key, $event.target.value)" :aria-label="'Choose ' + color.label + ' color'" class="size-9 shrink-0 rounded border cursor-pointer" :class="$styles.chromeBorder">
                            <input type="text" :value="colorValue(color.key)" @change="setColorText(color.key, $event)" @keydown.enter.prevent="$event.target.blur()" maxlength="7" pattern="#[0-9a-fA-F]{6}" spellcheck="false" :aria-label="color.label + ' hex color'" class="min-w-0 w-24 rounded-md border px-2 py-1.5 text-xs font-mono font-normal bg-white dark:bg-gray-900" :class="$styles.chromeBorder">
                            <button v-if="hasColorOverride(color.key)" type="button" @click="resetColor(color.key)" class="text-xs underline" :class="$styles.muted">reset</button>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                  <div class="border-t pt-4" :class="$styles.chromeBorder">
                    <div class="flex items-center justify-between gap-3 mb-1">
                      <label class="text-xs font-semibold" for="assistant-font-family">
                        Font family <span class="font-mono font-normal" :class="$styles.muted">--font-family</span>
                      </label>
                      <button v-if="hasFontOverride" type="button" @click="resetFont" class="text-xs underline" :class="$styles.muted">reset</button>
                    </div>
                    <input id="assistant-font-family" type="text" :value="fontFamily" @change="setFont($event.target.value)" class="mt-1 w-full rounded-md" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]">
                  </div>
                </div>
              </div>
            </section>

                <section class="rounded-lg border p-4 space-y-4" :class="$styles.chromeBorder">
                  <div><h3 class="font-semibold">Button</h3><p class="text-xs" :class="$styles.muted">Customize the floating launcher without exposing arbitrary CSS to the host page.</p></div>
                  <div class="grid sm:grid-cols-2 gap-3">
                    <div><label class="text-xs font-semibold">Button position</label><select v-model="config.appearance.position" class="mt-1 w-full rounded-md" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]"><option value="bottom-right">Bottom right</option><option value="bottom-left">Bottom left</option></select></div>
                    <div><label class="text-xs font-semibold">Button size</label><input v-model.number="config.appearance.button.size" type="number" min="40" max="96" class="mt-1 w-full rounded-md" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]"><span class="block mt-1 text-xs" :class="$styles.muted">40–96 px</span></div>
                    <div><label class="text-xs font-semibold">Icon size</label><input v-model.number="config.appearance.button.iconSize" type="number" min="16" max="72" class="mt-1 w-full rounded-md" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]"><span class="block mt-1 text-xs" :class="$styles.muted">16–72 px</span></div>
                    <div><label class="text-xs font-semibold">Corner radius</label><input v-model.number="config.appearance.button.borderRadius" type="number" min="0" max="50" class="mt-1 w-full rounded-md" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]"><span class="block mt-1 text-xs" :class="$styles.muted">Percentage; 50 is circular</span></div>
                    <div><label class="text-xs font-semibold">Shadow</label><select v-model="config.appearance.button.shadow" class="mt-1 w-full rounded-md" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]"><option value="none">None</option><option value="subtle">Subtle</option><option value="medium">Medium</option><option value="strong">Strong</option></select></div>
                    <div><label class="text-xs font-semibold">Border width</label><input v-model.number="config.appearance.button.borderWidth" type="number" min="0" max="8" class="mt-1 w-full rounded-md" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]"><span class="block mt-1 text-xs" :class="$styles.muted">0–8 px</span></div>
                    <div v-for="color in buttonColorFields" :key="color.key" class="text-xs"><span class="font-semibold">{{ color.label }}</span><div class="mt-1 flex items-center gap-2"><input type="color" :value="buttonColorValue(color.key)" @input="setButtonColor(color.key, $event.target.value)" :aria-label="'Choose ' + color.label + ' color'" class="size-9 shrink-0 rounded border cursor-pointer" :class="$styles.chromeBorder"><input type="text" :value="buttonColorValue(color.key)" @change="setButtonColorText(color.key, $event)" @keydown.enter.prevent="$event.target.blur()" maxlength="7" pattern="#[0-9a-fA-F]{6}" spellcheck="false" :aria-label="color.label + ' hex color'" class="min-w-0 w-24 rounded-md border px-2 py-1.5 text-xs font-mono font-normal bg-white dark:bg-gray-900" :class="$styles.chromeBorder"><button v-if="hasButtonColorOverride(color.key)" type="button" @click="resetButtonColor(color.key)" class="text-xs underline" :class="$styles.muted">reset</button></div></div>
                    <div><label class="text-xs font-semibold">Built-in icon</label><select v-model="config.appearance.icon" :disabled="!!launcherDataUri" class="mt-1 w-full rounded-md disabled:opacity-50" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]"><option value="sparkles">Sparkles</option><option value="chat">Chat</option><option value="help">Help</option></select></div>
                  </div>
                  <div><label class="text-xs font-semibold">Custom icon Data URI</label><textarea v-model.trim="config.appearance.button.iconDataUri" rows="3" maxlength="200000" placeholder="data:image/svg+xml,... or data:image/png;base64,..." class="mt-1 w-full rounded-md font-mono text-xs" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]"></textarea><span class="block mt-1 text-xs" :class="$styles.muted">Supports PNG, JPEG, GIF, WebP, and SVG images. When set, it replaces the built-in icon.</span></div>
                </section>

                <section class="rounded-lg border p-4 space-y-3" :class="$styles.chromeBorder">
                  <div>
                    <h3 class="font-semibold">Hosting and access</h3>
                    <p class="text-xs" :class="$styles.muted">Leave origins empty to allow the widget on any website.</p>
                  </div>
                  <div>
                    <label class="block text-xs font-semibold">Allowed origins</label>
                    <textarea v-model="originsText" rows="4" placeholder="https://docs.example.com\nhttps://*.example.com\nhttp://localhost:5173" class="mt-1 w-full rounded-md" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]"></textarea>
                  </div>
                  <div>
                    <label class="block text-xs font-semibold max-w-52">Requests per minute</label>
                    <input v-model.number="config.hosting.requestsPerMinute" type="number" min="1" max="1000" class="mt-1 w-full rounded-md" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]">
                  </div>
                </section>

                </fieldset>

                <section v-if="selected" class="rounded-lg border p-4 space-y-3" :class="$styles.chromeBorder"><div class="flex items-center justify-between"><div><h3 class="font-semibold">Deployment</h3><p class="text-xs" :class="$styles.muted">Only appearance can be overridden by data attributes.</p></div><span class="text-xs font-medium" :class="archived ? 'text-orange-600 dark:text-orange-400' : selected.published ? 'text-green-600' : $styles.muted">{{ archived ? 'Archived' : selected.published ? 'Published' : 'Draft' }}</span></div>
                  <template v-if="selected.published"><div class="relative"><textarea readonly rows="3" :value="selected.embedCode" class="w-full px-2.5 py-1.5 pr-9 rounded-md text-xs font-normal border font-mono bg-gray-50 dark:bg-gray-950" :class="$styles.chromeBorder"></textarea><button type="button" @click="copyEmbed" class="absolute top-2 right-2 p-1 rounded text-gray-500 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 hover:bg-black/5 dark:hover:bg-white/10" :title="copiedEmbed ? 'Copied to clipboard' : 'Copy embed code'"><svg v-if="copiedEmbed" class="size-4 text-green-600 dark:text-green-500" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="m9.55 18l-5.7-5.7l1.425-1.425L9.55 15.15l9.175-9.175L20.15 7.4z"/></svg><svg v-else xmlns="http://www.w3.org/2000/svg" class="size-4" viewBox="0 0 24 24"><path fill="currentColor" d="M16 1H4c-1.1 0-2 .9-2 2v14h2V3h12zm3 4H8c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h11c1.1 0 2-.9 2-2V7c0-1.1-.9-2-2-2m0 16H8V7h11z"/></svg></button></div><div class="flex gap-2"><button @click="save(false)" class="px-3 py-1.5 rounded-md text-sm border hover:bg-gray-50 dark:hover:bg-gray-800" :class="$styles.secondaryButton">Unpublish</button><button @click="regenerate" class="px-3 py-1.5 rounded-md border border-red-600 text-sm font-medium text-red-600 hover:bg-red-50 dark:hover:bg-red-950/30">Regenerate ID</button></div></template>
                  <div class="flex flex-wrap gap-2"><button v-if="archived" type="button" @click="restore" :disabled="busy" class="px-3 py-1.5 rounded-md text-sm font-medium disabled:opacity-50" :class="$styles.secondaryButton">Restore Assistant</button><button v-else type="button" @click="archive" :disabled="busy" class="px-3 py-1.5 rounded-md border border-red-600 text-sm font-medium text-red-600 hover:bg-red-50 dark:hover:bg-red-950/30 disabled:opacity-50">Archive assistant</button><button type="button" @click="openDelete" :disabled="busy" class="px-3 py-1.5 rounded-md border border-red-600 bg-red-600 text-sm font-semibold text-white hover:bg-red-700 hover:border-red-700 disabled:opacity-50">Delete permanently</button></div>
                </section>
                <div class="flex flex-wrap items-center justify-end gap-2 border-t pt-4" :class="$styles.chromeBorder">
                  <button v-if="!archived && !selected?.published" type="button" @click="save(false)" :disabled="busy || !canSaveDraft" class="px-4 py-2 rounded-md text-sm border font-medium hover:bg-gray-50 dark:hover:bg-gray-800 disabled:opacity-50" :class="$styles.secondaryButton">Save draft</button>
                  <button v-if="!archived" type="button" @click="save(true)" :disabled="busy || !canPublish" class="px-4 py-2 rounded-md text-sm font-semibold disabled:opacity-50" :class="$styles.primaryButton">{{ selected?.published ? 'Update published' : 'Publish' }}</button>
                </div>
              </div>

              <aside class="xl:sticky xl:top-3">
                <h3 class="text-sm font-semibold mb-2">Live preview</h3>
                <div
                  class="relative h-[34rem] rounded-xl border border-gray-200 overflow-hidden bg-gradient-to-br from-gray-100 to-gray-200 dark:from-gray-900 dark:to-gray-950">
                  <div class="absolute bottom-4 max-w-[calc(100%_-_2rem)]"
                    :class="[config.appearance.position === 'bottom-left' ? 'left-4' : 'right-4', config.appearance.panelSize === 'compact' ? 'w-[17rem]' : 'w-[19rem]']">
                    <div class="rounded-2xl overflow-hidden shadow-2xl border"
                      :style="{backgroundColor:palette['panel-bg'],color:palette['primary-text'],borderColor:palette['panel-border'],fontFamily}">
                      <div class="group px-4 py-3 text-white flex items-start gap-2" :style="{backgroundColor:palette['accent-bg']}">
                        <div class="grow min-w-0 cursor-pointer"><div class="flex items-center gap-1"><p class="font-semibold text-sm">{{ config.identity.title }}</p><svg class="size-4 shrink-0 opacity-0 group-hover:opacity-100 transition-opacity" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M8 3H3v5M16 3h5v5M8 21H3v-5M16 21h5v-5"/></svg></div><p class="text-xs opacity-80">{{ config.identity.description }}</p></div><span class="size-7 grid place-items-center rounded-md"><svg class="size-[18px]" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><path d="M6 6l12 12M18 6 6 18"/></svg></span>
                      </div>
                      <div class="h-72 p-3 overflow-y-auto text-sm space-y-2" :style="{backgroundColor:palette['conversation-bg']}">
                        <div>
                          <div class="inline-block rounded-xl border p-3"
                            :style="{backgroundColor:palette['assistant-bg'],color:palette['assistant-text'],borderColor:palette['assistant-border']}">
                            {{ config.identity.welcome }}</div>
                        </div>
                        <div class="flex justify-end">
                          <div class="inline-block rounded-xl border p-3"
                            :style="{backgroundColor:palette['user-bg'],color:palette['user-text'],borderColor:palette['user-border']}">
                            How do I get started?</div>
                        </div>
                        <div>
                          <div class="inline-block rounded-xl border p-3"
                            :style="{backgroundColor:palette['assistant-bg'],color:palette['assistant-text'],borderColor:palette['assistant-border']}">
                            I can help you find the right documentation and next steps.</div>
                        </div>
                        <div class="text-center text-[11px] text-gray-400 hover:underline" title="Clear this conversation and start a new one">clear</div>
                      </div>
                      <div class="border-t p-2" :style="{borderColor:palette['panel-border']}"><div class="flex gap-2">
                        <div class="grow rounded-lg border px-2 py-2 text-xs" :style="{color:palette['muted-text'],borderColor:palette['panel-border']}">Ask a question…</div><div class="size-8 rounded-lg text-white grid place-items-center" :style="{backgroundColor:palette['accent-bg']}">➤</div>
                      </div><div v-if="config.behavior.notice" class="mt-1.5 text-center text-[9px]" :style="{color:palette['muted-text']}">{{ config.behavior.notice }}</div>
                      </div>
                    </div>
                    <div class="mt-3 ml-auto text-white grid place-items-center overflow-hidden"
                      :style="launcherButtonStyle"><img v-if="launcherDataUri" :src="launcherDataUri" alt="" :style="launcherIconStyle"><span v-else v-html="launcherIcon" :style="launcherIconStyle" class="grid place-items-center [&>svg]:size-full"></span></div>
                  </div>
                </div>
              </aside>

            </div>
          </div>
          <div v-else class="rounded-lg border p-10 mb-10 text-center" :class="$styles.chromeBorder">
            <p class="font-medium">Create an Assistant for this File Store</p><p class="text-sm mt-1 mb-4" :class="$styles.muted">Choose its document scope, behavior, appearance, and allowed websites.</p>
            <button @click="newAssistant" class="px-3 py-1.5 rounded-md text-sm font-semibold" :class="$styles.primaryButton">New assistant</button>
        </div>
      </div>
    </div>`,
    setup(props, { emit }) {
        const items = ref([]), selected = ref(null), editing = ref(false), name = ref(''), config = ref(defaults())
        const loading = ref(false), busy = ref(false), error = ref(null), showConversations = ref(false), copiedEmbed = ref(false)
        const conversations = ref([]), conversation = ref(null), conversationPane = ref(null), conversationNavOpen = ref(false)
        const deleteOpen = ref(false), deleteBusy = ref(false), deleteLoading = ref(false)
        const deleteSummary = ref(null), deleteConfirmation = ref('')
        const savedSnapshot = ref('')
        const formSnapshot = computed(() => JSON.stringify({ name:name.value.trim(), config:config.value }))
        const dirty = computed(() => formSnapshot.value !== savedSnapshot.value)
        const archived = computed(() => !!selected.value && Number(selected.value.enabled) === 0)
        const canSaveDraft = computed(() => !archived.value && !!name.value.trim() && dirty.value)
        const canPublish = computed(() => !archived.value && !!name.value.trim() && (dirty.value || !selected.value?.published))
        const conversationTurns = computed(() => {
            const messages = conversation.value?.messages || []
            return messages.flatMap((message, index) => {
                if (message.role !== 'user') return []
                let response = null
                for (let i = index + 1; i < messages.length; i++) {
                    if (messages[i].role === 'user') break
                    if (messages[i].role === 'assistant') { response = messages[i]; break }
                }
                const text = String(message.content || '').replace(/\s+/g, ' ').trim()
                return [{ userId:message.id, targetId:response?.id ?? message.id, label:text.length > 80 ? `${text.slice(0, 77)}…` : text || '(empty message)' }]
            })
        })
        const colorScheme = globalThis.matchMedia?.('(prefers-color-scheme: dark)')
        const prefersDark = ref(colorScheme?.matches || false)
        const originsText = computed({ get: () => (config.value.hosting.allowedOrigins || []).join('\n'), set: v => config.value.hosting.allowedOrigins = v.split(/[\n,]/).map(x => x.trim()).filter(Boolean) })
        const scopeSummary = computed(() => Object.entries(config.value.scope || {}).filter(([,v]) => v).map(([k,v]) => `${k} = ${v}`).join(' · ') || 'All documents in this File Store')
        const resolvedTheme = computed(() => config.value.appearance.theme === 'auto' ? (prefersDark.value ? 'dark' : 'light') : config.value.appearance.theme)
        const presetPalette = computed(() => THEME_PRESETS[resolvedTheme.value] || THEME_PRESETS.light)
        const palette = computed(() => ({ ...presetPalette.value, ...((config.value.appearance.colors || {})[resolvedTheme.value] || {}) }))
        const hasColorOverrides = computed(() => config.value.appearance.theme !== 'auto' && Object.keys((config.value.appearance.colors || {})[config.value.appearance.theme] || {}).length > 0)
        const fontFamily = computed(() => (config.value.appearance.fonts || {})[resolvedTheme.value] || FONT_PRESETS[resolvedTheme.value] || SYSTEM_FONT)
        const hasFontOverride = computed(() => config.value.appearance.theme !== 'auto' && !!(config.value.appearance.fonts || {})[config.value.appearance.theme])
        const hasAppearanceOverrides = computed(() => hasColorOverrides.value || hasFontOverride.value)
        const launcherIcon = computed(() => LAUNCHER_ICONS[config.value.appearance.icon] || LAUNCHER_ICONS.sparkles)
        const launcherButton = computed(() => ({ ...DEFAULT_BUTTON, ...(config.value.appearance.button || {}) }))
        const launcherDataUri = computed(() => /^data:image\/(?:png|jpeg|gif|webp|svg\+xml)(?:;charset=[^;,]+)?(?:;base64)?,/i.test(launcherButton.value.iconDataUri || '') ? launcherButton.value.iconDataUri : '')
        const launcherButtonStyle = computed(() => {
            const button = launcherButton.value
            const size = Math.min(Math.max(Number(button.size) || 50, 40), 96)
            const shadows = { none:'none', subtle:'0 4px 12px rgba(15,23,42,.16)', medium:'0 10px 30px rgba(15,23,42,.28)', strong:'0 16px 42px rgba(15,23,42,.4)' }
            return { width:`${size}px`, height:`${size}px`, backgroundColor:/^#[0-9a-f]{6}$/i.test(button.background || '') ? button.background : palette.value['accent-bg'], color:/^#[0-9a-f]{6}$/i.test(button.iconColor || '') ? button.iconColor : '#ffffff', borderStyle:'solid', borderWidth:`${Math.min(Math.max(Number(button.borderWidth) || 0, 0), 8)}px`, borderColor:/^#[0-9a-f]{6}$/i.test(button.borderColor || '') ? button.borderColor : palette.value['panel-border'], borderRadius:`${Math.min(Math.max(Number(button.borderRadius) || 0, 0), 50)}%`, boxShadow:shadows[button.shadow] || shadows.medium }
        })
        const launcherIconStyle = computed(() => { const size = Math.min(Math.max(Number(launcherButton.value.iconSize) || 26, 16), 72); return { width:`${size}px`, height:`${size}px`, fontSize:`${size}px`, lineHeight:'1', color:'inherit', objectFit:'contain' } })
        function buttonColorValue(key) { const value = launcherButton.value[key]; if (/^#[0-9a-f]{6}$/i.test(value || '')) return value; return key === 'background' ? palette.value['accent-bg'] : key === 'borderColor' ? palette.value['panel-border'] : '#ffffff' }
        function setButtonColor(key, value) { if (/^#[0-9a-f]{6}$/i.test(value || '')) config.value.appearance.button[key] = value.toLowerCase() }
        function setButtonColorText(key, event) { const value = event.target.value.trim(); if (/^#[0-9a-f]{6}$/i.test(value)) setButtonColor(key, value); else event.target.value = buttonColorValue(key) }
        function hasButtonColorOverride(key) { return key === 'iconColor' ? launcherButton.value[key] !== DEFAULT_BUTTON[key] : !!launcherButton.value[key] }
        function resetButtonColor(key) { config.value.appearance.button[key] = DEFAULT_BUTTON[key] }
        function colorValue(key) { return palette.value[key] }
        function setColor(key, value) { if (config.value.appearance.theme === 'auto') return; if (value.toLowerCase() === presetPalette.value[key]) return resetColor(key); const colors = config.value.appearance.colors || {}; config.value.appearance.colors = { ...colors, [config.value.appearance.theme]:{ ...(colors[config.value.appearance.theme] || {}), [key]:value.toLowerCase() } } }
        function setColorText(key, event) { const value = event.target.value.trim(); if (/^#[0-9a-f]{6}$/i.test(value)) setColor(key, value); else event.target.value = colorValue(key) }
        function hasColorOverride(key) { return Object.prototype.hasOwnProperty.call((config.value.appearance.colors || {})[config.value.appearance.theme] || {}, key) }
        function resetColor(key) { const colors = { ...(config.value.appearance.colors || {}) }; const themeColors = { ...(colors[config.value.appearance.theme] || {}) }; delete themeColors[key]; if (Object.keys(themeColors).length) colors[config.value.appearance.theme] = themeColors; else delete colors[config.value.appearance.theme]; config.value.appearance.colors = colors }
        function resetColors() { const colors = { ...(config.value.appearance.colors || {}) }; delete colors[config.value.appearance.theme]; config.value.appearance.colors = colors }
        function setFont(value) { const font = value.trim(); if (!font || font === FONT_PRESETS[config.value.appearance.theme]) return resetFont(); config.value.appearance.fonts = { ...(config.value.appearance.fonts || {}), [config.value.appearance.theme]:font } }
        function resetFont() { const fonts = { ...(config.value.appearance.fonts || {}) }; delete fonts[config.value.appearance.theme]; config.value.appearance.fonts = fonts }
        function resetAppearance() { resetColors(); resetFont() }
        function facetOptions(field) { return (props.facets?.[field]?.values || []).map(x => typeof x === 'object' ? x : { value:x, count:'' }) }
        function addSuggestion(afterIndex) {
            const suggestions = config.value.identity.suggestions || (config.value.identity.suggestions = [])
            if (suggestions.length >= 6) return
            const index = afterIndex == null ? suggestions.length : afterIndex + 1
            suggestions.splice(index, 0, '')
            nextTick(() => document.querySelector(`[data-suggestion-index="${index}"]`)?.focus())
        }
        function removeSuggestion(index) {
            const suggestions = config.value.identity.suggestions
            suggestions.splice(index, 1)
            if (!suggestions.length) suggestions.push('')
        }
        async function load(syncRoute=true) { loading.value = true; const api = await ext.getJson(`/filestores/${props.storeId}/assistants`); loading.value = false; if (api.error) return error.value = api.error; items.value = api.response || []; emit('count', items.value.length); if (syncRoute) await syncRouteState() }
        function markClean() { savedSnapshot.value = formSnapshot.value }
        function clearConversationState() { showConversations.value = false; conversations.value = []; conversation.value = null; conversationNavOpen.value = false }
        function setAssistant(item) { selected.value = item; name.value = item.name; const value = clone(item.config || defaults()); value.identity.suggestions = value.identity.suggestions?.length ? value.identity.suggestions : ['']; value.behavior = { ...defaults().behavior, ...(value.behavior || {}) }; value.appearance = { ...defaults().appearance, ...(value.appearance || {}), button:{ ...DEFAULT_BUTTON, ...(value.appearance?.button || {}) } }; config.value = value; markClean(); editing.value = true; clearConversationState(); error.value = null }
        function newAssistant() { selected.value = null; name.value = ''; config.value = defaults(); markClean(); editing.value = true; clearConversationState(); error.value = null; emit('navigate', { assistant:null, conversations:null, conversation:null }) }
        function selectAssistant(item) { setAssistant(item); emit('navigate', { assistant:item.id, conversations:null, conversation:null }) }
        function applyTemplate() { config.value.behavior.systemPrompt = PROMPTS[config.value.behavior.template] || PROMPTS.documentation }
        async function save(published, extra={}) { if (archived.value) return error.value = { message:'Restore this Assistant before editing or publishing it' }; if (!name.value.trim()) return error.value = { message:'Name is required' }; const existed = !!selected.value, wasPublished = !!selected.value?.published; busy.value = true; error.value = null; const body = { name:name.value.trim(), config:clone(config.value), published, ...extra }; const api = selected.value ? await ext.putJson(`/assistants/${selected.value.id}`, body) : await ext.postJson(`/filestores/${props.storeId}/assistants`, body); busy.value = false; if (api.error) return error.value = api.error; await load(false); const fresh = items.value.find(x => x.id === api.response.id) || api.response; selectAssistant(fresh); const message = extra.regeneratePublicId ? 'Assistant ID regenerated' : published ? (wasPublished ? 'Published assistant updated' : 'Assistant published') : wasPublished ? 'Assistant unpublished' : existed ? 'Draft saved' : 'Draft created'; ctx.toast(message) }
        async function loadConversations() { if (!selected.value) return; const api = await ext.getJson(`/assistants/${selected.value.id}/conversations`); if (!api.error) { conversations.value = api.response || []; const count = Math.max(Number(selected.value.conversationCount || 0), conversations.value.length); selected.value = { ...selected.value, conversationCount:count }; items.value = items.value.map(x => x.id === selected.value.id ? { ...x, conversationCount:count } : x) } }
        async function toggleConversations() { showConversations.value = !showConversations.value; conversation.value = null; if (showConversations.value) await loadConversations(); emit('navigate', { assistant:selected.value.id, conversations:showConversations.value ? '1' : null, conversation:null }) }
        async function openConversation(item, navigate=true) { const api = await ext.getJson(`/assistants/${selected.value.id}/conversations/${item.id}`); if (!api.error) { conversation.value = api.response; conversationNavOpen.value = false; nextTick(() => conversationPane.value?.scrollTo({ top:0 })); if (navigate) emit('navigate', { assistant:selected.value.id, conversations:'1', conversation:item.id }) } }
        async function syncRouteState() {
            const assistantId = Number(props.routeAssistant)
            if (!Number.isInteger(assistantId) || assistantId <= 0) {
                if (selected.value) { selected.value = null; editing.value = false; clearConversationState() }
                return
            }
            const item = items.value.find(x => x.id === assistantId)
            if (!item) return
            if (selected.value?.id !== item.id) setAssistant(item)
            const wantsConversations = props.routeConversations === '1' || !!props.routeConversation
            if (!wantsConversations) {
                showConversations.value = false
                conversation.value = null
                return
            }
            showConversations.value = true
            await loadConversations()
            const conversationId = Number(props.routeConversation)
            if (Number.isInteger(conversationId) && conversationId > 0) {
                if (conversation.value?.id !== conversationId) await openConversation({ id:conversationId }, false)
            } else conversation.value = null
        }
        function jumpToConversationResponse(id) {
            conversationNavOpen.value = false
            if (!id) return
            nextTick(() => {
                const target = conversationPane.value?.querySelector(`[data-conversation-message-id="${id}"]`)
                if (target) conversationPane.value.scrollTo({ top:Math.max(0, target.offsetTop - 12), behavior:'smooth' })
            })
        }
        async function copyEmbed() { await navigator.clipboard.writeText(selected.value.embedCode); copiedEmbed.value = true; setTimeout(() => copiedEmbed.value = false, 2000) }
        async function regenerate() { if (confirm('Regenerate the public ID? Existing embed codes will stop working.')) await save(true, { regeneratePublicId:true }) }
        async function archive() {
            if (!selected.value || archived.value || !confirm('Archive this Assistant? Its public widget will stop working, but customer conversations will be retained.')) return
            const assistantId = selected.value.id
            busy.value = true
            let api
            try {
                api = await ext.deleteJson(`/assistants/${assistantId}`)
            } finally {
                busy.value = false
            }
            if (api.error) return error.value = api.error
            await load(false)
            const fresh = items.value.find(x => x.id === assistantId)
            if (fresh) setAssistant(fresh)
            emit('navigate', { assistant:assistantId, conversations:null, conversation:null })
            ctx.toast('Assistant archived')
        }
        async function restore() {
            if (!selected.value || !archived.value) return
            const assistantId = selected.value.id
            busy.value = true
            let api
            try {
                api = await ext.postJson(`/assistants/${assistantId}/restore`, {})
            } finally {
                busy.value = false
            }
            if (api.error) return error.value = api.error
            await load(false)
            const fresh = items.value.find(x => x.id === assistantId) || api.response
            setAssistant(fresh)
            emit('navigate', { assistant:assistantId, conversations:null, conversation:null })
            ctx.toast('Assistant restored as a draft')
        }
        async function openDelete() {
            if (!selected.value) return
            deleteOpen.value = true
            deleteLoading.value = true
            deleteSummary.value = null
            deleteConfirmation.value = ''
            const assistantId = selected.value.id
            try {
                const api = await ext.getJson(`/assistants/${assistantId}/delete-summary`)
                if (api.error) {
                    error.value = api.error
                    deleteOpen.value = false
                } else if (selected.value?.id === assistantId) {
                    deleteSummary.value = api.response
                }
            } catch (e) {
                error.value = { message:e?.message || String(e) }
                deleteOpen.value = false
            } finally {
                deleteLoading.value = false
            }
        }
        function closeDelete() {
            if (deleteBusy.value) return
            deleteOpen.value = false
            deleteSummary.value = null
            deleteConfirmation.value = ''
        }
        async function deletePermanently() {
            if (!selected.value || !deleteSummary.value || deleteConfirmation.value !== deleteSummary.value.name) return
            deleteBusy.value = true
            let api
            try {
                api = await ext.deleteJson(`/assistants/${selected.value.id}/permanent`, {
                    headers: { 'Content-Type':'application/json' },
                    body: JSON.stringify({ confirm:deleteConfirmation.value }),
                })
            } catch (e) {
                error.value = { message:e?.message || String(e) }
                return
            } finally {
                deleteBusy.value = false
            }
            if (api.error) return error.value = api.error
            deleteOpen.value = false
            selected.value = null
            editing.value = false
            clearConversationState()
            emit('navigate', { assistant:null, conversations:null, conversation:null })
            await load(false)
        }
        watch(() => props.storeId, () => load())
        watch([() => props.routeAssistant, () => props.routeConversations, () => props.routeConversation], syncRouteState)
        const onColorScheme = event => prefersDark.value = event.matches
        onMounted(() => { load(); colorScheme?.addEventListener?.('change', onColorScheme) })
        onBeforeUnmount(() => colorScheme?.removeEventListener?.('change', onColorScheme))
        return { items, selected, editing, name, config, loading, busy, error, copiedEmbed, dirty, archived, canSaveDraft, canPublish, scopeFields:SCOPE_FIELDS, bubbleColorGroups:BUBBLE_COLOR_GROUPS, lowerColorColumns:LOWER_COLOR_COLUMNS, buttonColorFields:BUTTON_COLOR_FIELDS, originsText, scopeSummary, resolvedTheme, palette, fontFamily, hasFontOverride, hasAppearanceOverrides, launcherIcon, launcherDataUri, launcherButtonStyle, launcherIconStyle, buttonColorValue, setButtonColor, setButtonColorText, hasButtonColorOverride, resetButtonColor, colorValue, setColor, setColorText, hasColorOverride, resetColor, resetColors, setFont, resetFont, resetAppearance, facetOptions, addSuggestion, removeSuggestion, newAssistant, selectAssistant, applyTemplate, save, showConversations, conversations, conversation, conversationPane, conversationTurns, conversationNavOpen, loadConversations, toggleConversations, openConversation, jumpToConversationResponse, copyEmbed, regenerate, archive, restore, deleteOpen, deleteBusy, deleteLoading, deleteSummary, deleteConfirmation, openDelete, closeDelete, deletePermanently }
    }
}
