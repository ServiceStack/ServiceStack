import { ref, computed, watch, onMounted } from 'vue'
import { MetadataInput, MetadataDialog, IMPORT_FIELDS, summariseMetadata, loadFacets } from './metadata.mjs'
import { Popover, RootsPanel, PathText, CheckBox } from './explorer.mjs'

let ext = null
export function initImport(extScope) {
    ext = extScope
}

// One entry per import option. `fields` drives the form, so a tab only ever shows what that
// option actually needs - the folder tab has no upload zone, the upload tab has no path.
export const IMPORT_TABS = [
    {
        id: 'upload', label: 'Upload files',
        blurb: 'Drop files in, including a .zip archive - which are expanded and individually imported with its folder structure becoming the category.',
        recurring: false, fields: [],
    },
    {
        id: 'folder', label: 'Folder', sourceType: 'folder',
        blurb: 'Index a folder on this machine and keep it in sync.',
        recurring: true,
        // A `pair` shares one grid cell, so the two settings that both shape the category sit
        // together and the glob fields get a row of their own.
        fields: [
            { key: 'path', label: 'Folder path', placeholder: '/Users/me/src/docs', mono: true, required: true,
              hint: 'Must be inside an allowed folder', roots: true },
            { pair: [
                // Condensed, with the full explanation on hover: the long form was the field's
                // whole second line and still didn't say what it meant to anyone new.
                { key: 'root', label: 'Category root', placeholder: 'docs', mono: true,
                  hint: 'Trimmed from each category',
                  title: 'Only files under this subfolder are imported, and this prefix is removed '
                       + 'from every category. With a root of "docs", the file docs/guides/auth.md '
                       + 'lands in the category guides.' },
                { key: 'maxDepth', label: 'Max depth', type: 'number', min: 0, step: 1,
                  placeholder: 'unlimited', width: 'w-28 shrink-0', hint: '0 = this folder only' },
            ] },
            { key: 'include', label: 'Include only', placeholder: '**/*.md', mono: true },
            { key: 'exclude', label: 'Exclude', placeholder: '**/drafts/**', mono: true },
        ],
    },
]

