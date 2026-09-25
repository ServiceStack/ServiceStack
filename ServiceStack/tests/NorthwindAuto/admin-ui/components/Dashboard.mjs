import { computed, inject, onMounted, onUnmounted, ref, watch } from "vue"
import { ApiResult, humanify } from "@servicestack/client"
import { useClient, useUtils } from "@servicestack/vue"
import { getIcon } from "core"
import { urlWithState } from "app"
import { AdminDashboard } from "dtos"

export const Dashboard = {
    template:/*html*/`
<div class="max-w-7xl space-y-8">
    <section class="flex flex-wrap items-center justify-between gap-4 rounded-lg bg-white px-5 py-4 shadow-sm ring-1 ring-gray-900/5">
        <h2 class="sr-only" id="profile-overview-title">Profile Overview</h2>
        <div class="flex items-center gap-4 min-w-0">
            <img class="h-12 w-12 shrink-0 rounded-full bg-gray-100" :src="store.authProfileUrl" alt="">
            <div class="min-w-0">
                <p class="text-sm text-gray-500">Welcome back,</p>
                <p class="truncate text-lg font-semibold text-gray-900">{{ store.displayName }}</p>
            </div>
            <div v-if="store.authRoles.length || store.authPermissions.length" class="ml-2 hidden sm:flex flex-wrap items-center gap-1.5">
                <span v-for="role in store.authRoles" title="Role"
                      class="inline-flex items-center rounded-md bg-gray-50 px-2 py-0.5 text-xs font-medium text-gray-700 ring-1 ring-inset ring-gray-500/15">{{ role }}</span>
                <span v-for="perm in store.authPermissions" title="Permission"
                      class="inline-flex items-center rounded-md bg-amber-50 px-2 py-0.5 text-xs font-medium text-amber-800 ring-1 ring-inset ring-amber-600/20">{{ perm }}</span>
            </div>
        </div>
        <button type="button" @click="store.logout()"
                class="cursor-pointer inline-flex items-center rounded-md bg-white px-3 py-1.5 text-sm font-medium text-gray-700 shadow-sm ring-1 ring-inset ring-gray-300 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-indigo-500">
            Sign Out
        </button>
    </section>

    <section>
        <div class="mb-3 flex items-baseline justify-between">
            <h3 class="text-sm font-semibold text-gray-900">API Stats</h3>
            <a :href="urlApiExplorer" class="text-sm font-medium text-indigo-600 hover:text-indigo-500">API Explorer <span aria-hidden="true">&rarr;</span></a>
        </div>
        <dl class="grid grid-cols-2 md:grid-cols-4 gap-px overflow-hidden rounded-lg bg-gray-900/5 shadow-sm ring-1 ring-gray-900/5">
            <div v-for="stat in apiStats" class="bg-white px-5 py-4">
                <dt class="text-sm text-gray-500">{{ stat.label }}</dt>
                <dd class="mt-1 text-2xl font-semibold tracking-tight tabular-nums text-gray-900">{{ fmtNum(stat.value) }}</dd>
            </div>
        </dl>
    </section>

    <section v-for="group in statGroups" :key="group.id">
        <div class="mb-3 flex items-center gap-1.5">
            <h3 class="text-sm font-semibold text-gray-900">{{ group.title }}</h3>
            <div v-if="group.info" class="relative flex items-center group">
                <svg class="w-4 h-4 text-gray-400 hover:text-gray-600" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                    <path fill="currentColor" d="M12 22C6.477 22 2 17.523 2 12S6.477 2 12 2s10 4.477 10 10s-4.477 10-10 10zm0-2a8 8 0 1 0 0-16a8 8 0 0 0 0 16zM11 7h2v2h-2V7zm0 4h2v6h-2v-6z"/>
                </svg>
                <div class="absolute left-6 top-0 z-10 hidden group-hover:block">
                    <span class="block rounded-md bg-gray-900 p-2 text-xs leading-snug text-white shadow-lg font-mono whitespace-pre">{{ group.info }}</span>
                </div>
            </div>
        </div>
        <dl class="grid grid-cols-2 md:grid-cols-4 xl:grid-cols-5 gap-px overflow-hidden rounded-lg bg-gray-900/5 shadow-sm ring-1 ring-gray-900/5">
            <div v-for="(stat,name) in group.stats" class="bg-white px-5 py-4">
                <dt class="text-sm text-gray-500 truncate" :title="statLabel(name)">{{ statLabel(name) }}</dt>
                <dd :class="['mt-1 text-2xl font-semibold tracking-tight tabular-nums', Number(stat) ? 'text-gray-900' : 'text-gray-400']">{{ fmtNum(stat) }}</dd>
            </div>
            <div v-for="i in fillers(group.stats)" class="hidden xl:block bg-white"></div>
        </dl>
    </section>

    <section class="rounded-lg bg-white px-5 py-4 shadow-sm ring-1 ring-gray-900/5">
        <div class="flex flex-wrap items-baseline justify-between gap-2">
            <h3 class="text-sm font-semibold text-gray-900">Admin UI Features</h3>
            <a href="https://docs.servicestack.net/admin-ui-features" target="_blank"
               class="text-sm font-medium text-indigo-600 hover:text-indigo-500">Discover how to enable more features <span aria-hidden="true">&rarr;</span></a>
        </div>
        <div class="mt-3 flex flex-wrap gap-2">
            <template v-for="(label,id) in adminFeatures" :key="id">
                <a v-if="isRegistered(id)" v-href="{ admin:id, $clear:true }"
                   class="inline-flex items-center gap-x-1.5 rounded-md bg-green-50 px-2 py-1 text-xs font-medium text-green-800 ring-1 ring-inset ring-green-600/20 hover:bg-green-100">
                    <svg class="h-1.5 w-1.5 fill-green-500" viewBox="0 0 6 6" aria-hidden="true"><circle cx="3" cy="3" r="3"/></svg>
                    {{ label }}
                </a>
                <span v-else title="Not enabled"
                      class="inline-flex items-center gap-x-1.5 rounded-md bg-gray-50 px-2 py-1 text-xs font-medium text-gray-500 ring-1 ring-inset ring-gray-500/10">
                    <svg class="h-1.5 w-1.5 fill-gray-300" viewBox="0 0 6 6" aria-hidden="true"><circle cx="3" cy="3" r="3"/></svg>
                    {{ label }}
                </span>
            </template>
        </div>
    </section>
</div>
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
            chat:'AI Chat',
            pdf:'PDF',
        }))

        const apiStats = computed(() => [
            { label:'Total APIs',     value:server.api.operations.length },
            { label:'Protected APIs', value:server.api.operations.filter(op => op.requiresAuth).length },
            { label:'Built-in APIs',  value:server.api.operations.filter(op => (op.request.namespace || "").startsWith('ServiceStack')).length },
            { label:'Unique DTOs',    value:Object.keys(store.allTypes).length },
        ])
        
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
        const statGroups = computed(() => [
            { id:'redis',        title:'Redis Stats',         stats:serverStats.value.redis },
            { id:'serverEvents', title:'Server Events Stats', stats:serverStats.value.serverEvents },
            { id:'mqWorkers',    title:'MQ Worker Stats',     stats:serverStats.value.mqWorkers, info:serverStats.value.mqDescription },
        ].filter(x => x.stats))
        const fmtNum = n => typeof n == 'number' ? n.toLocaleString() : n
        /** pad last row of 5-col grid so hairline dividers stay continuous */
        const fillers = stats => { const n = Object.keys(stats || {}).length % 5; return n ? 5 - n : 0 }
        
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
            apiStats,
            statGroups,
            fmtNum,
            fillers,
        }
    }
}