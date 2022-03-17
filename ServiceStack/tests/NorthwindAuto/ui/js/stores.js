import { leftPart } from "@servicestack/client"
import { APP, Authenticate } from "../../lib/types"
import { Crud, setBodyClass, invalidAccessMessage, sortOps } from "../../shared/js/core"
import { Types } from "../../shared/js/Types"

/*minify:*/
App.useTransitions({ sidebar: true })
let breakpoints = App.useBreakpoints({
    handlers: {
        change({ previous, current }) { console.log('breakpoints.change', previous, current) } /*debug*/
    }
})

let routes = App.usePageRoutes({
    page:'op',
    queryKeys:'tab,lang,preview,detailSrc,form,response,body,provider,doc'.split(','),
    handlers: {
        nav(state) { console.log('nav', state) } /*debug*/
    },
    extend: {
        queryHref() {
            let op = this.op && OpsMap[this.op]
            if (op && APP.ui.modules.indexOf('/locode') >= 0) {
                if (Crud.isQuery(op)) {
                    return `/locode/${this.op}`
                } else if (Crud.isCrud(op)) {
                    let queryOp = APP.api.operations.find(x => Crud.isQuery(x) && Types.equals(op.dataModel,x.dataModel))
                    if (queryOp)
                        return `/locode/${queryOp.request.name}`
                }
            }
            return ''
        },
    }
})

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
        this.loadDetailSrc()
        this.loadLang()
        this.loadPreview()
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
                operations: sortOps(nav.operations.filter(filter))
            }))

        /**:return [...ret, ...ret, ...ret, ...ret, ...ret]*/
        return ret
    },

    toggle(tag) {
        let nav = this.sideNav.find(x => x.tag === tag)
        nav.expanded = !nav.expanded
    },

    loadLang() {
        if (!this.activeLangSrc) {
            let cache = this.langCache()
            if (CACHE[cache.url]) {
                this.langResult = { cache, result: CACHE[cache.url] }
            } else {
                this.cachedFetch(cache.url)
                    .then(src => {
                        this.langResult = { cache, result: CACHE[cache.url] = cleanSrc(src) }
                        if (!this.activeLangSrc) {
                            this.loadLang()
                        }
                    })
            }
        }
    },

    getTypeUrl(types) { return `/types/csharp?IncludeTypes=${types}&WithoutOptions=true&MakeVirtual=false&MakePartial=false&AddServiceStackTypes=true` },

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
            if (CACHE[cache.url]) {
                this.previewResult = { type:'src', ...cache, result: CACHE[cache.url] }
            } else {
                this.cachedFetch(cache.url)
                    .then(src => {
                        this.previewResult = {
                            type:'src',
                            ...cache,
                            result: CACHE[cache.url] = cache.lang ? cleanSrc(src) : src
                        }
                    })
            }
        }
    },

    get previewSrc() {
        let r = this.previewResult
        if (!r) return ''
        return routes.preview === r.preview && r.type === 'src' && r.lang ? r.result : ''
    },

    get activeLangSrc() {
        let cache = this.langResult && this.langResult.cache
        let ret = cache && routes.op === cache.op && this.useLang === cache.lang ? this.langResult.result : null
        return ret
    },

    loadDetailSrc() {
        if (!routes.detailSrc) return
        let cache = { url: this.getTypeUrl(routes.detailSrc) }
        if (CACHE[cache.url]) {
            this.detailSrcResult[cache.url] = { ...cache, result: CACHE[cache.url] }
        } else {
            this.cachedFetch(cache.url).then(src => {
                this.detailSrcResult[cache.url] = { ...cache, result: CACHE[cache.url] = cleanSrc(src) }
            })
        }
    },

    get activeDetailSrc() { return routes.detailSrc && this.detailSrcResult[this.getTypeUrl(routes.detailSrc)] },

    get op() {
        return routes.op ? APP.api.operations.find(op => op.request.name === routes.op) : null
    },
    get opName() { return this.op && this.op.request.name },

    get opTabs() {
        return this.op
            ? { ['API']:'', 'Details':'details', ['Code']:'code' }
            : {}
    },

    get isServiceStackType() {
        return this.op && this.op.request.namespace.startsWith("ServiceStack")
    },

    langCache() {
        let op = routes.op, lang = this.useLang
        return { op, lang, url: `/types/${lang}?IncludeTypes=${op}.*&WithoutOptions=true&MakeVirtual=false&MakePartial=false` + (this.isServiceStackType ? '&AddServiceStackTypes=true' : '') }
    },

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
        : NoAuth({ message:`${APP.app.serviceName} API Explorer` })
    },

    login(args) {
        let provider = routes.provider || 'credentials'
        let authProvider = APP.plugins.auth.authProviders.find(x => x.name === provider)
            || APP.plugins.auth.authProviders[0]
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
    hasRole(role) { return this.auth && this.auth.roles.indexOf(role) >= 0 },
})

App.events.subscribe('route:nav', args => store.init())

/*:minify*/
