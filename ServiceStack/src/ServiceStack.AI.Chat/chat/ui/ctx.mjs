
import { reactive, markRaw, watch } from 'vue'
import { EventBus, humanize, combinePaths, pick } from "@servicestack/client"
import { storageObject, isHtml, sanitizeHtml } from './utils.mjs'

export class ExtensionScope {
    constructor(ctx, id) {
        /**@type {AppContext} */
        this.ctx = ctx
        this.router = ctx.router
        this.id = id
        this.baseUrl = `${ctx.ai.base}/ext/${this.id}`
        this.storageKey = `llms.${this.id}`
        this.state = reactive({})
        this.prefs = reactive(storageObject(this.storageKey))
    }
    getPrefs() {
        return this.prefs
    }
    setPrefs(o) {
        return storageObject(this.storageKey, Object.assign(this.prefs, o))
    }
    savePrefs() {
        storageObject(this.storageKey, this.prefs)
    }
    setState(o) {
        Object.assign(this.state, o)
    }
    get(url, options) {
        return this.ctx.ai.get(combinePaths(this.baseUrl, url), options)
    }
    delete(url, options) {
        this.ctx.clearError()
        return this.ctx.ai.get(combinePaths(this.baseUrl, url), {
            ...options,
            method: 'DELETE'
        })
    }
    async getJson(url, options) {
        return this.ctx.ai.getJson(combinePaths(this.baseUrl, url), options)
    }
    async deleteJson(url, options) {
        this.ctx.clearError()
        return this.ctx.ai.getJson(combinePaths(this.baseUrl, url), {
            ...options,
            method: 'DELETE'
        })
    }
    post(url, options) {
        this.ctx.clearError()
        return this.ctx.ai.post(combinePaths(this.baseUrl, url), options)
    }
    put(url, options) {
        this.ctx.clearError()
        return this.ctx.ai.post(combinePaths(this.baseUrl, url), {
            ...options,
            method: 'PUT'
        })
    }
    patch(url, options) {
        this.ctx.clearError()
        return this.ctx.ai.post(combinePaths(this.baseUrl, url), {
            ...options,
            method: 'PATCH'
        })
    }
    async postForm(url, options) {
        this.ctx.clearError()
        return await this.ctx.ai.postForm(combinePaths(this.baseUrl, url), options)
    }
    async postJson(url, body) {
        this.ctx.clearError()
        return this.ctx.ai.postJson(combinePaths(this.baseUrl, url), {
            body: body instanceof FormData ? body : JSON.stringify(body)
        })
    }
    async putJson(url, body) {
        this.ctx.clearError()
        return this.ctx.ai.postJson(combinePaths(this.baseUrl, url), {
            method: 'PUT',
            body: body instanceof FormData ? body : JSON.stringify(body)
        })
    }
    async patchJson(url, body) {
        this.ctx.clearError()
        return this.ctx.ai.postJson(combinePaths(this.baseUrl, url), {
            method: 'PATCH',
            body: body instanceof FormData ? body : JSON.stringify(body)
        })
    }
    async createJsonResult(res) {
        return this.ctx.ai.createJsonResult(res)
    }
    createErrorStatus(status) {
        return this.ctx.ai.createErrorStatus(status)
    }
    createErrorResult(e) {
        return this.ctx.ai.createErrorResult(e)
    }
    setError(e, msg = null) {
        const prefix = this.id ? `[${this.id}] ` : ''
        this.ctx.setError(e, msg ? `${prefix} ${msg}` : prefix)
    }
    clearError() {
        this.ctx.clearError()
    }
    toast(msg) {
        this.ctx.toast(msg)
    }
    to(route) {
        if (typeof route == 'string') {
            route = route.startsWith(this.baseUrl)
                ? route
                : combinePaths(this.baseUrl, route)
            const path = { path: route }
            console.log(`to/${this.id}`, path)
            this.router.push(path)
        } else {
            route.path = route.path.startsWith(this.baseUrl)
                ? route.path
                : combinePaths(this.baseUrl, route.path)
            console.log(`to/${this.id}`, route)
            this.router.push(route)
        }
    }
}

