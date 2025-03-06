import { inject, nextTick, onMounted, onUnmounted, ref, computed, watch } from "vue"
import { useClient, useMetadata } from "@servicestack/vue"
import { AdminGetRoles, AdminGetRolesResponse, AdminGetRole, AdminGetRoleResponse, AdminCreateRole, AdminUpdateRole, AdminDeleteRole, Property } from "dtos"
import { apiValueFmt, map, mapGet, humanify, ApiResult } from "@servicestack/client"

/**: cheap nav update without creating multiple App.events subs per child component */
let adminNav = null

const gridClass = 'grid grid-cols-12 gap-6'

export const NewRole = {
    template: `
      <SlideOver title="New Role" @done="close" content-class="relative flex-1">

        <form @submit.prevent="submit" autocomplete="off">
            <ErrorSummary :except="exceptFields" />
            <div class="pt-4">
              <input type="submit" class="hidden">
              <AutoFormFields :api="api" v-model="request" space-class="" />
            </div>
        </form>
        
        <template #footer>
          <div class="flex flex-wrap justify-between">
            <SecondaryButton @click="close" :disabled="loading">Cancel</SecondaryButton>
            <PrimaryButton class="ml-4" :disabled="loading" @click="submit">Create Role</PrimaryButton>
          </div>
        </template>
      </SlideOver>
    `,
    emits: ['done'],
    setup(props, { emit }) {
        const store = inject('store')
        const routes = inject('routes')
        const client = useClient()
        const exceptFields = ['name']
        const request = ref(new AdminCreateRole())
        const model = ref({ newRole:'', newPermission:'' })
        const api = ref(new ApiResult())

        const loading = computed(() => client.loading.value)

        async function submit() {
            api.value = await client.api(request.value, { jsconfig: 'eccn' })
            if (api.value.succeeded) emit('done')
        }

        function close() {
            emit('done')
        }

        return {
            routes,
            request,
            model,
            loading,
            api,
            exceptFields,
            submit,
            close,
        }
    }
}
export const EditRole = {
    template: `
      <SlideOver title="Edit Role" @done="close" content-class="relative flex-1">
        <form @submit.prevent="submit" autocomplete="off">
          <div>
            <input type="submit" class="hidden">
            <AutoFormFields v-if="showForm" :api="api" v-model="request" />
          </div>
        </form>
        
        <div class="mt-8 pt-4 border-t border-gray-900/10 px-4 sm:px-6">
          <h2 class="mb-3 text-lg font-medium text-gray-900 dark:text-gray-50" id="SlideOver-title">Manage Claims</h2>
          <div v-if="claims.length" class="mb-2 shadow overflow-hidden border-b border-gray-200 sm:rounded-lg">
            <table class="min-w-full divide-y divide-gray-200">
              <thead class="bg-gray-50">
              <tr>
                <th class="group cursor-pointer px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider whitespace-nowrap">Name</th>
                <th class="group cursor-pointer px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider whitespace-nowrap">Value</th>
                <th class="w-8"></th>
              </tr>
              </thead>
              <tbody>
              <tr v-for="(row,index) in claims" :key="row.id"
                  :class="[index % 2 === 0 ? 'bg-white' : 'bg-gray-50']">
                <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                  {{row.name}}
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                  {{row.value}}
                </td>
                <td class="w-8">
                  <span title="Delete Role" class="cursor-pointer" @click="deleteClaim(row)"><svg class="w-5 h-5 cursor-pointer hover:text-red-700" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32"><path fill="currentColor" d="M12 12h2v12h-2zm6 0h2v12h-2z"></path><path fill="currentColor" d="M4 6v2h2v20a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V8h2V6zm4 22V8h16v20zm4-26h8v2h-8z"></path></svg></span>
                </td>
              </tr>
              </tbody>
            </table>
          </div>
          <form @submit.prevent="addClaim" autocomplete="off">
            <input type="submit" class="hidden">
            <div class="pt-2 flex items-end">
              <div class="flex-grow grid grid-cols-6 gap-2">
                <div class="col-span-6 sm:col-span-3">
                  <TextInput id="claimName" v-model="newClaim.name" required placeholder="Name" />
                </div>
                <div class="col-span-6 sm:col-span-3">
                  <TextInput id="claimValue" v-model="newClaim.value" required placeholder="Value" />
                </div>
              </div>
              <div class="flex-shrink">
                <SecondaryButton class="ml-2 mt-2" @click="addClaim" :disabled="!newClaim.name || !newClaim.value">Add Claim</SecondaryButton>
              </div>
            </div>
          </form>
        </div>
                
        <template #footer>
          <div class="flex flex-wrap justify-between">
            <div>
              <ConfirmDelete @delete="deleteRole" />
            </div>
            <div>
              <FormLoading v-if="loading" />
            </div>
            <div class="flex justify-end">
              <SecondaryButton @click="close" :disabled="loading">Cancel</SecondaryButton>
              <PrimaryButton class="ml-4" :disabled="loading" @click="submit">Update Role</PrimaryButton>
            </div>
          </div>
        </template>
      </SlideOver>
    `,
    props: ['id'],
    emits: ['done','save'],
    setup(props, { emit }) {
        const store = inject('store')
        const routes = inject('routes')
        const client = useClient()
        const { toFormValues } = useMetadata()
        const exceptFields = ['name']
        
        const orig = ref(new AdminGetRoleResponse({ claims:[] }))
        const newClaim = ref(new Property({name:'',value:''}))
        const addClaims = ref([])
        const removeClaims = ref([])
        const claims = computed(() => orig.value.claims.filter(x => 
                !removeClaims.value.find(c => c.name === x.name && c.value === x.value)
            ).concat(addClaims.value))

        const request = ref(new AdminUpdateRole({ id:props.id }))
        const model = ref({ newRole:'', newPermission:'', password:'', confirmDelete: false })
        const origRoles = ref([])
        const roles = ref([])

        const loading = computed(() => client.loading.value)
        const api = ref(new ApiResult())
        const formFields = ref()

        function done() { emit('done') }
        function save() { emit('save') }

        async function send(request, success, error) {
            api.value = await client.api(request, { jsconfig: 'eccn' })
            if (api.value.error) {
                if (error) error(api.value.error)
            } else {
                if (success) success(api.value.response)
            }
        }

        async function deleteRole() {
            await send(new AdminDeleteRole({ id:props.id }), save)
        }
        async function submit() {
            if (removeClaims.value.length) {
                request.value.removeClaims = removeClaims.value
            }
            if (addClaims.value.length) {
                request.value.addClaims = addClaims.value 
            }
            await send(request.value, save)
        }

        /** @param {AdminGetRoleResponse} response */
        function bind(response) {
            orig.value = response
            request.value = new AdminUpdateRole(response.result)
            console.log(JSON.stringify(response, undefined, 2))

            // Hack needed for <AutoFormFields /> to remount itself + bind to updated data
            showForm.value = false
            nextTick(() => showForm.value = true)
        }

        async function update() {
            await send(new AdminGetRole({ id:props.id }), response => bind(response))
        }
        onMounted(async () => {
            adminNav = update
            await update()
        })

        const showForm = ref(true)
        function updateRequest(value) {
            console.debug('updateRequest', value)
        }
        function close() {
            emit('done')
        }
        
        function addClaim() {
            const existingClaim = orig.value.claims.find(x => x.name === newClaim.value.name && x.value === newClaim.value.value)
            if (existingClaim) return
            addClaims.value.push(newClaim.value)
            newClaim.value = new Property()
        }
        
        function deleteClaim(claim) {
            const existingClaim = orig.value.claims.find(x => x.name === claim.name && x.value === claim.value)
            if (existingClaim) {
                if (!removeClaims.value.find(x => x.name === existingClaim.name && x.value === existingClaim.value)) {
                    removeClaims.value = [...removeClaims.value, claim]
                }
            } else {
                addClaims.value = addClaims.value.filter(x => x.name !== claim.name && x.value !== claim.value)
            }
        }

        return {
            store,
            routes,
            request,
            exceptFields,
            loading,
            api,
            roles,
            origRoles,
            model,
            send,
            deleteRole,
            submit,
            mapGet,
            bind,
            apiValueFmt,
            updateRequest,
            formFields,
            showForm,
            close,
            orig,
            newClaim,
            claims,
            addClaim,
            deleteClaim,
        }
    }
}

