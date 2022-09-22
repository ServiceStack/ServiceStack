/*minify:*/
/**
 * Alt solution to optional chaining by only executing fn accessor if object is not null
 * @example
 * let a = b()?.c // equivalent to:
 * let a = map(b(), x => x.c)
 * @template T,V
 * @param {T} o
 * @param {(T) => V} f
 * @return {V|null}
 */
function map(o, f) { return o == null ? null : f(o) }
/** Set class on document.body if truthy otherwise set `no{class}`
 * @param {{[key:string]:string|any}} obj */
function setBodyClass(obj) {
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
/** Get CSS style property value 
 * @param {string} name */
function styleProperty(name) {
    return document.documentElement.style.getPropertyValue(name)
}
function setStyleProperty(props) {
    let style = document.documentElement.style
    Object.keys(props).forEach(name => style.setProperty(name, props[name]))
}
/** Tailwind CSS classes for standard Input controls 
 * @param {boolean} [invalid=false]
 * @param {string} [cls] */
function inputClass(invalid,cls) {
    return ['block w-full sm:text-sm rounded-md disabled:bg-gray-100 disabled:shadow-none', !invalid
        ? 'shadow-sm focus:ring-indigo-500 focus:border-indigo-500 border-gray-300'
        : 'pr-10 border-red-300 text-red-900 placeholder-red-300 focus:outline-none focus:ring-red-500 focus:border-red-500',
        '',cls].join(' ')
}
/** Get object value from map by (case-insensitive) id and if required convert API value for usage in HTML Inputs 
 * @param {*} o
 * @param {string} id */
function mapGetForInput(o, id) {
    let ret = apiValue(mapGet(o,id))
    return isDate(ret)
        ?  `${ret.getFullYear()}-${padInt(ret.getMonth() + 1)}-${padInt(ret.getDate())}`
        : ret
}
/** Set the browser's page fav icon by icon 
 * @param {ImageInfo} icon
 * @param {string} defaultSrc */
function setFavIcon(icon, defaultSrc) {
    setFavIconSrc(icon.uri || defaultSrc)
}
/** Set the browser's page fav icon by src 
 * @param {string} src */
function setFavIconSrc(src) {
    let link = $1("link[rel~='icon']")
    if (!link) {
        link = document.createElement('link')
        link.rel = 'icon'
        $1('head').appendChild(link)
    }
    link.href = src
}
/**
 * High-level API around highlight.js to add syntax highlighting to language source cde
 * @param {string} src
 * @param {string} language
 * @return {string}
 */
function highlight(src, language) {
    if (!language) language = 'csharp'
    return hljs.highlight(src, { language }).value
}
/** Create Request DTO from MetadataOperationType and map of args 
 * @param {MetadataOperationType} op
 * @param {*?} args */
function createRequest(op,args) { return !op ? null : createDto(op.request.name,args) }
/** Create Request DTO from API Name and map of args
 * @param {string} name
 * @param {*} obj */
function createDto(name, obj) {
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
/** Check if operation implements class name 
 * @param {MetadataOperationType} op
 * @param {string} cls */
const hasInterface = (op,cls) => resolve(op.request.implements.some(i => i.name === cls))
/**
 * API around CRUD APIs
 * @type {{Delete: string, AnyWrite: string[], isCreate: (function(*): any), Create: string, isDelete: (function(*): any), AnyRead: string[], isQuery: (function(*): boolean|null), isCrud: (function(*): boolean|null), Update: string, Patch: string, isUpdate: (function(*): any), isPatch: (function(*): any)}}
 */
const Crud = {
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
/** Check if authenticated session has Admin role 
 * @param {{roles:string[]}} [session] */
const isAdminAuth = session => map(session, x => x.roles && x.roles.indexOf('Admin') >= 0)
/** Check if array is not null or empty
 * @param {any[]|null} arr */
function hasItems(arr) { return arr && arr.length > 0 }
/** Check if Auth Session has access to API 
 * @param {MetadataOperationType?} op
 * @param {AuthenticateResponse|null} auth */
function canAccess(op, auth) {
    if (!op) return false
    if (!op.requiresAuth)
        return true
    if (!auth)
        return false
    if (isAdminAuth(auth))
        return true;
    let [roles, permissions] = [auth.roles || [], auth.permissions || []]
    let [requiredRoles, requiredPermissions, requiresAnyRole, requiresAnyPermission] = [
        op.requiredRoles || [], op.requiredPermissions || [], op.requiresAnyRole || [], op.requiresAnyPermission || []]
    if (!requiredRoles.every(role => roles.indexOf(role) >= 0))
        return false
    if (requiresAnyRole.length > 0 && !requiresAnyRole.some(role => roles.indexOf(role) >= 0))
        return false
    if (!requiredPermissions.every(perm => permissions.indexOf(perm) >= 0))
        return false
    if (requiresAnyPermission.length > 0 && !requiresAnyPermission.every(perm => permissions.indexOf(perm) >= 0))
        return false
    return true
}
/** Return error message if Auth Session cannot access API 
 * @param {MetadataOperationType} op
 * @param {{roles:string[],permissions:string[]}} auth */
function invalidAccessMessage(op, auth) {
    if (!op || !op.requiresAuth) return null
    if (!auth) {
        return `<b>${op.request.name}</b> requires Authentication`
    }
    if (isAdminAuth(auth))
        return null;
    let [roles, permissions] = [auth.roles || [], auth.permissions || []]
    let [requiredRoles, requiredPermissions, requiresAnyRole, requiresAnyPermission] = [
        op.requiredRoles || [], op.requiredPermissions || [], op.requiresAnyRole || [], op.requiresAnyPermission || []]
    let missingRoles = requiredRoles.filter(x => roles.indexOf(x) < 0)
    if (missingRoles.length > 0)
        return `Requires ${missingRoles.map(x => '<b>' + x + '</b>').join(', ')} Role` + (missingRoles.length > 1 ? 's' : '')
    let missingPerms = requiredPermissions.filter(x => permissions.indexOf(x) < 0)
    if (missingPerms.length > 0)
        return `Requires ${missingPerms.map(x => '<b>' + x + '</b>').join(', ')} Permission` + (missingPerms.length > 1 ? 's' : '')
    if (requiresAnyRole.length > 0 && !requiresAnyRole.some(role => roles.indexOf(role) >= 0))
        return `Requires any ${requiresAnyRole.filter(x => roles.indexOf(x) < 0)
            .map(x => '<b>' + x + '</b>').join(', ')} Role` + (missingRoles.length > 1 ? 's' : '')
   if (requiresAnyPermission.length > 0 && !requiresAnyPermission.every(perm => permissions.indexOf(perm) >= 0))
        return `Requires any ${requiresAnyPermission.filter(x => permissions.indexOf(x) < 0)
            .map(x => '<b>' + x + '</b>').join(', ')} Permission` + (missingPerms.length > 1 ? 's' : '')
    return null
}
/** Parse cookie string into Map 
 * @param {string} str 
 * @return {Record<string,string>} */
function parseCookie(str) {
    return str.split(';').map(v => v.split('=')) .reduce((acc, v) => {
        let key = v[0] && v[0].trim() && decodeURIComponent(v[0].trim())
        if (key) acc[key] = decodeURIComponent((v[1]||'').trim())
        return acc
    }, {});
}
/** High-level API to invoke an API Request by Request DTO and optional queryString args 
 * @param {function} createClient
 * @param {*} requestDto
 * @param {*} [queryArgs] */
function apiSend(createClient, requestDto, queryArgs) {
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
        json: indentJson(api.response || api.error),
        text: JSON.stringify(api.response || api.error),
        opName,
        requestDto,
        httpReq,
        httpRes,
        headers,
        cookies,
    }))
}
/** High-level API to invoke an API Request by Request DTO, FormData and optional queryString args
 * @param {function} createClient
 * @param {*} requestDto
 * @param {FormData} formData
 * @param {*} [queryArgs] */
