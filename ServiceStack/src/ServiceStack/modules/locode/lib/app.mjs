import { reactive, h } from "vue"
import { createRouter, createWebHistory } from "vue-router"
import {
    JsonServiceClient,
    map,
    lastLeftPart,
    trimEnd,
    appendQueryString,
    humanify,
    queryString,
    sanitize,
} from "@servicestack/client"
import ServiceStackVue, { useMetadata, useAuth, useConfig } from "@servicestack/vue"
import { App, useBreakpoints, setBodyClass, sortOps } from "core"
import { Authenticate } from "./dtos.mjs"
const { setConfig } = useConfig()
const { invalidAccessMessage, toAuth } = useAuth()
const { Crud, apiOf } = useMetadata()
let BASE_URL = lastLeftPart(trimEnd(document.baseURI, '/'), '/')
export let AppData = {
    init: false,
    baseUrl: BASE_URL,
    /** @type {string|null} */
    bearerToken: null,
    /** @type {string|null} */
    authsecret: null,
    /** @type {string|null} */
    userName: null,
    /** @type {string|null} */
    password: null,
    /** @type {() => void|null} */
    onRoutesEditChange: null,
    /** @type {string|null} */
    lastEditState: null,
    /** @type {Object<String,any>} */
    cache: {},
    /** @type Record<number,string> */
    HttpErrors: { 401:'Unauthorized', 403:'Forbidden' },
}
export const app = new App()
const server = globalThis.Server
/**
 * @param {RequestInit} req
 */
function clientRequestFilter(req) {
    if (store.apikey) {
        const httpHeader = store.plugins.apiKey?.httpHeader ?? 'X-Api-Key'
        req.headers.set(httpHeader, store.apikey)
    }
}
/**
 * Create a new `JsonServiceStack` client instance configured with the authenticated user
 *
 * @remarks
 * For typical API requests it's recommended to use the UIs pre-configured **client** instance
 *
 * @param {Function} [fn]
 * @return {JsonServiceClient}
 */
export function createClient(fn) {
    return new JsonServiceClient(BASE_URL).apply(c => {
        c.bearerToken = AppData.bearerToken
        c.enableAutoRefreshToken = false
        c.requestFilter = clientRequestFilter
        if (AppData.authsecret) c.headers.set('authsecret', AppData.authsecret)
        if (AppData.userName) c.userName = AppData.userName
        if (AppData.password) c.password = AppData.password
        let apiFmt = globalThis.Server.httpHandlers['ApiHandlers.Json']
        c.basePath = apiFmt ? apiFmt.replace('/{Request}', '') : null
        if (fn) fn(c)
    })
}
/** App's pre-configured `JsonServiceClient` instance for making typed API requests */
export const client = createClient()
/** @type {Object<String,String>} */
let qs = queryString(location.search)
let stateQs = qs.IncludeTypes ? `?IncludeTypes=${qs.IncludeTypes}` : ''
function urlWithState(url) {
    if (!url) return url
    let alreadyHasState = url.indexOf('IncludeTypes') >= 0
    let isBuiltinUi = url.indexOf('/ui') >= 0 || url.indexOf('/locode') >= 0 || url.indexOf('/admin-ui') >= 0
    if (!isBuiltinUi || alreadyHasState) return url
    return url + (url.indexOf('?') >= 0
        ? (stateQs ? '&' + stateQs.substring(1) : '')
        : stateQs)
}
export const breakpoints = useBreakpoints(app, {
    handlers: {
        //change({ previous, current }) { console.debug('breakpoints.change', previous, current) } /*debug*/
    }
})
// Create Vue Router instance
export const router = createRouter({
    history: createWebHistory('/locode'),
    routes: [
        {
            path: '/',
            name: 'welcome',
            component: {
                setup() {
                    return () => h(app.component('Welcome'))
                }
            }
        },
        {
            path: '/:op',
            name: 'operation',
            component: {
                setup() {
                    return () => h(app.component('OperationPage'))
                }
            }
        }
    ]
})
// Reactive routes object for backward compatibility
export const routes = reactive({
    // Current route params
    get op() { return router.currentRoute.value.params.op || '' },
    get tab() { return router.currentRoute.value.query.tab || '' },
    get provider() { return router.currentRoute.value.query.provider || '' },
    get preview() { return router.currentRoute.value.query.preview || '' },
    get body() { return router.currentRoute.value.query.body || '' },
    get doc() { return router.currentRoute.value.query.doc || '' },
    get skip() { return router.currentRoute.value.query.skip || '' },
    get new() { return router.currentRoute.value.query.new || '' },
    get edit() { return router.currentRoute.value.query.edit || '' },
    get dialog() { return router.currentRoute.value.query.dialog || '' },
    // Navigation methods
    to(args) {
        const op = args.$page !== undefined ? args.$page : (args.op || this.op)
        const query = {}
        const queryKeys = ['tab', 'provider', 'preview', 'body', 'doc', 'skip', 'new', 'edit', 'dialog']
        queryKeys.forEach(key => {
            if (args[key] !== undefined) {
                query[key] = args[key]
            } else if (!args.$clear && this[key]) {
                query[key] = this[key]
            }
        })
        // Remove empty query params
        Object.keys(query).forEach(key => {
            if (!query[key]) delete query[key]
        })
        if (op) {
            router.push({ name: 'operation', params: { op }, query })
        } else {
            router.push({ name: 'welcome', query })
        }
    },
    href(args) {
        const op = args?.$page !== undefined ? args.$page : (args?.op || this.op)
        const query = { ...router.currentRoute.value.query }
        if (args) {
            Object.keys(args).forEach(key => {
                if (key !== '$page' && key !== 'op') {
                    query[key] = args[key]
                }
            })
        }
        // Remove empty query params
        Object.keys(query).forEach(key => {
            if (!query[key]) delete query[key]
        })
        if (op) {
            return router.resolve({ name: 'operation', params: { op }, query }).href
        } else {
            return router.resolve({ name: 'welcome', query }).href
        }
    },
    get state() {
        return {
            op: this.op,
            tab: this.tab,
            provider: this.provider,
            preview: this.preview,
            body: this.body,
            doc: this.doc,
            skip: this.skip,
            new: this.new,
            edit: this.edit,
            dialog: this.dialog
        }
    },
    // UI Explorer link (if available)
    uiHref(args) {
        if (!this.op || !globalThis.Server.ui.modules.includes('/ui')) {
            return ''
        }
        const query = args ? { ...args } : {}
        let qs = Object.keys(query).map(k => `${encodeURIComponent(k)}=${encodeURIComponent(query[k])}`).join('&')
        return `/ui/${this.op}${qs ? '?' + qs : ''}`
    }
})
// Add v-href directive for backward compatibility
app.directive('href', function(el, binding) {
    el.href = routes.href(binding.value)
    el.onclick = e => {
        e.preventDefault()
        routes.to(binding.value)
    }
})
/** Manage users query & filter preferences in the Users browsers localStorage
 * @type {LocodeSettings} */
