/*minify:*/
import { createApp, reactive } from "vue"
import { createBus, map, each, leftPart, on, queryString } from "@servicestack/client"
import { useMetadata } from "@servicestack/vue"

const { typeOfRef } = useMetadata()

export class App {
    app
    events = createBus()
    Providers = {}
    Plugins = []
    Directives = {}
    Props = {}
    OnStart = []
    Components = {}

    provides(providers) {
        Object.keys(providers).forEach(k => this.Providers[k] = providers[k])
    }
    components(components) {
        Object.keys(components).forEach(k => this.Components[k] = components[k])
    }
    component(name, c) {
        if (c) {
            this.Components[name] = c
        }
        return this.Components[name]
    }
    directive(name, f) {
        this.Directives[name] = f
    }
    use(plugin) {
        this.Plugins.push(plugin)
    }

    build(component, props) {
        const app = this.app = createApp(component, props)
        this.Plugins.forEach(plugin => app.use(plugin))
        Object.keys(this.Providers).forEach(name => {
            app.provide(name, this.Providers[name])
        })
        Object.keys(this.Components).forEach(name => {
            app.component(name, this.Components[name])
        })
        Object.keys(this.Directives).forEach(name => {
            app.directive(name, this.Directives[name])
        })
        return app
    }

    /** @param {Function} f */
    onStart(f) {
        this.OnStart.push(f)
    }
    start() {
        this.OnStart.forEach(f => f(this))
    }

    unsubscribe() {
        if (this.sub) {
            this.sub.unsubscribe()
            this.sub = null
        }
    }
}

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
export function usePageRoutes(app, { page, queryKeys, handlers, extend }) {
    if (typeof page != 'string' || page === '')
        throw new Error('page is required')
    if (typeof queryKeys == 'undefined' || !queryKeys.length)
        throw new Error('Array of queryKeys is required')

    let allKeys = [page,...queryKeys]
    /** @return {string} */
    function getPage() {
        return leftPart(location.href,'?').substring(document.baseURI.length)
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

    let events = app.events
    let store = {
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
            if (typeof args['$page'] != 'undefined') {
                this[page] = args[page] = args['$page']
            }
            if (args['$clear']) {
                allKeys.forEach(k => this[k] = args[k] != null ? args[k] : '')
            } else {
                Object.keys(args).forEach(k => {
                    if (allKeys.indexOf(k) >= 0) {
                        this[k] = args[k]
                    }
                })
            }
        },
        get state() { return state(this) },
        /** @param { Record<string, any>} args */
        to(args) {
            this.set(args)
            let cleanArgs = state(this)
            if (typeof args.$on == 'function') args.$on(cleanArgs)
            let href = args.$qs ? this.href({ $qs:args.$qs }) : this.href(null)
            history.pushState(cleanArgs, this[page], href)
            publish('to', cleanArgs)
        },
        /** @param {Record<string,any>} args */
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
    }
    store = reactive(store)

    app.directive('href', function (el, binding) {
        el.href = store.href(binding)
        el.onclick = e => {
            e.preventDefault()
            store.to(binding.value)
        }
    })

    if (handlers) {
        let init = handlers.init && handlers.init.bind(store)
        if (init)
            events.subscribe('route:init', args => init(args))
        let to = handlers.to && handlers.to.bind(store)
        if (to)
            events.subscribe('route:to', args => to(args))
        let nav = handlers.nav && handlers.nav.bind(store)
        if (nav)
            events.subscribe('route:nav', args => nav(args))
    }

    app.onStart(app => store.start())

    return store
}


/**
 * Returns a reactive store that maintains different resolution states:
 *
 * @remarks
 * Events:
 *   breakpoint:change - the browser width changed breakpoints
 *
 * @defaultValue { 2xl:1536, xl:1280, lg:1024, md:768, sm:640 }
 *
 * @example
 * E.g. at 1200px: { 2xl:false, xl:false, lg:true, md:true, sm:true }
 *
 * @param {App} app
 * @param {{handlers: {change({previous: *, current: *}): void}}} options
 * @returns {Breakpoints & {previous:Breakpoints,current:Breakpoints,snap:()=>void}}
 */