export const IdentityRoles = {
    components: { NewRole, EditRole },
    template:/*html*/`
<section id="admin-users">
  <a v-href="{ new:1,edit:null }" class="inline-flex items-center px-3 py-2.5 border border-gray-300 shadow-sm text-sm leading-4 font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
    New
    <span class="hidden md:ml-1 md:inline">Role</span>
  </a>
  
  <EditRole v-if="routes.edit" :id="routes.edit" @done="close" @save="formSearch" />
  <NewRole v-else-if="routes.new" @done="formSearch" />

  <Loading v-if="loading" />
    <div v-else-if="results.length" class="-my-2 overflow-x-auto sm:-mx-6 lg:-mx-8">
        <div class="py-2 align-middle inline-block min-w-full sm:px-6 lg:px-8">
            <div class="shadow overflow-hidden border-b border-gray-200 sm:rounded-lg my-3">
                <table class="min-w-full divide-y divide-gray-200">
                    <thead class="bg-gray-50">
                    <tr>
                        <th v-for="name in fieldNames" scope="col" 
                            class="w-1/2 group cursor-pointer px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider whitespace-nowrap">
                            <a v-href="{ sort:sortBy(name), $on:search }" class="flex items-center">
                                <div>{{humanify(name)}}</div>
                                <svg v-if="name === request.orderBy || '-' + name === request.orderBy" 
                                     :class="[name === request.orderBy ? 'rotate-180' : '','mr-2 text-gray-400 flex-shrink-0 h-5 w-5 transform group-hover:text-gray-400 transition-colors ease-in-out duration-150']" viewBox="0 0 32 32" aria-hidden="true">
                                    <path d="M24 12l-8 10l-8-10z" fill="currentColor"/>
                                </svg>
                                <svg v-else class="mr-2 text-gray-50 group-hover:text-gray-400 flex-shrink-0 h-5 w-5 transform group-hover:text-gray-400 transition-colors ease-in-out duration-150']" viewBox="0 0 32 32" aria-hidden="true">
                                    <path d="M24 12l-8 10l-8-10z" fill="currentColor"/>
                                </svg>
                            </a>
                        </th>
                    </tr>
                    </thead>
                    <tbody>
                    <tr v-for="(row,index) in results" :key="mapGet(row, 'Id')" @click="toggle(row)"
                        :class="['w-1/2 cursor-pointer', expanded(mapGet(row, 'Id')) ? 'bg-indigo-100' : 'hover:bg-yellow-50 ' + (index % 2 === 0 ? 'bg-white' : 'bg-gray-50')]">
                        <td v-for="name in fieldNames" :key="mapGet(row, 'Id')" class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                            {{apiValueFmt(mapGet(row,name))}}
                        </td>
                    </tr>
                    </tbody>
                </table>
            </div>
        </div>
    </div> 
</section>
    `,
    setup() {
        const routes = inject('routes')
        const store = inject('store')
        const client = useClient()
        const refreshKey = ref(1)

        const request = ref(new AdminGetRoles())
        /** @type {Ref<ApiResult<AdminGetRolesResponse>>} */
        const api = ref(new ApiResult())

        const results = computed(() => api.value?.response?.results || [])
        const fieldNames = ['id','name']
        
        const link = computed(() => store.adminLink('users'))
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
                routes.to({ new:null, edit:mapGet(row,'Id'), $on:nav })
            else
                routes.to({ new:null, edit:null })
        }
        function expanded(id) { return routes.edit === id }

        async function formSearch() {
            routes.to({ new:null, edit:null })
            await search()
        }

        async function search() {
            request.value.orderBy = routes.sort ? routes.sort : null

            api.value = await client.api(request.value, { jsconfig: 'eccn' })
        }

        function sortBy(field) {
            return routes.sort === field
                ? '-' + field
                : routes.sort === '-' + field
                    ? ''
                    : field
        }

        async function update() {
            request.value = new AdminGetRoles()
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
            refreshKey.value++
            if (adminNav) adminNav()
        }

        return {
            client,
            store,
            routes,
            refreshKey,
            link,
            loading,
            request,
            onKeyDown,
            toggle,
            expanded,
            formSearch,
            search,
            sortBy,
            api,
            results,
            fieldNames,
            apiValueFmt,
            humanify,
            mapGet,
            nav,
            close,
        }

    }
}
