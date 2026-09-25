import { computed, inject } from "vue"
import { leftPart } from "@servicestack/client"
/** Compact year + month selector bound to the `year` and `month` route params */
const MonthPicker = {
    template:`<div v-if="months.length" class="flex flex-wrap items-center gap-x-3 gap-y-2">
        <div v-if="years.length > 1" class="flex items-center gap-1">
            <template v-for="year in years">
                <span v-if="year === selectedYear" class="rounded-md bg-gray-100 px-2 py-1 text-xs font-semibold text-gray-900">{{ year }}</span>
                <a v-else v-href="{ year }" class="rounded-md px-2 py-1 text-xs font-medium text-gray-500 hover:bg-gray-100 hover:text-gray-900">{{ year }}</a>
            </template>
        </div>
        <span v-else class="text-xs font-semibold text-gray-900">{{ selectedYear }}</span>
        <span class="h-4 w-px bg-gray-200" aria-hidden="true"></span>
        <div class="flex flex-wrap items-center gap-1">
            <template v-for="month in yearMonths">
                <span v-if="month === selectedMonth" class="rounded-md bg-indigo-600 px-2.5 py-1 text-xs font-semibold text-white shadow-sm">{{ monthName(month) }}</span>
                <a v-else v-href="{ month }" class="rounded-md px-2.5 py-1 text-xs font-medium text-gray-600 hover:bg-gray-100 hover:text-gray-900">{{ monthName(month) }}</a>
            </template>
        </div>
    </div>`,
    props: { months: { type: Array, default: () => [] } },
    setup(props) {
        const routes = inject('routes')
        const now = new Date()
        const years = computed(() => Array.from(new Set(props.months.map(x => leftPart(x, '-')))).toReversed())
        const selectedYear = computed(() => routes.year || now.getFullYear().toString())
        const selectedMonth = computed(() => routes.month || `${now.getFullYear()}-${(now.getMonth() + 1).toString().padStart(2,'0')}`)
        const yearMonths = computed(() => props.months.filter(x => x.startsWith(selectedYear.value)))
        const monthName = month => new Date(month + '-01T00:00:00').toLocaleString('default', { month: 'short' })
        return { years, selectedYear, selectedMonth, yearMonths, monthName }
    }
}
export default MonthPicker