export const settings = {
    events: {
        /** @param {string} op */
        op(op) {
            return `settings:${op}`
        },
        /** @param {string} op */
        lookup(op) {
            return `settings:lookup:${op}`
        },
        /** @param {string} op
         *  @param {string} name */
        opProp(op, name) {
            return `settings:${op}.${name}`
        },
    },
    /** @param {string} op */
    op(op) {
        return Object.assign({take: 25, selectedColumns: []},
            map(localStorage.getItem(`locode/op:${op}`), x => JSON.parse(x)))
    },
    /** @param {string} op */
    lookup(op) {
        return Object.assign({take: 10, selectedColumns: []},
            map(localStorage.getItem(`locode/lookup:${op}`), x => JSON.parse(x)))
    },
    /** @param {string} op
     *  @param {Function} fn */
    saveOp(op, fn) {
        let setting = this.op(op)
        fn(setting)
        localStorage.setItem(`locode/op:${op}`, JSON.stringify(setting))
        app.events.publish(this.events.op(op), setting)
    },
    /** @param {string} op
     *  @param {Function} fn */
    saveLookup(op, fn) {
        let setting = this.lookup(op)
        fn(setting)
        localStorage.setItem(`locode/lookup:${op}`, JSON.stringify(setting))
        app.events.publish(this.events.lookup(op), setting)
    },
    /** @param {string} op
     *  @param {string} name */
    opProp(op, name) {
        return Object.assign({sort: null, filters: []},
            map(localStorage.getItem(`locode/op:${op}.${name}`), x => JSON.parse(x)))
    },
    /** @param {string} op
     *  @param {string} name
     *  @param {Function} fn */
    saveOpProp(op, name, fn) {
        let setting = this.opProp(op, name)
        fn(setting)
        localStorage.setItem(`locode/op:${op}.${name}`, JSON.stringify(setting))
        app.events.publish(this.events.opProp(op, name), setting)
    },
    /** @param {string} op */
    hasPrefs(op) {
        let prefixes = [`locode/op:${op}`, `locode/lookup:${op}`]
        return Object.keys(localStorage).some(k => prefixes.some(p => k.startsWith(p)))
    },
    /** @param {string} op */
    clearPrefs(op) {
        let prefixes = [`locode/op:${op}`, `locode/lookup:${op}`]
        let removeKeys = Object.keys(localStorage).filter(k => prefixes.some(p => k.startsWith(p)))
        removeKeys.forEach(k => localStorage.removeItem(k))
    }
}
export const sideNav = ((Server) => {
    const operations = Server.api.operations 
    operations.forEach(op => {
        if (!op.tags) op.tags = []
    })
    let appOps = operations.filter(op => !(op.request.namespace || "").startsWith('ServiceStack') && Crud.isAnyQuery(op))
    let appTags = Array.from(new Set(appOps.flatMap(op => op.tags))).sort()
    /** Organized data structure to render Sidebar
     * @remarks
     * @type {{expanded: boolean, operations: MetadataOperationType[], tag: string}[]} */
    let sideNav = appTags.map(tag => ({
        tag,
        expanded: true,
        operations: appOps.filter(op => op.tags.indexOf(tag) >= 0)
    }))
    let ssOps = operations.filter(op => (op.request.namespace || "").startsWith('ServiceStack') && Crud.isAnyQuery(op))
    let ssTags = Array.from(new Set(ssOps.flatMap(op => op.tags))).sort()
    ssTags.map(tag => ({
        tag,
        expanded: true,
        operations: ssOps.filter(op => op.tags.indexOf(tag) >= 0)
    })).forEach(nav => sideNav.push(nav))
    let tags = Server.ui.locode.tags
    let other = {
        tag: appTags.length > 0 ? tags.other : tags.default,
        expanded: true,
        operations: [...appOps, ...ssOps].filter(op => op.tags.length === 0)
    }
    if (other.operations.length > 0) sideNav.push(other)
    let alwaysHideTags = Server.ui.alwaysHideTags || !DEBUG && Server.ui.hideTags
    if (alwaysHideTags) {
        sideNav = sideNav.filter(group => alwaysHideTags.indexOf(group.tag) < 0)
    }
    
    return sideNav
})(globalThis.Server);
/** App's primary reactive store maintaining global functionality for Locode Apps
 * @type {LocodeStore} */
