import { ref, computed, nextTick, watch, onMounted, onUnmounted, provide, inject } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useThreadStore } from './threadStore.mjs'
import { storageObject, addCopyButtons } from './utils.mjs'
import { renderMarkdown } from './markdown.mjs'
import ChatPrompt, { useChatPrompt } from './ChatPrompt.mjs'
import SignIn from './SignIn.mjs'

const ProviderStatus = {
    template:`
        <div ref="triggerRef" class="relative" :key="renderKey">
            <button type="button" @click="togglePopover" 
                class="mt-1 flex space-x-2 items-center text-sm font-semibold select-none rounded-sm py-2 px-3 border border-transparent hover:bg-gray-50 hover:shadow hover:border-gray-200">
                <span class="text-gray-600" :title="models.length + ' models from ' + (config.status.enabled||[]).length + ' enabled providers'">{{models.length}}</span>
                <div class="cursor-pointer flex items-center" :title="'Enabled:\\n' + (config.status.enabled||[]).map(x => '  ' + x).join('\\n')">
                    <svg class="size-4 text-green-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" fill="currentColor"/></svg>
                    <span class="text-green-700">{{(config.status.enabled||[]).length}}</span>
                </div>
                <div class="cursor-pointer flex items-center" :title="'Disabled:\\n' + (config.status.disabled||[]).map(x => '  ' + x).join('\\n')">
                    <svg class="size-4 text-red-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" fill="currentColor"/></svg>
                    <span class="text-red-700">{{(config.status.disabled||[]).length}}</span>
                </div>
            </button>
            <div v-if="showPopover" ref="popoverRef" class="absolute right-0 mt-2 w-72 max-h-116 overflow-y-auto bg-white border border-gray-200 rounded-md shadow-lg z-10">
                <div class="divide-y divide-gray-100">
                    <div v-for="p in allProviders" :key="p" class="flex items-center justify-between px-3 py-2">
                        <label :for="'chk_' + p" class="cursor-pointer text-sm text-gray-900 truncate mr-2" :title="p">{{ p }}</label>
                        <div @click="onToggle(p, !isEnabled(p))" class="cursor-pointer group relative inline-flex h-5 w-10 shrink-0 items-center justify-center rounded-full outline-offset-2 outline-green-600 has-focus-visible:outline-2">
                            <span class="absolute mx-auto h-4 w-9 rounded-full bg-gray-200 inset-ring inset-ring-gray-900/5 transition-colors duration-200 ease-in-out group-has-checked:bg-green-600" />
                            <span class="absolute left-0 size-5 rounded-full border border-gray-300 bg-white shadow-xs transition-transform duration-200 ease-in-out group-has-checked:translate-x-5" />
                            <input :id="'chk_' + p" type="checkbox" :checked="isEnabled(p)" class="cursor-pointer absolute inset-0 appearance-none focus:outline-hidden" aria-label="Use setting" name="setting" />
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `,
    emits: ['updated'],
    setup(props, { emit }) {
        const ai = inject('ai')
        const config = inject('config')
        const models = inject('models')
        const showPopover = ref(false)
        const triggerRef = ref(null)
        const popoverRef = ref(null)
        const pending = ref({})
        const renderKey = ref(0)
        const allProviders = computed(() => config.status?.all)
        const isEnabled = (p) => config.status.enabled.includes(p)
        const togglePopover = () => showPopover.value = !showPopover.value

        const onToggle = async (provider, enable) => {
            pending.value = { ...pending.value, [provider]: true }
            try {
                const res = await ai.post(`/providers/${encodeURIComponent(provider)}`, {
                    body: JSON.stringify(enable ? { enable: true } : { disable: true })
                })
                if (!res.ok) throw new Error(`HTTP ${res.status} ${res.statusText}`)
                const json = await res.json()
                config.status.enabled = json.enabled || []
                config.status.disabled = json.disabled || []
                if (json.feedback) {
                    alert(json.feedback)
                }

                try {
                    const [configRes, modelsRes] = await Promise.all([
                        ai.getConfig(),
                        ai.getModels(),
                    ])
                    const newConfig = await configRes.json()
                    const newModels = await modelsRes.json()
                    Object.assign(config, newConfig)
                    models.length = 0
                    newModels.forEach(m => models.push(m))
                    emit('updated')
                    renderKey.value++
                } catch (e) {
                    alert(`Failed to reload config: ${e.message}`)
                }

            } catch (e) {
                alert(`Failed to ${enable ? 'enable' : 'disable'} ${provider}: ${e.message}`)
            } finally {
                pending.value = { ...pending.value, [provider]: false }
            }
        }

        const onDocClick = (e) => {
            const t = e.target
            if (triggerRef.value?.contains(t)) return
            if (popoverRef.value?.contains(t)) return
            showPopover.value = false
        }
        onMounted(() => document.addEventListener('click', onDocClick))
        onUnmounted(() => document.removeEventListener('click', onDocClick))
        return { 
            renderKey,
            config,
            models,
            showPopover, 
            triggerRef, 
            popoverRef, 
            allProviders, 
            isEnabled, 
            togglePopover, 
            onToggle, 
            pending,
        }
    }
}

