import { reactive } from "vue"
import { ApiResult, combinePaths } from "@servicestack/client"

const base = ''
const headers = { 'Accept': 'application/json' }
const prefsKey = 'llms.prefs'

export const o = {
    version: '4.0.10',
    base,
    prefsKey,
    welcome: 'Welcome to llms.py',
    auth: null,
    requiresAuth: false,
    authType: 'apikey',  // 'oauth' or 'apikey' - controls which SignIn component to use
    headers,
    isSidebarOpen: true,  // Shared sidebar state (default open for lg+ screens)
    cacheUrlInfo: {},

    get hasAccess() {
        return !this.requiresAuth || this.auth
    },

    resolvePath(path) {
        return this.base ? combinePaths(this.base, path) : path
    },
    resolveUrl(url) {
        // urls built from ctx.ai.base (e.g. ExtensionScope.baseUrl) are already resolved
        return url.startsWith('http') || url.startsWith('/v1') || (base && url.startsWith(base + '/'))
            ? url
            : base + url
    },
    get(url, options) {
        return fetch(this.resolveUrl(url), {
            ...options,
            headers: Object.assign({}, this.headers, options?.headers),
        })
    },
    async getJson(url, options) {
        const res = await this.get(url, options)
        return await this.createJsonResult(res, url)
    },
    async post(url, options) {
        return await fetch(this.resolveUrl(url), {
            method: 'POST',
            ...options,
            headers: Object.assign({ 'Content-Type': 'application/json' }, this.headers, options?.headers),
        })
    },
    async postForm(url, options) {
        return await fetch(this.resolveUrl(url), {
            method: 'POST',
            ...options,
            headers: Object.assign({}, options?.headers),
        })
    },
    async postJson(url, options) {
        const res = await this.post(url, options)
        return await this.createJsonResult(res, url)
    },
    async createJsonResult(res, msg = null) {
        let txt = ''
        try {
            txt = await res.text()
            const response = JSON.parse(txt)
            if (response?.responseStatus?.errorCode) {
                return new ApiResult({ error: response.responseStatus })
            }
            if (!res.ok) {
                return new ApiResult({ error: { errorCode: 'Error', message: res.statusText } })
            }
            return new ApiResult({ response })
        } catch (e) {
            console.error('Failed to parse JSON', e, msg, txt)
            const responseStatus = {
                errorCode: 'Error',
                message: `${e.message ?? e}`,
                stackTrace: msg ? `${msg}\n${txt}` : txt,
            }
            return { responseStatus }
        }
    },
    createErrorStatus({ message, errorCode, stackTrace, errors, meta }) {
        const ret = {
            errorCode: errorCode || 'Error',
            message: message,
        }
        if (stackTrace) {
            ret.stackTrace = stackTrace
        }
        if (errors && Array.isArray(errors)) {
            ret.errors = errors
        }
        if (meta) {
            ret.meta = meta
        }
        return ret
    },
    createErrorResult(e) {
        return new ApiResult({
            error: e.errorCode
                ? this.createErrorStatus(e)
                : this.createErrorStatus({ message: `${e.message ?? e}` })
        })
    },
    async getConfig() {
        return this.get('/config')
    },
    async getModels() {
        return this.get('/models')
    },
    async getAuth() {
        return this.requiresAuth
            ? this.get('/auth')
            : new Promise(resolve => resolve({ json: () => ({ responseStatus: { errorCode: '!requiresAuth' } }) }))
    },
    get isAdmin() {
        return !this.requiresAuth || this.auth && this.auth.roles?.includes('Admin')
    },

    signIn(auth) {
        this.auth = auth
        if (auth?.apiKey) {
            this.headers.Authorization = `Bearer ${auth.apiKey}`
        } else {
            if (this.headers.Authorization) {
                delete this.headers.Authorization
            }
        }
    },
    async signOut() {
        try {
            await this.post('/auth/logout')
        } catch (error) {
            console.error('Logout error:', error)
        }
        this.auth = null
        if (this.headers.Authorization) {
            delete this.headers.Authorization
        }
    },
    async init(ctx) {
        // Load models and prompts
        const [configRes, modelsRes, extensionsRes, prefsRes] = await Promise.all([
            this.getConfig(),
            this.getModels(),
            this.get('/ext'),
            this.get('/prefs'),
        ])
        const config = await configRes.json()
        const models = await modelsRes.json()
        const extensions = await extensionsRes.json()
        const prefs = await prefsRes.json()

        // Update auth settings from server config
        if (config.requiresAuth != null) {
            this.requiresAuth = config.requiresAuth
        }
        if (config.authType != null) {
            this.authType = config.authType
        }

        // Get auth status
        const authRes = await this.getAuth()
        const auth = this.requiresAuth
            ? await authRes.json()
            : null
        if (auth?.responseStatus?.errorCode) {
            console.error(auth.responseStatus.errorCode, auth.responseStatus.message)
        } else {
            this.signIn(auth)
        }
        return { config, models, extensions, auth, prefs }
    },

    async uploadFile(file) {
        const formData = new FormData()
        formData.append('file', file)
        const response = await fetch(this.resolveUrl('/upload'), {
            method: 'POST',
            body: formData
        })
        if (!response.ok) {
            throw new Error(`Upload failed: ${response.statusText}`)
        }
        return response.json()
    },


    getCacheInfo(url) {
        return this.cacheUrlInfo[url]
    },
    async fetchCacheInfos(urls) {
        const infos = {}
        const fetchInfos = []
        for (const url of urls) {
            const info = this.getCacheInfo(url)
            if (info) {
                infos[url] = info
            } else {
                fetchInfos.push(fetch(this.resolveUrl(url + "?info")))
            }
        }
        const responses = await Promise.all(fetchInfos)
        for (let i = 0; i < urls.length; i++) {
            try {
                const info = await responses[i].json()
                this.setCacheInfo(urls[i], info)
                infos[urls[i]] = info
            } catch (e) {
                console.error('Failed to fetch info for', urls[i], e)
            }
        }
        return infos
    },
    setCacheInfo(url, info) {
        this.cacheUrlInfo[url] = info
    },

    shared: {
        "vars": {
        },
        "styles": {
            "app": "bg-[image:var(--background-image)] bg-cover",
            "appInner": "",
            "tagButtonLarge": "rounded-xl shadow-sm",
            "tagButtonSmall": "rounded-full shadow-sm",

            "tagLabel": "bg-[var(--tw-prose-code-bg)]/50 text-[var(--tw-prose-code)]/90 border border-[var(--tw-prose-code-border)]/70",
            "tagLabelHover": "hover:bg-[var(--tw-prose-code-bg)] hover:text-[var(--tw-prose-code)] hover:border-[var(--tw-prose-code-border)]",

            "messageUser": "bg-[var(--user-bg)] text-[var(--user-text)] border border-[var(--user-border)]",
            "messageAssistant": "bg-[var(--assistant-bg)] text-[var(--assistant-text)] border border-[var(--assistant-border)]"
        }
    },

    light: {
        "preview": {
            "chromeBorder": "border-gray-200",
            "bgBody": "bg-white",
            "bgSidebar": "bg-gray-50",
            "icon": "text-gray-500",
            "heading": "text-gray-900"
        },
        "vars": {
            "colorScheme": "light",
            // gray-900
            "--heading": "#111827",
            // white
            "--background": "#ffffff",
            // gray-200
            "--border": "#e5e7eb",
            // gray-200
            "--input": "#e5e7eb",
            // blue-600
            "--ring": "#2563eb",
            // gray-50
            "--scrollbar-track-bg": "#f9fafb",
            // gray-300
            "--scrollbar-thumb-bg": "#d1d5db",
            // white
            "--primary-bg": "#ffffff",
            // gray-50
            "--secondary-bg": "#f9fafb",
            // gray-200
            "--secondary-border": "#e5e7eb",
            // blue-100
            "--user-bg": "#dbeafe",
            // gray-900
            "--user-text": "#111827",
            // blue-200
            "--user-border": "#bfdbfe",
            // gray-100
            "--assistant-bg": "#f3f4f6",
            // gray-900
            "--assistant-text": "#111827",
            // gray-200
            "--assistant-border": "#e5e7eb",

            "--tw-prose-body": "#374151",
            "--tw-prose-headings": "#111827",
            "--tw-prose-lead": "#4b5563",
            "--tw-prose-links": "#111827",
            "--tw-prose-bold": "#111827",
            "--tw-prose-counters": "#6b7280",
            "--tw-prose-bullets": "#d1d5db",
            "--tw-prose-hr": "#e5e7eb",
            "--tw-prose-quotes": "#111827",
            "--tw-prose-quote-borders": "#e5e7eb",
            "--tw-prose-captions": "#6b7280",
            "--tw-prose-pre-code": "#e5e7eb",
            "--tw-prose-pre-bg": "#282c34",
            "--tw-prose-code": "#1e293b",
            "--tw-prose-code-bg": "#dbeafe",
            "--tw-prose-code-border": "#93c5fd",
            "--tw-prose-table-bg": "#f9fafb",
            "--tw-prose-th-bg": "#eff6ff",
            "--tw-prose-th-borders": "#bfdbfe",
            "--tw-prose-td-borders": "#d1d5db",
        },
        "styles": {
            "chromeBorder": "border-gray-200",
            "heading": "text-gray-900",
            "muted": "text-gray-500",
            "mutedHover": "hover:text-gray-600",
            "mutedActive": "text-gray-600",
            "highlighted": "text-blue-600",
            "link": "text-blue-500 hover:text-blue-600",
            "linkHover": "hover:text-blue-600 group-hover:text-blue-600",
            "bgBody": "bg-white",
            "bgSidebar": "bg-gray-50",
            "bgChat": "bg-gray-50",
            "bgPage": "bg-gray-50",
            "bgSuccess": "bg-green-100 text-green-800",
            "bgWarning": "bg-orange-100 text-orange-800",
            "bgInput": "bg-white",
            "dialog": "rounded-lg shadow-xl bg-white border border-gray-200",
            "textInput": "text-gray-900 placeholder-gray-500",
            "borderInput": "border-gray-300 focus:border-blue-500 focus:ring-blue-500 focus:ring-offset-white focus-within:ring-indigo-500 focus-within:border-indigo-500",
            "checkbox": "rounded bg-gray-500",
            "labelInput": "text-gray-700",
            "helpInput": "text-gray-500",
            "draggingInput": "border-blue-500 bg-blue-50 ring-1 ring-blue-500",
            "dropdownButton": "border border-gray-300 bg-white hover:bg-gray-50 text-gray-700 focus:outline-none",
            "bgPopover": "bg-white border border-gray-200",
            "popoverButton": "hover:bg-gray-100",
            "popoverButtonActive": "bg-blue-50",
            "codeTag": "text-gray-700 bg-gray-100",
            "codeTagStrong": "border border-blue-200 text-gray-700 bg-blue-50",
            "tagButtonGroup": "rounded-xl bg-black/5 shadow-inner",
            "tagButton": "cursor-pointer border border-gray-300 text-gray-700",
            "tagButtonActive": "border border-blue-200 text-blue-700 bg-blue-100",
            "tagButtonStrongActive": "bg-green-100 text-green-800 border-green-300",
            "panel": "border-gray-200 bg-gray-50",
            "card": "rounded-lg bg-white border border-gray-200",
            "infoCard": "rounded-lg bg-white border border-gray-200",
            "cardTitle": "border-b border-gray-200 bg-gray-50",
            "cardActive": "rounded-lg shadow-sm bg-white border border-blue-200",
            "cardActiveTitleBar": "border-b border-blue-100 bg-blue-50",
            "textBlock": "text-gray-900",
            "primaryButton": "border border-transparent shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed rounded-md",
            "secondaryButton": "text-gray-700 bg-white border border-gray-300 hover:bg-gray-50 rounded-md",
            "textLink": "underline hover:text-gray-900",
            "bgIcon": "bg-gray-500",
            "icon": "text-gray-500",
            "iconHover": "hover:text-gray-700",
            "iconActive": "bg-gray-200",
            "iconPartial": "text-indigo-800/80!",
            "iconFull": "text-green-700/80!",
            "mutedIcon": "text-gray-500",
            "mutedIconHover": "hover:text-gray-700",
            "chatButton": "border border-gray-300 text-gray-600 bg-white hover:bg-gray-50 disabled:text-gray-400 disabled:cursor-not-allowed disabled:border-gray-200 transition-colors",
            "voiceButtonDefault": "bg-white text-gray-400 hover:text-gray-600",
            "voiceButtonRecording": "border bg-red-100 border-red-400 text-red-600 animate-pulse",
            "voiceButtonProcessing": "bg-blue-100 text-blue-600 animate-spin",
            "threadItemActiveBorder": "border-blue-300",
            "threadItemActive": "bg-blue-100 border-blue-200",
            "threadItem": "border-transparent hover:bg-gray-100",
            "tabButton": "text-gray-500 hover:text-gray-900 hover:bg-gray-200",
        },
    },
    dark: {
        "preview": {
            "chromeBorder": "border-gray-700",
            "bgBody": "bg-gray-900",
            "bgSidebar": "bg-gray-800/50",
            "icon": "text-gray-400",
            "heading": "text-indigo-200"
        },
        "vars": {
            "colorScheme": "dark",
            // gray-200
            "--heading": "#e2e8f0",
            // gray-900
            "--background": "#111827",
            // gray-800
            "--border": "#1f2937",
            // gray-800
            "--input": "#1f2937",
            // gray-300
            "--ring": "#d1d5db",
            // gray-800
            "--scrollbar-track-bg": "#1f2937",
            // gray-600
            "--scrollbar-thumb-bg": "#4b5563",
            // gray-800
            "--primary-bg": "#1f2937",
            // gray-800
            "--secondary-bg": "#1f2937",
            // gray-700
            "--secondary-border": "#374151",
            // blue-900
            "--user-bg": "#1e3a8a",
            // gray-100
            "--user-text": "#f3f4f6",
            // gray-700
            "--user-border": "#374151",
            // gray-800
            "--assistant-bg": "#1e293b",
            // gray-100
            "--assistant-text": "#f1f5f9",
            // gray-700
            "--assistant-border": "#334155",

            "--tw-prose-body": "#d1d5db",
            "--tw-prose-headings": "#fff",
            "--tw-prose-lead": "#9ca3af",
            "--tw-prose-links": "#fff",
            "--tw-prose-bold": "#fff",
            "--tw-prose-counters": "#9ca3af",
            "--tw-prose-bullets": "#4b5563",
            "--tw-prose-hr": "#374151",
            "--tw-prose-quotes": "#f3f4f6",
            "--tw-prose-quote-borders": "#374151",
            "--tw-prose-captions": "#9ca3af",
            "--tw-prose-pre-code": "#d1d5db",
            "--tw-prose-pre-bg": "rgb(0 0 0 / 50%)",
            "--tw-prose-code": "#93c5fd",
            "--tw-prose-code-bg": "#1e40af99",
            "--tw-prose-code-border": "#2563eb99",
            "--tw-prose-table-bg": "#11182780",
            "--tw-prose-th-bg": "#1e3a8a66",
            "--tw-prose-th-borders": "#2563eb66",
            "--tw-prose-td-borders": "#374151",
        },
        "styles": {
            "chromeBorder": "border-gray-700",
            "heading": "text-gray-200",
            "muted": "text-gray-400",
            "mutedActive": "text-gray-300",
            "mutedHover": "hover:text-gray-200",
            "highlighted": "text-blue-400",
            "link": "text-blue-600 hover:text-blue-400",
            "linkHover": "hover:text-blue-400 group-hover:text-blue-400",
            "bgBody": "bg-gray-900",
            "bgSidebar": "bg-gray-800 lg:bg-gray-800/50",
            "bgChat": "bg-gray-800",
            "bgPage": "bg-gray-900",
            "bgSuccess": "bg-green-900 text-green-200",
            "bgWarning": "bg-orange-900 text-orange-200",
            "bgInput": "bg-gray-950",
            "dialog": "rounded-lg shadow-xl bg-gray-900 border border-gray-700",
            "textInput": "text-gray-100 placeholder-gray-500",
            "borderInput": "border-gray-600 focus:border-blue-500 focus:ring-blue-500 focus:ring-offset-gray-900 focus-within:ring-indigo-500 focus-within:border-indigo-500",
            "checkbox": "rounded bg-gray-600",
            "labelInput": "text-gray-300",
            "helpInput": "text-gray-400",
            "draggingInput": "border-blue-500 bg-blue-900/30 ring-1 ring-blue-500",
            "dropdownButton": "border border-gray-600 bg-gray-900 hover:bg-gray-800 text-gray-300 focus:outline-none",
            "bgPopover": "bg-gray-800 border border-gray-700",
            "popoverButton": "hover:bg-gray-700",
            "popoverButtonActive": "bg-blue-900/30",
            "codeTag": "text-gray-300 bg-gray-700",
            "codeTagStrong": "border border-blue-800 text-gray-300 bg-blue-900/30",
            "tagButtonGroup": "rounded-xl bg-white/5 border border-gray-700",
            "tagButton": "cursor-pointer border border-transparent text-gray-300",
            "tagButtonActive": "border border-blue-800 text-blue-300 bg-blue-900/50",
            "tagButtonStrongActive": "bg-green-900/40 text-green-300 border-green-800",
            "panel": "border-gray-700 bg-gray-800",
            "card": "rounded-lg bg-gray-800 border border-gray-700",
            "infoCard": "rounded-lg bg-gray-800 border border-gray-700",
            "cardTitle": "border-b border-gray-700 bg-gray-800/50",
            "cardActive": "rounded-lg shadow-sm bg-gray-800 border border-blue-800",
            "cardActiveTitleBar": "border-b border-blue-800 bg-blue-900/30",
            "textBlock": "text-gray-100",
            "primaryButton": "border border-transparent shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-offset-gray-900 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed rounded-md",
            "secondaryButton": "text-gray-300 bg-gray-800 border border-gray-600 hover:bg-gray-700 rounded-md",
            "textLink": "underline hover:text-gray-100",
            "bgIcon": "bg-gray-400",
            "icon": "text-gray-400",
            "iconHover": "hover:text-gray-300",
            "iconActive": "bg-gray-700",
            "iconPartial": "text-indigo-400/80!",
            "iconFull": "text-green-400/80!",
            "mutedIcon": "text-gray-400",
            "mutedIconHover": "hover:text-gray-200",
            "chatButton": "border border-gray-600 text-gray-400 bg-gray-800 hover:bg-gray-700 disabled:text-gray-400 disabled:cursor-not-allowed disabled:border-gray-700 transition-colors",
            "voiceButtonDefault": "bg-gray-800 text-gray-400 hover:text-gray-200",
            "voiceButtonRecording": "border border-red-600 bg-red-900/30 text-red-400 animate-pulse",
            "voiceButtonProcessing": "bg-blue-900/30 text-blue-400 animate-spin",
            "threadItemActiveBorder": "border-blue-600",
            "threadItemActive": "bg-blue-900 border-blue-700",
            "threadItem": "border-transparent hover:bg-gray-800",
            "tabButton": "text-gray-400 hover:text-white hover:bg-white",
        },
    },

    createTheme(theme = {}) {
        const colorScheme = theme.vars?.colorScheme
            || ((localStorage.getItem('color-scheme') === 'dark' || window.matchMedia('(prefers-color-scheme: dark)').matches) ? 'dark' : 'light')
        const isDark = colorScheme === 'dark'
        const defaultTheme = isDark ? this.dark : this.light

        const vars = Object.assign({
            colorScheme,
            "--background-image": `url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='1' height='1'%3E%3Crect width='1' height='1' fill='%2300000000'/%3E%3C/svg%3E%0A")`,
        }, this.shared.vars, defaultTheme.vars, theme.vars)
        const styles = Object.assign({}, this.shared.styles, defaultTheme.styles, theme.styles)

        let preview = theme.preview
        if (!preview) {
            preview = {}
            Object.keys(defaultTheme.preview).forEach(key => {
                preview[key] = styles[key] || defaultTheme.preview[key]
            })
        }

        const ret = {
            preview,
            vars,
            styles,
        }

        console.log('createTheme', ret)
        return ret
    }

}

let ai = reactive(o)
export default ai
