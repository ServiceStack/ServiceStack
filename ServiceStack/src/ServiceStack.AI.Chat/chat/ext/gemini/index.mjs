import { ref, computed, inject, onBeforeUnmount, onMounted, onUnmounted, toRef, watch } from 'vue'
import { appendQueryString, lastLeftPart, leftPart, rightPart } from '@servicestack/client'
import {
    initMetadata, loadFacets, CoverageStrip, BulkEditDialog, MetadataDialog, MetaChip,
    DOC_FIELDS, META_LIST_FIELDS, FACET_FIELDS, LIST_FIELDS
} from './metadata.mjs'
import { initSources, SourcesPanel, RunReport, TrustedFolders } from './sources.mjs'
import {
    initExplorer, Popover, Breadcrumb, FilterChips, CategoryTree, FacetPicker, Modal, SyncState,
    CheckBox, SelectionBar, ConfirmDialog
} from './explorer.mjs'
import { initImport, ImportPanel } from './import.mjs'
import { initAssistants, AssistantsPanel } from './assistants.mjs'

let ext = null
let ctx = null

async function loadFilestores() {
    const api = await ext.getJson("/filestores")
    if (api.error) {
        ext.setError(api.error)
        return
    }
    ext.setState({ filestores: api.response })
}

async function loadDocumentsWithDisplayNames(filestoreId, displayNames) {
    const cachedDocs = Object.values(ext.state.documentsCache)
    const missingDisplayNames = displayNames
        .filter(name => !cachedDocs.some(doc => doc.filestoreId === filestoreId && doc.displayName === name))

    console.log("loadDocumentsWithDisplayNames", filestoreId, cachedDocs.length, displayNames, missingDisplayNames)
    if (missingDisplayNames.length === 0) return
    const api = await ext.getJson(
        appendQueryString(`/documents`, {
            filestoreId: filestoreId,
            displayNames: missingDisplayNames.join(',')
        })
    )
    if (api.error) {
        ext.setError(api.error)
        return
    }
    api.response?.forEach(doc => {
        ext.state.documentsCache[doc.id] = doc
    })
}

function getDefaultGeminiModel() {
    const geminiModels = [
        'gemini-flash-lite-latest',
        'gemini-flash-latest',
        'gemini-3.6-flash',
        'gemini-3.5-flash-lite',
        'gemini-3.5-flash',
        'gemini-3-pro-preview',
    ]
    for (const modelId of geminiModels) {
        const model = ctx.state.models
            ?.find(x => x.id === modelId && x.provider === 'google')
        if (model) return model
    }
    for (const modelId of geminiModels) {
        const model = ctx.state.models?.find(x => x.id === modelId)
        if (model) return model
    }
    return null
}

function getGeminiModel() {
    const prefs = ext.getPrefs()
    if (prefs.model) {
        const model = ctx.state.models?.find(x => x.name === prefs.model || x.id === prefs.model)
        if (model) return model
    }
    return getDefaultGeminiModel()
}

function createNewChat(filestoreId, { category, document, metadataFilter, filters } = {}) {
    console.log('createNewChat', category, document)
    const model = getGeminiModel()
    if (!model) {
        ctx.setError({ message: 'No Gemini model available.' })
        return
    }

    const filestore = ext.state.filestores.find(s => s.id == filestoreId)
    /*
    Gemini Tool:
    {
        "file_search": {
            "file_search_store_names": [
                "fileSearchStores/servicestack-docs-3w65kkumaxcd"
            ]
        }
    }
    OpenAI Tool Call:
    {
        type: "file_search",
        file_search: {
            file_search_store_names: [
                "fileSearchStores/servicestack-docs-3w65kkumaxcd"
            ],
            "metadata_filter": "category=api"
        }
    }
    */

    // OpenAI File Search Tool
    const tool = {
        type: "file_search",
        file_search: {
            file_search_store_names: [filestore.name]
        }
    }
    if (metadataFilter) {
        tool.file_search.metadata_filter = metadataFilter
        tool.filters = filters || []
    } else if (category != null) {
        tool.file_search.metadata_filter = `category=${category || ''}`
        tool.category = category
    } else if (document != null) {
        tool.file_search.metadata_filter = `hash=${document.hash}`
        tool.document = document.displayName
    }
    const tools = [tool]

    const categoryFilter = filters?.find(f => f.field === 'category')?.value ?? category
    const filterCount = filters?.filter(f => f.field !== 'category').length || 0
    const categoryPath = categoryFilter ? `/${String(categoryFilter).replace(/^\/+|\/+$/g, '')}` : ''
    const title = `Ask ${filestore.displayName}${categoryPath}` + (filterCount ? ` (${filterCount} filter${filterCount === 1 ? '' : 's'})`
        : document ? ` about ${document.displayName}` : '')
    const thread = {
        title,
        model,
        tools,
        redirect: true
    }
    // console.log('startNewThread', JSON.stringify(thread, null, 4))
    ctx.chat.setSelectedModel(model)
    ctx.threads.startNewThread(thread)
}

const IssueCard = {
    props: ['name', 'issue'],
    template: `
        <div data-tag="IssueCard" v-if="issue?.count > 0" class="p-3 bg-gray-50 dark:bg-gray-800 rounded border border-gray-200 dark:border-gray-700">
            <div class="flex items-center justify-between mb-2">
                <span class="text-sm font-medium text-gray-900 dark:text-white">{{ name }}</span>
                <span class="px-2 py-0.5 rounded-full text-xs font-medium bg-orange-100 text-orange-800 dark:bg-orange-900 dark:text-orange-200">
                    {{ issue.count }}
                </span>
            </div>
            <div v-if="issue.docs?.length > 0" class="space-y-1">
                <div v-for="doc in issue.docs" :key="doc" class="text-xs text-gray-600 dark:text-gray-400 font-mono truncate">
                    {{ doc }}
                </div>
                <div v-if="issue.count > issue.docs.length" class="text-xs text-gray-500 italic">
                    ... and {{ issue.count - issue.docs.length }} more
                </div>
            </div>
        </div>
    `
}

const SyncReport = {
    components: { IssueCard },
    props: ['syncResult', 'syncing', 'pruning'],
    emits: ['sync', 'prune'],
    template: `
        <div data-tag="SyncReport" class="mb-8">
            <div class="flex justify-between items-start mb-4">
                <div>
                   <h3 class="text-lg font-medium text-gray-900 dark:text-white">Sync Store</h3>
                   <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">Synchronize local and remote documents to detect any issues.</p>
                </div>
                <button type="button"
                    @click="$emit('sync')"
                    :disabled="syncing"
                    :class="[$styles.primaryButton]"
                    class="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium" 
                >
                    <span v-if="syncing">Syncing...</span>
                    <span v-else>Sync Store</span>
                </button>
            </div>

            <div v-if="syncResult" class="space-y-4">
                <!-- Summary -->
                <div class="grid grid-cols-3 gap-4">
                    <div class="bg-gradient-to-br from-blue-50 to-blue-100 dark:from-blue-900/20 dark:to-blue-800/20 rounded-lg p-4 border border-blue-200 dark:border-blue-800">
                        <div class="flex items-center justify-between">
                            <div>
                                <p class="text-sm font-medium text-blue-600 dark:text-blue-400">Local Documents</p>
                                <p class="text-2xl font-bold text-blue-900 dark:text-blue-100 mt-1">{{ syncResult.Summary?.['Local Documents'] || 0 }}</p>
                            </div>
                            <svg class="size-8" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 36 36"><path fill="#31373d" d="M4 36s-4 0-4-4V4s0-4 4-4h26c1 0 2 1 2 1l3 3s1 1 1 2v26s0 4-4 4z"/><path fill="#55acee" d="M5 19v-1s0-2 2-2h21c2 0 2 2 2 2v1z"/><path fill="#e1e8ed" d="M5 32.021V19h25v13s0 2-2 2H7c-2 0-2-1.979-2-1.979M10 3s0-1 1-1h18c1.048 0 1 1 1 1v10s0 1-1 1H11s-1 0-1-1zm12 10h5V3h-5z"/></svg>
                        </div>
                    </div>
                    <div class="bg-gradient-to-br from-purple-50 to-purple-100 dark:from-purple-900/20 dark:to-purple-800/20 rounded-lg p-4 border border-purple-200 dark:border-purple-800">
                        <div class="flex items-center justify-between">
                            <div>
                                <p class="text-sm font-medium text-purple-600 dark:text-purple-400">Remote Documents</p>
                                <p class="text-2xl font-bold text-purple-900 dark:text-purple-100 mt-1">{{ syncResult.Summary?.['Remote Documents'] || 0 }}</p>
                            </div>
                            <svg class="text-blue-600 size-10" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="M19.35 10.04A7.49 7.49 0 0 0 12 4C9.11 4 6.6 5.64 5.35 8.04A5.994 5.994 0 0 0 0 14c0 3.31 2.69 6 6 6h13c2.76 0 5-2.24 5-5c0-2.64-2.05-4.78-4.65-4.96m-8.64 6.25a.996.996 0 0 1-1.41 0L7.2 14.2a.996.996 0 1 1 1.41-1.41L10 14.18l4.48-4.48a.996.996 0 1 1 1.41 1.41z"/></svg>
                        </div>
                    </div>
                    <div class="bg-gradient-to-br from-green-50 to-green-100 dark:from-green-900/20 dark:to-green-800/20 rounded-lg p-4 border border-green-200 dark:border-green-800">
                        <div class="flex items-center justify-between">
                            <div>
                                <p class="text-sm font-medium text-green-600 dark:text-green-400">Matched Documents</p>
                                <p class="text-2xl font-bold text-green-900 dark:text-green-100 mt-1">{{ syncResult.Summary?.['Matched Documents'] || 0 }}</p>
                            </div>
                            <svg class="text-green-600 size-8" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20"><path fill="currentColor" d="M10 20a10 10 0 0 1 0-20a10 10 0 1 1 0 20m-2-5l9-8.5L15.5 5L8 12L4.5 8.5L3 10z"/></svg>
                        </div>
                    </div>
                </div>

                <!-- Issues -->
                <div v-if="hasIssues" class="grid grid-cols-1 md:grid-cols-2 gap-3">
                    <IssueCard name="Missing from Local" :issue="syncResult['Missing from Local']" />
                    <IssueCard name="Missing from Gemini" :issue="syncResult['Missing from Gemini']" />
                    <IssueCard name="Missing Metadata" :issue="syncResult['Missing Metadata']" />
                    <IssueCard name="Metadata Mismatch" :issue="syncResult['Metadata Mismatch']" />
                    <IssueCard name="Unmatched Fields" :issue="syncResult['Unmatched Fields']" />
                    <IssueCard name="Duplicate Documents" :issue="syncResult['Duplicate Documents']" />
                </div>

                <!-- Duplicates are the one finding here with a one-click fix, because their cause
                     is known: an upload adds a copy rather than replacing one. -->
                <div v-if="duplicates" class="rounded-lg p-4 border flex flex-wrap items-center justify-between gap-3"
                    :class="[$styles.chromeBorder]">
                    <div class="text-sm min-w-0">
                        <p class="font-semibold">{{ duplicates.toLocaleString() }} document{{ duplicates === 1 ? ' has' : 's have' }} more than one copy in Gemini</p>
                        <p class="text-xs mt-0.5" :class="[$styles.muted]">
                            Left over from re-indexing before an upload removed the copy it replaced. Keeps the
                            newest copy of each and deletes the rest; nothing local changes.
                        </p>
                    </div>
                    <button type="button" @click="$emit('prune')" :disabled="pruning"
                        class="px-4 py-2 rounded-md text-sm font-semibold border shrink-0" :class="[$styles.chromeBorder]">
                        {{ pruning ? 'Removing…' : 'Remove extra copies' }}
                    </button>
                </div>

                <!-- Success Message -->
                <div v-else class="bg-gradient-to-br from-green-50 to-green-100 dark:from-green-900/20 dark:to-green-800/20 rounded-lg p-4 border border-green-200 dark:border-green-800">
                    <p class="text-sm font-semibold text-green-900 dark:text-green-100">Perfect Sync!</p>
                    <p class="text-xs text-green-700 dark:text-green-300 mt-1">All documents are properly synchronized.</p>
                </div>
            </div>
        </div>
    `,
    setup(props) {
        const hasIssues = computed(() => {
            if (!props.syncResult) return false
            return (
                (props.syncResult['Missing from Local']?.count || 0) > 0 ||
                (props.syncResult['Missing from Gemini']?.count || 0) > 0 ||
                (props.syncResult['Missing Metadata']?.count || 0) > 0 ||
                (props.syncResult['Metadata Mismatch']?.count || 0) > 0 ||
                (props.syncResult['Unmatched Fields']?.count || 0) > 0 ||
                (props.syncResult['Duplicate Documents']?.count || 0) > 0
            )
        })

        const duplicates = computed(() => props.syncResult?.['Duplicate Documents']?.count || 0)

        return {
            hasIssues,
            duplicates,
        }
    }
}

