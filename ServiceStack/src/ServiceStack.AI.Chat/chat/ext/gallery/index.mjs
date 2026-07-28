import { ref, watch, computed, inject, onMounted, onUnmounted } from "vue"

let ext

function useGallery(ext) {
    const ctx = ext.ctx

    const lightboxFooters = {}
    const audioActions = {}

    function setLightboxFooters(components) {
        Object.assign(lightboxFooters, components)
    }

    function setAudioActions(components) {
        Object.assign(audioActions, components)
    }

    return {
        lightboxFooters,
        audioActions,
        setLightboxFooters,
        setAudioActions,
    }
}

const GalleryPage = {
    template: `
        <div class="w-full max-w-[1600px] mx-auto p-4 md:p-8 text-gray-900 dark:text-gray-200 font-sans selection:bg-blue-500/30">
            
            <!-- Header -->
            <div class="flex flex-col md:flex-row justify-between items-center mb-8 gap-4">
                
                <!-- Left: Tabs -->
                <div class="flex gap-x-2 p-1.5 self-start md:self-auto" :class="[$styles.tagButtonGroup]">
                    <button type="button"
                        @click="setFilter('image')"
                        class="px-6 py-2 font-medium text-sm"
                        :class="[ext.prefs.type === 'image' 
                            ? $styles.tagButtonActive 
                            : $styles.tagButton,$styles.tagButtonLarge]"
                    >
                        Images
                    </button>
                    <button type="button"
                        @click="setFilter('audio')"
                        class="px-6 py-2 font-medium text-sm"
                        :class="[ext.prefs.type === 'audio' 
                            ? $styles.tagButtonActive 
                            : $styles.tagButton,$styles.tagButtonLarge]"
                    >
                        Audio
                    </button>
                </div>

                <!-- Center: Format Filter -->
                <div v-if="ext.prefs.type === 'image'" class="flex justify-between w-full md:w-auto gap-2">
                    <button type="button"
                        v-for="fmt in formats" 
                        :key="fmt.id"
                        @click="setFormat(fmt.id)"
                        class="p-2 flex flex-col items-center gap-1 min-w-[4.5rem]"
                        :class="[ext.prefs.format === fmt.id 
                            ? $styles.tagButtonActive 
                            : $styles.tagButton,$styles.tagButtonLarge]"
                    >
                        <span v-html="fmt.icon" class="w-5 h-5"></span>
                        <span class="text-[10px] font-medium uppercase tracking-wider">{{ fmt.label }}</span>
                    </button>
                </div>

                <!-- Right: Search -->
                <div class="relative group w-full md:w-72">
                    <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                        <svg class="h-4 w-4 group-focus-within:text-blue-500 dark:group-focus-within:text-blue-400 transition-colors" :class="[$styles.muted]" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                        </svg>
                    </div>
                    <input 
                        type="text" 
                        class="block w-full pl-10 pr-3 py-2.5 rounded-full"
                        :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]"
                        placeholder="Search prompts, models..." 
                        v-model="ext.prefs.q"
                        @input="onSearch"
                    >
                </div>
            </div>

            <!-- Image Grid -->
            <div v-if="ext.prefs.type === 'image'" class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 2xl:grid-cols-5 gap-3">
                <div
                    v-for="(item, index) in items" 
                    :key="item.id" 
                    class="group relative rounded-lg overflow-hidden bg-gray-100 dark:bg-gray-800/30 cursor-pointer border border-gray-200 dark:border-white/5 transition-all duration-300 hover:shadow-xl dark:hover:shadow-2xl hover:shadow-blue-500/10 hover:border-blue-400/50 dark:hover:border-blue-500/30"
                    :class="ext.prefs.format === 'landscape' ? 'aspect-video' : ext.prefs.format === 'square' ? 'aspect-square' : 'aspect-[3/4]'"
                    @click="openLightbox(index)"
                >
                    <img 
                        :src="item.url + '?variant=width=256'" 
                        loading="lazy" 
                        :alt="item.prompt"
                        class="w-full h-full object-cover transition-transform duration-500 group-hover:scale-105"
                    >
                    <div class="absolute inset-0 bg-gradient-to-t from-black/80 via-black/20 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-300 flex flex-col justify-end p-4">
                        <div class="transform translate-y-4 group-hover:translate-y-0 transition-transform duration-300">
                            <div class="text-xs font-bold text-blue-300 mb-1 uppercase tracking-wider">{{ item.model }}</div>
                            <div class="text-xs text-gray-300 font-medium">{{ $fmt.formatDate(item.created) }}</div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Audio List -->
            <div v-if="ext.prefs.type === 'audio'" class="flex flex-col gap-4 max-w-3xl mx-auto">
                <div v-for="(item, index) in items" :key="item.id" class="bg-white dark:bg-gray-800/40 p-4 rounded-2xl border border-gray-200 dark:border-white/5 flex items-center gap-4 hover:border-gray-300 dark:hover:border-gray-700 transition-colors shadow-sm">
                    <div class="flex flex-col items-center gap-2 shrink-0">
                        <div class="w-12 h-12 rounded-full bg-blue-100 dark:bg-blue-500/20 text-blue-600 dark:text-blue-400 flex items-center justify-center shrink-0">
                            <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19V6l12-2v13M9 19c0 1.105-1.343 2-3 2s-3-.895-3-2 1.343-2 3-2 3 .895 3 2zm12-3c0 1.105-1.343 2-3 2s-3-.895-3-2 1.343-2 3-2 3 .895 3 2zM9 10l12-2" />
                            </svg>
                        </div>
                        <button type="button" @click="remixAudio(item)" class="mb-1 px-2 py-0.5 bg-fuchsia-700 text-white border border-fuchsia-600 hover:bg-fuchsia-600 hover:border-fuchsia-400 rounded-full text-[10px] font-bold uppercase tracking-wider shadow-lg shadow-fuchsia-500/10 hover:shadow-fuchsia-500/40 shrink-0">
                            Remix
                        </button>
                    </div>
                    <div class="flex-1 min-w-0">
                        <div class="flex justify-between items-center mb-1">
                            <h3 class="font-medium truncate pr-4" :class="[$styles.heading]" :title="item.caption || item.prompt || ''">
                                {{ item.caption || item.prompt || 'Untitled' }}
                            </h3>
                            <span class="text-xs text-gray-500 shrink-0">{{ $fmt.formatDate(item.created) }}</span>
                        </div>
                        <div class="flex justify-between items-center mb-2">
                            <div class="text-xs text-blue-600 dark:text-blue-300/80">{{ item.model }}</div>

                            <!-- AUDIO ACTIONS -->
                            <div v-if="Object.keys($ctx.gallery.audioActions).length" class="flex items-center gap-1">
                                <div v-for="(component, key) in $ctx.gallery.audioActions" :key="key">
                                    <component :is="component" :item="item" />
                                </div>
                            </div>
                        </div>
                        <div class="flex items-center gap-2">
                            <audio controls class="w-full h-8 opacity-90" :src="item.url"></audio>
                            <button type="button" @click="deleteMedia(item)" class="p-1 text-gray-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-full transition-colors" title="Delete">
                                <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                            </button>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Loading State -->
            <div class="h-20 flex items-center justify-center mt-8" :class="[$styles.muted]" ref="loadingTrigger">
                <div v-if="loading" class="flex items-center gap-3">
                    <div class="w-2 h-2 bg-[var(--assistant-bg)] rounded-full animate-bounce [animation-delay:-0.3s]"></div>
                    <div class="w-2 h-2 bg-[var(--assistant-bg)] rounded-full animate-bounce [animation-delay:-0.15s]"></div>
                    <div class="w-2 h-2 bg-[var(--assistant-bg)] rounded-full animate-bounce"></div>
                </div>
                <div v-else-if="allLoaded && items.length > 0" class="text-sm font-medium opacity-50">
                    All caught up
                </div>
                 <div v-else-if="allLoaded && items.length === 0" class="flex flex-col items-center gap-2 opacity-50 py-12">
                    <svg class="w-12 h-12" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" /></svg>
                    <span>No media found</span>
                </div>
            </div>

            <!-- Lightbox -->
            <transition 
                enter-active-class="transition ease-out duration-300"
                enter-from-class="opacity-0"
                enter-to-class="opacity-100"
                leave-active-class="transition ease-in duration-200"
                leave-from-class="opacity-100"
                leave-to-class="opacity-0"
            >
                <div v-if="lightboxItem" class="fixed inset-0 z-100 flex bg-white/95 dark:bg-black/95 backdrop-blur-xl" @click.self="closeLightbox" @keydown.esc="closeLightbox" tabindex="0">
                    
                    <!-- Main Content -->
                    <div class="flex-1 relative flex items-center justify-center p-4">

                        <button type="button" class="absolute top-4 right-4 z-50 p-2 text-gray-500 hover:text-gray-900 dark:text-gray-400 dark:hover:text-white bg-gray-100 hover:bg-gray-200 dark:bg-black/50 dark:hover:bg-white/10 rounded-full transition-all" @click="closeLightbox">
                            <svg class="w-8 h-8" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg>
                        </button>

                        <button v-if="hasPrev" type="button" class="hidden md:flex absolute left-4 top-1/2 -translate-y-1/2 p-3 text-gray-700 dark:text-white bg-white/80 dark:bg-white/10 hover:bg-white dark:hover:bg-white/20 hover:scale-110 rounded-full backdrop-blur-md transition-all border border-gray-200 dark:border-white/5 shadow-lg" @click.stop="prevItem">
                            <svg class="w-8 h-8" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" /></svg>
                        </button>

                        <img :src="lightboxItem.url" class="max-w-full max-h-[90vh] object-contain shadow-2xl rounded-sm" @click.stop>

                        <button v-if="hasNext" type="button" class="hidden md:flex absolute right-4 top-1/2 -translate-y-1/2 p-3 text-gray-700 dark:text-white bg-white/80 dark:bg-white/10 hover:bg-white dark:hover:bg-white/20 hover:scale-110 rounded-full backdrop-blur-md transition-all border border-gray-200 dark:border-white/5 shadow-lg" @click.stop="nextItem">
                            <svg class="w-8 h-8" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" /></svg>
                        </button>
                    </div>

                    <!-- Sidebar -->
                    <div class="w-full md:w-[400px] h-full bg-gray-50 dark:bg-[#111111] border-l border-gray-200 dark:border-white/10 flex flex-col shadow-2xl text-gray-900 dark:text-gray-200">
                        <div class="p-6 overflow-y-auto custom-scrollbar flex-1 space-y-8">
                            
                            <!-- Model Badge -->
                            <div>
                                <h3 class="text-xs uppercase tracking-widest text-gray-500 font-semibold mb-2">Generated With</h3>
                                <div class="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-blue-100 dark:bg-blue-500/10 border border-blue-200 dark:border-blue-500/20 text-blue-600 dark:text-blue-400 text-sm font-medium">
                                    <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z" /></svg>
                                    {{ lightboxItem.model }}
                                </div>
                            </div>

                            <!-- Prompt -->
                            <div v-if="lightboxItem.prompt">
                                <div class="flex justify-between">
                                    <h3 class="text-xs uppercase tracking-widest text-gray-500 font-semibold mb-3">Prompt</h3>
                                    <button type="button" @click="remixImage" class="mb-2 px-3 py-1 bg-fuchsia-700 text-white border border-fuchsia-600 hover:bg-fuchsia-600 hover:border-fuchsia-400 rounded-full text-xs font-bold uppercase tracking-wider shadow-lg shadow-fuchsia-500/10 hover:shadow-fuchsia-500/40">
                                        Remix
                                    </button>
                                </div>                            
                                <div class="bg-white dark:bg-gray-900/50 p-4 rounded-xl border border-gray-200 dark:border-white/5 text-gray-600 dark:text-gray-300 text-sm leading-relaxed font-mono max-h-60 overflow-y-auto custom-scrollbar shadow-inner">
                                    {{ lightboxItem.prompt }}
                                </div>
                            </div>

                            <!-- Parameters -->
                            <div v-if="lightboxItem.params && Object.keys(lightboxItem.params).length">
                                <h3 class="text-xs uppercase tracking-widest text-gray-500 font-semibold mb-3">Parameters</h3>
                                <div class="flex flex-wrap gap-2">
                                    <span v-for="(val, key) in lightboxItem.params" :key="key" class="px-2.5 py-1 bg-white dark:bg-gray-800 rounded-md text-xs text-gray-600 dark:text-gray-300 border border-gray-200 dark:border-white/5 shadow-sm">
                                        <span class="text-gray-400 dark:text-gray-500 mr-1">{{key}}:</span> {{val}}
                                    </span>
                                </div>
                            </div>

                            <!-- Details Grid -->
                            <div>
                                <h3 class="text-xs uppercase tracking-widest text-gray-500 font-semibold mb-3">Details</h3>
                                <div class="grid grid-cols-2 gap-4 text-sm">
                                    <div class="bg-white dark:bg-gray-800/20 p-3 rounded-lg border border-gray-200 dark:border-white/5">
                                        <div class="text-gray-500 text-xs mb-1">Dimensions</div>
                                        <div class="font-mono text-gray-700 dark:text-gray-300">{{ lightboxItem.width }} × {{ lightboxItem.height }}</div>
                                    </div>
                                    <div class="bg-white dark:bg-gray-800/20 p-3 rounded-lg border border-gray-200 dark:border-white/5">
                                        <div class="text-gray-500 text-xs mb-1">File Size</div>
                                        <div class="font-mono text-gray-700 dark:text-gray-300">{{ $fmt.bytes(lightboxItem.size) }}</div>
                                    </div>
                                    <div class="bg-white dark:bg-gray-800/20 p-3 rounded-lg border border-gray-200 dark:border-white/5">
                                        <div class="text-gray-500 text-xs mb-1">Created</div>
                                        <div class="text-gray-700 dark:text-gray-300">{{ $fmt.shortDate(lightboxItem.created) }}</div>
                                    </div>
                                    <div v-if="lightboxItem.cost" class="bg-white dark:bg-gray-800/20 p-3 rounded-lg border border-gray-200 dark:border-white/5">
                                        <div class="text-gray-500 text-xs mb-1">Cost</div>
                                        <div class="text-green-600 dark:text-green-400 font-mono">$\{{ lightboxItem.cost.toFixed(5) }}</div>
                                    </div>
                                </div>
                            </div>
                            
                            <!-- Footer Components -->
                            <div v-if="Object.keys($ctx.gallery.lightboxFooters).length">
                                <div v-for="(component, key) in $ctx.gallery.lightboxFooters" :key="key">
                                    <component :is="component" :item="lightboxItem" />
                                </div>
                            </div>
                        </div>

                        <!-- Footer Actions -->
                        <div class="p-6 border-t border-gray-200 dark:border-white/5 bg-gray-50 dark:bg-[#161616] flex gap-2">
                            <button type="button" @click="deleteMedia(item)" class="flex items-center justify-center p-3 bg-red-100 dark:bg-red-900/20 text-red-600 dark:text-red-400 font-bold rounded-xl hover:bg-red-200 dark:hover:bg-red-900/40 transition-colors" title="Delete">
                                <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                            </button>
                             <a :href="lightboxItem.url" download class="flex-1 flex items-center justify-center gap-2 bg-gray-900 dark:bg-white text-white dark:text-black font-bold py-3 px-6 rounded-xl hover:bg-gray-800 dark:hover:bg-gray-200 transition-colors shadow-lg shadow-black/5 dark:shadow-white/5">
                                <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" /></svg>
                                Download
                            </a>
                        </div>
                    </div>
                </div>
            </transition>
        </div>
    `,
    setup() {
        const ctx = inject('ctx')
        const items = ref([])
        const loading = ref(false)
        const allLoaded = ref(false)
        const lightboxIndex = ref(-1)
        const loadingTrigger = ref(null)

        const PAGE_SIZE = 50
        let observer = null
        let searchTimeout = null

        async function loadMedia({ reset } = {}) {
            if (loading.value) return
            ext.savePrefs()
            const skip = reset ? 0 : items.value.length
            if (reset) {
                allLoaded.value = false
            }
            if (allLoaded.value) return

            loading.value = true
            try {
                const params = new URLSearchParams({
                    type: ext.prefs.type,
                    sort: '-id',
                    skip,
                    take: PAGE_SIZE,
                })
                if (ext.prefs.q) {
                    params.append('q', ext.prefs.q)
                }
                if (ext.prefs.format && ext.prefs.type !== 'audio') {
                    params.append('format', ext.prefs.format)
                }

                // USE ext.getJson AS REQUESTED
                const api = await ext.getJson(`/media?${params}`)
                const data = api.response || []

                if (data.length < PAGE_SIZE) {
                    allLoaded.value = true
                }

                const processed = data.map(item => {
                    try {
                        if (typeof item.category === 'string') item.category = JSON.parse(item.category)
                        if (typeof item.tags === 'string') item.tags = JSON.parse(item.tags)
                        if (typeof item.metadata === 'string') item.metadata = JSON.parse(item.metadata)
                    } catch (e) { }

                    return {
                        ...item,
                        params: {
                            ...(item.aspect_ratio ? { aspect: item.aspect_ratio } : {}),
                            ...(item.seed ? { seed: item.seed } : {}),
                        }
                    }
                })

                items.value = reset ? processed : [...items.value, ...processed]
            } catch (e) {
                console.error("Failed to load media", e)
            } finally {
                loading.value = false
            }
        }

        function setFilter(type) {
            ext.setPrefs({ type })
            loadMedia({ reset: true })
        }

        const formats = [
            { id: 'portrait', label: 'Portrait', icon: `<svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 mx-auto" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="5" y="2" width="14" height="20" rx="2" ry="2"></rect></svg>` },
            { id: 'square', label: 'Square', icon: `<svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 mx-auto" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect></svg>` },
            { id: 'landscape', label: 'Landscape', icon: `<svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 mx-auto" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="5" width="20" height="14" rx="2" ry="2"></rect></svg>` },
        ]

        function setFormat(fmt) {
            ext.prefs.format = fmt === ext.prefs.format ? '' : fmt
            loadMedia({ reset: true })
        }

        function onSearch() {
            if (searchTimeout) clearTimeout(searchTimeout)
            searchTimeout = setTimeout(() => {
                loadMedia({ reset: true })
            }, 500)
        }

        onMounted(() => {
            if (!ext.prefs.type) {
                ext.setPrefs({ type: 'image' })
            }
            loadMedia({ reset: true })
            observer = new IntersectionObserver((entries) => {
                if (entries[0].isIntersecting) {
                    loadMedia()
                }
            }, { threshold: 0.1 })
            if (loadingTrigger.value) observer.observe(loadingTrigger.value)
            window.addEventListener('keydown', handleKeydown)
        })

        onUnmounted(() => {
            if (observer) observer.disconnect()
            window.removeEventListener('keydown', handleKeydown)
        })

        watch(loadingTrigger, (el) => {
            if (el && observer) observer.observe(el)
        })

        const lightboxItem = computed(() => {
            return lightboxIndex.value >= 0 ? items.value[lightboxIndex.value] : null
        })

        const hasNext = computed(() => lightboxIndex.value < items.value.length - 1)
        const hasPrev = computed(() => lightboxIndex.value > 0)

        function openLightbox(index) {
            lightboxIndex.value = index
            document.body.style.overflow = 'hidden'
        }

        function closeLightbox() {
            lightboxIndex.value = -1
            document.body.style.overflow = ''
        }

        function nextItem() {
            if (hasNext.value) lightboxIndex.value++
        }

        function prevItem() {
            if (hasPrev.value) lightboxIndex.value--
        }

        function handleKeydown(e) {
            if (lightboxIndex.value === -1) return
            if (e.key === 'ArrowRight') nextItem()
            if (e.key === 'ArrowLeft') prevItem()
            if (e.key === 'Escape') closeLightbox()
        }

        function remixImage() {
            const selected = lightboxItem.value
            closeLightbox()
            ctx.chat.setSelectedModel(ctx.chat.getModel(selected.model))
            ctx.chat.messageText.value = selected.prompt
            ctx.chat.selectAspectRatio(selected.aspect_ratio)
            ctx.threads.startNewThread({
                title: selected.prompt,
                redirect: true,
            })
        }

        function remixAudio(item) {
            const selected = item || lightboxItem.value
            if (lightboxItem.value) closeLightbox()

            ctx.chat.setSelectedModel(ctx.chat.getModel(selected.model))
            ctx.chat.messageText.value = selected.prompt
            ctx.threads.startNewThread({
                title: selected.prompt,
            })
        }

        async function deleteMedia(item) {
            const target = item && item.hash ? item : lightboxItem.value
            console.log('deleteMedia', item, target)
            if (!target) return

            if (!confirm('Are you sure you want to delete this media?')) return

            const hash = target.hash
            try {
                const response = await fetch(`${ext.baseUrl}/media/${hash}`, {
                    method: 'DELETE'
                })
                if (response.ok) {
                    items.value = items.value.filter(item => item.hash !== hash)
                    if (lightboxItem.value && lightboxItem.value.hash === hash) {
                        closeLightbox()
                    }
                } else {
                    console.error("Failed to delete media", response)
                    alert("Failed to delete media")
                }
            } catch (e) {
                console.error("Error deleting media", e)
                alert("Error deleting media")
            }
        }

        return {
            ext,
            items,
            loading,
            allLoaded,
            loadingTrigger,
            formats,
            setFilter,
            setFormat,
            onSearch,
            loadMedia,
            lightboxIndex,
            lightboxItem,
            openLightbox,
            closeLightbox,
            nextItem,
            prevItem,
            hasNext,
            hasPrev,
            remixImage,
            remixAudio,
            deleteMedia,
        }
    }
}

export default {
    order: 40 - 100,

    install(ctx) {
        ext = ctx.scope('gallery')

        ctx.setGlobals({
            gallery: useGallery(ext)
        })

        ctx.components({
            GalleryPage,
        })

        ctx.setLeftIcons({
            gallery: {
                component: {
                    template: `<svg @click="$ctx.togglePath('/gallery')" viewBox="0 0 15 15"><path fill="currentColor" d="M10.71 3L7.85.15a.5.5 0 0 0-.707-.003L7.14.15L4.29 3H1.5a.5.5 0 0 0-.5.5v9a.5.5 0 0 0 .5.5h12a.5.5 0 0 0 .5-.5v-9a.5.5 0 0 0-.5-.5zM7.5 1.21L9.29 3H5.71zM13 12H2V4h11zM5 7a1 1 0 1 1 0-2a1 1 0 0 1 0 2m7 4H4.5L6 8l1.25 2.5L9.5 6z"/></svg>`,
                },
                isActive({ path }) {
                    return ctx.matchesPath(path, '/gallery')
                }
            }
        })

        ctx.routes.push({ path: '/gallery', component: GalleryPage, meta: { title: `Gallery` } })
    }
}