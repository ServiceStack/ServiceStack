import { ref, computed, inject, provide, Teleport } from "vue"
import { JsonServiceClient } from "@servicestack/client"
import { useAuth, SignIn, ModalDialog } from "@servicestack/vue"

const template = `
<div class="relative inline-block text-left">
    <!-- Button -->
    <button type="button" @click="onClick" :title="user ? (user.displayName || user.userName || 'Account') : 'Sign In'"
            class="flex items-center justify-center rounded-full p-1.5 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-800 focus:outline-none transition-colors">
        <!-- Signed in avatar -->
        <template v-if="user">
            <img v-if="user.profileUrl" :src="user.profileUrl" alt="Avatar" class="w-6 h-6 rounded-full object-cover" />
            <div v-else class="w-6 h-6 rounded-full bg-indigo-600 text-white flex items-center justify-center text-[10px] font-semibold uppercase">
                {{ initials }}
            </div>
        </template>
        <!-- Anonymous avatar -->
        <svg v-else class="w-5 h-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
            <path d="M0 0h24v24H0z" fill="none" />
            <g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5">
                <path d="M7 15a3 3 0 1 0 0 6a3 3 0 0 0 0-6m10 0a3 3 0 1 0 0 6a3 3 0 0 0 0-6m-3 2h-4m12-4c-2.457-1.228-6.027-2-10-2s-7.543.772-10 2" />
                <path d="m19 11.5l-1.058-6.788c-.215-1.384-1.719-2.134-2.933-1.463l-.615.34a4.94 4.94 0 0 1-4.788 0l-.615-.34c-1.214-.671-2.718.08-2.933 1.463L5 11.5" />
            </g>
        </svg>
    </button>

    <!-- Signed-in User Menu Dropdown -->
    <div v-if="openMenu" class="fixed inset-0 z-30" @click="openMenu = false"></div>
    <div v-if="openMenu" class="absolute right-0 mt-2 w-48 z-40 rounded-lg shadow-lg py-1 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 text-xs">
        <div class="px-4 py-2 border-b border-gray-100 dark:border-gray-700">
            <p class="font-semibold text-gray-900 dark:text-gray-100 truncate">{{ user?.displayName || user?.userName }}</p>
            <p v-if="user?.email" class="text-gray-500 dark:text-gray-400 truncate">{{ user.email }}</p>
        </div>
        <a :href="logoutUrl" class="block w-full text-left px-4 py-2 text-rose-600 dark:text-rose-400 hover:bg-gray-100 dark:hover:bg-gray-700 font-medium transition-colors">
            Sign Out
        </a>
    </div>

    <!-- Sign In Modal -->
    <Teleport to="body">
        <ModalDialog v-if="openSignIn" @done="openSignIn = false" size-class="sm:max-w-md sm:w-full">
            <div class="p-4 bg-white dark:bg-gray-900 rounded-lg">
                <SignIn @login="openSignIn = false" />
            </div>
        </ModalDialog>
    </Teleport>
</div>
`

export const AvatarButton = {
    name: 'AvatarButton',
    components: { SignIn, ModalDialog, Teleport },
    template,
    setup() {
        const client = inject('client', null) ?? new JsonServiceClient()
        provide('client', client)

        const { user, isAuthenticated } = useAuth()
        const openMenu = ref(false)
        const openSignIn = ref(false)

        const initials = computed(() => {
            const u = user.value
            if (!u) return ''
            const name = u.displayName || u.userName || ''
            return name ? name.substring(0, 2) : '?'
        })

        const logoutUrl = computed(() => {
            const current = typeof location !== 'undefined' ? (location.pathname + location.search) : '/'
            return '/auth/logout?ReturnUrl=' + encodeURIComponent(current)
        })

        function onClick() {
            if (isAuthenticated.value || user.value) {
                openMenu.value = !openMenu.value
            } else {
                openSignIn.value = true
            }
        }

        return {
            user,
            isAuthenticated,
            openMenu,
            openSignIn,
            initials,
            logoutUrl,
            onClick,
        }
    },
}

export default AvatarButton
