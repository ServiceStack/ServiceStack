import { ref, computed, nextTick, watch, onMounted, provide, inject } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useThreadStore } from './threadStore.mjs'
import { storageObject, addCopyButtons } from './utils.mjs'
import { renderMarkdown } from './markdown.mjs'
import ChatPrompt, { useChatPrompt } from './ChatPrompt.mjs'
import SignIn from './SignIn.mjs'
import Avatar from './Avatar.mjs'
import ModelSelector from './ModelSelector.mjs'
import SystemPromptSelector from './SystemPromptSelector.mjs'
import SystemPromptEditor from './SystemPromptEditor.mjs'
import { useSettings } from "./SettingsDialog.mjs"
import Welcome from './Welcome.mjs'

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
                                @click="exportThreads"
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
                                    class="absolute top-2 right-2 opacity-0 group-hover:opacity-100 transition-opacity duration-200 p-1 rounded hover:bg-black/10 focus:outline-none focus:ring-2 focus:ring-blue-500"
                                    :class="message.role === 'user' ? 'text-white/70 hover:text-white hover:bg-white/20' : 'text-gray-500 hover:text-gray-700'"
                                    title="Copy message content"
                                >
                                    <svg class="w-4 h-4" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
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
                                    {{ formatTime(message.timestamp) }}
                                </div>
                            </div>
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
                    selectedPrompt.value = null
                    currentSystemPrompt.value = ''
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
                    source: 'ServiceStack.AI.Chat',
                    threadCount: allThreads.length,
                    threads: allThreads
                }

                // Create and download JSON file
                const jsonString = JSON.stringify(exportData, null, 2)
                const blob = new Blob([jsonString], { type: 'application/json' })
                const url = URL.createObjectURL(blob)

                const link = document.createElement('a')
                link.href = url
                link.download = `aichat-threads-export-${new Date().toISOString().split('T')[0]}.json`
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

        function triggerImport() {
            if (isImporting.value) return
            fileInput.value?.click()
        }

        async function handleFileImport(event) {
            const file = event.target.files?.[0]
            if (!file) return

            isImporting.value = true
            try {
                const text = await file.text()
                const importData = JSON.parse(text)

                // Validate import data structure
                if (!importData.threads || !Array.isArray(importData.threads)) {
                    throw new Error('Invalid import file: missing or invalid threads array')
                }

                // Import threads one by one
                let importedCount = 0
                let updatedCount = 0

                for (const threadData of importData.threads) {
                    if (!threadData.id) {
                        console.warn('Skipping thread without ID:', threadData)
                        continue
                    }

                    try {
                        // Check if thread already exists
                        const existingThread = await threads.getThread(threadData.id)

                        if (existingThread) {
                            // Update existing thread
                            await threads.updateThread(threadData.id, {
                                title: threadData.title,
                                model: threadData.model,
                                systemPrompt: threadData.systemPrompt,
                                messages: threadData.messages || [],
                                createdAt: threadData.createdAt,
                                // Keep the existing updatedAt or use imported one
                                updatedAt: threadData.updatedAt || existingThread.updatedAt
                            })
                            updatedCount++
                        } else {
                            // Add new thread directly to IndexedDB
                            await threads.initDB()
                            const db = await threads.initDB()
                            const tx = db.transaction(['threads'], 'readwrite')
                            await tx.objectStore('threads').add({
                                id: threadData.id,
                                title: threadData.title || 'Imported Chat',
                                model: threadData.model || '',
                                systemPrompt: threadData.systemPrompt || '',
                                messages: threadData.messages || [],
                                createdAt: threadData.createdAt || new Date().toISOString(),
                                updatedAt: threadData.updatedAt || new Date().toISOString()
                            })
                            await tx.complete
                            importedCount++
                        }
                    } catch (error) {
                        console.error('Failed to import thread:', threadData.id, error)
                    }
                }

                // Reload threads to reflect changes
                await threads.loadThreads()

                alert(`Import completed!\nNew threads: ${importedCount}\nUpdated threads: ${updatedCount}`)

            } catch (error) {
                console.error('Failed to import threads:', error)
                alert('Failed to import threads: ' + error.message)
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
            formatTime,
            renderMarkdown,
            isReasoningExpanded,
            toggleReasoning,
            formatReasoning,
            copyMessageContent,
            configUpdated,
            exportThreads,
            isExporting,
            triggerImport,
            handleFileImport,
            isImporting,
            fileInput,
        }
    }
}