export class AppContext {
    constructor({ app, routes, ai, fmt, utils, marked, markedFallback }) {
        this.app = app
        this.routes = routes
        this.ai = ai
        this.fmt = fmt
        this.utils = utils
        this._components = {}
        this.marked = marked
        this.markedFallback = markedFallback

        const theme = ai.createTheme()
        const state = this.state = reactive({
            cacheBreaker: new Date().getTime(),
            theme,
            styles: theme.styles,
            profile: localStorage.getItem('llms.profile') || 'default',
            message: null,
        })
        this.events = new EventBus()
        this.modalComponents = {}
        this.extensions = []
        this.markedFilters = []
        this.chatRequestFilters = []
        this.chatResponseFilters = []
        this.chatErrorFilters = []
        this.createThreadFilters = []
        this.updateThreadFilters = []
        this.threadHeaderComponents = {}
        this.threadFooterComponents = {}
        this.userMenuItemComponents = {}
        this.top = {}
        this.left = {}
        this.leftTop = {}
        this.pdf = {
            previewActions: reactive({}),
            setPreviewActions: (components) => {
                if (!components) return
                Object.entries(components).forEach(([id, def]) => {
                    if (def && typeof def === 'object' && !def.component && (def.template || def.setup || def.render)) {
                        this.pdf.previewActions[id] = { id, component: markRaw(def) }
                    } else if (def && typeof def === 'object' && def.component) {
                        this.pdf.previewActions[id] = { id, ...def, component: markRaw(def.component) }
                    } else if (def) {
                        this.pdf.previewActions[id] = { id, component: markRaw(def) }
                    }
                })
            }
        }
        this.layout = reactive(storageObject(`llms.layout`))

        const oldPrefsKey = ai.prefsKey
        const prefsKey = ai.prefsKey + '.' + this.state.profile
        if (localStorage.getItem(oldPrefsKey)) {
            if (!localStorage.getItem(prefsKey)) {
                const oldPrefs = storageObject(oldPrefsKey)
                storageObject(prefsKey, oldPrefs)
            }
            localStorage.removeItem(oldPrefsKey)
        }
        this.prefs = reactive(storageObject(prefsKey))

        this._onRouterBeforeEach = []
        this._onClass = []

        if (!Array.isArray(this.layout.hide)) {
            this.layout.hide = []
        }
        Object.assign(app.config.globalProperties, {
            $ctx: this,
            $prefs: this.prefs,
            $state: this.state,
            $layout: this.layout,
            $ai: ai,
            $fmt: fmt,
            $utils: utils,
            get $styles() { return state.styles },
        })
        Object.keys(app.config.globalProperties).forEach(key => {
            globalThis[key] = app.config.globalProperties[key]
        })
        document.addEventListener('keydown', (e) => this.handleKeydown(e))

        let isRefreshing = false
        watch(() => ai.auth, async (newAuth, oldAuth) => {
            if (newAuth !== oldAuth) {
                if (isRefreshing) return
                isRefreshing = true
                try {
                    console.log('Authentication status changed, reloading context and modules...')
                    Object.assign(this.state, await this.ai.init(this))
                    await this.load()
                } catch (e) {
                    console.error('Failed to refresh context and modules:', e)
                } finally {
                    isRefreshing = false
                }
            }
        })
    }
    async init() {
        Object.assign(this.state, await this.ai.init(this))
        Object.assign(this.fmt, {
            markdown: this.renderMarkdown.bind(this),
            content: this.renderContent.bind(this),
        })
    }
    async load() {
        if (!this.installedModules) return
        const loadModules = this.installedModules.filter(x => x.module.default && x.module.default.load)
        console.log('Loading modules: ', loadModules.map(x => x.extension.id))
        await Promise.all(loadModules.map(async result => {
            try {
                await result.module.default.load(this)
                console.log(`Loaded extension: ${result.extension.id}`)
            } catch (e) {
                console.error(`Failed to load extension ${result.extension.id}:`, e)
            }
        }))
    }
    setGlobals(globals) {
        Object.entries(globals).forEach(([name, global]) => {
            const globalName = '$' + name
            globalThis[globalName] = this.app.config.globalProperties[globalName] = global
            this[name] = global
        })
    }
    getColorScheme() {
        return document.documentElement.classList.contains('dark') ? 'dark' : 'light'
    }
    getDarkMode() {
        return document.documentElement.classList.contains('dark')
    }
    setColorScheme(darkMode) {
        let html = document.documentElement
        html.classList.toggle('dark', darkMode)
        html.style.setProperty('color-scheme', darkMode ? 'dark' : null)
        localStorage.setItem('color-scheme', darkMode ? 'dark' : 'light')
    }
    getUserAvatar() {
        const theme = this.getPrefs().theme || this.getColorScheme()
        return this.resolveUrl(`/avatar/user?theme=${theme}&t=${this.state.cacheBreaker}`)
    }
    getAgentAvatar() {
        return this.getProfileAvatar(this.state.profile)
    }
    getProfileAvatar(profile) {
        if (profile && profile != 'default') {
            return this.resolveUrl(`/ext/agents/${profile}/avatar?t=${this.state.cacheBreaker}`)
        }
        return this.getDefaultAgentAvatar()
    }
    getDefaultAgentAvatar() {
        const prefs = this.getPrefs()
        const theme = prefs.theme || this.getColorScheme()
        return this.resolveUrl(`/agents/avatar?theme=${theme}&t=${this.state.cacheBreaker}`)
    }
    incCacheBreaker() {
        this.state.cacheBreaker++
    }
    getPrefsKey() {
        return this.ai.prefsKey + '.' + this.state.profile
    }
    changeProfile(profile, force = false) {
        console.log('changeProfile', profile ? JSON.stringify(Object.assign({}, profile, { prompt: profile.prompt?.substring(0, 100) }), null, 2) : 'default')
        const profileId = profile?.id || 'default'
        
        if (!force && this.state.profile === profileId) {
            localStorage.setItem('llms.profile', profileId)
            return
        }

        this.state.profile = profileId
        localStorage.setItem('llms.profile', profileId)
        Object.assign(this.prefs, storageObject(this.getPrefsKey()))

        const override = this.agents?.getProfileOverride ? this.agents.getProfileOverride(profileId) : null

        if (!profile) {
            const targetModel = override?.model || this.prefs.model
            if (targetModel) {
                this.setState({ selectedModel: targetModel })
                this.setPrefs({ model: targetModel })
            }
            const targetTheme = override?.theme || this.prefs.theme
            if (targetTheme) {
                this.selectTheme(targetTheme)
            }
            return
        }

        const prefs = this.prefs
        const targetTheme = override?.theme || profile.overrideTheme || profile.theme || prefs.theme
        if (targetTheme) {
            this.selectTheme(targetTheme)
        }

        const targetModel = override?.model || profile.overrideModel || profile.model || prefs.model
        const profilePrefs = {
            model: targetModel,
            onlyTools: profile.onlyTools !== undefined ? profile.onlyTools : prefs.onlyTools,
            onlySkills: profile.onlySkills !== undefined ? profile.onlySkills : prefs.onlySkills,
        }
        this.setPrefs(profilePrefs)
        if (targetModel) {
            this.setState({
                selectedModel: targetModel,
            })
        }
    }
    getPrefs() {
        return this.prefs
    }
    setPrefs(o) {
        storageObject(this.getPrefsKey(), Object.assign(this.prefs, o))
    }
    _validateComponents(componentMap) {
        Object.entries(componentMap).forEach(([id, def]) => {
            if (!def.component) {
                console.error(`Component Definition ${id} is missing component property`)
            }
            def.id = id
            if (!def.name) {
                def.name = humanize(id)
            }
            if (typeof def.isActive != 'function') {
                def.isActive = () => false
            }
        })
        return componentMap
    }
    setLeftTop(componentMap) {
        Object.assign(this.leftTop, this._validateComponents(componentMap))
    }
    _validateIcons(icons) {
        Object.entries(icons).forEach(([id, icon]) => {
            if (!icon.component) {
                console.error(`Icon ${id} is missing component property`)
            }
            icon.id = id
            if (!icon.name) {
                icon.name = humanize(id)
            }
            if (typeof icon.isActive != 'function') {
                icon.isActive = () => false
            }
        })
        return icons
    }
    setTopIcons(icons) {
        Object.assign(this.top, this._validateIcons(icons))
    }
    setLeftIcons(icons) {
        Object.assign(this.left, this._validateIcons(icons))
    }
    visibleComponents(componentsMap) {
        const to = {}
        Object.entries(componentsMap).forEach(([name, def]) => {
            if (typeof def.isVisible == 'function' && !def.isVisible()) {
                return
            }
            to[name] = def
        })
        return to
    }
    component(name, component) {
        if (!name) return name
        if (component) {
            this._components[name] = component
        }
        return component || this._components[name] || this.app.component(name)
    }
    components(components) {
        if (components) {
            Object.keys(components).forEach(name => {
                this._components[name] = components[name]
            })
        }
        return this._components
    }
    scope(extension) {
        return new ExtensionScope(this, extension)
    }
    modals(modals) {
        Object.keys(modals).forEach(name => {
            const modal = markRaw(modals[name])
            this.modalComponents[name] = modal
            this.component(name, modal)
        })
    }
    openModal(name) {
        const component = this.modalComponents[name]
        if (!component) {
            console.error(`Modal ${name} not found`)
            return
        }
        console.debug('openModal', name)
        this.router.push({ query: { open: name } })
        this.events.publish('modal:open', name)
        return component
    }
    closeModal(name) {
        console.debug('closeModal', name)
        this.router.push({ query: { open: undefined } })
        this.events.publish('modal:close', name)
    }
    handleKeydown(e) {
        if (e.key === 'Escape') {
            const modal = this.router.currentRoute.value?.query?.open
            if (modal) {
                this.closeModal(modal)
            }
            this.events.publish(`keydown:Escape`, e)
        }
    }
    setState(o) {
        Object.assign(this.state, o)
    }
    setLayout(o) {
        Object.assign(this.layout, o)
        storageObject(`llms.layout`, this.layout)
    }
    toggleLayout(key, toggle = undefined) {
        const hide = toggle == undefined
            ? !this.layout.hide.includes(key)
            : !toggle
        console.log('toggleLayout', key, hide)
        if (hide) {
            this.layout.hide.push(key)
        } else {
            this.layout.hide = this.layout.hide.filter(k => k != key)
        }
        storageObject(`llms.layout`, this.layout)
    }
    layoutVisible(key) {
        return !this.layout.hide.includes(key)
    }
    toggleTop(name, toggle) {
        if (toggle === false) {
            this.layout.top = undefined
        } else if (toggle === true) {
            this.layout.top = name
        } else {
            this.layout.top = this.layout.top == name ? undefined : name
        }
        storageObject(`llms.layout`, this.layout)
        console.log('toggleTop', name, toggle, this.layout.top, this.layout.top === name)
        return this.layout.top === name
    }
    matchesPath(pathA, pathB) {
        if (!pathA || !pathB) {
            return pathA === pathB
        }
        const a = pathA.startsWith(this.ai.base) ? pathA.substring(this.ai.base.length) : pathA
        const b = pathB.startsWith(this.ai.base) ? pathB.substring(this.ai.base.length) : pathB

        if (pathB.endsWith('*')) {
            return a.startsWith(pathB.substring(0, pathB.length - 1))
        }

        return a === b
    }
    togglePath(path, { left = true } = {}) {
        path = this.ai.resolvePath(path)
        const currentPath = this.router.currentRoute.value?.path
        const isSamePath = this.matchesPath(currentPath, path)
        console.log('togglePath', path, currentPath, left, isSamePath)
        if (!isSamePath) {
            this.router.push({ path })
        }
        // When navigating to a new path, show sidebar (left=true)
        // When clicking on same path, toggle the sidebar (left=undefined)
        const toggleValue = isSamePath ? undefined : left
        if (toggleValue !== undefined) {
            this.toggleLayout('left', toggleValue)
        } else {
            this.toggleLayout('left') // toggle
        }
        return this.layoutVisible('left')
    }
    setThreadHeaders(components) {
        Object.assign(this.threadHeaderComponents, components)
    }
    setThreadFooters(components) {
        Object.assign(this.threadFooterComponents, components)
    }
    setUserMenuItems(components) {
        Object.assign(this.userMenuItemComponents, components)
    }

