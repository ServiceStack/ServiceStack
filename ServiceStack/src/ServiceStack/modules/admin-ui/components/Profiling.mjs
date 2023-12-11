import { computed, inject, onMounted, onUnmounted, ref } from "vue"
import {
    ApiResult, map, apiValueFmt, humanize, toPascalCase, fromXsdDuration, toCamelCase, lastRightPart, toDate
} from "@servicestack/client"
import { useClient } from "@servicestack/vue"
import { keydown } from "app"
import { AdminProfiling } from "dtos"
import { prettyJson, hasItems } from "core"
export const Profiling = {
    template:/*html*/`
<div class="mb-2 flex flex-wrap">
<span class="relative z-0 inline-flex shadow-sm rounded-md">
      <button v-href="href({ withErrors:!routes.withErrors })" type="button"
              class="relative inline-flex items-center px-4 py-2 rounded-md border border-gray-300 bg-white text-sm font-medium text-gray-700 hover:bg-gray-50 focus:z-10 focus:outline-none focus:ring-1 focus:ring-indigo-500 focus:border-indigo-500">
          Has Errors
      </button>
    </span>
<div v-if="hasFilters" class="px-2">
  <button type="button" @click="clearFilters" title="Reset Filters"
          class="inline-flex items-center px-2.5 py-1.5 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
    <svg class="w-6 h-6 p-0.5" xmlns="http://www.w3.org/2000/svg" aria-hidden="true" viewBox="0 0 24 24">
      <path fill="currentColor" d="M6.78 2.72a.75.75 0 0 1 0 1.06L4.56 6h8.69a7.75 7.75 0 1 1-7.75 7.75a.75.75 0 0 1 1.5 0a6.25 6.25 0 1 0 6.25-6.25H4.56l2.22 2.22a.75.75 0 1 1-1.06 1.06l-3.5-3.5a.75.75 0 0 1 0-1.06l3.5-3.5a.75.75 0 0 1 1.06 0Z"/>
    </svg>
  </button>
</div>
<span class="relative z-0 inline-flex shadow-sm rounded-md">
      <button type="button" :class="[canPrev ? 'text-gray-700 hover:text-indigo-600' : 'text-gray-400',
        'relative inline-flex items-center px-4 py-2 rounded-l-md border border-gray-300 bg-white text-sm font-medium text-gray-700 hover:bg-gray-50 focus:z-10 focus:outline-none focus:ring-1 focus:ring-indigo-500 focus:border-indigo-500']"
              title="Previous page" :disabled="!canPrev" v-href="{ skip:nextSkip(-take) }">
        <svg class="w-5 h-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path
            d="M15.41 7.41L14 6l-6 6l6 6l1.41-1.41L10.83 12z" fill="currentColor"/></svg>
      </button>
      <button type="button" :class="[canNext ? 'text-gray-700 hover:text-indigo-600' : 'text-gray-400',
            '-ml-px relative inline-flex items-center px-4 py-2 rounded-r-md border border-gray-300 bg-white text-sm font-medium text-gray-700 hover:bg-gray-50 focus:z-10 focus:outline-none focus:ring-1 focus:ring-indigo-500 focus:border-indigo-500']"
              title="Next page" :disabled="!canNext" v-href="{ skip:nextSkip(take) }">
            <svg class="w-5 h-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path d="M10 6L8.59 7.41L13.17 12l-4.58 4.59L10 18l6-6z" fill="currentColor"/></svg>
      </button>
      <button type="button" @click="update" title="Refresh"
              class="ml-2 inline-flex items-center px-2.5 py-1.5 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
        <svg class="w-6 h-6 p-0.5" xmlns="http://www.w3.org/2000/svg" aria-hidden="true" viewBox="0 0 24 24"><g
            fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"><path
            d="M21.168 8A10.003 10.003 0 0 0 12 2c-5.185 0-9.45 3.947-9.95 9"/><path
            d="M17 8h4.4a.6.6 0 0 0 .6-.6V3M2.881 16c1.544 3.532 5.068 6 9.168 6c5.186 0 9.45-3.947 9.951-9"/><path
            d="M7.05 16h-4.4a.6.6 0 0 0-.6.6V21"/></g></svg>
      </button>
    </span>
</div>
<section>
<div class="flex flex-col">
  <div class="-my-2 overflow-x-auto sm:-mx-6 lg:-mx-8">
    <div class="py-2 align-middle inline-block sm:px-6 lg:px-8">
      <div v-if="results.length" class="md:shadow border-b border-gray-200 md:rounded-lg">
        <table class="divide-y divide-gray-200">
          <thead class="bg-gray-50">
          <tr>
            <th v-for="k in uniqueKeys"
                v-href="{ orderBy:routes.orderBy === k ? ('-' + k) : routes.orderBy === ('-' + k) ? '' : k }"
                class="cursor-pointer px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider whitespace-nowrap">
              <div class="flex">
                <span class="mr-1 select-none">{{ keyFmt(fieldLabels[k] || k) }}</span>
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
              :class="['cursor-pointer', expanded(row.id) ? 'bg-indigo-100' : statusBackground(row.error,index) + ' hover:bg-yellow-50']">
            <td v-for="k in uniqueKeys" :key="k" class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
              <span :title="apiValueTitle(row[k],k)">{{ valueFmt(row[k], k) }}</span>
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
              <div class="bg-gray-50 px-4 py-6 sm:px-6">
                <div class="flex items-start justify-between space-x-3">
                  <div class="space-y-1">
                    <h2 class="flex text-lg">
                      <div :class="['font-medium text-gray-900',statusColor(selected.error)]"
                           :title="selected.message || valueFmt(selected.eventType,'eventType')">
                        {{ msgFmt(selected.message || valueFmt(selected.eventType, 'eventType')) }}
                      </div>
                      <div class="ml-2 text-gray-600">
                        (<a v-href="href({ source:selected.source })"
                            class="text-blue-600 hover:text-blue-800">{{ selected.source }}</a>
                        <a v-href="href({ eventType:selected.eventType })" class="text-blue-600 hover:text-blue-800"
                           :title="selected.eventType">{{ valueFmt(selected.eventType, 'eventType') }}</a>)
                      </div>
                    </h2>
                    <div class="text-sm text-gray-500 flex flex-wrap">
                      <div v-if="selected.traceId" class="" title="Trace Id">
                        <a v-href="href({ traceId:selected.traceId })"
                           class="flex items-center text-blue-600 hover:text-blue-800" :title="selected.traceId">
                          <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 mr-0.5 text-gray-500" viewBox="0 0 24 24">
                            <g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2">
                              <path d="M13.544 10.456a4.368 4.368 0 0 0-6.176 0l-3.089 3.088a4.367 4.367 0 1 0 6.177 6.177L12 18.177"/>
                              <path d="M10.456 13.544a4.368 4.368 0 0 0 6.176 0l3.089-3.088a4.367 4.367 0 1 0-6.177-6.177L12 5.823"/>
                            </g>
                          </svg>
                          trace request
                        </a>
                      </div>
                      <div v-if="selected.threadId" class="ml-2 flex items-end" title="Thread Id">
                        <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 text-gray-500" viewBox="0 0 24 24">
                          <path fill="currentColor" d="M14 12.415V5h1V4H8v1h1v7.414l-2 2V15h9v-.586l-2-2Zm3 1.583v2L12 16v4.5l-.5 1.5l-.5-1.5V16l-5-.002v-2h.002L8 12V5.998H7v-3h8.999v3h-1V12l2 1.998Z"/>
                        </svg>
                        <a v-href="href({ threadId:selected.threadId })" class="text-blue-600 hover:text-blue-800">
                          {{ selected.threadId }}
                        </a>
                      </div>
                      <div v-if="selected.userAuthId" class="ml-2 flex items-center" title="User Id">
                        <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 mr-0.5 text-gray-500" viewBox="0 0 24 24">
                          <path fill="currentColor" d="M12 2a5 5 0 1 0 5 5a5 5 0 0 0-5-5zm0 8a3 3 0 1 1 3-3a3 3 0 0 1-3 3zm9 11v-1a7 7 0 0 0-7-7h-4a7 7 0 0 0-7 7v1h2v-1a5 5 0 0 1 5-5h4a5 5 0 0 1 5 5v1z"/>
                        </svg>
                        <a v-href="href({ userAuthId:selected.userAuthId })" class="text-blue-600 hover:text-blue-800">
                          {{ selected.userAuthId }}
                        </a>
                      </div>
                      <div v-if="selectedSession" class="ml-2 flex items-center"
                           :title="(selectedSession?.key || '') + ' cookie'">
                        <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 mr-0.5 text-gray-500" viewBox="0 0 24 24">
                          <path fill="currentColor" d="M21 18.5h-6.18A3 3 0 0 0 13 16.68V13.5h3.17a4.33 4.33 0 0 0 1.3-8.5A6 6 0 0 0 6.06 6.63A3.5 3.5 0 0 0 7 13.5h4v3.18a3 3 0 0 0-1.82 1.82H3a1 1 0 0 0 0 2h6.18a3 3 0 0 0 5.64 0H21a1 1 0 0 0 0-2Zm-14-7a1.5 1.5 0 0 1 0-3a1 1 0 0 0 1-1a4 4 0 0 1 7.79-1.29a1 1 0 0 0 .78.67a2.31 2.31 0 0 1 1.93 2.29a2.34 2.34 0 0 1-2.33 2.33Zm5 9a1 1 0 1 1 1-1a1 1 0 0 1-1 1Z"/>
                        </svg>
                        <a v-href="href({ sessionId:selectedSession.value })" class="text-blue-600 hover:text-blue-800">
                          {{ selectedSession.value }}
                        </a>
                      </div>
                      <div v-if="selected.duration" class="ml-2 flex items-center" title="Duration">
                        <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 mr-0.5 text-gray-500" viewBox="0 0 24 24">
                          <g fill='none' stroke='currentColor' stroke-linecap='round' stroke-linejoin='round' stroke-width='2'>
                            <path d='M10 2h4m-2 12l3-3'/>
                            <circle cx='12' cy='14' r='8'/>
                          </g>
                        </svg>
                        <span class="text-gray-600">
                            {{ valueFmt(selected.duration, 'duration') }}
                        </span>
                      </div>
                      <div v-if="selected.date" class="ml-2 flex items-center" title="Time">
                        <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 mr-0.5 text-gray-500"
                             viewBox="0 0 24 24">
                          <path fill="currentColor"
                                d="M7 11h2v2H7v-2zm14-5v14c0 1.1-.9 2-2 2H5a2 2 0 0 1-2-2l.01-14c0-1.1.88-2 1.99-2h1V2h2v2h8V2h2v2h1c1.1 0 2 .9 2 2zM5 8h14V6H5v2zm14 12V10H5v10h14zm-4-7h2v-2h-2v2zm-4 0h2v-2h-2v2z"/>
                        </svg>
                        <span class="text-gray-600">
                            {{ valueFmt(selected.date, 'date') }}
                        </span>
                      </div>
                    </div>
                  </div>
                  <div class="flex h-7 items-center">
                    <CloseButton @close="toggle(selected)" button-class="bg-gray-50" />
                  </div>
                </div>
              </div>
              <!-- Divider container -->
              <div class="space-y-6 py-6 sm:space-y-0 sm:divide-y sm:divide-gray-200 sm:py-0">
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
    `,
    setup() {
        const routes = inject('routes')
        const server = inject('server')
        const client = useClient()
        let plugin = server.plugins.profiling
        let summaryFields = server.plugins.profiling.summaryFields.map(toCamelCase)
        let linkFields = 'id,traceId,source,eventType,operation,threadId,commandType,userAuthId,sessionId,withErrors,tag,skip'.split(',')
        let fieldLabels = { eventType:'event', threadId:'thread', userAuthId:'userId', date:'time' }
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
            api.value = await client.api(request, { jsconfig: 'eccn' })
        }
        const errorSummary = computed(() => api.value.summaryMessage())
        /** @type {ComputedRef<DiagnosticEntry[]>} */
        const results = computed(() => api.value.response?.results || [])
        const total = computed(() => api.value.response?.total)
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
            if (k === 'traceId') {
                return obj.indexOf(':') >= 0
                    ? lastRightPart(obj, ':')
                    : lastRightPart(obj, '-')
            }
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
        function msgFmt(s) {
            let size = 30
            return !s || s.length < size
                ? s
                : s[0] === '/' || s.indexOf('http://') >= 0 || s.indexOf('https://') >= 0
                    ? '...' + s.substring(s.length - size)
                    : s.substring(0, Math.min(size, s.length - size)) + '...'
        }
        function hasFilters() {
            for (let i; i<linkFields.length; i++) {
                let x = linkFields[i]
                if (routes[x])
                    return true
            }
            return false
        }
        const selectedSession = computed(() => map(selected.value?.sessionId, x => ({ key:'ss-id', value: x })))
        function href(links) {
            return Object.assign(linkFields.reduce((acc,x) => { acc[x] = ''; return acc }, {}), links)
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
            routes,
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
            total,
            uniqueKeys,
            keyFmt,
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
                    ? (index % 2 === 0 ? 'bg-white' : 'bg-gray-50')
                    : 'bg-red-100'
            },
            selectedSession,
            href,
            clearFilters,
            keydown,
            canPrev,
            canNext,
            nextSkip,
        }
    }
}
