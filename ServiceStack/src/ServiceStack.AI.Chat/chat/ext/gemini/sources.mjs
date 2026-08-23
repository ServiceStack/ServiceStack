import { ref, computed, onMounted } from 'vue'
import { PathText } from './explorer.mjs'
import { MetaChip } from './metadata.mjs'

let ext = null
export function initSources(extScope) {
    ext = extScope
}

/**
 * Dry-run review — the screen where someone commits embedding spend.
 *
 * Leads with `unchanged`, because that's the number that makes a re-sync feel safe to run, and
 * shows which rules matched how many files: that's where you find out a rule matched nothing
 * because the folder is called `ref` rather than `reference`.
 * (Glob patterns are kept out of block comments here on purpose: a `*` followed by `/` closes
 *  the comment, and the remainder stays valid JavaScript, so it fails at runtime not at parse.)
 */
export const RunReport = {
    components: { MetaChip },
    template: `
        <div v-if="run" class="rounded-lg border p-4 space-y-3" :class="[$styles.chromeBorder]">
            <div class="flex items-center justify-between gap-3 flex-wrap">
                <h4 class="font-semibold text-sm">
                    {{ run.dryRun ? 'Preview — nothing written yet' : 'Run complete' }}
                </h4>
                <div v-if="run.dryRun" class="flex gap-2">
                    <button type="button" @click="$emit('confirm')" :disabled="!run.embeds"
                        class="px-4 py-2 rounded-md text-sm font-semibold text-white bg-blue-600 hover:bg-blue-700 disabled:opacity-40 disabled:cursor-not-allowed">
                        Import {{ (run.embeds || 0).toLocaleString() }} document{{ run.embeds === 1 ? '' : 's' }}
                    </button>
                    <!-- No Discard: a preview costs nothing and changes nothing, so there is
                         nothing to discard. Whatever opened this closes it. -->
                </div>
            </div>

            <div v-if="run.deleteRefused" class="px-3 py-2 rounded border text-xs border-red-500 text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/20">
                {{ run.deleteRefused }}
            </div>

            <div class="grid grid-cols-2 sm:grid-cols-3 gap-x-6 gap-y-1 text-sm">
                <div class="flex justify-between"><span :class="[$styles.muted]">Discovered</span><span class="tabular-nums">{{ (run.discovered||0).toLocaleString() }}</span></div>
                <div class="flex justify-between"><span :class="[$styles.muted]">New</span><span class="tabular-nums font-semibold">{{ (run.added||0).toLocaleString() }}</span></div>
                <div class="flex justify-between"><span :class="[$styles.muted]">Changed</span><span class="tabular-nums font-semibold">{{ (run.changed||0).toLocaleString() }}</span></div>
                <div class="flex justify-between"><span :class="[$styles.muted]">Metadata only</span><span class="tabular-nums">{{ (run.metadataOnly||0).toLocaleString() }}</span></div>
                <div class="flex justify-between text-emerald-600 dark:text-emerald-400"><span>Unchanged</span><span class="tabular-nums">{{ (run.unchanged||0).toLocaleString() }}</span></div>
                <div class="flex justify-between"><span :class="[$styles.muted]">Removed</span><span class="tabular-nums">{{ (run.removed||0).toLocaleString() }}</span></div>
                <div class="flex justify-between"><span :class="[$styles.muted]">Skipped</span><span class="tabular-nums">{{ (run.skipped||0).toLocaleString() }}</span></div>
                <div class="flex justify-between"><span :class="[$styles.muted]">Failed</span><span class="tabular-nums">{{ (run.failed||0).toLocaleString() }}</span></div>
                <div class="flex justify-between text-amber-600 dark:text-amber-400"><span>Embeds</span><span class="tabular-nums font-semibold">{{ (run.embeds||0).toLocaleString() }}</span></div>
            </div>

            <div v-if="rules.length" class="text-xs">
                <div class="font-semibold mb-1" :class="[$styles.muted]">Rules matched</div>
                <div v-for="[pattern, n] in rules" :key="pattern" class="flex justify-between gap-4 py-0.5">
                    <code class="truncate">{{ pattern }}</code>
                    <span class="tabular-nums shrink-0" :class="n ? '' : 'text-red-500'">{{ n.toLocaleString() }} file{{ n === 1 ? '' : 's' }}</span>
                </div>
            </div>

            <div v-if="preview.length" class="text-xs">
                <div class="font-semibold mb-2" :class="[$styles.muted]">Derived metadata (sample)</div>
                <div class="divide-y divide-gray-200 dark:divide-gray-700">
                    <div v-for="d in preview" :key="d.sourceKey" class="py-3 first:pt-1 last:pb-1 space-y-2">
                        <div class="font-mono truncate leading-5">{{ d.sourceKey }}</div>
                        <!-- Expanded per document, so a {category}/{name} template is checkable here
                             rather than after 1,500 documents have been indexed with a bad link. -->
                        <a v-if="d.sourceUrl" :href="d.sourceUrl" target="_blank" rel="noopener noreferrer"
                            class="block font-mono truncate text-[11px] leading-5 text-blue-600 dark:text-blue-400 hover:underline"
                            :title="'Open source URL: ' + d.sourceUrl">{{ d.sourceUrl }}</a>
                        <div class="flex flex-wrap gap-1.5">
                            <MetaChip v-for="f in metaFields(d)" :key="f.id" :field="f.k" :value="f.v" />
                        </div>
                    </div>
                </div>
            </div>

            <details v-if="samples.length" class="text-xs">
                <summary class="cursor-pointer" :class="[$styles.muted]">Skipped &amp; failed</summary>
                <div v-for="s in samples" :key="s.sourceKey" class="flex justify-between gap-4 py-0.5">
                    <code class="truncate">{{ s.sourceKey }}</code><span :class="[$styles.muted]">{{ s.reason }}</span>
                </div>
            </details>
        </div>
    `,
    props: { run: Object },
    emits: ['confirm', 'dismiss'],
    setup(props) {
        const rules = computed(() => Object.entries(props.run?.rulesMatched || {}))
        const preview = computed(() => props.run?.preview || [])
        const samples = computed(() => [
            ...(props.run?.samples?.skipped || []),
            ...(props.run?.samples?.failed || []),
        ])
        function metaFields(d) {
            return ['category', 'docType', 'status', 'locale', 'product', 'versions', 'tags']
                .filter(k => d[k] != null && d[k] !== '' && !(Array.isArray(d[k]) && !d[k].length))
                .flatMap(k => (Array.isArray(d[k]) ? d[k] : [d[k]])
                    .map((v, i) => ({ k, v, id: `${k}:${i}:${v}` })))
        }
        return { rules, preview, samples, metaFields }
    },
}

