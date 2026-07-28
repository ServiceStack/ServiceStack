import { ref, computed, inject, onMounted, onUnmounted, toRef, watch } from 'vue'
import { appendQueryString, lastLeftPart, leftPart, rightPart } from '@servicestack/client'

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

function getGeminiModel() {
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
            .find(x => x.id === modelId && x.provider === 'google')
        if (model) return model
    }
    for (const modelId of geminiModels) {
        const model = ctx.state.models.find(x => x.id === modelId)
        if (model) return model
    }
    return null
}

function createNewChat(filestoreId, { category, document } = {}) {
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
    if (category != null) {
        tool.file_search.metadata_filter = `category=${category || ''}`
        tool.category = category
    } else if (document != null) {
        tool.file_search.metadata_filter = `hash=${document.hash}`
        tool.document = document.displayName
    }
    const tools = [tool]

    const title = `Ask ${filestore.displayName}` + (category ? ` about ${category}` : document ? ` about ${document.displayName}` : '')
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
        <div v-if="issue?.count > 0" class="p-3 bg-gray-50 dark:bg-gray-800 rounded border border-gray-200 dark:border-gray-700">
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
    props: ['syncResult', 'syncing'],
    emits: ['sync'],
    template: `
        <div class="mb-8">
            <div class="flex justify-between items-start mb-4">
                <div>
                   <h3 class="text-lg font-medium text-gray-900 dark:text-white">Sync Store</h3>
                   <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">Synchronize local and remote documents to detect any issues.</p>
                </div>
                <button type="button"
                    @click="$emit('sync')"
                    :disabled="syncing"
                    class="inline-flex items-center px-4 py-2 border border-transparent rounded-full shadow-sm text-sm font-medium" :class="[$styles.primaryButton]"
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

        return {
            hasIssues
        }
    }
}

const FileStoreList = {
    template: `
        <div class="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
            <div class="flex justify-between items-center mb-8">
                <div>
                   <h1 class="text-2xl font-bold" :class="[$styles.heading]">File Stores</h1>
                   <p class="text-sm" :class="[$styles.muted]">Manage your file stores for Gemini search grounding</p>
                </div>
                <button type="button" @click="showCreate = true" class="inline-flex items-center px-4 py-2 border border-transparent rounded-full shadow-sm text-sm font-medium" :class="[$styles.primaryButton]">
                    New Store
                </button>
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
                                        <div class="ml-2 flex-shrink-0 flex">
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
                                    class="ml-2 cursor-pointer flex-shrink-0" :title="'Ask Gemini RAG about ' + store.displayName">
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

const FileStoreDetails = {
    components: { SyncReport },
    props: ['storeId'],

    template: `
        <div class="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-8" v-if="store">
            <div class="flex justify-between items-center mb-8">
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
                 <div class="flex items-center gap-2">
                     <input type="file" ref="fileInput" class="hidden" multiple @change="handleFileUpload">
                     <button type="button" 
                        @click="createNewChat(storeId)"
                        :disabled="uploading"
                        class="inline-flex items-center px-4 py-2 border border-transparent rounded-full shadow-sm text-sm font-medium" :class="[$styles.primaryButton]">
                        <span v-if="uploading">Uploading...</span>
                        <span v-else>New Chat</span>
                     </button>
                 </div>
            </div>

            <div class="mb-8">
                <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 2xl:grid-cols-4 gap-3">
                    <button
                        @click="selectCategory(null)"
                        type="button"
                        class="bg-white dark:bg-gray-800 shadow rounded-lg px-4 py-3 flex items-start hover:bg-gray-50 dark:hover:bg-gray-700 transition border-2"
                        :class="ext.prefs.category === null ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20' : 'border-transparent'"
                    >
                        <span class="text-2xl mr-3">📚</span>
                        <div class="min-w-0 flex-1 text-left">
                            <p class="text-sm font-medium text-gray-900 dark:text-white truncate"
                               :class="{'text-blue-600 dark:text-blue-400': ext.prefs.category === null}">
                                All Documents
                            </p>
                            <div class="mt-1 text-xs text-gray-500 dark:text-gray-400">
                                <div>{{ total.count }} document{{ total.count !== 1 ? 's' : '' }}</div>
                                <div>{{ $fmt.bytes(total.size) }}</div>
                            </div>
                        </div>
                        <span @click.prevent.stop="createNewChat(storeId)" 
                            class="cursor-pointer text-2xl text-gray-600" title="Ask Gemini RAG about All Documents"
                            >
                            <svg class="size-7" :class="[$styles.muted]" xmlns="http://www.w3.org/2000/svg" width="21" height="21" viewBox="0 0 21 21"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" d="M13.418 4.214A9.3 9.3 0 0 0 10.5 3.75c-4.418 0-8 3.026-8 6.759c0 1.457.546 2.807 1.475 3.91L3 19l3.916-2.447a9.2 9.2 0 0 0 3.584.714c4.418 0 8-3.026 8-6.758c0-.685-.12-1.346-.345-1.969M16.5 3.5v4m2-2h-4" stroke-width="1"/></svg>
                        </span>
                    </button>

                    <button
                        v-for="cat in categories"
                        :key="cat.category"
                        @click="selectCategory(cat.category)"
                        type="button"
                        class="bg-white dark:bg-gray-800 shadow rounded-lg px-4 py-3 flex items-start hover:bg-gray-50 dark:hover:bg-gray-700 transition border-2"
                        :class="ext.prefs.category === cat.category ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20' : 'border-transparent'"
                    >
                        <span class="text-2xl mr-3">{{ cat.category ? '📁' : '📄' }}</span>
                        <div class="min-w-0 flex-1 text-left">
                            <p class="text-sm font-medium text-gray-900 dark:text-white truncate"
                               :class="{'text-blue-600 dark:text-blue-400': ext.prefs.category === cat.category}">
                                {{ cat.category || 'Uncategorized' }}
                            </p>
                            <div class="mt-1 text-xs text-gray-500 dark:text-gray-400">
                                <div>{{ cat.count }} document{{ cat.count !== 1 ? 's' : '' }}</div>
                                <div>{{ $fmt.bytes(cat.size) }}</div>
                            </div>
                        </div>
                        <span @click.prevent.stop="createNewChat(storeId, { category: cat.category })" 
                            class="cursor-pointer text-2xl text-gray-600" :title="'Ask Gemini RAG about ' + (cat.category ? cat.category : 'Uncategorized') + ' documents'"
                            >
                            <svg class="size-7" :class="[$styles.muted]" xmlns="http://www.w3.org/2000/svg" width="21" height="21" viewBox="0 0 21 21"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" d="M13.418 4.214A9.3 9.3 0 0 0 10.5 3.75c-4.418 0-8 3.026-8 6.759c0 1.457.546 2.807 1.475 3.91L3 19l3.916-2.447a9.2 9.2 0 0 0 3.584.714c4.418 0 8-3.026 8-6.758c0-.685-.12-1.346-.345-1.969M16.5 3.5v4m2-2h-4" stroke-width="1"/></svg>
                        </span>
                    </button>
                </div>
            </div>

            <div class="mb-4 flex justify-between items-center gap-4">
                <h3 class="text-lg font-medium text-gray-900 dark:text-white flex items-center space-x-1">
                    <span>Documents</span>
                    <span v-if="ext.prefs.category != null" class="text-base font-normal text-gray-500 dark:text-gray-400">
                        in {{ ext.prefs.category === '' ? 'Uncategorized' : ext.prefs.category }}
                    </span>
                </h3>
                <div class="flex items-center gap-2 flex-shrink-0">
                    <div :class="[$styles.muted]">
                        <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24"><path fill="currentColor" d="M13 19c0 .34.04.67.09 1H4a2 2 0 0 1-2-2V6c0-1.11.89-2 2-2h6l2 2h8a2 2 0 0 1 2 2v5.81c-.88-.51-1.9-.81-3-.81c-3.31 0-6 2.69-6 6m7-1v-3h-2v3h-3v2h3v3h2v-3h3v-2z"/></svg>
                    </div>
                    <input
                        type="text"
                        v-model="newCategoryName"
                        @keyup.enter="createNewCategory"
                        placeholder="New category"
                        class="w-48 rounded-md shadow-sm sm:text-sm px-3 py-1.5"
                        :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]"
                    >
                </div>
            </div>

            <div
                @drop.prevent="onDrop"
                @dragover.prevent="dragover = true"
                @dragleave.prevent="dragover = false"
                @click="fileInput.click()"
                :class="{'border-blue-500 bg-blue-50 dark:bg-blue-900/20': dragover}"
                class="group relative transition-colors duration-200 text-center py-12 bg-gray-50 dark:bg-gray-800/50 rounded-lg border-2 border-dashed border-gray-300 dark:border-gray-700 mb-4 cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-800">
                <div class="mx-auto h-12 w-12 text-gray-400 text-5xl mb-4">📄</div>
                 <div v-if="(ext.prefs.category != null && ext.prefs.category !== '') || newCategoryName" class="mb-3 flex items-center justify-center gap-1">
                    📁
                    <span class="font-medium" :class="[$styles.heading]">{{ newCategoryName || ext.prefs.category }}</span>
                 </div>
                 <h3 class="mt-2 text-sm font-medium text-gray-900 dark:text-white">
                    <span :class="[$styles.linkHover]">Upload a file</span> or drag and drop
                 </h3>
                 <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">Upload PDFs, Text files or Markdown to get started.</p>
            </div>

            <div class="bg-white dark:bg-gray-800 shadow overflow-hidden sm:rounded-md mb-8">
               <div class="px-4 py-4 border-b border-gray-200 dark:border-gray-700 flex justify-between items-center bg-gray-50 dark:bg-gray-900/50">
                   <div class="flex items-center gap-3">
                       <div class="relative max-w-xs w-full">
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
                       <select v-model="ext.prefs.sortBy" class="block rounded-md border-gray-300 dark:border-gray-600 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm dark:bg-gray-700 dark:text-white py-1.5 pl-3 pr-8">
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
               <ul class="divide-y divide-gray-200 dark:divide-gray-700">
                   <li v-for="doc in docs" :key="doc.id">
                       <div class="px-4 py-4 sm:px-6 flex items-center justify-between">
                            <div class="flex items-center min-w-0 flex-1">
                                <div class="text-sm min-w-0 flex-1 mr-4">
                                   <div class="flex items-center gap-x-1">
                                       <span v-if="doc.category" class="cursor-pointer inline-flex items-center rounded font-medium text-gray-800 dark:text-gray-200" @click="selectCategory(doc.category)">
                                           📂 {{ doc.category }}
                                       </span>
                                       <span v-if="doc.category">/</span>
                                       <a :href="doc.url + '?download'" class="font-medium text-blue-600 dark:text-blue-400 hover:text-blue-700 dark:hover:text-blue-300 truncate" :title="'Download ' + doc.displayName">{{ doc.displayName }}</a>
                                   </div>
                                   <div class="flex items-center mt-1">
                                       <span class="flex-shrink-0 text-gray-500 dark:text-gray-400">
                                           {{ $fmt.bytes(doc.size) }} &middot; {{ $fmt.date(doc.uploadedAt || doc.createdAt) }}
                                       </span>
                                   </div>
                                </div>
                            </div>
                            <div class="flex-shrink-0 flex items-center gap-2">
                                <button type="button" @click.stop="deleteDocument(doc)" class="ml-2 p-1 text-gray-400 hover:text-red-600 dark:hover:text-red-400 transition-colors" title="Delete document">
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
                   <li v-if="docs.length === 0 && !docsLoading" class="px-4 py-8 text-center text-gray-500 dark:text-gray-400">
                       No documents found.
                   </li>
               </ul>

            </div>

            <SyncReport :syncResult="syncResult" :syncing="syncing" @sync="syncStore" />

            <div class="flex justify-between items-center dark:border-gray-700">
                <div>
                   <h3 class="text-lg font-medium text-gray-900 dark:text-white">
                       <span v-if="deleting">Deleting {{store.displayName}}...</span>
                       <span v-else>Delete {{store.displayName}}</span>
                   </h3>
                   <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">
                       <span v-if="deleting">Please wait, this may take a while.</span>
                       <span v-else>Permanently delete this file store and all its documents.</span>
                   </p>
                </div>
                <button type="button"
                    @click="deleteStore"
                    :disabled="deleting"
                    class="inline-flex items-center px-4 py-2 border border-transparent rounded-full shadow-sm text-sm font-medium text-white bg-red-600 hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500 disabled:opacity-50 disabled:cursor-not-allowed"
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
        const store = computed(() => ext.state.filestores?.find(s => s.id == props.storeId))
        const loading = ref(false)
        const fileInput = ref(null)
        const uploading = ref(false)
        const dragover = ref(false)
        const categories = ref([])
        const docs = ref([])
        const docsLoading = ref(false)
        const newCategoryName = ref('')
        const reuploadingDocs = ref(new Set())
        const syncing = ref(false)
        const syncResult = ref(null)
        const deleting = ref(false)
        let pollTimer = null
        let lastRequestId = 0

        const total = computed(() => {
            return {
                count: categories.value.reduce((sum, c) => sum + c.count, 0),
                size: categories.value.reduce((sum, c) => sum + c.size, 0),
            }
        })

        const currentCategoryCount = computed(() => {
            if (ext.prefs.category === null) {
                return total.value.count
            }
            const cat = categories.value.find(c => c.category === ext.prefs.category)
            return cat ? cat.count : 0
        })

        const totalPages = computed(() => {
            return Math.ceil(currentCategoryCount.value / 10)
        })

        async function loadDocuments() {
            const requestId = ++lastRequestId
            docsLoading.value = true
            try {
                const params = new URLSearchParams({
                    filestoreId: props.storeId,
                    take: 10,
                    skip: (ext.prefs.page - 1) * 10,
                    sort: ext.prefs.sortBy || '-uploadedAt',
                })
                if (ext.prefs.q) params.append('q', ext.prefs.q)
                if (ext.prefs.category != null) {
                    if (ext.prefs.category === '') {
                        params.append('null', 'category')
                    } else {
                        params.append('category', ext.prefs.category)
                    }
                }

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
                loadDocuments(),
            ])
        }

        function selectCategory(category) {
            ext.setPrefs({
                page: 1,
                category,
                sortBy: ext.prefs.sortBy === 'uploading' ? '-uploadedAt' : ext.prefs.sortBy,
            })
            loadDocuments()
        }

        function createNewCategory() {
            if (!newCategoryName.value.trim()) return

            const categoryName = newCategoryName.value.trim()
            newCategoryName.value = ''

            // Select the newly created category
            selectCategory(categoryName)
        }

        watch(() => props.storeId, () => {
            newCategoryName.value = ''
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
                // Use newCategoryName if being typed, otherwise use ext.prefs.category
                const categoryToUse = newCategoryName.value.trim() || ext.prefs.category

                if (categoryToUse != null && categoryToUse !== '') {
                    url += `?category=${encodeURIComponent(categoryToUse)}`
                }

                const res = await ext.postForm(url, { body: formData })
                const api = await ext.createJsonResult(res)
                if (api.error) {
                    ctx.setError(api.error)
                } else {
                    // If a new category was created via upload, clear the input and select it
                    if (newCategoryName.value.trim()) {
                        newCategoryName.value = ''
                    }

                    if (categoryToUse != ext.prefs.category) {
                        selectCategory(categoryToUse)
                    }

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

        async function deleteStore() {
            if (!store.value) return
            if (!confirm(`Are you sure you want to delete "${store.value.displayName}"? This cannot be undone.`)) return

            deleting.value = true
            try {
                const api = await ext.deleteJson("/filestores/" + store.value.id)
                if (api.error) {
                    ext.setError(api.error)
                } else {
                    await loadFilestores()
                    emit('back')
                }
            } finally {
                deleting.value = false
            }
        }

        async function deleteDocument(doc) {
            if (!confirm(`Are you sure you want to delete "${doc.displayName}"? This cannot be undone.`)) return

            const api = await ext.deleteJson("/documents/" + doc.id)
            if (api.error) {
                ext.setError(api.error)
            } else {
                await loadFilestores()
                await refresh()
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
            currentCategoryCount,
            totalPages,
            store,
            deleting,
            deleteStore,
            deleteDocument,
            reuploadDocument,
            reuploadingDocs,
            syncStore,
            syncing,
            syncResult,
            SyncReport,
            loading,
            fileInput,
            handleFileUpload,
            uploading,
            onDrop,
            dragover,
            docs,
            loadDocuments,
            docsLoading,
            formatDate,
            categories,
            selectCategory,
            newCategoryName,
            createNewCategory,
            createNewChat,
        }
    }
}

const GeminiPage = {
    template: `
        <div class="h-full overflow-y-auto">
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
        <div v-if="fileSearch" class="flex space-x-1 items-center cursor-pointer text-xs rounded transition-colors border" :class="[$styles.tagLabel]"
            :title="fileSearch.description ? fileSearch.description : 'Gemini File Search'"
            @click="fileSearch.url ? $ctx.to(fileSearch.url) : null" style="line-height: 20px;"
        >
            <svg class="ml-1 size-4" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><title>Gemini File Search</title><path fill="currentColor" d="m19.6 21l-6.3-6.3q-.75.6-1.725.95T9.5 16q-2.725 0-4.612-1.888T3 9.5t1.888-4.612T9.5 3t4.613 1.888T16 9.5q0 1.1-.35 2.075T14.7 13.3l6.3 6.3zM9.5 14q1.875 0 3.188-1.312T14 9.5t-1.312-3.187T9.5 5T6.313 6.313T5 9.5t1.313 3.188T9.5 14"/></svg>
            <span class="inline-block mr-1">{{fileSearch.description}}</span>
            <span v-if="fileSearch.category" class="px-1 font-semibold" :class="[$styles.mutedIcon]" :title="'Search in category ' + fileSearch.category">
                📂{{fileSearch.category}}
            </span>
            <span v-else-if="fileSearch.document" class="px-1 font-semibold" :class="[$styles.mutedIcon]" :title="'Search in document ' + fileSearch.document">
                📄 {{fileSearch.document}}
            </span>
            <span v-else class="mr-1" title="Search All Documents">📚</span>
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
            }
            if (def.category) {
                ret.category = def.category
            }
            if (def.document) {
                ret.document = def.document
            }
            const filestore = ext.state.filestores?.find(f => f.name === filestoreName)
            if (filestore) {
                ret.description = filestore.displayName
                ret.url = `/gemini/filestores/${filestore.id}`
            }
            if (!ret.category && tool.metadata_filter) {
                const field = leftPart(tool.metadata_filter, '=')
                const value = rightPart(tool.metadata_filter, '=')
                if (field === 'category' && value) {
                    ret.category = value
                }
            }
            return ret
        })
        return {
            fileSearch
        }
    }
}

