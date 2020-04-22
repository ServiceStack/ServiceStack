* x mix svg-action svg-alert svg-av svg-communication svg-content svg-device svg-editor svg-file svg-hardware svg-image svg-maps svg-navigation svg-places svg-social svg-toggle *

{{ const libs = { 
    '@servicestack/vue/servicestack-vue.umd.js': 'https://unpkg.com/@servicestack/vue', 
    'vue/dist/vue.js': 'https://unpkg.com/vue/dist/vue.min.js', 
    'vue-class-component/vue-class-component.js': 'https://unpkg.com/vue-class-component', 
    'vue-property-decorator/vue-property-decorator.umd.js': 'https://unpkg.com/vue-property-decorator', 
    'vue-router/dist/vue-router.min.js': 'https://unpkg.com/vue-router/dist/vue-router.min.js', 
    'bootstrap/bootstrap.min.js': 'https://unpkg.com/bootstrap/dist/js/bootstrap.min.js', 
    'jquery/jquery.min.js': 'https://unpkg.com/jquery/dist/jquery.min.js'
} }}

#each libs
    it.Value |> urlContentsWithCache() |> assignTo => js
    let path = `lib/js/${it.Key}`
    `writing file ${path}...`
    path.writeFile(js) |> end
/each
