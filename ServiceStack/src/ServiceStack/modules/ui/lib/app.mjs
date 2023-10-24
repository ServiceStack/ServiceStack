import { reactive } from "vue"
import { JsonServiceClient, combinePaths, lastLeftPart, trimEnd, appendQueryString, enc, queryString } from "@servicestack/client"
import ServiceStackVue, { useMetadata, useAuth, useConfig } from "@servicestack/vue"
import { App, usePageRoutes, useBreakpoints, setBodyClass, sortOps } from "core"
import { Authenticate } from "./dtos.mjs"
const { setConfig } = useConfig()
const { invalidAccessMessage, toAuth } = useAuth()
const { Crud, apiOf, typeEquals } = useMetadata()
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
        if (AppData.authsecret) c.headers.set('authsecret', AppData.authsecret)
        if (AppData.userName) c.userName = AppData.userName
        if (AppData.password) c.password = AppData.password
        let apiFmt = globalThis.Server.httpHandlers['ApiHandlers.Json']
        if (apiFmt)
            c.basePath = apiFmt.replace('/{Request}', '')
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
        change({ previous, current }) { console.debug('breakpoints.change', previous, current) } /*debug*/
    }
})
/** The App's reactive `routes` navigation component used for all App navigation
 * @type {ExplorerRoutes & ExplorerRoutesExtend & Routes} */
