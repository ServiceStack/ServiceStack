/*minify:*/
/** 
 * Execute tailwindui.com transition definition rules
 * @type {Transition}
 * */
let transition = useTransitions(App, { sidebar: true, 'select-columns': false })
/** 
 * Reactive store to maintain & programatically access Tailwind's responsive breakpoints
 * @type {Breakpoints & {previous: Breakpoints, current: Breakpoints, snap: (function(): void)}} 
 * */
let breakpoints = useBreakpoints(App, {
    handlers: {
        change({ previous, current }) { console.log('breakpoints.change', previous, current) } /*debug*/
    }
})
let onRoutesEditChange = null
let lastEditState = null
/** The App's reactive `routes` navigation component used for all App navigation
 * @type {LocodeRoutes & LocodeRoutesExtend & Routes} */
let routes = usePageRoutes(App,{
    page:'op',
    queryKeys:'tab,provider,preview,body,doc,skip,new,edit'.split(','),
    handlers: {
        nav(state) { 
            console.log('nav', state) /*debug*/
            this.update()
        }
    },
    /** @type LocodeRoutesExtend */
    extend: {
        uiHref(args) {
            return this.op && Server.ui.modules.indexOf('/ui') >= 0
                ? Meta.urlWithState(appendQueryString(`/ui/${this.op}`, args || {}))
                : ''
        },
        onEditChange(fn) {
            onRoutesEditChange = fn
            if (fn == null) lastEditState = null
            this.update()
        },
        update() {
            if (this.edit && onRoutesEditChange) {
                let newState = `${this.op}:${this.edit}`
                if (lastEditState == null || newState !== lastEditState) {
                    lastEditState = newState
                    onRoutesEditChange()
                }
            }
        }
    }
})
/** Manage users query & filter preferences in the Users browsers localStorage
 * @type {LocodeSettings} */
let settings = {
    events: {
        /** @param {string} op */
        op(op) { return `settings:${op}` },
        /** @param {string} op */
        lookup(op) { return `settings:lookup:${op}` },
        /** @param {string} op 
         *  @param {string} name */
        opProp(op,name) { return `settings:${op}.${name}` },
    },
    /** @param {string} op */
    op(op) { 
        return Object.assign({ take:25, selectedColumns:[] },
            map(localStorage.getItem(`locode/op:${op}`), x => JSON.parse(x))) 
    },
    /** @param {string} op */
    lookup(op) {
        return Object.assign({ take:10, selectedColumns:[] },
            map(localStorage.getItem(`locode/lookup:${op}`), x => JSON.parse(x)))
    },
    /** @param {string} op 
     *  @param {Function} fn */
    saveOp(op, fn) {
        let setting = this.op(op)
        fn(setting)
        localStorage.setItem(`locode/op:${op}`, JSON.stringify(setting))
        App.events.publish(this.events.op(op), setting)
    },
    /** @param {string} op
     *  @param {Function} fn */
    saveLookup(op, fn) {
        let setting = this.lookup(op)
        fn(setting)
        localStorage.setItem(`locode/lookup:${op}`, JSON.stringify(setting))
        App.events.publish(this.events.lookup(op), setting)
    },
    /** @param {string} op
     *  @param {string} name */
    opProp(op,name) {
        return Object.assign({ sort:null, filters:[] },
            map(localStorage.getItem(`locode/op:${op}.${name}`), x => JSON.parse(x)))
    },
    /** @param {string} op
     *  @param {string} name 
     *  @param {Function} fn */
    saveOpProp(op, name, fn) {
        let setting = this.opProp(op, name)
        fn(setting)
        localStorage.setItem(`locode/op:${op}.${name}`, JSON.stringify(setting))
        App.events.publish(this.events.opProp(op,name), setting)
    },
    /** @param {string} op */
    hasPrefs(op) {
        let prefixes = [`locode/op:${op}`,`locode/lookup:${op}`]
        return Object.keys(localStorage).some(k => prefixes.some(p => k.startsWith(p)))
    },
    /** @param {string} op */
    clearPrefs(op) {
        let prefixes = [`locode/op:${op}`,`locode/lookup:${op}`]
        let removeKeys = Object.keys(localStorage).filter(k => prefixes.some(p => k.startsWith(p)))
        removeKeys.forEach(k => localStorage.removeItem(k))
    }
}
/** App's primary reactive store maintaining global functionality for Locode Apps
 * @type {LocodeStore} */
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
    get op() { return routes.op ? Server.api.operations.find(op => op.request.name === routes.op) : null },
    /** @return {string} */
    get opName() { return this.op && this.op.request.name },
    /** @return {string} */
    get opDesc() { return this.op && (this.op.request.description || humanify(this.op.request.name)) },
    /** @return {string} */
    get opDataModel() { return this.op && this.op.dataModel && this.op.dataModel.name },
    /** @return {string} */
    get opViewModel() { return this.op && this.op.viewModel && this.op.viewModel.name },
    /** @return {boolean} */
    get isServiceStackType() { return this.op && this.op.request.namespace.startsWith("ServiceStack") },
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
        return Server.plugins.auth
        ? SignIn({
            plugin: Server.plugins.auth,
            provider:() => routes.provider,
            login:args => this.login(args, opt && opt.$on),
            api: () => this.api
        })
        : NoAuth({ message:`Welcome to ${Server.app.serviceName}` })
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
                if (!roleLinks[role]) return
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
/** 
 * Create a new state for an API that encapsulates its invocation and execution
 * @param {MetadataOperationType} op
 * @return {ApiState|null} */
