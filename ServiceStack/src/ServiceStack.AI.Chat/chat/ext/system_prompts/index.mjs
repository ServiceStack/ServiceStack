import { ref, computed, inject, watch, onMounted, onUnmounted, nextTick } from "vue"
import { AppContext } from "ctx.mjs"

let ext

const PromptFinder = {
    template: `
    <div v-if="modelValue" class="absolute right-0 z-10 mt-1 origin-top-right rounded-md bg-white dark:bg-gray-900 shadow-lg border border-gray-300 dark:border-gray-600 focus:outline-none"
         style="width:400px"
         role="menu" aria-orientation="vertical" aria-labelledby="menu-button" tabindex="-1">
        <div class="p-2" role="none">
            <div class="relative mb-2">
                <div class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3">
                    <svg class="h-4 w-4 text-gray-400" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                        <path fill-rule="evenodd" d="M9 3.5a5.5 5.5 0 100 11 5.5 5.5 0 000-11zM2 9a7 7 0 1112.452 4.391l3.328 3.329a.75.75 0 11-1.06 1.06l-3.329-3.328A7 7 0 012 9z" clip-rule="evenodd" />
                    </svg>
                </div>
                <input type="text" 
                    ref="searchInput"
                    v-model="searchQuery"
                    @keydown="onKeydown"
                    class="block w-full rounded-md border-0 py-1.5 pl-10 text-gray-900 dark:text-gray-100 shadow-sm ring-1 ring-inset ring-gray-300 dark:ring-gray-600 placeholder:text-gray-400 focus:ring-2 focus:ring-inset focus:ring-blue-600 sm:text-xs sm:leading-6 bg-transparent" 
                    placeholder="Search prompts...">
            </div>

            <div class="max-h-80 overflow-y-auto" ref="resultsList">
                <div v-if="filteredPrompts.length === 0" class="p-4 text-center text-xs text-gray-500">
                    No prompts found
                </div>
                <div v-for="(prompt, index) in filteredPrompts" :key="prompt.id" 
                    @click="selectPrompt(prompt)"
                    :class="['group relative flex gap-x-2 rounded-md p-2 cursor-pointer border-b border-gray-100 dark:border-gray-800 last:border-0', 
                             selectedIndex === index ? 'bg-blue-50 dark:bg-blue-900/20' : 'hover:bg-gray-50 dark:hover:bg-gray-800']"
                    :data-index="index">
                    <div class="flex-auto">
                        <div class="flex items-center justify-between">
                            <h4 :class="['font-semibold text-sm', selectedIndex === index ? 'text-blue-700 dark:text-blue-300' : 'text-gray-900 dark:text-gray-100']">
                                {{ prompt.name }}
                            </h4>
                        </div>
                        <p class="text-xs leading-4 text-gray-500 dark:text-gray-400 line-clamp-2 mt-0.5">{{ prompt.value }}</p>
                    </div>
                </div>
            </div>
        </div>
    </div>
    `,
    props: {
        modelValue: Boolean, // controls visibility
        prompts: {
            type: Array,
            default: () => []
        }
    },
    emits: ['update:modelValue', 'select'],
    setup(props, { emit }) {
        const searchQuery = ref('')
        const searchInput = ref(null)
        const resultsList = ref(null)
        const selectedIndex = ref(-1)

        const filteredPrompts = computed(() => {
            if (!searchQuery.value) return props.prompts
            const q = searchQuery.value.toLowerCase()
            return props.prompts.filter(p =>
                p.name.toLowerCase().includes(q) ||
                p.value.toLowerCase().includes(q) ||
                p.id.toLowerCase().includes(q)
            )
        })

        function selectPrompt(prompt) {
            emit('select', prompt)
            emit('update:modelValue', false)
        }

        function scrollToSelected() {
            nextTick(() => {
                if (!resultsList.value) return
                const el = resultsList.value.querySelector(`[data-index="${selectedIndex.value}"]`)
                if (el) {
                    el.scrollIntoView({ block: 'nearest' })
                }
            })
        }

        function onKeydown(e) {
            if (filteredPrompts.value.length === 0) return

            if (e.key === 'ArrowDown') {
                e.preventDefault()
                selectedIndex.value = (selectedIndex.value + 1) % filteredPrompts.value.length
                scrollToSelected()
            } else if (e.key === 'ArrowUp') {
                e.preventDefault()
                selectedIndex.value = (selectedIndex.value - 1 + filteredPrompts.value.length) % filteredPrompts.value.length
                scrollToSelected()
            } else if (e.key === 'Enter') {
                e.preventDefault()
                if (selectedIndex.value >= 0 && selectedIndex.value < filteredPrompts.value.length) {
                    selectPrompt(filteredPrompts.value[selectedIndex.value])
                }
            }
        }

        watch(() => props.modelValue, (isOpen) => {
            if (isOpen) {
                // Focus search input when modal opens
                nextTick(() => {
                    if (searchInput.value) {
                        searchInput.value.focus()
                    }
                })
                selectedIndex.value = -1
            } else {
                searchQuery.value = ''
            }
        })

        watch(searchQuery, () => {
            selectedIndex.value = 0 // Select first result on search
        })

        return {
            searchQuery,
            searchInput,
            resultsList,
            filteredPrompts,
            selectedIndex,
            selectPrompt,
            onKeydown
        }
    }
}