    async createJsonResult(res) {
        return this.ai.createJsonResult(res)
    }
    createErrorStatus(status) {
        return this.ai.createErrorStatus(status)
    }
    createErrorResult(e) {
        return this.ai.createErrorResult(e)
    }
    setError(error, msg = null) {
        this.state.error = error
        if (error) {
            if (msg) {
                console.error(error.message, msg, error)
            } else {
                console.error(error.message, error)
            }
        }
    }
    clearError() {
        this.state.error = null
    }

    resolveUrl(url) {
        return this.ai.resolveUrl(url)
    }
    async getJson(url, options) {
        return await this.ai.getJson(url, options)
    }
    async post(url, options) {
        return await this.ai.post(url, options)
    }
    async postForm(url, options) {
        return await this.ai.postForm(url, options)
    }
    async postJson(url, options) {
        return await this.ai.postJson(url, options)
    }
    to(route) {
        if (typeof route == 'string') {
            route = route.startsWith(this.ai.base)
                ? route
                : combinePaths(this.ai.base, route)
            const path = { path: route }
            console.log('to', path)
            this.router.push(path)
        } else {
            route.path = route.path.startsWith(this.ai.base)
                ? route.path
                : combinePaths(this.ai.base, route.path)
            console.log('to', route)
            this.router.push(route)
        }
    }

