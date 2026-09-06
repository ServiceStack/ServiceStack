// CONFIG is embedded by the server with this widget source. There is deliberately no second
// config request: one script tag is the complete published Search deployment.
if (!document.querySelector(`[data-gemini-search="${CONFIG.searchId}"]`)) {
    const script = typeof SCRIPT !== 'undefined' ? SCRIPT : document.currentScript
    const overrides = script ? script.dataset : {}
    const host = document.createElement('div')
    host.dataset.geminiSearch = CONFIG.searchId
    const shadow = host.attachShadow({ mode: 'open' })
    const appearance = CONFIG.appearance || {}
    const mountSelector = String(('mount' in overrides ? overrides.mount : appearance.mount) || '').trim()
    const resolveMount = () => {
        if (!mountSelector || /^(?:none|off|false)$/i.test(mountSelector)) return null
        let element
        try { element = document.querySelector(mountSelector) }
        catch { console.warn(`Gemini Search: ignoring invalid mount selector ${JSON.stringify(mountSelector)}.`); return null }
        if (!element) console.warn(`Gemini Search: mount element ${JSON.stringify(mountSelector)} was not found; using the floating launcher.`)
        return element
    }
    const mountElement = resolveMount()
    // The dialogs always overlay from document.body so an inline launcher cannot trap them inside a
    // transformed ancestor, and so opening Search never reflows the host page.
    const launcherHost = mountElement ? document.createElement('div') : null
    if (launcherHost) {
        launcherHost.style.cssText = 'all:initial;display:inline-flex;vertical-align:middle'
        launcherHost.dataset.geminiSearchLauncher = CONFIG.searchId
        launcherHost.dataset.inline = ''
    }
    const hosts = launcherHost ? [host, launcherHost] : [host]
    const behavior = CONFIG.behavior || {}
    const analytics = CONFIG.analytics || {}
    const analyticsEnabled = CONFIG.analyticsEnabled === true && !!CONFIG.analyticsUrl
    const markdownParser = typeof MARKDOWN !== 'undefined' && typeof MARKDOWN?.parse === 'function' ? MARKDOWN : null
    const platform = navigator.userAgentData?.platform || navigator.platform || ''
    const isMac = /Mac|iPhone|iPad|iPod/i.test(platform)
    const colorSchemeMedia = matchMedia('(prefers-color-scheme: dark)')
    const palettes = {
        light: { bg: '#fff', surface: '#f8fafc', surfaceHover: '#f1f5f9', text: '#1f2937', muted: '#64748b', border: '#d1d5db' },
        dark: { bg: '#111827', surface: '#1f2937', surfaceHover: '#273449', text: '#f3f4f6', muted: '#9ca3af', border: '#374151' },
        nord: { bg: '#2e3440', surface: '#3b4252', surfaceHover: '#434c5e', text: '#eceff4', muted: '#d8dee9', border: '#4c566a' },
        matrix: { bg: '#000', surface: '#020a04', surfaceHover: '#06160b', text: '#4ade80', muted: '#15803d', border: '#166534' },
        'soft-pink': { bg: '#fff', surface: '#fdf2f8', surfaceHover: '#fce7f3', text: '#831843', muted: '#9d174d', border: '#fbcfe8' },
    }
    const savedColorScheme = () => {
        try { const value = localStorage.getItem('color-scheme'); return value === 'dark' || value === 'light' ? value : null }
        catch (_) { return null }
    }
    const resolveTheme = () => appearance.theme === 'auto'
        ? savedColorScheme() || (colorSchemeMedia.matches ? 'dark' : 'light')
        : appearance.theme
    const cleanFont = String(appearance.fontFamily || '').replace(/[\x00-\x1f{};]/g, '').trim()
    const fontFamily = cleanFont || "Inter, 'Inter Fallback', system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', 'Noto Sans', Arial, sans-serif, 'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol', 'Noto Color Emoji'"
    const launcherStyle = ['raised', 'flat', 'inset'].includes(appearance.launcherStyle)
        ? appearance.launcherStyle : 'flat'
    const requested = overrides.position || appearance.position
    const position = ['top-left', 'top-right', 'bottom-left', 'bottom-right'].includes(requested)
        ? requested : 'bottom-right'
    const boundedOffset = side => `${Math.min(Math.max(Number(appearance.offset?.[side]) || 0, 0), 400)}px`
    for (const element of hosts) {
        element.dataset.position = position
        element.style.setProperty('--font-family', fontFamily)
        for (const side of ['top', 'right', 'bottom', 'left']) element.style.setProperty(`--offset-${side}`, boundedOffset(side))
        element.style.setProperty('--assistant-offset', '0px')
    }
    const applyTheme = () => {
        const theme = resolveTheme()
        const dark = ['dark', 'nord', 'matrix'].includes(theme)
        const palette = palettes[theme] || palettes.light
        const highlight = /^#[0-9a-f]{6}$/i.test(appearance.highlightColor || '')
            ? appearance.highlightColor : dark ? '#ffffff' : '#0ea5e9'
        const ring = 'inset 0 0 0 1px var(--launcher-ring)'
        const flat = launcherStyle === 'flat'
        const launcherDepth = launcherStyle === 'raised'
            ? [`0 8px 24px rgba(15,23,42,.14),${ring}`, `0 10px 28px rgba(15,23,42,.2),${ring}`, '-1px']
            : launcherStyle === 'inset'
                ? dark ? [`inset 0 2px 5px rgba(0,0,0,.5),${ring}`, `inset 0 3px 7px rgba(0,0,0,.6),${ring}`, '0px']
                    : [`inset 0 2px 4px rgba(15,23,42,.16),${ring}`, `inset 0 3px 6px rgba(15,23,42,.22),${ring}`, '0px']
                : [ring, ring, '0px']
        const vars = {
            '--color-scheme': dark ? 'dark' : 'light',
            '--bg': palette.bg, '--surface': palette.surface, '--surface-hover': palette.surfaceHover,
            '--text': palette.text, '--muted': palette.muted, '--border': palette.border,
            '--highlight': highlight, '--match-decoration': dark ? 'none' : 'underline',
            '--launcher-bg': dark
                ? (flat ? 'rgba(255,255,255,.04)' : 'rgba(255,255,255,.06)')
                : (flat ? 'rgba(3,7,18,.02)' : 'rgba(3,7,18,.04)'),
            '--launcher-hover': dark ? 'rgba(255,255,255,.11)' : 'rgba(3,7,18,.08)',
            '--launcher-ring': dark
                ? (flat ? 'rgba(255,255,255,.14)' : 'rgba(255,255,255,.10)')
                : (flat ? 'rgba(3,7,18,.08)' : 'rgba(3,7,18,.12)'),
            '--launcher-icon': 'color-mix(in srgb, var(--text) 45%, var(--muted))',
            '--launcher-shadow': launcherDepth[0], '--launcher-shadow-hover': launcherDepth[1],
            '--launcher-lift': launcherDepth[2],
            '--panel-shadow': dark ? '0 28px 90px rgba(0,0,0,.62)' : '0 28px 90px rgba(15,23,42,.28)',
        }
        for (const element of hosts) {
            element.dataset.theme = theme
            for (const [name, value] of Object.entries(vars)) element.style.setProperty(name, value)
        }
    }
    applyTheme()
    const styles = `
      :host{all:initial;color-scheme:var(--color-scheme);font-family:var(--font-family)}
      *,*:before,*:after{box-sizing:border-box}
      button,input{font:inherit}
      .launcher-wrap{position:fixed;z-index:2147483000;display:inline-flex}
      .launcher{position:relative;display:inline-flex;align-items:center;gap:3px;padding:5px 8px 5px 5px;border:0;border-radius:9999px;background:var(--launcher-bg);color:var(--muted);box-shadow:var(--launcher-shadow);cursor:pointer;transition:background-color .15s ease,color .15s ease,box-shadow .15s ease,transform .15s ease}
      :host([data-position="top-left"]) .launcher-wrap{top:var(--offset-top);left:var(--offset-left)}:host([data-position="top-right"]) .launcher-wrap{top:var(--offset-top);right:var(--offset-right)}:host([data-position="bottom-left"]) .launcher-wrap{bottom:var(--offset-bottom);left:var(--offset-left)}:host([data-position="bottom-right"]) .launcher-wrap{right:var(--offset-right);bottom:calc(var(--offset-bottom) + var(--assistant-offset))}
      :host([data-inline]) .launcher-wrap{position:relative;inset:auto}
      .launcher.slash-only{padding-right:14px}.launcher:hover{background:var(--launcher-hover);color:var(--text);box-shadow:var(--launcher-shadow-hover);transform:translateY(var(--launcher-lift))}.launcher:focus-visible{outline:2px solid var(--muted);outline-offset:2px}.launcher-icon{width:16px;height:16px;flex:none;fill:var(--launcher-icon,currentColor)}.launcher:hover .launcher-icon{fill:currentColor}.icon{width:20px;height:20px;flex:none}.key{font:13px/16px var(--font-family);color:inherit;white-space:nowrap}
      .tooltip{position:absolute;z-index:1;width:max-content;max-width:220px;padding:5px 9px;border-radius:8px;border:1px solid var(--border);background:var(--bg);color:var(--text);font:12px/1.45 var(--font-family);box-shadow:0 6px 20px rgba(15,23,42,.16);opacity:0;visibility:hidden;transition:opacity .14s ease,visibility 0s linear .14s}
      .tooltip:after{content:"";position:absolute;width:7px;height:7px;background:var(--bg);border:1px solid var(--border);transform:rotate(45deg)}
      .launcher-wrap:hover .tooltip,.launcher:focus-visible+.tooltip{opacity:1;visibility:visible;transition-delay:.25s,0s}
      :host([data-position="bottom-left"]) .tooltip,:host([data-position="bottom-right"]) .tooltip{bottom:calc(100% + 9px);top:auto}
      :host([data-position="bottom-left"]) .tooltip:after,:host([data-position="bottom-right"]) .tooltip:after{bottom:-4px;top:auto;border-left:0;border-top:0}
      :host([data-position="top-left"]) .tooltip,:host([data-position="top-right"]) .tooltip{top:calc(100% + 9px);bottom:auto}
      :host([data-position="top-left"]) .tooltip:after,:host([data-position="top-right"]) .tooltip:after{top:-4px;bottom:auto;border-right:0;border-bottom:0}
      :host([data-position="top-left"]) .tooltip,:host([data-position="bottom-left"]) .tooltip{left:0;right:auto}
      :host([data-position="top-left"]) .tooltip:after,:host([data-position="bottom-left"]) .tooltip:after{left:14px;right:auto}
      :host([data-position="top-right"]) .tooltip,:host([data-position="bottom-right"]) .tooltip{right:0;left:auto}
      :host([data-position="top-right"]) .tooltip:after,:host([data-position="bottom-right"]) .tooltip:after{right:14px;left:auto}
      :host([data-inline]) .tooltip{top:calc(100% + 9px);bottom:auto;left:50%;right:auto;transform:translateX(-50%)}
      :host([data-inline]) .tooltip:after{top:-4px;bottom:auto;left:calc(50% - 4px);right:auto;border-right:0;border-bottom:0;border-left:1px solid var(--border);border-top:1px solid var(--border)}
      @media(prefers-reduced-motion:reduce){.tooltip{transition:none}}
      .backdrop{position:fixed;inset:0;z-index:2147483646;background:rgba(15,23,42,.56);display:none;align-items:flex-start;justify-content:center;padding:8vh 16px;backdrop-filter:blur(2px)}.backdrop.open{display:flex}
      .dialog{width:${appearance.dialogWidth || 760}px;max-width:100%;max-height:78vh;display:flex;flex-direction:column;border:1px solid var(--border);border-radius:16px;background:var(--bg);color:var(--text);box-shadow:var(--panel-shadow);overflow:hidden}
      .searchbar{display:flex;align-items:center;gap:12px;padding:16px 18px;border-bottom:1px solid var(--border)}
      .searchbar input,.searchbar input:focus{min-width:0;flex:1;border:0;outline:0;box-shadow:none;background:transparent;color:var(--text);font-size:20px}.searchbar input::placeholder{color:var(--muted);opacity:.75}.esc{border:1px solid var(--border);border-radius:7px;background:transparent;color:var(--muted);font:12px/16px var(--font-family);padding:3px 7px;cursor:pointer;transition:background-color .15s ease,color .15s ease}.esc:hover{background:var(--surface);color:var(--text)}.close{border:0;background:transparent;color:var(--muted);font-size:25px;cursor:pointer;padding:2px 6px}.close:hover{color:var(--text)}
      .results{overflow:auto;padding:11px 12px 15px}.group-title{margin:12px 5px 6px;font-size:16px;font-weight:600;color:var(--muted)}
      .result-row{display:flex;align-items:center;gap:4px}.result{display:flex;gap:12px;min-width:0;flex:1;border:1px solid transparent;border-radius:10px;background:var(--surface);color:var(--text);padding:11px 12px;margin:5px 0;text-align:left;cursor:pointer;transition:background-color .12s ease,border-color .12s ease,box-shadow .12s ease}.result:hover,.result.selected{border-color:var(--border);background:var(--surface-hover);box-shadow:0 1px 2px rgba(15,23,42,.08)}.result-icon{width:22px;display:grid;place-items:center;flex:none;color:var(--muted);font-size:20px;line-height:22px}.result-icon svg{display:block}.copy{min-width:0}.snippet,.title{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.snippet{font-size:14px;font-weight:500}.title{margin-top:3px;font-size:13px;color:var(--muted)}.remove-recent{flex:none;border:0;border-radius:7px;background:transparent;color:color-mix(in srgb,var(--muted) 55%,transparent);font-size:22px;line-height:1;padding:7px;cursor:pointer;transition:color .12s ease,background-color .12s ease}.remove-recent:hover{background:var(--surface);color:var(--text)}mark{background:transparent;color:var(--highlight);font-weight:700;text-decoration:var(--match-decoration);text-decoration-color:var(--highlight);text-decoration-thickness:2px;text-underline-offset:2px;padding:0}.empty{padding:40px 16px;text-align:center;color:var(--muted);font-size:14px}.loading{padding:20px;text-align:center;color:var(--muted)}.loading-more{padding:12px;text-align:center;color:var(--muted);font-size:13px}
      .document-backdrop{position:fixed;inset:0;z-index:2147483647;background:rgba(15,23,42,.66);display:none;align-items:flex-start;justify-content:center;padding:5vh 16px;backdrop-filter:blur(2px)}.document-backdrop.open{display:flex}.document-dialog{width:min(920px,100%);height:88vh;display:flex;flex-direction:column;border:1px solid var(--border);border-radius:16px;background:var(--bg);color:var(--text);box-shadow:var(--panel-shadow);overflow:hidden}.document-header{display:flex;align-items:center;gap:12px;padding:13px 18px;border-bottom:1px solid var(--border)}.document-back{display:grid;flex:none;place-items:center;width:34px;height:34px;border:0;border-radius:8px;background:transparent;color:var(--muted);cursor:pointer}.document-back:hover{background:var(--surface);color:var(--text)}.document-back svg{width:22px;height:22px}.document-title{min-width:0;flex:1;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;font-size:17px;font-weight:600}.document-body{overflow:auto;outline:0;padding:24px 30px;font:15px/1.65 var(--font-family)}.document-body.plaintext{white-space:pre-wrap}.document-body h1,.document-body h2,.document-body h3,.document-body h4{margin:1.35em 0 .55em;font-weight:650;line-height:1.25}.document-body h1{font-size:2em}.document-body h2{font-size:1.55em;border-bottom:1px solid var(--border);padding-bottom:.25em}.document-body h3{font-size:1.25em}.document-body p,.document-body ul,.document-body ol,.document-body pre,.document-body blockquote{margin:.8em 0}.document-body pre{overflow:auto;border-radius:8px;background:var(--surface);padding:14px}.document-body code{border-radius:4px;background:var(--surface);padding:.15em .3em}.document-body pre code{padding:0}.document-body a{color:inherit;text-decoration:underline}.document-body img{max-width:100%}.document-body blockquote{margin-left:0;border-left:3px solid var(--border);padding-left:14px;color:var(--muted)}.document-body table{border-collapse:collapse}.document-body th,.document-body td{border:1px solid var(--border);padding:6px 9px}
      @media(max-width:640px){.backdrop,.document-backdrop{padding:0}.dialog,.document-dialog{width:100%;height:100%;max-height:none;border-radius:0}.document-body{padding:18px}}
    `
    shadow.innerHTML = `<style>${styles}</style>
    <span class="launcher-wrap"><button class="launcher" type="button" aria-label="Search">
      <svg class="launcher-icon" viewBox="0 0 16 16" aria-hidden="true"><path fill-rule="evenodd" d="M9.965 11.026a5 5 0 1 1 1.06-1.06l2.755 2.754a.75.75 0 1 1-1.06 1.06l-2.755-2.754ZM10.5 7a3.5 3.5 0 1 1-7 0 3.5 3.5 0 0 1 7 0Z" clip-rule="evenodd"></path></svg>
      <kbd class="key"></kbd>
    </button></span>
    <div class="backdrop" role="presentation"><section class="dialog" role="dialog" aria-modal="true" aria-label="Search">
      <div class="searchbar"><svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="m20 20-4-4"/></svg><input type="text" autocomplete="off" spellcheck="false"><button class="esc" type="button" aria-label="Close search">esc</button></div>
      <div class="results"></div>
    </section></div>
    <div class="document-backdrop" role="presentation"><section class="document-dialog" role="dialog" aria-modal="true" aria-label="Document preview">
      <div class="document-header"><button class="document-back" type="button" aria-label="Back to search results"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M19 12H5M12 19l-7-7 7-7"/></svg></button><div class="document-title"></div><button class="document-close close" type="button" aria-label="Close document">×</button></div>
      <article class="document-body" tabindex="-1"></article>
    </section></div>`
    const launcher = shadow.querySelector('.launcher')
    const launcherWrap = shadow.querySelector('.launcher-wrap')
    const tooltipText = String(CONFIG.tooltip || '').trim()
    if (tooltipText) {
        const tooltip = document.createElement('span')
        tooltip.className = 'tooltip'
        tooltip.id = `gemini-search-tooltip-${CONFIG.searchId}`
        tooltip.setAttribute('role', 'tooltip')
        tooltip.textContent = tooltipText
        launcherWrap.append(tooltip)
        launcher.setAttribute('aria-describedby', tooltip.id)
    }
    const floatingAssistant = () => document.querySelector('[data-gemini-assistant]:not([data-anchored])')
    const syncLauncherPosition = () => host.style.setProperty('--assistant-offset',
        !launcherHost && position === 'bottom-right' && floatingAssistant() ? '62px' : '0px')
    syncLauncherPosition()
    new MutationObserver(syncLauncherPosition).observe(document.body, { childList: true })
    const syncAutoTheme = event => {
        if (appearance.theme !== 'auto' || event?.key && event.key !== 'color-scheme') return
        applyTheme()
    }
    window.addEventListener('storage', syncAutoTheme)
    colorSchemeMedia.addEventListener?.('change', syncAutoTheme)
    document.addEventListener('visibilitychange', syncAutoTheme)
    new MutationObserver(syncAutoTheme).observe(document.documentElement, { attributes: true, attributeFilter: ['class', 'style'] })
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
    let timer = 0, requestNo = 0, searchController = null, items = [], selected = -1, hoverSelected = false
    let activeQuery = '', activeSearchEventId = null, nextSkip = 0, hasMore = false, loadingMore = false
    const recentKey = `gemini-search:${CONFIG.searchId}:recent`
    const recentLimit = 8

    const analyticsId = () => {
        try { return crypto.randomUUID() }
        catch (_) { return `${Date.now().toString(36)}${Math.random().toString(36).slice(2)}` }
    }
    const analyticsIdentity = () => {
        const clientKey = `gemini-search:${CONFIG.searchId}:client`
        const sessionKey = `gemini-search:${CONFIG.searchId}:session`
        const now = Date.now()
        let clientId = '', firstVisit = false, session = null
        try {
            clientId = localStorage.getItem(clientKey) || ''
            if (!clientId) { clientId = analyticsId(); localStorage.setItem(clientKey, clientId); firstVisit = true }
            session = JSON.parse(localStorage.getItem(sessionKey) || 'null')
            if (!session?.id || now - Number(session.lastAt || 0) > 30 * 60 * 1000) session = { id:analyticsId() }
            session.lastAt = now
            localStorage.setItem(sessionKey, JSON.stringify(session))
        } catch (_) {
            clientId = clientId || analyticsId()
            session = { id:analyticsId(), lastAt:now }
        }
        return { clientId, sessionId:session.id, firstVisit }
    }
    let pageViewTracked = false
    const analyticsAllowed = async () => {
        if (analytics.respectDoNotTrack !== false &&
            (navigator.doNotTrack === '1' || window.doNotTrack === '1')) return false
        if (!analytics.requireConsent) return true
        try {
            const callback = window.ServiceStackSearchAnalyticsConsent
            return typeof callback === 'function' && await callback({
                searchId:CONFIG.searchId, origin:location.origin, pageUrl:location.href,
            }) === true
        } catch (_) { return false }
    }
    const trackPageView = async () => {
        if (!analyticsEnabled || pageViewTracked) return
        if (!await analyticsAllowed()) return
        pageViewTracked = true
        const identity = analyticsIdentity()
        const params = new URLSearchParams(location.search)
        const navigation = performance.getEntriesByType?.('navigation')?.[0]
        const connection = navigator.connection || navigator.mozConnection || navigator.webkitConnection || {}
        const width = Math.max(document.documentElement.clientWidth || 0, innerWidth || 0)
        const touchPoints = Number(navigator.maxTouchPoints || 0)
        const deviceType = width <= 767 ? 'mobile' : width <= 1024 && touchPoints ? 'tablet' : 'desktop'
        const elapsed = value => Math.max(0, Math.min(Math.round(Number(value || 0)), 3600000))
        const payload = JSON.stringify({
            ...identity,
            pageUrl:location.href,
            pagePath:location.pathname + location.search,
            pageTitle:document.title,
            referrer:document.referrer || null,
            language:navigator.language || null,
            languages:Array.isArray(navigator.languages) ? navigator.languages.join(',') : null,
            timezone:Intl.DateTimeFormat().resolvedOptions().timeZone || null,
            platform:navigator.userAgentData?.platform || navigator.platform || null,
            deviceType,
            screenWidth:screen.width,
            screenHeight:screen.height,
            viewportWidth:width,
            viewportHeight:Math.max(document.documentElement.clientHeight || 0, innerHeight || 0),
            devicePixelRatio:devicePixelRatio || 1,
            colorDepth:screen.colorDepth || 0,
            touchPoints,
            connectionType:connection.effectiveType || connection.type || null,
            downlink:Number(connection.downlink || 0),
            rtt:elapsed(connection.rtt),
            saveData:connection.saveData === true,
            navigationType:navigation?.type || null,
            durationMs:elapsed(navigation?.duration),
            domContentLoadedMs:elapsed(navigation?.domContentLoadedEventEnd),
            loadMs:elapsed(navigation?.loadEventEnd),
            utmSource:params.get('utm_source'),
            utmMedium:params.get('utm_medium'),
            utmCampaign:params.get('utm_campaign'),
            utmTerm:params.get('utm_term'),
            utmContent:params.get('utm_content'),
        })
        fetch(CONFIG.analyticsUrl, {
            method:'POST', body:payload, keepalive:true, credentials:'omit',
            headers:{ 'Content-Type':'text/plain;charset=UTF-8', Accept:'application/json' },
        }).catch(() => { /* Analytics must never affect the host page or Search. */ })
    }

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
    const markdownTags = new Set(['p', 'br', 'strong', 'em', 'code', 'pre', 'ul', 'ol', 'li', 'blockquote',
        'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'hr', 'a', 'table', 'thead', 'tbody', 'tr', 'th', 'td', 'del', 'input'])
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
        try { documentBody.append(sanitizedMarkdown(markdownParser.parse(source, { gfm: true }))) }
        catch (_) { documentBody.classList.add('plaintext'); documentBody.textContent = source }
    }
    async function previewDocument(item) {
        if (!item?.previewUrl) return
        documentTitle.textContent = item.documentTitle || item.title || 'Document'
        documentBody.classList.add('plaintext'); documentBody.textContent = 'Loading…'
        documentBackdrop.classList.add('open')
        documentBody.focus({ preventScroll: true })
        try {
            const response = await fetch(item.previewUrl, { headers: { Accept: 'application/json' } })
            if (!response.ok) throw new Error(`Document preview failed (${response.status})`)
            const data = await response.json(); documentTitle.textContent = data.title || documentTitle.textContent
            renderMarkdown(data.markdown)
            if (item.anchor) setTimeout(() => shadow.getElementById(item.anchor)?.scrollIntoView({ block: 'start' }), 0)
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
            url: item.url || null,
            previewUrl: item.previewUrl || null,
            anchor: item.anchor || null,
            documentTitle: item.documentTitle || item.title || 'Document',
            title: item.title || item.documentTitle || 'Document',
            snippet: item.snippet || '',
            type: item.type || 'content',
            documentId: item.documentId || null,
            sectionId: item.id || item.sectionId || null,
            clickedAt: Date.now(),
        }
        writeRecent([recent, ...readRecent().filter(x => recentId(x) !== recentId(recent))])
    }
    const trackClick = item => {
        if (!CONFIG.clickUrl || !activeSearchEventId || !item?.documentId) return
        const payload = JSON.stringify({
            searchEventId: activeSearchEventId,
            documentId: item.documentId,
            sectionId: item.id || item.sectionId || null,
            position: item.position || 1,
            documentTitle: item.documentTitle || item.title || 'Document',
            sourceUrl: item.url || null,
            resultType: item.type || 'content',
        })
        fetch(CONFIG.clickUrl, {
            method: 'POST', body: payload, keepalive: true, credentials: 'omit',
            headers: { 'Content-Type': 'text/plain;charset=UTF-8', Accept: 'application/json' },
        }).catch(() => { /* Analytics must never interrupt navigation or document preview. */ })
    }
    const activate = item => { trackClick(item); remember(item); item?.url ? navigate(item.url) : previewDocument(item) }
    function choose(index, hover = false) {
        selected = index
        hoverSelected = hover
        shadow.querySelectorAll('.result').forEach((el, i) => el.classList.toggle('selected', i === selected))
        if (!hover) shadow.querySelectorAll('.result')[selected]?.scrollIntoView({ block: 'nearest' })
    }
    function addResult(item, documentTitle, recent = false, target = results, documentId = null) {
        const entry = { ...item, documentId: documentId || item.documentId || null,
            documentTitle: documentTitle || item.documentTitle || item.title || 'Document' }
        const index = items.push(entry) - 1
        entry.position = index + 1
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
        row.addEventListener('mouseenter', () => choose(index, true))
        row.addEventListener('mouseleave', () => { if (hoverSelected && selected === index) choose(-1, true) })
        button.addEventListener('click', () => activate(entry))
        if (recent) {
            const remove = document.createElement('button'); remove.type = 'button'; remove.className = 'remove-recent'
            remove.setAttribute('aria-label', `Remove ${entry.documentTitle} from recent searches`); remove.textContent = '×'
            remove.addEventListener('click', () => { writeRecent(readRecent().filter(x => recentId(x) !== recentId(entry))); renderRecent() })
            row.appendChild(remove)
        }
        target.appendChild(row)
    }
    function renderRecent() {
        results.replaceChildren(); items = []; selected = -1; activeQuery = ''; activeSearchEventId = null; nextSkip = 0; hasMore = false
        const recent = readRecent()
        if (!recent.length) {
            const empty = document.createElement('div'); empty.className = 'empty'; empty.textContent = CONFIG.title || 'Search documentation'
            results.appendChild(empty); return
        }
        const heading = document.createElement('h3'); heading.className = 'group-title'; heading.textContent = 'Recent'
        results.appendChild(heading)
        recent.forEach(item => addResult(item, item.documentTitle, true))
    }
    function render(data, append = false) {
        results.querySelector('.loading-more')?.remove()
        if (!append) {
            activeSearchEventId = Number(data?.searchEventId) || null
            results.replaceChildren(); items = []; selected = -1
        }
        const groups = data?.groups || []
        if (!groups.length && !append) {
            const empty = document.createElement('div'); empty.className = 'empty'
            empty.textContent = input.value.trim() ? (CONFIG.emptyText || 'No matching documents found.') : (CONFIG.title || 'Search documentation')
            results.appendChild(empty); return
        }
        groups.forEach(group => {
            let container = [...results.querySelectorAll('.result-group')]
                .find(element => element.dataset.documentId === String(group.documentId))
            if (!container) {
                container = document.createElement('section'); container.className = 'result-group'
                container.dataset.documentId = String(group.documentId)
                const heading = document.createElement('h3'); heading.className = 'group-title'; heading.textContent = group.title
                container.appendChild(heading); results.appendChild(container)
            }
            const remaining = Math.max(0, Number(behavior.groupLimit || 8) - container.querySelectorAll('.result-row').length)
            ; (group.items || []).filter(item => !items.some(existing => String(existing.id) === String(item.id)))
                .slice(0, remaining).forEach(item => addResult(item, group.title, false, container, group.documentId))
        })
        hasMore = data?.hasMore === true
        nextSkip = Number(data?.nextSkip || 0)
        if (!append && items.length) choose(0)
        setTimeout(maybeLoadMore, 0)
    }
    async function search() {
        const q = input.value.trim(), current = ++requestNo
        searchController?.abort()
        searchController = null
        results.removeAttribute('aria-busy')
        if (!q) return renderRecent()
        if (q.length < (behavior.minChars || 2)) return render({ groups: [] })
        activeQuery = q; nextSkip = 0; hasMore = false; loadingMore = false
        results.querySelector('.loading-more')?.remove()
        results.setAttribute('aria-busy', 'true')
        const controller = new AbortController()
        searchController = controller
        try {
            const response = await fetch(CONFIG.searchUrl + '?q=' + encodeURIComponent(q) + '&skip=0', {
                headers: { Accept: 'application/json' }, signal: controller.signal,
            })
            if (!response.ok) throw new Error(`Search failed (${response.status})`)
            const data = await response.json()
            if (current === requestNo) render(data)
        } catch (error) {
            if (error?.name !== 'AbortError' && current === requestNo && !results.children.length) {
                const el = document.createElement('div'); el.className = 'empty'; el.textContent = 'Search is temporarily unavailable.'; results.appendChild(el)
            }
        } finally {
            if (current === requestNo) results.removeAttribute('aria-busy')
            if (searchController === controller) searchController = null
        }
    }
    async function loadMore() {
        if (!hasMore || loadingMore || !activeQuery || activeQuery !== input.value.trim()) return
        loadingMore = true
        const current = requestNo, skip = nextSkip
        const loading = document.createElement('div'); loading.className = 'loading-more'; loading.textContent = 'Loading more…'
        results.appendChild(loading)
        try {
            const response = await fetch(CONFIG.searchUrl + '?q=' + encodeURIComponent(activeQuery) + '&skip=' + skip,
                { headers: { Accept: 'application/json' } })
            if (!response.ok) throw new Error(`Search failed (${response.status})`)
            const data = await response.json()
            if (current === requestNo && activeQuery === input.value.trim()) render(data, true)
        } catch (_) { loading.remove(); hasMore = false }
        finally { loadingMore = false; maybeLoadMore() }
    }
    function maybeLoadMore() {
        if (hasMore && !loadingMore && results.scrollTop + results.clientHeight >= results.scrollHeight - 120) loadMore()
    }
    results.addEventListener('scroll', maybeLoadMore, { passive: true })
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
    if (analyticsEnabled) {
        if (document.readyState === 'complete') setTimeout(trackPageView, 0)
        else addEventListener('load', trackPageView, { once:true })
    }
    if (launcherHost) {
        const launcherShadow = launcherHost.attachShadow({ mode: 'open' })
        launcherShadow.innerHTML = `<style>${styles}</style>`
        launcherShadow.append(launcherWrap)
        mountElement.append(launcherHost)
    }
}
