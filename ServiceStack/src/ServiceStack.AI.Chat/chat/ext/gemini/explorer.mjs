import { ref, computed, onMounted, onBeforeUnmount } from 'vue'

let ext = null
export function initExplorer(extScope) {
    ext = extScope
}

/**
 * A styled checkbox.
 *
 * Native checkboxes ignore most theming and render differently per platform and color scheme,
 * so this draws its own: rounded box, gray border that adapts to dark mode, blue fill with a
 * white tick when checked. v-model compatible.
 */
export const CheckBox = {
    template: `
        <input type="checkbox" :checked="modelValue" @change="$emit('update:modelValue', $event.target.checked)"
            class="appearance-none size-4 shrink-0 rounded border transition-colors cursor-pointer bg-no-repeat bg-center
                   border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900
                   checked:bg-blue-600 checked:border-blue-600
                   focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500/40"
            :style="modelValue ? checkedStyle : null">
    `,
    props: { modelValue: Boolean },
    emits: ['update:modelValue'],
    setup(props) {
        // The tick as a data URI rather than a ::after glyph: it scales crisply and needs no font.
        const TICK = "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 20' "
            + "fill='white'%3E%3Cpath fill-rule='evenodd' d='M16.7 5.3a1 1 0 0 1 0 1.4l-7.5 7.5a1 1 0 0 1-1.4 0"
            + "L3.3 9.7a1 1 0 0 1 1.4-1.4l3.8 3.8 6.8-6.8a1 1 0 0 1 1.4 0z' clip-rule='evenodd'/%3E%3C/svg%3E"
        // Keep the checked fill inline with the tick. In light mode the generated bg-white
        // utility can otherwise win the cascade over checked:bg-blue-600, leaving a white tick
        // on a white box. The checked utility classes remain for hover/theme consistency.
        const checkedStyle = computed(() => ({
            backgroundColor: '#2563eb',
            backgroundImage: `url("${TICK}")`,
        }))
        return { checkedStyle }
    },
}

/**
 * A dropdown panel anchored to its trigger.
 *
 * Exists so Categories and Coverage can be reached from the toolbar without either of them
 * holding a permanent column. Closes on outside click and on Escape, because a panel that only
 * closes by pressing its own button is a panel people leave open.
 */
export const Popover = {
    template: `
        <div class="relative inline-block" ref="root">
            <button type="button" ref="trigger" @click="toggle"
                class="px-2 py-1 rounded-md border text-xs font-medium inline-flex items-center gap-1"
                :class="[open ? $styles.primaryButton : $styles.secondaryButton]">
                <!-- SVG rather than a ▸ glyph: glyphs sit in a large em box and render tiny at
                     button sizes. Rotates right -> down as the panel opens. The icon prop drops
                     it entirely for triggers that carry their own glyph. -->
                <svg v-if="!icon" class="size-4 opacity-60 transition-transform duration-200 ease-out"
                    :class="open ? 'rotate-90' : ''"
                    viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"
                    stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                    <polyline points="9 6 15 12 9 18"/>
                </svg>
                <slot name="label">{{ label }}</slot>
                <span v-if="count" class="tabular-nums opacity-70">{{ count }}</span>
            </button>
            <!-- Teleported: the document card is overflow-hidden, which clips any panel that
                 tries to escape it. Fixed coordinates measured from the trigger avoid inheriting
                 that clip, and any stacking context along the way. -->
            <Teleport to="body">
                <div v-if="open" ref="panel" class="fixed rounded-lg border shadow-xl p-3 bg-white dark:bg-gray-900"
                    :class="[$styles.chromeBorder, wide ? 'w-96' : 'w-72']"
                    :style="{ top: pos.top + 'px', left: pos.left + 'px', zIndex: 200 }">
                    <!-- overflow-x-hidden: overflow-y-auto makes the browser compute
                         overflow-x as auto too, so one wide child would add a horizontal
                         scrollbar to a vertically-scrolling panel. -->
                    <div class="max-h-96 overflow-y-auto overflow-x-hidden">
                        <slot :close="close" />
                    </div>
                </div>
            </Teleport>
        </div>
    `,
    // The icon prop drops the chevron: on a glyph-only trigger it reads as part of the glyph.
    props: { label: String, count: [Number, String], wide: Boolean, icon: Boolean },
    emits: ['open'],
    setup(props, { emit }) {
        const open = ref(false)
        const root = ref(null)
        const trigger = ref(null)
        const panel = ref(null)
        const pos = ref({ top: 0, left: 0 })

        function place() {
            const t = trigger.value?.getBoundingClientRect()
            if (!t) return
            const width = props.wide ? 384 : 288
            // Right-aligned to the trigger, then pulled back inside the viewport rather than
            // being allowed to run off the edge on a narrow window.
            const left = Math.max(8, Math.min(t.right - width, window.innerWidth - width - 8))
            pos.value = { top: t.bottom + 4, left }
        }
        function close() { open.value = false }
        function toggle() {
            open.value = !open.value
            if (open.value) {
                place()
                // Lets the consumer refresh the panel's data at open time, so anything edited
                // elsewhere since mount (e.g. trusted folders) is what the panel shows.
                emit('open')
            }
        }
        function onDocClick(e) {
            if (!open.value) return
            if (root.value?.contains(e.target) || panel.value?.contains(e.target)) return
            close()
        }
        function onKey(e) { if (e.key === 'Escape') close() }
        onMounted(() => {
            document.addEventListener('click', onDocClick, true)
            document.addEventListener('keydown', onKey)
            window.addEventListener('resize', place)
            window.addEventListener('scroll', place, true)
        })
        onBeforeUnmount(() => {
            document.removeEventListener('click', onDocClick, true)
            document.removeEventListener('keydown', onKey)
            window.removeEventListener('resize', place)
            window.removeEventListener('scroll', place, true)
        })
        return { open, root, trigger, panel, pos, toggle, close }
    },
}

