import { ref, computed } from 'vue'
import { appendQueryString } from '@servicestack/client'

/**
 * Returns an ever-increasing unique integer id.
 */
export const nextId = (() => {
    let last = 0               // cache of the last id that was handed out
    return () => {
        const now = Date.now() // current millisecond timestamp
        last = (now > last) ? now : last + 1
        return last
    }
})();


const threads = ref([])
const threadDetails = ref({})
const threadActions = ref({})
const currentThread = ref(null)
const isLoading = ref(false)
const MAX_RENDERED_MESSAGES = 800

let ctx = null
let ext = null

function setError(error, msg = null) {
    ctx?.setError(error, msg)
}

async function query(query) {
    return (await ext.getJson(appendQueryString(`/threads`, query))).response || []
}

let watchTimeout = null
let eventSource = null
let sseConnectTimer = null
let sseHealthTimer = null
let watchGeneration = 0
const isWatchingThread = ref(false)

const defaultEventsConfig = Object.freeze({
    transport: 'auto',
    longPollTimeoutSeconds: 25,
    sseHeartbeatSeconds: 15,
    sseConnectTimeoutSeconds: 5,
    sseFailureThreshold: 3,
    sseRetryDelaySeconds: 10,
})

function getEventsConfig() {
    const configured = ctx?.state?.config?.defaults?.events || {}
    const config = { ...defaultEventsConfig, ...configured }
    config.transport = ['auto', 'sse', 'long-poll'].includes(config.transport)
        ? config.transport
        : 'auto'
    return config
}

async function fetchThread(threadId) {
    if (!threadId) return null
    const api = await ext.getJson(`/threads/${threadId}`)
    if (api.response) {
        const latestThread = api.response
        threadDetails.value[threadId] = latestThread
        replaceThread(latestThread, { forceActions: true })
        return latestThread
    }
    return null
}

async function fetchFullThread(threadId) {
    if (!threadId) return null
    const api = await ext.getJson(`/threads/${threadId}?allMessages=true`)
    return api.response || null
}

function rangesFor(messages) {
    const sequences = [...new Set(messages.map(x => x._sequence).filter(x => x != null))].sort((a, b) => a - b)
    const ranges = []
    for (const sequence of sequences) {
        const last = ranges[ranges.length - 1]
        if (!last || sequence > last.to + 1) ranges.push({ from: sequence, to: sequence })
        else last.to = sequence
    }
    return ranges
}

function mergeWindowMessages(existing, incoming, preserve = 'auto') {
    const values = new Map()
    for (const message of [...(existing || []), ...(incoming || [])]) {
        if (message.streaming) continue
        // An optimistically rendered message initially has only a timestamp. Once
        // persisted, the server returns the same timestamp plus its RDBMS sequence.
        // Replace that provisional identity instead of retaining both copies.
        if (message._sequence != null && message.timestamp != null) {
            values.delete(`t:${message.timestamp}`)
        }
        const key = message._sequence != null ? `s:${message._sequence}` : `t:${message.timestamp}`
        values.set(key, message)
    }
    const streaming = (incoming || []).filter(x => x.streaming)
    let messages = [...values.values()].sort((a, b) =>
        (a._sequence ?? Number.MAX_SAFE_INTEGER) - (b._sequence ?? Number.MAX_SAFE_INTEGER))
    if (messages.length > MAX_RENDERED_MESSAGES) {
        if (preserve === 'auto') {
            let leading = 1
            while (leading < messages.length
                && messages[leading]._sequence === messages[leading - 1]._sequence + 1) leading++
            preserve = leading > 20 ? 'start' : 'end'
        }
        messages = preserve === 'start'
            ? [...messages.slice(0, MAX_RENDERED_MESSAGES - 100), ...messages.slice(-100)]
            : [...messages.slice(0, 20), ...messages.slice(-(MAX_RENDERED_MESSAGES - 20))]
    }
    return [...messages, ...streaming]
}

