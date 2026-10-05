import { computed, ref, reactive, inject, watch, nextTick, onMounted, onUnmounted } from 'vue'
import GitChanges from './GitChanges.mjs'
import GitSync from './GitSync.mjs'
import GitContextMenu from './GitContextMenu.mjs'

export default {
    components: { GitChanges, GitSync, GitContextMenu },
    props: ['data', 'selectedFile', 'selectedCommit', 'selectedPreview', 'projectId', 'refreshKey', 'refreshing'],
    emits: ['file', 'changed', 'busy'],
    setup(props, { emit }) {
        const ctx = inject('ctx'), expanded = ref({}), files = reactive({})
        const changesBusy = ref(false), syncBusy = ref(false)
        const syncControl = ref(null), changesControl = ref(null)
        const primarySync = computed(() => syncControl.value?.primary)
        const menu = ref(null), actionError = ref(''), actionNotice = ref('')
        function setBusy(kind, value) {
            (kind === 'sync' ? syncBusy : changesBusy).value = value
            emit('busy', changesBusy.value || syncBusy.value)
        }
        const commitKey = commit => commit.id || commit.hash
        const githubRoot = computed(() => (syncControl.value?.remote || props.data.remotes?.find(remote => remote.name === props.data.remote))?.githubUrl)
        function openRepositoryMenu(event) {
            if (menu.value?.target?.repository) { menu.value = null; return }
            hidePreview()
            const origin = event.currentTarget, rect = origin.getBoundingClientRect()
            const item = (action, label, danger = false) => {
                const reason = changesControl.value?.menuReason(action) || (!props.data.canCommit ? 'Repository changes are unavailable here' : '')
                return { action, label, danger, disabled: !!reason, title: reason }
            }
            menu.value = { target: { repository: true }, origin, label: 'Repository actions', x: rect.right - 230, y: rect.bottom,
                items: [
                    ...['pull', 'push'].map(action => ({ action, label: action === 'pull' ? 'Pull' : 'Push',
                        disabled: !!syncControl.value?.reason(action), title: syncControl.value?.title(action) })),
                    { action: 'commit-menu', label: 'Commit', separator: true, children: [item('commit-default', 'Commit'), item('commit-staged', 'Commit Staged'), item('commit-all', 'Commit All'), item('undo', 'Undo Last Commit')] },
                    { action: 'changes-menu', label: 'Changes', children: [item('stage-all', 'Stage All Changes'), item('unstage-all', 'Unstage All Changes'), item('discard-all', 'Discard All Changes', true)] },
                    { action: 'stash-menu', label: 'Stash', children: [item('stash', 'Stash'), item('stash-untracked', 'Stash (Include Untracked)'), item('stash-staged', 'Stash Staged'), item('stash-apply', 'Apply Latest Stash'), item('stash-pop', 'Pop Latest Stash')] },
                ] }
        }
        function openCommitMenu(event, commit) {
            event.preventDefault(); hidePreview(); actionError.value = ''; actionNotice.value = ''
            const rect = event.currentTarget.getBoundingClientRect()
            menu.value = { target: commit, origin: event.currentTarget, label: 'Commit actions',
                x: event.type === 'contextmenu' ? event.clientX : rect.left,
                y: event.type === 'contextmenu' ? event.clientY : rect.bottom,
                items: [
                    { action: 'github', label: 'Open on GitHub', disabled: !githubRoot.value,
                        title: githubRoot.value ? 'Open this commit on GitHub' : 'The selected remote is not a GitHub repository' },
                    { action: 'hash', label: 'Copy Commit Hash', separator: true },
                    { action: 'message', label: 'Copy Commit Message' },
                ] }
        }
        function contextKey(event, commit) {
            if (event.key === 'ContextMenu' || event.key === 'F10' && event.shiftKey) openCommitMenu(event, commit)
        }
        async function commitAction({ action, target: commit }) {
            if (commit?.repository) {
                if (action === 'pull' || action === 'push') await syncControl.value?.sync(action)
                else await changesControl.value?.menuAction(action)
                return
            }
            const current = generation
            if (action === 'github') {
                if (githubRoot.value && /^[a-f0-9]{7,64}$/i.test(commitKey(commit)))
                    window.open(githubRoot.value + '/commit/' + commitKey(commit), '_blank', 'noopener,noreferrer')
                return
            }
            try {
                await navigator.clipboard.writeText(action === 'hash' ? commitKey(commit) :
                    commit.message ?? (commit.subject + (commit.body ? '\n\n' + commit.body : '')))
                if (current === generation) actionNotice.value = action === 'hash' ? 'Commit hash copied.' : 'Commit message copied.'
            } catch { if (current === generation) actionError.value = 'Unable to copy. Check clipboard permissions and try again.' }
        }
        watch([() => props.data, () => props.refreshing, () => syncControl.value?.selected], () => { menu.value = null })
        let generation = 0
        async function loadFiles(commit) {
            const key = commitKey(commit)
            if (files[key]?.loading || files[key]?.changes) return
            const current = generation
            files[key] = { loading: true, error: '' }
            const entry = files[key]
            const query = new URLSearchParams({ path: props.data.path, commit: key })
            if (props.projectId) query.set('projectId', props.projectId)
            try {
                const api = await ctx.getJson('/ext/git/commit?' + query)
                if (current !== generation || files[key] !== entry) return
                if (!api.response) throw new Error(api.error?.message || 'Unable to load commit files')
                entry.changes = api.response.changes
                entry.message = api.response.message
            } catch (e) { if (current === generation) entry.error = e.message }
            finally { if (current === generation) entry.loading = false }
        }
        function toggleCommit(commit) {
            hidePreview()
            const key = commitKey(commit)
            expanded.value[key] = !expanded.value[key]
            if (expanded.value[key]) loadFiles(commit)
        }
        function restoreSelection() {
            const commit = props.data.commits?.find(commit => commitKey(commit) === props.selectedCommit)
            if (commit) { expanded.value[commitKey(commit)] = true; loadFiles(commit) }
        }
        function invalidate() {
            generation++
            for (const key of Object.keys(files)) delete files[key]
        }
        watch([() => props.data.repository, () => props.projectId], () => { invalidate(); expanded.value = {}; menu.value = null; actionError.value = ''; actionNotice.value = ''; restoreSelection() })
        watch(() => props.refreshKey, () => {
            invalidate()
            for (const commit of props.data.commits || []) if (expanded.value[commitKey(commit)]) loadFiles(commit)
        })
        watch(() => [props.selectedCommit, props.data.commits], restoreSelection, { immediate: true })
        const changesOpen = ref(true), historyOpen = ref(true)
        const preview = ref(null), previewElement = ref(null), previewPosition = ref({})
        async function showPreview(event, commit) {
            const rect = event.currentTarget.getBoundingClientRect()
            preview.value = commit
            const width = Math.min(288, window.innerWidth - 16)
            previewPosition.value = { left: Math.max(8, rect.left - width - 10) + 'px', top: rect.top + 'px' }
            await nextTick()
            if (preview.value !== commit || !previewElement.value) return
            previewPosition.value.top = Math.max(8, Math.min(rect.top, window.innerHeight - previewElement.value.offsetHeight - 8)) + 'px'
        }
        function hidePreview() { preview.value = null }
        function onFocusOut(event) { if (!event.currentTarget.contains(event.relatedTarget)) hidePreview() }
        function statusLabel(status) { return ({'?':'Untracked', M:'Modified', D:'Deleted', A:'Added', R:'Renamed', C:'Copied', U:'Conflict', T:'Type changed'})[status] || status }
        onMounted(() => { window.addEventListener('resize', hidePreview); window.addEventListener('scroll', hidePreview, true) })
        onUnmounted(() => { generation++; window.removeEventListener('resize', hidePreview); window.removeEventListener('scroll', hidePreview, true) })
        return { changesBusy, syncBusy, syncControl, changesControl, primarySync, setBusy, expanded, files, commitKey, toggleCommit, loadFiles, changesOpen, historyOpen, preview, previewElement, previewPosition, showPreview, hidePreview, onFocusOut, statusLabel, menu, actionError, actionNotice, openCommitMenu, openRepositoryMenu, contextKey, commitAction }
    },
    template: `<div aria-label="Git explorer">
        <div class="px-2 py-2 mb-2 border-b" :class="$styles.chromeBorder">
            <div class="flex items-center gap-2 text-sm font-medium"><span class="truncate" :title="data.repository">{{data.repository.split(/[\\/]/).pop()}}</span><span class="ml-auto truncate rounded px-1.5 text-xs bg-black/10 dark:bg-white/10"  :title="data.branch">{{data.branch}}</span><button type="button" aria-label="Git repository actions" title="Git repository actions" aria-haspopup="menu" :aria-expanded="!!menu?.target?.repository" :disabled="refreshing || changesBusy || syncBusy" @click="openRepositoryMenu"  :class="[$styles.icon, $styles.iconHover]" data-workspace-icon class="flex items-center justify-center w-7 h-7 p-1 border border-transparent rounded shrink-0 cursor-pointer disabled:opacity-45 disabled:cursor-default focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-[var(--ring)] focus-visible:-outline-offset-0.5"><svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true"  class="w-4.5 h-4.5"><circle cx="3" cy="8" r="1.25"/><circle cx="8" cy="8" r="1.25"/><circle cx="13" cy="8" r="1.25"/></svg></button></div>
            <GitSync ref="syncControl" :key="(projectId || 'home') + ':' + data.repository" :data="data" :project-id="projectId" :refreshing="refreshing || changesBusy" @busy="setBusy('sync', $event)" @changed="$emit('changed', $event)" />
        </div>
        <GitChanges ref="changesControl" v-if="data.canCommit" :key="(projectId || 'home') + ':' + data.repository" :data="data" :project-id="projectId" :refreshing="refreshing || syncBusy" :sync="primarySync" :selected-file="selectedFile" :selected-preview="selectedCommit ? null : selectedPreview" @sync="syncControl?.sync('sync')" @file="$emit('file', $event)" @changed="$emit('changed', $event)" @busy="setBusy('changes', $event)" />
        <section v-else aria-label="Unstaged changes">
            <button type="button" @click="changesOpen = !changesOpen" :aria-expanded="changesOpen"  :class="$styles.threadItemHover"  class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-[var(--ring)] focus-visible:-outline-offset-0.5 w-full flex items-center gap-2 px-2 py-1 rounded text-left text-sm"><svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true" :style="{transform: changesOpen ? '' : 'rotate(-90deg)'}"><path d="m5 9 7 7 7-7"/></svg><span class="flex-1">Working changes</span><span class="rounded-full px-1.5 text-xs bg-black/10 dark:bg-white/10">{{data.changes?.length || 0}}</span></button>
            <div v-if="changesOpen">
            <p v-if="!data.changes?.length" class="px-2 py-1 text-xs opacity-70">No unstaged changes</p>
            <button v-for="change in data.changes" :key="change.relativePath" type="button" @click="$emit('file', change)" :aria-pressed="!selectedCommit && selectedFile === change.path" :title="change.relativePath + ' · ' + statusLabel(change.status)"  :class="!selectedCommit && selectedFile === change.path ? $styles.threadItemActive : $styles.threadItemHover"  class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-[var(--ring)] focus-visible:-outline-offset-0.5 w-full min-w-0 flex items-center gap-2 px-2 py-1 rounded text-left text-sm">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" class="shrink-0 opacity-70" aria-hidden="true"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8zM14 2v6h6"/></svg><span class="truncate">{{change.name}}</span><span class="truncate flex-1 text-xs opacity-60">{{change.relativePath.includes('/') ? change.relativePath.slice(0, change.relativePath.lastIndexOf('/')) : ''}}</span>
                <span class="shrink-0 text-xs font-mono" :class="change.status === 'D' ? 'text-red-500' : change.status === '?' ? 'text-green-600 dark:text-green-400' : 'text-amber-600 dark:text-amber-400'" :aria-label="statusLabel(change.status)">{{change.status === '?' ? 'U' : change.status}}</span>
            </button>
            </div>
        </section>
        <section aria-label="Commit history" class="mt-3">
            <p v-if="actionError" role="alert" class="px-2 text-xs text-red-600 dark:text-red-400">{{actionError}}</p>
            <p v-if="actionNotice" role="status" class="px-2 text-xs opacity-70">{{actionNotice}}</p>
            <button type="button" @click="historyOpen = !historyOpen" :aria-expanded="historyOpen"  :class="$styles.threadItemHover"  class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-[var(--ring)] focus-visible:-outline-offset-0.5 w-full flex items-center gap-2 px-2 py-1 rounded text-left text-sm"><svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true" :style="{transform: historyOpen ? '' : 'rotate(-90deg)'}"><path d="m5 9 7 7 7-7"/></svg>Recent commits</button>
            <div v-if="historyOpen">
            <p v-if="!data.commits?.length" class="px-2 py-1 text-xs opacity-70">No commits yet</p>
            <div v-for="commit in data.commits" :key="commitKey(commit)">
                <button type="button" aria-haspopup="menu" @click="toggleCommit(commit)" @contextmenu="openCommitMenu($event, commit)" @keydown="contextKey($event, commit)" @mouseenter="showPreview($event, commit)" @mouseleave="hidePreview" @focusin="showPreview($event, commit)" @focusout="onFocusOut" @keydown.esc.stop="hidePreview" @keydown.right.prevent="expanded[commitKey(commit)] || toggleCommit(commit)" @keydown.left.prevent="expanded[commitKey(commit)] && toggleCommit(commit)"  :class="$styles.threadItemHover" :aria-expanded="!!expanded[commitKey(commit)]" :aria-label="commit.subject + ' by ' + commit.author" :aria-describedby="preview === commit ? 'git-commit-preview' : undefined" data-workspace-commit class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-[var(--ring)] focus-visible:-outline-offset-0.5 flex items-center gap-2 w-full min-w-0 h-7 py-0 px-2 text-left cursor-pointer rounded">
                    <svg viewBox="0 0 12 28"  aria-hidden="true" data-workspace-commit-line class="w-3 h-7 shrink-0 text-[var(--color-blue-500)]"><path d="M6 0v10m0 8v10" stroke="currentColor" stroke-width="1.5"/><circle cx="6" cy="14" r="3" :fill="expanded[commitKey(commit)] ? 'none' : 'currentColor'" stroke="currentColor" stroke-width="1.5"/></svg>
                    <span class="truncate flex-1 text-sm">{{commit.subject}}</span>
                    <span v-if="commit.refs"  :title="commit.refs" data-workspace-commit-ref class="max-w-[35%] shrink-0 overflow-hidden [text-overflow:ellipsis] whitespace-nowrap rounded-lg py-0 px-1.5 text-[11px] leading-4.5 text-[var(--color-blue-600)] bg-[color-mix(in_srgb,_var(--color-blue-500)_15%,_transparent)] dark:text-[var(--color-blue-300)]">{{commit.refs.replace('HEAD -> ', '')}}</span>
                </button>
                <div v-if="expanded[commitKey(commit)]"  :aria-label="'Files changed in ' + commit.hash" :aria-busy="files[commitKey(commit)]?.loading" data-workspace-commit-files class="relative ml-3.5 pl-3 before:content-[''] before:absolute before:left-0 before:top-0 before:bottom-0 before:w-[1.5px] before:-translate-x-1/2 before:bg-[var(--color-blue-500)] before:pointer-events-none">
                    <p v-if="files[commitKey(commit)]?.loading" role="status" class="px-2 py-1 text-xs opacity-70">Loading changed files…</p>
                    <p v-else-if="files[commitKey(commit)]?.error" role="alert" class="px-2 py-1 text-xs">{{files[commitKey(commit)].error}} <button type="button" @click="loadFiles(commit)"   class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-[var(--ring)] focus-visible:-outline-offset-0.5 underline">Retry</button></p>
                    <template v-else>
                        <button v-for="change in files[commitKey(commit)]?.changes" :key="change.relativePath" type="button" @click="$emit('file', { ...change, commit: commitKey(commit) })" :aria-pressed="selectedCommit === commitKey(commit) && selectedFile === change.path" :title="change.relativePath + ' · ' + statusLabel(change.status)"  :class="selectedCommit === commitKey(commit) && selectedFile === change.path ? $styles.threadItemActive : $styles.threadItemHover" data-workspace-commit-file class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-[var(--ring)] focus-visible:-outline-offset-0.5 flex items-center gap-2 w-full min-w-0 h-7 py-0 px-2 text-left cursor-pointer rounded">
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" class="shrink-0 opacity-70" aria-hidden="true"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8zM14 2v6h6"/></svg>
                            <span class="truncate">{{change.name}}</span><span class="truncate flex-1 text-xs opacity-60">{{change.relativePath.includes('/') ? change.relativePath.slice(0, change.relativePath.lastIndexOf('/')) : ''}}</span>
                            <span class="shrink-0 text-xs font-mono" :class="change.status === 'D' ? 'text-red-500' : change.status === 'A' ? 'text-green-600 dark:text-green-400' : 'text-amber-600 dark:text-amber-400'" :aria-label="statusLabel(change.status)">{{change.status}}</span>
                        </button>
                        <p v-if="!files[commitKey(commit)]?.changes?.length" class="px-2 py-1 text-xs opacity-70">No changed files</p>
                        <p v-if="files[commitKey(commit)]?.message" class="px-2 py-1 text-xs opacity-70">{{files[commitKey(commit)].message}}</p>
                    </template>
                </div>
            </div>
            </div>
        </section>
        <GitContextMenu :menu="menu" @close="menu = null" @action="commitAction" />
        <Teleport to="body">
            <div v-if="preview" ref="previewElement" id="git-commit-preview" role="tooltip"  :style="previewPosition"  class="max-h-[calc(100vh-16px)] overflow-hidden fixed z-[300] pointer-events-none w-72 max-w-[calc(100vw-16px)] rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 shadow-xl p-3">
                <div class="text-sm font-semibold break-words">{{preview.subject}}</div>
                <div class="mt-2 text-xs text-gray-500 dark:text-gray-400">{{preview.author}}<span v-if="preview.email"> &lt;{{preview.email}}&gt;</span></div>
                <time class="mt-1 block text-xs text-gray-500 dark:text-gray-400">{{new Date(preview.date).toLocaleString()}}</time>
                <div class="mt-1 text-xs font-mono break-all opacity-70">{{preview.id || preview.hash}}</div>
                <div v-if="preview.refs" class="mt-1 text-xs text-blue-600 dark:text-blue-400">{{preview.refs}}</div>
                <div v-if="preview.body" class="mt-2 text-xs whitespace-pre-wrap break-words">{{preview.body}}</div>
            </div>
        </Teleport>
    </div>`
}