/**
 * Where you are, as a path you can click back through.
 *
 * A category is the shared path prefix of ingested documents, so the breadcrumb is the honest
 * rendering of it: each segment is a real, browsable location rather than a filter chip that
 * happens to contain slashes.
 */
export const Breadcrumb = {
    template: `
        <nav class="flex items-center flex-wrap gap-x-1 gap-y-0.5 text-sm min-w-0">
            <button type="button" @click="$emit('go', null)"
                class="px-1.5 py-0.5 rounded hover:bg-gray-100 dark:hover:bg-gray-800"
                :class="path == null ? 'font-semibold' : $styles.muted">{{ rootLabel || 'Top level' }}</button>
            <template v-if="path === ''">
                <span :class="[$styles.muted]">/</span>
                <span class="px-1.5 py-0.5 font-semibold">(uncategorised)</span>
            </template>
            <template v-for="(seg, i) in segments" :key="seg.path">
                <span :class="[$styles.muted]">/</span>
                <button type="button" @click="$emit('go', seg.path)"
                    class="px-1.5 py-0.5 rounded hover:bg-gray-100 dark:hover:bg-gray-800 truncate max-w-48"
                    :class="i === segments.length - 1 ? 'font-semibold' : $styles.muted">{{ seg.name }}</button>
            </template>
        </nav>
    `,
    props: { path: { type: String, default: null }, rootLabel: String },
    emits: ['go'],
    setup(props) {
        const segments = computed(() => {
            if (!props.path) return []
            const parts = props.path.split('/').filter(Boolean)
            return parts.map((name, i) => ({ name, path: parts.slice(0, i + 1).join('/') }))
        })
        return { segments }
    },
}

/**
 * The active non-category filters, each removable.
 *
 * Category is deliberately absent: it's a location, shown in the breadcrumb. Mixing the two
 * would leave you with two competing answers to "where am I".
 */
export const FilterChips = {
    template: `
        <div v-if="chips.length" class="flex items-center flex-wrap gap-1.5">
            <span v-for="c in chips" :key="c.field"
                class="inline-flex items-center gap-1.5 pl-2 pr-1 py-0.5 rounded-full text-[11px] border"
                :class="[$styles.tagLabel]">
                {{ c.field }} = {{ c.value === '' ? '(none)' : c.value }}
                <button type="button" class="opacity-60 hover:opacity-100 font-bold px-0.5"
                    @click="$emit('remove', c.field)" :title="'Remove ' + c.field + ' filter'">×</button>
            </span>
            <button v-if="chips.length > 1" type="button" @click="$emit('clear')"
                class="text-[11px] underline" :class="[$styles.muted]">clear all</button>
        </div>
    `,
    props: { active: { type: Object, default: () => ({}) } },
    emits: ['remove', 'clear'],
    setup(props) {
        const chips = computed(() => Object.entries(props.active)
            .filter(([f, v]) => f !== 'category' && v !== null && v !== undefined)
            .map(([field, value]) => ({ field, value })))
        return { chips }
    },
}

