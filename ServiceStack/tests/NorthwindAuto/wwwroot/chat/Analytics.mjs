import { ref, onMounted, watch, nextTick, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useFormatters } from "@servicestack/vue"
import { leftPart } from '@servicestack/client'
import { Chart, registerables } from "chart.js"
import { useThreadStore } from './threadStore.mjs'
import { formatCost } from './utils.mjs'
Chart.register(...registerables)

const { humanifyNumber, humanifyMs } = useFormatters()

export const colors = [
    { background: 'rgba(54, 162, 235, 0.2)',  border: 'rgb(54, 162, 235)' }, //blue
    { background: 'rgba(255, 99, 132, 0.2)',  border: 'rgb(255, 99, 132)' },
    { background: 'rgba(153, 102, 255, 0.2)', border: 'rgb(153, 102, 255)' },
    { background: 'rgba(54, 162, 235, 0.2)',  border: 'rgb(54, 162, 235)' },
    { background: 'rgba(255, 159, 64, 0.2)',  border: 'rgb(255, 159, 64)' },
    { background: 'rgba(67, 56, 202, 0.2)',   border: 'rgb(67, 56, 202)' },
    { background: 'rgba(255, 99, 132, 0.2)',  border: 'rgb(255, 99, 132)' },
    { background: 'rgba(14, 116, 144, 0.2)',  border: 'rgb(14, 116, 144)' },
    { background: 'rgba(162, 28, 175, 0.2)',  border: 'rgb(162, 28, 175)' },
    { background: 'rgba(201, 203, 207, 0.2)', border: 'rgb(201, 203, 207)' },
]

const MonthSelector = {
    template:`
    <div class="flex flex-col sm:flex-row gap-2 sm:gap-4 items-stretch sm:items-center w-full sm:w-auto">
        <!-- Months Row -->
        <div class="flex gap-1 sm:gap-2 flex-wrap justify-center overflow-x-auto">
            <template v-for="month in availableMonthsForYear" :key="month">
                <span v-if="selectedMonth === month"
                    class="text-xs leading-5 font-semibold bg-indigo-600 text-white rounded-full py-1 px-2 sm:px-3 flex items-center space-x-2 whitespace-nowrap">
                    <span class="hidden sm:inline">{{ new Date(selectedYear + '-' + month.toString().padStart(2,'0') + '-01').toLocaleString('default', { month: 'long' }) }}</span>
                    <span class="sm:hidden">{{ new Date(selectedYear + '-' + month.toString().padStart(2,'0') + '-01').toLocaleString('default', { month: 'short' }) }}</span>
                </span>
                <button v-else type="button"
                    class="text-xs leading-5 font-semibold bg-slate-400/10 rounded-full py-1 px-2 sm:px-3 flex items-center space-x-2 hover:bg-slate-400/20 dark:highlight-white/5 whitespace-nowrap"
                    @click="updateSelection(selectedYear, month)">
                    {{ new Date(selectedYear + '-' + month.toString().padStart(2,'0') + '-01').toLocaleString('default', { month: 'short' }) }}
                </button>
            </template>
        </div>

        <!-- Year Dropdown -->
        <select :value="selectedYear" @change="(e) => updateSelection(parseInt(e.target.value), selectedMonth)"
            class="border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-700 dark:text-gray-300 rounded-md text-sm font-medium hover:bg-gray-50 dark:hover:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-indigo-500 flex-shrink-0">
            <option v-for="year in availableYears" :key="year" :value="year">
                {{ year }}
            </option>
        </select>
    </div>
    `,
    props: {
        dailyData: Array,
    },
    setup(props) {
        const router = useRouter()
        const route = useRoute()

        const selectedMonth = computed(() => {
            const now = new Date()
            return route.query.month !== undefined ? parseInt(route.query.month) : now.getMonth() + 1
        })

        const selectedYear = computed(() => {
            const now = new Date()
            return route.query.year !== undefined ? parseInt(route.query.year) : now.getFullYear()
        })

        const updateSelection = (year, month) => {
            router.push({
                query: {
                    ...route.query,
                    month,
                    year
                }
            })
        }

        const availableYears = computed(() => {
            // Get all years that have data
            const yearsSet = new Set()
            Object.keys(props.dailyData || {}).forEach(dateKey => {
                const year = parseInt(leftPart(dateKey, '-'))
                yearsSet.add(year)
            })
            return Array.from(yearsSet).sort((a, b) => a - b)
        })

        const availableMonthsForYear = computed(() => {
            // Get all months that have data for the selected year
            const monthsSet = new Set()
            Object.keys(props.dailyData || {}).forEach(dateKey => {
                const date = new Date(dateKey + 'T00:00:00Z')
                if (date.getFullYear() === selectedYear.value) {
                    monthsSet.add(date.getMonth() + 1)
                }
            })
            return Array.from(monthsSet).sort((a, b) => a - b)
        })

        return {
            selectedMonth,
            selectedYear,
            updateSelection,
            availableYears,
            availableMonthsForYear,
        }
    }
}