    // Events
    onRouterBeforeEach(callback) {
        this._onRouterBeforeEach.push(callback)
    }

    onClass(callback) {
        this._onClass.push(callback)
    }

    cls(id, cls) {
        if (this._onClass.length) {
            this._onClass.forEach(callback => {
                cls = callback(id, cls) ?? cls
            })
        }
        return cls
    }
    toast(msg) {
        this.setState({ message: msg })
    }

    renderMarkdown(content) {
        if (Array.isArray(content)) {
            content = content.filter(c => c.type === 'text').map(c => c.text).join('\n')
        }
        if (content && content.startsWith('---')) {
            const headerEnd = content.indexOf('---', 3)
            const header = content.substring(3, headerEnd).trim()
            content = '<div class="frontmatter">' + header + '</div>\n' + content.substring(headerEnd + 3)
        }
        let html = content || ''
        try {
            html = this.marked.parse(content || '')
        } catch (e) {
            console.log('Failed to parse markdown, using fallback', e)
            try {
                html = this.markedFallback.parse(content || '')
            } catch (e2) {
                console.log('Failed to parse markdown, using raw content', e2)
                html = content || ''
            }
        }
        return sanitizeHtml(html)
    }

    renderContent(content) {
        // Check for HTML tags to detect HTML content
        if (isHtml(content)) {
            // If this is HTML content, return it in an iframe so it doesn't break the page
            return `<iframe src="data:text/html;charset=utf-8,${encodeURIComponent(content)}"></iframe>`
        }
        return this.renderMarkdown(content)
    }

