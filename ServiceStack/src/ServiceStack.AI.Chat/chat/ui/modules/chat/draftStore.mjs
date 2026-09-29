import { reactive, computed, watch } from 'vue'

// Drafts are private browser state and never part of thread/API projections.
export function createDraftStore(scope) {
    const records = reactive({})
    // Remember the persisted version we actually loaded, so normal edits after
    // a reload are not mistaken for concurrent changes from another tab.
    const loadedVersions = new Map()
    const version = draft => JSON.stringify([draft.tabId, draft.updatedAt, draft.revision])
    const tabId = globalThis.crypto?.randomUUID?.() || String(Math.random())
    const state = reactive({ key: 'local:initial' })
    let db, timer
    const ready = new Promise(resolve => {
        if (!globalThis.indexedDB) return resolve()
        let request
        try { request = indexedDB.open('llms-chat-drafts', 1) } catch { resolve(); return }
        request.onupgradeneeded = () => request.result.createObjectStore('drafts', { keyPath: 'key' })
        request.onerror = () => resolve()
        request.onsuccess = () => { db = request.result; resolve() }
    })
    const fullKey = (key, owner = scope()) => `${owner}::${key}`
    function get(key = state.key) {
        const id = fullKey(key)
        if (!records[id]) {
            records[id] = { key, scope: scope(), text: '', attachments: [], editingMessage: null, projectId: null,
                revision: 0, updatedAt: 0, tabId }
            const draft = records[id]
            ready.then(() => {
                if (!db) return
                const request = db.transaction('drafts').objectStore('drafts').get(id)
                request.onsuccess = () => {
                    if (request.result && draft.revision === 0) {
                        loadedVersions.set(id, version(request.result.value))
                        Object.assign(draft, request.result.value)
                    }
                }
            })
        }
        return records[id]
    }
    function touch(draft) { draft.revision++; draft.updatedAt = Date.now(); draft.tabId = tabId }
    function bind(key) {
        flush(); state.key = key || 'local:initial'
        if (state.key.startsWith('local:')) {
            try { sessionStorage.setItem(fullKey('active'), state.key) } catch {}
        }
        return get()
    }
    try { state.key = sessionStorage.getItem(fullKey('active')) || state.key } catch {}
    function fresh(projectId = null) {
        const key = `local:${globalThis.crypto?.randomUUID?.() || Date.now() + ':' + Math.random()}`
        bind(key); get().projectId = projectId; touch(get()); return key
    }
    function transfer(key, threadId, draft = get(key)) {
        const owner = draft.scope
        records[fullKey(String(threadId), owner)] = draft
        draft.key = String(threadId)
        if (scope() === owner && state.key === key) state.key = String(threadId)
        delete records[fullKey(key, owner)]
        ready.then(() => { if (db) db.transaction('drafts', 'readwrite').objectStore('drafts').delete(fullKey(key, owner)) })
        return draft
    }
    function adapter(field) {
        return computed({ get: () => get()[field], set: value => { get()[field] = value; touch(get()) } })
    }
    function snapshot() {
        const draft = get()
        return { draft, revision: draft.revision, text: draft.text, attachments: [...draft.attachments],
            editingMessage: draft.editingMessage, key: state.key, projectId: draft.projectId }
    }
    function accepted(sent) {
        const draft = sent.draft
        if (draft.revision === sent.revision && draft.text === sent.text && draft.editingMessage === sent.editingMessage) {
            draft.text = ''; draft.editingMessage = null
        }
        const ids = new Set(sent.attachments.map(f => f.id || f.url))
        draft.attachments = draft.attachments.filter(f => !ids.has(f.id || f.url))
        touch(draft)
    }
    async function discard(key) {
        const id = fullKey(key), draft = records[id]
        for (const file of draft?.attachments || []) if (file.preview) URL.revokeObjectURL(file.preview)
        delete records[id]
        loadedVersions.delete(id)
        await ready
        if (db) db.transaction('drafts', 'readwrite').objectStore('drafts').delete(id)
    }
    function remove(key, id) {
        const draft = get(key), file = draft.attachments.find(f => f.id === id)
        if (file?.preview?.startsWith('blob:')) URL.revokeObjectURL(file.preview)
        draft.attachments = draft.attachments.filter(f => f.id !== id)
        touch(draft)
    }
    async function attach(key, files, upload) {
        const draft = get(key)
        const entries = Array.from(files).map(file => ({ id: globalThis.crypto.randomUUID(), name: file.name,
            type: file.type, file, state: 'uploading', preview: file.type.startsWith('image/') ? URL.createObjectURL(file) : null }))
        draft.attachments.push(...entries); touch(draft)
        await Promise.all(entries.map(async entry => {
            try {
                const result = await upload(entry.file)
                const current = draft.attachments.find(f => f.id === entry.id)
                if (!current) return
                if (current.preview) URL.revokeObjectURL(current.preview)
                Object.assign(current, result, { state: 'ready', preview: null, file: null })
            } catch (error) {
                const current = draft.attachments.find(f => f.id === entry.id)
                if (current) Object.assign(current, { state: 'failed', error: String(error.message || error) })
            }
            touch(draft)
        }))
    }
    async function retry(key, id, upload) {
        const draft = get(key), entry = draft.attachments.find(f => f.id === id)
        if (!entry?.file || entry.state !== 'failed') return
        entry.state = 'uploading'; touch(draft)
        try {
            const result = await upload(entry.file)
            const current = draft.attachments.find(f => f.id === id)
            if (current) {
                if (current.preview) URL.revokeObjectURL(current.preview)
                Object.assign(current, result, {state:'ready', preview:null, file:null, error:null})
            }
        } catch (error) {
            const current = draft.attachments.find(f => f.id === id)
            if (current) Object.assign(current, {state:'failed', error:String(error.message || error)})
        }
        touch(draft)
    }
    async function flush() {
        clearTimeout(timer)
        await ready
        if (!db) return
        try {
            const tx = db.transaction('drafts', 'readwrite'), store = tx.objectStore('drafts')
            for (const [key, draft] of Object.entries(records)) {
                const value = JSON.parse(JSON.stringify({ ...draft, sending: undefined, attachments: draft.attachments.map(f => ({
                    ...f, file: undefined, preview: undefined,
                    state: f.state === 'uploading' ? 'failed' : f.state,
                    error: f.state === 'uploading' ? 'Please reattach this file' : f.error,
                })) }))
                const request = store.get(key)
                request.onsuccess = () => {
                    const previous = request.result?.value
                    if (previous && previous.tabId !== tabId && version(previous) !== loadedVersions.get(key) && previous.updatedAt !== value.updatedAt && !value.recovered) {
                        const losing = previous.updatedAt > value.updatedAt ? value : previous
                        const recoveryKey = `local:recovery:${losing.key}:${losing.tabId}`
                        const recovery = {...losing, key:recoveryKey, text:losing.text, recovered:true}
                        store.put({key:fullKey(recoveryKey, losing.scope), value:recovery})
                        const recoveryId = fullKey(recoveryKey, losing.scope)
                        if (!records[recoveryId] || records[recoveryId].updatedAt < recovery.updatedAt) records[recoveryId] = recovery
                        if (previous.updatedAt > value.updatedAt) return
                    }
                    store.put({ key, value })
                    loadedVersions.set(key, version(value))
                }
            }
        } catch { /* Quota/private mode: retain the in-memory draft. */ }
    }
    function list() {
        return Object.entries(records).filter(([key]) => key.startsWith(scope() + '::'))
            .map(([,draft]) => draft).filter(draft => draft.key.startsWith('local:') && (draft.text || draft.attachments.length || draft.projectId))
    }
    async function loadScope() {
        const owner = scope()
        await ready
        if (!db) return
        const request = db.transaction('drafts').objectStore('drafts').getAll()
        request.onsuccess = () => {
            for (const stored of request.result || []) {
                if (!stored.key.startsWith(owner + '::') || records[stored.key]) continue
                loadedVersions.set(stored.key, version(stored.value))
                records[stored.key] = stored.value
            }
        }
    }
    watch(scope, loadScope, {immediate:true})
    watch(records, () => { clearTimeout(timer); timer = setTimeout(flush, 250) }, { deep: true })
    globalThis.addEventListener?.('pagehide', flush)
    return { state, get, list, bind, fresh, transfer, adapter, snapshot, accepted, attach, retry, remove, discard, touch, flush }
}
