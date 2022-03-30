/*minify:*/
/**
 * Alt solution to optional chaining by only executing fn accessor if object is not null
 * @example
 * let a = b()?.c // equivalent to:
 * let a = map(b(), x => x.c)
 * @template T,V
 * @param {T} o
 * @param {(a:T) => V} f
 * @returns {V|null} 
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
    if (!requiresAnyRole.some(role => roles.indexOf(role) >= 0))
        return false
    if (!requiredPermissions.every(perm => permissions.indexOf(perm) >= 0))
        return false
    if (!requiresAnyPermission.every(perm => permissions.indexOf(perm) >= 0))
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
    missingRoles = requiresAnyRole.filter(x => roles.indexOf(x) < 0)
    if (missingRoles.length > 0)
        return `Requires any ${missingRoles.map(x => '<b>' + x + '</b>').join(', ')} Role` + (missingRoles.length > 1 ? 's' : '')
    missingPerms = requiresAnyPermission.filter(x => permissions.indexOf(x) < 0)
    if (missingPerms.length > 0)
        return `Requires any ${missingPerms.map(x => '<b>' + x + '</b>').join(', ')} Permission` + (missingPerms.length > 1 ? 's' : '')
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
            svg = `<svg ${attrs.join(' ')}' ${svg.substring(4)}`
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
/** Wrapper around SVG icons for File Types */
const Files = (function () {
    let web = 'png,jpg,jpeg,gif,svg,webp'.split(',') 
    const Ext = {
        img:'png,jpg,jpeg,gif,svg,webp,png,jpg,jpeg,gif,bmp,tif,tiff,webp,ai,psd,ps'.split(','),
        vid:'avi,m4v,mov,mp4,mpg,mpeg,wmv,webm'.split(','),
        aud:'mp3,mpa,ogg,wav,wma,mid,webm'.split(','),
        ppt:'key,odp,pps,ppt,pptx'.split(','),
        xls:'xls,xlsm,xlsx,ods,csv,tsv'.split(','),
        doc:'doc,docx,pdf,rtf,tex,txt,md,rst,xls,xlsm,xlsx,ods,key,odp,pps,ppt,pptx'.split(','),
        zip:'zip,tar,gz,7z,rar,gzip,deflate,br,iso,dmg,z,lz,lz4,lzh,s7z,apl,arg,jar,war'.split(','),
        exe:'exe,bat,sh,cmd,com,app,msi,run,vb,vbs,js,ws,wsh'.split(','),
        att:'bin,oct,dat'.split(','), //attachment
    }
    const ExtKeys = Object.keys(Ext)
    let S = (viewBox,body) => `<svg xmlns='http://www.w3.org/2000/svg' aria-hidden='true' role='img' preserveAspectRatio='xMidYMid meet' viewBox='${viewBox}'>${body}</svg>`
    const Icons = {
        img: S("0 0 16 16","<g fill='currentColor'><path d='M6.502 7a1.5 1.5 0 1 0 0-3a1.5 1.5 0 0 0 0 3z'/><path d='M14 14a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V2a2 2 0 0 1 2-2h5.5L14 4.5V14zM4 1a1 1 0 0 0-1 1v10l2.224-2.224a.5.5 0 0 1 .61-.075L8 11l2.157-3.02a.5.5 0 0 1 .76-.063L13 10V4.5h-2A1.5 1.5 0 0 1 9.5 3V1H4z'/></g>"),
        vid: S("0 0 24 24","<path fill='currentColor' d='m14 2l6 6v12a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h8m4 18V9h-5V4H6v16h12m-2-2l-2.5-1.7V18H8v-5h5.5v1.7L16 13v5Z'/>"),
        aud: S("0 0 24 24","<path fill='currentColor' d='M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V8l-6-6zM6 20V4h7v5h5v11H6zm10-9h-4v3.88a2.247 2.247 0 0 0-3.5 1.87c0 1.24 1.01 2.25 2.25 2.25S13 17.99 13 16.75V13h3v-2z'/>"),
        ppt: S("0 0 48 48","<g fill='none' stroke='currentColor' stroke-linecap='round' stroke-linejoin='round' stroke-width='4'><path d='M4 8h40'/><path d='M8 8h32v26H8V8Z' clip-rule='evenodd'/><path d='m22 16l5 5l-5 5m-6 16l8-8l8 8'/></g>"),
        xls: S("0 0 256 256","<path fill='currentColor' d='M200 26H72a14 14 0 0 0-14 14v26H40a14 14 0 0 0-14 14v96a14 14 0 0 0 14 14h18v26a14 14 0 0 0 14 14h128a14 14 0 0 0 14-14V40a14 14 0 0 0-14-14Zm-42 76h44v52h-44Zm44-62v50h-44V80a14 14 0 0 0-14-14h-2V38h58a2 2 0 0 1 2 2ZM70 40a2 2 0 0 1 2-2h58v28H70ZM38 176V80a2 2 0 0 1 2-2h104a2 2 0 0 1 2 2v96a2 2 0 0 1-2 2H40a2 2 0 0 1-2-2Zm32 40v-26h60v28H72a2 2 0 0 1-2-2Zm130 2h-58v-28h2a14 14 0 0 0 14-14v-10h44v50a2 2 0 0 1-2 2ZM69.2 148.4L84.5 128l-15.3-20.4a6 6 0 1 1 9.6-7.2L92 118l13.2-17.6a6 6 0 0 1 9.6 7.2L99.5 128l15.3 20.4a6 6 0 0 1-9.6 7.2L92 138l-13.2 17.6a6 6 0 1 1-9.6-7.2Z'/>"),
        doc: S("0 0 32 32","<path fill='currentColor' d='M26 30H11a2.002 2.002 0 0 1-2-2v-6h2v6h15V6h-9V4h9a2.002 2.002 0 0 1 2 2v22a2.002 2.002 0 0 1-2 2Z'/><path fill='currentColor' d='M17 10h7v2h-7zm-1 5h8v2h-8zm-1 5h9v2h-9zm-6-1a5.005 5.005 0 0 1-5-5V3h2v11a3 3 0 0 0 6 0V5a1 1 0 0 0-2 0v10H8V5a3 3 0 0 1 6 0v9a5.005 5.005 0 0 1-5 5z'/>"),
        zip: S("0 0 16 16","<g fill='currentColor'><path d='M6.5 7.5a1 1 0 0 1 1-1h1a1 1 0 0 1 1 1v.938l.4 1.599a1 1 0 0 1-.416 1.074l-.93.62a1 1 0 0 1-1.109 0l-.93-.62a1 1 0 0 1-.415-1.074l.4-1.599V7.5zm2 0h-1v.938a1 1 0 0 1-.03.243l-.4 1.598l.93.62l.93-.62l-.4-1.598a1 1 0 0 1-.03-.243V7.5z'/><path d='M2 2a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V2zm5.5-1H4a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1V2a1 1 0 0 0-1-1H9v1H8v1h1v1H8v1h1v1H7.5V5h-1V4h1V3h-1V2h1V1z'/></g>"),
        exe: S("0 0 16 16","<path fill='currentColor' fill-rule='evenodd' d='M14 4.5V14a2 2 0 0 1-2 2h-1v-1h1a1 1 0 0 0 1-1V4.5h-2A1.5 1.5 0 0 1 9.5 3V1H4a1 1 0 0 0-1 1v9H2V2a2 2 0 0 1 2-2h5.5L14 4.5ZM2.575 15.202H.785v-1.073H2.47v-.606H.785v-1.025h1.79v-.648H0v3.999h2.575v-.647ZM6.31 11.85h-.893l-.823 1.439h-.036l-.832-1.439h-.931l1.227 1.983l-1.239 2.016h.861l.853-1.415h.035l.85 1.415h.908l-1.254-1.992L6.31 11.85Zm1.025 3.352h1.79v.647H6.548V11.85h2.576v.648h-1.79v1.025h1.684v.606H7.334v1.073Z'/>"),
        att: S("0 0 24 24","<path fill='currentColor' d='M14 0a5 5 0 0 1 5 5v12a7 7 0 1 1-14 0V9h2v8a5 5 0 0 0 10 0V5a3 3 0 1 0-6 0v12a1 1 0 1 0 2 0V6h2v11a3 3 0 1 1-6 0V5a5 5 0 0 1 5-5Z'/>"),
    }
    //<svg xmlns="http://www.w3.org/2000/svg" aria-hidden="true" role="img" width="1em" height="1em" preserveAspectRatio="xMidYMid meet" viewBox="0 0 24 24"><path fill="currentColor" d="M14 0a5 5 0 0 1 5 5v12a7 7 0 1 1-14 0V9h2v8a5 5 0 0 0 10 0V5a3 3 0 1 0-6 0v12a1 1 0 1 0 2 0V6h2v11a3 3 0 1 1-6 0V5a5 5 0 0 1 5-5Z"/></svg>
    const symbols = /[\r\n%#()<>?[\\\]^`{|}]/g
    /** @param {string} s */
    function encodeSvg(s) {
        s = s.replace(/"/g, `'`)
        s = s.replace(/>\s+</g, `><`)
        s = s.replace(/\s{2,}/g, ` `)
        return s.replace(symbols, encodeURIComponent)
    }
    /** @param {string} svg */
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
    /** @param {string} path */
    function getFileName(path) {
        if (!path) return null
        let noQs = leftPart(path,'?')
        return lastRightPart(noQs,'/')
    }
    /** @param {string} path */
    function getExt(path) {
        let fileName = getFileName(path)
        if (fileName == null || fileName.indexOf('.') === -1)
            return null
        return lastRightPart(fileName,'.').toLowerCase()
    }
    /** @param {File} file */
    function fileImageUri(file) {
        let ext = getExt(file.name)
        if (web.indexOf(ext) >= 0)
            return objectUrl(file)
        return filePathUri(file.name)
    }
    /** @param {string} path */
    function canPreview(path) {
        if (!path) return false
        if (path.startsWith('blob:') || path.startsWith('data:'))
            return true
        let ext = getExt(path)
        return ext && web.indexOf(ext) >= 0;
    }
    /** @param {string} path */
    function filePathUri(path) {
        if (!path) return null
        let ext = getExt(path)
        if (ext == null || canPreview(path))
            return toAppUrl(path)
        return extSrc(ext) || svgToDataUri(Icons.doc)
    }
    function extSrc(ext) {
        if (Icons[ext])
            svgToDataUri(Icons[ext])
        for (let i=0; i<ExtKeys.length; i++) {
            let k = ExtKeys[i]
            if (Ext[k].indexOf(ext) >= 0)
                return svgToDataUri(Icons[k])
        }
        return null
    }
    const k = 1024
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB']
    /** @param {number} bytes
     *  @param {number} [d=2] */
    function formatBytes(bytes, d = 2) {
        if (bytes === 0) return '0 Bytes'
        const dm = d < 0 ? 0 : d
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i]
    }
    return {
        Ext,
        Icons,
        getExt,
        extSrc,
        encodeSvg,
        canPreview,
        svgToDataUri,
        fileImageUri,
        filePathUri,
        formatBytes,
        getFileName,
        flush,
    }
})()
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
/** @param {*} attrs */
function linkAttrs(attrs) {
    return Object.assign({target:'_blank',rel:'noopener','class':'text-blue-600'},attrs)
}
/** Create formatted HTML A URL links  
 * @param {string} href 
 * @param {*} [opt] */
function link(href, opt) {
    return htmlTag('a', href, linkAttrs({ ...opt, href }))
}
/** Create formatted HTML A mailto: links
 * @param {string} email
 * @param {*} [opt] */
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
 * @param {*} [opt] */
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
        || Files.svgToDataUri(Files.Icons.doc)
}
/** marker fn, special-cased to hide from query results
 * @param o
 * @return {string}
 */
function hidden(o) { return '' }
/*:minify*/
