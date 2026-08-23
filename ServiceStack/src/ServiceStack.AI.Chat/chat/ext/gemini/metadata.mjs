import { ref, computed, watch, onMounted, onBeforeUnmount } from 'vue'

// Set by index.mjs on install, same pattern the rest of the extension uses.
let ext = null
let ctx = null
export function initMetadata(extScope, context) {
    ext = extScope
    ctx = context
}

export const FACET_FIELDS = ['category', 'docType', 'status', 'locale', 'product', 'versions', 'tags']
export const LIST_FIELDS = ['versions', 'tags', 'categoryPath']
export const FIELD_VALUES = {
    docType: ['guide', 'reference', 'api', 'faq', 'release-notes', 'policy', 'changelog'],
    status: ['published', 'draft', 'deprecated', 'archived'],
}

/** The editable fields, in the order every editor shows them. */
export const META_FIELDS = [
    { key: 'docType', label: 'Doc type', hint: 'Routes a question to the right kind of content' },
    { key: 'status', label: 'Status', hint: 'Deprecated content is excluded from answers by default' },
    { key: 'product', label: 'Product' },
    { key: 'locale', label: 'Locale' },
    // `free`: a URL is not a vocabulary. Autocompleting it against the store's other URLs, or
    // asking "did you mean" about one, is noise - and unfaceted fields have nothing to check
    // against anyway, so every value would be announced as new.
    // `wide`: a URL is the one value here that is longer than half a dialog.
    { key: 'sourceUrl', label: 'Source URL', hint: 'Where a citation should link to', free: true, wide: true },
]
export const META_LIST_FIELDS = [
    { key: 'versions', label: 'Versions', placeholder: 'v2, v3' },
    { key: 'tags', label: 'Tags', placeholder: 'security, report' },
]

export const SOURCE_URL_VARIABLES = ['category', 'fullPath', 'path', 'pathNoExt', 'dir', 'name', 'filename', 'ext', 'title']
const SOURCE_URL_VARIABLE_KEYS = new Set(SOURCE_URL_VARIABLES.map(x => x.toLowerCase()))

export function sourceUrlTemplateError(value) {
    const text = String(value || '')
    if (!text) return ''
    const pairs = [...text.matchAll(/\{([^{}]*)\}/g)]
    const unknown = [...new Set(pairs
        .map(x => x[1])
        .filter(x => !SOURCE_URL_VARIABLE_KEYS.has(x.toLowerCase())))]
    if (unknown.length) return `Unknown variable${unknown.length === 1 ? '' : 's'}: ${unknown.map(x => `{${x}}`).join(', ')}`
    if (/[{}]/.test(text.replace(/\{[^{}]*\}/g, ''))) return 'Every “{” must have a matching “}”'
    return ''
}

/**
 * Import derives one URL per document from its path, so there the field is a template.
 *
 * A fixed string would put the identical citation link on all 1,500 documents of a run, which is
 * worse than leaving it empty - the link resolves, it just goes to the wrong page. Expansion
 * happens server-side in `ingest.expand_template`; the dry-run report shows what it produced.
 */
export const IMPORT_FIELDS = META_FIELDS.map(f => f.key === 'sourceUrl' ? {
    ...f,
    variables: SOURCE_URL_VARIABLES,
    placeholder: 'https://docs.acme.com/{category}/{name}',
    hint: 'Build one URL per document with the variables below.',
} : f)

/**
 * Editing documents that already exist adds `category`: moving a document between folders is a
 * metadata edit like any other. Import doesn't get it - there the category comes from the landing
 * folder and the discovered paths, and a default would just be overwritten.
 */
export const DOC_FIELDS = [
    { key: 'category', label: 'Folder', hint: 'Where the document lives - changing it moves it' },
    ...META_FIELDS,
]

/** Human summary of a metadata selection - what's shown once an editor is closed. */
export function summariseMetadata(meta) {
    const out = []
    for (const { key, label } of [...META_FIELDS, ...META_LIST_FIELDS]) {
        const v = meta?.defaults?.[key]
        if (v === undefined || v === null || v === '' || (Array.isArray(v) && !v.length)) continue
        out.push({ key, label, value: Array.isArray(v) ? v.join(', ') : String(v) })
    }
    return out
}

/**
 * How a field reads on a document row: an icon where one says the thing faster than a word, a
 * colour otherwise, and the field name in the tooltip either way.
 *
 * `docType: guide · status: published · product: llms` spends most of its width repeating labels
 * that never change down the column. Dropping them costs the reader nothing while the values stay
 * distinguishable — which is what the colour is for on the fields with no icon.
 */
export const META_CHIPS = {
    docType: {
        class: 'text-emerald-600 dark:text-emerald-400 border-emerald-500/50',
        icon: `<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 56 56"><path d="M0 0h56v56H0z" fill="none"/><path fill="currentColor" d="M15.555 53.125h24.89c4.852 0 7.266-2.461 7.266-7.336V24.508c0-3.024-.328-4.336-2.203-6.258L32.57 5.102c-1.78-1.829-3.234-2.227-5.882-2.227H15.555c-4.828 0-7.266 2.484-7.266 7.36v35.554c0 4.898 2.438 7.336 7.266 7.336m.187-3.773c-2.414 0-3.68-1.29-3.68-3.633V10.305c0-2.32 1.266-3.657 3.704-3.657h10.406v13.618c0 2.953 1.5 4.406 4.406 4.406h13.36v21.047c0 2.343-1.243 3.633-3.68 3.633ZM31 21.132c-.914 0-1.29-.374-1.29-1.312V7.375l13.5 13.758Z"/></svg>`,
    },
    status: {
        class: 'text-blue-600 dark:text-blue-400 border-blue-500/50',
        icon: `<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 16 16"><path d="M0 0h16v16H0z" fill="none"/><path fill="currentColor" fill-rule="evenodd" d="M15.941 7.033a8 8 0 0 1-14.784 5.112a.75.75 0 1 1 1.283-.778a6.5 6.5 0 1 0 8.922-8.93a.75.75 0 0 1 .776-1.284a8 8 0 0 1 3.803 5.88M9 1a1 1 0 1 1-2 0a1 1 0 0 1 2 0M2.804 5a1 1 0 1 0-1.732-1a1 1 0 0 0 1.732 1M1 7a1 1 0 1 1 0 2a1 1 0 0 1 0-2m4-4.196a1 1 0 1 0-1-1.732a1 1 0 0 0 1 1.732" clip-rule="evenodd"/></svg>`,
    },
    product: {
        class: 'text-violet-600 dark:text-violet-400 border-violet-500/50',
        icon: `<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 24 24"><path d="M0 0h24v24H0z" fill="none"/><path fill="currentColor" d="M22 3H2v6h1v11a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V9h1zM4 5h16v2H4zm15 15H5V9h14zM9 11h6a2 2 0 0 1-2 2h-2a2 2 0 0 1-2-2"/></svg>`,
    },
    locale: { class: 'text-amber-600 dark:text-amber-400 border-amber-500/50' },
    versions: { class: 'text-sky-600 dark:text-sky-400 border-sky-500/50' },
    tags: { class: 'text-rose-600 dark:text-rose-400 border-rose-500/50' },
}
const META_CHIP_FALLBACK = { class: 'text-gray-600 dark:text-gray-400 border-gray-500/50' }

