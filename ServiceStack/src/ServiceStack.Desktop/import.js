// Usage: node install.js

const writeTo = './lib'
const defaultPrefix = 'https://unpkg.com'
const files = {
    js: {
        '@servicestack/client/servicestack-client.min.js':        '/@servicestack/client/dist/servicestack-client.min.js',
        '@servicestack/vue/servicestack-vue.min.js':              '/@servicestack/vue@1/dist/servicestack-vue.umd.js',
        '@servicestack/react/servicestack-react.min.js':          '/@servicestack/react@1/dist/servicestack-react.min.js',

        'vue/vue.min.js':                                         '/vue@2/dist/vue.min.js',
        'vue-class-component/vue-class-component.min.js':         '/vue-class-component/dist/vue-class-component.min.js',
        'vue-property-decorator/vue-property-decorator.min.js':   '/vue-property-decorator',
        'vue-router/vue-router.min.js':                           '/vue-router@3/dist/vue-router.min.js',
        'vuex/vuex.min.js':                                       '/vuex@3/dist/vuex.min.js',
        'portal-vue/portal-vue.min.js':                           '/portal-vue@2/dist/portal-vue.umd.min.js',
        'bootstrap-vue/bootstrap-vue.min.js':                     '/bootstrap-vue@2/dist/bootstrap-vue.min.js',

        'react/react.production.min.js':                          '/react/umd/react.production.min.js',
        'react-dom/react-dom.production.min.js':                  '/react-dom/umd/react-dom.production.min.js',
        'react-router/react-router.min.js':                       '/react-router@5/cjs/react-router.min.js',
        'react-router-dom/react-router-dom.min.js':               '/react-router-dom@5/cjs/react-router-dom.min.js',
        'mobx/mobx.min.js':                                       '/mobx/dist/mobx.umd.production.min.js',
        'redux/redux.min.js':                                     '/redux/dist/redux.min.js',
        'react-redux/react-redux.min.js':                         '/react-redux/dist/react-redux.min.js',

        'bootstrap/bootstrap.min.js':                             '/bootstrap@5/dist/js/bootstrap.min.js',
        'popper/popper.min.js':                                   'https://cdn.jsdelivr.net/npm/popper.js/dist/umd/popper.min.js',
        'jquery/jquery.min.js':                                   '/jquery@3/dist/jquery.min.js',
    },
    css: {
        'bootstrap/bootstrap.css':                                'https://cdn.jsdelivr.net/npm/bootstrap/dist/css/bootstrap.min.css',
        'litewind/litewind.css':                                  'https://raw.githubusercontent.com/mythz/site/main/css/litewind.css',
    }
}

const path = require('path')
const fs = require('fs')
const http = require('http')
const https = require('https')

Object.keys(files).forEach(dir => {
    const dirFiles = files[dir]
    Object.keys(dirFiles).forEach(name => {
        let url = dirFiles[name]
        if (url.startsWith('/'))
            url = defaultPrefix + url
        const toFile = path.join(writeTo, dir, name)
        const toDir = path.dirname(toFile)
        if (!fs.existsSync(toDir)) {
            fs.mkdirSync(toDir, { recursive: true })
        }
        httpDownload(url, toFile, 5)
    })
})

function httpDownload(url, toFile, retries) {
    const client = url.startsWith('https') ? https : http
    const retry = (e) => {
        console.log(`get ${url} failed: ${e}${retries > 0 ? `, ${retries-1} retries remaining...` : ''}`)
        if (retries > 0) httpDownload(url, toFile, retries-1)
    }

    client.get(url, res => {
        if (res.statusCode === 301 || res.statusCode === 302) {
            let redirectTo = res.headers.location;
            if (redirectTo.startsWith('/'))
                redirectTo = new URL(res.headers.location, new URL(url).origin).href
            return httpDownload(redirectTo, toFile, retries)
        } else if (res.statusCode >= 400) {
            retry(`${res.statusCode} ${res.statusText || ''}`.trimEnd())
        }
        else {
            console.log(`writing ${url} to ${toFile}`)
            const file = fs.createWriteStream(toFile)
            res.pipe(file);
            file.on('finish', () => file.close())
        }
    }).on('error', retry)
}
