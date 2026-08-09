import { ref, computed, nextTick, watch, onMounted, onUnmounted, onUpdated, inject } from 'vue'
import { useRouter, useRoute } from 'vue-router'

function tryParseJson(str) {
    try {
        return JSON.parse(str)
    } catch (e) {
        return null
    }
}
function hasJsonStructure(str) {
    return tryParseJson(str) != null
}

function isEmpty(v) {
    return !v || v === '{}' || v === '[]' || v === 'null' || v === 'undefined' || v === '""' || v === "''" || v === "``"
}

export const ErrorBubble = {
    template: `
    <!-- Error message bubble -->
    <div v-if="$state.error" class="mt-8 flex items-start space-x-3">
        <!-- Avatar outside the bubble -->
        <div class="flex-shrink-0">
            <div class="size-8 rounded-full bg-red-600 dark:bg-red-500 text-white flex items-center justify-center text-lg font-bold">
                !
            </div>
        </div>

        <!-- Error bubble -->
        <div class="max-w-[85%] rounded-lg px-4 py-3 bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 text-red-800 dark:text-red-200 shadow-sm">
            <div class="flex items-start space-x-2">
                <div class="flex-1 min-w-0">
                    <div class="flex justify-between items-start">
                        <div class="text-base font-medium mb-1">{{ $state.error?.errorCode || 'Error' }}</div>
                        <button type="button" @click="$ctx.clearError()" title="Clear Error"
                            class="text-red-400 dark:text-red-300 hover:text-red-600 dark:hover:text-red-100 flex-shrink-0">
                            <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                                <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd"></path>
                            </svg>
                        </button>
                    </div>
                    <div v-if="$state.error?.message" class="text-base mb-1">{{ $state.error.message }}</div>
                    <div v-if="$state.error?.stackTrace" class="mt-2 text-sm whitespace-pre-wrap break-words max-h-80 overflow-y-auto font-mono p-2 border border-red-200/70 dark:border-red-800/70">
                        {{ $state.error.stackTrace }}
                    </div>
                </div>
            </div>
        </div>
    </div>
    `,
}

function embedHtml(html) {
    const resizeScript = `<script>
        let lastH = 0;
        const sendHeight = () => {
            const body = document.body;
            if (!body) return;
            // Force re-calc
            const h = document.documentElement.getBoundingClientRect().height;
            if (Math.abs(h - lastH) > 2) {
                lastH = h;
                window.parent.postMessage({ type: 'iframe-resize', height: h }, '*');
            }
        }
        const ro = new ResizeObserver(sendHeight);
        window.addEventListener('message', (e) => {
            if (e.data && e.data.type === 'stop-resize') {
                ro.disconnect();
            }
        });
        window.addEventListener('load', () => {
            // Inject styles to prevent infinite loops
            const style = document.createElement('style');
            style.textContent = 'html, body { height: auto !important; min-height: 0 !important; margin: 0 !important; padding: 0 !important; overflow: hidden !important; }';
            document.head.appendChild(style);
            
            const body = document.body;
            if (body) {
                ro.observe(body);
                ro.observe(document.documentElement);
                sendHeight();
            }
        });
    <\/script>`

    const escaped = (html + resizeScript)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;')
    return `<iframe srcdoc="${escaped}" sandbox="allow-scripts" style="width:100%;height:auto;border:none;"></iframe>`
}

export const TypeText = {
    template: `
        <div data-type="text" v-if="text.type === 'text'">
            <div v-html="html?.trim()" class="whitespace-pre-wrap"></div>
        </div>
    `,
    props: {
        text: {
            type: Object,
            required: true
        }
    },
    setup(props) {
        const ctx = inject('ctx')
        const html = computed(() => {
            try {
                return ctx.fmt.markdown(props.text.text)
            } catch (e) {
                console.error('TypeText: markdown', e)
                return `<div>${props.text.text}</div>`
            }
        })
        return { html }
    }
}

export const LightboxImage = {
    template: `
    <div>
      <!-- Thumbnail -->
      <div
        class="cursor-zoom-in hover:opacity-90 transition-opacity"
        @click="isOpen = true"
      >
        <img
          :src="src"
          :alt="alt"
          :width="width"
          :height="height"
          :class="imageClass"
        />
      </div>

      <!-- Lightbox Modal -->
      <Teleport to="body">
        <div v-if="isOpen"
          class="fixed inset-0 z-[100] flex items-center justify-center bg-black/90 p-4"
          @click="isOpen = false"
          style="z-index: 9999;"
        >
          <button type="button"
            class="absolute top-4 right-4 p-2 text-white hover:bg-white/10 rounded-lg transition-colors"
            @click="isOpen = false"
            aria-label="Close lightbox"
          >
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="w-6 h-6"><path d="M18 6 6 18"/><path d="m6 6 12 12"/></svg>
          </button>

          <div class="relative max-w-7xl max-h-[90vh] w-full h-full flex items-center justify-center">
            <img
              :src="src"
              :alt="alt"
              :width="width"
              :height="height"
              class="max-w-full max-h-full w-auto h-auto object-contain rounded"
              @click.stop
            />
          </div>
        </div>
      </Teleport>
    </div>
    `,
    props: {
        src: {
            type: String,
            required: true
        },
        alt: {
            type: String,
            default: ''
        },
        width: {
            type: [Number, String],
            default: undefined
        },
        height: {
            type: [Number, String],
            default: undefined
        },
        imageClass: {
            type: String,
            default: 'max-w-[400px] max-h-96 rounded-lg border border-gray-200 dark:border-gray-700 object-contain bg-gray-50 dark:bg-gray-900 shadow-sm transition-transform hover:scale-[1.02]'
        }
    },
    setup(props) {
        const ctx = inject('ctx')
        const isOpen = ref(false)

        let sub
        onMounted(() => {
            sub = ctx.events.subscribe(`keydown:Escape`, () => isOpen.value = false)
        })
        onUnmounted(() => sub?.unsubscribe())

        return {
            isOpen
        }
    }
}

export const TypeImage = {
    template: `
        <div data-type="image" v-if="image.type === 'image_url'">
            <LightboxImage :src="$ctx.resolveUrl(image.image_url.url)" />
        </div>
    `,
    props: {
        image: {
            type: Object,
            required: true
        }
    }
}

export const TypeAudio = {
    template: `
        <div data-type="audio" v-if="audio.type === 'audio_url' || audio.type === 'input_audio'">
            <slot></slot>
            <audio controls :src="audioUrl" class="h-8 w-64"></audio>
        </div>
    `,
    props: {
        audio: {
            type: Object,
            required: true
        }
    },
    setup(props) {
        const ctx = inject('ctx')

        const audioUrl = computed(() => ctx.resolveUrl(props.audio.type === 'input_audio'
            ? props.audio.input_audio.data
            : props.audio.audio_url.url))

        return {
            audioUrl,
        }
    }
}