const GeminiModelSelector = {
    props: {
        modelValue: { type: String, default: undefined },
        defaultText: String,
        helpText: String,
    },
    emits: ['update:modelValue'],
    template: `
        <div data-tag="GeminiModelSelector" class="flex items-center space-x-2">
            <button type="button" @click="openModelPicker"
                class="flex items-center justify-between rounded-lg px-3.5 py-2 transition-colors border border-gray-300 bg-white hover:bg-gray-50 text-gray-700 focus:outline-none dark:border-gray-600 dark:bg-gray-900 dark:hover:bg-gray-800 dark:text-gray-300 text-sm cursor-pointer">
                <span class="flex items-center space-x-2 truncate">
                    <ProviderIcon v-if="selectedModelObj?.provider" :provider="selectedModelObj.provider" class="size-4 shrink-0" />
                    <span class="font-medium truncate">
                        {{ overrideModelName ? selectedModelName : resolvedDefaultText }}
                    </span>
                </span>
                <svg class="size-4 opacity-70 shrink-0 ml-2" :class="$styles.icon" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd" />
                </svg>
            </button>
            <button v-if="overrideModelName" type="button" @click="clearModelOverride"
                class="px-2.5 py-2 text-xs font-medium text-gray-600 hover:text-red-600 dark:text-gray-400 dark:hover:text-red-400 border border-gray-300 dark:border-gray-600 rounded-md transition-colors cursor-pointer shrink-0"
                title="Clear model override">
                Clear Override
            </button>
        </div>

        <!-- Inner Model Selection Sub-Dialog -->
        <Teleport to="body">
            <div v-if="isModelPickerOpen" class="fixed inset-0 z-[200] !z-[200] overflow-hidden text-gray-900 dark:text-gray-100" @keydown.escape.stop="isModelPickerOpen = false">
                <div class="fixed inset-0 bg-black/60 transition-opacity" @click="isModelPickerOpen = false"></div>
                <div class="fixed inset-4 md:inset-10 lg:inset-16 flex items-center justify-center">
                    <div class="relative bg-white dark:bg-gray-800 rounded-xl shadow-2xl w-full h-full max-w-5xl max-h-[85vh] flex flex-col overflow-hidden border border-gray-200 dark:border-gray-700">
                        <div class="shrink-0 px-6 py-4 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
                            <div>
                                <h3 class="text-lg font-semibold">Select Model</h3>
                                <p class="text-xs" :class="$styles.muted">{{ helpText || 'Select a model for Gemini File Stores requests' }}</p>
                            </div>
                            <button type="button" @click="isModelPickerOpen = false" class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 transition-colors">
                                <svg class="size-6" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                                    <path fill="currentColor" d="M19 6.41L17.59 5L12 10.59L6.41 5L5 6.41L10.59 12L5 17.59L6.41 19L12 13.41L17.59 19L19 17.59L13.41 12z"/>
                                </svg>
                            </button>
                        </div>
                        <div class="p-4 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50 space-y-3">
                            <div class="flex flex-col sm:flex-row gap-3">
                                <div class="relative flex-1">
                                    <svg class="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-gray-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                                        <path fill-rule="evenodd" d="M9 3.5a5.5 5.5 0 100 11 5.5 5.5 0 000-11zM2 9a7 7 0 1112.452 4.391l3.328 3.329a.75.75 0 11-1.06 1.06l-3.329-3.328A7 7 0 012 9z" clip-rule="evenodd" />
                                    </svg>
                                    <input type="text" v-model="modelSearchQuery" placeholder="Search models by name or ID..."
                                        class="w-full pl-10 pr-8 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm" />
                                    <button v-if="modelSearchQuery" type="button" @click="modelSearchQuery = ''"
                                        class="absolute right-2.5 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 transition-colors p-0.5 rounded-full cursor-pointer"
                                        title="Clear search">
                                        <svg class="size-4" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                            <line x1="18" y1="6" x2="6" y2="18"></line>
                                            <line x1="6" y1="6" x2="18" y2="18"></line>
                                        </svg>
                                    </button>
                                </div>
                                <div class="flex items-center space-x-2">
                                    <label class="text-xs text-gray-500 dark:text-gray-400 whitespace-nowrap">Sort by:</label>
                                    <select v-model="modelSortBy"
                                        class="px-3 py-2 pr-8 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 cursor-pointer">
                                        <option v-for="opt in modelSortOptions" :key="opt.id" :value="opt.id">{{ opt.label }}</option>
                                    </select>
                                    <button type="button" @click="modelSortAsc = !modelSortAsc"
                                        class="p-2 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors cursor-pointer"
                                        :title="modelSortAsc ? 'Ascending' : 'Descending'">
                                        <svg v-if="modelSortAsc" class="size-5 text-gray-600 dark:text-gray-400" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24">
                                            <path fill="currentColor" d="M19 7h3l-4-4l-4 4h3v14h2M2 17h10v2H2M6 5v2H2V5m0 6h7v2H2z"/>
                                        </svg>
                                        <svg v-else class="size-5 text-gray-600 dark:text-gray-400" style="transform: scaleY(-1)" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24">
                                            <path fill="currentColor" d="M19 7h3l-4-4l-4 4h3v14h2M2 17h10v2H2M6 5v2H2V5m0 6h7v2H2z"/>
                                        </svg>
                                    </button>
                                </div>
                            </div>
                        </div>
                        <div class="flex-1 overflow-y-auto p-4">
                            <div v-if="filteredModelList.length === 0" class="text-center py-12 text-gray-500 dark:text-gray-400 text-sm">
                                No models found matching your search.
                            </div>
                            <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
                                <button v-for="m in filteredModelList" :key="m.id + '-' + m.provider"
                                    type="button"
                                    @click="selectModelOverride(m)"
                                    :class="[
                                        'text-left p-3.5 rounded-lg border transition-all cursor-pointer group hover:scale-[1.01]',
                                        isSelectedModel(m)
                                            ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/30 ring-2 ring-blue-500/50'
                                            : 'border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-700/50'
                                    ]">
                                    <div class="flex items-start justify-between mb-1.5">
                                        <div class="flex items-center space-x-2 min-w-0">
                                            <ProviderIcon :provider="m.provider" class="size-4 shrink-0" />
                                            <span class="font-medium text-sm text-gray-900 dark:text-gray-100 truncate">{{ m.name }}</span>
                                        </div>
                                    </div>
                                    <div class="text-[11px] text-gray-500 dark:text-gray-400 font-mono truncate mb-2">{{ m.id }}</div>
                                    <div class="flex flex-wrap gap-1 text-[10px] text-gray-600 dark:text-gray-400">
                                        <span v-if="m.limit?.context" class="px-1.5 py-0.5 rounded bg-gray-100 dark:bg-gray-700 font-mono" :title="(m.limit.context ? m.limit.context.toLocaleString() : '') + ' token context limit'">
                                            {{ formatShortNumber(m.limit.context) }}
                                        </span>
                                        <span v-if="m.release_date" class="px-1.5 py-0.5 rounded bg-gray-100 dark:bg-gray-700 font-mono">
                                            {{ m.release_date }}
                                        </span>
                                        <span v-if="isFreeModel(m)" class="px-1.5 py-0.5 rounded bg-green-100 dark:bg-green-900/50 text-green-700 dark:text-green-300 font-medium">
                                            Free
                                        </span>
                                        <span v-else-if="m.cost && (m.cost.input != null || m.cost.output != null)" class="px-1.5 py-0.5 rounded bg-gray-100 dark:bg-gray-700 font-mono" :title="'Input: $' + formatCostNum(m.cost.input) + ' / Output: $' + formatCostNum(m.cost.output) + ' per 1M tokens'">
                                            {{ formatCostNum(m.cost.input) }}/{{ formatCostNum(m.cost.output) }}
                                        </span>
                                        <span v-if="m.reasoning" class="px-1.5 py-0.5 rounded bg-purple-100 dark:bg-purple-900/50 text-purple-700 dark:text-purple-300">reasoning</span>
                                        <span v-if="m.tool_call" class="px-1.5 py-0.5 rounded bg-blue-100 dark:bg-blue-900/50 text-blue-700 dark:text-blue-300">tools</span>
                                    </div>
                                </button>
                            </div>
                        </div>
                        <div class="px-6 py-3 border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50 flex justify-between items-center text-xs">
                            <span :class="$styles.muted">{{ filteredModelList.length }} models</span>
                            <button type="button" @click="isModelPickerOpen = false"
                                class="px-4 py-1.5 font-medium rounded-md hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors cursor-pointer">
                                Close
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        </Teleport>
    `,
    setup(props, { emit }) {
        const isModelPickerOpen = ref(false)
        const modelSearchQuery = ref('')
        const modelSortBy = ref('release_date')
        const modelSortAsc = ref(false)
        const modelSortOptions = [
            { id: 'release_date', label: 'Release Date' },
            { id: 'name', label: 'Name' },
            { id: 'knowledge', label: 'Knowledge Cutoff' },
            { id: 'last_updated', label: 'Last Updated' },
            { id: 'cost_input', label: 'Cost (Input)' },
            { id: 'cost_output', label: 'Cost (Output)' },
            { id: 'context', label: 'Context Limit' },
        ]

        const controlled = computed(() => props.modelValue !== undefined)
        const overrideModelName = computed(() => controlled.value ? props.modelValue || '' : ext.getPrefs()?.model || '')
        const defaultModelObj = computed(() => getDefaultGeminiModel())
        const defaultModelDisplay = computed(() => defaultModelObj.value?.name || defaultModelObj.value?.id || 'gemini-flash-latest')
        const resolvedDefaultText = computed(() => props.defaultText || ('Default (' + defaultModelDisplay.value + ')'))

        const selectedModelObj = computed(() => {
            const override = overrideModelName.value
            if (override) {
                return ctx.state.models?.find(x => x.name === override || x.id === override) || { name: override, provider: 'google' }
            }
            return defaultModelObj.value
        })

        const selectedModelName = computed(() => selectedModelObj.value?.name || overrideModelName.value)

        function openModelPicker() {
            modelSearchQuery.value = 'Gemini'
            isModelPickerOpen.value = true
        }

        function selectModelOverride(model) {
            const modelName = controlled.value ? model.id || model.name : model.name || model.id
            if (controlled.value) {
                emit('update:modelValue', modelName)
                isModelPickerOpen.value = false
                return
            }
            const prefs = ext.getPrefs()
            prefs.model = modelName
            ext.setPrefs(prefs)
            isModelPickerOpen.value = false
        }

        function clearModelOverride() {
            if (controlled.value) {
                emit('update:modelValue', '')
                return
            }
            const prefs = ext.getPrefs()
            prefs.model = ''
            ext.setPrefs(prefs)
        }

        function isSelectedModel(model) {
            return overrideModelName.value === model.id || overrideModelName.value === model.name
        }

        const excludedSubstrings = [
            '-model',
            '-tts',
            '-embedding',
            '-customtools',
            '-robotics',
            '-computer-use',
            '-image',
            '-translate',
            '-omni',
            '-research',
            'gemma-',
            'lyria-',
            'veo-',
        ]

        const filteredModelList = computed(() => {
            let list = ctx.state.models || []
            // Only allow google provider models for Gemini File Stores
            list = list.filter(m => m.provider === 'google')

            // Exclude non-valid / non-chat models with specified substrings in model id
            list = list.filter(m => {
                if (!m.id) return true
                const lowerId = m.id.toLowerCase()
                return !excludedSubstrings.some(sub => lowerId.includes(sub))
            })

            if (modelSearchQuery.value?.trim()) {
                const q = modelSearchQuery.value.trim().toLowerCase()
                list = list.filter(m =>
                    (m.name && m.name.toLowerCase().includes(q)) ||
                    (m.id && m.id.toLowerCase().includes(q))
                )
            }
            list = [...list]
            list.sort((a, b) => {
                let cmp = 0
                switch (modelSortBy.value) {
                    case 'release_date':
                        cmp = (a.release_date || '').localeCompare(b.release_date || '')
                        break
                    case 'name':
                        cmp = (a.name || '').localeCompare(b.name || '')
                        break
                    case 'knowledge':
                        cmp = (a.knowledge || '').localeCompare(b.knowledge || '')
                        break
                    case 'last_updated':
                        cmp = (a.last_updated || '').localeCompare(b.last_updated || '')
                        break
                    case 'cost_input':
                        cmp = (parseFloat(a.cost?.input) || 0) - (parseFloat(b.cost?.input) || 0)
                        break
                    case 'cost_output':
                        cmp = (parseFloat(a.cost?.output) || 0) - (parseFloat(b.cost?.output) || 0)
                        break
                    case 'context':
                        cmp = (a.limit?.context || 0) - (b.limit?.context || 0)
                        break
                }
                return modelSortAsc.value ? cmp : -cmp
            })
            return list
        })

        function formatShortNumber(num) {
            if (num == null) return '-'
            if (num >= 1000000) return (num / 1000000).toFixed(1) + 'M'
            if (num >= 1000) return (num / 1000).toFixed(0) + 'K'
            return num.toLocaleString()
        }

        function formatCostNum(val) {
            if (val == null) return '0'
            const num = parseFloat(val)
            return num === 0 ? 'Free' : '$' + num.toFixed(2)
        }

        function isFreeModel(m) {
            return m.cost && parseFloat(m.cost.input) === 0 && parseFloat(m.cost.output) === 0
        }

        return {
            isModelPickerOpen,
            modelSearchQuery,
            modelSortBy,
            modelSortAsc,
            modelSortOptions,
            overrideModelName,
            defaultModelObj,
            defaultModelDisplay,
            resolvedDefaultText,
            selectedModelObj,
            selectedModelName,
            openModelPicker,
            selectModelOverride,
            clearModelOverride,
            isSelectedModel,
            filteredModelList,
            formatShortNumber,
            formatCostNum,
            isFreeModel,
        }
    }
}

