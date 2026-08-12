import { ref, computed, onMounted, onUnmounted, Teleport } from "vue"
import { unwrapResponse } from "@servicestack/vue"
const escapeHtml = s => String(s)
    .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
function highlight(json) {
    return escapeHtml(json).replace(
        /("(\\u[a-zA-Z0-9]{4}|\\[^u]|[^\\"])*"(\s*:)?|\b(true|false|null)\b|-?\d+(?:\.\d*)?(?:[eE][+-]?\d+)?)/g,
        match => {
            const cls = /^"/.test(match)
                ? (/:$/.test(match) ? 'text-sky-700 dark:text-sky-300' : 'text-emerald-700 dark:text-emerald-300')
                : /true|false/.test(match) ? 'text-purple-700 dark:text-purple-300'
                : /null/.test(match) ? 'text-gray-400 dark:text-gray-500'
                : 'text-amber-700 dark:text-amber-300'
            return `<span class="${cls}">${match}</span>`
        })
}
const CodeBlock = {
    props: { code: String, html: String, sizeClass: { type: String, default: 'max-h-[60vh]' } },
    template: `
    <div class="relative group flex flex-col h-full">
        <button type="button" @click="copy" :title="copied ? 'Copied' : 'Copy'"
                class="absolute right-2 top-2 z-10 rounded-md p-1.5 opacity-0 group-hover:opacity-100
                       focus:opacity-100 transition-opacity bg-white/80 dark:bg-gray-900/80
                       text-gray-400 hover:text-gray-600 dark:hover:text-gray-200">
            <span class="sr-only">Copy</span>
            <svg v-if="!copied" class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M15.75 17.25v3.375c0 .621-.504
                    1.125-1.125 1.125h-9.75a1.125 1.125 0 0 1-1.125-1.125V7.875c0-.621.504-1.125
                    1.125-1.125H6.75a9.06 9.06 0 0 1 1.5.124m7.5 10.376h3.375c.621 0 1.125-.504
                    1.125-1.125V11.25c0-4.46-3.243-8.161-7.5-8.876a9.06 9.06 0 0 0-1.5-.124H9.375c-.621
                    0-1.125.504-1.125 1.125v3.5m7.5 10.375H9.375a1.125 1.125 0 0 1-1.125-1.125v-9.25m12
                    6.625v-1.875a3.375 3.375 0 0 0-3.375-3.375h-1.5a1.125 1.125 0 0 1-1.125-1.125v-1.5a3.375
                    3.375 0 0 0-3.375-3.375H9.75"/>
            </svg>
            <svg v-else class="w-4 h-4 text-emerald-500" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="m4.5 12.75 6 6 9-13.5"/>
            </svg>
        </button>
        <pre :class="['overflow-auto rounded-lg bg-gray-50 dark:bg-black border border-gray-200',
                      'dark:border-gray-800 p-3 font-mono text-xs leading-relaxed flex-1 min-h-0', sizeClass]"
        ><code v-if="html" v-html="html"></code><code v-else>{{ code }}</code></pre>
    </div>`,
    setup(props) {
        const copied = ref(false)
        return {
            copied,
            copy() {
                navigator.clipboard?.writeText(props.code)
                copied.value = true
                setTimeout(() => copied.value = false, 1200)
            },
        }
    },
}
const template = `
<div>
    <div :class="['grid gap-6 items-start', prefs.schemaCollapsed ? '' : 'lg:grid-cols-2']">
        <ApiFormSchema ref="formSchema" :schema="schema" :client="client" :auto-execute="autoExecute" :sync-url="syncUrl"
                       :field-class="prefs.schemaCollapsed 
                            ? 'col-span-12 sm:col-span-6 xl:col-span-4 2xl:col-span-3'
                            : 'col-span-12 sm:col-span-6 3xl:col-span-4'"
                       @success="onSuccess" @error="onError">
            <template #default="{ requestText, curl }">
                <section class="mt-6">
                    <div class="flex items-center gap-2 mb-1.5">
                        <h2 class="text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                            Request
                        </h2>
                        <span class="flex-1"></span>
                        <button type="button" @click="showCurl = !showCurl"
                                class="text-xs text-gray-500 dark:text-gray-400 hover:text-indigo-600 dark:hover:text-indigo-400">
                            {{ showCurl ? 'hide curl' : 'curl' }}
                        </button>
                    </div>
                    <CodeBlock :code="requestText" size-class="max-h-64" />
                    <CodeBlock v-if="showCurl" :code="curl" size-class="max-h-64" class="mt-2" />
                </section>
            </template>
        </ApiFormSchema>
        <!-- schema -->
        <section v-if="!prefs.schemaCollapsed" class="min-w-0 flex flex-col" :style="formHeight ? { height: formHeight + 'px' } : {}">
            <div class="flex items-center gap-2 mb-1.5 min-h-6 shrink-0">
                <h2 class="text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                    Schema
                </h2>
                <span class="flex-1"></span>
                <button type="button" @click="toggleSchema" title="Collapse schema panel"
                        class="fixed right-0 top-[4.5rem] z-30 rounded-md p-1 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200
                               hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors">
                    <span class="sr-only">Collapse schema</span>
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" d="m8.25 4.5 7.5 7.5-7.5 7.5"/>
                    </svg>
                </button>
            </div>
            <div class="flex-1 min-h-0">
                <CodeBlock :html="schemaHtml" :code="schemaJson" size-class="h-full overflow-auto" />
            </div>
        </section>
    </div>
    <!-- floating expand button when schema panel is collapsed -->
    <button v-if="prefs.schemaCollapsed" type="button" @click="toggleSchema" title="Show schema panel"
            class="fixed right-0 top-[4.5rem] z-30 rounded-md p-1 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200
                   hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors">
        <span class="sr-only">Show schema</span>
        <svg class="w-4 h-4 rotate-180" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" d="m8.25 4.5 7.5 7.5-7.5 7.5"/>
        </svg>
    </button>
    <!-- response -->
    <section class="min-w-0 mt-6">
        <div class="flex items-center gap-2 mb-1.5 min-h-6">
            <h2 class="text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                Response
            </h2>
            <template v-if="result">
                <span :class="['font-mono text-[11px] font-semibold rounded px-1.5 py-0.5',
                               result.ok ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-500/15 dark:text-emerald-300'
                                         : 'bg-rose-100 text-rose-800 dark:bg-rose-500/15 dark:text-rose-300']">
                    {{ result.status }} {{ result.statusText }}
                </span>
                <span class="text-xs text-gray-500 dark:text-gray-400 tabular-nums">
                    {{ result.ms }} ms · {{ result.size }}
                </span>
            </template>
            <span class="flex-1"></span>
            <div v-if="result" class="flex items-center gap-2 text-xs">
                <button v-for="t in tabs" :key="t" type="button" @click="tab = t"
                        :class="['rounded px-1.5 py-0.5',
                                 tab === t ? 'bg-gray-100 dark:bg-gray-800 text-gray-900 dark:text-gray-100 font-medium'
                                           : 'text-gray-500 dark:text-gray-400 hover:text-indigo-600 dark:hover:text-indigo-400']">
                    {{ t }}
                </button>
                <button type="button" @click="maximized = true" title="Maximize view"
                        class="ml-1 rounded p-1 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200
                               hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors">
                    <span class="sr-only">Maximize view</span>
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 3.75v4.5m0-4.5h4.5m-4.5 0L9 9M3.75 20.25v-4.5m0 4.5h4.5m-4.5 0L9 15M20.25 3.75h-4.5m4.5 0v4.5m0-4.5L15 9m5.25 11.25h-4.5m4.5 0v-4.5m0 4.5L15 15"/>
                    </svg>
                </button>
            </div>
        </div>
        <div v-if="!result"
             :class="['rounded-lg border border-dashed border-gray-300 dark:border-gray-700',
                      'flex items-center justify-center text-xs text-gray-500 dark:text-gray-400',
                      paneHeight]">
            Run the API to see its response
        </div>
        <div v-else-if="tab === 'Data'"
             :class="['overflow-auto rounded-lg border border-gray-200 dark:border-gray-800 p-3', paneHeight]">
            <p v-if="payload.key" class="mb-2 text-xs text-gray-500 dark:text-gray-400">
                <code class="font-mono">{{ payload.key }}</code>
                <span v-if="payload.range"> · {{ payload.range }}</span>
            </p>
            <JsonView :value="payload.data" />
        </div>
        <CodeBlock v-else-if="tab === 'JSON'" :html="bodyHtml" :code="result.text" :size-class="paneHeight" />
        <CodeBlock v-else-if="tab === 'Headers'" :code="result.headers" :size-class="paneHeight" />
    </section>
    <Teleport to="body">
        <div v-if="maximized" class="fixed inset-0 z-50 flex flex-col bg-white dark:bg-gray-900 p-4 sm:p-6 overflow-hidden">
            <div class="flex items-center gap-3 pb-3 mb-3 border-b border-gray-200 dark:border-gray-800 shrink-0">
                <div class="flex items-center gap-2 flex-wrap min-w-0">
                    <h2 class="text-base font-semibold text-gray-900 dark:text-gray-100">
                        {{ schema.title || schema.request }} Response
                    </h2>
                    <template v-if="result">
                        <span :class="['font-mono text-xs font-semibold rounded px-2 py-0.5',
                                       result.ok ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-500/15 dark:text-emerald-300'
                                                 : 'bg-rose-100 text-rose-800 dark:bg-rose-500/15 dark:text-rose-300']">
                            {{ result.status }} {{ result.statusText }}
                        </span>
                        <span class="text-xs text-gray-500 dark:text-gray-400 tabular-nums">
                            {{ result.ms }} ms · {{ result.size }}
                        </span>
                    </template>
                    <span v-if="tab === 'Data' && payload.key" class="text-xs text-gray-500 dark:text-gray-400">
                        <code class="font-mono">{{ payload.key }}</code>
                        <span v-if="payload.range"> · {{ payload.range }}</span>
                    </span>
                </div>
                <span class="flex-1"></span>
                <div class="flex items-center gap-2 text-xs">
                    <button v-for="t in tabs" :key="t" type="button" @click="tab = t"
                            :class="['rounded px-2.5 py-1 font-medium',
                                     tab === t ? 'bg-gray-100 dark:bg-gray-800 text-gray-900 dark:text-gray-100'
                                               : 'text-gray-500 dark:text-gray-400 hover:text-indigo-600 dark:hover:text-indigo-400']">
                        {{ t }}
                    </button>
                    <button type="button" @click="maximized = false" title="Restore view (Esc)"
                            class="ml-2 rounded-md p-1.5 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200
                                   hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors">
                        <span class="sr-only">Restore view</span>
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" d="M9 9V4.5M9 9H4.5M9 9 3.75 3.75M9 15v4.5M9 15H4.5M9 15l-5.25 5.25M15 9h4.5M15 9V4.5M15 9l5.25-5.25M15 15h4.5M15 15v4.5m0-4.5 5.25 5.25"/>
                        </svg>
                    </button>
                </div>
            </div>
            <div class="flex-1 min-h-0 overflow-auto rounded-lg border border-gray-200 dark:border-gray-800 p-4 bg-white dark:bg-gray-900">
                <div v-if="tab === 'Data'">
                    <p v-if="payload.key" class="mb-3 text-xs text-gray-500 dark:text-gray-400">
                        <code class="font-mono">{{ payload.key }}</code>
                        <span v-if="payload.range"> · {{ payload.range }}</span>
                    </p>
                    <JsonView :value="payload.data" />
                </div>
                <CodeBlock v-else-if="tab === 'JSON'" :html="bodyHtml" :code="result.text" size-class="h-full min-h-full" />
                <CodeBlock v-else-if="tab === 'Headers'" :code="result.headers" size-class="h-full min-h-full" />
            </div>
        </div>
    </Teleport>
</div>
`
export const ApiExplorerSchema = {
    name: 'ApiExplorerSchema',
    components: { CodeBlock, Teleport },
    props: {
        schema: { type: Object, required: true },
        client: { type: Object, default: null },
        autoExecute: { type: Boolean, default: true },
        syncUrl: { type: Boolean, default: true },
    },
    emits: ['success', 'error'],
    template,
    setup(props, { emit }) {
        const schema = props.schema
        const formSchema = ref(null)
        const result = ref(null)
        const tab = ref('Data')
        const showCurl = ref(false)
        const maximized = ref(false)
        const prefs = ref(JSON.parse(localStorage.getItem('schema:prefs') || '{}'))
        function toggleSchema() {
            prefs.value.schemaCollapsed =  !prefs.value.schemaCollapsed
            localStorage.setItem('schema:prefs', JSON.stringify(prefs.value))
        }
        const formHeight = ref(null)
        let resizeObserver = null
        function onSuccess(payload) {
            result.value = payload.result
            tab.value = payload.json == null ? 'JSON' : 'Data'
            emit('success', payload)
        }
        function onError(payload) {
            result.value = payload.result
            emit('error', payload)
        }
        function onKeydown(e) {
            if (e.key === 'Escape' && maximized.value) {
                maximized.value = false
            }
        }
        onMounted(() => {
            window.addEventListener('keydown', onKeydown)
            const formEl = formSchema.value?.$el?.querySelector?.('form') ?? formSchema.value?.$el
            if (formEl && typeof ResizeObserver !== 'undefined') {
                resizeObserver = new ResizeObserver(entries => {
                    for (const entry of entries) {
                        formHeight.value = entry.target.offsetHeight
                    }
                })
                resizeObserver.observe(formEl)
            }
        })
        onUnmounted(() => {
            window.removeEventListener('keydown', onKeydown)
            if (resizeObserver) resizeObserver.disconnect()
        })
        const schemaJson = JSON.stringify(schema, null, 2)
        return {
            schema, formSchema, result, tab, showCurl, maximized, formHeight, prefs, toggleSchema,
            onSuccess, onError,
            schemaJson,
            schemaHtml: highlight(schemaJson),
            bodyHtml: computed(() => result.value ? highlight(result.value.text) : ''),
            payload: computed(() => {
                const { data, key, envelope } = unwrapResponse(result.value?.json)
                const total = envelope?.total ?? envelope?.Total
                const offset = envelope?.offset ?? envelope?.Offset ?? 0
                const range = Array.isArray(data) && data.length > 0
                    ? (total ? `${offset + 1}-${offset + data.length} of ${total}`
                             : `${offset + 1}-${offset + data.length}`)
                    : null
                return { data, key, range }
            }),
            tabs: ['Data', 'JSON', 'Headers'],
            paneHeight: 'max-h-[70vh] min-h-48',
        }
    },
}
export default ApiExplorerSchema
