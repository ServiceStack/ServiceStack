export function displayPublicationUrl(url) {
    if (!url) return ''
    try { return decodeURIComponent(url) }
    catch { return url }
}

export function publicationDestination(path) {
    // Exports always end in <user>/<project>; keep the host's export root private.
    return path ? '~/' + path.replace(/\\/g, '/').split('/').filter(Boolean).slice(-2).join('/') : ''
}

export function publicationUrl(config, publication, resolveUrl) {
    if (!publication?.urlPath) return null
    if (!config?.baseUrl) return resolveUrl?.(publication.urlPath) || null
    const relative = publication.urlPath.split('/').filter(Boolean).slice(-2).join('/')
    return config.baseUrl.replace(/\/+$/, '') + '/' + relative + '/'
}

export function publicationAge(timestamp, now = Date.now()) {
    const seconds = Math.max(0, Math.floor((now - new Date(timestamp).getTime()) / 1000))
    if (!Number.isFinite(seconds)) return ''
    for (const [duration, unit] of [[31536000, 'y'], [2592000, 'mo'], [604800, 'w'], [86400, 'd'], [3600, 'h'], [60, 'm'], [1, 's']]) {
        if (seconds >= duration) return `${Math.floor(seconds / duration)}${unit} ago`
    }
    return '0s ago'
}

export async function publishProjectOutput(ctx, ext, project, source, destination) {
    const snapshot = { ...project, publish: source }
    const owner = ctx.ai.auth?.userName || 'default'
    if (source !== project.publish) {
        const saved = await ctx.projects.saveProject(snapshot.name, snapshot, { reportError: false })
        if (saved.error) return saved
    }
    if ((ctx.ai.auth?.userName || 'default') !== owner) {
        return { error: { message: 'Account changed during publishing' } }
    }
    const path = destination === 'folder'
        ? `/project/${encodeURIComponent(snapshot.id)}/folder`
        : `/project/${encodeURIComponent(snapshot.name)}`
    return ext.postJson(path)
}