const FileStoreList = {
    components: { GeminiModelSelector },
    template: `
        <div data-tag="FileStoreList" class="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
            <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-8">
                <div>
                   <h1 class="text-2xl font-bold" :class="[$styles.heading]">File Stores</h1>
                   <p class="text-sm" :class="[$styles.muted]">Manage your file stores for Gemini search grounding</p>
                </div>
                <div class="flex items-center gap-3">
                    <GeminiModelSelector />
                    <button type="button" @click="showCreate = true" class="inline-flex items-center px-4 py-2 border border-transparent rounded-full shadow-sm text-sm font-medium shrink-0" :class="[$styles.primaryButton]">
                        New Store
                    </button>
                </div>
            </div>

            <div v-if="showCreate" class="mb-8 bg-gray-50 dark:bg-gray-800 rounded-lg p-6 border border-gray-200 dark:border-gray-700">
                <h3 class="text-lg font-medium text-gray-900 dark:text-white mb-4">Create New Store</h3>
                <p v-if="loading" class="mb-4 text-sm text-gray-500 dark:text-gray-400">Please wait, this may take a while.</p>
                <form @submit.prevent="createStore" class="flex gap-2">
                    <div class="flex-grow">
                        <label for="storeName" class="sr-only">Store Name</label>
                        <input type="text" id="storeName" v-model="newStoreName" placeholder="e.g. Project Documentation" class="block w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm dark:bg-gray-700 dark:text-white p-2">
                    </div>
                    <button type="submit" :disabled="loading || !newStoreName.trim()" class="inline-flex justify-center py-2 px-4 border border-transparent shadow-sm text-sm font-medium" :class="[$styles.primaryButton]">
                        <span v-if="loading">Creating...</span>
                        <span v-else>Create</span>
                    </button>
                    <button type="button" @click="showCreate = false" class="inline-flex justify-center py-2 px-4 border border-gray-300 dark:border-gray-600 shadow-sm text-sm font-medium" :class="[$styles.secondaryButton]">
                        Cancel
                    </button>
                </form>
            </div>

            <div class="bg-white dark:bg-gray-800 shadow overflow-hidden sm:rounded-md">
                <ul class="divide-y divide-gray-200 dark:divide-gray-700">
                    <li v-for="store in filestores" :key="store.id">
                        <button @click="$emit('select', store.id)" type="button" class="w-full block hover:bg-gray-50 dark:hover:bg-gray-700 transition duration-150 ease-in-out">
                            <div class="px-4 py-4 sm:px-6 flex items-start gap-3">
                                <div class="flex-1 min-w-0">
                                    <div class="flex items-center justify-between">
                                        <p class="text-sm font-medium text-blue-600 dark:text-blue-400 truncate">{{ store.displayName }}</p>
                                        <div class="ml-2 shrink-0 flex">
                                            <p class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full" :class="[$styles.bgSuccess]">
                                                {{ store.activeDocumentsCount || 0 }} docs
                                            </p>
                                        </div>
                                    </div>
                                    <div class="mt-2 sm:flex sm:justify-between">
                                        <div class="sm:flex">
                                            <p class="flex items-center text-sm text-gray-500 dark:text-gray-400">
                                                Created {{ $fmt.date(store.createdAt) }}
                                            </p>
                                        </div>
                                        <div class="mt-2 flex items-center text-sm text-gray-500 dark:text-gray-400 sm:mt-0">
                                            <p>
                                                {{ $fmt.bytes(store.sizeBytes || 0) }}
                                            </p>
                                        </div>
                                    </div>
                                </div>
                                <span @click.prevent.stop="createNewChat(store.id)"
                                    class="ml-2 cursor-pointer shrink-0" :title="'Ask Gemini RAG about ' + store.displayName">
                                    <svg class="size-10 text-gray-400 dark:text-gray-600 hover:text-blue-600 dark:hover:text-blue-400" xmlns="http://www.w3.org/2000/svg" width="21" height="21" viewBox="0 0 21 21"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" d="M13.418 4.214A9.3 9.3 0 0 0 10.5 3.75c-4.418 0-8 3.026-8 6.759c0 1.457.546 2.807 1.475 3.91L3 19l3.916-2.447a9.2 9.2 0 0 0 3.584.714c4.418 0 8-3.026 8-6.758c0-.685-.12-1.346-.345-1.969M16.5 3.5v4m2-2h-4" stroke-width="1"/></svg>
                                </span>
                            </div>
                        </button>
                    </li>
                    <li v-if="filestores.length === 0" class="px-4 py-8 text-center text-gray-500 dark:text-gray-400">
                        No file stores found. Create one to get started.
                    </li>
                </ul>
            </div>
        </div>
    `,
    emits: ['select'],
    setup() {
        const filestores = toRef(ext.state, 'filestores')
        const showCreate = ref(false)
        const newStoreName = ref('')
        const loading = ref(false)

        onMounted(() => {
            loadFilestores()
        })

        async function createStore() {
            if (!newStoreName.value.trim()) return
            loading.value = true
            try {
                await ext.postJson("/filestores", {
                    displayName: newStoreName.value
                })
                await loadFilestores()
                showCreate.value = false
                newStoreName.value = ''
            } finally {
                loading.value = false
            }
        }

        function formatDate(date) {
            if (!date) return ''
            return new Date(date).toLocaleDateString()
        }

        return {
            ext,
            showCreate,
            newStoreName,
            loading,
            filestores,
            createStore,
            formatDate,
            createNewChat,
        }
    }
}

/**
 * A File Store owns much more than the document rows visible in Explore. Make that full blast
 * radius explicit and require the one value an accidental click cannot plausibly supply.
 */
const DeleteStoreDialog = {
    props: {
        open: Boolean,
        busy: Boolean,
        loading: Boolean,
        storeName: String,
        summary: Object,
        modelValue: String,
    },
    emits: ['update:modelValue', 'close', 'confirm'],
    template: `
      <Teleport to="body">
        <div v-if="open" class="fixed inset-0 flex items-center justify-center p-4" style="z-index:220">
            <div class="fixed inset-0 bg-black/60" @click="close"></div>
            <div class="relative flex max-h-[calc(100vh-2rem)] w-full max-w-xl flex-col overflow-hidden rounded-xl border bg-white shadow-2xl dark:bg-gray-900"
                :class="[$styles.chromeBorder]" role="dialog" aria-modal="true" aria-labelledby="delete-store-title">
                <div class="flex items-start gap-3 border-b px-5 py-4" :class="[$styles.chromeBorder]">
                    <div class="mt-0.5 flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-red-100 text-red-600 dark:bg-red-950 dark:text-red-400">
                        <svg class="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <path d="M12 9v4m0 4h.01M10.3 3.7 2.4 17.4A2 2 0 0 0 4.1 20h15.8a2 2 0 0 0 1.7-3L13.7 3.7a2 2 0 0 0-3.4 0Z"/>
                        </svg>
                    </div>
                    <div>
                        <h3 id="delete-store-title" class="font-semibold" :class="[$styles.heading]">
                            Permanently delete {{ storeName }}?
                        </h3>
                        <p class="mt-1 text-sm" :class="[$styles.muted]">
                            This removes the Gemini File Search Store and every linked local record. It cannot be undone.
                        </p>
                    </div>
                </div>

                <div class="overflow-y-auto px-5 py-4">
                    <div v-if="loading" class="flex items-center justify-center gap-2 py-10 text-sm" :class="[$styles.muted]">
                        <svg class="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none">
                            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 0 1 8-8V0A12 12 0 0 0 0 12h4Z"/>
                        </svg>
                        Calculating everything that will be deleted…
                    </div>
                    <template v-else-if="summary">
                        <div class="overflow-hidden rounded-lg border text-sm" :class="[$styles.chromeBorder]">
                            <div class="flex items-center justify-between gap-4 border-b px-3 py-2.5" :class="[$styles.chromeBorder]">
                                <span>Gemini File Search Store <small v-if="!summary.remoteStoreExists" :class="[$styles.muted]">(already absent)</small></span>
                                <b>{{ summary.remoteStoreExists ? 1 : 0 }}</b>
                            </div>
                            <div class="flex items-center justify-between gap-4 border-b px-3 py-2.5" :class="[$styles.chromeBorder]">
                                <span>Gemini documents <small v-if="summary.remoteDocumentBytes" :class="[$styles.muted]">({{ $fmt.bytes(summary.remoteDocumentBytes) }})</small></span>
                                <b>{{ Number(summary.remoteDocuments || 0).toLocaleString() }}</b>
                            </div>
                            <div class="flex items-center justify-between gap-4 border-b px-3 py-2.5" :class="[$styles.chromeBorder]">
                                <span>Local document records <small v-if="summary.documentBytes" :class="[$styles.muted]">({{ $fmt.bytes(summary.documentBytes) }})</small></span>
                                <b>{{ Number(summary.documents || 0).toLocaleString() }}</b>
                            </div>
                            <div class="flex items-center justify-between gap-4 border-b px-3 py-2.5" :class="[$styles.chromeBorder]">
                                <span>Saved imports</span><b>{{ Number(summary.savedImports || 0).toLocaleString() }}</b>
                            </div>
                            <div class="flex items-center justify-between gap-4 border-b px-3 py-2.5" :class="[$styles.chromeBorder]">
                                <span>Import run history</span><b>{{ Number(summary.importRuns || 0).toLocaleString() }}</b>
                            </div>
                            <div class="flex items-center justify-between gap-4 border-b px-3 py-2.5" :class="[$styles.chromeBorder]">
                                <span>Assistants <small v-if="summary.publishedAssistants" class="text-red-600 dark:text-red-400">({{ summary.publishedAssistants }} published)</small></span>
                                <b>{{ Number(summary.assistants || 0).toLocaleString() }}</b>
                            </div>
                            <div class="flex items-center justify-between gap-4 border-b px-3 py-2.5" :class="[$styles.chromeBorder]">
                                <span>Customer conversations</span><b>{{ Number(summary.conversations || 0).toLocaleString() }}</b>
                            </div>
                            <div class="flex items-center justify-between gap-4 px-3 py-2.5">
                                <span>Conversation messages</span><b>{{ Number(summary.messages || 0).toLocaleString() }}</b>
                            </div>
                        </div>

                        <label for="delete-store-confirmation" class="mt-5 block text-sm font-medium">
                            Type <strong>{{ storeName }}</strong> to confirm
                        </label>
                        <input id="delete-store-confirmation" type="text" :value="modelValue"
                            @input="$emit('update:modelValue', $event.target.value)"
                            :disabled="busy" autocomplete="off" spellcheck="false"
                            class="mt-2 block w-full rounded-md px-3 py-2 text-sm"
                            :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]">
                    </template>
                </div>

                <div class="flex justify-end gap-2 border-t px-5 py-3" :class="[$styles.chromeBorder]">
                    <button type="button" @click="close" :disabled="busy"
                        class="rounded-md border px-3 py-1.5 text-sm disabled:opacity-50" :class="[$styles.secondaryButton]">
                        Cancel
                    </button>
                    <button type="button" @click="$emit('confirm')"
                        :disabled="busy || loading || !summary || modelValue !== storeName"
                        class="rounded-md bg-red-600 px-4 py-1.5 text-sm font-semibold text-white hover:bg-red-700 disabled:cursor-not-allowed disabled:opacity-40">
                        {{ busy ? 'Deleting everything…' : 'Delete everything' }}
                    </button>
                </div>
            </div>
        </div>
      </Teleport>
    `,
    setup(props, { emit }) {
        function close() { if (!props.busy) emit('close') }
        function onKey(event) { if (event.key === 'Escape' && props.open) close() }
        onMounted(() => document.addEventListener('keydown', onKey))
        onBeforeUnmount(() => document.removeEventListener('keydown', onKey))
        return { close }
    },
}

