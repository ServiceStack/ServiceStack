import { appendQueryString, createUrl, humanify, leftPart } from "@servicestack/client"
import { APP, Authenticate } from "../../lib/types"
import { setBodyClass, invalidAccessMessage } from "../../shared/js/core"

/*minify:*/
App.useTransitions({ sidebar: true, 'select-columns': false })
let breakpoints = App.useBreakpoints({
    handlers: {
        change({ previous, current }) { console.log('breakpoints.change', previous, current) } /*debug*/
    }
})

let routes = App.usePageRoutes({
    page:'op',
    queryKeys:'tab,preview,body,doc,skip,new,edit'.split(','),
    handlers: {
        nav(state) { console.log('nav', state) } /*debug*/
    },
    extend: {
        uiHref(args) {
            return this.op && APP.ui.modules.indexOf('/ui') >= 0
                ? appendQueryString(`/ui/${this.op}`, args || {})
                : ''
        },
    }
})

let settings = {
    events: {
        op(op) { return `settings:${op}` },
        opProp(op,name) { return `settings:${op}.${name}` },
    },
    op(op) { 
        return Object.assign({ take:25, selectedColumns:[] },
            map(localStorage.getItem(`query-ui/op:${op}`), x => JSON.parse(x))) 
    },
    saveOp(op, fn) {
        let setting = this.op(op)
        fn(setting)
        localStorage.setItem(`query-ui/op:${op}`, JSON.stringify(setting))
        App.events.publish(this.events.op(op), setting)
    },
    opProp(op,name) {
        return Object.assign({ sort:null, filters:[] },
            map(localStorage.getItem(`query-ui/op:${op}.${name}`), x => JSON.parse(x)))
    },
    saveOpProp(op, name, fn) {
        let setting = this.opProp(op, name)
        fn(setting)
        localStorage.setItem(`query-ui/op:${op}.${name}`, JSON.stringify(setting))
        App.events.publish(this.events.opProp(op,name), setting)
    },
}

let store = PetiteVue.reactive({
    previewResult: null,
    copied: false,
    filter: '',
    sideNav,
    detailSrcResult: {},
    debug: APP.config.debugMode,
    api: null,
    auth: window.AUTH,
    baseUrl: BASE_URL,

    get useLang() { return routes.lang || 'csharp' },

    init() {
        setBodyClass({ page: routes.op })
    },

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
                operations: nav.operations.filter(filter)
            }))

        /**:return [...ret, ...ret, ...ret, ...ret, ...ret]*/
        return ret
    },

    toggle(tag) {
        let nav = this.sideNav.find(x => x.tag === tag)
        nav.expanded = !nav.expanded
    },

    get op() { return routes.op ? APP.api.operations.find(op => op.request.name === routes.op) : null },
    get opName() { return this.op && this.op.request.name },
    get opDesc() { return this.op && (this.op.request.description || humanify(this.op.request.name)) },
    get opDataModel() { return this.op && this.op.dataModel && this.op.dataModel.name },
    get opViewModel() { return this.op && this.op.viewModel && this.op.viewModel.name },

    get isServiceStackType() { return this.op && this.op.request.namespace.startsWith("ServiceStack") },

    cachedFetch(url) {
        return new Promise((resolve,reject) => {
            let src = CACHE[url]
            if (src) {
                resolve(src)
            } else {
                fetch(url)
                    .then(r => {
                        if (r.ok) return r.text()
                        else throw r.statusText
                    })
                    .then(src => {
                        resolve(CACHE[url] = src)
                    })
                    .catch(e => {
                        console.error(`fetchCache (${url}):`, e)
                        reject(e)
                    })
            }
        })
    },

    SignIn() {
        return APP.plugins.auth
        ? SignIn({
            plugin: APP.plugins.auth,
            provider:() => routes.provider,
            login:args => this.login(args),
            api: () => this.api,
        })
        : NoAuth({ message:`${APP.app.serviceName} AutoQuery UI` })
    },

    login(args) {
        let provider = routes.provider || 'credentials'
        let authProvider = APP.plugins.auth.authProviders.find(x => x.name === provider)
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
                    r.error.message = HttpErrors[r.errorCode] || r.errorCode
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
    
    get authLinks() {
        let to = []
        let roleLinks = this.auth && APP.plugins.auth && APP.plugins.auth.roleLinks || {} 
        if (Object.keys(roleLinks).length > 0) {
            this.authRoles.forEach(role => {
                if (!roleLinks[role]) return;
                roleLinks[role].forEach(link => to.push(link))
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

    invalidAccess() { return invalidAccessMessage(this.op, this.auth) },
})

App.events.subscribe('route:nav', args => store.init())
/*:minify*/
