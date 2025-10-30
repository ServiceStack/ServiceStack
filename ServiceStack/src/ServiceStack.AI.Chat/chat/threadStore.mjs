import { ref, computed, unref } from 'vue'
import { openDB } from 'idb'
import { nextId, toModelInfo } from './utils.mjs'

// Thread store for managing chat threads with IndexedDB
const threads = ref([])
const currentThread = ref(null)
const isLoading = ref(false)

let db = null

// Initialize IndexedDB
async function initDB() {
    if (db) return db
    
    db = await openDB('LlmsThreads', 3, {
        upgrade(db, _oldVersion, _newVersion, transaction) {
            if (!db.objectStoreNames.contains('threads')) {
                // Create threads store
                const threadStore = db.createObjectStore('threads', {
                    keyPath: 'id',
                    autoIncrement: false
                })

                // Create indexes for efficient querying
                threadStore.createIndex('createdAt', 'createdAt')
                threadStore.createIndex('updatedAt', 'updatedAt')
                threadStore.createIndex('title', 'title')
            }
            if (!db.objectStoreNames.contains('requests')) {
                // Create requests store
                const requestStore = db.createObjectStore('requests', {
                    keyPath: 'id',
                    autoIncrement: false
                })
                requestStore.createIndex('threadId', 'threadId')
                requestStore.createIndex('model', 'model')
                requestStore.createIndex('provider', 'provider')
                requestStore.createIndex('inputTokens', 'inputTokens')
                requestStore.createIndex('outputTokens', 'outputTokens')
                requestStore.createIndex('cost', 'cost')
                requestStore.createIndex('duration', 'duration')
                requestStore.createIndex('created', 'created')
            }
        }
    })
    
    return db
}

// Generate unique thread ID
function generateThreadId() {
    return Date.now().toString()
}

async function logRequest(threadId, model, request, response) {
    await initDB()
    const metadata = response.metadata || {}
    const usage = response.usage || {}
    const [inputPrice, outputPrice] = metadata.pricing ? metadata.pricing.split('/') : [0, 0]
    const lastUserContent = request.messages?.slice().reverse().find(m => m.role === 'user')?.content
    const content = Array.isArray(lastUserContent) 
        ? lastUserContent.filter(c => c?.text).map(c => c.text).join(' ') 
        : lastUserContent
    const title = content.slice(0, 100) + (content.length > 100 ? '...' : '')
    const inputTokens = usage?.prompt_tokens ?? 0
    const outputTokens = usage?.completion_tokens ?? 0
    const inputCachedTokens = usage?.prompt_token_details?.cached_tokens ?? 0
    const finishReason = response.choices[0]?.finish_reason || 'unknown'

    const subtractDays = (date, days) => {
        const result = new Date(date * 1000)
        result.setDate(result.getDate() - days)
        return parseInt(result.valueOf() / 1000)
    }

    const log = {
        id: nextId(),
        threadId: threadId,
        model: model.id,
        provider: model.provider,
        providerModel: response.model || model.provider_model,
        title,
        inputTokens,
        outputTokens,
        inputCachedTokens,
        totalTokens: usage.total_tokens ?? (inputTokens + outputTokens),
        inputPrice,
        outputPrice,
        cost: (parseFloat(inputPrice) * inputTokens) + (parseFloat(outputPrice) * outputTokens),
        duration: metadata.duration ?? 0,
        created: response.created ?? Math.floor(Date.now() / 1000),
        finishReason,
        providerRef: response.provider,
        ref: response.id || undefined,
        usage: usage,
    }
    console.debug('logRequest', log)
    const tx = db.transaction(['requests'], 'readwrite')
    await tx.objectStore('requests').add(log)
    await tx.complete
    return log
}

// Create a new thread
async function createThread(title = 'New Chat', model = null, systemPrompt = '') {
    await initDB()

    const thread = {
        id: generateThreadId(),
        title: title,
        model: model?.id ?? '',
        info: toModelInfo(model),
        systemPrompt: systemPrompt,
        messages: [],
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
    }
    
    const tx = db.transaction(['threads'], 'readwrite')
    await tx.objectStore('threads').add(thread)
    await tx.complete
    
    threads.value.unshift(thread)
    // Note: currentThread will be set by router navigation

    return thread
}

// Update thread
async function updateThread(threadId, updates) {
    await initDB()
    
    const tx = db.transaction(['threads'], 'readwrite')
    const store = tx.objectStore('threads')
    
    const thread = await store.get(threadId)
    if (!thread) throw new Error('Thread not found')
    
    const updatedThread = {
        ...thread,
        ...updates,
        updatedAt: new Date().toISOString()
    }
    
    await store.put(updatedThread)
    await tx.complete
    
    // Update in memory
    const index = threads.value.findIndex(t => t.id === threadId)
    if (index !== -1) {
        threads.value[index] = updatedThread
    }
    
    if (currentThread.value?.id === threadId) {
        currentThread.value = updatedThread
    }
    
    return updatedThread
}