const FileStoreDetails = {
    components: {
        SyncReport, GeminiModelSelector, CoverageStrip, SelectionBar, BulkEditDialog, MetadataDialog,
        MetaChip, ConfirmDialog, SourcesPanel, ImportPanel, AssistantsPanel, RunReport, TrustedFolders,
        Popover, Breadcrumb, FilterChips, CategoryTree, FacetPicker, Modal, SyncState, CheckBox,
        DeleteStoreDialog
    },
    props: ['storeId'],

    template: `
        <!-- Room for the docked selection bar, so the last row isn't the one it covers. -->
        <div data-tag="FileStoreDetails" class="mx-auto px-4 sm:px-6 lg:px-8 py-8"
            :class="[bulkCount ? 'pb-24' : '', view === 'assistants' ? 'max-w-7xl' : 'max-w-5xl']" v-if="store">
            <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-8">
                 <div class="flex items-center gap-4">
                     <button type="button"
                        @click="$emit('back')"
                        class="p-2 rounded-full transition-colors focus:outline-none"
                        :class="[$styles.icon,$styles.iconHover]"
                     >
                        <svg class="w-6 h-6" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M19 12H5M12 19l-7-7 7-7"/></svg>
                     </button>
                     <div>
                        <h1 class="text-2xl font-bold" :class="$styles.heading">{{ store.displayName }}</h1>
                        <p class="text-sm" :class="$styles.muted">Upload documents to this store</p>
                     </div>
                 </div>
                 <div class="flex items-center gap-3">
                     <GeminiModelSelector />
                     <input type="file" ref="fileInput" class="hidden" multiple @change="handleFileUpload">
                     <button type="button" 
                        @click="createNewChat(storeId)"
                        :disabled="uploading"
                        class="inline-flex items-center px-4 py-2 border border-transparent rounded-full shadow-sm text-sm font-medium shrink-0" :class="[$styles.primaryButton]">
                        <span v-if="uploading">Uploading...</span>
                        <span v-else>New Chat</span>
                     </button>
                 </div>
            </div>


            <!-- Explore is the everyday view; Import is a place you go on purpose. Splitting them
                 keeps a form nobody needs while browsing from dominating the page. -->
            <div class="flex gap-1 mb-6 border-b" :class="[$styles.chromeBorder]">
                <button type="button" @click="selectView('explore')"
                    class="px-4 py-2 text-sm font-medium border-b-2 -mb-px"
                    :class="view === 'explore' ? 'border-blue-500 text-blue-600 dark:text-blue-400'
                                               : 'border-transparent hover:bg-gray-100 dark:hover:bg-gray-800'">
                    Explore
                    <span v-if="facetTotal" class="ml-1 text-xs tabular-nums" :class="[$styles.muted]">{{ facetTotal.toLocaleString() }}</span>
                </button>
                <button type="button" @click="selectView('import')"
                    class="px-4 py-2 text-sm font-medium border-b-2 -mb-px"
                    :class="view === 'import' ? 'border-blue-500 text-blue-600 dark:text-blue-400'
                                              : 'border-transparent hover:bg-gray-100 dark:hover:bg-gray-800'">
                    Import
                    <span v-if="sourceCount" class="ml-1 text-xs tabular-nums" :class="[$styles.muted]">{{ sourceCount }}</span>
                </button>
                <button type="button" @click="selectView('assistants')"
                    class="px-4 py-2 text-sm font-medium border-b-2 -mb-px"
                    :class="view === 'assistants' ? 'border-blue-500 text-blue-600 dark:text-blue-400'
                                                  : 'border-transparent hover:bg-gray-100 dark:hover:bg-gray-800'">
                    Assistants
                    <span v-if="assistantCount" class="ml-1 text-xs tabular-nums" :class="[$styles.muted]">{{ assistantCount }}</span>
                </button>
            </div>

            <div v-show="view === 'explore'">
                <div class="bg-white dark:bg-gray-800 shadow overflow-hidden sm:rounded-md mb-8">
                   <div class="px-4 py-4 border-b border-gray-200 dark:border-gray-700 flex flex-wrap justify-between items-center gap-3 bg-gray-50 dark:bg-gray-900/50">
                       <div class="flex flex-wrap items-center justify-between gap-3 grow">
                         <div class="flex flex-wrap items-center gap-3">
                           <div class="relative w-64 shrink-0">
                               <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                                  <svg class="h-4 w-4 text-gray-400" viewBox="0 0 20 20" fill="currentColor">
                                      <path fill-rule="evenodd" d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817 4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z" clip-rule="evenodd" />
                                  </svg>
                               </div>
                               <input type="text" v-model.lazy="ext.prefs.q" placeholder="Search"
                                   class="block w-full pl-9 pr-8 py-1.5 rounded-md" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]">
                               <button v-if="ext.prefs.q" @click="ext.prefs.q = ''; loadDocuments()" type="button" class="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-400 hover:text-gray-600 dark:hover:text-gray-300">
                                   <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
                                       <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
                                   </svg>
                               </button>
                           </div>
                           <select v-model="ext.prefs.sortBy" class="block rounded-md sm:text-sm py-1.5 pl-3 pr-8 cursor-pointer transition-colors border border-gray-300 bg-white hover:bg-gray-50 text-gray-700 focus:outline-none dark:border-gray-600 dark:bg-gray-900 dark:hover:bg-gray-800 dark:text-gray-300">
                               <option value="-uploadedAt">Newest First</option>
                               <option value="uploadedAt">Oldest First</option>
                               <option value="displayName">Name (A-Z)</option>
                               <option value="-displayName">Name (Z-A)</option>
                               <option value="-createdAt">Created (Newest)</option>
                               <option value="createdAt">Created (Oldest)</option>
                               <option value="-size">Size (Largest)</option>
                               <option value="size">Size (Smallest)</option>
                               <option value="issues">Sync Issues</option>
                               <option value="failed">Failed</option>
                               <option value="uploading">Uploading</option>
                           </select>
                         </div>
                         <div class="flex items-center gap-2">
                             <button type="button" @click="createNewChat(storeId, { metadataFilter: filterExpression, filters: chatFilters })"
                                 class="px-3 py-1.5 rounded-md text-sm font-medium whitespace-nowrap"
                                 :class="[$styles.primaryButton]"
                                 :title="askAboutTitle">
                                 Ask about this
                             </button>
                             <!-- Creating a category is importing into it: an empty category means
                                  nothing now that categories are derived from what was ingested. -->
                             <button type="button" @click="importInto(ext.prefs.category)"
                                  class="px-3 py-1.5 rounded-md text-sm font-medium whitespace-nowrap" :class="[$styles.secondaryButton]"
                                  :title="ext.prefs.category
                                      ? 'Add documents to ' + ext.prefs.category
                                      : 'Categories are created by importing into them'">
                                 Import here
                             </button>
                         </div>
                       </div>
                       <div class="flex items-center gap-4 text-sm font-medium">
                           <span v-if="!ext.prefs.q && totalPages > 0" class="text-gray-600 dark:text-gray-400">
                               Page {{ ext.prefs.page }} of {{ totalPages }}
                           </span>
                           <button v-if="ext.prefs.page > 1" @click="ext.prefs.page--; loadDocuments()" type="button" class="text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300 disabled:text-gray-400 disabled:cursor-not-allowed">
                               &larr; previous
                           </button>
                           <button v-if="ext.prefs.page < totalPages" @click="ext.prefs.page++; loadDocuments()" type="button" class="text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300 disabled:text-gray-400 disabled:cursor-not-allowed">
                               next &rarr;
                           </button>
                       </div>
                   </div>
                   <!-- Where you are. A category is a path, so the honest rendering is a path. -->
                   <div class="px-4 py-2 sm:px-6 flex flex-wrap items-center justify-between gap-x-4 gap-y-1 border-b"
                       :class="[$styles.chromeBorder]">
                       <Breadcrumb :path="ext.prefs.category" :root-label="store?.displayName" @go="selectCategory" />
                       <span v-if="searching" class="text-xs" :class="[$styles.muted]">
                           Searching {{ ext.prefs.category ? 'this folder and below' : 'all folders' }}
                       </span>
                       <span v-else-if="folderCount" class="text-xs tabular-nums" :class="[$styles.muted]">
                           {{ folderCount }} folder{{ folderCount === 1 ? '' : 's' }}
                       </span>
                   </div>

                   <!-- Select, then filter state, then the two panels that produce it. Filters
                        live next to the rows they act on rather than in a permanent column. -->
                   <div class="pl-4 pr-2 py-2 sm:pl-6 flex flex-wrap items-center justify-between gap-x-3 gap-y-2 text-xs border-b"
                       :class="[$styles.chromeBorder]">
                       <div class="flex items-center gap-3 shrink-0">
                           <CheckBox :model-value="allOnPageSelected" @update:model-value="togglePage()"
                               :disabled="!docs.length" />
                           <span v-if="!selected.size && !selectAllMatching" :class="[$styles.muted]">Select</span>
                           <template v-else-if="selectAllMatching">
                               <span>All <b>{{ (totalDocs || 0).toLocaleString() }}</b> matching are selected.</span>
                               <button type="button" class="text-blue-600 dark:text-blue-400 underline" @click="selectAllMatching = false">Only this page</button>
                           </template>
                           <template v-else>
                               <span><b>{{ selected.size }}</b> selected on this page.</span>
                               <button v-if="allOnPageSelected && totalDocs && totalDocs > docs.length" type="button"
                                   class="text-blue-600 dark:text-blue-400 underline" @click="selectAllMatching = true">
                                   Select all {{ totalDocs.toLocaleString() }}
                               </button>
                           </template>
                       </div>

                       <FilterChips class="grow" :active="filterChips" @remove="removeFilter" @clear="clearFilters" />

                       <div class="flex items-center gap-2 shrink-0">
                           <Popover v-for="field in availableQuickFilters" :key="field" :label="quickFilterLabel(field)" icon>
                               <template #default="{ close }">
                                   <div class="text-[11px] font-semibold uppercase tracking-wide mb-1.5" :class="[$styles.muted]">
                                       Filter by {{ quickFilterLabel(field) }}
                                   </div>
                                   <button v-for="v in (facets[field]?.values || []).slice(0, 30)" :key="v.value"
                                       type="button" @click="close(); pickFacet(field, v.value)"
                                       class="w-full flex justify-between items-center gap-3 px-2 py-1 rounded text-sm hover:bg-gray-100 dark:hover:bg-gray-800">
                                       <span class="truncate">{{ v.value }}</span>
                                       <span class="tabular-nums text-xs shrink-0" :class="[$styles.muted]">{{ v.count.toLocaleString() }}</span>
                                   </button>
                                   <button v-if="facets[field]?.null" type="button" @click="close(); showMissing(field)"
                                       class="w-full flex justify-between items-center gap-3 px-2 py-1 rounded text-sm hover:bg-gray-100 dark:hover:bg-gray-800">
                                       <span class="text-gray-400 dark:text-gray-500">(no value)</span>
                                       <span class="tabular-nums text-xs shrink-0" :class="[$styles.muted]">{{ facets[field].null.toLocaleString() }}</span>
                                   </button>
                               </template>
                           </Popover>
                           <span v-if="availableQuickFilters.length" class="h-5 border-l" :class="[$styles.chromeBorder]" aria-hidden="true"></span>
                           <Popover label="Categories" :count="folderTotal || ''">
                               <template #default="{ close }">
                                   <CategoryTree :tree="facets.category?.tree" :active="ext.prefs.category"
                                       :total="facetTotal" :root-label="store?.displayName"
                                       @go="p => { selectCategory(p); close() }" />
                               </template>
                           </Popover>
                            <button type="button" @click="coverageOpen = true"
                                class="px-2.5 py-1 rounded-md border text-xs font-medium inline-flex items-center gap-1.5"
                                :class="[$styles.secondaryButton]">
                                Coverage
                               <!-- Ambient, not an alarm: a count that tells you there is something
                                    to look at without interrupting what you came here to do. -->
                               <span v-if="pending.count" class="tabular-nums px-1 rounded"
                                   :class="[$styles.tagLabel]" :title="pending.count + ' documents have unpushed metadata'">
                                   {{ pending.count }}
                               </span>
                           </button>
                       </div>
                   </div>

                   <ul class="divide-y divide-gray-200 dark:divide-gray-700">
                       <!-- Folders first, the way a file explorer orders them. Hidden while
                            searching, because a search spans folders and the rows would be a
                            second, competing answer to "what matched". -->
                        <li v-if="!searching && ext.prefs.category" class="px-4 sm:px-6">
                            <button type="button" @click="selectCategory(parentCategory)"
                                class="w-full py-2.5 flex items-center gap-3 text-sm text-left group">
                                <svg class="size-4 shrink-0 transition-transform group-hover:-translate-x-0.5"
                                    :class="[$styles.muted]" viewBox="0 0 24 24" fill="none"
                                    stroke="currentColor" stroke-width="2" stroke-linecap="round"
                                    stroke-linejoin="round" aria-hidden="true">
                                    <polyline points="9 14 4 9 9 4"/>
                                    <path d="M20 20v-7a4 4 0 0 0-4-4H4"/>
                                </svg>
                                <span :class="[$styles.muted]">{{ parentCategory == null ? (store?.displayName || 'Top level') : parentCategory }}</span>
                            </button>
                        </li>
                       <li v-for="f in (searching ? [] : childFolders)" :key="f.path" class="px-4 sm:px-6">
                           <button type="button" @click="selectCategory(f.path)"
                               class="w-full py-2.5 flex items-center justify-between gap-3 text-sm text-left">
                                <span class="flex items-center gap-2 min-w-0">
                                    <svg class="size-4 shrink-0 opacity-70" viewBox="0 0 24 24" fill="none"
                                        stroke="currentColor" stroke-width="2" stroke-linecap="round"
                                        stroke-linejoin="round" aria-hidden="true">
                                        <path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/>
                                    </svg>
                                    <span class="font-medium truncate">{{ f.path === '' ? '(uncategorised)' : f.name }}</span>
                                </span>
                               <span class="tabular-nums text-xs shrink-0" :class="[$styles.muted]"
                                   :title="f.own + ' here, ' + f.total + ' including subfolders'">
                                   {{ f.total.toLocaleString() }}
                               </span>
                           </button>
                       </li>
                       <li v-for="doc in docs" :key="doc.id">
                           <div class="px-4 py-4 sm:px-6 flex items-center justify-between"
                                :class="selected.has(doc.id) ? 'bg-blue-50 dark:bg-blue-900/20' : ''">
                                <div class="flex items-center min-w-0 flex-1">
                                    <CheckBox class="mr-3" :model-value="selected.has(doc.id)"
                                        @update:model-value="toggleDoc(doc.id)" />
                                    <div class="text-sm min-w-0 flex-1 mr-4">
                                       <div class="flex items-center gap-x-1">
                                           <!-- Only while searching: in a folder listing the path
                                                is the breadcrumb, and repeating it on every row is
                                                noise. In results it's the one thing that locates a hit. -->
                                            <template v-if="searching && doc.category">
                                                <span class="cursor-pointer inline-flex items-center gap-1 rounded font-medium text-gray-800 dark:text-gray-200"
                                                    @click="selectCategory(doc.category)" title="Go to this folder">
                                                    <svg class="size-3.5 shrink-0 opacity-70" viewBox="0 0 24 24" fill="none"
                                                        stroke="currentColor" stroke-width="2" stroke-linecap="round"
                                                        stroke-linejoin="round" aria-hidden="true">
                                                        <path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/>
                                                    </svg>
                                                    {{ doc.category }}
                                                </span>
                                               <span>/</span>
                                           </template>
                                           <span v-if="deletingDocs.has(doc.id)"
                                               class="font-medium text-red-600 dark:text-red-400 line-through truncate"
                                               :title="'Deleting ' + doc.displayName">{{ doc.displayName }}</span>
                                           <a v-else :href="doc.url + '?download'" class="font-medium text-blue-600 dark:text-blue-400 hover:text-blue-700 dark:hover:text-blue-300 truncate" :title="'Download ' + doc.displayName">{{ doc.displayName }}</a>
                                       </div>
                                       <div class="flex items-center flex-wrap gap-1.5 mt-1">
                                           <span class="shrink-0 text-gray-500 dark:text-gray-400">
                                               {{ $fmt.bytes(doc.size) }} &middot; {{ $fmt.date(doc.uploadedAt || doc.createdAt) }}
                                           </span>
                                           <MetaChip v-for="m in docMeta(doc)" :key="m.id" :field="m.k" :value="m.v" />
                                           <span v-if="doc.tombstonedAt" class="px-1.5 py-0.5 rounded border text-[11px] text-red-500 border-red-500/50">removed upstream</span>
                                           <!-- At the end of what the metadata says, because that
                                                is where you are when you notice it's wrong. On a
                                                document with none, it's the invitation to add it. -->
                                           <button type="button" @click.stop="editDocument(doc)"
                                               class="px-1.5 py-0.5 rounded border text-[11px] inline-flex items-center gap-1"
                                               :class="[$styles.secondaryButton, $styles.muted, $styles.mutedHover]"
                                               :title="'Edit metadata for ' + doc.displayName">
                                               <svg class="size-3" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                                                   stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                                                   <path d="M12 20h9"/><path d="M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4z"/>
                                               </svg>
                                               {{ docMeta(doc).length ? 'Edit' : 'Add metadata' }}
                                           </button>
                                       </div>
                                    </div>
                                </div>
                                <div class="shrink-0 flex items-center gap-2">
                                    <span v-if="deletingDocs.has(doc.id)" class="ml-2 p-1 text-red-600 dark:text-red-400" title="Deleting document…">
                                        <svg class="size-5 animate-spin" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                                            <circle cx="12" cy="12" r="9" opacity=".25"/><path d="M21 12a9 9 0 0 0-9-9"/>
                                        </svg>
                                    </span>
                                    <button v-else type="button" @click.stop="deleteDocument(doc)" class="ml-2 p-1 text-gray-400 hover:text-red-600 dark:hover:text-red-400 transition-colors" title="Delete document">
                                        <svg class="size-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="M7 21q-.825 0-1.412-.587T5 19V6H4V4h5V3h6v1h5v2h-1v13q0 .825-.587 1.413T17 21zM17 6H7v13h10zM9 17h2V8H9zm4 0h2V8h-2zM7 6v13z"/></svg>
                                    </button>
                                    <!-- Show loading indicator if document is being uploaded/processed -->
                                    <span v-if="doc.startedAt && !doc.uploadedAt && !doc.error" class="p-1 text-blue-600" title="Uploading to Gemini...">
                                        <svg class="size-5 animate-spin" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="M12 2A10 10 0 1 0 22 12A10 10 0 0 0 12 2Zm0 18a8 8 0 1 1 8-8A8 8 0 0 1 12 20Z" opacity=".5"/><path fill="currentColor" d="M20 12h2A10 10 0 0 0 12 2V4A8 8 0 0 1 20 12Z"/></svg>
                                    </span>
                                    <!-- Show re-upload button only if document has been uploaded -->
                                    <button v-else-if="doc.uploadedAt || doc.error" type="button" @click.stop="reuploadDocument(doc)" :disabled="reuploadingDocs.has(doc.id)" class="p-1 text-gray-400 hover:text-blue-600 dark:hover:text-blue-400 transition-colors disabled:opacity-50 disabled:cursor-not-allowed" title="Re-upload document to Gemini">
                                        <svg v-if="!reuploadingDocs.has(doc.id)" class="size-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512"><path fill="currentColor" d="m346.231 284.746l-90.192-90.192l-90.192 90.192l22.627 22.627l51.565-51.565V496h32V255.808l51.565 51.565z"/><path fill="currentColor" d="M400 161.453V160c0-79.4-64.6-144-144-144S112 80.6 112 160v2.491A122.3 122.3 0 0 0 49.206 195.2A109.4 109.4 0 0 0 16 273.619c0 31.119 12.788 60.762 36.01 83.469C74.7 379.275 105.338 392 136.07 392H200v-32h-63.93C89.154 360 48 319.635 48 273.619c0-42.268 35.64-77.916 81.137-81.155L144 191.405V160a112 112 0 0 1 224 0v32.04l15.8.2c46.472.588 80.2 34.813 80.2 81.379C464 322.057 428.346 360 382.83 360H312v32h70.83a109.75 109.75 0 0 0 81.14-35.454c20.655-22.207 32.03-51.657 32.03-82.927c0-58.437-40.284-104.227-96-112.166"/></svg>
                                        <svg v-else class="size-5 animate-spin" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="M12 2A10 10 0 1 0 22 12A10 10 0 0 0 12 2Zm0 18a8 8 0 1 1 8-8A8 8 0 0 1 12 20Z" opacity=".5"/><path fill="currentColor" d="M20 12h2A10 10 0 0 0 12 2V4A8 8 0 0 1 20 12Z"/></svg>
                                    </button>
                                    <span v-if="doc.error" class="text-red-600" :title="doc.error">
                                        <svg class="size-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10s10-4.48 10-10S17.52 2 12 2m1 15h-2v-2h2zm0-4h-2V7h2z"/></svg>
                                    </span>
                                    <span v-else-if="doc.state === 'STATE_ACTIVE'" class="text-green-600" title="Active">
                                        <svg class="size-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" fill-rule="evenodd" d="M12 21a9 9 0 1 0 0-18a9 9 0 0 0 0 18m-.232-5.36l5-6l-1.536-1.28l-4.3 5.159l-2.225-2.226l-1.414 1.414l3 3l.774.774z" clip-rule="evenodd"/></svg>
                                    </span>                                
                                    <span v-else-if="doc.state && ['STATE_UNSPECIFIED','STATE_PENDING'].includes(doc.state)" class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-orange-100 text-orange-800 dark:bg-orange-900 dark:text-orange-200">{{ doc.state }}</span>
                                    <span v-else-if="doc.state && ['MISSING_METADATA','DUPLICATE_FILE','MISSING_FROM_REMOTE','METADATA_MISMATCH'].includes(doc.state)" class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200">{{ doc.state }}</span>
                                    <span v-else-if="doc.state" class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200">{{ doc.state }}</span>
                                    <span @click.prevent.stop="createNewChat(storeId, { document: doc })"
                                        class="cursor-pointer text-2xl text-gray-600" :title="'Ask Gemini RAG about ' + doc.displayName">
                                        <svg class="size-6" :class="[$styles.muted,$styles.mutedHover]" xmlns="http://www.w3.org/2000/svg" width="21" height="21" viewBox="0 0 21 21"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" d="M13.418 4.214A9.3 9.3 0 0 0 10.5 3.75c-4.418 0-8 3.026-8 6.759c0 1.457.546 2.807 1.475 3.91L3 19l3.916-2.447a9.2 9.2 0 0 0 3.584.714c4.418 0 8-3.026 8-6.758c0-.685-.12-1.346-.345-1.969M16.5 3.5v4m2-2h-4" stroke-width="1"/></svg>
                                    </span>
                                </div>
                           </div>
                       </li>
                       <li v-if="docs.length === 0 && !docsLoading && (searching || !childFolders.length)" class="px-4 py-10 text-center">
                           <p class="text-sm" :class="[$styles.muted]">
                               <span v-if="searching">Nothing matches.</span>
                               <span v-else-if="ext.prefs.category != null">This folder is empty.</span>
                               <span v-else-if="facetTotal">No documents on this page.</span>
                               <span v-else>Nothing imported yet.</span>
                           </p>
                           <button v-if="!facetTotal" type="button"
                               @click="importInto(ext.prefs.category)"
                               class="mt-3 px-4 py-1.5 rounded-md text-sm font-medium" :class="[$styles.primaryButton]">
                               {{ ext.prefs.category ? 'Import into ' + ext.prefs.category : 'Import documents' }}
                           </button>
                       </li>
                   </ul>

                </div>
            </div>

            <div v-show="view === 'import'">
                <div class="mb-6">
                    <ImportPanel ref="importPanel" :storeId="storeId" :facets="facets" :preset-category="importCategory"
                        :route-tab="routeQuery.import" :route-crawl="routeQuery.crawl" @navigate="onImportNavigate"
                        @previewing="onImportPreviewing" @preview="onImportPreview" @imported="onUploadImported" />
                </div>
                <div v-if="uploadProgress" class="mb-6 px-4 py-3 rounded-lg border flex flex-wrap items-center justify-between gap-3"
                    :class="[$styles.chromeBorder]">
                    <div class="flex items-center gap-2.5 min-w-0 text-sm">
                        <svg v-if="pending.uploading" class="size-5 shrink-0 animate-spin text-blue-600 dark:text-blue-400"
                            viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                            <circle cx="12" cy="12" r="9" opacity=".25"/><path d="M21 12a9 9 0 0 0-9-9"/>
                        </svg>
                        <svg v-else class="size-5 shrink-0 text-emerald-600 dark:text-emerald-400"
                            viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"
                            stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                            <circle cx="12" cy="12" r="9"/><path d="m8 12 2.5 2.5L16 9"/>
                        </svg>
                        <div class="font-medium">{{ uploadStatus }}</div>
                    </div>
                    <button type="button" @click="viewUploads"
                        class="px-3 py-1.5 rounded-md text-sm font-semibold border shrink-0"
                        :class="[$styles.chromeBorder]">View uploads</button>
                </div>
                <div v-if="importPreview" class="mb-6">
                    <RunReport :run="importPreview.run" @confirm="confirmImport" @dismiss="dismissImport" />
                </div>
                <div class="mb-8">
                    <SourcesPanel :key="sourceListVersion" :storeId="storeId" @imported="onImported" />
                    <TrustedFolders />
                </div>
            </div>

            <div v-show="view === 'assistants'" class="border-b border-gray-200 dark:border-gray-700 mb-8">
                <AssistantsPanel :storeId="storeId" :facets="facets"
                    :route-assistant="routeQuery.assistant" :route-conversations="routeQuery.conversations"
                    :route-conversation="routeQuery.conversation" @navigate="onAssistantNavigate"
                    @count="assistantCount = $event" />
            </div>

            <Modal :open="coverageOpen" title="Coverage & filters"
                subtitle="What metadata your documents carry, what you can filter on, and what Gemini currently holds."
                @close="coverageOpen = false">
                <div class="grid gap-6 sm:grid-cols-2">
                    <div>
                        <div class="text-xs font-semibold uppercase tracking-wide mb-2" :class="[$styles.muted]">Coverage</div>
                        <CoverageStrip v-if="facetTotal" :facets="facets" :total="facetTotal"
                            @pick="f => { showMissing(f, true); coverageOpen = false }" />
                        <p v-else class="text-sm" :class="[$styles.muted]">Nothing imported yet.</p>

                        <div class="mt-6 pt-4 border-t" :class="[$styles.chromeBorder]">
                            <div class="text-xs font-semibold uppercase tracking-wide mb-2" :class="[$styles.muted]">In Gemini</div>
                            <SyncState :pending="pending" :worker="pendingWorker" :busy="pushing"
                                @push="pushToGemini" @cancel="cancelPush"
                                @review="ids => { reviewPending(ids); coverageOpen = false }" />
                        </div>
                    </div>

                    <div>
                        <div class="text-xs font-semibold uppercase tracking-wide mb-2" :class="[$styles.muted]">Filter by</div>
                        <FacetPicker :facets="facets" :active="activeFacets" :field-names="facetFields"
                            @pick="(field, value) => { pickFacet(field, value, true); coverageOpen = false }"
                            @missing="f => { showMissing(f, true); coverageOpen = false }" />

                        <!-- A filter that silently matches nothing is the failure mode worth
                             catching, so show the expression a chat would actually send. -->
                        <div v-if="filterExpression" class="mt-6 pt-4 border-t" :class="[$styles.chromeBorder]">
                            <div class="text-xs font-semibold uppercase tracking-wide mb-1" :class="[$styles.muted]">Sent with a chat</div>
                            <p class="text-xs mb-1.5" :class="[$styles.muted]">
                                Filters here only narrow this page. Ask about this sends them to Gemini as:
                            </p>
                            <code class="block text-[11px] font-mono break-all px-2 py-1.5 rounded border"
                                :class="[$styles.chromeBorder, $styles.muted]">{{ filterExpression }}</code>
                        </div>
                    </div>
                </div>
            </Modal>

            <DeleteStoreDialog :open="deleteDialogOpen" :busy="deleting" :loading="deleteSummaryLoading"
                :store-name="store.displayName" :summary="deleteSummary" v-model="deleteConfirmation"
                @close="closeDeleteDialog" @confirm="deleteStore" />

            <SelectionBar :count="bulkCount" :all-matching="selectAllMatching"
                @edit="bulkEditOpen = true" @delete="openBulkDelete" @clear="clearSelection" />

            <BulkEditDialog v-if="bulkEditOpen" :selector="bulkSelector" :count="bulkCount" :facets="facets"
                @close="bulkEditOpen = false" @applied="onBulkApplied" />

            <!-- One document. Same dialog as the import defaults, minus the path rules: a document
                 that already exists has a path, so a rule that guesses one has nothing to do. -->
            <MetadataDialog v-if="editDoc" :model-value="editDocMetadata" :facets="facets"
                :fields="docFields" :list-fields="docListFields" :show-rules="false"
                title="Document metadata" :subtitle="editDoc.displayName" save-label="Save"
                note="Saved here; pushing to Gemini re-indexes it."
                @update:modelValue="saveDocument" @close="editDoc = null" />

            <ConfirmDialog :open="bulkDeleteOpen" :busy="bulkDeleting"
                :title="'Delete ' + bulkCount.toLocaleString() + ' document' + (bulkCount === 1 ? '' : 's') + '?'"
                :confirm-label="'Delete ' + bulkCount.toLocaleString()" busy-label="Deleting…"
                @confirm="confirmBulkDelete" @close="bulkDeleteOpen = false">
                <p>They are removed from Gemini as well as from here. This cannot be undone.</p>
                <ul v-if="deleteSample.length" class="mt-3 space-y-0.5 text-xs" :class="[$styles.muted]">
                    <li v-for="name in deleteSample" :key="name" class="truncate">{{ name }}</li>
                    <li v-if="bulkCount > deleteSample.length">and {{ (bulkCount - deleteSample.length).toLocaleString() }} more</li>
                </ul>
            </ConfirmDialog>
            <SyncReport :syncResult="syncResult" :syncing="syncing" :pruning="pruning"
                @sync="syncStore" @prune="pruneStore" />

            <div class="flex justify-between items-center dark:border-gray-700">
                <div>
                   <h3 class="text-lg font-medium text-gray-900 dark:text-white">
                       <span v-if="deleting">Deleting {{store.displayName}}...</span>
                       <span v-else>Delete {{store.displayName}}</span>
                   </h3>
                   <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">
                       <span v-if="deleting">Please wait, this may take a while.</span>
                       <span v-else>Permanently delete this File Store, documents, imports, assistants, and conversations.</span>
                   </p>
                </div>
                <button type="button"
                    @click="openDeleteDialog"
                    :disabled="deleting"
                    class="inline-flex items-center px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-red-600 hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                    <svg v-if="deleting" class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    <span v-if="deleting">Deleting...</span>
                    <span v-else>Delete Store</span>
                </button>
            </div>
        </div>
        <div v-else-if="loading" class="p-8 text-center text-gray-500">Loading store...</div>
        <div v-else class="p-8 text-center text-red-500">Store not found</div>
    `,
    emits: ['select', 'back'],
    setup(props, { emit }) {
        const route = ctx.router.currentRoute
        const routeQuery = computed(() => route.value.query || {})
        const queryValue = value => Array.isArray(value) ? value[0] : value
        const categoryFromRoute = () => {
            if (!Object.prototype.hasOwnProperty.call(routeQuery.value, 'category')) return null
            return queryValue(routeQuery.value.category) ?? ''
        }
        ext.setPrefs({ category: categoryFromRoute(), page: 1 })
        const store = computed(() => ext.state.filestores?.find(s => s.id == props.storeId))
        const loading = ref(false)
        const fileInput = ref(null)
        const uploading = ref(false)
        const dragover = ref(false)
        const categories = ref([])
        const docs = ref([])
        const docsLoading = ref(false)
        const deletingDocs = ref(new Set())
        const reuploadingDocs = ref(new Set())
        const syncing = ref(false)
        const pruning = ref(false)
        const syncResult = ref(null)
        const deleting = ref(false)
        const deleteDialogOpen = ref(false)
        const deleteSummaryLoading = ref(false)
        const deleteSummary = ref(null)
        const deleteConfirmation = ref('')
        const facets = ref({})
        const pendingIds = ref([])
        const PAGE_SIZE = 10
        const facetTotal = ref(0)
        const selected = ref(new Set())
        const selectAllMatching = ref(false)
        // Pending state used to live in the banner component. It now feeds an ambient count on
        // the Coverage button and the dialog's sync section, so it belongs here.
        const pending = ref({ count: 0 })
        const pendingWorker = ref({})
        const pushing = ref(false)
        const coverageOpen = ref(false)
        let pendingTimer = null

        async function refreshPending() {
            const api = await ext.getJson(`/documents/pending?filestoreId=${props.storeId}`)
            if (api.error) return
            pending.value = api.response || { count: 0 }
            pendingWorker.value = api.response?.worker || {}
            clearTimeout(pendingTimer)
            // Only poll while something is in flight; an idle store shouldn't be chatty.
            if (pendingWorker.value.running || pending.value.uploading) pendingTimer = setTimeout(refreshPending, 3000)
            else loadDocuments()   // the worker finished: pick up uploadedAt so spinners stop
        }

        async function pushToGemini() {
            pushing.value = true
            try {
                const api = await ext.postJson(`/filestores/${props.storeId}/reindex`, {})
                if (api.error) return ext.setError(api.error)
                await reload()
            } finally { pushing.value = false }
        }

        async function cancelPush() {
            await ext.postJson('/worker/cancel', {})
            refreshPending()
        }

        onBeforeUnmount(() => clearTimeout(pendingTimer))

        let pollTimer = null
        let lastRequestId = 0

        // --- facets ------------------------------------------------------------------
        // Derived from the documents, so the rail, the autocomplete and the coverage strip all
        // read the same numbers.
        async function loadFacetData() {
            const res = await loadFacets(props.storeId, FACET_FIELDS)
            if (!res) return
            facets.value = res.facets || {}
            facetTotal.value = res.total || 0
        }

        const activeFacets = computed(() => {
            const out = {}
            FACET_FIELDS.forEach(f => {
                const v = f === 'category' ? ext.prefs.category : ext.prefs['facet_' + f]
                if (v !== undefined && v !== null) out[f] = v
            })
            return out
        })
        const activeChips = computed(() => Object.entries(activeFacets.value))
        const quickFilterFields = ['docType', 'status', 'locale', 'product', 'versions', 'tags']
        const quickFilterLabels = {
            docType: 'doc type', status: 'status', locale: 'locale', product: 'product',
            versions: 'versions', tags: 'tags',
        }
        const quickFilterLabel = field => quickFilterLabels[field] || field
        const availableQuickFilters = computed(() => quickFilterFields.filter(field =>
            ext.prefs['facet_' + field] == null
            && ext.prefs.missing !== field
            && ((facets.value?.[field]?.values || []).length || facets.value?.[field]?.null)))

        const filterChips = computed(() => {
            const out = { ...activeFacets.value }
            delete out.category                       // a location, shown in the breadcrumb
            if (ext.prefs.missing) out[ext.prefs.missing] = ''   // FilterChips renders '' as (none)
            if (pendingIds.value.length) out['Sync status'] = 'differs from Gemini'
            return out
        })

        function removeFilter(field) {
            if (field === 'Sync status') {
                pendingIds.value = []
                ext.setPrefs({ page: 1 })
                loadDocuments()
                return
            }
            if (ext.prefs.missing === field) {
                ext.setPrefs({ page: 1, missing: null })
                loadDocuments()
                return
            }
            pickFacet(field, null)
        }

        // --- explorer ------------------------------------------------------------------
        // A search or a metadata filter spans folders, so the list stops being a directory
        // listing and becomes results. Everything that differs between the two hangs off this.
        const searching = computed(() => !!(ext.prefs.q || ext.prefs.missing || pendingIds.value.length
            || FACET_FIELDS.some(f => f !== 'category' && ext.prefs['facet_' + f] != null && ext.prefs['facet_' + f] !== '')))

        // Browsing the top level. Its documents are the ones in no folder at all - showing the
        // whole store here would contradict the folder rows sitting directly above them.
        const browsingRoot = computed(() => !searching.value && ext.prefs.category == null)

        const parentCategory = computed(() => {
            const cat = ext.prefs.category
            if (cat == null || cat === '') return null
            return cat.includes('/') ? cat.slice(0, cat.lastIndexOf('/')) : null
        })

        // The immediate children of wherever we are - what a folder listing shows.
        const childFolders = computed(() => {
            const cat = ext.prefs.category
            const roots = facets.value?.category?.tree || []
            if (cat == null) return roots.filter(n => n.path !== '')
            if (cat === '') return []          // (uncategorised) is a leaf by construction
            const find = nodes => (nodes || []).reduce((hit, n) =>
                hit || (n.path === cat ? n : find(n.children)), null)
            return find(roots)?.children || []
        })
        const folderCount = computed(() => childFolders.value.length)
        const folderTotal = computed(() => {
            let n = 0
            const walk = nodes => (nodes || []).forEach(x => { n++; walk(x.children) })
            walk(facets.value?.category?.tree)
            return n
        })

        function clearFilters() {
            pendingIds.value = []
            const patch = { page: 1, missing: null }
            FACET_FIELDS.filter(f => f !== 'category').forEach(f => { patch['facet_' + f] = null })
            ext.setPrefs(patch)
            loadDocuments()
        }

        function reviewPending(ids) {
            // Narrow the list to exactly the documents the banner is counting, so "which ones?"
            // has an answer that isn't "read the whole store".
            pendingIds.value = ids || []
            const patch = { page: 1, q: '', category: null, missing: null }
            FACET_FIELDS.filter(f => f !== 'category').forEach(f => { patch['facet_' + f] = null })
            ext.setPrefs(patch)
            updateExploreCategory(null)
            loadDocuments()
        }

        /**
         * The metadata_filter a chat over this selection would send.
         *
         * Shown because the grammar beyond `=` isn't documented, so this is how you tell a syntax
         * problem from an empty result - and it's what gets reused in an assistant's config.
         * Keys are snake_case: a camelCase key indexes fine and then never matches.
         */
        const filterExpression = computed(() => {
            const parts = []
            for (const [field, value] of Object.entries(activeFacets.value)) {
                if (value === null || value === undefined || value === '') continue
                const key = field.replace(/[A-Z]/g, c => '_' + c.toLowerCase())
                if (field === 'category') parts.push(`category_path:"${value}"`)
                else if (LIST_FIELDS.includes(field)) parts.push(`${key}:"${value}"`)
                else parts.push(`${key}="${value}"`)
            }
            return parts.join(' AND ')
        })
        const chatFilters = computed(() => Object.entries(activeFacets.value)
            .filter(([, value]) => value !== null && value !== undefined && value !== '')
            .map(([field, value]) => ({
                field,
                value,
                label: field === 'category' ? 'category' : quickFilterLabel(field),
            })))
        const askAboutTitle = computed(() => chatFilters.value.length
            ? 'Ask a question using:\n' + chatFilters.value.map(f => `${f.label} = ${f.value}`).join('\n')
            : 'Ask a question over every document in this store')

        function pickFacet(field, value, storeWide = false) {
            clearSelection()
            if (field === 'category') return selectCategory(value)
            ext.setPrefs({ page: 1, ...(storeWide ? { category: null } : {}), ['facet_' + field]: value })
            if (storeWide) updateExploreCategory(null)
            loadDocuments()
        }

        function showMissing(field, storeWide = false) {
            clearSelection()
            ext.setPrefs({ page: 1, ...(storeWide ? { category: null } : {}), missing: field })
            if (storeWide) updateExploreCategory(null)
            loadDocuments()
        }

        // --- selection ---------------------------------------------------------------
        // Tracked by id so it survives paging; losing a selection because you paged is the
        // fastest way to make someone give up on a backfill.
        function toggleDoc(id) {
            const next = new Set(selected.value)
            next.has(id) ? next.delete(id) : next.add(id)
            selected.value = next
            selectAllMatching.value = false
        }
        const allOnPageSelected = computed(() =>
            docs.value.length > 0 && docs.value.every(d => selected.value.has(d.id)))
        function togglePage() {
            selected.value = allOnPageSelected.value ? new Set() : new Set(docs.value.map(d => d.id))
            selectAllMatching.value = false
        }
        function clearSelection() {
            selected.value = new Set()
            selectAllMatching.value = false
        }

        /**
         * How many documents the current filter matches.
         *
         * Derived from the facet counts rather than a total on the documents response. Null when
         * the combination can't be counted exactly (two or more facets), which hides the
         * "select all matching" escalation instead of showing a number that might be wrong -
         * page selection and bulk apply still work.
         */
        const totalDocs = computed(() => matchCount.value)

        const bulkCount = computed(() => selectAllMatching.value ? (totalDocs.value || 0) : selected.value.size)
        // The filter form matters at scale: shipping 12,000 ids over the wire is not the plan,
        // and a filter is a stable description of intent that can be re-run.
        const bulkSelector = computed(() => selectAllMatching.value
            ? { filter: { filestoreId: Number(props.storeId), ...activeFacets.value } }
            : { ids: [...selected.value] })

        // --- editing -----------------------------------------------------------------
        const bulkEditOpen = ref(false)
        const bulkDeleteOpen = ref(false)
        const bulkDeleting = ref(false)
        const deleteSample = ref([])
        const editDoc = ref(null)

        async function onBulkApplied() {
            bulkEditOpen.value = false
            clearSelection()
            await reload()
        }

        function editDocument(doc) { editDoc.value = doc }

        const editDocMetadata = computed(() => {
            const doc = editDoc.value
            const defaults = {}
            for (const { key } of [...DOC_FIELDS, ...META_LIST_FIELDS]) {
                const v = doc?.[key]
                if (v !== null && v !== undefined && v !== '') defaults[key] = v
            }
            return { defaults }
        })

        /**
         * Save one document by diffing the form against what it already said.
         *
         * The dialog reports the values it holds rather than which ones you touched, so a field
         * that had a value and no longer does is how a clear arrives. Diffing also keeps the
         * untouched fields out of the request: `set` on all seven would mark the document pending
         * over edits nobody made, and pending costs a re-index.
         */
        async function saveDocument(meta) {
            const doc = editDoc.value
            if (!doc) return
            const defaults = meta?.defaults || {}
            const changes = []
            for (const { key } of [...DOC_FIELDS, ...META_LIST_FIELDS]) {
                const list = LIST_FIELDS.includes(key)
                const before = list ? (doc[key] || []) : (doc[key] ?? '')
                const after = list ? (defaults[key] || []) : (defaults[key] ?? '')
                if (list ? JSON.stringify(before) === JSON.stringify(after) : before === after) continue
                changes.push(list ? (after.length ? { field: key, op: 'set', value: after } : { field: key, op: 'clear' })
                    : (after ? { field: key, op: 'set', value: after } : { field: key, op: 'clear' }))
            }
            if (!changes.length) return
            const api = await ext.postJson('/documents/bulk', { ids: [doc.id], changes })
            if (api.error) return ext.setError(api.error)
            await reload()
        }

        async function openBulkDelete() {
            deleteSample.value = []
            bulkDeleteOpen.value = true
            // Named documents, from the same selector the delete will use. "Delete 412 documents"
            // is otherwise a number taken on trust - and a filter selection has no rows on screen
            // to check it against.
            const api = await ext.postJson('/documents/summary', { ...bulkSelector.value, fields: [] })
            if (!api.error) deleteSample.value = api.response?.sample || []
        }

        async function confirmBulkDelete() {
            bulkDeleting.value = true
            try {
                const api = await ext.postJson('/documents/delete', bulkSelector.value)
                if (api.error) return ext.setError(api.error)
                const failed = api.response?.errors || []
                bulkDeleteOpen.value = false
                clearSelection()
                await loadFilestores()
                await refresh()
                // Deleted what it could and named what it couldn't, rather than reporting success
                // for a batch that was partly refused.
                if (failed.length) ext.setError({
                    message:
                        `Deleted ${api.response.deleted} of ${api.response.selected}. ${failed[0].displayName}: ${failed[0].error}`
                })
            } finally { bulkDeleting.value = false }
        }

        function docMeta(doc) {
            return ['docType', 'status', 'locale', 'product', 'versions', 'tags']
                .filter(k => doc[k] != null && doc[k] !== '' && !(Array.isArray(doc[k]) && !doc[k].length))
                .flatMap(k => (Array.isArray(doc[k]) ? doc[k] : [doc[k]])
                    .map((v, i) => ({ k, v, id: `${k}:${i}:${v}` })))
        }

        async function reload() {
            await Promise.all([loadFacetData(), loadDocuments()])
            await refreshPending()
        }

        // --- import ------------------------------------------------------------------
        // A preview is always shown before anything is indexed, so the operator sees the cost
        // (and which rules matched) before committing.
        const importPreview = ref(null)
        const pageViews = ['explore', 'import', 'assistants']
        const importViews = ['upload', 'folder', 'crawl']
        const view = computed({
            get: () => {
                const explicit = queryValue(routeQuery.value.view)
                if (pageViews.includes(explicit)) return explicit
                if (routeQuery.value.assistant || routeQuery.value.conversations || routeQuery.value.conversation) return 'assistants'
                if (routeQuery.value.import || routeQuery.value.crawl) return 'import'
                return 'explore'
            },
            set: selectView,
        })
        function updateNavigation(patch) {
            const query = { ...routeQuery.value }
            for (const [key, value] of Object.entries(patch)) {
                if (value == null || (value === '' && key !== 'category')) delete query[key]
                else query[key] = String(value)
            }
            if (JSON.stringify(query) === JSON.stringify(routeQuery.value)) return
            ctx.router.push({ path: route.value.path, query, hash: route.value.hash })
        }
        function selectView(next) {
            if (!pageViews.includes(next)) next = 'explore'
            const patch = { view: next }
            if (next === 'import') {
                const currentImport = queryValue(routeQuery.value.import)
                patch.import = importViews.includes(currentImport)
                    ? currentImport
                    : importViews.includes(ext.prefs.importTab) ? ext.prefs.importTab : 'upload'
                if (patch.import !== 'crawl') patch.crawl = null
            } else {
                patch.import = null
                patch.crawl = null
            }
            if (next !== 'assistants') {
                patch.assistant = null
                patch.conversations = null
                patch.conversation = null
            }
            ext.setPrefs({ view: next })
            updateNavigation(patch)
        }
        function onImportNavigate(patch) {
            updateNavigation({ view: 'import', assistant: null, conversations: null, conversation: null, ...patch })
        }
        function onAssistantNavigate(patch) {
            updateNavigation({ view: 'assistants', import: null, crawl: null, ...patch })
        }
        function updateExploreCategory(category) {
            updateNavigation({
                view: 'explore', category, import: null, crawl: null,
                assistant: null, conversations: null, conversation: null
            })
        }
        const importCategory = ref(null)
        const sourceCount = ref(0)
        const assistantCount = ref(0)
        const sourceListVersion = ref(0)
        const importPanel = ref(null)
        const uploadProgress = ref(null)
        const uploadStatus = computed(() => {
            const total = Number(uploadProgress.value?.queued || 0)
            const remaining = Math.min(total, Number(pending.value.uploading || 0))
            const done = Math.max(0, total - remaining)
            const noun = total === 1 ? 'document' : 'documents'
            const destination = uploadProgress.value?.category || 'Gemini'
            return remaining
                ? `Uploading ${done.toLocaleString()}/${total.toLocaleString()} ${noun} to ${destination}…`
                : `Uploaded ${total.toLocaleString()}/${total.toLocaleString()} ${noun} to ${destination}.`
        })

        async function loadSourceCount() {
            const api = await ext.getJson(`/sources?filestoreId=${props.storeId}`)
            if (!api.error) sourceCount.value = (api.response || []).length
        }

        /**
         * Jump to Import with the category you were browsing already filled in.
         *
         * This is how a category gets created now - there's no "new category" box, because a
         * category with no documents in it means nothing once categories are derived from imports.
         */
        function importInto(category) {
            importCategory.value = category || null
            view.value = 'import'
        }

        function onImportPreviewing() { uploadProgress.value = null }

        function onImportPreview(payload) {
            uploadProgress.value = null
            importPreview.value = payload
        }

        async function onImported() {
            await Promise.all([reload(), loadSourceCount()])
            sourceListVersion.value++
        }

        async function onUploadImported(result) {
            uploadProgress.value = {
                queued: Number(result?.queued || 0),
                category: result?.category || null,
            }
            await onImported()
        }

        function viewUploads() {
            pendingIds.value = []
            const patch = {
                page: 1,
                q: '',
                missing: null,
                category: uploadProgress.value?.category ?? null,
                sortBy: 'uploading',
            }
            FACET_FIELDS.filter(f => f !== 'category').forEach(f => { patch['facet_' + f] = null })
            ext.setPrefs(patch)
            updateExploreCategory(patch.category)
            loadDocuments()
        }

        async function confirmImport() {
            const { source, keep } = importPreview.value || {}
            if (!source) return
            const api = await ext.postJson(`/sources/${source.id}/run`, { dryRun: false, saveConfig: !!keep })
            if (api.error) return ext.setError(api.error)
            // A one-off leaves no source behind; it existed only to carry the config through
            // the same pipeline a recurring import uses.
            if (!keep) await ext.deleteJson(`/sources/${source.id}?documents=keep`)
            uploadProgress.value = {
                queued: Number(api.response?.queued || 0),
                category: source.category?.prefix || null,
            }
            importPreview.value = null
            importPanel.value?.resetAfterImport()
            await onImported()
        }

        async function dismissImport() {
            const { source } = importPreview.value || {}
            // Always clean up, even when "Save as a recurring import" was ticked: a preview that
            // was never confirmed has never run, and a saved import that has never imported
            // anything is just a confusing "Not run yet" row nobody asked for. Ticking the box
            // states an intent; completing the import is what acts on it.
            if (source) await ext.deleteJson(`/sources/${source.id}?documents=keep`)
            importPreview.value = null
            await loadSourceCount()
        }

        const total = computed(() => {
            return {
                count: categories.value.reduce((sum, c) => sum + c.count, 0),
                size: categories.value.reduce((sum, c) => sum + c.size, 0),
            }
        })

        // Straight from the count of the query that produced the rows, so the pager can't claim
        // pages the filter doesn't have.
        const totalPages = computed(() => Math.ceil(matchCount.value / PAGE_SIZE))

        // One place that describes "what is on screen". The count endpoint is handed the same
        // object, which is what stops the pager and the list from disagreeing.
        function documentQuery() {
            const params = new URLSearchParams({ filestoreId: props.storeId })
            if (ext.prefs.q) params.append('q', ext.prefs.q)
            if (pendingIds.value.length) params.append('ids_in', pendingIds.value.join(','))
            // One comma-joined `null`: aiohttp's MultiDict hands the handler only the first value
            // for a repeated key, so two appends would silently drop one filter.
            const nulls = []
            if (ext.prefs.missing) nulls.push(ext.prefs.missing)
            if (browsingRoot.value || ext.prefs.category === '') {
                nulls.push('category')
            } else if (ext.prefs.category != null) {
                // Searching from a folder searches the folder *and below* - the exact match that
                // makes a directory listing correct makes a search useless.
                params.append(searching.value ? 'categoryUnder' : 'category', ext.prefs.category)
            }
            if (nulls.length) params.append('null', [...new Set(nulls)].join(','))
            // sql_filter() accepts any query parameter matching a column, so the other facets
            // needed no new server code.
            FACET_FIELDS.filter(f => f !== 'category').forEach(f => {
                const v = ext.prefs['facet_' + f]
                if (v != null && v !== '') params.append(f, v)
            })
            return params
        }

        const matchCount = ref(0)
        async function loadCount() {
            const api = await ext.getJson(`/documents/count?${documentQuery().toString()}`)
            if (!api.error) matchCount.value = api.response?.count ?? 0
        }

        async function loadDocuments() {
            const requestId = ++lastRequestId
            docsLoading.value = true
            loadCount()
            try {
                const params = documentQuery()
                params.append('take', PAGE_SIZE)
                params.append('skip', (ext.prefs.page - 1) * PAGE_SIZE)
                params.append('sort', ext.prefs.sortBy || '-uploadedAt')

                const api = await ext.getJson(`/documents?${params.toString()}`)
                if (requestId !== lastRequestId) return

                if (api.error) {
                    console.error("Failed to load docs", api.error)
                    return
                }
                api.response?.forEach(doc => {
                    const completed = doc.uploadedAt || doc.error
                    if (completed) {
                        ext.state.documentsCache[doc.id] = doc
                    }
                })
                docs.value = api.response

                // Check if we should start/stop polling after loading docs
                startPolling()
            } finally {
                if (requestId === lastRequestId) {
                    docsLoading.value = false
                }
            }
        }

        async function loadDocumentCategories() {
            const api = await ext.getJson(`/filestores/${props.storeId}/categories`)
            if (api.error) {
                ext.setError(api.error)
                return
            }
            categories.value = api.response || []
        }

        async function refresh() {
            await Promise.all([
                loadDocumentCategories(),
                loadFacetData(),
                loadDocuments(),
                loadSourceCount(),
            ])
        }

        function selectCategory(category) {
            pendingIds.value = []
            ext.setPrefs({
                page: 1,
                category,
                sortBy: ext.prefs.sortBy === 'uploading' ? '-uploadedAt' : ext.prefs.sortBy,
            })
            updateExploreCategory(category)
            loadDocuments()
        }

        watch(categoryFromRoute, category => {
            if (ext.prefs.category === category) return
            pendingIds.value = []
            ext.setPrefs({ page: 1, category })
            if (view.value === 'explore') loadDocuments()
        })

        watch(() => props.storeId, () => {
            ext.setPrefs({
                page: 1,
            })
            refresh()
        }, { immediate: true })

        watch(() => [ext.prefs.sortBy, ext.prefs.q], () => {
            ext.savePrefs()
            loadDocuments()
            startPolling()
        })

        function formatDate(date) {
            if (!date) return ''
            return new Date(date).toLocaleDateString() + ' ' + new Date(date).toLocaleTimeString()
        }

        async function handleFileUpload(e) {
            const files = e.target.files
            if (!files || files.length === 0) return
            await uploadFiles(files)
        }

        async function onDrop(e) {
            dragover.value = false
            const files = e.dataTransfer.files
            if (!files || files.length === 0) return
            await uploadFiles(files)
        }

        async function uploadFiles(files) {
            uploading.value = true
            try {
                const formData = new FormData()
                for (let i = 0; i < files.length; i++) {
                    formData.append('file', files[i])
                }

                let url = `/filestores/${store.value.id}/upload`
                const categoryToUse = ext.prefs.category

                if (categoryToUse != null && categoryToUse !== '') {
                    url += `?category=${encodeURIComponent(categoryToUse)}`
                }

                const res = await ext.postForm(url, { body: formData })
                const api = await ext.createJsonResult(res)
                if (api.error) {
                    ctx.setError(api.error)
                } else {
                    // Switch to "uploading" sort to show upload progress
                    ext.setPrefs({ sortBy: 'uploading' })

                    await loadFilestores()
                    loadDocuments() // Refresh the main list and start polling
                    refresh() // Refresh categories
                }
            } catch (e) {
                console.error("Upload failed", e)
                alert("Upload failed: " + (e.message || "Unknown error"))
            } finally {
                uploading.value = false
                if (fileInput.value) fileInput.value.value = ''
            }
        }

        async function pollDocuments() {
            try {
                await loadDocuments()
            } catch (e) {
                console.error("Polling documents failed", e)
            }
        }

        function startPolling() {
            // Clear existing timer
            if (pollTimer) {
                clearTimeout(pollTimer)
                pollTimer = null
            }

            // Always poll if we're in "uploading" sort mode
            if (ext.prefs.sortBy === 'uploading') {
                console.log('Starting polling in uploading mode')
                pollTimer = setTimeout(pollDocuments, 2000)
            }
        }

        onMounted(() => {
            ext.setPrefs({
                page: ext.prefs.page || 1,
                q: ext.prefs.q || '',
                sortBy: ext.prefs.sortBy || '-uploadedAt',
            })
            startPolling()
        })

        onUnmounted(() => {
            if (pollTimer) clearTimeout(pollTimer)
        })

        async function openDeleteDialog() {
            if (!store.value) return
            deleteDialogOpen.value = true
            deleteSummaryLoading.value = true
            deleteSummary.value = null
            deleteConfirmation.value = ''
            try {
                const api = await ext.getJson(`/filestores/${store.value.id}/delete-summary`)
                if (api.error) {
                    ext.setError(api.error)
                    deleteDialogOpen.value = false
                } else {
                    deleteSummary.value = api.response
                }
            } finally {
                deleteSummaryLoading.value = false
            }
        }

        function closeDeleteDialog() {
            if (deleting.value) return
            deleteDialogOpen.value = false
            deleteSummary.value = null
            deleteConfirmation.value = ''
        }

        async function deleteStore() {
            if (!store.value || deleteConfirmation.value !== store.value.displayName) return

            deleting.value = true
            try {
                const api = await ext.deleteJson("/filestores/" + store.value.id, {
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ confirm: deleteConfirmation.value }),
                })
                if (api.error) {
                    ext.setError(api.error)
                } else {
                    deleteDialogOpen.value = false
                    await loadFilestores()
                    emit('back')
                }
            } finally {
                deleting.value = false
            }
        }

        async function deleteDocument(doc) {
            if (deletingDocs.value.has(doc.id)) return
            if (!confirm(`Are you sure you want to delete "${doc.displayName}"? This cannot be undone.`)) return

            deletingDocs.value.add(doc.id)
            deletingDocs.value = new Set(deletingDocs.value)
            try {
                const api = await ext.deleteJson("/documents/" + doc.id)
                if (api.error) {
                    ext.setError(api.error)
                } else {
                    await loadFilestores()
                    await refresh()
                }
            } finally {
                deletingDocs.value.delete(doc.id)
                deletingDocs.value = new Set(deletingDocs.value)
            }
        }

        async function reuploadDocument(doc) {
            if (!confirm(`Re-upload "${doc.displayName}" to Gemini?`)) return

            reuploadingDocs.value.add(doc.id)
            // Trigger reactivity
            reuploadingDocs.value = new Set(reuploadingDocs.value)

            try {
                const api = await ext.postJson(`/documents/${doc.id}/upload`)
                if (api.error) {
                    ext.setError(api.error)
                } else {
                    if (api.response?.id) {
                        ext.state.documentsCache[api.response.id] = api.response
                    }
                    await loadFilestores()
                    await refresh()
                }
            } finally {
                reuploadingDocs.value.delete(doc.id)
                // Trigger reactivity
                reuploadingDocs.value = new Set(reuploadingDocs.value)
            }
        }

        /**
         * Drop the extra Gemini copies, then re-sync so the states settle.
         *
         * The re-sync is the point: DUPLICATE_FILE is written by sync and cleared by sync, so
         * without it every row keeps its red badge after the cause is gone.
         */
        async function pruneStore() {
            if (!store.value) return
            pruning.value = true
            try {
                const api = await ext.postJson(`/filestores/${store.value.id}/prune`, {})
                if (api.error) return ext.setError(api.error)
                const failed = api.response?.errors || []
                if (failed.length) ext.setError({
                    message:
                        `Removed ${api.response.removed}, but ${failed.length} could not be deleted: ${failed[0].error}`
                })
                await loadFilestores()
                await syncStore()
            } finally { pruning.value = false }
        }

        async function syncStore() {
            if (!store.value) return

            syncing.value = true
            syncResult.value = null

            try {
                const api = await ext.postJson(`/filestores/${store.value.id}/sync`)
                if (api.error) {
                    ext.setError(api.error)
                } else {
                    syncResult.value = api.response
                    await loadFilestores()
                    await refresh()
                }
            } finally {
                ext.setPrefs({
                    sortBy: 'issues'
                })
                syncing.value = false
            }
        }

        return {
            ext,
            total,
            totalPages,
            store,
            deleting,
            deleteDialogOpen,
            deleteSummaryLoading,
            deleteSummary,
            deleteConfirmation,
            openDeleteDialog,
            closeDeleteDialog,
            deleteStore,
            deleteDocument,
            reuploadDocument,
            reuploadingDocs,
            syncStore,
            syncing,
            pruneStore,
            pruning,
            syncResult,
            SyncReport,
            loading,
            fileInput,
            handleFileUpload,
            uploading,
            onDrop,
            dragover,
            docs,
            deletingDocs,
            loadDocuments,
            docsLoading,
            formatDate,
            categories,
            selectCategory,
            createNewChat,
            // metadata + ingest
            facets,
            facetTotal,
            activeFacets,
            activeChips,
            filterExpression, chatFilters, askAboutTitle,
            pickFacet,
            showMissing,
            selected,
            selectAllMatching,
            allOnPageSelected,
            toggleDoc,
            togglePage,
            clearSelection,
            bulkCount,
            bulkSelector,
            onBulkApplied,
            bulkEditOpen,
            bulkDeleteOpen,
            bulkDeleting,
            deleteSample,
            openBulkDelete,
            confirmBulkDelete,
            editDoc,
            editDocument,
            editDocMetadata,
            saveDocument,
            docFields: DOC_FIELDS,
            docListFields: META_LIST_FIELDS,
            totalDocs,
            pending, pendingWorker, pushing, coverageOpen, pushToGemini, cancelPush,
            filterChips, removeFilter, availableQuickFilters, quickFilterLabel,
            matchCount,
            searching, browsingRoot, parentCategory, childFolders, folderCount, folderTotal,
            clearFilters, reviewPending, facetFields: FACET_FIELDS,
            docMeta,
            reload,
            view, routeQuery, selectView, onImportNavigate, onAssistantNavigate,
            sourceCount, assistantCount, sourceListVersion, importPanel, uploadProgress, uploadStatus,
            importCategory,
            importInto,
            onImported, onUploadImported, viewUploads,
            importPreview,
            onImportPreviewing,
            onImportPreview,
            confirmImport,
            dismissImport,
        }
    }
}