async function loadMessageRange({ after = null, before = null, take = 100 } = {}) {
    const thread = currentThread.value
    if (!thread?.id) return null
    const query = { take }
    if (before != null) query.before = before
    else query.after = after || 0
    const api = await ext.getJson(appendQueryString(`/threads/${thread.id}/messages`, query))
    if (!api.response) {
        setError(api.error, `Loading messages for thread ${thread.id}`)
        return null
    }
    const messages = mergeWindowMessages(
        thread.messages, api.response.messages, before != null ? 'end' : 'start'
    )
    currentThread.value = {
        ...thread,
        messages,
        messageWindow: {
            ...(thread.messageWindow || {}),
            messageCount: api.response.messageCount,
            firstSequence: api.response.firstSequence,
            lastSequence: api.response.lastSequence,
            ranges: rangesFor(messages),
        },
    }
    return api.response
}

async function watchThreadUpdates(generation) {
    clearTimeout(watchTimeout)
    watchTimeout = null

    const thread = currentThread.value
    if (generation !== watchGeneration || !thread?.id || !thread.messages?.length || thread.completedAt || thread.error) {
        stopWatchingThread()
        return
    }

    const api = await ext.getJson(appendQueryString(`/threads/${thread.id}/updates`, { sig: thread.sig || '' }))

    if (api.response) {
        const isCompleted = !!(api.response.completedAt || api.response.error)
        replaceThread(api.response)
        if (isCompleted) {
            await fetchThread(thread.id)
        }
    } else if (api.error) {
        setError(api.error, `watching thread ${thread.id}`)
        stopWatchingThread()
    }

    if (generation === watchGeneration && isWatchingThread.value) {
        watchTimeout = setTimeout(() => watchThreadUpdates(generation), 100)
    }
}

function startLongPolling(generation) {
    if (generation !== watchGeneration || !isWatchingThread.value) return
    watchTimeout = setTimeout(() => watchThreadUpdates(generation), 100)
}

function closeSse() {
    if (eventSource) {
        eventSource.close()
        eventSource = null
    }
    clearTimeout(sseConnectTimer)
    clearTimeout(sseHealthTimer)
    sseConnectTimer = null
    sseHealthTimer = null
}

function startSse(generation, config) {
    const thread = currentThread.value
    if (generation !== watchGeneration || !thread?.id || !isWatchingThread.value) return

    let connected = false
    let failures = 0
    const url = ctx.resolveUrl(appendQueryString(
        `/ext/app/threads/${thread.id}/updates/stream`, { sig: thread.sig || '' }))
    const source = new EventSource(url, { withCredentials: true })
    eventSource = source

    const fallbackOrRetry = () => {
        if (generation !== watchGeneration || !isWatchingThread.value) return
        closeSse()
        if (config.transport === 'auto') {
            startLongPolling(generation)
        } else {
            watchTimeout = setTimeout(() => startSse(generation, config),
                config.sseRetryDelaySeconds * 1000)
        }
    }

    const checkHealth = () => {
        clearTimeout(sseHealthTimer)
        sseHealthTimer = setTimeout(() => {
            failures++
            if (failures >= config.sseFailureThreshold) fallbackOrRetry()
            else checkHealth()
        }, config.sseHeartbeatSeconds * 3000)
    }

    const markHealthy = () => {
        failures = 0
        checkHealth()
    }

    const applyEvent = async event => {
        if (generation !== watchGeneration) return
        markHealthy()
        try {
            const updated = JSON.parse(event.data)
            if (updated?.id) {
                const isCompleted = !!(updated.completedAt || updated.error)
                replaceThread(updated)
                if (isCompleted) await fetchThread(updated.id)
            }
        } catch (e) {
            console.warn('Ignoring invalid thread event', e)
        }
    }

    source.addEventListener('connected', event => {
        connected = true
        clearTimeout(sseConnectTimer)
        applyEvent(event)
    })
    source.addEventListener('thread', applyEvent)
    source.addEventListener('heartbeat', markHealthy)
    source.onerror = () => {
        if (!connected) {
            fallbackOrRetry()
            return
        }
        failures++
        if (failures >= config.sseFailureThreshold) fallbackOrRetry()
    }

    sseConnectTimer = setTimeout(() => {
        if (!connected) fallbackOrRetry()
    }, config.sseConnectTimeoutSeconds * 1000)
}

function startWatchingThread() {
    stopWatchingThread()
    const thread = currentThread.value
    if (thread?.id && thread.messages?.length > 0 && !thread.completedAt && !thread.error) {
        isWatchingThread.value = true
        const generation = watchGeneration
        const config = getEventsConfig()
        if (config.transport === 'long-poll' || typeof EventSource === 'undefined') {
            startLongPolling(generation)
        } else {
            startSse(generation, config)
        }
    } else {
        stopWatchingThread()
    }
}