export const TypeFile = {
    template: `
        <a data-type="file" v-if="file.type === 'file'" :href="$ctx.resolveUrl(file.file.file_data)" target="_blank" 
            class="flex items-center gap-2 px-3 py-2 rounded-lg border border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors text-sm text-blue-600 dark:text-blue-400 hover:underline">
            <svg class="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M13 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9z"></path><polyline points="13 2 13 9 20 9"></polyline></svg>
            <span class="max-w-xs truncate">{{ file.file.filename || 'Attachment' }}</span>
        </a>
    `,
    props: {
        file: {
            type: Object,
            required: true
        }
    }
}

export const ViewType = {
    template: `
    <div class="flex items-center gap-2 p-2 rounded-lg border border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800">
        <TypeText v-if="result.type === 'text'" :text="result" />
        <TypeImage v-else-if="result.type === 'image_url'" :image="result" />
        <TypeAudio v-else-if="result.type === 'audio_url' || result.type === 'input_audio'" :audio="result" />
        <TypeFile v-else-if="result.type === 'file'" :file="result" />
        <div data-type="other" v-else>
            <HtmlFormat :value="result" :classes="$utils.htmlFormatClasses" />
        </div>
    </div>
    `,
    props: {
        result: {
            type: Object,
            required: true
        }
    }
}
export const ViewTypes = {
    template: `
    <div v-if="results?.length" class="flex flex-col gap-2">
        <div v-if="texts.length > 0" :class="cls">
            <div v-if="hasResources" v-for="(text, i) in texts" :key="'raw-' + i" class="text-xs whitespace-pre-wrap">{{text.text}}</div>
            <TypeText v-else v-for="(text, i) in texts" :key="'text-' + i" :text="text" />
        </div>
        <div v-if="images.length > 0" :class="cls">
            <TypeImage v-for="(image, i) in images" :key="'image-' + i" :image="image" />
        </div>
        <div v-if="audios.length > 0" :class="cls">
            <TypeAudio v-for="(audio, i) in audios" :key="'audio-' + i" :audio="audio" />
        </div>
        <div v-if="files.length > 0" :class="cls">
            <TypeFile v-for="(file, i) in files" :key="'file-' + i" :file="file" />
        </div>
        <div v-if="others.length > 0" :class="cls">
            <HtmlFormat v-for="(other, i) in others" :key="'other-' + i" :value="other" :classes="$utils.htmlFormatClasses" />
        </div>
    </div>
    `,
    props: {
        results: {
            type: Array,
            required: true
        }
    },
    setup(props) {
        const cls = "flex items-center gap-2 p-2 rounded-lg border border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800"
        const texts = computed(() => props.results.filter(r => r.type === 'text'))
        const images = computed(() => props.results.filter(r => r.type === 'image_url'))
        const audios = computed(() => props.results.filter(r => r.type === 'audio_url' || r.type === 'input_audio'))
        const files = computed(() => props.results.filter(r => r.type === 'file'))
        const others = computed(() => props.results.filter(r => r.type !== 'text' && r.type !== 'image_url' && r.type !== 'audio_url' && r.type !== 'input_audio' && r.type !== 'file'))
        // If has resources, render as plain-text to avoid rendering resources multiple times
        const hasResources = computed(() => images.value.length > 0 || audios.value.length > 0 || files.value.length > 0 || others.value.length > 0)
        return { cls, texts, images, audios, files, others, hasResources }
    }
}
export const ViewToolTypes = {
    template: `<ViewTypes v-if="results?.length" :results="results" />`,
    props: {
        output: Object,
    },
    setup(props) {
        const results = computed(() => {
            const ret = []
            if (!props.output) return ret
            if (props.output.images) {
                ret.push(...props.output.images)
            }
            if (props.output.audios) {
                ret.push(...props.output.audios)
            }
            if (props.output.files) {
                ret.push(...props.output.files)
            }
            return ret
        })
        return { results }
    }
}


export const MessageUsage = {
    template: `
    <div class="mt-2 text-xs opacity-70">                                        
        <span v-if="message.model" @click="$chat.setSelectedModel({ name: message.model })" title="Select model"><span class="cursor-pointer hover:underline">{{ message.model }}</span> &#8226; </span>
        <span v-if="message.timestamp">{{ $fmt.time(message.timestamp) }}</span>
        <span v-if="usage" :title="$fmt.tokensTitle(usage)">
            &#8226;
            {{ $fmt.humanifyNumber(usage.tokens) }} tokens
            <span v-if="usage.cost">&#183; {{ $fmt.tokenCostLong(usage.cost) }}</span>
            <span v-if="usage.duration"> in {{ $fmt.humanifyMs(usage.duration * 1000) }} <span v-if="usage.tokens > 0 && usage.duration > 0">({{ Math.round(usage.tokens / usage.duration) }} tk/s)</span></span>
        </span>
    </div>    
    `,
    props: {
        usage: Object,
        message: Object,
    }
}

export const MessageReasoning = {
    template: `
    <div class="mt-2 mb-2">
        <button type="button" @click="toggleReasoning(message.timestamp)" class="text-xs flex items-center space-x-1" :class="[$styles.highlighted, $styles.linkHover]">
            <svg class="w-3 h-3" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" :class="isReasoningExpanded(message.timestamp) ? 'transform rotate-90' : ''"><path fill="currentColor" d="M7 5l6 5l-6 5z"/></svg>
            <span>
                {{ (!message?.content || String(message.content).trim().length === 0) ? 'Thinking...' : (isReasoningExpanded(message.timestamp) ? 'Hide reasoning' : 'Show reasoning') }}
                <span v-if="reasoning" class="opacity-75 font-mono ml-1">({{ reasoningLength }} chars)</span>
            </span>
        </button>
        <div v-if="isReasoningExpanded(message.timestamp)" 
            ref="reasoningBox"
            class="reasoning mt-2 p-2 rounded-lg border text-xs overflow-y-auto"
            :class="[
                $styles.card,
                (!message?.content || String(message.content).trim().length === 0) ? 'max-h-56' : 'max-h-96'
            ]">
            <div v-if="typeof reasoning === 'string'" v-html="$fmt.markdown(reasoning)" class="prose prose-xs max-w-none dark:prose-invert"></div>
            <pre v-else class="text-xs whitespace-pre-wrap overflow-x-auto">{{ formatReasoning(reasoning) }}</pre>
        </div>
    </div>
    `,
    props: {
        reasoning: [String, Object],
        message: Object,
    },
    setup(props) {
        const reasoningBox = ref(null)
        const expandedReasoning = ref(new Set())

        const isStreaming = computed(() => !props.message?.content || String(props.message.content).trim().length === 0)

        const isReasoningExpanded = (id) => {
            if (isStreaming.value) return true
            return expandedReasoning.value.has(id)
        }

        const toggleReasoning = (id) => {
            const s = new Set(expandedReasoning.value)
            if (s.has(id)) {
                s.delete(id)
            } else {
                s.add(id)
            }
            expandedReasoning.value = s
        }

        const reasoningLength = computed(() => {
            if (!props.reasoning) return '0'
            const len = typeof props.reasoning === 'string' ? props.reasoning.length : JSON.stringify(props.reasoning).length
            return len.toLocaleString()
        })

        const formatReasoning = (r) => typeof r === 'string' ? r : JSON.stringify(r, null, 2)

        const scrollToBottom = async () => {
            await nextTick()
            if (reasoningBox.value) {
                reasoningBox.value.scrollTop = reasoningBox.value.scrollHeight
            }
            requestAnimationFrame(() => {
                if (reasoningBox.value) {
                    reasoningBox.value.scrollTop = reasoningBox.value.scrollHeight
                }
            })
        }

        watch(
            () => props.reasoning,
            () => {
                if (isStreaming.value) {
                    scrollToBottom()
                }
            },
            { immediate: true }
        )

        onUpdated(() => {
            if (isStreaming.value) {
                scrollToBottom()
            }
        })

        return {
            reasoningBox,
            expandedReasoning,
            isReasoningExpanded,
            toggleReasoning,
            reasoningLength,
            formatReasoning,
        }
    }
}

