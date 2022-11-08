/**: Used by .d.ts */
import { MetadataOperationType, MetadataType, MetadataPropertyType, InputInfo, ThemeInfo } from "../../lib/types"

import { JsonServiceClient, lastLeftPart, leftPart, trimEnd } from "@servicestack/client"
import { Server } from "../../lib/types"
import { createForms, createMeta } from "../../shared/js/createForms"

/*minify:*/
//Server.config.debugMode = false
let BASE_URL = lastLeftPart(trimEnd(document.baseURI,'/'),'/')
let bearerToken = null
let authsecret = null

export function createClient(fn) {
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
let client = createClient()

Server.api.operations.forEach(op => {
    if (!op.tags) op.tags = []
})

let appOps = Server.api.operations.filter(op => !(op.request.namespace || "").startsWith('ServiceStack'))
let appTags = Array.from(new Set(appOps.flatMap(op => op.tags))).sort()
/** @type {{expanded: boolean, operations: MetadataOperationType[], tag: string}[]} */
export let sideNav = appTags.map(tag => ({
    tag,
    expanded: true,
    operations: appOps.filter(op => op.tags.indexOf(tag) >= 0)
}))

let ssOps = Server.api.operations.filter(op => (op.request.namespace || "").startsWith('ServiceStack'))
let ssTags = Array.from(new Set(ssOps.flatMap(op => op.tags))).sort()
ssTags.map(tag => ({
    tag,
    expanded: true,
    operations: ssOps.filter(op => op.tags.indexOf(tag) >= 0)
})).forEach(nav => sideNav.push(nav))

let tags = Server.ui.explorer.tags
let other = {
    tag: appTags.length > 0 ? tags.other : tags.default,
    expanded: true,
    operations: [...appOps, ...ssOps].filter(op => op.tags.length === 0)
}
if (other.operations.length > 0) sideNav.push(other)

let alwaysHideTags = Server.ui.alwaysHideTags || !DEBUG && Server.ui.hideTags
if (alwaysHideTags) {
    sideNav = sideNav.filter(group => alwaysHideTags.indexOf(group.tag) < 0)
}

let appName = 'explorer'
export let Meta = createMeta(Server, appName)
export let Forms = createForms(Meta, Server.ui.explorer.css, Server.ui)
/*:minify*/
