import { inject, nextTick, onMounted, onUnmounted, ref, computed, watch } from "vue"
import { useClient, useMetadata } from "@servicestack/vue"
import { AdminCreateUser, AdminDeleteUser, AdminGetUser, AdminQueryUsers, AdminUpdateUser } from "dtos"
import { apiValueFmt, map, mapGet, humanify, ApiResult, toCamelCase, dateFmt } from "@servicestack/client"
import { mapGetForInput } from "core"

/**: cheap nav update without creating multiple App.events subs per child component */
let adminUsersNav = null

const formClass = 'shadow overflow-hidden sm:rounded-md bg-white max-w-screen-lg'
const gridClass = 'grid grid-cols-12 gap-6'
const dtoProps = 'userName,email,password,roles'.split(',')


function createForm(server) {
    let formLayout = map(server.plugins.adminIdentityUsers, x => x.formLayout) || []
    let inputIds = formLayout.map(input => input.id)
    let exceptFields = [...formLayout.map(x => x.id), 'password', 'id']
    let defaults = {
        roles: [],
        permissions: [],
        addRoles: [],
        removeRoles: [],
    }
    function init(model,dtoProps) {
        inputIds.forEach(id => {
            model[id] = defaults[id] || null
            // if (dtoProps.indexOf(id) >= 0)
            //     model[id] = defaults[id] || null
            // else
            //     model.userAuthProperties[id] = defaults[id] || null
        })
        dtoProps.forEach(id => model[id] = defaults[id] || null)
        return model
    }
    formLayout = formLayout.map(f => {
        //f.id = toCamelCase(f.id)
        if (!f.css) f.css = { field: 'col-span-12' }
        return f
    })
    return { formLayout, inputIds, exceptFields, init }
}