/**
 * The category tree, for jumping anywhere rather than walking there folder by folder.
 *
 * `total` is the subtree count and `own` what sits directly in the folder; showing total is what
 * stops a parent whose documents all live in children from reading as empty.
 */
export const CategoryTree = {
    template: `
        <div>
            <button type="button" @click="$emit('go', null)"
                class="w-full flex justify-between items-center px-2 py-1 rounded text-sm"
                :class="active == null ? 'bg-blue-50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400' : 'hover:bg-gray-100 dark:hover:bg-gray-800'">
                <span>{{ rootLabel || 'Top level' }}</span>
                <span class="tabular-nums text-xs" :class="[$styles.muted]"
                    title="Everything in the store">{{ (total || 0).toLocaleString() }}</span>
            </button>
            <div v-for="node in flat" :key="node.path" :style="{ paddingLeft: (node.depth * 12) + 'px' }">
                <button type="button" @click="$emit('go', node.path)"
                    class="w-full flex justify-between items-center px-2 py-1 rounded text-sm"
                    :class="active === node.path ? 'bg-blue-50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400' : 'hover:bg-gray-100 dark:hover:bg-gray-800'">
                    <span class="truncate">{{ node.path === '' ? '(uncategorised)' : node.name }}</span>
                    <span class="tabular-nums text-xs shrink-0" :class="[$styles.muted]"
                        :title="node.own + ' here, ' + node.total + ' including subfolders'">{{ node.total.toLocaleString() }}</span>
                </button>
            </div>
            <p v-if="!flat.length" class="text-xs px-2 py-3 text-center" :class="[$styles.muted]">
                No folders — every document is at the top level.
            </p>
        </div>
    `,
    props: { tree: Array, active: { type: String, default: null }, total: Number, rootLabel: String },
    emits: ['go'],
    setup(props) {
        const flat = computed(() => {
            const out = []
            const walk = (nodes, depth) => (nodes || []).forEach(n => {
                out.push({ ...n, depth })
                walk(n.children, depth + 1)
            })
            walk(props.tree, 0)
            return out
        })
        return { flat }
    },
}

/**
 * Facet values for everything that isn't category, as a filter picker.
 *
 * Sits alongside coverage in the same panel because the two answer one question between them:
 * how much of the corpus carries this field, and which values can I narrow to.
 */
export const FacetPicker = {
    template: `
        <div class="space-y-3">
            <div v-for="field in fields" :key="field">
                <div class="flex items-baseline justify-between">
                    <div class="text-xs font-semibold uppercase tracking-wide" :class="[$styles.muted]">{{ field }}</div>
                    <button v-if="active[field] != null" type="button" @click="$emit('pick', field, null)"
                        class="text-[11px] underline" :class="[$styles.muted]">clear</button>
                </div>
                <button v-for="v in (facets[field]?.values || []).slice(0, 12)" :key="v.value" type="button"
                    @click="$emit('pick', field, active[field] === v.value ? null : v.value)"
                    class="w-full flex justify-between items-center px-2 py-1 rounded text-sm"
                    :class="active[field] === v.value ? 'bg-blue-50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400' : 'hover:bg-gray-100 dark:hover:bg-gray-800'">
                    <span class="truncate">{{ v.value }}</span>
                    <span class="tabular-nums text-xs" :class="[$styles.muted]">{{ v.count.toLocaleString() }}</span>
                </button>
                <button v-if="facets[field]?.null" type="button" @click="$emit('missing', field)"
                    class="w-full text-left px-2 py-0.5 text-xs hover:underline" :class="[$styles.muted]">
                    {{ facets[field].null.toLocaleString() }} without a value
                </button>
            </div>
            <p v-if="!fields.length" class="text-xs py-3 text-center" :class="[$styles.muted]">
                No metadata to filter on yet. Add some from the document list or during import.
            </p>
        </div>
    `,
    props: { facets: Object, active: Object, fieldNames: Array },
    emits: ['pick', 'missing'],
    setup(props) {
        const fields = computed(() => (props.fieldNames || [])
            .filter(f => f !== 'category' && (props.facets?.[f]?.values || []).length))
        return { fields }
    },
}

