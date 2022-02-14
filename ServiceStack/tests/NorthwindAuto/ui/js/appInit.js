import { JsonServiceClient, lastLeftPart, leftPart, trimEnd } from "@servicestack/client"
import { APP } from "../../lib/types"
/*minify:*/
//APP.config.debugMode = false
let BASE_URL = lastLeftPart(trimEnd(document.baseURI,'/'),'/')
let bearerToken = null
let authsecret = null

export function createClient(fn) {
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

APP.api.operations.forEach(op => {
    if (!op.tags) op.tags = []
})

let appOps = APP.api.operations.filter(op => !op.request.namespace.startsWith('ServiceStack'))
let appTags = Array.from(new Set(appOps.flatMap(op => op.tags))).sort()
let sideNav = appTags.map(tag => ({
    tag,
    expanded: true,
    operations: appOps.filter(op => op.tags.indexOf(tag) >= 0)
}))

let ssOps = APP.api.operations.filter(op => op.request.namespace.startsWith('ServiceStack'))
let ssTags = Array.from(new Set(ssOps.flatMap(op => op.tags))).sort()
ssTags.map(tag => ({
    tag,
    expanded: true,
    operations: ssOps.filter(op => op.tags.indexOf(tag) >= 0)
})).forEach(nav => sideNav.push(nav))

let other = {
    tag: appTags.length > 0 ? 'other' : 'APIs',
    expanded: true,
    operations: [...appOps, ...ssOps].filter(op => op.tags.length === 0)
}
if (other.operations.length > 0) sideNav.push(other)

let alwaysHideTags = APP.ui.alwaysHideTags || !DEBUG && APP.ui.hideTags
if (alwaysHideTags) {
    sideNav = sideNav.filter(group => alwaysHideTags.indexOf(group.tag) < 0)
}

let CACHE = {}
let OpsMap = {}
let TypesMap = {}
let HttpErrors = { 401:'Unauthorized', 403:'Forbidden' }
APP.api.operations.forEach(op => {
    OpsMap[op.request.name] = op
    TypesMap[op.request.name] = op.request
    if (op.response) TypesMap[op.response.name] = op.response
})
APP.api.types.forEach(type => TypesMap[type.name] = type)

let cleanSrc = src => src.trim();

function invalidAccessMessage(op, authRoles, authPerms) {
    if (authRoles.indexOf('Admin') >= 0) return null

    let missingRoles = op.requiredRoles.filter(x => authRoles.indexOf(x) < 0)
    if (missingRoles.length > 0)
        return `Requires ${missingRoles.map(x => '<b>' + x + '</b>').join(', ')} Role` + (missingRoles.length > 1 ? 's' : '')
    let missingPerms = op.requiredPermissions.filter(x => authPerms.indexOf(x) < 0)
    if (missingPerms.length > 0)
        return `Requires ${missingPerms.map(x => '<b>' + x + '</b>').join(', ')} Permission` + (missingPerms.length > 1 ? 's' : '')

    if (missingRoles.length > 0)
        return `Requires any ${missingRoles.map(x => '<b>' + x + '</b>').join(', ')} Role` + (missingRoles.length > 1 ? 's' : '')
    missingPerms = op.requiresAnyPermission.filter(x => authPerms.indexOf(x) < 0)
    if (missingPerms.length > 0)
        return `Requires any ${missingPerms.map(x => '<b>' + x + '</b>').join(', ')} Permission` + (missingPerms.length > 1 ? 's' : '')
    return null
}
/*:minify*/
