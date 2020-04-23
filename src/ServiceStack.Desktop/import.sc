* x mix svg-action svg-alert svg-av svg-communication svg-content svg-device svg-editor svg-file svg-hardware svg-image svg-maps svg-navigation svg-places svg-social svg-toggle *

{{ const libs = { 
    '@servicestack/vue/servicestack-vue.umd.js': 'https://unpkg.com/@servicestack/vue',
    '@servicestack/react/servicestack-react.umd.js': 'https://unpkg.com/@servicestack/react/dist/servicestack-react.min.js', 
     
    'vue/vue.min.js': 'https://unpkg.com/vue/dist/vue.min.js', 
    'vue-class-component/vue-class-component.min.js': 'https://unpkg.com/vue-class-component/dist/vue-class-component.min.js', 
    'vue-property-decorator/vue-property-decorator.umd.js': 'https://unpkg.com/vue-property-decorator', 
    'vue-router/vue-router.min.js': 'https://unpkg.com/vue-router/dist/vue-router.min.js',
    
    'react/react.production.min.js':'https://unpkg.com/react/umd/react.production.min.js',
    'react-dom/react-dom.production.min.js':'https://unpkg.com/react-dom/umd/react-dom.production.min.js',
    'react-router/react-router.min.js':'https://unpkg.com/react-router/umd/react-router.min.js',
    'react-router-dom/react-router-dom.min.js':'https://unpkg.com/react-router-dom/umd/react-router-dom.min.js',
     
    'bootstrap/bootstrap.min.js': 'https://unpkg.com/bootstrap/dist/js/bootstrap.min.js', 
    'bootstrap/popper.min.js':'https://cdn.jsdelivr.net/npm/popper.js/dist/umd/popper.min.js',
    'jquery/jquery.min.js': 'https://unpkg.com/jquery/dist/jquery.min.js',
} }}

#each libs
    it.Value |> urlContentsWithCache() |> to => js
    let path = `lib/js/${it.Key}`
    `writing file ${path}...`
    path.writeFile(js) |> end
/each
