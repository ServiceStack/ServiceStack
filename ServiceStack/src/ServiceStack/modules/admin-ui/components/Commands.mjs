import { computed, inject, onMounted, onUnmounted, ref, watch, getCurrentInstance } from "vue"
import { useClient, useFormatters, useMetadata, useUtils } from "@servicestack/vue"
import { ApiResult, toDate, humanify } from "@servicestack/client"
import { prettyJson } from "core"
import { ViewCommands } from "dtos"
import { Chart, registerables } from 'chart.js'
Chart.register(...registerables)
export const Commands = {
    template:/*html*/`
    <section class="">
      <div>
        <div class="sm:hidden">
          <label for="redis-tabs" class="sr-only">Select a tab</label>
          <select id="redis-tabs"
                  class="block w-full pl-3 pr-10 py-2 text-base border-gray-300 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm rounded-md"
                  @change="routes.to({ tab: $event.target.value })">
            <option v-for="(tab,name) in tabs" :selected="routes.tab === tab" :value="tab">{{ name }}</option>
          </select>
        </div>
        <div class="hidden sm:block">
          <div class="border-b border-gray-200">
            <nav class="-mb-px flex space-x-8" aria-label="Tabs">
              <a v-for="(tab,name) in tabs" v-href="{ tab, op:'', show:'', body:'' }"
                 :class="[routes.tab === tab ? 'border-indigo-500 text-indigo-600' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300', 'whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm']">
                {{ name }}
              </a>
            </nav>
          </div>
        </div>
      </div>
      <ErrorSummary :status="api.error" />
        
      <div v-if="api.response">
        <div v-if="routes.tab === 'latest'" class="flex">
          <DataGrid :items="api.response.latestCommands" />
        </div>
        <div v-else-if="routes.tab === 'errors'" class="flex">
          <DataGrid :items="api.response.latestFailed"
                    selected-columns="type,name,ms,at,attempt,error"
                    @row-selected="rowSelectedError" :is-selected="row => routes.show === row.id"
            />           
        </div>
        <div v-else>
          <div :class="['mt-2',{ hidden: !routes.op }]" style="max-width:1024px;max-height:512px">
            <canvas ref="elChart"></canvas>
          </div>
          <div class="mt-2 flex flex-wrap">
            <div>
              <button type="button" @click="refresh()" title="Refresh" class="inline-flex items-center px-2.5 py-1.5 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
                <svg class="w-5 h-5" xmlns="http://www.w3.org/2000/svg" aria-hidden="true" viewBox="0 0 24 24"><g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5">
                  <path d="M21.168 8A10.003 10.003 0 0 0 12 2c-5.185 0-9.45 3.947-9.95 9"></path><path d="M17 8h4.4a.6.6 0 0 0 .6-.6V3M2.881 16c1.544 3.532 5.068 6 9.168 6c5.186 0 9.45-3.947 9.951-9"></path><path d="M7.05 16h-4.4a.6.6 0 0 0-.6.6V21"></path></g>
                </svg>
              </button>
            </div>
            <div class="ml-2">
                <span class="inline-flex rounded-md shadow-sm">
                  <label for="message-type" class="sr-only">Select Type</label>
                  <select id="message-type" v-model="type" class="-ml-px block w-full rounded-md border-0 bg-white py-1.5 pl-3 pr-9 text-gray-900 ring-1 ring-inset ring-gray-300 focus:ring-2 focus:ring-inset focus:ring-indigo-600 sm:text-sm sm:leading-6">
                    <option value="ALL">All</option>
                    <option value="API">APIs</option>
                    <option value="CMD">Commands</option>
                  </select>
                </span>
            </div>
            <div class="ml-2">
              <div class="relative">
                <svg class="absolute ml-2.5 mt-2.5 h-4 w-4 text-gray-500" fill="currentColor" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                  <path d="M16.32 14.9l5.39 5.4a1 1 0 0 1-1.42 1.4l-5.38-5.38a8 8 0 1 1 1.41-1.41zM10 16a6 6 0 1 0 0-12 6 6 0 0 0 0 12z"></path>
                </svg>
                <input type="search" placeholder="Filter..." v-model="q" class="border rounded-full overflow-hidden flex w-full px-4 py-1 pl-8 border-gray-200">
              </div>
            </div>
          </div>
          
          <div class="flex">
            <DataGrid :items="commandTotals"
                      selected-columns="name,count,failed,averageMs,medianMs,minMs,maxMs,retries,lastError"
                      @header-selected="headerSelected"
                      @row-selected="rowSelected" :is-selected="row => routes.op === row.type + '.' + row.name"
                >
              <template #name-header><SortableColumn name="name" /></template>
              <template #count-header><SortableColumn name="count" /></template>
              <template #failed-header><SortableColumn name="failed" /></template>
              <template #averageMs-header><SortableColumn name="averageMs" /></template>
              <template #medianMs-header><SortableColumn name="medianMs" /></template>
              <template #minMs-header><SortableColumn name="minMs" /></template>
              <template #maxMs-header><SortableColumn name="maxMs" /></template>
              <template #retries-header><SortableColumn name="retries" /></template>
              
              <template #lastError="{ lastError }">
                <div v-if="lastError" class="w-72 whitespace-nowrap overflow-ellipsis overflow-hidden" :title="altError(lastError)">
                  <b>{{lastError.errorCode}}</b> {{lastError.message}}
                </div>
              </template>
            </DataGrid>
          </div>
        </div>
        <div class="mt-12 flex justify-center">
          <loading v-if="loadingMore" class="text-gray-400">loading...</loading>
        </div>
        <span ref="bottom"></span>
        <div v-if="selectedError" class="relative z-20" aria-labelledby="slide-over-title" role="dialog" aria-modal="true">
          <div class="fixed overflow-hidden">
            <div class="absolute overflow-hidden">
              <div class="pointer-events-none fixed inset-y-0 right-0 flex max-w-full pl-10 sm:pl-16">
                <div class="pointer-events-auto w-screen max-w-2xl">
                  <form class="flex h-full flex-col overflow-y-auto bg-white shadow-xl">
                    <div class="flex-1">
                      <!-- Header -->
                      <div class="bg-gray-50 px-4 py-6 sm:px-6">
                        <div class="flex items-start justify-between space-x-3">
                          <div class="space-y-1">
                            <h2 class="flex text-lg">
                              <div class="font-medium text-gray-900">
                                {{selectedError.type}} {{selectedError.name}} {{selectedError.ms}}ms at {{time(toDate(selectedError.at))}}
                              </div>
                            </h2>
                          </div>
                          <div class="flex h-7 items-center">
                            <CloseButton @close="toggleError(selectedError)" button-class="bg-gray-50" />
                          </div>
                        </div>
                      </div>
                      <div class="space-y-6 py-6 sm:space-y-0 sm:divide-y sm:divide-gray-200 sm:py-0">
                        <div class="flex overflow-auto">
                          <div class="relative w-full">
                            <div class="bg-indigo-700 text-white px-3 py-3">
                              <div class="flex items-start justify-between space-x-3">
                                <h2 class="font-medium text-white">Request</h2>
                              </div>
                            </div>
                            <div class="p-2">
                              <span class="relative z-0 inline-flex shadow-sm rounded-md">
                                <a v-for="(tab,name) in {Pretty:'',Preview:'preview'}" v-href="{ body:tab }"
                                   :class="[{ Pretty:'rounded-l-md',Raw:'-ml-px',Preview:'rounded-r-md -ml-px' }[name], routes.body === tab ? 'z-10 outline-none ring-1 ring-indigo-500 border-indigo-500' : '', 'cursor-pointer relative inline-flex items-center px-4 py-1 border border-gray-300 bg-white text-sm font-medium text-gray-700 hover:bg-gray-50']">
                                    {{name}}
                                </a>
                              </span>
                              <div v-if="routes.body == ''" class="pt-2 icon-outer" style="min-height:2.5rem">
                                <CopyIcon class="absolute right-4" :text="prettyJson(selectedError.request)" />
                                <pre class="whitespace-pre-wrap"><code lang="json" v-highlightjs="prettyJson(selectedError.request)"></code></pre>
                              </div>
                              <div v-else-if="routes.body === 'preview'" class="body-preview flex pt-2 overflow-x-auto">
                                <HtmlFormat :value="JSON.parse(selectedError.request)" />
                              </div>
                            </div>
                            <div class="bg-indigo-700 text-white px-3 py-3">
                              <div class="flex items-start justify-between space-x-3">
                                <h2 class="font-medium text-white">Error</h2>
                              </div>
                            </div>
                            <div class="p-2">
                              <ViewError :error="selectedError.error" />
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>
                  </form>
                </div>
              </div>
            </div>
          </div>
        </div>
        
      </div>
    </section>
    `,
    setup(props) {
        const store = inject('store')
        const routes = inject('routes')
        const server = inject('server')
        const client = inject('client')
        const {
            time,
            relativeTime,
            relativeTimeFromDate,
            relativeTimeFromMs,
        } = useFormatters()
        let take = 50
        const bottom = ref()
        const loadingMore = ref(false)
        let hasMore = true
        
        const q = ref(routes.q)
        const type = ref(routes.type ?? "ALL")
        const api = ref(new ApiResult())
        const commandTotals = ref([])
        
        function filterCommandTotals(results) {
            let to = results
            if (q.value) {
                to = to.filter(x => x.name.toLowerCase().includes(q.value.toLowerCase()))
            }
            if (type.value === 'API' || type.value === 'CMD') {
                to = to.filter(x => x.type === type.value)
            }
            if (routes.sort) {
                to.sort((a,b) => {
                    const desc = routes.sort.startsWith('-')
                    const by = desc ? routes.sort.substring(1) : routes.sort
                    const ret = a[by] > b[by] ? 1 : a[by] < b[by] ? -1 : 0
                    return desc ? ret * -1 : ret
                })
            }
            return to
        }
        
        function update() {
            commandTotals.value = filterCommandTotals(api.value?.response?.commandTotals ?? [])
        }
        watch(() => routes.sort, update)
        watch(() => routes.type, update)
        watch(() => routes.q, update)
        const tabs = { 'Summary':'', 'Errors':'errors', 'Latest':'latest', }
        const elChart = ref()
        
        const selectedRow = computed(() => 
            api.value?.response?.commandTotals?.find(x => x.type + '.' + x.name === routes.op))
        
        let chart = null
        function createChart(row) {
            if (!row) return
            const timings = row.timings
            if (chart) {
                chart.destroy()
                chart = null
            }
            chart = new Chart(elChart.value, {
                type: 'line',
                data: {
                    labels: [...Array(timings.length).keys()],
                    datasets: [{
                        label: 'timings',
                        data: timings,
                        borderWidth: 1,
                        borderColor: 'rgb(165 180 252)', //indigo-300
                        backgroundColor:'rgb(224 231 255)', //indigo-100
                        fill: true,
                        pointStyle:false,
                    }, {
                        label: 'average',
                        data: Array(timings.length).fill(row.averageMs),
                        fill: false,
                        borderColor: '#ef4444',
                        borderWidth: 1,
                        backgroundColor: '#fff',
                        pointStyle:false,
                    }, {
                        label: 'median',
                        data: Array(timings.length).fill(row.medianMs),
                        fill: false,
                        borderColor: '#f97316',
                        borderWidth: 1,
                        backgroundColor: '#fff',
                        pointStyle: false,
                    }]
                },
                options: {
                    plugins: {
                        title: {
                            display: true,
                            text: `Performance of ${row.name} ${row.type === 'CMD' ? 'Command' : 'API'}`,
                            font: {
                                size: 20,
                            }
                        },
                        subtitle: {
                            display: true,
                            text: 'in milliseconds',
                        },
                    },
                    scales: {
                        y: {
                            beginAtZero: true,
                        },
                        x: { 
                            display: false
                        },
                    }
                }
            })
        }
        
        async function refresh() {
            api.value = await client.api(new ViewCommands({ include:['StackTrace'], take }))
            if (api.value.response?.latestFailed) {
                let i = 0
                api.value.response?.latestFailed.forEach(x => x.id = `${++i}`)
            }
            commandTotals.value = filterCommandTotals(api.value?.response?.commandTotals ?? [])
            createChart(selectedRow.value)
        }
        /** @param {KeyboardEvent} e */
        function handleNav(e) {
            if (e.key === 'ArrowUp') {
                navNext(-1)
                e.preventDefault()
            } else if (e.key === 'ArrowDown') {
                navNext(1)
                e.preventDefault()
            }
        }
        
        function navNext(next) {
            if (routes.tab === 'errors') {
                const results = api.value?.response?.latestFailed ?? []
                const pos = results.findIndex(x => x === selectedError.value)
                if (pos !== -1) {
                    const nextPos = (pos + next) < 0 ? results.length - 1 : (pos + next) % results.length
                    const nextResult = results[nextPos]
                    routes.to({ show: nextResult.id })
                }
            } else {
                const results = commandTotals.value
                const pos = results.findIndex(x => x === selectedRow.value)
                if (pos !== -1) {
                    const nextPos = (pos + next) < 0 ? results.length - 1 : (pos + next) % results.length
                    const nextResult = results[nextPos]
                    routes.to({ op: nextResult.type + '.' + nextResult.name })
                }
            }
        }
        onMounted(async () => {
            await refresh()
            setTimeout(initObserver, 1000)
            document.addEventListener('keydown', handleNav)
        })
        onUnmounted(() => {
            try {
                observer?.unobserve()
            } catch (e) { console.log(e.message) }
            document.removeEventListener('keydown', handleNav)
        })
        
        watch(q, q => {
            routes.to({ q })
        })
        watch(type, type => {
            routes.to({ type })
        })
        watch(() => routes.op, op => {
            createChart(selectedRow.value)
        })
        function headerSelected(column) {
            //console.log('headerSelected',column)
        }
        function rowSelected(row) {
            const op = `${row.type}.${row.name}`
            routes.to({ op: routes.op === op ? null : op })
            //console.log('rowSelected', row)
            createChart(selectedRow.value)
        }
        async function loadMore() {
            const shouldLoad = routes.tab === 'latest' || routes.tab === 'errors'
            console.log('load more...', hasMore, shouldLoad)
            if (shouldLoad && hasMore) {
                take += 50
                loadingMore.value = true
                await refresh()
                loadingMore.value = false
                if (api.value.succeeded) {
                    const results = api.value.response?.latestCommands || []
                    hasMore = results.length === take
                }
            }
        }
        let observer = null
        function initObserver() {
            if (!bottom.value) return
            observer = new IntersectionObserver(
                ([{ isIntersecting, target }]) => {
                    if (isIntersecting) loadMore()
                }, { threshold: 1.0 })
            observer.observe(bottom.value)
        }
        const selectedError = computed(() => 
            routes.tab === 'errors' && routes.show && api.value.response?.latestFailed?.find(x => x.id === routes.show))
        const selectedClean = computed(() => {
            let row = selectedError.value
            if (!row) return
            return row
        })
        function toggleError(row) { 
            routes.to({ show: routes.show === row.id ? '' : row.id }) 
        }
        function rowSelectedError(row) {
            const show = row.id
            routes.to({ show: routes.show === show ? null : show })
            //console.log('rowSelected', row)
            createChart(selectedRow.value)
        }
        
        function altError(error) {
            return [
                error.errorCode, 
                error.message,
                error.stackTrace,
                error.errors?.length > 0 ? '\n' + error.errors?.map(x => `  - ${x.errorCode}: ${x.message}`).join('\n') : null,
            ].filter(x => !!x).join('\n')
        }
        return { 
            routes, api, commandTotals, tabs, elChart, bottom, loadingMore, q, type, 
            refresh, headerSelected, rowSelected,
            selectedError, selectedClean, prettyJson, toggleError, rowSelectedError,
            toDate, time, altError,
        }
    }
}
