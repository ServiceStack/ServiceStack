
import { ref, computed, watch, onMounted } from "vue"
import { useMetadata } from "@servicestack/vue"
import { mapGet } from "@servicestack/client"
import SchemaGrid from "./SchemaGrid.mjs"
import { rowSchema as rowSchemaOf } from "./useSchemas.mjs"
// $id is the URL to call. ServiceStack's pre-defined routes carry no placeholders, but a
// hand-written schema may, so {Id} is still filled from the data and kept out of the body.
// /todos/{Id} -> /todos/1
export const resolvePath = (path, data) =>
    path.replace(/\{(\w+)\}/g, (_, name) => encodeURIComponent(mapGet(data, name) ?? ''))
const EMPTY_QUERY = { filters: {}, orderBy: '', skip: 0 }
const template = `
<div>
    <div class="flex items-center gap-3 mb-3 min-h-9">
        <button type="button" @click="showPrefs = true" title="Query Preferences"
                class="rounded-md p-1.5 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200
                       hover:bg-gray-100 dark:hover:bg-gray-800">
            <span class="sr-only">Query Preferences</span>
            <svg class="w-7 h-7" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke-width="1.5">
                <path d="M9 3H3.6a.6.6 0 0 0-.6.6v16.8a.6.6 0 0 0 .6.6H9M9 3v18M9 3h6M9 21h6m0-18h5.4a.6.6 0 0 1
                    .6.6v16.8a.6.6 0 0 1-.6.6H15m0-18v18" stroke="currentColor"/>
            </svg>
        </button>
        <div v-if="schema.query" class="flex items-center">
            <button type="button" title="First page" :disabled="!canPrev" @click="skipTo(0)" :class="pagingClass(canPrev)">
                <span class="sr-only">First page</span>
                <svg class="w-7 h-7" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                    <path d="M18.41 16.59L13.82 12l4.59-4.59L17 6l-6 6l6 6zM6 6h2v12H6z" fill="currentColor"/>
                </svg>
            </button>
            <button type="button" title="Previous page" :disabled="!canPrev" @click="skipTo(skip - take)" :class="pagingClass(canPrev)">
                <span class="sr-only">Previous page</span>
                <svg class="w-7 h-7" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                    <path d="M15.41 7.41L14 6l-6 6l6 6l1.41-1.41L10.83 12z" fill="currentColor"/>
                </svg>
            </button>
            <button type="button" title="Next page" :disabled="!canNext" @click="skipTo(skip + take)" :class="pagingClass(canNext)">
                <span class="sr-only">Next page</span>
                <svg class="w-7 h-7" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                    <path d="M10 6L8.59 7.41L13.17 12l-4.58 4.59L10 18l6-6z" fill="currentColor"/>
                </svg>
            </button>
            <button type="button" title="Last page" :disabled="!canNext" @click="skipTo(lastPageSkip)" :class="pagingClass(canNext)">
                <span class="sr-only">Last page</span>
                <svg class="w-7 h-7" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                    <path d="M5.59 7.41L10.18 12l-4.59 4.59L7 18l6-6l-6-6zM16 6h2v12h-2z" fill="currentColor"/>
                </svg>
            </button>
        </div>
        <div v-if="schema.query" class="px-2 text-gray-500 dark:text-gray-400 whitespace-nowrap">
            <span v-if="total"><span class="hidden xl:inline">Showing Results </span>{{ from }} - {{ to }} of {{ total }}</span>
            <span v-else-if="!listError">No Results</span>
        </div>
        <div v-if="activeFilters.length" class="flex flex-wrap items-center gap-2">
            <span v-for="f in activeFilters" :key="f.key"
                  class="inline-flex items-center gap-1 rounded-full pl-2.5 pr-1 py-0.5 text-xs
                         bg-indigo-50 dark:bg-indigo-900/40 text-indigo-700 dark:text-indigo-300
                         border border-indigo-200 dark:border-indigo-800">
                {{ f.label }} {{ f.op }} {{ f.value }}
                <button type="button" @click="clearFilter(f.key)" :title="'Remove ' + f.label + ' filter'"
                        class="rounded-full p-0.5 hover:bg-indigo-200 dark:hover:bg-indigo-800">
                    <span class="sr-only">Remove</span>
                    <svg class="w-3 h-3" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6M6 6l12 12"/>
                    </svg>
                </button>
            </span>
            <button type="button" @click="clearFilters"
                    class="text-xs text-gray-500 dark:text-gray-400 hover:underline">Clear all</button>
        </div>
        <span class="flex-1"></span>
        <slot name="toolbar"></slot>
    </div>
    <ErrorSummary v-if="listError" :status="listError" class="mb-4" />
    <SchemaGrid :items="rows" :schema="rowSchema" :selected-columns="columns" :header-titles="headerTitles"
                :is-selected="selectable ? isSelected : undefined"
                @row-selected="$emit('rowSelected', $event)" @header-selected="onHeaderSelected">
        <template #header="{ column, label }">
            <div class="flex items-center justify-between gap-1"
                 :class="canFilter(column) ? 'cursor-pointer hover:text-gray-900 dark:hover:text-gray-50' : ''">
                <span class="mr-1 select-none">{{ label }}</span>
                <svg v-if="filterCount(column)" class="size-3.5 text-indigo-600 dark:text-indigo-400"
                     viewBox="0 0 24 24" fill="none" aria-hidden="true">
                    <path d="M3 4a1 1 0 0 1 1-1h16a1 1 0 0 1 1 1v2.586a1 1 0 0 1-.293.707l-6.414 6.414a1 1 0 0
                        0-.293.707V17l-4 4v-6.586a1 1 0 0 0-.293-.707L3.293 7.293A1 1 0 0 1 3 6.586V4z"
                        stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
                <svg v-else-if="sortOf(column) === 'ASC'" class="size-3.5 text-indigo-600 dark:text-indigo-400"
                     viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                    <path d="M8.998 4.71L6.354 7.354a.5.5 0 1 1-.708-.707L9.115 3.18A.499.499 0 0 1 9.498 3H9.5a.5.5
                        0 0 1 .354.147l.01.01l3.49 3.49a.5.5 0 1 1-.707.707l-2.65-2.649V16.5a.5.5 0 0 1-1 0V4.71z"/>
                </svg>
                <svg v-else-if="sortOf(column) === 'DESC'" class="size-3.5 text-indigo-600 dark:text-indigo-400"
                     viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                    <path d="M10.002 15.29l2.645-2.644a.5.5 0 0 1 .707.707L9.886 16.82a.5.5 0 0 1-.384.179h-.001a.5.5
                        0 0 1-.354-.147l-.01-.01l-3.49-3.49a.5.5 0 1 1 .707-.707l2.648 2.649V3.5a.5.5 0 0 1 1 0v11.79z"/>
                </svg>
                <svg v-else-if="canFilter(column)" class="size-3.5 text-gray-400 dark:text-gray-500"
                     viewBox="0 0 1024 1024" fill="currentColor" aria-hidden="true">
                    <path d="M505.5 658.7c3.2 4.4 9.7 4.4 12.9 0l178-246c3.8-5.3 0-12.7-6.5-12.7H643c-10.2 0-19.9
                        4.9-25.9 13.2L512 558.6L406.8 413.2c-6-8.3-15.6-13.2-25.9-13.2H334c-6.5 0-10.3 7.4-6.5 12.7l178 246z"/>
                </svg>
            </div>
        </template>
    </SchemaGrid>
    <div v-if="!rows.length && !listError" class="py-8 text-center text-xs text-gray-500 dark:text-gray-400">
        No results
    </div>
    <!-- Both position themselves against the viewport, so they're teleported out: a grid nested
         in a ModalDialog sits inside a panel whose resting transform (translate-y-0 scale-100)
         makes it the containing block for fixed descendants, which would anchor the filter popup
         to the panel and size Query Preferences to it. At body level they behave the same
         wherever the grid is. -->
    <Teleport to="body">
        <FilterColumn v-if="showFilters" :definitions="conventions" :column="showFilters.column"
                      :top-left="showFilters.topLeft" @done="showFilters = null" @save="onFilterSave" />
        <QueryPrefs v-if="showPrefs" :columns="allColumns" :prefs="prefs"
                    @done="showPrefs = false" @save="onPrefsSave" />
    </Teleport>
</div>`
const SchemaResults = {
    name: 'SchemaResults',
    components: { SchemaGrid },
    template,
    props: {

        schema: { type: Object, required: true },

        query: { type: Object, default: null },

        take: { type: Number, default: 25 },

        prefsKey: { type: String, default: null },

        columnOrder: { type: Array, default: null },

        selectable: { type: Boolean, default: false },
    },
    emits: ['update:query', 'rowSelected', 'loaded'],
    setup(props, { emit, expose }) {
        const { filterDefinitions } = useMetadata()
        const rows = ref([])
        const total = ref(0)
        const listError = ref(null)
        const showFilters = ref(null) // { column, topLeft }
        const showPrefs = ref(false)
        // Used only when the host doesn't bind :query - a picker has nowhere to put it
        const localQuery = ref({ ...EMPTY_QUERY })
        const q = computed(() => props.query ?? localQuery.value)
        const filters = computed(() => q.value.filters ?? {})
        const orderBy = computed(() => String(q.value.orderBy ?? ''))
        const skip = computed(() => Math.max(0, parseInt(q.value.skip) || 0))

        function setQuery(next) {
            const value = { filters: {}, orderBy: '', skip: 0, ...next }
            localQuery.value = value
            emit('update:query', value)
        }
        // an IQueryDb<From,Into> returns the Into shape, so that's what the grid describes
        const modelSchema = computed(() => rowSchemaOf(props.schema))
        const modelProps = computed(() => modelSchema.value?.properties || {})
        // --- filtering ------------------------------------------------------

        const metaProp = name => {
            const prop = modelProps.value[name]
            return prop ? { name, type: prop.type, isEnum: false } : null
        }
        // AutoQuery's implicit query conventions, e.g. { name:'>', value:'%>' }
        const conventions = filterDefinitions
        // Which column + operator produced a query param, e.g. Cost>=100 -> Cost >= 100
        function describe(key) {
            for (const name of Object.keys(modelProps.value)) {
                const conv = conventions.value.find(c => c.value.replace('%', name) === key)
                if (conv) return { column: name, op: conv.name }
            }
            return { column: key, op: '=' }
        }
        // shown above the grid so an active filter is never hidden in a popup
        const activeFilters = computed(() => Object.entries(filters.value).map(([key, value]) => {
            const { column, op } = describe(key)
            return { key, value, op, label: modelProps.value[column]?.title || column }
        }))
        const filterCount = name => Object.keys(filters.value)
            .filter(k => describe(k).column === name).length
        const sortOf = name => {
            const entry = orderBy.value.split(',').find(x => x.replace(/^-/, '') === name)
            return entry ? (entry.startsWith('-') ? 'DESC' : 'ASC') : null
        }
        // AutoQuery's implicit conventions work on scalar Data Model fields
        const canFilter = name => !!metaProp(name)
            && modelProps.value[name]?.type !== 'object' && modelProps.value[name]?.type !== 'array'
        const without = key => Object.fromEntries(
            Object.entries(filters.value).filter(([k]) => k !== key))
        const clearFilter = key => setQuery({ filters: without(key), orderBy: orderBy.value })
        const clearFilters = () => setQuery({ orderBy: orderBy.value })
        // --- column filter popup -------------------------------------------
        // the query, read back as the ColumnSettings <FilterColumn> edits
        function columnSettings(name) {
            const found = []
            for (const [key, value] of Object.entries(filters.value)) {
                const conv = conventions.value.find(c => c.value.replace('%', name) === key)
                if (conv) found.push({ key: conv.value, name: conv.name, value: String(value) })
            }
            return { filters: found, sort: sortOf(name) ?? undefined }
        }
        function onHeaderSelected(name, e) {
            if (!canFilter(name)) return
            const meta = metaProp(name)
            const tableRect = e.target?.closest('TABLE')?.getBoundingClientRect()
            if (!meta || !tableRect) return
            // anchor the popup under the header cell that was clicked (same math as AutoQueryGrid)
            const width = 318
            showFilters.value = {
                column: { name: meta.name, type: meta.type, meta, settings: columnSettings(meta.name) },
                topLeft: {
                    x: Math.max(Math.floor(e.clientX + width / 2), tableRect.x + width + 10),
                    y: tableRect.y + 45,
                },
            }
        }
        function onFilterSave(settings) {
            const name = showFilters.value?.column?.name
            showFilters.value = null
            if (!name) return
            // drop every param this column could have set, then re-add what survived
            const next = { ...filters.value }
            for (const conv of conventions.value) delete next[conv.value.replace('%', name)]
            for (const f of settings.filters) next[f.key.replace('%', name)] = f.value
            const sorts = orderBy.value.split(',').filter(x => x && x.replace(/^-/, '') !== name)
            if (settings.sort) sorts.push((settings.sort === 'DESC' ? '-' : '') + name)
            setQuery({ filters: next, orderBy: sorts.join(',') })
        }
        // --- columns & preferences -----------------------------------------
        const prefsKey = computed(() => props.prefsKey || `auto:prefs:${props.schema?.name}`)
        const allNames = computed(() => props.columnOrder ?? Object.keys(modelProps.value))
        const allColumns = computed(() => allNames.value.map(name => ({ name })))
        const prefs = ref(loadPrefs())
        function loadPrefs() {
            try {
                return JSON.parse(localStorage.getItem(prefsKey.value)) || {}
            } catch {
                return {}
            }
        }
        function onPrefsSave(value) {
            prefs.value = value
            localStorage.setItem(prefsKey.value, JSON.stringify(value))
            showPrefs.value = false
            loadRows()
        }
        const take = computed(() => prefs.value.take || props.take)
        const columns = computed(() => {
            const selected = prefs.value.selectedColumns
            return selected?.length ? allNames.value.filter(c => selected.includes(c)) : allNames.value
        })
        const headerTitles = computed(() => Object.fromEntries(
            Object.entries(modelProps.value).map(([name, p]) => [name, p.title || name])))
        // --- paging ---------------------------------------------------------
        const canPrev = computed(() => skip.value > 0)
        const canNext = computed(() => skip.value + take.value < total.value)
        const lastPageSkip = computed(() => Math.max(0, Math.floor((total.value - 1) / take.value) * take.value))
        const skipTo = value => setQuery({ filters: filters.value, orderBy: orderBy.value, skip: Math.max(0, value) })
        const pagingClass = enabled => ['px-0.5', enabled
            ? 'text-gray-700 dark:text-gray-300 hover:text-indigo-600 dark:hover:text-indigo-400'
            : 'text-gray-400 dark:text-gray-500 cursor-not-allowed']
        // --- data ------------------------------------------------------------
        async function loadRows() {
            if (!props.schema?.query) return
            listError.value = null
            try {
                const qs = new URLSearchParams(filters.value)
                if (orderBy.value) qs.set('orderBy', orderBy.value)
                qs.set('skip', skip.value)
                qs.set('take', take.value)
                // AutoQuery only counts the full result set when asked, and paging needs it
                if (!qs.has('include')) qs.set('include', 'total')
                const url = resolvePath(props.schema.query.$id, filters.value) + '?' + qs
                const res = await fetch(url, { headers: { Accept: 'application/json' } })
                const json = res.status !== 204 ? await res.json() : {}
                if (!res.ok) throw mapGet(json, 'responseStatus') || { message: `${res.status} ${res.statusText}` }
                rows.value = mapGet(json, 'results') || []
                // APIs that don't compute a total still report the rows they returned
                total.value = mapGet(json, 'total') ?? (skip.value + rows.value.length)
            } catch (e) {
                listError.value = e
                rows.value = []
                total.value = 0
            }
            emit('loaded', { results: rows.value, total: total.value })
        }
        // only refetch when what's being queried changes
        watch(() => JSON.stringify([filters.value, orderBy.value, skip.value]), loadRows)
        watch(prefsKey, () => { prefs.value = loadPrefs() })
        onMounted(loadRows)
        // so the host can refresh after it creates, updates or deletes a row
        expose({ reload: loadRows })
        return {
            rowSchema: modelSchema,
            rows, total, skip, take, listError, showFilters, showPrefs, prefs,
            columns, allColumns, headerTitles, conventions, activeFilters,
            canFilter, filterCount, sortOf,
            from: computed(() => total.value ? skip.value + 1 : 0),
            to: computed(() => Math.min(skip.value + rows.value.length, total.value)),
            isSelected: () => false,
            onHeaderSelected, onFilterSave, onPrefsSave,
            clearFilter, clearFilters,
            canPrev, canNext, lastPageSkip, skipTo, pagingClass,
        }
    },
}
export default SchemaResults
