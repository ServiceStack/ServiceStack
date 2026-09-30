import { lazyComponent, lazyModule } from '/ui/lazy.mjs'

export default {
    order: 20 - 100,

    install(ctx) {
        const load = lazyModule(() => import('./pages.mjs'), module => {
            ctx.components(module.components)
            Object.entries(module.components).forEach(([name, component]) => ctx.app.component(name, component))
        })
        ctx.components({
            Analytics: lazyComponent(() => load().then(module => module.Analytics)),
        })

        ctx.setLeftIcons({
            analytics: {
                component: {
                    template: `<svg @click="$ctx.togglePath('/analytics')" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="M5 22a1 1 0 0 1-1-1v-8a1 1 0 0 1 2 0v8a1 1 0 0 1-1 1m5 0a1 1 0 0 1-1-1V3a1 1 0 0 1 2 0v18a1 1 0 0 1-1 1m5 0a1 1 0 0 1-1-1V9a1 1 0 0 1 2 0v12a1 1 0 0 1-1 1m5 0a1 1 0 0 1-1-1v-4a1 1 0 0 1 2 0v4a1 1 0 0 1-1 1"/></svg>`
                },
                isActive({ path }) {
                    return ctx.matchesPath(path, '/analytics')
                }
            }
        })

        ctx.routes.push({ path: '/analytics', component: () => load().then(module => module.Analytics), meta: { title: 'Analytics', header: false } })
    }
}
