import { reactive } from "vue"
import { useThreadStore } from "./threadStore.mjs"
const base = '/chat'
const headers = { 'Accept': 'application/json' }
const prefsKey = 'llms.prefs'
export const o = {
    base,
    prefsKey,
    welcome: 'Welcome to AI Chat',
    auth: null,
    requiresAuth: true,
    headers,
    
    resolveUrl(url){
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
        } else if (this.headers.Authorization) {
            delete this.headers.Authorization
        }
    },
    async init() {
        // Load models and prompts
        const { initDB } = useThreadStore()
        const [_, configRes, modelsRes, authRes] = await Promise.all([
            initDB(),
            this.getConfig(),
            this.getModels(),
            this.getAuth(),
        ])
        const config = await configRes.json()
        const models = await modelsRes.json()
        const auth = this.requiresAuth
            ? await authRes.json()
            : null
        if (auth?.responseStatus?.errorCode) {
            console.error(auth.responseStatus.errorCode, auth.responseStatus.message)
        } else {
            this.signIn(auth)
        }
        return { config, models, auth }
    }
}
let ai = reactive(o)
export default ai
