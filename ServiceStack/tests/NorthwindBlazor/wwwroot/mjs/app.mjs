import { createApp, reactive, ref, computed } from "vue"
import { JsonApiClient, $1, $$ } from "@servicestack/client"
import ServiceStackVue from "@servicestack/vue"
import GettingStarted from "./components/GettingStarted.mjs"

let client = null, Apps = []
let AppData = {
    init:false
}
export { client, Apps }

/** Shared Global Components */
const Components = {
    GettingStarted,
}
const CustomElements = [
    'lite-youtube'
]

const alreadyMounted = el => el.__vue_app__

const mockArgs = { attrs:{}, slots:{}, emit:() => {}, expose: () => {} }
function hasTemplate(el,component) {
    return !!(el.firstElementChild
        || component.template
        || (component.setup && typeof component.setup({}, mockArgs) == 'function'))
}

/** Mount Vue3 Component
 * @param sel {string|Element} - Element or Selector where component should be mounted
 * @param component
 * @param [props] {any} */
export function mount(sel, component, props) {
    if (!AppData.init) {
        init(globalThis)
    }
    const el = $1(sel)
    if (alreadyMounted(el)) return

    if (!hasTemplate(el, component)) {
        // Fallback for enhanced navigation clearing HTML DOM template of Vue App, requiring a force reload
        // Avoid by disabling enhanced navigation to page, e.g. by adding data-enhance-nav="false" to element
        console.warn('Vue Compontent template is missing, force reloading...', el, component)
        blazorRefresh()
        return
    }

    const app = createApp(component, props)
    app.provide('client', client)
    Object.keys(Components).forEach(name => {
        app.component(name, Components[name])
    })
    app.use(ServiceStackVue)
    app.component('RouterLink', ServiceStackVue.component('RouterLink'))
    app.directive('hash', (el, binding) => {
        /** @param {Event} e */
        el.onclick = (e) => {
            e.preventDefault()
            location.hash = binding.value
        }
    })
    if (component.install) {
        component.install(app)
    }
    if (client && !app._context.provides.client) {
        app.provide('client', client)
    }
    app.config.errorHandler = error => { console.log(error) }
    app.config.compilerOptions.isCustomElement = tag => CustomElements.includes(tag)
    app.mount(el)
    Apps.push(app)
    return app
}

async function mountApp(el, props) {
    let appPath = el.getAttribute('data-component')
    if (!appPath.startsWith('/') && !appPath.startsWith('.')) {
        appPath = `../${appPath}`
    }

    const module = await import(appPath)
    unmount(el)
    mount(el, module.default, props)
}

export async function remount() {
    if (!AppData.init) {
        init({ force: true })
    } else {
        mountAll({ force: true })
    }
}

//Default Vue App that gets created with [data-component] is empty, e.g. Blog Posts without Vue components
const DefaultApp = {
    setup() {
        function nav(url) {
            window.open(url)
        }
        return { nav }
    }
}

function blazorRefresh() {
    if (globalThis.Blazor)
        globalThis.Blazor.navigateTo(location.pathname.substring(1), true)
    else
        globalThis.location.reload()
}

export function mountAll(opt) {
    $$('[data-component]').forEach(el => {

        if (opt && opt.force) {
            unmount(el)
        } else {
            if (alreadyMounted(el)) return
        }

        let componentName = el.getAttribute('data-component')
        let propsStr = el.getAttribute('data-props')
        let props = propsStr && new Function(`return (${propsStr})`)() || {}

        if (!componentName) {
            mount(el, DefaultApp, props)
            return
        }

        if (componentName.includes('.')) {
            mountApp(el, props)
            return
        }

        let component = Components[componentName] || ServiceStackVue.component(componentName)
        if (!component) {
            console.error(`Component ${componentName} does not exist`)
            return
        }

        mount(el, component, props)
    })
    $$('[data-module]').forEach(async el => {
        let modulePath = el.getAttribute('data-module')
        if (!modulePath) return
        if (!modulePath.startsWith('/') && !modulePath.startsWith('.')) {
            modulePath = `../${modulePath}`
        }
        try {
            const module = await import(modulePath)
            if (typeof module.default?.load == 'function') {
                module.default.load()
            }
        } catch(e) {
            console.error(`Couldn't load module ${el.getAttribute('data-module')}`, e)
        }
    })
}

/** @param {any} [exports] */
export function init(opt) {
    if (AppData.init) return
    client = JsonApiClient.create()
    AppData = reactive(AppData)
    AppData.init = true
    mountAll(opt)

    if (opt && opt.exports) {
        opt.exports.client = client
        opt.exports.Apps = Apps
    }
}

function unmount(el) {
    if (!el) return

    try {
        if (el.__vue_app__) {
            el.__vue_app__.unmount(el)
        }
    } catch (e) {
        console.log('force unmount', el.id)
        el._vnode = el.__vue_app__ = undefined
    }
}


/* used in :::sh and :::nuget CopyContainerRenderer */
globalThis.copy = function (e) {
    e.classList.add('copying')
    let $el = document.createElement("textarea")
    let text = (e.querySelector('code') || e.querySelector('p')).innerHTML
    $el.innerHTML = text
    document.body.appendChild($el)
    $el.select()
    document.execCommand("copy")
    document.body.removeChild($el)
    setTimeout(() => e.classList.remove('copying'), 3000)
}

document.addEventListener('DOMContentLoaded', () =>
    Blazor.addEventListener('enhancedload', () => {
        remount()
        globalThis.hljs?.highlightAll()
    }))
