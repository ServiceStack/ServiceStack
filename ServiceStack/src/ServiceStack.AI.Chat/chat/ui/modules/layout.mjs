import { computed, inject, ref, onMounted, onUnmounted } from "vue"
import { toJsonObject } from "../utils.mjs"

const Brand = {
    template: `
    <div class="select-none flex-shrink-0 p-2 border-b" :class="$styles.chromeBorder">
        <div class="flex items-center justify-between">
            <div class="flex items-center space-x-2">
                <button type="button"
                    @click="$ctx.to('/')"
                    class="text-lg font-semibold focus:outline-none transition-colors" :class="[$styles.heading, $styles.linkHover]"
                    title="Go back home">
                    {{ $state.title }}
                </button>
            </div>
        </div>
    </div>
    `,
}

const Welcome = {
    template: `
        <div class="mb-2 flex justify-center">
            <svg class="size-20" :class="[$styles.icon]" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 16 16"><path fill="currentColor" d="M8 2.19c3.13 0 5.68 2.25 5.68 5s-2.55 5-5.68 5a5.7 5.7 0 0 1-1.89-.29l-.75-.26l-.56.56a14 14 0 0 1-2 1.55a.13.13 0 0 1-.07 0v-.06a6.58 6.58 0 0 0 .15-4.29a5.25 5.25 0 0 1-.55-2.16c0-2.77 2.55-5 5.68-5M8 .94c-3.83 0-6.93 2.81-6.93 6.27a6.4 6.4 0 0 0 .64 2.64a5.53 5.53 0 0 1-.18 3.48a1.32 1.32 0 0 0 2 1.5a15 15 0 0 0 2.16-1.71a6.8 6.8 0 0 0 2.31.36c3.83 0 6.93-2.81 6.93-6.27S11.83.94 8 .94"/><ellipse cx="5.2" cy="7.7" fill="currentColor" rx=".8" ry=".75"/><ellipse cx="8" cy="7.7" fill="currentColor" rx=".8" ry=".75"/><ellipse cx="10.8" cy="7.7" fill="currentColor" rx=".8" ry=".75"/></svg>
        </div>
        <h2 class="text-2xl font-semibold" :class="[$styles.heading]">{{ $ai.welcome }}</h2>
    `
}

const Avatar = {
    template: `
        <div v-if="$ai.auth && showComponents.length" class="relative" ref="avatarContainer">
            <img
                @click.stop="toggleMenu"
                :src="$ctx.getUserAvatar()"
                :title="authTitle"
                class="mr-1 size-6 rounded-full cursor-pointer hover:ring-2 hover:ring-[var(--assistant-border)]"
            />
            <div
                v-if="showMenu"
                @click.stop
                class="absolute right-0 mt-2 w-48 rounded-md shadow-lg py-1 z-50 border" :class="[$styles.messageAssistant]">

                <div v-for="component in showComponents">
                    <component :is="component" :auth="$ai.auth" @done="showMenu = false" />
                </div>

            </div>
        </div>
    `,
    setup() {
        const ctx = inject('ctx')
        const ai = ctx.ai
        const showMenu = ref(false)
        const avatarContainer = ref(null)

        const authTitle = computed(() => {
            if (!ai.auth) return ''
            const { userId, userName, displayName, bearerToken, roles } = ai.auth
            const name = userName || displayName
            const prefix = roles && roles.includes('Admin') ? 'Admin' : 'Name'
            const sb = [
                name ? `${prefix}: ${name}` : '',
                name && name != userId ? `${userId}` : '',
            ]
            return sb.filter(x => x).join('\n')
        })

        function toggleMenu() {
            showMenu.value = !showMenu.value
        }

        // Close menu when clicking outside
        const handleClickOutside = (event) => {
            if (showMenu.value && avatarContainer.value && !avatarContainer.value.contains(event.target)) {
                showMenu.value = false
            }
        }

        const showComponents = computed(() => {
            const args = { auth: ai.auth }
            return Object.values(ctx.userMenuItemComponents).filter(def => def.show(args)).map(def => def.component)
        })

        onMounted(() => {
            document.addEventListener('click', handleClickOutside)
        })

        onUnmounted(() => {
            document.removeEventListener('click', handleClickOutside)
        })

        return {
            authTitle,
            showMenu,
            toggleMenu,
            avatarContainer,
            showComponents,
        }
    }
}