export default {
    components: {
        ChatPrompt,
        ProviderStatus,
        SignIn,
    },
    template: `
        <div class="flex flex-col h-full w-full">
            <!-- Header with model and prompt selectors -->
            <div class="border-b border-gray-200 bg-white px-2 py-2 w-full min-h-16">
                <div class="flex items-center justify-between w-full">
                    <!-- Model Selector -->
                    <div class="pl-1 flex space-x-2">
                        <Autocomplete id="model" :options="models" v-model="selectedModel" label=""
                            class="w-72 xl:w-84"
                            :match="(x, value) => x.toLowerCase().includes(value.toLowerCase())"
                            placeholder="Select Model...">
                            <template #item="{ value }">
                                <div class="truncate max-w-72" :title="value">{{value}}</div>
                            </template>
                        </Autocomplete>
                        <ProviderStatus @updated="configUpdated" />
                    </div>

                    <!-- System Prompt Selector -->
                    <div class="flex items-center space-x-2">
                        <button v-if="selectedPrompt" type="button" title="Clear System Prompt" @click="selectedPrompt = null">
                            <svg class="size-4 text-gray-500" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="M19 6.41L17.59 5L12 10.59L6.41 5L5 6.41L10.59 12L5 17.59L6.41 19L12 13.41L17.59 19L19 17.59L13.41 12z"/></svg>
                        </button>
                        
                        <Autocomplete id="prompt" :options="prompts" v-model="selectedPrompt" label=""
                            class="w-72 xl:w-84"
                            :match="(x, value) => x.name.toLowerCase().includes(value.toLowerCase())"
                            placeholder="Select a System Prompt...">
                            <template #item="{ value }">
                                <div class="truncate max-w-72" :title="value">{{value}}</div>
                            </template>
                        </Autocomplete>

                        <!-- Toggle System Prompt Visibility -->
                        <button type="button"
                            @click="showSystemPrompt = !showSystemPrompt"
                            :class="showSystemPrompt ? 'text-blue-700' : 'text-gray-600'"
                            class="p-1 rounded-md hover:bg-blue-100 hover:text-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
                            :title="showSystemPrompt ? 'Hide system prompt' : 'Show system prompt'"
                        >
                            <svg v-if="!showSystemPrompt" class="size-6" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 36 36"><path fill="currentColor" d="M33.62 17.53c-3.37-6.23-9.28-10-15.82-10S5.34 11.3 2 17.53l-.28.47l.26.48c3.37 6.23 9.28 10 15.82 10s12.46-3.72 15.82-10l.26-.48Zm-15.82 8.9C12.17 26.43 7 23.29 4 18c3-5.29 8.17-8.43 13.8-8.43S28.54 12.72 31.59 18c-3.05 5.29-8.17 8.43-13.79 8.43"/><path fill="currentColor" d="M18.09 11.17A6.86 6.86 0 1 0 25 18a6.86 6.86 0 0 0-6.91-6.83m0 11.72A4.86 4.86 0 1 1 23 18a4.87 4.87 0 0 1-4.91 4.89"/><path fill="none" d="M0 0h36v36H0z"/></svg>
                            <svg v-else class="size-6" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 36 36"><path fill="currentColor" d="M25.19 20.4a6.8 6.8 0 0 0 .43-2.4a6.86 6.86 0 0 0-6.86-6.86a6.8 6.8 0 0 0-2.37.43L18 13.23a5 5 0 0 1 .74-.06A4.87 4.87 0 0 1 23.62 18a5 5 0 0 1-.06.74Z" class="clr-i-outline clr-i-outline-path-1"/><path fill="currentColor" d="M34.29 17.53c-3.37-6.23-9.28-10-15.82-10a16.8 16.8 0 0 0-5.24.85L14.84 10a14.8 14.8 0 0 1 3.63-.47c5.63 0 10.75 3.14 13.8 8.43a17.8 17.8 0 0 1-4.37 5.1l1.42 1.42a19.9 19.9 0 0 0 5-6l.26-.48Z"/><path fill="currentColor" d="m4.87 5.78l4.46 4.46a19.5 19.5 0 0 0-6.69 7.29l-.26.47l.26.48c3.37 6.23 9.28 10 15.82 10a16.9 16.9 0 0 0 7.37-1.69l5 5l1.75-1.5l-26-26Zm9.75 9.75l6.65 6.65a4.8 4.8 0 0 1-2.5.72A4.87 4.87 0 0 1 13.9 18a4.8 4.8 0 0 1 .72-2.47m-1.45-1.45a6.85 6.85 0 0 0 9.55 9.55l1.6 1.6a14.9 14.9 0 0 1-5.86 1.2c-5.63 0-10.75-3.14-13.8-8.43a17.3 17.3 0 0 1 6.12-6.3Z"/><path fill="none" d="M0 0h36v36H0z"/></svg>
                        </button>
                        
                        <div v-if="$ai.auth?.profileUrl" :title="$ai.authTitle">
                            <img :src="$ai.auth.profileUrl" class="size-8 rounded-full" />
                        </div>
                    </div>
                </div>
            </div>

            <!-- System Prompt Editor -->
            <div v-if="showSystemPrompt" class="border-b border-gray-200 bg-gray-50 px-6 py-4">
                <div class="max-w-6xl mx-auto">
                    <label class="block text-sm font-medium text-gray-700 mb-2">
                        System Prompt
                        <span v-if="selectedPrompt" class="text-gray-500 font-normal">
                            ({{ prompts.find(p => p.id === selectedPrompt.id)?.name || 'Custom' }})
                        </span>
                    </label>
                    <textarea
                        v-model="currentSystemPrompt"
                        placeholder="Enter a system prompt to guide AI's behavior..."
                        rows="6"
                        class="block w-full resize-vertical rounded-md border border-gray-300 px-3 py-2 text-sm placeholder-gray-500 focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
                    ></textarea>
                    <div class="mt-2 text-xs text-gray-500">
                        You can modify this system prompt before sending messages. Changes will only apply to new conversations.
                    </div>
                </div>
            </div>

            <!-- Messages Area -->
            <div class="flex-1 overflow-y-auto" ref="messagesContainer">
                <div class="mx-auto max-w-6xl px-4 py-6">
                    <div v-if="$ai.requiresAuth && !$ai.auth">
                        <SignIn @done="$ai.signIn($event)" />
                    </div>
                    <!-- Welcome message when no thread is selected -->
                    <div v-else-if="!currentThread" class="text-center py-12">
                        <div class="mb-2 flex justify-center">
                            <svg class="size-20 text-gray-700" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 16 16"><path fill="currentColor" d="M8 2.19c3.13 0 5.68 2.25 5.68 5s-2.55 5-5.68 5a5.7 5.7 0 0 1-1.89-.29l-.75-.26l-.56.56a14 14 0 0 1-2 1.55a.13.13 0 0 1-.07 0v-.06a6.58 6.58 0 0 0 .15-4.29a5.25 5.25 0 0 1-.55-2.16c0-2.77 2.55-5 5.68-5M8 .94c-3.83 0-6.93 2.81-6.93 6.27a6.4 6.4 0 0 0 .64 2.64a5.53 5.53 0 0 1-.18 3.48a1.32 1.32 0 0 0 2 1.5a15 15 0 0 0 2.16-1.71a6.8 6.8 0 0 0 2.31.36c3.83 0 6.93-2.81 6.93-6.27S11.83.94 8 .94"/><ellipse cx="5.2" cy="7.7" fill="currentColor" rx=".8" ry=".75"/><ellipse cx="8" cy="7.7" fill="currentColor" rx=".8" ry=".75"/><ellipse cx="10.8" cy="7.7" fill="currentColor" rx=".8" ry=".75"/></svg>
                        </div>
                        <h2 class="text-2xl font-semibold text-gray-900 mb-2">{{ $ai.welcome }}</h2>

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
                        <div v-if="errorMessage" class="flex items-start space-x-3">
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
                                        <div class="text-base font-medium mb-1">{{ errorStatus || 'Error' }}</div>
                                        <div class="text-sm whitespace-pre-wrap break-words max-h-80 overflow-y-auto font-mono p-2 rounded">
                                            {{ errorMessage }}
                                         </div>
                                    </div>
                                    <button type="button"
                                        @click="errorMessage = null"
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
        const router = useRouter()
        const route = useRoute()
        const threads = useThreadStore()
        const { currentThread } = threads
        const chatPrompt = useChatPrompt()
        const { 
            errorStatus,
            errorMessage,
            isGenerating,
        } = chatPrompt
        provide('threads', threads)
        provide('chatPrompt', chatPrompt)
        const models = inject('models')
        const config = inject('config')

        const prefs = storageObject('prefs')

        const customPromptValue = ref('')
        const customPrompt = {
            id: '_custom_',
            name: 'Custom...',
            value: ''
        }

        const prompts = computed(() => [customPrompt, ...config.prompts])

        const selectedModel = ref(prefs.model || config.defaults.text.model || '')
        const selectedPrompt = ref(prefs.systemPrompt || '')
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
            localStorage.setItem('prefs', JSON.stringify({
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
                    source: 'llms.py',
                    threadCount: allThreads.length,
                    threads: allThreads
                }

                // Create and download JSON file
                const jsonString = JSON.stringify(exportData, null, 2)
                const blob = new Blob([jsonString], { type: 'application/json' })
                const url = URL.createObjectURL(blob)

                const link = document.createElement('a')
                link.href = url
                link.download = `llms-threads-export-${new Date().toISOString().split('T')[0]}.json`
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
            errorMessage,
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
