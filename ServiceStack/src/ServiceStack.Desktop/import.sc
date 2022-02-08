* Usage: x run import.sc *
* x mix svg-action svg-alert svg-av svg-communication svg-content svg-device svg-editor svg-file svg-hardware svg-image svg-maps svg-navigation svg-places svg-social svg-toggle *

{{ const libs = { 
    '@servicestack/client/servicestack-client.min.js': 'https://unpkg.com/@servicestack/client/dist/servicestack-client.min.js',
    '@servicestack/vue/servicestack-vue.min.js': 'https://unpkg.com/@servicestack/vue/dist/servicestack-vue.umd.js',
    '@servicestack/react/servicestack-react.min.js': 'https://unpkg.com/@servicestack/react/dist/servicestack-react.min.js', 
     
    'vue/vue.min.js': 'https://unpkg.com/vue@2/dist/vue.min.js', 
    'vue-class-component/vue-class-component.min.js': 'https://unpkg.com/vue-class-component/dist/vue-class-component.min.js', 
    'vue-property-decorator/vue-property-decorator.min.js': 'https://unpkg.com/vue-property-decorator', 
    'vue-router/vue-router.min.js': 'https://unpkg.com/vue-router@3/dist/vue-router.min.js',
    'vuex/vuex.min.js': 'https://unpkg.com/vuex@3/dist/vuex.min.js',
    'portal-vue/portal-vue.min.js': 'https://unpkg.com/portal-vue/dist/portal-vue.umd.min.js',
    'bootstrap-vue/bootstrap-vue.min.js': 'https://unpkg.com/bootstrap-vue/dist/bootstrap-vue.min.js',
    
    'react/react.production.min.js':'https://unpkg.com/react/umd/react.production.min.js',
    'react-dom/react-dom.production.min.js':'https://unpkg.com/react-dom/umd/react-dom.production.min.js',
    'react-router/react-router.min.js':'https://unpkg.com/react-router@5/cjs/react-router.min.js',
    'react-router-dom/react-router-dom.min.js':'https://unpkg.com/react-router-dom@5/cjs/react-router-dom.min.js',
    'mobx/mobx.min.js': 'https://unpkg.com/mobx/dist/mobx.umd.production.min.js',
    'redux/redux.min.js': 'https://unpkg.com/redux/dist/redux.min.js',
    'react-redux/react-redux.min.js': 'https://unpkg.com/react-redux/dist/react-redux.min.js',
     
    'bootstrap/bootstrap.min.js': 'https://unpkg.com/bootstrap/dist/js/bootstrap.min.js', 
    'popper/popper.min.js':'https://cdn.jsdelivr.net/npm/popper.js/dist/umd/popper.min.js',
    'jquery/jquery.min.js': 'https://unpkg.com/jquery/dist/jquery.min.js',
} 
}}

#each libs
    it.Value |> urlContentsWithCache() |> to => js
    let path = `lib/js/${it.Key}`
    `writing file ${path}...`
    path.writeFile(js) |> end
/each

{{ css = {
    'bootstrap/bootstrap.css': 'https://cdn.jsdelivr.net/npm/bootstrap/dist/css/bootstrap.min.css',
    'litewind/litewind.css':   'https://raw.githubusercontent.com/mythz/site/main/css/litewind.css',
}
}}

#each css
    it.Value |> urlContentsWithCache() |> to => js
    let path = `lib/css/${it.Key}`
    `writing file ${path}...`
    path.writeFile(js) |> end
/each