export const MetaChip = {
    template: `
        <span class="inline-flex items-center gap-1 px-1.5 py-0.5 rounded border text-xs"
            :class="chip.class" :title="field + ': ' + text">
            <span v-if="chip.icon" v-html="chip.icon" class="shrink-0 opacity-80" aria-hidden="true"></span>
            <span class="truncate max-w-[16rem]">{{ text }}</span>
        </span>
    `,
    props: { field: String, value: [String, Number, Array] },
    setup(props) {
        return {
            chip: computed(() => META_CHIPS[props.field] || META_CHIP_FALLBACK),
            text: computed(() => Array.isArray(props.value) ? props.value.join(', ') : String(props.value ?? '')),
        }
    },
}

/**
 * Autocomplete options for a field: real values from the store first, so counts are visible, then
 * the known vocabulary so a fresh store still offers sensible options.
 */
export function fieldValues(facets, key) {
    const merged = [...(facets?.[key]?.values || [])]
    for (const v of FIELD_VALUES[key] || []) {
        if (!merged.some(m => m.value === v)) merged.push({ value: v, count: 0 })
    }
    return merged
}

export async function loadFacets(storeId, fields) {
    const qs = fields?.length ? `?fields=${fields.join(',')}` : ''
    const api = await ext.getJson(`/filestores/${storeId}/facets${qs}`)
    if (api.error) {
        ext.setError(api.error)
        return null
    }
    return api.response
}

const norm = s => String(s ?? '').trim().toLowerCase().replace(/[\s_-]+/g, '')

function levenshtein(a, b) {
    const m = [...Array(a.length + 1)].map((_, i) => [i, ...Array(b.length).fill(0)])
    for (let j = 0; j <= b.length; j++) m[0][j] = j
    for (let i = 1; i <= a.length; i++)
        for (let j = 1; j <= b.length; j++)
            m[i][j] = Math.min(m[i - 1][j] + 1, m[i][j - 1] + 1, m[i - 1][j - 1] + (a[i - 1] === b[j - 1] ? 0 : 1))
    return m[a.length][b.length]
}

/** Closest existing value within an edit or two — the check that actually stops vocabulary drift. */
export function nearestValue(values, input) {
    const v = norm(input)
    if (!v) return null
    let best = null
    for (const { value, count } of values || []) {
        const c = norm(value)
        if (c === v) continue
        const d = levenshtein(c, v)
        if (d <= (v.length <= 4 ? 1 : 2) && (!best || count > best.count)) best = { value, count, dist: d }
    }
    return best
}

/**
 * Value input with four states.
 *
 * `exact` and `new` are the obvious two. `canonical` exists because matching case-insensitively
 * and then storing what was typed is how this control would *cause* the drift it's meant to
 * prevent — a case-insensitive hit resolves to the stored spelling instead. `near` is where most
 * of the value is: free-form metadata dies of guide/guides and v8/V8.
 */
