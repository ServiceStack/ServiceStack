import { h } from 'vue'
import { lazyModule } from '/ui/lazy.mjs'
import { decisionTreePath } from './StudioIcon.mjs'

export default {
    order: -69,
    install(ctx) {
        const closeRight = () => {
            if (ctx.layoutVisible('right')) ctx.toggleLayout('right', false)
        }
        const open = () => {
            closeRight()
            ctx.togglePath('/jev', { left: false })
        }
        const load = lazyModule(() => import('./JevPage.mjs'))
        ctx.setLeftIcons({
            jev: {
                title: 'Decision Studio',
                component: {
                    methods: { open },
                    template: `<svg @click="open" xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 32 32" role="button" tabindex="0" @keydown.enter="open" @keydown.space.prevent="open" aria-label="Decision Studio"  class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3"><path d="M0 0h32v32H0z" fill="none"/><path fill="currentColor" d="${decisionTreePath}"/></svg>`,
                },
                isActive: ({ path }) => ctx.matchesPath(path, '/jev'),
            },
        })
        ctx.routes.push({
            path: '/jev',
            beforeEnter: () => { closeRight() },
            component: () =>
                load().then((m) => ({
                    render: () =>
                        h(m.default, {
                            key:
                                (ctx.ai.base || location.origin) +
                                '::' +
                                (ctx.ai.auth?.userName || ctx.ai.auth?.userId || 'default'),
                        }),
                })),
            // Jev's panels own scrolling; allow its compact header to extend into the top bar.
            meta: { title: 'Decision Studio', header: true, pageScroll: false },
        })
    },
}
