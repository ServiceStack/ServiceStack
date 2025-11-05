import { ref, onMounted, watch, nextTick, computed, inject, onUnmounted } from 'vue'
import { useClient, useFormatters } from "@servicestack/vue"
import { leftPart } from '@servicestack/client'
import { Chart, registerables } from "chart.js"
import { QueryDb, QueryResponse } from "dtos"
import { Marked } from "marked"

Chart.register(...registerables)

const { humanifyNumber, humanifyMs } = useFormatters()

export class AdminQueryChatCompletionLogs extends QueryDb {
    /** @param {{month?:string,skip?:number,take?:number,orderBy?:string,orderByDesc?:string,include?:string,fields?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { super(init); Object.assign(this, init) }
    /** @type {?string} */
    month;
    getTypeName() { return 'AdminQueryChatCompletionLogs' }
    getMethod() { return 'GET' }
    createResponse() { return new QueryResponse() }
}
export class ChatCompletionLog {
    /** @param {{id?:number,refId?:string,userId?:string,apiKey?:string,model?:string,provider?:string,userPrompt?:string,answer?:string,requestBody?:string,responseBody?:string,errorCode?:string,error?:ResponseStatus,createdDate?:string,tag?:string,durationMs?:number,promptTokens?:number,completionTokens?:number,cost?:number,providerRef?:string,providerModel?:string,finishReason?:string,usage?:ModelUsage,threadId?:string,title?:string,meta?:{ [index:string]: string; }}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {number} */
    id;
    /** @type {string} */
    refId;
    /** @type {string} */
    userId;
    /** @type {string} */
    apiKey;
    /** @type {string} */
    model;
    /** @type {string} */
    provider;
    /** @type {string} */
    userPrompt;
    /** @type {string} */
    answer;
    /** @type {string} */
    requestBody;
    /** @type {string} */
    responseBody;
    /** @type {string} */
    errorCode;
    /** @type {ResponseStatus} */
    error;
    /** @type {string} */
    createdDate;
    /** @type {string} */
    tag;
    /** @type {?number} */
    durationMs;
    /** @type {?number} */
    promptTokens;
    /** @type {?number} */
    completionTokens;
    /** @type {number} */
    cost;
    /** @type {string} */
    providerRef;
    /** @type {string} */
    providerModel;
    /** @type {string} */
    finishReason;
    /** @type {ModelUsage} */
    usage;
    /** @type {string} */
    threadId;
    /** @type {string} */
    title;
    /** @type {{ [index:string]: string; }} */
    meta;
}
export class AdminMonthlyChatCompletionAnalytics {
    /** @param {{month?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    month;
    getTypeName() { return 'AdminMonthlyChatCompletionAnalytics' }
    getMethod() { return 'GET' }
    createResponse() { return new AdminMonthlyChatCompletionAnalyticsResponse() }
}
export class AdminMonthlyChatCompletionAnalyticsResponse {
    /** @param {{month?:string,modelStats?:ChatCompletionStat[],providerStats?:ChatCompletionStat[],dailyStats?:ChatCompletionStat[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    month;
    /** @type {ChatCompletionStat[]} */
    modelStats = [];
    /** @type {ChatCompletionStat[]} */
    providerStats = [];
    /** @type {ChatCompletionStat[]} */
    dailyStats = [];
}
export class AdminDailyChatCompletionAnalytics {
    /** @param {{day?:string}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {?string} */
    day;
    getTypeName() { return 'AdminDailyChatCompletionAnalytics' }
    getMethod() { return 'GET' }
    createResponse() { return new AdminDailyChatCompletionAnalyticsResponse() }
}
export class AdminDailyChatCompletionAnalyticsResponse {
    /** @param {{modelStats?:ChatCompletionStat[],providerStats?:ChatCompletionStat[]}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {ChatCompletionStat[]} */
    modelStats = [];
    /** @type {ChatCompletionStat[]} */
    providerStats = [];
}

export class ChatCompletionStat {
    /** @param {{name?:string,requests?:number,inputTokens?:number,outputTokens?:number,cost?:number}} [init] */
    constructor(init) { Object.assign(this, init) }
    /** @type {string} */
    name;
    /** @type {number} */
    requests;
    /** @type {number} */
    inputTokens;
    /** @type {number} */
    outputTokens;
    /** @type {number} */
    cost;
}

function formatCost(cost) {
    if (!cost) return '$0.00'
    const numFmt = new Intl.NumberFormat(undefined,{style:'currency',currency:'USD', maximumFractionDigits:6})
    var ret = numFmt.format(parseFloat(cost))
    return ret.endsWith('.00') ? ret.slice(0, -3) : ret
}