/**
 * A centred modal dialog.
 *
 * Coverage outgrew a dropdown: six bars, six value lists and the sync state don't fit in 384px
 * without a scrollbar that hides half of it. A dialog is the honest answer to "this needs room".
 */
export const Modal = {
    template: `
        <Teleport to="body">
            <!-- The app sidebar is z-100, so anything below that renders underneath it. -->
            <div v-if="open" class="fixed inset-0 flex items-start justify-center p-4 sm:p-6 overflow-y-auto"
                style="z-index:200" @click.self="$emit('close')">
                <!-- The backdrop is what clicks actually land on (it covers the root), so the
                     dismiss handler lives here rather than only on the root's @click.self. -->
                <div class="fixed inset-0 bg-black/40" @click="$emit('close')"></div>
                <div class="relative w-full max-w-4xl rounded-xl border shadow-2xl bg-white dark:bg-gray-900"
                    :class="[$styles.chromeBorder]">
                    <div class="flex items-start justify-between gap-4 px-5 py-3 border-b" :class="[$styles.chromeBorder]">
                        <div>
                            <h3 class="text-base font-semibold">{{ title }}</h3>
                            <p v-if="subtitle" class="text-xs mt-0.5" :class="[$styles.muted]">{{ subtitle }}</p>
                        </div>
                        <button type="button" @click="$emit('close')"
                            class="shrink-0 px-2 py-0.5 rounded-md text-lg" :class="[$styles.muted]" title="Close">×</button>
                    </div>
                    <div class="px-5 py-4"><slot /></div>
                </div>
            </div>
        </Teleport>
    `,
    props: { open: Boolean, title: String, subtitle: String },
    emits: ['close'],
    setup(props, { emit }) {
        function onKey(e) { if (e.key === 'Escape' && props.open) emit('close') }
        onMounted(() => document.addEventListener('keydown', onKey))
        onBeforeUnmount(() => document.removeEventListener('keydown', onKey))
        return {}
    },
}

/**
 * What you can do with a selection, docked where it stays reachable.
 *
 * Slim on purpose: it names the selection and offers the operations, and the operations open
 * somewhere with room. A bar wide enough to *edit* metadata in covers the rows you're selecting,
 * which is the one thing it must not do.
 */
export const SelectionBar = {
    template: `
      <Teleport to="body">
        <!-- Spans the window, so it has to sit above the app sidebar (z-100) rather than slide
             under it. Teleported for the same reason the dialogs are. -->
        <div data-tag="SelectionBar" v-if="count" style="z-index:200"
            class="fixed left-0 right-0 bottom-0 border-t shadow-2xl px-5 py-2.5 bg-white dark:bg-gray-900"
            :class="[$styles.chromeBorder]">
            <div class="max-w-6xl mx-auto flex flex-wrap items-center gap-x-4 gap-y-2">
                <span class="text-sm">
                    <b class="tabular-nums">{{ count.toLocaleString() }}</b>
                    {{ count === 1 ? 'document' : 'documents' }} selected
                    <span v-if="allMatching" :class="[$styles.muted]">— everything matching this filter</span>
                </span>
                <div class="grow"></div>
                <button type="button" @click="$emit('edit')"
                    class="px-4 py-1.5 rounded-md text-sm font-semibold text-white bg-blue-600 hover:bg-blue-700">
                    Edit metadata
                </button>
                <button type="button" @click="$emit('delete')"
                    class="px-3 py-1.5 rounded-md text-sm font-medium border border-red-500/60 text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20">
                    Delete
                </button>
                <button type="button" @click="$emit('clear')"
                    class="px-3 py-1.5 rounded-md text-sm border" :class="[$styles.secondaryButton]">Clear selection</button>
            </div>
        </div>
      </Teleport>
    `,
    props: { count: Number, allMatching: Boolean },
    emits: ['edit', 'delete', 'clear'],
}

/**
 * Confirm for the operations a pending buffer can't take back.
 *
 * A metadata edit gets a preview because it's undoable until a re-index; a delete gets this,
 * because it isn't. The count in the button is the count the same selector produced, so what you
 * agree to is what runs.
 */
