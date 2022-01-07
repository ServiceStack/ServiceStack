/**
 * App to register and build a PetiteVue App
 * @class
 * @requires PetiteVue
 */
function PetiteVueApp() {
    let Components = []
    let Directives = {}
    this.petite = null
    
    let assertNotBuilt = (name) => {
        if (this.petite)
            throw new Error(`Cannot call App.${name}() after App is built`)
    }
    let assertComponent = (name, fn) => {
        if (typeof fn != "function")
            throw new Error(`${name} is not a Component`)
        return fn
    }
    
    this.components = function (components) {
        assertNotBuilt('components')
        Object.keys(components).forEach(name => {
            window[name] = Components[name] = assertComponent(name, components[name])
        })
    }
    
    this.component = function(name, component) {
        assertNotBuilt('component')
        window[name] = Components[name] = assertComponent(name, component)
    }
    
    this.templates = function (templates) {
        assertNotBuilt('templates')
        Object.keys(templates).forEach(name => {
            let $template = templates[name]
            if (typeof $template != 'string')
                throw new Error('Template ${name} must be a template selector not ${$template}')
            window[name] = Components[name] = assertComponent(name, function(args, apply) {
                let to = {
                    $template,
                    ...args
                }
                if (apply) apply(to,name)
                return to
            })
        })
    }
    
    this.directive = function(name, fn) {
        assertNotBuilt('directive')
        Directives[name] = fn
    }

    this.build = function(args) {
        if (!window.PetiteVue)
            throw new Error('PetiteVue does not exist')
        Object.assign(this, window.PetiteVue)
        
        this.petite = PetiteVue.createApp({
            ...Components.reduce((acc,x) => { acc[x] = Components[x]; return acc }, {}),
            ...args,
        })
        Object.keys(Directives).forEach(name => {
            this.petite.directive(name, Directives[name])
        })
        return this.petite
    } 
    
    this.import = function (src) {
        return new Promise((resolve, reject) => {
            let s = document.createElement('script')
            s.setAttribute('src', src)
            s.addEventListener('load', resolve)
            s.addEventListener('error', reject)
            document.body.appendChild(s)
        })
    }

    if (window.PetiteVue) Object.assign(this, window.PetiteVue)
}