function stopWatchingThread() {
    watchGeneration++
    isWatchingThread.value = false
    closeSse()
    if (watchTimeout) {
        clearTimeout(watchTimeout)
        watchTimeout = null
    }
}

function replaceThread(thread, opt = {}) {
    if (!thread) return
    const existing = currentThread.value?.id === thread.id ? currentThread.value : null
    if (!opt?.resetMessages && existing?.messageWindow && thread.messageWindow) {
        const messages = mergeWindowMessages(existing.messages, thread.messages)
        thread = {
            ...thread,
            messages,
            messageWindow: { ...thread.messageWindow, ranges: rangesFor(messages) },
        }
    }
    const index = threads.value.findIndex(t => t.id === thread.id)
    if (index !== -1) threads.value[index] = thread
    if (currentThread.value?.id === thread.id) currentThread.value = thread

    if (thread.completedAt || thread.error) {
        threadDetails.value[thread.id] = thread
        if (!threadActions.value[thread.id] || opt?.forceActions) {
            loadThreadActions(thread.id, opt?.forceActions ? { force: true } : undefined)
        }
        stopWatchingThread()
    } else if (currentThread.value?.id === thread.id && !isWatchingThread.value) {
        startWatchingThread()
    }
    return thread
}

async function cancelThread() {
    console.log('cancelThread')
    stopWatchingThread()
    const thread = currentThread.value
    if (!thread) return
    const api = await ext.postJson(`/threads/${thread.id}/cancel`)
    if (api.response) {
        replaceThread(api.response)
        await fetchThread(thread.id)
    } else {
        setError(api.error, `Canceling thread ${thread.id}`)
    }
}

// Create a new thread
async function createThread(args = {}) {
    const thread = {
        ...args
    }
    if (!thread.title) {
        thread.title = 'New Chat'
    }
    if (thread.title.length > 200) {
        thread.title = thread.title.slice(0, 200) + '...'
    }
    if (!thread.messages) {
        thread.messages = []
    }

    ctx.createThreadFilters.forEach(f => f(thread))

    const api = await ext.postJson("/threads", thread)
    if (api.response) {
        threads.value.unshift(api.response)
        return api.response
    } else {
        setError(api.error, `Creating thread ${thread.title}`)
    }

    return thread
}

// Update thread
async function updateThread(threadId, updates) {

    if (!threadId)
        throw new Error('threadId is required')

    ctx.updateThreadFilters.forEach(f => f(updates))

    const api = await ext.patchJson(`/threads/${threadId}`, updates)
    if (api.response) {
        return replaceThread(api.response, { resetMessages: !!updates.truncate })
    } else {
        setError(api.error, `Updating thread ${threadId}`)
    }
}

async function deleteMessageFromThread(threadId, timestamp) {
    const thread = await fetchFullThread(threadId)
    if (!thread) throw new Error('Thread not found')
    const updatedMessages = thread.messages.filter(m => m.timestamp !== timestamp)
    console.log('deleteMessageFromThread', threadId, timestamp, updatedMessages)
    // truncate: deleting a message deliberately shrinks history, the server rejects
    // shrinking writes that don't opt in so an in-flight request can't erase a thread
    await updateThread(threadId, { messages: updatedMessages, truncate: true })
}

async function updateMessageInThread(threadId, messageId, updates) {
    const thread = await fetchFullThread(threadId)
    if (!thread) throw new Error('Thread not found')

    const messageIndex = thread.messages.findIndex(m => m.timestamp === messageId)
    if (messageIndex === -1) throw new Error('Message not found')

    const updatedMessages = [...thread.messages]
    updatedMessages[messageIndex] = {
        ...updatedMessages[messageIndex],
        ...updates
    }

    await updateThread(threadId, { messages: updatedMessages, truncate: true })
}

