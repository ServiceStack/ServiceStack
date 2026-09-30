import { languages } from './languages.mjs'
import { lazyModule, loadCodeEditor } from '/ui/lazy.mjs'

export default {
    install(ctx) {
        const load = lazyModule(() => import('./pages.mjs'), module => module.init(ctx))
        const CodePage = async () => {
            const [module] = await Promise.all([load(), loadCodeEditor(ctx)])
            return module.CodePage
        }
        const CalcPage = () => load().then(module => module.CalcPage)

        const LANGUAGE_TOOLS = Object.values(languages).map(x => x.tool).filter(Boolean)
        ctx.setLeftIcons({
            code: {
                component: {
                    template: `<svg @click="$ctx.togglePath('/code')" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><g fill="none"><path d="M0 0h24v24H0z"/><path fill="currentColor" d="M14.486 3.143a1 1 0 0 1 .692 1.233l-4.43 15.788a1 1 0 0 1-1.926-.54l4.43-15.788a1 1 0 0 1 1.234-.693M7.207 7.05a1 1 0 0 1 0 1.414L3.672 12l3.535 3.535a1 1 0 1 1-1.414 1.415L1.55 12.707a1 1 0 0 1 0-1.414L5.793 7.05a1 1 0 0 1 1.414 0m9.586 1.414a1 1 0 1 1 1.414-1.414l4.243 4.243a1 1 0 0 1 0 1.414l-4.243 4.243a1 1 0 0 1-1.414-1.415L20.328 12z"/></g></svg>`,
                    setup() {
                    }
                },
                isVisible() {
                    return LANGUAGE_TOOLS.some(tool => ctx.state.tool?.groups?.core_tools?.includes(tool))
                },
                isActive({ path }) {
                    return ctx.matchesPath(path, '/code')
                },
                title: 'Run Code',
            },
            calc: {
                component: {
                    template: `
                        <svg @click="$ctx.togglePath('/calc')" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 14 14">
                            <path d="M0 0h14v14H0z" fill="none" />
                            <g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round">
                                <path d="M11.5.5h-9a1 1 0 0 0-1 1v11a1 1 0 0 0 1 1h9a1 1 0 0 0 1-1v-11a1 1 0 0 0-1-1m-10 5h11" />
                                <path d="M4.25 8.5a.25.25 0 0 1 0-.5m0 .5a.25.25 0 0 0 0-.5M7 8.5A.25.25 0 0 1 7 8m0 .5A.25.25 0 0 0 7 8m2.75.5a.25.25 0 0 1 0-.5m0 .5a.25.25 0 0 0 0-.5m-5.5 3a.25.25 0 1 1 0-.5m0 .5a.25.25 0 1 0 0-.5M7 11a.25.25 0 1 1 0-.5m0 .5a.25.25 0 1 0 0-.5m2.75.5a.25.25 0 1 1 0-.5m0 .5a.25.25 0 1 0 0-.5M10 3H9" />
                            </g>
                        </svg>
                    `,
                    setup() {
                    }
                },
                isVisible() {
                    return ctx.state.tool?.groups?.core_tools?.includes('calc')
                },
                isActive({ path }) {
                    return ctx.matchesPath(path, '/calc')
                },
                title: 'Calculator',
            }
        })

        ctx.routes.push({ path: '/code', component: CodePage, meta: { title: 'Run Code', header: false } })
        ctx.routes.push({ path: '/calc', component: CalcPage, meta: { title: 'Calculator', header: false } })
    }
}
