import { onMounted, inject } from 'vue'
import { useRouter } from 'vue-router'
import { useFormatters } from '@servicestack/vue'
import { useThreadStore } from './threadStore.mjs'
import Brand from './Brand.mjs'
import { statsTitle, formatCost } from './utils.mjs'

const { humanifyNumber, humanifyMs } = useFormatters()

// Thread Item Component
const ThreadItem = {
    template: `
        <div
            class="group relative mx-2 mb-1 rounded-md cursor-pointer transition-colors  border border-transparent"
            :class="isActive ? 'bg-blue-100 border-blue-200' : 'hover:bg-gray-100'"
            @click="$emit('select', thread.id)"
        >
            <div class="flex items-center px-3 py-2">
                <div class="flex-1 min-w-0">
                    <div class="text-sm font-medium text-gray-900 truncate" :title="thread.title">
                        {{ thread.title }}
                    </div>
                    <div class="text-xs text-gray-500 truncate">
                        <span>{{ formatRelativeTime(thread.updatedAt) }} • {{ thread.messages.length }} msgs</span>
                        <span v-if="thread.stats?.inputTokens" :title="statsTitle(thread.stats)">
                            &#8226; {{ humanifyNumber(thread.stats.inputTokens + thread.stats.outputTokens) }} toks
                            {{ thread.stats.cost ? ' ' + formatCost(thread.stats.cost) : '' }}
                        </span>
                    </div>
                    <div v-if="thread.model" class="text-xs text-blue-600 truncate">
                        {{ thread.model }}
                    </div>
                </div>

                <!-- Delete button (shown on hover) -->
                <button type="button"
                    @click.stop="$emit('delete', thread.id)"
                    class="opacity-0 group-hover:opacity-100 ml-2 p-1 rounded text-gray-400 hover:text-red-600 hover:bg-red-50 transition-all"
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
        const formatRelativeTime = (timestamp) => {
            const now = new Date()
            const date = new Date(timestamp)
            const diffInSeconds = Math.floor((now - date) / 1000)

            if (diffInSeconds < 60) return 'Just now'
            if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m ago`
            if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h ago`
            if (diffInSeconds < 604800) return `${Math.floor(diffInSeconds / 86400)}d ago`

            return date.toLocaleDateString()
        }

        return {
            formatRelativeTime,
            humanifyNumber,
            statsTitle,
            formatCost,
        }
    }
}

const GroupedThreads = {
    components: {
        ThreadItem,
    },
    template: `
    <!-- Today -->
    <div v-if="groupedThreads.today.length > 0" class="mb-4">
        <h3 class="px-4 py-2 text-xs font-semibold text-gray-500 uppercase tracking-wider select-none">Today</h3>
        <ThreadItem
            v-for="thread in groupedThreads.today"
            :key="thread.id"
            :thread="thread"
            :is-active="currentThread?.id === thread.id"
            @select="$emit('select', $event)"
            @delete="$emit('delete', $event)"
        />
    </div>

    <!-- Last 7 Days -->
    <div v-if="groupedThreads.lastWeek.length > 0" class="mb-4">
        <h3 class="px-4 py-2 text-xs font-semibold text-gray-500 uppercase tracking-wider select-none">Last 7 Days</h3>
        <ThreadItem
            v-for="thread in groupedThreads.lastWeek"
            :key="thread.id"
            :thread="thread"
            :is-active="currentThread?.id === thread.id"
            @select="$emit('select', $event)"
            @delete="$emit('delete', $event)"
        />
    </div>

    <!-- Last 30 Days -->
    <div v-if="groupedThreads.lastMonth.length > 0" class="mb-4">
        <h3 class="px-4 py-2 text-xs font-semibold text-gray-500 uppercase tracking-wider select-none">Last 30 Days</h3>
        <ThreadItem
            v-for="thread in groupedThreads.lastMonth"
            :key="thread.id"
            :thread="thread"
            :is-active="currentThread?.id === thread.id"
            @select="$emit('select', $event)"
            @delete="$emit('delete', $event)"
        />
    </div>

    <!-- Older (grouped by month/year) -->
    <div v-for="(monthThreads, monthKey) in groupedThreads.older" :key="monthKey" class="mb-4">
        <h3 class="px-4 py-2 text-xs font-semibold text-gray-500 uppercase tracking-wider select-none">{{ monthKey }}</h3>
        <ThreadItem
            v-for="thread in monthThreads"
            :key="thread.id"
            :thread="thread"
            :is-active="currentThread?.id === thread.id"
            @select="$emit('select', $event)"
            @delete="$emit('delete', $event)"
        />
    </div>
    <div class="mb-4 flex w-full justify-center">
        <button @click="$router.push($ai.base + '/recents')" type="button"
            class="flex text-sm space-x-1 font-semibold text-gray-900 hover:text-blue-600 focus:outline-none transition-colors">
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

const Sidebar = {
    components: {
        Brand,
        GroupedThreads,
        ThreadItem,
    },
    template: `
        <div class="flex flex-col h-full bg-gray-50 border-r border-gray-200">
            <Brand @home="goToInitialState" @new="createNewThread" @analytics="goToAnalytics" />
            <!-- Thread List -->
            <div class="flex-1 overflow-y-auto">
                <div v-if="isLoading" class="p-4 text-center text-gray-500">
                    <div class="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-600 mx-auto"></div>
                    <p class="mt-2 text-sm">Loading threads...</p>
                </div>

                <div v-else-if="threads.length === 0" class="p-4 text-center text-gray-500">
                    <div class="mb-2 flex justify-center">
                        <svg class="size-8" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 16 16"><path fill="currentColor" d="M8 2.19c3.13 0 5.68 2.25 5.68 5s-2.55 5-5.68 5a5.7 5.7 0 0 1-1.89-.29l-.75-.26l-.56.56a14 14 0 0 1-2 1.55a.13.13 0 0 1-.07 0v-.06a6.58 6.58 0 0 0 .15-4.29a5.25 5.25 0 0 1-.55-2.16c0-2.77 2.55-5 5.68-5M8 .94c-3.83 0-6.93 2.81-6.93 6.27a6.4 6.4 0 0 0 .64 2.64a5.53 5.53 0 0 1-.18 3.48a1.32 1.32 0 0 0 2 1.5a15 15 0 0 0 2.16-1.71a6.8 6.8 0 0 0 2.31.36c3.83 0 6.93-2.81 6.93-6.27S11.83.94 8 .94"/><ellipse cx="5.2" cy="7.7" fill="currentColor" rx=".8" ry=".75"/><ellipse cx="8" cy="7.7" fill="currentColor" rx=".8" ry=".75"/><ellipse cx="10.8" cy="7.7" fill="currentColor" rx=".8" ry=".75"/></svg>
                    </div>
                    <p class="text-sm">No conversations yet</p>
                    <p class="text-xs text-gray-400 mt-1">Start a new chat to begin</p>
                </div>

                <div v-else class="py-2">
                    <GroupedThreads :currentThread="currentThread" :groupedThreads="threadStore.getGroupedThreads(18)" 
                        @select="selectThread" @delete="deleteThread" />        
                </div>
            </div>
        </div>
    `,
    setup() {
        const ai = inject('ai')
        const router = useRouter()
        const threadStore = useThreadStore()
        const {
            threads,
            currentThread,
            isLoading,
            groupedThreads,
            loadThreads,
            createThread,
            deleteThread: deleteThreadFromStore,
            clearCurrentThread
        } = threadStore

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
            const newThread = await createThread()
            router.push(`${ai.base}/c/${newThread.id}`)
        }

        const goToInitialState = () => {
            clearCurrentThread()
            router.push(`${ai.base}/`)
        }

        const goToAnalytics = () => {
            router.push(`${ai.base}/analytics`)
        }

        return {
            threadStore,
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

export default Sidebar
