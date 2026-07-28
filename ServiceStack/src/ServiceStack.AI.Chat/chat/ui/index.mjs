
import { createApp } from 'vue'
import { createWebHistory, createRouter } from "vue-router"
import ServiceStackVue, { useFormatters } from "@servicestack/vue"
import App from './App.mjs'
import ai from './ai.mjs'
import LayoutModule from './modules/layout.mjs'
import ChatModule from './modules/chat/index.mjs'
import ModelSelectorModule from './modules/model-selector.mjs'
import IconsModule from './modules/icons.mjs'
import { utilsFunctions, utilsFormatters } from './utils.mjs'
import { marked, markedFallback } from './markdown.mjs'
import { AppContext } from './ctx.mjs'

const Components = {
}

const BuiltInModules = {
    LayoutModule,
    ChatModule,
    ModelSelectorModule,
    IconsModule,
}


export async function createContext() {
    const app = createApp(App)

    app.use(ServiceStackVue)
    Object.keys(Components).forEach(name => {
        app.component(name, Components[name])
    })

    const fmt = Object.assign({}, useFormatters(), utilsFormatters())
    const utils = Object.assign({}, utilsFunctions())
    const routes = []

    const ctx = new AppContext({ app, routes, ai, fmt, utils, marked, markedFallback })
    app.provide('ctx', ctx)
    await ctx.init()

    // Load modules in parallel
    const validExtensions = ctx.state.extensions.filter(x => x.path);
    ctx.modules = await Promise.all(validExtensions.map(async extension => {
        try {
            const module = await import(extension.path)
            const order = module.default.order || 0
            return { extension, module, order }
        } catch (e) {
            console.error(`Failed to load extension module ${extension.name}:`, e)
            return null
        }
    }))

    // sort modules by order
    ctx.modules.sort((a, b) => a.order - b.order)

    const installedModules = []

    // Install built-in modules sequentially
    Object.entries(BuiltInModules).forEach(([name, module]) => {
        try {
            module.install(ctx)
            installedModules.push({ extension: { id: name }, module: { default: module } })
            console.log(`Installed built-in: ${name}`)
        } catch (e) {
            console.error(`Failed to install built-in ${name}:`, e)
        }
    })

    // Install extensions sequentially
    for (const result of ctx.modules) {
        if (result && result.module.default && result.module.default.install) {
            try {
                result.module.default.install(ctx)
                installedModules.push(result)
                console.log(`Installed extension: ${result.extension.id}`)
            } catch (e) {
                console.error(`Failed to install extension ${result.extension.id}:`, e)
            }
        }
    }

    // Register all components with Vue
    Object.entries(ctx._components).forEach(([name, component]) => {
        app.component(name, component)
    })

    // Add fallback route and create router
    routes.push({ path: '/:fallback(.*)*', component: ctx.component('Home') })
    routes.forEach(r => r.path = ai.base + r.path)
    ctx.router = createRouter({
        history: createWebHistory(),
        routes,
    })
    app.use(ctx.router)

    ctx.router.beforeEach((to, from) => {
        const title = to.meta.title || 'Chat'
        console.debug('router:change', to.path, title)
        ctx.setLayout({ path: to.path })
        ctx.setState({ title })
        document.title = title
        return true
    })
    ctx._onRouterBeforeEach.forEach(ctx.router.beforeEach)

    if (ai.hasAccess) {
        if (ctx.layout.path && location.pathname === '/' && !location.search) {
            console.log('redirecting to saved path: ', ctx.layout.path)
            ctx.router.push({ path: ctx.layout.path })
        }
    } else {
        ctx.router.push({ path: '/' })
    }

    ctx.installedModules = installedModules
    await ctx.load()

    return ctx
}