const GeminiPage = {
    template: `
        <div data-tag="GeminiPage" class="h-full overflow-y-auto">
            <div class="m-2">
                <ErrorViewer />
            </div>
            <component :is="activeComponent" v-bind="componentProps" @select="onSelect" @back="onBack" />
        </div>
    `,
    setup() {
        const ctx = inject('ctx')
        const route = ctx.router.currentRoute

        const activeComponent = computed(() => {
            if (route.value.params.id) return FileStoreDetails
            return FileStoreList
        })

        const componentProps = computed(() => {
            if (route.value.params.id) return { storeId: route.value.params.id }
            return {}
        })

        function onSelect(storeId) {
            ctx.to('/gemini/filestores/' + storeId)
        }

        function onBack() {
            ctx.to('/gemini')
        }

        return { activeComponent, componentProps, onSelect, onBack }
    }
}

const GeminiHeader = {
    template: `
        <div data-tag="GeminiHeader" v-if="fileSearch" class="flex space-x-1 items-center cursor-pointer text-xs rounded transition-colors border" :class="[$styles.tagLabel]"
            :title="fileSearch.title"
            @click="fileSearch.url ? $ctx.to(fileSearch.url) : null" style="line-height: 20px;"
        >
            <svg class="ml-1 size-4" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><title>Gemini File Search</title><path fill="currentColor" d="m19.6 21l-6.3-6.3q-.75.6-1.725.95T9.5 16q-2.725 0-4.612-1.888T3 9.5t1.888-4.612T9.5 3t4.613 1.888T16 9.5q0 1.1-.35 2.075T14.7 13.3l6.3 6.3zM9.5 14q1.875 0 3.188-1.312T14 9.5t-1.312-3.187T9.5 5T6.313 6.313T5 9.5t1.313 3.188T9.5 14"/></svg>
            <span class="inline-block mr-1">{{fileSearch.description}}{{fileSearch.categoryPath}}<template v-if="fileSearch.otherFilterCount"> ({{ fileSearch.otherFilterCount }})</template></span>
            <span v-if="fileSearch.document" class="px-1 font-semibold inline-flex items-center gap-1" :class="[$styles.mutedIcon]" :title="'Search in document ' + fileSearch.document">
                <svg class="size-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
                    stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                    <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                    <polyline points="14 2 14 8 20 8"/>
                </svg>
                {{fileSearch.document}}
            </span>
            <span v-else-if="!fileSearch.category && !fileSearch.filters?.length" class="mr-1 inline-flex items-center" title="Search All Documents">
                <svg class="size-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
                    stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                    <path d="M16 6l4 14"/>
                    <path d="M12 6v14"/>
                    <path d="M8 8v12"/>
                    <path d="M4 4v16"/>
                </svg>
            </span>
        </div>
    `,
    props: {
        thread: Object
    },
    setup(props) {
        const fileSearch = computed(() => {
            const def = props.thread.tools?.find(t => t.type === 'file_search')
            const tool = def?.file_search
            if (!tool) return null
            const filestoreName = tool.file_search_store_names[0]
            const ret = {
                name: filestoreName || 'File Search',
                description: lastLeftPart(rightPart(filestoreName || '', '/'), '-') || '',
                filters: def.filters || [],
            }
            if (def.category) {
                ret.category = def.category
            }
            if (def.document) {
                ret.document = def.document
            }
            const categoryFilter = ret.filters.find(f => f.field === 'category')
            if (categoryFilter) {
                ret.category = categoryFilter.value
            }
            ret.categoryPath = ret.category
                ? `/${String(ret.category).replace(/^\/+|\/+$/g, '')}`
                : ''
            ret.otherFilterCount = ret.filters.filter(f => f.field !== 'category').length
            const filestore = ext.state.filestores?.find(f => f.name === filestoreName)
            if (filestore) {
                ret.description = filestore.displayName
                ret.url = `/gemini/filestores/${filestore.id}`
            }
            ret.title = ret.filters.length
                ? ret.description + '\n' + ret.filters.map(f => `${f.label || f.field} = ${f.value}`).join('\n')
                : ret.description || 'Gemini File Search'
            if (!ret.category && tool.metadata_filter) {
                const field = leftPart(tool.metadata_filter, '=')
                const value = rightPart(tool.metadata_filter, '=')
                if (field === 'category' && value) {
                    ret.category = value
                    ret.categoryPath = `/${String(value).replace(/^\/+|\/+$/g, '')}`
                }
            }
            return ret
        })
        return {
            fileSearch
        }
    }
}

