import { inject, onMounted, ref, watch } from "vue"
import { useUtils } from "@servicestack/vue"
import { getIcon } from "core"
const SidebarNav = {
    template:/*html*/`
    <nav class="flex-1 space-y-1 bg-white pb-4 md:pb-scroll" aria-label="Sidebar">
        <div v-for="nav in store.filteredSideNav" class="space-y-1">
            <button v-if="nav.tag" type="button" @click.prevent="store.toggle(nav.tag)"
                    class="bg-white text-gray-600 hover:bg-gray-50 hover:text-gray-900 group w-full flex items-center pr-2 py-2 text-left text-sm font-medium">
                <svg :class="[nav.expanded ? 'text-gray-400 rotate-90' : 'text-gray-300','mr-2 flex-shrink-0 h-5 w-5 transform group-hover:text-gray-400 transition-colors ease-in-out duration-150']" viewBox="0 0 20 20" aria-hidden="true">
                    <path d="M6 6L14 10L6 14V6Z" fill="currentColor" />
                </svg>
                {{nav.tag}}
            </button>
              <div v-if="nav.expanded" class="space-y-1">
                <a v-for="op in nav.operations" v-href="{ op:op.request.name, provider:'', preview:'' }"
                   :class="[op.request.name == routes.op ? 'bg-indigo-50 border-indigo-600 text-indigo-600' : 
                    'border-transparent text-gray-600 hover:text-gray-900 hover:bg-gray-50', 'border-l-4 group w-full flex justify-between items-center pl-10 pr-2 py-2 text-sm font-medium']">
                  <span class="nav-item flex-grow">{{op.request.name}}</span>
                  <span v-if="op.requiresAuth" class="svg-lock w-5 h-5"></span>
                  <span v-else-if="op.requiresApiKey" class="svg-key w-5 h-5"></span>
                </a>
              </div>
        </div>
    </nav>
    `,
    //props:['$on'],
    setup(props) {
        const store = inject('store')
        const routes = inject('routes')
        
        return {
            store,
            routes,
            getIcon,
        }
    }
}
const SidebarTop = {
    template:/*html*/`
    <div>
      <Brand class="pl-4 pb-3" :icon="server.ui.brandIcon" :name="server.app.serviceName" />
      <div class="bg-white py-1.5 px-3.5">
        <svg class="absolute ml-2.5 mt-2.5 h-4 w-4 text-gray-500" fill="currentColor" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
          <path d="M16.32 14.9l5.39 5.4a1 1 0 0 1-1.42 1.4l-5.38-5.38a8 8 0 1 1 1.41-1.41zM10 16a6 6 0 1 0 0-12 6 6 0 0 0 0 12z"/>
        </svg>
        <input type="search" v-model="store.filter" placeholder="Filter..."
               class="border rounded-full overflow-hidden flex w-full px-4 py-1 pl-8 border-gray-200">
      </div>
    </div>
    `,
    setup(props) {
        const server = inject('server')
        const store = inject('store')
        return { server, store }
    }
}
export const Sidebar = {
    components: { SidebarNav, SidebarTop },
    template:/*html*/`
    <div id="sidebar" class="fixed inset-0 flex z-40 md:hidden" role="dialog" aria-modal="true">
        <!---: Off-canvas menu overlay, show/hide based on off-canvas menu state. -->
        <div :class="['fixed inset-0 bg-gray-600 bg-opacity-75', transition1]" aria-hidden="true"></div>
    
        <!---: Off-canvas menu, show/hide based on off-canvas menu state. -->
        <div :class="['relative flex-1 flex flex-col max-w-sidebar w-full bg-white', transition2]">
            <!---: Close button, show/hide based on off-canvas menu state. -->
            <div :class="['absolute top-0 right-0 -mr-12 pt-2', transition3]">
                <button type="button" @click="hide" 
                        class="ml-1 flex items-center justify-center h-10 w-10 rounded-full focus:outline-none focus:ring-2 focus:ring-inset focus:ring-white">
                    <span class="sr-only">Close sidebar</span>
                    <!---: Heroicon name: outline/x -->
                    <svg class="h-6 w-6 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                    </svg>
                </button>
            </div>
            <div class="flex-1 flex flex-col pt-5 pb-4">
                <SidebarTop class="z-10" />
                <!---: sm: use top-nav  -->
                <SidebarNav  class="flex-1 flex flex-col fixed top-top-nav w-sidebar h-top-nav overflow-y-auto" />
            </div>
        </div>
        <div class="flex-shrink-0 w-14">
            <!---: Force sidebar to shrink to fit close icon -->
        </div>
    </div>
    
    <div class="hidden md:flex w-sidebar md:flex-col md:fixed md:inset-y-0">
        <div class="flex-1 flex flex-col min-h-0 border-r border-gray-200 bg-white">
            <div class="flex-1 flex flex-col pt-5 pb-4">
                <SidebarTop class="z-10" />
                <SidebarNav class="flex-1 flex flex-col fixed top-sub-nav w-sidebar -ml-[1px] h-sub-nav overflow-y-auto" />
            </div>
        </div>
    </div>    
    `,
    emits: ['hide'],
    setup(props, { emit }) {
        const store = inject('store')
        
        const transition1 = ref('')
        const rule1 = {
            entering: { cls:'transition-opacity ease-linear duration-300', from:'opacity-0',   to:'opacity-100'},
            leaving:  { cls:'transition-opacity ease-linear duration-300', from:'opacity-100', to:'opacity-0'}
        }
        const transition2 = ref('')
        const rule2 = {
            entering: { cls:'transition ease-in-out duration-300 transform', from:'-translate-x-full', to:'translate-x-0'},
            leaving:  { cls:'transition ease-in-out duration-300 transform', from:'translate-x-0',     to:'-translate-x-full'}
        }
        const transition3 = ref('')
        const rule3 = {
            entering: { cls:'ease-in-out duration-300', from:'opacity-0',   to:'opacity-100'},
            leaving:  { cls:'ease-in-out duration-300', from:'opacity-100', to:'opacity-0'}
        }
        
        const { transition } = useUtils()
        function toggle(show) {
            transition(rule1, transition1, show)
            transition(rule2, transition2, show)
            transition(rule3, transition3, show)
        }
        
        function hide() {
            toggle(false)
            setTimeout(() => emit('hide'), 200)
        }
        
        onMounted(() => {
            toggle(true)
        })
        return { store, transition1, transition2, transition3, hide }
    }
}