export const SourcesPanel = {
    components: { RunReport, PathText },
    template: `
        <div class="space-y-4">
            <div>
                <h3 class="font-semibold" :class="[$styles.heading]">Saved imports</h3>
                <p class="text-xs" :class="[$styles.muted]">Re-run these to pick up changes. Create one by ticking “Save as a recurring import” above.</p>
            </div>

            <div v-for="s in sources" :key="s.id" class="rounded-lg border p-4 sm:p-5 space-y-4" :class="[$styles.chromeBorder]">
                <div class="flex flex-col sm:flex-row sm:items-start justify-between gap-4">
                    <div class="min-w-0 flex-1 space-y-2">
                        <div class="flex items-center gap-2 flex-wrap">
                            <span class="shrink-0 px-2 py-0.5 rounded text-[11px] font-medium"
                                :class="[$styles.tagLabel]" :title="'Import type: ' + s.type">{{ s.type }}</span>
                            <span class="font-medium text-sm">{{ s.name }}</span>
                            <!-- Where the import lands. A folder import without one scatters into
                                 the root, which is worth seeing rather than guessing. -->
                            <span v-if="s.category?.prefix"
                                class="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[11px] border"
                                :class="[$styles.chromeBorder]" :title="'Destination category: ' + s.category.prefix">
                                <svg class="size-3 shrink-0 opacity-70" viewBox="0 0 24 24" fill="none"
                                    stroke="currentColor" stroke-width="2" stroke-linecap="round"
                                    stroke-linejoin="round" aria-hidden="true">
                                    <path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/>
                                </svg>
                                {{ s.category.prefix }}
                            </span>
                        </div>
                        <PathText class="text-xs block" :path="s.config?.path || ''" :max="52" :class="[$styles.muted]" />
                        <div class="text-xs" :class="[$styles.muted]">
                            <span v-if="s.lastRunAt">Last run {{ $fmt.date(s.lastRunAt) }}</span>
                            <span v-else>Not run yet</span>
                        </div>
                    </div>
                    <div class="flex gap-2 shrink-0 self-start">
                        <!-- A toggle, so the button always describes what pressing it does. -->
                        <button type="button" @click="reports[s.id] ? reports[s.id] = null : run(s, true)"
                            :disabled="running === s.id"
                            class="px-3 py-1.5 rounded-md text-xs border font-semibold disabled:opacity-50" :class="[$styles.secondaryButton]">
                            {{ running === s.id ? 'Scanning…' : (reports[s.id] ? 'Close' : 'Preview') }}
                        </button>
                        <button type="button" @click="remove(s)"
                            class="px-3 py-1.5 rounded-md text-xs border" :class="[$styles.secondaryButton]">Delete</button>
                    </div>
                </div>

                <RunReport v-if="reports[s.id]" :run="reports[s.id]"
                    @confirm="run(s, false)" @dismiss="reports[s.id] = null" />
            </div>

            <p v-if="!sources.length" class="text-sm py-6 text-center" :class="[$styles.muted]">
                No saved imports. One-off imports don't appear here.
            </p>
        </div>
    `,
    props: { storeId: [String, Number] },
    emits: ['imported'],
    setup(props, { emit }) {
        const sources = ref([])
        const reports = ref({})
        const running = ref(null)

        async function load() {
            const s = await ext.getJson(`/sources?filestoreId=${props.storeId}`)
            if (!s.error) sources.value = s.response || []
        }

        async function run(source, dryRun) {
            running.value = source.id
            try {
                const api = await ext.postJson(`/sources/${source.id}/run`, { dryRun })
                if (api.error) return ext.setError(api.error)
                reports.value = { ...reports.value, [source.id]: dryRun ? api.response : null }
                if (!dryRun) {
                    await load()
                    emit('imported', api.response)
                }
            } finally { running.value = null }
        }

        async function remove(source) {
            const api = await ext.deleteJson(`/sources/${source.id}`)
            if (api.error) return ext.setError(api.error)
            await load()
        }

        onMounted(load)
        return { sources, reports, running, run, remove }
    },
}