export const MetadataInput = {
    template: `
        <div data-tag="MetadataInput" class="relative">
            <input type="text" autocomplete="off" :placeholder="placeholder" :disabled="disabled"
                v-model="text" @input="onInput" @focus="open = !disabled" @blur="onBlur" @keydown="onKey"
                class="w-full px-2.5 py-1.5 rounded-md text-sm"
                :class="[borderClass, $styles.textInput, $styles.bgInput]">
            <div v-if="open && suggestions.length" class="absolute z-30 left-0 right-0 mt-1 max-h-52 overflow-auto rounded-md border shadow-lg bg-white dark:bg-gray-800"
                :class="[$styles.chromeBorder]">
                <div v-for="s in suggestions" :key="s.value" @mousedown.prevent="choose(s.value)"
                    class="flex justify-between items-center gap-3 px-2.5 py-1.5 text-sm cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-700">
                    <span class="truncate">{{ s.value }}</span>
                    <span class="text-xs tabular-nums shrink-0" :class="[$styles.muted]">{{ s.count.toLocaleString() }}</span>
                </div>
                <div v-if="!exact && text.trim()" @mousedown.prevent="forceNew()"
                    class="flex justify-between gap-3 px-2.5 py-1.5 text-sm cursor-pointer border-t text-amber-600 dark:text-amber-400 hover:bg-gray-100 dark:hover:bg-gray-700"
                    :class="[$styles.chromeBorder]">
                    <span class="truncate">Add “{{ text.trim() }}” as a new value</span>
                    <span class="text-xs">⌘⏎</span>
                </div>
            </div>
            <slot />
            <div v-if="message && !disabled" class="mt-1 text-xs flex items-center gap-1.5 flex-wrap" :class="messageClass">
                <span>{{ message }}</span>
                <button v-if="near && !forced" type="button" @mousedown.prevent="choose(near.value)"
                    class="px-1.5 py-0.5 rounded border text-xs font-semibold border-current">Use “{{ near.value }}”</button>
                <button v-if="near && !forced" type="button" @mousedown.prevent="forceNew()"
                    class="px-1.5 py-0.5 rounded border text-xs font-semibold border-current">Keep “{{ text.trim() }}”</button>
            </div>
            <div v-else-if="hint && !disabled" class="mt-1 text-xs" :class="[$styles.muted]">{{ hint }}</div>
        </div>
    `,
    props: {
        modelValue: String,
        values: { type: Array, default: () => [] },   // [{value, count}]
        placeholder: { type: String, default: 'Start typing…' },
        disabled: Boolean,
        // Shown under the box only while it has nothing of its own to say - the state message is
        // always the more useful of the two, and stacking both makes a field twice as tall.
        hint: String,
        // Tag mode: a committed value is handed to the parent as a `commit` and the box empties,
        // instead of the box *being* the value.
        clearOnCommit: Boolean,
    },
    emits: ['update:modelValue', 'commit'],
    setup(props, { emit }) {
        const text = ref(props.modelValue || '')
        const open = ref(false)
        const forced = ref(false)

        watch(() => props.modelValue, v => { if ((v || '') !== text.value) text.value = v || '' })

        // Case-insensitive hit on an existing value, whatever the source of the text - typing,
        // pasting, or a comma-separated run. Everything that stores a value goes through it, which
        // is what stops this control from causing the drift it exists to prevent.
        const matchValue = v =>
            (props.values || []).find(x => String(x.value).toLowerCase() === String(v ?? '').trim().toLowerCase())
        const exact = computed(() => matchValue(text.value))
        const near = computed(() => exact.value ? null : nearestValue(props.values, text.value))

        const suggestions = computed(() => {
            const q = text.value.trim().toLowerCase()
            return (props.values || [])
                .filter(v => !q || String(v.value).toLowerCase().includes(q))
                .slice(0, 40)
        })

        const state = computed(() => {
            const v = text.value.trim()
            if (!v) return null
            if (exact.value) return exact.value.value === v ? 'exact' : 'canonical'
            if (near.value && !forced.value) return 'near'
            return 'new'
        })

        const borderClass = computed(() => ({
            exact: 'border-emerald-500 bg-emerald-50 dark:bg-emerald-900/20',
            canonical: 'border-emerald-500 bg-emerald-50 dark:bg-emerald-900/20',
            near: 'border-red-500 bg-red-50 dark:bg-red-900/20',
            new: 'border-amber-500 bg-amber-50 dark:bg-amber-900/20',
        }[state.value] || 'border-gray-300 dark:border-gray-600'))

        const messageClass = computed(() => ({
            exact: 'text-emerald-600 dark:text-emerald-400',
            canonical: 'text-emerald-600 dark:text-emerald-400',
            near: 'text-red-600 dark:text-red-400',
            new: 'text-amber-600 dark:text-amber-400',
        }[state.value] || ''))

        const message = computed(() => {
            const v = text.value.trim()
            if (state.value === 'exact') return `✓ Existing value — ${exact.value.count.toLocaleString()} documents use it`
            if (state.value === 'canonical')
                return `✓ Matches “${exact.value.value}” (${exact.value.count.toLocaleString()} docs) — will be saved with that spelling`
            if (state.value === 'near') return `⚠ Did you mean “${near.value.value}” (${near.value.count.toLocaleString()} docs)?`
            if (state.value === 'new') return `＋ New value — first document in this store with it`
            return ''
        })

        function commit(v) {
            if (props.clearOnCommit) return commitMany([v])
            text.value = v
            emit('update:modelValue', v)
        }
        /**
         * Tag mode hands over every value from one edit at once, and leaves `rest` in the box.
         *
         * One event rather than one per value because the receiver appends to a prop: emitting
         * `api` and `v8` separately has them both append to the same pre-edit list, and only the
         * last one survives.
         */
        function commitMany(vals, rest = '') {
            const out = vals.map(v => matchValue(v)?.value ?? String(v ?? '').trim()).filter(Boolean)
            if (out.length) emit('commit', out)
            text.value = rest
            emit('update:modelValue', rest)
        }
        function choose(v) { forced.value = false; open.value = false; commit(v) }
        function forceNew() { forced.value = true; open.value = false; commit(text.value.trim()) }
        function onInput() {
            // A comma is a separator, not a character: it commits what precedes it and keeps the
            // rest. Handled here rather than on keydown so pasting `security, gdpr` works too.
            if (props.clearOnCommit && text.value.includes(',')) {
                const parts = text.value.split(',')
                const rest = parts.pop()
                forced.value = false
                return commitMany(parts, rest)
            }
            forced.value = false
            emit('update:modelValue', text.value.trim())
        }
        function onBlur() {
            // Synchronous, unlike the menu close below: clicking Save blurs the input and runs the
            // click handler immediately, so a tag typed but not entered has to land before it. The
            // suggestions can't be lost to this - they use mousedown.prevent, so they never blur.
            if (props.clearOnCommit && text.value.trim()) commit(exact.value?.value ?? text.value.trim())
            setTimeout(() => {
                open.value = false
                // Canonicalise on blur too, so it holds even if the menu was never opened.
                if (!props.clearOnCommit && exact.value && exact.value.value !== text.value.trim()) commit(exact.value.value)
            }, 150)
        }
        function onKey(e) {
            if (e.key === 'Escape') open.value = false
            if (e.key === 'Enter') {
                e.preventDefault()
                if (e.metaKey || e.ctrlKey) return forceNew()
                if (exact.value) return choose(exact.value.value)
                // In tag mode Enter has to add the value or the field appears not to work at all;
                // a near miss is still shown while typing, and a wrong chip is one click to drop.
                if (props.clearOnCommit && text.value.trim()) return choose(text.value.trim())
                open.value = false
            }
        }
        return { text, open, forced, exact, near, suggestions, state, borderClass, messageClass, message, choose, forceNew, onInput, onBlur, onKey }
    },
}

/**
 * A list field as chips plus one input, rather than a comma-joined string.
 *
 * The string version could not work: the box rendered `value.join(', ')`, so the comma you typed
 * was split, trimmed and joined away before the next frame — pressing `,` visibly did nothing.
 * Chips also get `tags` and `versions` the vocabulary checking every other field already had,
 * which is where drift actually happens: guide/guides, v8/V8.
 */
