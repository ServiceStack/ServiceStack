import { ref, computed, nextTick, watch, onMounted, provide, inject } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useFormatters } from '@servicestack/vue'
import { useThreadStore } from './threadStore.mjs'
import { storageObject, addCopyButtons, formatCost, statsTitle } from './utils.mjs'
import { renderMarkdown } from './markdown.mjs'
import ChatPrompt, { useChatPrompt } from './ChatPrompt.mjs'
import SignIn from './SignIn.mjs'
import Avatar from './Avatar.mjs'
import ModelSelector from './ModelSelector.mjs'
import SystemPromptSelector from './SystemPromptSelector.mjs'
import SystemPromptEditor from './SystemPromptEditor.mjs'
import { useSettings } from "./SettingsDialog.mjs"
import Welcome from './Welcome.mjs'

const { humanifyMs, humanifyNumber } = useFormatters()

export default {
    components: {
        ModelSelector,
        SystemPromptSelector,
        SystemPromptEditor,
        ChatPrompt,
        SignIn,
        Avatar,
        Welcome,
    },
    template: `
        <div class="flex flex-col h-full w-full">
            <!-- Header with model and prompt selectors -->
            <div class="border-b border-gray-200 bg-white px-2 py-2 w-full min-h-16">
                <div class="flex items-center justify-between w-full">
                    <ModelSelector :models="models" v-model="selectedModel" @updated="configUpdated" />

                    <div class="flex items-center space-x-2">
                        <SystemPromptSelector :prompts="prompts" v-model="selectedPrompt" 
                            :show="showSystemPrompt" @toggle="showSystemPrompt = !showSystemPrompt" />
                        <Avatar />
                    </div>
                </div>
            </div>

            <SystemPromptEditor v-if="showSystemPrompt" 
                v-model="currentSystemPrompt" :prompts="prompts" :selected="selectedPrompt" />

            <!-- Messages Area -->
            <div class="flex-1 overflow-y-auto" ref="messagesContainer">
                <div class="mx-auto max-w-6xl px-4 py-6">
                    <div v-if="$ai.requiresAuth && !$ai.auth">
                        <SignIn @done="$ai.signIn($event)" />
                    </div>
                    <!-- Welcome message when no thread is selected -->
                    <div v-else-if="!currentThread" class="text-center py-12">
                        <Welcome />

                        <!-- Chat input for new conversation -->
                        <div class="max-w-2xl mx-auto">
                            <ChatPrompt :model="selectedModel" :systemPrompt="currentSystemPrompt" />
                        </div>

                        <!-- Export/Import buttons -->
                        <div class="mt-2 flex space-x-3 justify-center">
                            <button type="button"
                                @click="(e) => e.altKey ? exportRequests() : exportThreads()"
                                :disabled="isExporting"
                                :title="'Export ' + threads?.threads?.value?.length + ' conversations'"
                                class="inline-flex items-center px-3 py-2 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
                            >
                                <svg v-if="!isExporting" class="size-5 mr-1" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                                    <path fill="currentColor" d="m12 16l-5-5l1.4-1.45l2.6 2.6V4h2v8.15l2.6-2.6L17 11zm-6 4q-.825 0-1.412-.587T4 18v-3h2v3h12v-3h2v3q0 .825-.587 1.413T18 20z"></path>
                                </svg>
                                <svg v-else class="size-5 mr-1 animate-spin" fill="none" viewBox="0 0 24 24">
                                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                                </svg>
                                {{ isExporting ? 'Exporting...' : 'Export' }}
                            </button>

                            <button type="button"
                                @click="triggerImport"
                                :disabled="isImporting"
                                title="Import conversations from JSON file"
                                class="inline-flex items-center px-3 py-2 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
                            >
                                <svg v-if="!isImporting" class="size-5 mr-1" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                                    <path fill="currentColor" d="m14 12l-4-4v3H2v2h8v3m10 2V6a2 2 0 0 0-2-2H6a2 2 0 0 0-2 2v3h2V6h12v12H6v-3H4v3a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2"/>
                                </svg>
                                <svg v-else class="size-5 mr-1 animate-spin" fill="none" viewBox="0 0 24 24">
                                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                                </svg>
                                {{ isImporting ? 'Importing...' : 'Import' }}
                            </button>

                            <!-- Hidden file input for import -->
                            <input
                                ref="fileInput"
                                type="file"
                                accept=".json"
                                @change="handleFileImport"
                                class="hidden"
                            />
                        </div>

                    </div>

                    <!-- Messages -->
                    <div v-else class="space-y-6">
                        <div
                            v-for="message in currentThread.messages"
                            :key="message.id"
                            class="flex items-start space-x-3 group"
                            :class="message.role === 'user' ? 'flex-row-reverse space-x-reverse' : ''"
                        >
                            <!-- Avatar outside the bubble -->
                            <div class="flex-shrink-0 flex flex-col justify-center">
                                <div class="w-8 h-8 rounded-full flex items-center justify-center text-sm font-medium"
                                     :class="message.role === 'user'
                                        ? 'bg-blue-600 text-white'
                                        : 'bg-gray-600 text-white'"
                                >
                                    {{ message.role === 'user' ? 'U' : 'AI' }}
                                </div>

                                <!-- Delete button (shown on hover) -->
                                <button type="button" @click.stop="threads.deleteMessageFromThread(currentThread.id, message.id)"
                                    class="mx-auto opacity-0 group-hover:opacity-100 mt-2 rounded text-gray-400 hover:text-red-600 hover:bg-red-50 transition-all"
                                    title="Delete message">
                                    <svg class="size-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path>
                                    </svg>
                                </button>
                            </div>

                            <!-- Message bubble -->
                            <div
                                class="message rounded-lg px-4 py-3 relative group"
                                :class="message.role === 'user'
                                    ? 'bg-blue-600 text-white'
                                    : 'bg-gray-100 text-gray-900 border border-gray-200'"
                            >
                                <!-- Copy button in top right corner -->
                                <button
                                    type="button"
                                    @click="copyMessageContent(message)"
                                    class="absolute top-2 right-2 opacity-0 group-hover:opacity-100 transition-opacity duration-200 p-1 rounded hover:bg-black/10 focus:outline-none focus:ring-0"
                                    :class="message.role === 'user' ? 'text-white/70 hover:text-white hover:bg-white/20' : 'text-gray-500 hover:text-gray-700'"
                                    title="Copy message content"
                                >
                                    <svg v-if="copying === message" class="size-4 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg>
                                    <svg v-else class="size-4" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                        <rect width="14" height="14" x="8" y="8" rx="2" ry="2"/>
                                        <path d="M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2"/>
                                    </svg>
                                </button>

                                <div
                                    v-if="message.role === 'assistant'"
                                    v-html="renderMarkdown(message.content)"
                                    class="prose prose-sm max-w-none"
                                ></div>

                                <!-- Collapsible reasoning section -->
                                <div v-if="message.role === 'assistant' && message.reasoning" class="mt-2">
                                    <button type="button" @click="toggleReasoning(message.id)" class="text-xs text-gray-600 hover:text-gray-800 flex items-center space-x-1">
                                        <svg class="w-3 h-3" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" :class="isReasoningExpanded(message.id) ? 'transform rotate-90' : ''"><path fill="currentColor" d="M7 5l6 5l-6 5z"/></svg>
                                        <span>{{ isReasoningExpanded(message.id) ? 'Hide reasoning' : 'Show reasoning' }}</span>
                                    </button>
                                    <div v-if="isReasoningExpanded(message.id)" class="mt-2 rounded border border-gray-200 bg-gray-50 p-2">
                                        <div v-if="typeof message.reasoning === 'string'" v-html="renderMarkdown(message.reasoning)" class="prose prose-xs max-w-none"></div>
                                        <pre v-else class="text-xs whitespace-pre-wrap overflow-x-auto">{{ formatReasoning(message.reasoning) }}</pre>
                                    </div>
                                </div>

                                <div v-if="message.role !== 'assistant'" class="whitespace-pre-wrap">{{ message.content }}</div>
                                <div class="mt-2 text-xs opacity-70">
                                    <span>{{ formatTime(message.timestamp) }}</span>
                                    <span v-if="message.usage" :title="tokensTitle(message.usage)">
                                        &#8226;
                                        {{ humanifyNumber(message.usage.tokens) }} tokens
                                        <span v-if="message.usage.cost">&#183; {{ message.usage.cost }}</span>
                                        <span v-if="message.usage.duration"> in {{ humanifyMs(message.usage.duration) }}</span>
                                    </span>
                                </div>
                            </div>

                            <!-- Edit and Redo buttons (shown on hover for user messages, outside bubble) -->
                            <div v-if="message.role === 'user'" class="flex flex-col gap-2 opacity-0 group-hover:opacity-100 transition-opacity mt-1">
                                <button type="button" @click.stop="editMessage(message)"
                                    class="whitespace-nowrap text-xs px-2 py-1 rounded text-gray-400 hover:text-green-600 hover:bg-green-50 transition-all"
                                    title="Edit message">
                                    <svg class="size-4 inline mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"></path>
                                    </svg>
                                    Edit
                                </button>
                                <button type="button" @click.stop="redoMessage(message)"
                                    class="whitespace-nowrap text-xs px-2 py-1 rounded text-gray-400 hover:text-blue-600 hover:bg-blue-50 transition-all"
                                    title="Redo message (clears all responses after this message and re-runs it)">
                                    <svg class="size-4 inline mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"></path>
                                    </svg>
                                    Redo
                                </button>
                            </div>
                        </div>

                        <div v-if="currentThread.stats && currentThread.stats.outputTokens" class="text-center text-gray-500 text-sm">
                            <span :title="statsTitle(currentThread.stats)">
                                {{ currentThread.stats.cost ? formatCost(currentThread.stats.cost) + '  for ' : '' }} {{ humanifyNumber(currentThread.stats.inputTokens) }} → {{ humanifyNumber(currentThread.stats.outputTokens) }} tokens over {{ currentThread.stats.requests }} request{{currentThread.stats.requests===1?'':'s'}} in {{ humanifyMs(currentThread.stats.duration) }}
                            </span>
                        </div>

                        <!-- Loading indicator -->
                        <div v-if="isGenerating" class="flex items-start space-x-3">
                            <!-- Avatar outside the bubble -->
                            <div class="flex-shrink-0">
                                <div class="w-8 h-8 rounded-full bg-gray-600 text-white flex items-center justify-center text-sm font-medium">
                                    AI
                                </div>
                            </div>

                            <!-- Loading bubble -->
                            <div class="rounded-lg px-4 py-3 bg-gray-100 border border-gray-200">
                                <div class="flex space-x-1">
                                    <div class="w-2 h-2 bg-gray-400 rounded-full animate-bounce"></div>
                                    <div class="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style="animation-delay: 0.1s"></div>
                                    <div class="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style="animation-delay: 0.2s"></div>
                                </div>
                            </div>
                        </div>

                        <!-- Error message bubble -->
                        <div v-if="errorStatus" class="flex items-start space-x-3">
                            <!-- Avatar outside the bubble -->
                            <div class="flex-shrink-0">
                                <div class="w-8 h-8 rounded-full bg-red-600 text-white flex items-center justify-center text-sm font-medium">
                                    !
                                </div>
                            </div>

                            <!-- Error bubble -->
                            <div class="max-w-[85%] rounded-lg px-4 py-3 bg-red-50 border border-red-200 text-red-800 shadow-sm">
                                <div class="flex items-start space-x-2">
                                    <div class="flex-1 min-w-0">
                                        <div class="text-base font-medium mb-1">{{ errorStatus?.errorCode || 'Error' }}</div>
                                        <div v-if="errorStatus?.message" class="text-base mb-1">{{ errorStatus.message }}</div>
                                        <div v-if="errorStatus?.stackTrace" class="text-sm whitespace-pre-wrap break-words max-h-80 overflow-y-auto font-mono p-2 rounded">
                                            {{ errorStatus.stackTrace }}
                                        </div>
                                    </div>
                                    <button type="button"
                                        @click="errorStatus = null"
                                        class="text-red-400 hover:text-red-600 flex-shrink-0"
                                    >
                                        <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                                            <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd"></path>
                                        </svg>
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Edit message modal -->
                <div v-if="editingMessageId" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
                    <div class="relative bg-white rounded-lg shadow-lg p-6 max-w-2xl w-full mx-4">
                        <CloseButton @click="cancelEdit" class="" />
                        <h3 class="text-lg font-semibold text-gray-900 mb-4">Edit Message</h3>
                        <textarea
                            v-model="editingMessageContent"
                            class="w-full h-40 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent resize-none"
                            placeholder="Edit your message..."
                        ></textarea>
                        <div class="mt-4 flex gap-2 justify-end">
                            <button type="button" @click="cancelEdit"
                                class="px-4 py-2 rounded-md border border-gray-300 text-gray-700 hover:bg-gray-50 transition-all">
                                Cancel
                            </button>
                            <button type="button" @click="saveEditedMessage"
                                class="px-4 py-2 rounded-md bg-blue-600 text-white hover:bg-blue-700 transition-all">
                                Save
                            </button>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Input Area - only show when thread is selected -->
            <div v-if="currentThread" class="flex-shrink-0 border-t border-gray-200 bg-white px-6 py-4">
                <ChatPrompt :model="selectedModel" :systemPrompt="currentSystemPrompt" />
            </div>
        </div>
    `,
    props: {
    },
    setup(props) {
        const ai = inject('ai')
        const router = useRouter()
        const route = useRoute()
        const threads = useThreadStore()
        const { currentThread } = threads
        const chatPrompt = useChatPrompt()
        const chatSettings = useSettings()
        const { 
            errorStatus,
            isGenerating,
        } = chatPrompt
        provide('threads', threads)
        provide('chatPrompt', chatPrompt)
        provide('chatSettings', chatSettings)
        const models = inject('models')
        const config = inject('config')

        const prefs = storageObject(ai.prefsKey)

        const customPromptValue = ref('')
        const customPrompt = {
            id: '_custom_',
            name: 'Custom...',
            value: ''
        }

        const prompts = computed(() => [customPrompt, ...config.prompts])

        const selectedModel = ref(prefs.model || config.defaults.text.model || '')
        const selectedPrompt = ref(prefs.systemPrompt || null)
        const currentSystemPrompt = ref('')
        const showSystemPrompt = ref(false)
        const messagesContainer = ref(null)
        const isExporting = ref(false)
        const isImporting = ref(false)
        const fileInput = ref(null)
        const copying = ref(null)
        const editingMessageId = ref(null)
        const editingMessageContent = ref('')
        const editingMessage = ref(null)

        // Auto-scroll to bottom when new messages arrive
        const scrollToBottom = async () => {
            await nextTick()
            if (messagesContainer.value) {
                messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight
            }
        }

        // Watch for new messages and scroll
        watch(() => currentThread.value?.messages?.length, scrollToBottom)

        // Watch for route changes and load the appropriate thread
        watch(() => route.params.id, async (newId) => {
            const thread = await threads.setCurrentThreadFromRoute(newId, router)

            // If the selected thread specifies a model and it's available, switch to it
            if (thread?.model && Array.isArray(models) && models.includes(thread.model)) {
                selectedModel.value = thread.model
            }

            // Sync System Prompt selection from thread
            if (thread) {
                const norm = s => (s || '').replace(/\s+/g, ' ').trim()
                const tsp = norm(thread.systemPrompt || '')
                if (tsp) {
                    const match = config.prompts.find(p => norm(p.value) === tsp)
                    if (match) {
                        selectedPrompt.value = match
                        currentSystemPrompt.value = match.value.replace(/\n/g, ' ')
                    } else {
                        selectedPrompt.value = customPrompt
                        currentSystemPrompt.value = thread.systemPrompt
                    }
                } else {
                    // Preserve existing selected prompt
                    // selectedPrompt.value = null
                    // currentSystemPrompt.value = ''
                }
            }

            if (!newId) {
                chatPrompt.reset()
            }
            nextTick(addCopyButtons)
        }, { immediate: true })

        // Watch selectedPrompt and update currentSystemPrompt
        watch(selectedPrompt, (newPrompt) => {
            // If using a custom prompt, keep whatever is already in currentSystemPrompt
            if (newPrompt && newPrompt.id === '_custom_') return
            const prompt = newPrompt && config.prompts.find(p => p.id === newPrompt.id)
            currentSystemPrompt.value = prompt ? prompt.value.replace(/\n/g,' ') : ''
        }, { immediate: true })

        watch(() => [selectedModel.value, selectedPrompt.value], () => {
            localStorage.setItem(ai.prefsKey, JSON.stringify({
                model: selectedModel.value,
                systemPrompt: selectedPrompt.value
            }))
        })

        async function exportThreads() {
            if (isExporting.value) return

            isExporting.value = true
            try {
                // Load all threads from IndexedDB
                await threads.loadThreads()
                const allThreads = threads.threads.value

                // Create export data with metadata
                const exportData = {
                    exportedAt: new Date().toISOString(),
                    version: '1.0',
                    source: 'llmspy',
                    threadCount: allThreads.length,
                    threads: allThreads
                }

                // Create and download JSON file
                const jsonString = JSON.stringify(exportData, null, 2)
                const blob = new Blob([jsonString], { type: 'application/json' })
                const url = URL.createObjectURL(blob)

                const link = document.createElement('a')
                link.href = url
                link.download = `llmsthreads-export-${new Date().toISOString().split('T')[0]}.json`
                document.body.appendChild(link)
                link.click()
                document.body.removeChild(link)
                URL.revokeObjectURL(url)

            } catch (error) {
                console.error('Failed to export threads:', error)
                alert('Failed to export threads: ' + error.message)
            } finally {
                isExporting.value = false
            }
        }

        async function exportRequests() {
            if (isExporting.value) return

            isExporting.value = true
            try {
                // Load all threads from IndexedDB
                const allRequests = await threads.getAllRequests()

                // Create export data with metadata
                const exportData = {
                    exportedAt: new Date().toISOString(),
                    version: '1.0',
                    source: 'llmspy',
                    requestsCount: allRequests.length,
                    requests: allRequests
                }

                // Create and download JSON file
                const jsonString = JSON.stringify(exportData, null, 2)
                const blob = new Blob([jsonString], { type: 'application/json' })
                const url = URL.createObjectURL(blob)

                const link = document.createElement('a')
                link.href = url
                link.download = `llmsrequests-export-${new Date().toISOString().split('T')[0]}.json`
                document.body.appendChild(link)
                link.click()
                document.body.removeChild(link)
                URL.revokeObjectURL(url)

            } catch (error) {
                console.error('Failed to export requests:', error)
                alert('Failed to export requests: ' + error.message)
            } finally {
                isExporting.value = false
            }
        }

        function triggerImport() {
            if (isImporting.value) return
            fileInput.value?.click()
        }

        async function handleFileImport(event) {
            const file = event.target.files?.[0]
            if (!file) return

            isImporting.value = true
            var importType = 'threads'
            try {
                const text = await file.text()
                const importData = JSON.parse(text)
                importType = importData.threads 
                    ? 'threads' 
                    : importData.requests
                        ? 'requests'
                        : 'unknown'

                // Import threads one by one
                let importedCount = 0
                let existingCount = 0

                const db = await threads.initDB()

                if (importData.threads) {
                    if (!Array.isArray(importData.threads)) {
                        throw new Error('Invalid import file: missing or invalid threads array')
                    }

                    const threadIds = new Set(await threads.getAllThreadIds())

                    for (const threadData of importData.threads) {
                        if (!threadData.id) {
                            console.warn('Skipping thread without ID:', threadData)
                            continue
                        }

                        try {
                            // Check if thread already exists
                            const existingThread = threadIds.has(threadData.id)
                            if (existingThread) {
                                existingCount++
                            } else {
                                // Add new thread directly to IndexedDB
                                const tx = db.transaction(['threads'], 'readwrite')
                                await tx.objectStore('threads').add(threadData)
                                await tx.complete
                                importedCount++
                            }
                        } catch (error) {
                            console.error('Failed to import thread:', threadData.id, error)
                        }
                    }

                    // Reload threads to reflect changes
                    await threads.loadThreads()

                    alert(`Import completed!\nNew threads: ${importedCount}\nExisting threads: ${existingCount}`)
                }
                if (importData.requests) {
                    if (!Array.isArray(importData.requests)) {
                        throw new Error('Invalid import file: missing or invalid requests array')
                    }

                    const requestIds = new Set(await threads.getAllRequestIds())

                    for (const requestData of importData.requests) {
                        if (!requestData.id) {
                            console.warn('Skipping request without ID:', requestData)
                            continue
                        }

                        try {
                            // Check if request already exists
                            const existingRequest = requestIds.has(requestData.id)
                            if (existingRequest) {
                                existingCount++
                            } else {
                                // Add new request directly to IndexedDB
                                const db = await threads.initDB()
                                const tx = db.transaction(['requests'], 'readwrite')
                                await tx.objectStore('requests').add(requestData)
                                await tx.complete
                                importedCount++
                            }
                        } catch (error) {
                            console.error('Failed to import request:', requestData.id, error)
                        }
                    }

                    alert(`Import completed!\nNew requests: ${importedCount}\nExisting requests: ${existingCount}`)
                }

            } catch (error) {
                console.error('Failed to import ' + importType + ':', error)
                alert('Failed to import ' + importType + ': ' + error.message)
            } finally {
                isImporting.value = false
                // Clear the file input
                if (fileInput.value) {
                    fileInput.value.value = ''
                }
            }
        }

        function configUpdated() {
            console.log('configUpdated', selectedModel.value, models.length, models.includes(selectedModel.value))
            if (selectedModel.value && !models.includes(selectedModel.value)) {
                selectedModel.value = config.defaults.text.model || ''
            }
        }

        // Format timestamp
        const formatTime = (timestamp) => {
            return new Date(timestamp).toLocaleTimeString([], {
                hour: '2-digit',
                minute: '2-digit'
            })
        }

        // Reasoning collapse state and helpers
        const expandedReasoning = ref(new Set())
        const isReasoningExpanded = (id) => expandedReasoning.value.has(id)
        const toggleReasoning = (id) => {
            const s = new Set(expandedReasoning.value)
            if (s.has(id)) {
                s.delete(id)
            } else {
                s.add(id)
            }
            expandedReasoning.value = s
        }
        const formatReasoning = (r) => typeof r === 'string' ? r : JSON.stringify(r, null, 2)

        // Copy message content to clipboard
        const copyMessageContent = async (message) => {
            try {
                copying.value = message
                await navigator.clipboard.writeText(message.content)
                // Could add a toast notification here if desired
            } catch (err) {
                console.error('Failed to copy message content:', err)
                // Fallback for older browsers
                const textArea = document.createElement('textarea')
                textArea.value = message.content
                document.body.appendChild(textArea)
                textArea.select()
                document.execCommand('copy')
                document.body.removeChild(textArea)
            }
            setTimeout(() => { copying.value = null }, 2000)
        }

        // Redo a user message (clear all messages after it and re-run)
        const redoMessage = async (message) => {
            if (!currentThread.value || message.role !== 'user') return

            try {
                const threadId = currentThread.value.id

                // Clear all messages after this one
                await threads.redoMessageFromThread(threadId, message.id)

                // Extract the actual message content (remove media indicators if present)
                let messageContent = message.content
                // Remove media indicators like [🖼️ filename] or [🔉 filename] or [📎 filename]
                messageContent = messageContent.replace(/\n\n\[[🖼️🔉📎] [^\]]+\]$/, '')

                // Set the message text in the chat prompt
                chatPrompt.messageText.value = messageContent

                // Clear any attached files since we're re-running
                chatPrompt.attachedFiles.value = []

                // Trigger send by simulating the send action
                // We'll use a small delay to ensure the UI updates
                await nextTick()

                // Find the send button and click it
                const sendButton = document.querySelector('button[title*="Send"]')
                if (sendButton && !sendButton.disabled) {
                    sendButton.click()
                }
            } catch (error) {
                console.error('Failed to redo message:', error)
                errorStatus.value = {
                    errorCode: 'Error',
                    message: 'Failed to redo message: ' + error.message,
                    stackTrace: null
                }
            }
        }

        // Edit a user message
        const editMessage = (message) => {
            if (!currentThread.value || message.role !== 'user') return

            editingMessage.value = message
            editingMessageId.value = message.id
            // Extract the actual message content (remove media indicators if present)
            let messageContent = message.content
            messageContent = messageContent.replace(/\n\n\[[🖼️🔉📎] [^\]]+\]$/, '')
            editingMessageContent.value = messageContent
        }

        // Save edited message
        const saveEditedMessage = async () => {
            if (!currentThread.value || !editingMessage.value || !editingMessageContent.value.trim()) return

            try {
                const threadId = currentThread.value.id
                const messageId = editingMessage.value.id
                const updatedContent = editingMessageContent.value

                // Update the message content
                editingMessage.value.content = updatedContent
                await threads.updateMessageInThread(threadId, messageId, { content: updatedContent })

                // Clear editing state
                editingMessageId.value = null
                editingMessageContent.value = ''
                editingMessage.value = null

                // Now redo the message (clear all responses after it and re-run)
                await nextTick()
                await threads.redoMessageFromThread(threadId, messageId)

                // Set the message text in the chat prompt
                chatPrompt.messageText.value = updatedContent

                // Clear any attached files since we're re-running
                chatPrompt.attachedFiles.value = []

                // Trigger send by simulating the send action
                await nextTick()

                // Find the send button and click it
                const sendButton = document.querySelector('button[title*="Send"]')
                if (sendButton && !sendButton.disabled) {
                    sendButton.click()
                }
            } catch (error) {
                console.error('Failed to save edited message:', error)
                errorStatus.value = {
                    errorCode: 'Error',
                    message: 'Failed to save edited message: ' + error.message,
                    stackTrace: null
                }
            }
        }

        // Cancel editing
        const cancelEdit = () => {
            editingMessageId.value = null
            editingMessageContent.value = ''
            editingMessage.value = null
        }

        function tokensTitle(usage) {
            let title = []
            if (usage.tokens && usage.price) {
                const msg = parseFloat(usage.price) > 0
                    ? `${usage.tokens} tokens @ ${usage.price} = ${tokenCost(usage.price, usage.tokens)}`
                    : `${usage.tokens} tokens`
                const duration = usage.duration ? ` in ${usage.duration}ms` : ''
                title.push(msg + duration)
            }
            return title.join('\n')
        }
        const numFmt = new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD', minimumFractionDigits: 6 })
        function tokenCost(price, tokens) {
            if (!price || !tokens) return ''
            return numFmt.format(parseFloat(price) * tokens)
        }

        onMounted(() => {
            setTimeout(addCopyButtons, 1)
        })

        return {
            config,
            models,
            threads,
            prompts,
            isGenerating,
            customPromptValue,
            currentThread,
            selectedModel,
            selectedPrompt,
            currentSystemPrompt,
            showSystemPrompt,
            messagesContainer,
            errorStatus,
            copying,
            editingMessageId,
            editingMessageContent,
            editingMessage,
            formatTime,
            renderMarkdown,
            isReasoningExpanded,
            toggleReasoning,
            formatReasoning,
            copyMessageContent,
            redoMessage,
            editMessage,
            saveEditedMessage,
            cancelEdit,
            configUpdated,
            exportThreads,
            exportRequests,
            isExporting,
            triggerImport,
            handleFileImport,
            isImporting,
            fileInput,
            tokensTitle,
            humanifyMs,
            humanifyNumber,
            formatCost,
            statsTitle,
        }
    }
}