const GeminiFooter = {
    template: `
        <div v-if="hasMetadata" class="mt-4 border-t pt-4 space-y-4" :class="[$styles.chromeBorder]">
            <!-- Grounding Sources -->
            <div v-if="groundingChunks.length > 0" class="space-y-2">
                <div class="flex items-center gap-2 text-sm font-medium text-gray-700 dark:text-gray-300">
                    <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                        <polyline points="14 2 14 8 20 8"/>
                    </svg>
                    <span>Sources ({{ groundingChunks.length }})</span>
                </div>
                <div class="grid grid-cols-1 gap-2">
                    <div v-for="(chunk, idx) in groundingChunks" :key="idx"
                        class="group relative bg-gray-50 dark:bg-gray-800 rounded-lg p-3 border border-gray-200 dark:border-gray-700 hover:border-blue-400 dark:hover:border-blue-600 transition-colors">
                        <div class="flex items-start justify-between gap-2"
                            @click="chunk.retrievedContext.text && toggleChunk(idx)"
                            :class="{'cursor-pointer': chunk.retrievedContext.text}">
                            <div class="flex-1 min-w-0">
                                <div v-if="getDocument(chunk)">
                                    <a
                                        @click.stop
                                        :href="getDocument(chunk).url + '?download'"
                                        class="text-sm font-medium text-blue-600 dark:text-blue-400 hover:text-blue-700 dark:hover:text-blue-300 truncate"
                                        :title="'Download ' + chunk.retrievedContext.title">
                                        {{ chunk.retrievedContext.title }}
                                    </a>
                                </div>
                                <div v-else class="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">
                                    {{ chunk.retrievedContext.title }}
                                </div>
                                <div v-if="chunk.retrievedContext.text" class="mt-1 text-xs text-gray-600 dark:text-gray-400 line-clamp-2">
                                    {{ chunk.retrievedContext.text.substring(0, 150) }}{{ chunk.retrievedContext.text.length > 150 ? '...' : '' }}
                                </div>
                            </div>
                            <div
                                v-if="chunk.retrievedContext.text"
                                class="flex-shrink-0 p-1 text-gray-400 transition-colors pointer-events-none"
                                :title="expandedChunks.has(idx) ? 'Show less' : 'Show more'">
                                <svg class="w-4 h-4 transition-transform" :class="{'rotate-180': expandedChunks.has(idx)}" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <polyline points="6 9 12 15 18 9"/>
                                </svg>
                            </div>
                        </div>
                        <div v-if="expandedChunks.has(idx) && chunk.retrievedContext.text" class="mt-2 pt-2 border-t border-gray-200 dark:border-gray-700">
                            <div class="prose prose-sm max-w-none dark:prose-invert whitespace-wrap" style="font-size:13px" v-html="$fmt.markdown(chunk.retrievedContext.text)"></div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `,
    props: {
        thread: Object
    },
    setup(props) {
        const expandedChunks = ref(new Set())

        const groundingChunks = computed(() => {
            const candidate = props.thread?.providerResponse?.candidates?.[0]
            const chunks = candidate?.groundingMetadata?.groundingChunks || []
            return chunks
        })

        const modelVersion = computed(() => {
            return props.thread?.providerResponse?.modelVersion
        })

        const hasMetadata = computed(() => {
            return groundingChunks.value.length > 0
        })

        function getDocument(chunk) {
            const title = chunk.retrievedContext?.title
            if (!title) return null
            const docs = Object.values(ext.state.documentsCache)
            return docs.find(doc => doc.displayName === title)
        }

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
            // Load documents for all grounding chunks
            const filestoreNames = chunks.map(c => c.retrievedContext?.fileSearchStore).filter(Boolean)
            filestoreNames.forEach(name => {
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

        onMounted(() => {
            loadDocumentChunks(groundingChunks.value)
        })

        return {
            ext,
            expandedChunks,
            groundingChunks,
            modelVersion,
            hasMetadata,
            getDocument,
            toggleChunk,
        }
    }
}

export default {
    install(context) {
        ext = context.scope('gemini')
        ctx = context

        ctx.setLeftIcons({
            gemini: {
                component: {
                    template: `<svg @click="$ctx.togglePath('/gemini')" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-linejoin="round" stroke-width="1.5" d="M3 12a9 9 0 0 0 9-9a9 9 0 0 0 9 9a9 9 0 0 0-9 9a9 9 0 0 0-9-9Z"/></svg>`
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

        ctx.setThreadFooters({
            gemini: {
                component: GeminiFooter,
                show({ thread }) {
                    return thread.provider === 'google' || thread.model?.toLowerCase().includes('gemini')
                }
            }
        })

        ext.setState({
            filestores: [],
            documentsCache: {},
        })
    },

    async load(ctx) {
        await loadFilestores()
    }
}
