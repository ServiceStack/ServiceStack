import { EventBus, each } from '@servicestack/client'
/*minify:*/
/**
 * App to register and build a PetiteVue App
 * @class
 */
function PetiteVueApp() {
    let Components = {}
    let Directives = {}
    let Props = {}
    let OnStart = []
    this.petite = null
    this.events = new EventBus()
    
    let assertNotBuilt = (name) => {
        if (this.petite)
            throw new Error(`Cannot call App.${name}() after App is built`)
    }
    function template (name, $template) {
        if (typeof $template != 'string')
            throw new Error(`Template ${name} must be a template selector not ${$template}`)
        return create(name, function(args, apply) {
            let to = {
                $template,
                ...args
            }
            if (apply) apply(to,name)
            return to
        })
    }
    function create (name, x) {
        if (typeof x == "string")
            return template(name, x)
        else if (typeof x == "function")
            return x
        throw new Error(`${name} is not a Component or $template`)
    }
    function register (name, component) {
        window[name] = Components[name] = create(name, component)
    }
    this.components = function (components) {
        assertNotBuilt('components')
        Object.keys(components).forEach(name => register(name, components[name]))
    }
    this.component = function(name, component) {
        assertNotBuilt('component')
        register(name, component)
    }
    this.template = function (name, $template) {
        assertNotBuilt('template')
        register(name, template(name, $template))
    }
    this.templates = function (templates) {
        assertNotBuilt('template')
        Object.keys(templates).forEach(name => register(name, template(name, templates[name])))
    }
    
    this.directive = function(name, fn) {
        assertNotBuilt('directive')
        Directives[name] = fn
    }
    
    this.prop = function (name, val) {
        assertNotBuilt('prop')
        Props[name] = val
    }
    this.props = function (props) {
        assertNotBuilt('props')
        Object.assign(Props, props)
    }

    this.build = function(args) {
        if (!window.PetiteVue)
            throw new ReferenceError('PetiteVue is not defined')
        Object.assign(this, window.PetiteVue)
        
        this.petite = PetiteVue.createApp({
            ...Props,
            ...Components,
            ...args,
        })
        Object.keys(Directives).forEach(name => this.petite.directive(name, Directives[name]))
        return this.petite
    }
    
    this.plugin = function (plugins) {
        Object.keys(plugins).forEach(name => {
            let f = plugins[name]
            this[name] = typeof f == 'function' ? f.bind(this) : f 
        })
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
    
    this.onStart = function (f) {
        OnStart.push(f)
    }
    this.start = function () {
        OnStart.forEach(f => f(this))
    }

    Object.assign(this, window.PetiteVue||{})
}
/*:minify*/
