import { APP, Authenticate } from "../../lib/types"

/**: SignIn:provider */
let routes = App.pageRoutes({
    page:'admin',
    queryKeys:'tab,provider,q,page,sort,new,edit'.split(','),
    handlers: {
        init(state) {
            if (DEBUG) console.log('pageRoutes:init', state)
        },
        to(state) {
            if (DEBUG) console.log('pageRoutes:to', state)
        }
    }
})

let store = PetiteVue.reactive({
    copied: false,
    filter: '',
    debug: APP.config.debugMode,
    api: null,
    auth: window.AUTH,
    baseUrl: BASE_URL,
    get sm() { return isSmall() },
    doLayout() {
        /**: sm: only show sidebar on home page */
        let root = document.documentElement
        let sidebar = $1('.sidebar')
        if (!sidebar) return
        if (isSmall() && routes.admin) {
            sidebar.style.display = 'none'
            root.style.setProperty('--sidebar-width', '0px')
        } else {
            sidebar.style.display = 'block'
            root.style.setProperty('--sidebar-width', SidebarWidth)
        }
    },

    init() {
        this.doLayout()
        setBodyClass({ page: routes.admin })
    },

    get adminUsers() { return APP.plugins.adminUsers },

    adminLink(id) { return APP.ui.adminLinks.find(x => x.id === id) },

    get adminLinks() { return APP.ui.adminLinks },
    
    get link() { return this.adminLink(routes.admin) },

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
                        console.log(`ERROR fetchCache (${url}):`, e)
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
        client.api(auth)
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

    /**: v-if doesn't protect against nested access so need to guard against deep NRE access */
    get authRoles() { return this.auth && this.auth.roles || [] },
    get authPermissions() { return this.auth && this.auth.permissions || [] },
    get authProfileUrl() { return this.auth && this.auth.profileUrl },
    get isAdmin() { return this.authRoles.indexOf('Admin') >= 0 },
    
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

App.events.subscribe('pageRoutes:nav', args => store.init())

