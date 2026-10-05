import { ref, computed, inject, onMounted, onUnmounted } from 'vue'

export function registrationUrl(base, userName, nonce) {
    const url = new URL(base)
    url.searchParams.set('callerOrigin', location.origin)
    url.searchParams.set('nonce', nonce)
    if (userName) url.searchParams.set('username', userName)
    return url.href
}
export function registrationMessage(event, frame, url, nonce) {
    return (
        !!frame &&
        event.source === frame.contentWindow &&
        event.origin === new URL(url).origin &&
        event.data?.type === 'register-success' &&
        event.data?.nonce === nonce &&
        typeof event.data.apiKey === 'string' &&
        typeof event.data.userName === 'string'
    )
}
export function initiateRegistration(frame, url, nonce) {
    frame?.contentWindow?.postMessage(
        { type: 'publisher-connect-init', nonce },
        new URL(url).origin,
    )
}
export default {
    props: { account: Object },
    emits: ['connected'],
    template: `<section><p data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.6]">Publisher: {{account?.userName || 'Not connected'}} · {{account?.baseUrl}}</p><button v-if="!account?.apiKey" type="button"  @click="connecting=true" data-jev-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 pointer-coarse:min-h-11">Connect account</button><iframe v-if="connecting&&!account?.apiKey" ref="frame" :src="url" @load="init" title="Connect publisher account" style="width:100%;height:550px;border:0"/></section>`,
    setup(props, { emit }) {
        const ctx = inject('ctx'),
            ext = ctx.scope('publish'),
            frame = ref(null),
            connecting = ref(false)
        const nonce = crypto.randomUUID(),
            owner = ctx.ai.auth?.userName || 'default'
        const url = computed(() =>
            registrationUrl(props.account?.registerUrl, ctx.ai.auth?.userName, nonce),
        )
        const init = () => initiateRegistration(frame.value, url.value, nonce)
        async function receive(event) {
            if (
                (ctx.ai.auth?.userName || 'default') !== owner ||
                !registrationMessage(event, frame.value, url.value, nonce)
            )
                return
            const { apiKey, userName, userId } = event.data
            const result = await ext.postJson('/config.json', {
                apiKey,
                userName,
                userId,
            })
            if ((ctx.ai.auth?.userName || 'default') !== owner) return
            if (result.response) {
                ext.setState({ publish: result.response })
                connecting.value = false
                emit('connected')
            } else ext.setError(result.error, 'Failed to connect publisher account')
        }
        onMounted(() => window.addEventListener('message', receive))
        onUnmounted(() => window.removeEventListener('message', receive))
        return { frame, connecting, url, init }
    },
}
