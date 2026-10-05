
import { createApp, nextTick } from 'vue'
import { createWebHistory, createRouter } from "vue-router"
import ServiceStackVue, { useFormatters } from "@servicestack/vue"
import App from './App.mjs'
import ModelPicker from './components/ModelPicker.mjs'
import { CheckBox } from './components/CheckBox.mjs'
import ai from './ai.mjs'
import LayoutModule from './modules/layout.mjs'
import ChatModule from './modules/chat/index.mjs'
import ModelSelectorModule from './modules/model-selector.mjs'
import IconsModule from './modules/icons.mjs'
import { utilsFunctions, utilsFormatters } from './utils.mjs'
import { marked, markedFallback } from './markdown.mjs'
import { AppContext } from './ctx.mjs'
import { afterPaint, importExtensions, installExtensions } from './startup.mjs'

const Components = { ModelPicker, CheckBox }

const BuiltInModules = {
    LayoutModule,
    ChatModule,
    ModelSelectorModule,
    IconsModule,
}


export async function createContext({ deferExtensions = false } = {}) {
    const app = createApp(App)

    app.use(ServiceStackVue)
    Object.keys(Components).forEach(name => {
        if (app.component(name) !== Components[name])
            app.component(name, Components[name])
    })

    const fmt = Object.assign({}, useFormatters(), utilsFormatters())
    const utils = Object.assign({}, utilsFunctions())
    const routes = []

    const ctx = new AppContext({ app, routes, ai, fmt, utils, marked, markedFallback })
    app.provide('ctx', ctx)
    await ctx.init()

    ctx.installedModules = []

    // Install built-in modules sequentially
    Object.entries(BuiltInModules).forEach(([name, module]) => {
        try {
            module.install(ctx)
            ctx.installedModules.push({ extension: { id: name }, module: { default: module } })
            console.log(`Installed built-in: ${name}`)
        } catch (e) {
            console.error(`Failed to install built-in ${name}:`, e)
        }
    })

    // Register all components with Vue
    Object.entries(ctx._components).forEach(([name, component]) => {
        if (app.component(name) !== component)
            app.component(name, component)
    })

    // Add fallback route and create router
    const fallbackRoute = { path: '/:fallback(.*)*', name: 'llms-fallback', component: ctx.component('Home') }
    routes.push(fallbackRoute)
    routes.forEach(r => r.path = ai.base + r.path)
    ctx.router = createRouter({
        history: createWebHistory(),
        routes,
    })
    const savedPath = ctx.layout.path
    const builtInRouteCount = routes.length
    app.use(ctx.router)

    ctx.router.beforeEach((to, from) => {
        const title = to.meta.title || 'Chat'
        console.debug('router:change', to.path, title)
        ctx.setLayout({ path: to.path })
        ctx.setState({ title })
        document.title = title
        return true
    })
    let startup
    ctx.start = () => startup ??= (async () => {
        await afterPaint()
        ctx.modules = await importExtensions(ctx.state.extensions)
        await installExtensions(ctx, ctx.modules)

        // Extensions can add components, routes and navigation guards after mounting.
        Object.entries(ctx._components).forEach(([name, component]) => {
            if (app.component(name) !== component) app.component(name, component)
        })
        // Preserve an extension's Home override for fallback navigation.
        fallbackRoute.component = ctx.component('Home')
        ctx.router.addRoute(fallbackRoute)
        routes.slice(builtInRouteCount).forEach(route => {
            route.path = ai.base + route.path
            ctx.router.addRoute(route)
        })
        ctx._onRouterBeforeEach.forEach(ctx.router.beforeEach)
        performance.mark('llms:extensions-installed')

        // Keep chat gated until prompts, profile settings and tools are loaded.
        await ctx.load()
        await ctx.router.isReady()
        const path = ai.hasAccess
            ? savedPath && (location.pathname === ai.resolvePath('/') || (ai.base && location.pathname === ai.base)) && !location.search
                ? savedPath
                : ctx.router.currentRoute.value.fullPath
            : ai.resolvePath('/')
        // Re-match deep links that initially hit the fallback before extensions added routes.
        await ctx.router.replace(path)
        ctx.setState({ startupReady: true })
        await nextTick()
        performance.mark('llms:startup-ready')
    })()

    // Existing hosts can continue awaiting a fully initialized context before mounting.
    if (!deferExtensions) await ctx.start()
    return ctx
}