export let routes = usePageRoutes(app,{
    page:'op',
    queryKeys:'tab,lang,provider,preview,body,doc,detailSrc,form,response'.split(','),
    handlers: {
        nav(state) { console.debug('nav', state) } /*debug*/
    },
    extend: {
        queryHref(args) {
            let op = this.op && apiOf(this.op)
            if (op && globalThis.Server.ui.modules.indexOf('/locode') >= 0) {
                if (Crud.isQuery(op)) {
                    return `/locode/${this.op}`
                } else if (Crud.isCrud(op)) {
                    let queryOp = globalThis.Server.api.operations.find(x => Crud.isQuery(x) && typeEquals(op.dataModel,x.dataModel))
                    if (queryOp)
                        return urlWithState(appendQueryString(`/locode/${queryOp.request.name}`, args || {}))
                }
            }
            return ''
        },
    }
})
export const sideNav = ((Server) => {
    const operations = Server.api.operations
    operations.forEach(op => {
        if (!op.tags) op.tags = []
    })
    let appOps = operations.filter(op => !(op.request.namespace || "").startsWith('ServiceStack'))
    let appTags = Array.from(new Set(appOps.flatMap(op => op.tags))).sort()
    /** @type {{expanded: boolean, operations: MetadataOperationType[], tag: string}[]} */
    let sideNav = appTags.map(tag => ({
        tag,
        expanded: true,
        operations: appOps.filter(op => op.tags.indexOf(tag) >= 0)
    }))
    let ssOps = operations.filter(op => (op.request.namespace || "").startsWith('ServiceStack'))
    let ssTags = Array.from(new Set(ssOps.flatMap(op => op.tags))).sort()
    ssTags.map(tag => ({
        tag,
        expanded: true,
        operations: ssOps.filter(op => op.tags.indexOf(tag) >= 0)
    })).forEach(nav => sideNav.push(nav))
    let tags = Server.ui.explorer.tags
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
let cleanSrc = src => src.trim();
/** App's primary reactive store maintaining global functionality for API Explorer 
 * @type {ExplorerStore} */
let store = {
    /** @type {string|null} */
    previewResult: null,
    copied: false,
    /** @type {string} */
    filter: '',
    sideNav,
    detailSrcResult: {},
    /** @type {boolean} */
    debug: globalThis.Server.config.debugMode,
    /** @type {ApiResult<AuthenticateResponse>} */
    api: null,
    /** @type {AuthenticateResponse} */
    auth: window.AUTH,
    /** @type {string} */
    baseUrl: BASE_URL,
    
    get plugins() { return globalThis.Server.plugins },
    /** @return {string} */
    get useLang() { return routes.lang || 'csharp' },
    init() {
        this.loadDetailSrc()
        this.loadLang()
        this.loadPreview()
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
    loadLang() {
        if (!this.activeLangSrc) {
            let cache = this.langCache()
            if (AppData.cache[cache.url]) {
                this.langResult = { cache, result: AppData.cache[cache.url] }
            } else {
                this.cachedFetch(cache.url)
                    .then(src => {
                        this.langResult = { cache, result: AppData.cache[cache.url] = cleanSrc(src) }
                        if (!this.activeLangSrc) {
                            this.loadLang()
                        }
                    })
            }
        }
    },
    /** @param {string} types 
     *  @returns {string} */
    getTypeUrl(types) { return `/types/csharp?IncludeTypes=${types}&WithoutOptions=true&MakeVirtual=false&MakePartial=false&AddServiceStackTypes=true` },
    /** @return {{preview:string,url:string,lang:string}|null} */
    get previewCache() {
        if (routes.preview.startsWith('types.')) {
            let types = routes.preview.substring('types.'.length)
            return { preview: routes.preview, url: this.getTypeUrl(types), lang:'csharp' }
        }
        return null
    },
    loadPreview() {
        if (!this.previewSrc) {
            let cache = this.previewCache
            if (!cache) return
            if (AppData.cache[cache.url]) {
                this.previewResult = { type:'src', ...cache, result: AppData.cache[cache.url] }
            } else {
                this.cachedFetch(cache.url)
                    .then(src => {
                        this.previewResult = {
                            type:'src',
                            ...cache,
                            result: AppData.cache[cache.url] = cache.lang ? cleanSrc(src) : src
                        }
                    })
            }
        }
    },
    /** @return {string} */
    get previewSrc() {
        let r = this.previewResult
        if (!r) return ''
        return routes.preview === r.preview && r.type === 'src' && r.lang ? r.result : ''
    },
    /** @return {string|null} */
    get activeLangSrc() {
        let cache = this.langResult && this.langResult.cache
        let ret = cache && routes.op === cache.op && this.useLang === cache.lang ? this.langResult.result : null
        return ret
    },
    loadDetailSrc() {
        if (!routes.detailSrc) return
        let cache = { url: this.getTypeUrl(routes.detailSrc) }
        if (AppData.cache[cache.url]) {
            this.detailSrcResult[cache.url] = { ...cache, result: AppData.cache[cache.url] }
        } else {
            this.cachedFetch(cache.url).then(src => {
                this.detailSrcResult[cache.url] = { ...cache, result: AppData.cache[cache.url] = cleanSrc(src) }
            })
        }
    },
    /** @return {string} */
    get activeDetailSrc() { return routes.detailSrc && this.detailSrcResult[this.getTypeUrl(routes.detailSrc)] },
    /** @return {MetadataOperationType|null} */
    get op() {
        return routes.op ? globalThis.Server.api.operations.find(op => op.request.name === routes.op) : null
    },
    /** @return {string} */
    get opName() { return this.op && this.op.request.name },
    /** @return {{[index:string]:string}} */
    get opTabs() {
        return this.op
            ? { ['API']:'', 'Details':'details', ['Code']:'code' }
            : {}
    },
    /** @return {boolean} */
    get isServiceStackType() {
        return this.op && (this.op.request.namespace || "").startsWith("ServiceStack")
    },
    /** @return {{op:string,lang:string,url:string}} */
    langCache() {
        let op = routes.op, lang = this.useLang
        return { op, lang, url: `/types/${lang}?IncludeTypes=${op}.*&WithoutOptions=true&MakeVirtual=false&MakePartial=false` + (this.isServiceStackType ? '&AddServiceStackTypes=true' : '') }
    },
    /** @param {string} url
     *  @returns {Promise<string>} */
    cachedFetch(url) {
        return new Promise((resolve,reject) => {
            let src = AppData.cache[url]
            if (src) {
                resolve(src)
            } else {
                if (url[0] === '/') {
                    url = combinePaths(BASE_URL, url)
                }
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
    /** @return {string|null} */
    get authProfileUrl() { return this.auth && this.auth.profileUrl },
    /** @return {LinkInfo[]} */
    get authLinks() {
        let to = []
        let roleLinks = this.auth && globalThis.Server.plugins.auth && globalThis.Server.plugins.auth.roleLinks || {} 
        if (Object.keys(roleLinks).length > 0) {
            this.authRoles.forEach(role => {
                if (!roleLinks[role]) return;
                roleLinks[role].forEach(link => to.push(Object.assign(link, { href: urlWithState(link.href) })))
            })
        }
        return to
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
}
store = reactive(store)
export { store }
app.events.subscribe('route:nav', args => store.init())
app.use(ServiceStackVue)
app.component('RouterLink', ServiceStackVue.component('RouterLink'))
app.provides({ app, client, store, routes, breakpoints, server:globalThis.Server })
app.directive('highlightjs', (el, binding) => {
    if (binding.value) {
        //el.className = ''
        el.innerHTML = enc(binding.value)
        globalThis.hljs.highlightElement(el)
    }
})
app.directive('hash', (el,binding) => {
    /** @param {Event} e */
    el.onclick = (e) => {
        e.preventDefault()
        location.hash = binding.value
    }
})
setConfig({
    navigate: (url) => {
        console.debug('navigate', url)
        if (url.startsWith('/signin')) {
            routes.to({ op:'', tab:'', lang:'', provider:'', preview:'', body:'', doc:'', detailSrc:'', form:'', response:'' })
        } else {
            location.href = url
        }
    }
})
