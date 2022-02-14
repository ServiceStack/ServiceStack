import { apiValue, isDate, mapGet, padInt, $1, enc } from "@servicestack/client"
import { MetadataOperationType, AuthenticateResponse } from "../../lib/types"
/*minify:*/

/** @param {{[key:string]:string}} obj */
export function setBodyClass(obj) {
    let bodyCls = document.body.classList
    Object.keys(obj).forEach(name => {
        if (obj[name]) {
            bodyCls.add(name)
            bodyCls.remove(`no${name}`)
        } else {
            bodyCls.remove(name)
            bodyCls.add(`no${name}`)
        }
    })
}

/** @param {string} name */
export function styleProperty(name) {
    return document.documentElement.style.getPropertyValue(name)
}
export function setStyleProperty(props) {
    let style = document.documentElement.style
    Object.keys(props).forEach(name => style.setProperty(name, props[name]))
}

/** @param {boolean=} invalid */
export function inputClass(invalid) {
    return ['block w-full sm:text-sm rounded-md', !invalid
        ? 'shadow-sm focus:ring-indigo-500 focus:border-indigo-500 border-gray-300'
        : 'pr-10 border-red-300 text-red-900 placeholder-red-300 focus:outline-none focus:ring-red-500 focus:border-red-500'].join(' ')
}

/** @param {*} o
    @param {string} id */
function mapGetForInput(o, id) {
    let ret = apiValue(mapGet(o,id))
    return isDate(ret)
        ?  `${ret.getFullYear()}-${padInt(ret.getMonth() + 1)}-${padInt(ret.getDate())}`
        : ret
}

function gridClass() { return `grid grid-cols-6 gap-6` }
function gridInputs(formLayout) {
    let to = []
    formLayout.forEach(group => {
        group.forEach(input => {
            to.push({ input, rowClass: colClass(group.length) })
        })
    })
    return to
}

/** @param {number} fields */
function colClass(fields) {
    return `col-span-6` + (fields === 2 ? ' sm:col-span-3' : fields === 3 ? ' sm:col-span-2' : '')
}

/** @param {ImageInfo} icon
    @param {string} defaultSrc */
export function setFavIcon(icon, defaultSrc) {
    setFavIconSrc(icon.uri || defaultSrc)
}

/** @param {string} src */
export function setFavIconSrc(src) {
    let link = $1("link[rel~='icon']")
    if (!link) {
        link = document.createElement('link')
        link.rel = 'icon'
        $1('head').appendChild(link)
    }
    link.href = src
}

export function highlight(src, language) {
    if (!language) language = 'csharp'
    return hljs.highlight(src, { language }).value
}

/** @param {string} name
    @param {*} obj */
export function createDto(name, obj) {
    let dtoCtor = window[name]
    if (!dtoCtor) {
        console.log(`Couldn't find Request DTO for ${name}`) /*debug*/
        let AnonResponse = /** @class */ (function () { return function (init) { Object.assign(this, init) } }())
        dtoCtor = /** @class */ (function () {
            function AnonRequest(init) { Object.assign(this, init) }
            AnonRequest.prototype.createResponse = function () { return new AnonResponse() }
            AnonRequest.prototype.getTypeName = function () { return name }
            AnonRequest.prototype.getMethod = function () { return 'POST' }
            return AnonRequest
        }())
    }
    return new dtoCtor(obj)
}

/** @param {MetadataOperationType} op */
export const isQuery = op => resolve(op.request.inherits, x => x && x.name.startsWith('QueryDb`'))

export const crudInterfaces = ['ICreateDb`1','IUpdateDb`1','IPatchDb`1','IDeleteDb`1']

/** @param {MetadataOperationType} op */
export const isCrud = (op) => op.request.implements?.some(x => crudInterfaces.indexOf(x.name) >= 0)

/** @param {{namespace:string,name:string}} x
    @param {{namespace:string,name:string}} y */
export const matchesType = (x,y) =>
    (x && y) && x.name === y.name && ((!x.namespace || !y.namespace) || x.namespace === y.namespace)

/** @param {AuthenticateResponse} session */
export const isAdminAuth = (session) => session && session.roles && session.roles.indexOf('Admin') >= 0

/** @param {string} opName
    @param {MetadataOperationType} op */
export const canAccess = (opName, op) => {
    if (!op.requiresAuth)
        return true
    const session = window.AUTH
    if (!session)
        return false
    if (isAdminAuth(session))
        return true;
    const userRoles = session.roles || []
    if (op.requiredRoles?.length > 0 && !op.requiredRoles.every(role => userRoles.indexOf(role) >= 0))
        return false
    if (op.requiresAnyRole?.length > 0 && !op.requiresAnyRole.some(role => userRoles.indexOf(role) >= 0))
        return false
    const userPermissions = session.permissions || []
    if (op.requiredPermissions?.length > 0 && !op.requiredRoles.every(perm => userPermissions.indexOf(perm) >= 0))
        return false
    if (op.requiresAnyPermission?.length > 0 && !op.requiresAnyPermission.every(perm => userPermissions.indexOf(perm) >= 0))
        return false

    return true
}

/** @param {string} str */
export function parseCookie(str) {
    return str.split(';').map(v => v.split('=')) .reduce((acc, v) => {
        let key = v[0] && v[0].trim() && decodeURIComponent(v[0].trim())
        if (key) acc[key] = decodeURIComponent((v[1]||'').trim())
        return acc
    }, {});
}

/** @param {function} createClient
    @param {*} requestDto */
export function apiSend(createClient, requestDto) {
    if (!requestDto) throw new Error('!requestDto')
    let opName = requestDto.getTypeName()
    let httpReq = null, httpRes = null, headers = null
    let cookies = parseCookie(document.cookie)
    let newClient = createClient(c => {
        c.requestFilter = req => httpReq = req
        c.responseFilter = res => {
            httpRes = res
            headers = Object.fromEntries(res.headers)
        }
    })
    let returnsVoid = typeof requestDto.createResponse == 'function' && !requestDto.createResponse()
    let task = returnsVoid
        ? newClient.apiVoid(requestDto, { jsconfig: 'eccn' })
        : newClient.api(requestDto, { jsconfig: 'eccn' })
    return task.then(api => ({
        api,
        json: JSON.stringify(api.response || api.error, undefined, 4),
        text: JSON.stringify(api.response || api.error),
        opName,
        requestDto,
        httpReq,
        httpRes,
        headers,
        cookies,
    }))
}

/** @param {string} text
    @param {number} [timeout=3000] */
export function copy(text,timeout) {
    if (typeof timeout != 'number') timeout = 3000
    this.copied = true
    let $el = document.createElement("textarea")
    $el.innerHTML = enc(text)
    document.body.appendChild($el)
    $el.select()
    document.execCommand("copy")
    document.body.removeChild($el)
    setTimeout(() => this.copied = false, timeout)
}

/*:minify*/