export const NewUser = {
    template: `
      <SlideOver title="New User" @done="close" content-class="relative flex-1">

        <form @submit.prevent="submit" autocomplete="off">
            <ErrorSummary :except="exceptFields" />
            <div class="pt-4">
              <input type="submit" class="hidden">
              <AutoFormFields :formLayout="formLayout" :api="api" v-model="request" :hideSummary="true" space-class="" />
              <div class="mt-8 pt-4 border-t border-gray-900/10 px-4 sm:px-6">
                <fieldset :class="gridClass">
                  <div class="col-span-6">
                    <TextInput id="password" type="password" v-model="request.password" autocomplete="new-password" />
                  </div>
                  <div class="col-span-6">
                    <div v-if="request.roles.length > 0">
                      <label class="mb-2 block text-sm font-medium text-gray-700">Add Roles</label>
                      <div v-for="role in request.roles" class="mb-2 flex items-center">
                        <svg @click="request.roles = request.roles.filter(x => x !== role)"
                             class="mr-1 w-5 h-5 text-gray-500 hover:text-gray-700 cursor-pointer" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                          <title>Remove Role</title>
                          <g fill="none">
                            <path d="M19 7l-.867 12.142A2 2 0 0 1 16.138 21H7.862a2 2 0 0 1-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 0 0-1-1h-4a1 1 0 0 0-1 1v3M4 7h16" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                          </g>
                        </svg>
                        <span>{{ role }}</span>
                      </div>
                    </div>
                    <div v-if="missingRoles.length > 1" class="col-span-6 flex items-end">
                      <SelectInput id="newRole" v-model="model.newRole" :values="missingRoles" class="w-full" />
                      <div>
                        <button type="button" @click="addRole" :disabled="!model.newRole" :class="[model.newRole ? 'text-gray-700 hover:bg-gray-50' : 'text-gray-400',
                               'ml-2 inline-flex items-center px-3 py-2.5 border border-gray-300 shadow-sm text-sm leading-4 font-medium rounded-md bg-white focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500']">
                          Add
                        </button>
                      </div>
                    </div>
                  </div>
                </fieldset>
              </div>
            </div>
        </form>
        
        <template #footer>
          <div class="flex flex-wrap justify-between">
            <SecondaryButton @click="close" :disabled="loading">Cancel</SecondaryButton>
            <PrimaryButton class="ml-4" :disabled="loading" @click="submit">Create User</PrimaryButton>
          </div>
        </template>
      </SlideOver>
    `,
    emits: ['done'],
    setup(props, { emit }) {
        const store = inject('store')
        const routes = inject('routes')
        const client = useClient()
        const { formLayout, inputIds, exceptFields, init } = createForm(inject('server'))
        const request = ref(init(new AdminCreateUser({ userAuthProperties:{} }), dtoProps))
        const model = ref({ newRole:'', newPermission:'' })
        const api = ref(new ApiResult())

        const loading = computed(() => client.loading.value)

        function addRole() {
            request.value.roles.push(model.value.newRole)
            model.value.newRole = ''
        }
        const missingRoles = computed(() => ['', ...store.adminIdentityUsers.allRoles.filter(x => request.value.roles.indexOf(x) < 0)])

        async function submit() {
            const requestDto = new AdminCreateUser({ userAuthProperties:{ } })
            Object.keys(request.value).forEach(k => {
                const value = mapGet(request.value, k)
                if (!value) return;
                
                if (dtoProps.some(x => x.toLowerCase() === k.toLowerCase())) {
                    requestDto[k] = value
                } else {
                    requestDto.userAuthProperties[k] = value
                }
            })

            api.value = await client.api(requestDto, { jsconfig: 'eccn' })
            if (api.value.succeeded) emit('done')
        }

        function close() {
            emit('done')
        }

        return {
            routes,
            dtoProps,
            request,
            model,
            loading,
            api,
            exceptFields,
            formLayout,
            formClass,
            gridClass,
            addRole,
            missingRoles,
            submit,
            close,
        }
    }
}
export const EditUser = {
    template: `
      <SlideOver title="Edit User" @done="close" content-class="relative flex-1">

        <form @submit.prevent="submit" autocomplete="off">
          <ErrorSummary :except="exceptFields" />
          <div class="pt-4">
            <input type="submit" class="hidden">
            <AutoFormFields v-if="showForm" ref="formFields" :formLayout="formLayout" :api="api" :hideSummary="true" space-class="" v-model="request" />
            <div class="mt-8 pt-4 border-t border-gray-900/10 px-4 sm:px-6">
              <fieldset :class="gridClass">
                <div class="col-span-6">
                  <div class="flex justify-between w-full">
                    <TextInput id="password" type="password" v-model="model.password" autocomplete="current-password" class="mr-2 w-full" />
                    <div class="flex items-end">
                      <PrimaryButton type="button" color="red" @click="changePassword" :disabled="!model.password">Change</PrimaryButton>
                    </div>
                  </div>
                  <div class="pt-4 flex items-end">
                    <div v-if="!lockedDate" class="mr-2">
                      <PrimaryButton type="button" color="red" @click="lockUser">Lock User</PrimaryButton>
                    </div>
                    <div v-else class="flex items-center justify-between w-full">
                      <div class="text-gray-700">Locked {{lockedDate}}</div>
                      <PrimaryButton color="red" type="button" @click="unlockUser">Unlock User</PrimaryButton>
                    </div>
                  </div>
                </div>
                <div class="col-span-6">
                  <div v-if="roles.length > 0">
                    <label class="mb-2 block text-sm font-medium text-gray-700">Add Roles</label>
                    <div v-for="role in roles" class="mb-2 flex items-center">
                      <svg @click="roles = roles.filter(x => x !== role)" class="mr-1 w-5 h-5 text-gray-500 hover:text-gray-700 cursor-pointer"
                           xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                        <title>Remove Role</title>
                        <g fill="none"><path d="M19 7l-.867 12.142A2 2 0 0 1 16.138 21H7.862a2 2 0 0 1-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 0 0-1-1h-4a1 1 0 0 0-1 1v3M4 7h16" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></g>
                      </svg>
                      <span>{{role}}</span>
                    </div>
                  </div>
                  <div v-if="missingRoles.length > 1" class="flex items-end">
                    <SelectInput id="newRole" v-model="model.newRole" :values="missingRoles" class="w-full mr-2" />
                    <OutlineButton @click="addRole" :disabled="!model.newRole">Add</OutlineButton>
                  </div>
                </div>
              </fieldset>
            </div>
          </div>
        </form>

        <template #footer>
          <div class="flex flex-wrap justify-between">
            <div>
              <ConfirmDelete @delete="deleteUser" />
            </div>
            <div>
              <FormLoading v-if="loading" />
            </div>
            <div class="flex justify-end">
              <SecondaryButton @click="close" :disabled="loading">Cancel</SecondaryButton>
              <PrimaryButton class="ml-4" :disabled="loading" @click="submit">Update User</PrimaryButton>
            </div>
          </div>
        </template>
      </SlideOver>
    `,
    props: ['id'],
    emits: ['done'],
    setup(props, { emit }) {
        const store = inject('store')
        const routes = inject('routes')
        const client = useClient()
        const { toFormValues } = useMetadata()
        const { formLayout, inputIds, exceptFields, init } = createForm(inject('server'))

        const request = ref(init(new AdminUpdateUser({ id:props.id }), dtoProps))
        const model = ref({ newRole:'', newPermission:'', password:'', confirmDelete: false })
        const successMessage = ref('')
        const lockedDate = ref(null)
        const origRoles = ref([])
        const roles = ref([])

        const loading = computed(() => client.loading.value)
        const api = ref(new ApiResult())
        const formFields = ref()

        function done() { emit('done') }

        function isDtoProp(id) { return dtoProps.indexOf(id) >= 0 }

        function addRole() {
            roles.value.push(model.value.newRole)
            model.value.newRole = ''
        }
        const missingRoles = computed(() => ['', ...store.adminIdentityUsers.allRoles.filter(x => roles.value.indexOf(x) < 0)])

        async function send(request, success, error) {
            successMessage.value = ''
            api.value = await client.api(request, { jsconfig: 'eccn' })
            if (api.value.error) {
                if (error) error(api.value.error)
            } else {
                if (success) success(api.value.response)
            }
        }

        async function changePassword() {
            await send(new AdminUpdateUser({ id:props.id, password:model.value.password }), response => {
                model.value.password = ''
                successMessage.value = 'Password was changed'
                bind(response)
            })
        }

        async function deleteUser() {
            await send(new AdminDeleteUser({ id:props.id }), done)
        }
        async function lockUser() {
            await send(new AdminUpdateUser({ id:props.id, lockUser:true }), response => bind(response))
        }
        async function unlockUser() {
            await send(new AdminUpdateUser({ id:props.id, unlockUser:true }), response => bind(response))
        }
        async function submit() {
            const requestDto = new AdminUpdateUser({ id:props.id, userAuthProperties:{ } })
            const dtoPropsLower = dtoProps.map(x => x.toLowerCase())
            Object.keys(request.value).forEach(k => {
                if (dtoPropsLower.includes(k.toLowerCase())) {
                    requestDto[toCamelCase(k)] = request.value[k]
                } else {
                    requestDto.userAuthProperties[toCamelCase(k)] = request.value[k]
                }
            })

            requestDto.addRoles = roles.value.filter(x => origRoles.value.indexOf(x) < 0)
            requestDto.removeRoles = origRoles.value.filter(x => roles.value.indexOf(x) < 0)
            await send(requestDto, done)
        }

        function bind(response) {
            const requestDto = init(new AdminUpdateUser(), dtoProps)
            const result = response.result
            Object.keys(result).forEach(k => requestDto[k] = result[k])
            requestDto.id = props.id

            const lockoutEnd = mapGet(result, 'lockoutEnd')
            if (lockoutEnd) {
                const date = new Date(lockoutEnd)
                lockedDate.value = date.getFullYear() > (new Date().getFullYear() + 100)
                    ? 'indefinitely'
                    : `until ${dateFmt(date)}`   
            } else {
                lockedDate.value = null
            }
            roles.value = mapGet(result, 'roles') || []
            origRoles.value = [...roles.value]

            request.value = toFormValues(requestDto)

            // Hack needed for <AutoFormFields /> to remount itself + bind to updated data
            showForm.value = false
            nextTick(() => showForm.value = true)
        }

        async function update() {
            await send(new AdminGetUser({ id:props.id }), response => bind(response))
        }
        onMounted(async () => {
            adminUsersNav = update
            await update()
        })

        const showForm = ref(true)
        function updateRequest(value) {
            console.log('updateRequest', value)
        }
        function close() {
            emit('done')
        }

        return {
            routes,
            request,
            exceptFields,
            isDtoProp,
            successMessage,
            loading,
            api,
            lockedDate,
            roles,
            origRoles,
            model,
            formLayout,
            formClass,
            gridClass,
            addRole,
            missingRoles,
            send,
            changePassword,
            deleteUser,
            lockUser,
            unlockUser,
            submit,
            mapGet,
            bind,
            apiValueFmt,
            updateRequest,
            formFields,
            showForm,
            close,
        }
    }
}

