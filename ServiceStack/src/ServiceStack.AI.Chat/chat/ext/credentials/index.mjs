/* C#-only UI extension (not synced from llms-py):
 * the Identity Auth version of llms-py's credentials SignIn — posts to /auth/login which
 * authenticates with ServiceStack's Authenticate API using the 'credentials' auth provider.
 * User management is the host's (Identity's Admin Users UI), not users.json. */
import { ref, computed, inject } from 'vue'

const SignIn = {
    template: `
    <div class="min-h-full -mt-12 flex flex-col justify-center py-12 sm:px-6 lg:px-8">
        <div class="sm:mx-auto sm:w-full sm:max-w-md">
            <h2 class="mt-6 text-center text-3xl font-extrabold" :class="[$styles.heading]">
                Sign In
            </h2>
        </div>
        <div class="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
            <ErrorSummary v-if="errorSummary" class="mb-3" :status="errorSummary" />
            <div class="py-8 px-4 shadow sm:rounded-lg sm:px-10" :class="[$styles.infoCard]">
                <form @submit.prevent="submit">
                    <div class="flex flex-1 flex-col justify-between">
                        <div class="space-y-6">
                            <fieldset class="grid grid-cols-12 gap-6" :disabled="loading">
                                <div class="w-full col-span-12">
                                    <TextInput id="userName" name="userName" label="Email address"
                                        autocomplete="username" v-model="userName" />
                                </div>
                                <div class="w-full col-span-12">
                                    <TextInput id="password" name="password" type="password" label="Password"
                                        autocomplete="current-password" v-model="password" />
                                </div>
                                <div class="w-full col-span-12">
                                    <CheckboxInput id="rememberMe" name="rememberMe" label="Remember Me"
                                        v-model="rememberMe" />
                                </div>
                            </fieldset>
                        </div>
                    </div>
                    <div class="mt-8">
                        <PrimaryButton class="w-full" :disabled="loading">Sign In</PrimaryButton>
                    </div>
                </form>
            </div>
        </div>
    </div>
    `,
    emits: ['done'],
    setup(props, { emit }) {
        const ctx = inject('ctx')
        const userName = ref('')
        const password = ref('')
        const rememberMe = ref(true)
        const loading = ref(false)
        const errorSummary = ref()

        async function submit() {
            errorSummary.value = null
            if (!userName.value || !password.value) {
                errorSummary.value = {
                    errorCode: 'Validation',
                    message: 'Username and password are required',
                }
                return
            }

            loading.value = true
            const api = await ctx.postJson('/auth/login', {
                body: JSON.stringify({
                    userName: userName.value,
                    password: password.value,
                    rememberMe: rememberMe.value,
                })
            })
            loading.value = false

            if (api.response) {
                emit('done', api.response)
                // the server partitions threads, prefs and media per user, all of which were
                // loaded anonymously before signing in
                location.reload()
            } else {
                errorSummary.value = api.error
            }
        }

        return {
            userName,
            password,
            rememberMe,
            loading,
            submit,
            errorSummary,
        }
    }
}

const AuthMenuItems = {
    template: `
        <div class="px-4 py-2 text-sm border-b border-[var(--user-border)]">
            <div class="font-medium whitespace-nowrap overflow-hidden text-ellipsis">{{ auth.displayName || auth.userName }}</div>
            <div class="text-xs whitespace-nowrap overflow-hidden text-ellipsis">{{ auth.userName }}</div>
        </div>

        <a v-if="isAdmin" href="/admin-ui/users"
            class="block px-4 py-2 text-sm border-b border-[var(--user-border)] hover:bg-[var(--background)]/50">
            <div class="font-medium whitespace-nowrap overflow-hidden text-ellipsis">Manage Users</div>
        </a>

        <button type="button"
            @click="handleSignOut"
            class="w-full text-left px-4 py-2 text-sm flex items-center whitespace-nowrap hover:bg-[var(--background)]/50">
            <svg class="w-4 h-4 mr-2 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"></path>
            </svg>
            Sign Out
        </button>
    `,
    emits: ['done'],
    props: { auth: Object },
    setup(props, { emit }) {
        const ctx = inject('ctx')
        const isAdmin = computed(() => props.auth?.roles?.includes('Admin'))

        async function handleSignOut() {
            emit('done')
            await ctx.ai.signOut()
            // reload to show the sign-in screen with no signed-in user's data left in memory
            location.reload()
        }

        return {
            isAdmin,
            handleSignOut,
        }
    }
}

export default {
    order: 100,
    install(ctx) {
        if (ctx.ai.authType !== 'credentials')
            return

        ctx.setUserMenuItems({
            auth: {
                component: AuthMenuItems,
                show: ({ auth }) => !!auth,
            }
        })

        ctx.component('SignIn', SignIn)
    }
}