const SignIn = {
    template: `
    <div class="min-h-full -mt-12 flex flex-col justify-center py-12 sm:px-6 lg:px-8">
        <div class="sm:mx-auto sm:w-full sm:max-w-md">
            <h2 class="mt-6 text-center text-3xl font-extrabold text-gray-900 dark:text-gray-50">
                Sign In
            </h2>
        </div>
        <div class="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
            <ErrorSummary v-if="errorSummary" class="mb-3" :status="errorSummary" />
            <div class="bg-white dark:bg-black py-8 px-4 shadow sm:rounded-lg sm:px-10">
                <form @submit.prevent="submit">
                    <div class="flex flex-1 flex-col justify-between">
                        <div class="space-y-6">
                            <fieldset class="grid grid-cols-12 gap-6">
                                <div class="w-full col-span-12">
                                    <TextInput id="apiKey" name="apiKey" label="API Key" v-model="apiKey" />
                                </div>
                            </fieldset>
                        </div>
                    </div>
                    <div class="mt-8">
                        <PrimaryButton class="w-full">Sign In</PrimaryButton>
                    </div>
                </form>
            </div>
        </div>
    </div>     
    `,
    emits: ['done'],
    setup(props, { emit }) {
        const ctx = inject('ctx')
        const ai = ctx.ai
        const apiKey = ref('')
        const errorSummary = ref()
        async function submit() {
            const r = await ai.get('/auth', {
                headers: {
                    'Authorization': `Bearer ${apiKey.value}`
                },
            })
            const txt = await r.text()
            const json = toJsonObject(txt)
            // console.log('json', json)
            if (r.ok) {
                json.apiKey = apiKey.value
                emit('done', json)
            } else {
                errorSummary.value = json.responseStatus || {
                    errorCode: "Unauthorized",
                    message: 'Invalid API Key'
                }
            }
        }

        return {
            apiKey,
            submit,
            errorSummary,
        }
    }
}

const ErrorViewer = {
    template: `
        <div v-if="$state.error" class="rounded-lg px-4 py-3 bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 text-red-800 dark:text-red-200 shadow-sm">
            <div class="flex items-start space-x-2">
                <div class="flex-1 min-w-0">
                    <div class="flex justify-between items-start">
                        <div class="text-base font-medium mb-1">{{ $state.error?.errorCode || 'Error' }}</div>
                        <button type="button" @click="$ctx.clearError()" title="Clear Error"
                            class="text-red-400 dark:text-red-300 hover:text-red-600 dark:hover:text-red-100 flex-shrink-0">
                            <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                                <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd"></path>
                            </svg>
                        </button>
                    </div>
                    <div v-if="$state.error?.message" class="text-base mb-1">{{ $state.error.message }}</div>
                    <div v-if="$state.error?.stackTrace" class="mt-2 text-sm whitespace-pre-wrap break-words max-h-80 overflow-y-auto font-mono p-2 border border-red-200/70 dark:border-red-800/70">
                        {{ $state.error.stackTrace }}
                    </div>
                </div>
            </div>
        </div>
    `,
    setup() {
    }
}


