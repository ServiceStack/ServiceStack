import { $$ } from "@servicestack/client"
/*minify:*/

/** @typedef {import('../js/createApp').App} App */

/**
 * Implements https://tailwindui.com transition states by encoding in data-transition attr, e.g:
 * data-transition="{ 
 *   entering: { cls:'transition ease-in-out duration-300 transform', from:'-translate-x-full', to:'translate-x-0'}, 
 *   leaving:  { cls:'transition ease-in-out duration-300 transform', from:'translate-x-0',     to:'-translate-x-full' } 
 * }" data-transition-for="sidebar"
 * @param {App} App
 * @param {{[index:string]:boolean}} transitions
 * @return {(prop:string,enter?:boolean) => boolean}
 */
export function useTransitions(App, transitions) {

    /**
     * Invoke a named Tailwind Transition animation definition
     * 
     * @param {string} prop
     * @param {boolean} [enter]
     * @return {boolean}
     */
    function transition(prop, enter) {
        let transitionEls = $$(`[data-transition-for=${prop}]`)
        transitionEls.forEach(el => {
            let duration = 300
            let attr = el.getAttribute('data-transition')
            el.style.display = null

            if (attr) {
                let rule = new Function("return " + attr)()
                let prevTransition = rule[enter ? 'leaving' : 'entering']
                let nextTransition = rule[enter ? 'entering' : 'leaving']
                if (prevTransition.cls) {
                    if (el.className.indexOf(prevTransition.cls) < 0) el.className.replace(prevTransition,'').trim()
                }
                if (nextTransition.cls) {
                    if (el.className.indexOf(nextTransition.cls) < 0) el.className += ` ${nextTransition.cls}`
                    let clsDuration = nextTransition.cls.split(' ').find(x => x.startsWith('duration-'))
                    if (clsDuration) {
                        duration = parseInt(clsDuration.split('-')[1])
                    }
                }

                el.className = el.className.replace(` ${prevTransition.to}`, '').trim()
                el.className += ` ${nextTransition.from}`
                setTimeout(() => {
                    el.className = el.className.replace(nextTransition.from, nextTransition.to).trim()
                }, duration)
            }

            setTimeout(() => {
                el.style.display = enter ? null : 'none'
            }, duration * 2)
        })
        App.events.publish(prop, enter)
        return enter
    }

    let PropValues = {}
    if (transitions) {
        Object.keys(transitions).forEach(prop => {
            let transProp = transitions[prop]
            if (typeof transProp != 'boolean')
                throw new Error(`useTransitions({ ${prop} }) must be a boolean not '${transProp}'`)
            PropValues[prop] = transProp
        })
    }

    App.props({
        transition(name, val) {
            let enter = typeof val == 'boolean'
                ? val
                : PropValues[name] = !PropValues[name]
            return transition(name, enter)
        },
    })
    
    return transition
}
/*:minify*/
