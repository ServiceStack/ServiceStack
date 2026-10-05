// Cache directory listings independently of selection; never fetch collapsed children.
export function createWorkspaceTree(state, fetchDirectory) {
    let epoch = 0
    const pending = new Map()
    function node(path) {
        if (!state.nodes[path]) state.nodes[path] = { path, expanded: false, loading: false, error: '', response: null }
        // Read back through reactive state: an assignment expression returns the raw object,
        // whose async mutations would otherwise bypass Vue's rendering notifications.
        return state.nodes[path]
    }
    async function ensure(path = '') {
        const item = node(path)
        if (item.response) return item
        if (pending.has(path)) return pending.get(path)
        const current = epoch
        item.loading = true; item.error = ''
        const work = (async () => {
            try {
                const response = await fetchDirectory(path)
                if (current !== epoch) return null
                item.path = response.path || path
                item.response = response
                if (response.path) state.nodes[response.path] = item
                return item
            } catch (e) {
                if (current === epoch) item.error = e.message
                throw e
            } finally {
                if (current === epoch) { item.loading = false; pending.delete(path) }
            }
        })()
        pending.set(path, work)
        return work
    }
    function reset() { epoch++; pending.clear(); state.nodes = {} }
    return { node, ensure, reset }
}
