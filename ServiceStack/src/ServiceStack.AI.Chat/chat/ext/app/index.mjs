import { onMounted, inject } from 'vue'
import { useRouter } from 'vue-router'
import { appendQueryString } from '@servicestack/client'
import ThreadStore from './threadStore.mjs'
import Recents from './Recents.mjs'

let ext

// Thread Item Component
const ThreadItem = {
    template: `
        <div
            class="group relative mx-2 mb-1 rounded-md cursor-pointer transition-colors border"
            :class="isActive ? $styles.threadItemActive : $styles.threadItem"
            @click="$emit('select', thread.id)"
        >
            <div class="flex items-center px-3 py-2">
                <div class="flex-1 min-w-0">
                    <div class="text-sm font-medium truncate" :class="$styles.heading" :title="thread.title">
                        {{ thread.title }}
                    </div>
                    <div class="text-xs truncate" :class="[isActive ? $styles.mutedActive : $styles.muted]">
                        <span>{{ $fmt.relativeTime(thread.updatedAt) }} • {{ thread.messages.length }} msgs</span>
                        <span v-if="thread.stats?.inputTokens" :title="$fmt.statsTitle(thread.stats)">
                            &#8226; {{ $fmt.humanifyNumber(thread.stats.inputTokens + thread.stats.outputTokens) }} toks
                            {{ thread.stats.cost ? ' ' + $fmt.cost(thread.stats.cost) : '' }}
                        </span>
                    </div>
                    <div v-if="thread.model" class="text-xs truncate" :class="$styles.highlighted">
                        {{ thread.model }}
                    </div>
                </div>

                <!-- Delete button (shown on hover) -->
                <button type="button"
                    @click.stop="$emit('delete', thread.id)"
                    class="opacity-0 group-hover:opacity-100 ml-2 p-1 transition-all rounded hover:text-red-600 dark:hover:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/30"
                    :class="[$styles.icon, $styles.iconHover]"
                    title="Delete conversation"
                >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path>
                    </svg>
                </button>
            </div>
        </div>
    `,

    props: {
        thread: {
            type: Object,
            required: true
        },
        isActive: {
            type: Boolean,
            default: false
        }
    },

    emits: ['select', 'delete'],

    setup() {
        return {
        }
    }
}