export const colors = [
    { background: 'rgba(54, 162, 235, 0.2)',  border: 'rgb(54, 162, 235)' }, //blue
    { background: 'rgba(255, 99, 132, 0.2)',  border: 'rgb(255, 99, 132)' },
    { background: 'rgba(153, 102, 255, 0.2)', border: 'rgb(153, 102, 255)' },
    { background: 'rgba(255, 206, 86, 0.2)',  border: 'rgb(255, 206, 86)' },
    { background: 'rgba(255, 159, 64, 0.2)',  border: 'rgb(255, 159, 64)' },
    { background: 'rgba(67, 56, 202, 0.2)',   border: 'rgb(67, 56, 202)' },
    { background: 'rgba(14, 116, 144, 0.2)',  border: 'rgb(14, 116, 144)' },
    { background: 'rgba(162, 28, 175, 0.2)',  border: 'rgb(162, 28, 175)' },
    { background: 'rgba(75, 192, 192, 0.2)',  border: 'rgb(75, 192, 192)' },
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
        months: Array,
    },
    setup(props) {
        const routes = inject('routes')
        const now = new Date()

        const selectedMonth = computed(() => {
            return routes.month ? parseInt(routes.month.split('-')[1]) : now.getMonth() + 1
        })

        const selectedYear = computed(() => {
            return routes.month ? parseInt(routes.month.split('-')[0]) : now.getFullYear()
        })

        const updateSelection = (year, month) => {
            const monthStr = `${year}-${month.toString().padStart(2, '0')}`
            routes.to({ month: monthStr, day:undefined })
        }

        const availableYears = computed(() => {
            if (!props.months) return []
            const yearsSet = new Set()
            props.months.forEach(monthStr => {
                const year = parseInt(leftPart(monthStr, '-'))
                yearsSet.add(year)
            })
            return Array.from(yearsSet).sort((a, b) => a - b)
        })

        const availableMonthsForYear = computed(() => {
            if (!props.months) return []
            const monthsSet = new Set()
            props.months.forEach(monthStr => {
                const [year, month] = monthStr.split('-')
                if (parseInt(year) === selectedYear.value) {
                    monthsSet.add(parseInt(month))
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

const LogDetailDialog = {
    template: `
        <div v-if="log" class="fixed inset-0 z-50 overflow-hidden" aria-labelledby="slide-over-title" role="dialog" aria-modal="true">
            <div class="absolute inset-0 overflow-hidden">
                <!-- Background overlay -->
                <div class="absolute inset-0 bg-gray-500/50 transition-opacity" @click="$emit('close')"></div>
                
                <!-- Slide-over panel -->
                <div class="pointer-events-none fixed inset-y-0 right-0 flex max-w-full pl-10">
                    <div class="pointer-events-auto w-screen max-w-2xl">
                        <div class="flex h-full flex-col overflow-y-scroll bg-white dark:bg-gray-800 shadow-xl">
                            <!-- Header -->
                            <div class="bg-gray-50 dark:bg-gray-900 px-4 py-6 sm:px-6">
                                <div class="flex items-start justify-between">
                                    <h2 class="text-lg font-medium text-gray-900 dark:text-gray-100" id="slide-over-title">
                                        Request Details
                                    </h2>
                                    <div class="ml-3 flex h-7 items-center">
                                        <button type="button" @click="$emit('close')" class="rounded-md text-gray-400 hover:text-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500">
                                            <span class="sr-only">Close panel</span>
                                            <svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                                                <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
                                            </svg>
                                        </button>
                                    </div>
                                </div>
                            </div>
                            
                            <!-- Content -->
                            <div class="relative flex-1 px-4 py-6 sm:px-6">
                                <div class="space-y-6">
                                    <!-- Basic Info -->
                                    <div>
                                        <h3 class="text-sm font-medium text-gray-900 dark:text-gray-100 mb-3">Basic Information</h3>
                                        <dl class="grid grid-cols-1 gap-x-4 gap-y-3 sm:grid-cols-2">
                                            <div>
                                                <dt class="text-sm font-medium text-gray-500 dark:text-gray-400">Model</dt>
                                                <dd class="mt-1 text-sm text-gray-900 dark:text-gray-100">{{ log.model }}</dd>
                                            </div>
                                            <div>
                                                <dt class="text-sm font-medium text-gray-500 dark:text-gray-400">Provider</dt>
                                                <dd class="mt-1 text-sm text-gray-900 dark:text-gray-100">{{ log.provider }}</dd>
                                            </div>
                                            <div>
                                                <dt class="text-sm font-medium text-gray-500 dark:text-gray-400">Cost</dt>
                                                <dd class="mt-1 text-sm text-gray-900 dark:text-gray-100">{{ formatCost(log.cost) }}</dd>
                                            </div>
                                            <div>
                                                <dt class="text-sm font-medium text-gray-500 dark:text-gray-400">Duration</dt>
                                                <dd class="mt-1 text-sm text-gray-900 dark:text-gray-100">{{ log.durationMs ? humanifyMs(log.durationMs) : '—' }}</dd>
                                            </div>
                                            <div>
                                                <dt class="text-sm font-medium text-gray-500 dark:text-gray-400">Prompt Tokens</dt>
                                                <dd class="mt-1 text-sm text-gray-900 dark:text-gray-100">{{ humanifyNumber(log.promptTokens || 0) }}</dd>
                                            </div>
                                            <div>
                                                <dt class="text-sm font-medium text-gray-500 dark:text-gray-400">Completion Tokens</dt>
                                                <dd class="mt-1 text-sm text-gray-900 dark:text-gray-100">{{ humanifyNumber(log.completionTokens || 0) }}</dd>
                                            </div>
                                            <div v-if="log.finishReason">
                                                <dt class="text-sm font-medium text-gray-500 dark:text-gray-400">Finish Reason</dt>
                                                <dd class="mt-1 text-sm text-gray-900 dark:text-gray-100">{{ log.finishReason }}</dd>
                                            </div>
                                            <div v-if="log.createdDate">
                                                <dt class="text-sm font-medium text-gray-500 dark:text-gray-400">Created</dt>
                                                <dd class="mt-1 text-sm text-gray-900 dark:text-gray-100">{{ new Date(log.createdDate).toLocaleString() }}</dd>
                                            </div>
                                        </dl>
                                    </div>

                                    <!-- User Prompt -->
                                    <div v-if="log.userPrompt">
                                        <h3 class="text-sm font-medium text-gray-900 dark:text-gray-100 mb-2">User Prompt</h3>
                                        <div class="bg-gray-50 dark:bg-gray-900 rounded-md p-3 text-sm text-gray-900 dark:text-gray-100 whitespace-pre-wrap">{{ log.userPrompt }}</div>
                                    </div>

                                    <!-- Answer -->
                                    <div v-if="log.answer">
                                        <div class="flex justify-between items-center mb-2">
                                            <h3 class="text-sm font-medium text-gray-900 dark:text-gray-100">Answer</h3>
                                            <div class="flex space-x-2">
                                                <!-- code icon -->
                                                <svg @click="preview = false" :class="['cursor-pointer size-4', !preview ? 'text-indigo-600 dark:text-indigo-400' : 'text-gray-400 hover:text-gray-600 dark:hover:text-gray-300']" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-linecap="square" stroke-width="2" d="M5.536 15.536L2 12l3.536-3.536m12.928 7.072L22 12l-3.536-3.536M14 4l-4 16"/></svg>
                                                <!-- preview icon -->
                                                <svg @click="preview = true" :class="['cursor-pointer size-4', preview ? 'text-indigo-600 dark:text-indigo-400' : 'text-gray-400 hover:text-gray-600 dark:hover:text-gray-300']" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 26 26"><path fill="currentColor" d="M4 0C1.8 0 0 1.8 0 4v17c0 2.2 1.8 4 4 4h11c.4 0 .7-.094 1-.094c-1.4-.3-2.594-1.006-3.594-1.906H4c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h6.313c.7.2.687 1.1.687 2v3c0 .6.4 1 1 1h3c1 0 2 0 2 1v1h.5c.5 0 1 .088 1.5.188V8c0-1.1-.988-2.112-2.688-3.813c-.3-.2-.512-.487-.812-.687c-.2-.3-.488-.513-.688-.813C13.113.988 12.1 0 11 0zm13.5 12c-3 0-5.5 2.5-5.5 5.5s2.5 5.5 5.5 5.5c1.273 0 2.435-.471 3.375-1.219l.313.313a.955.955 0 0 0 .125 1.218l2.5 2.5c.4.4.975.4 1.375 0l.5-.5c.4-.4.4-1.006 0-1.406l-2.5-2.5a.935.935 0 0 0-1.157-.156l-.281-.313c.773-.948 1.25-2.14 1.25-3.437c0-3-2.5-5.5-5.5-5.5m0 1.5c2.2 0 4 1.8 4 4s-1.8 4-4 4s-4-1.8-4-4s1.8-4 4-4"/></svg>
                                            </div>
                                        </div>
                                        <!-- Code view (default) -->
                                        <div v-if="!preview" class="bg-gray-50 dark:bg-gray-900 rounded-md p-3 text-sm text-gray-900 dark:text-gray-100 whitespace-pre-wrap">{{ log.answer?.trim() }}</div>
                                        <!-- Preview view (rendered markdown) -->
                                        <div v-else class="bg-gray-50 dark:bg-gray-900 rounded-md p-3 text-sm text-gray-900 dark:text-gray-100 prose dark:prose-invert max-w-none" v-html="renderMarkdown(log.answer?.trim())"></div>
                                    </div>

                                    <!-- Error -->
                                    <div v-if="log.error">
                                        <h3 class="text-sm font-medium text-red-600 dark:text-red-400 mb-2">Error</h3>
                                        <div class="bg-red-50 dark:bg-red-900/20 rounded-md p-3 text-sm text-red-900 dark:text-red-100">
                                            <div v-if="log.errorCode" class="font-medium mb-1">{{ log.errorCode }}</div>
                                            <div>{{ log.error.message || JSON.stringify(log.error) }}</div>
                                        </div>
                                    </div>

                                    <!-- Request Body -->
                                    <div v-if="log.requestBody">
                                        <h3 class="text-sm font-medium text-gray-900 dark:text-gray-100 mb-2">Request Body</h3>
                                        <pre class="bg-gray-50 dark:bg-gray-900 rounded-md p-3 text-xs text-gray-900 dark:text-gray-100 overflow-x-auto">{{ formatJson(log.requestBody) }}</pre>
                                    </div>

                                    <!-- Response Body -->
                                    <div v-if="log.responseBody">
                                        <h3 class="text-sm font-medium text-gray-900 dark:text-gray-100 mb-2">Response Body</h3>
                                        <pre class="bg-gray-50 dark:bg-gray-900 rounded-md p-3 text-xs text-gray-900 dark:text-gray-100 overflow-x-auto">{{ formatJson(log.responseBody) }}</pre>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `,
    props: ['log', 'logs', 'routes'],
    emits: ['close'],
    setup(props, { emit }) {
        // Load preview preference from localStorage, default to false
        const savedPreview = localStorage.getItem('adminChat.preview')
        const preview = ref(savedPreview === 'true')

        // Watch for changes and save to localStorage
        watch(preview, (newValue) => {
            localStorage.setItem('adminChat.preview', newValue.toString())
        })

        function formatJson(json) {
            try {
                return JSON.stringify(JSON.parse(json), null, 2)
            } catch {
                return json
            }
        }

        // Navigate to next/previous log
        function navigateToLog(direction) {
            if (!props.logs || !props.log) return

            const currentIndex = props.logs.findIndex(log => log.id === props.log.id)
            if (currentIndex === -1) return

            let nextIndex
            if (direction === 'next') {
                nextIndex = currentIndex + 1
                if (nextIndex >= props.logs.length) return // At the end
            } else {
                nextIndex = currentIndex - 1
                if (nextIndex < 0) return // At the beginning
            }

            const nextLog = props.logs[nextIndex]
            if (nextLog) {
                props.routes.to({ show: nextLog.id })
            }
        }

        // Handle keyboard navigation
        function handleKeydown(event) {
            if (event.key === 'Escape') {
                emit('close')
            } else if (event.key === 'ArrowDown') {
                event.preventDefault()
                navigateToLog('next')
            } else if (event.key === 'ArrowUp') {
                event.preventDefault()
                navigateToLog('prev')
            }
        }

        onMounted(() => {
            document.addEventListener('keydown', handleKeydown)
        })

        onUnmounted(() => {
            document.removeEventListener('keydown', handleKeydown)
        })

        return {
            formatCost,
            humanifyMs,
            humanifyNumber,
            formatJson,
            preview,
            renderMarkdown,
        }
    }
}

export const AdminChat = {
    components: {
        MonthSelector,
        LogDetailDialog,
    },
    template: `
        <div class="flex flex-col h-full w-full bg-gray-50 dark:bg-gray-900">
            <!-- Header with Title and Month Selector -->
            <div class="border-b border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 px-2 sm:px-4 py-3">
                <div class="max-w-6xl mx-auto flex items-center justify-between">
                    <h1 class="text-2xl font-semibold text-gray-900 dark:text-gray-100">Chat Analytics</h1>
                    <MonthSelector :months="months" />
                </div>
            </div>

            <!-- Tabs -->
            <div class="border-b border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 px-4">
                <div class="max-w-6xl mx-auto flex gap-8">
                    <button type="button"
                        @click="routes.to({ tab:'' })"
                        :class="['py-3 px-1 border-b-2 font-medium text-sm transition-colors',
                                 !routes.tab || routes.tab === 'cost'
                                    ? 'border-blue-500 text-blue-600 dark:text-blue-400'
                                    : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200']">
                        Cost Analysis
                    </button>
                    <button type="button"
                        @click="routes.to({ tab:'tokens' })"
                        :class="['py-3 px-1 border-b-2 font-medium text-sm transition-colors',
                                 routes.tab === 'tokens'
                                    ? 'border-blue-500 text-blue-600 dark:text-blue-400'
                                    : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200']">
                        Token Usage
                    </button>
                    <button type="button"
                        @click="routes.to({ tab:'activity' })"
                        :class="['py-3 px-1 border-b-2 font-medium text-sm transition-colors',
                                 routes.tab === 'activity'
                                    ? 'border-blue-500 text-blue-600 dark:text-blue-400'
                                    : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200']">
                        Activity
                    </button>
                </div>
            </div>

            <!-- Content -->
            <div ref="scrollableContent" tabindex="0" class="flex-1 overflow-auto p-4 focus:outline-none">
                <div class="max-w-6xl mx-auto">
                    <!-- Cost Analysis Tab -->
                    <div v-if="!routes.tab || routes.tab === 'cost'" class="space-y-6">
                        <!-- Pie Charts Row -->
                        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                            <!-- Monthly Costs by Model -->
                            <div class="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
                                <h3 class="text-base font-semibold text-gray-900 dark:text-gray-100 mb-4">Monthly Costs by Model</h3>
                                <div style="height: 300px">
                                    <canvas ref="refCostsByModel"></canvas>
                                </div>
                            </div>

                            <!-- Monthly Costs by Provider -->
                            <div class="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
                                <h3 class="text-base font-semibold text-gray-900 dark:text-gray-100 mb-4">Monthly Costs by Provider</h3>
                                <div style="height: 300px">
                                    <canvas ref="refCostsByProvider"></canvas>
                                </div>
                            </div>
                        </div>

                        <!-- Daily Costs Bar Chart -->
                        <div class="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
                            <h3 class="text-base font-semibold text-gray-900 dark:text-gray-100 mb-4">
                                Total Cost by Day
                            </h3>
                            <div style="height: 300px">
                                <canvas ref="refDailyCosts"></canvas>
                            </div>
                        </div>

                        <!-- Daily Breakdown Pie Charts -->
                        <div v-if="selectedDay && dailyAnalytics">
                            <!-- Title with Date and Stats -->
                            <div class="p-3 mb-3">
                                <div class="flex items-center justify-between">
                                    <h3 class="text-lg font-semibold text-gray-900 dark:text-gray-100">
                                        {{ new Date(selectedDay).toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' }) }}
                                    </h3>
                                    <div class="text-sm text-gray-600 dark:text-gray-400">
                                        {{ formatCost((dailyAnalytics.modelStats || []).reduce((sum, s) => sum + (s.cost || 0), 0)) }}
                                        · {{ (dailyAnalytics.modelStats || []).reduce((sum, s) => sum + (s.requests || 0), 0) }} Request{{ (dailyAnalytics.modelStats || []).reduce((sum, s) => sum + (s.requests || 0), 0) !== 1 ? 's' : '' }}
                                        · {{ humanifyNumber((dailyAnalytics.modelStats || []).reduce((sum, s) => sum + (s.inputTokens || 0), 0)) }} → {{ humanifyNumber((dailyAnalytics.modelStats || []).reduce((sum, s) => sum + (s.outputTokens || 0), 0)) }} Tokens
                                    </div>
                                </div>
                            </div>

                            <!-- Pie Charts -->
                            <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                                <!-- Daily Costs by Model -->
                                <div class="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
                                    <h3 class="text-base font-semibold text-gray-900 dark:text-gray-100 mb-4">
                                        Daily Costs by Model
                                    </h3>
                                    <div style="height: 300px">
                                        <canvas ref="refDailyCostsByModel"></canvas>
                                    </div>
                                </div>

                                <!-- Daily Costs by Provider -->
                                <div class="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
                                    <h3 class="text-base font-semibold text-gray-900 dark:text-gray-100 mb-4">
                                        Daily Costs by Provider
                                    </h3>
                                    <div style="height: 300px">
                                        <canvas ref="refDailyCostsByProvider"></canvas>
                                    </div>
                                </div>
                            </div>
                        </div>

                    </div>

                    <!-- Token Usage Tab -->
                    <div v-if="routes.tab === 'tokens'" class="space-y-6">
                        <!-- Pie Charts Row -->
                        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                            <!-- Monthly Tokens by Model -->
                            <div class="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
                                <h3 class="text-base font-semibold text-gray-900 dark:text-gray-100 mb-4">Monthly Tokens by Model</h3>
                                <div style="height: 300px">
                                    <canvas ref="refTokensByModel"></canvas>
                                </div>
                            </div>

                            <!-- Monthly Tokens by Provider -->
                            <div class="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
                                <h3 class="text-base font-semibold text-gray-900 dark:text-gray-100 mb-4">Monthly Tokens by Provider</h3>
                                <div style="height: 300px">
                                    <canvas ref="refTokensByProvider"></canvas>
                                </div>
                            </div>
                        </div>

                        <!-- Daily Token Usage Bar Chart -->
                        <div class="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
                            <h3 class="text-base font-semibold text-gray-900 dark:text-gray-100 mb-4">
                                Daily Token Usage
                            </h3>
                            <div style="height: 300px">
                                <canvas ref="refDailyTokens"></canvas>
                            </div>
                        </div>

                        <!-- Daily Breakdown Pie Charts -->
                        <div v-if="selectedDay && dailyAnalytics">
                            <!-- Title with Date and Stats -->
                            <div class="p-3 mb-3">
                                <div class="flex items-center justify-between">
                                    <h3 class="text-lg font-semibold text-gray-900 dark:text-gray-100">
                                        {{ new Date(selectedDay).toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' }) }}
                                    </h3>
                                    <div class="text-sm text-gray-600 dark:text-gray-400">
                                        {{ formatCost((dailyAnalytics.modelStats || []).reduce((sum, s) => sum + (s.cost || 0), 0)) }}
                                        · {{ (dailyAnalytics.modelStats || []).reduce((sum, s) => sum + (s.requests || 0), 0) }} Request{{ (dailyAnalytics.modelStats || []).reduce((sum, s) => sum + (s.requests || 0), 0) !== 1 ? 's' : '' }}
                                        · {{ humanifyNumber((dailyAnalytics.modelStats || []).reduce((sum, s) => sum + (s.inputTokens || 0), 0)) }} → {{ humanifyNumber((dailyAnalytics.modelStats || []).reduce((sum, s) => sum + (s.outputTokens || 0), 0)) }} Tokens
                                    </div>
                                </div>
                            </div>

                            <!-- Pie Charts -->
                            <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                                <!-- Daily Tokens by Model -->
                                <div class="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
                                    <h3 class="text-base font-semibold text-gray-900 dark:text-gray-100 mb-4">
                                        Daily Tokens by Model
                                    </h3>
                                    <div style="height: 300px">
                                        <canvas ref="refDailyTokensByModel"></canvas>
                                    </div>
                                </div>

                                <!-- Daily Tokens by Provider -->
                                <div class="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
                                    <h3 class="text-base font-semibold text-gray-900 dark:text-gray-100 mb-4">
                                        Daily Tokens by Provider
                                    </h3>
                                    <div style="height: 300px">
                                        <canvas ref="refDailyTokensByProvider"></canvas>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- Activity Tab -->
                    <div v-if="routes.tab === 'activity'">
                        <div class="bg-white dark:bg-gray-800 shadow overflow-hidden rounded-lg">
                            <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                                <thead class="bg-gray-50 dark:bg-gray-900">
                                    <tr>
                                        <th scope="col" @click="toggleSort('createdDate')" class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-800 select-none">
                                            <div class="flex items-center gap-1">
                                                Date
                                                <span v-if="sortBy === 'createdDate'" class="text-gray-400">↓</span>
                                                <span v-else-if="sortBy === '-createdDate'" class="text-gray-400">↑</span>
                                            </div>
                                        </th>
                                        <th scope="col" @click="toggleSort('model')" class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-800 select-none">
                                            <div class="flex items-center gap-1">
                                                Model
                                                <span v-if="sortBy === 'model'" class="text-gray-400">↓</span>
                                                <span v-else-if="sortBy === '-model'" class="text-gray-400">↑</span>
                                            </div>
                                        </th>
                                        <th scope="col" @click="toggleSort('provider')" class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-800 select-none">
                                            <div class="flex items-center gap-1">
                                                Provider
                                                <span v-if="sortBy === 'provider'" class="text-gray-400">↓</span>
                                                <span v-else-if="sortBy === '-provider'" class="text-gray-400">↑</span>
                                            </div>
                                        </th>
                                        <th scope="col" @click="toggleSort('completionTokens')" class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-800 select-none">
                                            <div class="flex items-center gap-1">
                                                Tokens
                                                <span v-if="sortBy === 'completionTokens'" class="text-gray-400">↓</span>
                                                <span v-else-if="sortBy === '-completionTokens'" class="text-gray-400">↑</span>
                                            </div>
                                        </th>
                                        <th scope="col" @click="toggleSort('cost')" class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-800 select-none">
                                            <div class="flex items-center gap-1">
                                                Cost
                                                <span v-if="sortBy === 'cost'" class="text-gray-400">↓</span>
                                                <span v-else-if="sortBy === '-cost'" class="text-gray-400">↑</span>
                                            </div>
                                        </th>
                                        <th scope="col" @click="toggleSort('durationMs')" class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-800 select-none">
                                            <div class="flex items-center gap-1">
                                                Duration
                                                <span v-if="sortBy === 'durationMs'" class="text-gray-400">↓</span>
                                                <span v-else-if="sortBy === '-durationMs'" class="text-gray-400">↑</span>
                                            </div>
                                        </th>
                                    </tr>
                                </thead>
                                <tbody class="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
                                    <tr v-for="log in logs" :key="log.id"
                                        @click="routes.to({ show:log.id })"
                                        class="hover:bg-gray-50 dark:hover:bg-gray-700 cursor-pointer transition-colors">
                                        <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900 dark:text-gray-100">
                                            {{ new Date(log.createdDate).toLocaleString() }}
                                        </td>
                                        <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900 dark:text-gray-100">
                                            {{ log.model }}
                                        </td>
                                        <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900 dark:text-gray-100">
                                            {{ log.provider }}
                                        </td>
                                        <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900 dark:text-gray-100">
                                            {{ humanifyNumber(log.completionTokens || 0) }}
                                        </td>
                                        <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900 dark:text-gray-100">
                                            {{ formatCost(log.cost) }}
                                        </td>
                                        <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900 dark:text-gray-100">
                                            {{ log.durationMs ? humanifyMs(log.durationMs) : '—' }}
                                        </td>
                                    </tr>
                                </tbody>
                            </table>

                            <!-- Pagination Controls -->
                            <div class="bg-gray-50 dark:bg-gray-900 px-4 py-3 flex items-center justify-between border-t border-gray-200 dark:border-gray-700 sm:px-6">
                                <div class="flex-1 flex justify-between sm:hidden">
                                    <button @click="previousPage" :disabled="currentPage === 1"
                                        :class="['relative inline-flex items-center px-4 py-2 border border-gray-300 dark:border-gray-600 text-sm font-medium rounded-md',
                                                 currentPage === 1
                                                    ? 'bg-gray-100 dark:bg-gray-800 text-gray-400 dark:text-gray-600 cursor-not-allowed'
                                                    : 'bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700']">
                                        Previous
                                    </button>
                                    <button @click="nextPage" :disabled="!hasMorePages"
                                        :class="['ml-3 relative inline-flex items-center px-4 py-2 border border-gray-300 dark:border-gray-600 text-sm font-medium rounded-md',
                                                 !hasMorePages
                                                    ? 'bg-gray-100 dark:bg-gray-800 text-gray-400 dark:text-gray-600 cursor-not-allowed'
                                                    : 'bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700']">
                                        Next
                                    </button>
                                </div>
                                <div class="hidden sm:flex-1 sm:flex sm:items-center sm:justify-between">
                                    <div>
                                        <p class="text-sm text-gray-700 dark:text-gray-300">
                                            Showing
                                            <span class="font-medium">{{ (currentPage - 1) * pageSize + 1 }}</span>
                                            to
                                            <span class="font-medium">{{ Math.min(currentPage * pageSize, (currentPage - 1) * pageSize + logs.length) }}</span>
                                            <span v-if="totalCount > 0">
                                                of
                                                <span class="font-medium">{{ totalCount }}</span>
                                                results
                                            </span>
                                        </p>
                                    </div>
                                    <div>
                                        <nav class="relative z-0 inline-flex rounded-md shadow-sm -space-x-px" aria-label="Pagination">
                                            <button type="button" @click="previousPage" :disabled="currentPage === 1"
                                                :class="['relative inline-flex items-center px-2 py-2 rounded-l-md border border-gray-300 dark:border-gray-600 text-sm font-medium',
                                                         currentPage === 1
                                                            ? 'bg-gray-100 dark:bg-gray-800 text-gray-400 dark:text-gray-600 cursor-not-allowed'
                                                            : 'bg-white dark:bg-gray-800 text-gray-500 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700']">
                                                <span class="sr-only">Previous</span>
                                                <svg class="h-5 w-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                                                    <path fill-rule="evenodd" d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z" clip-rule="evenodd" />
                                                </svg>
                                            </button>

                                            <!-- Page Numbers -->
                                            <template v-for="page in visiblePages" :key="page">
                                                <button type="button" v-if="page === '...'" disabled
                                                    class="relative inline-flex items-center px-4 py-2 border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm font-medium text-gray-700 dark:text-gray-300">
                                                    ...
                                                </button>
                                                <button type="button" v-else @click="goToPage(page)"
                                                    :class="['relative inline-flex items-center px-4 py-2 border border-gray-300 dark:border-gray-600 text-sm font-medium',
                                                             page === currentPage
                                                                ? 'z-10 bg-indigo-50 dark:bg-indigo-900 border-indigo-500 text-indigo-600 dark:text-indigo-300'
                                                                : 'bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700']">
                                                    {{ page }}
                                                </button>
                                            </template>

                                            <button @click="nextPage" :disabled="!hasMorePages"
                                                :class="['relative inline-flex items-center px-2 py-2 rounded-r-md border border-gray-300 dark:border-gray-600 text-sm font-medium',
                                                         !hasMorePages
                                                            ? 'bg-gray-100 dark:bg-gray-800 text-gray-400 dark:text-gray-600 cursor-not-allowed'
                                                            : 'bg-white dark:bg-gray-800 text-gray-500 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700']">
                                                <span class="sr-only">Next</span>
                                                <svg class="h-5 w-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                                                    <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd" />
                                                </svg>
                                            </button>
                                        </nav>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Log Detail Dialog -->
            <LogDetailDialog v-if="selectedLog" :log="selectedLog" :logs="logs" :routes="routes" @close="routes.to({ show:undefined })" />
        </div>
    `,
    setup() {
        const routes = inject('routes')
        const server = inject('server')
        const client = useClient()
        const selectedLog = ref(null)
        const preview = ref(false)

        // Scrollable content ref for keyboard navigation
        const scrollableContent = ref(null)

        // Data refs
        const analytics = ref(null)
        const dailyAnalytics = ref(null)
        const logs = ref([])
        const months = ref(server.plugins.adminChat?.analytics?.months ?? [])
        const sortBy = ref('-id')
        const currentPage = ref(1)
        const pageSize = ref(25)
        const totalCount = ref(0)
        const selectedDay = computed(() => {
            if (routes.day) {
                return routes.day
            }
            // Default to today
            const today = new Date()
            return today.toISOString().split('T')[0]
        })

        const hasMorePages = computed(() => {
            return logs.value.length === pageSize.value
        })

        const totalPages = computed(() => {
            if (totalCount.value === 0) return 1
            return Math.ceil(totalCount.value / pageSize.value)
        })

        const visiblePages = computed(() => {
            const total = totalPages.value
            const current = currentPage.value
            const delta = 2
            const pages = []

            if (total <= 7) {
                // Show all pages if 7 or fewer
                for (let i = 1; i <= total; i++) {
                    pages.push(i)
                }
            } else {
                // Always show first page
                pages.push(1)

                // Calculate range around current page
                let start = Math.max(2, current - delta)
                let end = Math.min(total - 1, current + delta)

                // Add ellipsis after first page if needed
                if (start > 2) {
                    pages.push('...')
                }

                // Add pages around current
                for (let i = start; i <= end; i++) {
                    pages.push(i)
                }

                // Add ellipsis before last page if needed
                if (end < total - 1) {
                    pages.push('...')
                }

                // Always show last page
                pages.push(total)
            }

            return pages
        })

        // Chart refs
        const refDailyCosts = ref(null)
        const refCostsByModel = ref(null)
        const refCostsByProvider = ref(null)
        const refDailyTokens = ref(null)
        const refTokensByModel = ref(null)
        const refTokensByProvider = ref(null)
        const refDailyCostsByModel = ref(null)
        const refDailyCostsByProvider = ref(null)
        const refDailyTokensByModel = ref(null)
        const refDailyTokensByProvider = ref(null)

        // Chart instances
        let dailyCostsChart = null
        let costsByModelChart = null
        let costsByProviderChart = null
        let dailyTokensChart = null
        let tokensByModelChart = null
        let tokensByProviderChart = null
        let dailyCostsByModelChart = null
        let dailyCostsByProviderChart = null
        let dailyTokensByModelChart = null
        let dailyTokensByProviderChart = null

        // Toggle sort column
        function toggleSort(column) {
            if (sortBy.value === column) {
                // Currently ascending, switch to descending
                sortBy.value = `-${column}`
            } else if (sortBy.value === `-${column}`) {
                // Currently descending, switch to ascending
                sortBy.value = column
            } else {
                // New column, default to descending
                sortBy.value = `-${column}`
            }
            currentPage.value = 1 // Reset to first page when sorting changes
            loadData({ orderBy: sortBy.value })
        }

        // Pagination functions
        function nextPage() {
            if (hasMorePages.value) {
                currentPage.value++
                loadData()
            }
        }

        function previousPage() {
            if (currentPage.value > 1) {
                currentPage.value--
                loadData()
            }
        }

        function goToPage(page) {
            if (page !== '...' && page !== currentPage.value) {
                currentPage.value = page
                loadData()
            }
        }

        // Load data from API
        async function loadData(args={}) {
            const apiAnalytics = await client.api(new AdminMonthlyChatCompletionAnalytics({
                month: routes.month,
            }))
            analytics.value = apiAnalytics.response || null

            const skip = (currentPage.value - 1) * pageSize.value
            const apiLogs = await client.api(new AdminQueryChatCompletionLogs({
                month: routes.month,
                include: 'total',
                orderBy: sortBy.value,
                skip: skip,
                take: pageSize.value,
                ...args,
            }))
            logs.value = apiLogs.response?.results || []
            totalCount.value = apiLogs.response?.total || 0

            // Always show a day - either current day or the latest day with data
            if (analytics.value?.dailyStats?.length > 0 && analytics.value.month) {
                const today = new Date()
                const currentDay = today.getDate().toString()
                const currentMonth = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}`
                const viewingMonth = analytics.value.month // Use the month from the response
                await loadDailyData(selectedDay.value)
            }
        }

        // Load daily analytics data
        async function loadDailyData(day) {
            const apiDailyAnalytics = await client.api(new AdminDailyChatCompletionAnalytics({
                day: day,
            }))
            dailyAnalytics.value = apiDailyAnalytics.response || null
        }

        // Create Daily Costs Bar Chart
        function createDailyCostsChart() {
            if (!analytics.value?.dailyStats || !refDailyCosts.value) return

            // Sort by day number
            const sortedStats = [...analytics.value.dailyStats].sort((a, b) => parseInt(a.name) - parseInt(b.name))
            const labels = sortedStats.map(stat => stat.name)
            const data = sortedStats.map(stat => stat.cost)

            // Determine background colors based on selected day
            const backgroundColor = labels.map(day => {
                if (selectedDay.value) {
                    const month = analytics.value.month
                    const dayStr = `${month}-${day.padStart(2, '0')}`
                    return dayStr === selectedDay.value ? 'rgba(34, 197, 94, 0.2)' : 'rgba(54, 162, 235, 0.2)'
                }
                return 'rgba(54, 162, 235, 0.2)'
            })
            const borderColor = labels.map(day => {
                if (selectedDay.value) {
                    const month = analytics.value.month
                    const dayStr = `${month}-${day.padStart(2, '0')}`
                    return dayStr === selectedDay.value ? 'rgb(34, 197, 94)' : 'rgb(54, 162, 235)'
                }
                return 'rgb(54, 162, 235)'
            })

            dailyCostsChart?.destroy()
            dailyCostsChart = new Chart(refDailyCosts.value, {
                type: 'bar',
                data: {
                    labels,
                    datasets: [{
                        label: 'Cost',
                        data,
                        backgroundColor,
                        borderColor,
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    onHover: (event, activeElements) => {
                        event.native.target.style.cursor = activeElements.length > 0 ? 'pointer' : 'default'
                    },
                    plugins: {
                        legend: {
                            display: false
                        },
                        tooltip: {
                            callbacks: {
                                title: (context) => {
                                    const dayLabel = context[0].label
                                    return `Day ${dayLabel}`
                                },
                                label: (context) => {
                                    const dayLabel = context.label
                                    const dayStat = sortedStats.find(s => s.name === dayLabel)
                                    if (!dayStat) return formatCost(context.raw)

                                    return [
                                        `Cost: ${formatCost(dayStat.cost || 0)}`,
                                        `Requests: ${dayStat.requests || 0}`,
                                        `Input Tokens: ${humanifyNumber(dayStat.inputTokens || 0)}`,
                                        `Output Tokens: ${humanifyNumber(dayStat.outputTokens || 0)}`
                                    ]
                                }
                            }
                        }
                    },
                    scales: {
                        y: {
                            beginAtZero: true,
                            ticks: {
                                callback: (value) => formatCost(value)
                            }
                        }
                    },
                    onClick: (e, elements, chart) => {
                        const points = chart.getElementsAtEventForMode(e, 'nearest', { intersect: true }, false)
                        if (points.length) {
                            const firstPoint = points[0]
                            const dayLabel = chart.data.labels[firstPoint.index]
                            const month = analytics.value.month
                            const day = `${month}-${dayLabel.padStart(2, '0')}`
                            routes.to({ day })
                        }
                    }
                }
            })
        }

        // Create Costs by Model Pie Chart
        function createCostsByModelChart() {
            if (!analytics.value?.modelStats || !refCostsByModel.value) return

            const labels = analytics.value.modelStats.map(stat => stat.name)
            const data = analytics.value.modelStats.map(stat => stat.cost)
            const backgroundColor = labels.map((_, i) => colors[i % colors.length].background)
            const borderColor = labels.map((_, i) => colors[i % colors.length].border)

            costsByModelChart?.destroy()
            costsByModelChart = new Chart(refCostsByModel.value, {
                type: 'pie',
                data: {
                    labels,
                    datasets: [{
                        data,
                        backgroundColor,
                        borderColor,
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            position: 'right'
                        },
                        tooltip: {
                            callbacks: {
                                label: (context) => {
                                    const label = context.label || ''
                                    const value = context.raw || 0
                                    const total = context.dataset.data.reduce((a, b) => a + b, 0)
                                    const percentage = Math.round((value / total) * 100)
                                    const stat = analytics.value.modelStats[context.dataIndex]
                                    const totalTokens = (stat.inputTokens || 0) + (stat.outputTokens || 0)
                                    return [
                                        `${label}: (${percentage}%)`,
                                        `${formatCost(value)} · ${humanifyNumber(totalTokens)} tokens`
                                    ]
                                }
                            }
                        }
                    }
                }
            })
        }

        // Create Costs by Provider Pie Chart
        function createCostsByProviderChart() {
            if (!analytics.value?.providerStats || !refCostsByProvider.value) return

            const labels = analytics.value.providerStats.map(stat => stat.name)
            const data = analytics.value.providerStats.map(stat => stat.cost)
            const backgroundColor = labels.map((_, i) => colors[i % colors.length].background)
            const borderColor = labels.map((_, i) => colors[i % colors.length].border)

            costsByProviderChart?.destroy()
            costsByProviderChart = new Chart(refCostsByProvider.value, {
                type: 'pie',
                data: {
                    labels,
                    datasets: [{
                        data,
                        backgroundColor,
                        borderColor,
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            position: 'right'
                        },
                        tooltip: {
                            callbacks: {
                                label: (context) => {
                                    const label = context.label || ''
                                    const value = context.raw || 0
                                    const total = context.dataset.data.reduce((a, b) => a + b, 0)
                                    const percentage = Math.round((value / total) * 100)
                                    const stat = analytics.value.providerStats[context.dataIndex]
                                    const totalTokens = (stat.inputTokens || 0) + (stat.outputTokens || 0)
                                    return [
                                        `${label}: (${percentage}%)`,
                                        `${formatCost(value)} · ${humanifyNumber(totalTokens)} tokens`
                                    ]
                                }
                            }
                        }
                    }
                }
            })
        }

        // Create Daily Tokens Bar Chart
        function createDailyTokensChart() {
            if (!analytics.value?.dailyStats || !refDailyTokens.value) return

            // Sort by day number
            const sortedStats = [...analytics.value.dailyStats].sort((a, b) => parseInt(a.name) - parseInt(b.name))
            const labels = sortedStats.map(stat => stat.name)
            const inputTokensData = sortedStats.map(stat => stat.inputTokens || 0)
            const outputTokensData = sortedStats.map(stat => stat.outputTokens || 0)

            // Determine background colors based on selected day
            const inputBackgroundColor = labels.map(day => {
                if (selectedDay.value) {
                    const month = analytics.value.month
                    const dayStr = `${month}-${day.padStart(2, '0')}`
                    return dayStr === selectedDay.value ? 'rgba(34, 197, 94, 0.2)' : 'rgba(54, 162, 235, 0.2)'
                }
                return 'rgba(54, 162, 235, 0.2)'
            })
            const inputBorderColor = labels.map(day => {
                if (selectedDay.value) {
                    const month = analytics.value.month
                    const dayStr = `${month}-${day.padStart(2, '0')}`
                    return dayStr === selectedDay.value ? 'rgb(34, 197, 94)' : 'rgb(54, 162, 235)'
                }
                return 'rgb(54, 162, 235)'
            })
            const outputBackgroundColor = labels.map(day => {
                if (selectedDay.value) {
                    const month = analytics.value.month
                    const dayStr = `${month}-${day.padStart(2, '0')}`
                    return dayStr === selectedDay.value ? 'rgba(34, 197, 94, 0.4)' : 'rgba(75, 192, 192, 0.2)'
                }
                return 'rgba(75, 192, 192, 0.2)'
            })
            const outputBorderColor = labels.map(day => {
                if (selectedDay.value) {
                    const month = analytics.value.month
                    const dayStr = `${month}-${day.padStart(2, '0')}`
                    return dayStr === selectedDay.value ? 'rgb(34, 197, 94)' : 'rgb(75, 192, 192)'
                }
                return 'rgb(75, 192, 192)'
            })

            dailyTokensChart?.destroy()
            dailyTokensChart = new Chart(refDailyTokens.value, {
                type: 'bar',
                data: {
                    labels,
                    datasets: [{
                        label: 'Input Tokens',
                        data: inputTokensData,
                        backgroundColor: inputBackgroundColor,
                        borderColor: inputBorderColor,
                        borderWidth: 1
                    }, {
                        label: 'Output Tokens',
                        data: outputTokensData,
                        backgroundColor: outputBackgroundColor,
                        borderColor: outputBorderColor,
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    onHover: (event, activeElements) => {
                        event.native.target.style.cursor = activeElements.length > 0 ? 'pointer' : 'default'
                    },
                    plugins: {
                        legend: {
                            display: true,
                            position: 'top'
                        },
                        tooltip: {
                            callbacks: {
                                title: (context) => {
                                    const dayLabel = context[0].label
                                    return `Day ${dayLabel}`
                                },
                                beforeBody: (context) => {
                                    const dayLabel = context[0].label
                                    const dayStat = sortedStats.find(s => s.name === dayLabel)
                                    if (!dayStat) return []

                                    return [
                                        `Cost: ${formatCost(dayStat.cost || 0)}`,
                                        `Requests: ${dayStat.requests || 0}`,
                                        `Input Tokens: ${humanifyNumber(dayStat.inputTokens || 0)}`,
                                        `Output Tokens: ${humanifyNumber(dayStat.outputTokens || 0)}`,
                                        '' // Empty line separator
                                    ]
                                },
                                label: (context) => {
                                    return `${context.dataset.label}: ${humanifyNumber(context.raw)}`
                                }
                            }
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
                                callback: (value) => humanifyNumber(value)
                            }
                        }
                    },
                    onClick: (e, elements, chart) => {
                        const points = chart.getElementsAtEventForMode(e, 'nearest', { intersect: true }, false)
                        if (points.length) {
                            const firstPoint = points[0]
                            const dayLabel = chart.data.labels[firstPoint.index]
                            const month = analytics.value.month
                            const day = `${month}-${dayLabel.padStart(2, '0')}`
                            routes.to({ day })
                        }
                    }
                }
            })
        }

        // Create Tokens by Model Pie Chart
        function createTokensByModelChart() {
            if (!analytics.value?.modelStats || !refTokensByModel.value) return

            const labels = analytics.value.modelStats.map(stat => stat.name)
            const data = analytics.value.modelStats.map(stat => (stat.inputTokens || 0) + (stat.outputTokens || 0))
            const backgroundColor = labels.map((_, i) => colors[i % colors.length].background)
            const borderColor = labels.map((_, i) => colors[i % colors.length].border)

            tokensByModelChart?.destroy()
            tokensByModelChart = new Chart(refTokensByModel.value, {
                type: 'pie',
                data: {
                    labels,
                    datasets: [{
                        data,
                        backgroundColor,
                        borderColor,
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            position: 'right'
                        },
                        tooltip: {
                            callbacks: {
                                label: (context) => {
                                    const label = context.label || ''
                                    const value = context.raw || 0
                                    const total = context.dataset.data.reduce((a, b) => a + b, 0)
                                    const percentage = Math.round((value / total) * 100)
                                    const stat = analytics.value.modelStats[context.dataIndex]
                                    return [
                                        `${label}: (${percentage}%)`,
                                        `${humanifyNumber(value)} tokens · ${formatCost(stat.cost)}`
                                    ]
                                }
                            }
                        }
                    }
                }
            })
        }

        // Create Tokens by Provider Pie Chart
        function createTokensByProviderChart() {
            if (!analytics.value?.providerStats || !refTokensByProvider.value) return

            const labels = analytics.value.providerStats.map(stat => stat.name)
            const data = analytics.value.providerStats.map(stat => (stat.inputTokens || 0) + (stat.outputTokens || 0))
            const backgroundColor = labels.map((_, i) => colors[i % colors.length].background)
            const borderColor = labels.map((_, i) => colors[i % colors.length].border)

            tokensByProviderChart?.destroy()
            tokensByProviderChart = new Chart(refTokensByProvider.value, {
                type: 'pie',
                data: {
                    labels,
                    datasets: [{
                        data,
                        backgroundColor,
                        borderColor,
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            position: 'right'
                        },
                        tooltip: {
                            callbacks: {
                                label: (context) => {
                                    const label = context.label || ''
                                    const value = context.raw || 0
                                    const total = context.dataset.data.reduce((a, b) => a + b, 0)
                                    const percentage = Math.round((value / total) * 100)
                                    const stat = analytics.value.providerStats[context.dataIndex]
                                    return [
                                        `${label}: (${percentage}%)`,
                                        `${humanifyNumber(value)} tokens · ${formatCost(stat.cost)}`
                                    ]
                                }
                            }
                        }
                    }
                }
            })
        }

        // Create Daily Costs by Model Pie Chart
        function createDailyCostsByModelChart() {
            if (!dailyAnalytics.value?.modelStats || !refDailyCostsByModel.value) return

            const labels = dailyAnalytics.value.modelStats.map(stat => stat.name)
            const data = dailyAnalytics.value.modelStats.map(stat => stat.cost)
            const backgroundColor = labels.map((_, i) => colors[i % colors.length].background)
            const borderColor = labels.map((_, i) => colors[i % colors.length].border)

            dailyCostsByModelChart?.destroy()
            dailyCostsByModelChart = new Chart(refDailyCostsByModel.value, {
                type: 'pie',
                data: {
                    labels,
                    datasets: [{
                        data,
                        backgroundColor,
                        borderColor,
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            position: 'right'
                        },
                        tooltip: {
                            callbacks: {
                                label: (context) => {
                                    const label = context.label || ''
                                    const value = context.raw || 0
                                    const total = context.dataset.data.reduce((a, b) => a + b, 0)
                                    const percentage = Math.round((value / total) * 100)
                                    const stat = dailyAnalytics.value.modelStats[context.dataIndex]
                                    const totalTokens = (stat.inputTokens || 0) + (stat.outputTokens || 0)
                                    return [
                                        `${label}: (${percentage}%)`,
                                        `${formatCost(value)} · ${humanifyNumber(totalTokens)} tokens`
                                    ]
                                }
                            }
                        }
                    }
                }
            })
        }

        // Create Daily Costs by Provider Pie Chart
        function createDailyCostsByProviderChart() {
            if (!dailyAnalytics.value?.providerStats || !refDailyCostsByProvider.value) return

            const labels = dailyAnalytics.value.providerStats.map(stat => stat.name)
            const data = dailyAnalytics.value.providerStats.map(stat => stat.cost)
            const backgroundColor = labels.map((_, i) => colors[i % colors.length].background)
            const borderColor = labels.map((_, i) => colors[i % colors.length].border)

            dailyCostsByProviderChart?.destroy()
            dailyCostsByProviderChart = new Chart(refDailyCostsByProvider.value, {
                type: 'pie',
                data: {
                    labels,
                    datasets: [{
                        data,
                        backgroundColor,
                        borderColor,
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            position: 'right'
                        },
                        tooltip: {
                            callbacks: {
                                label: (context) => {
                                    const label = context.label || ''
                                    const value = context.raw || 0
                                    const total = context.dataset.data.reduce((a, b) => a + b, 0)
                                    const percentage = Math.round((value / total) * 100)
                                    const stat = dailyAnalytics.value.providerStats[context.dataIndex]
                                    const totalTokens = (stat.inputTokens || 0) + (stat.outputTokens || 0)
                                    return [
                                        `${label}: (${percentage}%)`,
                                        `${formatCost(value)} · ${humanifyNumber(totalTokens)} tokens`
                                    ]
                                }
                            }
                        }
                    }
                }
            })
        }

        // Create Daily Tokens by Model Pie Chart
        function createDailyTokensByModelChart() {
            if (!dailyAnalytics.value?.modelStats || !refDailyTokensByModel.value) return

            const labels = dailyAnalytics.value.modelStats.map(stat => stat.name)
            const data = dailyAnalytics.value.modelStats.map(stat => (stat.inputTokens || 0) + (stat.outputTokens || 0))
            const backgroundColor = labels.map((_, i) => colors[i % colors.length].background)
            const borderColor = labels.map((_, i) => colors[i % colors.length].border)

            dailyTokensByModelChart?.destroy()
            dailyTokensByModelChart = new Chart(refDailyTokensByModel.value, {
                type: 'pie',
                data: {
                    labels,
                    datasets: [{
                        data,
                        backgroundColor,
                        borderColor,
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            position: 'right'
                        },
                        tooltip: {
                            callbacks: {
                                label: (context) => {
                                    const label = context.label || ''
                                    const value = context.raw || 0
                                    const total = context.dataset.data.reduce((a, b) => a + b, 0)
                                    const percentage = Math.round((value / total) * 100)
                                    const stat = dailyAnalytics.value.modelStats[context.dataIndex]
                                    return [
                                        `${label}: (${percentage}%)`,
                                        `${humanifyNumber(value)} tokens · ${formatCost(stat.cost)}`
                                    ]
                                }
                            }
                        }
                    }
                }
            })
        }

        // Create Daily Tokens by Provider Pie Chart
        function createDailyTokensByProviderChart() {
            if (!dailyAnalytics.value?.providerStats || !refDailyTokensByProvider.value) return

            const labels = dailyAnalytics.value.providerStats.map(stat => stat.name)
            const data = dailyAnalytics.value.providerStats.map(stat => (stat.inputTokens || 0) + (stat.outputTokens || 0))
            const backgroundColor = labels.map((_, i) => colors[i % colors.length].background)
            const borderColor = labels.map((_, i) => colors[i % colors.length].border)

            dailyTokensByProviderChart?.destroy()
            dailyTokensByProviderChart = new Chart(refDailyTokensByProvider.value, {
                type: 'pie',
                data: {
                    labels,
                    datasets: [{
                        data,
                        backgroundColor,
                        borderColor,
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            position: 'right'
                        },
                        tooltip: {
                            callbacks: {
                                label: (context) => {
                                    const label = context.label || ''
                                    const value = context.raw || 0
                                    const total = context.dataset.data.reduce((a, b) => a + b, 0)
                                    const percentage = Math.round((value / total) * 100)
                                    const stat = dailyAnalytics.value.providerStats[context.dataIndex]
                                    return [
                                        `${label}: (${percentage}%)`,
                                        `${humanifyNumber(value)} tokens · ${formatCost(stat.cost)}`
                                    ]
                                }
                            }
                        }
                    }
                }
            })
        }

        function updateCharts() {
            nextTick(() => {
                if (!routes.tab || routes.tab === 'cost') {
                    createDailyCostsChart()
                    createCostsByModelChart()
                    createCostsByProviderChart()
                } else if (routes.tab === 'tokens') {
                    createDailyTokensChart()
                    createTokensByModelChart()
                    createTokensByProviderChart()
                }
            })
        }

        function updateDailyCharts() {
            nextTick(() => {
                if (!routes.tab || routes.tab === 'cost') {
                    createDailyCostsByModelChart()
                    createDailyCostsByProviderChart()
                } else if (routes.tab === 'tokens') {
                    createDailyTokensByModelChart()
                    createDailyTokensByProviderChart()
                }
            })
        }

        onMounted(async () => {
            await loadData()
            updateCharts()
            selectedLog.value = logs.value.find(log => log.id === parseInt(routes.show))
            // Focus the scrollable content to enable keyboard navigation (Page Up/Down, Home/End)
            nextTick(() => {
                scrollableContent.value?.focus()
            })
        })

        onUnmounted(() => {
            [
                dailyCostsChart,
                costsByModelChart,
                costsByProviderChart,
                dailyTokensChart,
                tokensByModelChart,
                tokensByProviderChart,
                dailyCostsByModelChart,
                dailyCostsByProviderChart,
                dailyTokensByModelChart,
                dailyTokensByProviderChart,
            ].forEach(chart => chart?.destroy())
        })

        watch(() => routes.tab, () => {
            updateCharts()
            updateDailyCharts()
        })
        watch(() => analytics.value, updateCharts)
        watch(() => dailyAnalytics.value, updateDailyCharts)
        watch(() => selectedDay.value, () => {
            // Load daily analytics for the selected day
            if (selectedDay.value) {
                loadDailyData(selectedDay.value)
            }
            // Update the bar charts to show the selected day in green
            if (!routes.tab || routes.tab === 'cost') {
                createDailyCostsChart()
            } else if (routes.tab === 'tokens') {
                createDailyTokensChart()
            }
        })
        watch(() => routes.month, () => {
            selectedDay.value = null
            dailyAnalytics.value = null
            loadData()
        })
        watch(() => routes.show, () => {
            selectedLog.value = logs.value.find(log => log.id === parseInt(routes.show))
        })

        return {
            routes,
            selectedLog,
            logs,
            sortBy,
            toggleSort,
            currentPage,
            pageSize,
            totalCount,
            hasMorePages,
            totalPages,
            visiblePages,
            nextPage,
            previousPage,
            goToPage,
            months,
            selectedDay,
            dailyAnalytics,
            scrollableContent,
            refDailyCosts,
            refCostsByModel,
            refCostsByProvider,
            refDailyTokens,
            refTokensByModel,
            refTokensByProvider,
            refDailyCostsByModel,
            refDailyCostsByProvider,
            refDailyTokensByModel,
            refDailyTokensByProvider,
            formatCost,
            humanifyNumber,
            humanifyMs,
            preview,
        }
    }
}

const hljs = globalThis.hljs
export const marked = (() => {
    const aliases = {
        vue: 'html',
    }
    const ret = new Marked(
        markedHighlight({
            langPrefix: 'hljs language-',
            highlight(code, lang, info) {
                if (aliases[lang]) {
                    lang = aliases[lang]
                }
                if (lang && hljs.getLanguage(lang)) {
                    return hljs.highlight(code, { language: lang }).value
                }
                // Return plain code without highlighting if language is not recognized
                return code
            }
        })
    )
    return ret
})();

export function renderMarkdown(content) {
    if (content) {
        content = content
            .replaceAll(`\\[ \\boxed{`,'\n<span class="inline-block text-xl text-blue-500 bg-blue-50 dark:text-blue-400 dark:bg-blue-950 px-3 py-1 rounded">')
            .replaceAll('} \\]','</span>\n')
    }
    return marked.parse(content)
}

export function markedHighlight(options) {
    if (typeof options === 'function') {
        options = {
            highlight: options
        }
    }

    if (!options || typeof options.highlight !== 'function') {
        throw new Error('Must provide highlight function')
    }

    if (typeof options.langPrefix !== 'string') {
        options.langPrefix = 'language-'
    }

    return {
        async: !!options.async,
        walkTokens(token) {
            if (token.type !== 'code') {
                return
            }

            const lang = getLang(token.lang)

            if (options.async) {
                return Promise.resolve(options.highlight(token.text, lang, token.lang || '')).then(updateToken(token))
            }

            const code = options.highlight(token.text, lang, token.lang || '')
            if (code instanceof Promise) {
                throw new Error('markedHighlight is not set to async but the highlight function is async. Set the async option to true on markedHighlight to await the async highlight function.')
            }
            updateToken(token)(code)
        },
        renderer: {
            code(code, infoString) {
                const lang = getLang(infoString)
                let text = code.text
                const classAttr = lang
                    ? ` class="${options.langPrefix}${escape(lang)}"`
                    : ' class="hljs"';
                text = text.replace(/\n$/, '')
                return `<pre><code${classAttr}>${code.escaped ? text : escape(text, true)}\n</code></pre>`
            }
        }
    }
}

function getLang(lang) {
    return (lang || '').match(/\S*/)[0]
}

function updateToken(token) {
    return code => {
        if (typeof code === 'string' && code !== token.text) {
            token.escaped = true
            token.text = code
        }
    }
}

// copied from marked helpers
const escapeTest = /[&<>"']/
const escapeReplace = new RegExp(escapeTest.source, 'g')
const escapeTestNoEncode = /[<>"']|&(?!(#\d{1,7}|#[Xx][a-fA-F0-9]{1,6}|\w+);)/
const escapeReplaceNoEncode = new RegExp(escapeTestNoEncode.source, 'g')
const escapeReplacements = {
    '&': '&amp;',
    '<': '&lt;',
    '>': '&gt;',
    '"': '&quot;',
    "'": '&#39;'
}
const getEscapeReplacement = ch => escapeReplacements[ch]
function escape(html, encode) {
    if (encode) {
        if (escapeTest.test(html)) {
            return html.replace(escapeReplace, getEscapeReplacement)
        }
    } else {
        if (escapeTestNoEncode.test(html)) {
            return html.replace(escapeReplaceNoEncode, getEscapeReplacement)
        }
    }

    return html
}
