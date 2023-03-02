import { computed, inject, onMounted, onUnmounted, ref, watch } from "vue"
import { useClient, useFormatters, useMetadata, useUtils } from "@servicestack/vue"
import { ApiResult, createUrl, flatMap, humanify, mapGet, omit, map, queryString, combinePaths, appendQueryString } from "@servicestack/client"
import { keydown } from "app"
import { AdminDatabase } from "dtos"
import { prettyJson } from "core"

export const Database = {
    template:/*html*/`
    <section class="">
        <div v-if="!routes.table" class="flex flex-wrap">
            <nav v-for="db in databases" class="flex-1 space-y-1 bg-white pb-4 md:pb-scroll" aria-label="Tables">
                <div class="">

                    <span class="text-2xl text-gray-900 group flex items-center pr-2 py-2 text-sm font-medium rounded-md">
                        <svg class="text-gray-500 mr-3 h-8 w-8" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true"><g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1"><ellipse cx="12" cy="6" rx="8" ry="3"></ellipse><path d="M4 6v6a8 3 0 0 0 16 0V6"></path><path d="M4 12v6a8 3 0 0 0 16 0v-6"></path></g></svg>
                        {{db.alias || db.name}}
                    </span>

                    <div v-for="schema in db.schemas" class="space-y-1">
                        <button type="button" @click.prevent="toggleSchema(db.name,schema.name)"
                                class="bg-white text-gray-600 hover:bg-gray-50 hover:text-gray-900 group w-full flex items-center pr-2 py-2 text-left text-sm font-medium">
                            <svg :class="[!isCollapsed(db.name,schema.name) ? 'text-gray-400 rotate-90' : 'text-gray-300','mr-2 flex-shrink-0 h-5 w-5 transform group-hover:text-gray-400 transition-colors ease-in-out duration-150']" viewBox="0 0 20 20" aria-hidden="true">
                                <path d="M6 6L14 10L6 14V6Z" fill="currentColor" />
                            </svg>
                            {{schema.alias || schema.name}}
                        </button>
                        <div v-if="!isCollapsed(db.name,schema.name)" class="space-y-1">
                            <a v-for="table in schema.tables" v-href="{ db:db.name, schema:schema.name, table }"
                               :class="[table === routes.table ? 'bg-indigo-50 border-indigo-600 text-indigo-600' : 
                                    'border-transparent text-gray-600 hover:text-gray-900 hover:bg-gray-50', 'border-l-4 group w-full flex justify-between items-center pl-10 pr-2 py-2 text-sm font-medium']">
                                <span class="nav-item flex-grow">{{table}}</span>
                            </a>
                        </div>
                    </div>
                </div>
            </nav>
        </div>
        <div v-else>

            <nav class="flex" aria-label="Breadcrumb">
                <ol role="list" class="flex items-center space-x-4">
                    <li title="All Databases">
                        <div>
                            <a v-href="{ db:'', schema:'', table:'', show:'', skip:'' }" class="text-gray-400 hover:text-gray-500">
                                <svg class="flex-shrink-0 h-5 w-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                                    <path d="M10.707 2.293a1 1 0 00-1.414 0l-7 7a1 1 0 001.414 1.414L4 10.414V17a1 1 0 001 1h2a1 1 0 001-1v-2a1 1 0 011-1h2a1 1 0 011 1v2a1 1 0 001 1h2a1 1 0 001-1v-6.586l.293.293a1 1 0 001.414-1.414l-7-7z" />
                                </svg>
                                <span class="sr-only">Home</span>
                            </a>
                        </div>
                    </li>
                    <li v-if="routes.db" :title="routes.db + ' database'">
                        <div class="flex items-center">
                            <svg class="flex-shrink-0 h-5 w-5 text-gray-300" xmlns="http://www.w3.org/2000/svg" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
                                <path d="M5.555 17.776l8-16 .894.448-8 16-.894-.448z" />
                            </svg>
                            <a v-href="{ db:routes.db, schema:'', table:'', show:'', skip:'' }" class="ml-4 text-sm font-medium text-gray-500 hover:text-gray-700">
                                {{dbAlias(routes.db)}}
                            </a>
                        </div>
                    </li>
                    <li v-if="routes.schema" :title="routes.schema + ' schema'">
                        <div class="flex items-center">
                            <svg class="flex-shrink-0 h-5 w-5 text-gray-300" xmlns="http://www.w3.org/2000/svg" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
                                <path d="M5.555 17.776l8-16 .894.448-8 16-.894-.448z" />
                            </svg>
                            <a v-href="{ db:routes.db, schema:routes.schema, table:'', show:'', skip:'' }" class="ml-4 text-sm font-medium text-gray-500 hover:text-gray-700">
                                {{schemaAlias(routes.db, routes.schema)}}
                            </a>
                        </div>
                    </li>
                    <li>
                        <div class="flex items-center">
                            <svg class="flex-shrink-0 h-5 w-5 text-gray-300" xmlns="http://www.w3.org/2000/svg" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
                                <path d="M5.555 17.776l8-16 .894.448-8 16-.894-.448z" />
                            </svg>
                            <span class="ml-4 text-sm font-medium text-gray-700" aria-current="page">{{routes.table}}</span>
                        </div>
                    </li>
                </ol>
            </nav>

            <div class="mt-4">
                <QueryPrefs v-if="showQueryPrefs" :maxLimit="plugin.queryLimit" :columns="viewModelColumns" :prefs="settings.table(routes.dbTable())" @save="updatePrefs" @done="showQueryPrefs=false" />
                <div class="flex flex-wrap">
                    <div class="flex pb-1 sm:pb-0">
                        <button type="button" class="text-gray-700 hover:text-indigo-600" :title="routes.table + ' Preferences'" @click="showQueryPrefs=true">
                            <svg class="w-8 h-8" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><g stroke-width="1.5" fill="none"><path d="M9 3H3.6a.6.6 0 0 0-.6.6v16.8a.6.6 0 0 0 .6.6H9M9 3v18M9 3h6M9 21h6m0-18h5.4a.6.6 0 0 1 .6.6v16.8a.6.6 0 0 1-.6.6H15m0-18v18" stroke="currentColor"/></g></svg>
                        </button>
                        <button type="button" :class="['pl-2', canFirst ? 'text-gray-700 hover:text-indigo-600' : 'text-gray-400']"
                                title="First page" :disabled="!canFirst" v-href="{ skip:nextSkip(-total) }">
                            <svg class="w-8 h-8" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path d="M18.41 16.59L13.82 12l4.59-4.59L17 6l-6 6l6 6zM6 6h2v12H6z" fill="currentColor"/></svg>
                        </button>
                        <button type="button" :class="['pl-2', canPrev ? 'text-gray-700 hover:text-indigo-600' : 'text-gray-400']"
                                title="Previous page" :disabled="!canPrev" v-href="{ skip:nextSkip(-take) }">
                            <svg class="w-8 h-8" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path d="M15.41 7.41L14 6l-6 6l6 6l1.41-1.41L10.83 12z" fill="currentColor"/></svg>
                        </button>
                        <button type="button" :class="['pl-2', canNext ? 'text-gray-700 hover:text-indigo-600' : 'text-gray-400']"
                                title="Next page" :disabled="!canNext" v-href="{ skip:nextSkip(take) }">
                            <svg class="w-8 h-8" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path d="M10 6L8.59 7.41L13.17 12l-4.58 4.59L10 18l6-6z" fill="currentColor"/></svg>
                        </button>
                        <button type="button" :class="['pl-2', canLast ? 'text-gray-700 hover:text-indigo-600' : 'text-gray-400']"
                                title="Last page" :disabled="!canLast" v-href="{ skip:nextSkip(total) }">
                            <svg class="w-8 h-8" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path d="M5.59 7.41L10.18 12l-4.59 4.59L7 18l6-6l-6-6zM16 6h2v12h-2z" fill="currentColor"/></svg>
                        </button>
                    </div>
                    <div class="flex pb-1 sm:pb-0">
                        <div class="px-4 text-lg">
                            <span v-if="apiLoading">Querying...</span>
                            <span v-else-if="results.length"><span class="hidden xl:inline">Showing Results</span> {{skip+1}} - {{min(skip + results.length,total)}} <span v-if="total!=null">of {{total}}</span></span>
                            <span v-else-if="api">No Results</span>
                        </div>
                    </div>
                    <div class="flex pb-1 sm:pb-0">
                        <div class="pl-2">
                            <button type="button" @click="downloadCsv" title="Download CSV"
                                    class="inline-flex items-center px-2.5 py-1.5 border border-gray-300 shadow-sm text-sm font-medium rounded text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
                                <svg class="w-5 h-5 mr-1" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32"><path d="M28.781 4.405h-10.13V2.018L2 4.588v22.527l16.651 2.868v-3.538h10.13A1.162 1.162 0 0 0 30 25.349V5.5a1.162 1.162 0 0 0-1.219-1.095zm.16 21.126H18.617l-.017-1.889h2.487v-2.2h-2.506l-.012-1.3h2.518v-2.2H18.55l-.012-1.3h2.549v-2.2H18.53v-1.3h2.557v-2.2H18.53v-1.3h2.557v-2.2H18.53v-2h10.411z" fill="#20744a" fill-rule="evenodd"/><path fill="#20744a" d="M22.487 7.439h4.323v2.2h-4.323z"/><path fill="#20744a" d="M22.487 10.94h4.323v2.2h-4.323z"/><path fill="#20744a" d="M22.487 14.441h4.323v2.2h-4.323z"/><path fill="#20744a" d="M22.487 17.942h4.323v2.2h-4.323z"/><path fill="#20744a" d="M22.487 21.443h4.323v2.2h-4.323z"/><path fill="#fff" fill-rule="evenodd" d="M6.347 10.673l2.146-.123l1.349 3.709l1.594-3.862l2.146-.123l-2.606 5.266l2.606 5.279l-2.269-.153l-1.532-4.024l-1.533 3.871l-2.085-.184l2.422-4.663l-2.238-4.993z"/></svg>
                                <span class="text-green-900">Excel</span>
                            </button>
                        </div>
                        <div class="pl-2">
                            <button type="button" @click="copyApiUrl" title="Copy API URL"
                                    class="inline-flex items-center px-2.5 py-1.5 border border-gray-300 shadow-sm text-sm font-medium rounded text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
                                <svg v-if="copied" class="w-5 h-5 mr-1 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg>
                                <svg v-else class="w-5 h-5 mr-1" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><g fill="none"><path d="M8 4v12a2 2 0 0 0 2 2h8a2 2 0 0 0 2-2V7.242a2 2 0 0 0-.602-1.43L16.083 2.57A2 2 0 0 0 14.685 2H10a2 2 0 0 0-2 2z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/><path d="M16 18v2a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V9a2 2 0 0 1 2-2h2" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></g></svg>
                                <span>Copy URL</span>
                            </button>
                        </div>
                        <div v-if="hasPrefs" class="pl-2">
                            <button type="button" @click="clearPrefs" title="Reset Preferences &amp; Filters"
                                    class="inline-flex items-center px-2.5 py-1.5 border border-gray-300 shadow-sm text-sm font-medium rounded text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
                                <svg class="w-5 h-5" xmlns="http://www.w3.org/2000/svg" aria-hidden="true" viewBox="0 0 24 24"><path fill="currentColor" d="M6.78 2.72a.75.75 0 0 1 0 1.06L4.56 6h8.69a7.75 7.75 0 1 1-7.75 7.75a.75.75 0 0 1 1.5 0a6.25 6.25 0 1 0 6.25-6.25H4.56l2.22 2.22a.75.75 0 1 1-1.06 1.06l-3.5-3.5a.75.75 0 0 1 0-1.06l3.5-3.5a.75.75 0 0 1 1.06 0Z"/></svg>
                            </button>
                        </div>
                        <div v-if="filtersCount" class="pl-2">
                            <button type="button" @click="open = open === 'filters' ? '' : 'filters'"
                                    class="px-1 py-1.5 group text-gray-700 font-medium flex items-center" aria-expanded="false">
                                <svg class="flex-none w-5 h-5 mr-2 text-gray-400 group-hover:text-gray-500" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                                    <path fill-rule="evenodd" d="M3 3a1 1 0 011-1h12a1 1 0 011 1v3a1 1 0 01-.293.707L12 11.414V15a1 1 0 01-.293.707l-2 2A1 1 0 018 17v-5.586L3.293 6.707A1 1 0 013 6V3z" clip-rule="evenodd" />
                                </svg>
                                <span class="mr-1">
                                        {{filtersCount}} {{ filtersCount === 1 ? 'Filter' : 'Filters' }}
                                    </span>
                                <svg v-if="open!=='filters'"
                                     class="h-5 w-5 text-gray-400 group-hover:text-gray-500" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                                    <path fill-rule="evenodd" d="M10 5a1 1 0 011 1v3h3a1 1 0 110 2h-3v3a1 1 0 11-2 0v-3H6a1 1 0 110-2h3V6a1 1 0 011-1z" clip-rule="evenodd" />
                                </svg>
                                <svg v-else
                                     class="h-5 w-5 text-gray-400 group-hover:text-gray-500" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                                    <path fill-rule="evenodd" d="M5 10a1 1 0 011-1h8a1 1 0 110 2H6a1 1 0 01-1-1z" clip-rule="evenodd" />
                                </svg>
                            </button>
                        </div>
                    </div>
                </div>
            </div>
            
            <FilterViews v-if="open==='filters'" :definitions="definitions" :columns="viewModelColumns" @done="open=''" @change="filtersChanged"  />
            <ErrorSummary class="p-4" />
            <Loading v-if="apiLoading" class="pt-4" />
            <div v-else-if="results.length" class="mt-2">
                <div v-if="showFilters">
                  <FilterColumn :definitions="definitions" :column="filter.column" :topLeft="filter.topLeft" @done="onFilterDone" @save="onFilterSave" />
                </div>
              
                <div ref="refResults" v-if="results.length" class="sm:-ml-2 lg:-ml-4 flex flex-col">
                    <div class="overflow-x-auto pb-4">
                        <div class="py-2 align-middle inline-block min-w-full sm:px-2 lg:px-4">
                            <div class="md:shadow overflow-hidden border-b border-gray-200 md:rounded-lg">
                                <table class="table-array min-w-full divide-y divide-gray-200">
                                    <thead class="bg-gray-50">
                                    <tr>
                                        <th v-for="c in columns" :key="c.name" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider whitespace-nowrap">
                                            <div @click="onHeaderSelected(c,$event)">
                                              <div v-if="complexProp(c)" class="flex justify-between items-center text-sm">
                                                <span class="mr-1 select-none">{{fieldName(c.name)}}</span>
                                              </div>
                                              <div v-else @click="selectColumn(c,$event)" class="flex justify-between items-center text-sm cursor-pointer hover:text-gray-900">
                                                <span class="mr-1 select-none">{{fieldName(c.name)}}</span>
                                                <SettingsIcons :column="c" :is-open="showFilters?.column.name === c.name" />
                                              </div>
                                            </div>
                                        </th>
                                    </tr>
                                    </thead>
                                    <tbody>
                                    <tr v-for="(row,index) in results" @click="toggle(row)"
                                        :class="['cursor-pointer', expanded(row.id) ? 'bg-indigo-100' : 'hover:bg-yellow-50']">
                                        <td v-for="c in columns" :key="c.name" class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                                          <HtmlFormat :value="mapGet(row,c.name)" :format="c.format" />  
                                        </td>
                                    </tr>
                                    </tbody>
                                </table>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div v-if="selected" class="relative z-20" aria-labelledby="slide-over-title" role="dialog" aria-modal="true">
            <div class="fixed overflow-hidden">
                <div class="absolute overflow-hidden">
                    <div class="pointer-events-none fixed inset-y-0 right-0 flex max-w-full pl-10 sm:pl-16">
                        <div class="pointer-events-auto w-screen max-w-2xl">
                            <form v-if="selected" class="flex h-full flex-col overflow-y-auto bg-white shadow-xl">
                                <div class="flex-1">
                                    
                                    <!-- Header -->
                                    <div class="bg-gray-50 px-4 py-6 sm:px-6">
                                        <div class="flex items-start justify-between space-x-3">
                                            <div class="space-y-1">
                                                <h2 class="flex text-lg">
                                                    <div :class="['font-medium text-gray-900']">
                                                        {{routes.table}} {{selected.id}}
                                                    </div>
                                                </h2>
                                            </div>
                                            <div class="flex h-7 items-center">
                                                <CloseButton @close="toggle(selected)" />
                                            </div>
                                        </div>
                                    </div>

                                    <div class="space-y-6 py-6 sm:space-y-0 sm:divide-y sm:divide-gray-200 sm:py-0">

                                        <div class="flex overflow-auto">
                                            <div class="p-2 relative w-full">
                                            <span class="relative z-0 inline-flex shadow-sm rounded-md">
                                              <a v-for="(tab,name) in {Pretty:'',Preview:'preview'}" v-href="{ body:tab }"
                                                 :class="[{ Pretty:'rounded-l-md',Raw:'-ml-px',Preview:'rounded-r-md -ml-px' }[name], routes.body === tab ? 'z-10 outline-none ring-1 ring-indigo-500 border-indigo-500' : '', 'cursor-pointer relative inline-flex items-center px-4 py-1 border border-gray-300 bg-white text-sm font-medium text-gray-700 hover:bg-gray-50']">
                                                {{name}}
                                              </a>
                                            </span>
                                                <div v-if="routes.body == ''" class="pt-2 icon-outer" style="min-height:2.5rem">
                                                    <CopyIcon class="absolute right-4" :text="prettyJson(selected)" />
                                                    <pre class="whitespace-pre-wrap"><code lang="json" v-highlightjs="prettyJson(selected)"></code></pre>
                                                </div>
                                                <div v-else-if="routes.body === 'preview'" class="body-preview flex pt-2 overflow-x-auto">
                                                    <HtmlFormat :value="selectedClean" />
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
    </section>
    `,
    setup() {

        const app = inject('app')
        const routes = inject('routes')
        const server = inject('server')
        const settings = inject('settings')
        const apiUrlBase = inject('client').replyBaseUrl
        const client = useClient()
        const { copyText } = useUtils()
        const { formatValue } = useFormatters()
        const { getPrimaryKeyByProps, isComplexProp } = useMetadata()
        
        let refreshPrefs = () => settings.table(routes.dbTable())
        let schemaKey = (db,schema) => `${db}.${schema}`
        function refreshFilters() {
            /** @type {import('@servicestack/vue').Column[]} */
            const cols = columnsMap[routes.dbTable()].map(column => ({
                ...column,
                meta: column,
                settings: settings.tableProp(routes.dbTable(), column.name)
            }))
            viewModelColumns.value = cols
        }
            
        let d = (name,value,opt) => (Object.assign({ name, value }, opt))
        let definitions = [
            d("=","%"),
            d("!=","%!"),
            d("<","<%"),
            d("<=","%<"),
            d(">","%>"),
            d(">=",">%"),
            d("In","%In"),
            d("Starts With","%StartsWith", { types: "string" }),
            d("Contains","%Contains", { types: "string" }),
            d("Ends With","%EndsWith", { types: "string" }),
            d("Exists","%IsNotNull", { valueType: "none" }),
            d("Not Exists","%IsNull", { valueType: "none" }),
        ]
        let filterConverters = {
            "%In": (k,v) => [k + '[]', v],
            /** convert from AutoQuery conventions to AdminDatabase filters (nicer syntax but doesn't support multi column filters)
             "%StartsWith": (k,v) => [k, v + '%'],
             "%Contains": (k,v) => [k, '%' + v + '%'],
             "%EndsWith": (k,v) => [k, '%' + v],
             "%IsNotNull": (k,v) => [k + '!', 'null'],
             "%IsNull": (k,v) => [k, 'null'],
             */
        }
        let unknownIdSeq = 1

        /** @type {Ref<ApiResult<AdminDatabaseResponse>>} */
        const api = ref(new ApiResult())
        const apiColumns = ref(new ApiResult())
        const results = computed(() => api.value?.response?.results || [])
        const total = computed(() => api.value?.response?.total || 0)
        const apiLoading = computed(() => client.loading.value)

        const collapsed = ref({})
        const plugin = server.plugins.adminDatabase
        const databases = plugin.databases
        const schemas = flatMap(x => x.schemas, databases)
        const show = ref()

        const open = ref(null)
        const filter = ref(null)
        const lastState = ref(null)
        const filters = computed(() => viewModelColumns.value || [])
        const filtersCount = computed(() => filters.value.reduce((acc,x) => acc + x.settings.filters.length,0) || 0)
        const columnsMap = ref({})
        const copied = ref(false)
        /** @type{Ref<{column?:string,topLeft?:{x:number,y:number}}>} */
        const showFilters = ref()
        const showQueryPrefs = ref(false)

        const prefs = ref(refreshPrefs())
        const hasPrefs = computed(() => settings.hasPrefs(routes.dbTable()))

        const selected = computed(() => routes.show && results.value.find(x => x.id === routes.show))
        const selectedClean = computed(() => {
            let row = selected.value
            if (!row) return
            if (row.id && row.Id) {
                return omit(row, ['id'])
            }
            return row
        })

        const type = computed(() => ({ name:routes.dbTable(), properties:viewModelColumns.value }))
        /** @type {Ref<MetadataPropertyType[]>} */
        const viewModelColumns = ref([])
        const columns = computed(() => {
            let only = prefs.value.selectedColumns.length > 0 ? prefs.value.selectedColumns : null
            let opColumns = only
                ? only.map(name => viewModelColumns.value.find(x => x.name === name))
                : viewModelColumns.value
            return opColumns.filter(x => !!x)
        })

        async function filtersChanged(column) {
            settings.saveTableProp(routes.dbTable(), column.name, x => Object.assign(x, column.settings))
            await update()
        }

        function onFilterDone() {
            showFilters.value = null
        }
        async function onFilterSave(colSettings) {
            let column = showFilters.value?.column
            if (column) {
                settings.saveTableProp(routes.dbTable(), column.name, x => Object.assign(x, colSettings))
            }
            await update()
            showFilters.value = null
        }

        function canFilter(name) {
            return true
        }

        /** @param {string} name
         *  @param {HTMLElement} e */
        function onHeaderSelected(column, e) {
            /** @type {HTMLElement} */
            let elTarget = e.target
            if (canFilter(name) && elTarget?.tagName !== 'TD') {
                let tableRect = elTarget?.closest('TABLE')?.getBoundingClientRect()
                //console.log('columns', columns.value, name)
                //let column = columns.value.find(x => x.name.toLowerCase() === name.toLowerCase())
                if (column && tableRect) {
                    let filterDialogWidth = 318
                    let minLeft = tableRect.x + filterDialogWidth + 10
                    showFilters.value = {
                        column,
                        topLeft: {
                            x: Math.max(Math.floor(e.clientX + filterDialogWidth / 2), minLeft),
                            y: tableRect.y + 45,
                        }
                    }
                }
            }
        }

        function toggle(row) { routes.to({ show: routes.show === row.id ? '' : row.id }) }
        function expanded(id) { return routes.show === id }
        function toggleSchema(db,schema) {
            let key = schemaKey(db,schema)
            collapsed.value[key] = !collapsed.value[key]
        }
        function isCollapsed(db,schema) { return collapsed.value[schemaKey(db,schema)] }
        function dbAlias(db) { return map(databases.find(x => x.name === db), x => x.alias) || db }
        function schemaAlias(db, schema) {
            return map(databases.find(x => x.name === db), x => map(x.schemas.find(s => s.name === schema), s => s.alias)) || schema
        }

        /** @param {*} row
         *  @param {MetadataPropertyType} column */
        function format(row, column) {
            return formatValue(mapGet(row,column.name), column?.format)
        }
        function fieldName(name) { return humanify(name) }
        /** @param {MetadataPropertyType} column */
        function complexProp(column) {
            return isComplexProp(column)
        }
        const refResults = ref()
        function selectColumn(column,e) {
            let dialogWidth = 318
            let tableRect = refResults.value.getBoundingClientRect()
            let div = e.target.tagName === 'DIV' ? e.target : e.target.closest('DIV')
            let rect = div.getBoundingClientRect()
            let minLeft = tableRect.x + dialogWidth + 25
            let args = { column, topLeft:{ x:Math.max(Math.floor(rect.x + rect.width), minLeft), y:Math.floor(rect.y + rect.height + 1) } }
            filter.value = args
        }
        function resolveApiUrl(op) {
            return combinePaths(apiUrlBase,op)
        }
        function copyApiUrl() {
            let args = createRequestArgs()
            let apiUrl = createUrl(resolveApiUrl('AdminDatabase'), { ...args, jsconfig:'edv' })
            copied.value = true
            copyText(apiUrl)
            setTimeout(() => copied.value = false, 3000)
        }
        function downloadCsv() {
            let args = createRequestArgs()
            let csvUrl = createUrl(resolveApiUrl('AdminDatabase'), { ...args, format:'csv', jsconfig:'edv' })
            window.open(csvUrl)
        }
        async function save() {
            lastState.value = null
            await update()
        }
        async function updatePrefs(prefs) {
            lastState.value = null
            settings.saveTable(routes.dbTable(), x => Object.assign(x, prefs))
            showQueryPrefs.value = false
            await update()
        }
        async function clearPrefs() {
            lastState.value = null
            settings.clearPrefs(routes.dbTable())
            await update()
        }

        function createRequestArgs() {
            let args = {
                db: routes.db,
                schema: routes.schema,
                table: routes.table,
                skip: skip.value,
                take: take.value,
                include: "total",
            }
            let selectedColumns = prefs.value.selectedColumns
            if (selectedColumns.length > 0) {
                args.fields = selectedColumns.join(',')
            }
            let orderBy = []
            filters.value.forEach(f => {
                if (f.settings.sort) orderBy.push((f.settings.sort === 'DESC' ? '-' : '') + f.name)
                f.settings.filters.forEach(filter => {
                    let converter = filterConverters[filter.key]
                    if (converter) {
                        let [k,v] = converter(f.name, filter.value)
                        args[k] = v
                    } else {
                        let k = filter.key.replace('%', f.name)
                        args[k] = filter.value
                    }
                })
            })
            let qs = queryString(location.search)
            Object.keys(qs).forEach(k => {
                let field = viewModelColumns.value.find(x => x.name === k)
                if (field) args[k] = qs[k]
            })
            if (orderBy.length > 0) {
                args.orderBy = orderBy.join(',')
            }
            return args
        }
        
        async function update() {
            skip.value = parseInt(routes.skip) || 0
            if (routes.table) {
                prefs.value = refreshPrefs()
                if ((columnsMap[routes.dbTable()] || []).length === 0) {
                    apiColumns.value = await client.api(new AdminDatabase({ db:routes.db, schema:routes.schema, table:routes.table, take:1, include:'columns' }))
                    columnsMap[routes.dbTable()] = apiColumns.value.response?.columns || []
                }
                refreshFilters()

                let args = createRequestArgs()
                let newState = appendQueryString(`${routes.dbTable()}:${routes.skip}>${results.value.length}`, args)
                let skipRefresh = newState === lastState.value
                if (skipRefresh) return
                lastState.value = newState

                api.value = await client.api(new AdminDatabase(args))
                /** populate row.id with PK if Id doesn't exist */
                api.value.response?.results?.filter(x => x.id == null).forEach(x => {
                    x.id = x.Id
                        || map(getPrimaryKeyByProps(type.value, viewModelColumns.value), pk => mapGet(x, pk.name))
                        || unknownIdSeq++
                })
            } else {
                api.value = new ApiResult()
            }
        }
        
        function clearFilters() {
            routes.to({ show:'' })
        }
        function min(num1,num2) { return Math.min(num1, num2) }

        const skip = ref(0)
        const take = computed(() => parseInt(prefs.value?.take || 25))
        const canFirst = computed(() => routes.skip > 0)
        const canPrev = computed(() => routes.skip > 0)
        const canNext = computed(() => results.value.length >= take.value)
        const canLast = computed(() => results.value.length >= take.value)
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
            skip,
            settings,
            apiLoading,
            results,
            total,
            showFilters,
            showQueryPrefs,

            definitions,
            prefs,
            hasPrefs,
            take,
            open,
            filter,
            filters,
            filtersCount,
            lastState,
            columnsMap,
            copied,

            collapsed,
            plugin,
            databases,
            schemas,

            show,
            selected,
            selectedClean,
            toggle,
            expanded,
            toggleSchema,
            isCollapsed,
            dbAlias,
            schemaAlias,
            min,

            prettyJson,
            type,
            viewModelColumns,
            columns,
            refResults,
            canFirst,
            canPrev,
            canNext,
            canLast,
            nextSkip,

            mapGet,            
            format,
            fieldName,
            complexProp,
            selectColumn,
            copyApiUrl,
            downloadCsv,
            save,
            updatePrefs,
            clearPrefs,
            createRequestArgs,
            update,
            onHeaderSelected,
            filtersChanged,
            onFilterDone,
            onFilterSave,
        }
    }
}
