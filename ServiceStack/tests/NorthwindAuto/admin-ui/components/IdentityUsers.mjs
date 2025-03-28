import { inject, nextTick, onMounted, onUnmounted, ref, computed, watch } from "vue"
import { useClient, useFiles, useMetadata, useFormatters } from "@servicestack/vue"
import { AdminCreateUser, AdminDeleteUser, AdminGetUser, AdminQueryUsers, AdminUsersResponse, AdminUpdateUser, AdminUserResponse, Property, GetAnalyticsReports } from "dtos"
import { apiValueFmt, map, mapGet, humanify, ApiResult, toCamelCase, dateFmt, leftPart } from "@servicestack/client"
import { createStatusCodesChart, createDurationRangesChart, createRequestsChart, mapCounts, sortedDetailRequests, hiddenApiKey } from "charts"

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

const Analytics = {
    template:`
      <div class="mt-8 pt-4 border-t border-gray-900/10 px-4 sm:px-6">
        <h2 class="float-left mb-3 text-lg font-medium text-gray-900 dark:text-gray-50" id="SlideOver-title">Analytics</h2>
        <div>
          <div class="mb-2 flex flex-wrap justify-center">
            <template v-for="year in years">
              <b v-if="year === (routes.year || new Date().getFullYear().toString())" class="ml-3 text-sm font-semibold">
                {{ year }}
              </b>
              <a v-else v-href="{ year }" class="ml-3 text-sm text-indigo-700 font-semibold hover:underline">
                {{ year }}
              </a>
            </template>
          </div>

          <div class="mb-4 flex flex-wrap justify-center">
            <template v-for="month in months.filter(x => x.startsWith(routes.year || new Date().getFullYear().toString()))">
                <span v-if="month === (routes.month || (new Date().getFullYear() + '-' + (new Date().getMonth() + 1).toString().padStart(2,'0')))" class="mr-2 mb-2 text-xs leading-5 font-semibold bg-indigo-600 text-white rounded-full py-1 px-3 flex items-center space-x-2">
                  {{ new Date(month + '-01').toLocaleString('default', { month: 'long' }) }}
                </span>
              <a v-else v-href="{ month }" class="mr-2 mb-2 text-xs leading-5 font-semibold bg-slate-400/10 rounded-full py-1 px-3 flex items-center space-x-2 hover:bg-slate-400/20 dark:highlight-white/5">
                {{ new Date(month + '-01').toLocaleString('default', { month: 'short' }) }}
              </a>
            </template>
          </div>
          
          <div v-if="analytics">
            <div class="w-full">
              <div>
                <div class="float-right text-sm">
                  <div>
                    <span class="pr-2">Total Requests</span>
                    <span>{{humanifyNumber(analytics.totalRequests)}}</span>
                  </div>
                </div>
                <LogLinks title="User" :links="userLinks" :filter="{ userId:id }" />
              </div>
              <div class="mt-4">
                <HtmlFormat :value="userAnalytics" />
              </div>
            </div>
            <div class="my-1">
              API Requests
            </div>
            <div class="grid grid-cols-1 lg:grid-cols-2 w-full gap-2">
              <div>
                <div class="bg-white rounded shadow p-4" style="height:300px">
                  <canvas ref="refUserStatusCodes"></canvas>
                </div>
              </div>
              <div>
                <div class="bg-white rounded shadow p-4 mb-8" style="height:300px">
                  <canvas ref="refUserDurationRanges"></canvas>
                </div>
              </div>
            </div>
            <div class="grid grid-cols-1 md:grid-cols-2 w-full gap-2">
              <div v-if="mapCounts(analytics,'apis')">
                Top APIs
                <div class="mt-1 bg-white rounded shadow p-4" style="height:300px">
                  <canvas ref="refUserTopApis"></canvas>
                </div>
              </div>
              <div v-if="mapCounts(analytics,'apiKeys')">
                Top API Keys
                <div class="mt-1 bg-white rounded shadow p-4" style="height:300px">
                  <canvas ref="refUserTopApiKeys"></canvas>
                </div>
              </div>
              <div v-if="mapCounts(analytics,'ips')">
                Top IP Addresses
                <div class="mt-1 bg-white rounded shadow p-4" style="height:300px">
                  <canvas ref="refUserTopIps"></canvas>
                </div>
              </div>
            </div>
          </div>
          <div v-else class="text-center">
            User did not make any API requests during this period 
          </div>
          
        </div>
      </div>
    `,
    props: {
      info: Object,
      id: String,
    },
    setup(props) {
        const routes = inject('routes')
        const client = useClient()
        const api = ref(new ApiResult())
        const analytics = computed(() =>
            api.value.response?.result?.users ? Object.values(api.value.response.result.users ?? {} )?.[0] : null)
        const months = ref(props.info.months ?? [])
        const years = computed(() =>
            Array.from(new Set(months.value.map(x => leftPart(x,'-')))).toReversed())

        const refUserStatusCodes = ref(null)
        const refUserDurationRanges = ref(null)
        const refUserTopApis = ref(null)
        const refUserTopApiKeys = ref(null)
        const refUserTopIps = ref(null)

        let userStatusCodesChart = null
        let userRequestsChart = null
        let userDurationRangesChart = null

        let userTopApisChart = null
        let userTopApiKeysChart = null
        let userTopIpsChart = null

        const { formatBytes } = useFiles()
        const { humanifyMs, humanifyNumber, formatDate } = useFormatters()
        const numFmt = new Intl.NumberFormat('en-US', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        })
        function round(n) {
            return n.toString().indexOf(".") === -1 ? n : numFmt.format(n);
        }

        const userAnalytics = computed(() => {
            const user = analytics.value
            if (user) {
                let ret = []
                if (user.totalDuration) {
                    ret.push({ name: 'Duration',
                        Total: humanifyMs(round(user.totalDuration)),
                        Min: humanifyMs(round(user.minDuration)),
                        Max: humanifyMs(round(user.maxDuration)),
                    })
                }
                if (user.totalRequestLength) {
                    ret.push({ name: 'Request Body',
                        Total: formatBytes(user.totalRequestLength),
                        Min: formatBytes(user.minRequestLength),
                        Max: formatBytes(user.maxRequestLength),
                    })
                }
                return ret
            }
            return []
        })
        const userLinks = computed(() => {
            const user = analytics.value
            if (user) {
                let linkBase = `./logging?userId=${routes.userId}`
                if (routes.month) {
                    linkBase += `&month=${routes.month}`
                }
                const ret = []
                Object.entries(user.status).forEach(([status, count]) => {
                    ret.push({ label:status, href:`${linkBase}&status=${status}`, count })
                })
                return ret
            }
            return []
        })        
        async function update() {
            api.value = await client.api(new GetAnalyticsReports({
                filter: "user",
                value: props.id,
                month: routes.month ? `${routes.month}-01` : undefined,
            }))

            nextTick(() => {
                userStatusCodesChart = createStatusCodesChart({
                    requests: analytics.value?.status,
                    chart: userStatusCodesChart,
                    refEl: refUserStatusCodes,
                })

                userDurationRangesChart = createDurationRangesChart(
                    analytics.value?.durations, userDurationRangesChart, refUserDurationRanges)

                userTopApisChart = createRequestsChart({
                    requests: sortedDetailRequests(analytics.value?.apis),
                    chart: userTopApisChart,
                    refEl: refUserTopApis,
                    formatY: function(ctx, value, index, values) {
                        return ctx.labels[index]
                    },
                    //onClick: onClick(props.analytics, routes, 'api'),
                })
                userTopApiKeysChart = createRequestsChart({
                    requests: sortedDetailRequests(analytics.value?.apiKeys),
                    chart: userTopApiKeysChart,
                    refEl: refUserTopApiKeys,
                    formatY: function(ctx, value, index, values) {
                        return hiddenApiKey(ctx.labels[index])
                    },
                    //onClick: onClick(props.analytics, routes, 'apiKey'),
                })
                userTopIpsChart = createRequestsChart({
                    requests: sortedDetailRequests(analytics.value?.ips),
                    chart: userTopIpsChart,
                    refEl: refUserTopIps,
                    //onClick: onClick(props.analytics, routes, 'ip'),
                })
            })
        }
        
        onMounted(update)
        
        onUnmounted(() => {
            [
                userStatusCodesChart,
                userRequestsChart,
                userDurationRangesChart,
            ].forEach(chart => chart?.destroy())
        })
        
        watch(() => [routes.month], update)
        
        return {
            routes, 
            months,
            years,
            analytics,
            userAnalytics,
            userLinks,
            refUserStatusCodes,
            refUserDurationRanges,
            refUserTopApis,
            refUserTopApiKeys,
            refUserTopIps,
            humanifyNumber,
            mapCounts,
        }
    }
}