const GroupedThreads = {
    template: `
    <!-- Today -->
    <div v-if="groupedThreads.today.length > 0" class="mb-4">
        <h3 class="px-4 py-2 text-xs font-semibold uppercase tracking-wider select-none" :class="$styles.muted">Today</h3>
        <ThreadItem
            v-for="thread in groupedThreads.today"
            :key="thread.id"
            :thread="thread"
            :is-active="currentThread?.id == thread.id"
            @select="$emit('select', $event)"
            @delete="$emit('delete', $event)"
        />
    </div>

    <!-- Yesterday -->
    <div v-if="groupedThreads.yesterday.length > 0" class="mb-4">
        <h3 class="px-4 py-2 text-xs font-semibold uppercase tracking-wider select-none" :class="$styles.muted">Yesterday</h3>
        <ThreadItem
            v-for="thread in groupedThreads.yesterday"
            :key="thread.id"
            :thread="thread"
            :is-active="currentThread?.id == thread.id"
            @select="$emit('select', $event)"
            @delete="$emit('delete', $event)"
        />
    </div>

    <!-- Last 7 Days -->
    <div v-if="groupedThreads.lastWeek.length > 0" class="mb-4">
        <h3 class="px-4 py-2 text-xs font-semibold uppercase tracking-wider select-none" :class="$styles.muted">Last 7 Days</h3>
        <ThreadItem
            v-for="thread in groupedThreads.lastWeek"
            :key="thread.id"
            :thread="thread"
            :is-active="currentThread?.id == thread.id"
            @select="$emit('select', $event)"
            @delete="$emit('delete', $event)"
        />
    </div>

    <!-- Last 30 Days -->
    <div v-if="groupedThreads.lastMonth.length > 0" class="mb-4">
        <h3 class="px-4 py-2 text-xs font-semibold uppercase tracking-wider select-none" :class="$styles.muted">Last 30 Days</h3>
        <ThreadItem
            v-for="thread in groupedThreads.lastMonth"
            :key="thread.id"
            :thread="thread"
            :is-active="currentThread?.id == thread.id"
            @select="$emit('select', $event)"
            @delete="$emit('delete', $event)"
        />
    </div>

    <!-- Older (grouped by month/year) -->
    <div v-for="(monthThreads, monthKey) in groupedThreads.older" :key="monthKey" class="mb-4">
        <h3 class="px-4 py-2 text-xs font-semibold uppercase tracking-wider select-none" :class="$styles.muted">{{ monthKey }}</h3>
        <ThreadItem
            v-for="thread in monthThreads"
            :key="thread.id"
            :thread="thread"
            :is-active="currentThread?.id == thread.id"
            @select="$emit('select', $event)"
            @delete="$emit('delete', $event)"
        />
    </div>
    <div class="mb-4 flex w-full justify-center">
        <button @click="$router.push($ai.base + '/recents')" type="button"
            class="flex text-sm space-x-1 font-semibold focus:outline-none transition-colors" :class="$styles.muted,$styles.mutedHover">
            <svg class="size-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 16 16"><path fill="currentColor" d="M8 2.19c3.13 0 5.68 2.25 5.68 5s-2.55 5-5.68 5a5.7 5.7 0 0 1-1.89-.29l-.75-.26l-.56.56a14 14 0 0 1-2 1.55a.13.13 0 0 1-.07 0v-.06a6.58 6.58 0 0 0 .15-4.29a5.25 5.25 0 0 1-.55-2.16c0-2.77 2.55-5 5.68-5M8 .94c-3.83 0-6.93 2.81-6.93 6.27a6.4 6.4 0 0 0 .64 2.64a5.53 5.53 0 0 1-.18 3.48a1.32 1.32 0 0 0 2 1.5a15 15 0 0 0 2.16-1.71a6.8 6.8 0 0 0 2.31.36c3.83 0 6.93-2.81 6.93-6.27S11.83.94 8 .94"></path><ellipse cx="5.2" cy="7.7" fill="currentColor" rx=".8" ry=".75"></ellipse><ellipse cx="8" cy="7.7" fill="currentColor" rx=".8" ry=".75"></ellipse><ellipse cx="10.8" cy="7.7" fill="currentColor" rx=".8" ry=".75"></ellipse></svg>
            <span>All Chats</span>
        </button>
    </div>
    `,
    props: {
        currentThread: Object,
        groupedThreads: {
            type: Object,
            required: true
        }
    },
    emits: ['select', 'delete'],
}

const ThreadsSidebar = {
    template: `
        <div class="flex flex-col h-full">
            <Brand />
            <!-- Thread List -->
            <div class="flex-1 overflow-y-auto">
                <div v-if="isLoading" class="p-4 text-center" :class="$styles.muted">
                    <div class="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-600 dark:border-blue-400 mx-auto"></div>
                    <p class="mt-2 text-sm">Loading threads...</p>
                </div>

                <div v-else-if="threads.length === 0" class="p-4 text-center" :class="$styles.muted">
                    <div class="mb-2 flex justify-center">
                        <svg class="size-8" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 16 16"><path fill="currentColor" d="M8 2.19c3.13 0 5.68 2.25 5.68 5s-2.55 5-5.68 5a5.7 5.7 0 0 1-1.89-.29l-.75-.26l-.56.56a14 14 0 0 1-2 1.55a.13.13 0 0 1-.07 0v-.06a6.58 6.58 0 0 0 .15-4.29a5.25 5.25 0 0 1-.55-2.16c0-2.77 2.55-5 5.68-5M8 .94c-3.83 0-6.93 2.81-6.93 6.27a6.4 6.4 0 0 0 .64 2.64a5.53 5.53 0 0 1-.18 3.48a1.32 1.32 0 0 0 2 1.5a15 15 0 0 0 2.16-1.71a6.8 6.8 0 0 0 2.31.36c3.83 0 6.93-2.81 6.93-6.27S11.83.94 8 .94"/><ellipse cx="5.2" cy="7.7" fill="currentColor" rx=".8" ry=".75"/><ellipse cx="8" cy="7.7" fill="currentColor" rx=".8" ry=".75"/><ellipse cx="10.8" cy="7.7" fill="currentColor" rx=".8" ry=".75"/></svg>
                    </div>
                    <p class="text-sm">No conversations yet</p>
                    <p class="text-xs mt-1" :class="$styles.muted">Start a new chat to begin</p>
                </div>

                <div v-else class="relative py-2">

                    <div class="flex items-center space-x-2 absolute top-2 right-2">
                        <button type="button"
                            @click="createNewThread"
                            class="focus:outline-none transition-colors"
                            :class="[$styles.icon, $styles.iconHover]"
                            title="New Chat"
                        >
                            <svg class="size-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"><path d="M12 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.375 2.625a1 1 0 0 1 3 3l-9.013 9.014a2 2 0 0 1-.853.505l-2.873.84a.5.5 0 0 1-.62-.62l.84-2.873a2 2 0 0 1 .506-.852z"/></g></svg>
                        </button>
                    </div>

                    <GroupedThreads :currentThread="currentThread" :groupedThreads="$threads.getGroupedThreads(50)"
                        @select="selectThread" @delete="deleteThread" />
                </div>
            </div>
        </div>
    `,
    setup(props) {
        const ctx = inject('ctx')
        const ai = ctx.ai
        const router = useRouter()
        const {
            threads,
            currentThread,
            isLoading,
            groupedThreads,
            loadThreads,
            createThread,
            deleteThread: deleteThreadFromStore,
            clearCurrentThread
        } = ctx.threads

        onMounted(async () => {
            await loadThreads()
        })

        const selectThread = async (threadId) => {
            router.push(`${ai.base}/c/${threadId}`)
        }

        const deleteThread = async (threadId) => {
            if (confirm('Are you sure you want to delete this conversation?')) {
                const wasCurrent = currentThread?.value?.id === threadId
                await deleteThreadFromStore(threadId)
                if (wasCurrent) {
                    router.push(`${ai.base}/`)
                }
            }
        }

        const createNewThread = async () => {
            ctx.threads.startNewThread({ title: 'New Chat', redirect: true })
        }

        const goToInitialState = () => {
            clearCurrentThread()
            ctx.to(`/`)
        }

        const goToAnalytics = () => {
            ctx.to(`/analytics`)
        }

        return {
            threads,
            currentThread,
            isLoading,
            groupedThreads,
            selectThread,
            deleteThread,
            createNewThread,
            goToInitialState,
            goToAnalytics,
        }
    }
}

