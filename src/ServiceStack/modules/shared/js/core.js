const $1 = (sel, el) => typeof sel === "string" ? (el || document).querySelector(sel) : sel || null
const $$ = (sel, el) => typeof sel === "string"
    ? Array.prototype.slice.call((el || document).querySelectorAll(sel))
    : Array.isArray(sel) ? sel : [sel]
function on(sel, handlers) {
    $$(sel).forEach(e => {
        Object.keys(handlers).forEach(function (evt) {
            let fn = handlers[evt]
            if (typeof evt === 'string' && typeof fn === 'function') {
                e.addEventListener(evt, fn.bind(e))
            }
        })
    })
}
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
function colClass(fields) {
    return `col-span-6` + (fields === 2 ? ' sm:col-span-3' : fields === 3 ? ' sm:col-span-3' : '')
}
