// CONFIG is embedded by the server with this widget source. There is deliberately no second
// config request: one script tag is the complete published Search deployment.
if (!document.querySelector(`[data-gemini-search="${CONFIG.searchId}"]`)) {
    const host = document.createElement('div')
    host.dataset.geminiSearch = CONFIG.searchId
    const shadow = host.attachShadow({ mode:'open' })
    const appearance = CONFIG.appearance || {}
    const behavior = CONFIG.behavior || {}
    const markdownParser = typeof MARKDOWN !== 'undefined' && typeof MARKDOWN?.parse === 'function' ? MARKDOWN : null
    const platform = navigator.userAgentData?.platform || navigator.platform || ''
    const isMac = /Mac|iPhone|iPad|iPod/i.test(platform)
    const theme = appearance.theme === 'auto'
        ? (matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light')
        : appearance.theme
    const dark = ['dark','nord','matrix'].includes(theme)
    const palette = theme === 'nord'
        ? { bg:'#2e3440', surface:'#3b4252', text:'#eceff4', muted:'#d8dee9', border:'#4c566a' }
        : theme === 'matrix'
        ? { bg:'#000', surface:'#020a04', text:'#4ade80', muted:'#15803d', border:'#166534' }
        : theme === 'soft-pink'
        ? { bg:'#fff', surface:'#fdf2f8', text:'#831843', muted:'#9d174d', border:'#fbcfe8' }
        : dark
        ? { bg:'#111827', surface:'#1f2937', text:'#f3f4f6', muted:'#9ca3af', border:'#374151' }
        : { bg:'#fff', surface:'#f8fafc', text:'#1f2937', muted:'#64748b', border:'#d1d5db' }
    const highlightColor = /^#[0-9a-f]{6}$/i.test(appearance.highlightColor || '')
        ? appearance.highlightColor : dark ? '#ffffff' : '#0ea5e9'
    shadow.innerHTML = `<style>
      :host{all:initial;color-scheme:${dark ? 'dark' : 'light'}}
      *,*:before,*:after{box-sizing:border-box}
      button,input{font:inherit}
      .launcher{display:inline-flex;align-items:center;gap:4px;padding:4px 8px;border:0;border-radius:9999px;background:${dark ? 'rgba(255,255,255,.05)' : 'rgba(3,7,18,.04)'};color:${palette.muted};box-shadow:inset 0 0 0 1px ${dark ? 'rgba(255,255,255,.08)' : 'rgba(3,7,18,.10)'};cursor:pointer}
      .launcher.slash-only{padding-right:12px}.launcher:hover{background:${dark ? 'rgba(255,255,255,.09)' : 'rgba(3,7,18,.07)'}}.launcher:focus-visible{outline:2px solid ${palette.muted};outline-offset:2px}.launcher-icon{width:16px;height:16px;flex:none;fill:currentColor}.icon{width:20px;height:20px;flex:none}.key{font:12px/16px ui-sans-serif,system-ui,sans-serif;color:${palette.muted};white-space:nowrap}
      .backdrop{position:fixed;inset:0;z-index:2147483646;background:#0008;display:none;align-items:flex-start;justify-content:center;padding:8vh 16px}.backdrop.open{display:flex}
      .dialog{width:${appearance.dialogWidth || 760}px;max-width:100%;max-height:78vh;display:flex;flex-direction:column;border:1px solid ${palette.border};border-radius:16px;background:${palette.bg};color:${palette.text};box-shadow:0 24px 80px #0007;overflow:hidden}
      .searchbar{display:flex;align-items:center;gap:12px;padding:15px 18px;border-bottom:1px solid ${palette.border}}
      .searchbar input,.searchbar input:focus{min-width:0;flex:1;border:0;outline:0;box-shadow:none;background:transparent;color:${palette.text};font-size:20px}.esc{border:1px solid ${palette.border};border-radius:7px;background:transparent;color:${palette.muted};font:12px/16px ui-sans-serif,system-ui,sans-serif;padding:3px 7px;cursor:pointer}.esc:hover{background:${palette.surface};color:${palette.text}}.close{border:0;background:transparent;color:${palette.muted};font-size:25px;cursor:pointer;padding:2px 6px}.close:hover{color:${palette.text}}
      .results{overflow:auto;padding:10px 12px 14px}.group-title{margin:9px 4px 5px;font-size:17px;font-weight:500;color:${palette.muted}}
      .result-row{display:flex;align-items:center;gap:4px}.result{display:flex;gap:11px;min-width:0;flex:1;border:0;border-radius:10px;background:${palette.surface};color:${palette.text};padding:10px 12px;margin:5px 0;text-align:left;cursor:pointer}.result:hover,.result.selected{outline:2px solid ${palette.muted};outline-offset:-2px}.result-icon{width:22px;display:grid;place-items:center;flex:none;color:${palette.muted};font-size:20px;line-height:22px}.result-icon svg{display:block}.copy{min-width:0}.snippet,.title{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.snippet{font-size:14px}.title{margin-top:3px;font-size:13px;color:${palette.muted}}.remove-recent{flex:none;border:0;border-radius:7px;background:transparent;color:${palette.muted};font-size:22px;line-height:1;padding:7px;cursor:pointer}.remove-recent:hover{background:${palette.surface};color:${palette.text}}mark{background:transparent;color:${highlightColor};font-weight:700;text-decoration:${dark ? 'none' : 'underline'};text-decoration-color:${highlightColor};text-decoration-thickness:2px;text-underline-offset:2px;padding:0}.empty{padding:36px 16px;text-align:center;color:${palette.muted};font-size:14px}.loading{padding:18px;text-align:center;color:${palette.muted}}
      .document-backdrop{position:fixed;inset:0;z-index:2147483647;background:#0009;display:none;align-items:flex-start;justify-content:center;padding:5vh 16px}.document-backdrop.open{display:flex}.document-dialog{width:min(920px,100%);height:88vh;display:flex;flex-direction:column;border:1px solid ${palette.border};border-radius:16px;background:${palette.bg};color:${palette.text};box-shadow:0 24px 80px #0009;overflow:hidden}.document-header{display:flex;align-items:center;gap:12px;padding:13px 18px;border-bottom:1px solid ${palette.border}}.document-back{display:grid;flex:none;place-items:center;width:34px;height:34px;border:0;border-radius:8px;background:transparent;color:${palette.muted};cursor:pointer}.document-back:hover{background:${palette.surface};color:${palette.text}}.document-back svg{width:22px;height:22px}.document-title{min-width:0;flex:1;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;font-size:17px;font-weight:600}.document-body{overflow:auto;outline:0;padding:24px 30px;font:15px/1.65 ui-sans-serif,system-ui,sans-serif}.document-body.plaintext{white-space:pre-wrap}.document-body h1,.document-body h2,.document-body h3,.document-body h4{margin:1.35em 0 .55em;font-weight:650;line-height:1.25}.document-body h1{font-size:2em}.document-body h2{font-size:1.55em;border-bottom:1px solid ${palette.border};padding-bottom:.25em}.document-body h3{font-size:1.25em}.document-body p,.document-body ul,.document-body ol,.document-body pre,.document-body blockquote{margin:.8em 0}.document-body pre{overflow:auto;border-radius:8px;background:${palette.surface};padding:14px}.document-body code{border-radius:4px;background:${palette.surface};padding:.15em .3em}.document-body pre code{padding:0}.document-body a{color:inherit;text-decoration:underline}.document-body img{max-width:100%}.document-body blockquote{margin-left:0;border-left:3px solid ${palette.border};padding-left:14px;color:${palette.muted}}.document-body table{border-collapse:collapse}.document-body th,.document-body td{border:1px solid ${palette.border};padding:6px 9px}
      @media(max-width:640px){.backdrop,.document-backdrop{padding:0}.dialog,.document-dialog{width:100%;height:100%;max-height:none;border-radius:0}.document-body{padding:18px}}
    </style>
    <button class="launcher" type="button" aria-label="Search">
      <svg class="launcher-icon" viewBox="0 0 16 16" aria-hidden="true"><path fill-rule="evenodd" d="M9.965 11.026a5 5 0 1 1 1.06-1.06l2.755 2.754a.75.75 0 1 1-1.06 1.06l-2.755-2.754ZM10.5 7a3.5 3.5 0 1 1-7 0 3.5 3.5 0 0 1 7 0Z" clip-rule="evenodd"></path></svg>
      <kbd class="key"></kbd>
    </button>
    <div class="backdrop" role="presentation"><section class="dialog" role="dialog" aria-modal="true" aria-label="Search">
      <div class="searchbar"><svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="m20 20-4-4"/></svg><input type="text" autocomplete="off" spellcheck="false"><button class="esc" type="button" aria-label="Close search">esc</button></div>
      <div class="results"></div>
    </section></div>
    <div class="document-backdrop" role="presentation"><section class="document-dialog" role="dialog" aria-modal="true" aria-label="Document preview">
      <div class="document-header"><button class="document-back" type="button" aria-label="Back to search results"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M19 12H5M12 19l-7-7 7-7"/></svg></button><div class="document-title"></div><button class="document-close close" type="button" aria-label="Close document">×</button></div>
      <article class="document-body" tabindex="-1"></article>
    </section></div>`
    const launcher = shadow.querySelector('.launcher')
    launcher.setAttribute('aria-label', CONFIG.placeholder || 'Search docs')
    const commandKEnabled = behavior.commandKShortcut !== false
    const slashOnly = !commandKEnabled && behavior.slashShortcut !== false
    const shortcutLabel = commandKEnabled ? (isMac ? '⌘K' : 'Ctrl K') : slashOnly ? '/' : ''
    launcher.classList.toggle('slash-only', slashOnly)
    if (shortcutLabel) shadow.querySelector('.key').textContent = shortcutLabel
    else shadow.querySelector('.key').remove()
    const backdrop = shadow.querySelector('.backdrop')
    const dialog = shadow.querySelector('.dialog')
    const input = shadow.querySelector('input')
    const results = shadow.querySelector('.results')
    const documentBackdrop = shadow.querySelector('.document-backdrop')
    const documentDialog = shadow.querySelector('.document-dialog')
    const documentTitle = shadow.querySelector('.document-title')
    const documentBody = shadow.querySelector('.document-body')
    input.placeholder = CONFIG.placeholder || 'Search docs'
    dialog.setAttribute('aria-label', CONFIG.title || 'Search documentation')
    let timer = 0, requestNo = 0, items = [], selected = -1
    const recentKey = `gemini-search:${CONFIG.searchId}:recent`
    const recentLimit = 8

    const open = () => { backdrop.classList.add('open'); if (!input.value.trim()) renderRecent(); setTimeout(() => input.focus(), 0) }
    const closeDocument = () => { documentBackdrop.classList.remove('open'); input.focus() }
    const close = () => { documentBackdrop.classList.remove('open'); backdrop.classList.remove('open'); launcher.focus() }
    launcher.addEventListener('click', open)
    shadow.querySelector('.esc').addEventListener('click', close)
    shadow.querySelector('.document-back').addEventListener('click', closeDocument)
    shadow.querySelector('.document-close').addEventListener('click', closeDocument)
    backdrop.addEventListener('click', e => { if (e.target === backdrop) close() })
    documentBackdrop.addEventListener('click', e => { if (e.target === documentBackdrop) closeDocument() })
    documentDialog.addEventListener('click', event => event.stopPropagation())

    const appendParts = (el, parts) => (parts || []).forEach(part => {
        const node = document.createElement(part.match ? 'mark' : 'span')
        node.textContent = part.text
        el.appendChild(node)
    })
    const navigate = value => {
        if (!value) return
        try {
            const url = new URL(value, location.href)
            if (!url.pathname.includes('/~cache/') && (url.protocol === 'http:' || url.protocol === 'https:')) location.href = url.href
        } catch (_) { /* An invalid imported Source URL is not a navigation target. */ }
    }
    const markdownTags = new Set(['p','br','strong','em','code','pre','ul','ol','li','blockquote',
        'h1','h2','h3','h4','h5','h6','hr','a','table','thead','tbody','tr','th','td','del','input'])
    const safeMarkdownUrl = value => {
        const url = String(value || '').trim()
        return !url || /^(?:javascript|data|vbscript):/i.test(url) ? '' : url
    }
    function sanitizedMarkdown(html) {
        const template = document.createElement('template'); template.innerHTML = html
        const headingAnchors = {}
        for (const element of [...template.content.querySelectorAll('*')]) {
            const tag = element.tagName.toLowerCase()
            if (!markdownTags.has(tag)) { element.replaceWith(document.createTextNode(element.textContent || '')); continue }
            const href = tag === 'a' ? safeMarkdownUrl(element.getAttribute('href')) : ''
            const title = tag === 'a' ? element.getAttribute('title') : ''
            const checked = tag === 'input' && element.hasAttribute('checked')
            for (const attribute of [...element.attributes]) element.removeAttribute(attribute.name)
            if (tag === 'a' && href) {
                element.setAttribute('href', href); if (title) element.setAttribute('title', title)
                if (!href.startsWith('#')) { element.setAttribute('target', '_blank'); element.setAttribute('rel', 'noopener noreferrer') }
            } else if (tag === 'input') {
                element.setAttribute('type', 'checkbox'); element.disabled = true; element.checked = checked
            } else if (/^h[1-6]$/.test(tag)) {
                const base = (element.textContent || '').normalize('NFD').replace(/[\u0300-\u036f]/g, '')
                    .toLowerCase().replace(/[^a-z0-9\s-]/g, '').replace(/[-\s]+/g, '-').replace(/^-|-$/g, '') || 'section'
                const number = headingAnchors[base] || 0; headingAnchors[base] = number + 1
                element.id = number ? `${base}-${number}` : base
            }
        }
        return template.content
    }
    function renderMarkdown(text) {
        const source = String(text || ''); documentBody.replaceChildren(); documentBody.classList.remove('plaintext')
        if (!markdownParser) { documentBody.classList.add('plaintext'); documentBody.textContent = source; return }
        try { documentBody.append(sanitizedMarkdown(markdownParser.parse(source, { gfm:true }))) }
        catch (_) { documentBody.classList.add('plaintext'); documentBody.textContent = source }
    }
    async function previewDocument(item) {
        if (!item?.previewUrl) return
        documentTitle.textContent = item.documentTitle || item.title || 'Document'
        documentBody.classList.add('plaintext'); documentBody.textContent = 'Loading…'
        documentBackdrop.classList.add('open')
        documentBody.focus({ preventScroll:true })
        try {
            const response = await fetch(item.previewUrl, { headers:{ Accept:'application/json' } })
            if (!response.ok) throw new Error(`Document preview failed (${response.status})`)
            const data = await response.json(); documentTitle.textContent = data.title || documentTitle.textContent
            renderMarkdown(data.markdown)
            if (item.anchor) setTimeout(() => shadow.getElementById(item.anchor)?.scrollIntoView({ block:'start' }), 0)
        } catch (_) { documentBody.classList.add('plaintext'); documentBody.textContent = 'This document preview is temporarily unavailable.' }
    }
    const recentId = item => item?.url || item?.previewUrl || ''
    const readRecent = () => {
        try {
            const value = JSON.parse(localStorage.getItem(recentKey) || '[]')
            return Array.isArray(value) ? value.filter(x => x && recentId(x)
                && !String(x.url || '').includes('/~cache/')).slice(0, recentLimit) : []
        } catch (_) { return [] }
    }
    const writeRecent = value => {
        try { localStorage.setItem(recentKey, JSON.stringify(value.slice(0, recentLimit))) }
        catch (_) { /* Private browsing or an embedding policy may disable localStorage. */ }
    }
    const remember = item => {
        if (!recentId(item)) return
        const recent = {
            url:item.url || null,
            previewUrl:item.previewUrl || null,
            anchor:item.anchor || null,
            documentTitle:item.documentTitle || item.title || 'Document',
            title:item.title || item.documentTitle || 'Document',
            snippet:item.snippet || '',
            type:item.type || 'content',
            clickedAt:Date.now(),
        }
        writeRecent([recent, ...readRecent().filter(x => recentId(x) !== recentId(recent))])
    }
    const activate = item => { remember(item); item?.url ? navigate(item.url) : previewDocument(item) }
    function choose(index) {
        selected = index
        shadow.querySelectorAll('.result').forEach((el, i) => el.classList.toggle('selected', i === selected))
        shadow.querySelectorAll('.result')[selected]?.scrollIntoView({ block:'nearest' })
    }
    function addResult(item, documentTitle, recent = false) {
        const entry = { ...item, documentTitle:documentTitle || item.documentTitle || item.title || 'Document' }
        const index = items.push(entry) - 1
        const row = document.createElement('div'); row.className = 'result-row'
        const button = document.createElement('button'); button.type = 'button'; button.className = 'result'
        const icon = document.createElement('span'); icon.className = 'result-icon'; icon.innerHTML = entry.type === 'doc'
          ? '<svg width="20" height="20" viewBox="0 0 20 20"><path d="M17 6v12c0 .52-.2 1-1 1H4c-.7 0-1-.33-1-1V2c0-.55.42-1 1-1h8l5 5zM14 8h-3.13c-.51 0-.87-.34-.87-.87V4" stroke="currentColor" fill="none" fill-rule="evenodd" stroke-linejoin="round"></path></svg>'
          : entry.type === 'heading'
            ? '<svg width="20" height="20" viewBox="0 0 20 20"><path d="M13 13h4-4V8H7v5h6v4-4H7V8H3h4V3v5h6V3v5h4-4v5zm-6 0v4-4H3h4z" stroke="currentColor" fill="none" fill-rule="evenodd" stroke-linecap="round" stroke-linejoin="round"></path></svg>'
            : '<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 512 512"><path d="M0 0h512v512H0z" fill="none"></path><path fill="currentColor" d="M80 96h352v32H80zm0 144h352v32H80zm0 144h352v32H80z"></path></svg>'
        const copy = document.createElement('span'); copy.className = 'copy'
        const snippet = document.createElement('div'); snippet.className = 'snippet'
        const title = document.createElement('div'); title.className = 'title'
        if (recent) {
            snippet.textContent = entry.documentTitle
            title.textContent = entry.title !== entry.documentTitle ? entry.title : entry.snippet
        } else {
            appendParts(snippet, entry.snippetParts)
            appendParts(title, entry.titleParts)
        }
        copy.append(snippet, title); button.append(icon, copy); row.appendChild(button)
        button.addEventListener('mouseenter', () => choose(index))
        button.addEventListener('click', () => activate(entry))
        if (recent) {
            const remove = document.createElement('button'); remove.type = 'button'; remove.className = 'remove-recent'
            remove.setAttribute('aria-label', `Remove ${entry.documentTitle} from recent searches`); remove.textContent = '×'
            remove.addEventListener('click', () => { writeRecent(readRecent().filter(x => recentId(x) !== recentId(entry))); renderRecent() })
            row.appendChild(remove)
        }
        results.appendChild(row)
    }
    function renderRecent() {
        results.replaceChildren(); items = []; selected = -1
        const recent = readRecent()
        if (!recent.length) {
            const empty = document.createElement('div'); empty.className = 'empty'; empty.textContent = CONFIG.title || 'Search documentation'
            results.appendChild(empty); return
        }
        const heading = document.createElement('h3'); heading.className = 'group-title'; heading.textContent = 'Recent'
        results.appendChild(heading)
        recent.forEach(item => addResult(item, item.documentTitle, true))
    }
    function render(data) {
        results.replaceChildren(); items = []; selected = -1
        const groups = data?.groups || []
        if (!groups.length) {
            const empty = document.createElement('div'); empty.className = 'empty'
            empty.textContent = input.value.trim() ? (CONFIG.emptyText || 'No matching documents found.') : (CONFIG.title || 'Search documentation')
            results.appendChild(empty); return
        }
        groups.forEach(group => {
            const heading = document.createElement('h3'); heading.className = 'group-title'; heading.textContent = group.title
            results.appendChild(heading)
            ;(group.items || []).forEach(item => {
                addResult(item, group.title)
            })
        })
        if (items.length) choose(0)
    }
    async function search() {
        const q = input.value.trim(), current = ++requestNo
        if (!q) return renderRecent()
        if (q.length < (behavior.minChars || 2)) return render({ groups:[] })
        results.innerHTML = '<div class="loading">Searching…</div>'
        try {
            const response = await fetch(CONFIG.searchUrl + '?q=' + encodeURIComponent(q), { headers:{ Accept:'application/json' } })
            if (!response.ok) throw new Error(`Search failed (${response.status})`)
            const data = await response.json()
            if (current === requestNo) render(data)
        } catch (error) {
            if (current === requestNo) { results.innerHTML = ''; const el=document.createElement('div'); el.className='empty'; el.textContent='Search is temporarily unavailable.'; results.appendChild(el) }
        }
    }
    input.addEventListener('input', () => { clearTimeout(timer); timer = setTimeout(search, 180) })
    input.addEventListener('keydown', event => {
        if (event.key === 'ArrowDown' && items.length) { choose((selected + 1) % items.length); event.preventDefault() }
        else if (event.key === 'ArrowUp' && items.length) { choose((selected - 1 + items.length) % items.length); event.preventDefault() }
        else if (event.key === 'Enter' && recentId(items[selected])) { activate(items[selected]); event.preventDefault() }
        else if (event.key === 'Escape') {
            documentBackdrop.classList.contains('open') ? closeDocument() : close()
            event.preventDefault(); event.stopPropagation()
        }
    })
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape' && documentBackdrop.classList.contains('open')) { closeDocument(); event.preventDefault(); event.stopPropagation(); return }
        if (event.key === 'Escape' && backdrop.classList.contains('open')) { close(); event.preventDefault(); event.stopPropagation(); return }
        const commandK = behavior.commandKShortcut !== false && event.key.toLowerCase() === 'k'
            && (event.ctrlKey || event.metaKey) && !event.altKey && !event.shiftKey
        const slash = behavior.slashShortcut !== false && event.key === '/'
            && !event.metaKey && !event.ctrlKey && !event.altKey && !event.shiftKey
            && !/^(INPUT|TEXTAREA|SELECT)$/.test(event.target?.tagName || '') && !event.target?.isContentEditable
        if (commandK || slash) { open(); event.preventDefault() }
    })
    dialog.addEventListener('click', event => event.stopPropagation())
    renderRecent()
    document.body.appendChild(host)
}
