import {computed, inject, nextTick, onMounted, onUnmounted, ref, watch} from "vue"
import {
    ApiResult, map, apiValueFmt, humanize, toPascalCase, fromXsdDuration, parseCookie, leftPart, queryString
} from "@servicestack/client"
import {useClient, useFormatters, css} from "@servicestack/vue"
import {keydown} from "app"
import {RequestLogs, GetAnalyticsInfo} from "dtos"
import {prettyJson, parseJsv, hasItems} from "core"

export const Logging = {
    template:/*html*/`
      <div v-if="useAutoQuery">
        <div>
          <div class="mb-2 flex flex-wrap justify-center">
            <template v-for="year in years">
              <b v-if="year === (routes.year || new Date().getFullYear().toString())"
                 class="ml-3 text-sm font-semibold">
                {{ year }}
              </b>
              <a v-else v-href="{ year }" class="ml-3 text-sm text-indigo-700 font-semibold hover:underline">
                {{ year }}
              </a>
            </template>
          </div>

          <div class="flex flex-wrap justify-center">
            <template
                v-for="month in months.filter(x => x.startsWith(routes.year || new Date().getFullYear().toString()))">
                <span
                    v-if="month === (routes.month || (new Date().getFullYear() + '-' + (new Date().getMonth() + 1).toString().padStart(2,'0')))"
                    class="mr-2 mb-2 text-xs leading-5 font-semibold bg-indigo-600 text-white rounded-full py-1 px-3 flex items-center space-x-2">
                  {{ new Date(month + '-01').toLocaleString('default', {month: 'long'}) }}
                </span>
              <a v-else v-href="{ month }"
                 class="mr-2 mb-2 text-xs leading-5 font-semibold bg-slate-400/10 rounded-full py-1 px-3 flex items-center space-x-2 hover:bg-slate-400/20 dark:highlight-white/5">
                {{ new Date(month + '-01').toLocaleString('default', {month: 'short'}) }}
              </a>
            </template>
          </div>
        </div>

        <AutoQueryGrid ref="grid" type="RequestLog"
                       selectedColumns="id,statusCode,httpMethod,pathInfo,operationName,userAuthId,sessionId,ipAddress,requestDuration"
                       :headerTitles="{statusCode:'Status',httpMethod:'Method',operationName:'Operation',userAuthId:'UserId',ipAddress:'IP',requestDuration:'Duration'}"
                       @rowSelected="routes.edit = routes.edit == $event.id ? null : $event.id"
                       :isSelected="(row) => routes.edit == row.id" :filters="gridFilters"
                       hide="forms"
                       :rowClass="(row,i) => row.statusCode >= 300 ? (statusBackground(row.statusCode,i) + ' cursor-pointer hover:bg-yellow-50') : css.grid.getTableRowClass('stripedRows', i, routes.edit == row.id, true)"
        >
          <template #requestDuration="{requestDuration}">
            <span :title="requestDuration">{{ valueFmt(requestDuration, 'requestDuration') }}</span>
          </template>
        </AutoQueryGrid>
      </div>
      <div v-else>
        <div class="mb-2 flex flex-wrap">
        <span class="relative z-0 inline-flex shadow-sm rounded-md">
          <button v-href="href({ withErrors:!routes.withErrors })" type="button"
                  class="relative inline-flex items-center px-4 py-2 rounded-l-md border border-gray-300 bg-white text-sm font-medium text-gray-700 hover:bg-gray-50 focus:z-10 focus:outline-none focus:ring-1 focus:ring-indigo-500 focus:border-indigo-500">
              Has Errors
          </button>
          <button v-href="href({ hasResponse:!routes.hasResponse })" type="button"
                  class="-ml-px relative inline-flex items-center px-4 py-2 rounded-r-md border border-gray-300 bg-white text-sm font-medium text-gray-700 hover:bg-gray-50 focus:z-10 focus:outline-none focus:ring-1 focus:ring-indigo-500 focus:border-indigo-500">
              Has Response
          </button>
        </span>
          <div v-if="hasFilters" class="px-2">
            <button type="button" @click="clearFilters" title="Reset Filters"
                    class="inline-flex items-center px-2.5 py-1.5 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
              <svg class="w-6 h-6 p-0.5" xmlns="http://www.w3.org/2000/svg" aria-hidden="true" viewBox="0 0 24 24">
                <path fill="currentColor"
                      d="M6.78 2.72a.75.75 0 0 1 0 1.06L4.56 6h8.69a7.75 7.75 0 1 1-7.75 7.75a.75.75 0 0 1 1.5 0a6.25 6.25 0 1 0 6.25-6.25H4.56l2.22 2.22a.75.75 0 1 1-1.06 1.06l-3.5-3.5a.75.75 0 0 1 0-1.06l3.5-3.5a.75.75 0 0 1 1.06 0Z"/>
              </svg>
            </button>
          </div>
          <span class="px-2 relative z-0 inline-flex shadow-sm rounded-md">
          <button type="button" :class="[canPrev ? 'text-gray-700 hover:text-indigo-600' : 'text-gray-400',
                'relative inline-flex items-center px-4 py-2 rounded-l-md border border-gray-300 bg-white text-sm font-medium text-gray-700 hover:bg-gray-50 focus:z-10 focus:outline-none focus:ring-1 focus:ring-indigo-500 focus:border-indigo-500']"
                  title="Previous page" :disabled="!canPrev" v-href="{ skip:nextSkip(-take) }">
            <svg class="w-5 h-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path
                d="M15.41 7.41L14 6l-6 6l6 6l1.41-1.41L10.83 12z" fill="currentColor"/></svg>
          </button>
          <button type="button" :class="[canNext ? 'text-gray-700 hover:text-indigo-600' : 'text-gray-400',
                '-ml-px relative inline-flex items-center px-4 py-2 rounded-r-md border border-gray-300 bg-white text-sm font-medium text-gray-700 hover:bg-gray-50 focus:z-10 focus:outline-none focus:ring-1 focus:ring-indigo-500 focus:border-indigo-500']"
                  title="Next page" :disabled="!canNext" v-href="{ skip:nextSkip(take) }">
            <svg class="w-5 h-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path
                d="M10 6L8.59 7.41L13.17 12l-4.58 4.59L10 18l6-6z" fill="currentColor"/></svg>
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
                        <svg class="w-4 h-4" v-if="routes.orderBy===k" xmlns="http://www.w3.org/2000/svg"
                             viewBox="0 0 20 20">
                          <g fill="none">
                            <path
                                d="M8.998 4.71L6.354 7.354a.5.5 0 1 1-.708-.707L9.115 3.18A.499.499 0 0 1 9.498 3H9.5a.5.5 0 0 1 .354.147l.01.01l3.49 3.49a.5.5 0 1 1-.707.707l-2.65-2.649V16.5a.5.5 0 0 1-1 0V4.71z"
                                fill="currentColor"/>
                          </g>
                        </svg>
                        <svg class="w-4 h-4" v-else-if="routes.orderBy===('-' + k)" xmlns="http://www.w3.org/2000/svg"
                             viewBox="0 0 20 20">
                          <g fill="none">
                            <path
                                d="M10.002 15.29l2.645-2.644a.5.5 0 0 1 .707.707L9.886 16.82a.5.5 0 0 1-.384.179h-.001a.5.5 0 0 1-.354-.147l-.01-.01l-3.49-3.49a.5.5 0 1 1 .707-.707l2.648 2.649V3.5a.5.5 0 0 1 1 0v11.79z"
                                fill="currentColor"/>
                          </g>
                        </svg>
                        <span v-else class="w-4 h-4"></span>
                      </div>
                    </th>
                  </tr>
                  </thead>
                  <tbody>
                  <tr v-for="(row,index) in results" :key="row.id" @click="toggle(row)"
                      :class="['cursor-pointer', expanded(row.id) ? 'bg-indigo-100' : statusBackground(row.statusCode,index) + ' hover:bg-yellow-50']">
                    <td v-for="k in uniqueKeys" :key="k" class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      <span :title="row[k]">{{ valueFmt(row[k], k) }}</span>
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
      </div>

      <section v-if="selected" class="relative z-20" aria-labelledby="slide-over-title" role="dialog" aria-modal="true">
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
                          <h2 class="text-lg font-medium text-gray-900 flex">
                            <div v-if="selected.statusCode"
                                 :class="['mr-2 font-medium',statusColor(selected.statusCode)]">
                              {{ selected.statusCode }}
                            </div>
                            <div>
                              <span v-if="selected.httpMethod">{{ selected.httpMethod }}</span>
                              <a v-href="href({ pathInfo:selected.pathInfo })"
                                 class="text-blue-600 hover:text-blue-800">
                                {{ selected.pathInfo }}
                              </a>
                            </div>
                          </h2>
                          <div class="text-sm text-gray-500 flex flex-wrap">
                            <div v-if="selected.operationName" class="flex items-center" title="API">
                              <a v-href="href({ operationName:selected.operationName })"
                                 class="text-blue-600 hover:text-blue-800">
                                {{ selected.operationName }}
                              </a>
                            </div>
                            <div v-if="selected.userAuthId" class="ml-2 flex items-center" title="User Id">
                              <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 mr-0.5 text-gray-500"
                                   viewBox="0 0 24 24">
                                <path fill="currentColor"
                                      d="M12 2a5 5 0 1 0 5 5a5 5 0 0 0-5-5zm0 8a3 3 0 1 1 3-3a3 3 0 0 1-3 3zm9 11v-1a7 7 0 0 0-7-7h-4a7 7 0 0 0-7 7v1h2v-1a5 5 0 0 1 5-5h4a5 5 0 0 1 5 5v1z"/>
                              </svg>
                              <a v-href="href({ userAuthId:selected.userAuthId })"
                                 class="text-blue-600 hover:text-blue-800">
                                {{ selected.userAuthId }}
                              </a>
                            </div>
                            <div v-if="selected.ipAddress" class="ml-2 flex items-center" title="IP Address">
                              <span class="text-gray-500 mr-0.5">IP</span>
                              <a v-href="href({ ipAddress:selected.ipAddress })"
                                 class="text-blue-600 hover:text-blue-800">
                                {{ selected.ipAddress }}
                              </a>
                            </div>
                            <div v-if="selectedSession" class="ml-2 flex items-center"
                                 :title="(selectedSession?.key||'') + ' cookie'">
                              <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 mr-0.5 text-gray-500"
                                   viewBox="0 0 24 24">
                                <path fill="currentColor"
                                      d="M21 18.5h-6.18A3 3 0 0 0 13 16.68V13.5h3.17a4.33 4.33 0 0 0 1.3-8.5A6 6 0 0 0 6.06 6.63A3.5 3.5 0 0 0 7 13.5h4v3.18a3 3 0 0 0-1.82 1.82H3a1 1 0 0 0 0 2h6.18a3 3 0 0 0 5.64 0H21a1 1 0 0 0 0-2Zm-14-7a1.5 1.5 0 0 1 0-3a1 1 0 0 0 1-1a4 4 0 0 1 7.79-1.29a1 1 0 0 0 .78.67a2.31 2.31 0 0 1 1.93 2.29a2.34 2.34 0 0 1-2.33 2.33Zm5 9a1 1 0 1 1 1-1a1 1 0 0 1-1 1Z"/>
                              </svg>
                              <a v-href="href({ sessionId:selectedSession.value })"
                                 class="text-blue-600 hover:text-blue-800">
                                {{ selectedSession && selectedSession.value }}
                              </a>
                            </div>
                            <div v-if="selected.traceId" class="ml-2 flex items-center" title="Trace Id">
                              <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 mr-0.5 text-gray-500"
                                   viewBox="0 0 24 24">
                                <g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round"
                                   stroke-width="2">
                                  <path
                                      d="M13.544 10.456a4.368 4.368 0 0 0-6.176 0l-3.089 3.088a4.367 4.367 0 1 0 6.177 6.177L12 18.177"/>
                                  <path
                                      d="M10.456 13.544a4.368 4.368 0 0 0 6.176 0l3.089-3.088a4.367 4.367 0 1 0-6.177-6.177L12 5.823"/>
                                </g>
                              </svg>
                              <a v-href="href({ $page:'profiling', traceId:selected.traceId })"
                                 :title="selected.traceId"
                                 class="text-blue-600 hover:text-blue-800">
                                trace request
                              </a>
                            </div>
                          </div>
                        </div>
                        <div class="flex h-7 items-center">
                          <CloseButton buttonClass="bg-gray-50" @close="toggle(selected)"/>
                        </div>
                      </div>
                    </div>

                    <!-- Divider container -->
                    <div class="space-y-6 py-6 sm:space-y-0 sm:divide-y sm:divide-gray-200 sm:py-0">
                      <div v-if="selected.requestDto" class="flex overflow-auto">
                        <div class="p-2 relative w-full">
                        <span class="relative z-0 inline-flex shadow-sm rounded-md">
                          <a v-for="(tab,name) in {Pretty:'',Raw:'raw',Preview:'preview'}"
                             v-href="{ body:tab }"
                             :class="[{ Pretty:'rounded-l-md',Raw:'-ml-px',Preview:'rounded-r-md -ml-px' }[name], routes.body === tab ? 'z-10 outline-none ring-1 ring-indigo-500 border-indigo-500' : '', 'cursor-pointer relative inline-flex items-center px-4 py-1 border border-gray-300 bg-white text-sm font-medium text-gray-700 hover:bg-gray-50']">
                            {{ name }}
                          </a>
                        </span>
                          <div v-if="routes.body === ''" class="pt-2 icon-outer" style="min-height:2.5rem">
                            <CopyIcon class="absolute right-4" :text="selectedRequestDtoJson"/>
                            <pre class="whitespace-pre-wrap"><code lang="json"
                                                                   v-highlightjs="selectedRequestDtoJson"></code></pre>
                          </div>
                          <div v-else-if="routes.body === 'raw'" class="flex pt-2">
                            <textarea class="flex-1" rows="10" v-html="selected.requestDto"></textarea>
                          </div>
                          <div v-else-if="routes.body === 'preview'" class="body-preview flex pt-2 overflow-x-auto">
                            <HtmlFormat :value="selectedRequestDtoObj"/>
                          </div>
                        </div>
                      </div>

                      <div v-if="selected.responseDto" class="bg-indigo-700 text-white px-3 py-3">
                        <div class="flex items-start justify-between space-x-3">
                          <h2 class="font-medium text-white">Response</h2>
                        </div>
                      </div>
                      <div v-if="selected.responseDto" class="flex overflow-auto">
                        <div class="p-2 relative w-full">
                        <span class="relative z-0 inline-flex shadow-sm rounded-md">
                          <a v-for="(tab,name) in {Pretty:'',Raw:'raw',Preview:'preview'}"
                             v-href="{ body:tab }"
                             :class="[{ Pretty:'rounded-l-md',Raw:'-ml-px',Preview:'rounded-r-md -ml-px' }[name], routes.body === tab ? 'z-10 outline-none ring-1 ring-indigo-500 border-indigo-500' : '', 'cursor-pointer relative inline-flex items-center px-4 py-1 border border-gray-300 bg-white text-sm font-medium text-gray-700 hover:bg-gray-50']">
                            {{ name }}
                          </a>
                        </span>
                          <div v-if="routes.body === ''" class="pt-2 icon-outer" style="min-height:2.5rem">
                            <CopyIcon class="absolute right-4" :text="selectedResponseDtoJson"/>
                            <pre class="whitespace-pre-wrap"><code lang="json"
                                                                   v-highlightjs="selectedResponseDtoJson"></code></pre>
                          </div>
                          <div v-else-if="routes.body === 'raw'" class="flex pt-2">
                            <textarea class="flex-1" rows="10" v-html="selected.responseDto"></textarea>
                          </div>
                          <div v-else-if="routes.body === 'preview'" class="body-preview flex pt-2 overflow-x-auto">
                            <HtmlFormat :value="selectedResponseDtoObj"/>
                          </div>
                        </div>
                      </div>

                      <div v-if="hasItems(selectedHeaders)" class="bg-indigo-700 text-white px-3 py-3">
                        <div class="flex items-start justify-between space-x-3">
                          <h2 class="font-medium text-white">HTTP Headers</h2>
                        </div>
                      </div>
                      <div v-if="hasItems(selectedHeaders)" class="flex overflow-auto">
                        <table>
                          <tr v-for="(value,key) in selectedHeaders">
                            <th class="text-left font-medium align-top py-2 px-4 whitespace-nowrap">
                              <div class=" whitespace-nowrap w-[9em] overflow-hidden" :title="key">
                                {{ key }}
                              </div>
                            </th>
                            <td class="align-top py-2 px-4">
                              <a v-if="key === 'X-Forwarded-For'" v-href="href({ forwardedFor:value })"
                                 class="text-blue-600 hover:text-blue-800">
                                {{ value }}
                              </a>
                              <a v-else-if="key === 'Referer'" v-href="href({ referer:value })"
                                 class="text-blue-600 hover:text-blue-800">
                                {{ value }}
                              </a>
                              <span v-else>{{ value }}</span>
                            </td>
                          </tr>
                        </table>
                      </div>

                      <div v-if="hasItems(responseHeaders)" class="bg-indigo-700 text-white px-3 py-3">
                        <div class="flex items-start justify-between space-x-3">
                          <h2 class="font-medium text-white">HTTP Response Headers</h2>
                        </div>
                      </div>
                      <div v-if="hasItems(responseHeaders)" class="flex overflow-auto">
                        <table>
                          <tr v-for="(value,key) in responseHeaders">
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

                      <div v-if="hasItems(selectedCookies)" class="bg-indigo-700 text-white px-3 py-3">
                        <div class="flex items-start justify-between space-x-3">
                          <h2 class="font-medium text-white">Cookies</h2>
                        </div>
                      </div>
                      <div v-if="hasItems(selectedCookies)" class="flex overflow-auto">
                        <table>
                          <tr v-for="cookie in selectedCookies">
                            <th class="text-left font-medium align-top py-2 px-4">
                              <div class=" whitespace-nowrap w-[9em] overflow-hidden" :title="cookie.name"
                                   title="Name">
                                {{ cookie.name }}
                              </div>
                            </th>
                            <td class="align-top py-2 px-4" title="Value">
                              {{ cookie.value }}
                            </td>
                            <td class="align-top py-2 px-4" title="Path">
                              {{ cookie.path }}
                            </td>
                          </tr>
                        </table>
                      </div>

                      <div v-if="hasItems(selectedJwt)" class="bg-indigo-700 text-white px-3 py-3">
                        <div class="flex items-start justify-between space-x-3">
                          <h2 class="font-medium text-white">JWT</h2>
                        </div>
                      </div>
                      <div v-if="hasItems(selectedJwt)" class="flex overflow-auto">
                        <table>
                          <tr v-for="(value,key) in selectedJwt">
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
      </section>
    `,
    setup() {

        const app = inject('app')
        const routes = inject('routes')
        const server = inject('server')
        const client = useClient()
        const grid = ref()
        const gridFilters = ref({})
        const selected = ref()
        const {Formats} = useFormatters()
        const useAutoQuery = computed(() => server.plugins.requestLogs?.analytics)
        const months = ref(server.plugins.requestLogs?.analytics?.months ?? [])
        const years = computed(() =>
            Array.from(new Set(months.value.map(x => leftPart(x, '-')))).toReversed())

        const idSortKey = `Column/AutoQueryGrid:RequestLog.Id`
        if (!localStorage.getItem(idSortKey)) {
            localStorage.setItem(idSortKey, `{"filters":[],"sort":"DESC"}`)
        }

        const qs = queryString(location.search)
        const opKey = 'Column/AutoQueryGrid:RequestLog.OperationName'
        if (qs.op) {
            localStorage.setItem(opKey, `{"filters":[{"key":"%","name":"=","value":"${qs.op}"}]}`)
        } else {
            localStorage.removeItem(opKey)
        }
        const statusKey = 'Column/AutoQueryGrid:RequestLog.StatusCode'
        if (qs.status) {
            localStorage.setItem(statusKey, `{"filters":[{"key":"%","name":"=","value":"${qs.status}"}]}`)
        } else {
            localStorage.removeItem(statusKey)
        }
        const userKey = 'Column/AutoQueryGrid:RequestLog.UserAuthId'
        if (qs.userId) {
            localStorage.setItem(userKey, `{"filters":[{"key":"%","name":"=","value":"${qs.userId}"}]}`)
        } else {
            localStorage.removeItem(userKey)
        }
        const ipKey = 'Column/AutoQueryGrid:RequestLog.IpAddress'
        if (qs.ip) {
            localStorage.setItem(ipKey, `{"filters":[{"key":"%","name":"=","value":"${qs.ip}"}]}`)
        } else {
            localStorage.removeItem(ipKey)
        }

        function parseJwt(token) {
            let base64Url = token.split('.')[1];
            let base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
            let jsonPayload = decodeURIComponent(window.atob(base64).split('')
                .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
                .join(''));
            return JSON.parse(jsonPayload);
        }

        let fieldLabels = {
            operationName: 'operation', statusCode: 'status', pathInfo: 'path', httpMethod: 'method',
            userAuthId: 'userId', ipAddress: 'ip', requestDuration: 'duration'
        }

        let summaryFields = 'id,statusCode,httpMethod,pathInfo,operationName,userAuthId,sessionId,ipAddress,requestDuration'.split(',')
        let ignoredHeaders = [':method', 'Cookie']
        let linkFields = 'operationName,userAuthId,sessionId,pathInfo,ipAddress,referer,forwardedFor,hasResponse,withErrors,skip'.split(',')

        let api = ref(new ApiResult())

        function parseObject(o) {
            if (!o) return null
            if (typeof o == 'string') {
                try {
                    return JSON.parse(o)
                } catch (e) {
                    try {
                        return parseJsv(o)
                    } catch (e2) {
                        console.error(`Couldn't parse as JSON or JSV`)
                    }
                }
            }
            return o
        }

        async function update() {
            // AutoQuery is queried using AutoQueryGrid
            if (!useAutoQuery.value) {
                let request = new RequestLogs({take: 100})
                if (routes.orderBy)
                    request.orderBy = routes.orderBy
                linkFields.forEach(x => {
                    if (routes[x]) request[x] = routes[x]
                })
                api.value = await client.api(request, {jsconfig: 'eccn'})
            } else {
                updateMonth()
            }
        }

        watch(() => routes.edit, async () => {
            const id = parseInt(routes.edit)
            if (!isNaN(id)) {
                const request = new RequestLogs({ids: [id]})
                if (routes.month) {
                    request.month = `${routes.month}-01`
                }
                const apiResult = await client.api(request)
                selected.value = apiResult.response?.results?.[0]
            } else {
                selected.value = null
            }
        })

        function updateMonth() {
            gridFilters.value = routes.month
                ? {month: `${routes.month}-01`}
                : {}
            setTimeout(() => grid.value?.update(), 1)
        }

        watch(() => routes.month, updateMonth)

        function valueFmt(obj, k) {
            if (k === 'requestDuration' && obj === 'PT0S') return ''
            if (k === 'sessionId') {
                return obj && obj.substring(0, 10)
            }
            return typeof obj === 'string' && obj.startsWith('PT')
                ? fromXsdDuration(obj)
                : apiValueFmt(obj)

        }

        const errorSummary = computed(() => api.value?.summaryMessage())
        const results = computed(() => api.value.response?.results || [])
        const total = computed(() => api.value?.response?.total)
        const uniqueKeys = summaryFields
        const hasFilters = computed(() => {
            for (let i; i < linkFields.length; i++) {
                let x = linkFields[i]
                if (routes[x])
                    return true
            }
            return false
        })

        const selectedRequestDtoObj = computed(() => selected.value?.requestDto && parseObject(selected.value.requestDto))
        const selectedRequestDtoJson = computed(() => selected.value?.requestDto && prettyJson(parseObject(selected.value.requestDto)))
        const selectedResponseDtoObj = computed(() => selected.value?.responseDto && parseObject(selected.value.responseDto))
        const selectedResponseDtoJson = computed(() => selected.value?.responseDto && prettyJson(parseObject(selected.value.responseDto)))

        function keyFmt(t) {
            return humanize(toPascalCase(t))
        }

        const selectedCookies = computed(() => {
            let headers = selected.value?.headers
            return map(headers && headers.Cookie, x => x.split(',').map(parseCookie))
        })
        const selectedSession = computed(() => {
            return selectedCookies.value &&
                map(selectedCookies.value.find(x => x.name === 'ss-opt') === 'perm'
                        ? selectedCookies.value.find(x => x.name === 'ss-pid')
                        : selectedCookies.value.find(x => x.name === 'ss-id') || selectedCookies.value.find(x => x.name === 'ss-pid'),
                    x => ({key: x.name, value: x.value}))
        })
        const selectedJwt = computed(() => {
            let jwt = selectedCookies.value?.find(c => c.name === 'ss-tok')
            return jwt && parseJwt(jwt.value)
        })

        function href(links) {
            return Object.assign(linkFields.reduce((acc, x) => {
                acc[x] = '';
                return acc
            }, {}), links)
        }

        function clearFilters() {
            routes.to(href({edit: ''}))
        }

        const take = ref(server.plugins.requestLogs?.defaultLimit ?? 100)
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
            keydown(e, {canPrev, canNext, nextSkip, take, results, selected, clearFilters})
        }

        let sub = null
        onMounted(async () => {
            document.addEventListener('keydown', handleKeyDown)
            sub = app.subscribe('route:nav', update)
            updateMonth()
            await update()
            const api = await client.api(new GetAnalyticsInfo({type: 'info'}))
            if (api.succeeded) {
                months.value = api.response.months
            }
        })

        onUnmounted(() => {
            document.removeEventListener('keydown', handleKeyDown)
            app.unsubscribe(sub)
        })

        return {
            routes,
            useAutoQuery,
            css,
            parseObject,
            prettyJson,
            api,
            take,
            parseCookie,
            fieldLabels,
            linkFields,
            valueFmt,
            hasItems,
            errorSummary,
            /** @type {RequestLogEntry[]} */
            results,
            total,
            uniqueKeys,
            keyFmt,
            hasFilters,
            selected,
            selectedRequestDtoObj,
            selectedRequestDtoJson,
            selectedResponseDtoObj,
            selectedResponseDtoJson,
            toggle(row) {
                routes.to({edit: routes.edit === row.id ? '' : row.id})
            },
            expanded(id) {
                return selected.value?.id === id
            },
            get selectedHeaders() {
                if (!selected.value || !selected.value.headers) return null
                return Object.keys(selected.value.headers).filter(x => ignoredHeaders.indexOf(x) < 0)
                    .reduce((acc, x) => {
                        acc[x] = selected.value.headers[x];
                        return acc
                    }, {})
            },
            get responseHeaders() {
                if (!selected.value || !selected.value.responseHeaders) return null
                return Object.keys(selected.value.responseHeaders).filter(x => ignoredHeaders.indexOf(x) < 0)
                    .reduce((acc, x) => {
                        acc[x] = selected.value.responseHeaders[x];
                        return acc
                    }, {})
            },
            statusColor(status) {
                return map(status, x => x <= 300
                    ? 'text-green-700' : x < 400 ? 'text-amber-600' : x < 500 ? 'text-red-700' : null) || 'text-gray-700'
            },
            statusBackground(status, index) {
                return status < 300
                    ? (index % 2 === 0 ? 'bg-white' : 'bg-gray-50')
                    : status < 400
                        ? 'bg-amber-100'
                        : status < 500
                            ? 'bg-red-100'
                            : 'bg-orange-100'
            },
            /** @type {Cookie[]} */
            selectedCookies,
            selectedSession,
            selectedJwt,

            href,
            clearFilters,

            update,
            keydown,
            canPrev,
            canNext,
            nextSkip,
            Formats,
            grid,
            gridFilters,
            years,
            months,
        }
    }
}