export const ImportPanel = {
    components: { MetadataDialog, MetadataInput, Popover, RootsPanel, PathText, CheckBox },
    template: `
        <div class="rounded-lg border overflow-hidden" :class="[$styles.chromeBorder]">
            <div class="flex border-b" :class="[$styles.chromeBorder]">
                <button v-for="t in tabs" :key="t.id" type="button" @click="select(t.id)"
                    class="px-4 py-2.5 text-sm font-medium whitespace-nowrap border-b-2 -mb-px inline-flex items-center gap-1.5"
                    :class="tab === t.id
                        ? 'border-blue-500 text-blue-600 dark:text-blue-400'
                        : 'border-transparent hover:bg-gray-100 dark:hover:bg-gray-800'">
                    <!-- SVGs rather than emoji: crisper, themeable, and consistent with the
                         chevrons and folder icons elsewhere in the extension. -->
                    <svg v-if="t.id === 'upload'" class="size-4 opacity-70" viewBox="0 0 24 24" fill="none"
                        stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                        <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                        <polyline points="14 2 14 8 20 8"/>
                        <line x1="12" y1="18" x2="12" y2="12"/>
                        <polyline points="9 15 12 12 15 15"/>
                    </svg>
                    <svg v-else-if="t.id === 'folder'" class="size-4 opacity-70" viewBox="0 0 24 24" fill="none"
                        stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                        <path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/>
                    </svg>
                    {{ t.label }}
                    <span v-if="t.unavailable" class="ml-1 text-xs" :class="[$styles.muted]">· unavailable</span>
                </button>
            </div>

            <div class="p-5 space-y-4">
                <p class="text-sm" :class="[$styles.muted]">{{ active.blurb }}</p>

                <div v-if="active.unavailable" class="px-3 py-2 rounded border text-sm border-amber-500 text-amber-700 dark:text-amber-400 bg-amber-50 dark:bg-amber-900/20">
                    This import option isn't available on this machine{{ active.reason ? ': ' + active.reason : '' }}.
                </div>

                <template v-else>
                    <!-- Upload: the drop zone lives in its own tab now -->
                    <div v-if="tab === 'upload'">
                        <input type="file" ref="fileInput" class="hidden" multiple
                            accept=".zip,.pdf,.md,.mdx,.markdown,.txt,.html,.htm,.rst,.adoc,.csv,.json,.yaml,.yml"
                            @change="onFiles">
                        <div @click="fileInput?.click()" @dragover.prevent="dragover = true"
                            @dragleave.prevent="dragover = false" @drop.prevent="onDrop"
                            class="border-2 border-dashed rounded-lg p-8 text-center cursor-pointer transition"
                            :class="dragover ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20' : [$styles.chromeBorder]">
                            <svg class="size-10 mx-auto mb-2 text-gray-300 dark:text-gray-600" viewBox="0 0 24 24" fill="none"
                                stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                                <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                                <polyline points="14 2 14 8 20 8"/>
                                <line x1="12" y1="18" x2="12" y2="12"/>
                                <polyline points="9 15 12 12 15 15"/>
                            </svg>
                            <p class="text-sm font-medium" :class="[$styles.heading]">
                                <span :class="[$styles.linkHover]">Choose files</span> or drag and drop
                            </p>
                            <p class="text-xs mt-1" :class="[$styles.muted]">PDFs, text, Markdown, HTML — or a .zip of them</p>
                            <p v-if="files.length" class="text-xs mt-2 text-blue-600 dark:text-blue-400">
                                {{ files.length }} file{{ files.length === 1 ? '' : 's' }} ready<span v-if="hasArchive"> — archives are expanded on upload</span>
                            </p>
                        </div>
                    </div>

                    <!-- Everything else is driven by the tab's own field list -->
                    <div v-else class="grid sm:grid-cols-2 gap-4">
                        <div v-for="cell in formCells" :key="cell.id" class="flex items-start gap-3"
                            :class="cell.wide ? 'sm:col-span-2' : ''">
                            <div v-for="f in cell.fields" :key="f.key" :class="f.width || 'grow min-w-0'">
                                <label class="block text-xs font-semibold mb-1">
                                    {{ f.label }}<span v-if="f.required" class="text-red-500">*</span>
                                </label>
                                <div class="flex items-center gap-1.5">
                                    <input v-model="config[f.key]" :type="f.type || 'text'" :placeholder="f.placeholder"
                                        :min="f.min" :step="f.step"
                                        class="grow min-w-0 px-2.5 py-1.5 rounded-md text-sm border-2 bg-white dark:bg-gray-900"
                                        :class="[$styles.chromeBorder, f.mono ? 'font-mono' : '']">
                                    <!-- A path you can only satisfy by guessing is a broken field.
                                         The allowed folders are the answer, but a dumped list ate
                                         the form - so they live one click away instead. -->
                                    <Popover v-if="f.roots" icon wide title="Where can I import from?" @open="loadRoots">
                                        <template #label>
                                            <svg class="size-[18px] opacity-70" viewBox="0 0 24 24" fill="none"
                                                stroke="currentColor" stroke-width="2" stroke-linecap="round"
                                                stroke-linejoin="round" aria-hidden="true">
                                                <path d="m6 14 1.5-2.9A2 2 0 0 1 9.24 10H20a2 2 0 0 1 1.94 2.5l-1.54 6a2 2 0 0 1-1.95 1.5H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h7a2 2 0 0 1 2 2v1"/>
                                            </svg>
                                        </template>
                                        <template #default="{ close }">
                                            <RootsPanel :roots="folderRoots" :unrestricted="rootsUnrestricted"
                                                @pick="p => { config[f.key] = p; close() }" />
                                        </template>
                                    </Popover>
                                </div>
                                <p v-if="f.roots" class="mt-0.5 text-xs" :class="[$styles.muted]">
                                    {{ rootsUnrestricted ? 'Any folder on this machine' : f.hint }}
                                </p>
                                <p v-else-if="f.hint" class="mt-0.5 text-xs" :class="[$styles.muted]"
                                    :title="f.title">{{ f.hint }}</p>
                            </div>
                        </div>
                    </div>

                    <!-- Import is where categories come from. Browsing one in Explore is a
                         read-only view of what was ingested; typing a new name here is how one
                         starts existing, by having documents put in it. -->
                    <div class="sm:w-2/3">
                        <label class="block text-xs font-semibold mb-1">
                            Destination category
                            <span class="font-normal" :class="[$styles.muted]">— optional</span>
                        </label>
                        <MetadataInput :model-value="landingCategory || ''"
                            @update:modelValue="setLanding" :values="categoryValues"
                            placeholder="e.g. guides/auth — or leave empty" />
                        <p class="mt-0.5 text-xs" :class="[$styles.muted]">
                            <span v-if="tab === 'upload'">Where these files will live. Leave empty and a .zip keeps its own folder structure.</span>
                            <span v-else>The folder's own structure nests underneath this.</span>
                        </p>
                    </div>

                    <!-- Metadata: one button, then a read-optimised summary -->
                    <div class="rounded-lg border p-3" :class="[$styles.chromeBorder]">
                        <div class="flex items-center justify-between gap-3 flex-wrap">
                            <div class="min-w-0">
                                <div class="text-xs font-semibold">Metadata</div>
                                <div v-if="!summary.length && !(metadata.rules || []).length" class="text-xs" :class="[$styles.muted]">
                                    None set — documents will import unlabelled
                                </div>
                                <div v-else class="flex flex-wrap gap-1.5 mt-1">
                                    <span v-for="s in summary" :key="s.key"
                                        class="px-2 py-0.5 rounded-full text-xs border border-emerald-500 text-emerald-600 dark:text-emerald-400 bg-emerald-50 dark:bg-emerald-900/20">
                                        {{ s.label }}: <b>{{ s.value }}</b>
                                    </span>
                                    <span v-if="(metadata.rules || []).length"
                                        class="px-2 py-0.5 rounded-full text-xs border" :class="[$styles.chromeBorder]">
                                        +{{ metadata.rules.length }} rule{{ metadata.rules.length === 1 ? '' : 's' }} by path
                                    </span>
                                </div>
                            </div>
                            <button type="button" @click="dialogOpen = true"
                                class="px-3 py-1.5 rounded-md text-sm border font-medium shrink-0" :class="[$styles.chromeBorder]">
                                {{ summary.length || (metadata.rules || []).length ? 'Edit metadata' : 'Add metadata' }}
                            </button>
                        </div>
                    </div>

                    <!-- Only offered where re-running actually means something -->
                    <label v-if="active.recurring" class="flex items-start gap-2.5 text-sm cursor-pointer">
                        <CheckBox v-model="saveSource" class="mt-0.5" />
                        <span>
                            <span class="font-medium">Save as a recurring import</span>
                            <span class="block text-xs" :class="[$styles.muted]">
                                Keep it so you can re-sync later and pick up changes. Unchecked, this is a one-off.
                            </span>
                        </span>
                    </label>
                    <div v-if="active.recurring && saveSource" class="sm:w-1/2">
                        <label class="block text-xs font-semibold mb-1">Name</label>
                        <input v-model="name" placeholder="Product docs"
                            class="w-full px-2.5 py-1.5 rounded-md text-sm border-2 bg-white dark:bg-gray-900" :class="[$styles.chromeBorder]">
                    </div>

                    <div class="flex items-center gap-3 pt-1">
                        <button type="button" @click="submit" :disabled="!canSubmit || busy"
                            class="px-4 py-2 rounded-md text-sm font-semibold text-white bg-blue-600 hover:bg-blue-700 disabled:opacity-40 disabled:cursor-not-allowed">
                            {{ busy ? busyLabel : submitLabel }}
                        </button>
                        <span v-if="tab !== 'upload'" class="text-xs" :class="[$styles.muted]">
                            Preview costs nothing — you confirm before anything is indexed.
                        </span>
                    </div>
                </template>
            </div>

            <MetadataDialog v-if="dialogOpen" v-model="metadata" :facets="facets" :fields="importFields"
                :show-rules="tab !== 'upload'" @close="dialogOpen = false" />
        </div>
    `,
    props: { storeId: [String, Number], facets: Object, presetCategory: String },
    emits: ['previewing', 'preview', 'imported'],
    setup(props, { emit }) {
        const tabs = ref(IMPORT_TABS.map(t => ({ ...t })))
        // Which import option you were on survives a refresh, the same way the Explore/Import
        // choice does - coming back to a half-filled Folder form and finding Upload is jarring.
        const saved = IMPORT_TABS.some(t => t.id === ext.prefs.importTab) ? ext.prefs.importTab : 'upload'
        const tab = ref(saved)
        const config = ref({})
        const metadata = ref({ defaults: {}, rules: [] })
        const saveSource = ref(false)
        const name = ref('')
        const files = ref([])
        const dragover = ref(false)
        const dialogOpen = ref(false)
        const busy = ref(false)
        const fileInput = ref(null)

        const active = computed(() => tabs.value.find(t => t.id === tab.value) || tabs.value[0])

        // Arriving from "Import into guides/auth": for an upload that's the category itself, for a
        // folder import it's a prefix the discovered structure nests under - build_plan derives
        // category from the path, so a plain default would just be overwritten.
        watch(() => props.presetCategory, cat => {
            const defaults = { ...(metadata.value.defaults || {}) }
            if (cat) defaults.category = cat
            else delete defaults.category
            metadata.value = {
                ...metadata.value,
                defaults,
            }
        }, { immediate: true })
        const hasArchive = computed(() => files.value.some(f => f.name.toLowerCase().endsWith('.zip')))
        // One entry per grid cell. A plain field is a cell of one; a `pair` puts two side by side.
        const formCells = computed(() => (active.value.fields || []).map(f => f.pair
            ? { id: f.pair.map(p => p.key).join('+'), fields: f.pair }
            : { id: f.key, fields: [f], wide: f.wide }))
        const summary = computed(() => summariseMetadata(metadata.value)
            .filter(s => !(s.key === 'category' && s.value === landingCategory.value)))
        const landingCategory = computed(() => metadata.value?.defaults?.category || null)
        function setLanding(value) {
            const defaults = { ...(metadata.value.defaults || {}) }
            if (value) defaults.category = value
            else delete defaults.category
            metadata.value = { ...metadata.value, defaults }
        }
        // Existing categories with their counts, so importing into one that already exists is
        // obvious - and a genuinely new name is flagged as new rather than typed blind.
        const categoryValues = computed(() => props.facets?.category?.values || [])
        const folderRoots = ref({})
        const rootsUnrestricted = ref(false)

        // Re-fetched every time the folder picker opens: trusted folders are editable in the
        // Trusted import folders section (and the config file), and the list here must show
        // those changes without a page refresh.
        async function loadRoots() {
            const api = await ext.getJson('/source-types')
            if (api.error) return {}
            const byType = Object.fromEntries((api.response || []).map(t => [t.type, t]))
            folderRoots.value = byType.folder?.roots || {}
            rootsUnrestricted.value = !!byType.folder?.unrestricted
            return byType
        }

        onMounted(async () => {
            // A source type that can't run here is shown disabled with the reason, rather than
            // being offered and failing on first use.
            const byType = await loadRoots()
            tabs.value = tabs.value.map(t => t.sourceType && byType[t.sourceType] && !byType[t.sourceType].available
                ? { ...t, unavailable: true, reason: byType[t.sourceType].reason }
                : t)
        })

        function select(id) {
            tab.value = id
            config.value = {}
            ext.setPrefs({ importTab: id })
        }

        const canSubmit = computed(() => {
            if (active.value.unavailable) return false
            if (tab.value === 'upload') return files.value.length > 0
            return (active.value.fields || []).every(f => !f.required || String(config.value[f.key] || '').trim())
        })
        const submitLabel = computed(() => tab.value === 'upload'
            ? `Upload ${files.value.length || ''} file${files.value.length === 1 ? '' : 's'}`.replace('  ', ' ')
            : 'Preview import')
        const busyLabel = computed(() => tab.value === 'upload' ? 'Uploading…' : 'Scanning…')

        function onFiles(e) { files.value = [...e.target.files] }
        function onDrop(e) { dragover.value = false; files.value = [...e.dataTransfer.files] }

        /** Rule rows from the dialog -> the shape build_plan() expects. */
        function rulesPayload() {
            const defaults = { ...(metadata.value.defaults || {}) }
            // On a source import the category comes from the path (prefixed above), so leaving it
            // in defaults would be a value that silently never applies.
            if (tab.value !== 'upload') delete defaults.category
            return {
                defaults,
                rules: (metadata.value.rules || []).map(r => r.field
                    ? { match: r.match, set: { [r.field]: ['versions', 'tags'].includes(r.field)
                        ? String(r.value || '').split(',').map(s => s.trim()).filter(Boolean) : r.value } }
                    : { match: r.match, skip: true }),
            }
        }

        async function submit() {
            busy.value = true
            try {
                if (tab.value === 'upload') return await uploadFiles()
                await previewSource()
            } finally { busy.value = false }
        }

        async function uploadFiles() {
            const form = new FormData()
            for (const [k, v] of Object.entries(metadata.value.defaults || {})) {
                if (v === '' || v == null) continue
                form.append(k, Array.isArray(v) ? v.join(',') : v)
            }
            for (const f of files.value) form.append('file', f)
            // Use the extension scope so uploads respect the host's configured route prefix
            // (e.g. /chat/ext/gemini instead of assuming /ext/gemini is mounted at the root).
            const res = await ext.postForm(`/filestores/${props.storeId}/upload`, { body: form })
            const api = await ext.createJsonResult(res)
            if (api.error) return ext.setError(api.error)
            const payload = api.response
            files.value = []
            if (fileInput.value) fileInput.value.value = ''
            emit('imported', { queued: Array.isArray(payload) ? payload.length : 0, category: landingCategory.value || null })
        }

        async function previewSource() {
            const cfg = config.value
            emit('previewing')
            const defaultName = cfg.path?.split('/').filter(Boolean).pop() || active.value.label
            // Provisional one-off sources are deleted after the confirmed run. Give them an
            // internal unique name so they can preview the same folder as an existing saved
            // import without weakening saved-import name uniqueness.
            const sourceName = saveSource.value
                ? name.value || defaultName
                : `${defaultName} (one-off ${Date.now().toString(36)})`
            const body = {
                filestoreId: Number(props.storeId),
                name: sourceName,
                type: active.value.sourceType,
                config: {
                    path: cfg.path,
                    include: cfg.include ? [cfg.include] : null,
                    exclude: cfg.exclude ? [cfg.exclude] : null,
                },
                category: {
                    root: cfg.root || null,
                    maxDepth: cfg.maxDepth !== '' && cfg.maxDepth != null ? Number(cfg.maxDepth) : null,
                    prefix: landingCategory.value || null,
                },
                rules: rulesPayload(),
                // A one-off is still a source row - it's just deleted once it has run, which keeps
                // one code path instead of two.
                // Whether this is kept is decided when the import is confirmed, not here - the
                // source row exists either way, because running the pipeline needs one.
            }
            const created = await ext.postJson('/sources', body)
            if (created.error) return ext.setError(created.error)
            const run = await ext.postJson(`/sources/${created.response.id}/run`, { dryRun: true })
            if (run.error) return ext.setError(run.error)
            emit('preview', { source: created.response, run: run.response, keep: saveSource.value })
        }

        function resetAfterImport() {
            saveSource.value = false
            name.value = ''
        }

        return {
            tabs, tab, active, config, metadata, saveSource, name, files, dragover, dialogOpen,
            busy, fileInput, hasArchive, formCells, summary, canSubmit,
            importFields: IMPORT_FIELDS,
            landingCategory, setLanding, categoryValues, folderRoots, rootsUnrestricted, loadRoots,
            submitLabel, busyLabel, select, onFiles, onDrop, submit, resetAfterImport,
        }
    },
}
