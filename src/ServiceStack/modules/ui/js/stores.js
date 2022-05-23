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
                if (invalidAccessMessage(op, this.auth.roles, this.auth.permissions))
                    return false
            }
            return !lowerFilter || op.request.name.toLowerCase().indexOf(lowerFilter) >= 0
        }
        let ret = this.sideNav.filter(nav => nav.operations.some(filter))
            .map(nav => ({
                ...nav,
                operations: nav.operations.filter(filter)
            }))
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
    },
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
    invalidAccess() {
        let op = this.op
        if (!op || !op.requiresAuth) return null
        if (!this.auth) return `<b>${op.request.name}</b> requires Authentication`
        ;return invalidAccessMessage(op, this.auth.roles, this.auth.permissions)
    },
})
App.events.subscribe('route:nav', args => store.init())
function typeProperties(type) {
    let props = []
    while (type) {
        if (type.properties) props.push(...type.properties)
        type = type.inherits ? TypesMap[type.inherits.name] : null
    }
    return props.map(prop => prop.type.endsWith('[]')
        ? {...prop, type:'List`1', genericArgs:[prop.type.substring(0,prop.type.length-2)] }
        : prop)
}
let NumTypesMap = {
    Byte: 'byte',
    Int16: 'short',
    Int32: 'int',
    Int64: 'long',
    UInt16: 'ushort',
    Unt32: 'uint',
    UInt64: 'ulong',
    Single: 'float',
    Double: 'double',
    Decimal: 'decimal',
}
let NumTypes = [ ...Object.keys(NumTypesMap), ...Object.values(NumTypesMap) ]
let TypeAliases = {
    String: 'string',
    Boolean: 'bool',
    ...NumTypesMap,
}
function isNumberType(type) {
    return type && NumTypes.indexOf(type) >= 0
}
function typeAlias(typeName) {
    return TypeAliases[typeName] || typeName
}
function unwrap(type) { return type && type.endsWith('?') ? type.substring(0,type.length-1) : type }
function typeName2(name, genericArgs) {
    if (!name) return ''
    if (!genericArgs)
        genericArgs = []
    if (name === 'Nullable`1')
        return typeAlias(genericArgs[0]) + '?'
    if (name.endsWith('[]'))
        return `List<${typeAlias(name.substring(0,name.length-2))}>`
    ;if (genericArgs.length === 0)
        return typeAlias(name)
    return leftPart(typeAlias(name), '`') + '<' + genericArgs.join(',') + '>'
}
function typeName(metaType) { return metaType && typeName2(metaType.name, metaType.genericArgs) }
/*:minify*/
