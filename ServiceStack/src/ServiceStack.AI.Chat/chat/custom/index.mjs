// This file is intentionally C#-owned: sync.sh does not replace chat/custom/**.
import { ref, computed, inject, onMounted } from 'vue'
import hljs from 'highlight.js'

let ext
let mcpExt

const McpToolPageHeader = {
    template: `
    <div class="text-sm flex flex-col items-end mb-8">
        <!-- Collapsed Header -->
        <div
            @click="toggleExpanded"
            class="inline-flex items-center gap-2 cursor-pointer select-none group"
        >
            <svg
                class="w-5 h-5 transition-transform duration-200"
                :class="[{ 'rotate-90': isExpanded }, $styles.muted]"
                xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor"
            >
                <path d="M10 17l5-5-5-5v10z"/>
            </svg>
            <span class="font-medium" :class="[$styles.mutedActive,$styles.mutedHover]">
                MCP Server
            </span>
            <span v-if="info.isEnabled && (enabledCount > 0 || apiCount > 0)" class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium" :class="[$styles.bgSuccess]">
                {{ enabledCount + apiCount }} {{ (enabledCount + apiCount) === 1 ? 'tool' : 'tools' }}
            </span>
            <span v-else-if="!info.isEnabled" class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-gray-100 dark:bg-gray-800 text-gray-500 dark:text-gray-400 border border-gray-200 dark:border-gray-700">
                Disabled
            </span>
        </div>

        <!-- Expanded Details -->
        <div
            v-if="isExpanded"
            class="w-full mt-3 p-4 rounded-lg shadow-sm border border-gray-200 dark:border-gray-800 text-left"
            :class="[$styles.bgCard]"
        >
            <div class="flex flex-col gap-3">
                <div class="flex items-center justify-between">
                    <span class="font-semibold text-gray-800 dark:text-gray-200">
                        {{ info.serverName || 'servicestack-ai-chat' }} (v{{ info.serverVersion }})
                    </span>
                    <span v-if="info.isEnabled" class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium" :class="[$styles.bgSuccess]">
                        Enabled
                    </span>
                </div>

                <div class="text-xs text-gray-500 dark:text-gray-400">
                    Streamable HTTP MCP Endpoint
                </div>

                <!-- Detail Metadata Table -->
                <table class="w-full text-xs border-separate border-spacing-y-3.5">
                    <tbody>
                        <!-- Endpoint URL -->
                        <tr>
                            <td class="font-medium whitespace-nowrap pr-4 align-top pt-1 w-1" :class="[$styles.muted]">Endpoint URL:</td>
                            <td class="align-top py-1">
                                <div class="flex items-center gap-2 min-w-0">
                                    <code class="px-2 py-1 rounded font-mono text-xs overflow-x-auto" :class="[$styles.codeTag]">
                                        {{ mcpUrl }}
                                    </code>
                                    <button
                                        @click="copyUrl"
                                        type="button"
                                        class="px-2.5 py-1 text-xs font-medium rounded border transition-colors inline-flex items-center gap-1.5 shrink-0 cursor-pointer"
                                        :class="[$styles.secondaryButton]"
                                        :title="copying ? 'Copied to clipboard' : 'Copy MCP Server URL'"
                                    >
                                        <svg v-if="copying" class="w-3.5 h-3.5 text-green-600 dark:text-green-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="m9.55 18l-5.7-5.7l1.425-1.425L9.55 15.15l9.175-9.175L20.15 7.4z"/></svg>
                                        <svg v-else class="w-3.5 h-3.5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="M16 1H4c-1.1 0-2 .9-2 2v14h2V3h12zm3 4H8c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h11c1.1 0 2-.9 2-2V7c0-1.1-.9-2-2-2m0 16H8V7h11z"/></svg>
                                        <span>{{ copying ? 'Copied!' : 'Copy URL' }}</span>
                                    </button>
                                </div>
                            </td>
                        </tr>

                        <!-- Exposed Tools -->
                        <tr v-if="info.tools?.length">
                            <td class="font-medium whitespace-nowrap pr-4 align-top pt-1 w-1" :class="[$styles.muted]">Exposed Tools:</td>
                            <td class="align-top py-1">
                                <div class="flex flex-wrap gap-1.5 min-w-0">
                                    <button
                                        v-for="tool in info.tools"
                                        :key="tool"
                                        type="button"
                                        @click="selectTool(tool)"
                                        title="Click to view/execute tool"
                                        class="font-mono px-2 py-0.5 rounded text-xs transition-colors cursor-pointer"
                                        :class="$ctx.tools?.selectedTool === tool ? $styles.tagButtonActive : $styles.tagButton"
                                    >
                                        {{ tool }}
                                    </button>
                                </div>
                            </td>
                        </tr>

                        <!-- Exposed APIs -->
                        <tr v-if="info.apiTools?.length">
                            <td class="font-medium whitespace-nowrap pr-4 align-top pt-1 w-1" :class="[$styles.muted]">Exposed APIs:</td>
                            <td class="align-top py-1">
                                <div class="flex flex-wrap gap-1.5 min-w-0">
                                    <button
                                        v-for="api in info.apiTools"
                                        :key="api"
                                        type="button"
                                        @click="inspectingApi === api ? closeInspectApi() : inspectApi(api)"
                                        title="Click to view API schema & simulate call"
                                        class="font-mono px-2 py-0.5 rounded text-xs transition-colors cursor-pointer select-none border"
                                        :class="inspectingApi === api ? $styles.tagButtonActive : $styles.tagButton"
                                    >
                                        {{ api }}
                                    </button>
                                </div>
                            </td>
                        </tr>

                        <!-- Instructions -->
                        <tr v-if="info.instructions">
                            <td class="font-medium whitespace-nowrap pr-4 align-top pt-1 w-1" :class="[$styles.muted]">Instructions:</td>
                            <td class="align-top py-1 text-gray-600 dark:text-gray-300 italic">
                                {{ info.instructions }}
                            </td>
                        </tr>
                    </tbody>
                </table>

                <!-- Inline Exposed API Tab Panel -->
                <div v-if="inspectingApi" class="mt-4 pt-3 border-t border-gray-200 dark:border-gray-700/60 animate-in fade-in slide-in-from-top-2 duration-150 text-left">
                    <div class="rounded-lg border border-blue-200 dark:border-blue-900/60 bg-blue-50/30 dark:bg-blue-950/20 overflow-hidden">
                        <!-- Tab Bar Header -->
                        <div class="px-4 py-2.5 bg-blue-100/50 dark:bg-blue-950/60 border-b border-blue-200 dark:border-blue-900/60 flex items-center justify-between">
                            <div class="flex items-center gap-3">
                                <span class="font-mono font-bold text-xs text-blue-700 dark:text-blue-300">API: {{ inspectingApi }}</span>
                                <div class="inline-flex rounded-md shadow-xs overflow-hidden border border-gray-300 dark:border-gray-600 divide-x divide-gray-200 dark:divide-gray-700">
                                    <button type="button" @click="inspectTab = 'describe'"
                                        class="px-3 py-1 text-xs font-medium transition-colors cursor-pointer"
                                        :class="inspectTab === 'describe'
                                            ? 'bg-blue-600 text-white'
                                            : 'bg-white dark:bg-gray-900 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800'">
                                        Schema (api_describe)
                                    </button>
                                    <button type="button" @click="inspectTab = 'invoke'"
                                        class="px-3 py-1 text-xs font-medium transition-colors cursor-pointer"
                                        :class="inspectTab === 'invoke'
                                            ? 'bg-blue-600 text-white'
                                            : 'bg-white dark:bg-gray-900 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800'">
                                        Simulate Call (api_call)
                                    </button>
                                </div>
                            </div>
                            <button @click="closeInspectApi" type="button" class="p-1 rounded-md text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 hover:bg-blue-200/50 dark:hover:bg-blue-900/50 transition-colors cursor-pointer" title="Close API details">
                                <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" viewBox="0 0 20 20" fill="currentColor">
                                    <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
                                </svg>
                            </button>
                        </div>

                        <!-- Tab Panel Content -->
                        <div class="p-4 space-y-3 text-xs">
                            <!-- Tab 1: Describe Schema -->
                            <div v-if="inspectTab === 'describe'" class="space-y-2">
                                <div v-if="loadingDescribe" class="flex items-center gap-2 text-gray-500 italic py-2">
                                    <svg class="animate-spin h-4 w-4 text-blue-500" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                                    </svg>
                                    Fetching API schema from api_describe...
                                </div>
                                <div v-else-if="describeError" class="p-3 rounded-md bg-red-50 dark:bg-red-900/20 text-red-700 dark:text-red-300 font-mono">
                                    {{ describeError }}
                                </div>
                                <div v-else class="space-y-2">
                                    <p class="text-gray-500 dark:text-gray-400">
                                        Schema visible to AI Assistants when calling <code class="font-mono bg-gray-100 dark:bg-gray-800 px-1 py-0.5 rounded">api_describe</code>:
                                    </p>
                                    <pre class="p-3 rounded-lg bg-gray-900 font-mono text-xs overflow-x-auto select-all max-h-80 whitespace-pre-wrap hljs"><code class="language-json" v-html="describeResultHtml"></code></pre>
                                </div>
                            </div>

                            <!-- Tab 2: Simulate Invoke -->
                            <div v-if="inspectTab === 'invoke'" class="space-y-3">
                                <div class="flex items-center justify-between gap-3">
                                    <p class="text-gray-500 dark:text-gray-400">
                                        Simulate calling <code class="font-mono bg-gray-100 dark:bg-gray-800 px-1 py-0.5 rounded">api_call</code> with custom JSON arguments:
                                    </p>
                                    <button type="button" @click="executeInvoke" :disabled="loadingInvoke || invokeArgsObj === undefined"
                                        class="px-3.5 py-1.5 text-xs font-medium rounded-md bg-blue-600 hover:bg-blue-700 text-white disabled:opacity-50 transition-colors inline-flex items-center gap-2 cursor-pointer whitespace-nowrap shrink-0">
                                        <svg v-if="loadingInvoke" class="animate-spin h-3.5 w-3.5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                                            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                                            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                                        </svg>
                                        <span>{{ loadingInvoke ? 'Executing...' : 'Run api_call' }}</span>
                                    </button>
                                </div>

                                <div>
                                    <label class="block text-[11px] font-medium mb-1 text-gray-700 dark:text-gray-300">Arguments (JSON):</label>
                                    <JsonInput v-model="invokeArgsObj" />
                                </div>

                                <div v-if="invokeResult !== null || invokeError" class="space-y-1.5 pt-2 border-t border-gray-200 dark:border-gray-700/60">
                                    <span class="font-medium text-gray-700 dark:text-gray-300 text-[11px]">API Result Payload:</span>
                                    <div v-if="invokeError" class="p-3 rounded-md bg-red-50 dark:bg-red-900/20 text-red-700 dark:text-red-300 font-mono whitespace-pre-wrap">
                                        {{ invokeError }}
                                    </div>
                                    <pre v-else class="p-3 rounded-lg bg-gray-900 font-mono text-xs overflow-x-auto select-all max-h-80 whitespace-pre-wrap hljs"><code class="language-json" v-html="invokeResultHtml"></code></pre>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
    `,
    setup() {
        const ctx = inject('ctx')
        const copying = ref(false)
        const info = computed(() => mcpExt?.state.info || {})
        const enabledCount = computed(() => (info.value.isEnabled && info.value.tools) ? info.value.tools.length : 0)
        const apiCount = computed(() => (info.value.isEnabled && info.value.apiTools) ? info.value.apiTools.length : 0)
        const isExpanded = computed(() => mcpExt?.prefs.expanded !== false)

        const inspectingApi = ref(null)
        const inspectTab = ref('describe')
        const loadingDescribe = ref(false)
        const describeResult = ref('')
        const describeError = ref(null)

        const invokeArgsObj = ref({})
        const loadingInvoke = ref(false)
        const invokeResult = ref(null)
        const invokeError = ref(null)

        function formatToolResult(data) {
            if (data == null) return ''
            let payload = data
            if (Array.isArray(payload) && payload.length === 1 && payload[0]?.type === 'text' && typeof payload[0].text === 'string') {
                const text = payload[0].text.trim()
                if ((text.startsWith('{') && text.endsWith('}')) || (text.startsWith('[') && text.endsWith(']'))) {
                    try {
                        payload = JSON.parse(text)
                    } catch (e) {
                        payload = text
                    }
                } else {
                    payload = text
                }
            }
            if (typeof payload === 'string') {
                const trimmed = payload.trim()
                if ((trimmed.startsWith('{') && trimmed.endsWith('}')) || (trimmed.startsWith('[') && trimmed.endsWith(']'))) {
                    try {
                        return JSON.stringify(JSON.parse(trimmed), null, 2)
                    } catch (e) {
                        return payload
                    }
                }
                return payload
            }
            return JSON.stringify(payload, null, 2)
        }

        function highlight(code, lang = 'json') {
            if (!code) return ''
            try {
                const text = typeof code === 'string' ? code : JSON.stringify(code, null, 2)
                const language = hljs.getLanguage(lang) ? lang : 'plaintext'
                return hljs.highlight(text, { language }).value
            } catch (e) {
                console.error('Highlight error:', e)
                return typeof code === 'string' ? code : JSON.stringify(code, null, 2)
            }
        }

        const describeResultHtml = computed(() => {
            if (!describeResult.value) return ''
            return highlight(describeResult.value, 'json')
        })

        const invokeResultHtml = computed(() => {
            if (!invokeResult.value) return ''
            return highlight(invokeResult.value, 'json')
        })

        const mcpUrl = computed(() => {
            const rel = info.value.url || '/mcp'
            return `${window.location.origin}${ctx.ai.resolveUrl(rel)}`
        })

        function toggleExpanded() {
            if (mcpExt) {
                mcpExt.setPrefs({ expanded: !isExpanded.value })
            }
        }

        async function fetchInfo() {
            if (mcpExt) {
                try {
                    const api = await mcpExt.getJson('')
                    if (api.response) {
                        mcpExt.setState({ info: api.response })
                    }
                } catch (e) {
                    console.error('Failed to fetch MCP info:', e)
                }
            }
        }

        onMounted(() => {
            fetchInfo()
            if (mcpExt?.prefs?.selectedApi) {
                inspectApi(mcpExt.prefs.selectedApi)
            }
        })

        async function copyUrl() {
            await navigator.clipboard.writeText(mcpUrl.value)
            copying.value = true
            setTimeout(() => { copying.value = false }, 2000)
        }

        function selectTool(tool) {
            if (ctx.tools?.selectedTool === tool) {
                ctx.tools?.selectTool({ group: ctx.tools?.selectedGroup || 'All', tool: null })
                return
            }
            let group = 'All'
            if (ctx.state.tool?.groups) {
                for (const [gName, gTools] of Object.entries(ctx.state.tool.groups)) {
                    if (Array.isArray(gTools) && gTools.includes(tool)) {
                        group = gName
                        break
                    }
                }
            }
            ctx.tools?.selectTool({ group, tool })
        }

        async function inspectApi(apiName) {
            if (inspectingApi.value === apiName && describeResult.value) {
                closeInspectApi()
                return
            }
            inspectingApi.value = apiName
            if (mcpExt) {
                mcpExt.setPrefs({ selectedApi: apiName })
            }
            inspectTab.value = 'describe'
            invokeArgsObj.value = {}
            invokeResult.value = null
            invokeError.value = null
            describeError.value = null
            describeResult.value = ''
            loadingDescribe.value = true
            try {
                const toolsExt = ctx.scope('tools')
                const res = await toolsExt.postJson('/exec/api_describe', { names: [apiName] })
                if (res.error) {
                    describeError.value = res.error.message || String(res.error)
                } else {
                    describeResult.value = formatToolResult(res.response)
                }
            } catch (e) {
                describeError.value = e.message || String(e)
            } finally {
                loadingDescribe.value = false
            }
        }

        function closeInspectApi() {
            inspectingApi.value = null
            if (mcpExt) {
                mcpExt.setPrefs({ selectedApi: null })
            }
        }

        async function executeInvoke() {
            if (!inspectingApi.value) return
            if (invokeArgsObj.value === undefined) {
                invokeError.value = 'Invalid JSON arguments'
                return
            }
            loadingInvoke.value = true
            invokeResult.value = null
            invokeError.value = null
            try {
                const parsedArgs = invokeArgsObj.value || {}
                const toolsExt = ctx.scope('tools')
                const res = await toolsExt.postJson('/exec/api_call', { name: inspectingApi.value, args: parsedArgs })
                if (res.error) {
                    invokeError.value = res.error.message || String(res.error)
                } else {
                    invokeResult.value = formatToolResult(res.response)
                }
            } catch (e) {
                invokeError.value = e.message || String(e)
            } finally {
                loadingInvoke.value = false
            }
        }

        return {
            mcpExt,
            info,
            enabledCount,
            apiCount,
            isExpanded,
            toggleExpanded,
            mcpUrl,
            copying,
            copyUrl,
            selectTool,
            inspectingApi,
            inspectTab,
            loadingDescribe,
            describeResult,
            describeError,
            invokeArgsObj,
            loadingInvoke,
            invokeResult,
            invokeError,
            inspectApi,
            closeInspectApi,
            executeInvoke,
            describeResultHtml,
            invokeResultHtml,
        }
    }
}