    createChatContext({ request, thread, context, model }) {
        if (!request.messages) request.messages = []
        if (!request.metadata) request.metadata = {}
        if (!context) context = {}
        if (thread && Array.isArray(thread.tools) && thread.tools.length > 0) {
            if (!request.tools) request.tools = []
            thread.tools.forEach(t => {
                const exists = request.tools.some(existing => JSON.stringify(existing) === JSON.stringify(t))
                if (!exists) {
                    request.tools.push(t)
                }
            })
        }
        Object.assign(context, {
            systemPrompt: '',
            requiredSystemPrompts: [],
        }, context)
        return {
            request,
            thread,
            context,
            model,
        }
    }

    completeChatContext({ request, thread, context }) {

        let existingSystemPrompt = request.messages.find(m => m.role === 'system')?.content

        let existingMessages = request.messages.filter(m => m.role == 'assistant' || m.role == 'tool')
        if (existingMessages.length) {
            const messageTypes = {}
            request.messages.forEach(m => {
                messageTypes[m.role] = (messageTypes[m.role] || 0) + 1
            })
            const summary = JSON.stringify(messageTypes).replace(/"/g, '')
            console.debug(`completeChatContext(${summary})`, request)
            return
        }

        let newSystemPrompts = context.requiredSystemPrompts ?? []
        if (context.systemPrompt) {
            newSystemPrompts.push(context.systemPrompt)
        }
        if (existingSystemPrompt) {
            newSystemPrompts.push(existingSystemPrompt)
        }

        let newSystemPrompt = newSystemPrompts.join('\n\n')
        if (newSystemPrompt) {
            // add or replace system prompt
            request.messages = request.messages.filter(m => m.role !== 'system')
            request.messages.unshift({ role: 'system', content: newSystemPrompt })
        }

        console.debug(`completeChatContext()`, request, context, thread)
    }

    resolveThemes(themes) {
        const ret = {}
        for (const [id, theme] of Object.entries(themes)) {
            ret[id] = this.createTheme(theme)
        }
        return ret
    }

    createTheme(theme) {
        return this.ai.createTheme(theme)
    }

    changeTheme(theme) {
        console.log('changeTheme.1', theme)

        const fullTheme = this.createTheme(theme)
        Object.assign(this.state.theme, fullTheme)
        Object.assign(this.state.styles, fullTheme.styles)

        console.log('changeTheme.2', this.state.theme.vars, this.state.styles.bgBody)

        Object.entries(fullTheme.vars).forEach(([key, value]) => {
            if (key === 'colorScheme') {
                this.setColorScheme(value === 'dark')
            } else if (key.startsWith("--")) {
                document.documentElement.style.setProperty(key, value)
            }
        })

        document.body.className = this.state.styles.bgBody || ''
    }

    get selectedTheme() {
        return this.getPrefs().theme || (this.getDarkMode() ? 'dark' : 'light')
    }

    getTheme(theme) {
        return this.state.themes[theme]
            || this.state.themes[this.getColorScheme()]
            || this.ai.light
    }

    selectTheme(theme) {
        this.setPrefs({
            theme
        })
        this.setTheme(this.getTheme(theme))
    }

    setTheme(theme) {
        if (!theme) return
        this.changeTheme(theme)
    }
}
