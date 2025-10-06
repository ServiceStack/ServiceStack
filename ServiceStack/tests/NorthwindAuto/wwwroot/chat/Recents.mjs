import { ref, onMounted, watch, inject } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useThreadStore } from './threadStore.mjs'
import { renderMarkdown } from './markdown.mjs'

const RecentResults = {
    template:`
        <div class="flex-1 overflow-y-auto" @scroll="onScroll">
            <div class="mx-auto max-w-6xl px-4 py-4">
                <div class="text-sm text-gray-600 mb-3" v-if="threads.length">
                    <span v-if="q">{{ filtered.length }} result{{ filtered.length===1?'':'s' }}</span>
                    <span v-else>Searching {{ threads.length }} conversation{{ threads.length===1?'':'s' }}</span>
                </div>

                <div v-if="!threads.length" class="text-gray-500">No conversations yet.</div>

                <table class="w-full">
                    <tbody>
                        <tr v-for="t in displayed" :key="t.id" class="hover:bg-gray-50">
                            <td class="py-3 px-1 border-b border-gray-200 max-w-3xl">
                                <button type="button" @click="open(t.id)" class="w-full text-left">
                                    <div class="flex items-start justify-between gap-3">
                                        <div class="min-w-0 flex-1">
                                            <div class="font-medium text-gray-900 truncate" :title="t.title">{{ t.title || 'Untitled chat' }}</div>
                                            <div class="mt-1 text-sm text-gray-600 line-clamp-2">
                                                <div v-html="snippet(t)"></div>
                                            </div>
                                        </div>
                                    </div>
                                </button>
                            </td>
                            <td class="py-3 px-1 border-b border-gray-200">
                                <div class="text-right whitespace-nowrap">
                                    <div class="text-xs text-gray-500">{{ formatDate(t.updatedAt || t.createdAt) }}</div>
                                    <div class="text-[11px] text-gray-500/80">{{ (t.messages?.length || 0) }} messages</div>
                                    <div v-if="t.model" class="text-[11px] text-blue-600">{{ t.model }}</div>
                                </div>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>
    `,
    props: {
        q: String
    },
    setup(props) {
        const ai = inject('ai')
        const router = useRouter()
        const config = inject('config')
        const { threads, loadThreads } = useThreadStore()
        let defaultVisibleCount = 25
        const visibleCount = ref(defaultVisibleCount)
        const filtered = ref([])
        const displayed = ref([])

        const start = Date.now()
        console.log('start', start, threads.value.length)

        onMounted(async () => {
            visibleCount.value = defaultVisibleCount
            if (!threads.value.length) {
                await loadThreads()
            }
            update()
            console.log('end', Date.now() - start)
        })

        const normalized = (s) => (s || '').toString().toLowerCase()

        const replaceChars = new Set('<>`*|#'.split(''))
        const clean = s => [...s].map(c => replaceChars.has(c) ? ' ' : c).join('')

        function update() {
            console.log('update', props.q)
            const query = normalized(props.q)
            filtered.value = !query
                ? threads.value
                : threads.value.filter(t => {
                    const inTitle = normalized(t.title).includes(query)
                    const inMsgs = Array.isArray(t.messages) && t.messages.some(m => normalized(m?.content).includes(query))
                    return inTitle || inMsgs
                })
            updateVisible()
        }
        function updateVisible() {
            displayed.value = filtered.value.slice(0, Math.min(visibleCount.value, filtered.value.length))
        }

        const onScroll = (e) => {
            const el = e.target
            if (el.scrollTop + el.clientHeight >= el.scrollHeight - 24) {
                if (visibleCount.value < filtered.value.length) {
                    visibleCount.value = Math.min(visibleCount.value + defaultVisibleCount, filtered.value.length)
                    updateVisible()
                }
            }
        }

        watch(() => props.q, () => {
            visibleCount.value = defaultVisibleCount
            update()
        })

        const snippet = (t) => {
            const highlight = (s) => clean(s).replace(new RegExp(`(${query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')})`, 'gi'), `<mark>$1</mark>`)
            const query = normalized(props.q)
            if (!query) return (t.messages && t.messages.length) ? highlight(t.messages[t.messages.length-1].content) : ''
            if (normalized(t.title).includes(query)) return highlight(t.title)
            if (Array.isArray(t.messages)){
                for (const m of t.messages){
                    const c = normalized(m?.content)
                    if (c.includes(query)){
                        // return small excerpt around first match
                        const idx = c.indexOf(query)
                        const orig = (m?.content || '')
                        const start = Math.max(0, idx - 40)
                        const end = Math.min(orig.length, idx + query.length + 60)
                        const prefix = start>0 ? '…' : ''
                        const suffix = end<orig.length ? '…' : ''
                        const snippet = prefix + orig.slice(start, end) + suffix
                        // return snippet
                        return highlight(snippet)
                    }
                }
            }
            return ''
        }

        const open = (id) => router.push(`${ai.base}/c/${id}`)
        const formatDate = (iso) => new Date(iso).toLocaleString()

        return {
            config,
            threads,
            filtered,
            displayed,
            snippet,
            open,
            formatDate,
            renderMarkdown,
            onScroll,
        }
    }
}

export default {
    components: {
        RecentResults,
    },
    template: `
        <div class="flex flex-col h-full w-full">
            <!-- Header -->
            <div class="border-b border-gray-200 bg-white px-4 py-3 min-h-16">
                <div class="max-w-6xl mx-auto flex items-center justify-between gap-3">
                    <h2 class="text-lg font-semibold text-gray-900">Search Chats</h2>
                    <div class="flex-1 flex items-center gap-2">
                        <input
                            v-model="q"
                            type="search"
                            placeholder="Search titles and messages..."
                            spellcheck="false"
                            class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm placeholder-gray-500 focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
                        />
                    </div>
                </div>
            </div>
            <RecentResults :q="q" />
        </div>
    `,
    setup() {
        const router = useRouter()
        const route = useRoute()
        const q = ref('')

        // Initialize search query from URL parameter
        onMounted(() => {
            const urlQuery = route.query.q || ''
            q.value = urlQuery
        })

        // Watch for changes in the search input and update URL
        watch(q, (newQuery) => {
            const currentQuery = route.query.q || ''
            if (newQuery !== currentQuery) {
                // Update URL without triggering navigation
                router.replace({
                    path: route.path,
                    query: newQuery ? { q: newQuery } : {}
                })
            }
        })

        // Watch for URL changes (browser back/forward) and update search input
        watch(() => route.query.q, (newQuery) => {
            const urlQuery = newQuery || ''
            if (q.value !== urlQuery) {
                q.value = urlQuery
            }
        })

        return {
            q,
        }
    }
}