export const IdentityUsers = {
    components: { NewUser, EditUser },
    template:/*html*/`
<section id="admin-users">
    <form @submit.prevent="formSearch" class="mb-3">
        <div class="flex items-center">
            <TextInput id="query" type="search" v-model="request.query" label="" placeholder="Search Users" @search="formSearch" />
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
                <span class="hidden md:ml-1 md:inline">User</span>
            </a>
        </div>
    </form>
  
  <EditUser v-if="routes.edit" :id="routes.edit" @done="formSearch" />
  <NewUser v-else-if="routes.new" @done="formSearch" />

  <Loading v-if="loading" />
    <div v-else-if="results.length" class="-my-2 overflow-x-auto sm:-mx-6 lg:-mx-8">
        <div class="py-2 align-middle inline-block min-w-full sm:px-6 lg:px-8">
            <div class="shadow overflow-hidden border-b border-gray-200 sm:rounded-lg my-3">
                <table class="min-w-full divide-y divide-gray-200">
                    <thead class="bg-gray-50">
                    <tr>
                        <th v-for="name in fieldNames" scope="col" 
                            class="group cursor-pointer px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider whitespace-nowrap">
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
                    <tr v-for="(row,index) in results" key="mapGet(row, 'Id')" @click="toggle(row)"
                        :class="['cursor-pointer', expanded(row.Id) ? 'bg-indigo-100' : 'hover:bg-yellow-50 ' + (index % 2 === 0 ? 'bg-white' : 'bg-gray-50')]">
                        <td v-for="name in fieldNames" key="mapGet(row, 'Id')" class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
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

        const request = ref(new AdminQueryUsers({ query:routes.q }))
        /** @type {Ref<ApiResult<AdminQueryUsersResponse>>>} */
        const api = ref(new ApiResult())

        const results = computed(() => api.value?.response?.results || [])
        const fieldNames = computed(() => store.adminIdentityUsers.queryIdentityUserProperties || [])

        const pageSize = 25
        const page = computed(() => routes.page ? parseInt(routes.page) : 0)
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
                routes.to({ new:null, edit:row.Id, $on:nav })
            else
                routes.to({ new:null, edit:null })
        }
        function expanded(id) { return routes.edit === id }

        async function formSearch() {
            routes.to({ new:null, edit:null, page:0, q:request.value.query })
            await search()
        }

        async function search() {
            request.value.orderBy = routes.sort ? routes.sort : null
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

        async function update() {
            request.value = new AdminQueryUsers({ query:routes.q })
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
            if (adminUsersNav) adminUsersNav()
        }

        return {
            client,
            store,
            routes,
            refreshKey,
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
            fieldNames,
            apiValueFmt,
            humanify,
            mapGet,
            nav,
            close,
        }

    }
}
