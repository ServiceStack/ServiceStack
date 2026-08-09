
import { reactive } from "vue"

export const rowSchema = auto => auto?.viewModel ?? auto?.model ?? null
const cache = reactive({ models: {}, apis: {} })
const inflight = new Map()
function load(store, name, url) {
    if (!name) return Promise.resolve(null)
    if (name in store) return Promise.resolve(store[name])
    if (!inflight.has(url)) {
        inflight.set(url, fetch(url, { headers: { Accept: 'application/json' } })
            .then(res => res.ok ? res.json() : null)
            .catch(() => null)
            .then(value => {
                store[name] = value
                inflight.delete(url)
                return value
            }))
    }
    return inflight.get(url)
}
export function useSchemas() {
    return {

        model(name) {
            if (name && !(name in cache.models)) load(cache.models, name, `/auto/${name}.json`)
            return name ? cache.models[name] : null
        },

        api(request) {
            if (request && !(request in cache.apis)) load(cache.apis, request, `/schema/${request}.json`)
            return request ? cache.apis[request] : null
        },
        loadModel: name => load(cache.models, name, `/auto/${name}.json`),
        loadApi: request => load(cache.apis, request, `/schema/${request}.json`),
    }
}
