/**
 * useSchemas - fetches JSON Schemas on demand, instead of loading the whole App metadata.
 *
 *     const { model, api, loadModel } = useSchemas()
 *     const coupon = model('Coupon')      // undefined until it arrives, then reactive
 *     await loadModel('Coupon')           // or await it directly
 *
 * Two endpoints, both already served as JSON:
 *   /auto/{Model}.json          a Data Model + the CRUD API schemas available on it
 *   /schema/{RequestDto}.json   a single API schema
 *
 * Results are cached per name and in-flight requests are shared, so a grid of 25 rows all
 * referencing the same Model fetches it once. Failures cache as null rather than retrying
 * on every render.
 *
 * Depends only on vue - the fetch is plain, so no client needs providing.
 */

import { reactive } from "vue"

/**
 * The schema describing the rows a Model's Query API returns. IQueryDb<From,Into> projects
 * into a different shape to the table it reads, and the server emits that as `viewModel`;
 * everything else queries the Data Model itself. Grids, column lists and cell formatting all
 * describe rows, so they all go through here - only the write forms use `model`.
 */
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
        /** the Model's schema, or undefined while it loads - starts the fetch on first ask */
        model(name) {
            if (name && !(name in cache.models)) load(cache.models, name, `/auto/${name}.json`)
            return name ? cache.models[name] : null
        },
        /** the API's schema, or undefined while it loads */
        api(request) {
            if (request && !(request in cache.apis)) load(cache.apis, request, `/schema/${request}.json`)
            return request ? cache.apis[request] : null
        },
        loadModel: name => load(cache.models, name, `/auto/${name}.json`),
        loadApi: request => load(cache.apis, request, `/schema/${request}.json`),
    }
}