export const MetadataListInput = {
    components: { MetadataInput },
    template: `
        <div data-tag="MetadataListInput">
            <!-- The box first, so it lines up with the plain field beside it: chips above pushed
                 it down by however many tags happened to be on this document. -->
            <!-- Placeholder and hint both go once there's a chip: the box has been demonstrated,
                 and an example list sitting under real values only competes with them. -->
            <MetadataInput :model-value="''" @commit="add" clear-on-commit :values="values" :disabled="disabled"
                :placeholder="empty ? placeholder : ''" :hint="empty ? 'Comma or Enter adds one' : ''">
                <!-- Above the message, not below the box: a “＋ New value” warning appearing
                     between the two shunts every chip down a line while you type. -->
                <div v-if="modelValue?.length" class="flex flex-wrap gap-1 mt-1.5">
                    <span v-for="v in modelValue" :key="v"
                        class="inline-flex items-center gap-1 pl-1.5 pr-1 py-0.5 rounded border text-xs text-emerald-600 dark:text-emerald-400 border-emerald-500/50">
                        <span class="truncate max-w-[10rem]">{{ v }}</span>
                        <button v-if="!disabled" type="button" @click="remove(v)" :title="'Remove ' + v"
                            class="px-0.5 leading-none opacity-60 hover:opacity-100">×</button>
                    </span>
                </div>
            </MetadataInput>
        </div>
    `,
    props: {
        modelValue: { type: Array, default: () => [] },
        values: { type: Array, default: () => [] },
        placeholder: String,
        disabled: Boolean,
    },
    emits: ['update:modelValue'],
    setup(props, { emit }) {
        const empty = computed(() => !props.modelValue?.length)

        /** `values` is everything one edit produced — a comma-separated paste can be several. */
        function add(values) {
            const next = [...props.modelValue]
            for (const v of values) {
                const value = String(v ?? '').trim()
                if (value && !next.includes(value)) next.push(value)
            }
            if (next.length !== props.modelValue.length) emit('update:modelValue', next)
        }
        function remove(v) {
            emit('update:modelValue', props.modelValue.filter(x => x !== v))
        }
        return { empty, add, remove }
    },
}

/** Category tree with own/total counts — a parent whose docs are all in subfolders isn't empty. */
export const FacetRail = {
    name: 'FacetRail',
    components: { },
    template: `
        <div data-tag="FacetRail" class="space-y-4">
            <div>
                <div class="text-xs font-semibold uppercase tracking-wide mb-1.5" :class="[$styles.muted]">Category</div>
                <button type="button" @click="$emit('pick', 'category', null)"
                    class="w-full flex justify-between items-center px-2 py-1 rounded text-sm"
                    :class="active.category == null ? 'bg-blue-50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400' : 'hover:bg-gray-100 dark:hover:bg-gray-800'">
                    <span>All documents</span><span class="tabular-nums text-xs" :class="[$styles.muted]">{{ total.toLocaleString() }}</span>
                </button>
                <div v-for="node in flatTree" :key="node.path"
                    :style="{ paddingLeft: (node.depth * 12) + 'px' }">
                    <button type="button" @click="$emit('pick', 'category', node.path)"
                        class="w-full flex justify-between items-center px-2 py-1 rounded text-sm"
                        :class="active.category === node.path ? 'bg-blue-50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400' : 'hover:bg-gray-100 dark:hover:bg-gray-800'">
                        <span class="truncate">{{ node.name || '(root)' }}</span>
                        <span class="tabular-nums text-xs shrink-0" :class="[$styles.muted]"
                            :title="node.own + ' here, ' + node.total + ' including subfolders'">{{ node.total.toLocaleString() }}</span>
                    </button>
                </div>
            </div>

            <div v-for="field in otherFields" :key="field">
                <div class="text-xs font-semibold uppercase tracking-wide mb-1.5" :class="[$styles.muted]">{{ field }}</div>
                <button v-for="v in (facets[field]?.values || []).slice(0, 12)" :key="v.value" type="button"
                    @click="$emit('pick', field, active[field] === v.value ? null : v.value)"
                    class="w-full flex justify-between items-center px-2 py-1 rounded text-sm"
                    :class="active[field] === v.value ? 'bg-blue-50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400' : 'hover:bg-gray-100 dark:hover:bg-gray-800'">
                    <span class="truncate">{{ v.value }}</span>
                    <span class="tabular-nums text-xs" :class="[$styles.muted]">{{ v.count.toLocaleString() }}</span>
                </button>
                <div v-if="facets[field]?.null" class="px-2 py-0.5 text-xs" :class="[$styles.muted]">
                    {{ facets[field].null.toLocaleString() }} without a value
                </div>
            </div>
        </div>
    `,
    props: { facets: { type: Object, default: () => ({}) }, active: { type: Object, default: () => ({}) }, total: { type: Number, default: 0 } },
    emits: ['pick'],
    setup(props) {
        const flatTree = computed(() => {
            const out = []
            const walk = (nodes, depth) => (nodes || []).forEach(n => {
                out.push({ ...n, depth })
                walk(n.children, depth + 1)
            })
            walk(props.facets?.category?.tree, 0)
            return out
        })
        const otherFields = computed(() =>
            FACET_FIELDS.filter(f => f !== 'category' && (props.facets[f]?.values || []).length))
        return { flatTree, otherFields }
    },
}

/** Percentage populated per field — makes missing metadata visible, and clickable. */
export const CoverageStrip = {
    template: `
        <div v-if="rows.length" class="space-y-1">
            <div v-for="r in rows" :key="r.field" class="flex items-center gap-2.5 text-xs">
                <span class="w-20 shrink-0" :class="[$styles.muted]">{{ r.field }}</span>
                <span class="flex-1 h-1.5 rounded bg-gray-200 dark:bg-gray-700 overflow-hidden">
                    <i class="block h-full bg-blue-500" :style="{ width: r.pct + '%' }"></i>
                </span>
                <span class="w-28 text-right tabular-nums" :class="[$styles.muted]">
                    {{ r.pct }}%
                    <a v-if="r.missing" href="#" @click.prevent="$emit('pick', r.field)"
                        class="text-blue-600 dark:text-blue-400 hover:underline">· {{ r.missing.toLocaleString() }} missing</a>
                </span>
            </div>
        </div>
    `,
    props: { facets: Object, total: Number },
    emits: ['pick'],
    setup(props) {
        const rows = computed(() => FACET_FIELDS.filter(f => f !== 'category').map(field => {
            const missing = props.facets?.[field]?.null ?? 0
            const total = props.total || 0
            return { field, missing, pct: total ? Math.round(((total - missing) / total) * 100) : 0 }
        }).filter(r => props.total))
        return { rows }
    },
}