export const EditUser = {
    components: {
        Analytics,
    },
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

            <div v-if="store.plugins?.apiKey" class="mt-8 pt-4 border-t border-gray-900/10 px-4 sm:px-6">
              <ManageUserApiKeys :user="request" />
            </div>
            
            <Analytics v-if="server.plugins.requestLogs?.analytics && !server.plugins.requestLogs.analytics.disableUserAnalytics" 
                       :info="server.plugins.requestLogs?.analytics" :id="id" />
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
    emits: ['done','save'],
    setup(props, { emit }) {
        const store = inject('store')
        const routes = inject('routes')
        const server = inject('server')
        const client = useClient()
        const { toFormValues } = useMetadata()
        const { formLayout, inputIds, exceptFields, init } = createForm(server)

        const request = ref(init(new AdminUpdateUser({ id:props.id }), dtoProps))
        const model = ref({ newRole:'', newPermission:'', password:'', confirmDelete: false })
        const successMessage = ref('')
        const lockedDate = ref(null)
        const origRoles = ref([])
        const roles = ref([])

        const orig = ref(new AdminUserResponse({ claims:[] }))
        const newClaim = ref(new Property({name:'',value:''}))
        const addClaims = ref([])
        const removeClaims = ref([])
        const claims = computed(() => orig.value.claims.filter(x =>
            !removeClaims.value.find(c => c.name === x.name && c.value === x.value)
        ).concat(addClaims.value))
        
        
        const loading = computed(() => client.loading.value)
        const api = ref(new ApiResult())
        const formFields = ref()

        function done() { emit('done') }
        function save() { emit('save') }

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
            await send(new AdminDeleteUser({ id:props.id }), save)
        }
        async function lockUser() {
            await send(new AdminUpdateUser({ id:props.id, lockUser:true }), save)
        }
        async function unlockUser() {
            await send(new AdminUpdateUser({ id:props.id, unlockUser:true }), save)
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

            if (removeClaims.value.length) {
                requestDto.removeClaims = removeClaims.value
            }
            if (addClaims.value.length) {
                requestDto.addClaims = addClaims.value
            }
            
            await send(requestDto, save)
        }

        function bind(response) {
            orig.value = response
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
            server,
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
            orig,
            newClaim,
            claims,
            addClaim,
            deleteClaim,
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
  
  <EditUser v-if="routes.edit" :id="routes.edit" @done="close" @save="formSearch" />
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
        /** @type {Ref<ApiResult<AdminUsersResponse>>} */
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