export default {
    components: {
        MonthSelector,
    },
    template: `
        <div class="flex flex-col h-full w-full">
            <!-- Header -->
            <div class="border-b border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 px-2 sm:px-4 py-3">
                <div
                :class="!$ai.isSidebarOpen ? 'pl-3' : ''"
                class="max-w-6xl mx-auto flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3">
                    <h2 class="text-lg font-semibold text-gray-900 dark:text-gray-100 flex-shrink-0">
                        <RouterLink to="/analytics">Analytics</RouterLink>
                    </h2>
                    <MonthSelector :dailyData="allDailyData" />
                </div>
            </div>

            <!-- Tabs -->
            <div class="border-b border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 px-4">
                <div class="max-w-6xl mx-auto flex gap-8">
                    <button type="button"
                        @click="activeTab = 'cost'"
                        :class="['py-3 px-1 border-b-2 font-medium text-sm transition-colors',
                                 activeTab === 'cost'
                                    ? 'border-blue-500 text-blue-600 dark:text-blue-400'
                                    : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200']">
                        Cost Analysis
                    </button>
                    <button type="button"
                        @click="activeTab = 'tokens'"
                        :class="['py-3 px-1 border-b-2 font-medium text-sm transition-colors',
                                 activeTab === 'tokens'
                                    ? 'border-blue-500 text-blue-600 dark:text-blue-400'
                                    : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200']">
                        Token Usage
                    </button>
                    <button type="button"
                        @click="activeTab = 'activity'"
                        :class="['py-3 px-1 border-b-2 font-medium text-sm transition-colors',
                                 activeTab === 'activity'
                                    ? 'border-blue-500 text-blue-600 dark:text-blue-400'
                                    : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200']">
                        Activity
                    </button>
                </div>
            </div>

            <!-- Content -->
            <div class="flex-1 overflow-auto bg-gray-50 dark:bg-gray-900" :class="activeTab === 'activity' ? 'p-0' : 'p-4'">

                <div :class="activeTab === 'activity' ? 'h-full' : 'max-w-6xl mx-auto'">
                    <!-- Stats Summary (hidden for Activity tab) -->
                    <div v-if="activeTab !== 'activity'" class="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
                        <div class="bg-white dark:bg-gray-800 rounded-lg shadow p-4">
                            <div class="text-sm font-medium text-gray-600 dark:text-gray-400">Total Cost</div>
                            <div class="text-2xl font-bold text-gray-900 dark:text-gray-100 mt-1">{{ formatCost(totalCost) }}</div>
                        </div>
                        <div class="bg-white dark:bg-gray-800 rounded-lg shadow p-4">
                            <div class="text-sm font-medium text-gray-600 dark:text-gray-400">Total Requests</div>
                            <div class="text-2xl font-bold text-gray-900 dark:text-gray-100 mt-1">{{ totalRequests }}</div>
                        </div>
                        <div class="bg-white dark:bg-gray-800 rounded-lg shadow p-4">
                            <div class="text-sm font-medium text-gray-600 dark:text-gray-400">Total Input Tokens</div>
                            <div class="text-2xl font-bold text-gray-900 dark:text-gray-100 mt-1">{{ humanifyNumber(totalInputTokens) }}</div>
                        </div>
                        <div class="bg-white dark:bg-gray-800 rounded-lg shadow p-4">
                            <div class="text-sm font-medium text-gray-600 dark:text-gray-400">Total Output Tokens</div>
                            <div class="text-2xl font-bold text-gray-900 dark:text-gray-100 mt-1">{{ humanifyNumber(totalOutputTokens) }}</div>
                        </div>
                    </div>

                    <!-- Cost Analysis Tab -->
                    <div v-if="activeTab === 'cost'" class="bg-white dark:bg-gray-800 rounded-lg shadow p-4 sm:p-6">
                        <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 mb-6">
                            <h3 class="text-base sm:text-lg font-semibold text-gray-900 dark:text-gray-100">Daily Costs</h3>
                            <h3 class="text-sm sm:text-lg font-semibold text-gray-900 dark:text-gray-100">
                                {{ new Date(selectedDay).toLocaleDateString(undefined, { year: 'numeric', month: 'long' }) }}
                            </h3>
                            <select v-model="costChartType" class="px-3 pr-6 py-2 border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-700 dark:text-gray-300 rounded-md text-sm font-medium hover:bg-gray-50 dark:hover:bg-gray-800 flex-shrink-0">
                                <option value="bar">Bar Chart</option>
                                <option value="line">Line Chart</option>
                            </select>
                        </div>

                        <div v-if="chartData.labels.length > 0" class="relative h-96">
                            <canvas ref="costChartCanvas"></canvas>
                        </div>
                        <div v-else class="flex items-center justify-center h-96 text-gray-500 dark:text-gray-400">
                            <p>No request data available</p>
                        </div>
                    </div>

                    <!-- Token Usage Tab -->
                    <div v-if="activeTab === 'tokens'" class="bg-white dark:bg-gray-800 rounded-lg shadow p-4 sm:p-6">
                        <h3 class="text-base sm:text-lg font-semibold text-gray-900 dark:text-gray-100 mb-6 flex flex-col sm:flex-row justify-between items-start sm:items-center gap-2">
                            <span>Daily Token Usage</span>
                            <span class="text-sm sm:text-base">
                                {{ new Date(selectedDay).toLocaleDateString(undefined, { year: 'numeric', month: 'long' }) }}
                            </span>
                        </h3>

                        <div v-if="tokenChartData.labels.length > 0" class="relative h-96">
                            <canvas ref="tokenChartCanvas"></canvas>
                        </div>
                        <div v-else class="flex items-center justify-center h-96 text-gray-500 dark:text-gray-400">
                            <p>No request data available</p>
                        </div>
                    </div>

                    <div v-if="allDailyData[selectedDay]?.requests && ['cost', 'tokens'].includes(activeTab)" class="mt-8 px-2 text-sm sm:text-base text-gray-700 dark:text-gray-300 font-medium flex flex-col sm:flex-row items-start sm:items-center justify-between gap-2">
                        <div>
                            {{ new Date(selectedDay).toLocaleDateString(undefined, { year: 'numeric', month: 'long', day: 'numeric' }) }}
                        </div>
                        <div class="flex flex-wrap gap-x-2 gap-y-1">
                           <span>{{ formatCost(allDailyData[selectedDay]?.cost || 0) }}</span>
                           <span>&#183;</span>
                           <span>{{ allDailyData[selectedDay]?.requests || 0 }} Requests</span>
                           <span>&#183;</span>
                           <span>{{ humanifyNumber(allDailyData[selectedDay]?.inputTokens || 0) }} -> {{ humanifyNumber(allDailyData[selectedDay]?.outputTokens || 0) }} Tokens</span>
                        </div>
                    </div>

                    <!-- Pie Charts for Selected Day -->
                    <div v-if="allDailyData[selectedDay]?.requests && activeTab === 'cost' && selectedDay" class="mt-6 grid grid-cols-1 lg:grid-cols-2 gap-6">
                        <!-- Model Pie Chart -->
                        <div class="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
                            <h3 class="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">
                                Cost by Model
                            </h3>
                            <div v-if="modelPieData.labels.length > 0" class="relative h-80">
                                <canvas ref="modelPieCanvas"></canvas>
                            </div>
                            <div v-else class="flex items-center justify-center h-80 text-gray-500 dark:text-gray-400">
                                <p>No data for selected day</p>
                            </div>
                        </div>

                        <!-- Provider Pie Chart -->
                        <div class="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
                            <h3 class="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">
                                Cost by Provider
                            </h3>
                            <div v-if="providerPieData.labels.length > 0" class="relative h-80">
                                <canvas ref="providerPieCanvas"></canvas>
                            </div>
                            <div v-else class="flex items-center justify-center h-80 text-gray-500 dark:text-gray-400">
                                <p>No data for selected day</p>
                            </div>
                        </div>
                    </div>

                    <!-- Token Pie Charts for Selected Day -->
                    <div v-if="allDailyData[selectedDay]?.requests && activeTab === 'tokens' && selectedDay" class="mt-6 grid grid-cols-1 lg:grid-cols-2 gap-6">
                        <!-- Token Model Pie Chart -->
                        <div class="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
                            <h3 class="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">
                                Tokens by Model
                            </h3>
                            <div v-if="tokenModelPieData.labels.length > 0" class="relative h-80">
                                <canvas ref="tokenModelPieCanvas"></canvas>
                            </div>
                            <div v-else class="flex items-center justify-center h-80 text-gray-500 dark:text-gray-400">
                                <p>No data for selected day</p>
                            </div>
                        </div>

                        <!-- Token Provider Pie Chart -->
                        <div class="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
                            <h3 class="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">
                                Tokens by Provider
                            </h3>
                            <div v-if="tokenProviderPieData.labels.length > 0" class="relative h-80">
                                <canvas ref="tokenProviderPieCanvas"></canvas>
                            </div>
                            <div v-else class="flex items-center justify-center h-80 text-gray-500 dark:text-gray-400">
                                <p>No data for selected day</p>
                            </div>
                        </div>
                    </div>

                    <!-- Activity Tab - Full Page Layout -->
                    <div v-if="activeTab === 'activity'" class="h-full flex flex-col bg-white dark:bg-gray-800">
                        <!-- Filters Bar -->
                        <div class="flex-shrink-0 border-b border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 px-3 sm:px-6 py-4">
                            <div class="flex flex-wrap gap-2 sm:gap-4 items-end">
                                <div class="flex flex-col flex-1 min-w-[120px] sm:flex-initial">
                                    <select v-model="selectedModel" class="px-3 py-2 border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 w-full">
                                        <option value="">All Models</option>
                                        <option v-for="model in filterOptions.models" :key="model" :value="model">
                                            {{ model }}
                                        </option>
                                    </select>
                                </div>

                                <div class="flex flex-col flex-1 min-w-[120px] sm:flex-initial">
                                    <select v-model="selectedProvider" class="px-3 py-2 border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 w-full">
                                        <option value="">All Providers</option>
                                        <option v-for="provider in filterOptions.providers" :key="provider" :value="provider">
                                            {{ provider }}
                                        </option>
                                    </select>
                                </div>

                                <div class="flex flex-col flex-1 min-w-[140px] sm:flex-initial">
                                    <select v-model="sortBy" class="px-3 py-2 border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 w-full">
                                        <option value="created">Date (Newest)</option>
                                        <option value="cost">Cost (Highest)</option>
                                        <option value="duration">Duration (Longest)</option>
                                        <option value="totalTokens">Tokens (Most)</option>
                                    </select>
                                </div>

                                <button v-if="hasActiveFilters" @click="clearActivityFilters" class="px-4 py-2 text-sm text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 border border-gray-300 dark:border-gray-600 rounded-md hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors whitespace-nowrap">
                                    Clear Filters
                                </button>
                            </div>
                        </div>

                        <!-- Requests List with Infinite Scroll -->
                        <div class="flex-1 overflow-y-auto" @scroll="onActivityScroll" ref="activityScrollContainer">
                            <div v-if="isActivityLoading && activityRequests.length === 0" class="flex items-center justify-center h-full">
                                <div class="text-center">
                                    <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
                                    <p class="mt-4 text-gray-600 dark:text-gray-400">Loading requests...</p>
                                </div>
                            </div>

                            <div v-else-if="activityRequests.length === 0" class="flex items-center justify-center h-full">
                                <p class="text-gray-500 dark:text-gray-400">No requests found</p>
                            </div>

                            <div v-else class="divide-y divide-gray-200 dark:divide-gray-700">
                                <div v-for="request in activityRequests" :key="request.id" class="px-3 sm:px-6 py-4 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors">
                                    <div class="flex flex-col lg:flex-row items-start justify-between gap-4">
                                        <div class="flex-1 min-w-0 w-full">
                                            <div class="flex flex-col sm:flex-row justify-between gap-2 mb-2">
                                                <div class="flex items-center gap-2 flex-wrap">
                                                    <span class="text-xs px-2 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-300 rounded font-medium">{{ request.model }}</span>
                                                    <span class="text-xs px-2 py-1 bg-purple-100 dark:bg-purple-900/30 text-purple-800 dark:text-purple-300 rounded font-medium">{{ request.provider }}</span>
                                                    <span v-if="request.providerRef" class="text-xs px-2 py-1 bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-300 rounded font-medium">{{ request.providerRef }}</span>
                                                    <span v-if="request.finishReason" class="text-xs px-2 py-1 bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-300 rounded font-medium">{{ request.finishReason }}</span>
                                                </div>
                                                <div class="text-xs text-gray-500 dark:text-gray-400 whitespace-nowrap">
                                                    {{ formatActivityDate(request.created) }}
                                                </div>
                                            </div>
                                            <div class="text-sm font-semibold text-gray-900 dark:text-gray-100 truncate mb-3">
                                                {{ request.title }}
                                            </div>

                                            <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-3 sm:gap-4">
                                                <div :title="request.cost">
                                                    <div class="text-xs text-gray-500 dark:text-gray-400 font-medium">Cost</div>
                                                    <div class="text-sm font-semibold text-gray-900 dark:text-gray-100">{{ formatCost(request.cost) }}</div>
                                                </div>
                                                <div class="col-span-2 sm:col-span-1">
                                                    <div class="text-xs text-gray-500 dark:text-gray-400 font-medium">Tokens</div>
                                                    <div class="text-sm font-semibold text-gray-900 dark:text-gray-100">
                                                        {{ humanifyNumber(request.inputTokens) }} -> {{ humanifyNumber(request.outputTokens) }}
                                                        <span v-if="request.inputCachedTokens" class="ml-1 text-xs text-gray-500 dark:text-gray-400">({{ humanifyNumber(request.inputCachedTokens) }} cached)</span>
                                                    </div>
                                                </div>
                                                <div>
                                                    <div class="text-xs text-gray-500 dark:text-gray-400 font-medium">Duration</div>
                                                    <div class="text-sm font-semibold text-gray-900 dark:text-gray-100">{{ request.duration ? humanifyMs(request.duration) : '—' }}</div>
                                                </div>
                                                <div>
                                                    <div class="text-xs text-gray-500 dark:text-gray-400 font-medium">Speed</div>
                                                    <div class="text-sm font-semibold text-gray-900 dark:text-gray-100">{{ request.duration && request.outputTokens ? (request.outputTokens / (request.duration / 1000)).toFixed(1) + ' tok/s' : '—' }}</div>
                                                </div>
                                            </div>
                                        </div>
                                        <div class="flex flex-row lg:flex-col gap-2 w-full lg:w-auto">
                                            <button type="button" v-if="threadExists(request.threadId)" @click="openThread(request.threadId)" class="flex-1 lg:flex-initial px-3 sm:px-4 py-2 text-sm font-medium text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300 border border-blue-300 dark:border-blue-600 rounded hover:bg-blue-50 dark:hover:bg-blue-900/30 transition-colors whitespace-nowrap">
                                                View<span class="hidden sm:inline"> Thread</span>
                                            </button>
                                            <button type="button" @click="deleteRequestLog(request.id)" class="flex-1 lg:flex-initial px-3 sm:px-4 py-2 text-sm font-medium text-red-600 dark:text-red-500 hover:text-red-800 dark:hover:text-red-400 border border-red-300 dark:border-red-600 rounded hover:bg-red-50 dark:hover:bg-red-900/30 transition-colors whitespace-nowrap">
                                                Delete<span class="hidden sm:inline"> Request</span>
                                            </button>
                                        </div>
                                    </div>
                                </div>

                                <div v-if="isActivityLoadingMore" class="px-6 py-8 text-center">
                                    <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto"></div>
                                </div>

                                <div v-if="!activityHasMore && activityRequests.length > 0" class="px-6 py-8 text-center text-gray-500 dark:text-gray-400 text-sm">
                                    No more requests to load
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `,
    setup() {
        const router = useRouter()
        const route = useRoute()
        const threads = useThreadStore()
        const { initDB } = threads

        // Initialize activeTab from URL query parameter, default to 'cost'
        const activeTab = ref(route.query.tab || 'cost')
        const costChartType = ref('bar')
        const costChartCanvas = ref(null)
        const tokenChartCanvas = ref(null)
        const modelPieCanvas = ref(null)
        const providerPieCanvas = ref(null)
        const tokenModelPieCanvas = ref(null)
        const tokenProviderPieCanvas = ref(null)
        let costChartInstance = null
        let tokenChartInstance = null
        let modelPieChartInstance = null
        let providerPieChartInstance = null
        let tokenModelPieChartInstance = null
        let tokenProviderPieChartInstance = null

        // Month/Year selection - read from URL as source of truth
        const currentDate = new Date()
        const selectedMonth = computed(() => {
            return route.query.month !== undefined ? parseInt(route.query.month) : currentDate.getMonth() + 1
        })
        const selectedYear = computed(() => {
            return route.query.year !== undefined ? parseInt(route.query.year) : currentDate.getFullYear()
        })
        const allDailyData = ref({}) // Store all data for filtering

        // Selected day - read from URL, default to today
        const selectedDay = computed(() => {
            if (route.query.day !== undefined) {
                return route.query.day
            }
            // Default to today
            const today = new Date()
            return today.toISOString().split('T')[0]
        })

        const chartData = ref({
            labels: [],
            datasets: [],
            dateKeys: [] // Store full date keys for click handling
        })

        const tokenChartData = ref({
            labels: [],
            datasets: [],
            dateKeys: [] // Store full date keys for click handling
        })

        const modelPieData = ref({
            labels: [],
            datasets: []
        })

        const providerPieData = ref({
            labels: [],
            datasets: []
        })

        const tokenModelPieData = ref({
            labels: [],
            datasets: []
        })

        const tokenProviderPieData = ref({
            labels: [],
            datasets: []
        })

        const totalCost = computed(() => {
            // Calculate totals for selected month/year only
            const filteredDates = Object.keys(allDailyData.value)
                .filter(dateKey => {
                    const date = new Date(dateKey + 'T00:00:00Z')
                    return date.getFullYear() === selectedYear.value && (date.getMonth() + 1) === selectedMonth.value
                })
            return filteredDates.reduce((sum, date) => sum + (allDailyData.value[date].cost || 0), 0)
        })

        const totalRequests = computed(() => {
            // Calculate totals for selected month/year only
            const filteredDates = Object.keys(allDailyData.value)
                .filter(dateKey => {
                    const date = new Date(dateKey + 'T00:00:00Z')
                    return date.getFullYear() === selectedYear.value && (date.getMonth() + 1) === selectedMonth.value
                })
            return filteredDates.reduce((sum, date) => sum + (allDailyData.value[date].requests || 0), 0)
        })

        const totalInputTokens = computed(() => {
            // Calculate totals for selected month/year only
            const filteredDates = Object.keys(allDailyData.value)
                .filter(dateKey => {
                    const date = new Date(dateKey + 'T00:00:00Z')
                    return date.getFullYear() === selectedYear.value && (date.getMonth() + 1) === selectedMonth.value
                })
            return filteredDates.reduce((sum, date) => sum + (allDailyData.value[date].inputTokens || 0), 0)
        })

        const totalOutputTokens = computed(() => {
            // Calculate totals for selected month/year only
            const filteredDates = Object.keys(allDailyData.value)
                .filter(dateKey => {
                    const date = new Date(dateKey + 'T00:00:00Z')
                    return date.getFullYear() === selectedYear.value && (date.getMonth() + 1) === selectedMonth.value
                })
            return filteredDates.reduce((sum, date) => sum + (allDailyData.value[date].outputTokens || 0), 0)
        })

        // Activity tab state
        const activityRequests = ref([])
        const isActivityLoading = ref(false)
        const isActivityLoadingMore = ref(false)
        const activityHasMore = ref(true)
        const activityOffset = ref(0)
        const activityPageSize = 20
        const existingThreadIds = ref(new Set())

        const selectedModel = ref('')
        const selectedProvider = ref('')
        const sortBy = ref('created')
        const filterOptions = ref({ models: [], providers: [] })
        const activityScrollContainer = ref(null)

        const hasActiveFilters = computed(() => selectedModel.value || selectedProvider.value)

        async function loadAnalyticsData() {
            try {
                const db = await initDB()
                const tx = db.transaction(['requests'], 'readonly')
                const store = tx.objectStore('requests')
                const allRequests = await store.getAll()

                // Group requests by date
                const dailyData = {}
                let totalCostSum = 0
                let totalInputSum = 0
                let totalOutputSum = 0
                const yearsSet = new Set()

                allRequests.forEach(req => {
                    const date = new Date(req.created * 1000)
                    const dateKey = date.toISOString().split('T')[0] // YYYY-MM-DD
                    yearsSet.add(date.getFullYear())

                    if (!dailyData[dateKey]) {
                        dailyData[dateKey] = {
                            cost: 0,
                            requests: 0,
                            inputTokens: 0,
                            outputTokens: 0
                        }
                    }

                    dailyData[dateKey].cost += req.cost || 0
                    dailyData[dateKey].requests += 1
                    dailyData[dateKey].inputTokens += req.inputTokens || 0
                    dailyData[dateKey].outputTokens += req.outputTokens || 0

                    totalCostSum += req.cost || 0
                    totalInputSum += req.inputTokens || 0
                    totalOutputSum += req.outputTokens || 0
                })

                // Store all daily data for filtering
                allDailyData.value = dailyData

                // Update chart data based on selected month/year
                updateChartData()

                await nextTick()
                renderCostChart()
                renderTokenChart()
            } catch (error) {
                console.error('Error loading analytics data:', error)
            }
        }

        function updateChartData() {
            // Filter data for selected month and year
            const filteredDates = Object.keys(allDailyData.value)
                .filter(dateKey => {
                    const date = new Date(dateKey + 'T00:00:00Z')
                    return date.getFullYear() === selectedYear.value && (date.getMonth() + 1) === selectedMonth.value
                })
                .sort()

            const costs = filteredDates.map(date => allDailyData.value[date].cost)
            const inputTokens = filteredDates.map(date => allDailyData.value[date].inputTokens)
            const outputTokens = filteredDates.map(date => allDailyData.value[date].outputTokens)

            // Extract day numbers from dates for labels
            const dayLabels = filteredDates.map(dateKey => {
                const date = new Date(dateKey + 'T00:00:00Z')
                return date.getDate().toString()
            })

            // Cost chart data
            chartData.value = {
                labels: dayLabels,
                dateKeys: filteredDates, // Store full date keys for click handling
                datasets: [{
                    label: 'Daily Cost ($)',
                    data: costs,
                    backgroundColor: colors[0].background,
                    borderColor: colors[0].border,
                    borderWidth: 2,
                    tension: 0.1
                }]
            }

            // Token chart data (stacked)
            tokenChartData.value = {
                labels: dayLabels,
                dateKeys: filteredDates, // Store full date keys for click handling
                datasets: [
                    {
                        label: 'Input Tokens',
                        data: inputTokens,
                        backgroundColor: 'rgba(168, 85, 247, 0.2)',
                        borderColor: 'rgb(126, 34, 206)',
                        borderWidth: 1
                    },
                    {
                        label: 'Output Tokens',
                        data: outputTokens,
                        backgroundColor: 'rgba(251, 146, 60, 0.2)',
                        borderColor: 'rgb(234, 88, 12)',
                        borderWidth: 1
                    }
                ]
            }
        }

        async function updatePieChartData(dateKey) {
            if (!dateKey) {
                modelPieData.value = { labels: [], datasets: [] }
                providerPieData.value = { labels: [], datasets: [] }
                return
            }

            try {
                const db = await initDB()
                const tx = db.transaction(['requests'], 'readonly')
                const store = tx.objectStore('requests')
                const allRequests = await store.getAll()

                // Filter requests for the selected day
                const dayStart = Math.floor(new Date(dateKey + 'T00:00:00Z').getTime() / 1000)
                const dayEnd = Math.floor(new Date(dateKey + 'T23:59:59Z').getTime() / 1000)

                const dayRequests = allRequests.filter(req => req.created >= dayStart && req.created <= dayEnd)

                // Aggregate by model
                const modelData = {}
                const providerData = {}

                dayRequests.forEach(req => {
                    // Model aggregation
                    if (!modelData[req.model]) {
                        modelData[req.model] = { cost: 0, count: 0 }
                    }
                    modelData[req.model].cost += req.cost || 0
                    modelData[req.model].count += 1

                    // Provider aggregation
                    if (!providerData[req.provider]) {
                        providerData[req.provider] = { cost: 0, count: 0 }
                    }
                    providerData[req.provider].cost += req.cost || 0
                    providerData[req.provider].count += 1
                })

                // Prepare model pie chart data
                const modelLabels = Object.keys(modelData).sort()
                const modelCosts = modelLabels.map(model => modelData[model].cost)

                modelPieData.value = {
                    labels: modelLabels,
                    datasets: [{
                        label: 'Cost by Model',
                        data: modelCosts,
                        backgroundColor: colors.map(c => c.background),
                        borderColor: colors.map(c => c.border),
                        borderWidth: 2
                    }]
                }

                // Prepare provider pie chart data
                const providerLabels = Object.keys(providerData).sort()
                const providerCosts = providerLabels.map(provider => providerData[provider].cost)

                providerPieData.value = {
                    labels: providerLabels,
                    datasets: [{
                        label: 'Cost by Provider',
                        data: providerCosts,
                        backgroundColor: colors.map(c => c.background),
                        borderColor: colors.map(c => c.border),
                        borderWidth: 2
                    }]
                }
            } catch (error) {
                console.error('Error updating pie chart data:', error)
            }
        }

        async function updateTokenPieChartData(dateKey) {
            if (!dateKey) {
                tokenModelPieData.value = { labels: [], datasets: [] }
                tokenProviderPieData.value = { labels: [], datasets: [] }
                return
            }

            try {
                const db = await initDB()
                const tx = db.transaction(['requests'], 'readonly')
                const store = tx.objectStore('requests')
                const allRequests = await store.getAll()

                // Filter requests for the selected day
                const dayStart = Math.floor(new Date(dateKey + 'T00:00:00Z').getTime() / 1000)
                const dayEnd = Math.floor(new Date(dateKey + 'T23:59:59Z').getTime() / 1000)

                const dayRequests = allRequests.filter(req => req.created >= dayStart && req.created <= dayEnd)

                // Aggregate by model and provider (using tokens)
                const modelData = {}
                const providerData = {}

                dayRequests.forEach(req => {
                    const totalTokens = (req.inputTokens || 0) + (req.outputTokens || 0)

                    // Model aggregation
                    if (!modelData[req.model]) {
                        modelData[req.model] = { tokens: 0, count: 0 }
                    }
                    modelData[req.model].tokens += totalTokens
                    modelData[req.model].count += 1

                    // Provider aggregation
                    if (!providerData[req.provider]) {
                        providerData[req.provider] = { tokens: 0, count: 0 }
                    }
                    providerData[req.provider].tokens += totalTokens
                    providerData[req.provider].count += 1
                })

                // Prepare model pie chart data
                const modelLabels = Object.keys(modelData).sort()
                const modelTokens = modelLabels.map(model => modelData[model].tokens)

                tokenModelPieData.value = {
                    labels: modelLabels,
                    datasets: [{
                        label: 'Tokens by Model',
                        data: modelTokens,
                        backgroundColor: colors.map(c => c.background),
                        borderColor: colors.map(c => c.border),
                        borderWidth: 2
                    }]
                }

                // Prepare provider pie chart data
                const providerLabels = Object.keys(providerData).sort()
                const providerTokens = providerLabels.map(provider => providerData[provider].tokens)

                tokenProviderPieData.value = {
                    labels: providerLabels,
                    datasets: [{
                        label: 'Tokens by Provider',
                        data: providerTokens,
                        backgroundColor: colors.map(c => c.background),
                        borderColor: colors.map(c => c.border),
                        borderWidth: 2
                    }]
                }
            } catch (error) {
                console.error('Error updating token pie chart data:', error)
            }
        }

        function renderCostChart() {
            if (!costChartCanvas.value || chartData.value.labels.length === 0) return

            // Destroy existing chart
            if (costChartInstance) {
                costChartInstance.destroy()
            }

            const ctx = costChartCanvas.value.getContext('2d')
            const chartTypeValue = costChartType.value

            // Find the index of the selected day
            const selectedDayIndex = chartData.value.dateKeys.indexOf(selectedDay.value)

            // Create color arrays with highlight for selected day
            const backgroundColor = chartData.value.dateKeys.map((_, index) => {
                if (index === selectedDayIndex) {
                    return 'rgba(34, 197, 94, 0.8)' // Green for selected day
                }
                return colors[0].background
            })

            const borderColor = chartData.value.dateKeys.map((_, index) => {
                if (index === selectedDayIndex) {
                    return 'rgb(22, 163, 74)' // Darker green for selected day
                }
                return colors[0].border
            })

            // Update dataset with dynamic colors
            const chartDataWithColors = {
                ...chartData.value,
                datasets: [{
                    ...chartData.value.datasets[0],
                    backgroundColor: backgroundColor,
                    borderColor: borderColor
                }]
            }

            costChartInstance = new Chart(ctx, {
                type: chartTypeValue,
                data: chartDataWithColors,
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    onClick: async (_, elements) => {
                        if (chartTypeValue === 'bar' && elements.length > 0) {
                            const index = elements[0].index
                            const dateKey = chartData.value.dateKeys[index]
                            // Update URL with selected day
                            router.push({
                                query: {
                                    ...route.query,
                                    day: dateKey
                                }
                            })
                        }
                    },
                    plugins: {
                        legend: {
                            display: true,
                            position: 'top'
                        },
                        tooltip: {
                            callbacks: {
                                title: function(context) {
                                    const index = context[0].dataIndex
                                    const dateKey = chartData.value.dateKeys[index]
                                    const date = new Date(dateKey + 'T00:00:00Z')
                                    return date.toLocaleDateString(undefined, { year: 'numeric', month: 'long', day: 'numeric' })
                                },
                                label: function(context) {
                                    return `Cost: ${formatCost(context.parsed.y)}`
                                }
                            }
                        }
                    },
                    scales: {
                        y: {
                            beginAtZero: true,
                            ticks: {
                                callback: function(value) {
                                    return '$' + value.toFixed(4)
                                }
                            }
                        }
                    }
                }
            })
        }

        function renderTokenChart() {
            if (!tokenChartCanvas.value || tokenChartData.value.labels.length === 0) return

            // Destroy existing chart
            if (tokenChartInstance) {
                tokenChartInstance.destroy()
            }

            const ctx = tokenChartCanvas.value.getContext('2d')

            // Find the index of the selected day
            const selectedDayIndex = tokenChartData.value.dateKeys.indexOf(selectedDay.value)

            // Create color arrays with highlight for selected day
            const inputBackgroundColor = tokenChartData.value.dateKeys.map((_, index) => {
                if (index === selectedDayIndex) {
                    return 'rgba(34, 197, 94, 0.2)' // Green for selected day (light/transparent)
                }
                return 'rgba(168, 85, 247, 0.2)' // Purple for input tokens (light/transparent)
            })

            const inputBorderColor = tokenChartData.value.dateKeys.map((_, index) => {
                if (index === selectedDayIndex) {
                    return 'rgb(22, 163, 74)' // Darker green for selected day
                }
                return 'rgb(126, 34, 206)' // Darker purple for input tokens
            })

            const outputBackgroundColor = tokenChartData.value.dateKeys.map((_, index) => {
                if (index === selectedDayIndex) {
                    return 'rgba(34, 197, 94, 0.2)' // Green for selected day (light/transparent)
                }
                return 'rgba(251, 146, 60, 0.2)' // Orange for output tokens (light/transparent)
            })

            const outputBorderColor = tokenChartData.value.dateKeys.map((_, index) => {
                if (index === selectedDayIndex) {
                    return 'rgb(22, 163, 74)' // Darker green for selected day
                }
                return 'rgb(234, 88, 12)' // Darker orange for output tokens
            })

            // Update datasets with dynamic colors
            const chartDataWithColors = {
                ...tokenChartData.value,
                datasets: [
                    {
                        ...tokenChartData.value.datasets[0],
                        backgroundColor: inputBackgroundColor,
                        borderColor: inputBorderColor
                    },
                    {
                        ...tokenChartData.value.datasets[1],
                        backgroundColor: outputBackgroundColor,
                        borderColor: outputBorderColor
                    }
                ]
            }

            tokenChartInstance = new Chart(ctx, {
                type: 'bar',
                data: chartDataWithColors,
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    indexAxis: 'x',
                    onClick: async (_, elements) => {
                        if (elements.length > 0) {
                            const index = elements[0].index
                            const dateKey = tokenChartData.value.dateKeys[index]
                            // Update URL with selected day
                            router.push({
                                query: {
                                    ...route.query,
                                    day: dateKey
                                }
                            })
                        }
                    },
                    scales: {
                        x: {
                            stacked: true
                        },
                        y: {
                            stacked: true,
                            beginAtZero: true,
                            ticks: {
                                callback: function(value) {
                                    return humanifyNumber(value)
                                }
                            }
                        }
                    },
                    plugins: {
                        legend: {
                            display: true,
                            position: 'top'
                        },
                        tooltip: {
                            callbacks: {
                                title: function(context) {
                                    const index = context[0].dataIndex
                                    const dateKey = tokenChartData.value.dateKeys[index]
                                    const date = new Date(dateKey + 'T00:00:00Z')
                                    return date.toLocaleDateString(undefined, { year: 'numeric', month: 'long', day: 'numeric' })
                                },
                                label: function(context) {
                                    return `${context.dataset.label}: ${humanifyNumber(context.parsed.y)}`
                                }
                            }
                        }
                    }
                }
            })
        }

        function renderModelPieChart() {
            if (!modelPieCanvas.value || modelPieData.value.labels.length === 0) return

            // Destroy existing chart
            if (modelPieChartInstance) {
                modelPieChartInstance.destroy()
            }

            const ctx = modelPieCanvas.value.getContext('2d')

            // Custom plugin to draw percentage labels on pie slices
            const percentagePlugin = {
                id: 'percentageLabel',
                afterDatasetsDraw(chart) {
                    const { ctx: chartCtx, data } = chart
                    chart.getDatasetMeta(0).data.forEach((datapoint, index) => {
                        const { x, y } = datapoint.tooltipPosition()
                        const value = data.datasets[0].data[index]
                        const sum = data.datasets[0].data.reduce((a, b) => a + b, 0)
                        const percentage = ((value * 100) / sum).toFixed(1)

                        // Only display label if percentage > 1%
                        if (parseFloat(percentage) > 1) {
                            // Use white color in dark mode, black in light mode
                            const isDarkMode = document.documentElement.classList.contains('dark')
                            chartCtx.fillStyle = isDarkMode ? '#fff' : '#000'
                            chartCtx.font = 'bold 12px Arial'
                            chartCtx.textAlign = 'center'
                            chartCtx.textBaseline = 'middle'
                            chartCtx.fillText(percentage + '%', x, y)
                        }
                    })
                }
            }

            modelPieChartInstance = new Chart(ctx, {
                type: 'pie',
                data: modelPieData.value,
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            display: true,
                            position: 'right'
                        },
                        tooltip: {
                            callbacks: {
                                label: function(context) {
                                    return `${context.label}: ${formatCost(context.parsed)}`
                                }
                            }
                        }
                    }
                },
                plugins: [percentagePlugin]
            })
        }

        function renderProviderPieChart() {
            if (!providerPieCanvas.value || providerPieData.value.labels.length === 0) return

            // Destroy existing chart
            if (providerPieChartInstance) {
                providerPieChartInstance.destroy()
            }

            const ctx = providerPieCanvas.value.getContext('2d')

            // Custom plugin to draw percentage labels on pie slices
            const percentagePlugin = {
                id: 'percentageLabel',
                afterDatasetsDraw(chart) {
                    const { ctx: chartCtx, data } = chart
                    chart.getDatasetMeta(0).data.forEach((datapoint, index) => {
                        const { x, y } = datapoint.tooltipPosition()
                        const value = data.datasets[0].data[index]
                        const sum = data.datasets[0].data.reduce((a, b) => a + b, 0)
                        const percentage = ((value * 100) / sum).toFixed(1)

                        // Only display label if percentage > 1%
                        if (parseFloat(percentage) > 1) {
                            // Use white color in dark mode, black in light mode
                            const isDarkMode = document.documentElement.classList.contains('dark')
                            chartCtx.fillStyle = isDarkMode ? '#fff' : '#000'
                            chartCtx.font = 'bold 12px Arial'
                            chartCtx.textAlign = 'center'
                            chartCtx.textBaseline = 'middle'
                            chartCtx.fillText(percentage + '%', x, y)
                        }
                    })
                }
            }

            providerPieChartInstance = new Chart(ctx, {
                type: 'pie',
                data: providerPieData.value,
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            display: true,
                            position: 'right'
                        },
                        tooltip: {
                            callbacks: {
                                label: function(context) {
                                    return `${context.label}: ${formatCost(context.parsed)}`
                                }
                            }
                        }
                    }
                },
                plugins: [percentagePlugin]
            })
        }

        function renderTokenModelPieChart() {
            if (!tokenModelPieCanvas.value || tokenModelPieData.value.labels.length === 0) return

            // Destroy existing chart
            if (tokenModelPieChartInstance) {
                tokenModelPieChartInstance.destroy()
            }

            const ctx = tokenModelPieCanvas.value.getContext('2d')

            // Custom plugin to draw percentage labels on pie slices
            const percentagePlugin = {
                id: 'percentageLabel',
                afterDatasetsDraw(chart) {
                    const { ctx: chartCtx, data } = chart
                    chart.getDatasetMeta(0).data.forEach((datapoint, index) => {
                        const { x, y } = datapoint.tooltipPosition()
                        const value = data.datasets[0].data[index]
                        const sum = data.datasets[0].data.reduce((a, b) => a + b, 0)
                        const percentage = ((value * 100) / sum).toFixed(1)

                        // Only display label if percentage > 1%
                        if (parseFloat(percentage) > 1) {
                            // Use white color in dark mode, black in light mode
                            const isDarkMode = document.documentElement.classList.contains('dark')
                            chartCtx.fillStyle = isDarkMode ? '#fff' : '#000'
                            chartCtx.font = 'bold 12px Arial'
                            chartCtx.textAlign = 'center'
                            chartCtx.textBaseline = 'middle'
                            chartCtx.fillText(percentage + '%', x, y)
                        }
                    })
                }
            }

            tokenModelPieChartInstance = new Chart(ctx, {
                type: 'pie',
                data: tokenModelPieData.value,
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            display: true,
                            position: 'right'
                        },
                        tooltip: {
                            callbacks: {
                                label: function(context) {
                                    return `${context.label}: ${humanifyNumber(context.parsed)}`
                                }
                            }
                        }
                    }
                },
                plugins: [percentagePlugin]
            })
        }

        function renderTokenProviderPieChart() {
            if (!tokenProviderPieCanvas.value || tokenProviderPieData.value.labels.length === 0) return

            // Destroy existing chart
            if (tokenProviderPieChartInstance) {
                tokenProviderPieChartInstance.destroy()
            }

            const ctx = tokenProviderPieCanvas.value.getContext('2d')

            // Custom plugin to draw percentage labels on pie slices
            const percentagePlugin = {
                id: 'percentageLabel',
                afterDatasetsDraw(chart) {
                    const { ctx: chartCtx, data } = chart
                    chart.getDatasetMeta(0).data.forEach((datapoint, index) => {
                        const { x, y } = datapoint.tooltipPosition()
                        const value = data.datasets[0].data[index]
                        const sum = data.datasets[0].data.reduce((a, b) => a + b, 0)
                        const percentage = ((value * 100) / sum).toFixed(1)

                        // Only display label if percentage > 1%
                        if (parseFloat(percentage) > 1) {
                            // Use white color in dark mode, black in light mode
                            const isDarkMode = document.documentElement.classList.contains('dark')
                            chartCtx.fillStyle = isDarkMode ? '#fff' : '#000'
                            chartCtx.font = 'bold 12px Arial'
                            chartCtx.textAlign = 'center'
                            chartCtx.textBaseline = 'middle'
                            chartCtx.fillText(percentage + '%', x, y)
                        }
                    })
                }
            }

            tokenProviderPieChartInstance = new Chart(ctx, {
                type: 'pie',
                data: tokenProviderPieData.value,
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            display: true,
                            position: 'right'
                        },
                        tooltip: {
                            callbacks: {
                                label: function(context) {
                                    return `${context.label}: ${humanifyNumber(context.parsed)}`
                                }
                            }
                        }
                    }
                },
                plugins: [percentagePlugin]
            })
        }

        // Activity tab functions
        const loadActivityFilterOptions = async () => {
            try {
                filterOptions.value = await threads.getFilterOptions()
            } catch (error) {
                console.error('Failed to load filter options:', error)
            }
        }

        const loadExistingThreadIds = async () => {
            try {
                // Calculate date range for selected month/year
                const startDate = new Date(selectedYear.value, selectedMonth.value - 1, 1)
                const endDate = new Date(selectedYear.value, selectedMonth.value, 0, 23, 59, 59)

                // Convert to timestamp strings (threadId format)
                const startThreadId = startDate.getTime().toString()
                const endThreadId = endDate.getTime().toString()

                const db = await initDB()
                const tx = db.transaction(['threads'], 'readonly')
                const store = tx.objectStore('threads')

                // Use IDBKeyRange to only load threads within the month's timestamp range
                const range = IDBKeyRange.bound(startThreadId, endThreadId)
                const monthThreads = await store.getAll(range)

                // Create a Set of existing thread IDs for the month
                existingThreadIds.value = new Set(monthThreads.map(thread => thread.id))
            } catch (error) {
                console.error('Failed to load existing thread IDs:', error)
                existingThreadIds.value = new Set()
            }
        }

        const threadExists = (threadId) => {
            return threadId ? existingThreadIds.value.has(threadId) : false
        }

        const loadActivityRequests = async (reset = false) => {
            if (reset) {
                activityOffset.value = 0
                activityRequests.value = []
                isActivityLoading.value = true
                // Load all existing thread IDs once when resetting
                await loadExistingThreadIds()
            } else {
                isActivityLoadingMore.value = true
            }

            try {
                // Calculate date range for selected month/year
                const startDate = new Date(selectedYear.value, selectedMonth.value - 1, 1)
                const endDate = new Date(selectedYear.value, selectedMonth.value, 0, 23, 59, 59)

                const filters = {
                    model: selectedModel.value || null,
                    provider: selectedProvider.value || null,
                    sortBy: sortBy.value,
                    sortOrder: 'desc',
                    startDate: Math.floor(startDate.getTime() / 1000),
                    endDate: Math.floor(endDate.getTime() / 1000)
                }

                const result = await threads.getRequests(filters, activityPageSize, activityOffset.value)

                if (reset) {
                    activityRequests.value = result.requests
                } else {
                    activityRequests.value.push(...result.requests)
                }

                activityHasMore.value = result.hasMore
                activityOffset.value += activityPageSize
            } catch (error) {
                console.error('Failed to load requests:', error)
            } finally {
                isActivityLoading.value = false
                isActivityLoadingMore.value = false
            }
        }

        const onActivityScroll = async () => {
            if (!activityScrollContainer.value) return

            const { scrollTop, scrollHeight, clientHeight } = activityScrollContainer.value
            const isNearBottom = scrollHeight - scrollTop - clientHeight < 200

            if (isNearBottom && activityHasMore.value && !isActivityLoadingMore.value && !isActivityLoading.value) {
                await loadActivityRequests(false)
            }
        }

        const clearActivityFilters = async () => {
            selectedModel.value = ''
            selectedProvider.value = ''
            sortBy.value = 'created'
            await loadActivityRequests(true)
        }

        const formatActivityDate = (timestamp) => {
            const date = new Date(timestamp * 1000)
            return date.toLocaleTimeString(undefined, { hour12: false }) + ' ' 
                + date.toLocaleDateString(undefined, { year: 'numeric', month: 'long', day: 'numeric' }) 
                
        }

        const openThread = (threadId) => {
            router.push(`${router.currentRoute.value.path.split('/').slice(0, -1).join('/')}/c/${threadId}`)
        }

        const deleteRequestLog = async (requestId) => {
            if (confirm('Are you sure you want to delete this request log?')) {
                try {
                    await threads.deleteRequest(requestId)
                    // Remove from the list
                    activityRequests.value = activityRequests.value.filter(r => r.id !== requestId)
                    // Reload analytics data
                    await loadAnalyticsData()
                } catch (error) {
                    console.error('Failed to delete request:', error)
                    alert('Failed to delete request')
                }
            }
        }

        watch(costChartType, () => {
            renderCostChart()
        })

        watch(() => route.query, async () => {
            updateChartData()
            await nextTick()
            renderCostChart()
            renderTokenChart()

            // Also update pie charts if a day is selected
            if (selectedDay.value) {
                if (activeTab.value === 'cost') {
                    await updatePieChartData(selectedDay.value)
                    await nextTick()
                    renderModelPieChart()
                    renderProviderPieChart()
                } else if (activeTab.value === 'tokens') {
                    await updateTokenPieChartData(selectedDay.value)
                    await nextTick()
                    renderTokenModelPieChart()
                    renderTokenProviderPieChart()
                }
            }
        })

        watch(selectedDay, async (newDay) => {
            if (newDay) {
                if (activeTab.value === 'cost') {
                    await updatePieChartData(newDay)
                    await nextTick()
                    renderModelPieChart()
                    renderProviderPieChart()
                } else if (activeTab.value === 'tokens') {
                    await updateTokenPieChartData(newDay)
                    await nextTick()
                    renderTokenModelPieChart()
                    renderTokenProviderPieChart()
                }
            }
        })

        watch(modelPieData, () => {
            renderModelPieChart()
        }, { deep: true })

        watch(providerPieData, () => {
            renderProviderPieChart()
        }, { deep: true })

        watch(tokenModelPieData, () => {
            renderTokenModelPieChart()
        }, { deep: true })

        watch(tokenProviderPieData, () => {
            renderTokenProviderPieChart()
        }, { deep: true })

        watch(activeTab, async (newTab) => {
            // Update URL when tab changes, preserving other query parameters
            router.push({ query: { ...route.query, tab: newTab } })

            await nextTick()
            if (newTab === 'cost') {
                renderCostChart()
                renderModelPieChart()
                renderProviderPieChart()
            } else if (newTab === 'tokens') {
                renderTokenChart()
                // Load token pie data if not already loaded
                if (tokenModelPieData.value.labels.length === 0 && selectedDay.value) {
                    await updateTokenPieChartData(selectedDay.value)
                    await nextTick()
                }
                renderTokenModelPieChart()
                renderTokenProviderPieChart()
            } else if (newTab === 'activity') {
                await loadActivityFilterOptions()
                await loadActivityRequests(true)
            }
        })

        // Watch for activity filter changes and reload requests
        watch([selectedModel, selectedProvider, sortBy, selectedMonth, selectedYear], async () => {
            if (activeTab.value === 'activity') {
                await loadActivityRequests(true)
            }
        })

        onMounted(async () => {
            await loadAnalyticsData()

            // Load pie chart data for the selected day (default to today)
            await nextTick()

            if (activeTab.value === 'cost') {
                await updatePieChartData(selectedDay.value)
                await nextTick()
                renderModelPieChart()
                renderProviderPieChart()
            } else if (activeTab.value === 'tokens') {
                await updateTokenPieChartData(selectedDay.value)
                await nextTick()
                renderTokenModelPieChart()
                renderTokenProviderPieChart()
            }

            // If Activity tab is active on page load, load activity data
            if (activeTab.value === 'activity') {
                await loadActivityFilterOptions()
                await loadActivityRequests(true)
            }
        })

        return {
            activeTab,
            costChartType,
            costChartCanvas,
            tokenChartCanvas,
            modelPieCanvas,
            providerPieCanvas,
            tokenModelPieCanvas,
            tokenProviderPieCanvas,
            chartData,
            tokenChartData,
            modelPieData,
            providerPieData,
            tokenModelPieData,
            tokenProviderPieData,
            selectedDay,
            totalCost,
            totalRequests,
            totalInputTokens,
            totalOutputTokens,
            formatCost,
            humanifyNumber,
            humanifyMs,
            // Month/Year selection
            selectedMonth,
            selectedYear,
            allDailyData,
            // Activity tab
            activityRequests,
            isActivityLoading,
            isActivityLoadingMore,
            activityHasMore,
            selectedModel,
            selectedProvider,
            sortBy,
            filterOptions,
            hasActiveFilters,
            activityScrollContainer,
            onActivityScroll,
            clearActivityFilters,
            formatActivityDate,
            threadExists,
            openThread,
            deleteRequestLog,
            loadActivityFilterOptions,
            loadActivityRequests,
        }
    }
}