export const ConfirmDialog = {
    template: `
      <Teleport to="body">
        <div v-if="open" class="fixed inset-0 flex items-center justify-center p-4" style="z-index:210">
            <div class="fixed inset-0 bg-black/60" @click="$emit('close')"></div>
            <div class="relative w-full max-w-lg rounded-xl shadow-2xl border bg-white dark:bg-gray-900"
                :class="[$styles.chromeBorder]">
                <div class="px-5 py-4 border-b" :class="[$styles.chromeBorder]">
                    <h3 class="font-semibold" :class="[$styles.heading]">{{ title }}</h3>
                </div>
                <div class="px-5 py-4 text-sm"><slot /></div>
                <div class="px-5 py-3 border-t flex justify-end gap-2" :class="[$styles.chromeBorder]">
                    <button type="button" @click="$emit('close')" :disabled="busy"
                        class="px-3 py-1.5 rounded-md text-sm border disabled:opacity-50" :class="[$styles.chromeBorder]">Cancel</button>
                    <button type="button" @click="$emit('confirm')" :disabled="busy"
                        class="px-4 py-1.5 rounded-md text-sm font-semibold text-white disabled:opacity-50"
                        :class="danger ? 'bg-red-600 hover:bg-red-700' : 'bg-blue-600 hover:bg-blue-700'">
                        {{ busy ? busyLabel : confirmLabel }}
                    </button>
                </div>
            </div>
        </div>
      </Teleport>
    `,
    props: {
        open: Boolean,
        title: String,
        confirmLabel: { type: String, default: 'Confirm' },
        busyLabel: { type: String, default: 'Working…' },
        busy: Boolean,
        danger: { type: Boolean, default: true },
    },
    emits: ['confirm', 'close'],
    setup(props, { emit }) {
        function onKey(e) { if (e.key === 'Escape' && props.open && !props.busy) emit('close') }
        onMounted(() => document.addEventListener('keydown', onKey))
        onBeforeUnmount(() => document.removeEventListener('keydown', onKey))
        return {}
    },
}

/**
 * Sync state, as an explanation rather than an alarm.
 *
 * Lives inside the Coverage dialog because it is never urgent: the documents are already in
 * Gemini and browsing uses local values regardless. What was a permanent amber bar demanding an
 * action it could never satisfy is now a section you open, read, and act on if it applies.
 */
export const SyncState = {
    template: `
        <div>
            <div v-if="worker.running">
                <div class="flex items-center justify-between gap-3">
                    <span class="text-sm">Pushing to Gemini — <b>{{ worker.done }}/{{ worker.total }}</b><span v-if="worker.etaSeconds">, ~{{ Math.ceil(worker.etaSeconds / 60) }} min left</span></span>
                    <button type="button" @click="$emit('cancel')"
                        class="px-3 py-1 rounded-md border text-xs font-medium" :class="[$styles.chromeBorder]">Cancel</button>
                </div>
                <div class="mt-2 h-1.5 rounded bg-gray-200 dark:bg-gray-700 overflow-hidden">
                    <i class="block h-full bg-blue-500" :style="{ width: (worker.total ? (worker.done / worker.total * 100) : 0) + '%' }"></i>
                </div>
            </div>

            <div v-else-if="!pending.count" class="text-sm" :class="[$styles.muted]">
                Every document's metadata matches the copy in Gemini. Nothing to push.
            </div>

            <div v-else>
                <p class="text-sm">
                    <b>{{ pending.count.toLocaleString() }} document{{ pending.count === 1 ? '' : 's' }}</b>
                    {{ pending.count === 1 ? 'has' : 'have' }} metadata that differs from the copy in Gemini.
                </p>
                <!-- What changed, not just how many. This is the reason to spend a re-index. -->
                <ul class="mt-2 space-y-1">
                    <li v-for="f in pending.fields || []" :key="f.field" class="flex items-center gap-2 text-xs">
                        <code class="font-mono">{{ f.field }}</code>
                        <span class="grow h-1 rounded bg-gray-200 dark:bg-gray-700 overflow-hidden">
                            <i class="block h-full bg-blue-500"
                                :style="{ width: (pending.count ? (f.count / pending.count * 100) : 0) + '%' }"></i>
                        </span>
                        <span class="tabular-nums shrink-0" :class="[$styles.muted]">{{ f.count.toLocaleString() }}</span>
                    </li>
                </ul>
                <p v-if="pending.neverPushed" class="text-xs mt-2" :class="[$styles.muted]">
                    {{ pending.neverPushed.toLocaleString() }} of these have never had metadata pushed at all.
                </p>

                <p class="text-xs mt-3" :class="[$styles.muted]">
                    Browsing, searching and filtering on this page use your local values, so nothing here
                    is broken. Pushing only changes what a <em>chat</em> can filter on. Gemini has no way to
                    patch metadata in place, so pushing re-uploads and re-embeds each document — which is
                    why it's one deliberate step when you've finished editing, not something that happens
                    after every change.
                </p>

                <div class="mt-3 flex items-center gap-2">
                    <button type="button" @click="$emit('push')" :disabled="busy"
                        class="px-3 py-1.5 rounded-md text-sm font-medium disabled:opacity-50" :class="[$styles.primaryButton]">
                        {{ busy ? 'Queueing…' : 'Push ' + pending.count.toLocaleString() + ' to Gemini' }}
                    </button>
                    <button type="button" @click="$emit('review', pending.ids)"
                        class="px-3 py-1.5 rounded-md border text-sm font-medium" :class="[$styles.chromeBorder]">
                        Show me which
                    </button>
                </div>
            </div>
        </div>
    `,
    props: { pending: { type: Object, default: () => ({}) }, worker: { type: Object, default: () => ({}) }, busy: Boolean },
    emits: ['push', 'cancel', 'review'],
}

