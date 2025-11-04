import { computed, inject, ref, onMounted, onUnmounted } from "vue"

export default {
    template:`
        <div v-if="$ai.auth?.profileUrl" class="relative" ref="avatarContainer">
            <img
                @click.stop="toggleMenu"
                :src="$ai.auth.profileUrl"
                :title="authTitle"
                class="size-8 rounded-full cursor-pointer hover:ring-2 hover:ring-gray-300"
            />
            <div
                v-if="showMenu"
                @click.stop
                class="absolute right-0 mt-2 w-48 bg-white dark:bg-gray-800 rounded-md shadow-lg py-1 z-50 border border-gray-200 dark:border-gray-700"
            >
                <div class="px-4 py-2 text-sm text-gray-700 dark:text-gray-300 border-b border-gray-200 dark:border-gray-700">
                    <div class="font-medium whitespace-nowrap overflow-hidden text-ellipsis">{{ $ai.auth.displayName || $ai.auth.userName }}</div>
                    <div class="text-xs text-gray-500 dark:text-gray-400 whitespace-nowrap overflow-hidden text-ellipsis">{{ $ai.auth.email }}</div>
                </div>
                <button type="button"
                    @click="handleLogout"
                    class="w-full text-left px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center whitespace-nowrap"
                >
                    <svg class="w-4 h-4 mr-2 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"></path>
                    </svg>
                    Sign Out
                </button>
            </div>
        </div>
    `,
    setup() {
        const ai = inject('ai')
        const showMenu = ref(false)
        const avatarContainer = ref(null)

        const authTitle = computed(() => {
            if (!ai.auth) return ''
            const { userId, userName, displayName, bearerToken, roles } = ai.auth
            const name = userName || displayName
            const prefix = roles && roles.includes('Admin') ? 'Admin' : 'Name'
            const sb = [
                name ? `${prefix}: ${name}` : '',
                `API Key: ${bearerToken}`,
                `${userId}`,
            ]
            return sb.filter(x => x).join('\n')
        })

        function toggleMenu() {
            showMenu.value = !showMenu.value
        }

        async function handleLogout() {
            showMenu.value = false
            await ai.signOut()
            // Reload the page to show sign-in screen
            window.location.reload()
        }

        // Close menu when clicking outside
        const handleClickOutside = (event) => {
            if (showMenu.value && avatarContainer.value && !avatarContainer.value.contains(event.target)) {
                showMenu.value = false
            }
        }

        onMounted(() => {
            document.addEventListener('click', handleClickOutside)
        })

        onUnmounted(() => {
            document.removeEventListener('click', handleClickOutside)
        })

        return {
            authTitle,
            handleLogout,
            showMenu,
            toggleMenu,
            avatarContainer,
        }
    }
}