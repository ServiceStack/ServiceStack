import { activeRun } from './recipeModel.mjs'

export function createApi(ext) {
    return async function api(path, method = 'GET', body) {
        const response = await ext.get(path, {
            method,
            headers: { 'Content-Type': 'application/json' },
            ...(body === undefined ? {} : { body: JSON.stringify(body) }),
        })
        let result
        try {
            result = await response.json()
        } catch {
            throw Error('The server returned an unreadable response. Check your connection and try again.')
        }
        if (!response.ok || result.responseStatus?.errorCode) {
            const status = result.responseStatus || {}
            const error = Error(status.message || 'Request failed.')
            error.status = response.status
            error.fields = status.errors
            error.code = status.errorCode
            error.existingRecipe = status.existingRecipe
            throw error
        }
        return result
    }
}

export function createRunTracker(api, onUpdate) {
    const tracked = new Map()
    let disposed = false
    function follow(run, origin) {
        if (disposed) return
        onUpdate(run, origin)
        if (!activeRun(run)) return
        if (tracked.has(run.id)) return
        const entry = { run, origin, timer: null, failures: 0 }
        tracked.set(run.id, entry)
        async function poll() {
            if (disposed) return
            if (globalThis.document?.hidden) {
                entry.timer = setTimeout(poll, 2000)
                return
            }
            try {
                const current = await api('/runs/' + encodeURIComponent(run.id))
                if (disposed) return
                entry.run = current
                entry.failures = 0
                onUpdate(current, entry.origin)
                if (!activeRun(current)) {
                    tracked.delete(run.id)
                    return
                }
            } catch (e) {
                if (disposed) return
                entry.failures++
                onUpdate({ ...entry.run, trackingError: e.message }, entry.origin)
                if (entry.failures >= 5) {
                    tracked.delete(run.id)
                    onUpdate(
                        {
                            ...entry.run,
                            trackingError: 'Status tracking paused. Refresh history to reconnect.',
                        },
                        entry.origin,
                    )
                    return
                }
            }
            entry.timer = setTimeout(poll, Math.min(5000, 800 * 2 ** entry.failures))
        }
        entry.timer = setTimeout(poll, 500)
    }
    return {
        follow,
        retarget(oldOrigin, newOrigin) {
            for (const entry of tracked.values()) if (entry.origin === oldOrigin) entry.origin = newOrigin
        },
        dispose() {
            disposed = true
            for (const entry of tracked.values()) clearTimeout(entry.timer)
            tracked.clear()
        },
    }
}