/**
 * Trusted import folders.
 *
 * Admins aren't held to this list — it exists for everyone else — so the panel says so rather
 * than letting an admin think they're editing their own limits. Each row shows where the entry
 * actually resolves to, because `~/docs` and `$WORKSPACE` are the two forms most likely to be
 * typed and the least likely to be obvious.
 */
export const TrustedFolders = {
    template: `
        <div v-if="loaded" class="mt-8 rounded-lg border" :class="[$styles.chromeBorder]">
            <div class="px-4 py-3 border-b" :class="[$styles.chromeBorder]">
                <div class="flex flex-wrap items-baseline justify-between gap-2">
                    <h3 class="font-semibold">Trusted import folders</h3>
                    <PathText class="text-[11px]" :path="path" :class="[$styles.muted]" />
                </div>
                <p class="text-xs mt-0.5" :class="[$styles.muted]">
                    Folders non-admins may import from. Admins can import from anywhere, so this list
                    does not limit you.
                    <span v-if="!configured"> Nothing is configured yet, so the server defaults apply.</span>
                </p>
            </div>

            <div class="px-4 py-3">
                <ul v-if="roots.length" class="space-y-1.5 mb-3">
                    <li v-for="(r, i) in roots" :key="i"
                        class="flex items-start gap-3 px-3 py-2 rounded-md border" :class="[$styles.chromeBorder]">
                        <div class="grow min-w-0">
                            <!-- No max: the row is as wide as the card, so paths use it all and
                                 only ellipsize via CSS when they genuinely don't fit. -->
                            <PathText class="text-sm block" :path="r.value" />
                            <PathText v-if="r.resolved && r.resolved !== r.value" class="text-[11px] block"
                                :path="'→ ' + r.resolved" :class="[$styles.muted]" />
                            <div class="text-[11px] mt-0.5 space-x-2">
                                <span v-if="!r.exists" class="text-amber-600 dark:text-amber-400">
                                    No such folder — nothing will match it
                                </span>
                                <span v-if="r.broad" class="text-red-600 dark:text-red-400">
                                    This grants everything below it
                                </span>
                            </div>
                        </div>
                        <button v-if="isAdmin" type="button" @click="removeAt(i)"
                            class="text-xs shrink-0 text-red-600 dark:text-red-400 hover:underline">Remove</button>
                    </li>
                </ul>

                <div v-if="isAdmin" class="flex flex-wrap items-center gap-2">
                    <!-- One path per entry. The old placeholder listed three separated by commas,
                         which read as "commas are supported" - they aren't, and a pasted list
                         would have been stored as one unmatchable path. -->
                    <input type="text" v-model="draft" @keyup.enter="add" placeholder="One folder, e.g. /srv/docs"
                        class="grow min-w-0 px-2.5 py-1.5 rounded-md text-sm font-mono bg-white dark:bg-gray-900"
                        :class="[$styles.textInput, $styles.bgInput, $styles.borderInput]"
                        title="A single absolute path. ~ expands, and $WORKSPACE / $TEMP resolve to the server's own folders.">
                    <button type="button" @click="add" :disabled="!draft.trim() || saving"
                        class="px-3 py-1.5 rounded-md text-sm font-medium border disabled:opacity-50"
                        :class="[$styles.secondaryButton]">Add</button>
                    <span v-if="saving" class="text-xs" :class="[$styles.muted]">Saving…</span>
                </div>

                <!-- After the Add row, not before it: an empty list is a prompt to add one, so it
                     belongs next to the thing that adds one. -->
                <p v-if="!roots.length" class="text-sm pt-3" :class="[$styles.muted]">
                    No folders configured.
                </p>
            </div>
        </div>
    `,
    components: { PathText },
    setup() {
        const loaded = ref(false)
        const isAdmin = ref(false)
        const configured = ref(false)
        const path = ref('')
        const roots = ref([])
        const draft = ref('')
        const saving = ref(false)

        function apply(r) {
            isAdmin.value = !!r.isAdmin
            configured.value = !!r.configured
            path.value = r.path || ''
            roots.value = r.roots || []
        }

        async function load() {
            const api = await ext.getJson('/config/import-roots')
            if (api.error) return
            apply(api.response || {})
            loaded.value = true
        }

        // Every add/remove persists immediately: there is no Save step to forget, so the list
        // on screen is always what the server enforces. The response re-applies the server's
        // resolved view (exists / broad / ~ expansion) over the optimistic local row.
        async function persist() {
            saving.value = true
            try {
                const api = await ext.postJson('/config/import-roots', { roots: roots.value.map(r => r.value) })
                if (api.error) return ext.setError(api.error)
                apply(api.response || {})
            } finally { saving.value = false }
        }

        async function add() {
            // Deliberately not split on commas: a path may legally contain one, so accepting a
            // list here would turn `/srv/a,b` into two folders that don't exist.
            let v = draft.value.trim()
            // A trailing slash resolves to the same folder, but storing it makes the row show
            // value + resolved as if they were two paths. "/" itself is the only path that
            // keeps its slash.
            v = v.replace(/\/+$/, '') || '/'
            if (!v || roots.value.some(r => r.value === v)) return
            draft.value = ''
            roots.value = [...roots.value, { value: v, resolved: '', exists: true, broad: false }]
            await persist()
        }

        async function removeAt(i) {
            roots.value = roots.value.filter((_, n) => n !== i)
            await persist()
        }

        onMounted(load)
        return { loaded, isAdmin, configured, path, roots, draft, saving, add, removeAt }
    },
}
