import { ref, reactive, shallowRef, computed, watch, nextTick, onMounted, onUnmounted, inject } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import hljs from 'highlight.js'
import { PdfView } from './pdf-preview.mjs'
import { defineTypstMode } from './typst-mode.mjs'
import { registerTypst } from './typst-hljs.mjs'
import { toAttachments, MAX_ATTACHMENTS, MAX_PDF_PAGES } from './attachments.mjs'
import { JsonSchemaForm } from '/ui/components/JsonSchemaForm.mjs'
import { TYPE_LANGUAGES, generateTypes as generateTypesFor } from '/ui/components/jsonTypes.mjs'

let ext
let tools

const RENDER_DEBOUNCE_MS = 400
const ZOOM_STEPS = [0.5, 0.75, 1, 1.25, 1.5, 2, 3]
const PREVIEW_GUTTER = 16 // 1em, matches the preview panel's p-4 padding

// joined segmented button group
const BTN_GROUP =
    'inline-flex rounded-md shadow-sm overflow-hidden border border-gray-300 dark:border-gray-600 ' +
    'divide-x divide-gray-200 dark:divide-gray-700'
const BTN_ON = 'bg-indigo-600 text-white'
const BTN_OFF = 'bg-white dark:bg-gray-900 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800'
const AI_HISTORY_MAX = 10
// how many times the model gets to fix its own output before we stop and show the errors
const MAX_FIX_ATTEMPTS = 3
const IMAGE_EXTS = ['.png', '.jpg', '.jpeg', '.gif', '.webp', '.svg', '.bmp', '.ico', '.avif']
// editor key for generated code, so it never collides with a real file of the same name
const GEN_PREFIX = 'generated:'
// rendered PDFs live here, one folder per template, so they show up in the explorer
const SAVED_DIR = 'saved'

