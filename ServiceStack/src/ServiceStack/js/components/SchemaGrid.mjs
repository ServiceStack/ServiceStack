
import { computed } from "vue"
import { useFormatters, useConfig } from "@servicestack/vue"
import { mapGet } from "@servicestack/client"
import { useSchemas, rowSchema } from "./useSchemas.mjs"
const { formatValue, Formats } = useFormatters()
const SchemaGrid = {
    name: 'SchemaGrid',
    template: `
    <div class="overflow-hidden rounded-lg border border-gray-200 dark:border-gray-700">
        <!-- the scroll container is nested: a scroll container can't clip its own scrollbar, so
             the horizontal bar's square ends would sit outside the rounded corners. Putting the
             radius on an ancestor with overflow-hidden clips the bar along with the content. -->
        <div class="overflow-x-auto">
        <table class="w-full text-sm">
            <thead class="bg-gray-50 dark:bg-gray-800">
                <tr>
                    <td v-for="column in visibleColumns" :key="column"
                        class="px-3 py-2 text-left font-semibold whitespace-nowrap text-gray-500 dark:text-gray-400"
                        @click="$emit('headerSelected', column, $event)">
                        <slot name="header" :column="column" :label="labelOf(column)">
                            <span class="select-none">{{ labelOf(column) }}</span>
                        </slot>
                    </td>
                </tr>
            </thead>
            <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
                <tr v-for="(item, i) in items" :key="i" :class="rowClass(i)"
                    @click="$emit('rowSelected', item, $event)">
                    <td v-for="(cell, c) in cells[i]" :key="c"
                        class="px-3 py-3 text-sm text-gray-500 dark:text-gray-400">
                        <!-- capped so one long value can't push every other column off screen;
                             what's cut off is on the title, which also carries the full JSON of
                             a value the cell only summarises -->
                        <div class="max-w-[500px] flex items-center" :title="cell.title">
                            <Icon v-if="cell.icon" class="w-5 h-5 mr-1 shrink-0" :image="cell.icon" />
                            <!-- min-w-0 lets a flex child shrink below its content, which is what
                                 makes truncate take effect at all -->
                            <span v-if="cell.text != null" class="min-w-0 truncate">{{ cell.text }}</span>
                            <span v-else class="min-w-0 truncate" v-html="cell.html"></span>
                        </div>
                    </td>
                </tr>
            </tbody>
        </table>
        </div>
        <slot name="empty"></slot>
    </div>`,
    props: {
        items: { type: Array, default: () => [] },

        schema: { type: Object, default: null },
        selectedColumns: { type: Array, default: null },
        headerTitles: { type: Object, default: null },

        isSelected: { type: Function, default: null },
    },
    emits: ['rowSelected', 'headerSelected'],
    setup(props) {
        const schemas = useSchemas()
        const { config } = useConfig()
        const properties = computed(() => props.schema?.properties ?? {})
        const propOf = column => properties.value[column]
        // `hidden` is how the app says "don't put this in a grid" - Booking.Notes uses it
        const visibleColumns = computed(() =>
            (props.selectedColumns ?? Object.keys(properties.value))
                .filter(x => propOf(x)?.ui?.format?.method !== 'hidden'))

        function refProperty(column) {
            const prop = propOf(column)
            const ref = prop?.ui?.ref
            if (!ref) return null
            if (prop.type === 'object') return { name: column, ref }
            const [name] = Object.entries(properties.value)
                .find(([, p]) => p.type === 'object' && p.ui?.ref?.model === ref.model) ?? []
            return name ? { name, ref } : null
        }

        function labelField(ref) {
            if (ref.refLabel) return ref.refLabel
            const refProps = rowSchema(schemas.model(ref.model))?.properties ?? {}
            const [name] = Object.entries(refProps)
                .find(([n, p]) => p.type === 'string' && n !== ref.refId) ?? []
            return name ?? null
        }

        function formatOf(prop) {
            if (prop?.ui?.format) return prop.ui.format
            if (prop?.format === 'date-time' || prop?.format === 'date') return Formats.date
            return null
        }

        function refIcon(column) {
            const ref = propOf(column)?.ui?.ref
            return ref ? (ref.icon ?? config.value.tableIcon) : null
        }
        function refLabel(item, column) {
            const found = refProperty(column)
            if (!found) return null
            const row = mapGet(item, found.name)
            if (!row || typeof row !== 'object') return null
            const field = labelField(found.ref)
            return field ? (mapGet(row, field) ?? null) : null
        }

        function isObjectList(prop, value) {
            return Array.isArray(value)
                && (prop?.items?.type === 'object' || value.some(x => x && typeof x === 'object'))
        }
        const indented = value => value == null ? null
            : typeof value === 'object' ? JSON.stringify(value, null, 2) : String(value)

        function cellOf(item, column) {
            const prop = propOf(column)
            const value = mapGet(item, column)
            // a [Ref] resolves to the referenced row's label, not its raw id
            const label = refLabel(item, column)
            if (label != null) return { text: label, icon: refIcon(column), title: String(label) }
            if (isObjectList(prop, value)) {
                return {
                    text: `${value.length} item${value.length === 1 ? '' : 's'}`,
                    title: indented(value),
                }
            }
            try {
                // a format's options can be an expression over the row, e.g. Booking.Cost's
                // `{ currency:modelValue.notes||'GBP' }` - a bad value must blank the cell,
                // not take the row down with it
                return { html: formatValue(value, formatOf(prop), { modelValue: item }), title: indented(value) }
            } catch {
                return { html: '', title: null }
            }
        }
        return {
            visibleColumns,
            labelOf: column => props.headerTitles?.[column] ?? propOf(column)?.title ?? column,
            // built per row so each cell is resolved once, not once per binding that reads it
            cells: computed(() => props.items.map(item =>
                visibleColumns.value.map(column => cellOf(item, column)))),
            rowClass: i => (props.isSelected ? 'cursor-pointer hover:bg-yellow-50 dark:hover:bg-blue-900 ' : '')
                + (i % 2 === 0 ? 'bg-white dark:bg-black' : 'bg-gray-50 dark:bg-gray-800'),
        }
    },
}
export default SchemaGrid
