import { apiValue, isDate, mapGet, padInt, $1, enc, resolve } from "@servicestack/client"
import { MetadataOperationType, AuthenticateResponse, APP } from "../../lib/types"
import { Types } from "./Types";
import { FullTypesMap, TypesMap } from "../../query-ui/js/appInit";
/*minify:*/

/** @template T,V
    @param {*} o
    @param {(a:T) => V} f
    @returns {V|null} */
export function map(o, f) { return o == null ? null : f(o) }

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

/** @param {boolean} [invalid=false] 
    @param {string} [cls] */
export function inputClass(invalid,cls) {
    return ['block w-full sm:text-sm rounded-md disabled:bg-gray-100 disabled:shadow-none', !invalid
        ? 'shadow-sm focus:ring-indigo-500 focus:border-indigo-500 border-gray-300'
        : 'pr-10 border-red-300 text-red-900 placeholder-red-300 focus:outline-none focus:ring-red-500 focus:border-red-500',
        '',cls].join(' ')
}

/** @param {*} o
    @param {string} id */
function mapGetForInput(o, id) {
    let ret = apiValue(mapGet(o,id))
    return isDate(ret)
        ?  `${ret.getFullYear()}-${padInt(ret.getMonth() + 1)}-${padInt(ret.getDate())}`
        : ret
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


/** @param {MetadataOperationType} op
    @param {*?} args */
export function createRequest(op,args) { return !op ? null : createDto(op.request.name,args) }

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

/** @param {MetadataTypes} api */
export function appApis(api) {
    let CACHE = {}
    let HttpErrors = { 401:'Unauthorized', 403:'Forbidden' }
    let OpsMap = {}
    let TypesMap = {}
    let FullTypesMap = {}
    api.operations.forEach(op => {
        OpsMap[op.request.name] = op
        TypesMap[op.request.name] = op.request
        FullTypesMap[Types.key(op.request)] = op.request
        if (op.response) TypesMap[op.response.name] = op.response
        if (op.response) FullTypesMap[Types.key(op.response)] = op.response
    })
    api.types.forEach(type => TypesMap[type.name] = type)
    api.types.forEach(type => FullTypesMap[Types.key(type)] = type)

    /** @param {string} opName */
    function getOp(opName) {
        return OpsMap[opName]
    }

    /** @param {{namespace:string?,name:string}|string} typeRef
        @return {MetadataType} */
    function getType(typeRef) {
        return !typeRef ? null 
            : typeof typeRef == 'string' 
                ? TypesMap[typeRef]
                : FullTypesMap[Types.key(typeRef)] || TypesMap[typeRef.name]
    }

    /** @param {string} type */
    function isEnum(type) {
        return type && map(TypesMap[type], x => x.isEnum) === true
    }
    /** @param {string} type
        @return {{key:string,value:string}[]} */
    function enumValues(type) {
        let enumType = type && map(TypesMap[type], x => x.isEnum ? x : null)
        if (!enumType) return []
        if (enumType.enumValues) {
            let ret = []
            for (let i=0; i<enumType.enumNames; i++) {
                ret.push({ key:enumType.enumValues[i], value:enumType.enumNames[i] })
            }
            return ret
        } else {
            return enumType.enumNames.map(x => ({ key:x, value:x }))
        }
    }

    return { CACHE, HttpErrors, OpsMap, TypesMap, FullTypesMap, getOp, getType, isEnum, enumValues }
}

/** @param {MetadataOperationType} op
    @param {string} cls */
const hasInterface = (op,cls) => resolve(op.request.implements.some(i => i.name === cls))

export const Crud = {
    Create:'ICreateDb`1',
    Update:'IUpdateDb`1',
    Patch:'IPatchDb`1',
    Delete:'IDeleteDb`1',
    AnyRead: ['QueryDb`1','QueryDb`2'],
    AnyWrite: ['ICreateDb`1','IUpdateDb`1','IPatchDb`1','IDeleteDb`1'],
    isQuery: op => map(op.request.inherits, x => Crud.AnyRead.indexOf(x.name) >= 0),
    isCrud: op => map(op.request.implements, x => x.some(x => Crud.AnyWrite.indexOf(x.name) >= 0)),
    isCreate: op => hasInterface(op, Crud.Create),
    isUpdate: op => hasInterface(op, Crud.Update),
    isPatch: op => hasInterface(op, Crud.Patch),
    isDelete: op => hasInterface(op, Crud.Delete),
}

/** @param {AuthenticateResponse} [session] */
export const isAdminAuth = session => map(session, x => x.roles && x.roles.indexOf('Admin') >= 0)

/** @param {MetadataOperationType?} op 
    @param {AuthenticateResponse|null} auth */
export function canAccess(op, auth) {
    if (!op) return false
    if (!op.requiresAuth)
        return true
    if (!auth)
        return false
    if (isAdminAuth(auth))
        return true;
    const userRoles = auth.roles || []
    const hasItems = arr => arr && arr.length > 0 
    if (hasItems(op.requiredRoles) && !op.requiredRoles.every(role => userRoles.indexOf(role) >= 0))
        return false
    if (hasItems(op.requiresAnyRole) && !op.requiresAnyRole.some(role => userRoles.indexOf(role) >= 0))
        return false
    const userPermissions = auth.permissions || []
    if (hasItems(op.requiredPermissions) && !op.requiredRoles.every(perm => userPermissions.indexOf(perm) >= 0))
        return false
    if (hasItems(op.requiresAnyPermission) && !op.requiresAnyPermission.every(perm => userPermissions.indexOf(perm) >= 0))
        return false

    return true
}

/** @param {MetadataOperationType} op
    @param {{roles:string[],permissions:string[]}} auth */
export function invalidAccessMessage(op, auth) {
    if (!op || !op.requiresAuth) return null
    if (!auth) {
        return `<b>${op.request.name}</b> requires Authentication`
    }
    let { roles, permissions } = auth
    if (roles.indexOf('Admin') >= 0) return null

    let missingRoles = op.requiredRoles.filter(x => roles.indexOf(x) < 0)
    if (missingRoles.length > 0)
        return `Requires ${missingRoles.map(x => '<b>' + x + '</b>').join(', ')} Role` + (missingRoles.length > 1 ? 's' : '')
    let missingPerms = op.requiredPermissions.filter(x => permissions.indexOf(x) < 0)
    if (missingPerms.length > 0)
        return `Requires ${missingPerms.map(x => '<b>' + x + '</b>').join(', ')} Permission` + (missingPerms.length > 1 ? 's' : '')

    if (missingRoles.length > 0)
        return `Requires any ${missingRoles.map(x => '<b>' + x + '</b>').join(', ')} Role` + (missingRoles.length > 1 ? 's' : '')
    missingPerms = op.requiresAnyPermission.filter(x => permissions.indexOf(x) < 0)
    if (missingPerms.length > 0)
        return `Requires any ${missingPerms.map(x => '<b>' + x + '</b>').join(', ')} Permission` + (missingPerms.length > 1 ? 's' : '')
    return null
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
    @param {*} requestDto
    @param {*} [queryArgs] */
export function apiSend(createClient, requestDto, queryArgs) {
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
        ? newClient.apiVoid(requestDto, Object.assign({ jsconfig: 'eccn' }, queryArgs))
        : newClient.api(requestDto, Object.assign({ jsconfig: 'eccn' }, queryArgs))
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