const SystemPromptEditor = {
    template: `
    <div class="px-4 py-4 overflow-y-auto border-b" :class="$styles.panel">
        <div class="max-w-6xl mx-auto">
            <div class="mb-2 text-xs font-bold uppercase tracking-wider" :class="$styles.heading">
                System Prompt
            </div>
            <div class="flex justify-end items-center">
                <div v-if="hasMessages" class="text-sm text-gray-500 dark:text-gray-400">
                    {{ !ext.prefs.systemPrompt ? '' : prompts.find(x => x.value === ext.prefs.systemPrompt)?.name || 'Custom' }}
                </div>
                <div v-else class="mb-2" ref="containerRef">
                    <div class="flex items-center gap-2">
                        <span v-if="selected" class="text-sm text-gray-500 dark:text-gray-400">
                            {{ selected.name }}
                        </span>
                        <button v-if="modelValue" type="button" title="Clear System Prompt" @click="$emit('update:modelValue', null)"
                            class="rounded-full p-1 hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors">
                            <svg class="size-4 text-gray-500 dark:text-gray-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="M19 6.41L17.59 5L12 10.59L6.41 5L5 6.41L10.59 12L5 17.59L6.41 19L12 13.41L17.59 19L19 17.59L13.41 12z"/></svg>
                        </button>
                        <button type="button" 
                            @click="ext.setPrefs({ showFinder: !ext.prefs.showFinder })"
                            class="inline-flex items-center gap-x-1.5 rounded-md bg-white dark:bg-gray-900 px-2.5 py-1.5 text-sm font-medium text-gray-700 dark:text-gray-300 shadow-sm border border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800">
                            Explore Prompts
                        </button>
                    </div>
                    <PromptFinder v-model="ext.prefs.showFinder" :prompts="prompts" @select="onSelect" />
                </div>
            </div>
            <div v-if="hasMessages" class="w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 px-3 py-2 text-sm">
                <TextViewer prefsName="systemPrompt" :text="$threads.getCurrentThreadSystemPrompt() || 'No System Prompt was used'" />
            </div>
            <div v-else>
                <textarea
                    :value="modelValue" @input="$emit('update:modelValue', $event.target.value)"
                    placeholder="Enter a system prompt to guide AI's behavior..."
                    rows="6"
                    class="block w-full resize-vertical rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 px-3 py-2 text-sm placeholder-gray-500 dark:placeholder-gray-400 focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
                ></textarea>
            </div>
        </div>
    </div>
    `,
    emits: ['update:modelValue'],
    props: {
        prompts: Array,
        selected: Object,
        modelValue: String,
    },
    setup(props, { emit }) {
        /**@type {AppContext} */
        const ctx = inject('ctx')
        const containerRef = ref()
        const hasMessages = computed(() => ctx.threads.currentThread.value?.messages?.length > 0)
        const selected = computed(() =>
            props.prompts.find(x => x.value === props.modelValue) ?? { name: "Custom", value: props.modelValue })

        function onSelect(prompt) {
            ext.setPrefs({ prompt: prompt }) // {"id","name","value"}
            emit('update:modelValue', prompt.value)
        }

        function closeFinder(e) {
            if (ext.prefs.showFinder && containerRef.value && !containerRef.value.contains(e.target)) {
                ext.setPrefs({ showFinder: false })
            }
        }

        watch(() => props.modelValue, systemPrompt => {
            ext.setPrefs({ systemPrompt })
        })

        onMounted(() => {
            document.addEventListener('click', closeFinder)
            if (ext.prefs.prompt) {
                const promptValue = typeof ext.prefs.prompt === 'object'
                    ? ext.prefs.prompt.value
                    : ext.prefs.prompt
                if (promptValue) {
                    emit('update:modelValue', promptValue)
                    ext.setPrefs({ systemPrompt: promptValue })
                }
            }
        })
        onUnmounted(() => {
            document.removeEventListener('click', closeFinder)
        })

        return {
            ext,
            hasMessages,
            selected,
            containerRef,
            onSelect,
        }
    }
}

export default {
    order: 30 - 100,

    install(ctx) {
        ext = ctx.scope('system_prompts')
        ctx.components({
            PromptFinder,
            SystemPromptEditor,
            SystemPromptsPanel: {
                template: `<SystemPromptEditor :prompts="ext.state.prompts" v-model="ext.prefs.prompt" />`,
                setup() {
                    return { ext }
                }
            }
        })
        if (!ext.prefs.systemPrompt) ext.setPrefs({ systemPrompt: '' })

        ctx.setTopIcons({
            system_prompts: {
                component: {
                    template: `<svg @click="$ctx.toggleTop('SystemPromptsPanel')" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m5 7l5 5l-5 5m8 0h6"/></svg>`,
                },
                isActive({ top }) { return top === 'SystemPromptsPanel' }
            }
        })

        ctx.chatRequestFilters.push(({ request, thread, context }) => {

            const hasSystemPrompt = !!context.systemPrompt
            console.log('system_prompts chatRequestFilters', hasSystemPrompt)
            if (hasSystemPrompt) {
                console.log('Already has system prompt', hasSystemPrompt.content)
                return
            }

            if (ext.prefs.systemPrompt) {
                context.systemPrompt = ext.prefs.systemPrompt
            }
        })

        ctx.setState({ prompts: [] })
    },

    async load(ctx) {
        const api = await ext.getJson(`/prompts.json`)
        const prompts = api.response || []
        ext.setState({ prompts })
    }
}
