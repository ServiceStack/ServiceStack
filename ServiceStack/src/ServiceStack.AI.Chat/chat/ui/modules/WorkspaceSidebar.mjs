import { computed, inject, reactive, ref, watch, onMounted, onUnmounted, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { directoryBreadcrumbs, workspaceQuery, resolveWorkspaceView } from './explorerState.mjs'
import { createWorkspaceTree } from './workspaceTree.mjs'
import WorkspaceTreeNode from './WorkspaceTreeNode.mjs'

export default {
    components: { WorkspaceTreeNode },
    emits: ['close'],
    setup() {
        const route = useRoute(), router = useRouter(), ctx = inject('ctx')
        const data = ref(null), error = ref(''), loading = ref(false), root = ref(null)
        const refreshKey = ref(0), panelBusy = ref(false), filesRefreshing = ref(false)
        const icons = computed(() => ctx.visibleComponents(ctx.right, { workspace: data.value, projectId: projectId.value }))
        const view = computed(() => resolveWorkspaceView(icons.value, route.query.workspaceView))
        const busy = computed(() => loading.value || panelBusy.value || filesRefreshing.value)
        const projectId = computed(() => {
            const thread = ctx.threads?.currentThread.value
            return String(thread?.id) === ctx.chat.drafts.state.key ? thread?.projectId : ctx.chat.drafts.get().projectId
        })
        const rootPickerOpen = ref(false), rootPicker = ref(null), rootMenu = ref(null)
        let rootTrigger
        function closeRootPicker() { rootPickerOpen.value = false; rootTrigger?.focus() }
        async function toggleRootPicker(event) {
            rootTrigger = event.currentTarget
            rootPickerOpen.value = !rootPickerOpen.value
            if (rootPickerOpen.value) { await nextTick(); rootMenu.value?.querySelector('[aria-checked="true"]')?.focus() }
        }
        function outsideRootPicker(event) {
            if (rootPickerOpen.value && !rootPicker.value?.contains(event.target)) rootPickerOpen.value = false
        }
        onMounted(() => document.addEventListener('pointerdown', outsideRootPicker))
        onUnmounted(() => document.removeEventListener('pointerdown', outsideRootPicker))
        function chooseRoot(path) {
            closeRootPicker()
            router.push({ query: workspaceQuery(route.query, { workspacePath: path, workspaceFile: null, workspacePreview: null, workspaceCommit: null, workspaceProject: projectId.value || null }) })
        }
        function onRootPickerKeydown(event) {
            if (!rootPickerOpen.value) return
            if (event.key === 'Escape') { event.stopPropagation(); closeRootPicker(); return }
            const items = [...(rootMenu.value?.querySelectorAll('button') || [])]
            const index = items.indexOf(document.activeElement)
            const next = { ArrowDown: (index + 1) % items.length, ArrowUp: (index - 1 + items.length) % items.length, Home: 0, End: items.length - 1 }[event.key]
            if (next != null) { event.preventDefault(); items[next]?.focus() }
        }
        function clickDirectory(path) {
            rootPickerOpen.value = false
            const opening = !tree.node(path).expanded
            toggle(path)
            if (opening) navigate(path)
        }
        const collapsed = new Set()
        const state = reactive({ nodes: {} })
        async function request(path) {
            const query = new URLSearchParams()
            if (projectId.value) query.set('projectId', projectId.value)
            if (path) query.set('path', path)
            const api = await ctx.getJson('/ext/projects/explorer?' + query)
            if (!api.response) throw new Error(api.error?.message || 'Unable to load workspace')
            return api.response
        }
        const tree = createWorkspaceTree(state, path => request(path))
        let generation = 0
        onUnmounted(() => { generation++; tree.reset() })
        function findRoot(response) {
            return response.roots.find(r => response.path === r || response.path?.startsWith(r.replace(/[\\/]+$/, '') + (r.includes('\\') ? '\\' : '/')))
        }
        async function load() {
            const current = ++generation
            loading.value = true; error.value = ''
            const path = (route.query.workspaceProject || null) === (projectId.value || null) ? route.query.workspacePath : undefined
            try {
                const selected = await tree.ensure(path)
                if (current !== generation || !selected) return
                const response = selected.response
                data.value = response
                root.value = findRoot(response) || null
                if (!root.value) return
                // Restore just the ancestor chain for URL selection, leaving siblings collapsed.
                for (const crumb of directoryBreadcrumbs(response.path, root.value)) {
                    const ancestor = await tree.ensure(crumb.path)
                    if (current !== generation || !ancestor) return
                    if (!collapsed.has(crumb.path)) ancestor.expanded = true
                }
            } catch (e) { if (current === generation) error.value = e.message }
            finally { if (current === generation) loading.value = false }
        }
        async function toggle(path) {
            rootPickerOpen.value = false
            const item = tree.node(path)
            item.expanded = !item.expanded
            if (item.expanded) collapsed.delete(path)
            else collapsed.add(path)
            if (item.expanded) { try { await tree.ensure(path) } catch { /* Display error on this folder. */ } }
        }
        function navigate(path) {
            rootPickerOpen.value = false
            for (const crumb of directoryBreadcrumbs(path, root.value)) collapsed.delete(crumb.path)
            if (route.query.workspacePath === path && view.value === 'files' && !route.query.workspaceFile) {
                tree.node(path).expanded = true
                load()
            } else router.push({ query: workspaceQuery(route.query, { workspacePath: path, workspaceFile: null, workspacePreview: null, workspaceCommit: null, workspaceView: 'files', workspaceProject: projectId.value || null }) })
        }
        function selectFile(entry) {
            rootPickerOpen.value = false
            let path = entry.path.slice(0, Math.max(entry.path.lastIndexOf('/'), entry.path.lastIndexOf('\\'))) || '/'
            if (/^[A-Za-z]:$/.test(path)) path += '\\'
            path = entry.directoryPath || path
            router.push({ query: workspaceQuery(route.query, { workspacePath: path, workspaceFile: entry.path, workspacePreview: entry.preview || null, workspaceCommit: entry.commit || null, workspaceProject: projectId.value || null }) })
        }
        function changeView(selectedView) {
            rootPickerOpen.value = false
            if (selectedView === 'git' && view.value === 'git') refreshKey.value++
            router.push({ query: workspaceQuery(route.query, { workspaceView: selectedView, workspaceFile: null, workspacePreview: null, workspaceCommit: null }) })
        }
        async function refresh() {
            rootPickerOpen.value = false
            if (view.value !== 'files') { refreshKey.value++; return }
            const expanded = [...new Set(Object.values(state.nodes).filter(item => item.expanded).map(item => item.path))]
            tree.reset()
            filesRefreshing.value = true
            const current = generation + 1
            try {
                await load()
                if (current !== generation) return
                for (const path of expanded) {
                    if (collapsed.has(path)) continue
                    try {
                        const item = await tree.ensure(path)
                        if (current !== generation) return
                        if (item && !collapsed.has(path)) item.expanded = true
                    } catch { /* Failed folders retain an inline retry. */ }
                }
            } finally { filesRefreshing.value = false }
        }
        const breadcrumbs = computed(() => directoryBreadcrumbs(data.value?.path, root.value))
        const rootEntry = computed(() => root.value ? {path: root.value, name: root.value.replace(/[\\/]+$/, '').split(/[\\/]/).pop() || root.value, directory: true} : null)
        watch(projectId, (id) => {
            rootPickerOpen.value = false
            collapsed.clear()
            generation++; tree.reset(); data.value = null; root.value = null
            if ((route.query.workspaceProject || null) !== (id || null)) router.replace({ query: workspaceQuery(route.query, { workspaceProject: id || null, workspacePath: null, workspaceFile: null, workspacePreview: null, workspaceCommit: null }) })
        }, { immediate: true })
        watch(() => [route.query.workspacePath, projectId.value], load, { immediate: true })
        watch(view, () => { panelBusy.value = false })
        watch(() => route.query.workspaceView, selected => {
            const resolved = resolveWorkspaceView(ctx.right, selected)
            if (selected && selected !== resolved) router.replace({ query: workspaceQuery(route.query, { workspaceView: resolved }) })
        }, { immediate: true })
        return { projectId, icons, refreshKey, panelBusy, busy, data, error, loading, view, refresh, load, navigate, selectFile, changeView, breadcrumbs, route, root, rootEntry, tree, toggle, clickDirectory, rootPickerOpen, rootPicker, rootMenu, toggleRootPicker, closeRootPicker, chooseRoot, onRootPickerKeydown }
    },
    template: `<aside id="workspace-sidebar" aria-label="Workspace explorer" class="flex flex-col h-full border-l" :class="[$styles.bgSidebar, $styles.chromeBorder]">
        <div :class="$styles.chromeBorder" data-app-toolbar data-workspace-toolbar class="min-h-9 pt-1 pb-1 flex items-center gap-1 pl-2 pr-1 shrink-0 shadow-[inset_0_-1px_0_var(--border)]">
            <div role="group" aria-label="Workspace view" class="flex items-center gap-1 flex-1 min-w-0">
                <button v-for="(icon, id) in icons" :key="id" type="button" @click="changeView(id)" :aria-label="icon.title || icon.name" :title="icon.title || icon.name" :aria-pressed="view === id"  :class="[view === id ? $styles.iconActive : $styles.icon, $styles.iconHover]" data-workspace-icon class="flex items-center justify-center w-7 h-7 p-1 border border-transparent rounded shrink-0 cursor-pointer disabled:opacity-45 disabled:cursor-default focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-[var(--ring)] focus-visible:-outline-offset-0.5">
                    <component :is="icon.component" />
                </button>
            </div>
            <button type="button" @click="refresh" :disabled="busy" title="Refresh workspace" aria-label="Refresh workspace"  :class="[$styles.icon, $styles.iconHover]" data-workspace-icon class="flex items-center justify-center w-7 h-7 p-1 border border-transparent rounded shrink-0 cursor-pointer disabled:opacity-45 disabled:cursor-default focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-[var(--ring)] focus-visible:-outline-offset-0.5">
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" aria-hidden="true" :class="{ 'animate-spin motion-reduce:animate-none': busy }"  class="w-4.5 h-4.5"><path d="M0 0h24v24H0z" fill="none"/><path fill="currentColor" fill-rule="evenodd" d="M10.546 5.132L8.828 3.414L10.243 2l3.889 3.89a.5.5 0 0 1 0 .706l-3.89 3.89L8.829 9.07l1.946-1.946a6 6 0 1 0 5.468 1.632l1.415-1.414a8 8 0 1 1-7.11-2.211z"/></svg>
            </button>
            <button type="button" @click="$emit('close')" title="Close workspace" aria-label="Close workspace"  :class="[$styles.icon, $styles.iconHover]" data-workspace-icon class="flex items-center justify-center w-7 h-7 p-1 border border-transparent rounded shrink-0 cursor-pointer disabled:opacity-45 disabled:cursor-default focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-[var(--ring)] focus-visible:-outline-offset-0.5">
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true"  class="w-4.5 h-4.5"><path d="m6 6 12 12M6 18 18 6"/></svg>
            </button>
        </div>
        <div v-if="data?.roots.length" ref="rootPicker"  :class="$styles.chromeBorder" @keydown="onRootPickerKeydown" data-workspace-location class="flex items-center h-[35px] py-0 px-2 shrink-0 text-xs border-b relative">
            <nav aria-label="Workspace directory breadcrumbs"  data-workspace-breadcrumbs class="flex items-center gap-1 overflow-x-auto whitespace-nowrap">
                <template v-for="(crumb, index) in breadcrumbs" :key="crumb.path"  class="shrink-0">
                    <span v-if="index"  aria-hidden="true"  class="shrink-0 opacity-40">/</span>
                    <button type="button" @click="index === 0 ? toggleRootPicker($event) : navigate(crumb.path)" :title="index === 0 ? 'Switch workspace directory: ' + crumb.path : crumb.path" :aria-label="index === 0 ? 'Choose workspace directory' : undefined" :aria-haspopup="index === 0 ? 'menu' : undefined" :aria-expanded="index === 0 ? rootPickerOpen : undefined"  :class="[{ 'hover:bg-gray-500/12 aria-expanded:bg-gray-500/12 focus-visible:bg-gray-500/12': index === 0 }, $styles.muted, $styles.iconHover]" data-workspace-breadcrumb-button class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-[var(--ring)] focus-visible:-outline-offset-0.5 shrink-0 rounded py-0.75 px-1 text-left cursor-pointer">{{crumb.name}}</button>
                </template>
            </nav>
            <div v-if="rootPickerOpen" ref="rootMenu" role="menu" aria-label="Allowed directories"  :class="$styles.messageAssistant" data-workspace-root-menu class="absolute top-[calc(100%_+_4px)] left-2 right-2 max-h-[min(320px,_60vh)] overflow-y-auto border rounded-md shadow-lg p-1 z-50">
                <div class="px-2 py-1 text-xs opacity-60">Switch directory</div>
                <button v-for="directory in data.roots" :key="directory" type="button" role="menuitemradio" :aria-checked="root === directory" @click="chooseRoot(directory)"  :class="root === directory ? $styles.threadItemActive : $styles.threadItemHover"  class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-[var(--ring)] focus-visible:-outline-offset-0.5 w-full text-left text-sm p-2 rounded break-all">{{directory}}</button>
            </div>
        </div>
        <div data-workspace-content class="p-2 [scrollbar-gutter:stable] flex-1 overflow-y-auto min-h-0 text-sm">
            <p v-if="loading && !data" role="status"  data-workspace-message class="p-2 text-xs opacity-80">Loading workspace…</p>
            <p v-if="error" role="alert"  data-workspace-message class="p-2 text-xs opacity-80">{{error}} <button type="button" @click="load()"   class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-[var(--ring)] focus-visible:-outline-offset-0.5 underline">Retry</button></p>
            <div v-else-if="!loading && !data?.roots.length"  data-workspace-empty class="py-6 px-3 text-center"><p class="font-medium">No workspace selected</p><p class="mt-1 text-xs opacity-70">Select a project to browse its files.</p></div>
            <WorkspaceTreeNode v-if="view === 'files' && rootEntry" :entry="rootEntry" :tree="tree" :selected-path="data?.path" :selected-file="route.query.workspaceFile" :is-root="true" @directory="clickDirectory" @retry="navigate" @file="selectFile" />
            <component v-else-if="icons[view]?.panel && data?.roots.length" :is="icons[view].panel" :key="view" :workspace="data" :project-id="projectId" :refresh-key="refreshKey" @file="selectFile" @busy="panelBusy = $event" />
        </div>
    </aside>`
}
