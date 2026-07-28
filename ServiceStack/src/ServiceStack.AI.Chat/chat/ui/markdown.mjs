import { Marked } from "marked"
import hljs from "highlight.js"

export function createMarked() {
    const aliases = {
        vue: 'html',
    }

    const ret = new Marked(
        markedHighlight({
            langPrefix: 'hljs language-',
            highlight(code, lang, info) {
                if (aliases[lang]) {
                    lang = aliases[lang]
                }
                const language = hljs.getLanguage(lang) ? lang : 'plaintext'
                return hljs.highlight(code, { language }).value
            }
        })
    )
    ret.use({ extensions: [thinkTag()] })
    //ret.use({ extensions: [divExtension()] })
    return ret
}

export const marked = createMarked()
export const markedFallback = createMarked()

export function renderMarkdown(content) {
    if (Array.isArray(content)) {
        content = content.filter(c => c.type === 'text').map(c => c.text).join('\n')
    }
    if (content) {
        content = content
            .replaceAll(`\\[ \\boxed{`, '\n<span class="inline-block text-xl text-blue-500 bg-blue-50 dark:text-blue-400 dark:bg-blue-950 px-3 py-1 rounded">')
            .replaceAll('} \\]', '</span>\n')
    }
    return marked.parse(content || '')
}

// export async function renderMarkdown(body) {
//     const rawHtml = marked.parse(body)
//     return <main dangerouslySetInnerHTML={{ __html: rawHtml }} />
// }

export function markedHighlight(options) {
    if (typeof options === 'function') {
        options = {
            highlight: options
        }
    }

    if (!options || typeof options.highlight !== 'function') {
        throw new Error('Must provide highlight function')
    }

    if (typeof options.langPrefix !== 'string') {
        options.langPrefix = 'language-'
    }

    return {
        async: !!options.async,
        walkTokens(token) {
            if (token.type !== 'code') {
                return
            }

            const lang = getLang(token.lang)

            if (options.async) {
                return Promise.resolve(options.highlight(token.text, lang, token.lang || '')).then(updateToken(token))
            }

            const code = options.highlight(token.text, lang, token.lang || '')
            if (code instanceof Promise) {
                throw new Error('markedHighlight is not set to async but the highlight function is async. Set the async option to true on markedHighlight to await the async highlight function.')
            }
            updateToken(token)(code)
        },
        renderer: {
            code(code, infoString) {
                const lang = getLang(infoString)
                let text = code.text
                const classAttr = lang
                    ? ` class="${options.langPrefix}${escape(lang)}"`
                    : ' class="hljs"';
                text = text.replace(/\n$/, '')
                return `<pre><code${classAttr}>${code.escaped ? text : escape(text, true)}\n</code></pre>`
            }
        }
    }
}

function getLang(lang) {
    return (lang || '').match(/\S*/)[0]
}

function updateToken(token) {
    return code => {
        if (typeof code === 'string' && code !== token.text) {
            token.escaped = true
            token.text = code
        }
    }
}

// copied from marked helpers
const escapeTest = /[&<>"']/
const escapeReplace = new RegExp(escapeTest.source, 'g')
const escapeTestNoEncode = /[<>"']|&(?!(#\d{1,7}|#[Xx][a-fA-F0-9]{1,6}|\w+);)/
const escapeReplaceNoEncode = new RegExp(escapeTestNoEncode.source, 'g')
const escapeReplacements = {
    '&': '&amp;',
    '<': '&lt;',
    '>': '&gt;',
    '"': '&quot;',
    "'": '&#39;'
}
const getEscapeReplacement = ch => escapeReplacements[ch]
function escape(html, encode) {
    if (encode) {
        if (escapeTest.test(html)) {
            return html.replace(escapeReplace, getEscapeReplacement)
        }
    } else {
        if (escapeTestNoEncode.test(html)) {
            return html.replace(escapeReplaceNoEncode, getEscapeReplacement)
        }
    }

    return html
}

/**
 * Marked.js extension for rendering <think> tags as expandable, scrollable components
 * using Tailwind CSS
 */

// Extension for Marked.js to handle <think> tags
function thinkTag() {
    globalThis.toggleThink = toggleThink
    return ({
        name: 'thinkTag',
        level: 'block',
        start(src) {
            return src.match(/^<think>/)?.index;
        },
        tokenizer(src) {
            const rule = /^<think>([\s\S]*?)<\/think>/
            const match = rule.exec(src)
            if (match) {
                return {
                    type: 'thinkTag',
                    raw: match[0],
                    content: match[1].trim(),
                }
            }
            return undefined
        },
        renderer(token) {
            // Parse the markdown content inside the think tag
            const parsedContent = marked.parse(token.content)

            // Generate a unique ID for this think component
            const uniqueId = 'think-' + Math.random().toString(36).substring(2, 10)

            // Create the expandable, scrollable component with Tailwind CSS
            return `
    <div class="my-4 border border-gray-200 dark:border-gray-700 rounded-lg shadow-sm">
      <button type="button"
        id="${uniqueId}-toggle"
        class="flex justify-between items-center w-full py-2 px-4 text-left text-gray-700 dark:text-gray-300 font-medium hover:bg-gray-50 dark:hover:bg-gray-700 focus:outline-none"
        onclick="toggleThink('${uniqueId}')">
        <span>Thinking</span>
        <svg id="${uniqueId}-icon" class="h-5 w-5 text-gray-500 dark:text-gray-400 transform transition-transform" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"></path>
        </svg>
      </button>
      <div
        id="${uniqueId}-content"
        class="hidden overflow-auto max-h-64 px-4 border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900"
        style="max-height:16rem;">
        ${parsedContent}
      </div>
    </div>
    `
        }
    })
}

// JavaScript function to toggle the visibility of the think content
function toggleThink(id) {
    const content = document.getElementById(`${id}-content`)
    const icon = document.getElementById(`${id}-icon`)

    if (content.classList.contains('hidden')) {
        content.classList.remove('hidden')
        icon.classList.add('rotate-180')
    } else {
        content.classList.add('hidden')
        icon.classList.remove('rotate-180')
    }
}

export function markdownFormatters() {
    return {
        markdown: renderMarkdown
    }
}