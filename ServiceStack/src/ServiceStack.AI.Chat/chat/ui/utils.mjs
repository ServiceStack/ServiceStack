import { toRaw } from "vue"
import { rightPart, toDate } from "@servicestack/client"

export function toJsonArray(json) {
    try {
        return json ? JSON.parse(json) : []
    } catch (e) {
        return []
    }
}

export function toJsonObject(json) {
    try {
        return json ? JSON.parse(json) : null
    } catch (e) {
        return null
    }
}

export function tryParseJson(str) {
    try {
        return JSON.parse(str)
    } catch (e) {
        return null
    }
}
export function hasJsonStructure(str) {
    return tryParseJson(str) != null
}

const escapeHtml = s => String(s)
    .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')

/**
 * Minimal JSON highlighter. A response is the whole point of this page, so it earns colour;
 * pulling in a syntax highlighter for one language does not.
 */
export function highlightJson(json) {
    return escapeHtml(json).replace(
        /("(\\u[a-zA-Z0-9]{4}|\\[^u]|[^\\"])*"(\s*:)?|\b(true|false|null)\b|-?\d+(?:\.\d*)?(?:[eE][+-]?\d+)?)/g,
        match => {
            const cls = /^"/.test(match)
                ? (/:$/.test(match) ? 'text-sky-700 dark:text-sky-300' : 'text-emerald-700 dark:text-emerald-300')
                : /true|false/.test(match) ? 'text-purple-700 dark:text-purple-300'
                : /null/.test(match) ? 'text-gray-400 dark:text-gray-500'
                : 'text-amber-700 dark:text-amber-300'
            return `<span class="${cls}">${match}</span>`
        })
}

export function storageArray(key, save) {
    if (save && Array.isArray(save)) {
        localStorage.setItem(key, JSON.stringify(save))
    }
    return toJsonArray(localStorage.getItem(key)) ?? []
}

export function storageObject(key, save) {
    if (typeof save == 'object') {
        localStorage.setItem(key, JSON.stringify(save))
    }
    return toJsonObject(localStorage.getItem(key)) ?? {}
}

export function fileToBase64(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader()
        reader.readAsDataURL(file) //= "data:…;base64,…"
        reader.onload = () => {
            resolve(rightPart(reader.result, ',')) // strip prefix
        }
        reader.onerror = err => reject(err)
    })
}

export function fileToDataUri(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader()
        reader.readAsDataURL(file) //= "data:…;base64,…"
        reader.onload = () => resolve(reader.result)
        reader.onerror = err => reject(err)
    })
}

export function serializedClone(obj) {
    try {
        return JSON.parse(JSON.stringify(obj))
    } catch (e) {
        console.warn('Deep cloning failed, returning original value:', e)
        return obj
    }
}

export function deepClone(o) {
    if (o === null || typeof o !== 'object') return o

    // Handle Array objects
    if (Array.isArray(o)) {
        return o.map(x => deepClone(x))
    }

    if (typeof structuredClone === 'function') { // available (modern browsers, Node.js 17+)
        try {
            return structuredClone(o)
        } catch (e) {
            console.warn('structuredClone failed, falling back to JSON:', e)
            console.log(o)
            // console.log(JSON.stringify(o, undefined, 2))
        }
    }

    // Fallback to JSON stringify/parse for older environments
    return serializedClone(o)
}

export function toModelInfo(model) {
    if (!model) return undefined
    const props = ['id', 'name', 'provider', 'cost', 'modalities']
    const to = {}
    props.forEach(k => to[k] = toRaw(model[k]))
    return deepClone(to)
}

export function pluralize(word, count) {
    return count === 1 ? word : word + 's'
}

const currFmt2 = new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD', maximumFractionDigits: 2 })
const currFmt6 = new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD', maximumFractionDigits: 6 })

export function tokenCost(price, tokens = 1000000) {
    if (!price) return ''
    var ret = currFmt2.format(parseFloat(price) * (tokens / 1000000))
    return ret.endsWith('.00') ? ret.slice(0, -3) : ret
}
export function tokenCostLong(price, tokens = 1000000) {
    if (!price) return ''
    const ret = currFmt6.format(parseFloat(price) * (tokens / 1000000))
    return ret.endsWith('.000000') ? ret.slice(0, -7) : ret
}
export function formatCost(cost) {
    if (!cost) return ''
    return currFmt2.format(parseFloat(cost))
}
export function tokensTitle(usage) {
    let title = []
    if (usage.tokens && usage.price) {
        const msg = parseFloat(usage.cost) > 0
            ? `${usage.tokens} tokens @ ${usage.price} = ${tokenCostLong(usage.cost)}`
            : `${usage.tokens} tokens`
        const duration = usage.duration ? ` in ${usage.duration}ms` : ''
        title.push(msg + duration)
    }
    return title.join('\n')
}