export default {
    install(ctx) {
        ext = ctx.scope('custom')
        mcpExt = ctx.scope('mcp')

        ctx.components({ McpToolPageHeader })

        ctx.tools?.setToolPageHeaders({
            mcp: McpToolPageHeader
        })

        ctx.pdf.setPreviewActions({
            adminPdf: {
                isVisible: c => ctx.ai.isAdmin && c.entry?.endsWith('.typ')
                    && !/^(?:lib(?:\.preview)?\.typ|lib\/)/i.test(c.entry),
                component: {
                    inheritAttrs: false,
                    props: ['entry', 'buffers', 'rendering', 'save'],
                    template: `
                        <button v-if="publishable" type="button" @click="publish" :disabled="rendering || publishing"
                            title="Publish this template to PDF Admin UI"
                            class="inline-flex items-center gap-1.5 px-2.5 py-1 text-xs disabled:opacity-40 mr-1 text-gray-700 bg-white border border-gray-300 hover:bg-gray-50 rounded-md">
                            {{ publishing ? 'Publishing…' : 'Publish' }}
                        </button>
                        
                        <a v-if="publishable" :href="'/admin-ui/pdf?template=' + entry + '&origin=chat'" title="View template in Admin UI"
                            class="inline-flex items-center justify-center p-1 rounded-md text-gray-500 hover:text-indigo-600 dark:text-gray-400 dark:hover:text-indigo-400 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors">
                            <svg xmlns="http://www.w3.org/2000/svg" class="size-5" viewBox="0 0 24 24">
                                <path d="M0 0h24v24H0z" fill="none" />
                                <g fill="none" stroke="currentColor" stroke-width="1.5">
                                    <path d="M21.544 11.045c.304.426.456.64.456.955c0 .316-.152.529-.456.955C20.178 14.871 16.689 19 12 19c-4.69 0-8.178-4.13-9.544-6.045C2.152 12.529 2 12.315 2 12c0-.316.152-.529.456-.955C3.822 9.129 7.311 5 12 5c4.69 0 8.178 4.13 9.544 6.045Z" />
                                    <path d="M15 12a3 3 0 1 0-6 0a3 3 0 0 0 6 0Z" />
                                </g>
                            </svg>
                        </a>`,
                    setup(props) {
                        const publishing = ref(false)
                        const publishable = computed(() => !!props.entry?.endsWith('.typ')
                            && !/^(?:lib(?:\.preview)?\.typ|lib\/)/i.test(props.entry))
                        const hasUnsavedChanges = () => Object.values(props.buffers || {})
                            .some(x => x.content !== x.saved)
                        const post = async body => {
                            const res = await fetch('/api/AdminPublishPdfTemplate', {
                                method: 'POST', credentials: 'same-origin',
                                headers: Object.assign({ 'Content-Type': 'application/json' }, ctx.ai.headers),
                                body: JSON.stringify(body),
                            })
                            return await ctx.ai.createJsonResult(res)
                        }
                        const publish = async () => {
                            if (!publishable.value || publishing.value) return
                            if (hasUnsavedChanges()) {
                                if (!confirm('Save your changes before publishing? Publishing uses the files on disk.')) return
                                await props.save()
                                if (hasUnsavedChanges()) return
                            }
                            publishing.value = true
                            try {
                                let api = await post({ path: props.entry })
                                if (api.error?.errorCode === 'AlreadyExists') {
                                    const owner = api.error.meta || {}
                                    if (confirm(`${owner.name || props.entry} was published by ${owner.user || 'another user'} from ${owner.source || 'another template'}. Overwrite it?`))
                                        api = await post({ path: props.entry, overwrite: true })
                                }
                                if (api.error) {
                                    if (api.error.errorCode === 'PdfContractValidation' && api.error.errors?.length) {
                                        const details = api.error.errors.map(x => {
                                            const fixture = x.meta?.fixture ? `${x.meta.fixture}: ` : ''
                                            const path = x.fieldName ? `${x.fieldName}: ` : ''
                                            return `${fixture}${path}${x.message}`
                                        }).join('\n')
                                        return ext.setError(Object.assign({}, api.error, { message: `PDF contract validation failed\n${details}` }))
                                    }
                                    return ext.setError(api.error)
                                }
                                ext.toast(`Published ${api.response.template.name}`)
                                if (api.response.libUpdated) ext.toast('lib.typ was updated and may affect other published templates')
                            } catch (e) {
                                ext.setError(ctx.ai.createErrorStatus({ message: e.message || String(e) }))
                            } finally {
                                publishing.value = false
                            }
                        }
                        return { publishing, publishable, publish }
                    },
                }
            }
        })
    },

    async load(ctx) {
        mcpExt = ctx.scope('mcp')
        try {
            const api = await mcpExt.getJson('')
            if (api.response) {
                mcpExt.setState({ info: api.response })
            }
        } catch (e) {
            console.error('Failed to load MCP info:', e)
        }
    }
}
