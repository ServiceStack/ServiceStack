/*minify:*/
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
/*:minify*/
