/**
 * Maintain page route state:
 *  - /{pageKey}?{queryKeys}
 * Events:
 *   pageRoutes:init - loaded from URL
 *   pageRoutes:to   - navigated by to()
 *   pageRoutes:nav  - fired for both
 */
App.plugin({
    pageRoutes({ page, queryKeys, handlers }) {
        if (typeof page  != 'string' || page === '')
            throw new Error('page is required')
        if (typeof queryKeys == 'undefined' || !queryKeys.length)
            throw new Error('Array of queryKeys is required')
        let allKeys = [page,...queryKeys] 
        let getPage = () => leftPart(location.href.substring(document.baseURI.length),'?')
        let state = store => allKeys.reduce((acc,x) => {
            if (store[x]) acc[x] = store[x]
            return acc
        }, {})
        let publish = (name,args) => {
            events.publish('pageRoutes:' + name, args)
            events.publish('pageRoutes:nav',args)
        }
        let events = this.events
        let store = App.reactive({
            page, 
            queryKeys,
            ...allKeys.reduce((acc,x) => { acc[x] = ''; return acc }, {}),
            start() {
                window.addEventListener('popstate', (event) => {
                    this.set({ [page]:getPage(), ...event.state})
                    publish('init', state(this))
                })
                this.set({ [page]:getPage(), ...(location.search ? queryString(location.search) : {}) })
                publish('init', state(this))
            },
            set(args) {
                if (typeof args['$page'] != 'undefined') args[page] = args['$page']
                Object.keys(args).forEach(k => {
                    if (allKeys.indexOf(k) >= 0) {
                        this[k] = args[k]
                    }
                })
            },
            get state() { return state(this) },
            to(args) {
                this.set(args)
                let cleanArgs = state(this)
                if (typeof args.$on == 'function') args.$on(cleanArgs)
                history.pushState(cleanArgs, this[page], this.href())
                publish('to', cleanArgs)
            },
            href(args) {
                if (args && typeof args['$page'] != 'undefined') args[page] = args['$page']
                let s = args ? Object.assign({}, state(this), args) : state(this)
                let path = s[page] || ''
                let qs = queryKeys.filter(k => s[k]).map(k =>
                    `${encodeURIComponent(k)}=${encodeURIComponent(s[k])}`).join('&')
                return path + (qs ? '?' + qs : '')
            }
        })
        this.directive('href',  ({ effect, get, el }) => {
            el.href = store.href(get())
            el.onclick = e => {
                e.preventDefault()
                store.to(get())
            }
        })
        if (handlers) {
            if (handlers.init)
                this.events.subscribe('pageRoutes:init', args => handlers.init(args))
            if (handlers.to)
                this.events.subscribe('pageRoutes:to', args => handlers.to(args))
        }
        return store
    }
})