export const TextViewer = {
    template: `
        <div v-if="text.length > 200" class="relative group">
            <div class="absolute top-0 right-3 opacity-0 group-hover:opacity-100 transition-opacity duration-200 flex items-center space-x-2 bg-gray-50/90 dark:bg-gray-800/90 backdrop-blur-sm rounded-md px-2 py-1 z-10 border border-gray-200 dark:border-gray-700 shadow-sm">
                <!-- Style Selector -->
                <div class="relative flex items-center">
                    <button type="button" @click="toggleDropdown" class="text-[10px] uppercase font-bold tracking-wider text-gray-600 dark:text-gray-400 hover:text-blue-600 dark:hover:text-blue-400 focus:outline-none flex items-center select-none">
                        <span>{{ prefs || 'pre' }}</span>
                        <svg class="mb-0.5 size-3 opacity-70" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3"><path d="M6 9l6 6 6-6"/></svg>
                    </button>
                    <!-- Popover -->
                    <div v-if="dropdownOpen" class="absolute right-0 top-full w-28 bg-white dark:bg-gray-800 rounded-md shadow-lg border border-gray-200 dark:border-gray-700 py-1 z-20 overflow-hidden">
                        <button 
                            v-for="style in textStyles" 
                            :key="style"
                            @click="setStyle(style)"
                            class="block w-full text-left px-3 py-1.5 text-xs text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors uppercase tracking-wider font-medium"
                            :class="{ 'text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20': prefs === style }"
                        >
                            {{ style }}
                        </button>
                    </div>
                </div>

                <div class="w-px h-3 bg-gray-300 dark:bg-gray-600"></div>

                <!-- Text Length -->
                <span class="text-xs text-gray-500 dark:text-gray-400 tabular-nums" :title="text.length + ' characters'">
                    {{ $fmt.humanifyNumber(text.length) }}
                </span>

                <!-- Copy Button -->
                <button type="button" @click="copyToClipboard" class="text-gray-500 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 focus:outline-none p-0.5 rounded transition-colors" title="Copy to clipboard">
                    <svg v-if="copied" class="size-4 text-green-600 dark:text-green-500" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="m9.55 18l-5.7-5.7l1.425-1.425L9.55 15.15l9.175-9.175L20.15 7.4z"/></svg>
                    <svg v-else xmlns="http://www.w3.org/2000/svg" class="size-4" viewBox="0 0 24 24"><path fill="currentColor" d="M16 1H4c-1.1 0-2 .9-2 2v14h2V3h12zm3 4H8c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h11c1.1 0 2-.9 2-2V7c0-1.1-.9-2-2-2m0 16H8V7h11z"/></svg>
                </button>

                <!-- Maximize Toggle -->
                <button type="button" @click="toggleMaximized" class="text-gray-500 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 focus:outline-none p-0.5 rounded transition-colors" :title="isMaximized ? 'Minimize' : 'Maximize'">
                    <svg class="size-4" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                        <path v-if="isMaximized" fill="currentColor" d="M9 9H3V7h4V3h2zm0 6H3v2h4v4h2zm12 0h-6v6h2v-4h4zm-6-6h6V7h-4V3h-2z"/>
                        <path v-else fill="currentColor" d="M3 3h6v2H5v4H3zm0 18h6v-2H5v-4H3zm12 0h6v-6h-2v4h-4zm6-18h-6v2h4v4h2z"/>
                    </svg>
                </button>
            </div>

            <!-- Content -->
            <div :class="containerClass">
                <div v-if="prefs === 'markdown'" class="prose prose-sm max-w-none dark:prose-invert">
                    <div v-html="$fmt.markdown(text)"></div>
                </div>
                <div v-else-if="prefs === 'preview' && jsonValue">
                    <HtmlFormat :value="jsonValue" />
                </div>
                <div v-else :class="['p-0.5', contentClass]">{{ text }}</div>
            </div>
        </div>
        <div v-else class="whitespace-pre-wrap">{{ text }}</div>    
    `,
    props: {
        prefsName: String,
        text: String,
    },
    setup(props) {
        const ctx = inject('ctx')
        const prefs = ref('pre')
        const maximized = ref({})
        const dropdownOpen = ref(false)
        const hash = computed(() => ctx.utils.hashString(props.text))
        const jsonValue = computed(() => ctx.utils.toJsonObject(props.text))
        const textStyles = computed(() => {
            const ret = ['pre', 'normal', 'markdown']
            if (jsonValue.value) {
                ret.push('preview')
            }
            return ret
        })

        const toggleDropdown = () => {
            dropdownOpen.value = !dropdownOpen.value
        }

        const copied = ref(false)
        const copyToClipboard = () => {
            navigator.clipboard.writeText(props.text)
            copied.value = true
            setTimeout(() => {
                copied.value = false
            }, 2000)
        }

        const setStyle = (style) => {
            prefs.value = style
            dropdownOpen.value = false
            const key = props.prefsName || 'default'
            const currentPrefs = ctx.getPrefs().textStyle || {}
            ctx.setPrefs({
                textStyle: {
                    ...currentPrefs,
                    [key]: style
                }
            })
        }

        onMounted(() => {
            const current = ctx.getPrefs()
            const key = props.prefsName || 'default'
            if (current.textStyle && current.textStyle[key]) {
                prefs.value = current.textStyle[key]
            }
        })

        const isMaximized = computed(() => maximized.value[hash.value])

        const toggleMaximized = () => {
            maximized.value[hash.value] = !maximized.value[hash.value]
        }

        const containerClass = computed(() => {
            return isMaximized.value ? 'w-full h-full' : 'max-h-60 overflow-y-auto'
        })

        const contentClass = computed(() => {
            if (prefs.value === 'pre') return 'whitespace-pre-wrap font-mono text-xs'
            if (prefs.value === 'normal') return 'font-sans text-sm'
            return ''
        })

        return {
            hash,
            textStyles,
            prefs,
            jsonValue,
            dropdownOpen,
            toggleDropdown,
            setStyle,
            isMaximized,
            toggleMaximized,

            containerClass,
            contentClass,
            copied,
            copyToClipboard
        }
    }
}