/**
 * Resolves a grounding chunk to the source a reader should be sent to.
 *
 * Preference order is deliberate: the document's own `sourceUrl` (the customer's live page)
 * beats the URI the provider echoed back, which beats a download of the cached upload.
 * Nobody wants a support answer citing `/~cache/ab/ab12…md`.
 */
function resolveSource(chunk) {
    const rc = chunk?.retrievedContext || chunk?.web || {}
    const title = rc.title || rc.documentName || 'Source'
    const doc = Object.values(ext.state.documentsCache)
        .find(d => d.displayName === rc.title)
    const url = doc?.sourceUrl || rc.uri || (doc ? doc.url + '?download' : null)
    return { title, url, doc, text: rc.text }
}

function escapeAttr(s) {
    return String(s ?? '').replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
}

/**
 * Rewrites an answer to carry inline [n] citation markers.
 *
 * `groundingSupports` give the byte range of each claim in the UTF-8 answer plus the sources
 * backing it, so the markers land on the sentence they support instead of all being dumped at
 * the end. The stored message is never modified - this runs at render time, so copying the
 * message still yields exactly what the model wrote.
 */
function withInlineCitations(content, { message }) {
    const supports = message?.groundingMetadata?.groundingSupports
    const chunks = message?.groundingMetadata?.groundingChunks
    if (!content || typeof content !== 'string' || !supports?.length || !chunks?.length) return content

    const bytes = new TextEncoder().encode(content)
    const decoder = new TextDecoder()

    // Several supports can end at the same place; group them so a claim backed by three
    // sources renders [1][2][3] once rather than three separate superscripts.
    const byOffset = new Map()
    for (const support of supports) {
        const end = support.segment?.endIndex
        if (end == null || end < 0 || end > bytes.length) continue
        // A split inside a multi-byte character would decode to a replacement char, so an
        // offset that isn't on a character boundary is skipped rather than corrupting the text.
        if (end < bytes.length && (bytes[end] & 0xC0) === 0x80) continue
        const nums = (support.groundingChunkIndices || [])
            .filter(i => i >= 0 && i < chunks.length)
            .map(i => i + 1)
        if (!nums.length) continue
        byOffset.set(end, (byOffset.get(end) || []).concat(nums))
    }
    if (!byOffset.size) return content

    const sources = chunks.map(resolveSource)
    let out = ''
    let prev = 0
    for (const offset of [...byOffset.keys()].sort((a, b) => a - b)) {
        out += decoder.decode(bytes.slice(prev, offset))
        const nums = [...new Set(byOffset.get(offset))].sort((a, b) => a - b)
        out += '<sup class="gemini-citation whitespace-nowrap">' + nums.map(n => {
            const src = sources[n - 1]
            const label = escapeAttr(src?.title ? `${n}. ${src.title}` : `Source ${n}`)
            return src?.url
                ? `<a href="${escapeAttr(src.url)}" target="_blank" rel="noopener noreferrer" title="${label}" class="px-px text-blue-600 dark:text-blue-400 no-underline hover:underline">[${n}]</a>`
                : `<span title="${label}" class="px-px text-gray-500 dark:text-gray-400">[${n}]</span>`
        }).join('') + '</sup>'
        prev = offset
    }
    out += decoder.decode(bytes.slice(prev))
    return out
}

