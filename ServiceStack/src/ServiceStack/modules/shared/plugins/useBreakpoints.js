/*minify:*/
/** @typedef {import('../js/createApp').App} App */
/** @typedef {Record<'2xl'|'xl'|'lg'|'md'|'sm',boolean>} Breakpoints */
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
 * @param {App} App
 * @param {{handlers: {change({previous: *, current: *}): void}}} options
 * @returns {Breakpoints & {previous:Breakpoints,current:Breakpoints,snap:()=>void}}
 */
function useBreakpoints(App, options) {
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