export const ToolArguments = {
    template: `
        <div ref="refArgs" v-if="dict" class="not-prose">
            <div class="prose html-format">
                <table class="table-object border-none">
                    <tr v-for="(v, k) in dict" :key="k">
                        <td data-arg="name" class="align-top py-2 px-4 text-left text-sm font-medium tracking-wider whitespace-nowrap lowercase">{{ k }}</td>
                        <td data-arg="html" v-if="$utils.isHtml(v)" style="margin:0;padding:0;width:100%">
                            <div v-html="embedHtml(v)" class="w-full h-full"></div>
                        </td>
                        <td data-arg="string" v-else-if="typeof v === 'string'" class="align-top py-2 px-4 text-sm">
                            <TextViewer prefsName="toolArgs" :text="v" />
                        </td>
                        <td data-arg="value" v-else class="align-top py-2 px-4 text-sm whitespace-pre-wrap">
                            <HtmlFormat :value="v" :classes="$utils.htmlFormatClasses" :formatText="$utils.sanitizeHtml" />
                        </td>
                    </tr>
                </table>            
            </div>
        </div>
        <div v-else-if="list" class="not-prose px-3 py-2">
            <HtmlFormat :value="list" :classes="$utils.htmlFormatClasses" />
        </div>
        <pre v-else-if="!isEmpty(value)" class="tool-arguments">{{ value }}</pre>
    `,
    props: {
        value: String,
    },
    setup(props) {
        const ctx = inject('ctx')
        const refArgs = ref()
        const maximized = ref({})
        const dict = computed(() => {
            if (isEmpty(props.value)) return null
            const ret = tryParseJson(props.value)
            return typeof ret === 'object' && !Array.isArray(ret) ? ret : null
        })
        const list = computed(() => {
            if (isEmpty(props.value)) return null
            const ret = tryParseJson(props.value)
            return Array.isArray(ret) && ret.length > 0 ? ret : null
        })

        const handleMessage = (event) => {
            console.log('handleMessage', event)
            if (event.data?.type === 'iframe-resize' && typeof event.data.height === 'number') {
                const iframes = refArgs.value?.querySelectorAll('iframe')
                iframes?.forEach(iframe => {
                    if (iframe.contentWindow === event.source) {
                        const messages = document.getElementById('messages')
                        const maxHeight = messages ? messages.clientHeight : window.innerHeight
                        const calculatedHeight = event.data.height + 30
                        const targetHeight = Math.min(calculatedHeight, maxHeight)

                        if (iframe.style.height !== targetHeight + 'px') {
                            iframe.style.height = targetHeight + 'px'
                        }

                        if (calculatedHeight > maxHeight) {
                            event.source.postMessage({ type: 'stop-resize' }, '*')
                        }
                    }
                })
            }
        }

        onMounted(() => {
            window.addEventListener('message', handleMessage)
            const hasIframes = refArgs.value?.querySelector('iframe')
            if (hasIframes) {
                refArgs.value.classList.add('has-iframes')
            }
        })

        onUnmounted(() => {
            window.removeEventListener('message', handleMessage)
        })

        return {
            refArgs,
            maximized,
            dict,
            list,
            isEmpty,
            embedHtml,
        }
    }
}

export const ToolOutput = {
    template: `
        <div v-if="output" class="border-t" :class="[$styles.chromeBorder]">
            <div class="px-3 py-1.5 flex justify-between items-center border-b" :class="[$styles.chromeBorder]">
                <div class="flex items-center gap-2">
                    <svg class="size-3.5 text-gray-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M5 12h14M12 5l7 7-7 7"/></svg>
                    <span class="text-[10px] uppercase tracking-wider font-medium" :class="[$styles.muted]">Output</span>
                </div>    
                <div v-if="hasJsonStructure(output.content)" class="flex items-center gap-2 text-[10px] uppercase tracking-wider font-medium select-none">
                    <span @click="$ctx.setPrefs({ toolFormat: 'text' })" 
                        class="cursor-pointer transition-colors"
                        :class="$ctx.prefs.toolFormat !== 'preview' ? 'text-gray-600 dark:text-gray-300' : 'text-gray-400 hover:text-gray-600 dark:hover:text-gray-300'">
                        text
                    </span>
                    <span class="text-gray-300 dark:text-gray-700">|</span>
                    <span @click="$ctx.setPrefs({ toolFormat: 'preview' })" 
                        class="cursor-pointer transition-colors"
                        :class="$ctx.prefs.toolFormat == 'preview' ? 'text-gray-600 dark:text-gray-300' : 'text-gray-400 hover:text-gray-600 dark:hover:text-gray-300'">
                        preview
                    </span>
                </div>
            </div>
            <div class="px-3 py-2">
                <div v-if="$ctx.prefs.toolFormat !== 'preview' || !hasJsonStructure(output.content)">
                    <TextViewer prefsName="toolOutput" :text="output.content" />
                </div>
                <div v-else class="not-prose text-xs">
                    <HtmlFormat v-if="tryParseJson(output.content)" :value="tryParseJson(output.content)" :classes="$utils.htmlFormatClasses" />
                    <div v-else class="text-gray-500 italic p-2">Invalid JSON content</div>
                </div>
            </div>
            <ViewToolTypes :output="output" class="p-2" />
        </div>
    `,
    props: {
        tool: Object,
        output: Object,
    },
    setup(props) {

        return {
            tryParseJson,
            hasJsonStructure,
        }
    }
}

export const CompactThreadButton = {
    template: `
        <button v-if="currentThread.messages.length > 10 || percentUsed > 40" type="button" @click.stop="compactThread()"
            class="ml-3 px-2 pt-1 pb-0.5 rounded-lg text-xs font-semibold border transition-colors select-none disabled:opacity-60 disabled:cursor-not-allowed"
            :class="buttonClass"
            :title="tooltipText"
            :disabled="compacting">
            <span v-if="compacting" class="inline-flex items-center gap-1 font-mono tabular-nums">
                <svg class="animate-spin h-3 w-3" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                <span>compacting...</span>
            </span>
            <span v-else class="inline-flex items-center gap-1 font-mono tabular-nums">
                <span v-if="percentUsed !== null">{{ percentUsed }}% used</span>
                <span v-if="percentUsed !== null">·</span>
                <span>compact</span>
            </span>
        </button>
    `,
    props: {
        currentThread: Object,
    },
    setup(props) {
        const ctx = inject('ctx')
        const contextTokens = computed(() => props.currentThread.contextTokens)
        const contextLimit = computed(() => props.currentThread.modelInfo?.limit?.context)
        const compacting = ref(false)

        // Calculate percentage (0-100)
        const percentUsed = computed(() => {
            if (!contextLimit.value || !contextTokens.value) return null
            return Math.round((contextTokens.value / contextLimit.value) * 100)
        })

        // Class for dark mode and base styles
        const buttonClass = computed(() => {
            const pct = percentUsed.value || 0
            if (pct < 40) {
                return 'border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700'
            } else if (pct < 70) {
                return 'border-orange-300 dark:border-orange-700 bg-orange-50 dark:bg-orange-900/30 text-orange-800 dark:text-orange-400 hover:bg-orange-100 dark:hover:bg-orange-900/50'
            } else {
                return 'border-red-400 dark:border-red-700 bg-red-50 dark:bg-red-900/30 text-red-700 dark:text-red-400 hover:bg-red-100 dark:hover:bg-red-900/50'
            }
        })

        const tooltipText = computed(() => {
            if (!contextTokens.value || !contextLimit.value) return 'Compact thread'
            return `${percentUsed.value}% context used - ${contextTokens.value.toLocaleString()} / ${contextLimit.value.toLocaleString()} tokens`
        })

        async function compactThread() {
            compacting.value = true
            const api = await ctx.postJson(`/ext/app/threads/${props.currentThread.id}/compact`)
            if (api.response?.id) {
                ctx.threads.loadThreads()
                ctx.router.push(`/c/${api.response.id}`)
            } else {
                ctx.setError(api.error)
            }
            compacting.value = false
        }

        return {
            compacting,
            contextTokens,
            contextLimit,
            percentUsed,
            buttonClass,
            tooltipText,
            compactThread,
        }
    }
}

