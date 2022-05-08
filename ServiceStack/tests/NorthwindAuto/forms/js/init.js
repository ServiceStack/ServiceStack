import { Authenticate, Server } from "../../lib/types"
import { JsonServiceClient, lastLeftPart, trimEnd } from "@servicestack/client";
import { createForms, createMeta } from "../../shared/js/createForms";

let BASE_URL = lastLeftPart(trimEnd(document.baseURI,'/'),'/')
/** @type {string|null} */
let bearerToken = null
/** @type {string|null} */
let authsecret = null

function createClient(fn) {
    return new JsonServiceClient(BASE_URL).apply(c => {
        c.bearerToken = bearerToken
        c.enableAutoRefreshToken = false
        if (authsecret) c.headers.set('authsecret', authsecret)
        let apiFmt = Server.httpHandlers['ApiHandlers.Json']
        if (apiFmt)
            c.basePath = apiFmt.replace('/{Request}', '')
        if (fn) fn(c)
    })
}

/** App's pre-configured `JsonServiceClient` instance for making typed API requests */
export let client = createClient()

let appName = 'admin-ui'
export let Meta = createMeta(Server, appName)
export let Forms = createForms(Meta, Server.plugins.adminUsers.css, Server.ui)

/**
 * @type {Routes}
 */
export let routes = usePageRoutes(App,{
    page:'op',
    queryKeys:'tab,provider,preview,body,doc,skip,new,edit'.split(','),
    handlers: {
        nav(state) { console.log('nav', state) } /*debug*/
    }
})

export let store = App.reactive({
    copied: false,
    filter: '',
    debug: Server.config.debugMode,
    api: null,
    auth: window.AUTH,
    baseUrl: BASE_URL,
    /** @return {MetadataOperationType} */
    get op() { return routes.op ? Server.api.operations.find(op => op.request.name === routes.op) : null },
    /** @return {string} */
    get opName() { return this.op && this.op.request.name },

    init() {}    
})

App.events.subscribe('route:nav', args => store.init())
