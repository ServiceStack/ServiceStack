import { defineAsyncComponent } from 'vue'

// Share an import and initialization across a page's components; allow retry after failure.
export function lazyModule(loader, initialize = () => {}) {
    let pending
    return () => pending ??= loader().then(async module => {
        await initialize(module)
        return module
    }).catch(error => {
        pending = null
        throw error
    })
}

export function lazyComponent(loader) {
    return defineAsyncComponent({
        loader,
        delay: 150,
        loadingComponent: { template: '<div role="status" class="p-4">Loading…</div>' },
        errorComponent: { template: '<div role="alert" class="p-4">Unable to load this view. Please reload the page.</div>' },
    })
}

const assets = new Map()
function loadAsset(url, type) {
    if (!assets.has(url)) {
        assets.set(url, new Promise((resolve, reject) => {
            const element = document.createElement(type === 'script' ? 'script' : 'link')
            if (type === 'script') element.src = url
            else { element.rel = 'stylesheet'; element.href = url }
            element.onload = resolve
            element.onerror = () => {
                element.remove()
                assets.delete(url)
                reject(new Error(`Unable to load ${url}`))
            }
            document.head.appendChild(element)
        }))
    }
    return assets.get(url)
}

export const loadScript = url => loadAsset(url, 'script')
export const loadStylesheet = url => loadAsset(url, 'style')

export async function loadCodeEditor(ctx) {
    // Editors retain their textarea fallback when the core_tools extension is disabled.
    if (!ctx.state.config.extensions.includes('core_tools')) return
    const base = ctx.ai.resolvePath('/ext/core_tools/codemirror')
    await Promise.all([
        loadScript(`${base}/codemirror.js`),
        loadStylesheet(`${base}/codemirror.css`),
        loadStylesheet(`${base}/theme/mocha.css`),
    ])
    // Modes and addons must run after CodeMirror creates its global.
    await Promise.all([
        'mode/clike/clike.js', 'mode/javascript/javascript.js', 'mode/python/python.js',
        'addon/edit/matchbrackets.js', 'addon/selection/active-line.js',
    ].map(path => loadScript(`${base}/${path}`)))
}