export const ToolCall = {
    template: `
        <div v-if="collapsed" @click="collapsed = !collapsed" class="cursor-pointer rounded-lg overflow-hidden" :class="[$styles.card]">
            <!-- Tool Call Header -->
            <div class="px-3 py-2 flex items-center justify-between space-x-4">
                <div class="flex items-center gap-2">
                    <svg class="size-3.5 text-gray-500" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z"></path></svg>
                    <span class="font-mono text-xs font-bold">{{ tool?.function?.name || '' }}</span>
                    <span v-if="toolSummary" :title="toolSummary" class="font-mono text-xs truncate overflow-hidden xl:max-w-2xl lg:max-w-xl md:max-w-lg sm:max-w-sm max-w-xs">{{ toolSummary }}</span>
                </div>
                <span class="text-[10px] uppercase tracking-wider font-medium whitespace-nowrap" :class="[$styles.muted]">Tool Call</span>
            </div>
        </div>
        <div v-else class="rounded-lg border overflow-hidden" :class="[$styles.card]">
            <!-- Tool Call Header -->
            <div @click="collapsed = !collapsed" class="cursor-pointer px-3 py-2 flex items-center space-x-4 justify-between border-b" :class="[$styles.chromeBorder]">
                <div class="flex items-center gap-2">
                    <svg class="size-3.5 text-gray-500" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z"></path></svg>
                    <span class="font-mono text-xs font-bold">{{ tool?.function?.name || '' }}</span>
                </div>
                <span class="text-[10px] uppercase tracking-wider font-medium whitespace-nowrap">Tool Call</span>
            </div>
            
            <component v-if="toolCallBody" :is="toolCallBody.component"
                :thread="thread" :tool="tool" :output="toolOutput" />
            <template v-else>
                <ToolArguments :value="tool?.function?.arguments || ''" />
                <ToolOutput :tool="tool" :output="toolOutput" />
            </template>
        </div>    
    `,
    props: {
        thread: {
            type: Object,
            required: true
        },
        tool: {
            type: Object,
            required: true
        }
    },
    setup(props) {
        const ctx = inject('ctx')

        const collapsed = ref(true)
        const toolOutput = computed(() => props.thread?.messages?.find(m => m.role === 'tool' && m.tool_call_id === props.tool?.id))
        const toolCallBody = computed(() => ctx.toolCallBodyComponents?.[props.tool?.function?.name])
        const autoExpand = () => {
            try {
                return toolCallBody.value?.autoExpand?.({
                    thread: props.thread,
                    tool: props.tool,
                    output: toolOutput.value,
                }) === true
            } catch (e) {
                console.error('tool call autoExpand failed', e)
                return false
            }
        }
        watch([toolCallBody, () => props.thread?.status, toolOutput], () => {
            if (autoExpand()) collapsed.value = false
        }, { immediate: true })
        const toolFailed = computed(() => {
            const output = toolOutput.value
            return output?.content?.includes('Error')
        })
        const toolArgs = computed(() => ctx.utils.toJsonObject(props.tool?.function?.arguments))
        const toolSummary = computed(() => {
            const toolName = props.tool?.function?.name || ''
            const args = toolArgs.value || {}
            const output = toolOutput.value
            if (toolName == 'run_bash' && args.command) {
                return args.command
            }
            else if (toolName == 'skill' && args.name) {
                return args.name
            }
            else if (args.path) {
                if (toolName == 'read_text_file') {
                    return args.path + ' (' + ctx.fmt.humanifyNumber(output?.content?.length || 0) + ')'
                } else if (toolName == 'directory_tree') {
                    const tree = ctx.utils.toJsonObject(output?.content)
                    let dirCount = 0
                    let fileCount = 0
                    const countItems = (items) => {
                        if (!items) return
                        items.forEach(item => {
                            if (item.type == 'file') {
                                fileCount++
                            } else if (item.type == 'directory') {
                                dirCount++
                                countItems(item.children)
                            }
                        })
                    }
                    countItems(tree)
                    return `${args.path} 📁${dirCount} 📄${fileCount}`
                }
                return args.path
            } else if (toolName == 'open' && args.target) {
                return args.target
            } else if (toolName == 'computer') {
                if (args.action) {
                    return args.action
                }
            } else if (toolName.startsWith('run_') && args.code) {
                const firstLine = args.code.split('\n')[0]
                return firstLine
            }
            return ''
        })

        return {
            collapsed,
            toolSummary,
            toolOutput,
            toolFailed,
            toolCallBody,
        }
    }
}

export const UserAvatar = {
    template: `
        <img class="size-8 rounded-full shadow" :class="[$styles.messageUser.replace('rounded-none', '')]" :src="$ctx.getUserAvatar()" />
    `
}

export const AgentAvatar = {
    template: `
        <img class="size-8 rounded-full shadow" :class="[$styles.messageAssistant.replace('rounded-none', '')]" :src="$ctx.getProfileAvatar(profile)" />
    `,
    props: {
        profile: String
    },
}