/**
 * The field grid every metadata editor is built from: import defaults, one document, a selection.
 *
 * `ops` is what separates them. Without it a field is a *value* — what this document, or this
 * import, will have. With it a field is an *operation*: "set where empty" and "overwrite" are
 * different edits over a selection, and offering only the second is how a backfill quietly
 * destroys the values someone already curated.
 */
export const MetadataFields = {
    components: { MetadataInput, MetadataListInput },
    template: `
        <div data-tag="MetadataFields" class="grid sm:grid-cols-2 gap-x-5 gap-y-5">
            <div v-for="f in allFields" :key="f.key" :class="f.wide ? 'sm:col-span-2' : ''">
                <div class="flex items-baseline justify-between gap-2 mb-1">
                    <label class="text-xs font-semibold">{{ f.label }}</label>
                    <!-- Per-field effect, next to the field causing it. The total in the footer is
                         by document, so it can't answer "which of my five edits does nothing". -->
                    <span v-if="ops && dirty(f.key) && counts?.[f.key]" class="text-xs tabular-nums"
                        :class="counts[f.key].change ? 'text-amber-600 dark:text-amber-400' : $styles.muted">
                        {{ counts[f.key].change.toLocaleString() }} change<span
                            v-if="counts[f.key].skipped">, {{ counts[f.key].skipped.toLocaleString() }} kept</span>
                    </span>
                </div>

                <!-- A list field is a list in both modes; the operation (add / remove / replace)
                     is what ops mode adds on top, and it applies to every chip. -->
                <MetadataListInput v-if="f.list" :model-value="modelValue[f.key] || []"
                    @update:modelValue="v => setValue(f.key, v)" :values="valuesFor(f.key)"
                    :placeholder="ops ? 'Leave unchanged' : f.placeholder" :disabled="opFor(f) === 'clear'" />
                <template v-else-if="f.free">
                    <input type="text" :value="modelValue[f.key] || ''"
                        @input="e => setValue(f.key, e.target.value)" :disabled="opFor(f) === 'clear'"
                        :placeholder="ops ? 'Leave unchanged' : (f.placeholder || 'Optional')"
                        :aria-invalid="f.key === 'sourceUrl' && !!sourceUrlTemplateError(modelValue[f.key])"
                        class="w-full rounded-md"
                        :class="[$styles.textInput, f.key === 'sourceUrl' && sourceUrlTemplateError(modelValue[f.key])
                            ? 'border-red-500 bg-red-50 dark:bg-red-900/20' 
                            : $styles.borderInput + ' ' + $styles.bgInput]">
                    <div v-if="f.variables?.length" class="mt-1.5 flex flex-wrap items-center gap-1">
                        <span class="text-xs mr-0.5" :class="[$styles.muted]">Append</span>
                        <button v-for="variable in f.variables" :key="variable" type="button"
                            @click="appendVariable(f.key, variable)" :title="'Append {' + variable + '} to Source URL'"
                            class="px-0.5 py-0.5 rounded border font-mono text-xs hover:bg-gray-50 dark:hover:bg-gray-800"
                            :class="[$styles.chromeBorder]">{{ '{' + variable + '}' }}</button>
                    </div>
                    <p v-if="f.key === 'sourceUrl' && sourceUrlTemplateError(modelValue[f.key])"
                        class="mt-1 text-xs text-red-600 dark:text-red-400">
                        {{ sourceUrlTemplateError(modelValue[f.key]) }}
                    </p>
                </template>
                <MetadataInput v-else :model-value="modelValue[f.key] || ''"
                    @update:modelValue="v => setValue(f.key, v)"
                    :values="valuesFor(f.key)" :disabled="opFor(f) === 'clear'"
                    :hint="summaryFor(f.key) ? '' : f.hint"
                    :placeholder="ops ? 'Leave unchanged' : (f.placeholder || 'Optional')" />

                <div v-if="ops" class="mt-1.5 flex flex-wrap items-center gap-2">
                    <select :value="opFor(f)" @change="e => setOp(f, e.target.value)"
                        class="pl-1.5 pr-6 py-0.5 text-xs rounded-md"
                        :class="[$styles.textInput, $styles.borderInput, $styles.bgInput, dirty(f.key) ? '' : 'opacity-50']">
                        <option v-for="o in opsFor(f)" :key="o.value" :value="o.value">{{ o.label }}</option>
                    </select>
                    <span v-if="dirty(f.key)" class="text-xs" :class="[$styles.muted]">{{ OP_HINTS[opFor(f)] }}</span>
                </div>

                <!-- What the selection holds today. Over many documents an empty input can't tell
                     "they all say guide" from "they say six different things", and those want
                     different edits. Each value is also the fastest way to type it. -->
                <p v-if="summaryFor(f.key)" class="mt-1.5 flex flex-wrap items-center gap-1 text-xs">
                    <span :class="[$styles.muted]">Now</span>
                    <button v-for="v in summaryFor(f.key).values.slice(0, 4)" :key="v.value" type="button"
                        @click="setValue(f.key, v.value)" :title="'Use “' + v.value + '”'"
                        class="px-1.5 py-0.5 rounded border max-w-[12rem] truncate" :class="[$styles.chromeBorder]">
                        {{ v.value }} <span class="tabular-nums" :class="[$styles.muted]">{{ v.count.toLocaleString() }}</span>
                    </button>
                    <span v-if="summaryFor(f.key).values.length > 4" :class="[$styles.muted]">
                        +{{ summaryFor(f.key).values.length - 4 }} more</span>
                    <span v-if="summaryFor(f.key).empty" :class="[$styles.muted]">
                        · {{ summaryFor(f.key).empty.toLocaleString() }} empty</span>
                </p>
                <p v-else-if="f.free && f.hint && !f.variables?.length" class="mt-1 text-xs" :class="[$styles.muted]">{{ f.hint }}</p>
            </div>
        </div>
    `,
    props: {
        modelValue: { type: Object, default: () => ({}) },  // { field: value }
        fields: { type: Array, default: () => META_FIELDS },
        listFields: { type: Array, default: () => META_LIST_FIELDS },
        facets: Object,
        ops: Object,        // { field: op } — present only when editing a selection
        summary: Object,    // { field: { values: [{value, count}], empty } } for that selection
        counts: Object,     // { field: { change, same, skipped } } from the server's dry run
    },
    emits: ['update:modelValue', 'update:ops'],
    setup(props, { emit }) {
        const allFields = computed(() => {
            const all = [...props.fields, ...props.listFields.map(f => ({ ...f, list: true }))]
            // Full-width fields go last: one in the middle either leaves a hole beside it or
            // pushes the field it displaced into the next row on its own.
            return [...all.filter(f => !f.wide), ...all.filter(f => f.wide)]
        })

        const OP_HINTS = {
            fill: 'only where empty',
            set: 'overwrites existing',
            clear: 'removes the value',
            add: 'keeps existing values',
            remove: 'where present',
        }
        function opsFor(f) {
            return f.list
                ? [{ value: 'add', label: 'Add to list' }, { value: 'remove', label: 'Remove from list' },
                   { value: 'set', label: 'Replace list' }, { value: 'clear', label: 'Clear' }]
                : [{ value: 'fill', label: 'Set where empty' }, { value: 'set', label: 'Overwrite' },
                   { value: 'clear', label: 'Clear' }]
        }
        // The safe default in both shapes: neither one destroys what's already there.
        const defaultOp = f => (f.list ? 'add' : 'fill')
        const opFor = f => props.ops?.[f.key] || defaultOp(f)

        function dirty(key) {
            const v = props.modelValue?.[key]
            return props.ops?.[key] === 'clear' || (Array.isArray(v) ? v.length > 0 : !!v)
        }
        function setValue(key, value) {
            emit('update:modelValue', { ...props.modelValue, [key]: value })
        }
        function appendVariable(key, variable) {
            let value = String(props.modelValue?.[key] || '')
            if (value.endsWith('}')) {
                value += variable === 'ext' ? '.' : '/'
            }
            setValue(key, value + `{${variable}}`)
        }
        function setOp(f, op) {
            emit('update:ops', { ...props.ops, [f.key]: op })
        }
        function summaryFor(key) {
            const s = props.summary?.[key]
            return s && (s.values?.length || s.empty) ? s : null
        }
        return {
            allFields, OP_HINTS, opsFor, opFor, dirty, setValue, setOp, summaryFor,
            appendVariable, sourceUrlTemplateError,
            valuesFor: key => fieldValues(props.facets, key),
        }
    },
}

