import { ref, computed, watch, inject } from 'vue'
import { storageObject } from './utils.mjs'

const settingsKey = 'llms.settings'

export function useSettings() {
    const intFields = [
        'max_completion_tokens',
        'n',
        'seed',
        'top_logprobs',
    ]
    const floatFields = [
        'frequency_penalty',
        'presence_penalty',
        'temperature',
        'top_p',
    ]
    const boolFields = [
        'enable_thinking',
        'parallel_tool_calls',
        'store',
    ]
    const strFields = [
        'prompt_cache_key',
        'reasoning_effort',
        'safety_identifier',
        'service_tier',
        'verbosity',
    ]
    const listFields = [
        'stop',
    ]
    const allFields = [
        ...intFields,
        ...floatFields,
        ...boolFields,
        ...strFields,
        ...listFields,
    ]

    let settings = ref(storageObject(settingsKey))
    
    function validSettings(localSettings) {
        const to = {}
        intFields.forEach(f => {
            if (localSettings[f] != null && localSettings[f] !== '' && !isNaN(parseInt(localSettings[f]))) {
                to[f] = parseInt(localSettings[f])
            }
        })
        floatFields.forEach(f => {
            if (localSettings[f] != null && localSettings[f] !== '' && !isNaN(parseFloat(localSettings[f]))) {
                to[f] = parseFloat(localSettings[f])
            }
        })
        boolFields.forEach(f => {
            if (localSettings[f] != null && localSettings[f] !== '' && !!localSettings[f]) {
                to[f] = !!localSettings[f]
            }
        })
        strFields.forEach(f => {
            if (localSettings[f] != null && localSettings[f] !== '') {
                to[f] = localSettings[f]
            }
        })
        listFields.forEach(f => {
            if (localSettings[f] != null && localSettings[f] !== '') {
                to[f] = Array.isArray(localSettings[f]) 
                    ? localSettings[f]
                    : typeof localSettings[f] == 'string' 
                        ? localSettings[f].split(',').map(x => x.trim())
                        : []
            }
        })
        return to
    }

    function applySettings(chatRequest) {
        console.log('applySettings', JSON.stringify(settings.value, undefined, 2))
        const removeFields = allFields.filter(f => !(f in settings.value))
        removeFields.forEach(f => delete chatRequest[f])
        Object.keys(settings.value).forEach(k => {
            chatRequest[k] = settings.value[k]
        })
        console.log('applySettings.chatRequest', JSON.stringify(chatRequest, undefined, 2))
    }

    function resetSettings() {
        return saveSettings({})
    }
    
    function saveSettings(localSettings) {
        // console.log('saveSettings', JSON.stringify(localSettings, undefined, 2))
        settings.value = validSettings(localSettings)
        console.log('saveSettings.settings', JSON.stringify(settings.value, undefined, 2))
        return storageObject(settingsKey, settings.value)
    }

    return {
        allFields,
        settings,
        applySettings,
        saveSettings,
        resetSettings,
    }
}

