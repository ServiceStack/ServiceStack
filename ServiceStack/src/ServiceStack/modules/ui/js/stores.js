/** @typedef {import("../../shared/plugins/useBreakpoints").Breakpoints} Breakpoints */
/*minify:*/
/**
 * Execute tailwindui.com transition definition rules
 * @remarks
 * @type {(prop:string,enter?:boolean) => boolean}
 * */
let transition = useTransitions(App, { sidebar: true })
/** @type {Breakpoints & {previous: Breakpoints, current: Breakpoints, snap: (function(): void)}} */
let breakpoints = useBreakpoints(App, {
    handlers: {
        change({ previous, current }) { console.log('breakpoints.change', previous, current) } /*debug*/
    }
})
/** Custom route params used in API Explorer
 * @typedef {{op?:string,tab?:string,lang?:string,provider?:string,preview?:string,body?:string,doc?:string,detailSrc?:string,form?:string,response?:string}} UiRoutes */
/** Route methods used in API Explorer
 * @typedef {{queryHref(): string}} UiRoutesExtend */
/**
 * The App's reactive `routes` navigation component used for all App navigation
 * @remarks
 * @type {UiRoutes & UiRoutesExtend & {page: string, set: (function(any): void), state: any, to: (function(any): void), href: (function(any): string)}}
 */
let routes = usePageRoutes(App,{
    page:'op',
    queryKeys:'tab,lang,provider,preview,body,doc,detailSrc,form,response'.split(','),
    handlers: {
        nav(state) { console.log('nav', state) } /*debug*/
    },
    extend: {
        queryHref() {
            let op = this.op && Meta.OpsMap[this.op]
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
let cleanSrc = src => src.trim();
/**
 * App's primary reactive store maintaining global functionality for API Explorer
 * @remarks
 * @type {{
 * cachedFetch: (url:string) => Promise<string>,
 *     copied: boolean, 
 *     readonly opTabs: {[p: string]: string}, 
 *     sideNav: {expanded: boolean, operations: MetadataOperationType[], tag: string}[], 
 *     auth: AuthenticateResponse, 
 *     readonly displayName: string|null, 
 *     loadLang: () => void, 
 *     langCache: () => {op: string, lang: string, url: string}, 
 *     login: (args:any, $on?:Function) => void, 
 *     detailSrcResult: {}, 
 *     logout: () => void, 
 *     readonly isServiceStackType: boolean, 
 *     api: ApiResult<AuthenticateResponse>, 
 *     init: () => void, 
 *     readonly op: MetadataOperationType|null, 
 *     debug: boolean, 
 *     readonly filteredSideNav: {tag: string, operations: MetadataOperationType[], expanded: boolean}[], 
 *     readonly authProfileUrl: string|null, 
 *     previewResult: string|null, 
 *     readonly activeLangSrc: string|null, 
 *     readonly previewCache: {preview: string, url: string, lang: string}|null, 
 *     toggle: (tag:string) => void, 
 *     getTypeUrl: (types: string) => string, 
 *     readonly authRoles: string[], 
 *     filter: string, 
 *     loadDetailSrc: () => void, 
 *     baseUrl: string, 
 *     readonly activeDetailSrc: string, 
 *     readonly authLinks: LinkInfo[], 
 *     readonly opName: string, 
 *     readonly previewSrc: string, 
 *     SignIn: (opt:any) => Function,
 *     hasRole: (role:string) => boolean, 
 *     loadPreview: () => void, 
 *     readonly authPermissions: string[], 
 *     readonly useLang: string, 
 *     invalidAccess: () => string|null
 * }}
 */
let store = App.reactive({
    /** @type {string|null} */
    previewResult: null,
    copied: false,
    /** @type {string} */
    filter: '',
    sideNav,
    detailSrcResult: {},
    /** @type {boolean} */
    debug: APP.config.debugMode,
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
        return routes.op ? APP.api.operations.find(op => op.request.name === routes.op) : null
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
        return this.op && this.op.request.namespace.startsWith("ServiceStack")
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
        return APP.plugins.auth
        ? SignIn({
            plugin: APP.plugins.auth,
            provider:() => routes.provider,
            login:args => this.login(args, opt && opt.$on),
            api: () => this.api,
        })
        : NoAuth({ message:`${APP.app.serviceName} API Explorer` })
    },
    /** @param {any} args
     *  @param {Function} [$on] */
    login(args, $on) {
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
        let roleLinks = this.auth && APP.plugins.auth && APP.plugins.auth.roleLinks || {} 
        if (Object.keys(roleLinks).length > 0) {
            this.authRoles.forEach(role => {
                if (!roleLinks[role]) return;
                roleLinks[role].forEach(link => to.push(link))
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