function apiState(op) {
    if (!op) return null
    let formLayout = Forms.resolveFormLayout(op)
    function createModel(args) {
        let ret = Forms.populateModel(createRequest(op), formLayout)
        if (args) Object.keys(args).forEach(k => {
            ret[k] = Forms.apiValue(args[k])
        })
        return ret
    }
    return {
        op,
        client,
        apiState,
        formLayout,
        createModel,
        apiLoading: false,
        apiResult: null,
        get api() { return map(this.apiResult, x => x.api) },
        createRequest: args => createRequest(op,args),
        model: createModel(),
        title: Forms.opTitle(op),
        get error(){ return this.apiResult && this.apiResult.api.error },
        get errorSummary() {
            if (!formLayout) return null
            let except = formLayout.map(input => input.id).filter(x => x)
            return this.apiResult && this.apiResult.api.summaryMessage(except)
        },
        /** @param {string} id 
         *  @return {string|null} */
        fieldError(id) {
            let error = this.error
            let fieldError = error && error.errors && error.errors.find(x => x.fieldName.toLowerCase() === id.toLowerCase());
            return fieldError && fieldError.message
        },
        /** @param {string} propName
            @param {(args:{id:string,input:InputInfo,rowClass:string}) => void} [f] */
        field(propName, f) {
            let propLower = propName.toLowerCase()
            let input = (formLayout || []).find(input => (input.id||'').toLowerCase() === propLower)
            let inputFn = Crud.isCreate(op)
                ? Forms.forCreate(op.request)
                : Crud.isPatch(op) || Crud.isUpdate(op)
                    ? Forms.forEdit(op.request)
                    : null
            let field = input && Forms.getGridInput(input, inputFn)
            if (f) f(field)
            return field
        },
        /** @param {Record<string,any>} dtoArgs
            @param {Record<string,any>} [queryArgs]*/
        apiSend(dtoArgs,queryArgs) {
            let requestDto = this.createRequest(dtoArgs)
            let complete = delaySet(x => {
                this.apiResult = null
                this.apiLoading = x
            })
            return apiSend(createClient, requestDto, queryArgs).then(r => {
                complete()
                this.apiResult = r
                return this.apiResult
            })
        },
        /** @param {FormData} formData
            @param {Record<string,any>} [queryArgs]*/
        apiForm(formData,queryArgs) {
            let requestDto = this.createRequest()
            let complete = delaySet(x => {
                this.apiResult = null
                this.apiLoading = x
            })
            return apiForm(createClient, requestDto, formData, queryArgs).then(r => {
                complete()
                this.apiResult = r
                return this.apiResult
            })
        }
    }
}
/** 
 * Return all CRUD API States available for this operation
 * @param {string} opName
 * @return {CrudApisState|null}
 */
function createState(opName) {
    let op = opName && Server.api.operations.find(x => x.request.name === opName)
    if (op) {
        /** @param f
         *  @returns {MetadataOperationType|null} */
        function findOp(f) {
            return Server.api.operations.find(x => f(x) && Types.equals(op.dataModel,x.dataModel))
        }
        /** @param {MetadataOperationType} op
         *  @returns {ApiState|null} */
        function hasApi(op) { 
            return canAccess(op,store.auth)
                ? apiState(op) 
                : null 
        }
        let { opQuery, opCreate, opPatch, opUpdate, opDelete } = {
            opQuery:   op,
            opCreate:  findOp(Crud.isCreate),
            opPatch:   findOp(Crud.isPatch),
            opUpdate:  findOp(Crud.isUpdate),
            opDelete:  findOp(Crud.isDelete),
        }
        return {
            opQuery,
            opCreate, 
            opPatch, 
            opUpdate, 
            opDelete,
            apiQuery:  hasApi(op),
            apiCreate: hasApi(opCreate),
            apiPatch:  hasApi(opPatch),
            apiUpdate: hasApi(opUpdate),
            apiDelete: hasApi(opDelete),
        }
    }
    console.log('!createState.op') /*debug*/
}
/*:minify*/
