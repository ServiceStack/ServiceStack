import { reactive } from "vue"
import {
    JsonServiceClient,
    map,
    lastLeftPart,
    trimEnd,
    appendQueryString,
    humanify,
    queryString,
    enc
} from "@servicestack/client"
import ServiceStackVue, { useMetadata, useAuth, useConfig, useUtils } from "@servicestack/vue"
import { App, usePageRoutes, useBreakpoints, setBodyClass, sortOps } from "core"
import { Authenticate } from "./dtos.mjs"

const { setConfig } = useConfig()
const { invalidAccessMessage } = useAuth()
const { Crud, apiOf } = useMetadata()

let BASE_URL = lastLeftPart(trimEnd(document.baseURI, '/'), '/')

export let AppData = {
    init: false,
    baseUrl: BASE_URL,
    /** @type {string|null} */
    bearerToken: null,
    /** @type {string|null} */
    authsecret: null,
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
        let apiFmt = server.httpHandlers['ApiHandlers.Json']
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
export function urlWithState(url) {
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


/**
 * The App's reactive `routes` navigation component used for all App navigation
 * @remarks
 * @type {AdminRoutes & Routes}
 */
export let routes = usePageRoutes(app, {
    page:'admin',
    queryKeys: ('tab,provider,db,schema,table,q,page,sort,new,edit,op,skip,' +
        'show,orderBy,operationName,userAuthId,sessionId,pathInfo,ipAddress,referer,forwardedFor,hasResponse,withErrors,' +
        'source,threadId,eventType,traceId,userId,tag,body').split(','),
    handlers: {
        nav(state) { console.debug('nav', state) } /*debug*/
    },
    extend: {
        dbTable() {
            return this.table && `${this.db}.${this.schema}.${this.table}` || ''
        }
    },
})

/** @param {KeyboardEvent} e */
export function hasModifierKey(e) {
    return e.shiftKey || e.ctrlKey || e.altKey || e.metaKey || e.code === 'MetaLeft' || e.code === 'MetaRight'
}
/** Is element an Input control
 * @param {Element} e */
let InputTags = 'INPUT,SELECT,TEXTAREA'.split(',')
export function isInput(e) {
    return e && InputTags.indexOf(e.tagName) >= 0
}

export function keydown(e, ctx) {
    const { unRefs } = useUtils()
    const { canPrev, canNext, nextSkip, take, results, selected, clearFilters } = unRefs(ctx)
    if (hasModifierKey(e) || isInput(e.target) || results.length === 0) return
    if (e.key === 'Escape') {
        clearFilters()
        return
    }
    if (e.key === 'ArrowLeft' && canPrev) {
        routes.to({ skip:nextSkip(-take) })
        return
    } else if (e.key === 'ArrowRight' && canNext) {
        routes.to({ skip:nextSkip(take) })
        return
    }
    let row = selected
    if (!row) return routes.to({ show:map(results[0], x => x.id) || '' })
    let activeIndex = results.findIndex(x => x.id === row.id)
    let navs = {
        ArrowUp:   activeIndex - 1,
        ArrowDown: activeIndex + 1,
        Home: 0,
        End: results.length -1,
    }
    let nextIndex = navs[e.key]
    if (nextIndex != null) {
        if (nextIndex === -1) nextIndex = results.length - 1
        routes.to({ show: map(results[nextIndex % results.length], x => x.id) })
        if (e.key.startsWith('Arrow')) {
            e.preventDefault()
        }
    }
}

/** Manage users query & filter preferences in the Users browsers localStorage */
export let settings = {
    events: {
        /** @param {string} op */
        table(table) { return `settings:table:${table}` },
        lookup(table) { return `settings:table:lookup:${table}` },
        /** @param {string} table
         *  @param {string} name */
        tableProp(table, name) { return `settings:table:${table}.${name}` },
    },
    /** @param {string} table */
    table(table) {
        return Object.assign({ take:25, selectedColumns:[] },
            map(localStorage.getItem(`admin/table:${table}`), x => JSON.parse(x)))
    },
    /** @param {string} table */
    lookup(table) {
        return Object.assign({ take:10, selectedColumns:[] },
            map(localStorage.getItem(`admin/lookup:${table}`), x => JSON.parse(x)))
    },
    /** @param {string} table
     *  @param {Function} fn */
    saveTable(table, fn) {
        let setting = this.table(table)
        fn(setting)
        localStorage.setItem(`admin/table:${table}`, JSON.stringify(setting))
        app.events.publish(this.events.table(table), setting)
    },
    /** @param {string} table
     *  @param {Function} fn */
    saveLookup(table, fn) {
        let setting = this.lookup(table)
        fn(setting)
        localStorage.setItem(`admin/lookup:${table}`, JSON.stringify(setting))
        app.events.publish(this.events.lookup(table), setting)
    },
    /** @param {string} table
     *  @param {string} name */
    tableProp(table, name) {
        return Object.assign({ sort:null, filters:[] },
            map(localStorage.getItem(`admin/table:${table}.${name}`), x => JSON.parse(x)))
    },
    /** @param {string} table
     *  @param {string} name
     *  @param {Function} fn */
    saveTableProp(table, name, fn) {
        let setting = this.tableProp(table, name)
        fn(setting)
        localStorage.setItem(`admin/table:${table}.${name}`, JSON.stringify(setting))
        app.events.publish(this.events.tableProp(table,name), setting)
    },
    /** @param {string} table */
    hasPrefs(table) {
        let prefixes = [`admin/table:${table}`,`admin/lookup:${table}`]
        return Object.keys(localStorage).some(k => prefixes.some(p => k.startsWith(p)))
    },
    /** @param {string} table */
    clearPrefs(table) {
        let prefixes = [`admin/table:${table}`,`admin/lookup:${table}`]
        let removeKeys = Object.keys(localStorage).filter(k => prefixes.some(p => k.startsWith(p)))
        removeKeys.forEach(k => localStorage.removeItem(k))
    }
}

/**
 * App's primary reactive store maintaining global functionality for Admin UI
 * @remarks
 * @type {AdminStore}
 */
let store = {
    copied: false,
    filter: '',
    debug: server.config.debugMode,
    api: null,
    auth: window.AUTH,
    baseUrl: BASE_URL,
    allTypes:  [...server.api.operations.map(x => x.request), 
                ...server.api.operations.map(x => x.response), 
                ...server.api.types.map(x => x)]
        .filter(x => x).reduce((acc,x) => { acc[x.name] = x; return acc }, {}),

    init() {
        setBodyClass({ page: routes.admin })
    },

    get adminUsers() { return server.plugins.adminUsers },

    /** @param {string|any} id
     *  @return {LinkInfo} */
    adminLink(id) { return server.ui.adminLinks.find(x => x.id === id) },

    get adminLinks() { return server.ui.adminLinks },

    get link() { return this.adminLink(routes.admin) },

    /** @param {string} url
     *  @return {Promise<any>} */
    cachedFetch(url) {
        return new Promise((resolve,reject) => {
            let src = Meta.CACHE[url]
            if (src) {
                resolve(src)
            } else {
                fetch(url)
                    .then(r => {
                        if (r.ok) return r.text()
                        else throw r.statusText
                    })
                    .then(src => {
                        resolve(Meta.CACHE[url] = src)
                    })
                    .catch(e => {
                        console.error(`fetchCache (${url}):`, e)
                        reject(e)
                    })
            }
        })
    },

    /** @param {any} args */
    login(args) {
        let provider = routes.provider || 'credentials'
        let authProvider = server.plugins.auth.authProviders.find(x => x.name === provider)
            || server.plugins.auth.authProviders[0]
        if (!authProvider)
            throw new Error("!authProvider")
        let auth = new Authenticate()
        AppData.bearerToken = AppData.authsecret = null
        if (authProvider.type === 'Bearer') {
            AppData.bearerToken = client.bearerToken = (args['BearerToken'] || '').trim()
        } else if (authProvider.type === 'authsecret') {
            AppData.authsecret = (args['authsecret'] || '').trim()
            client.headers.set('authsecret',AppData.authsecret)
        } else {
            auth = new Authenticate({ provider, ...args })
        }
        client.api(auth, { jsconfig: 'eccn' })
            .then(r => {
                this.api = r
                if (r.error && !r.error.message)
                    r.error.message = Meta.HttpErrors[r.errorCode] || r.errorCode
                if (this.api.succeeded) {
                    this.auth = this.api.response
                    setBodyClass({ auth: this.auth })
                }
            })
    },

    logout() {
        setBodyClass({ auth: this.auth })
        client.api(new Authenticate({ provider: 'logout' }))
        AppData.authsecret = AppData.bearerToken = client.bearerToken = null
        client.headers.delete('authsecret')
        this.auth = null
        routes.to({ $page:null })
    },

    /**: v-if doesn't protect against nested access so need to guard against deep NRE access */
    get authRoles() { return this.auth && this.auth.roles || [] },
    get authPermissions() { return this.auth && this.auth.permissions || [] },
    get authProfileUrl() { return this.auth && this.auth.profileUrl },
    get isAdmin() { return this.authRoles.indexOf('Admin') >= 0 },

    /** @return {LinkInfo[]} */
    get authLinks() {
        let to = []
        let roleLinks = this.auth && server.plugins.auth && server.plugins.auth.roleLinks || {}
        if (Object.keys(roleLinks).length > 0) {
            this.authRoles.forEach(role => {
                if (!roleLinks[role]) return;
                roleLinks[role].forEach(link => to.push(Object.assign(link, { href: Meta.urlWithState(link.href) })))
            })
        }
        return to
    },

    get displayName() {
        let auth = this.auth
        return auth
            ? auth.displayName || (auth.firstName ? `${auth.firstName} ${auth.lastName}` : null) || auth.userName || auth.email
            : null
    },
}
store = reactive(store)
export { store }

app.subscribe('route:nav', args => store.init())

app.use(ServiceStackVue)
app.component('RouterLink', ServiceStackVue.component('RouterLink'))
app.provides({ app, server, client, store, routes, breakpoints, settings })
app.directive('highlightjs', (el, binding) => {
    if (binding.value) {
        //el.className = ''
        el.innerHTML = enc(binding.value)
        globalThis.hljs.highlightElement(el)
    }
})


setConfig({
    navigate: (url) => {
        console.debug('navigate', url)
        if (url.startsWith('/signin')) {
            routes.to({ op:'', provider:'', skip:'', preview:'', new:'', edit:'' })
        } else {
            location.href = url
        }
    }
})

