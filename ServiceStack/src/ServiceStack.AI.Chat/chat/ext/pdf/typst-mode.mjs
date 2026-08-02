/**
 * Minimal CodeMirror 5 mode for typst (https://typst.app) markup + code expressions.
 * Registered lazily so it works with the CodeMirror global core_tools injects into the page.
 *
 * typst alternates between markup and code, and the two nest arbitrarily:
 *
 *     #text(size: 9pt)[Total #calc.round(data.total, digits: 2)]
 *     └ expr └ group ┘└ content ────── └ expr └ group ─────┘ ┘ ┘
 *
 * so the state carries a stack of contexts rather than a pair of depth counters:
 *   expr    a `#…` expression      (ends at end-of-line, or where the expression can't continue)
 *   group   `(` or `{` inside code (ends at the matching bracket)
 *   content `[ … ]` content block  (markup again until the matching `]`)
 */

const STATEMENTS = ['let', 'set', 'show', 'import', 'include', 'if', 'else', 'for', 'while', 'return', 'context']
const KEYWORDS = [...STATEMENTS, 'in', 'break', 'continue', 'as']
const ATOMS = ['none', 'auto', 'true', 'false']

const top = state => state.stack[state.stack.length - 1]
const inCode = state => top(state) === 'expr' || top(state) === 'group'
/** a bare `#name` expression only continues through `.member`, `(call)` and `[content]` */
const CONTINUES = /[.([]/

function endExpr(state) {
    if (top(state) === 'expr') {
        state.stack.pop()
        state.stmt = false
    }
}

/** after an identifier or a closing bracket, decide whether the bare expression carries on */
function maybeEndExpr(stream, state) {
    if (top(state) !== 'expr' || state.stmt) return
    if (!CONTINUES.test(stream.peek() ?? '')) endExpr(state)
}

function tokenBlockComment(stream, state) {
    while (!stream.eol()) {
        if (stream.match('*/')) {
            state.blockComment = false
            break
        }
        stream.next()
    }
    return 'comment'
}

function tokenCode(stream, state) {
    // neither a statement nor a bare expression survives a line break at expression level
    if (stream.sol() && top(state) === 'expr') {
        endExpr(state)
        return tokenMarkup(stream, state)
    }

    if (stream.eatSpace()) {
        // `#data.title and more text` - the space ends the expression, `#let x = 1` keeps going
        if (!state.stmt) maybeEndExprAfterSpace(state)
        return null
    }

    if (stream.match('//')) {
        stream.skipToEnd()
        return 'comment'
    }
    if (stream.match('/*')) {
        state.blockComment = true
        return tokenBlockComment(stream, state)
    }
    if (stream.match(/^"(?:[^"\\]|\\.)*"?/)) return 'string'
    if (stream.match(/^\d+(\.\d+)?(pt|mm|cm|in|em|deg|rad|fr|%)?/)) return 'number'

    const char = stream.peek()

    if (char === '[') {
        stream.next()
        state.stack.push('content') // back to markup until the matching ]
        return 'bracket'
    }
    if (char === '(' || char === '{') {
        stream.next()
        state.stack.push('group')
        return 'bracket'
    }
    if (char === ')' || char === '}') {
        stream.next()
        if (top(state) === 'group') state.stack.pop()
        maybeEndExpr(stream, state)
        return 'bracket'
    }
    if (char === '#') {
        stream.next()
        return 'meta'
    }
    if (char === '.') {
        stream.next() // member access, stays in code
        return null
    }

    if (stream.match(/^[\w-]+/)) {
        const word = stream.current()
        if (KEYWORDS.includes(word)) {
            if (STATEMENTS.includes(word)) state.stmt = true
            return 'keyword'
        }
        if (ATOMS.includes(word)) {
            maybeEndExpr(stream, state)
            return 'atom'
        }
        const next = stream.peek()
        maybeEndExpr(stream, state)
        if (next === '(') return 'builtin'
        if (next === ':') return 'property'
        return 'variable'
    }

    if (stream.match(/^(=>|==|!=|<=|>=|\.\.|[+\-*/=<>!])/)) return 'operator'

    stream.next()
    if (!state.stmt) endExpr(state)
    return null
}

/** whitespace ends a bare expression, but only once it is back at expression level */
function maybeEndExprAfterSpace(state) {
    if (top(state) === 'expr') endExpr(state)
}

function tokenMarkup(stream, state) {
    if (stream.sol()) state.lineStart = true

    if (stream.match('//')) {
        stream.skipToEnd()
        return 'comment'
    }
    if (stream.match('/*')) {
        state.blockComment = true
        return tokenBlockComment(stream, state)
    }

    // raw blocks ```lang ... ``` and inline `code`
    if (stream.match('```')) {
        state.raw = state.raw === 'block' ? null : 'block'
        return 'string-2'
    }
    if (state.raw === 'block') {
        stream.next()
        return 'string-2'
    }
    if (stream.match(/^`[^`]*`?/)) return 'string-2'

    // math $ ... $
    if (stream.match('$')) {
        state.math = !state.math
        return 'string-2'
    }
    if (state.math) {
        stream.next()
        return 'string-2'
    }

    const lineStart = state.lineStart
    state.lineStart = false

    // headings, list markers, term lists
    if (lineStart) {
        stream.eatSpace()
        if (stream.match(/^=+\s/)) {
            stream.skipToEnd()
            return 'header'
        }
        if (stream.match(/^([-+]|\d+\.)\s/)) return 'variable-2'
        if (stream.match(/^\/\s/)) return 'variable-2'
    }

    // #code expression / #let ... / #show ...
    if (stream.match('#')) {
        state.stack.push('expr')
        state.stmt = false
        return 'meta'
    }

    if (stream.match(/^\*[^*\n]+\*/)) return 'strong'
    if (stream.match(/^_[^_\n]+_/)) return 'em'
    if (stream.match(/^<[\w-]+>/)) return 'tag'
    if (stream.match(/^@[\w-]+/)) return 'link'
    if (stream.match(/^\\[#*_$`@<\\]/)) return 'escape'

    if (stream.match(']')) {
        if (top(state) === 'content') {
            state.stack.pop() // back into the code expression that opened it
            maybeEndExpr(stream, state)
        }
        return 'bracket'
    }

    if (stream.eatWhile(/[\w-]/)) return null
    stream.next()
    return null
}

export function defineTypstMode(CodeMirror) {
    if (!CodeMirror || CodeMirror.modes?.typst) return

    CodeMirror.defineMode('typst', () => ({
        startState: () => ({
            stack: [],
            stmt: false,
            blockComment: false,
            math: false,
            raw: null,
            lineStart: true,
        }),
        copyState: state => ({ ...state, stack: state.stack.slice() }),
        token(stream, state) {
            if (state.blockComment) return tokenBlockComment(stream, state)
            return inCode(state) ? tokenCode(stream, state) : tokenMarkup(stream, state)
        },
        lineComment: '//',
        blockCommentStart: '/*',
        blockCommentEnd: '*/',
    }))
    CodeMirror.defineMIME('text/x-typst', 'typst')
}
