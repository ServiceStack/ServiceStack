import { inject, ref, onMounted, onUnmounted } from "vue"
import Sidebar from "./Sidebar.mjs"

export default {
    components: {
        Sidebar,
    },
    setup() {
        const ai = inject('ai')
        const isMobile = ref(false)

        const checkMobile = () => {
            const wasMobile = isMobile.value
            isMobile.value = window.innerWidth < 1024 // lg breakpoint

            // Only auto-adjust sidebar state when transitioning between mobile/desktop
            if (wasMobile !== isMobile.value) {
                if (isMobile.value) {
                    ai.isSidebarOpen = false
                } else {
                    ai.isSidebarOpen = true
                }
            }
        }

        const toggleSidebar = () => {
            ai.isSidebarOpen = !ai.isSidebarOpen
        }

        const closeSidebar = () => {
            if (isMobile.value) {
                ai.isSidebarOpen = false
            }
        }

        onMounted(() => {
            checkMobile()
            window.addEventListener('resize', checkMobile)
        })

        onUnmounted(() => {
            window.removeEventListener('resize', checkMobile)
        })

        return { ai, isMobile, toggleSidebar, closeSidebar }
    },
    template: `
        <div class="flex h-screen bg-white dark:bg-gray-900">
            <!-- Mobile Overlay -->
            <div
                v-if="isMobile && ai.isSidebarOpen && !(ai.requiresAuth && !ai.auth)"
                @click="closeSidebar"
                class="fixed inset-0 bg-black/50 z-40 lg:hidden"
            ></div>

            <!-- Sidebar (hidden when auth required and not authenticated) -->
            <div
                v-if="!(ai.requiresAuth && !ai.auth) && ai.isSidebarOpen"
                :class="[
                    'transition-transform duration-300 ease-in-out z-50',
                    'w-72 xl:w-80 flex-shrink-0',
                    'lg:relative',
                    'fixed inset-y-0 left-0'
                ]"
            >
                <Sidebar @thread-selected="closeSidebar" @toggle-sidebar="toggleSidebar" />
            </div>

            <!-- Main Area -->
            <div class="flex-1 flex flex-col">
                <!-- Collapsed Sidebar Toggle Button -->
                <div
                    v-if="!(ai.requiresAuth && !ai.auth) && !ai.isSidebarOpen"
                    class="fixed top-4 left-0"
                >
                    <button type="button"
                        @click="toggleSidebar"
                        class="group p-1 text-gray-500 dark:text-gray-400 hover:text-blue-600 dark:hover:text-blue-400 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
                        title="Open sidebar"
                    >
                        <div class="relative w-5 h-5">
                            <!-- Default sidebar icon -->
                            <svg class="absolute inset-0 group-hover:hidden" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
                                <line x1="9" y1="3" x2="9" y2="21"></line>
                            </svg>
                            <!-- Hover state: |→ icon -->
                            <svg class="absolute inset-0 hidden group-hover:block" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="m17.172 11l-4.657-4.657l1.414-1.414L21 12l-7.071 7.071l-1.414-1.414L17.172 13H8v-2zM4 19V5h2v14z"/></svg>
                        </div>
                    </button>
                </div>

                <RouterView />
            </div>
        </div>
    `,
}
