// This file is intentionally C#-owned: sync.sh does not replace chat/custom/**.
import { ref, computed, inject, onMounted } from 'vue'

let ext
let mcpExt

const McpToolPageHeader = {
    template: `
    <div class="text-sm flex flex-col items-end mb-4">
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
            <span v-if="info.isEnabled && enabledCount > 0" class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium" :class="[$styles.bgSuccess]"
                :title="(info.tools || []).join('\\n')">
                {{ enabledCount }} {{ enabledCount === 1 ? 'tool' : 'tools' }} exposed
            </span>
            <span v-else-if="!info.isEnabled" class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-gray-100 dark:bg-gray-800 text-gray-500 dark:text-gray-400 border border-gray-200 dark:border-gray-700">
                Disabled
            </span>
            <span v-else class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium" :class="[$styles.bgWarning]">
                No tools exposed
            </span>
        </div>

        <!-- Expanded Content -->
        <div v-if="isExpanded" class="mt-3 pb-2 space-y-3 w-full text-left">
            <div class="relative rounded-lg p-4 border" :class="[$styles.infoCard]">
                <div class="flex items-center justify-between gap-2 mb-3 pb-2 border-b border-gray-200 dark:border-gray-700/60">
                    <div class="flex items-center gap-2">
                        <span :class="info.isEnabled ? 'bg-green-500' : 'bg-gray-400'" class="inline-block w-2.5 h-2.5 rounded-full"></span>
                        <span class="font-bold text-base" :class="[$styles.heading]">{{ info.serverName || 'servicestack-ai-chat' }}</span>
                        <span v-if="info.serverVersion" class="text-xs px-1.5 py-0.5 rounded font-mono" :class="[$styles.codeTag]">v{{ info.serverVersion }}</span>
                    </div>
                    <div class="text-xs" :class="[$styles.muted]">
                        Streamable HTTP MCP Endpoint
                    </div>
                </div>

                <div class="space-y-3 text-xs">
                    <!-- Endpoint URL -->
                    <div class="flex flex-col sm:flex-row sm:items-center gap-2">
                        <span class="w-24 flex-shrink-0 font-medium" :class="[$styles.muted]">Endpoint URL:</span>
                        <div class="flex items-center gap-2 flex-1 min-w-0">
                            <code class="px-2 py-1 rounded font-mono text-xs truncate flex-1" :class="[$styles.codeTagStrong]">{{ mcpUrl }}</code>
                            <button type="button"
                                @click.stop="copyUrl"
                                class="px-2.5 py-1 text-xs font-medium rounded border transition-colors inline-flex items-center gap-1 shrink-0"
                                :class="[$styles.secondaryButton]"
                                :title="copying ? 'Copied!' : 'Copy MCP Server URL'"
                            >
                                <svg v-if="copying" class="size-3.5 text-green-600 dark:text-green-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="m9.55 18l-5.7-5.7l1.425-1.425L9.55 15.15l9.175-9.175L20.15 7.4z"/></svg>
                                <svg v-else class="size-3.5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="M16 1H4c-1.1 0-2 .9-2 2v14h2V3h12zm3 4H8c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h11c1.1 0 2-.9 2-2V7c0-1.1-.9-2-2-2m0 16H8V7h11z"/></svg>
                                <span>{{ copying ? 'Copied!' : 'Copy URL' }}</span>
                            </button>
                        </div>
                    </div>

                    <!-- Exposed Tools -->
                    <div v-if="info.tools?.length" class="flex flex-col sm:flex-row sm:items-start gap-2 pt-1">
                        <span class="w-24 flex-shrink-0 font-medium pt-0.5" :class="[$styles.muted]">Exposed Tools:</span>
                        <div class="flex flex-wrap gap-1.5 flex-1">
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
                    </div>

                    <!-- Instructions / Description if present -->
                    <div v-if="info.instructions" class="flex flex-col sm:flex-row sm:items-start gap-2 pt-1">
                        <span class="w-24 flex-shrink-0 font-medium" :class="[$styles.muted]">Instructions:</span>
                        <div class="flex-1 text-gray-600 dark:text-gray-300 italic">
                            {{ info.instructions }}
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
        const isExpanded = computed(() => !!mcpExt?.prefs.expanded)

        const mcpUrl = computed(() => {
            const rel = info.value.url || '/mcp'
            return `${window.location.origin}${ctx.ai.resolveUrl(rel)}`
        })

        function toggleExpanded() {
            if (mcpExt) {
                mcpExt.setPrefs({ expanded: !mcpExt.prefs.expanded })
            }
        }

        async function fetchInfo() {
            if (mcpExt && !mcpExt.state.info) {
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
        })

        async function copyUrl() {
            await navigator.clipboard.writeText(mcpUrl.value)
            copying.value = true
            setTimeout(() => { copying.value = false }, 2000)
        }

        function selectTool(tool) {
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

        return {
            mcpExt,
            info,
            enabledCount,
            isExpanded,
            toggleExpanded,
            mcpUrl,
            copying,
            copyUrl,
            selectTool,
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
                isVisible: c => ctx.ai.isAdmin && c.entry?.endsWith('.typ') && !/^lib(?:\.preview)?\.typ$/i.test(c.entry),
                component: {
                    props: ['entry', 'buffers', 'rendering', 'save'],
                    template: `
                        <button type="button" @click="publish" :disabled="!entry || rendering || publishing"
                            title="Publish this template to App_Data/pdf"
                            class="inline-flex items-center gap-1.5 px-2.5 py-1 text-xs disabled:opacity-40 mr-1 text-gray-700 bg-white border border-gray-300 hover:bg-gray-50 rounded-md">
                            {{ publishing ? 'Publishing…' : 'Publish' }}
                        </button>`,
                    setup(props) {
                        const publishing = ref(false)
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
                            if (!props.entry || publishing.value) return
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
                                if (api.error) return ext.setError(api.error)
                                ext.toast(`Published ${api.response.template.name}`)
                                if (api.response.libUpdated) ext.toast('lib.typ was updated and may affect other published templates')
                            } catch (e) {
                                ext.setError(ctx.ai.createErrorStatus({ message: e.message || String(e) }))
                            } finally {
                                publishing.value = false
                            }
                        }
                        return { publishing, publish }
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