async function calculateThreadStats(threadId) {
    await initDB()
    
    const tx = db.transaction(['requests'], 'readonly')
    const store = tx.objectStore('requests')
    const index = store.index('threadId')
    
    const requests = await index.getAll(threadId)
    
    let inputTokens = 0
    let outputTokens = 0
    let cost = 0.0
    let duration = 0
    
    requests.forEach(req => {
        inputTokens += req.inputTokens || 0
        outputTokens += req.outputTokens || 0
        cost += req.cost || 0.0
        duration += req.duration || 0
    })
    
    return {
        inputTokens,
        outputTokens,
        cost,
        duration,
        requests: requests.length
    }
}

// Add message to thread
async function addMessageToThread(threadId, message, usage) {
    const thread = await getThread(threadId)
    if (!thread) throw new Error('Thread not found')
    
    const newMessage = {
        id: nextId(),
        timestamp: new Date().toISOString(),
        ...message
    }
    
    // Add input and output token usage to previous 'input' message
    if (usage?.prompt_tokens != null) {
        const lastMessage = thread.messages[thread.messages.length - 1]
        if (lastMessage && lastMessage.role === 'user') {
            lastMessage.usage = {
                tokens: parseInt(usage.prompt_tokens),
                price: usage.input || '0',
            }
        }
    }
    if (usage?.completion_tokens != null) {
        newMessage.usage = {
            tokens: parseInt(usage.completion_tokens),
            price: usage.output || '0',
            duration: usage.duration || undefined,
        }
    }

    const updatedMessages = [...thread.messages, newMessage]
    
    // Auto-generate title from first user message if still "New Chat"
    let title = thread.title
    if (title === 'New Chat' && message.role === 'user' && updatedMessages.length <= 2) {
        title = message.content.slice(0, 200) + (message.content.length > 200 ? '...' : '')
    }

    const stats = await calculateThreadStats(threadId)
    
    await updateThread(threadId, {
        messages: updatedMessages,
        title: title,
        stats,
    })
    
    return newMessage
}

async function deleteMessageFromThread(threadId, messageId) {
    const thread = await getThread(threadId)
    if (!thread) throw new Error('Thread not found')
    const updatedMessages = thread.messages.filter(m => m.id !== messageId)
    await updateThread(threadId, { messages: updatedMessages })
}

async function updateMessageInThread(threadId, messageId, updates) {
    const thread = await getThread(threadId)
    if (!thread) throw new Error('Thread not found')

    const messageIndex = thread.messages.findIndex(m => m.id === messageId)
    if (messageIndex === -1) throw new Error('Message not found')

    const updatedMessages = [...thread.messages]
    updatedMessages[messageIndex] = {
        ...updatedMessages[messageIndex],
        ...updates
    }

    await updateThread(threadId, { messages: updatedMessages })
}

async function redoMessageFromThread(threadId, messageId) {
    const thread = await getThread(threadId)
    if (!thread) throw new Error('Thread not found')

    // Find the index of the message to redo
    const messageIndex = thread.messages.findIndex(m => m.id === messageId)
    if (messageIndex === -1) throw new Error('Message not found')

    // Keep only messages up to and including the target message
    const updatedMessages = thread.messages.slice(0, messageIndex + 1)

    // Update the thread with the new messages
    await updateThread(threadId, { messages: updatedMessages })
}

// Get all threads
async function loadThreads() {
    await initDB()
    isLoading.value = true
    
    try {
        const tx = db.transaction(['threads'], 'readonly')
        const store = tx.objectStore('threads')
        const index = store.index('updatedAt')
        
        const allThreads = await index.getAll()
        threads.value = allThreads.reverse() // Most recent first
        
        return threads.value
    } finally {
        isLoading.value = false
    }
}

// Get single thread
async function getThread(threadId) {
    await initDB()
    
    const tx = db.transaction(['threads'], 'readonly')
    const thread = await tx.objectStore('threads').get(threadId)
    
    return thread
}

// Delete thread
async function deleteThread(threadId) {
    await initDB()
    
    const tx = db.transaction(['threads'], 'readwrite')
    await tx.objectStore('threads').delete(threadId)
    await tx.complete
    
    threads.value = threads.value.filter(t => t.id !== threadId)
    
    if (currentThread.value?.id === threadId) {
        currentThread.value = null
    }
}

// Set current thread
async function setCurrentThread(threadId) {
    const thread = await getThread(threadId)
    if (thread) {
        currentThread.value = thread
    }
    return thread
}

