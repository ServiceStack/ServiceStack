import {inject, ref, computed} from "vue"
import { useMetadata, useConfig } from "@servicestack/vue"
/**
 * Operation Page Component - displays AutoQueryGrid for a specific operation
 */
export const OperationPage = {
    template:/*html*/`
    <div>
        <alert v-if="store.op && store.op.requiresApiKey && !(store.apikey || ['apikey','authsecret'].includes(store.auth?.authProvider))" class="pt-4 px-4">
            This API Requires an <a @click="$router.push({ query: { ...$route.query, dialog: 'apikey' } })" class="underline cursor-pointer">API Key</a>
        </alert>
        <div v-else-if="store.op">
            <div>
                <div class="w-full md:w-sidebar border-b border-gray-200 bg-white flex">
                    <h1 class="flex max-w-screen-sm lg:max-w-screen-md py-2.5 px-4 text-2xl" aria-label="Tabs" :title="store.opName">
                        {{store.opDesc}}
                    </h1>
                </div>
                <div class="pt-2 sm:mx-2 lg:mx-4">
                    <alert v-if="store.op.requiresApiKey && !(store.apikey || ['apikey','authsecret'].includes(store.auth?.authProvider))" class="pt-4 px-4">
                        This API Requires an <a v-href="{ dialog:'apikey' }" target="_blank" class="underline">API Key</a>
                    </alert>
                    <div v-else>
                        <component v-if="store.pageComponentFor(store.opDataModel)" :key="store.opDataModel + 'Grid'" :is="store.pageComponentFor(store.opDataModel)" :type="store.opDataModel"></component>
                        <AutoQueryGrid v-else :key="store.opDataModel" ref="grid" :type="store.opDataModel">
                            <template #formfooter="{ form, type, apis, model, id }">
                                <audit-events v-if="form === 'edit' && canAccessCrudEvents" class="mt-4" :key="id" :type="type" :id="id"></audit-events>
                            </template>
                            <template v-if="app.component('New' + store.opDataModel)" #createform="{ type, configure, done, save }">
                                <component :is="app.component('New' + store.opDataModel)" :type="type" :configure="configure" @save="save" @done="done" />
                            </template>
                            <template v-if="app.component('Edit' + store.opDataModel)" #editform="{ model, type, deleteType, configure, done, save }">
                                <component :is="app.component('Edit' + store.opDataModel)" :model="model" :type="type" :deleteType="deleteType"
                                           :configure="configure" @save="save" @done="done" />
                            </template>
                        </AutoQueryGrid>
                    </div>
                </div>
            </div>
        </div>
    </div>
    `,
    setup() {
        /*
        <pre>{{ Object.values(Apis.createContext({ apis:'QueryBookings' }).apis)
            .filter(x => x?.request)
            .map(x => x.request.name) }}</pre>
            <AutoQueryGrid ref="grid" apis="QueryBookings" />
            <pre>{{ Apis.createContext({ type:'Booking' }) }}</pre>
            <AutoQueryGrid ref="grid" apis="QueryBookings" />
         */
        
        const app = inject('app')
        const store = inject('store')
        const server = inject('server')
        const grid = ref()
        const opName = computed(() => store.opName)
        const canAccessCrudEvents = computed(() =>
            server.plugins.autoQuery.crudEventsServices && store.hasRole(server.plugins.autoQuery.accessRole))
        const { Apis } = useMetadata()
        const { Sole } = useConfig()
        const ctx = computed(() => Apis.createContext({
            id: store.opDataModel,
            type: store.opDataModel,
        }))
        // console.log('ctx', ctx)
        
        return {app, store, grid, opName, canAccessCrudEvents, Apis, ctx}
    }
}
