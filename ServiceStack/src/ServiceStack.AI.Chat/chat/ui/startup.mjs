// Give the mounted shell a paint opportunity before extension work starts.
export function afterPaint() {
    return new Promise(resolve => {
        const timer = setTimeout(resolve, 100) // background tabs may suspend animation frames
        requestAnimationFrame(() => setTimeout(() => {
            clearTimeout(timer)
            resolve()
        }, 0))
    })
}

export async function importExtensions(extensions, importModule = path => import(path)) {
    const modules = await Promise.all(extensions.filter(x => x.path).map(async extension => {
        try {
            const module = await importModule(extension.path)
            if (typeof module.default?.install !== 'function') {
                throw new Error('Extension must export a default install(ctx) function')
            }
            return { extension, module, order: module.default.order ?? 0 }
        } catch (e) {
            console.error(`Failed to load extension ${extension.id}:`, e)
            return null
        }
    }))
    // A failed import must not break sorting or prevent other extensions installing.
    return modules.filter(Boolean).sort((a, b) => a.order - b.order)
}

export async function installExtensions(ctx, modules) {
    for (const result of modules) {
        try {
            // Install in order, including async installers with dependencies on earlier modules.
            await result.module.default.install(ctx)
            ctx.installedModules.push(result)
        } catch (e) {
            console.error(`Failed to install extension ${result.extension.id}:`, e)
        }
    }
}
