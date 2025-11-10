import { inject } from "vue"

/**
 * Example component demonstrating Locode routing
 * This shows how to navigate between the Welcome page and different operations
 */
export const NavigationExample = {
    template:/*html*/`

    <component v-if="store.pageComponentFor(store.opDataModel)" :key="store.opDataModel + 'Grid'" :is="store.pageComponentFor(store.opDataModel)" :type="store.opDataModel"></component>
    <auto-query-grid v-else :key="store.opDataModel" ref="grid" :type="store.opDataModel">
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
    </auto-query-grid>


    <div class="p-4 bg-gray-50 rounded-lg">
        <h3 class="text-lg font-semibold mb-4">Navigation Example</h3>
        
        <!-- Current Route Info -->
        <div class="mb-4 p-3 bg-white rounded border">
            <p class="text-sm font-medium text-gray-700">Current Route:</p>
            <p class="text-sm text-gray-600">
                Page: <span class="font-mono">{{ routes.op || '(Welcome)' }}</span>
            </p>
            <p v-if="routes.edit" class="text-sm text-gray-600">
                Edit ID: <span class="font-mono">{{ routes.edit }}</span>
            </p>
            <p class="text-sm text-gray-600">
                Is Welcome: <span class="font-mono">{{ routes.isWelcome }}</span>
            </p>
        </div>
        
        <!-- Navigation Buttons -->
        <div class="space-y-2">
            <h4 class="text-sm font-medium text-gray-700">Navigate using methods:</h4>
            
            <button @click="routes.toWelcome()" 
                    class="block w-full px-4 py-2 text-left text-sm bg-white border rounded hover:bg-gray-50">
                Go to Welcome Page
            </button>
            
            <button @click="routes.toOp('QueryBookings')" 
                    class="block w-full px-4 py-2 text-left text-sm bg-white border rounded hover:bg-gray-50">
                Go to QueryBookings
            </button>
            
            <button @click="routes.to({ $page: 'QueryBookings', edit: '123' })" 
                    class="block w-full px-4 py-2 text-left text-sm bg-white border rounded hover:bg-gray-50">
                Go to QueryBookings with Edit Dialog (ID: 123)
            </button>
        </div>
        
        <!-- Navigation Links -->
        <div class="mt-4 space-y-2">
            <h4 class="text-sm font-medium text-gray-700">Navigate using v-href:</h4>
            
            <a v-href="{ $page: '' }" 
               class="block px-4 py-2 text-sm text-blue-600 hover:text-blue-800 underline">
                Link to Welcome Page
            </a>
            
            <a v-href="{ $page: 'QueryBookings' }" 
               class="block px-4 py-2 text-sm text-blue-600 hover:text-blue-800 underline">
                Link to QueryBookings
            </a>
            
            <a v-href="{ $page: 'QueryBookings', new: 'true' }" 
               class="block px-4 py-2 text-sm text-blue-600 hover:text-blue-800 underline">
                Link to QueryBookings with New Dialog
            </a>
        </div>
        
        <!-- Route State -->
        <div class="mt-4 p-3 bg-white rounded border">
            <h4 class="text-sm font-medium text-gray-700 mb-2">Full Route State:</h4>
            <pre class="text-xs text-gray-600 overflow-auto">{{ JSON.stringify(routes.state, null, 2) }}</pre>
        </div>
    </div>
    `,
    setup() {
        const routes = inject('routes')
        
        return { routes }
    }
}

