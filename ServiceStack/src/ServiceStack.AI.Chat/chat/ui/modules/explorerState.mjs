export function directoryBreadcrumbs(path, root) {
    if (!path || !root) return []
    const separator = root.includes('\\') ? '\\' : '/'
    const base = root.replace(/[\\/]+$/, '')
    const tail = path.slice(base.length).split(/[\\/]/).filter(Boolean)
    const crumbs = [{ name: base.split(/[\\/]/).pop() || root, path: root }]
    let current = base
    for (const name of tail) { current += separator + name; crumbs.push({ name, path: current }) }
    return crumbs
}
export function workspaceQuery(query, changes) {
    const next = { ...query, ...changes }
    for (const [key, value] of Object.entries(next)) if (value == null || value === '') delete next[key]
    return next
}

// A disabled extension's saved URL falls back to Files; extensions can retain old links via aliases.
export function resolveWorkspaceView(icons, selected) {
    if (selected && Object.hasOwn(icons, selected)) return selected
    return Object.keys(icons).find(id => icons[id].aliases?.includes(selected)) || 'files'
}

// Keep workspace visibility across page navigation while letting destinations opt out.
export function installWorkspaceNavigation(router) {
    return router.beforeEach((to, from) => {
        if (to.meta.workspaceSidebar === false) {
            if (!['workspace', 'workspacePath', 'workspaceFile', 'workspacePreview', 'workspaceCommit', 'workspaceView', 'workspaceProject'].some(key => key in to.query)) return
            return { path: to.path, hash: to.hash, query: workspaceQuery(to.query, {
                workspace: null, workspacePath: null, workspaceFile: null, workspacePreview: null, workspaceCommit: null, workspaceView: null, workspaceProject: null,
            }), replace: true }
        }
        if ('workspace' in to.query || from.query.workspace !== '1') return
        return { path: to.path, hash: to.hash, query: workspaceQuery(to.query, {
            workspace: '1', workspacePath: from.query.workspacePath,
            workspaceView: from.query.workspaceView, workspaceProject: from.query.workspaceProject,
        }) }
    })
}
