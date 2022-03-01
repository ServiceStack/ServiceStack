/*minify:*/
/**
 * Maintain page route state:
 *  - /{pageKey}?{queryKeys}
 * Events:
 *   route:init - loaded from URL
 *   route:to   - navigated by to()
 *   route:nav  - fired for both
 */
App.plugin({
    usePageRoutes({ page, queryKeys, handlers, extend }) {
        if (typeof page  != 'string' || page === '')
            throw new Error('page is required')
        if (typeof queryKeys == 'undefined' || !queryKeys.length)
            throw new Error('Array of queryKeys is required')
        let allKeys = [page,...queryKeys] 
        let getPage = () => leftPart(location.href.substring(document.baseURI.length),'?')
        let state = store => each(allKeys, (o, key) => store[key] ? o[key] = store[key] : null)
        let publish = (name,args) => {
            events.publish('route:' + name, args)
            events.publish('route:nav',args)
        }
        let events = this.events
        let store = App.reactive({
            page, 
            queryKeys,
            ...each(allKeys, (o,x) => o[x] = ''),
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
                let href = args.$qs ? this.href({ $qs:args.$qs }) : this.href()
                history.pushState(cleanArgs, this[page], href)
                publish('to', cleanArgs)
            },
            href(args) {
                if (args && typeof args['$page'] != 'undefined') args[page] = args['$page']
                let s = args ? Object.assign({}, state(this), args) : state(this)
                let path = s[page] || ''
                let qsArgs = queryKeys.filter(k => s[k]).map(k => `${encodeURIComponent(k)}=${encodeURIComponent(s[k])}`)
                let $qs = args && typeof args['$qs'] == 'object' ? args['$qs'] : null
                if ($qs) {
                    qsArgs = [...qsArgs, ...Object.keys($qs).map(k => `${encodeURIComponent(k)}=${encodeURIComponent($qs[k])}`)]
                }
                let qs = qsArgs.join('&')
                return path + (qs ? '?' + qs : '')
            },
            ...extend
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
                this.events.subscribe('route:init', args => handlers.init(args))
            if (handlers.to)
                this.events.subscribe('route:to', args => handlers.to(args))
            if (handlers.nav)
                this.events.subscribe('route:nav', args => handlers.nav(args))
        }
        this.onStart(app => store.start())
        return store
    }
})
/*:minify*/