async function redoMessageFromThread(threadId, timestamp) {
    const thread = await fetchFullThread(threadId)
    if (!thread) throw new Error('Thread not found')

    // Find the index of the message to redo
    const messageIndex = thread.messages.findIndex(m => m.timestamp === timestamp)
    if (messageIndex === -1) {
        setError({ message: `Message not found for timestamp ${timestamp}` })
        return
    }

    // setError({
    //     errorCode: 'TestError',
    //     message: `Error redoing message ${timestamp} in thread ${threadId}`,
    //     stackTrace: `Error in page.mjs
    //         at Line 1
    //         at Line 2
    //         at Line 3`,
    // })
    // return

    // Keep only messages up to and including the target message
    const updatedMessages = thread.messages.slice(0, messageIndex + 1)

    // Update the thread with the new messages (truncate: an intentional rewrite)
    const request = { messages: updatedMessages, truncate: true }

    const model = thread.modelInfo
    const api = await queueChat({ request, thread, model })
    if (api.response) {
        replaceThread(api.response, { resetMessages: true })
    } else {
        setError(api.error, `Redoing message ${timestamp} in thread ${threadId}`)
    }
}

async function loadThreads() {
    isLoading.value = true

    try {
        const api = await ext.getJson('/threads?take=30')
        threads.value = api.response || []
        return threads.value
    } finally {
        isLoading.value = false
    }
}

async function getThread(threadId) {
    const cachedThread = threads.value.find(t => t.id == threadId)
    if (cachedThread) return cachedThread
    const api = await ext.getJson(`/threads?id=${threadId}`)
    return api.response && api.response[0] || null
}

// Delete thread
async function deleteThread(threadId) {
    await ext.delete(`/threads/${threadId}`)

    threads.value = threads.value.filter(t => t.id !== threadId)

    if (currentThread.value?.id === threadId) {
        currentThread.value = null
    }
}

// Load thread actions from extension
async function loadThreadActions(threadId, opt) {
    if (threadActions.value[threadId] && !opt?.force) {
        return
    }
    const thread = threadDetails.value[threadId] || (currentThread.value?.id == threadId ? currentThread.value : await getThread(threadId))
    if (!thread) {
        return
    }
    const profile = thread.metadata?.profile ?? 'default'
    const res = await ctx.getJson(`/ext/agents/${profile}/actions`)
    threadActions.value[threadId] = res.response || []
}

function getThreadActions(threadId) {
    return threadActions.value[threadId] || []
}

// Set current thread
async function setCurrentThread(threadId) {
    if (!threadId) {
        currentThread.value = null
        stopWatchingThread()
        return null
    }
    const thread = await fetchThread(threadId)
    if (thread) {
        currentThread.value = thread
        startWatchingThread()
    } else {
        stopWatchingThread()
    }
    return thread
}

// Set current thread from router params (router-aware version)
async function setCurrentThreadFromRoute(threadId, router) {
    if (!threadId) {
        currentThread.value = null
        stopWatchingThread()
        return null
    }

    loadThreadDetails(threadId)
    loadThreadActions(threadId)
    const thread = await setCurrentThread(threadId)
    if (thread) {
        return thread
    } else {
        // Thread not found, redirect to home
        if (router) {
            router.push((globalThis.ai?.base || '') + '/')
        }
        currentThread.value = null
        stopWatchingThread()
        return null
    }
}

// Clear current thread (go back to initial state)
function clearCurrentThread() {
    currentThread.value = null
    stopWatchingThread()
}

function getGroupedThreads(total) {
    const now = new Date()
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate())
    const yesterday = new Date(today.getTime() - 24 * 60 * 60 * 1000)
    const lastWeek = new Date(today.getTime() - 7 * 24 * 60 * 60 * 1000)
    const lastMonth = new Date(today.getTime() - 30 * 24 * 60 * 60 * 1000)

    const groups = {
        today: [],
        yesterday: [],
        lastWeek: [],
        lastMonth: [],
        older: {}
    }

    const takeThreads = threads.value.slice(0, total)

    takeThreads.forEach(thread => {
        const threadDate = new Date(thread.updatedAt)

        if (threadDate >= today) {
            groups.today.push(thread)
        } else if (threadDate >= yesterday) {
            groups.yesterday.push(thread)
        } else if (threadDate >= lastWeek) {
            groups.lastWeek.push(thread)
        } else if (threadDate >= lastMonth) {
            groups.lastMonth.push(thread)
        } else {
            const year = threadDate.getFullYear()
            const month = threadDate.toLocaleString('default', { month: 'long' })
            const key = `${month} ${year}`

            if (!groups.older[key]) {
                groups.older[key] = []
            }
            groups.older[key].push(thread)
        }
    })

    return groups
}

