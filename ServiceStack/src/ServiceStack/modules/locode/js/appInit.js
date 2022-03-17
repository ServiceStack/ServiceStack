/*minify:*/
//APP.config.debugMode = false
let BASE_URL = lastLeftPart(trimEnd(document.baseURI,'/'),'/')
let bearerToken = null
let authsecret = null
function createClient(fn) {
    return new JsonServiceClient(BASE_URL).apply(c => {
        c.bearerToken = bearerToken
        c.enableAutoRefreshToken = false
        if (authsecret) c.headers.set('authsecret', authsecret)
        let apiFmt = APP.httpHandlers['ApiHandlers.Json']
        if (apiFmt)
            c.basePath = apiFmt.replace('/{Request}', '')
        if (fn) fn(c)
    })
}
let client = createClient()
let resolveApiUrl = (op) => combinePaths(client.replyBaseUrl,op) 
APP.api.operations.forEach(op => {
    if (!op.tags) op.tags = []
})
let appOps = APP.api.operations.filter(op => !op.request.namespace.startsWith('ServiceStack') && Crud.isQuery(op))
let appTags = Array.from(new Set(appOps.flatMap(op => op.tags))).sort()
let sideNav = appTags.map(tag => ({
    tag,
    expanded: true,
    operations: appOps.filter(op => op.tags.indexOf(tag) >= 0)
}))
let ssOps = APP.api.operations.filter(op => op.request.namespace.startsWith('ServiceStack') && Crud.isQuery(op))
let ssTags = Array.from(new Set(ssOps.flatMap(op => op.tags))).sort()
ssTags.map(tag => ({
    tag,
    expanded: true,
    operations: ssOps.filter(op => op.tags.indexOf(tag) >= 0)
})).forEach(nav => sideNav.push(nav))
let tags = APP.ui.locode.tags
let other = {
    tag: appTags.length > 0 ? tags.other : tags.default,
    expanded: true,
    operations: [...appOps, ...ssOps].filter(op => op.tags.length === 0)
}
if (other.operations.length > 0) sideNav.push(other)
let alwaysHideTags = APP.ui.alwaysHideTags || !DEBUG && APP.ui.hideTags
if (alwaysHideTags) {
    sideNav = sideNav.filter(group => alwaysHideTags.indexOf(group.tag) < 0)
}
let { CACHE, HttpErrors, OpsMap, TypesMap, FullTypesMap, getOp, getType, isEnum, enumValues, getIcon } = appApis(APP)
let Forms = createForms(TypesMap, APP.ui.locode.css, APP.ui)
let onRoutesEditChange = () => {}
/*:minify*/
