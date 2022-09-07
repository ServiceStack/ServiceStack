/*minify:*/
/**
 * Execute tailwindui.com transition definition rules
 * @remarks
 * @type {Transition}
 * */
let transition = useTransitions(App, { sidebar: true })
/** @type {Breakpoints & {previous: Breakpoints, current: Breakpoints, snap: (function(): void)}} */
let breakpoints = useBreakpoints(App, {
    handlers: {
        change({ previous, current }) { console.log('breakpoints.change', previous, current) } /*debug*/
    }
})
/** The App's reactive `routes` navigation component used for all App navigation
 * @type {ExplorerRoutes & ExplorerRoutesExtend & Routes} */
let routes = usePageRoutes(App,{
    page:'op',
    queryKeys:'tab,lang,provider,preview,body,doc,detailSrc,form,response'.split(','),
    handlers: {
        nav(state) { console.log('nav', state) } /*debug*/
    },
    extend: {
        queryHref() {
            let op = this.op && Meta.OpsMap[this.op]
            if (op && Server.ui.modules.indexOf('/locode') >= 0) {
                if (Crud.isQuery(op)) {
                    return Meta.locodeUrl(this.op)
                } else if (Crud.isCrud(op)) {
                    let queryOp = Server.api.operations.find(x => Crud.isQuery(x) && Types.equals(op.dataModel,x.dataModel))
                    if (queryOp)
                        return Meta.locodeUrl(queryOp.request.name) 
                }
            }
            return ''
        },
    }
})
let cleanSrc = src => src.trim();
/** App's primary reactive store maintaining global functionality for API Explorer 
 * @type {ExplorerStore} */
let store = App.reactive({
    /** @type {string|null} */
    previewResult: null,
    copied: false,
    /** @type {string} */
    filter: '',
    sideNav,
    detailSrcResult: {},
    /** @type {boolean} */
    debug: Server.config.debugMode,
    /** @type {ApiResult<AuthenticateResponse>} */
    api: null,
    /** @type {AuthenticateResponse} */
    auth: window.AUTH,
    /** @type {string} */
    baseUrl: BASE_URL,
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
            if (Meta.CACHE[cache.url]) {
                this.langResult = { cache, result: Meta.CACHE[cache.url] }
            } else {
                this.cachedFetch(cache.url)
                    .then(src => {
                        this.langResult = { cache, result: Meta.CACHE[cache.url] = cleanSrc(src) }
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
            if (Meta.CACHE[cache.url]) {
                this.previewResult = { type:'src', ...cache, result: Meta.CACHE[cache.url] }
            } else {
                this.cachedFetch(cache.url)
                    .then(src => {
                        this.previewResult = {
                            type:'src',
                            ...cache,
                            result: Meta.CACHE[cache.url] = cache.lang ? cleanSrc(src) : src
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
        if (Meta.CACHE[cache.url]) {
            this.detailSrcResult[cache.url] = { ...cache, result: Meta.CACHE[cache.url] }
        } else {
            this.cachedFetch(cache.url).then(src => {
                this.detailSrcResult[cache.url] = { ...cache, result: Meta.CACHE[cache.url] = cleanSrc(src) }
            })
        }
    },
    /** @return {string} */
    get activeDetailSrc() { return routes.detailSrc && this.detailSrcResult[this.getTypeUrl(routes.detailSrc)] },
    /** @return {MetadataOperationType|null} */
    get op() {
        return routes.op ? Server.api.operations.find(op => op.request.name === routes.op) : null
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
        return this.op && this.(op.request.namespace ?? "").startsWith("ServiceStack")
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
            let src = Meta.CACHE[url]
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
                        resolve(Meta.CACHE[url] = src)
                    })
                    .catch(e => {
                        console.error(`fetchCache (${url}):`, e)
                        reject(e)
                    })
            }
        })
    },
    /** @param opt
     *  @return {Function}
     *  @constructor */
    SignIn(opt) {
        return Server.plugins.auth
        ? SignIn({
            plugin: Server.plugins.auth,
            provider:() => routes.provider,
            login:args => this.login(args, opt && opt.$on),
            api: () => this.api,
        })
        : NoAuth({ message:`${Server.app.serviceName} API Explorer` })
    },
    /** @param {any} args
     *  @param {Function} [$on] */
    login(args, $on) {
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
                    if ($on) $on()
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
    /** @return {string[]} */
    get authRoles() { return this.auth && this.auth.roles || [] },
    /** @return {string[]} */
    get authPermissions() { return this.auth && this.auth.permissions || [] },
    /** @return {string|null} */
    get authProfileUrl() { return this.auth && this.auth.profileUrl },
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
})
App.events.subscribe('route:nav', args => store.init())
/*:minify*/