/**
 * Metadata as a form: all fields at once, with room to read them.
 *
 * One component covers the import defaults and a single document, because they're the same job —
 * a set of values, typed once — and the differences are copy and which fields apply. The
 * selection editor next door adds operations to the same grid rather than forking it.
 */
export const MetadataDialog = {
    components: { MetadataFields },
    template: `
      <Teleport to="body">
        <!-- Teleported past the panel: rendered in place it inherits the main column's box, so it
             centred on the panel rather than the window and left the sidebar untinted. The app
             sidebar is z-100, so an overlay below that renders underneath it. -->
        <div class="fixed inset-0 z-[200] flex items-center justify-center p-4" style="z-index:200">
            <div class="fixed inset-0 bg-black/60" @click="$emit('close')"></div>
            <div class="relative w-full max-w-2xl max-h-[85vh] overflow-auto rounded-xl shadow-2xl bg-white dark:bg-gray-900 border"
                :class="[$styles.chromeBorder]">
                <div class="px-5 py-4 border-b flex items-center justify-between gap-4" :class="[$styles.chromeBorder]">
                    <div class="min-w-0">
                        <h3 class="font-semibold" :class="[$styles.heading]">{{ title }}</h3>
                        <p class="text-xs truncate" :class="[$styles.muted]">{{ subtitle }}</p>
                    </div>
                    <button type="button" @click="$emit('close')" class="p-1 rounded shrink-0" :class="[$styles.icon, $styles.iconHover]">
                        <svg class="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M18 6 6 18M6 6l12 12"/></svg>
                    </button>
                </div>

                <div class="p-5 space-y-4">
                    <MetadataFields :model-value="draft.defaults" @update:modelValue="v => draft.defaults = v"
                        :fields="fields" :list-fields="listFields" :facets="facets" />

                    <div v-if="showRules">
                        <button type="button" @click="rulesOpen = !rulesOpen" class="text-xs font-semibold flex items-center gap-1" :class="[$styles.muted]">
                            <span>{{ rulesOpen ? '▾' : '▸' }}</span> Rules by path - override the values above for parts of the tree
                        </button>
                        <div v-if="rulesOpen" class="mt-2 space-y-2">
                            <div v-for="(rule, i) in draft.rules" :key="i" class="flex flex-wrap items-center gap-2">
                                <input v-model="rule.match" placeholder="**/reference/**"
                                    class="w-48 px-2 py-1 rounded-md text-xs font-mono border-2 bg-white dark:bg-gray-900" :class="[$styles.chromeBorder]">
                                <select v-model="rule.field" class="pl-2 pr-8 py-1 rounded-md text-xs border-2 bg-white dark:bg-gray-900" :class="[$styles.chromeBorder]">
                                    <option value="">skip these files</option>
                                    <option v-for="f in [...fields, ...listFields]" :key="f.key" :value="f.key">{{ f.label }}</option>
                                </select>
                                <div v-if="rule.field" class="w-40">
                                    <input v-model="rule.value" placeholder="value"
                                        class="w-full px-2 py-1 rounded-md text-xs border-2 bg-white dark:bg-gray-900"
                                        :class="rule.field === 'sourceUrl' && sourceUrlTemplateError(rule.value)
                                            ? 'border-red-500 bg-red-50 dark:bg-red-900/20' : $styles.chromeBorder">
                                    <p v-if="rule.field === 'sourceUrl' && sourceUrlTemplateError(rule.value)"
                                        class="mt-1 text-xs text-red-600 dark:text-red-400">
                                        {{ sourceUrlTemplateError(rule.value) }}
                                    </p>
                                </div>
                                <button type="button" @click="draft.rules.splice(i, 1)" class="text-xs px-1.5 py-0.5 rounded border" :class="[$styles.chromeBorder]">Remove</button>
                            </div>
                            <button type="button" @click="draft.rules.push({ match: '', field: '', value: '' })"
                                class="text-xs px-2 py-1 rounded border" :class="[$styles.chromeBorder]">Add rule</button>
                        </div>
                    </div>
                </div>

                <div class="px-5 py-3 border-t flex items-center justify-between gap-3" :class="[$styles.chromeBorder]">
                    <button type="button" @click="clearAll" class="text-xs" :class="[$styles.muted]">Clear all</button>
                    <div class="flex items-center gap-3">
                        <span v-if="templateError" class="text-xs text-right text-red-600 dark:text-red-400">{{ templateError }}</span>
                        <span v-else-if="note" class="text-xs text-right" :class="[$styles.muted]">{{ note }}</span>
                        <button type="button" @click="$emit('close')" class="px-3 py-1.5 rounded-md text-sm border" :class="[$styles.secondaryButton]">Cancel</button>
                        <button type="button" @click="save" :disabled="!!templateError"
                            class="px-4 py-1.5 rounded-md text-sm font-semibold disabled:opacity-40 disabled:cursor-not-allowed"
                            :class="[$styles.primaryButton]">{{ saveLabel }}</button>
                    </div>
                </div>
            </div>
        </div>
      </Teleport>
    `,
    props: {
        modelValue: Object,
        facets: Object,
        showRules: { type: Boolean, default: true },
        fields: { type: Array, default: () => META_FIELDS },
        listFields: { type: Array, default: () => META_LIST_FIELDS },
        title: { type: String, default: 'Metadata for this import' },
        subtitle: { type: String, default: 'Applied to every document. Setting it here is what saves a bulk backfill later.' },
        note: String,
        saveLabel: { type: String, default: 'Done' },
    },
    emits: ['update:modelValue', 'close'],
    setup(props, { emit }) {
        const draft = ref({ defaults: {}, rules: [] })

        function reset() {
            draft.value = {
                defaults: { ...(props.modelValue?.defaults || {}) },
                rules: (props.modelValue?.rules || []).map(r => ({ ...r })),
            }
        }
        reset()
        watch(() => props.modelValue, reset)

        const templateError = computed(() => {
            const error = sourceUrlTemplateError(draft.value.defaults?.sourceUrl)
            if (error) return `Source URL: ${error}`
            const badRule = draft.value.rules.find(r => r.field === 'sourceUrl' && sourceUrlTemplateError(r.value))
            return badRule ? `Source URL rule: ${sourceUrlTemplateError(badRule.value)}` : ''
        })

        function clearAll() { draft.value = { defaults: {}, rules: [] } }
        function save() {
            if (templateError.value) return
            const defaults = {}
            for (const [k, v] of Object.entries(draft.value.defaults)) {
                if (v !== '' && v !== null && v !== undefined && !(Array.isArray(v) && !v.length)) defaults[k] = v
            }
            emit('update:modelValue', { defaults, rules: draft.value.rules.filter(r => r.match) })
            emit('close')
        }
        return { draft, rulesOpen: ref(false), templateError, sourceUrlTemplateError, clearAll, save }
    },
}

