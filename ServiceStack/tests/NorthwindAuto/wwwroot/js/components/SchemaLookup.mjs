/**
 * SchemaLookup - picks a referenced row for a [Ref] property, the way LookupInput +
 * ModalLookup do, but driven entirely by JSON Schema instead of App metadata.
 *
 *     <SchemaLookup id="CouponId" :prop="prop" :model="form.data" :status="error" />
 *
 * The property's `ui.ref` says which Model is referenced ({ model, refId, refLabel, icon }).
 * This fetches that Model's own /auto/{Model}.json on demand, uses its Query API to resolve
 * the current value's label, and opens a picker that is a full SchemaResults over the same
 * schema - so picking a row uses the same paging, per-column sort/filter and query preferences
 * as the page's own grid, rather than a reduced search box.
 *
 * Dependencies
 *   vue                   3.x
 *   vue-router            no
 *   @servicestack/vue     ModalDialog, Icon, useConfig() for the fallback icon
 *   @servicestack/client  mapGet()
 *   ./useSchemas.mjs      fetches and caches the referenced Model's schema
 *   ./SchemaResults.mjs   the picker's grid, with the same paging, sorting, column filters
 *                         and query preferences as the page's own results
 *
 * Edits `model[id]` in place and emits `update:modelValue` with it, matching LookupInput.
 */

import { ref, computed, onMounted, watch } from "vue"
import { useConfig } from "@servicestack/vue"
import { mapGet } from "@servicestack/client"
import { useSchemas, rowSchema } from "./useSchemas.mjs"
import SchemaResults from "./SchemaResults.mjs"

