/* DOM functions used in Blazor Components */
JS = (function () {
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
            return null
        }
    }
})()