export default {
    template: `
    <div v-if="isOpen" class="fixed inset-0 z-50 overflow-y-auto" @click.self="close">
        <div class="flex min-h-screen items-center justify-center p-4">
            <!-- Backdrop -->
            <div class="fixed inset-0 bg-black/40 transition-opacity" @click="close"></div>
            
            <!-- Dialog -->
            <div class="relative bg-white dark:bg-gray-800 rounded-lg shadow-xl max-w-2xl w-full max-h-[90vh] overflow-hidden">
                <!-- Header -->
                <div class="flex items-center justify-between px-6 py-4 border-b border-gray-200 dark:border-gray-700">
                    <h2 class="text-xl font-semibold text-gray-900 dark:text-gray-100">Chat Request Settings</h2>
                    <button type="button" @click="close" class="text-gray-400 dark:text-gray-500 hover:text-gray-600 dark:hover:text-gray-300">
                        <svg class="size-6" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                            <path fill="currentColor" d="M19 6.41L17.59 5L12 10.59L6.41 5L5 6.41L10.59 12L5 17.59L6.41 19L12 13.41L17.59 19L19 17.59L13.41 12z"/>
                        </svg>
                    </button>
                </div>

                <!-- Content -->
                <form class="px-6 py-4 overflow-y-auto max-h-[calc(90vh-140px)]" @submit.prevent="save">
                    <p class="text-sm text-gray-600 dark:text-gray-400 mb-4">
                        Configure default values for chat request options. Leave empty to use model defaults.
                    </p>

                    <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                        <!-- Temperature -->
                        <div>
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                                Temperature
                                <span class="text-gray-500 dark:text-gray-400 font-normal">(0-2)</span>
                            </label>
                            <input type="number" v-model="localSettings.temperature"
                                step="0.1" min="0" max="2"
                                placeholder="e.g., 0.7"
                                class="block w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500" />
                            <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">Higher values more random, lower for more focus</p>
                        </div>

                        <!-- Max Completion Tokens -->
                        <div>
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                                Max Completion Tokens
                            </label>
                            <input type="number" v-model="localSettings.max_completion_tokens"
                                step="1" min="1"
                                placeholder="e.g., 2048"
                                class="block w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500" />
                            <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">Max tokens for completion (inc. reasoning tokens)</p>
                        </div>

                        <!-- Seed -->
                        <div>
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                                Seed
                            </label>
                            <input type="number" v-model="localSettings.seed"
                                step="1"
                                placeholder="e.g., 42"
                                class="block w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500" />
                            <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">For deterministic sampling (Beta feature)</p>
                        </div>

                        <!-- Top P -->
                        <div>
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                                Top P
                                <span class="text-gray-500 dark:text-gray-400 font-normal">(0-1)</span>
                            </label>
                            <input type="number" v-model="localSettings.top_p"
                                step="0.1" min="0" max="1"
                                placeholder="e.g., 0.9"
                                class="block w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500" />
                            <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">Nucleus sampling - alternative to temperature</p>
                        </div>

                        <!-- Frequency Penalty -->
                        <div>
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                                Frequency Penalty
                                <span class="text-gray-500 dark:text-gray-400 font-normal">(-2.0 to 2.0)</span>
                            </label>
                            <input type="number" v-model="localSettings.frequency_penalty"
                                step="0.1" min="-2" max="2"
                                placeholder="e.g., 0.5"
                                class="block w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500" />
                            <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">Penalize tokens based on frequency in text</p>
                        </div>

                        <!-- Presence Penalty -->
                        <div>
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                                Presence Penalty
                                <span class="text-gray-500 dark:text-gray-400 font-normal">(-2.0 to 2.0)</span>
                            </label>
                            <input type="number" v-model="localSettings.presence_penalty"
                                step="0.1" min="-2" max="2"
                                placeholder="e.g., 0.5"
                                class="block w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500" />
                            <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">Penalize tokens based on presence in text</p>
                        </div>

                        <!-- Stop Sequences -->
                        <div>
                            <label for="stop" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                                Stop Sequences
                            </label>
                            <TagInput id="stop" inputClass="h-[37px] !shadow-none"
                                v-model="localSettings.stop"
                                placeholder=""
                                label=""
                                />
                            <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">Up to 4 sequences where API stops generating</p>
                        </div>

                        <!-- Reasoning Effort -->
                        <div>
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                                Reasoning Effort
                            </label>
                            <select v-model="localSettings.reasoning_effort"
                                class="block w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500">
                                <option value="">Default</option>
                                <option value="minimal">Minimal</option>
                                <option value="low">Low</option>
                                <option value="medium">Medium</option>
                                <option value="high">High</option>
                            </select>
                            <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">Constrains effort on reasoning for reasoning models</p>
                        </div>

                        <!-- Verbosity -->
                        <div>
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                                Verbosity
                            </label>
                            <select v-model="localSettings.verbosity"
                                class="block w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500">
                                <option value="">Default</option>
                                <option value="low">Low</option>
                                <option value="medium">Medium</option>
                                <option value="high">High</option>
                            </select>
                            <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">Constrains verbosity of model's response</p>
                        </div>

                        <!-- Service Tier -->
                        <div>
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                                Service Tier
                            </label>
                            <input type="text" v-model="localSettings.service_tier"
                                placeholder="e.g., auto, default"
                                class="block w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500" />
                            <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">Processing type for serving the request</p>
                        </div>

                        <!-- Top Logprobs -->
                        <div>
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                                Top Logprobs
                                <span class="text-gray-500 dark:text-gray-400 font-normal">(0-20)</span>
                            </label>
                            <input type="number" v-model="localSettings.top_logprobs"
                                step="1" min="0" max="20"
                                placeholder="e.g., 5"
                                class="block w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500" />
                            <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">Number of most likely tokens to return with log probs</p>
                        </div>

                        <!-- Safety Identifier -->
                        <div>
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                                Safety Identifier
                            </label>
                            <input type="text" v-model="localSettings.safety_identifier"
                                placeholder="Unique user identifier"
                                class="block w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500" />
                            <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">Identifier to help detect policy violations</p>
                        </div>

                        <!-- Store -->
                        <div>
                            <label class="flex items-center">
                                <input type="checkbox" v-model="localSettings.store"
                                    class="rounded border-gray-300 dark:border-gray-600 text-blue-600 focus:ring-blue-500" />
                                <span class="ml-2 text-sm font-medium text-gray-700 dark:text-gray-300">Store Output</span>
                            </label>
                            <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">Store output for model distillation or evals</p>
                        </div>

                        <!-- Enable Thinking -->
                        <div>
                            <label class="flex items-center">
                                <input type="checkbox" v-model="localSettings.enable_thinking"
                                    class="rounded border-gray-300 dark:border-gray-600 text-blue-600 focus:ring-blue-500" />
                                <span class="ml-2 text-sm font-medium text-gray-700 dark:text-gray-300">Enable Thinking</span>
                            </label>
                            <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">Enable thinking mode for supported models (Qwen)</p>
                        </div>
                    </div>
                </form>

                <!-- Footer -->
                <div class="flex items-center justify-between px-6 py-4 border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800">
                    <button type="button" @click="reset"
                        class="px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 hover:text-gray-900 dark:hover:text-gray-100">
                        Reset to Defaults
                    </button>
                    <div class="flex space-x-3">
                        <button type="button" @click="close"
                            class="px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-md hover:bg-gray-50 dark:hover:bg-gray-700">
                            Cancel
                        </button>
                        <button type="submit" @click="save"
                            class="px-4 py-2 text-sm font-medium text-white bg-blue-600 dark:bg-blue-500 rounded-md hover:bg-blue-700 dark:hover:bg-blue-600">
                            Save Settings
                        </button>
                    </div>
                </div>
            </div>
        </div>
    </div>
    `,
    props: {
        isOpen: {
            type: Boolean,
            default: false
        }
    },
    emits: ['close'],
    setup(props, { emit }) {
        const chatSettings = inject('chatSettings')
        const { settings, saveSettings, resetSettings } = chatSettings
        
        // Local copy for editing
        const localSettings = ref(Object.assign({}, settings.value))

        // Watch for dialog open to sync local settings
        watch(() => props.isOpen, (isOpen) => {
            if (isOpen) {
                localSettings.value = Object.assign({}, settings.value)
            }
        })

        function close() {
            emit('close')
        }

        function save() {
            saveSettings(localSettings.value)
            close()
        }

        function reset() {
            localSettings.value = resetSettings()
        }

        return {
            localSettings,
            close,
            save,
            reset,
        }
    }
}

