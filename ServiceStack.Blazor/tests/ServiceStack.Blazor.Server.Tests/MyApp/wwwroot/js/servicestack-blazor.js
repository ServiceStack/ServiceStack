/* DOM functions used in Blazor Components */
JS = (function () {

    let dotnetRefs = []
    let NavKeys = 'Escape,ArrowLeft,ArrowRight,ArrowUp,ArrowDown,Home,End'.split(',')
    let InputTags = 'INPUT,SELECT,TEXTAREA'.split(',')
    let onKeyNav = e => {
        let hasModifierKey = e.shiftKey || e.ctrlKey || e.altKey || e.metaKey || e.code === 'MetaLeft' || e.code === 'MetaRight'
        if (hasModifierKey || (e.target && InputTags.indexOf(e.target.tagName) >= 0)) return
        if (NavKeys.indexOf(e.key) == -1) return
        e.preventDefault()
        dotnetRefs.forEach(dotnetRef => {
            dotnetRef.invokeMethodAsync('OnKeyNav', e.key)
        })
    }
    let el = sel => typeof sel == "string" ? document.querySelector(sel) : sel

    return {
        get(name) { return window[name] },
        /* Loading */
        prerenderedPage() {
            const el = document.querySelector('#app-loading .content')
            return el && el.innerHTML || ''
        },
        invoke(target, fnName, args) {
            let f = target[fnName]
            if (typeof f == 'function') {
                let ret = f.apply(target, args || [])
                return ret
            }
            return f
        },
        addClass(sel, ...classes) {
            el(sel).classList.add(...classes)
        },
        removeClass(sel, ...classes) {
            el(sel).classList.remove(...classes)
        },
        registerKeyNav(dotnetRef) {
            dotnetRefs.push(dotnetRef)
            if (dotnetRefs.length == 1) {
                document.addEventListener('keydown', onKeyNav)
            }
        },
        disposeKeyNav(dotnetRef) {
            dotnetRefs = dotnetRefs.filter(x => x != dotnetRef)
            if (dotnetRefs.length == 0) {
                document.removeEventListener('keydown', onKeyNav)
            }
        },
    }
})()
