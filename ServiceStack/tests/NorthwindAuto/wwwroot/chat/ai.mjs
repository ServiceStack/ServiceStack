import { reactive } from "vue"
import { useThreadStore } from "./threadStore.mjs"

const base = '/chat'
const headers = { 'Accept': 'application/json' }
const prefsKey = 'llms.prefs'

export const o = {
    version: '2.0.30',
    base,
    prefsKey,
    welcome: 'Welcome to AI Chat',
    auth: null,
    requiresAuth: true,
    headers,
    isSidebarOpen: true,  // Shared sidebar state (default open for lg+ screens)

    resolveUrl(url){
        if (url === '/auth/logout') return url
        return url.startsWith('http') || url.startsWith('/v1') ? url : base + url
    },
    get(url, options) {
        return fetch(this.resolveUrl(url), {
            ...options,
            headers: Object.assign({}, this.headers, options?.headers),
        })
    },
    post(url, options) {
        return fetch(this.resolveUrl(url), {
            method: 'POST',
            ...options,
            headers: Object.assign({'Content-Type': 'application/json'}, this.headers, options?.headers),
        })
    },

    async getConfig() {
        return this.get('/config')
    },
    async getModels() {
        return this.get('/models')
    },
    async getAuth() {
        return this.requiresAuth
            ? this.get('/auth')
            : new Promise(resolve => resolve({ json: () => ({ responseStatus: { errorCode: '!requiresAuth' } })}))
    },
    get isAdmin() {
        return !this.requiresAuth || this.auth && this.auth.roles?.includes('Admin')
    },

    signIn(auth) {
        this.auth = auth
        if (auth?.apiKey) {
            this.headers.Authorization = `Bearer ${auth.apiKey}`
            //localStorage.setItem('llms:auth', JSON.stringify({ apiKey: auth.apiKey }))
        } else if (auth?.sessionToken) {
            this.headers['X-Session-Token'] = auth.sessionToken
            localStorage.setItem('llms:auth', JSON.stringify({ sessionToken: auth.sessionToken }))
        } else {
            if (this.headers.Authorization) {
                delete this.headers.Authorization
            }
            if (this.headers['X-Session-Token']) {
                delete this.headers['X-Session-Token']
            }
        }
    },
    async signOut() {
        // Call logout endpoint for OAuth sessions
        try {
            await this.post('/auth/logout')
        } catch (error) {
            console.error('Logout error:', error)
        }
        this.auth = null
        if (this.headers.Authorization) {
            delete this.headers.Authorization
        }
        if (this.headers['X-Session-Token']) {
            delete this.headers['X-Session-Token']
        }
        localStorage.removeItem('llms:auth')
    },
    async init() {
        // Load models and prompts
        const { initDB } = useThreadStore()
        const [_, configRes, modelsRes] = await Promise.all([
            initDB(),
            this.getConfig(),
            this.getModels(),
        ])
        const config = await configRes.json()
        const models = await modelsRes.json()

        // Update auth settings from server config
        if (config.requiresAuth != null) {
            this.requiresAuth = config.requiresAuth
        }
        if (config.authType != null) {
            this.authType = config.authType
        }

        // Try to restore session from localStorage
        if (this.requiresAuth) {
            const storedAuth = localStorage.getItem('llms:auth')
            if (storedAuth) {
                try {
                    const authData = JSON.parse(storedAuth)
                    if (authData.sessionToken) {
                        this.headers['X-Session-Token'] = authData.sessionToken
                    }
                    // else if (authData.apiKey) {
                    //     this.headers.Authorization = `Bearer ${authData.apiKey}`
                    // }
                } catch (e) {
                    console.error('Failed to restore auth from localStorage:', e)
                    localStorage.removeItem('llms:auth')
                }
            }
        }

        // Get auth status
        const authRes = await this.getAuth()
        const auth = this.requiresAuth
            ? await authRes.json()
            : null
        if (auth?.responseStatus?.errorCode) {
            console.error(auth.responseStatus.errorCode, auth.responseStatus.message)
            // Clear invalid session from localStorage
            localStorage.removeItem('llms:auth')
        } else {
            this.signIn(auth)
        }
        return { config, models, auth }
    }
}

let ai = reactive(o)
export default ai