export function useBreakpoints(app, options) {
    if (!options) options = {}
    let {resolutions, handlers} = options
    if (!resolutions) resolutions = {'2xl': 1536, xl: 1280, lg: 1024, md: 768, sm: 640}
    let sizes = Object.keys(resolutions)

    let previous = {}
    let events = app.events

    let store = {
        get previous() {
            return previous
        },
        get current() {
            return each(sizes, (o, res) => o[res] = this[res])
        },
        snap() {
            let w = document.body.clientWidth
            let current = each(sizes, (o, res) => o[res] = w > resolutions[res])
            let changed = false
            sizes.forEach(res => {
                if (current[res] !== this[res]) {
                    this[res] = current[res]
                    changed = true
                }
            })

            if (changed) {
                previous = current
                events.publish('breakpoint:change', this)
            }
        },
    }
    store = reactive(store)

    on(window, {
        resize: () => store.snap()
    })

    if (handlers && handlers.change)
        events.subscribe('breakpoint:change', args => handlers.change(args))

    app.onStart(app => store.snap())

    return store
}

/** Set class on document.body if truthy otherwise set `no{class}`
 * @param {{[key:string]:string|any}} obj */
export function setBodyClass(obj) {
    let bodyCls = document.body.classList
    Object.keys(obj).forEach(name => {
        if (obj[name]) {
            bodyCls.add(name)
            bodyCls.remove(`no${name}`)
        } else {
            bodyCls.remove(name)
            bodyCls.add(`no${name}`)
        }
    })
}

/** Set the browser's page fav icon by icon
 * @param {ImageInfo} icon
 * @param {string} defaultSrc */
export function setFavIcon(icon, defaultSrc) {
    setFavIconSrc(icon.uri || defaultSrc)
}
function setFavIconSrc(src) {
    let link = document.querySelector("link[rel~='icon']")
    if (!link) {
        link = document.createElement('link')
        link.rel = 'icon'
        document.querySelector('head').appendChild(link)
    }
    link.href = src
}

const SORT_METHODS = ['GET','POST','PATCH','PUT','DELETE']

/** @param {MetadataOperationType} op */
function opSortName(op) {
    // group related services by model or inherited generic type
    let group = map(op.dataModel, x => x.name) || map(op.request.inherits, x => x.genericArgs && x.genericArgs[0])
    let sort1 = group ? group + map(SORT_METHODS.indexOf(op.method || 'ANY'), x => x === -1 ? '' : x.toString()) : 'z'
    return sort1 + `_` + op.request.name
}

/** Sort & group operations operations in logical order
 * @param {MetadataOperationType[]} ops
 * @return {MetadataOperationType[]} */
export function sortOps(ops) {
    ops.sort((a,b) => opSortName(a).localeCompare(opSortName(b)))
    return ops
}

let defaultIcon = globalThis.Server.ui.theme.modelIcon ||
    { svg:`<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><g fill="none" stroke="currentColor" stroke-width="1.5"><path d="M5 12v6s0 3 7 3s7-3 7-3v-6"/><path d="M5 6v6s0 3 7 3s7-3 7-3V6"/><path d="M12 3c7 0 7 3 7 3s0 3-7 3s-7-3-7-3s0-3 7-3Z"/></g></svg>` }

/** Get API Icon
 * @param {{op:MetadataOperationType?,type:MetadataType?}} opt
 * @return {{svg:string}} */
export function getIcon({op, type}) {
    if (op) {
        let img = op.request.icon
            || typeOfRef(op.viewModel)?.icon
            || typeOfRef(op.dataModel)?.icon
        if (img)
            return img
    }
    if (type && type.icon) {
        return type.icon
    }
    return defaultIcon
}
/*:minify*/
