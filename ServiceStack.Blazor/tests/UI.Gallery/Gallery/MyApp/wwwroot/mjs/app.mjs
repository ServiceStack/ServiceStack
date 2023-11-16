export async function remount() {
    document.querySelectorAll('[data-module]').forEach(async el => {
        let modulePath = el.dataset.module
        if (!modulePath) return
        if (!modulePath.startsWith('/') && !modulePath.startsWith('.')) {
            modulePath = `../${modulePath}`
        }
        try {
            const module = await import(modulePath)
            if (typeof module.default?.load == 'function') {
                module.default.load()
            }
        } catch (e) {
            console.error(`Couldn't load module ${el.dataset.module}`, e)
        }
    })
}

document.addEventListener('DOMContentLoaded', () =>
    Blazor.addEventListener('enhancedload', () => {
        remount()
        globalThis.hljs?.highlightAll()
    }))
