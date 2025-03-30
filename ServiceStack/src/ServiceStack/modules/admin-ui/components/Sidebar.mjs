import { inject, onMounted, ref, watch } from "vue"
import { useUtils } from "@servicestack/vue"
const SidebarNav = {
    template:/*html*/`
      <div>
          <Brand class="flex items-center flex-shrink-0 px-4" :icon="server.ui.brandIcon" :name="server.app.serviceName" />
          <nav class="mt-5 flex-1 px-2 bg-white space-y-1" aria-label="Sidebar">
            <a v-for="({id,label,icon}) in store.adminLinks" v-href="{ admin:id, $clear:true }"
               :class="[(routes.admin ?? '') === id ? 'bg-gray-100 text-gray-900' : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900', 
                            'group flex items-center px-2 py-2 text-base font-medium rounded-md']">
              <Icon :image="icon" :class="[(routes.admin ?? '') === id ? 'text-gray-500' : 'text-gray-400 group-hover:text-gray-500', 'mr-3 h-6 w-6']" />
              {{ label }}
            </a>
          </nav>
      </div>
    `,
    setup(props) {
        const store = inject('store')
        const routes = inject('routes')
        const server = inject('server')
        
        return {
            store,
            routes,
            server,
        }
    }
}
const SidebarAuth = {
    template:/*html*/`
      <a v-href="{ $page:'' }" class="flex-shrink-0 w-full group block">
      <div class="flex items-center">
        <img v-if="store.authProfileUrl" class="h-8 w-8 rounded-full text-gray-700" :src="store.authProfileUrl" :onerror="'this.src=' + JSON.stringify(store.userIconUri)" alt="">
        <div class="ml-3">
          <p class="text-base font-medium text-gray-700 group-hover:text-gray-900">
            {{ store.displayName }}
          </p>
        </div>
      </div>
      </a>
    `,
    setup(props) {
        const store = inject('store')
        return { store }
    }
}
export const Sidebar = {
    components: { SidebarNav, SidebarAuth },
    template:/*html*/`
    <div>
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
              <SidebarNav class="flex-1 h-0 pt-5 pb-4 overflow-y-auto" />
              <!---: sm: use top-nav  -->
              <SidebarAuth class="flex-shrink-0 flex border-t border-gray-200 p-4" />
            </div>
            <div class="flex-shrink-0 w-14">
                <!---: Force sidebar to shrink to fit close icon -->
            </div>
        </div>
        
        <div class="hidden md:flex md:w-64 md:flex-col md:fixed md:inset-y-0 z-10 bg-white">
            <div class="flex-1 flex flex-col min-h-0 border-r border-gray-200 bg-white">
              <div class="flex-1 flex flex-col pt-5 pb-4 overflow-y-auto">
                <SidebarNav class="overflow-y-auto flex-1 flex flex-col overflow-y-auto" />
              </div>
              <SidebarAuth class="flex-shrink-0 flex border-t border-gray-200 p-4"></SidebarAuth>
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