const GeminiMessageFooter = {
    template: `
        <div data-tag="GeminiMessageFooter" v-if="groundingChunks.length" class="mt-4 border-t pt-4 space-y-4" :class="[$styles.chromeBorder]">
            <div class="space-y-2">
                <div class="flex items-center gap-2 text-sm font-medium text-gray-700 dark:text-gray-300">
                    <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                        <polyline points="14 2 14 8 20 8"/>
                    </svg>
                    <span>Sources ({{ groundingChunks.length }})</span>
                </div>
                <div class="grid grid-cols-1 gap-2">
                    <div v-for="(source, idx) in sources" :key="idx"
                        class="group relative bg-gray-50 dark:bg-gray-800 rounded-lg p-3 border border-gray-200 dark:border-gray-700 hover:border-blue-400 dark:hover:border-blue-600 transition-colors">
                        <div class="flex items-start justify-between gap-2"
                            @click="source.text && toggleChunk(idx)"
                            :class="{'cursor-pointer': source.text}">
                            <span class="shrink-0 mt-0.5 text-xs font-semibold tabular-nums" :class="[$styles.muted]">{{ idx + 1 }}.</span>
                            <div class="flex-1 min-w-0">
                                <a v-if="source.url"
                                    @click.stop
                                    :href="source.url"
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    class="text-sm font-medium text-blue-600 dark:text-blue-400 hover:text-blue-700 dark:hover:text-blue-300 truncate"
                                    :title="source.doc?.sourceUrl || source.title">
                                    {{ source.title }}
                                </a>
                                <div v-else class="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">
                                    {{ source.title }}
                                </div>
                                <div v-if="source.doc?.sourceUrl" class="text-xs truncate" :class="[$styles.muted]">
                                    {{ source.doc.sourceUrl }}
                                </div>
                                <div v-if="source.text" class="mt-1 text-xs text-gray-600 dark:text-gray-400 line-clamp-2">
                                    {{ source.text.substring(0, 150) }}{{ source.text.length > 150 ? '...' : '' }}
                                </div>
                            </div>
                            <div
                                v-if="source.text"
                                class="shrink-0 p-1 text-gray-400 transition-colors pointer-events-none"
                                :title="expandedChunks.has(idx) ? 'Show less' : 'Show more'">
                                <svg class="w-4 h-4 transition-transform" :class="{'rotate-180': expandedChunks.has(idx)}" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <polyline points="6 9 12 15 18 9"/>
                                </svg>
                            </div>
                        </div>
                        <div v-if="expandedChunks.has(idx) && source.text" class="mt-2 pt-2 border-t border-gray-200 dark:border-gray-700">
                            <div class="prose prose-sm max-w-none dark:prose-invert whitespace-wrap" style="font-size:13px" v-html="$fmt.markdown(source.text)"></div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `,
    props: {
        thread: Object,
        message: Object,
    },
    setup(props) {
        const expandedChunks = ref(new Set())

        const groundingChunks = computed(() => props.message?.groundingMetadata?.groundingChunks || [])
        const sources = computed(() => groundingChunks.value.map(resolveSource))

        function toggleChunk(idx) {
            if (expandedChunks.value.has(idx)) {
                expandedChunks.value.delete(idx)
            } else {
                expandedChunks.value.add(idx)
            }
            // Trigger reactivity
            expandedChunks.value = new Set(expandedChunks.value)
        }

        function loadDocumentChunks(chunks) {
            // Resolve each cited title to its local document, which is what carries sourceUrl
            const filestoreNames = chunks.map(c => c.retrievedContext?.fileSearchStore).filter(Boolean)
            new Set(filestoreNames).forEach(name => {
                const filestore = ext.state.filestores.find(fs => fs.name === name)
                if (!filestore) return
                const displayNames = new Set(chunks
                    .filter(c => c.retrievedContext?.fileSearchStore === name)
                    .map(c => c.retrievedContext?.title)
                    .filter(Boolean))
                if (displayNames.size > 0) {
                    loadDocumentsWithDisplayNames(filestore.id, [...displayNames])
                }
            })
        }

        onMounted(() => loadDocumentChunks(groundingChunks.value))
        watch(groundingChunks, chunks => loadDocumentChunks(chunks))

        return {
            ext,
            expandedChunks,
            groundingChunks,
            sources,
            toggleChunk,
        }
    }
}

