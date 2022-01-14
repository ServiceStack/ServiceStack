import { apiValue, isDate, mapGet, padInt } from "@servicestack/client"

export const $1 = (sel, el) => typeof sel === "string" ? (el || document).querySelector(sel) : sel || null
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

export function isSmall() { 
    return window.matchMedia('(max-width:640px)').matches 
}

export function humanify(id) { 
    return humanize(toPascalCase(id)) 
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
    return `col-span-6` + (fields === 2 ? ' sm:col-span-3' : fields === 3 ? ' sm:col-span-2' : '')
}

export function toggleAttr(el, attr) {
    let hasAttr = el.hasAttribute(attr) 
    if (hasAttr) 
        el.removeAttribute(attr)
    else
        el.setAttribute(attr, 'true')
    return hasAttr
}

export function parentsWithAttr(el, attr)
{
    let els = []
    let parentEl = el
    while ((parentEl = parentEl.parentElement.closest(`[${attr}]`)) != null) {
        els.push(parentEl)
        let siblingEl = parentEl
        while ((siblingEl = siblingEl.previousElementSibling) != null) {
            if (siblingEl.hasAttribute(attr)) els.push(siblingEl)
        }
        siblingEl = parentEl
        while ((siblingEl = siblingEl.nextElementSibling) != null) {
            if (siblingEl.hasAttribute(attr)) els.push(siblingEl)
        }
    }
    return els
}