const SettingsPage = {
    template: `
    <div class="max-w-2xl mx-auto p-6">
        <h1 class="text-2xl font-bold mb-8" :class="[$styles.heading]">Settings</h1>

        <!-- User Avatar Section -->
        <div class="mb-8 p-6 rounded-xl shadow-sm" :class="[$styles.card]">
            <h2 class="text-lg font-semibold mb-4" :class="[$styles.heading]">User Avatar</h2>
            <div class="flex items-center gap-6">
                <label for="userAvatarInput" class="relative group cursor-pointer">
                    <img :src="$ctx.getUserAvatar()" 
                        class="w-20 h-20 rounded-full object-cover border-2 shadow-md" :class="[$styles.messageUser]"
                        alt="User Avatar"
                    />
                    <div class="absolute inset-0 rounded-full bg-black/40 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center">
                        <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 9a2 2 0 012-2h.93a2 2 0 001.664-.89l.812-1.22A2 2 0 0110.07 4h3.86a2 2 0 011.664.89l.812 1.22A2 2 0 0018.07 7H19a2 2 0 012 2v9a2 2 0 01-2 2H5a2 2 0 01-2-2V9z"/>
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 13a3 3 0 11-6 0 3 3 0 016 0z"/>
                        </svg>
                    </div>
                    <input id="userAvatarInput" type="file" class="hidden" accept="image/*" @change="uploadUserAvatar" />
                </label>
                <div class="flex-1">
                    <p class="text-sm text-gray-600 dark:text-gray-400 mb-3">
                        Upload a new image for your avatar
                    </p>
                    <div class="flex items-center gap-3">
                        <label for="userAvatarInput" class="cursor-pointer px-4 py-2 text-sm font-medium transition-colors" :class="[$styles.primaryButton]">
                            <span>Choose File</span>
                        </label>
                        <span v-if="userUploading" class="text-sm text-gray-500 dark:text-gray-400 flex items-center gap-2">
                            <svg class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                            </svg>
                            Uploading...
                        </span>
                        <span v-if="userSuccess" class="text-sm text-green-600 dark:text-green-400 flex items-center gap-1">
                            <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/>
                            </svg>
                            Uploaded!
                        </span>
                    </div>
                    <p v-if="userError" class="mt-2 text-sm text-red-600 dark:text-red-400">{{ userError }}</p>
                </div>
            </div>
        </div>
        
        <!-- Agent Avatar Section -->
        <div class="mb-8 p-6 rounded-xl shadow-sm" :class="[$styles.card]">
            <h2 class="text-lg font-semibold mb-4" :class="[$styles.heading]">Agent Avatar</h2>
            <div class="flex items-center gap-6">
                <label for="agentAvatarInput" class="relative group cursor-pointer">
                    <img 
                        :src="$ctx.getAgentAvatar()" 
                        class="w-20 h-20 rounded-full object-cover border-2 shadow-md" :class="[$styles.messageUser]"
                        alt="Agent Avatar"
                    />
                    <div class="absolute inset-0 rounded-full bg-black/40 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center">
                        <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 9a2 2 0 012-2h.93a2 2 0 001.664-.89l.812-1.22A2 2 0 0110.07 4h3.86a2 2 0 011.664.89l.812 1.22A2 2 0 0018.07 7H19a2 2 0 012 2v9a2 2 0 01-2 2H5a2 2 0 01-2-2V9z"/>
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 13a3 3 0 11-6 0 3 3 0 016 0z"/>
                        </svg>
                    </div>
                    <input id="agentAvatarInput" type="file" class="hidden" accept="image/*" @change="uploadAgentAvatar" />
                </label>
                <div class="flex-1">
                    <p class="text-sm text-gray-600 dark:text-gray-400 mb-3">
                        Upload a new image for your Agent's avatar
                    </p>
                    <div class="flex items-center gap-3">
                        <label for="agentAvatarInput" class="cursor-pointer px-4 py-2 text-sm font-medium transition-colors" :class="[$styles.primaryButton]">
                            <span>Choose File</span>
                        </label>
                        <span v-if="agentUploading" class="text-sm text-gray-500 dark:text-gray-400 flex items-center gap-2">
                            <svg class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                            </svg>
                            Uploading...
                        </span>
                        <span v-if="agentSuccess" class="text-sm text-green-600 dark:text-green-400 flex items-center gap-1">
                            <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/>
                            </svg>
                            Uploaded!
                        </span>
                    </div>
                    <p v-if="agentError" class="mt-2 text-sm text-red-600 dark:text-red-400">{{ agentError }}</p>
                </div>
            </div>
        </div>

        <!-- Appearance Section (backdrop-blur-sm in card styles renders always on top of ThemeSelector Popup) -->
        <div class="mb-8 p-6 rounded-xl shadow-sm flex items-center" :class="[$styles.card]">
            <h2 class="text-lg font-semibold mr-12" :class="[$styles.heading]">Theme</h2>
            <ThemeSelector />
        </div>

    </div>
    `,
    setup() {
        const ctx = inject('ctx')

        const userUploading = ref(false)
        const userSuccess = ref(false)
        const userError = ref('')
        const agentUploading = ref(false)
        const agentSuccess = ref(false)
        const agentError = ref('')

        async function uploadUserAvatar(event) {
            const file = event.target.files?.[0]
            if (!file) return

            userUploading.value = true
            userSuccess.value = false
            userError.value = ''

            try {
                const formData = new FormData()
                formData.append('file', file)

                const response = await ctx.postForm('/user/avatar', { body: formData })
                const result = await response.json()

                if (response.ok && result.success) {
                    userSuccess.value = true
                    ctx.incCacheBreaker()
                    setTimeout(() => { userSuccess.value = false }, 3000)
                } else {
                    userError.value = result.message || 'Upload failed'
                }
            } catch (e) {
                userError.value = e.message || 'Upload failed'
            } finally {
                userUploading.value = false
                event.target.value = ''
            }
        }

        async function uploadAgentAvatar(event) {
            const file = event.target.files?.[0]
            if (!file) return

            agentUploading.value = true
            agentSuccess.value = false
            agentError.value = ''

            try {
                const formData = new FormData()
                formData.append('file', file)

                const response = await ctx.postForm('/agents/avatar', { body: formData })
                const result = await response.json()

                if (response.ok && result.success) {
                    agentSuccess.value = true
                    ctx.incCacheBreaker()
                    setTimeout(() => { agentSuccess.value = false }, 3000)
                } else {
                    agentError.value = result.message || 'Upload failed'
                }
            } catch (e) {
                agentError.value = e.message || 'Upload failed'
            } finally {
                agentUploading.value = false
                event.target.value = ''
            }
        }

        return {
            userUploading,
            userSuccess,
            userError,
            agentUploading,
            agentSuccess,
            agentError,
            uploadUserAvatar,
            uploadAgentAvatar,
        }
    }
}

export default {
    install(ctx) {
        ctx.components({
            Brand,
            Welcome,
            Avatar,
            SignIn,
            ErrorViewer,
            SettingsPage,
        })

        ctx.routes.push(...[
            { path: '/settings', component: SettingsPage },
        ])
    }
}
