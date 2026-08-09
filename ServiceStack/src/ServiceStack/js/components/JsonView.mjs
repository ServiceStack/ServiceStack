
import { computed } from "vue"
import { useFormatters } from "@servicestack/vue"
import { humanify, uniqueKeys } from "@servicestack/client"
const { formatValue, Formats } = useFormatters()
const isScalar = v => v == null || typeof v !== 'object'
const isEmail = v => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v)
const isUrl = v => /^https?:\/\/\S+$/i.test(v) || /^\/[^\s"']*$/.test(v)
const isImage = v => /\.(png|jpe?g|gif|svg|webp|avif)(\?|$)/i.test(v)
// the shapes .NET serializes dates and timestamps as
const isDateish = v => /^\d{4}-\d{2}-\d{2}([T ]\d{2}:\d{2}|$)/.test(v)

const FOLD_DEPTH = 3
const template = `
<span v-if="value == null" class="text-gray-400 dark:text-gray-600 italic">null</span>
<span v-else-if="typeof value === 'boolean'"
      :class="['inline-flex items-center gap-1 rounded px-1.5 py-0.5 text-xs font-medium',
               value ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300'
                     : 'bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400']">
    <svg class="w-3 h-3" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24">
        <path v-if="value" stroke-linecap="round" stroke-linejoin="round" d="m4.5 12.75 6 6 9-13.5"/>
        <path v-else stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6M6 6l12 12"/>
    </svg>
    {{ value }}
</span>
<span v-else-if="typeof value === 'number'" class="tabular-nums">{{ scalar }}</span>
<time v-else-if="date" :datetime="value" :title="value">{{ scalar }}</time>
<img v-else-if="image" :src="value" :alt="value" loading="lazy"
     class="max-h-16 rounded border border-gray-200 dark:border-gray-800">
<a v-else-if="link" :href="link" :target="link.startsWith('mailto:') ? null : '_blank'" rel="noopener"
   class="text-indigo-600 dark:text-indigo-400 hover:underline break-all">{{ value }}</a>
<span v-else-if="scalarValue" class="whitespace-pre-wrap break-normal min-w-max">{{ scalar }}</span>
<!-- a list -->
<span v-else-if="isEmptyList" class="text-gray-400 dark:text-gray-600 italic">no items</span>
<ol v-else-if="scalarList" class="flex flex-wrap gap-1">
    <li v-for="(item, i) in value" :key="i"
        class="rounded bg-gray-100 dark:bg-gray-800 px-1.5 py-0.5 text-xs">
        <JsonView :value="item" :depth="depth + 1" />
    </li>
</ol>
<div v-else-if="Array.isArray(value)" class="overflow-hidden rounded-lg border border-gray-200 dark:border-gray-800">
    <div class="overflow-x-auto">
        <table class="w-max min-w-full text-left">
            <caption class="caption-top px-3 py-1.5 text-left text-xs text-gray-500 dark:text-gray-400
                            bg-gray-50 dark:bg-gray-900 border-b border-gray-200 dark:border-gray-800">
                {{ value.length }} {{ value.length === 1 ? 'row' : 'rows' }}
            </caption>
            <thead class="bg-gray-50 dark:bg-gray-900">
                <tr>
                    <th v-for="k in columns" :key="k" scope="col"
                        class="px-3 py-2 font-semibold whitespace-nowrap text-xs uppercase tracking-wide
                               text-gray-500 dark:text-gray-400">{{ label(k) }}</th>
                </tr>
            </thead>
            <tbody class="divide-y divide-gray-200 dark:divide-gray-800">
                <tr v-for="(row, i) in value" :key="i"
                    :class="i % 2 ? 'bg-gray-50/60 dark:bg-gray-900/40' : ''">
                    <td v-for="k in columns" :key="k" class="px-3 py-2 align-top">
                        <JsonView :value="row?.[k]" :depth="depth + 1" />
                    </td>
                </tr>
            </tbody>
        </table>
    </div>
</div>
<!-- an object -->
<span v-else-if="isEmptyObject" class="text-gray-400 dark:text-gray-600 italic">no fields</span>
<details v-else-if="folded" class="group">
    <summary class="cursor-pointer text-xs text-gray-500 dark:text-gray-400 hover:text-indigo-600
                    dark:hover:text-indigo-400 select-none">
        {{ entries.length }} {{ entries.length === 1 ? 'field' : 'fields' }}
    </summary>
    <dl class="mt-1 grid grid-cols-[auto_auto] gap-x-3 gap-y-1 border-l-2 border-gray-200 dark:border-gray-800 pl-3 min-w-max">
        <template v-for="e in entries" :key="e.key">
            <dt class="text-xs text-gray-500 dark:text-gray-400 whitespace-nowrap pt-0.5">{{ e.label }}</dt>
            <dd class="min-w-max"><JsonView :value="e.value" :depth="depth + 1" /></dd>
        </template>
    </dl>
</details>
<dl v-else :class="['grid grid-cols-[auto_auto] gap-x-3 min-w-max', depth === 0 ? 'gap-y-2' : 'gap-y-1',
                    depth > 0 ? 'border-l-2 border-gray-200 dark:border-gray-800 pl-3' : '']">
    <template v-for="e in entries" :key="e.key">
        <dt :class="['whitespace-nowrap text-gray-500 dark:text-gray-400',
                     depth === 0 ? 'font-medium pt-0.5' : 'text-xs pt-0.5']">{{ e.label }}</dt>
        <dd class="min-w-max"><JsonView :value="e.value" :depth="depth + 1" /></dd>
    </template>
</dl>`
const JsonView = {
    name: 'JsonView',
    template,
    props: {
        value: { default: null },
        depth: { type: Number, default: 0 },
    },
    setup(props) {
        const str = computed(() => typeof props.value === 'string' ? props.value : null)
        const list = computed(() => Array.isArray(props.value) ? props.value : null)
        const entries = computed(() => Object.entries(props.value ?? {})
            .map(([key, value]) => ({ key, label: humanify(key), value })))
        return {
            entries,
            label: humanify,
            scalarValue: computed(() => isScalar(props.value)),
            date: computed(() => str.value != null && isDateish(str.value)),
            image: computed(() => str.value != null && isUrl(str.value) && isImage(str.value)),
            link: computed(() => str.value == null ? null
                : isEmail(str.value) ? `mailto:${str.value}`
                : isUrl(str.value) && !isImage(str.value) ? str.value
                : null),
            // dates and numbers read the same here as they do in a data grid. formatValue()
            // leaves an ISO string alone unless told it's a date, and a timestamp that carries
            // a real time of day shouldn't lose it to a date-only format
            scalar: computed(() => {
                if (!(str.value != null && isDateish(str.value))) return formatValue(props.value)
                const day = formatValue(str.value, Formats.date)
                if (!/[T ]\d{2}:\d{2}/.test(str.value) || /[T ]00:00(:00(\.0+)?)?Z?$/.test(str.value))
                    return day
                const time = new Date(str.value).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })
                return `${day}, ${time}`
            }),
            isEmptyList: computed(() => list.value?.length === 0),
            scalarList: computed(() => list.value?.length > 0 && list.value.every(isScalar)),
            // the union of every row's keys, so a row missing one still lines up
            columns: computed(() => list.value ? uniqueKeys(list.value) : []),
            isEmptyObject: computed(() => !isScalar(props.value) && !list.value && entries.value.length === 0),
            folded: computed(() => props.depth >= FOLD_DEPTH && entries.value.length > 0),
        }
    },
}

export function unwrapResponse(json) {
    if (!json || typeof json !== 'object' || Array.isArray(json)) return { data: json, key: null }
    const keys = Object.keys(json)
    const key = keys.find(k => k.toLowerCase() === 'results') ?? keys.find(k => k.toLowerCase() === 'result')
    if (key == null || json[key] == null) return { data: json, key: null }
    return { data: json[key], key, envelope: json }
}
export default JsonView
