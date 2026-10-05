import { ref, onMounted, onUnmounted, inject } from "vue"

export default {
    template: `
    <div class="mb-8 p-6 rounded-xl shadow-sm transition-all" :class="[$styles.card]">
        
        <!-- Header -->
        <div class="flex items-start justify-between gap-4 mb-4">
            <div class="flex items-center gap-3">
                <div class="w-10 h-10 rounded-xl bg-emerald-500/10 dark:bg-emerald-500/20 text-emerald-600 dark:text-emerald-400 flex items-center justify-center shrink-0">
                    <svg class="w-6 h-6" viewBox="0 0 24 24" fill="currentColor">
                        <path d="M22.2819 9.8211a5.9847 5.9847 0 0 0-.5157-4.9108 6.0462 6.0462 0 0 0-6.5098-2.9A6.0651 6.0651 0 0 0 4.9807 4.1818a5.9847 5.9847 0 0 0-3.9977 2.9 6.0462 6.0462 0 0 0 .7427 7.0966 5.98 5.98 0 0 0 .511 4.9107 6.051 6.051 0 0 0 6.5146 2.9001A5.9847 5.9847 0 0 0 13.2599 24a6.0557 6.0557 0 0 0 5.7718-4.2058 5.9894 5.9894 0 0 0 3.9977-2.9001 6.0557 6.0557 0 0 0-.7475-7.0729zm-9.022 12.6081a4.4755 4.4755 0 0 1-2.8764-1.0408l.1419-.0804 4.7783-2.7582a.7948.7948 0 0 0 .3927-.6813v-6.7369l2.02 1.1683a.071.071 0 0 1 .038.052v5.5826a4.504 4.504 0 0 1-4.4945 4.4947zm-9.6607-4.1254a4.4708 4.4708 0 0 1-.5346-3.0137l.142.0852 4.783 2.7582a.7712.7712 0 0 0 .7806 0l5.8428-3.3685v2.3324a.0804.0804 0 0 1-.0332.0615L9.74 19.9502a4.4992 4.4992 0 0 1-6.1408-1.6464zM2.3408 7.8956a4.485 4.485 0 0 1 2.3655-1.9728V11.6a.7664.7664 0 0 0 .3879.6765l5.8144 3.3543-2.0201 1.1683a.0757.0757 0 0 1-.071 0l-4.8303-2.7866A4.4992 4.4992 0 0 1 2.3408 7.872zm16.5963 3.8558L13.1038 8.364 15.1192 7.2a.0757.0757 0 0 1 .071 0l4.8303 2.7913a4.4944 4.4944 0 0 1-.6765 8.1042v-5.6772a.79.79 0 0 0-.407-.6667zm2.0107-3.0231l-.142-.0852-4.7735-2.7818a.7759.7759 0 0 0-.7854 0L9.409 9.2297V6.8974a.0662.0662 0 0 1 .0284-.0615l4.8303-2.7866a4.4992 4.4992 0 0 1 6.6802 4.66zM8.3065 12.863l-2.02-1.1636a.0804.0804 0 0 1-.038-.0567V6.0742a4.4992 4.4992 0 0 1 7.3757-3.4537l-.142.0805L8.704 5.459a.7948.7948 0 0 0-.3927.6813zm1.0976-2.3654l2.602-1.4998 2.6069 1.4998v2.9994l-2.5974 1.4997-2.6067-1.4997Z"/>
                    </svg>
                </div>
                <div>
                    <h2 class="text-lg font-semibold flex items-center gap-2" :class="[$styles.heading]">
                        OpenAI Subscription
                    </h2>
                    <p class="text-sm text-gray-500 dark:text-gray-400">
                        Use your ChatGPT Plus or Pro subscription directly in chat
                    </p>
                </div>
            </div>

            <!-- Status Badge -->
            <div class="shrink-0">
                <span v-if="status.connected && !status.expired" class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-emerald-100 text-emerald-800 dark:bg-emerald-950/60 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800/60">
                    <svg class="w-3.5 h-3.5 text-emerald-500" viewBox="0 0 20 20" fill="currentColor">
                        <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd"/>
                    </svg>
                    Connected
                </span>
                <span v-else-if="status.connected && status.expired" class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-amber-100 text-amber-800 dark:bg-amber-950/60 dark:text-amber-300 border border-amber-200 dark:border-amber-800/60">
                    Expired
                </span>
                <span v-else class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400 border border-gray-200 dark:border-gray-700">
                    Not connected
                </span>
            </div>
        </div>

        <!-- Content: Loading State -->
        <div v-if="loading" class="py-6 flex items-center justify-center text-gray-400">
            <svg class="animate-spin w-5 h-5 mr-2" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            Loading status...
        </div>

        <!-- Content: Connected State -->
        <div v-else-if="status.connected && status.plan_enabled !== false" class="space-y-4 pt-2">
            <div class="p-4 rounded-lg bg-gray-50 dark:bg-gray-900/50 border border-gray-200 dark:border-gray-800 flex flex-col sm:flex-row justify-between items-start sm:items-center gap-3">
                <div class="space-y-1">
                    <div class="text-sm font-medium text-gray-900 dark:text-gray-100 flex items-center gap-2">
                        <span>{{ status.email || status.name || 'ChatGPT Account' }}</span>
                        <span v-if="status.plan" class="px-2 py-0.5 text-[11px] font-semibold rounded-md bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-300">
                            {{ status.plan }}
                        </span>
                    </div>
                    <div class="text-xs text-gray-500 dark:text-gray-400">
                        {{ status.plan_enabled === false ? 'Signed in. Authorize ChatGPT plan usage to enable subscription chat.' : 'Text chat uses your authorized ChatGPT plan. Other modalities may use your configured API-key provider.' }}
                    </div>
                </div>

                <button 
                    type="button"
                    @click="disconnect"
                    :disabled="actionLoading"
                    class="px-3.5 py-1.5 text-xs font-medium rounded-lg text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-950/30 border border-red-200 dark:border-red-900/50 transition-colors disabled:opacity-50 shrink-0"
                >
                    <span v-if="actionLoading">Disconnecting...</span>
                    <span v-else>Disconnect</span>
                </button>
            </div>
        </div>

        <!-- Content: Disconnected State -->
        <div v-else class="space-y-4 pt-2">
            <p v-if="status.connected && status.plan_enabled === false" class="text-sm text-amber-600 dark:text-amber-400">
                Signed in. Sign in again and authorize ChatGPT plan usage to enable subscription chat.
            </p>
            <p v-if="status.requires_reconnect" class="text-sm text-amber-600 dark:text-amber-400">
                Your saved connection requires a new sign-in using the public ChatGPT authentication flow.
            </p>
            <p class="text-sm text-gray-600 dark:text-gray-400 leading-relaxed">
                Connect your personal ChatGPT Plus or Pro account to use OpenAI models directly through your existing subscription instead of consumption-based API billing.
            </p>

            <div v-if="status.has_api_key" class="text-xs text-gray-500 dark:text-gray-400 flex items-center gap-1.5">
                <svg class="w-4 h-4 text-blue-500 shrink-0" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd"/>
                </svg>
                <span>OPENAI_API_KEY is currently configured and will remain active until you connect your subscription.</span>
            </div>

            <div class="flex flex-wrap items-center gap-3 pt-1">
                <button
                    v-if="!connecting"
                    type="button"
                    @click="connect(true)"
                    class="cursor-pointer px-4 py-2 text-sm font-medium transition-colors rounded-lg flex items-center gap-2"
                    :class="[$styles.primaryButton]"
                >
                    <svg class="w-4 h-4" viewBox="0 0 24 24" fill="currentColor">
                        <path d="M22.2819 9.8211a5.9847 5.9847 0 0 0-.5157-4.9108 6.0462 6.0462 0 0 0-6.5098-2.9A6.0651 6.0651 0 0 0 4.9807 4.1818a5.9847 5.9847 0 0 0-3.9977 2.9 6.0462 6.0462 0 0 0 .7427 7.0966 5.98 5.98 0 0 0 .511 4.9107 6.051 6.051 0 0 0 6.5146 2.9001A5.9847 5.9847 0 0 0 13.2599 24a6.0557 6.0557 0 0 0 5.7718-4.2058 5.9894 5.9894 0 0 0 3.9977-2.9001 6.0557 6.0557 0 0 0-.7475-7.0729zm-9.022 12.6081a4.4755 4.4755 0 0 1-2.8764-1.0408l.1419-.0804 4.7783-2.7582a.7948.7948 0 0 0 .3927-.6813v-6.7369l2.02 1.1683a.071.071 0 0 1 .038.052v5.5826a4.504 4.504 0 0 1-4.4945 4.4947zm-9.6607-4.1254a4.4708 4.4708 0 0 1-.5346-3.0137l.142.0852 4.783 2.7582a.7712.7712 0 0 0 .7806 0l5.8428-3.3685v2.3324a.0804.0804 0 0 1-.0332.0615L9.74 19.9502a4.4992 4.4992 0 0 1-6.1408-1.6464zM2.3408 7.8956a4.485 4.485 0 0 1 2.3655-1.9728V11.6a.7664.7664 0 0 0 .3879.6765l5.8144 3.3543-2.0201 1.1683a.0757.0757 0 0 1-.071 0l-4.8303-2.7866A4.4992 4.4992 0 0 1 2.3408 7.872zm16.5963 3.8558L13.1038 8.364 15.1192 7.2a.0757.0757 0 0 1 .071 0l4.8303 2.7913a4.4944 4.4944 0 0 1-.6765 8.1042v-5.6772a.79.79 0 0 0-.407-.6667zm2.0107-3.0231l-.142-.0852-4.7735-2.7818a.7759.7759 0 0 0-.7854 0L9.409 9.2297V6.8974a.0662.0662 0 0 1 .0284-.0615l4.8303-2.7866a4.4992 4.4992 0 0 1 6.6802 4.66zM8.3065 12.863l-2.02-1.1636a.0804.0804 0 0 1-.038-.0567V6.0742a4.4992 4.4992 0 0 1 7.3757-3.4537l-.142.0805L8.704 5.459a.7948.7948 0 0 0-.3927.6813zm1.0976-2.3654l2.602-1.4998 2.6069 1.4998v2.9994l-2.5974 1.4997-2.6067-1.4997Z"/>
                    </svg>
                    <span>Sign in with ChatGPT</span>
                </button>

                <button
                    v-if="!connecting"
                    type="button"
                    @click="copySigninLink"
                    :disabled="actionLoading"
                    class="cursor-pointer px-3.5 py-2 text-sm font-medium transition-colors rounded-lg flex items-center gap-2 border border-gray-300 dark:border-gray-700 hover:bg-gray-100 dark:hover:bg-gray-800 text-gray-700 dark:text-gray-300 disabled:opacity-50"
                    title="Generate and copy sign-in link to open in any browser"
                >
                    <svg class="w-4 h-4 text-gray-500" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
                        <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
                    </svg>
                    <span>{{ copiedLink ? 'Copied link!' : 'Copy sign-in link' }}</span>
                </button>

                <button
                    v-if="status.has_codex_auth && !connecting"
                    type="button"
                    @click="importCodex"
                    :disabled="actionLoading"
                    class="cursor-pointer px-4 py-2 text-sm font-medium transition-colors rounded-lg flex items-center gap-2 border border-gray-300 dark:border-gray-700 hover:bg-gray-100 dark:hover:bg-gray-800 text-gray-700 dark:text-gray-300 disabled:opacity-50"
                >
                    <svg class="w-4 h-4 text-emerald-500" viewBox="0 0 20 20" fill="currentColor">
                        <path fill-rule="evenodd" d="M3 17a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zm3.293-7.707a1 1 0 011.414 0L9 10.586V3a1 1 0 112 0v7.586l1.293-1.293a1 1 0 111.414 1.414l-3 3a1 1 0 01-1.414 0l-3-3a1 1 0 010-1.414z" clip-rule="evenodd"/>
                    </svg>
                    <span>Import from Codex CLI</span>
                </button>

                <div v-else-if="connecting" class="flex flex-wrap items-center gap-3">
                    <span class="text-sm text-blue-600 dark:text-blue-400 flex items-center gap-2">
                        <svg class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                        </svg>
                        Waiting for authorization in browser...
                    </span>
                    <button
                        v-if="authUrl"
                        type="button"
                        @click="copySigninLink"
                        class="text-xs text-blue-600 dark:text-blue-400 underline underline-offset-2 flex items-center gap-1 cursor-pointer"
                    >
                        <svg class="w-3.5 h-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
                            <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
                        </svg>
                        {{ copiedLink ? 'Copied link!' : 'Copy sign-in link' }}
                    </button>
                    <a v-if="authUrl" :href="authUrl" target="_blank" class="text-xs text-blue-600 dark:text-blue-400 underline underline-offset-2">
                        Reopen login window
                    </a>
                    <button 
                        type="button" 
                        @click="cancelConnect"
                        class="px-3 py-1.5 text-xs text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200 border border-gray-300 dark:border-gray-700 rounded-lg transition-colors cursor-pointer"
                    >
                        Cancel
                    </button>
                </div>

                <button 
                    type="button" 
                    @click="showManual = !showManual"
                    class="text-xs text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300 underline underline-offset-2 transition-colors ml-auto"
                >
                    {{ showManual ? 'Hide callback entry' : 'Enter callback URL' }}
                </button>
            </div>

            <!-- Manual URL/Code fallback section -->
            <div v-if="showManual" class="mt-4 p-4 rounded-lg bg-gray-50 dark:bg-gray-900/40 border border-gray-200 dark:border-gray-800 space-y-3">
                <p class="text-xs text-gray-600 dark:text-gray-400">
                    After authorizing, copy the complete callback URL from your browser’s address bar and paste it below, including state and client_id. The callback page may not load. A bare authorization code cannot verify this sign-in.
                </p>
                <div class="flex gap-2">
                    <input 
                        type="text" 
                        v-model="manualInput" 
                        placeholder="http://127.0.0.1:1455/auth/callback?code=..." 
                        class="flex-1 px-3 py-2 text-xs rounded-lg border bg-white dark:bg-gray-950 font-mono"
                        :class="[$styles.borderInput, $styles.textInput]"
                    />
                    <button 
                        type="button" 
                        @click="submitManual" 
                        :disabled="!manualInput || actionLoading"
                        class="px-4 py-2 text-xs font-semibold rounded-lg shrink-0 disabled:opacity-50"
                        :class="[$styles.primaryButton]"
                    >
                        Submit
                    </button>
                </div>
                <p v-if="manualError" class="text-xs text-red-500">{{ manualError }}</p>
            </div>
        </div>

    </div>
    `,
    setup() {
        const ctx = inject('ctx')
        const ext = ctx.scope('openai_auth')

        const loading = ref(true)
        const connecting = ref(false)
        const actionLoading = ref(false)
        const showManual = ref(false)
        const manualInput = ref('')
        const manualError = ref('')
        const authUrl = ref('')
        const status = ref({
            connected: false,
            email: '',
            name: '',
            plan: '',
            expires_at: 0,
            expired: false,
            has_api_key: false,
            has_codex_auth: false,
        })

        let pollInterval = null

        async function fetchStatus() {
            try {
                const api = await ext.getJson('/status')
                const res = api?.response || api
                if (res) {
                    status.value = res
                    if (res.connected && res.plan_enabled !== false && !res.pending) {
                        connecting.value = false
                        authUrl.value = ''
                        stopPolling()
                    }
                }
            } catch (e) {
                console.error("Failed to fetch OpenAI subscription status:", e)
            } finally {
                loading.value = false
            }
        }

        function startPolling() {
            stopPolling()
            let count = 0
            pollInterval = setInterval(async () => {
                count++
                await fetchStatus()
                if (count > 60 || status.value.connected) {
                    stopPolling()
                    connecting.value = false
                }
            }, 2000)
        }

        function stopPolling() {
            if (pollInterval) {
                clearInterval(pollInterval)
                pollInterval = null
            }
        }

        const copiedLink = ref(false)

        function cancelConnect() {
            connecting.value = false
            authUrl.value = ''
            copiedLink.value = false
            stopPolling()
        }

        async function connect(openWindow = true) {
            connecting.value = true
            manualError.value = ''
            authUrl.value = ''
            try {
                const api = await ext.postJson('/connect', {})
                const res = api?.response || api
                if (res && res.auth_url) {
                    authUrl.value = res.auth_url
                    showManual.value = res.manual_callback === true
                    if (openWindow) {
                        window.open(res.auth_url, '_blank')
                    }
                    startPolling()
                    return res.auth_url
                } else {
                    connecting.value = false
                    alert(api?.error?.message || res?.responseStatus?.message || "Failed to initiate OpenAI sign-in")
                }
            } catch (e) {
                connecting.value = false
                console.error("Connect error:", e)
                alert("Failed to connect: " + (e.message || e))
            }
            return null
        }

        async function copySigninLink() {
            let url = authUrl.value
            if (!url) {
                actionLoading.value = true
                try {
                    url = await connect(false)
                } finally {
                    actionLoading.value = false
                }
            }

            if (url) {
                try {
                    await navigator.clipboard.writeText(url)
                    copiedLink.value = true
                    ctx.toast("Sign-in link copied to clipboard!")
                    setTimeout(() => {
                        copiedLink.value = false
                    }, 3000)
                } catch (e) {
                    console.error("Clipboard copy failed:", e)
                    window.prompt("Copy this OpenAI sign-in link:", url)
                }
            }
        }

        async function disconnect() {
            if (!confirm("Are you sure you want to disconnect your ChatGPT subscription?")) return
            actionLoading.value = true
            try {
                const api = await ext.postJson('/disconnect', {})
                if (api?.error) {
                    alert(api.error.message || "Failed to disconnect")
                } else {
                    await fetchStatus()
                    ctx.toast("OpenAI subscription disconnected")
                    const res = api?.response || api
                    if (res?.remote_revocation_confirmed === false) {
                        ctx.toast("Disconnected locally. Remote revocation was not confirmed; you can remove app access in ChatGPT Settings.")
                    }
                }
            } catch (e) {
                console.error("Disconnect error:", e)
                alert("Failed to disconnect: " + (e.message || e))
            } finally {
                actionLoading.value = false
            }
        }

        async function submitManual() {
            if (!manualInput.value) return
            actionLoading.value = true
            manualError.value = ''
            try {
                const api = await ext.postJson('/callback_manual', { url_or_code: manualInput.value })
                const res = api?.response || api
                if (res && res.success) {
                    manualInput.value = ''
                    showManual.value = false
                    connecting.value = false
                    authUrl.value = ''
                    stopPolling()
                    await fetchStatus()
                    ctx.toast("OpenAI subscription connected successfully!")
                } else {
                    manualError.value = api?.error?.message || res?.responseStatus?.message || "Failed to complete authentication"
                }
            } catch (e) {
                manualError.value = e.message || "Failed to submit code"
            } finally {
                actionLoading.value = false
            }
        }

        async function importCodex() {
            actionLoading.value = true
            try {
                const api = await ext.postJson('/import_codex', {})
                const res = api?.response || api
                if (res && res.success) {
                    await fetchStatus()
                    ctx.toast("OpenAI subscription imported from Codex CLI!")
                } else {
                    alert(api?.error?.message || res?.responseStatus?.message || "Failed to import Codex credentials")
                }
            } catch (e) {
                console.error("Import error:", e)
                alert("Failed to import: " + (e.message || e))
            } finally {
                actionLoading.value = false
            }
        }

        onMounted(() => {
            fetchStatus()
        })

        onUnmounted(() => {
            stopPolling()
        })

        return {
            loading,
            connecting,
            actionLoading,
            showManual,
            manualInput,
            manualError,
            status,
            connect,
            cancelConnect,
            disconnect,
            submitManual,
            importCodex,
            copiedLink,
            copySigninLink,
        }
    }
}
