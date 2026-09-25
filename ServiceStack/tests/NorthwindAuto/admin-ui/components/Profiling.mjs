import { computed, inject, onMounted, onUnmounted, ref, watch } from "vue"
import {
    ApiResult, map, apiValueFmt, humanize, toPascalCase, fromXsdDuration, toCamelCase, lastRightPart, toDate
} from "@servicestack/client"
import { useClient } from "@servicestack/vue"
import { keydown } from "app"
import { AdminProfiling } from "dtos"
import { prettyJson, hasItems } from "core"
export const Profiling = {
    template:`
<section v-if="!plugin">
  <div class="max-w-3xl">
    <Alert type="info">Admin Profiling UI is not enabled</Alert>
    <div class="my-4">
      <div>
        <p>
            The <b>ProfilingFeature</b> plugin needs to be configured with your App
            <a href="https://docs.servicestack.net/admin-ui-profiling" class="ml-2 whitespace-nowrap font-medium text-blue-700 hover:text-blue-600" target="_blank">
               Learn more <span aria-hidden="true">&rarr;</span>
            </a>
        </p>
      </div>
    </div>
    <div>
        <p class="text-sm text-gray-700 my-2">Quick start:</p>
        <CopyLine text="npx add-in profiling" />
    </div>
  </div>
</section>
<div v-else>
    <div class="mb-3 flex flex-wrap items-center gap-2">
        <label class="text-sm text-gray-700" for="profiling-trace-id">Trace Id</label>
        <input id="profiling-trace-id" type="search" :value="routes.traceId || ''"
               @input="onTraceFilterInput($event.target.value)"
               @change="setTraceFilter($event.target.value)" @keyup.enter="setTraceFilter($event.target.value)"
               placeholder="Trace Id" aria-label="Filter by Trace Id"
               class="h-9 w-72 rounded-md border border-gray-300 px-2 font-mono text-xs" />
        <button v-href="href({ withErrors:hasErrors ? '' : true })" type="button" :aria-pressed="hasErrors"
                :class="['inline-flex h-9 items-center gap-x-1.5 rounded-md px-3 text-sm font-medium shadow-sm ring-1 ring-inset focus:outline-none focus:ring-2 focus:ring-indigo-500',
                    hasErrors ? 'bg-red-50 text-red-700 ring-red-300 hover:bg-red-100' : 'bg-white text-gray-700 ring-gray-300 hover:bg-gray-50']">
            <span :class="['h-2 w-2 rounded-full', hasErrors ? 'bg-red-500' : 'bg-gray-300']" aria-hidden="true"></span>
            Has Errors
        </button>
        <span class="isolate inline-flex rounded-md shadow-sm">
          <button type="button" :class="[canPrev ? 'text-gray-600 hover:bg-gray-50 hover:text-gray-900' : 'text-gray-300 cursor-default',
            'relative inline-flex h-9 items-center rounded-l-md bg-white px-2 ring-1 ring-inset ring-gray-300 focus:z-10 focus:outline-none focus:ring-2 focus:ring-indigo-500']"
                  title="Previous page" :disabled="!canPrev" v-href="{ skip:nextSkip(-take) }">
            <svg class="w-5 h-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path d="M15.41 7.41L14 6l-6 6l6 6l1.41-1.41L10.83 12z" fill="currentColor"/></svg>
          </button>
          <button type="button" :class="[canNext ? 'text-gray-600 hover:bg-gray-50 hover:text-gray-900' : 'text-gray-300 cursor-default',
                '-ml-px relative inline-flex h-9 items-center rounded-r-md bg-white px-2 ring-1 ring-inset ring-gray-300 focus:z-10 focus:outline-none focus:ring-2 focus:ring-indigo-500']"
                  title="Next page" :disabled="!canNext" v-href="{ skip:nextSkip(take) }">
            <svg class="w-5 h-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path d="M10 6L8.59 7.41L13.17 12l-4.58 4.59L10 18l6-6z" fill="currentColor"/></svg>
          </button>
        </span>
        <button type="button" @click="update" title="Refresh" class="inline-flex h-9 items-center rounded-md bg-white px-2.5 text-sm font-medium text-gray-600 shadow-sm ring-1 ring-inset ring-gray-300 hover:bg-gray-50 hover:text-gray-900 focus:outline-none focus:ring-2 focus:ring-indigo-500">
            <svg class="w-4 h-4" xmlns="http://www.w3.org/2000/svg" aria-hidden="true" viewBox="0 0 24 24"><g
                fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"><path
                d="M21.168 8A10.003 10.003 0 0 0 12 2c-5.185 0-9.45 3.947-9.95 9"/><path
                d="M17 8h4.4a.6.6 0 0 0 .6-.6V3M2.881 16c1.544 3.532 5.068 6 9.168 6c5.186 0 9.45-3.947 9.951-9"/><path
                d="M7.05 16h-4.4a.6.6 0 0 0-.6.6V21"/></g></svg>
        </button>
        <button v-if="hasFilters" type="button" @click="clearFilters" title="Reset Filters" class="inline-flex h-9 items-center rounded-md bg-white px-2.5 text-sm font-medium text-gray-600 shadow-sm ring-1 ring-inset ring-gray-300 hover:bg-gray-50 hover:text-gray-900 focus:outline-none focus:ring-2 focus:ring-indigo-500 gap-x-1.5">
            <svg class="w-4 h-4" xmlns="http://www.w3.org/2000/svg" aria-hidden="true" viewBox="0 0 24 24">
              <path fill="currentColor" d="M6.78 2.72a.75.75 0 0 1 0 1.06L4.56 6h8.69a7.75 7.75 0 1 1-7.75 7.75a.75.75 0 0 1 1.5 0a6.25 6.25 0 1 0 6.25-6.25H4.56l2.22 2.22a.75.75 0 1 1-1.06 1.06l-3.5-3.5a.75.75 0 0 1 0-1.06l3.5-3.5a.75.75 0 0 1 1.06 0Z"/>
            </svg>
            Reset
        </button>
    </div>
    <div v-if="routes.traceId" class="mb-4 border-b border-gray-200" role="tablist" aria-label="Profiling results view">
      <button type="button" role="tab" id="profiling-details-tab" aria-controls="profiling-details-panel"
              :aria-selected="activeView === 'details'" @click="setView('details')"
              :class="[activeView === 'details' ? 'border-indigo-500 text-indigo-600' : 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700', 'mr-6 border-b-2 px-1 py-2 text-sm font-medium']">
        Details <span class="text-xs">({{ total ?? results.length }})</span>
      </button>
      <button type="button" role="tab" id="profiling-trace-tab" aria-controls="profiling-trace-panel"
              :aria-selected="activeView === 'trace'" @click="setView('trace')"
              :class="[activeView === 'trace' ? 'border-indigo-500 text-indigo-600' : 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700', 'border-b-2 px-1 py-2 text-sm font-medium']">
        Trace view
      </button>
    </div>
    <section v-if="routes.traceId && activeView === 'trace'" id="profiling-trace-panel" role="tabpanel" aria-labelledby="profiling-trace-tab"
             class="mb-4 rounded-md border border-gray-200 bg-white p-3" aria-label="Trace view">
      <div class="flex flex-wrap items-center gap-2 text-sm">
        <strong>Trace view</strong>
        <a v-href="href({ traceId:routes.traceId, skip:'' })" class="font-mono text-blue-600 hover:underline">{{ routes.traceId }}</a>
        <a v-if="externalTraceUrl" :href="externalTraceUrl" target="_blank" rel="noopener noreferrer"
           class="text-blue-600 hover:underline">Open in trace backend</a>
      </div>
      <p v-if="total > results.length" class="mt-2 text-sm text-gray-600">
        Showing {{ results.length }} of {{ total }} events.
        <button type="button" @click="loadAllEvents" class="text-blue-600 hover:underline">Load all</button>
      </p>
      <p v-if="!traceRows.length" class="mt-2 text-sm text-gray-600">No locally retained events for this trace.</p>
      <p v-if="hasMissingParent" class="mt-2 text-xs text-gray-500">Some parent spans aren't in local profiling history. The trace backend may have the full hierarchy.</p>
      <ol v-if="traceRows.length" class="mt-2 divide-y divide-gray-100">
        <li v-for="row in traceRows" :key="row.id" class="py-2 text-sm" :style="{ paddingLeft: (row.depth * 16) + 'px' }">
          <div class="flex flex-wrap items-center gap-2">
            <span :class="row.error ? 'text-red-700' : 'text-gray-800'">{{ row.operation || row.message || row.eventType }}</span>
            <span class="text-gray-500">{{ row.source }} · {{ valueFmt(row.duration, 'duration') }}</span>
            <span v-if="row.error" class="text-red-700">error</span>
            <span v-else-if="row.pending" class="text-amber-700" title="No matching After event, the step is still running or never completed">pending</span>
          </div>
          <div v-if="row.spanId" class="flex items-center gap-1 text-xs text-gray-500">
            span <a v-href="href({ spanId:row.spanId, skip:'' })" class="font-mono text-blue-600 hover:underline">{{ row.spanId }}</a>
          </div>
        </li>
      </ol>
    </section>
    <section v-if="!routes.traceId || activeView === 'details'" id="profiling-details-panel" role="tabpanel" :aria-labelledby="routes.traceId ? 'profiling-details-tab' : undefined">
    <div class="flex flex-col">
      <div class="-my-2 overflow-x-auto sm:-mx-6 lg:-mx-8">
        <div class="py-2 align-middle inline-block sm:px-6 lg:px-8">
          <div v-if="results.length" class="md:shadow border-b border-gray-200 md:rounded-lg">
            <table class="divide-y divide-gray-200">
              <thead class="bg-gray-50">
              <tr>
                <th v-for="k in uniqueKeys"
                    v-href="{ orderBy:routes.orderBy === k ? ('-' + k) : routes.orderBy === ('-' + k) ? '' : k }"
                    class="cursor-pointer px-4 py-2.5 text-left text-xs font-semibold text-gray-600 tracking-wide whitespace-nowrap">
                  <div class="flex">
                    <span class="mr-1 select-none">{{ fieldLabels[k] || keyFmt(k) }}</span>
                    <svg class="w-4 h-4" v-if="routes.orderBy===k" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20">
                      <g fill="none">
                        <path d="M8.998 4.71L6.354 7.354a.5.5 0 1 1-.708-.707L9.115 3.18A.499.499 0 0 1 9.498 3H9.5a.5.5 0 0 1 .354.147l.01.01l3.49 3.49a.5.5 0 1 1-.707.707l-2.65-2.649V16.5a.5.5 0 0 1-1 0V4.71z" fill="currentColor"/>
                      </g>
                    </svg>
                    <svg class="w-4 h-4" v-else-if="routes.orderBy===('-' + k)" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20">
                      <g fill="none">
                        <path d="M10.002 15.29l2.645-2.644a.5.5 0 0 1 .707.707L9.886 16.82a.5.5 0 0 1-.384.179h-.001a.5.5 0 0 1-.354-.147l-.01-.01l-3.49-3.49a.5.5 0 1 1 .707-.707l2.648 2.649V3.5a.5.5 0 0 1 1 0v11.79z" fill="currentColor"/>
                      </g>
                    </svg>
                    <span v-else class="w-4 h-4"></span>
                  </div>
                </th>
              </tr>
              </thead>
              <tbody>
              <tr v-for="(row,index) in results" :key="row.id" @click="toggle(row)"
                  :class="['cursor-pointer', expanded(row.id) ? 'bg-indigo-50' : statusBackground(row.error,index) + ' hover:bg-gray-100']">
                <td v-for="k in uniqueKeys" :key="k" class="px-4 py-2.5 whitespace-nowrap text-sm text-gray-700">
                  <a v-if="row[k] && ['traceId','spanId'].includes(k)"
                     v-href="identifierHref(k, row[k])" @click.stop
                     :title="row[k]" class="text-blue-600 hover:underline">{{ valueFmt(row[k], k) }}</a>
                  <span v-else :title="apiValueTitle(row[k],k)">{{ valueFmt(row[k], k) }}</span>
                </td>
              </tr>
              </tbody>
            </table>
          </div>
          <div v-else-if="api && api.completed">
            <h3 class="p-2">No Results</h3>
          </div>
        </div>
      </div>
    </div>
    
    <div v-if="selected" class="relative z-20" aria-labelledby="slide-over-title" role="dialog" aria-modal="true">
      <div class="fixed overflow-hidden">
        <div class="absolute overflow-hidden">
          <div class="pointer-events-none fixed inset-y-0 right-0 flex max-w-full pl-10 sm:pl-16">
            <div class="pointer-events-auto w-screen max-w-2xl">
              <form v-if="selected" class="flex h-full flex-col overflow-y-scroll bg-white shadow-xl">
                <div class="flex-1">
                  <!-- Header -->
                  <div class="bg-gray-50 px-4 py-5 sm:px-6">
                    <div class="flex items-start gap-4">
                      <div class="min-w-0 flex-1">
                        <h2 id="slide-over-title" :class="['break-words text-lg font-semibold leading-6', statusColor(selected.error)]"
                            :title="selected.message || valueFmt(selected.eventType,'eventType')">
                          {{ msgFmt(selected.message || valueFmt(selected.eventType, 'eventType')) }}
                        </h2>
                        <div class="mt-1 flex flex-wrap items-center gap-x-1.5 text-sm text-gray-600">
                          <a v-href="href({ source:selected.source })" class="text-blue-600 hover:text-blue-800">{{ selected.source }}</a>
                          <span aria-hidden="true">·</span>
                          <a v-href="href({ eventType:selected.eventType })" class="text-blue-600 hover:text-blue-800"
                             :title="selected.eventType">{{ valueFmt(selected.eventType, 'eventType') }}</a>
                        </div>
                      </div>
                      <CloseButton @close="toggle(selected)" button-class="shrink-0 bg-gray-50" />
                    </div>
                    <dl v-if="selected.traceId || selected.spanId || selected.threadId || selected.duration || selected.date"
                        class="mt-4 grid grid-cols-2 gap-x-4 gap-y-3 text-sm sm:grid-cols-4">
                      <div v-if="selected.traceId" class="min-w-0">
                        <dt class="text-xs text-gray-500">Trace Id</dt>
                        <dd class="mt-0.5"><a v-href="href({ traceId:selected.traceId, skip:'' })" :title="selected.traceId"
                                            class="font-mono text-blue-600 hover:underline">{{ shortId(selected.traceId) }}</a></dd>
                      </div>
                      <div v-if="selected.spanId" class="min-w-0">
                        <dt class="text-xs text-gray-500">Span Id</dt>
                        <dd class="mt-0.5"><a v-href="href({ spanId:selected.spanId, skip:'' })" :title="selected.spanId"
                                            class="font-mono text-blue-600 hover:underline">{{ shortId(selected.spanId) }}</a></dd>
                      </div>
                      <div v-if="selected.threadId" class="min-w-0">
                        <dt class="text-xs text-gray-500">Thread</dt>
                        <dd class="mt-0.5"><a v-href="href({ threadId:selected.threadId })" class="text-blue-600 hover:text-blue-800">{{ selected.threadId }}</a></dd>
                      </div>
                      <div v-if="selected.date" class="min-w-0">
                        <dt class="text-xs text-gray-500">Time</dt>
                        <dd class="mt-0.5 text-gray-700">{{ valueFmt(selected.date, 'date') }}</dd>
                      </div>
                      <div v-if="selected.duration" class="min-w-0">
                        <dt class="text-xs text-gray-500">Duration</dt>
                        <dd class="mt-0.5 text-gray-700">{{ valueFmt(selected.duration, 'duration') }}</dd>
                      </div>
                    </dl>
                  </div>
                  <!-- Divider container -->
                  <div class="space-y-6 py-6 sm:space-y-0 sm:divide-y sm:divide-gray-200 sm:py-0">
                    <dl v-if="selected.userAuthId || selectedSession"
                        class="space-y-3 border-b border-gray-200 px-4 py-4 text-sm text-gray-600 sm:px-6">
                        <div v-if="selected.userAuthId">
                          <dt class="text-xs text-gray-500">User Id</dt>
                          <dd class="mt-1 break-all"><a v-href="href({ userAuthId:selected.userAuthId })" class="text-blue-600 hover:underline">{{ selected.userAuthId }}</a></dd>
                        </div>
                        <div v-if="selectedSession">
                          <dt class="text-xs text-gray-500">Session Id</dt>
                          <dd class="mt-1 break-all"><a v-href="href({ sessionId:selectedSession.value })" class="text-blue-600 hover:underline">{{ selectedSession.value }}</a></dd>
                        </div>
                    </dl>
                    <div v-if="selected.tag" class="bg-indigo-700 text-white px-3 py-3">
                      <div class="flex items-start justify-between space-x-3">
                        <h2 class="font-medium text-white">{{ keyFmt(fieldLabels.tag || 'tag') }}</h2>
                      </div>
                    </div>
                    <div v-if="selected.tag" class="p-4">
                      <a v-if="isLinkable(selected.tag)" v-href="href({ tag:selected.tag })"
                         class="text-blue-600 hover:text-blue-800">
                        {{ selected.tag }}
                      </a>
                      <div v-else class="font-mono whitespace-pre">{{ selected.tag }}</div>
                    </div>
    
                    <div v-if="selected.command" class="bg-indigo-700 text-white px-3 py-3">
                      <div class="flex items-start justify-between space-x-3">
                        <h2 class="font-medium text-white">Command</h2>
                      </div>
                    </div>
                    <div v-if="selected.command" class="p-4">
                      {{ selected.command }}
                    </div>
    
                    <div v-if="selectedArgs" class="bg-indigo-700 text-white px-3 py-3">
                      <div class="flex items-start justify-between space-x-3">
                        <h2 class="font-medium text-white">Arguments</h2>
                      </div>
                    </div>
                    <div v-if="selectedArgs" class="flex overflow-auto">
                      <div class="p-2 relative w-full">
                        <span class="relative z-0 inline-flex shadow-sm rounded-md">
                          <a v-for="(tab,name) in {Pretty:'',Raw:'raw',Preview:'preview'}"
                             v-href="{ body:tab }"
                             :class="[{ Pretty:'rounded-l-md',Raw:'-ml-px',Preview:'rounded-r-md -ml-px' }[name], routes.body == tab ? 'z-10 outline-none ring-1 ring-indigo-500 border-indigo-500' : '', 'cursor-pointer relative inline-flex items-center px-4 py-1 border border-gray-300 bg-white text-sm font-medium text-gray-700 hover:bg-gray-50']">
                            {{ name }}
                          </a>
                        </span>
                        <div v-if="routes.body == ''" class="pt-2 icon-outer" style="min-height:2.5rem">
                          <CopyIcon class="absolute right-4" :text="prettyJson(selectedArgs)" />
                          <pre class="whitespace-pre-wrap"><code lang="json" v-highlightjs="prettyJson(selectedArgs)"></code></pre>
                        </div>
                        <div v-else-if="routes.body == 'raw'" class="flex pt-2">
                          <textarea class="flex-1" rows="10" v-html="JSON.stringify(selectedArgs)"></textarea>
                        </div>
                        <div v-else-if="routes.body == 'preview'" class="body-preview flex pt-2 overflow-x-auto">
                          <HtmlFormat :value="selectedArgs" />
                        </div>
                      </div>
                    </div>
    
                    <div v-if="selected.error" class="bg-indigo-700 text-white px-3 py-3">
                      <div class="flex items-start justify-between space-x-3">
                        <h2 class="font-medium text-white">Error</h2>
                      </div>
                    </div>
                    <div v-if="selected.error" class="flex overflow-auto">
                      <div class="p-2 relative w-full">
                        <div class="pt-2 icon-outer" style="min-height:2.5rem">
                          <CopyIcon class="absolute right-4" :text="prettyJson(selected.error)" />
                          <table>
                          <tbody>
                            <tr>
                              <th class="text-left font-medium align-top pr-2">Code</th>
                              <td>{{ selected.error.errorCode }}</td>
                            </tr>
                            <tr>
                              <th class="text-left font-medium align-top pr-2">Message</th>
                              <td>{{ selected.error.message }}</td>
                            </tr>
                            <tr>
                              <th class="text-left font-medium align-top pr-2">StackTrace</th>
                              <td>
                                <div class="whitespace-pre">{{selected.error.stackTrace }}</div>
                              </td>
                            </tr>
                            <tr v-if="hasItems(selected.error.errors)">
                              <th class="text-left font-medium align-top pr-2">Errors</th>
                              <td>
                                <HtmlFormat :value="selected.error.errors" />
                              </td>
                            </tr>
                          </tbody>
                          </table>
                        </div>
                      </div>
                    </div>
    
                    <div v-if="selected.stackTrace" class="bg-indigo-700 text-white px-3 py-3">
                      <div class="flex items-start justify-between space-x-3">
                        <h2 class="font-medium text-white">StackTrace</h2>
                      </div>
                    </div>
                    <div v-if="selected.stackTrace" class="pt-4 font-mono whitespace-pre">{{ selected.stackTrace }}</div>
    
                    <div v-if="hasItems(selected.meta)" class="bg-indigo-700 text-white px-3 py-3">
                      <div class="flex items-start justify-between space-x-3">
                        <h2 class="font-medium text-white">Meta</h2>
                      </div>
                    </div>
                    <div v-if="hasItems(selected.meta)" class="flex overflow-auto">
                      <table>
                        <tr v-for="(value,key) in selected.meta">
                          <th class="text-left font-medium align-top py-2 px-4 whitespace-nowrap">
                            <div class=" whitespace-nowrap w-[9em] overflow-hidden" :title="key">
                              {{ key }}
                            </div>
                          </th>
                          <td class="align-top py-2 px-4">
                            {{ value }}
                          </td>
                        </tr>
                      </table>
                    </div>
    
                  </div>
    
                </div>
              </form>
            </div>
          </div>
        </div>
      </div>
    </div>
    </section>
</div>
    `,
    setup() {
        const routes = inject('routes')
        const activeView = computed(() => routes.view === 'trace' ? 'trace' : 'details')
        function setView(view) {
            routes.to({ view: view === 'details' ? '' : view })
        }
        const loadAll = ref(false)
        watch(() => routes.traceId, () => { loadAll.value = false })
        const server = inject('server')
        const client = useClient()
        let plugin = server.plugins.profiling
        let summaryFields = server.plugins.profiling.summaryFields.map(toCamelCase)
        let linkFields = 'id,traceId,spanId,source,eventType,operation,threadId,commandType,userAuthId,sessionId,withErrors,tag,skip'.split(',')
        let fieldLabels = { eventType:'Event', threadId:'Thread', userAuthId:'User Id', date:'Time', traceId:'Trace Id' }
        if (plugin.tagLabel)
            fieldLabels.tag = plugin.tagLabel
        let timeFmt = new Intl.DateTimeFormat('en-US', {hour:'numeric',minute:'numeric',second:'numeric',fractionalSecondDigits:3,hour12:false})
        let showTitle = 'traceId,eventType,duration,timestamp'.split(',')
        /** @type {Ref<ApiResult<AdminProfilingResponse>>} */
        const api = ref(new ApiResult())
        
        async function update() {
            let request = new AdminProfiling()
            if (routes.orderBy)
                request.orderBy = routes.orderBy
            linkFields.forEach(x => {
                if (routes[x]) request[x] = routes[x]
            })
            // route values from the URL are strings
            request.withErrors = hasErrors.value || undefined
            if (loadAll.value && routes.traceId) {
                request.skip = 0
                request.take = Math.max(total.value || 0, results.value.length)
            }
            api.value = await client.api(request, { jsconfig: 'eccn' })
        }
        const errorSummary = computed(() => api.value.summaryMessage())
        const hasErrors = computed(() => routes.withErrors === true || routes.withErrors === 'true')
        /** @type {ComputedRef<DiagnosticEntry[]>} */
        const results = computed(() => api.value.response?.results || [])
        const externalTraceUrl = computed(() => api.value.response?.externalTraceUrl)
        // Before events start a step, After/Error events complete it with its duration
        const isStartEvent = eventType => /Before$/.test(eventType || '') || /\.Request$/.test(eventType || '')
        const opId = x => x.operationId && !/^[0-]+$/.test(x.operationId) ? x.operationId : null
        const traceRows = computed(() => {
            const completed = new Set(results.value.filter(x => !isStartEvent(x.eventType) && opId(x)).map(opId))
            // One row per step: its After or Error event, or its Before event if the step never completed
            const rows = results.value
                .filter(x => !isStartEvent(x.eventType) || !opId(x) || !completed.has(opId(x)))
                .map(x => isStartEvent(x.eventType) ? { ...x, pending:true } : x)
                .sort((a,b) => new Date(a.date) - new Date(b.date))
            const spans = new Map(rows.filter(x => x.spanId).map(x => [x.spanId, x]))
            return rows.map(row => {
                const seen = new Set([row.spanId])
                let depth = 0, parent = row.parentSpanId
                while (parent && spans.has(parent) && !seen.has(parent) && depth < 8) {
                    seen.add(parent)
                    depth++
                    parent = spans.get(parent).parentSpanId
                }
                return { ...row, depth, missingParent: !!row.parentSpanId && !spans.has(row.parentSpanId) }
            })
        })
        const hasMissingParent = computed(() => traceRows.value.some(row => row.missingParent))
        const total = computed(() => api.value.response?.total)
        function loadAllEvents() {
            loadAll.value = true
            if (routes.skip) routes.to({ skip:'' })
            else update()
        }
        const uniqueKeys = summaryFields
        const selected = computed(() => routes.show && results.value.find(x => x.id == routes.show))
        const selectedArgs = computed(() => {
            let namedArgs = selected.value?.namedArgs
            let args = selected.value?.args
            return hasItems(namedArgs)
                ? namedArgs
                : hasItems(args)
                    ? args
                    : selected.value?.arg
        })
        
        function valueFmt(obj, k) {
            if (obj == null) return ''
            if (k === 'traceId' || k === 'spanId') return shortId(obj)
            if (k === 'eventType') {
                let evt = lastRightPart(obj, '.')
                if (evt.startsWith('Write')) {
                    evt = evt.substring('Write'.length)
                }
                return evt
            }
            if (k === 'date') {
                let d = toDate(obj)
                return timeFmt.format(d)
            }
            if (k === 'timestamp') {
                let d = new Date(obj / 10)
                return timeFmt.format(d)
            }
            return typeof obj === 'string' && obj.startsWith('PT')
                ? fromXsdDuration(obj)
                : apiValueFmt(obj)
        }
        function keyFmt(t) {
            return humanize(toPascalCase(t))
        }
        function shortId(id) {
            return id?.length > 8 ? id.slice(-8) : id
        }
        function msgFmt(s) {
            let size = 30
            return !s || s.length < size
                ? s
                : s[0] === '/' || s.indexOf('http://') >= 0 || s.indexOf('https://') >= 0
                    ? '...' + s.substring(s.length - size)
                    : s.substring(0, Math.min(size, s.length - size)) + '...'
        }
        function hasFilters() {
            for (let i=0; i<linkFields.length; i++) {
                let x = linkFields[i]
                if (routes[x])
                    return true
            }
            return false
        }
        const selectedSession = computed(() => map(selected.value?.sessionId, x => ({ key:'ss-id', value: x })))
        function href(links) {
            return Object.assign({ show:'', view:'' }, linkFields.reduce((acc,x) => { acc[x] = ''; return acc }, {}), links)
        }
        function identifierHref(key, value) {
            return href({ [key]:value, skip:'' })
        }
        function setTraceFilter(value) {
            const traceId = value.trim()
            if (traceId === (routes.traceId || '')) return
            routes.to(traceId
                ? href({ traceId:traceId, skip:'' })
                : { traceId:'', skip:'', show:'' })
        }
        function clearFilters() {
            routes.to(href({show:''}))
        }
        const take = ref(plugin.defaultLimit)
        const canPrev = computed(() => routes.skip > 0)
        const canNext = computed(() => results.value.length >= take.value)
        function nextSkip(skip) {
            skip += (parseInt(routes.skip, 10) || 0)
            if (typeof total.value == 'number') {
                const lastPage = Math.floor(total.value / take.value) * take.value
                if (skip > lastPage) return lastPage
            }
            if (skip < 0) return 0
            return skip
        }
        function handleKeyDown(e) {
            keydown(e, { canPrev, canNext, nextSkip, take, results, selected, clearFilters })
        }
        let sub = null
        onMounted(async () => {
            document.addEventListener('keydown', handleKeyDown)
            sub = app.subscribe('route:nav', update)
            await update()
        })
        onUnmounted(() => {
            document.removeEventListener('keydown', handleKeyDown)
            app.unsubscribe(sub)
        })
        
        return {
            hasErrors,
            plugin,
            routes,
            activeView,
            setView,
            loadAllEvents,
            api,
            prettyJson,
            fieldLabels,
            linkFields,
            take,
            update,
            hasItems,
            selectedArgs,
            errorSummary,
            isLinkable(s) {
                return s.indexOf('{') < 0 && s.indexOf('[') && s.indexOf('"') && s.indexOf("'") < 0 && s.length < 100
            },
            apiValueTitle(obj,k) {
                if (showTitle.indexOf(k) >= 0) {
                    return obj
                }
                return ''
            },
            valueFmt,
            results,
            traceRows,
            hasMissingParent,
            externalTraceUrl,
            setTraceFilter,
            onTraceFilterInput(value) { if (!value.trim()) setTraceFilter('') },
            total,
            uniqueKeys,
            keyFmt,
            shortId,
            msgFmt,
            hasFilters,
            selected,
            toggle(row) {
                routes.to({ show: routes.show === row.id ? '' : row.id })
            },
            expanded(id) { return selected.value?.id === id },
            statusColor(error) {
                return error ? 'text-red-700' : 'text-gray-700'
            },
            statusBackground(error,index) {
                return !error
                    ? (index % 2 === 0 ? 'bg-white' : 'bg-gray-50/60')
                    : 'bg-red-100'
            },
            selectedSession,
            href,
            identifierHref,
            clearFilters,
            keydown,
            canPrev,
            canNext,
            nextSkip,
        }
    }
}