// Set current thread from router params (router-aware version)
async function setCurrentThreadFromRoute(threadId, router) {
    if (!threadId) {
        currentThread.value = null
        return null
    }

    const thread = await getThread(threadId)
    if (thread) {
        currentThread.value = thread
        return thread
    } else {
        // Thread not found, redirect to home
        if (router) {
            router.push((globalThis.ai?.base || '') + '/')
        }
        currentThread.value = null
        return null
    }
}

// Clear current thread (go back to initial state)
function clearCurrentThread() {
    currentThread.value = null
}

function getGroupedThreads(total) {
    const now = new Date()
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate())
    const lastWeek = new Date(today.getTime() - 7 * 24 * 60 * 60 * 1000)
    const lastMonth = new Date(today.getTime() - 30 * 24 * 60 * 60 * 1000)
    
    const groups = {
        today: [],
        lastWeek: [],
        lastMonth: [],
        older: {}
    }
    
    const takeThreads = threads.value.slice(0, total)

    takeThreads.forEach(thread => {
        const threadDate = new Date(thread.updatedAt)
        
        if (threadDate >= today) {
            groups.today.push(thread)
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

async function getAllRequests() {
    await initDB()

    const tx = db.transaction(['requests'], 'readonly')
    const store = tx.objectStore('requests')
    const allRequests = await store.getAll()
    return allRequests
}

async function getRequest(requestId) {
    await initDB()

    const tx = db.transaction(['requests'], 'readonly')
    const store = tx.objectStore('requests')
    const request = await store.get(requestId)
    return request
}

async function getAllRequestIds() {
    await initDB()
    
    const tx = db.transaction(['requests'], 'readonly')
    const store = tx.objectStore('requests')
    const ids = await store.getAllKeys()
    return ids
}

async function getAllThreadIds() {
    await initDB()
    const tx = db.transaction(['threads'], 'readonly')
    const store = tx.objectStore('threads')
    const ids = await store.getAllKeys()
    return ids    
}

// Query requests with pagination and filtering
async function getRequests(filters = {}, limit = 20, offset = 0) {
    try {
        await initDB()

        const {
            model = null,
            provider = null,
            threadId = null,
            sortBy = 'created',
            sortOrder = 'desc',
            startDate = null,
            endDate = null
        } = filters

        const tx = db.transaction(['requests'], 'readonly')
        const store = tx.objectStore('requests')

        // Get all requests and filter in memory (IndexedDB limitations)
        const allRequests = await store.getAll()

        // Apply filters
        let results = allRequests.filter(req => {
            if (model && req.model !== model) return false
            if (provider && req.provider !== provider) return false
            if (threadId && req.threadId !== threadId) return false
            if (startDate && req.created < startDate) return false
            if (endDate && req.created > endDate) return false
            return true
        })

        // Sort
        results.sort((a, b) => {
            let aVal = a[sortBy]
            let bVal = b[sortBy]

            if (sortOrder === 'desc') {
                return bVal - aVal
            } else {
                return aVal - bVal
            }
        })

        // Paginate
        const total = results.length
        const paginatedResults = results.slice(offset, offset + limit)

        return {
            requests: paginatedResults,
            total,
            hasMore: offset + limit < total
        }
    } catch (error) {
        console.error('Error in getRequests:', error)
        return {
            requests: [],
            total: 0,
            hasMore: false
        }
    }
}

// Get unique values for filter options
async function getFilterOptions() {
    try {
        await initDB()

        const tx = db.transaction(['requests'], 'readonly')
        const store = tx.objectStore('requests')
        const allRequests = await store.getAll()

        const models = [...new Set(allRequests.map(r => r.model).filter(m => m))].sort()
        const providers = [...new Set(allRequests.map(r => r.provider).filter(p => p))].sort()

        return {
            models,
            providers
        }
    } catch (error) {
        console.error('Error in getFilterOptions:', error)
        return {
            models: [],
            providers: []
        }
    }
}

// Delete a request by ID
async function deleteRequest(requestId) {
    await initDB()

    const tx = db.transaction(['requests'], 'readwrite')
    await tx.objectStore('requests').delete(requestId)
    await tx.complete
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
        initDB,
        logRequest,
        createThread,
        updateThread,
        addMessageToThread,
        deleteMessageFromThread,
        updateMessageInThread,
        redoMessageFromThread,
        loadThreads,
        getThread,
        deleteThread,
        setCurrentThread,
        setCurrentThreadFromRoute,
        clearCurrentThread,
        getGroupedThreads,
        getRequest,
        getRequests,
        getAllRequests,
        getFilterOptions,
        deleteRequest,
        getAllRequestIds,
        getAllThreadIds,
    }
}
