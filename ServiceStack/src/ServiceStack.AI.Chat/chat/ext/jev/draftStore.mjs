import { clone, uid } from './recipeModel.mjs'

// Private payloads never enter ExtensionScope's unscoped localStorage preferences.
const stores = new Map(),
    tab = uid()

export function createDraftStore(scope) {
    if (stores.has(scope)) return stores.get(scope)
    const versions = new Map(),
        recoveries = new Map()
    let db,
        failure = '',
        chain = Promise.resolve()
    const ready = new Promise((resolve) => {
        if (!globalThis.indexedDB) {
            failure = 'Draft recovery is unavailable in this browser.'
            resolve()
            return
        }
        let open
        try {
            open = indexedDB.open('llms-jev-drafts', 1)
        } catch {
            failure = 'Draft recovery is unavailable. Export important unsaved recipes.'
            resolve()
            return
        }
        open.onupgradeneeded = () => open.result.createObjectStore('drafts', { keyPath: 'key' })
        open.onerror = () => {
            failure = 'Draft recovery is unavailable. Export important unsaved recipes.'
            resolve()
        }
        open.onsuccess = () => {
            db = open.result
            resolve()
        }
    })
    const full = (key) => scope + '::' + key
    async function load(key) {
        await chain
        await ready
        if (!db) return null
        return new Promise((resolve) => {
            const request = db.transaction('drafts').objectStore('drafts').get(full(key))
            request.onerror = () => resolve(null)
            request.onsuccess = () => {
                versions.set(key, request.result?.version)
                resolve(request.result?.value || null)
            }
        })
    }
    function save(key, value) {
        const snapshot = clone(value),
            ownerKey = full(key)
        chain = chain
            .catch(() => {})
            .then(async () => {
                await ready
                if (!db) return
                await new Promise((resolve) => {
                    const tx = db.transaction('drafts', 'readwrite'),
                        store = tx.objectStore('drafts'),
                        get = store.get(ownerKey)
                    get.onsuccess = () => {
                        const old = get.result
                        if (old && old.version !== versions.get(key) && old.tab !== tab) {
                            const recoveryKey = recoveries.get(key) || 'local:' + uid()
                            recoveries.set(key, recoveryKey)
                            store.put({
                                key: full(recoveryKey),
                                value: {
                                    ...snapshot,
                                    id: recoveryKey,
                                    revision: 0,
                                    savedDocument: '',
                                    recovery: true,
                                },
                                version: uid(),
                                tab,
                            })
                            failure =
                                'Another tab changed this draft. Your edits were kept as a recovery recipe.'
                            return
                        }
                        const version = uid()
                        versions.set(key, version)
                        store.put({ key: ownerKey, value: snapshot, version, tab })
                    }
                    tx.oncomplete = resolve
                    tx.onabort = tx.onerror = () => {
                        failure = 'Could not save the browser draft. Export important unsaved recipes.'
                        resolve()
                    }
                })
            })
        return chain
    }
    async function list() {
        await chain
        await ready
        if (!db) return []
        return new Promise((resolve) => {
            const request = db.transaction('drafts').objectStore('drafts').getAll()
            request.onerror = () => resolve([])
            request.onsuccess = () =>
                resolve(
                    request.result
                        .filter((row) => row.key.startsWith(scope + '::local:'))
                        .map((row) => {
                            const key = row.key.slice(scope.length + 2)
                            versions.set(key, row.version)
                            return { key, value: row.value }
                        }),
                )
        })
    }
    async function remove(key) {
        await chain
        await ready
        if (db)
            await new Promise((resolve) => {
                const tx = db.transaction('drafts', 'readwrite')
                tx.objectStore('drafts').delete(full(key))
                tx.oncomplete = resolve
                tx.onerror = resolve
            })
        versions.delete(key)
    }
    const store = { load, save, list, remove, flush: () => chain, error: () => failure }
    stores.set(scope, store)
    return store
}