function apiForm(createClient, requestDto, formData, queryArgs) {
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
        ? newClient.apiFormVoid(requestDto, formData, Object.assign({ jsconfig: 'eccn' }, queryArgs))
        : newClient.apiForm(requestDto, formData, Object.assign({ jsconfig: 'eccn' }, queryArgs))
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
/** Utility to copy text to OS clipboard 
 * @param {string} text
 * @param {number} [timeout=3000] */
function copy(text,timeout) {
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
/** Render ImageInfo into HTML IMG  
 * @param {ImageInfo} icon 
 * @param {*} [opt] */
function iconHtml(icon, opt) {
    if (!icon) return ''
    if (!opt) opt = {}
    let { svg, uri, alt, cls } = icon
    if (!cls) cls = 'w-5 h-5'
    if (opt.cls) {
        cls += ' ' + opt.cls 
    }
    if (svg) {
        let attrs = [
            cls ? `class="${cls}"` : null, 
            svg.indexOf('role') === -1 ? `role="img"` : null,
            svg.indexOf('aria-hidden') === -1 ? `aria-hidden="true"` : null,
            `onerror="iconOnError(this,'img')"`
        ].filter(x => !!x)
        if (attrs.length > 0) {
            svg = `<svg ${attrs.join(' ')} ${svg.substring(4)}`
        }
        return svg
    }
    if (uri) {
        let attrs = [
            cls ? `class="${cls}"` : null,
            alt ? `alt="${alt}"` : null,
            `onerror="iconOnError(this,'img')"`
        ].filter(x => !!x)
        return `<img src="${uri}" ${attrs.join(' ')}>`
    }
    return ''
}
/** Is element an Input control
 * @param {Element} e */
let InputTags = 'INPUT,SELECT,TEXTAREA'.split(',')
function isInput(e) {
    return e && InputTags.indexOf(e.tagName) >= 0
}
/** Whether any modifier keys were pressed
 * @param {KeyboardEvent} e */
function hasModifierKey(e) {
    return e.shiftKey || e.ctrlKey || e.altKey || e.metaKey || e.code === 'MetaLeft' || e.code === 'MetaRight'
}
let SORT_METHODS = ['GET','POST','PATCH','PUT','DELETE']
/** @param {MetadataOperationType} op */
function opSortName(op) {
    // group related services by model or inherited generic type
    let group = map(op.dataModel, x => x.name) || map(op.request.inherits, x => x.genericArgs && x.genericArgs[0]) 
    let sort1 = group ? group + map(SORT_METHODS.indexOf(op.method || 'ANY'), x => x === -1 ? '' : x.toString()) : 'z'
    return sort1 + `_` + op.request.name
}
/** Sort & group operations operations in logical order 
 * @param {MetadataOperationType[]} ops
 * @return {MetadataOperationType[]} */
function sortOps(ops) {
    ops.sort((a,b) => opSortName(a).localeCompare(opSortName(b)))
    return ops
}
/**
 * Return absolute URL from relative URL 
 * @param url
 * @return {*|string}
 */
function toAppUrl(url) {
    return !url || typeof BASE_URL != 'string' || url.indexOf('://') >= 0 
        ? url
        : combinePaths(BASE_URL, url)
}
/** Format number into USD currency 
 * @param {number} val */
function currency(val) {
    return new Intl.NumberFormat(undefined,{style:'currency',currency:'USD'}).format(val)
}
/** Format bytes into human-readable file size 
 * @param {number} val */
function bytes(val) {
    return Files.formatBytes(val)
}
/** HTML Tag builder 
 * @param {string} tag
 * @param {string} [child]
 * @param {*} [attrs] */
function htmlTag(tag,child,attrs) {
    if (!attrs) attrs = {}
    let cls = attrs.cls || attrs.className || attrs['class']
    if (cls) {
        attrs = omit(attrs,['cls','class','className'])
        attrs['class'] = cls
    }
    return `<${tag}` + Object.keys(attrs).reduce((acc,k) => `${acc} ${k}="${enc(attrs[k])}"`, '') + `>${child||''}</${tag}>`
}
/** @param {{href:string,cls?:string,target?:string,rel?:string}} attrs */
function linkAttrs(attrs) {
    return Object.assign({target:'_blank',rel:'noopener','class':'text-blue-600'},attrs)
}
/** Create formatted HTML A URL links  
 * @param {string} href
 * @param {{cls?:string,target?:string,rel?:string}} [opt] */
function link(href, opt) {
    return htmlTag('a', href, linkAttrs({ ...opt, href }))
}
/** Create formatted HTML A mailto: links
 * @param {string} email
 * @param {{subject?:string,body?:string,cls?:string,target?:string,rel?:string}} [opt] */
function linkMailTo(email, opt) {
    if (!opt) opt = {}
    let { subject, body } = opt
    let attrs = omit(opt, ['subject','body'])
    let args = {}
    if (subject) args.subject = subject
    if (body) args.body = body
    return htmlTag('a', email, linkAttrs({...attrs, href:`mailto:${appendQueryString(email,args)}` }))
}
/** Create formatted HTML A tel: links
 * @param {string} tel
 * @param {{cls?:string,target?:string,rel?:string}} [opt] */
function linkTel(tel, opt) {
    return htmlTag('a', tel, linkAttrs({...opt, href:`tel:${tel}` }))
}
/** Create HTML IMG Icon from URL 
 * @param {string} url */
function icon(url) {
    return `<img class="w-6 h-6" title="${url}" src="${toAppUrl(url)}" onerror="iconOnError(this)">`
}
/** Create rounded HTML IMG Icon from URL
 * @param {string} url */
function iconRounded(url) {
    return `<img class="w-8 h-8 rounded-full" title="${url}" src="${toAppUrl(url)}" onerror="iconOnError(this)">`
}
/** Create HTML Link for file attachment
 * @param {string} url */
function attachment(url) {
    let fileName = Files.getFileName(url)
    let ext = Files.getExt(fileName)
    let imgSrc = ext == null || Files.canPreview(url)
        ? toAppUrl(url)
        : iconFallbackSrc(url)
    return `<a class="flex" href="${toAppUrl(url)}" title="${url}" target="_blank"><img class="w-6 h-6" src="${imgSrc}" onerror="iconOnError(this,'att')"><span class="pl-1">${fileName}</span></a>`
}
/** Handle IMG onerror to populate fallback icon 
 * @param {HTMLImageElement} img
 * @param {string} [fallbackSrc] */
function iconOnError(img,fallbackSrc) {
    img.onerror = null
    img.src = iconFallbackSrc(img.src,fallbackSrc)
}
/** Create icon with fallback 
 * @param {string} src
 * @param {string} [fallbackSrc] */
function iconFallbackSrc(src,fallbackSrc) {
    return Files.extSrc(lastRightPart(src,'.').toLowerCase())
        || (fallbackSrc
            ? Files.extSrc(fallbackSrc) || fallbackSrc
            : null)
        || Files.extSrc('doc')
}
/** Hides field from being displayed in search results
 * @param o
 * @return {string}
 */
function hidden(o) { return '' }
/** Return indented JSON
 * @param o
 * @return {string}
 */
function indentJson(o) {
    if (o == null) return ''
    if (typeof o == 'string') o
    if (typeof o == 'object' && '__type' in o) delete o['__type']
    if (o.ResponseStatus) delete o.ResponseStatus['__type']
    if (o.responseStatus) delete o.responseStatus['__type']
    return JSON.stringify(o, undefined, 4) 
}
/*:minify*/
