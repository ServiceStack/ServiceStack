import { each, on } from "@servicestack/client"

/*minify:*/
/** @typedef {import('../js/createApp').App} App */
/** @typedef {{'2xl':boolean,xl:boolean,lg:boolean,md:boolean,sm:boolean}} Breakpoints */
/**
 * Returns a reactive store that maintains different resolution states:
 * Defaults: 2xl:1536, xl:1280, lg:1024, md:768, sm:640
 * E.g. at 1200px: { 2xl:false, xl:false, lg:true, md:true, sm:true }
 * Events:
 *   breakpoint:change - the browser width changed breakpoints
 * @param {App} App
 * @param {{handlers: {change({previous: *, current: *}): void}}} options
 * @returns {Breakpoints & {previous:Breakpoints,current:Breakpoints,snap:()=>void}}
 */
export function useBreakpoints(App, options) {
    if (!options) options = {}
    let {resolutions, handlers} = options
    if (!resolutions) resolutions = {'2xl': 1536, xl: 1280, lg: 1024, md: 768, sm: 640}
    let sizes = Object.keys(resolutions)

    let previous = {}
    let events = App.events

    let store = App.reactive({
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
    })

    on(window, {
        resize: () => store.snap()
    })

    if (handlers && handlers.change)
        events.subscribe('breakpoint:change', args => handlers.change(args))

    App.onStart(app => store.snap())

    return store
}
/*:minify*/
