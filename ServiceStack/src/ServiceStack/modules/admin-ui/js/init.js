/*minify:*/
let BASE_URL = lastLeftPart(trimEnd(document.baseURI,'/'),'/')
/** @type {string|null} */
let bearerToken = null
/** @type {string|null} */
let authsecret = null
function createClient(fn) {
    return new JsonServiceClient(BASE_URL).apply(c => {
        c.bearerToken = bearerToken
        c.enableAutoRefreshToken = false
        if (authsecret) c.headers.set('authsecret', authsecret)
        let apiFmt = Server.httpHandlers['ApiHandlers.Json']
        if (apiFmt)
            c.basePath = apiFmt.replace('/{Request}', '')
        if (fn) fn(c)
    })
}
/** App's pre-configured `JsonServiceClient` instance for making typed API requests */
let client = createClient()
let appName = 'admin-ui'
let Meta = createMeta(Server, appName)
let Forms = createForms(Meta, Server.ui.admin.css, Server.ui)
/*:minify*/