export default {
    order: -70,

    install(context) {
        ext = context.scope('gemini')
        ctx = context
        initMetadata(ext, ctx)
        initSources(ext)
        initImport(ext)
        initAssistants(ext, ctx, { GeminiModelSelector })
        initExplorer(ext)

        ctx.setLeftIcons({
            gemini: {
                component: {
                    template: `<svg @click="$ctx.togglePath('/gemini', { left:false })" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-linejoin="round" stroke-width="1.5" d="M3 12a9 9 0 0 0 9-9a9 9 0 0 0 9 9a9 9 0 0 0-9 9a9 9 0 0 0-9-9Z"/></svg>`
                },
                isActive({ path }) {
                    return ctx.matchesPath(path, '/gemini*')
                }
            }
        })

        // Define routes with /gemini prefix to match ext.to() behavior
        ctx.routes.push(
            { path: '/gemini', component: GeminiPage, meta: { title: 'Gemini' } },
            { path: '/gemini/filestores/:id', component: GeminiPage, meta: { title: 'File Store' } }
        )

        ctx.setThreadHeaders({
            gemini: {
                component: GeminiHeader,
                show({ thread }) {
                    return (thread.tools || []).filter(x => x.type === 'file_search').length
                }
            }
        })

        // Sources hang off the message that cited them, not the thread: a thread-level footer
        // can only ever show the most recent answer's sources, so scrolling back through a
        // conversation attributed every earlier answer to the newest one's documents.
        ctx.setMessageFooters({
            gemini: {
                component: GeminiMessageFooter,
                show({ message }) {
                    return message?.groundingMetadata?.groundingChunks?.length > 0
                }
            }
        })

        ctx.addMessageContentFilter(withInlineCitations)

        ext.setState({
            filestores: [],
            documentsCache: {},
        })
    },

    async load(ctx) {
        await loadFilestores()
    }
}