function useRequests(ext) {
    async function query(query) {
        return (await ext.getJson(appendQueryString(`/requests`, query))).response || []
    }
    async function deleteById(requestId) {
        if (!requestId) {
            throw new Error('Request ID is required')
        }
        return await ext.deleteJson(`/requests/${requestId}`)
    }

    async function getThreadIds(query) {
        return (await ext.getJson(appendQueryString(`/requests?fields=threadId&not_null=threadId&as=column&take=10000`, query))).response || []
    }

    async function getSummary() {
        return (await ext.getJson(`/requests/summary`)).response
    }
    async function getDailySummary(day) {
        return (await ext.getJson(`/requests/summary/${day}`)).response
    }

    // Get unique values for filter options
    async function getFilterOptions() {
        const results = await query({
            select: 'distinct',
            fields: 'model,provider',
            not_null: 'model,provider',
        })

        if (results) {
            const models = [...new Set(results.map(r => r.model).filter(m => m))].sort()
            const providers = [...new Set(results.map(r => r.provider).filter(p => p))].sort()
            console.log('getFilterOptions', models, providers)
            return {
                models,
                providers
            }
        }
    }

    return {
        query,
        deleteById,
        getThreadIds,
        getSummary,
        getDailySummary,
        getFilterOptions,
    }
}

export default {
    order: -100,

    install(ctx) {
        ext = ctx.scope('app')
        ctx.components({
            ThreadsSidebar,
            ThreadItem,
            GroupedThreads,
            Recents,
        })
        ctx.routes.push(...[
            { path: '/recents', component: Recents },
        ])
        ThreadStore.install(ctx)

        ctx.setGlobals({
            requests: useRequests(ext)
        })

        ctx.setLayout({
            left: 'ThreadsSidebar',
        })

        ctx.setState({
            themes: ctx.utils.storageObject('llms.themes') || {
                "light": {
                    "vars": {
                        "colorScheme": "light"
                    }
                },
                "dark": {
                    "vars": {
                        "colorScheme": "dark"
                    }
                }
            }
        })
        const selectedTheme = ctx.getTheme(ctx.selectedTheme)
        console.log('selectedTheme', ctx.selectedTheme, selectedTheme)
        ctx.setTheme(selectedTheme)
    },

    async load(ctx) {
        const api = await ctx.getJson("/themes")
        if (api.response) {
            ctx.setState({
                themes: api.response
            })
            localStorage.setItem('llms.themes', JSON.stringify(api.response))
            ctx.setTheme(ctx.getTheme(ctx.selectedTheme))
        }
    }
}