// files a typst template pulls in: json("x.json"), image("logo.png"), #include "part.typ", ...
// lib.typ's load-data() counts too, since that's how every bundled template reaches its .json
const RESOURCE_RE = /\b(?:json|yaml|toml|csv|xml|cbor|read|image|bibliography|load-data)\s*\(\s*"([^"]+)"|#?\b(?:include|import)\s+"([^"]+)"/g

function baseName(path) {
    return path ? path.split('/').pop() : ''
}
function dirName(path) {
    return path && path.includes('/') ? path.slice(0, path.lastIndexOf('/')) : ''
}
function extName(path) {
    const name = baseName(path)
    const dot = name.lastIndexOf('.')
    return dot > 0 ? name.slice(dot).toLowerCase() : ''
}
function sidecarOf(path) {
    return path.replace(/\.typ$/, '.json')
}
/** invoice.json -> invoice.ui.json, the JSON Schema its form is generated from */
function schemaOf(path) {
    return path.replace(/\.json$/, '.ui.json')
}
function isSchemaFile(path) {
    return (path ?? '').endsWith('.ui.json')
}
/** invoice.typ, invoice.json, invoice.ui.json, invoice.cs all share the stem "invoice" */
/**
 * Everything before the first dot, so one rule covers the lot: invoice.json, invoice.ui.json,
 * invoice.signature.png and lib.preview.typ all belong to their base document. It matches how
 * rename picks up a template's companions.
 */
function stemOf(name) {
    return name.split('.')[0] || name
}
const LIB_NAME = 'lib.typ'
const isLibrary = path => baseName(path ?? '') === LIB_NAME
// the file a group opens when you click it, most template-ish first
const PRIMARY_EXTS = ['.typ', '.json']

/**
 * Nest files that belong together under one row, the way IDEs nest compiled output under its source:
 * invoice.typ with invoice.json / invoice.ui.json / invoice.cs beneath it.
 */
function groupFiles(nodes) {
    const dirs = nodes
        .filter(n => !n.isFile)
        .map(n => ({ ...n, children: groupFiles(n.children ?? []) }))
        .sort((a, b) => a.name.localeCompare(b.name))
    const files = nodes.filter(n => n.isFile)

    const stems = new Map()
    for (const file of files) {
        const stem = stemOf(file.name)
        if (!stems.has(stem)) stems.set(stem, [])
        stems.get(stem).push(file)
    }

    const rows = []
    for (const [stem, group] of stems) {
        if (group.length === 1) {
            rows.push(group[0])
            continue
        }
        const primary =
            PRIMARY_EXTS.map(ext => group.find(f => f.name === stem + ext)).find(Boolean) ??
            group.find(f => !isSchemaFile(f.path)) ??
            group[0]
        rows.push({
            isGroup: true,
            isFile: true, // selectable like a file
            name: stem, // the group is the document, not one of its files
            stem,
            path: primary.path,
            ext: primary.ext,
            primary,
            children: group.filter(f => f !== primary).sort((a, b) => a.name.localeCompare(b.name)),
            paths: group.map(f => f.path),
        })
    }

    // the library is infrastructure rather than a document, so it sits at the end of the files
    rows.sort((a, b) => Number(isLibrary(a.path)) - Number(isLibrary(b.path)) || a.name.localeCompare(b.name))
    // templates first, folders after them - the documents are what you're usually reaching for
    return [...rows, ...dirs]
}
function isImage(path) {
    return IMAGE_EXTS.includes(extName(path))
}
function editorMode(path) {
    // clike, javascript and python modes come from the CodeMirror bundle core_tools loads
    switch (extName(path)) {
        case '.typ': return 'typst'
        case '.json': return { name: 'javascript', json: true }
        case '.js': return 'javascript'
        case '.ts': return { name: 'javascript', typescript: true }
        case '.cs': return 'text/x-csharp'
        case '.py': return 'python'
        default: return null
    }
}
function joinPath(dir, name) {
    return dir ? `${dir}/${name}` : name
}

/** Paths of every file the template references, resolved relative to the template (or to the root for /paths) */
function parseResources(entryPath, source) {
    if (!entryPath || !source) return []
    const dir = dirName(entryPath)
    const found = []
    for (const match of source.matchAll(RESOURCE_RE)) {
        const raw = match[1] ?? match[2]
        if (!raw || raw.startsWith('@')) continue // @preview/... packages aren't files
        const path = raw.startsWith('/')
            ? raw.slice(1)
            : raw.split('/').reduce((parts, part) => {
                if (part === '.' || part === '') return parts
                if (part === '..') return parts.slice(0, -1)
                return [...parts, part]
            }, dir ? dir.split('/') : []).join('/')
        if (path && path !== entryPath && !found.includes(path)) found.push(path)
    }
    return found
}

// --- .typ formatting toolbar ---------------------------------------------------
const CMARKER = '@preview/cmarker:0.1.10'
const ICON = {
    link: '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.828 10.172a4 4 0 010 5.656l-3 3a4 4 0 01-5.656-5.656l1.5-1.5M10.172 13.828a4 4 0 010-5.656l3-3a4 4 0 015.656 5.656l-1.5 1.5" />',
    image: '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />',
    table: '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M3 15h18M9 5v14M4 5h16a1 1 0 011 1v12a1 1 0 01-1 1H4a1 1 0 01-1-1V6a1 1 0 011-1z" />',
    pagebreak: '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 4h9l5 5v3M6 20h12M3 16h18M8 16v4m8-8v-4" />',
    rule: '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 12h16" />',
    center: '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M7 12h10M4 18h16" />',
    page: '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />',
    font: '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 7V4h16v3M9 20h6M12 4v16" />',
}

/**
 * The formatting most templates actually use. `wrap` surrounds the selection, `line` prefixes every selected
 * line (and toggles back off), `snippet` drops a block in at the cursor.
 */
const TYPST_ACTIONS = [
    { id: 'bold', text: 'B', cls: 'font-bold', title: 'Bold  *text*', wrap: ['*', '*'], placeholder: 'bold' },
    { id: 'italic', text: 'I', cls: 'italic', title: 'Italic  _text_', wrap: ['_', '_'], placeholder: 'italic' },
    { id: 'underline', text: 'U', cls: 'underline', title: 'Underline', wrap: ['#underline[', ']'], placeholder: 'underlined' },
    { id: 'strike', text: 'S', cls: 'line-through', title: 'Strikethrough', wrap: ['#strike[', ']'], placeholder: 'struck out' },
    { divider: true },
    { id: 'h1', text: 'H1', cls: 'font-bold', title: 'Heading', line: '= ' },
    { id: 'h2', text: 'H2', cls: 'font-semibold', title: 'Subheading', line: '== ' },
    { id: 'bullet', text: '┇', title: 'Bullet list', line: '- ' },
    { id: 'number', text: '1.', cls: 'font-bold', title: 'Numbered list', line: '+ ' },
    { divider: true },
    { id: 'raw', text: '✗', cls: '', title: 'Raw / code', wrap: ['`', '`'], placeholder: 'code' },
    { id: 'link', icon: 'link', title: 'Link', snippet: '#link("https://example.com")[link text]', inline: true },
    { id: 'image', icon: 'image', title: 'Image', dialog: 'image' },
    {
        id: 'table',
        icon: 'table',
        title: 'Table',
        snippet:
            // `$` opens math mode in markup, so a literal dollar sign has to be escaped
            '#table(\n  columns: (1fr, auto, auto),\n  align: (left, right, right),\n  table.header([*Item*], [*Qty*], [*Amount*]),\n  [Widget], [2], [\\$40.00],\n  [Spare kit], [1], [\\$15.00],\n)',
    },
    { id: 'center', icon: 'center', title: 'Centre', snippet: '#align(center)[centred]' },
    { id: 'rule', icon: 'rule', title: 'Horizontal rule', snippet: '#line(length: 100%, stroke: 0.5pt + luma(180))' },
    { id: 'pagebreak', icon: 'pagebreak', title: 'Page break', snippet: '#pagebreak()' },
    { divider: true },
]

const REF_PATH_RE = /^[A-Za-z_][\w-]*(?:\([^()]*\)|\.[A-Za-z_][\w-]*)*$/
/**
 * Is the selection a typst expression rather than prose? A leading `#` is typst's own marker for code, so
 * `#notes` counts. Without one we ask for a `.field` or a `(call)`, so a single selected word stays Markdown.
 */
const isMarkdownRef = text =>
    text.startsWith('#') ? REF_PATH_RE.test(text.slice(1)) : REF_PATH_RE.test(text) && /[.(]/.test(text)

/** `#set text(size: 10pt, font: "Libertinus Serif")` - group 2 is the family name */
const SET_TEXT_FONT_RE = /(#set\s+text\([^)]*font:\s*)"([^"]*)"/

const MARKDOWN_SNIPPET = [
    '#cmarker.render(```md',
    '# Heading',
    '',
    'Markdown with **bold**, _italic_ and a list:',
    '',
    '- one',
    '- two',
    '```)',
].join('\n')

const LIB_ICON =
    '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253" />'

const PdfFileNode = {
    name: 'PdfFileNode',
    template: `
    <div>
        <!-- a file with related files nested under it -->
        <div v-if="node.isGroup">
            <div @click="$emit('select', node)" @contextmenu.prevent.stop="$emit('menu', { event: $event, node: node.primary })"
                class="group flex items-center gap-1.5 pr-2 py-1 text-xs cursor-pointer border-l transition-colors"
                :class="groupActive ? ($styles.threadItemActive + ' ' + $styles.threadItemActiveBorder) : 'border-transparent ' + $styles.threadItem">
                <button type="button" @click.stop="expanded = !expanded" :title="expanded ? 'Collapse' : 'Show related files'"
                    class="pl-1.5 py-1 -my-1 flex-shrink-0" :class="[$styles.icon, $styles.iconHover]">
                    <svg xmlns="http://www.w3.org/2000/svg" class="size-3 transition-transform" :class="{ '-rotate-90': !expanded }" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" /></svg>
                </button>
                <svg v-if="isLib" xmlns="http://www.w3.org/2000/svg" class="size-3.5 flex-shrink-0 text-indigo-500" fill="none" viewBox="0 0 24 24" stroke="currentColor" v-html="libIcon"></svg>
                <svg v-else xmlns="http://www.w3.org/2000/svg" class="size-3.5 flex-shrink-0" :class="$styles.icon" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" /></svg>
                <span class="select-none truncate flex-1" :class="{ 'font-medium': node.ext === '.typ' }" :title="isLib ? 'Shared styles and helpers every template imports' : node.path">{{ node.name }}</span>
                <span v-if="isLib" class="text-[10px] px-1 rounded flex-shrink-0 text-indigo-600 dark:text-indigo-300 bg-indigo-50 dark:bg-indigo-900" title="Shared styles and helpers every template imports">lib</span>
                <span v-if="!expanded" class="text-xs flex-shrink-0" :class="$styles.muted">+{{ node.children.length }}</span>
                <button type="button" @click.stop="$emit('menu', { event: $event, node: node.primary })" title="Actions"
                    class="opacity-0 group-hover:opacity-100 p-0.5 rounded" :class="[$styles.icon, $styles.iconHover]">
                    <svg xmlns="http://www.w3.org/2000/svg" class="size-3" viewBox="0 0 20 20" fill="currentColor"><path d="M10 6a2 2 0 110-4 2 2 0 010 4zM10 12a2 2 0 110-4 2 2 0 010 4zM10 18a2 2 0 110-4 2 2 0 010 4z" /></svg>
                </button>
            </div>
            <div v-show="expanded" class="pl-4">
                <PdfFileNode v-for="child in node.children" :key="child.path" :node="child" :selected-path="selectedPath"
                    @select="$emit('select', $event)" @menu="$emit('menu', $event)" />
            </div>
        </div>

        <div v-else-if="node.isFile" @click="$emit('select', node)" @contextmenu.prevent.stop="$emit('menu', { event: $event, node })"
            class="group flex items-center gap-1.5 px-2 py-1 text-xs cursor-pointer border-l transition-colors"
            :class="selectedPath === node.path ? ($styles.threadItemActive + ' ' + $styles.threadItemActiveBorder) : 'border-transparent ' + $styles.threadItem">
            <svg v-if="isLib" xmlns="http://www.w3.org/2000/svg" class="size-3.5 flex-shrink-0 text-indigo-500" fill="none" viewBox="0 0 24 24" stroke="currentColor" v-html="libIcon"></svg>
            <svg v-else-if="isImg" xmlns="http://www.w3.org/2000/svg" class="size-3.5 flex-shrink-0" :class="$styles.icon" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" /></svg>
            <svg v-else xmlns="http://www.w3.org/2000/svg" class="size-3.5 flex-shrink-0" :class="[$styles.icon, node.ext === '.typ' ? '' : 'opacity-60']" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" /></svg>
            <span class="select-none truncate flex-1" :class="{ 'font-medium': node.ext === '.typ' }" :title="node.path">{{ node.name }}</span>
            <button type="button" @click.stop="$emit('menu', { event: $event, node })" title="Actions"
                class="opacity-0 group-hover:opacity-100 p-0.5 rounded" :class="[$styles.icon, $styles.iconHover]">
                <svg xmlns="http://www.w3.org/2000/svg" class="size-3" viewBox="0 0 20 20" fill="currentColor"><path d="M10 6a2 2 0 110-4 2 2 0 010 4zM10 12a2 2 0 110-4 2 2 0 010 4zM10 18a2 2 0 110-4 2 2 0 010 4z" /></svg>
            </button>
        </div>
        <div v-else>
            <div @click="expanded = !expanded" @contextmenu.prevent.stop="$emit('menu', { event: $event, node })"
                class="group flex items-center gap-1.5 px-2 py-1 text-xs cursor-pointer" :class="$styles.threadItem">
                <svg xmlns="http://www.w3.org/2000/svg" class="size-3 transition-transform flex-shrink-0" :class="[{ '-rotate-90': !expanded }, isSavedFolder ? 'text-amber-500 dark:text-amber-400' : $styles.icon]" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" /></svg>
                <svg v-if="isSavedFolder" xmlns="http://www.w3.org/2000/svg" class="size-3.5 flex-shrink-0 text-amber-500 dark:text-amber-400" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 8h14M5 8a2 2 0 01-2-2V5a2 2 0 012-2h4l2 2h7a2 2 0 012 2v1M5 8v10a2 2 0 002 2h10a2 2 0 002-2V8m-9 4h4" /></svg>
                <svg v-else xmlns="http://www.w3.org/2000/svg" class="size-3.5 flex-shrink-0" :class="$styles.icon" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 7a2 2 0 012-2h3.586a1 1 0 01.707.293l1.414 1.414a1 1 0 00.707.293H19a2 2 0 012 2v8a2 2 0 01-2 2H5a2 2 0 01-2-2V7z" /></svg>
                <span class="select-none font-medium truncate flex-1" :class="isSavedFolder ? 'text-amber-600 dark:text-amber-400 font-semibold' : ''">{{ node.name }}</span>
                <button type="button" @click.stop="$emit('menu', { event: $event, node })" title="Actions"
                    class="opacity-0 group-hover:opacity-100 p-0.5 rounded" :class="[$styles.icon, $styles.iconHover]">
                    <svg xmlns="http://www.w3.org/2000/svg" class="size-3" viewBox="0 0 20 20" fill="currentColor"><path d="M10 6a2 2 0 110-4 2 2 0 010 4zM10 12a2 2 0 110-4 2 2 0 010 4zM10 18a2 2 0 110-4 2 2 0 010 4z" /></svg>
                </button>
            </div>
            <div v-show="expanded" class="pl-3">
                <PdfFileNode v-for="child in node.children" :key="child.path" :node="child" :selected-path="selectedPath"
                    @select="$emit('select', $event)" @menu="$emit('menu', $event)" />
            </div>
        </div>
    </div>`,
    props: {
        node: { type: Object, required: true },
        selectedPath: { type: String, default: null },
    },
    emits: ['select', 'menu'],
    setup(props) {
        // groups start collapsed, folders start open
        const expanded = ref(!props.node.isGroup)
        const isSavedFolder = computed(() => !props.node.isFile && (props.node.name === SAVED_DIR || props.node.path === SAVED_DIR || (props.node.path ?? '').endsWith('/' + SAVED_DIR)))
        return {
            expanded,
            isSavedFolder,
            isImg: computed(() => props.node.isFile && IMAGE_EXTS.includes(props.node.ext)),
            isLib: computed(() => isLibrary(props.node.path)),
            libIcon: LIB_ICON,
            // when collapsed, the row stands in for whichever of its files is open
            groupActive: computed(() =>
                props.node.isGroup &&
                (expanded.value
                    ? props.selectedPath === props.node.path
                    : (props.node.paths ?? []).includes(props.selectedPath)),
            ),
        }
    },
}

/** Right-click / kebab menu for the file explorer */
const PdfContextMenu = {
    template: `
    <div class="pdf-menu fixed z-100 w-48 py-1 text-xs rounded-md shadow-xl" :class="$styles.bgPopover" :style="{ left: x + 'px', top: y + 'px' }">
        <template v-for="(item, i) in items" :key="i">
            <div v-if="item.divider" class="my-2 border-t" :class="$styles.chromeBorder"></div>
            <div v-else class="group flex items-center" :class="$styles.popoverButton">
                <button type="button" @click="$emit('pick', item)"
                    class="flex-1 min-w-0 text-left px-3 py-1.5 flex items-center gap-2" :class="item.danger ? 'text-red-600 dark:text-red-400' : ''">
                    <span class="truncate">{{ item.label }}</span>
                </button>
            </div>
        </template>
    </div>`,
    props: {
        x: { type: Number, required: true },
        y: { type: Number, required: true },
        items: { type: Array, required: true },
    },
    emits: ['pick'],
}

/** Small inline prompt/confirm used for new, rename and delete */
const PdfPrompt = {
    template: `
    <div class="absolute inset-0 z-100 flex items-center justify-center bg-black/40" @click.self="$emit('cancel')">
        <div class="w-full max-w-md mx-4 p-4" :class="$styles.dialog">
            <h3 class="text-sm font-semibold mb-2">{{ title }}</h3>
            <p v-if="message" class="text-xs mb-3" :class="$styles.muted">{{ message }}</p>
            <input v-if="!confirmOnly" ref="input" v-model="text" type="text" @keyup.enter="submit" @keyup.esc="$emit('cancel')"
                class="w-full px-2.5 py-1.5 text-sm rounded-md border" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]" />
            <slot name="extra"></slot>
            <div class="mt-4 flex justify-end gap-2">
                <button type="button" @click="$emit('cancel')" class="px-3 py-1.5 text-sm" :class="$styles.secondaryButton">Cancel</button>
                <button v-if="altText" type="button" @click="$emit('alt')" class="px-3 py-1.5 text-sm" :class="$styles.secondaryButton">{{ altText }}</button>
                <button type="button" @click="submit" class="px-3 py-1.5 text-sm"
                    :class="danger ? 'rounded-md border border-transparent shadow-sm text-white bg-red-600 hover:bg-red-700' : $styles.primaryButton">{{ okText }}</button>
            </div>
        </div>
    </div>`,
    props: {
        title: { type: String, required: true },
        message: { type: String, default: '' },
        value: { type: String, default: '' },
        okText: { type: String, default: 'OK' },
        danger: { type: Boolean, default: false },
        confirmOnly: { type: Boolean, default: false },
        /** optional second action, e.g. "Discard" next to "Save" */
        altText: { type: String, default: '' },
    },
    emits: ['submit', 'cancel', 'alt'],
    setup(props, { emit }) {
        const text = ref(props.value)
        const input = ref(null)
        onMounted(() => nextTick(() => {
            input.value?.focus()
            const dot = text.value.lastIndexOf('.')
            if (dot > 0) input.value?.setSelectionRange(0, dot)
        }))
        let submitted = false
        function submit() {
            if (submitted) return
            const val = props.confirmOnly ? true : text.value.trim()
            if (!val) return
            submitted = true
            emit('submit', val)
        }
        return { text, input, submit }
    },
}

/** Modal font & text styling picker with search filter, color picker, size, weight, spacing, and live preview */
/**
 * Locate `#set <name>(...)` and its body. A regex can't do this: `[^)]*` stops at the `)` closing
 * `rgb("#2563eb")`, which truncates the parse and leaves a stray `)` behind on replace.
 */
function findSetRule(doc, name) {
    const open = doc.search(new RegExp(`#set\\s+${name}\\s*\\(`))
    if (open < 0) return null
    const from = doc.indexOf('(', open)
    let depth = 0
    for (let i = from; i < doc.length; i++) {
        const c = doc[i]
        if (c === '"') {
            // skip strings so a paren inside one doesn't count
            i = doc.indexOf('"', i + 1)
            if (i < 0) return null
        } else if (c === '(') depth++
        else if (c === ')' && --depth === 0) {
            return { start: open, end: i + 1, body: doc.slice(from + 1, i) }
        }
    }
    return null
}

const findSetTextRule = doc => findSetRule(doc, 'text')

const MM_PER = { mm: 1, cm: 10, in: 25.4, pt: 25.4 / 72 }
const toMillimetres = (value, unit) => (Number(value) || 0) * (MM_PER[unit] ?? 1)

/** Split a rule body on its top level commas, ignoring those nested in parens or strings */
function splitParams(body) {
    const out = []
    let depth = 0
    let start = 0
    for (let i = 0; i < (body ?? '').length; i++) {
        const c = body[i]
        if (c === '"') {
            i = body.indexOf('"', i + 1)
            if (i < 0) break
        } else if ('([{'.includes(c)) depth++
        else if (')]}'.includes(c)) depth--
        else if (c === ',' && depth === 0) {
            out.push(body.slice(start, i))
            start = i + 1
        }
    }
    out.push((body ?? '').slice(start))
    return out.map(p => p.trim()).filter(Boolean)
}

/**
 * Rewrite only the keys we manage, so options the dialog doesn't know about - a `header:`, a
 * `margin: (x: 2cm, y: 1cm)` - survive being edited. A null value removes the key.
 */
function mergeParams(body, updates) {
    const keyOf = p => p.match(/^([A-Za-z_][\w-]*)\s*:/)?.[1] ?? null
    const seen = new Set()
    const out = []
    for (const param of splitParams(body)) {
        const key = keyOf(param)
        if (key && key in updates) {
            seen.add(key)
            if (updates[key] != null) out.push(`${key}: ${updates[key]}`)
            continue
        }
        out.push(param)
    }
    for (const [key, value] of Object.entries(updates)) {
        if (!seen.has(key) && value != null) out.push(`${key}: ${value}`)
    }
    return out
}

const PdfFontPicker = {
    template: `
    <div class="fixed inset-0 z-100 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs" @click.self="$emit('close')" @keydown.esc="$emit('close')">
        <div class="w-full max-w-3xl max-h-[88vh] flex flex-col rounded-lg shadow-2xl overflow-hidden border" :class="[$styles.dialog, $styles.chromeBorder]">
            <!-- Header -->
            <div class="flex items-center justify-between px-4 py-3 border-b flex-shrink-0" :class="$styles.chromeBorder">
                <div class="flex items-center gap-2">
                    <svg xmlns="http://www.w3.org/2000/svg" class="size-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" v-html="icons.font"></svg>
                    <h3 class="text-sm font-semibold">Text & Font Formatting</h3>
                    <span class="text-xs px-2 py-0.5 rounded-full border" :class="[$styles.muted, $styles.chromeBorder]">{{ filteredFonts.length }} / {{ fonts.length }} fonts</span>
                </div>
                <button type="button" @click="$emit('close')" class="p-1 rounded flex-shrink-0" :class="[$styles.icon, $styles.iconHover]" title="Close (Esc)">
                    <svg xmlns="http://www.w3.org/2000/svg" class="size-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                    </svg>
                </button>
            </div>

            <!-- Controls Toolbar Section -->
            <div class="p-3 border-b flex flex-col gap-3 flex-shrink-0" :class="[$styles.bgSidebar, $styles.chromeBorder]">
                <!-- Search & Font Size Row -->
                <div class="flex flex-wrap items-center gap-3 text-xs">
                    <!-- Search Font -->
                    <div class="relative flex-1 min-w-[140px]">
                        <svg xmlns="http://www.w3.org/2000/svg" class="size-3.5 absolute left-2.5 top-2 pointer-events-none" :class="$styles.muted" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                        </svg>
                        <input ref="searchInput" v-model="filter" type="text" placeholder="Filter font family…" @keyup.enter="selectFirst" @keyup.esc="$emit('close')" spellcheck="false"
                            class="w-full pl-8 pr-7 py-1 rounded border outline-none" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]" />
                        <button v-if="filter" type="button" @click="filter = ''; searchInput?.focus()" class="absolute right-2 top-2" :class="$styles.muted">
                            <svg xmlns="http://www.w3.org/2000/svg" class="size-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                            </svg>
                        </button>
                    </div>

                    <!-- Font Size -->
                    <div class="flex items-center gap-1.5">
                        <span :class="$styles.muted">Size:</span>
                        <input v-model="fontSize" type="number" min="4" max="144" step="0.5" class="w-14 px-1.5 py-1 text-center rounded border outline-none font-mono" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]" />
                        <span :class="$styles.muted">pt</span>
                    </div>

                </div>

                <!-- Colour, Weight & Spacing Row -->
                <div class="flex flex-wrap items-center gap-4 text-xs">
                    <!-- Text Color -->
                    <div class="flex items-center gap-1.5">
                        <span :class="$styles.muted">Color:</span>
                        <div class="relative flex items-center gap-1">
                            <input v-model="color" type="color" class="size-6 rounded border cursor-pointer p-0 bg-transparent" />
                            <input v-model="color" type="text" class="w-18 px-1.5 py-1 text-center font-mono rounded border outline-none uppercase" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]" />
                        </div>
                        <!-- Color Swatches -->
                        <div class="flex items-center gap-1 ml-1">
                            <button v-for="c in PRESET_COLORS" :key="c" type="button" @click="color = c"
                                class="size-4 rounded-full border border-black/20 dark:border-white/20 transition-transform hover:scale-110"
                                :style="{ backgroundColor: c }" :title="c"></button>
                        </div>
                    </div>

                    <div class="h-4 w-px bg-gray-300 dark:bg-gray-700 hidden sm:block"></div>

                    <!-- Weight -->
                    <div class="flex items-center gap-1.5">
                        <span :class="$styles.muted">Weight:</span>
                        <select v-model="weight" class="pl-1.5 pr-6 py-1 text-xs rounded border outline-none" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]">
                            <option v-for="w in WEIGHTS" :key="w.id" :value="w.id">{{ w.label }}</option>
                        </select>
                    </div>

                    <!-- Letter Spacing (Tracking) -->
                    <div class="flex items-center gap-1.5">
                        <span :class="$styles.muted">Spacing:</span>
                        <select v-model="tracking" class="pl-1.5 pr-6 py-1 text-xs rounded border outline-none" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]">
                            <option v-for="tr in TRACKINGS" :key="tr.id" :value="tr.id">{{ tr.label }}</option>
                        </select>
                    </div>

                </div>

                <!-- Live Sample Preview -->
                <div class="mt-1 p-3 rounded-md border text-center transition-all overflow-hidden flex items-center justify-center min-h-[50px]"
                    :class="[$styles.bgInput, $styles.borderInput]"
                    :style="{
                        fontFamily: fontStack(selectedFont),
                        fontSize: (fontSize || 11) + 'pt',
                        color: color || '#000000',
                        fontWeight: weight === 'medium' ? 500 : weight === 'semibold' ? 600 : weight === 'bold' ? 700 : 400,
                        letterSpacing: (tracking || 0) + 'pt'
                    }">
                    The quick brown fox jumps over the lazy dog (1234567890)
                </div>
            </div>

            <!-- Font Grid -->
            <div class="flex-1 overflow-y-auto p-3 min-h-[160px]">
                <div v-if="!filteredFonts.length" class="py-12 text-center text-xs" :class="$styles.muted">
                    No fonts matching "{{ filter }}"
                </div>
                <div v-else class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-1.5">
                    <button v-for="font in filteredFonts" :key="font" type="button"
                        @click="selectedFont = font"
                        @dblclick="selectedFont = font; apply()"
                        class="flex items-center justify-between px-3 py-2 text-xs rounded border text-left transition-colors truncate group"
                        :class="font === selectedFont ? 'border-blue-500 bg-blue-50/50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 font-semibold shadow-xs' : [$styles.bgInput, $styles.textInput, $styles.borderInput, 'hover:border-blue-400 dark:hover:border-blue-500']"
                        :title="font">
                        <span class="truncate flex-1" :style="{ fontFamily: fontStack(font) }">{{ font }}</span>
                        <svg v-if="font === selectedFont" xmlns="http://www.w3.org/2000/svg" class="size-3.5 flex-shrink-0 text-blue-500 ml-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                        </svg>
                    </button>
                </div>
            </div>

            <!-- Modal Footer -->
            <div class="flex items-center justify-between px-4 py-3 border-t flex-shrink-0" :class="[$styles.bgSidebar, $styles.chromeBorder]">
                <div class="text-xs truncate max-w-sm" :class="$styles.muted">
                    <span class="font-medium text-gray-700 dark:text-gray-300">{{ selectedFont || 'Default' }}</span>
                    <span> · {{ fontSize }}pt · {{ color }}</span>
                </div>
                <div class="flex items-center gap-2">
                    <button type="button" @click="$emit('close')" class="px-3 py-1.5 text-xs" :class="$styles.secondaryButton">Cancel</button>
                    <button type="button" @click="apply" class="px-4 py-1.5 text-xs" :class="$styles.primaryButton">Apply Style</button>
                </div>
            </div>
        </div>
    </div>`,
    props: {
        fonts: { type: Array, required: true },
        documentSettings: { type: Object, default: () => ({}) },
    },
    emits: ['apply', 'close'],
    setup(props, { emit }) {
        const filter = ref('')
        const searchInput = ref(null)

        const selectedFont = ref(props.documentSettings?.font || props.fonts[0] || '')
        const fontSize = ref(props.documentSettings?.size || '11')
        const color = ref(props.documentSettings?.fill || '#000000')
        const weight = ref(props.documentSettings?.weight || 'regular')
        const tracking = ref(props.documentSettings?.tracking || '0')

        const PRESET_COLORS = ['#000000', '#1e293b', '#2563eb', '#059669', '#dc2626', '#d97706', '#7c3aed', '#64748b']
        const WEIGHTS = [
            { id: 'regular', label: 'Regular' },
            { id: 'medium', label: 'Medium' },
            { id: 'semibold', label: 'Semibold' },
            { id: 'bold', label: 'Bold' },
        ]
        const TRACKINGS = [
            { id: '0', label: 'Normal (0)' },
            { id: '0.5', label: '+0.5pt' },
            { id: '1', label: '+1.0pt' },
            { id: '1.5', label: '+1.5pt' },
            { id: '2', label: '+2.0pt' },
        ]

        const filteredFonts = computed(() => {
            const q = filter.value.trim().toLowerCase()
            if (!q) return props.fonts
            return props.fonts.filter(f => f.toLowerCase().includes(q))
        })

        onMounted(() => nextTick(() => searchInput.value?.focus()))

        function selectFirst() {
            if (filteredFonts.value.length > 0) {
                selectedFont.value = filteredFonts.value[0]
            }
        }

        function apply() {
            emit('apply', {
                font: selectedFont.value,
                size: fontSize.value,
                fill: color.value,
                weight: weight.value,
                tracking: tracking.value,
            })
        }

        /** quoting a family name here rather than in the template, where a `\'` would collapse to a bare quote */
        const fontStack = font => (font ? `'${font}', sans-serif` : 'sans-serif')

        return {
            filter, searchInput, filteredFonts, selectFirst, apply, fontStack,
            selectedFont, fontSize, color, weight, tracking,
            PRESET_COLORS, WEIGHTS, TRACKINGS,
            icons: ICON
        }
    },
}

/** typst's built in paper names, with their sizes so the preview gets the shape right */
const PAPERS = [
    { id: 'a3', label: 'A3', w: 297, h: 420 },
    { id: 'a4', label: 'A4', w: 210, h: 297 },
    { id: 'a5', label: 'A5', w: 148, h: 210 },
    { id: 'a6', label: 'A6', w: 105, h: 148 },
    { id: 'us-letter', label: 'US Letter', w: 216, h: 279 },
    { id: 'us-legal', label: 'US Legal', w: 216, h: 356 },
    { id: 'us-tabloid', label: 'US Tabloid', w: 279, h: 432 },
    { id: 'presentation-16-9', label: 'Slide 16:9', w: 297, h: 167 },
    { id: 'presentation-4-3', label: 'Slide 4:3', w: 280, h: 210 },
]
const NUMBERINGS = [
    { id: '', label: 'None' },
    { id: '1', label: '1' },
    { id: '1 / 1', label: '1 / 1' },
    { id: '1 of 1', label: '1 of 1' },
    { id: 'i', label: 'i, ii, iii' },
]
const MARGIN_UNITS = ['cm', 'mm', 'in', 'pt']

const PdfPageSetup = {
    template: `
    <div class="fixed inset-0 z-100 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs" @click.self="$emit('close')" @keydown.esc="$emit('close')">
        <div class="w-full max-w-xl flex flex-col rounded-lg shadow-2xl overflow-hidden border" :class="[$styles.dialog, $styles.chromeBorder]">
            <!-- Header -->
            <div class="flex items-center justify-between px-4 py-3 border-b flex-shrink-0" :class="$styles.chromeBorder">
                <div class="flex items-center gap-2">
                    <svg xmlns="http://www.w3.org/2000/svg" class="size-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" v-html="icons.page"></svg>
                    <h3 class="text-sm font-semibold">Page Setup</h3>
                </div>
                <button type="button" @click="$emit('close')" class="p-1 rounded flex-shrink-0" :class="[$styles.icon, $styles.iconHover]" title="Close (Esc)">
                    <svg xmlns="http://www.w3.org/2000/svg" class="size-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                    </svg>
                </button>
            </div>

            <div class="p-3 flex gap-4">
                <!-- Controls -->
                <div class="flex-1 flex flex-col gap-3 text-xs min-w-0">
                    <div class="flex items-center gap-1.5">
                        <span class="w-16 flex-shrink-0" :class="$styles.muted">Paper:</span>
                        <select v-model="paper" class="flex-1 px-1.5 py-1 rounded border outline-none" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]">
                            <option v-for="p in PAPERS" :key="p.id" :value="p.id">{{ p.label }}</option>
                            <option value="custom">Custom…</option>
                        </select>
                    </div>

                    <div v-if="paper === 'custom'" class="flex items-center gap-1.5">
                        <span class="w-16 flex-shrink-0" :class="$styles.muted">Size:</span>
                        <input v-model="width" type="number" min="10" step="1" class="w-16 px-1.5 py-1 text-center rounded border outline-none font-mono" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]" />
                        <span :class="$styles.muted">×</span>
                        <input v-model="height" type="number" min="10" step="1" class="w-16 px-1.5 py-1 text-center rounded border outline-none font-mono" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]" />
                        <span :class="$styles.muted">mm</span>
                    </div>

                    <div class="flex items-center gap-1.5">
                        <span class="w-16 flex-shrink-0" :class="$styles.muted">Layout:</span>
                        <div class="inline-flex rounded border overflow-hidden" :class="$styles.borderInput">
                            <button type="button" @click="landscape = false" class="px-2 py-1 transition-colors"
                                :class="!landscape ? 'bg-indigo-600 text-white font-semibold' : [$styles.bgInput, $styles.textInput]">Portrait</button>
                            <button type="button" @click="landscape = true" class="px-2 py-1 transition-colors"
                                :class="landscape ? 'bg-indigo-600 text-white font-semibold' : [$styles.bgInput, $styles.textInput]">Landscape</button>
                        </div>
                    </div>

                    <div class="flex items-center gap-1.5">
                        <span class="w-16 flex-shrink-0" :class="$styles.muted">Margin:</span>
                        <input v-model="margin" type="number" min="0" step="0.1" placeholder="auto" class="w-16 px-1.5 py-1 text-center rounded border outline-none font-mono" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]" />
                        <select v-model="marginUnit" class="pl-1.5 pr-6 py-1 rounded border outline-none" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]">
                            <option v-for="u in MARGIN_UNITS" :key="u" :value="u">{{ u }}</option>
                        </select>
                        <span v-if="!margin" class="truncate" :class="$styles.muted">leaves it unchanged</span>
                    </div>

                    <div class="flex items-center gap-1.5">
                        <span class="w-16 flex-shrink-0" :class="$styles.muted">Columns:</span>
                        <input v-model="columns" type="number" min="1" max="6" step="1" class="w-16 px-1.5 py-1 text-center rounded border outline-none font-mono" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]" />
                    </div>

                    <div class="flex items-center gap-1.5">
                        <span class="w-16 flex-shrink-0" :class="$styles.muted">Numbers:</span>
                        <select v-model="numbering" class="flex-1 px-1.5 py-1 rounded border outline-none" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]">
                            <option v-for="n in NUMBERINGS" :key="n.id" :value="n.id">{{ n.label }}</option>
                        </select>
                    </div>

                    <div class="flex items-center gap-1.5">
                        <span class="w-16 flex-shrink-0" :class="$styles.muted">Fill:</span>
                        <input type="checkbox" v-model="hasFill" class="rounded" />
                        <input v-model="fill" type="color" :disabled="!hasFill" class="size-6 rounded border cursor-pointer p-0 bg-transparent disabled:opacity-40" />
                        <input v-model="fill" type="text" :disabled="!hasFill" class="w-20 px-1.5 py-1 text-center font-mono rounded border outline-none uppercase disabled:opacity-40" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]" />
                    </div>
                </div>

                <!-- Proportional preview -->
                <div class="w-32 flex-shrink-0 flex flex-col items-center justify-center gap-2">
                    <div class="border shadow-sm relative" :class="$styles.chromeBorder"
                        :style="{ width: preview.w + 'px', height: preview.h + 'px', background: hasFill ? fill : '#ffffff' }">
                        <div class="absolute border border-dashed border-blue-400" :style="preview.inset"></div>
                    </div>
                    <span class="text-[10px] tabular-nums" :class="$styles.muted">{{ sizeLabel }}</span>
                </div>
            </div>

            <!-- Footer -->
            <div class="flex items-center justify-between px-4 py-3 border-t flex-shrink-0" :class="[$styles.bgSidebar, $styles.chromeBorder]">
                <code class="text-[11px] truncate" :class="$styles.muted">{{ ruleText }}</code>
                <div class="flex items-center gap-2 flex-shrink-0">
                    <button type="button" @click="$emit('close')" class="px-3 py-1.5 text-xs" :class="$styles.secondaryButton">Cancel</button>
                    <button type="button" @click="apply" class="px-4 py-1.5 text-xs" :class="$styles.primaryButton">Apply Page</button>
                </div>
            </div>
        </div>
    </div>`,
    props: {
        documentSettings: { type: Object, default: () => ({}) },
    },
    emits: ['apply', 'close'],
    setup(props, { emit }) {
        const d = props.documentSettings ?? {}
        const paper = ref(d.paper ?? (d.width || d.height ? 'custom' : 'a4'))
        const width = ref(d.width ?? '210')
        const height = ref(d.height ?? '297')
        const landscape = ref(!!d.flipped)
        const margin = ref(d.margin ?? '')
        const marginUnit = ref(d.marginUnit ?? 'cm')
        const columns = ref(d.columns ?? '1')
        const numbering = ref(d.numbering ?? '')
        const hasFill = ref(!!d.fill)
        const fill = ref(d.fill ?? '#ffffff')

        const size = computed(() => {
            if (paper.value === 'custom') return { w: Number(width.value) || 210, h: Number(height.value) || 297 }
            const p = PAPERS.find(x => x.id === paper.value) ?? PAPERS[1]
            return { w: p.w, h: p.h }
        })
        const sizeLabel = computed(() => {
            const { w, h } = size.value
            return landscape.value ? `${h} × ${w} mm` : `${w} × ${h} mm`
        })

        /** scale the page to fit the preview box, and inset the margin proportionally */
        const preview = computed(() => {
            const { w, h } = size.value
            const [pw, ph] = landscape.value ? [h, w] : [w, h]
            const scale = Math.min(110 / pw, 130 / ph)
            const mm = margin.value ? toMillimetres(Number(margin.value), marginUnit.value) : 0
            const inset = Math.max(0, mm * scale)
            return {
                w: Math.round(pw * scale),
                h: Math.round(ph * scale),
                inset: { inset: `${inset}px` },
            }
        })

        const updates = computed(() => {
            const out = {}
            if (paper.value === 'custom') {
                out.paper = null
                out.width = `${Number(width.value) || 210}mm`
                out.height = `${Number(height.value) || 297}mm`
            } else {
                out.paper = `"${paper.value}"`
                out.width = null
                out.height = null
            }
            out.flipped = landscape.value ? 'true' : null
            if (String(margin.value).trim()) out.margin = `${margin.value}${marginUnit.value}`
            out.columns = Number(columns.value) > 1 ? String(Number(columns.value)) : null
            out.numbering = numbering.value ? `"${numbering.value}"` : null
            out.fill = hasFill.value ? `rgb("${fill.value}")` : null
            return out
        })

        const ruleText = computed(() => {
            const set = Object.entries(updates.value).filter(([, v]) => v != null)
            return `#set page(${set.map(([k, v]) => `${k}: ${v}`).join(', ')})`
        })

        function apply() {
            emit('apply', updates.value)
        }

        return {
            paper, width, height, landscape, margin, marginUnit, columns, numbering, hasFill, fill,
            preview, sizeLabel, ruleText, apply,
            PAPERS, NUMBERINGS, MARGIN_UNITS, icons: ICON,
        }
    },
}

const IMAGE_DIR = 'images'

const PdfImagePicker = {
    template: `
    <div class="fixed inset-0 z-100 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs" @click.self="$emit('close')">
        <div class="w-full max-w-lg flex flex-col rounded-lg shadow-2xl overflow-hidden border" :class="[$styles.dialog, $styles.chromeBorder]">
            <div class="flex items-center justify-between px-4 py-3 border-b flex-shrink-0" :class="$styles.chromeBorder">
                <h3 class="text-sm font-semibold">Insert image</h3>
                <button type="button" @click="$emit('close')" class="p-1 rounded" :class="[$styles.icon, $styles.iconHover]" title="Close (Esc)">
                    <svg xmlns="http://www.w3.org/2000/svg" class="size-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                    </svg>
                </button>
            </div>

            <div class="p-3 flex flex-col gap-3 text-xs">
                <!-- drop / choose a file -->
                <div @dragover.prevent="over = true" @dragleave="over = false" @drop.prevent="onDrop" @paste="onPaste"
                    class="rounded-md border-2 border-dashed p-4 text-center"
                    :class="over ? 'border-indigo-400 bg-indigo-50 dark:bg-gray-900' : $styles.chromeBorder">
                    <template v-if="!file">
                        <input ref="fileInput" type="file" accept="image/*" class="hidden" @change="onPick" />
                        <button type="button" @click="$refs.fileInput.click()" class="px-3 py-1.5" :class="$styles.secondaryButton">Choose an image…</button>
                        <div class="mt-1.5" :class="$styles.muted">or drop one here</div>
                    </template>
                    <div v-else class="flex items-center gap-3 text-left">
                        <img :src="file.url" class="size-16 object-contain rounded border" :class="$styles.chromeBorder" />
                        <div class="min-w-0 flex-1">
                            <div class="truncate font-medium">{{ file.name }}</div>
                            <div :class="$styles.muted">{{ file.width }} × {{ file.height }} · {{ Math.round(file.size / 1024) }}KB</div>
                        </div>
                        <button type="button" @click="file = null" class="px-2 py-1" :class="$styles.secondaryButton">Clear</button>
                    </div>
                </div>

                <!-- where it goes -->
                <div v-if="file" class="flex flex-col gap-2">
                    <div class="flex items-center gap-1.5">
                        <span class="w-16 flex-shrink-0" :class="$styles.muted">Save as:</span>
                        <input v-model="name" type="text" class="flex-1 px-1.5 py-1 rounded border outline-none font-mono"
                            :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]" />
                    </div>
                    <div class="flex items-center gap-1.5">
                        <span class="w-16 flex-shrink-0" :class="$styles.muted">Store in:</span>
                        <div class="inline-flex rounded border overflow-hidden" :class="$styles.borderInput">
                            <button type="button" @click="shared = true" class="px-2 py-1"
                                :class="shared ? 'bg-indigo-600 text-white font-semibold' : [$styles.bgInput, $styles.textInput]">{{ imageDir }}/</button>
                            <button type="button" @click="shared = false" class="px-2 py-1"
                                :class="!shared ? 'bg-indigo-600 text-white font-semibold' : [$styles.bgInput, $styles.textInput]">With this template</button>
                        </div>
                    </div>
                    <p :class="$styles.muted">
                        <template v-if="shared">Shared by every template - the place for logos and signatures.</template>
                        <template v-else>Named <span class="font-mono">{{ attachedName }}</span>, so it groups with the template and follows it when renamed.</template>
                    </p>
                </div>

                <!-- images already in the folder -->
                <div v-if="existing.length" class="border-t pt-3" :class="$styles.chromeBorder">
                    <div class="mb-1.5" :class="$styles.muted">Or use one already here:</div>
                    <div class="flex flex-wrap gap-2 overflow-y-auto" style="max-height:8rem">
                        <button v-for="path in existing" :key="path" type="button" @click="$emit('insert', { path, width })"
                            class="p-1 rounded border hover:border-blue-400" :class="$styles.chromeBorder" :title="path">
                            <img :src="rawUrl(path)" class="size-12 object-contain" />
                            <div class="mt-0.5 w-12 truncate text-[10px]" :class="$styles.muted">{{ baseName(path) }}</div>
                        </button>
                    </div>
                </div>

                <div class="flex items-center gap-1.5">
                    <span class="w-16 flex-shrink-0" :class="$styles.muted">Width:</span>
                    <input v-model="width" type="text" class="w-20 px-1.5 py-1 rounded border outline-none font-mono"
                        :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]" />
                    <span :class="$styles.muted">e.g. 40%, 3cm, auto</span>
                </div>
            </div>

            <div class="flex items-center justify-between px-4 py-3 border-t flex-shrink-0" :class="[$styles.bgSidebar, $styles.chromeBorder]">
                <code class="text-[11px] truncate" :class="$styles.muted">{{ file ? targetPath : 'pick or upload an image' }}</code>
                <div class="flex items-center gap-2 flex-shrink-0">
                    <button type="button" @click="$emit('close')" class="px-3 py-1.5 text-xs" :class="$styles.secondaryButton">Cancel</button>
                    <button type="button" @click="upload" :disabled="!file || busy" class="px-4 py-1.5 text-xs disabled:opacity-40" :class="$styles.primaryButton">
                        {{ busy ? 'Uploading…' : 'Upload & insert' }}
                    </button>
                </div>
            </div>
        </div>
    </div>`,
    props: {
        existing: { type: Array, default: () => [] },
        stem: { type: String, default: '' },
        rawUrl: { type: Function, required: true },
    },
    emits: ['insert', 'upload', 'close'],
    setup(props, { emit }) {
        const file = ref(null)
        const name = ref('')
        const shared = ref(true)
        const width = ref('40%')
        const over = ref(false)
        const busy = ref(false)
        const fileInput = ref(null)

        const attachedName = computed(() => `${props.stem}.${name.value}`)
        const targetPath = computed(() =>
            shared.value ? `${IMAGE_DIR}/${name.value}` : attachedName.value,
        )

        async function take(picked) {
            over.value = false
            if (!picked || !picked.type.startsWith('image/')) return
            const url = await new Promise(resolve => {
                const reader = new FileReader()
                reader.onload = () => resolve(reader.result)
                reader.readAsDataURL(picked)
            })
            const img = await new Promise(resolve => {
                const el = new Image()
                el.onload = () => resolve(el)
                el.onerror = () => resolve({ naturalWidth: 0, naturalHeight: 0 })
                el.src = url
            })
            // a name typst can reference without quoting gymnastics
            name.value = (picked.name || 'image.png').toLowerCase().replace(/[^a-z0-9.\-_]+/g, '-')
            file.value = { blob: picked, url, name: picked.name, size: picked.size, width: img.naturalWidth, height: img.naturalHeight }
        }

        const onPick = e => take(e.target.files?.[0])
        const onDrop = e => take(e.dataTransfer?.files?.[0])
        const onPaste = e => take(e.clipboardData?.files?.[0])

        async function upload() {
            if (!file.value) return
            busy.value = true
            try {
                await emit('upload', { blob: file.value.blob, path: targetPath.value, width: width.value })
            } finally {
                busy.value = false
            }
        }

        return {
            file, name, shared, width, over, busy, fileInput, attachedName, targetPath,
            onPick, onDrop, onPaste, upload, imageDir: IMAGE_DIR, baseName
        }
    },
}

const PdfDesigner = {
    template: `
    <div id="pdf-designer" class="relative h-full w-full min-w-0 overflow-hidden">
      <!-- absolute so the designer's contents (wide canvases, editors) never widen the app shell -->
      <div class="absolute inset-0 flex overflow-hidden">

        <!-- File Explorer -->
        <div v-if="prefs.showExplorer" style="width:14rem" class="flex-shrink-0 flex flex-col border-r overflow-hidden" :class="[$styles.chromeBorder, $styles.bgSidebar]">
            <div class="pdf-header flex items-center justify-between px-2 py-1.5 border-b flex-shrink-0" :class="$styles.chromeBorder">
                <span class="text-xs font-semibold uppercase tracking-wide" :class="$styles.muted">Templates</span>
                <div class="flex items-center gap-0.5">
                    <button type="button" @click="promptNewTemplate()" title="New template" class="p-1 rounded" :class="[$styles.icon, $styles.iconHover]">
                        <svg xmlns="http://www.w3.org/2000/svg" class="size-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 13h6m-3-3v6m5 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" /></svg>
                    </button>
                    <button type="button" @click="promptNewFolder()" title="New folder" class="p-1 rounded" :class="[$styles.icon, $styles.iconHover]">
                        <svg xmlns="http://www.w3.org/2000/svg" class="size-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m-3-3h6m-9 8h12a2 2 0 002-2V8a2 2 0 00-2-2h-5l-2-2H5a2 2 0 00-2 2v11a2 2 0 002 2z" /></svg>
                    </button>
                    <button type="button" @click="loadFiles" title="Refresh" class="p-1 rounded" :class="[$styles.icon, $styles.iconHover]">
                        <svg xmlns="http://www.w3.org/2000/svg" class="size-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" /></svg>
                    </button>
                </div>
            </div>
            <div class="flex-1 overflow-y-auto py-1" @contextmenu.prevent="openMenu({ event: $event })">
                <PdfFileNode v-for="node in tree" :key="node.path" :node="node" :selected-path="activeTab"
                    @select="onNodeSelect" @menu="openMenu" />
                <div v-if="!tree.length" class="px-3 py-6 text-center text-xs" :class="$styles.muted">
                    No templates yet
                </div>
            </div>
            <div class="px-2 py-1.5 border-t text-[10px] truncate flex-shrink-0" :class="[$styles.chromeBorder, $styles.muted]" :title="root">{{ root }}</div>
        </div>

        <div class="flex-1 flex min-w-0 overflow-hidden">

        <!-- Editors -->
        <div class="flex flex-col min-w-0 overflow-hidden" :style="{ width: prefs.splitPct + '%' }">
            <div class="pdf-header flex items-center gap-1 pr-2 border-b flex-shrink-0" :class="$styles.chromeBorder">
                <button type="button" @click="toggleExplorer" :title="prefs.showExplorer ? 'Hide files' : 'Show files'" class="p-1 ml-1 mr-1 rounded flex-shrink-0" :class="[$styles.icon, $styles.iconHover]">
                    <svg xmlns="http://www.w3.org/2000/svg" class="size-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" /></svg>
                </button>

                <!-- Tabs: the template + every resource it references -->
                <div class="flex-1 self-stretch flex items-stretch overflow-x-auto min-w-0">
                    <button v-for="tab in tabs" :key="tab.path" type="button" @click="selectTab(tab.path)"
                        :title="tab.missing ? tab.path + ' (referenced but missing - click to create)' : tab.path"
                        class="group flex items-center gap-1.5 px-2.5 text-xs border-b-2 whitespace-nowrap"
                        :class="[
                            activeTab === tab.path ? 'border-blue-500 ' + $styles.mutedActive : 'border-transparent ' + $styles.muted + ' ' + $styles.mutedHover,
                            tab.missing ? 'opacity-50 italic' : '',
                        ]">
                        <span :class="{ 'font-medium': tab.isEntry }">{{ tab.name }}</span>
                        <span v-if="isDirty(tab.path)" class="pdf-dirty size-1.5 rounded-full flex-shrink-0" title="Unsaved changes"></span>
                        <span v-if="tab.error" class="text-yellow-500 dark:text-yellow-400" :title="tab.error">!</span>
                        <span v-if="tab.closable" @click.stop="closeTab(tab.path)" class="opacity-0 group-hover:opacity-100" title="Close tab">&times;</span>
                    </button>
                    <div v-if="!tabs.length" class="flex items-center px-2 text-xs" :class="$styles.muted">No template selected</div>
                </div>

                <button type="button" @click="save" :disabled="!dirty" class="px-2.5 py-1 text-xs flex-shrink-0 disabled:opacity-40" :class="$styles.secondaryButton">Save</button>
            </div>

            <!-- views of the active .json: its source, the generated form, and each generated language -->
            <div v-if="activeIsData" class="flex items-center gap-2 px-2 py-1 border-b flex-shrink-0" :class="[$styles.chromeBorder, $styles.bgSidebar]">
                <div :class="btnGroup">
                    <button v-for="view in dataViews" :key="view.id" type="button" @click="setDataView(view.id)"
                        class="px-3 py-1 text-xs font-medium" :class="dataView === view.id ? btnOn : btnOff">
                        {{ view.label }}
                    </button>
                </div>
                <div :class="btnGroup">
                    <button v-for="lang in typeLanguages" :key="lang.id" type="button" @click="selectLanguage(lang)"
                        :title="'Generate ' + lang.file"
                        class="px-3 py-1 text-xs font-medium inline-flex items-center gap-1"
                        :class="dataView === lang.id ? btnOn : btnOff">
                        {{ lang.label }}
                    </button>
                </div>
                <div class="flex-1"></div>
                <span v-if="langFile" class="text-xs truncate" :class="$styles.muted" :title="langFile">{{ baseName(langFile) }}</span>
                <button v-if="!showForm" type="button" @click="copyEditor" class="px-2 py-1 text-xs inline-flex items-center gap-1" :class="$styles.secondaryButton" title="Copy to clipboard">
                    <svg xmlns="http://www.w3.org/2000/svg" class="size-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <rect x="9" y="9" width="13" height="13" rx="2"/><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/>
                    </svg>
                    {{ copied ? 'Copied' : 'Copy' }}
                </button>
            </div>

            <!-- formatting for the typst template -->
            <div v-if="showTypstBar" class="flex flex-wrap items-center gap-1 px-2 py-1 border-b flex-shrink-0" :class="[$styles.chromeBorder, $styles.bgSidebar]">
                <template v-for="(action, i) in typstActions" :key="action.id ?? 'd' + i">
                    <div v-if="action.divider" class="w-px h-4 flex-shrink-0" :class="$styles.chromeBorder" style="background:currentColor;opacity:.2"></div>
                    <button v-else type="button" @click="applyFormat(action)" :title="action.title"
                        class="px-1.5 py-1 rounded text-xs leading-4 flex-shrink-0 inline-flex items-center justify-center w-6"
                        :class="[$styles.icon, $styles.iconHover, action.cls]">
                        <svg v-if="action.icon" xmlns="http://www.w3.org/2000/svg" class="size-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" v-html="icons[action.icon]"></svg>
                        <span v-else>{{ action.text }}</span>
                    </button>
                </template>
                <button type="button" @click="showPageSetup = true" title="Page setup - paper, orientation, margins, columns"
                    class="px-1.5 py-1 rounded text-xs leading-4 flex-shrink-0 inline-flex items-center justify-center w-6"
                    :class="[$styles.icon, $styles.iconHover]">
                    <svg xmlns="http://www.w3.org/2000/svg" class="size-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" v-html="icons.page"></svg>
                </button>
                <button v-if="fonts.length" type="button" @click="openFontPicker"
                    title="Set the font - applies to the selection, or to the whole document"
                    class="px-1.5 py-1 rounded text-xs leading-4 flex-shrink-0 inline-flex items-center justify-center w-6"
                    :class="[$styles.icon, $styles.iconHover]">
                    <svg xmlns="http://www.w3.org/2000/svg" class="size-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" v-html="icons.font"></svg>
                </button>
                <button type="button" @click="insertMarkdown" title="Render Markdown"
                    class="px-2 py-1 text-xs flex-shrink-0 inline-flex items-center gap-1" :class="$styles.secondaryButton">

                    Markdown
                </button>
            </div>

            <div class="flex-1 min-h-0 overflow-hidden relative">
                <!-- schema driven form for a .json data file -->
                <div v-if="showForm" class="absolute inset-0 overflow-y-auto" :class="$styles.bgInput">
                    <div v-if="schemaBusy" class="h-full flex items-center justify-center gap-2 text-sm" :class="$styles.muted">
                        <svg class="animate-spin size-4 text-blue-500" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
                        </svg>
                        Generating form schema…
                    </div>
                    <div v-else-if="formError" class="p-4 text-xs" :class="$styles.muted">{{ formError }}</div>
                    <div v-else-if="!formSchema" class="h-full flex flex-col items-center justify-center gap-3 p-6 text-center">
                        <p class="text-xs max-w-sm" :class="$styles.muted">
                            No <span class="font-mono">{{ baseName(schemaOf(activeTab)) }}</span> yet. Generate a JSON Schema
                            for this data and the designer will render it as a form.
                        </p>
                        <button type="button" @click="generateSchema" class="px-3 py-1.5 text-xs" :class="$styles.primaryButton">Generate form schema</button>
                    </div>
                    <template v-else>
                        <div class="flex items-center justify-end px-3 pt-2">
                            <button type="button" @click="generateSchema()" :disabled="schemaBusy"
                                class="inline-flex items-center gap-1 px-1.5 py-0.5 text-xs rounded disabled:opacity-40"
                                :class="[$styles.muted, $styles.mutedHover]"
                                :title="'Rebuild ' + baseName(schemaOf(activeTab)) + ' from the current data - use it after changing the shape of the data'">
                                <svg xmlns="http://www.w3.org/2000/svg" class="size-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" /></svg>
                                Regenerate form
                            </button>
                        </div>
                        <JsonSchemaForm :schema="formSchema" :data="formData" :show-title="false" class="px-3 pb-3" @change="onFormChange" />
                    </template>
                </div>
                <div v-show="!activeIsImage && !showForm" ref="editorEl" class="h-full text-sm">
                    <textarea v-if="!hasCodeMirror" :value="editorContent" :readonly="!!langFile" @input="onTextareaInput" spellcheck="false"
                        class="w-full h-full p-3 font-mono text-xs resize-none outline-none" :class="[$styles.bgInput, $styles.textInput]"></textarea>
                </div>
                <div v-if="activeIsImage" class="absolute inset-0 overflow-auto p-4 flex items-center justify-center">
                    <img :src="rawUrl(activeTab)" :alt="activeTab" class="max-w-full shadow-lg" />
                </div>
                <div v-if="!tabs.length" class="absolute inset-0 flex items-center justify-center text-sm" :class="$styles.muted">
                    Select a .typ template
                </div>
            </div>

            <!-- Edit with AI -->
            <div class="flex-shrink-0 flex flex-col border-t" :class="$styles.chromeBorder">
                <div v-if="prefs.showAi" class="pdf-drag-y h-1 -mt-1" @mousedown.prevent="startDragAi"></div>
                <div class="flex items-center gap-2 px-2 py-1 select-none" :class="$styles.bgSidebar">
                    <button type="button" @click="toggleAi" class="flex items-center gap-1.5 text-xs" :class="$styles.muted">
                        <svg xmlns="http://www.w3.org/2000/svg" class="size-3 transition-transform" :class="{ '-rotate-90': !prefs.showAi }" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" /></svg>
                        <svg xmlns="http://www.w3.org/2000/svg" class="size-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 3v4M3 5h4M6 17v4m-2-2h4m5-16l2.286 6.857L21 12l-5.714 2.143L13 21l-2.286-6.857L5 12l5.714-2.143L13 3z" /></svg>
                        <span class="font-medium">Edit with AI</span>
                    </button>
                    <span v-if="!prefs.showAi && aiResult" class="text-xs truncate flex-1" :class="$styles.muted">{{ aiResult.message }}</span>
                    <div v-else class="flex-1"></div>
                    <svg v-if="aiBusy" class="animate-spin size-3.5 text-blue-500" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
                    </svg>
                    <span class="text-xs truncate" style="max-width:12rem" :class="$styles.muted" :title="aiModel || 'No model selected'">{{ aiModel || 'no model selected' }}</span>
                </div>

                <div v-show="prefs.showAi" class="flex flex-col overflow-hidden relative" :style="{ height: prefs.aiHeight + 'px' }"
                    @dragover.prevent="aiDragging = true" @dragleave="aiDragging = false" @drop.prevent="onAiDrop">
                    <div v-if="aiDragging" class="absolute inset-0 z-10 flex items-center justify-center text-xs rounded border-2 border-dashed border-indigo-400 bg-indigo-50 dark:bg-gray-900 text-indigo-700 dark:text-indigo-300">
                        Drop a screenshot or PDF to build the template from
                    </div>
                    <div class="flex-1 overflow-y-auto px-2 pt-2 min-h-0">
                        <div v-if="aiError" class="px-2 py-1.5 text-xs rounded border bg-red-50 dark:bg-red-900/30 border-red-200 dark:border-red-800 text-red-800 dark:text-red-200">
                            <div class="flex items-start gap-2">
                                <span class="flex-1">{{ aiError }}</span>
                                <button v-if="aiUndo" type="button" @click="undoAiEdit" class="flex-shrink-0 px-1.5 py-0.5 rounded hover:bg-red-100" title="Restore the previous contents">Undo</button>
                            </div>
                        </div>
                        <div v-else-if="aiResult" class="px-2 py-1.5 text-xs rounded" :class="$styles.bgPopover">
                            <div v-if="aiResult.message" class="mb-1.5 whitespace-pre-wrap" :class="$styles.muted">{{ aiResult.message }}</div>
                            <div v-if="aiResult.paths.length" class="flex flex-wrap items-center gap-1">
                                <span class="mr-1" :class="$styles.muted">Updated</span>
                                <span v-for="path in aiResult.paths" :key="path" @click="openTab(path)"
                                    class="px-1.5 py-0.5 rounded cursor-pointer" :class="$styles.codeTagStrong">{{ path }}</span>
                                <div class="flex-1"></div>
                                <button type="button" @click="undoAiEdit" class="px-1.5 py-0.5 rounded" :class="[$styles.muted, $styles.mutedHover]" title="Restore the previous contents">Undo</button>
                            </div>
                            <div v-else :class="$styles.muted">No changes were made</div>
                        </div>
                        <div v-else class="px-2 py-1.5 text-xs" :class="$styles.muted">
                            Describe a change and the model will rewrite the template and its data. Edits unsaved until saved.
                            <span class="block mt-1">Attach, drop or paste a screenshot or PDF (first {{ maxPdfPages }} pages) to be rebuild it as a template.</span>
                        </div>
                    </div>
                    <!-- attached screenshots / rasterised PDF pages -->
                    <div v-if="aiImages.length" class="flex flex-wrap gap-2 px-2 pt-2">
                        <div v-for="(img, i) in aiImages" :key="i" class="group relative">
                            <img :src="img.url" :alt="img.name" :title="img.name"
                                class="size-12 object-cover rounded border" :class="$styles.chromeBorder" />
                            <button type="button" @click="removeAiImage(i)" title="Remove"
                                class="absolute top-0 right-0 size-4 flex items-center justify-center rounded-full bg-gray-700 text-white text-[10px] opacity-0 group-hover:opacity-100">&times;</button>
                        </div>
                    </div>
                    <div class="flex items-end gap-2 p-2">
                        <input ref="aiFileInput" type="file" multiple accept="image/*,application/pdf" class="hidden" @change="onAiFiles" />
                        <button type="button" @click="$refs.aiFileInput.click()" :disabled="aiBusy || aiAttaching"
                            class="p-1.5 rounded flex-shrink-0 disabled:opacity-40" :class="[$styles.icon, $styles.iconHover]"
                            title="Attach a screenshot or PDF to build the template from">
                            <svg v-if="!aiAttaching" xmlns="http://www.w3.org/2000/svg" class="size-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.172 7l-6.586 6.586a2 2 0 102.828 2.828l6.414-6.586a4 4 0 00-5.656-5.656l-6.415 6.585a6 6 0 108.486 8.486L20.5 13" /></svg>
                            <svg v-else class="animate-spin size-4 text-blue-500" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
                            </svg>
                        </button>
                        <textarea ref="aiInput" v-model="aiPrompt" :disabled="aiBusy" @paste="onAiPaste"
                            @keydown.enter.exact.prevent="sendAiEdit()"
                            @keydown.up="cycleHistory(-1, $event)"
                            @keydown.down="cycleHistory(1, $event)"
                            @input="historyIndex = -1"
                            rows="2" placeholder="e.g. make the totals bold, add a Due column and highlight overdue invoices in red"
                            class="flex-1 min-w-0 px-2 py-1.5 text-xs rounded-md border resize-none disabled:opacity-50"
                            :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]"></textarea>
                        <div class="flex flex-col items-end gap-1 flex-shrink-0">
                            <button type="button" @click="sendAiEdit()" :disabled="aiBusy || !aiPrompt.trim() || !entry"
                                class="px-3 py-1.5 text-xs disabled:opacity-40" :class="$styles.primaryButton">
                                {{ aiBusy ? 'Working…' : 'Send' }}
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Splitter -->
        <div class="pdf-drag-x w-1 flex-shrink-0 transition-colors" @mousedown.prevent="startDragSplit"></div>

        <!-- Preview -->
        <div class="flex-1 flex flex-col min-w-0 overflow-hidden border-l" :class="[$styles.chromeBorder, $styles.bgPage]">
            <div class="pdf-header flex items-center gap-1 px-2 py-1.5 border-b flex-shrink-0" :class="$styles.chromeBorder">
                <button type="button" @click="zoom(-1)" title="Zoom out" class="p-1 rounded" :class="[$styles.icon, $styles.iconHover]">
                    <svg xmlns="http://www.w3.org/2000/svg" class="size-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 12H4" /></svg>
                </button>
                <span class="text-xs w-12 text-center tabular-nums" :class="$styles.muted">{{ Math.round(scale * 100) }}%</span>
                <button type="button" @click="zoom(1)" title="Zoom in" class="p-1 rounded" :class="[$styles.icon, $styles.iconHover]">
                    <svg xmlns="http://www.w3.org/2000/svg" class="size-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" /></svg>
                </button>
                <button type="button" @click="fitToWidth" title="Fit page to panel width" class="px-2 py-0.5 text-xs"
                    :class="fitMode ? $styles.primaryButton : $styles.secondaryButton">Fit</button>
                <span v-if="pages" class="text-xs ml-2" :class="$styles.muted">{{ pages }} page{{ pages === 1 ? '' : 's' }}</span>
                <svg v-if="rendering" class="animate-spin size-3.5 ml-1 text-blue-500" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
                </svg>
                <div class="flex-1"></div>
                <button type="button" @click="promptSavePdf" :disabled="!pdfBlob" title="Save this PDF into the saved/ folder"
                    class="inline-flex items-center gap-1.5 px-2.5 py-1 text-xs disabled:opacity-40 mr-1" :class="$styles.secondaryButton">
                    Save
                </button>
                <button type="button" @click="download" :disabled="!pdfBlob" class="inline-flex items-center gap-1.5 px-2.5 py-1 text-xs disabled:opacity-40" :class="$styles.primaryButton">
                    <svg xmlns="http://www.w3.org/2000/svg" class="size-3.5" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M3 17a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zm3.293-7.707a1 1 0 011.414 0L9 10.586V3a1 1 0 112 0v7.586l1.293-1.293a1 1 0 111.414 1.414l-3 3a1 1 0 01-1.414 0l-3-3a1 1 0 010-1.414z" clip-rule="evenodd" /></svg>
                    PDF
                </button>
                <template v-for="action in previewActions" :key="action.id">
                    <component v-if="(!action.isVisible || action.isVisible(previewContext)) && (!action.show || action.show(previewContext))"
                               :is="action.component"
                               v-bind="previewContext" />
                </template>
            </div>

            <div v-if="diagnostics.length" style="max-height:8rem" class="flex-shrink-0 overflow-y-auto pl-3 pr-1 py-1 text-xs font-mono border-b bg-red-50 dark:bg-red-900/30 border-red-200 dark:border-red-800 text-red-800 dark:text-red-200">
                <div class="flex items-center gap-2">
                    <div class="flex-1 min-w-0">
                        <div v-for="(d, i) in diagnostics" :key="i" @click="goToDiagnostic(d)" class="truncate cursor-pointer" :title="d.message">
                            <span v-if="d.line" class="opacity-70 mr-2">{{ d.file }}:{{ d.line }}:{{ d.col }}</span>{{ d.message }}
                        </div>
                    </div>
                    <button v-if="errorDiagnostics.length" type="button" @click="fixWithAi" :disabled="aiBusy"
                        class="flex-shrink-0 px-2 py-0.5 rounded border font-sans border-red-300 dark:border-red-700 hover:bg-red-100 disabled:opacity-40"
                        :title="'Ask ' + (aiModel || 'the model') + ' to fix ' + errorDiagnostics.length + ' error' + (errorDiagnostics.length === 1 ? '' : 's')">
                        {{ aiBusy ? 'Fixing…' : 'Fix' }}
                    </button>
                </div>
            </div>

            <div ref="previewEl" class="pdf-preview flex-1 overflow-auto p-4">
                <div v-if="!pages && !rendering" class="h-full flex items-center justify-center text-sm" :class="$styles.muted">
                    {{ entry ? 'Nothing rendered yet' : 'Select a .typ template to preview' }}
                </div>
                <div class="flex flex-col items-center gap-4">
                    <canvas v-for="n in pages" :key="n" :ref="el => canvasEls[n - 1] = el"
                        class="shadow-lg bg-white max-w-none"></canvas>
                </div>
            </div>
        </div>

        </div>

      </div>

        <PdfContextMenu v-if="menu" v-bind="menu" @pick="onMenuPick" />
        <ErrorViewer class="absolute top-2 left-1/2 -translate-x-1/2 z-100 max-w-lg w-full mx-4 shadow-lg" />
        <PdfPrompt v-if="prompt" v-bind="prompt" @submit="prompt?.onSubmit($event)" @alt="prompt?.onAlt?.()" @cancel="prompt?.onCancel?.(); prompt = null">
            <template v-if="prompt?.withAi" #extra>
                <div class="mt-3" @dragover.prevent @drop.prevent="onAiDrop">
                    <label class="block mb-1 text-xs" :class="$styles.muted">Describe it, and the model can build it after it's created (optional)</label>
                    <textarea v-model="aiPrompt" rows="3" @paste="onAiPaste" spellcheck="false"
                        placeholder="e.g. a delivery note with a signature box, or attach a design to copy"
                        class="w-full px-2.5 py-1.5 text-sm rounded-md border resize-none"
                        :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]"></textarea>
                    <div class="mt-1.5 flex items-center gap-2">
                        <input ref="newFileInput" type="file" multiple accept="image/*,application/pdf" class="hidden" @change="onAiFiles" />
                        <button type="button" @click="$refs.newFileInput.click()" :disabled="aiAttaching"
                            class="p-1 rounded flex-shrink-0 disabled:opacity-40" :class="[$styles.icon, $styles.iconHover]"
                            title="Attach a screenshot or PDF to build it from">
                            <svg xmlns="http://www.w3.org/2000/svg" class="size-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.172 7l-6.586 6.586a2 2 0 102.828 2.828l6.414-6.586a4 4 0 00-5.656-5.656l-6.415 6.585a6 6 0 108.486 8.486L20.5 13" /></svg>
                        </button>
                        <div v-for="(img, i) in aiImages" :key="i" class="group relative">
                            <img :src="img.url" :alt="img.name" :title="img.name" class="size-8 object-cover rounded border" :class="$styles.chromeBorder" />
                            <button type="button" @click="removeAiImage(i)" title="Remove"
                                class="absolute top-0 right-0 size-4 flex items-center justify-center rounded-full bg-gray-700 text-white text-[10px] opacity-0 group-hover:opacity-100">&times;</button>
                        </div>
                        <span v-if="!aiImages.length" class="text-xs" :class="$styles.muted">or drop / paste a screenshot</span>
                    </div>
                </div>
            </template>
        </PdfPrompt>
        <PdfImagePicker v-if="showImagePicker" :existing="folderImages" :stem="entryStem" :raw-url="rawUrl"
            @insert="insertImage" @upload="uploadImage" @close="showImagePicker = false" />
        <PdfPageSetup v-if="showPageSetup" :document-settings="documentPageSettings" @apply="handleApplyPageStyle" @close="showPageSetup = false" />
        <PdfFontPicker v-if="showFontPicker" :fonts="fonts" :document-settings="documentTextSettings" @apply="handleApplyTextStyle" @close="showFontPicker = false" />

        <component :is="'style'">
            #pdf-designer .CodeMirror { height: 100%; font-size: 12.5px; line-height: 1.5; }
            #pdf-designer .cm-error-line { background: rgba(239, 68, 68, 0.15); }
            #pdf-designer .pdf-drag-x { cursor: ew-resize; }
            #pdf-designer .pdf-drag-y { cursor: ns-resize; }
            #pdf-designer .pdf-drag-x:hover, #pdf-designer .pdf-drag-y:hover { background: rgba(59, 130, 246, 0.4); }
            #pdf-designer .pdf-dirty { background: #f59e0b; }
            #pdf-designer .pdf-header { min-height: 2.4375rem; } /* 39px: matches the tallest header (preview toolbar) */
            #pdf-designer .pdf-preview { scrollbar-gutter: stable; }
        </component>
    </div>`,

    setup() {
        /**@type {import('ctx.mjs').AppContext} */
        const ctx = inject('ctx')
        const route = useRoute()
        const router = useRouter()
        const prefs = ext.prefs

        const root = ref('')
        const files = ref([])
        const entry = ref(null) // the .typ being compiled

        const previewActions = computed(() => {
            const actions = ctx?.pdf?.previewActions || {}
            return Object.values(actions)
        })
        const previewContext = computed(() => ({
            pdfBlob: pdfBlob.value,
            entry: entry.value,
            files: files.value,
            buffers,
            pages: pages.value,
            rendering: rendering.value,
            save,
            download,
            promptSavePdf,
            root: root.value,
        }))
        const activeTab = ref(null)
        const buffers = reactive({}) // path -> { content, saved }
        const extraTabs = ref([]) // files opened from the explorer that the template doesn't reference

        const pages = ref(0)
        const scale = ref(prefs.scale ?? 1)
        const fitMode = ref(prefs.fit ?? true)
        const rendering = ref(false)
        const diagnostics = ref([])
        const pdfBlob = shallowRef(null)
        const prompt = ref(null)
        const menu = ref(null)

        const generated = reactive({}) // language id -> generated source, regenerated on demand and never saved
        const copied = ref(false)
        const schemaBusy = ref(false)
        const formSchema = shallowRef(null)
        const formData = ref(null) // deep ref: the form mutates nested objects/arrays in place
        const formError = ref('')
        let formSource = null // the buffer content the form was parsed from, to avoid re-parsing our own writes

        const aiPrompt = ref('')
        const aiImages = ref([]) // { name, url, width, height } - screenshots or rasterised PDF pages
        const aiAttaching = ref(false)
        const aiDragging = ref(false)
        let suggestedPrompt = '' // the last prompt we filled in, so we know it's ours to replace
        const aiInput = ref(null)
        const historyIndex = ref(-1) // -1 = editing a new prompt, 0 = most recent
        const aiBusy = ref(false)
        const aiError = ref('')
        const aiResult = ref(null)
        const aiUndo = ref(null) // { path: previous content } from the last AI run, for one click Undo

        const editorEl = ref(null)
        const previewEl = ref(null)
        const canvasEls = []

        const hasCodeMirror = ref(typeof CodeMirror !== 'undefined')
        const pdfView = new PdfView(ext.baseUrl)
        const docs = new Map() // path -> CodeMirror.Doc
        let cm = null
        let renderTimer = null
        let fitTimer = null
        let fittedWidth = 0
        let inflight = null
        let themeObserver = null
        let resizeObserver = null
        let menuNode = null
        let copiedTimer = null

        const tree = computed(() => groupFiles(files.value))
        const filePaths = computed(() => {
            const paths = []
            const walk = nodes => nodes.forEach(n => (n.isFile ? paths.push(n.path) : walk(n.children ?? [])))
            walk(files.value)
            return paths
        })

        const resources = computed(() => parseResources(entry.value, buffers[entry.value]?.content))

        const tabs = computed(() => {
            if (!entry.value) return []
            const seen = new Set()
            const list = []
            const add = (path, opts) => {
                if (seen.has(path)) return
                seen.add(path)
                list.push({
                    path,
                    name: baseName(path),
                    missing: !filePaths.value.includes(path),
                    error: jsonError(path),
                    ...opts,
                })
            }
            add(entry.value, { isEntry: true })
            resources.value.forEach(path => add(path, {}))
            extraTabs.value.forEach(path => add(path, { closable: true }))
            return list
        })

        const dirty = computed(() => Object.values(buffers).some(b => b.content !== b.saved))
        const activeContent = computed(() => buffers[activeTab.value]?.content ?? '')
        /** what the editor shows: generated code when a language view is active, the file otherwise */
        const editorContent = computed(() => generatedFor(editorPath.value) ?? activeContent.value)
        const activeIsImage = computed(() => !!activeTab.value && isImage(activeTab.value))
        const fonts = ref([])
        const activeIsTyp = computed(() => extName(activeTab.value ?? '') === '.typ')
        /** the font the document already sets, so the dropdown reads as its current value */
        const documentFont = computed(() => {
            if (!activeIsTyp.value) return null
            return buffers[activeTab.value]?.content?.match(SET_TEXT_FONT_RE)?.[2] ?? null
        })
        const showTypstBar = computed(() => activeIsTyp.value && hasCodeMirror.value)
        const activeIsJson = computed(() => extName(activeTab.value ?? '') === '.json')
        // .ui.json is the schema itself - it gets the code editor, not a form
        const activeIsData = computed(() => activeIsJson.value && !isSchemaFile(activeTab.value))
        const dataViews = [
            { id: 'code', label: 'Code' },
            { id: 'form', label: 'Form' },
        ]
        const VIEW_IDS = [...dataViews.map(v => v.id), ...TYPE_LANGUAGES.map(l => l.id)]
        const dataView = computed(() => (VIEW_IDS.includes(prefs.dataView) ? prefs.dataView : 'code'))
        const langView = computed(() => TYPE_LANGUAGES.find(l => l.id === dataView.value) ?? null)
        /** the file the editor shows: the .json itself, or one of the languages generated from it */
        const langFile = computed(() => (activeIsData.value && langView.value ? typesPathFor(langView.value) : null))
        const editorPath = computed(() => (langFile.value ? GEN_PREFIX + langFile.value : activeTab.value))
        const typeLanguages = computed(() => TYPE_LANGUAGES.map(lang => ({ ...lang, file: baseName(typesPathFor(lang)) })))
        const showForm = computed(() => activeIsData.value && dataView.value === 'form')

        /** clicking a language regenerates it and shows it in place - it's cheap, so it's never saved */
        async function selectLanguage(lang) {
            if (!generateTypes(lang.id)) return
            ext.setPrefs({ dataView: lang.id, lastPage: 'json' })
            await nextTick()
            showDoc(editorPath.value)
        }

        function isDirty(path) {
            const buffer = buffers[path]
            return !!buffer && buffer.content !== buffer.saved
        }

        // Formatting -----------------------------------------------------------
        /** Wrap the selection, prefix the selected lines, or drop a block in at the cursor */
        function applyFormat(action) {
            if (!cm) return
            cm.focus()
            if (action.dialog === 'image') return (showImagePicker.value = true)
            if (action.wrap) return wrapSelection(action)
            if (action.line) return toggleLinePrefix(action.line)
            if (action.snippet) return insertSnippet(action.snippet, action.inline)
        }

        function wrapSelection({ wrap: [open, close], placeholder }) {
            const selected = cm.getSelection()
            if (selected) return cm.replaceSelection(open + selected + close, 'around')
            const at = cm.getCursor()
            cm.replaceSelection(open + placeholder + close)
            // leave the placeholder selected so typing replaces it
            cm.setSelection(
                { line: at.line, ch: at.ch + open.length },
                { line: at.line, ch: at.ch + open.length + placeholder.length },
            )
        }

        function toggleLinePrefix(prefix) {
            const from = cm.getCursor('from').line
            const to = cm.getCursor('to').line
            const lines = []
            for (let n = from; n <= to; n++) lines.push(cm.getLine(n) ?? '')
            const on = lines.every(line => line.startsWith(prefix))
            lines.forEach((line, i) => {
                const next = on ? line.slice(prefix.length) : prefix + line
                cm.replaceRange(next, { line: from + i, ch: 0 }, { line: from + i, ch: line.length })
            })
        }

        function insertSnippet(snippet, inline = false) {
            const at = cm.getCursor()
            const line = cm.getLine(at.line) ?? ''
            if (inline) return cm.replaceSelection(snippet)
            // block snippets want their own line, and a blank one after
            const before = line.slice(0, at.ch).trim() ? '\n' : ''
            const after = line.slice(at.ch).trim() ? '\n' : '\n'
            cm.replaceSelection(before + snippet + after)
        }

        /**
         * Wrap the selection in `#text(font: ...)`, or retarget the document's existing `#set text` rule,
         * or add one when there isn't one yet.
         */
        function applyFont(name) {
            if (!cm || !name) return
            cm.focus()
            const selected = cm.getSelection()
            if (selected) return cm.replaceSelection(`#text(font: "${name}")[${selected}]`, 'around')

            const doc = cm.getValue()
            const rule = doc.match(SET_TEXT_FONT_RE)
            if (rule) {
                // swap just the name inside the set rule that's already there
                const at = doc.indexOf(rule[0]) + rule[1].length
                return cm.replaceRange(`"${name}"`, cm.posFromIndex(at), cm.posFromIndex(at + rule[2].length + 2))
            }
            insertSnippet(`#set text(font: "${name}", size: 11pt)`)
        }

        /** Parse text settings from the active document */
        const documentTextSettings = computed(() => {
            if (!activeIsTyp.value) return {}
            const content = buffers[activeTab.value]?.content ?? ''
            const rule = findSetTextRule(content)
            if (!rule) return {}
            const body = rule.body
            const font = body.match(/font:\s*"([^"]+)"/)?.[1] ?? null
            const size = body.match(/size:\s*([\d.]+)pt/)?.[1] ?? null
            // only a hex colour can go back into the <input type=color>; luma(90) and friends can't
            const hex = body.match(/fill:\s*(?:rgb\(\s*)?"?(#[0-9a-fA-F]{3,8})"?/)?.[1] ?? null
            const fill = hex ?? null
            const weight = body.match(/weight:\s*"([^"]+)"/)?.[1] ?? null
            const style = body.match(/style:\s*"([^"]+)"/)?.[1] ?? null
            const tracking = body.match(/tracking:\s*([\d.]+)pt/)?.[1] ?? null
            return { font, size, fill, weight, style, tracking }
        })

        const showImagePicker = ref(false)
        const folderImages = computed(() => filePaths.value.filter(isImage))
        const entryStem = computed(() => baseName(entry.value ?? 'template.typ').split('.')[0])

        /** typst resolves #image() relative to the template, so walk up out of any subfolder */
        function imageRef(path) {
            const depth = dirName(entry.value ?? '') ? dirName(entry.value).split('/').length : 0
            return '../'.repeat(depth) + path
        }

        function insertImage({ path, width }) {
            showImagePicker.value = false
            const size = width?.trim() && width.trim() !== 'auto' ? `, width: ${width.trim()}` : ''
            insertSnippet(`#image("${imageRef(path)}"${size})`)
        }

        async function uploadImage({ blob, path, width }) {
            const res = await ext.post(`/asset?path=${encodeURIComponent(path)}`, {
                body: blob,
                headers: { 'Content-Type': blob.type || 'application/octet-stream' },
            })
            if (!res.ok) {
                const api = await res.json().catch(() => null)
                showImagePicker.value = false
                return ext.setError(api?.responseStatus ?? { message: `Could not upload ${baseName(path)}` })
            }
            await loadFiles()
            insertImage({ path, width })
        }

        const showPageSetup = ref(false)

        /** Read the document's current `#set page(...)` so the dialog opens on what's already there */
        const documentPageSettings = computed(() => {
            if (!activeIsTyp.value) return {}
            const rule = findSetRule(buffers[activeTab.value]?.content ?? '', 'page')
            if (!rule) return {}
            const body = rule.body
            const margin = body.match(/margin:\s*([\d.]+)(mm|cm|in|pt)/)
            return {
                paper: body.match(/paper:\s*"([^"]+)"/)?.[1] ?? null,
                width: body.match(/width:\s*([\d.]+)mm/)?.[1] ?? null,
                height: body.match(/height:\s*([\d.]+)mm/)?.[1] ?? null,
                flipped: /flipped:\s*true/.test(body),
                margin: margin?.[1] ?? null,
                marginUnit: margin?.[2] ?? 'cm',
                columns: body.match(/columns:\s*(\d+)/)?.[1] ?? null,
                numbering: body.match(/numbering:\s*"([^"]*)"/)?.[1] ?? null,
                fill: body.match(/fill:\s*(?:rgb\(\s*)?"?(#[0-9a-fA-F]{3,8})"?/)?.[1] ?? null,
            }
        })

        /** Merge into the existing rule so anything the dialog doesn't manage (header, footer) survives */
        function applyPageStyle(updates) {
            if (!cm) return
            cm.focus()
            const doc = cm.getValue()
            const rule = findSetRule(doc, 'page')
            const params = mergeParams(rule?.body ?? '', updates)
            const text = `#set page(${params.join(', ')})`
            if (rule) return cm.replaceRange(text, cm.posFromIndex(rule.start), cm.posFromIndex(rule.end))
            insertRuleAtTop(text)
        }

        function handleApplyPageStyle(updates) {
            applyPageStyle(updates)
            showPageSetup.value = false
        }

        const showFontPicker = ref(false)

        function openFontPicker() {
            if (!fonts.value.length) loadFonts()
            showFontPicker.value = true
        }

        function applyTextStyle(styleObj) {
            if (!cm) return
            cm.focus()
            const { font, size, fill, weight, style, tracking } = styleObj
            const params = []
            if (font) params.push(`font: "${font}"`)
            if (size) params.push(`size: ${size}pt`)
            if (fill) params.push(`fill: rgb("${fill}")`)
            if (weight && weight !== 'regular') params.push(`weight: "${weight}"`)
            if (style && style !== 'normal') params.push(`style: "${style}"`)
            if (tracking && parseFloat(tracking) > 0) params.push(`tracking: ${tracking}pt`)

            if (!params.length) return

            const selected = cm.getSelection()
            if (selected) {
                return cm.replaceSelection(`#text(${params.join(', ')})[${selected}]`, 'around')
            }

            const doc = cm.getValue()
            const rule = findSetTextRule(doc)
            if (rule) {
                const newRule = `#set text(${params.join(', ')})`
                return cm.replaceRange(newRule, cm.posFromIndex(rule.start), cm.posFromIndex(rule.end))
            }
            insertRuleAtTop(`#set text(${params.join(', ')})`)
        }

        /** A `#set` rule only styles what follows it, so a document-wide one belongs above the content */
        function insertRuleAtTop(rule) {
            const lines = cm.getValue().split('\n')
            let at = 0
            while (at < lines.length && (!lines[at].trim() || /^\s*(\/\/|#import\b)/.test(lines[at]))) at++
            cm.replaceRange(rule + '\n', { line: at, ch: 0 })
        }

        function handleApplyTextStyle(styleObj) {
            applyTextStyle(styleObj)
            showFontPicker.value = false
        }

        async function loadFonts() {
            const api = await ext.getJson('/fonts')
            if (!api.error) fonts.value = api.response.fonts ?? []
        }

        /**
         * cmarker renders Markdown inside typst. What the button does depends on the selection:
         * nothing selected inserts a starter block, a reference like `#data.body` becomes
         * `#cmarker.render(data.body)`, and anything else is wrapped as literal Markdown.
         */
        function insertMarkdown() {
            if (!cm) return
            cm.focus()
            const selected = cm.getSelection()
            if (!cm.getValue().includes(CMARKER)) {
                // CodeMirror shifts the selection along with the insert, so this stays safe to do first
                cm.replaceRange(`#import "${CMARKER}"\n\n`, { line: 0, ch: 0 })
            }
            if (!selected.trim()) return insertSnippet(MARKDOWN_SNIPPET)

            const expression = selected.trim()
            if (isMarkdownRef(expression)) {
                // inside the call we're already in code, so the `#` goes
                return cm.replaceSelection(`#cmarker.render(${expression.replace(/^#/, '')})`, 'around')
            }
            cm.replaceSelection(['#cmarker.render(```md', selected.replace(/\s+$/, ''), '```)'].join('\n'), 'around')
        }

        function jsonError(path) {
            if (extName(path) !== '.json') return null
            const content = buffers[path]?.content
            if (!content?.trim()) return null
            try {
                JSON.parse(content)
                return null
            } catch (e) {
                return `invalid JSON: ${e.message}`
            }
        }

        function rawUrl(path) {
            return `${ext.baseUrl}/raw?path=${encodeURIComponent(path)}`
        }

        // Editor ---------------------------------------------------------------
        function cmTheme() {
            return ctx.getDarkMode() ? 'ctp-mocha' : 'default'
        }

        function initEditor() {
            if (cm || !hasCodeMirror.value || !editorEl.value) return
            defineTypstMode(CodeMirror)
            cm = CodeMirror(editorEl.value, {
                theme: cmTheme(),
                lineNumbers: true,
                styleActiveLine: true,
                matchBrackets: true,
                lineWrapping: true,
                tabSize: 2,
                indentUnit: 2,
                extraKeys: { 'Ctrl-S': () => save(), 'Cmd-S': () => save() },
            })
        }

        /** One CodeMirror instance, one Doc per open file, so each tab keeps its own history + cursor */
        function showDoc(path) {
            if (!cm || isImage(path)) return
            const gen = generatedFor(path)
            if (gen !== null) {
                // regenerated on every click, so it's read only rather than silently discarding edits
                let doc = docs.get(path)
                if (!doc) docs.set(path, (doc = CodeMirror.Doc(gen, editorMode(path))))
                else if (doc.getValue() !== gen) doc.setValue(gen)
                cm.swapDoc(doc)
                cm.setOption('mode', editorMode(path))
                cm.setOption('readOnly', true)
                cm.refresh()
                return
            }
            let doc = docs.get(path)
            if (!doc) {
                doc = CodeMirror.Doc(buffers[path]?.content ?? '', editorMode(path))
                doc.on('change', () => {
                    const buffer = buffers[path]
                    if (buffer) buffer.content = doc.getValue()
                })
                docs.set(path, doc)
            }
            cm.swapDoc(doc)
            cm.setOption('mode', editorMode(path))
            cm.setOption('readOnly', false)
            cm.refresh()
            markDiagnostics()
        }

        /** the generated source behind an editor key, or null when it's a real file */
        function generatedFor(path) {
            if (!path?.startsWith(GEN_PREFIX)) return null
            const lang = TYPE_LANGUAGES.find(l => path.endsWith(l.ext))
            return lang ? (generated[lang.id] ?? '') : null
        }

        async function copyEditor() {
            const text = generatedFor(editorPath.value) ?? cm?.getValue() ?? activeContent.value
            try {
                await navigator.clipboard.writeText(text)
            } catch {
                // clipboard is blocked outside a secure context - fall back to a temporary selection
                const el = document.createElement('textarea')
                el.value = text
                document.body.appendChild(el)
                el.select()
                document.execCommand('copy')
                el.remove()
            }
            copied.value = true
            clearTimeout(copiedTimer)
            copiedTimer = setTimeout(() => (copied.value = false), 1500)
        }

        function onTextareaInput(e) {
            if (buffers[activeTab.value]) buffers[activeTab.value].content = e.target.value
        }

        function markDiagnostics() {
            if (!cm) return
            for (let i = 0; i < cm.lineCount(); i++) {
                cm.removeLineClass(i, 'background', 'cm-error-line')
            }
            diagnostics.value
                .filter(d => d.line && (!d.file || d.file === activeTab.value))
                .forEach(d => {
                    if (d.line - 1 < cm.lineCount()) cm.addLineClass(d.line - 1, 'background', 'cm-error-line')
                })
        }

        async function goToDiagnostic(d) {
            if (!d.file || !d.line) return
            if (d.file !== activeTab.value) await openTab(d.file)
            cm?.setCursor({ line: d.line - 1, ch: Math.max(0, (d.col ?? 1) - 1) })
            cm?.scrollIntoView({ line: d.line - 1, ch: 0 }, 120)
            cm?.focus()
        }

        // Files ----------------------------------------------------------------
        async function loadFiles() {
            const api = await ext.getJson('/files')
            if (api.error) return ext.setError(api.error)
            root.value = api.response.path
            files.value = api.response.files
            if (!entry.value) {
                // the URL wins on load, as long as it still points at something that exists
                const wanted = route.query.template
                const target = (wanted && filePaths.value.includes(wanted) ? wanted : null)
                    ?? filePaths.value.find(p => p.endsWith('.typ'))
                if (target) await selectTemplate(target, { replace: true })
            }
        }

        async function loadBuffer(path) {
            if (path?.startsWith(GEN_PREFIX) || buffers[path] || isImage(path)) return
            if (!filePaths.value.includes(path)) {
                // referenced but not created yet: start an empty buffer, Save writes it out
                buffers[path] = { content: '', saved: null }
                return
            }
            const api = await ext.getJson(`/file?path=${encodeURIComponent(path)}`)
            if (api.error) {
                buffers[path] = { content: '', saved: null }
                return
            }
            buffers[path] = { content: api.response.content, saved: api.response.content }
        }

        /**
         * The open template lives in `?template=`, so a reload comes back to it and back/forward walk
         * through the templates you visited. `replace` is for moves the user didn't navigate to - the
         * initial pick, a rename, a delete - which shouldn't add a history entry.
         */
        function syncUrl(path, replace = false) {
            if ((route.query.template ?? null) === (path ?? null)) return
            const query = { ...route.query }
            if (path) query.template = path
            else delete query.template
            router[replace ? 'replace' : 'push']({ query })
        }

        /**
         * Switching template throws away every buffer, so unsaved work gets a say first. `force` is set by
         * the prompt's own answer, and `onCancel` puts the URL back when the switch came from history.
         */
        async function selectTemplate(path, { replace = false, force = false, onCancel = null, proceed = null } = {}) {
            if (!force && entry.value && path !== entry.value && dirty.value) {
                confirmUnsaved({
                    // callers that do more than switch (openGroup) resume their whole flow instead
                    proceed: proceed ?? (() => selectTemplate(path, { replace, force: true })),
                    cancel: onCancel,
                })
                return false
            }
            syncUrl(path, replace)
            ext.setPrefs({ lastPage: 'typ' })
            entry.value = path
            extraTabs.value = []
            Object.keys(buffers).forEach(key => delete buffers[key])
            docs.clear()
            diagnostics.value = []
            await loadBuffer(path)
            await openTab(path)
            scheduleRender(0)
            return true
        }

        /** Save / Discard / Cancel before unsaved edits are thrown away */
        function confirmUnsaved({ proceed, cancel }) {
            const pending = Object.entries(buffers)
                .filter(([, b]) => b.content !== b.saved)
                .map(([path]) => baseName(path))
            prompt.value = {
                title: 'Unsaved changes',
                message: `${pending.join(', ')} ${pending.length === 1 ? 'has' : 'have'} unsaved changes.`,
                confirmOnly: true,
                okText: 'Save',
                altText: 'Discard',
                async onSubmit() {
                    prompt.value = null
                    await save()
                    proceed()
                },
                onAlt() {
                    prompt.value = null
                    proceed()
                },
                onCancel: cancel,
            }
        }

        /** a tab click also updates which page type to reopen on the next document */
        function selectTab(path) {
            if (path === entry.value) ext.setPrefs({ lastPage: 'typ' })
            else if (extName(path) === '.json' && !isSchemaFile(path)) ext.setPrefs({ lastPage: 'json' })
            return openTab(path)
        }

        async function openTab(path) {
            if (isImage(path)) {
                activeTab.value = path
                return
            }
            await loadBuffer(path)
            await loadSchemaFor(path)
            activeTab.value = path
            await nextTick()
            showDoc(editorPath.value)
        }

        /** keep a data file's <name>.ui.json alongside it - generated types are sharper with the schema */
        async function loadSchemaFor(path) {
            if (extName(path ?? '') !== '.json' || isSchemaFile(path)) return
            const schemaPath = schemaOf(path)
            if (filePaths.value.includes(schemaPath)) await loadBuffer(schemaPath)
        }

        function closeTab(path) {
            extraTabs.value = extraTabs.value.filter(p => p !== path)
            docs.delete(path)
            if (!resources.value.includes(path)) delete buffers[path]
            if (activeTab.value === path) openTab(entry.value)
        }

        /**
         * Which page a group opens, cascading down from whatever the user last had open:
         * a language -> the form -> the raw json -> the template.
         */
        function cascadeFor(group) {
            const paths = group.paths ?? [group.path]
            const find = test => paths.find(test)
            const typ = find(p => p.endsWith('.typ'))
            const json = find(p => p.endsWith('.json') && !isSchemaFile(p))
            const hasSchema = json ? paths.includes(schemaOf(json)) : false

            const preferred = prefs.lastPage === 'json' ? dataView.value : 'typ'
            const order = TYPE_LANGUAGES.some(l => l.id === preferred)
                ? [preferred, 'form', 'code', 'typ']
                : preferred === 'form'
                    ? ['form', 'code', 'typ']
                    : preferred === 'code'
                        ? ['code', 'typ']
                        : ['typ', 'code']

            for (const step of order) {
                if (step === 'typ' && typ) return { view: 'typ', typ }
                if (step === 'code' && json) return { view: 'code', typ, json }
                if (step === 'form' && json && hasSchema) return { view: 'form', typ, json }
                if (json && TYPE_LANGUAGES.some(l => l.id === step)) return { view: step, typ, json }
            }
            return { view: 'typ', typ: typ ?? group.path }
        }

        /** Open a group at the remembered page type, remembering whatever it settled on */
        async function openGroup(group, { force = false } = {}) {
            const target = cascadeFor(group)
            if (target.typ && target.typ !== entry.value) {
                const switched = await selectTemplate(target.typ, {
                    force,
                    proceed: () => openGroup(group, { force: true }),
                })
                if (!switched) return
            }
            if (target.view === 'typ') {
                ext.setPrefs({ lastPage: 'typ' })
                if (!target.typ) await onNodeSelect(group.primary)
                return
            }
            if (!tabs.value.some(t => t.path === target.json) && !extraTabs.value.includes(target.json)) {
                extraTabs.value = [...extraTabs.value, target.json]
            }
            ext.setPrefs({ lastPage: 'json', dataView: target.view })
            await openTab(target.json)
            if (target.view === 'form') loadForm()
        }

        async function onNodeSelect(node) {
            if (node.isGroup) return openGroup(node)
            // a rendered PDF isn't editable - hand it to the browser's own viewer
            if (node.ext === '.pdf') return window.open(rawUrl(node.path), '_blank', 'noopener')
            if (node.ext === '.typ' && node.path !== entry.value) return selectTemplate(node.path)
            if (!tabs.value.some(t => t.path === node.path) && !extraTabs.value.includes(node.path)) {
                extraTabs.value = [...extraTabs.value, node.path]
            }
            await openTab(node.path)
        }

        async function save() {
            const pending = Object.entries(buffers).filter(([, b]) => b.content !== b.saved)
            if (!pending.length) return
            for (const [path, buffer] of pending) {
                const api = await ext.postJson('/file', { path, content: buffer.content })
                if (api.error) return ext.setError(api.error)
                buffer.saved = buffer.content
            }
            await loadFiles()
            ext.toast(pending.length === 1 ? `Saved ${baseName(pending[0][0])}` : `Saved ${pending.length} files`)
        }

        // Explorer actions -----------------------------------------------------
        function targetDir(node) {
            if (!node) return ''
            return node.isFile ? dirName(node.path) : node.path
        }

        function promptNewTemplate(node, { force = false } = {}) {
            // creating switches to the new template, which would strand unsaved edits - settle them first
            if (!force && dirty.value) {
                return confirmUnsaved({ proceed: () => promptNewTemplate(node, { force: true }), cancel: null })
            }
            const dir = targetDir(node)
            prompt.value = {
                title: 'New template',
                message: `Creates ${joinPath(dir, '<name>.typ')} with a matching .json data file`,
                value: 'template.typ',
                okText: 'Create',
                withAi: true,
                async onSubmit(name) {
                    prompt.value = null
                    const api = await ext.postJson('/create', { path: joinPath(dir, name), withData: true })
                    if (api.error) return ext.setError(api.error)
                    await loadFiles()
                    // already answered for above, so don't ask again on the way in
                    await selectTemplate(api.response.path, { force: true })
                    if (aiPrompt.value.trim() || aiImages.value.length) {
                        // they've already said what they want, so send it at the new template right away
                        ext.setPrefs({ showAi: true })
                        if (aiImages.value.length) suggestRebuildPrompt()
                        await nextTick()
                        if (aiPrompt.value.trim()) await sendAiEdit()
                        else aiInput.value?.focus()
                    }
                },
            }
        }

        function promptNewFile(node) {
            const dir = targetDir(node)
            prompt.value = {
                title: 'New file',
                message: dir ? `Created in ${dir}/` : '',
                value: 'data.json',
                okText: 'Create',
                async onSubmit(name) {
                    prompt.value = null
                    const path = joinPath(dir, name)
                    const content = extName(path) === '.json' ? '{\n    \n}\n' : ''
                    const api = await ext.postJson('/file', { path, content })
                    if (api.error) return ext.setError(api.error)
                    await loadFiles()
                    await onNodeSelect({ path, ext: extName(path), isFile: true })
                },
            }
        }

        function promptNewFolder(node) {
            const dir = targetDir(node)
            prompt.value = {
                title: 'New folder',
                message: dir ? `Created in ${dir}/` : '',
                value: '',
                okText: 'Create',
                async onSubmit(name) {
                    prompt.value = null
                    const api = await ext.postJson('/folder', { path: joinPath(dir, name) })
                    if (api.error) return ext.setError(api.error)
                    await loadFiles()
                },
            }
        }

        /** Files that travel with a template when it's renamed - its data, schema, preview */
        function companionsOf(path) {
            if (extName(path) !== '.typ') return []
            const stem = baseName(path).split('.')[0]
            const dir = dirName(path)
            return filePaths.value.filter(
                p => p !== path && dirName(p) === dir && baseName(p).startsWith(stem + '.'),
            )
        }

        function promptRename(node) {
            const others = companionsOf(node.path).map(baseName)
            prompt.value = {
                title: 'Rename',
                message: others.length ? `${others.join(', ')} will be renamed too, and references updated` : '',
                value: node.name,
                okText: 'Rename',
                async onSubmit(name) {
                    prompt.value = null
                    // rename moves files on disk, so unsaved buffers would be stranded - write them out first
                    if (dirty.value) await save()
                    const to = joinPath(dirName(node.path), name)
                    const api = await ext.postJson('/rename', { from: node.path, to })
                    if (api.error) return ext.setError(api.error)
                    await loadFiles()
                    if (entry.value === node.path) {
                        // the buffers are keyed by the old paths, so reload the document from disk
                        Object.keys(buffers).forEach(key => delete buffers[key])
                        docs.clear()
                        await selectTemplate(api.response.path, { replace: true, force: true })
                    }
                },
            }
        }

        function promptDelete(node) {
            const others = companionsOf(node.path).map(baseName)
            prompt.value = {
                title: `Delete ${node.name}?`,
                message: others.length ? `${others.join(', ')} will be deleted too` : '',
                okText: 'Delete',
                danger: true,
                confirmOnly: true,
                async onSubmit() {
                    prompt.value = null
                    const api = await ext.deleteJson(`/file?path=${encodeURIComponent(node.path)}&sidecar=true`)
                    if (api.error) return ext.setError(api.error)
                    if (entry.value === node.path || (node.isFile === false && entry.value?.startsWith(node.path + '/'))) {
                        syncUrl(null, true)
                        entry.value = null
                        activeTab.value = null
                        extraTabs.value = []
                        Object.keys(buffers).forEach(key => delete buffers[key])
                        docs.clear()
                        pages.value = 0
                        pdfBlob.value = null
                        pdfView.destroy()
                    } else {
                        closeTab(node.path)
                    }
                    await loadFiles()
                },
            }
        }

        // Context menu ---------------------------------------------------------
        function openMenu({ event, node }) {
            menuNode = node ?? null
            const items = [
                { id: 'new-template', label: 'New Template' },
                { id: 'new-file', label: 'New File' },
                { id: 'new-folder', label: 'New Folder' },
            ]
            if (node) {
                items.push(
                    { divider: true },
                    { id: 'rename', label: 'Rename…' },
                    { id: 'delete', label: 'Delete', danger: true },
                )
            }
            items.push({ divider: true }, { id: 'refresh', label: 'Refresh' })
            // keep the menu on screen
            const width = 176
            const height = items.length * 30 + 8
            menu.value = {
                x: Math.min(event.clientX, window.innerWidth - width - 8),
                y: Math.min(event.clientY, window.innerHeight - height - 8),
                items,
            }
        }

        function closeMenu() {
            menu.value = null
            menuNode = null
        }



        function onMenuPick(item) {
            const node = menuNode
            closeMenu()
            if (item.id === 'new-template') promptNewTemplate(node)
            else if (item.id === 'new-file') promptNewFile(node)
            else if (item.id === 'new-folder') promptNewFolder(node)
            else if (item.id === 'rename') promptRename(node)
            else if (item.id === 'delete') promptDelete(node)
            else if (item.id === 'refresh') loadFiles()
        }

        // Form view over a .json data file ---------------------------------------
        function setDataView(view) {
            ext.setPrefs({ dataView: view, lastPage: 'json' })
            if (view === 'form') loadForm()
            else {
                nextTick(() => {
                    showDoc(activeTab.value) // back to the .json source
                    cm?.refresh()
                })
            }
        }

        /** Parse the data buffer and its <name>.ui.json schema, if one exists */
        async function loadForm() {
            const dataPath = activeTab.value
            if (!dataPath || isSchemaFile(dataPath)) return
            formError.value = ''
            const raw = buffers[dataPath]?.content ?? ''
            try {
                formData.value = JSON.parse(raw || '{}')
                formSource = raw
            } catch (e) {
                formData.value = null
                formSchema.value = null
                formError.value = `This file isn't valid JSON yet - fix it in the Code view. (${e.message})`
                return
            }
            const path = schemaOf(dataPath)
            // a freshly generated schema is only an unsaved buffer, so it won't be in the file list yet
            if (!buffers[path] && !filePaths.value.includes(path)) {
                formSchema.value = null
                return
            }
            await loadBuffer(path)
            try {
                formSchema.value = JSON.parse(buffers[path]?.content ?? '{}')
            } catch (e) {
                formSchema.value = null
                formError.value = `${baseName(path)} isn't valid JSON: ${e.message}`
            }
        }

        /** Write the edited object back into the data buffer, which re-renders the preview */
        function onFormChange() {
            const dataPath = activeTab.value
            if (!dataPath || !buffers[dataPath]) return
            const json = JSON.stringify(formData.value, null, 2) + '\n'
            formSource = json
            buffers[dataPath].content = json
            docs.get(dataPath)?.setValue(json)
        }

        async function generateSchema(dataPath = activeTab.value, { quiet = false } = {}) {
            if (!dataPath || schemaBusy.value) return false
            formError.value = ''
            if (!aiModel.value) {
                if (!quiet) formError.value = 'Select a model first, then generate the form schema.'
                return false
            }
            schemaBusy.value = true
            try {
                const api = await tools.postJson('/schema', {
                    name: dataPath,
                    model: aiModel.value,
                    content: buffers[dataPath]?.content,
                })
                if (api.error) {
                    if (!quiet) formError.value = api.error.message ?? 'Schema generation failed'
                    return false
                }
                const path = schemaOf(dataPath)
                const { content } = api.response
                buffers[path] = { content, saved: null } // unsaved, like every other generated file
                docs.get(path)?.setValue(content)
                if (!extraTabs.value.includes(path)) extraTabs.value = [...extraTabs.value, path]
                if (dataPath === activeTab.value) formSchema.value = JSON.parse(content)
                ext.toast(`Generated ${baseName(path)} - Save to keep it`)
                return true
            } catch (e) {
                if (!quiet) formError.value = `${e.message ?? e}`
                return false
            } finally {
                schemaBusy.value = false
            }
        }

        // Generate typed classes from a .json data file --------------------------
        /** where `<name>.json` would generate `<name>.<ext>` for a language */
        function typesPathFor(lang) {
            return (activeTab.value ?? '').replace(/\.json$/, lang.ext)
        }

        /** Types are generated locally - no model needed. A <name>.ui.json schema, when one exists, is used
         *  in preference to the example, since it carries types JSON can't (decimals, dates, enums, required).
         *  Generating is instant, so the result is thrown away and rebuilt rather than written to disk. */
        function generateTypes(language) {
            const jsonPath = activeTab.value
            if (!jsonPath) return false
            aiError.value = ''
            try {
                const { content } = generateTypesFor({
                    name: jsonPath,
                    json: buffers[jsonPath]?.content || '{}',
                    schema: buffers[schemaOf(jsonPath)]?.content || undefined,
                    language,
                })
                generated[language] = content
                return true
            } catch (e) {
                // the only way this fails is malformed JSON in the data file or its schema
                aiError.value = `${e.message ?? e}`
                ext.setPrefs({ showAi: true })
                return false
            }
        }

        // AI editing -----------------------------------------------------------
        const aiModel = computed(() => ctx.state.selectedModel)

        /** Last AI_HISTORY_MAX prompts, newest first, persisted with the rest of the pdf prefs */
        const aiHistory = computed(() => (Array.isArray(prefs.aiHistory) ? prefs.aiHistory : []))
        let historyDraft = ''

        function rememberPrompt(text) {
            const history = [text, ...aiHistory.value.filter(p => p !== text)].slice(0, AI_HISTORY_MAX)
            ext.setPrefs({ aiHistory: history })
        }

        /**
         * Cycle older (-1) / newer (+1) prompts. Only takes over the arrow key when the caret is on the
         * first/last line, so moving between the lines of a multi-line prompt still works as normal.
         */
        function cycleHistory(direction, e) {
            const history = aiHistory.value
            if (!history.length || aiBusy.value) return
            // only from the very start of the box - anywhere else the arrows just move the caret
            const box = e.target
            if (box.selectionStart !== 0 || box.selectionEnd !== 0) return

            const next = historyIndex.value + (direction < 0 ? 1 : -1)
            if (next < -1 || next >= history.length) return
            e.preventDefault()

            if (historyIndex.value === -1) historyDraft = aiPrompt.value
            historyIndex.value = next
            aiPrompt.value = next === -1 ? historyDraft : history[next]
            // leave the caret where it was so the next press keeps walking the history
            nextTick(() => aiInput.value?.setSelectionRange(0, 0))
        }

        function selectedModelInfo() {
            return (ctx.state.models ?? []).find(m => m.id === aiModel.value || m.name === aiModel.value)
        }

        /** Screenshots and PDFs the model should build the template from */
        async function attachFiles(files) {
            const list = [...(files ?? [])]
            if (!list.length) return
            aiError.value = ''
            aiAttaching.value = true
            try {
                const { attachments, errors } = await toAttachments(list, ext.baseUrl)
                const room = MAX_ATTACHMENTS - aiImages.value.length
                aiImages.value = [...aiImages.value, ...attachments.slice(0, Math.max(0, room))]
                if (attachments.length > room) {
                    errors.push(`Only ${MAX_ATTACHMENTS} attachments at a time - the rest were dropped`)
                }
                if (errors.length) aiError.value = errors.join('. ')
                if (attachments.length) {
                    ext.setPrefs({ showAi: true })
                    suggestRebuildPrompt()
                }
            } catch (e) {
                aiError.value = `${e.message ?? e}`
            } finally {
                aiAttaching.value = false
            }
        }

        function onAiFiles(e) {
            attachFiles(e.target.files)
            e.target.value = '' // so picking the same file twice still fires
        }

        function onAiDrop(e) {
            aiDragging.value = false
            attachFiles(e.dataTransfer?.files)
        }

        /** Ctrl+V of a screenshot straight into the prompt box */
        function onAiPaste(e) {
            const files = [...(e.clipboardData?.files ?? [])]
            if (!files.length) return
            e.preventDefault()
            attachFiles(files)
        }

        function removeAiImage(i) {
            aiImages.value = aiImages.value.filter((_, n) => n !== i)
            // the suggestion only makes sense while something is attached
            if (!aiImages.value.length && aiPrompt.value === suggestedPrompt) aiPrompt.value = ''
        }

        /** Fill in the ask for them - attaching a design almost always means "rebuild this" */
        function suggestRebuildPrompt() {
            if (aiPrompt.value.trim() && aiPrompt.value !== suggestedPrompt) return // never clobber their own words
            const typ = baseName(entry.value ?? 'the template')
            const data = baseName(sidecarOf(entry.value ?? 'data.typ'))
            const pages = aiImages.value.length > 1 ? 'attached pages' : 'attached document'
            suggestedPrompt =
                `Rebuild ${typ} so it reproduces the ${pages}: match the layout as closely as typst allows, ` +
                `and put the values it shows in ${data} rather than hardcoding them in the template.`
            aiPrompt.value = suggestedPrompt
            historyIndex.value = -1
            // land the cursor in the box so Enter sends it as-is, or they can edit it first
            nextTick(() => aiInput.value?.focus())
        }

        const errorDiagnostics = computed(() => diagnostics.value.filter(d => d.severity !== 'warning'))

        /** Hand the model exactly what typst said, positions and all */
        function buildFixPrompt(errors) {
            const lines = errors.map(d =>
                d.line ? `${d.file ?? entry.value}:${d.line}:${d.col ?? 1}: ${d.message}` : d.message,
            )
            return (
                `This does not compile. typst reports:\n\n${lines.join('\n')}\n\n` +
                'Fix the errors and return the complete corrected files.'
            )
        }

        /** The Fix button: same request the auto-repair makes, but started by hand */
        async function fixWithAi() {
            if (!errorDiagnostics.value.length || aiBusy.value) return
            ext.setPrefs({ showAi: true })
            aiPrompt.value = buildFixPrompt(errorDiagnostics.value)
            await sendAiEdit()
        }

        async function sendAiEdit({ fixAttempt = 0 } = {}) {
            const request = aiPrompt.value.trim()
            if (!request || !entry.value) return
            // a retry runs inside the original call, so it has to be allowed past the busy guard
            if (aiBusy.value && !fixAttempt) return

            aiError.value = ''
            if (!aiModel.value) {
                aiError.value = 'Select a model first'
                return
            }
            const modalities = selectedModelInfo()?.modalities
            const output = modalities?.output
            if (output?.length && !output.includes('text')) {
                aiError.value = `${aiModel.value} outputs ${output.join('/')}, not text. Select a text model.`
                return
            }
            const input = modalities?.input
            if (aiImages.value.length && input?.length && !input.includes('image')) {
                aiError.value = `${aiModel.value} can't read images. Select a vision model, or remove the attachments.`
                return
            }

            // give the model the template plus every text resource it references
            for (const tab of tabs.value) {
                if (!isImage(tab.path)) await loadBuffer(tab.path)
            }
            const files = {}
            for (const [path, buffer] of Object.entries(buffers)) {
                if (!isImage(path)) files[path] = buffer.content
            }

            aiBusy.value = true
            aiResult.value = null
            try {
                const api = await ext.postJson('/ai', {
                    path: entry.value,
                    prompt: request,
                    model: aiModel.value,
                    files,
                    images: aiImages.value.map(img => img.url),
                })
                if (api.error) {
                    aiError.value = api.error.message ?? 'AI request failed'
                    return
                }
                const edits = api.response.files ?? {}
                // keep the previous contents so the edit can be undone in one click. On a retry the
                // earlier snapshot wins, so Undo reverts the whole chain rather than the last attempt.
                const before = Object.fromEntries(Object.keys(edits).map(path => [path, buffers[path]?.content ?? null]))
                aiUndo.value = fixAttempt ? { ...before, ...aiUndo.value } : before
                applyAiEdits(edits)
                aiResult.value = { message: api.response.message, paths: Object.keys(edits) }
                if (!fixAttempt) rememberPrompt(request) // a generated fix prompt isn't worth recalling
                aiImages.value = []
                aiPrompt.value = ''
                historyIndex.value = -1
                historyDraft = ''
                if (Object.keys(edits).length) await verifyOrFix(fixAttempt)
                // only the outer call, and only once it compiles - a broken template isn't worth a schema
                if (!fixAttempt && !errorDiagnostics.value.length) await ensureSchema()
            } catch (e) {
                aiError.value = `${e.message ?? e}`
            } finally {
                aiBusy.value = false
            }
        }

        /**
         * A template that renders is worth a form. Generate the schema the first time one is missing,
         * so the Form tab works without the user having to ask for it.
         */
        async function ensureSchema() {
            const dataPath = sidecarOf(entry.value ?? '')
            if (!dataPath || !buffers[dataPath] || filePaths.value.includes(schemaOf(dataPath))) return
            if (buffers[schemaOf(dataPath)]) return // already generated this session, just unsaved
            await generateSchema(dataPath, { quiet: true })
        }

        /**
         * Compile what the model just wrote and, if typst rejects it, hand the errors straight back -
         * up to MAX_FIX_ATTEMPTS times before leaving it to the user.
         */
        async function verifyOrFix(fixAttempt) {
            clearTimeout(renderTimer) // the buffer watcher queued one; render now so we can read the result
            await render()
            const errors = errorDiagnostics.value
            if (!errors.length) return
            if (fixAttempt >= MAX_FIX_ATTEMPTS) {
                aiError.value = `Still not compiling after ${MAX_FIX_ATTEMPTS} attempts - fix the errors above, or try again.`
                return
            }
            aiPrompt.value = buildFixPrompt(errors)
            aiResult.value = {
                message: `Attempt ${fixAttempt + 1} of ${MAX_FIX_ATTEMPTS}: asking ${aiModel.value} to fix ${errors.length} error${errors.length === 1 ? '' : 's'}…`,
                paths: [],
            }
            await sendAiEdit({ fixAttempt: fixAttempt + 1 })
        }

        /** Land AI edits as unsaved buffers so they re-render live and can be reviewed before saving */
        function applyAiEdits(edits) {
            for (const [path, content] of Object.entries(edits)) {
                if (buffers[path]) buffers[path].content = content
                else buffers[path] = { content, saved: null } // new file: dirty until saved
                const doc = docs.get(path)
                if (doc) doc.setValue(content)
            }
            const changed = Object.keys(edits)
            if (changed.length) openTab(changed.includes(entry.value) ? entry.value : changed[0])
        }

        function undoAiEdit() {
            if (!aiUndo.value) return
            for (const [path, content] of Object.entries(aiUndo.value)) {
                if (content === null) continue
                if (buffers[path]) buffers[path].content = content
                docs.get(path)?.setValue(content)
            }
            aiUndo.value = null
            aiResult.value = null
        }

        // Render ---------------------------------------------------------------
        function scheduleRender(delay = RENDER_DEBOUNCE_MS) {
            clearTimeout(renderTimer)
            renderTimer = setTimeout(() => render(), delay)
        }

        async function render() {
            if (!entry.value) return
            inflight?.abort()
            const controller = (inflight = new AbortController())
            rendering.value = true
            try {
                // only unsaved buffers need sending, the mirror already has what's on disk
                const overlay = {}
                for (const [path, buffer] of Object.entries(buffers)) {
                    if (buffer.content !== buffer.saved) overlay[path] = buffer.content
                }
                overlay[entry.value] = buffers[entry.value]?.content ?? ''

                const res = await ext.post('/render', {
                    body: JSON.stringify({ path: entry.value, files: overlay }),
                    signal: controller.signal,
                })
                if (!res.ok) {
                    const error = await res.json().catch(() => null)
                    diagnostics.value = error?.diagnostics?.length
                        ? error.diagnostics
                        : [{ severity: 'error', message: error?.responseStatus?.message ?? res.statusText }]
                    markDiagnostics()
                    return // keep the last good preview on screen
                }
                diagnostics.value = []
                markDiagnostics()
                const blob = await res.blob()
                pdfBlob.value = blob
                pages.value = await pdfView.load(await blob.arrayBuffer())
                await nextTick()
                const fitted = fitMode.value ? await fitScale() : null
                if (fitted && Math.abs(fitted - scale.value) > 0.001) {
                    scale.value = fitted // the scale watcher paints the pages
                } else {
                    await renderPages()
                }
            } catch (e) {
                if (e.name !== 'AbortError') ext.setError(ext.createErrorResult(e).error)
            } finally {
                if (inflight === controller) {
                    inflight = null
                    rendering.value = false
                }
            }
        }

        async function renderPages() {
            for (let n = 1; n <= pages.value; n++) {
                await pdfView.renderPage(n, canvasEls[n - 1], scale.value)
            }
        }

        function zoom(direction) {
            const current = scale.value
            const next = direction > 0
                ? ZOOM_STEPS.find(s => s > current + 0.001)
                : [...ZOOM_STEPS].reverse().find(s => s < current - 0.001)
            if (!next) return
            setFitMode(false)
            scale.value = next
        }

        function setFitMode(on) {
            fitMode.value = on
            ext.setPrefs({ fit: on })
        }

        /** Scale that fits the page to the preview width, less the panel's 1em gutters */
        async function fitScale() {
            const size = await pdfView.pageSize()
            if (!size || !previewEl.value) return null
            // clientWidth includes the p-4 (1em) padding on both sides
            const available = previewEl.value.clientWidth - PREVIEW_GUTTER * 2
            fittedWidth = previewEl.value.clientWidth
            // floor to whole percent so rounding can never push the page past the gutters
            return Math.max(0.1, Math.floor((available / size.width) * 100) / 100)
        }

        async function fitToWidth() {
            setFitMode(true)
            const fitted = await fitScale()
            if (fitted === null) return
            if (Math.abs(fitted - scale.value) > 0.001) scale.value = fitted
            else await renderPages()
        }

        /** Keep the page fitted whenever the preview panel changes width (splitter, explorer, window) */
        function onPreviewResize() {
            if (!fitMode.value || !previewEl.value || !pages.value) return
            if (previewEl.value.clientWidth === fittedWidth) return
            clearTimeout(fitTimer)
            fitTimer = setTimeout(fitToWidth, 100)
        }

        /** saved/<template>/<template>-0001.pdf, numbered one past whatever is already in there */
        function nextSavedPdf() {
            const stem = baseName(entry.value ?? 'document.typ').replace(/\.typ$/, '')
            const dir = joinPath(SAVED_DIR, stem)
            const re = new RegExp(`^${dir}/${stem}-(\\d+)\\.pdf$`, 'i')
            const highest = filePaths.value.reduce((max, path) => {
                const n = Number(path.match(re)?.[1])
                return Number.isFinite(n) ? Math.max(max, n) : max
            }, 0)
            return { dir, name: `${stem}-${String(highest + 1).padStart(4, '0')}.pdf` }
        }

        function promptSavePdf() {
            if (!pdfBlob.value) return
            const { dir, name } = nextSavedPdf()
            prompt.value = {
                title: 'Save PDF',
                message: `Saves the rendered PDF to ${dir}/`,
                value: name,
                okText: 'Save PDF',
                async onSubmit(fileName) {
                    prompt.value = null
                    await savePdf(joinPath(dir, fileName.endsWith('.pdf') ? fileName : `${fileName}.pdf`))
                },
            }
        }

        async function savePdf(path) {
            const res = await ext.post(`/pdf?path=${encodeURIComponent(path)}`, {
                body: pdfBlob.value,
                headers: { 'Content-Type': 'application/pdf' },
            })
            if (!res.ok) {
                const api = await res.json().catch(() => null)
                return ext.setError(api?.responseStatus ?? { message: `Could not save ${baseName(path)}` })
            }
            await loadFiles()
            ext.toast(`Saved ${baseName(path)}`)
        }

        function download() {
            if (!pdfBlob.value) return
            const url = URL.createObjectURL(pdfBlob.value)
            const a = document.createElement('a')
            a.href = url
            a.download = baseName(entry.value).replace(/\.typ$/, '') + '.pdf'
            document.body.appendChild(a)
            a.click()
            a.remove()
            setTimeout(() => URL.revokeObjectURL(url), 1000)
        }

        // Layout ---------------------------------------------------------------
        function toggleExplorer() {
            ext.setPrefs({ showExplorer: !prefs.showExplorer })
            nextTick(() => cm?.refresh())
        }

        function toggleAi() {
            ext.setPrefs({ showAi: !prefs.showAi })
            nextTick(() => cm?.refresh())
        }

        function drag(onMove) {
            const move = ev => onMove(ev)
            const up = () => {
                document.removeEventListener('mousemove', move)
                document.removeEventListener('mouseup', up)
                document.body.style.userSelect = ''
                ext.savePrefs()
                cm?.refresh()
            }
            document.body.style.userSelect = 'none'
            document.addEventListener('mousemove', move)
            document.addEventListener('mouseup', up)
        }

        function startDragAi(e) {
            const startY = e.clientY
            const startHeight = prefs.aiHeight
            drag(ev => {
                prefs.aiHeight = Math.min(500, Math.max(96, startHeight - (ev.clientY - startY)))
            })
        }

        function startDragSplit(e) {
            const container = e.currentTarget.parentElement
            const rect = container.getBoundingClientRect()
            drag(ev => {
                const pct = ((ev.clientX - rect.left) / rect.width) * 100
                prefs.splitPct = Math.min(85, Math.max(15, Math.round(pct)))
            })
        }

        function onKeyDown(e) {
            if ((e.ctrlKey || e.metaKey) && e.key === 's') {
                e.preventDefault()
                save()
            } else if (e.key === 'Escape') {
                closeMenu()
            }
        }

        // Lifecycle ------------------------------------------------------------
        watch(buffers, () => scheduleRender(), { deep: true })
        watch([activeTab, dataView], () => {
            if (showForm.value) loadForm()
        })
        // back/forward (and anyone editing the URL) reopen the template named there
        watch(
            () => route.query.template,
            path => {
                if (!path || path === entry.value || !filePaths.value.includes(path)) return
                const current = entry.value
                selectTemplate(path, { replace: true, onCancel: () => syncUrl(current, true) })
            },
        )
        // a language view only holds while a data tab is open
        watch(activeTab, () => {
            if (langView.value && !activeIsData.value) ext.setPrefs({ dataView: 'code' })
        })
        // the schema may arrive after the data file did - regenerate once it's there
        watch(filePaths, async () => {
            if (!activeIsData.value) return
            await loadSchemaFor(activeTab.value)
            if (langView.value && generateTypes(langView.value.id)) showDoc(editorPath.value)
        })
        // keep generated code in step with the data it was generated from
        watch(
            () => (langView.value ? buffers[activeTab.value]?.content : null),
            content => {
                if (content == null) return
                if (generateTypes(langView.value.id)) showDoc(editorPath.value)
            },
        )
        watch(editorPath, async path => {
            if (!path || isImage(path) || showForm.value) return
            await loadBuffer(path)
            await nextTick()
            showDoc(path)
        })
        watch(
            () => (showForm.value ? buffers[activeTab.value]?.content : null),
            content => {
                // re-parse only when the change came from somewhere other than the form itself
                if (content != null && content !== formSource) loadForm()
            },
        )
        watch(scale, () => {
            ext.setPrefs({ scale: scale.value })
            renderPages()
        })
        // load newly referenced resources so their tabs show live content
        watch(resources, paths => paths.forEach(path => filePaths.value.includes(path) && loadBuffer(path)))

        onMounted(async () => {
            initEditor()
            if (cm) {
                themeObserver = new MutationObserver(() => cm.setOption('theme', cmTheme()))
                themeObserver.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] })
            }
            document.addEventListener('keydown', onKeyDown)
            document.addEventListener('click', closeMenu)
            resizeObserver = new ResizeObserver(onPreviewResize)
            resizeObserver.observe(previewEl.value)
            loadFonts() // independent of the tree, so don't hold the first render up for it
            await loadFiles()
        })

        onUnmounted(() => {
            clearTimeout(renderTimer)
            clearTimeout(fitTimer)
            inflight?.abort()
            document.removeEventListener('keydown', onKeyDown)
            document.removeEventListener('click', closeMenu)
            themeObserver?.disconnect()
            resizeObserver?.disconnect()
            pdfView.destroy()
        })

        return {
            prefs, root, files, tree, entry, tabs, activeTab, activeContent, activeIsImage, dirty, isDirty,
            pages, scale, fitMode, rendering, diagnostics, pdfBlob, prompt, menu,
            activeIsJson, activeIsData, dataView, dataViews, showForm, setDataView,
            showTypstBar, typstActions: TYPST_ACTIONS, icons: ICON, applyFormat, insertMarkdown,
            showPageSetup, documentPageSettings, handleApplyPageStyle,
            showImagePicker, folderImages, entryStem, insertImage, uploadImage,
            fonts, documentFont, applyFont, documentTextSettings, applyTextStyle, handleApplyTextStyle, showFontPicker, openFontPicker,
            typeLanguages, selectLanguage, langFile, copyEditor, copied, editorContent,
            btnGroup: BTN_GROUP, btnOn: BTN_ON, btnOff: BTN_OFF,
            formSchema, formData, formError, schemaBusy, generateSchema, onFormChange, schemaOf,
            aiPrompt, aiInput, aiBusy, aiError, aiResult, aiUndo, aiModel, sendAiEdit, undoAiEdit,
            aiImages, aiAttaching, aiDragging, onAiFiles, onAiDrop, onAiPaste, removeAiImage, maxPdfPages: MAX_PDF_PAGES,
            errorDiagnostics, fixWithAi,
            aiHistory, historyIndex, cycleHistory,
            editorEl, previewEl, canvasEls, hasCodeMirror,
            loadFiles, onNodeSelect, openTab, selectTab, closeTab, save, download, promptSavePdf, zoom, fitToWidth, goToDiagnostic,
            promptNewTemplate, promptNewFile, promptNewFolder, openMenu, onMenuPick,
            toggleExplorer, toggleAi, startDragSplit, startDragAi, onTextareaInput, rawUrl, baseName,
            previewActions, previewContext,
        }
    },
}

export default {
    order: 30 - 100,

    install(ctx) {
        ext = ctx.scope('pdf')
        tools = ctx.scope('core_tools') // JSON -> UI schema endpoint

        // the bundled highlight.js has no typst grammar, so ```typst blocks in chat render unhighlighted
        registerTypst(hljs)

        ext.setPrefs({
            showExplorer: ext.prefs.showExplorer ?? true,
            splitPct: ext.prefs.splitPct ?? 45,
            showAi: ext.prefs.showAi ?? true,
            aiHeight: ext.prefs.aiHeight ?? 180,
            scale: ext.prefs.scale ?? 1,
            fit: ext.prefs.fit ?? true,
            lastPage: ext.prefs.lastPage ?? 'typ',
        })

        ctx.components({
            JsonSchemaForm,
            PdfFileNode,
            PdfContextMenu,
            PdfPrompt,
            PdfFontPicker,
            PdfPageSetup,
            PdfImagePicker,
            PdfDesigner,
        })

        ctx.setLeftIcons({
            pdf: {
                title: 'PDF Studio',
                component: {
                    template: `
                    <svg @click="$ctx.togglePath('/pdf', { left:false })" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1536 1792"><path d="M0 0h1536v1792H0z" fill="none" /><path fill="currentColor" d="M1468 380q28 28 48 76t20 88v1152q0 40-28 68t-68 28H96q-40 0-68-28t-28-68V96q0-40 28-68T96 0h896q40 0 88 20t76 48zm-444-244v376h376q-10-29-22-41l-313-313q-12-12-41-22m384 1528V640H992q-40 0-68-28t-28-68V128H128v1536zm-514-593q33 26 84 56q59-7 117-7q147 0 177 49q16 22 2 52q0 1-1 2l-2 2v1q-6 38-71 38q-48 0-115-20t-130-53q-221 24-392 83q-153 262-242 262q-15 0-28-7l-24-12q-1-1-6-5q-10-10-6-36q9-40 56-91.5t132-96.5q14-9 23 6q2 2 2 4q52-85 107-197q68-136 104-262q-24-82-30.5-159.5T657 552q11-40 42-40h22q23 0 35 15q18 21 9 68q-2 6-4 8q1 3 1 8v30q-2 123-14 192q55 164 146 238m-576 411q52-24 137-158q-51 40-87.5 84t-49.5 74m398-920q-15 42-2 132q1-7 7-44q0-3 7-43q1-4 4-8q-1-1-1-2q-1-2-1-3q-1-22-13-36q0 1-1 2zm-124 661q135-54 284-81q-2-1-13-9.5t-16-13.5q-76-67-127-176q-27 86-83 197q-30 56-45 83m646-16q-24-24-140-24q76 28 124 28q14 0 18-1q0-1-2-3" /></svg>
                    `,
                },
                isActive({ path }) {
                    return ctx.matchesPath(path, '/pdf')
                },
            },
        })

        ctx.routes.push({ path: '/pdf', component: PdfDesigner, meta: { title: 'PDF Studio' } })
    },
}