let store = {
    /** @type {string|null} */
    previewResult: null,
    copied: false,
    /** @type {string} */
    filter: '',
    sideNav,
    detailSrcResult: {},
    /** @type {string} */
    serviceName: globalThis.Server.app.serviceName,
    /** @type {PluginInfo} */
    plugins: globalThis.Server.plugins,
    /** @type {boolean} */
    debug: globalThis.Server.config.debugMode,
    /** @type {ApiResult<AuthenticateResponse>} */
    api: null,
    config: globalThis.config,
    /** @type {AuthenticateResponse} */
    auth: window.AUTH,
    /** @type {string} */
    baseUrl: BASE_URL,
    /** @type {any|null} */
    modalLookup: null,
    /** @return {string} */
    get useLang() { return 'csharp' },
    init() {
        setBodyClass({ page: routes.op })
    },
    /** @return {{tag:string,operations:MetadataOperationType[],expanded:boolean}[]} */
    get filteredSideNav() {
        let filter = op => {
            let lowerFilter = this.filter.toLowerCase()
            if (op.requiresAuth && !this.debug)
            {
                if (!this.auth)
                    return false
                if (invalidAccessMessage(op, this.auth))
                    return false
            }
            return !lowerFilter || op.request.name.toLowerCase().indexOf(lowerFilter) >= 0
        }
        let ret = this.sideNav.filter(nav => nav.operations.some(filter))
            .map(nav => ({
                ...nav,
                operations: sortOps(nav.operations.filter(filter))
            }))
        return ret
    },
    /** @param {string} tag */
    toggle(tag) {
        let nav = this.sideNav.find(x => x.tag === tag)
        nav.expanded = !nav.expanded
    },
    /** @return {MetadataOperationType} */
    get op() { return routes.op ? apiOf(routes.op) : null },
    /** @return {string} */
    get opName() { return this.op && this.op.request.name },
    /** @return {string} */
    get opDesc() { return this.op && (this.op.request.description || humanify(this.op.request.name)) },
    /** @return {string} */
    get opDataModel() {
        const result = this.op && this.op.dataModel && this.op.dataModel.name
        console.log('opDataModel accessed:', result)
        console.trace('opDataModel stack')
        return result
    },
    /** @return {string} */
    get opViewModel() { return this.op && this.op.viewModel && this.op.viewModel.name },
    /** @return {boolean} */
    get isServiceStackType() { return this.op && (this.op.request.namespace || "").startsWith("ServiceStack") },
    /** @param {string} url
     *  @returns {Promise<string>} */
    cachedFetch(url) {
        return new Promise((resolve,reject) => {
            let src = AppData.cache[url]
            if (src) {
                resolve(src)
            } else {
                fetch(url)
                    .then(r => {
                        if (r.ok) return r.text()
                        else throw r.statusText
                    })
                    .then(src => {
                        resolve(AppData.cache[url] = src)
                    })
                    .catch(e => {
                        console.error(`fetchCache (${url}):`, e)
                        reject(e)
                    })
            }
        })
    },
    /** @param {AuthenticateResponse} auth */
    login(auth) {
        auth = sanitize(auth)
        globalThis.AUTH = this.auth = toAuth(auth)
        AppData.bearerToken = AppData.authsecret = AppData.userName = AppData.password = null
        if (auth.bearerToken) {
            AppData.bearerToken = client.bearerToken = auth.bearerToken
        }
        if (client.userName) AppData.userName = client.userName
        if (client.password) AppData.password = client.password
        setBodyClass({ auth: this.auth })
    },
    logout() {
        globalThis.AUTH = this.auth = AppData.authsecret = AppData.bearerToken = client.bearerToken = AppData.userName = AppData.password = null
        setBodyClass({ auth: null })
        client.api(new Authenticate({ provider: 'logout' }))
        client.headers.delete('authsecret')
        client.userName = client.password = null
        routes.to({ $page:null })
    },
    /** @return {string[]} */
    get authRoles() { return this.auth && this.auth.roles || [] },
    /** @return {string[]} */
    get authPermissions() { return this.auth && this.auth.permissions || [] },
    get defaultUserUri() { return 'data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIzMiIgaGVpZ2h0PSIzMiIgdmlld0JveD0iMCAwIDMyIDMyIj48cGF0aCBmaWxsPSIjNGE1NTY1IiBkPSJNMTYgOGE1IDUgMCAxIDAgNSA1YTUgNSAwIDAgMC01LTUiLz48cGF0aCBmaWxsPSIjNGE1NTY1IiBkPSJNMTYgMmExNCAxNCAwIDEgMCAxNCAxNEExNC4wMTYgMTQuMDE2IDAgMCAwIDE2IDJtNy45OTMgMjIuOTI2QTUgNSAwIDAgMCAxOSAyMGgtNmE1IDUgMCAwIDAtNC45OTIgNC45MjZhMTIgMTIgMCAxIDEgMTUuOTg1IDAiLz48L3N2Zz4=' },
    get userIconUri() { return server.ui.userIcon?.uri || this.defaultUserUri },
    get authProfileUrl() { return this.auth && this.auth.profileUrl || this.userIconUri },
    /** @return {LinkInfo[]} */
    get authLinks() {
        let to = []
        let roleLinks = this.auth && globalThis.Server.plugins.auth && globalThis.Server.plugins.auth.roleLinks || {}
        if (Object.keys(roleLinks).length > 0) {
            this.authRoles.forEach(role => {
                if (!roleLinks[role]) return
                roleLinks[role].forEach(link => to.push(Object.assign(link, { href: urlWithState(link.href) })))
            })
        }
        const preferredOrder = globalThis.Server.ui.adminLinksOrder || []
        const adminLinksSorted = preferredOrder.map(id => {
            return to.find(link => link.id === id)
        }).filter(link => link !== undefined)
        const adminLinksUnsorted = to.filter(link => !preferredOrder.includes(link.id))
        return [...adminLinksSorted, ...adminLinksUnsorted]
    },
    /** @return {string|null} */
    get displayName() {
        let auth = this.auth
        return auth
            ? auth.displayName || (auth.firstName ? `${auth.firstName} ${auth.lastName}` : null) || auth.userName || auth.email
            : null
    },
    /** @return {string|null} */
    invalidAccess() { return invalidAccessMessage(this.op, this.auth) },
    /** @param {string} role
     *  @return {boolean} */
    hasRole(role) { return this.auth && this.auth.roles.indexOf(role) >= 0 },
    pageComponentFor(dataModel) {
        return app.component(dataModel + 'Page')
    }
}
store = reactive(store)
export { store }
// Initialize store when route changes
router.afterEach((to, from) => {
    store.init()
    app.events.publish('route:nav', { op: to.params.op, ...to.query })
})
app.use(ServiceStackVue)
app.use(router)
app.component('RouterLink', ServiceStackVue.component('RouterLink'))
app.provides({ app, client, store, routes, router, breakpoints, settings, server:globalThis.Server })
setConfig({
    navigate: (url) => {
        console.debug('navigate', url)
        if (typeof url === 'string') {
            if (url.startsWith('/signin')) {
                router.push({ name: 'welcome', query: {} })
            } else {
                location.href = url
            }
        } else if (url && typeof url === 'object') {
            // Handle object-based navigation (e.g., { name: 'welcome' })
            router.push(url)
        }
    }
})