export const ChatBody = {
    template: `
        <div class="flex flex-col h-full">
            <!-- Messages Area -->
            <div id="messages" class="flex-1 overflow-y-auto" ref="messagesContainer" @scroll="checkUserScroll">
                <div class="mx-auto max-w-7xl px-4 py-6">

                    <div v-if="!$ai.hasAccess">
                        <SignIn @done="$ai.signIn($event)" />
                    </div>
                    <!-- Welcome message when no thread is selected -->
                    <div v-else-if="!currentThread" class="text-center py-12">
                        <Welcome />
                        <HomeTools />
                    </div>

                    <!-- Messages -->
                    <div v-else-if="currentThread">
                        <button v-if="currentThread.parentId" type="button" @click.stop="$ctx.to('/c/' + currentThread.parentId)"
                            title="Return to previous thread"
                            class="float-left mb-2 p-1.5 rounded-lg transition-colors text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700">
                            <svg class="size-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                <path d="M9 14 4 9l5-5"/>
                                <path d="M4 9h10.5a5.5 5.5 0 0 1 5.5 5.5a5.5 5.5 0 0 1-5.5 5.5H11"/>
                            </svg>
                        </button>
                        <ThreadHeader v-if="currentThread" :thread="currentThread" class="mb-2" />
                        <div class="space-y-2" v-if="currentThread?.messages?.length">
                            <div
                                v-for="message in currentThreadMessages"
                                :key="message.timestamp"
                                v-show="message.role !== 'tool' && !!(message.content || message.reasoning || message.thinking || message.reasoning_content || message.tool_calls?.length || message.images?.length || message.audios?.length)"
                                :data-role="message.role"
                                :data-has-content="!!(typeof message.content === 'string' ? message.content?.trim() : message.content?.length)"
                                :data-has-tools="!!message.tool_calls?.length"
                                :data-tool-call-id="message.tool_call_id || undefined"
                                class="flex items-start space-x-3 group"
                                :class="message.role === 'user' ? 'flex-row-reverse space-x-reverse' : ''"
                            >
                                <!-- Avatar outside the bubble -->
                                <div class="flex-shrink-0 flex flex-col justify-center">
                                    <UserAvatar v-if="message.role === 'user'" />
                                    <AgentAvatar v-else :profile="currentThread?.metadata?.profile" />

                                    <!-- Delete button (shown on hover) -->
                                    <button type="button" @click.stop="$threads.deleteMessageFromThread(currentThread.id, message.timestamp)"
                                        class="p-1 mx-auto opacity-0 group-hover:opacity-100 mt-2 rounded hover:text-red-600 dark:hover:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/30 transition-all"
                                        :class="$styles.mutedIcon"
                                        title="Delete message">
                                        <svg class="size-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path>
                                        </svg>
                                    </button>
                                </div>

                                <!-- Message bubble -->
                                <div v-if="message.role === 'assistant' && !message.content?.trim() && !message.reasoning && !message.thinking && !message.reasoning_content && message.tool_calls && message.tool_calls.length > 0 && !message.images?.length && !message.audios?.length">

                                    <div v-if="message.tool_calls && message.tool_calls.length > 0" class="mb-3 space-y-4">
                                        <ToolCall v-for="(tool, i) in message.tool_calls" :key="i" :thread="currentThread" :tool="tool" />
                                    </div>
                                </div>
                                <div v-else
                                    class="message rounded-lg px-4 py-3 relative group"
                                    :class="message.role === 'user'
                                        ? $styles.messageUser
                                        : $styles.messageAssistant"
                                >
                                    <!-- Copy button in top right corner -->
                                    <button v-if="message.content"
                                        type="button"
                                        @click="copyMessageContent(message)"
                                        class="absolute top-2 right-2 opacity-0 group-hover:opacity-100 transition-opacity duration-200 p-1 rounded hover:bg-black/10 dark:hover:bg-white/10 focus:outline-none focus:ring-0"
                                        :class="[$styles.mutedIcon, $styles.mutedIconHover]"
                                        title="Copy message content"
                                    >
                                        <svg v-if="copying === message" class="size-4 text-green-500 dark:text-green-400" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg>
                                        <svg v-else class="size-4" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                            <rect width="14" height="14" x="8" y="8" rx="2" ry="2"/>
                                            <path d="M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2"/>
                                        </svg>
                                    </button>

                                    <div
                                        v-if="message.role === 'assistant'"
                                        v-html="$fmt.markdown(message.content)"
                                        class="prose prose-sm max-w-none dark:prose-invert"
                                    ></div>

                                    <!-- Collapsible reasoning section -->
                                    <MessageReasoning v-if="message.role === 'assistant' && (message.reasoning || message.thinking || message.reasoning_content)" 
                                        :reasoning="message.reasoning || message.thinking || message.reasoning_content" :message="message" />

                                    <!-- Tool Calls & Outputs -->
                                    <div v-if="message.tool_calls && message.tool_calls.length > 0" class="mb-3 space-y-4">
                                        <ToolCall v-for="(tool, i) in message.tool_calls" :key="i" :thread="currentThread" :tool="tool" />
                                    </div>

                                    <!-- Tool Output (Orphaned) -->
                                    <div v-if="message.role === 'tool' && !isToolLinked(message)" class="text-sm">
                                        <div class="flex items-center gap-2 mb-1 opacity-70">
                                            <div class="flex items-center text-xs font-mono font-medium text-gray-500 uppercase tracking-wider">
                                                <svg class="size-3 mr-1" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M5 12h14M12 5l7 7-7 7"/></svg>
                                                Tool Output
                                            </div>
                                            <div v-if="message.name" class="text-xs font-mono bg-gray-200 dark:bg-gray-700 px-1.5 rounded text-gray-700 dark:text-gray-300">
                                                {{ message.name }}
                                            </div>
                                            <div v-if="message.tool_call_id" class="text-[10px] font-mono text-gray-400">
                                                {{ message.tool_call_id.slice(0,8) }}
                                            </div>
                                        </div>
                                        <div class="not-prose bg-white dark:bg-gray-900 rounded border border-gray-200 dark:border-gray-800 p-2 overflow-x-auto">
                                            <pre class="tool-output">{{ message.content }}</pre>
                                        </div>
                                    </div>

                                    <!-- Assistant Images -->
                                    <div v-if="message.images && message.images.length > 0" class="mt-2 flex flex-wrap gap-2">
                                        <template v-for="(img, i) in message.images" :key="i">
                                            <TypeImage v-if="img.type === 'image_url'" :image="img" />
                                        </template>
                                    </div>

                                    <!-- Assistant Audios -->
                                    <div v-if="message.audios && message.audios.length > 0" class="mt-2 flex flex-wrap gap-2">
                                        <template v-for="(audio, i) in message.audios" :key="i">
                                            <TypeAudio v-if="audio.type === 'audio_url' || audio.type === 'input_audio'" :audio="audio" 
                                               class="flex items-center gap-2 p-2 rounded-lg border border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800">
                                                <svg class="w-5 h-5 text-gray-500" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M9 18V5l12-2v13"></path><circle cx="6" cy="18" r="3"></circle><circle cx="18" cy="16" r="3"></circle></svg>
                                            </TypeAudio>
                                        </template>
                                    </div>

                                    <!-- User Message with separate attachments -->
                                    <div v-else-if="message.role !== 'assistant' && message.role !== 'tool'">
                                        <div v-html="$fmt.content(message.content)" class="prose prose-sm max-w-none dark:prose-invert break-words"></div>
                                        <ViewTypes :results="getAttachments(message)" />
                                    </div>

                                    <MessageUsage :message="message" :usage="getMessageUsage(message)" />
                                </div>

                                <!-- Edit and Redo buttons (shown on hover for user messages, outside bubble) -->
                                <div v-if="message.role === 'user'" class="flex flex-col gap-2 opacity-0 group-hover:opacity-100 transition-opacity mt-1">
                                    <button type="button" @click.stop="editMessage(message)"
                                        class="whitespace-nowrap text-xs px-2 py-1 rounded hover:text-green-600 dark:hover:text-green-400 hover:bg-green-50 dark:hover:bg-green-900/30 transition-all"
                                        :class="$styles.mutedIcon"
                                        title="Edit message">
                                        <svg class="size-4 inline mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"></path>
                                        </svg>
                                        Edit
                                    </button>
                                    <button type="button" @click.stop="redoMessage(message)"
                                        class="whitespace-nowrap text-xs px-2 py-1 rounded hover:text-blue-600 dark:hover:text-blue-400 hover:bg-blue-50 dark:hover:bg-blue-900/30 transition-all"
                                        :class="$styles.mutedIcon"
                                        title="Redo message (clears all responses after this message and re-runs it)">
                                        <svg class="size-4 inline mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"></path>
                                        </svg>
                                        Redo
                                    </button>
                                </div>
                            </div>

                            <div v-if="currentThread.stats && currentThread.stats.outputTokens" class="text-center text-sm" :class="[$styles.muted]">
                                <span :title="$fmt.statsTitle(currentThread.stats)">
                                    {{ currentThread.stats.cost ? $fmt.costLong(currentThread.stats.cost) + '  for ' : '' }} {{ $fmt.humanifyNumber(currentThread.stats.inputTokens) }} → {{ $fmt.humanifyNumber(currentThread.stats.outputTokens) }} tokens over {{ currentThread.stats.requests }} request{{currentThread.stats.requests===1?'':'s'}} in {{ $fmt.humanifyMs(currentThread.stats.duration * 1000) }} <span v-if="currentThread.stats.outputTokens > 0 && currentThread.stats.duration > 0">({{ Math.round(currentThread.stats.outputTokens / currentThread.stats.duration) }} tk/s)</span>
                                </span>
                                <CompactThreadButton :currentThread="currentThread" />
                            </div>

                            <!-- Loading indicator -->
                            <div v-if="$threads.watchingThread" class="flex items-center space-x-3 group">
                                <!-- Avatar outside the bubble -->
                                <div class="flex-shrink-0">
                                    <AgentAvatar :profile="currentThread?.metadata?.profile" />
                                </div>

                                <!-- Cancel button -->
                                <button type="button" @click="$threads.cancelThread()"
                                    class="px-3 py-1 rounded text-sm hover:text-red-600 dark:hover:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/30 border border-transparent hover:border-red-300 dark:hover:border-red-600 transition-all"
                                    :class="$styles.muted"
                                    title="Cancel request">
                                    cancel
                                </button>

                                <!-- Loading bubble -->
                                <div class="" :class="[$styles.muted]">
                                    <div class="flex space-x-1">
                                        <div class="w-2 h-2 rounded-full animate-bounce" :class="[$styles.bgIcon]"></div>
                                        <div class="w-2 h-2 rounded-full animate-bounce" :class="[$styles.bgIcon]" style="animation-delay: 0.1s"></div>
                                        <div class="w-2 h-2 rounded-full animate-bounce" :class="[$styles.bgIcon]" style="animation-delay: 0.2s"></div>
                                    </div>
                                </div>

                                <span :class="['text-sm', $styles.muted]">{{currentThread?.status}}</span>
                            </div>

                            <!-- Thread error message bubble -->
                            <div v-if="currentThread?.error" class="mt-8 flex items-center">
                                <!-- Avatar outside the bubble -->
                                <div class="flex-shrink-0">
                                    <div class="size-8 rounded-full bg-red-600 dark:bg-red-500 text-white flex items-center justify-center text-lg font-bold">
                                        !
                                    </div>
                                </div>
                                <!-- Error bubble -->
                                <div class="ml-3 max-w-[85%] rounded-lg px-3 py-1 bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 text-red-800 dark:text-red-200 shadow-sm">
                                    <div class="flex items-start space-x-2">
                                        <div class="flex-1 min-w-0">
                                            <div v-if="currentThread.error" class="text-base mb-1">{{ currentThread.error }}</div>
                                        </div>
                                    </div>
                                </div>
                                <button type="button" @click="$chat.sendUserMessage('retry')" title="Retry request"
                                    class="ml-1 px-3 py-1 rounded transition-all" :class="[$styles.muted,$styles.mutedHover]">
                                    retry
                                </button>
                            </div>

                            <ErrorBubble />
                        </div>
                        <div v-else>
                            <ErrorBubble />
                        </div>
                        <ThreadFooter v-if="!$threads.watchingThread && $threads.threadDetails.value[currentThread.id]" :thread="$threads.threadDetails.value[currentThread.id]" />
                    </div>
                </div>
            </div>

            <!-- Input Area -->
            <div v-if="$ai.hasAccess" :class="$ctx.cls('chat-input', 'flex-shrink-0 px-6 py-4 border-t ' + $styles.chromeBorder + ' ' + $styles.bgChat)">
                <ChatPrompt :model="$chat.getSelectedModel()" />
            </div>
        </div>
    `,
    setup() {
        const ctx = inject('ctx')
        const models = ctx.state.models
        const config = ctx.state.config
        const threads = ctx.threads
        const chatPrompt = ctx.chat
        const { currentThread } = threads

        const router = useRouter()
        const route = useRoute()

        const prefs = ref(ctx.getPrefs())

        const selectedModel = ref(prefs.value.model || config.defaults.text.model || '')
        const selectedModelObj = computed(() => {
            if (!selectedModel.value || !models) return null
            return models.find(m => m.name === selectedModel.value) || models.find(m => m.id === selectedModel.value)
        })
        const messagesContainer = ref(null)
        const copying = ref(null)

        const resolveUrl = (url) => {
            if (url && url.startsWith('~')) {
                return '/' + url
            }
            return ctx.ai.resolveUrl(url)
        }

        // Auto-scroll to bottom as content streams or new messages arrive
        const isUserScrolledUp = ref(false)

        const checkUserScroll = () => {
            if (!messagesContainer.value) return
            const { scrollTop, scrollHeight, clientHeight } = messagesContainer.value
            isUserScrolledUp.value = scrollHeight - (scrollTop + clientHeight) > 100
        }

        const scrollToBottom = async (force = false) => {
            await nextTick()
            if (messagesContainer.value && (force || !isUserScrolledUp.value)) {
                messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight
            }
        }

        // Reset user scroll state when thread changes
        watch(() => currentThread.value?.id, () => {
            isUserScrolledUp.value = false
            scrollToBottom(true)
        })

        // Watch for new messages, content stream updates, and thread updatedAt to auto-scroll
        watch(
            [
                () => currentThread.value?.messages?.length,
                () => currentThread.value?.updatedAt,
                () => {
                    const msgs = currentThread.value?.messages
                    if (!msgs || !msgs.length) return ''
                    const last = msgs[msgs.length - 1]
                    return (last?.content || '') + (last?.reasoning || '') + (last?.thinking || '') + (last?.reasoning_content || '')
                }
            ],
            () => {
                scrollToBottom()
            }
        )

        // Watch for route changes and load the appropriate thread
        watch(() => route.params.id, async (newId) => {
            // console.debug('watch route.params.id', newId)
            ctx.clearError()
            threads.setCurrentThreadFromRoute(newId, router)

            if (!newId) {
                chatPrompt.reset()
            }
            nextTick(ctx.chat.addCopyButtons)
        }, { immediate: true })

        watch(() => [selectedModel.value], () => {
            ctx.setPrefs({
                model: selectedModel.value,
            })
        })
        function configUpdated() {
            console.log('configUpdated', selectedModel.value, models.length, models.includes(selectedModel.value))
            if (selectedModel.value && !models.includes(selectedModel.value)) {
                selectedModel.value = config.defaults.text.model || ''
            }
        }

        const copyMessageContent = async (message) => {
            let content = ''
            if (Array.isArray(message.content)) {
                content = message.content.map(part => {
                    if (part.type === 'text') return part.text
                    if (part.type === 'image_url') {
                        const name = part.image_url.url.split('/').pop() || 'image'
                        return `\n![${name}](${part.image_url.url})\n`
                    }
                    if (part.type === 'input_audio') {
                        const name = part.input_audio.data.split('/').pop() || 'audio'
                        return `\n[${name}](${part.input_audio.data})\n`
                    }
                    if (part.type === 'file') {
                        const name = part.file.filename || part.file.file_data.split('/').pop() || 'file'
                        return `\n[${name}](${part.file.file_data})`
                    }
                    return ''
                }).join('\n')
            } else {
                content = message.content
            }

            try {
                copying.value = message
                await navigator.clipboard.writeText(content)
                // Could add a toast notification here if desired
            } catch (err) {
                console.error('Failed to copy message content:', err)
                // Fallback for older browsers
                const textArea = document.createElement('textarea')
                textArea.value = content
                document.body.appendChild(textArea)
                textArea.select()
                document.execCommand('copy')
                document.body.removeChild(textArea)
            }
            setTimeout(() => { copying.value = null }, 2000)
        }

        const getAttachments = (message) => {
            if (!Array.isArray(message.content)) return []
            return message.content.filter(c => c.type === 'image_url' || c.type === 'input_audio' || c.type === 'file')
        }
        const hasAttachments = (message) => getAttachments(message).length > 0

        // Helper to extract content and files from message
        const extractMessageState = async (message) => {
            let text = ''
            let files = []
            const getCacheInfos = []

            if (Array.isArray(message.content)) {
                for (const part of message.content) {
                    if (part.type === 'text') {
                        text += part.text
                    } else if (part.type === 'image_url') {
                        const url = part.image_url.url
                        const name = url.split('/').pop() || 'image'
                        files.push({ name, url, type: 'image/png' }) // Assume image
                        getCacheInfos.push(url)
                    } else if (part.type === 'input_audio') {
                        const url = part.input_audio.data
                        const name = url.split('/').pop() || 'audio'
                        files.push({ name, url, type: 'audio/wav' }) // Assume audio
                        getCacheInfos.push(url)
                    } else if (part.type === 'file') {
                        const url = part.file.file_data
                        const name = part.file.filename || url.split('/').pop() || 'file'
                        files.push({ name, url })
                        getCacheInfos.push(url)
                    }
                }
            } else {
                text = message.content
            }

            const infos = await ctx.ai.fetchCacheInfos(getCacheInfos)
            // replace name with info.name
            for (let i = 0; i < files.length; i++) {
                const url = files[i]?.url
                const info = infos[url]
                if (info) {
                    files[i].name = info.name
                }
            }

            return { text, files }
        }

        // Redo a user message (clear all messages after this one and re-run)
        const redoMessage = async (message) => {
            if (!currentThread.value || message.role !== 'user') return

            const threadId = currentThread.value.id

            // Clear all messages after this one
            await threads.redoMessageFromThread(threadId, message.timestamp)

            const state = await extractMessageState(message)

            // Set the message text in the chat prompt
            chatPrompt.messageText.value = state.text

            // Restore attached files
            chatPrompt.attachedFiles.value = state.files
        }

        // Edit a user message
        const editMessage = async (message) => {
            if (!currentThread.value || message.role !== 'user') return

            // set the message in the input box
            const state = await extractMessageState(message)
            chatPrompt.messageText.value = state.text
            chatPrompt.attachedFiles.value = state.files
            chatPrompt.editingMessage.value = message.timestamp

            // Focus the textarea
            nextTick(() => {
                const textarea = document.querySelector('textarea')
                if (textarea) {
                    textarea.focus()
                    // Set cursor to end
                    textarea.selectionStart = textarea.selectionEnd = textarea.value.length
                }
            })
        }

        let sub
        onMounted(() => setTimeout(ctx.chat.addCopyButtons, 1))
        onUnmounted(() => sub?.unsubscribe())

        const getToolOutput = (toolCallId) => {
            return currentThread.value?.messages?.find(m => m.role === 'tool' && m.tool_call_id === toolCallId)
        }

        const getMessageUsage = (message) => {
            let usage = message.usage
            if (!usage && message.tool_calls?.length) {
                const toolUsages = message.tool_calls.map(tc => getToolOutput(tc.id)?.usage)
                usage = {
                    tokens: toolUsages.reduce((a, b) => a + (b?.tokens || 0), 0),
                    cost: toolUsages.reduce((a, b) => a + (b?.cost || 0), 0),
                    duration: toolUsages.reduce((a, b) => a + (b?.duration || 0), 0)
                }
            }
            if (usage && !usage.tokens && (message.content || message.reasoning)) {
                const text = (message.content || '') + (message.reasoning || '')
                const estTokens = Math.max(1, Math.round(text.length / 4))
                usage = { ...usage, tokens: estTokens }
            }
            return usage
        }

        const isToolLinked = (message) => {
            if (message.role !== 'tool') return false
            return currentThread.value?.messages?.some(m => m.role === 'assistant' && m.tool_calls?.some(tc => tc.id === message.tool_call_id))
        }

        function setPrefs(o) {
            Object.assign(prefs.value, o)
            ctx.setPrefs(prefs.value)
        }

        const ignoreUserMessages = ['proceed', 'retry']
        const currentThreadMessages = computed(() =>
            currentThread.value?.messages?.filter(x => x.role !== 'system' && !(x.role === 'user' && Array.isArray(x.content) && ignoreUserMessages.includes(x.content[0]?.text))))

        const getBottomLines = (text, maxLines = 2) => {
            if (!text || typeof text !== 'string') return ''
            const lines = text.trim().split('\n').filter(line => line.trim().length > 0)
            if (lines.length === 0) return ''
            return lines.slice(-maxLines).join(' ')
        }

        const activeReasoningProgress = computed(() => {
            const thread = currentThread.value
            if (!thread || !thread.messages || !thread.messages.length) return thread?.status || null
            const last = thread.messages[thread.messages.length - 1]
            if (last && last.role === 'assistant') {
                const reasoning = last.reasoning || last.thinking || last.reasoning_content
                const hasContent = last.content && String(last.content).trim().length > 0
                if (reasoning && !hasContent) {
                    const reasoningStr = typeof reasoning === 'string' ? reasoning : JSON.stringify(reasoning)
                    const bottomText = getBottomLines(reasoningStr, 2)
                    return bottomText ? `Thinking: ${bottomText}` : 'Thinking...'
                }
            }
            return thread?.status || null
        })

        return {
            prefs,
            setPrefs,
            config,
            models,
            currentThread,
            currentThreadMessages,
            activeReasoningProgress,
            selectedModel,
            selectedModelObj,
            messagesContainer,
            checkUserScroll,
            scrollToBottom,
            copying,
            copyMessageContent,
            redoMessage,
            editMessage,
            configUpdated,
            getAttachments,
            hasAttachments,
            resolveUrl,
            getMessageUsage,
            isToolLinked,
            tryParseJson,
            hasJsonStructure,
        }
    }
}
