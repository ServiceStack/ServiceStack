import { computed, inject, onMounted, onUnmounted, ref, watch, getCurrentInstance } from "vue"
import { useClient, useFormatters, useMetadata, useUtils } from "@servicestack/vue"
import { ApiResult, toDate, humanify } from "@servicestack/client"
import { prettyJson } from "core"
import { ViewCommands, ExecuteCommand } from "dtos"
import { Chart, registerables } from 'chart.js'
Chart.register(...registerables)
export const Commands = {
    template:`
    <section v-if="!plugin">
      <div class="p-4 max-w-3xl">
        <Alert type="info">Admin Commands UI is not enabled</Alert>
        <div class="my-4">
          <div>
            <p>
                The <b>CommandsFeature</b> plugin needs to be configured with your App
                <a href="https://docs.servicestack.net/commands#command-admin-ui" class="ml-2 whitespace-nowrap font-medium text-blue-700 hover:text-blue-600" target="_blank">
                   Learn more <span aria-hidden="true">&rarr;</span>
                </a>
            </p>
          </div>
        </div>
      </div>
    </section>
    <section v-else>
      <div>
        <div class="sm:hidden">
          <label for="redis-tabs" class="sr-only">Select a tab</label>
          <select id="redis-tabs"
                  class="block w-full pl-3 pr-10 py-2 text-base border-gray-300 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm rounded-md"
                  @change="routes.to({ tab: $event.target.value, type:'' })">
            <option v-for="(tab,name) in tabs" :selected="routes.tab === tab" :value="tab">{{ name }}</option>
          </select>
        </div>
        <div class="hidden sm:block">
          <div class="border-b border-gray-200">
            <nav class="-mb-px flex space-x-8" aria-label="Tabs">
              <a v-for="(tab,name) in tabs" v-href="{ tab, op:'', show:'', body:'', type:'' }"
                 :class="[routes.tab === tab ? 'border-indigo-500 text-indigo-600' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300', 'whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm']">
                {{ name }}
              </a>
            </nav>
          </div>
        </div>
      </div>
      <ErrorSummary :status="api.error" />
        
      <div v-if="api.response">
        <div v-if="routes.tab === 'explore'" class="flex">
          <div class="w-64 mt-2">
            <div class="relative">
              <svg class="absolute ml-2.5 mt-2 h-4 w-4 text-gray-500" fill="currentColor" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                <path d="M16.32 14.9l5.39 5.4a1 1 0 0 1-1.42 1.4l-5.38-5.38a8 8 0 1 1 1.41-1.41zM10 16a6 6 0 1 0 0-12 6 6 0 0 0 0 12z"></path>
              </svg>
              <input type="search" placeholder="Filter..." v-model="q" class="border rounded-full overflow-hidden flex w-full px-4 pl-8 border-gray-200">
            </div>
            <nav class="w-64 space-y-1 bg-white pb-4 md:pb-scroll" aria-label="Sidebar">
              <div v-for="nav in filteredNav" class="space-y-1">
                <button v-if="nav.tag" type="button" @click.prevent="toggleNav(nav.tag)"
                        class="bg-white text-gray-600 hover:bg-gray-50 hover:text-gray-900 group w-full flex items-center pr-2 py-2 text-left text-sm font-medium">
                  <svg :class="[nav.expanded ? 'text-gray-400 rotate-90' : 'text-gray-300','mr-2 flex-shrink-0 h-5 w-5 transform group-hover:text-gray-400 transition-colors ease-in-out duration-150']" viewBox="0 0 20 20" aria-hidden="true">
                    <path d="M6 6L14 10L6 14V6Z" fill="currentColor" />
                  </svg>
                  {{nav.tag}}
                </button>
                <div v-if="nav.expanded" class="space-y-1">
                  <a v-for="op in nav.commands" v-href="{ op:op.name, type:'' }"
                     :class="[op.name === routes.op ? 'bg-indigo-50 border-indigo-600 text-indigo-600' : 
                        'border-transparent text-gray-600 hover:text-gray-900 hover:bg-gray-50', 'border-l-4 group w-full flex justify-between items-center pl-10 pr-2 py-2 text-sm font-medium']">
                    <span class="nav-item flex-grow">{{op.name}}</span>
                  </a>
                </div>
              </div>
            </nav>
          </div>
          <div v-if="routes.op" class="flex-grow p-4">
            <form ref="elForm" @submit.prevent="submitForm($event.target)" autocomplete="off"
                class="shadow sm:rounded-md max-w-screen-md">
              <input type="submit" class="hidden">
              <AutoFormFields v-if="formLayout.length" :type="routes.op"
                              :metaType="metaType" :formLayout="formLayout" v-model="model"
                              class="sm:m-4 max-w-4xl" />
              <div class="mt-4 px-4 py-3 bg-gray-50 dark:bg-gray-900 sm:px-6 flex flex-wrap justify-between">
                <div>
                  <FormLoading v-if="false" />
                </div>
                <div class="flex justify-end">
                  <PrimaryButton class="ml-4">Submit</PrimaryButton>
                </div>
              </div>
            </form>
            <div v-if="commandApi.response || commandApi.error" class="mt-2 p-2">
                <ErrorSummary v-if="commandApi.error ?? commandApi.response?.commandResult?.error" :status="commandApi.error ?? commandApi.response?.commandResult?.error" />
                <div v-else>
                    <div v-if="commandApi.response?.commandResult" class="mb-4">
                      <p>Completed in <b>{{commandApi.response?.commandResult.ms}}ms</b></p>
                    </div>
                    <span class="relative z-0 inline-flex shadow-sm rounded-md">
                        <a v-for="(tab,name) in {Pretty:'',Preview:'preview'}" @click="routes.body = tab"
                        :class="[{ Pretty:'rounded-l-md',Preview:'rounded-r-md -ml-px' }[name], routes.body === tab ? 'z-10 outline-none ring-1 ring-indigo-500 border-indigo-500' : '', 'cursor-pointer relative inline-flex items-center px-4 py-1 border border-gray-300 bg-white text-sm font-medium text-gray-700 hover:bg-gray-50']">
                        {{name}}
                        </a>
                    </span>
                    <div v-if="routes.body === ''" class="pt-2">
                        <CopyIcon v-if="commandJson" :text="commandJson" class="absolute right-4" />
                        <pre class="whitespace-pre-wrap"><code lang="json" v-highlightjs="commandJson"></code></pre>
                    </div>
                    <div v-else-if="routes.body === 'preview'" class="body-preview flex pt-2 overflow-x-auto">
                        <HtmlFormat :value="JSON.parse(commandJson)" :fieldAttrs="fieldAttrs" />
                    </div>
                </div>
            </div>
        </div>
          
        </div>
        <div v-else-if="routes.tab === 'latest'" class="flex">
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
          <div class="mt-2 flex flex-wrap items-center">
            <div>
              <button type="button" @click="refresh()" title="Refresh" class="inline-flex items-center px-2.5 py-1.5 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
                <svg class="w-5 h-5" xmlns="http://www.w3.org/2000/svg" aria-hidden="true" viewBox="0 0 24 24"><g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5">
                  <path d="M21.168 8A10.003 10.003 0 0 0 12 2c-5.185 0-9.45 3.947-9.95 9"></path><path d="M17 8h4.4a.6.6 0 0 0 .6-.6V3M2.881 16c1.544 3.532 5.068 6 9.168 6c5.186 0 9.45-3.947 9.951-9"></path><path d="M7.05 16h-4.4a.6.6 0 0 0-.6.6V21"></path></g>
                </svg>
              </button>
            </div>
            <div class="ml-2">
                <span class="inline-flex">
                  <label for="message-type" class="sr-only">Select Type</label>
                  <select id="message-type" v-model="type" class="mt-1 block w-full pl-3 pr-10 py-2 text-base focus:outline-none sm:text-sm rounded-md dark:text-white dark:bg-gray-900 dark:border-gray-600 disabled:bg-slate-50 disabled:text-slate-500 disabled:border-slate-200 disabled:shadow-none shadow-sm border-gray-300 text-gray-900 focus:ring-indigo-500 focus:border-indigo-500">
                    <option value="ALL">All</option>
                    <option value="API">APIs</option>
                    <option value="CMD">Commands</option>
                  </select>
                </span>
            </div>
            <div class="ml-2">
              <div class="relative">
                <svg class="absolute ml-2.5 mt-2 h-4 w-4 text-gray-500" fill="currentColor" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                  <path d="M16.32 14.9l5.39 5.4a1 1 0 0 1-1.42 1.4l-5.38-5.38a8 8 0 1 1 1.41-1.41zM10 16a6 6 0 1 0 0-12 6 6 0 0 0 0 12z"></path>
                </svg>
                <input type="search" placeholder="Filter..." v-model="q" class="border shadow-sm rounded-full overflow-hidden flex w-full px-4 pl-8 border-gray-200">
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
        const plugin = server.plugins.commands
        const {
            time,
            relativeTime,
            relativeTimeFromDate,
            relativeTimeFromMs,
        } = useFormatters()
        const { createFormLayout } = useMetadata()
        let take = 50
        const bottom = ref()
        const loadingMore = ref(false)
        let hasMore = true
        
        const q = ref(routes.q)
        const type = ref(routes.type ?? "ALL")
        const api = ref(new ApiResult())
        const commandTotals = ref([])
        
        const commandApi = ref(new ApiResult())
        const commandJson = computed(() => {
            try {
                const result = commandApi.value?.response?.result 
                const json = JSON.parse(result)
                return JSON.stringify(json, null, 4)
            } catch (e) {
                return null
            }
        })
        function fieldAttrs(id) {
            let useId = id.replace(/\s+/g,'').toLowerCase()
            return useId === 'stacktrace'
                ? { 'class': 'whitespace-pre overflow-x-auto' }
                : {}
        }
        const model = ref({})
        const commandInfos = plugin.commands
        const navs = ref(Array.from(new Set(commandInfos.map(x => x.tag ?? 'other')))
            .map(x => ({
                tag: x,
                expanded: true,
                commands: commandInfos.filter(c => c.tag === x || (x === 'other' && !c.tag))
            })))
        
        const filteredNav = computed(() => navs.value.map(x => ({
            ...x,
            commands: x.commands.filter(c => c.name.toLowerCase().includes(q.value.toLowerCase()))
        })).filter(x => x.commands.length > 0))
        function toggleNav(tag) {
            const nav = navs.value.find(x => x.tag === tag)
            if (nav) nav.expanded = !nav.expanded
        }
        
        function getFormLayout(metaType) {
            return !metaType
                ? null
                : metaType.formLayout ?? createFormLayout(metaType)
        }
        const metaType = computed(() => commandInfos.find(x => x.name === routes.op)?.request)
        const formLayout = computed(() => metaType.value 
            ? getFormLayout(metaType.value) 
            : null)
        
        async function submitForm(e) {
            commandApi.value = await client.api(new ExecuteCommand({
                command: routes.op,
                requestJson: JSON.stringify(model.value)
            }))
        }
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
            commandApi.value = new ApiResult()
            commandTotals.value = filterCommandTotals(api.value?.response?.commandTotals ?? [])
        }
        watch(() => routes.sort, update)
        watch(() => routes.type, update)
        watch(() => routes.q, update)
        watch(() => routes.op, update)
        watch(() => routes.tab, refresh)
        const tabs = { 'Summary':'', 'Explore':'explore', 'Latest':'latest', 'Errors':'errors', }
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
            console.debug('load more...', hasMore, shouldLoad)
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
            plugin, routes, api, commandTotals, tabs, elChart, bottom, loadingMore, q, type, 
            refresh, headerSelected, rowSelected,
            selectedError, selectedClean, prettyJson, toggleError, rowSelectedError,
            toDate, time, altError,
            filteredNav, toggleNav,
            model, metaType, formLayout, commandApi, commandJson, submitForm, fieldAttrs,
        }
    }
}
