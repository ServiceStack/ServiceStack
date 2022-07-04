import { ApiResult } from "@servicestack/client"
import { Server, Transition, Routes, AdminRoutes, AdminStore, Authenticate, LinkInfo } from "../../lib/types"
import { useTransitions } from "../../shared/plugins/useTransitions"
import { Meta } from "./init"

/*minify:*/
/**
 * Execute tailwindui.com transition definition rules
 * @type {Transition}
 * */
export let transition = useTransitions(App, { sidebar: true })

/**
 * The App's reactive `routes` navigation component used for all App navigation
 * @remarks
 * @type {AdminRoutes & Routes}
 */
export let routes = usePageRoutes(App,{
    page:'admin',
    queryKeys:('tab,provider,q,page,sort,new,edit,op,skip,' +
        'show,orderBy,operationName,userAuthId,sessionId,pathInfo,ipAddress,referer,forwardedFor,hasResponse,withErrors').split(','),
    handlers: {
        nav(state) { console.log('nav', state) } /*debug*/
    }
})

/**
 * App's primary reactive store maintaining global functionality for Admin UI
 * @remarks
 * @type {AdminStore}
 */
export let store = App.reactive({
    copied: false,
    filter: '',
    debug: Server.config.debugMode,
    api: null,
    auth: window.AUTH,
    baseUrl: BASE_URL,

    init() {
        setBodyClass({ page: routes.admin })
    },

    get adminUsers() { return Server.plugins.adminUsers },

    /** @param {string|any} id
     *  @return {LinkInfo} */
    adminLink(id) { return Server.ui.adminLinks.find(x => x.id === id) },

    get adminLinks() { return Server.ui.adminLinks },
    
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

    SignIn() {
        return Server.plugins.auth
        ? SignIn({
            plugin: Server.plugins.auth,
            provider:() => routes.provider,
            login:args => this.login(args),
            api: () => this.api,
        })
        : NoAuth({ message:`${Server.app.serviceName} API Explorer` })
    },

    /** @param {any} args */
    login(args) {
        let provider = routes.provider || 'credentials'
        let authProvider = Server.plugins.auth.authProviders.find(x => x.name === provider)
            || Server.plugins.auth.authProviders[0]
        if (!authProvider)
            throw new Error("!authProvider")
        let auth = new Authenticate()
        bearerToken = authsecret = null
        if (authProvider.type === 'Bearer') {
            bearerToken = client.bearerToken = (args['BearerToken'] || '').trim()
        } else if (authProvider.type === 'authsecret') {
            authsecret = (args['authsecret'] || '').trim()
            client.headers.set('authsecret',authsecret)
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
        authsecret = bearerToken = client.bearerToken = null
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
        let roleLinks = this.auth && Server.plugins.auth && Server.plugins.auth.roleLinks || {} 
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
})

App.events.subscribe('route:nav', args => store.init())
/*:minify*/
