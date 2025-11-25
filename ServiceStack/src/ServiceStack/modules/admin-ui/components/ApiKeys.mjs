import { computed, inject, onMounted, onUnmounted, ref, watch } from "vue"
import { useClient, useFormatters, css } from "@servicestack/vue";
import { ApiResult, apiValueFmt, humanify, mapGet } from "@servicestack/client"
import { AdminQueryApiKeys } from "dtos"
export const ApiKeys = {
    template:`
      <section v-if="!plugin">
          <div class="p-4 max-w-3xl">
            <Alert type="info">API Keys Admin UI is not enabled</Alert>
            <div class="my-4">
              <div>
                <p>
                    The <b>ApiKeysFeature</b> plugin needs to be configured with your App
                    <a href="https://docs.servicestack.net/auth/apikeys" class="ml-2 whitespace-nowrap font-medium text-blue-700 hover:text-blue-600" target="_blank">
                       Learn more <span aria-hidden="true">&rarr;</span>
                    </a>
                </p>
              </div>
            </div>
            <div>
                <p class="text-sm text-gray-700 mb-2">Quick start:</p>
                <CopyLine text="npx add-in apikeys" />
            </div>
          </div>
      </section>
      <section v-else id="apikeys">
        <form @submit.prevent="formSearch" class="mb-3">
          <div class="flex items-center">
            <TextInput id="query" type="search" v-model="request.search" label="" placeholder="Search API Keys" @search="formSearch" class="-mt-1" />
            <button class="ml-2 inline-flex items-center px-3 py-2.5 border border-gray-300 shadow-sm text-sm leading-4 font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
              Go
            </button>
            <div class="ml-3">
              <nav class="relative z-0 inline-flex rounded-md shadow-sm -space-x-px" aria-label="Pagination">
                <a v-href="{ page: Math.max(page - 1,0), $on:search }" title="Previous Page"
                   :class="[page > 0 ? 'text-gray-500 hover:bg-gray-50' : 'text-gray-300 cursor-text', 'relative inline-flex items-center px-2 py-2 rounded-l-md border border-gray-300 bg-white text-sm font-medium']">
                  <span class="sr-only">Previous</span>
                  <!---: Heroicon name: solid/chevron-left -->
                  <svg class="h-5 w-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                    <path fill-rule="evenodd" d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z" clip-rule="evenodd" />
                  </svg>
                </a>
                <a v-href="{ page: results.length < pageSize ? page : page + 1, $on:search }" title="Next Page"
                   :class="[results.length >= pageSize ? 'text-gray-500 hover:bg-gray-50' : 'text-gray-300 cursor-text', 'relative inline-flex items-center px-2 py-2 rounded-r-md border border-gray-300 bg-white text-sm font-medium']">
                  <span class="sr-only">Next</span>
                  <!---: Heroicon name: solid/chevron-right -->
                  <svg class="h-5 w-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                    <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd" />
                  </svg>
                </a>
              </nav>
            </div>
            <div class="ml-3 align-middle">
              <p class="text-sm text-gray-700">
                <span class="hidden lg:inline mr-1">Showing results</span>
                <span class="whitespace-nowrap">
                        <span class="font-medium">{{ (page * pageSize) + 1 }}</span>
                        to
                        <span class="font-medium">{{ (page * pageSize) + results.length }}</span>
                    </span>
              </p>
            </div>
            <a v-href="{ new:1,edit:null }" class="ml-3 inline-flex items-center px-3 py-2.5 border border-gray-300 shadow-sm text-sm leading-4 font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
              New
              <span class="hidden md:ml-1 md:inline">API Key</span>
            </a>
          </div>
        </form>
        <CreateApiKeyForm v-if="routes.new" @done="done" class="mt-2 max-w-screen-md" :key="renderKey" />
        <EditApiKeyForm v-else-if="routes.edit" :id="routes.edit" @done="done" class="mt-2 max-w-screen-md" :key="renderKey+1000" />
        <div class="w-full overflow-scroll px-1 -ml-1">
          <DataGrid v-if="results.length" :items="results"
                    @row-selected="rowSelected" :is-selected="row => routes.edit === row.id"
                    :rowClass="(row,i) => !row.active ? 'cursor-pointer hover:bg-yellow-50 bg-red-100' : css.grid.getTableRowClass('stripedRows', i, routes.edit === row.id, true)"
                    :selectedColumns="columns">
            <template #id-header><SortableColumn name="id" /></template>
            <template #userName-header><SortableColumn name="userName" /></template>
            <template #name-header><SortableColumn name="name" /></template>
            <template #visibleKey-header><SortableColumn name="visibleKey" alias="Secret Key" /></template>
            <template #createdDate-header><SortableColumn name="createdDate" alias="Created" /></template>
            <template #expiryDate-header><SortableColumn name="expiryDate" alias="Expires" /></template>
            <template #lastUsedDate-header><SortableColumn name="lastUsedDate" alias="Last Used" /></template>
            
            <template #createdDate="{createdDate}">
              {{formatDate(createdDate)}}
            </template>
            <template #expiryDate="{expiryDate}">
              {{formatDate(expiryDate)}}
            </template>
            <template #scopes="{scopes}">
                <span v-if="scopes.length" :title="scopes.join('\\n')">
                    {{scopes.length}} {{scopes.slice(0,2).join(', ') + (scopes.length > 2 ? '...' : '')}}
                </span>
            </template>
            <template #features="{features}">
                <span v-if="features.length" :title="features.join('\\n')">
                    {{features.length}} {{features.slice(0,2).join(', ') + (features.length > 2 ? '...' : '')}}
                </span>
            </template>
            <template #lastUsedDate="{lastUsedDate}">
              <span v-if="lastUsedDate">
                {{relativeTime(lastUsedDate)}}
              </span>
            </template>
          </DataGrid>
        </div>
    </section>
    `,
    setup(props) {
        const routes = inject('routes')
        const store = inject('store')
        const server = inject('server')
        const plugin = server.plugins.apiKey
        const client = useClient()
        const { formatDate, relativeTime } = useFormatters()
        const renderKey = ref(1)
        const request = ref(new AdminQueryApiKeys())
        const api = ref(new ApiResult())
        const results = computed(() => api.value?.response?.results || [])
        const columns = 'id,userName,name,visibleKey,createdDate,expiryDate'.split(',')
        if (plugin.scopes.length) {
            columns.push('scopes')
        }
        if (plugin.features.length) {
            columns.push('features')
        }
        columns.push('lastUsedDate')
        const pageSize = 25
        const page = computed(() => routes.page ? parseInt(routes.page) : 0)
        const link = computed(() => store.adminLink('apikeys'))
        const loading = computed(() => client.loading.value)
        function onKeyDown(e) {
            if (e.key === 'Escape' && (routes.new || routes.edit)) {
                close()
            }
        }
        function close() {
            routes.to({ new:null, edit:null })
        }
        function toggle(row) {
            if (routes.edit !== row.Id)
                routes.to({ new:null, edit:row.Id, $on:nav })
            else
                routes.to({ new:null, edit:null })
        }
        function expanded(id) { return routes.edit === id }
        async function formSearch() {
            routes.to({ new:null, edit:null, page:0, q:request.value.search })
            await search()
        }
        async function search() {
            request.value.orderBy = routes.sort ? routes.sort : '-id'
            request.value.skip = routes.page > 0 ? pageSize * Number(routes.page || 1) : 0
            request.value.take = pageSize
            api.value = await client.api(request.value, { jsconfig: 'eccn' })
        }
        function sortBy(field) {
            return routes.sort === field
                ? '-' + field
                : routes.sort === '-' + field
                    ? ''
                    : field
        }
        
        watch(() => routes.sort, () => {
            search()
        })
        async function update() {
            request.value = new AdminQueryApiKeys({ search:routes.q })
            await search()
        }
        onMounted(() => {
            update()
            document.addEventListener('keydown', onKeyDown)
        })
        onUnmounted(() => {
            document.removeEventListener('keydown', onKeyDown)
        })
        function nav() {
            renderKey.value++
        }
        async function done() {
            routes.to({ 'new':null, edit:null})
            await search()
        }
        function rowSelected(row) {
            routes.to({ 'new':null, edit: routes.edit === row.id ? null : row.id })
            renderKey.value++
        }
        return {
            css,
            client,
            store,
            server,
            plugin,
            routes,
            renderKey,
            link,
            loading,
            pageSize,
            page,
            request,
            onKeyDown,
            toggle,
            expanded,
            formSearch,
            search,
            sortBy,
            api,
            results,
            columns,
            apiValueFmt,
            humanify,
            mapGet,
            nav,
            close,
            done,
            formatDate,
            relativeTime,
            rowSelected,
        }
    }
}
