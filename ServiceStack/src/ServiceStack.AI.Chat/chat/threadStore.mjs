import { ref, computed } from 'vue'
import { openDB } from 'idb'
import { nextId } from './utils.mjs'
// Thread store for managing chat threads with IndexedDB
const threads = ref([])
const currentThread = ref(null)
const isLoading = ref(false)
let db = null
// Initialize IndexedDB
async function initDB() {
    if (db) return db
    
    db = await openDB('LlmsThreads', 1, {
        upgrade(db) {
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
    })
    
    return db
}
// Generate unique thread ID
function generateThreadId() {
    return Date.now().toString()
}
// Create a new thread
async function createThread(title = 'New Chat', model = '', systemPrompt = '') {
    await initDB()
    
    const thread = {
        id: generateThreadId(),
        title: title,
        model: model,
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
// Add message to thread
async function addMessageToThread(threadId, message) {
    const thread = await getThread(threadId)
    if (!thread) throw new Error('Thread not found')
    
    const newMessage = {
        id: nextId(),
        timestamp: new Date().toISOString(),
        ...message
    }
    
    const updatedMessages = [...thread.messages, newMessage]
    
    // Auto-generate title from first user message if still "New Chat"
    let title = thread.title
    if (title === 'New Chat' && message.role === 'user' && updatedMessages.length <= 2) {
        title = message.content.slice(0, 200) + (message.content.length > 200 ? '...' : '')
    }
    
    await updateThread(threadId, {
        messages: updatedMessages,
        title: title
    })
    
    return newMessage
}
async function deleteMessageFromThread(threadId, messageId) {
    const thread = await getThread(threadId)
    if (!thread) throw new Error('Thread not found')
    const updatedMessages = thread.messages.filter(m => m.id !== messageId)
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
        createThread,
        updateThread,
        addMessageToThread,
        deleteMessageFromThread,
        loadThreads,
        getThread,
        deleteThread,
        setCurrentThread,
        setCurrentThreadFromRoute,
        clearCurrentThread,
        getGroupedThreads,
    }
}