/**
 * The same grid over a selection, plus operations and a price.
 *
 * A dialog rather than a docked bar because editing five fields is a form, not a control strip —
 * and because the thing you most need while doing it is to see what those documents already say,
 * which needs room. The preview is the server's own dry run, so the number on the button is
 * produced by the code that does the work; it counts *documents*, because that's what a re-index
 * charges for — three fields changed on one document is one embedding pass, not three.
 */
export const BulkEditDialog = {
    components: { MetadataFields },
    template: `
      <Teleport to="body">
        <div class="fixed inset-0 flex items-center justify-center p-4" style="z-index:200">
            <div class="fixed inset-0 bg-black/60" @click="$emit('close')"></div>
            <div class="relative w-full max-w-3xl max-h-[88vh] flex flex-col rounded-xl shadow-2xl bg-white dark:bg-gray-900 border"
                :class="[$styles.chromeBorder]">
                <div class="px-5 py-4 border-b flex items-center justify-between gap-4" :class="[$styles.chromeBorder]">
                    <div class="min-w-0">
                        <h3 class="font-semibold" :class="[$styles.heading]">Edit {{ count.toLocaleString() }} documents</h3>
                        <p class="text-xs" :class="[$styles.muted]">
                            Fields you leave alone are untouched. Nothing reaches Gemini until you push.
                        </p>
                    </div>
                    <button type="button" @click="$emit('close')" class="p-1 rounded shrink-0" :class="[$styles.icon, $styles.iconHover]">
                        <svg class="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M18 6 6 18M6 6l12 12"/></svg>
                    </button>
                </div>

                <div class="p-5 overflow-auto">
                    <p v-if="loading" class="text-sm" :class="[$styles.muted]">Reading what these documents say…</p>
                    <MetadataFields v-model="values" v-model:ops="ops" :fields="fields" :list-fields="listFields"
                        :facets="facets" :summary="summary" :counts="preview?.fields" />
                </div>

                <div class="px-5 py-3 border-t flex flex-wrap items-center justify-between gap-3" :class="[$styles.chromeBorder]">
                    <!-- Dimmed while a preview is in flight: the numbers describe the edit you
                         had a moment ago, and Apply carries one of them. -->
                    <div class="text-xs flex flex-wrap items-center gap-x-4 gap-y-1" :class="stale ? 'opacity-40' : ''">
                        <span v-if="!dirtyCount" :class="[$styles.muted]">No changes yet</span>
                        <template v-else>
                            <span><b class="text-amber-600 dark:text-amber-400">{{ (preview?.change ?? 0).toLocaleString() }}</b> documents change</span>
                            <span v-if="preview?.skipped" :class="[$styles.muted]">{{ preview.skipped.toLocaleString() }} already set — skipped</span>
                            <span v-if="preview?.same" :class="[$styles.muted]">{{ preview.same.toLocaleString() }} already match</span>
                            <span class="text-amber-600 dark:text-amber-400">{{ (preview?.change ?? 0).toLocaleString() }} re-embeds when pushed</span>
                        </template>
                    </div>
                    <div class="flex gap-2">
                        <button type="button" @click="$emit('close')" class="px-3 py-1.5 rounded-md text-sm border" :class="[$styles.secondaryButton]">Cancel</button>
                        <button type="button" @click="apply" :disabled="!preview?.change || busy || stale"
                            class="px-4 py-1.5 rounded-md text-sm font-semibold text-white bg-blue-600 hover:bg-blue-700 disabled:opacity-40 disabled:cursor-not-allowed">
                            {{ busy ? 'Applying…' : (preview?.change ? 'Apply to ' + preview.change.toLocaleString() : 'Apply') }}
                        </button>
                    </div>
                </div>
            </div>
        </div>
      </Teleport>
    `,
    props: { selector: Object, count: Number, facets: Object },
    emits: ['close', 'applied'],
    setup(props, { emit }) {
        const fields = DOC_FIELDS
        const listFields = META_LIST_FIELDS
        const values = ref({})
        const ops = ref({})
        const summary = ref(null)
        const preview = ref(null)
        const stale = ref(false)
        const loading = ref(true)
        const busy = ref(false)

        const allKeys = [...fields, ...listFields].map(f => f.key)
        const isList = key => LIST_FIELDS.includes(key)

        /** Only the fields actually being edited become operations; the rest aren't sent at all. */
        const changes = computed(() => allKeys.reduce((out, key) => {
            const op = ops.value[key] || (isList(key) ? 'add' : 'fill')
            const value = values.value[key]
            const empty = isList(key) ? !value?.length : !value
            if (op !== 'clear' && empty) return out
            out.push({ field: key, op, value: isList(key) ? (value || []) : value })
            return out
        }, []))
        const dirtyCount = computed(() => changes.value.length)

        onMounted(async () => {
            const api = await ext.postJson('/documents/summary', { ...props.selector, fields: allKeys })
            loading.value = false
            if (api.error) return ext.setError(api.error)
            summary.value = api.response?.fields || {}
        })

        let seq = 0, timer = null
        async function refresh() {
            if (!dirtyCount.value) { preview.value = null; stale.value = false; return }
            const mine = ++seq
            const api = await ext.postJson('/documents/bulk', { ...props.selector, changes: changes.value, dryRun: true })
            if (mine !== seq) return           // a newer preview already superseded this one
            preview.value = api.error ? null : api.response
            stale.value = false
        }
        // Debounced: the preview follows typing, and a keystroke isn't worth a round trip.
        watch(changes, () => {
            stale.value = true
            clearTimeout(timer)
            timer = setTimeout(refresh, 300)
        })
        onBeforeUnmount(() => clearTimeout(timer))

        async function apply() {
            busy.value = true
            try {
                const api = await ext.postJson('/documents/bulk', { ...props.selector, changes: changes.value })
                if (api.error) return ext.setError(api.error)
                emit('applied', api.response)
            } finally { busy.value = false }
        }

        return { fields, listFields, values, ops, summary, preview, stale, loading, busy, dirtyCount, apply }
    },
}

