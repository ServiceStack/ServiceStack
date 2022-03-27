import { EventBus } from '@servicestack/client'

/*minify:*/

/** @typedef {<T>(args:T) => T} Identity */

/** @typedef {{subscribe:function(string,Function):void,publish:function(string,any):void}} EventBusFn */
/** @typedef {{
    events: EventBusFn;
    readonly petite: any;
    components: function(Object.<string,Function>): void;
    component: function(string, any): void;
    template: function(string, string): void;
    templates: function(Object.<string,string>): void;
    directive: function(string, Function): void;
    prop: function(string, any): void;
    props: function(Object.<string,any>): void;
    build: function(Object.<string,any>): any;
    plugin: function(Object.<string,any>): void;
    import: function(string): Promise<any>;
    onStart: function(Function): void;
    start: function(): void;
    unsubscribe: function(): void;
    createApp: function(any): any;
    nextTick: function(Function): void;
    reactive: Identity; 
}} App
*/

/** App to register and build a PetiteVueApp
 * @param {{createApp:(initialData?:any) => any,nextTick:(fn:Function) => void,reactive:Identity}} PetiteVue 
 * @returns {App}
 */
export function createApp(PetiteVue) {
    let Components = {}
    let Directives = {}
    let Props = {}
    let OnStart = []
    let petite = null
    /** @type {EventBusFn} */
    let events = new EventBus()

    function assertNotBuilt(name) {
        if (petite)
            throw new Error(`Cannot call App.${name}() after App is built`)
    }
    function template(name, $template) {
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
    function create(name, x) {
        if (typeof x == "string")
            return template(name, x)
        else if (typeof x == "function")
            return x
        throw new Error(`${name} is not a Component or $template`)
    }
    function register(name, component) {
        window[name] = Components[name] = create(name, component)
    }

    return {
        events,
        get petite() { return petite },
        /** @param {{[index:string]:Function}} components */
        components(components) {
            assertNotBuilt('components')
            Object.keys(components).forEach(name => register(name, components[name]))
        },
        /** @param {string} name
         *  @param {any} component */
        component(name, component) {
            assertNotBuilt('component')
            register(name, component)
        },
        /** @param {string} name
         *  @param {string} $template */
        template(name, $template) {
            assertNotBuilt('template')
            register(name, template(name, $template))
        },
        /** @param {{[index:string]:string}} templates */
        templates(templates) {
            assertNotBuilt('template')
            Object.keys(templates).forEach(name => register(name, template(name, templates[name])))
        },
        /** @param {string} name
         *  @param {Function} fn */
        directive(name, fn) {
            assertNotBuilt('directive')
            Directives[name] = fn
        },
        /** @param {string} name
         *  @param {any} val */
        prop(name, val) {
            assertNotBuilt('prop')
            Props[name] = val
        },
        /** @param {{[index:string]:any}} props */
        props(props) {
            assertNotBuilt('props')
            Object.assign(Props, props)
        },
        /** @param {{[index:string]:any}} args */
        build(args) {
            if (!PetiteVue)
                throw new ReferenceError('PetiteVue is not defined')
            Object.assign(this, PetiteVue)

            petite = PetiteVue.createApp({
                ...Props,
                ...Components,
                ...args,
            })
            Object.keys(Directives).forEach(name => petite.directive(name, Directives[name]))
            return petite
        },
        /** @param {{[index:string]:any}} plugins */
        plugin(plugins) {
            Object.keys(plugins).forEach(name => {
                let f = plugins[name]
                this[name] = typeof f == 'function' ? f.bind(this) : f
            })
        },
        /** @param {string} src
         *  @return {Promise<any>} */
        import(src) {
            return new Promise((resolve, reject) => {
                let s = document.createElement('script')
                s.setAttribute('src', src)
                s.addEventListener('load', resolve)
                s.addEventListener('error', reject)
                document.body.appendChild(s)
            })
        },
        /** @param {Function} f */
        onStart(f) {
            OnStart.push(f)
        },
        start() {
            OnStart.forEach(f => f(this))
        },
        unsubscribe() {
            if (this.sub) {
                this.sub.unsubscribe()
                this.sub = null
            }
        },
        createApp: PetiteVue.createApp,
        nextTick: PetiteVue.nextTick,
        reactive: PetiteVue.reactive,
    }
}
/*:minify*/
