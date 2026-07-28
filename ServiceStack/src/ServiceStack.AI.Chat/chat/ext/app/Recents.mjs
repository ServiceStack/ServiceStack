import { ref, onMounted, watch, inject } from 'vue'
import { useRouter, useRoute } from 'vue-router'

const RecentResults = {
    template: `
        <div class="flex-1 overflow-y-auto" @scroll="onScroll">
            <div class="mx-auto max-w-6xl px-4 py-4">
                <div class="text-sm text-gray-600 dark:text-gray-400 mb-3">
                    <span v-if="q">{{ total }} result{{ total===1?'':'s' }}</span>
                    <span v-else>All conversations</span>
                </div>

                <div v-if="!loading && threads.length === 0" class="text-gray-500 dark:text-gray-400">No conversations found.</div>

                <table class="w-full">
                    <tbody>
                        <tr v-for="t in threads" :key="t.id" class="hover:bg-gray-50 dark:hover:bg-gray-800">
                            <td class="py-3 px-1 border-b border-gray-200 dark:border-gray-700 max-w-2xl">
                                <button type="button" @click="open(t.id)" class="w-full text-left">
                                    <div class="flex items-start justify-between gap-3">
                                        <div class="min-w-0 flex-1">
                                            <div class="font-medium text-gray-900 dark:text-gray-100 truncate" :title="t.title">{{ t.title || 'Untitled chat' }}</div>
                                            <div class="mt-1 text-sm text-gray-600 dark:text-gray-400 line-clamp-2">
                                                <div v-html="snippet(t)"></div>
                                            </div>
                                        </div>
                                    </div>
                                </button>
                            </td>
                            <td class="py-3 px-1 border-b border-gray-200 dark:border-gray-700">
                                <div class="text-right whitespace-nowrap">
                                    <div class="text-xs text-gray-500 dark:text-gray-400">{{ formatDate(t.updatedAt || t.createdAt) }}</div>
                                    <div class="text-[11px] text-gray-500/80 dark:text-gray-400/80">{{ (t.messages?.length || 0) }} messages</div>
                                    <div v-if="t.model" class="text-[11px] text-blue-600 dark:text-blue-400 max-w-[140px] truncate" :title="t.model">{{ t.model }}</div>
                                </div>
                            </td>
                        </tr>
                    </tbody>
                </table>
                <div v-if="loading" class="py-4 text-center text-gray-500 dark:text-gray-400">Loading...</div>
            </div>
        </div>
    `,
    props: {
        q: String
    },
    setup(props) {
        const ctx = inject('ctx')
        const ai = ctx.ai
        const router = useRouter()

        const threads = ref([])
        const loading = ref(false)
        const noMore = ref(false)
        const total = ref(0)
        let skip = 0
        const take = 25

        // Simple debounce function
        function debounce(fn, delay) {
            let timeoutID = null
            return function () {
                clearTimeout(timeoutID)
                timeoutID = setTimeout(() => fn.apply(this, arguments), delay)
            }
        }

        const normalized = (s) => (s || '').toString().toLowerCase()
        const replaceChars = new Set('<>`*|#'.split(''))
        const clean = s => [...(s || '')].map(c => replaceChars.has(c) ? ' ' : c).join('')

        const loadMore = async (reset = false) => {
            if (reset) {
                skip = 0
                threads.value = []
                noMore.value = false
            }

            if (loading.value || noMore.value) return

            loading.value = true
            try {
                const query = {
                    take,
                    skip,
                    ...(props.q ? { q: props.q } : {})
                }

                const results = await ctx.threads.query(query)

                if (results.length < take) {
                    noMore.value = true
                }

                if (reset) {
                    threads.value = results
                } else {
                    threads.value.push(...results)
                }

                skip += results.length

                total.value = threads.value.length
            } catch (e) {
                console.error("Failed to load threads", e)
            } finally {
                loading.value = false
            }
        }

        const update = debounce(() => loadMore(true), 250)

        onMounted(() => {
            loadMore(true)
        })

        const onScroll = (e) => {
            const el = e.target
            if (el.scrollTop + el.clientHeight >= el.scrollHeight - 50) { // 50px threshold
                loadMore()
            }
        }

        watch(() => props.q, () => {
            update()
        })

        const snippet = (t) => {
            const highlight = (s) => clean(s).replace(new RegExp(`(${query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')})`, 'gi'), `<mark>$1</mark>`)
            const query = normalized(props.q)
            if (!query) return (t.messages && t.messages.length) ? highlight(t.messages[t.messages.length - 1].content) : ''

            // Check title
            if (normalized(t.title).includes(query)) return highlight(t.title)

            // Check messages
            if (Array.isArray(t.messages)) {
                for (const m of t.messages) {
                    const c = normalized(m?.content)
                    if (c.includes(query)) {
                        // return small excerpt around first match
                        const idx = c.indexOf(query)
                        const orig = (m?.content || '')
                        const start = Math.max(0, idx - 40)
                        const end = Math.min(orig.length, idx + query.length + 60)
                        const prefix = start > 0 ? '…' : ''
                        const suffix = end < orig.length ? '…' : ''
                        const snippetText = prefix + orig.slice(start, end) + suffix
                        return highlight(snippetText)
                    }
                }
            }

            // Fallback to last message if no specific match found (e.g. matched on hidden metadata or partial?)
            return (t.messages && t.messages.length) ? highlight(t.messages[t.messages.length - 1].content) : ''
        }

        const open = (id) => router.push(`${ai.base}/c/${id}`)
        const formatDate = (iso) => new Date(iso).toLocaleString()

        return {
            threads,
            loading,
            total,
            snippet,
            open,
            formatDate,
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
            <div class="border-b border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 px-4 py-3 min-h-16">
                <div class="max-w-6xl mx-auto flex items-center justify-between gap-3">
                    <label for="search-history" class="cursor-pointer text-lg font-semibold text-gray-900 dark:text-gray-100">Search History</label>
                    <div class="flex-1 flex items-center gap-2 max-w-sm">
                        <input
                            id="search-history"
                            v-model="q"
                            type="search"
                            placeholder="Search titles and messages..."
                            spellcheck="false"
                            class="w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 px-3 py-2 text-sm placeholder-gray-500 dark:placeholder-gray-400 focus:border-blue-500 dark:focus:border-blue-400 focus:outline-none focus:ring-1 focus:ring-blue-500 dark:focus:ring-blue-400"
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