/**
 * A filesystem path in a fixed amount of space.
 *
 * Truncates from the *front*: the tail of a path identifies it, so
 * `/Users/me/src/ServiceStack/docs.servicestack.net/MyApp/_pages` is far more useful as
 * `…/docs.servicestack.net/MyApp/_pages` than as `/Users/me/src/ServiceStack/doc…`.
 *
 * Done in JS rather than with the `direction: rtl` CSS trick, which reorders the leading
 * separator and renders the path with its slash on the wrong end.
 */
export const PathText = {
    // The overflow-hidden/ellipsis classes are the hard stop: without a `max`, the path uses
    // whatever width the container has and only ellipsizes when it genuinely doesn't fit;
    // with one, the JS cuts at a segment boundary up front.
    template: `<span class="font-mono block min-w-0 max-w-full overflow-hidden text-ellipsis whitespace-nowrap" :title="path">{{ shown }}</span>`,
    props: { path: String, max: { type: Number, default: 0 } },
    setup(props) {
        const shown = computed(() => {
            const p = String(props.path || '')
            if (!props.max || p.length <= props.max) return p
            // Drop whole segments where possible, so the result is still a readable path.
            const segs = p.split('/')
            let out = ''
            for (let i = segs.length - 1; i >= 0; i--) {
                const next = '/' + segs[i] + out
                if (next.length + 1 > props.max) break
                out = next
            }
            return out ? '…' + out : '…' + p.slice(-(props.max - 1))
        })
        return { shown }
    },
}

/**
 * Where a folder import may read from, and why.
 *
 * Two lists rather than one: paths an Admin added to config, and paths the server itself grants.
 * They enforce identically, but only the first is something a person chose, and conflating them
 * makes a configured list look like it isn't taking effect.
 */
export const RootsPanel = {
    components: { PathText },
    template: `
        <div class="space-y-3">
            <p v-if="unrestricted" class="text-xs" :class="[$styles.muted]">
                You're an Admin, so you can import from any folder on this machine. These are the
                folders everyone else is limited to.
            </p>
            <div v-for="g in groups" :key="g.key">
                <div class="text-xs font-semibold uppercase tracking-wide" :class="[$styles.muted]">{{ g.label }}</div>
                <p class="text-[11px] mb-1" :class="[$styles.muted]">{{ g.blurb }}</p>
                <button v-for="p in g.paths" :key="p" type="button" @click="$emit('pick', p)"
                    class="w-full min-w-0 text-left px-2 py-1 rounded text-xs hover:bg-gray-100 dark:hover:bg-gray-800 flex"
                    title="Use this folder">
                    <PathText :path="p" :max="46" />
                </button>
                <p v-if="!g.paths.length" class="text-[11px] px-2 py-1" :class="[$styles.muted]">None.</p>
            </div>
        </div>
    `,
    props: { roots: Object, unrestricted: Boolean },
    emits: ['pick'],
    setup(props) {
        const groups = computed(() => [
            { key: 'trusted', label: 'Trusted paths', paths: props.roots?.trusted || [],
              blurb: 'Added by an Admin under Trusted import folders.' },
            { key: 'allowed', label: 'Allowed directories', paths: props.roots?.allowed || [],
              blurb: 'Granted by the server itself — workspace, temp and agent folders.' },
        ])
        return { groups }
    },
}
