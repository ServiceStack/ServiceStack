/** @template T 
 *  @typedef {import("@servicestack/client").ApiResult} ApiResult */
/*minify:*/
/** @type {function(string, boolean?): boolean} */
let transition = useTransitions(App, { sidebar: true })
/** @typedef {{tab?:string,provider?:string,q?:string,page?:string,sort?:string,new?:string,edit?:string}} AdminRoutes */
/** @type {AdminRoutes & {page: string, set: (function(any): void), state: any, to: (function(any): void), href: (function(any): string)}} */
let routes = usePageRoutes(App,{
    page:'admin',
    queryKeys:'tab,provider,q,page,sort,new,edit'.split(','),
    handlers: {
        nav(state) { console.log('nav', state) } /*debug*/
    }
})
/**
 * @type {{
    adminLink(string): LinkInfo, 
    init(): void, 
    cachedFetch(string): Promise<unknown>, 
    debug: boolean, 
    copied: boolean, 
    auth: AuthenticateResponse|null, 
    readonly authProfileUrl: string|null, 
    readonly displayName: null, 
    readonly link: LinkInfo, 
    readonly isAdmin: boolean, 
    login(any): void, 
    readonly adminUsers: AdminUsersInfo, 
    readonly authRoles: string[], 
    filter: string, 
    baseUrl: string, 
    logout(): void, 
    readonly authLinks: LinkInfo[], 
    SignIn(): Function, 
    readonly adminLinks: LinkInfo[], 
    api: ApiResult<AuthenticateResponse>|null, 
    readonly authPermissions: *
    }}
 */
let store = App.reactive({
    copied: false,
    filter: '',
    debug: APP.config.debugMode,
    api: null,
    auth: window.AUTH,
    baseUrl: BASE_URL,
    init() {
        setBodyClass({ page: routes.admin })
    },
    get adminUsers() { return APP.plugins.adminUsers },
    /** @param {string|any} id
     *  @return {LinkInfo} */
    adminLink(id) { return APP.ui.adminLinks.find(x => x.id === id) },
    get adminLinks() { return APP.ui.adminLinks },
    get link() { return this.adminLink(routes.admin) },
    /** @param {string} url
     *  @return {Promise<any>} */
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
    /** @param {any} args */
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
    get authRoles() { return this.auth && this.auth.roles || [] },
    get authPermissions() { return this.auth && this.auth.permissions || [] },
    get authProfileUrl() { return this.auth && this.auth.profileUrl },
    get isAdmin() { return this.authRoles.indexOf('Admin') >= 0 },
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
    get displayName() {
        let auth = this.auth
        return auth
            ? auth.displayName || (auth.firstName ? `${auth.firstName} ${auth.lastName}` : null) || auth.userName || auth.email
            : null
    },
})
App.events.subscribe('route:nav', args => store.init())
/*:minify*/