// Group threads by time periods
const groupedThreads = computed(() => getGroupedThreads(threads.value.length))

function getLatestCachedThread() {
    return threads.value[0]
}

async function startNewThread(args = {}) {
    let { title, model, messages, redirect, tools, ...rest } = typeof args === 'string' ? { title: args } : args
    if (!title) {
        title = 'New Chat'
    }
    const latestThread = getLatestCachedThread()

    console.log('startNewThread', title, ctx.router.currentRoute.value?.path, latestThread?.messages?.length)
    ctx.setLayout({ left: 'ThreadsSidebar' })

    if (latestThread && latestThread.title == title && !latestThread.messages?.length) {
        if (ctx.router.currentRoute.value?.path != `/c/${latestThread.id}`) {
            ctx.to(`/c/${latestThread.id}`)
        }
        return latestThread
    }
    const newThread = await createThread({
        title,
        ...(tools ? { tools } : {}),
        ...(model ? { model: typeof model === 'string' ? model : model.name || model.id } : {}),
        ...rest
    })

    console.log('newThread', newThread)
    if (redirect) {
        // Navigate to the new thread URL
        ctx.to(`/c/${newThread.id}`)
    }

    // Get the thread to check for duplicates
    let thread = await getThread(newThread.id)
    console.log('thread', thread)

    if (messages) {
        if (!model) {
            model = ctx.chat.getSelectedModel()
        }
        const request = { model: model.name, messages }
        const api = await queueChat({ request, thread, model })
        if (api.response) {
            thread = api.response
            ctx.chat.completeChat(thread)
        } else {
            ctx.setError(api.error)
        }
    }
    return thread
}

async function queueChat(ctxRequest, options = {}) {
    if (!ctxRequest.request) return ctx.createErrorResult({ message: 'No request provided' })
    if (!ctxRequest.thread) return ctx.createErrorResult({ message: 'No thread provided' })
    ctxRequest = ctx.createChatContext(ctxRequest)
    ctx.chatRequestFilters.forEach(f => f(ctxRequest))
    const { thread, request } = ctxRequest
    ctx.completeChatContext(ctxRequest)

    const api = await ctx.postJson(`/ext/app/threads/${thread.id}/chat`, {
        ...options,
        body: typeof request == 'string'
            ? request
            : JSON.stringify(request),
    })
    return api
}

async function loadThreadDetails(id, opt = null) {
    if (!threadDetails.value[id] || opt?.force) {
        const api = await ctx.getJson(`/ext/app/threads/${id}`)
        if (api.response) {
            threadDetails.value[id] = api.response
        }
        if (api.error) {
            console.error(api.error)
        }
    }
    return threadDetails.value[id]
}

function getCurrentThreadSystemPrompt() {
    return currentThread.value?.systemPrompt
        ?? currentThread.value?.messages?.find(m => m.role == 'system')?.content
        ?? ''
}

// Export the store
export function useThreadStore() {
    return {
        // State
        threads,
        currentThread,
        isLoading,
        groupedThreads,

        // Actions
        getCurrentThreadSystemPrompt,
        query,
        createThread,
        updateThread,
        deleteMessageFromThread,
        updateMessageInThread,
        redoMessageFromThread,
        loadThreads,
        getThread,
        fetchThread,
        fetchFullThread,
        loadMessageRange,
        deleteThread,
        setCurrentThread,
        setCurrentThreadFromRoute,
        clearCurrentThread,
        getGroupedThreads,
        getLatestCachedThread,
        startNewThread,
        replaceThread,
        queueChat,
        threadDetails,
        threadActions,
        isWatchingThread,
        loadThreadDetails,
        loadThreadActions,
        getThreadActions,
        startWatchingThread,
        stopWatchingThread,
        cancelThread,
        get watchingThread() {
            return isWatchingThread.value
        },
    }
}

export default {
    install(context) {
        ctx = context
        ext = ctx.scope('app')
        ctx.setGlobals({ threads: useThreadStore() })
        console.log('ctx.setGlobals threads', !!ctx.threads)
    },

    async load() {
        await ctx.threads.loadThreads()
    }
}
