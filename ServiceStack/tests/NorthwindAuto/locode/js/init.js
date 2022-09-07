/**: Used by .d.ts */
import { MetadataOperationType, MetadataType, MetadataPropertyType, InputInfo, ThemeInfo } from "../../lib/types"

import { combinePaths, JsonServiceClient, lastLeftPart, trimEnd } from "@servicestack/client"
import { Server } from "../../lib/types"
import { Crud } from "../../shared/js/core"
import { createForms, createMeta } from "../../shared/js/createForms"

/*minify:*/
//Server.config.debugMode = false
let BASE_URL = lastLeftPart(trimEnd(document.baseURI,'/'),'/')
/** @type {string|null} */
let bearerToken = null
/** @type {string|null} */
let authsecret = null

/** 
 * Create a new `JsonServiceStack` client instance configured with the authenticated user
 * 
 * @remarks
 * For typical API requests it's recommended to use the UI's pre-configured **client** instance
 * 
 * @param {Function} [fn]
 * @return {JsonServiceClient}
 */
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

/** App's pre-configured `JsonServiceClient` instance for making typed API requests */
export let client = createClient()

/** Resolve Absolute URL for API Name
 * @param {string} op 
 * @return {string} */
export function resolveApiUrl(op) { 
    return combinePaths(client.replyBaseUrl,op) 
} 

Server.api.operations.forEach(op => {
    if (!op.tags) op.tags = []
})

let appOps = Server.api.operations.filter(op => !(op.request.namespace || "").startsWith('ServiceStack') && Crud.isQuery(op))
let appTags = Array.from(new Set(appOps.flatMap(op => op.tags))).sort()
/** Organized data structure to render Sidebar
 * @remarks
 * @type {{expanded: boolean, operations: MetadataOperationType[], tag: string}[]} */
export let sideNav = appTags.map(tag => ({
    tag,
    expanded: true,
    operations: appOps.filter(op => op.tags.indexOf(tag) >= 0)
}))

let ssOps = Server.api.operations.filter(op => (op.request.namespace || "").startsWith('ServiceStack') && Crud.isQuery(op))
let ssTags = Array.from(new Set(ssOps.flatMap(op => op.tags))).sort()
ssTags.map(tag => ({
    tag,
    expanded: true,
    operations: ssOps.filter(op => op.tags.indexOf(tag) >= 0)
})).forEach(nav => sideNav.push(nav))

let tags = Server.ui.locode.tags
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

let appName = 'locode'
export let Meta = createMeta(Server, appName)
export let Forms = createForms(Meta, Server.ui.locode.css, Server.ui)

/*:minify*/
