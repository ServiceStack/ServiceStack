/**: Extend locode App with custom JS **/

/** Custom [Format] method to style text with custom class
 * @param {*} val
 * @param {{cls:string}} opt */
function stylize(val, opt) {
    let cls = opt && opt.cls || 'text-green-600'
    return `<span class="${cls}">${val}</span>`
}
