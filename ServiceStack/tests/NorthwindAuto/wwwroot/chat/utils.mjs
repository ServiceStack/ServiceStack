import { $$, createElement, rightPart } from "@servicestack/client"

export function toJsonArray(json) {
    try {
        return json ? JSON.parse(json) : []
    } catch (e) {
        return []
    }
}

export function toJsonObject(json) {
    try {
        return json ? JSON.parse(json) : null
    } catch (e) {
        return null
    }
}

export function storageArray(key, save) {
    if (save && Array.isArray(save)) {
        localStorage.setItem(key, JSON.stringify(save))
    }
    return toJsonArray(localStorage.getItem(key)) ?? []
}

export function storageObject(key, save) {
    if (typeof save == 'object') {
        localStorage.setItem(key, JSON.stringify(save))
    }
    return toJsonObject(localStorage.getItem(key)) ?? {}
}

export function deepClone(obj) {
    return JSON.parse(JSON.stringify(obj))
}

export function fileToBase64(file) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.readAsDataURL(file) //= "data:…;base64,…"
    reader.onload  = () => {
        resolve(rightPart(reader.result, ',')) // strip prefix
    }
    reader.onerror = err => reject(err)
  })
}

export function fileToDataUri(file) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.readAsDataURL(file) //= "data:…;base64,…"
    reader.onload  = () => resolve(reader.result)
    reader.onerror = err => reject(err)
  })
}

const svg = {
    clipboard: `<svg class="w-6 h-6" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><g fill="none"><path d="M8 5H6a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2v-1M8 5a2 2 0 0 0 2 2h2a2 2 0 0 0 2-2M8 5a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2m0 0h2a2 2 0 0 1 2 2v3m2 4H10m0 0l3-3m-3 3l3 3" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"></path></g></svg>`,
    check: `<svg class="w-6 h-6 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg>`,
}

function copyBlock(btn) {
    // console.log('copyBlock',btn)
    const label = btn.previousElementSibling
    const code = btn.parentElement.nextElementSibling
    label.classList.remove('hidden')
    label.innerHTML = 'copied'
    btn.classList.add('border-gray-600', 'bg-gray-700')
    btn.classList.remove('border-gray-700')
    btn.innerHTML = svg.check
    navigator.clipboard.writeText(code.innerText)
    setTimeout(() => {
        label.classList.add('hidden')
        label.innerHTML = ''
        btn.innerHTML = svg.clipboard
        btn.classList.remove('border-gray-600', 'bg-gray-700')
        btn.classList.add('border-gray-700')
    }, 2000)
}

export function addCopyButtonToCodeBlocks(sel) {
    globalThis.copyBlock ??= copyBlock
    //console.log('addCopyButtonToCodeBlocks', sel, [...$$(sel)].length)

    $$(sel).forEach(code => {
        let pre = code.parentElement;
        if (pre.classList.contains('group')) return
        pre.classList.add('relative', 'group')

        const div = createElement('div', {attrs: {className: 'opacity-0 group-hover:opacity-100 transition-opacity duration-100 flex absolute right-2 -mt-1 select-none'}})
        const label = createElement('div', {attrs: {className: 'hidden font-sans p-1 px-2 mr-1 rounded-md border border-gray-600 bg-gray-700 text-gray-400'}})
        const btn = createElement('button', {
            attrs: {
                type: 'button',
                className: 'p-1 rounded-md border block text-gray-500 hover:text-gray-400 border-gray-700 hover:border-gray-600',
                onclick: 'copyBlock(this)'
            }
        })
        btn.innerHTML = svg.clipboard
        div.appendChild(label)
        div.appendChild(btn)
        pre.insertBefore(div, code)
    })
}

export function addCopyButtons() {
    addCopyButtonToCodeBlocks('.prose pre>code')
}

/**
 * Returns an ever-increasing unique integer id.
 */
export const nextId = (() => {
  let last = 0               // cache of the last id that was handed out
  return () => {
    const now = Date.now() // current millisecond timestamp
    last = (now > last) ? now : last + 1
    return last
  }
})();