/**
 * Pending = local metadata differs from the copy Gemini holds.
 *
 * The documents themselves are already in Gemini - `pending_documents()` only considers rows with
 * `uploadedAt` set - so this is never "your import didn't work". It's a staging buffer in front of
 * a costed operation: there's no API to patch a document's metadata in place, so pushing a change
 * means re-uploading and re-embedding it. Editing freely and pushing once is the whole point.
 *
 * Deliberately not styled as a warning. Nothing is broken and nothing is lost by ignoring it, so
 * an amber alert bar demanding attention right after an import was miscommunicating twice over:
 * about urgency, and about what had gone wrong (nothing).
 */
export const PendingBanner = {
    template: `
        <div data-tag="PendingBanner" v-if="pending.count" class="flex flex-wrap items-center justify-between gap-x-4 gap-y-2 px-4 py-2.5 mb-4 rounded-lg border text-sm"
             :class="[$styles.chromeBorder]">
            <div class="min-w-0">
                <span v-if="worker.running">
                    Pushing metadata to Gemini: <b>{{ worker.done }}/{{ worker.total }}</b><span v-if="worker.etaSeconds">, ~{{ Math.ceil(worker.etaSeconds / 60) }} min left</span>.
                </span>
                <span v-else>
                    <b>{{ pending.count.toLocaleString() }} document{{ pending.count === 1 ? '' : 's' }}</b>
                    {{ pending.count === 1 ? 'has' : 'have' }} metadata edits not yet pushed to Gemini.
                    <button type="button" @click="showWhy = !showWhy" class="underline" :class="[$styles.muted]">
                        {{ showWhy ? 'hide' : "what's this?" }}
                    </button>
                </span>
                <p v-if="showWhy && !worker.running" class="text-xs mt-1 max-w-2xl" :class="[$styles.muted]">
                    The documents are already in Gemini — it's the metadata that differs. Browsing and
                    filtering here use your local values either way; pushing only matters for what a
                    chat can filter on. There's no way to patch metadata in place, so pushing
                    re-uploads and re-embeds each document — which is why it's a step you take once,
                    when you're done editing, rather than after every change.
                </p>
            </div>
            <div class="flex gap-2 shrink-0">
                <button v-if="!worker.running && pending.ids?.length" type="button" @click="$emit('review', pending.ids)"
                    class="px-3 py-1 rounded-md text-xs font-medium" :class="[$styles.secondaryButton]">Review</button>
                <button v-if="worker.running" type="button" @click="cancel"
                    class="px-3 py-1 rounded-md text-xs font-medium" :class="[$styles.secondaryButton]">Cancel</button>
                <button v-else type="button" @click="reindex" :disabled="busy"
                    class="px-3 py-1 rounded-md text-xs font-medium" :class="[$styles.primaryButton]">
                    {{ busy ? 'Queueing…' : 'Push ' + pending.count.toLocaleString() + ' to Gemini' }}
                </button>
            </div>
        </div>
    `,
    props: { storeId: [String, Number] },
    emits: ['changed', 'review'],
    setup(props, { emit }) {
        const pending = ref({ count: 0 })
        const worker = ref({})
        const busy = ref(false)
        const showWhy = ref(false)
        let timer = null

        async function refresh() {
            const api = await ext.getJson(`/documents/pending?filestoreId=${props.storeId}`)
            if (api.error) return
            pending.value = api.response
            worker.value = api.response.worker || {}
            // Only poll while there's something in flight; an idle store shouldn't be chatty.
            clearTimeout(timer)
            if (worker.value.running) timer = setTimeout(refresh, 3000)
        }
        async function reindex() {
            busy.value = true
            try {
                const api = await ext.postJson(`/filestores/${props.storeId}/reindex`, {})
                if (api.error) return ext.setError(api.error)
                emit('changed')
                refresh()
            } finally { busy.value = false }
        }
        async function cancel() {
            await ext.postJson('/worker/cancel', {})
            refresh()
        }

        onMounted(refresh)
        watch(() => props.storeId, refresh)
        return { pending, worker, busy, showWhy, reindex, cancel, refresh }
    },
}