// Accessible in views via $fmt
export function utilsFormatters() {
    function relativeTime(timestamp) {
        const now = new Date()
        const date = new Date(timestamp)
        const diffInSeconds = Math.floor((now - date) / 1000)

        if (diffInSeconds < 60) return 'Just now'
        if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m ago`
        if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h ago`
        if (diffInSeconds < 604800) return `${Math.floor(diffInSeconds / 86400)}d ago`

        return date.toLocaleDateString()
    }
    function costLong(cost) {
        if (!cost) return ''
        const ret = currFmt6.format(parseFloat(cost))
        return ret.endsWith('.000000') ? ret.slice(0, -7) : ret
    }
    function statsTitle(stats) {
        let title = []
        // Each stat on its own line
        if (stats.cost) {
            title.push(`Total Cost: ${costLong(stats.cost)}`)
        }
        if (stats.inputTokens) {
            title.push(`Input Tokens: ${stats.inputTokens}`)
        }
        if (stats.outputTokens) {
            title.push(`Output Tokens: ${stats.outputTokens}`)
        }
        if (stats.requests) {
            title.push(`Requests: ${stats.requests}`)
        }
        if (stats.duration) {
            title.push(`Duration: ${stats.duration}ms`)
        }
        return title.join('\n')
    }

    function time(timestamp) {
        return new Date(timestamp).toLocaleTimeString([], {
            hour: '2-digit',
            minute: '2-digit'
        })
    }

    function shortDate(ts) {
        if (!ts) return ''
        const date = typeof ts === 'number' ? new Date(ts * 1000) : new Date(ts)
        return date.toLocaleDateString(undefined, {
            month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit'
        })
    }

    function date(d) {
        date = toDate(d)
        return date.toLocaleDateString(undefined, {
            month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit'
        })
    }


    return {
        currFmt: currFmt2,
        tokenCost,
        tokenCostLong,
        tokensTitle,
        cost: formatCost,
        costLong,
        statsTitle,
        relativeTime,
        time,
        pluralize,
        shortDate,
        date,
    }
}

const htmlStartTags = ['<!doctype', '<html', '<head', '<body', '<script', '<style', '<link']
export function isHtml(s) {
    if (!s || typeof s != 'string') return false
    const lower = s.toLowerCase().trim()
    const isHtml = htmlStartTags.some(tag => lower.startsWith(tag))
    return isHtml
}

const htmlEntities = {
    '&': '&amp;',
    '<': '&lt;',
    '>': '&gt;',
    '"': '&quot;',
    "'": '&#39;'
}

export function encodeHtml(str) {
    if (typeof str !== 'string' || !str) return ''
    return str.replace(/[&<>"']/g, m => htmlEntities[m]);
}

/**
 * @param {object|array} type 
 * @param {'div'|'table'|'thead'|'th'|'tr'|'td'} tag 
 * @param {number} depth 
 * @param {string} cls 
 * @param {number} index 
*/
function htmlFormatClasses(type, tag, depth, cls, index) {
    cls = cls.replace('shadow ring-1 ring-black/5 md:rounded-lg', '')
    if (tag == 'th') {
        cls += ' lowercase'
    }
    if (tag == 'td') {
        cls += ' whitespace-pre-wrap'
    }
    return cls
}

const dangerousTags = [
    'script',
    'iframe',
    'object',
    'embed',
    'link',
    'style',
    'meta',
    'base',
    'frame',
    'frameset',
    'applet',
    'noscript',
    'template'
]
const anyDangerousTag = new RegExp(`<(${dangerousTags.join('|')})`, 'i')

export function sanitizeHtml(html) {
    if (!html || typeof html !== 'string') return html

    let result = html
    let lowerResult = result.toLowerCase()
    function updateResult(r) {
        result = r
        lowerResult = result.toLowerCase()
    }

    if (anyDangerousTag.test(lowerResult)) {
        for (const tag of dangerousTags) {
            const tagOpen = `<${tag}`

            if (lowerResult.indexOf(tagOpen) === -1) continue

            const regex = new RegExp(`<${tag}[^>]*>([\\s\\S]*?)<\\/${tag}>`, 'gi')
            updateResult(result.replace(regex, ''))

            if (lowerResult.indexOf(tagOpen) !== -1) {
                const selfClosingRegex = new RegExp(`<${tag}[^>]*\\/?>`, 'gi')
                updateResult(result.replace(selfClosingRegex, ''))
            }
        }
    }

    return result
}

export function toKebabCase(str) {
    if (!str) return ''
    return str
        .replace(/[^\w\s-]/g, '')
        .trim()
        .replace(/[\s_]+/g, '-')
        .replace(/-+/g, '-')
        .toLowerCase()
}

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

export function fnv1a(str) {
    let hash = 0x811c9dc5
    for (let i = 0; i < str.length; i++) {
        hash ^= str.charCodeAt(i)
        hash = Math.imul(hash, 0x01000193)
    }
    return hash >>> 0
}
export const hashString = fnv1a

export const sleep = (ms) => new Promise(resolve => setTimeout(resolve, ms))

export function idToName(id) {
    return id.split(/[-_]/).map(word =>
        word.charAt(0).toUpperCase() + word.slice(1)
    ).join(' ')
}

export function utilsFunctions() {
    return {
        nextId,
        toJsonArray,
        toJsonObject,
        tryParseJson,
        hasJsonStructure,
        escapeHtml,
        highlightJson,
        storageArray,
        storageObject,
        fileToBase64,
        fileToDataUri,
        serializedClone,
        deepClone,
        toModelInfo,
        pluralize,
        isHtml,
        htmlFormatClasses,
        encodeHtml,
        sanitizeHtml,
        toKebabCase,
        hashString,
        sleep,
        idToName,
    }
}