const SchemaLookup = {
    name: 'SchemaLookup',
    components: { SchemaResults },
    template: `
    <div class="lookup-field">
        <div class="flex justify-between">
            <label :for="id" class="block text-sm font-medium text-gray-700 dark:text-gray-300">{{ label }}</label>
            <div v-if="value != null && value !== ''" class="flex items-center">
                <span class="text-sm text-gray-500 dark:text-gray-400 pr-1">{{ value }}</span>
                <button type="button" @click="clear" title="clear"
                        class="mr-1 rounded-md text-gray-400 hover:text-gray-500 dark:hover:text-gray-400">
                    <span class="sr-only">Clear</span>
                    <svg class="h-4 w-4" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12"/>
                    </svg>
                </button>
            </div>
        </div>

        <div class="mt-1 relative">
            <button type="button" @click="open = true"
                    class="lookup flex relative w-full bg-white dark:bg-black border border-gray-300 dark:border-gray-700
                           rounded-md shadow-sm pl-3 pr-10 py-2 text-left focus:outline-none focus:ring-1
                           focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm">
                <span class="w-full inline-flex truncate">
                    <span class="text-blue-700 dark:text-blue-300 flex cursor-pointer">
                        <Icon v-if="refIcon" class="mr-1 w-5 h-5" :image="refIcon" />
                        <span>{{ display }}</span>
                    </span>
                </span>
                <span class="absolute inset-y-0 right-0 flex items-center pr-2 pointer-events-none">
                    <svg class="h-5 w-5 text-gray-400" viewBox="0 0 20 20" fill="currentColor">
                        <path fill-rule="evenodd" d="M10 3a1 1 0 01.707.293l3 3a1 1 0 01-1.414 1.414L10 5.414
                            7.707 7.707a1 1 0 01-1.414-1.414l3-3A1 1 0 0110 3zm-3.707 9.293a1 1 0 011.414
                            0L10 14.586l2.293-2.293a1 1 0 011.414 1.414l-3 3a1 1 0 01-1.414 0l-3-3a1 1 0 010-1.414z"
                            clip-rule="evenodd"/>
                    </svg>
                </span>
            </button>
        </div>

        <p v-if="error" class="mt-2 text-sm text-red-500">{{ error }}</p>
        <p v-else-if="help" class="mt-2 text-sm text-gray-500">{{ help }}</p>

        <!-- Teleported out because this sits inside the edit dialog, whose form body scrolls:
             left in place the picker would be sized and clipped by that box, giving scrollbars
             inside scrollbars. At the end of <body> it's a plain top-level modal, full viewport,
             and later in DOM order so it paints over the dialog that opened it. -->
        <Teleport to="body">
            <ModalDialog v-if="open" :id="id + '-lookup'" size-class="sm:max-w-6xl sm:w-full" @done="open = false">
                <div class="px-6 py-4 border-b border-gray-200 dark:border-gray-700">
                    <h3 class="text-base font-semibold">Select {{ refInfo?.model }}</h3>
                </div>

                <div class="px-6 py-4 max-h-[70vh] overflow-y-auto">
                    <SchemaResults v-if="refSchema" :schema="refSchema" :prefs-key="prefsKey"
                                   :column-order="pickerColumns" selectable @row-selected="pick" />
                    <p v-else class="py-8 text-center text-xs text-gray-500 dark:text-gray-400">Loading…</p>
                </div>
            </ModalDialog>
        </Teleport>
    </div>`,
    props: {
        id: { type: String, required: true },
        /** the schema property, whose ui.ref names the referenced Model */
        prop: { type: Object, required: true },
        /** the object being edited - mutated in place, as LookupInput does */
        model: { type: Object, required: true },
        status: { type: Object, default: null },
        label: { type: String, default: null },
        help: { type: String, default: null },
    },
    emits: ['update:modelValue'],
    setup(props, { emit }) {
        const schemas = useSchemas()
        const { config } = useConfig()
        const open = ref(false)
        const label = ref('')

        const refInfo = computed(() => props.prop?.ui?.ref ?? null)
        // the referenced Model's [Icon], else the generic table icon, as LookupInput does
        const refIcon = computed(() => refInfo.value ? (refInfo.value.icon ?? config.value.tableIcon) : null)
        const refSchema = computed(() => refInfo.value ? schemas.model(refInfo.value.model) : null)
        const value = computed(() => mapGet(props.model, props.id))

        // what the button shows: the referenced row's label once resolved, else the raw id
        const display = computed(() => label.value || (value.value ?? '') || `Select ${refInfo.value?.model ?? ''}`)

        const error = computed(() => {
            const errors = props.status?.errors ?? []
            const match = errors.find(x => String(x.fieldName ?? '').toLowerCase() === props.id.toLowerCase())
            return match ? match.message : null
        })

        // the picker shows the id and the label first, then whatever else the Model has
        const pickerColumns = computed(() => {
            const all = Object.keys(rowSchema(refSchema.value)?.properties ?? {})
            const lead = [refInfo.value?.refId, refInfo.value?.refLabel].filter(x => x && all.includes(x))
            return [...lead, ...all.filter(x => !lead.includes(x))]
        })

        /** resolve the current value's label by looking the referenced row up by its refId */
        async function resolveLabel() {
            const ref = refInfo.value
            if (!ref?.refLabel || value.value == null || value.value === '') { label.value = ''; return }

            // the row may already be on the model as a sibling complex property
            const sibling = Object.values(props.model).find(x =>
                x && typeof x === 'object' && !Array.isArray(x) && mapGet(x, ref.refId) == value.value)
            if (sibling && mapGet(sibling, ref.refLabel)) {
                label.value = String(mapGet(sibling, ref.refLabel))
                return
            }

            await schemas.loadModel(ref.model)
            const api = refSchema.value?.query
            if (!api) return
            try {
                const res = await fetch(`${api.$id}?${new URLSearchParams({ [ref.refId]: value.value, take: 1 })}`,
                    { headers: { Accept: 'application/json' } })
                const row = res.ok ? (mapGet(await res.json(), 'results') ?? [])[0] : null
                if (row) label.value = String(mapGet(row, ref.refLabel) ?? value.value)
            } catch { /* leave the raw id showing */ }
        }

        function pick(row) {
            const ref = refInfo.value
            props.model[props.id] = mapGet(row, ref.refId)
            label.value = String(mapGet(row, ref.refLabel) ?? '')
            open.value = false
            emit('update:modelValue', props.model)
        }

        function clear() {
            props.model[props.id] = null
            label.value = ''
            emit('update:modelValue', props.model)
        }

        watch(open, isOpen => { if (isOpen) schemas.loadModel(refInfo.value?.model) })
        onMounted(resolveLabel)

        return {
            open, refInfo, refIcon, refSchema, value, display, error, pickerColumns,
            // a picker is a different view of the Model to the page's grid, so it keeps its
            // own visible columns rather than overwriting the ones chosen there
            prefsKey: computed(() => `auto:prefs:${refInfo.value?.model}:lookup`),
            pick, clear,
        }
    },
}

export default SchemaLookup
