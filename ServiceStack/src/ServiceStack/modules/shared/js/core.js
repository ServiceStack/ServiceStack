/*minify:*/
function map(o, f) { return o == null ? null : f(o) }
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
function styleProperty(name) {
    return document.documentElement.style.getPropertyValue(name)
}
function setStyleProperty(props) {
    let style = document.documentElement.style
    Object.keys(props).forEach(name => style.setProperty(name, props[name]))
}
function inputClass(invalid,cls) {
    return ['block w-full sm:text-sm rounded-md disabled:bg-gray-100 disabled:shadow-none', !invalid
        ? 'shadow-sm focus:ring-indigo-500 focus:border-indigo-500 border-gray-300'
        : 'pr-10 border-red-300 text-red-900 placeholder-red-300 focus:outline-none focus:ring-red-500 focus:border-red-500',
        '',cls].join(' ')
}
function mapGetForInput(o, id) {
    let ret = apiValue(mapGet(o,id))
    return isDate(ret)
        ?  `${ret.getFullYear()}-${padInt(ret.getMonth() + 1)}-${padInt(ret.getDate())}`
        : ret
}
function setFavIcon(icon, defaultSrc) {
    setFavIconSrc(icon.uri || defaultSrc)
}
function setFavIconSrc(src) {
    let link = $1("link[rel~='icon']")
    if (!link) {
        link = document.createElement('link')
        link.rel = 'icon'
        $1('head').appendChild(link)
    }
    link.href = src
}
function highlight(src, language) {
    if (!language) language = 'csharp'
    return hljs.highlight(src, { language }).value
}
function createRequest(op,args) { return !op ? null : createDto(op.request.name,args) }
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
function appApis(app) {
    let api = app.api
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
    function getOp(opName) {
        return OpsMap[opName]
    }
    function getType(typeRef) {
        return !typeRef ? null 
            : typeof typeRef == 'string' 
                ? TypesMap[typeRef]
                : FullTypesMap[Types.key(typeRef)] || TypesMap[typeRef.name]
    }
    function isEnum(type) {
        return type && map(TypesMap[type], x => x.isEnum) === true
    }
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
    let defaultIcon = app.ui.theme.modelIcon ||
        { svg:`<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><g fill="none" stroke="currentColor" stroke-width="1.5"><path d="M5 12v6s0 3 7 3s7-3 7-3v-6"/><path d="M5 6v6s0 3 7 3s7-3 7-3V6"/><path d="M12 3c7 0 7 3 7 3s0 3-7 3s-7-3-7-3s0-3 7-3Z"/></g></svg>` }
    function getIcon({op,type}) {
        if (op) {
            let img = map(op.request, x => x.icon)
                || map(getType(op.viewModel), x => x.icon)
                || map(getType(op.dataModel), x => x.icon)
            if (img)
                return img
        }
        if (type && type.icon) {
            return type.icon
        }
        return defaultIcon
    }
    return { CACHE, HttpErrors, OpsMap, TypesMap, FullTypesMap, getOp, getType, isEnum, enumValues, getIcon }
}
const hasInterface = (op,cls) => resolve(op.request.implements.some(i => i.name === cls))
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
const isAdminAuth = session => map(session, x => x.roles && x.roles.indexOf('Admin') >= 0)
function canAccess(op, auth) {
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
function invalidAccessMessage(op, auth) {
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
function parseCookie(str) {
    return str.split(';').map(v => v.split('=')) .reduce((acc, v) => {
        let key = v[0] && v[0].trim() && decodeURIComponent(v[0].trim())
        if (key) acc[key] = decodeURIComponent((v[1]||'').trim())
        return acc
    }, {});
}
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
function currency(val) {
    return new Intl.NumberFormat(undefined,{style:'currency',currency:'USD'}).format(val)
}
function icon(url) {
    return `<img class="w-6 h-6" title="${url}" src="${url}">`
}
function iconRounded(url) {
    return `<img class="w-8 h-8 rounded-full" title="${url}" src="${url}">`
}
// marker fn, special-cased to hide from query results
function hidden(o) { return '' }
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
            svg.indexOf('aria-hidden') === -1 ? `aria-hidden="true"` : null
        ].filter(x => !!x)
        if (attrs.length > 0) {
            svg = `<svg ${attrs.join(' ')}' ${svg.substring(4)}`
        }
        return svg
    }
    if (uri) {
        let attrs = [
            cls ? `class="${cls}"` : null,
            alt ? `alt="${alt}"` : null
        ].filter(x => !!x)
        return `<img src="${uri}" ${attrs.join(' ')}>`
    }
    return ''
}
let SORT_METHODS = ['GET','POST','PATCH','PUT','DELETE']
function opSortName(op) {
    // group related services by model or inherited generic type
    let group = map(op.dataModel, x => x.name) || map(op.request.inherits, x => x.genericArgs && x.genericArgs[0]) 
    let sort1 = group ? group + map(SORT_METHODS.indexOf(op.method || 'ANY'), x => x === -1 ? '' : x.toString()) : 'z'
    return sort1 + `_` + op.request.name
}
function sortOps(ops) {
    ops.sort((a,b) => opSortName(a).localeCompare(opSortName(b)))
    return ops
}
const Files = (function () {
    let web = 'png,jpg,jpeg,gif,svg,webp'.split(',') 
    const Ext = {
        img:'png,jpg,jpeg,gif,svg,webp,png,jpg,jpeg,gif,bmp,tif,tiff,webp,ai,psd,ps'.split(','),
        vid:'avi,m4v,mov,mp4,mpg,mpeg,wmv,webm'.split(','),
        aud:'mp3,mpa,ogg,wav,wma,mid,webm'.split(','),
        ppt:'key,odp,pps,ppt,pptx'.split(','),
        xls:'xls,xlsm,xlsx,ods,csv,tsv'.split(','),
        doc:'doc,docx,pdf,rtf,tex,txt,md,rst,xls,xlsm,xlsx,ods,key,odp,pps,ppt,pptx'.split(','),
    }
    let S = (viewBox,body) => `<svg xmlns='http://www.w3.org/2000/svg' aria-hidden='true' role='img' preserveAspectRatio='xMidYMid meet' viewBox='${viewBox}'>${body}</svg>`
    const Icons = {
        img: S("0 0 16 16","<g fill='currentColor'><path d='M6.502 7a1.5 1.5 0 1 0 0-3a1.5 1.5 0 0 0 0 3z'/><path d='M14 14a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V2a2 2 0 0 1 2-2h5.5L14 4.5V14zM4 1a1 1 0 0 0-1 1v10l2.224-2.224a.5.5 0 0 1 .61-.075L8 11l2.157-3.02a.5.5 0 0 1 .76-.063L13 10V4.5h-2A1.5 1.5 0 0 1 9.5 3V1H4z'/></g>"),
        vid: S("0 0 24 24","<path fill='currentColor' d='m14 2l6 6v12a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h8m4 18V9h-5V4H6v16h12m-2-2l-2.5-1.7V18H8v-5h5.5v1.7L16 13v5Z'/>"),
        aud: S("0 0 24 24","<path fill='currentColor' d='M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V8l-6-6zM6 20V4h7v5h5v11H6zm10-9h-4v3.88a2.247 2.247 0 0 0-3.5 1.87c0 1.24 1.01 2.25 2.25 2.25S13 17.99 13 16.75V13h3v-2z'/>"),
        ppt: S("0 0 48 48","<g fill='none' stroke='currentColor' stroke-linecap='round' stroke-linejoin='round' stroke-width='4'><path d='M4 8h40'/><path d='M8 8h32v26H8V8Z' clip-rule='evenodd'/><path d='m22 16l5 5l-5 5m-6 16l8-8l8 8'/></g>"),
        xls: S("0 0 256 256","<path fill='currentColor' d='M200 26H72a14 14 0 0 0-14 14v26H40a14 14 0 0 0-14 14v96a14 14 0 0 0 14 14h18v26a14 14 0 0 0 14 14h128a14 14 0 0 0 14-14V40a14 14 0 0 0-14-14Zm-42 76h44v52h-44Zm44-62v50h-44V80a14 14 0 0 0-14-14h-2V38h58a2 2 0 0 1 2 2ZM70 40a2 2 0 0 1 2-2h58v28H70ZM38 176V80a2 2 0 0 1 2-2h104a2 2 0 0 1 2 2v96a2 2 0 0 1-2 2H40a2 2 0 0 1-2-2Zm32 40v-26h60v28H72a2 2 0 0 1-2-2Zm130 2h-58v-28h2a14 14 0 0 0 14-14v-10h44v50a2 2 0 0 1-2 2ZM69.2 148.4L84.5 128l-15.3-20.4a6 6 0 1 1 9.6-7.2L92 118l13.2-17.6a6 6 0 0 1 9.6 7.2L99.5 128l15.3 20.4a6 6 0 0 1-9.6 7.2L92 138l-13.2 17.6a6 6 0 1 1-9.6-7.2Z'/>"),
        doc: S("0 0 32 32","<path fill='currentColor' d='M26 30H11a2.002 2.002 0 0 1-2-2v-6h2v6h15V6h-9V4h9a2.002 2.002 0 0 1 2 2v22a2.002 2.002 0 0 1-2 2Z'/><path fill='currentColor' d='M17 10h7v2h-7zm-1 5h8v2h-8zm-1 5h9v2h-9zm-6-1a5.005 5.005 0 0 1-5-5V3h2v11a3 3 0 0 0 6 0V5a1 1 0 0 0-2 0v10H8V5a3 3 0 0 1 6 0v9a5.005 5.005 0 0 1-5 5z'/>"),
    }
    const symbols = /[\r\n%#()<>?[\\\]^`{|}]/g
    function encodeSvg(s) {
        s = s.replace(/"/g, `'`)
        s = s.replace(/>\s+</g, `><`)
        s = s.replace(/\s{2,}/g, ` `)
        return s.replace(symbols, encodeURIComponent)
    }
    function svgToDataUri(svg) {
        return "data:image/svg+xml;utf8," + encodeSvg(svg)
    }
    let Track = []
    function objectUrl(file) {
        let ret = URL.createObjectURL(file)
        Track.push(ret)
        return ret
    }
    function flush() {
        Track.forEach(x => {
            try {
                URL.revokeObjectURL(x)
            } catch (e) {
                console.error('URL.revokeObjectURL', e)
            }
        })
        Track = []
    }
    function fileImageUri(file) {
        let ext = lastRightPart(file.name,'.').toLowerCase()
        if (web.indexOf(ext) >= 0)
            return objectUrl(file)
        let keys = Object.keys(Ext)
        for (let i=0; i<keys.length; i++) {
            let k = keys[i]
            if (Ext[k].indexOf(ext) >= 0)
                return svgToDataUri(Icons[k])
        }
        return svgToDataUri(Icons.doc)
    }
    const k = 1024
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB']
    function formatBytes(bytes, d = 2) {
        if (bytes === 0) return '0 Bytes'
        const dm = d < 0 ? 0 : d
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i]
    }
    return {
        Ext,
        Icons,
        encodeSvg,
        svgToDataUri,
        fileImageUri,
        formatBytes,
        flush,
    }
})()
/*:minify*/
