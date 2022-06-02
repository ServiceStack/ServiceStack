import { queryString, leftPart, each } from "@servicestack/client"
import { App, Routes } from "../../lib/types"

/*minify:*/

/**
 * @template {Record<<string,Function>} T
 * Maintain page route state:
 *  - /{pageKey}?{queryKeys}
 * @remarks
 * Events:
 *   route:init - loaded from URL
 *   route:to   - navigated by to()
 *   route:nav  - fired for both
 * @param {App} App
 * @param {{page:string,queryKeys:string[],handlers?:{init?:(args:any)=>void,to?:(args:any)=>void,nav?:(args:any)=>void},extend?:T}} opt 
 * @return {T & Routes}
 */
export function usePageRoutes(App, { page, queryKeys, handlers, extend }) {
    if (typeof page  != 'string' || page === '')
        throw new Error('page is required')
    if (typeof queryKeys == 'undefined' || !queryKeys.length)
        throw new Error('Array of queryKeys is required')

    let allKeys = [page,...queryKeys]
    /** @return {string} */
    function getPage() {
        return leftPart(location.href.substring(document.baseURI.length),'?')
    }

    /** @param {Record<string, any>} store
     *  @return {Record<string, any>} */
    function state(store) { 
        return each(allKeys, (o, key) => store[key] ? o[key] = store[key] : null) 
    }

    /** @param {string} name
     *  @param {Record<string,any>} args */
    let publish = (name,args) => {
        events.publish('route:' + name, args)
        events.publish('route:nav',args)
    }
    
    let events = App.events
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
        /** @param {Record<string, any>} args */
        set(args) {
            if (typeof args['$page'] != 'undefined') args[page] = args['$page']
            Object.keys(args).forEach(k => {
                if (allKeys.indexOf(k) >= 0) {
                    this[k] = args[k]
                }
            })
        },
        get state() { return state(this) },
        /** @param { Record<string, any>} args */
        to(args) {
            this.set(args)
            let cleanArgs = state(this)
            if (typeof args.$on == 'function') args.$on(cleanArgs)
            let href = args.$qs ? this.href({ $qs:args.$qs }) : this.href()
            history.pushState(cleanArgs, this[page], href)
            publish('to', cleanArgs)
        },
        /** @param { Record<string, any>} args */
        href(args) {
            /**: can't mutate reactive stores before createApp() */
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
    
    App.directive('href',  ({ effect, get, el }) => {
        el.href = store.href(get())
        el.onclick = e => {
            e.preventDefault()
            store.to(get())
        }
    })

    if (handlers) {
        let init = handlers.init && handlers.init.bind(store)
        if (init)
            App.events.subscribe('route:init', args => init(args))
        let to = handlers.to && handlers.to.bind(store)
        if (to)
            App.events.subscribe('route:to', args => to(args))
        let nav = handlers.nav && handlers.nav.bind(store)
        if (nav)
            App.events.subscribe('route:nav', args => nav(args))
    }
    
    App.onStart(app => store.start())

    return store
}

/*:minify*/
