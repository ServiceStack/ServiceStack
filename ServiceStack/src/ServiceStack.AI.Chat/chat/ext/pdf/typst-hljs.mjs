/**
 * highlight.js language definition for typst (https://typst.app).
 *
 * The bundled highlight.js is the "common" subset, which has no typst grammar, so ```typst blocks in
 * chat render as plaintext. Registering this gives them the same treatment as every other language.
 */

const KEYWORDS = {
    keyword: 'let set show import include as in while for if else return break continue context',
    literal: 'none auto true false',
    built_in:
        'text par page heading strong emph raw link ref label figure image table grid stack box block ' +
        'align pad move place rotate scale hide repeat line rect square circle ellipse polygon path ' +
        'list enum terms quote cite bibliography footnote outline columns colbreak pagebreak parbreak ' +
        'linebreak smartquote lorem measure layout locate style counter state query selector ' +
        'json yaml toml csv xml cbor read eval str int float bool array dict type repr panic assert ' +
        'calc sys datetime duration regex symbol color gradient luma rgb cmyk oklab oklch',
}

export default function typst(hljs) {
    const COMMENT = [hljs.COMMENT('//', '$'), hljs.COMMENT('/\\*', '\\*/', { contains: ['self'] })]

    const STRING = {
        className: 'string',
        begin: '"',
        end: '"',
        contains: [hljs.BACKSLASH_ESCAPE],
    }

    // 12pt, 2.5cm, 45deg, 1fr, 50%
    const NUMBER = {
        className: 'number',
        begin: /\b\d+(\.\d+)?(pt|mm|cm|in|em|deg|rad|fr|%)?\b/,
        relevance: 0,
    }

    // a code expression introduced from markup: #let, #show, #text(..), #data.title
    const HASH_EXPR = {
        className: 'meta',
        begin: /#[a-zA-Z_][\w-]*(\.[a-zA-Z_][\w-]*)*/,
        keywords: KEYWORDS,
        relevance: 10,
    }

    return {
        name: 'Typst',
        aliases: ['typ'],
        case_insensitive: false,
        keywords: KEYWORDS,
        contains: [
            ...COMMENT,
            STRING,
            // headings: = Title, == Section
            { className: 'section', begin: /^\s*=+\s+.*$/, relevance: 10 },
            // raw blocks and inline raw
            { className: 'code', begin: /```/, end: /```/, relevance: 5 },
            { className: 'code', begin: /`[^`\n]*`/ },
            // math mode
            { className: 'symbol', begin: /\$/, end: /\$/, relevance: 5 },
            // <label> and @reference
            { className: 'symbol', begin: /<[a-zA-Z_][\w-]*>/ },
            { className: 'symbol', begin: /@[a-zA-Z_][\w-]*/ },
            // *strong* and _emphasis_
            { className: 'strong', begin: /\*[^*\n]+\*/ },
            { className: 'emphasis', begin: /_[^_\n]+_/ },
            // escapes: \#, \*, \$ ...
            { className: 'literal', begin: /\\[#*_$`@<\\]/ },
            HASH_EXPR,
            NUMBER,
        ],
    }
}

/** Register the grammar once, tolerating a highlight.js build that already knows typst */
export function registerTypst(hljs) {
    if (!hljs || hljs.getLanguage('typst')) return false
    hljs.registerLanguage('typst', typst)
    return true
}
