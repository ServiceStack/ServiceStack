const memory = new Map()
const pending = new Map()
const DAY = 24 * 60 * 60 * 1000
// Labels are the only tag values. This comparison also recognizes older lowercase
// and hyphenated values without modifying imported recipe documents.
export const tagKey = (value) => value.trim().toLowerCase().replace(/\s+/g, '-')
export function validCatalog(value) {
    return !!(
        value && Number.isInteger(value.version) && value.version > 0 &&
        Array.isArray(value.tags) && value.tags.length <= 100 &&
        value.tags.every(tag => tag && typeof tag.label === 'string' &&
            tag.label.trim() === tag.label && tag.label.length > 0 && tag.label.length <= 40 &&
            !/[\x00-\x1f,]/.test(tag.label) &&
            ['content', 'tag', 'context', 'task'].includes(tag.group) &&
            (value.version < 3 || !('name' in tag))) &&
        new Set(value.tags.map(tag => tagKey(tag.label))).size === value.tags.length
    )
}
const cleanCatalog = (value) => ({
    version: value.version,
    tags: value.tags.map(tag => ({ label: tag.label, group: tagGroup(tag) })),
})
export const canonicalTagLabel = (value, catalog = []) =>
    catalog.find(tag => tagKey(tag.label) === tagKey(value))?.label || value
// Public suggestions only. Credentials and recipe data never enter this cache.
export async function loadDecisionTags(api, scope = 'default', options = {}) {
    const key = 'jev:decision-tags:v3:' + scope
    let legacyCache = false
    const now = options.now ?? Date.now()
    let storage = options.storage
    if (storage === undefined)
        try {
            storage = globalThis.localStorage
        } catch {}
    let cached = memory.get(key)
    if (!cached)
        try {
            cached = JSON.parse(storage?.getItem(key) || 'null')
            if (!cached) {
                cached = JSON.parse(
                    storage?.getItem('jev:decision-tags:v2:' + scope) ||
                    storage?.getItem('jev:decision-tags:v1:' + scope) || 'null',
                )
                legacyCache = !!cached
            }
        } catch {}
    if (
        !cached ||
        !validCatalog(cached.catalog) ||
        !Number.isFinite(cached.savedAt) ||
        cached.savedAt > now
    )
        cached = null
    if (cached) cached = { ...cached, catalog: cleanCatalog(cached.catalog) }
    if (cached && !legacyCache && now - cached.savedAt < DAY)
        return cached.catalog
    if (pending.has(key)) return pending.get(key)
    const request = (async () => {
        try {
            const response = await api('/tags')
            if (!validCatalog(response)) throw Error('Invalid tag catalogue')
            const catalog = cleanCatalog(response)
            const entry = { savedAt: now, catalog }
            memory.set(key, entry)
            try {
                storage?.setItem(key, JSON.stringify(entry))
            } catch {}
            return catalog
        } catch {
            return cached?.catalog || { version: 3, tags: [] }
        }
    })()
    pending.set(key, request)
    try {
        return await request
    } finally {
        pending.delete(key)
    }
}
export function normalizeTags(text) {
    const seen = new Set()
    return text.split(',').map(tag => tag.trim().replace(/\s+/g, ' ')).filter(tag => {
        if (!tag || seen.has(tagKey(tag))) return false
        seen.add(tagKey(tag))
        return true
    })
}

export const tagGroup = (tag) =>
    ['context', 'content'].includes(tag.group) ? 'content' : 'tag'

// Older recipes mixed content types and task tags in one list. Keep the original
// document untouched until metadata is edited, then write the separate fields.
export function recipeMetadata(recipe, catalog = []) {
    const tags = (recipe.tags || []).map(tag => canonicalTagLabel(tag, catalog))
    if (typeof recipe.content === 'string')
        return { content: canonicalTagLabel(recipe.content, catalog), tags }
    const contentLabels = new Set(catalog.filter(tag => tagGroup(tag) === 'content').map(tag => tagKey(tag.label)))
    return {
        content: tags.find(tag => contentLabels.has(tagKey(tag))) || '',
        tags: tags.filter(tag => !contentLabels.has(tagKey(tag))),
    }
}
