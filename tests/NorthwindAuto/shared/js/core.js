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
