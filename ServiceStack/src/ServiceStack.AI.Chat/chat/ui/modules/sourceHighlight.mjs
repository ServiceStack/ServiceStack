import hljs from '../lib/highlight.min.mjs'

const languages = {
    js: 'javascript', mjs: 'javascript', cjs: 'javascript', jsx: 'javascript',
    ts: 'typescript', mts: 'typescript', cts: 'typescript', tsx: 'typescript',
    py: 'python', pyw: 'python', pyi: 'python', cs: 'csharp', csx: 'csharp',
    c: 'c', h: 'c', cc: 'cpp', cpp: 'cpp', cxx: 'cpp', hpp: 'cpp', hh: 'cpp',
    rs: 'rust', go: 'go', java: 'java', kt: 'kotlin', kts: 'kotlin', swift: 'swift',
    rb: 'ruby', php: 'php', pl: 'perl', pm: 'perl', lua: 'lua', r: 'r', vb: 'vbnet',
    json: 'json', jsonc: 'json', jsonl: 'json', yaml: 'yaml', yml: 'yaml',
    toml: 'ini', ini: 'ini', cfg: 'ini', conf: 'ini', env: 'ini', properties: 'ini',
    html: 'xml', htm: 'xml', xml: 'xml', svg: 'xml', vue: 'xml', xaml: 'xml',
    csproj: 'xml', fsproj: 'xml', props: 'xml', targets: 'xml',
    css: 'css', scss: 'scss', less: 'less', sql: 'sql',
    sh: 'bash', bash: 'bash', zsh: 'bash', md: 'markdown', markdown: 'markdown',
    graphql: 'graphql', gql: 'graphql', wat: 'wasm',
}
const filenames = { makefile: 'makefile', gnumakefile: 'makefile', '.bashrc': 'bash', '.zshrc': 'bash', '.profile': 'bash' }
const escapeHtml = text => text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')

// Use the filename rather than guessing a language for prose or unfamiliar file types.
// Return escaped text for large/minified files so highlighting can't stall their preview.
export function highlightSource(content, path) {
    const text = String(content ?? '')
    const name = String(path || '').split(/[\\/]/).pop().toLowerCase()
    const extension = name.split('.').pop()
    const language = Object.hasOwn(filenames, name) ? filenames[name]
        : Object.hasOwn(languages, extension) ? languages[extension] : null
    if (!language || !hljs.getLanguage(language) || text.length > 200_000
        || text.split('\n').some(line => line.length > 8_000)) return escapeHtml(text)
    try {
        return hljs.highlight(text, { language, ignoreIllegals: true }).value
    } catch {
        return escapeHtml(text)
    }
}
