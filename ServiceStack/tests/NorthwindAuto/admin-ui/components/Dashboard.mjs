import { computed, inject, onMounted, onUnmounted, ref, watch } from "vue"
import { ApiResult, humanify } from "@servicestack/client"
import { useClient, useUtils } from "@servicestack/vue"
import { getIcon } from "core"
import { urlWithState } from "app"
import { AdminDashboard } from "dtos"

export const Dashboard = {
    template:/*html*/`
<div class="rounded-lg bg-white overflow-hidden shadow mb-3">
    <h2 class="sr-only" id="profile-overview-title">Profile Overview</h2>
    <div class="bg-white p-6">
        <div class="sm:flex sm:justify-between">
            <div class="sm:flex sm:space-x-5">
                <div class="flex-shrink-0">
                    <img class="mx-auto max-h-24 max-w-24 rounded-full"
                         :src="store.authProfileUrl" alt="">
                </div>
                <div class="mt-4 sm:mt-0 sm:pt-1 sm:text-left">
                    <p class="text-sm font-medium text-gray-600">Welcome back,</p>
                    <p class="text-xl font-bold text-gray-900 sm:text-2xl mb-2">{{ store.displayName }}</p>
                    <div v-if="store.authRoles.length" class="mb-2 flex flex-wrap">
                        <span v-for="role in store.authRoles" title="Role"
                              class="inline-flex items-center mr-2 px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
                          {{ role }}
                        </span>
                    </div>
                    <div v-if="store.authPermissions.length" class="mb-2 flex flex-wrap">
                        <span v-for="perm in store.authPermissions" title="Permission"
                              class="inline-flex items-center mr-2 px-2.5 py-0.5 rounded-full text-xs font-medium bg-yellow-100 text-yellow-800">
                          {{ perm }}
                        </span>
                    </div>
                </div>
            </div>
            <div class="">
                <button type="button" @click="store.logout()"
                        class="cursor-pointer inline-flex items-center px-6 py-3 border border-gray-300 shadow-sm md:text-lg font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500">
                    Sign Out
                </button>
            </div>
        </div>
    </div>
</div>

<a :href="urlApiExplorer" title="go to /ui" class="block pt-3">
    <h3 class="text-lg leading-6 font-medium text-gray-900">
        API Stats
    </h3>
    <dl class="mt-5 grid grid-cols-1 rounded-lg bg-white hover:bg-gray-50 overflow-hidden shadow divide-y divide-gray-200 md:grid-cols-4 md:divide-y-0 md:divide-x">
        <div class="px-4 py-5 sm:p-6">
            <dt class="text-base font-normal text-gray-900">
                Total APIs
            </dt>
            <dd class="mt-1 flex justify-between items-baseline md:block lg:flex">
                <div class="flex items-baseline text-2xl font-semibold text-indigo-600">
                    {{ server.api.operations.length }}
                </div>
            </dd>
        </div>

        <div class="px-4 py-5 sm:p-6">
            <dt class="text-base font-normal text-gray-900">
                Protected APIs
            </dt>
            <dd class="mt-1 flex justify-between items-baseline md:block lg:flex">
                <div class="flex items-baseline text-2xl font-semibold text-indigo-600">
                    {{ server.api.operations.filter(op => op.requiresAuth).length }}
                </div>
            </dd>
        </div>

        <div class="px-4 py-5 sm:p-6">
            <dt class="text-base font-normal text-gray-900">
                Built-in APIs
            </dt>
            <dd class="mt-1 flex justify-between items-baseline md:block lg:flex">
                <div class="flex items-baseline text-2xl font-semibold text-indigo-600">
                    {{ server.api.operations.filter(op => (op.request.namespace || "").startsWith('ServiceStack')).length }}
                </div>
            </dd>
        </div>

        <div class="px-4 py-5 sm:p-6">
            <dt class="text-base font-normal text-gray-900">
                Unique DTOs
            </dt>
            <dd class="mt-1 flex justify-between items-baseline md:block lg:flex">
                <div class="flex items-baseline text-2xl font-semibold text-indigo-600">
                    {{ Object.keys(store.allTypes).length }}
                </div>
            </dd>
        </div>
    </dl>
</a>

<div v-if="serverStats.redis" class="mt-5">
    <h3 class="text-lg leading-6 font-medium text-gray-900">
        Redis Stats
    </h3>
    <dl class="grid grid-cols-1 bg-white overflow-hidden xl:grid-cols-5 md:grid-cols-4">
        <div v-for="(stat,name) in serverStats.redis" class="py-3 sm:p-3">
            <dt class="text-base font-normal text-gray-900">
                {{ statLabel(name) }}
            </dt>
            <dd class="mt-1 flex justify-between items-baseline md:block lg:flex">
                <div class="flex items-baseline text-2xl font-semibold text-indigo-600">
                    {{ stat }}
                </div>
            </dd>
        </div>
    </dl>
</div>

<div v-if="serverStats.serverEvents" class="mt-5">
    <h3 class="text-lg leading-6 font-medium text-gray-900">
        Server Events Stats
    </h3>
    <dl class="grid grid-cols-1 bg-white overflow-hidden xl:grid-cols-5 md:grid-cols-4">
        <div v-for="(stat,name) in serverStats.serverEvents" class="py-3 sm:p-3">
            <dt class="text-base font-normal text-gray-900">
                {{ statLabel(name) }}
            </dt>
            <dd class="mt-1 flex justify-between items-baseline md:block lg:flex">
                <div class="flex items-baseline text-2xl font-semibold text-indigo-600">
                    {{ stat }}
                </div>
            </dd>
        </div>
    </dl>
</div>

<div v-if="serverStats.mqWorkers" class="mt-5">
    <div class="flex items-center">
        <h3 class="text-lg leading-6 font-medium text-gray-900">
            MQ Worker Stats
        </h3>
        <div class="ml-1 relative flex flex-col items-center group">
            <svg class="w-5 h-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path fill="currentColor"
                      d="M12 22C6.477 22 2 17.523 2 12S6.477 2 12 2s10 4.477 10 10s-4.477 10-10 10zm0-2a8 8 0 1 0 0-16a8 8 0 0 0 0 16zM11 7h2v2h-2V7zm0 4h2v6h-2v-6z"/>
            </svg>
            <div class="absolute bottom-0 flex flex-col items-center hidden mb-6 group-hover:flex">
            <span
                    class="relative z-10 p-2 text-xs leading-none text-white whitespace-no-wrap bg-black shadow-lg font-mono whitespace-pre">{{ serverStats.mqDescription }}</span>
                <div class="w-3 h-3 -mt-2 rotate-45 bg-black"></div>
            </div>
        </div>
    </div>

    <dl class="grid grid-cols-1 bg-white overflow-hidden xl:grid-cols-5 md:grid-cols-4">
        <div v-for="(stat,name) in serverStats.mqWorkers" class="py-3 sm:p-3">
            <dt class="text-base font-normal text-gray-900">
                {{ statLabel(name) }}
            </dt>
            <dd class="mt-1 flex justify-between items-baseline md:block lg:flex">
                <div class="flex items-baseline text-2xl font-semibold text-indigo-600">
                    {{ stat }}
                </div>
            </dd>
        </div>
    </dl>
</div>

<a href="https://docs.servicestack.net/admin-ui-features"
   class="block mt-5 rounded-lg bg-white hover:bg-gray-50 shadow">
    <div class="px-4 py-5 sm:p-6">
        <h3 class="text-lg leading-6 font-medium text-gray-900">Admin UI Features</h3>
        <div class="mt-2 max-w-2xl text-sm text-gray-500">
            <p>Discover and learn how to enable new Admin UI Features</p>
        </div>
        <div class="mt-5">
            <span v-for="(label,id) in adminFeatures" :key="id"
                  :class="['mr-2 mb-1 inline-flex items-center px-2.5 py-0.5 rounded-md text-sm font-medium', 
                  isRegistered(id) ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800']">
              <svg :class="['-ml-0.5 mr-1.5 h-2 w-2', isRegistered(id) ? 'text-green-400' : 'text-gray-400']" fill="currentColor" viewBox="0 0 8 8"><circle cx="4" cy="4" r="3"/></svg>
              {{ label }}
            </span>
        </div>
    </div>
</a>
    `,
    setup() {
        const store = inject('store')
        const routes = inject('routes')
        const server = inject('server')
        const urlApiExplorer = computed(() => urlWithState('../ui'))
        const loading = ref(false)
        
        const adminFeatures = computed(() => ({
            analytics:'Analytics',
            users:'Users',
            roles:'Roles',
            apikeys:'API Keys',
            logging:'Logging',
            profiling:'Profiling',
            commands:'Commands',
            backgroundjobs:'Background Jobs',
            validation:'Validation', 
            database:'Database', 
            redis:'Redis',
            aichat:'AI Chat',
        }))
        
        function isRegistered(id) {
            return server.ui.adminLinks.some(link => link.id === id)
        }

        const client = useClient()
        const api = ref(new ApiResult())
        async function updated(){
            api.value = await client.api(new AdminDashboard(), { jsconfig: 'eccn' })
        }
        const serverStats = computed(() => api.value.response?.serverStats || {})
        function statLabel(name) {
            return humanify(name.replace('Total',''))
        }
        
        let sub = null
        onMounted(async () => {
            sub = app.subscribe('route:nav', args => updated())
            await updated()
        })
        onUnmounted(() => app.unsubscribe(sub))
        
        return {
            store,
            routes,
            server,
            loading,
            api,
            urlApiExplorer,
            adminFeatures,
            isRegistered,
            updated,
            serverStats,
            statLabel,
        }
    }
}