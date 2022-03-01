import { JsonServiceClient, lastLeftPart, trimEnd } from "@servicestack/client"
import { APP } from "../../lib/types"
import { createForms } from "../../shared/js/createForms"
import { appApis } from "../../shared/js/core"

/*minify:*/

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

export let { CACHE, HttpErrors, OpsMap, TypesMap, FullTypesMap, getOp, getType, isEnum, enumValues, getIcon } = appApis(APP)
export let Forms = createForms(TypesMap, APP.plugins.adminUsers.css, APP.ui)

/*:minify*/
