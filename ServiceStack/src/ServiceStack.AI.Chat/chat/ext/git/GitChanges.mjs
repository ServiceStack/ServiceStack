import { computed, inject, ref, watch, onMounted, onUnmounted } from 'vue'
import GitContextMenu from './GitContextMenu.mjs'

const drafts = new Map()
export default {
    components: { GitContextMenu },
    props: ['data', 'projectId', 'selectedFile', 'selectedPreview', 'refreshing', 'sync'],
    emits: ['file', 'changed', 'busy', 'sync'],
    setup(props, { emit }) {
        const ctx = inject('ctx'), working = ref(false), error = ref(''), notice = ref('')
        const generating = ref(false), editor = ref(null)
        const menu = ref(null)
        const busy = computed(() => working.value || props.refreshing || generating.value)
        const key = `${props.projectId || 'home'}:${props.data.repository}`
        const draft = drafts.get(key) || { message: '', name: props.data.identity?.name || '', email: props.data.identity?.email || '', save: false }
        drafts.set(key, draft)
        const message = ref(draft.message), name = ref(draft.name), email = ref(draft.email), save = ref(draft.save)
        const expanded = ref({stage: true, unstage: true})
        const needsIdentity = computed(() => !props.data.identity?.name || !props.data.identity?.email)
        const canCommit = computed(() => !busy.value && message.value.trim() && props.data.stagedChanges?.length &&
            (!needsIdentity.value || name.value.trim() && email.value.trim()))
        const canGenerate = computed(() => !busy.value && props.data.canGenerateCommit !== false && props.data.stagedChanges?.length)
        const repositoryActions = ['commit-default', 'commit-staged', 'commit-all', 'stage-all', 'unstage-all', 'discard-all', 'undo', 'stash', 'stash-untracked', 'stash-staged', 'stash-apply', 'stash-pop']
        let alive = true, requestId, requestFingerprint, observer, controller, editVersion = 0, editorWidth
        function resizeEditor() {
            const element = editor.value
            if (!element) return
            element.style.height = 'auto'
            const height = element.scrollHeight + element.offsetHeight - element.clientHeight
            element.style.height = Math.min(height, 240) + 'px'
            element.style.overflowY = height > 240 ? 'auto' : 'hidden'
        }
        watch(message, () => editVersion++, { flush: 'sync' })
        watch(message, resizeEditor, { flush: 'post' })
        onMounted(() => {
            observer = new ResizeObserver(entries => {
                const width = entries[0].contentRect.width
                if (width !== editorWidth) { editorWidth = width; resizeEditor() }
            })
            observer.observe(editor.value)
            resizeEditor()
        })
        onUnmounted(() => { alive = false; observer?.disconnect(); controller?.abort(); emit('busy', false) })
        watch([message, name, email, save], () => Object.assign(draft, { message: message.value, name: name.value, email: email.value, save: save.value }))
        const sections = computed(() => [
            { title: 'Staged changes', action: 'unstage', preview: 'git-staged', files: props.data.stagedChanges || [] },
            { title: 'Working changes', action: 'stage', preview: 'git', files: props.data.changes || [] },
        ])
        watch([busy, () => props.data], () => { menu.value = null })
        function openMenu(event, file, section) {
            if (section.action !== 'stage') return
            event.preventDefault()
            const origin = event.currentTarget.querySelector('[data-git-change-file]'), rect = origin.getBoundingClientRect()
            menu.value = { target: file, origin, label: 'Unstaged file actions',
                x: event.type === 'contextmenu' ? event.clientX : rect.left,
                y: event.type === 'contextmenu' ? event.clientY : rect.bottom,
                items: [{ action: 'discard', label: 'Discard Changes', danger: true, disabled: busy.value }] }
        }
        function contextKey(event, file, section) {
            if (event.key === 'ContextMenu' || event.key === 'F10' && event.shiftKey) openMenu(event, file, section)
        }
        async function discard({ target: file }) {
            if (busy.value) return
            const question = file.status === '?' ? `Delete untracked file "${file.relativePath}"? This cannot be undone.` :
                `Discard unstaged changes to "${file.relativePath}"? Staged changes will be kept. This cannot be undone.`
            if (window.confirm(question)) await run('discard', [file.relativePath], file)
        }
        function menuReason(action) {
            if (busy.value) return 'A Git operation is in progress'
            const staged = props.data.stagedChanges?.length, working = props.data.changes?.length
            if (action === 'undo') return props.data.canUndo ? '' : props.data.undoReason || 'There is no local commit to undo'
            if (['stash', 'stash-untracked', 'stash-staged', 'stash-apply', 'stash-pop'].includes(action) && !props.data.head) return 'Create a commit before using stashes'
            if (['stash-apply', 'stash-pop'].includes(action)) {
                if (!props.data.stashId) return 'There is no stash to apply'
                if (staged || props.data.changes?.some(file => file.status !== '?')) return 'Commit or stash your current changes before applying a stash'
                return ''
            }
            if (['commit-staged', 'unstage-all', 'stash-staged'].includes(action) && !staged) return 'There are no staged changes'
            if (['stage-all', 'discard-all'].includes(action) && !working) return 'There are no unstaged changes'
            if (action === 'stash' && !staged && !props.data.changes?.some(file => file.status !== '?')) return 'There are no tracked changes to stash'
            if (!staged && !working) return 'There are no changes'
            return ''
        }
        async function menuAction(action) {
            if (menuReason(action)) return
            if (['commit-default', 'commit-staged', 'commit-all'].includes(action)) {
                if (!message.value.trim()) { error.value = 'Enter a commit message first.'; editor.value?.focus(); return }
                if (needsIdentity.value && (!name.value.trim() || !email.value.trim())) { error.value = 'Enter your Git author name and email first.'; document.querySelector('#git-author-name')?.focus(); return }
                if (action === 'commit-default' && !props.data.stagedChanges?.length && !window.confirm('Stage and commit all working changes, including untracked files?')) return
                action = action === 'commit-staged' || action === 'commit-default' && props.data.stagedChanges?.length ? 'commit' : 'commit-all'
            }
            if (action === 'discard-all' && !window.confirm('Discard all unstaged changes and delete untracked files? Staged changes will be kept. This cannot be undone.')) return
            if (action === 'undo' && !window.confirm('Undo the last local commit? Its changes will be kept staged.')) return
            await run(action)
        }
        async function generate() {
            if (!canGenerate.value) return
            const version = editVersion, revision = props.data.indexRevision
            controller = new AbortController()
            generating.value = true; error.value = ''; notice.value = ''; emit('busy', true)
            try {
                const api = await ctx.postJson('/ext/git/repositories/message', { signal: controller.signal,
                    body: JSON.stringify({ path: props.data.path, projectId: props.projectId || null, indexRevision: revision }) })
                if (!alive) return
                if (!api.response) throw new Error(api.error?.message || 'Unable to generate a commit message')
                if (props.data.indexRevision !== revision || api.response.indexRevision !== revision)
                    throw new Error('Staged changes changed while generating. Refresh and generate again.')
                if (editVersion !== version) { notice.value = 'Your edits were kept. Generate again to replace them.'; return }
                message.value = api.response.message
                draft.message = api.response.message
                notice.value = 'Message generated from staged changes.'
                editor.value?.focus()
            } catch (e) { if (alive) error.value = e.message }
            finally { if (alive) { generating.value = false; emit('busy', false) } }
        }
        async function run(action, paths, file) {
            if (busy.value) return
            const body = { path: props.data.path, projectId: props.projectId || null, paths }
            const submitted = message.value
            if (repositoryActions.includes(action)) Object.assign(body, { indexRevision: props.data.indexRevision,
                workspaceRevision: props.data.workspaceRevision, head: props.data.head, branch: props.data.branch, stashId: props.data.stashId })
            if (action === 'discard') Object.assign(body, { indexRevision: props.data.indexRevision, worktreeRevision: file.worktreeRevision })
            if (['stash', 'stash-untracked', 'stash-staged'].includes(action)) body.identity = needsIdentity.value ? { name: name.value.trim(), email: email.value.trim() } : props.data.identity
            const committing = action === 'commit' || action === 'commit-all'
            if (committing) {
                if (action === 'commit' ? !canCommit.value : !message.value.trim() || !props.data.changes?.length && !props.data.stagedChanges?.length || needsIdentity.value && (!name.value.trim() || !email.value.trim())) return
                Object.assign(body, { message: submitted, indexRevision: props.data.indexRevision,
                    identity: needsIdentity.value ? { name: name.value.trim(), email: email.value.trim() } : props.data.identity,
                    saveIdentity: needsIdentity.value && save.value })
                const fingerprint = JSON.stringify(body)
                if (fingerprint !== requestFingerprint) { requestId = crypto.randomUUID(); requestFingerprint = fingerprint }
                body.requestId = requestId
            }
            working.value = true; error.value = ''; notice.value = ''; emit('busy', true)
            try {
                const api = await ctx.postJson('/ext/git/repositories/' + action, { body: JSON.stringify(body) })
                if (!api.response) throw new Error(api.error?.message || 'Unable to complete the Git operation')
                if (committing) {
                    if (draft.message === submitted) draft.message = ''
                    if (alive && message.value === submitted) message.value = ''
                    requestId = requestFingerprint = undefined
                }
                if (!alive) return
                if (action === 'undo' && !message.value.trim()) message.value = api.response.message || ''
                const notices = { 'stage-all': 'All changes staged.', 'unstage-all': 'All changes unstaged.',
                    'discard-all': 'All unstaged changes discarded.', undo: 'Last commit undone. Its changes are staged.',
                    stash: 'Tracked changes stashed.', 'stash-untracked': 'Changes and untracked files stashed.',
                    'stash-staged': 'Staged changes stashed.', 'stash-apply': 'Latest stash applied and kept.', 'stash-pop': 'Latest stash applied and removed.' }
                notice.value = committing ? `Committed ${api.response.revision.slice(0, 7)}` : action === 'discard' ? `Discarded changes to ${file.relativePath}` : notices[action] || ''
                emit('changed', action)
            } catch (e) { if (alive) { error.value = e.message; if (action === 'discard' || repositoryActions.includes(action)) emit('changed', action) } }
            finally { if (alive) { working.value = false; emit('busy', false) } }
        }
        return { message, name, email, save, busy, working, generating, editor, error, notice, needsIdentity, canCommit, canGenerate, generate, sections, expanded, run, menu, openMenu, contextKey, discard, menuAction, menuReason }
    },
    template: `<div data-git-changes>
        <form @submit.prevent="sync?.visible ? $emit('sync') : run('commit')"  data-git-commit-form class="pt-1 pr-2 pb-3 pl-2">
            <label for="git-commit-message" class="sr-only">Commit message</label>
            <div data-git-commit-editor class="relative">
                <textarea id="git-commit-message" ref="editor" v-model="message" rows="1" placeholder="Commit message" :disabled="working || refreshing" @keydown.ctrl.enter.prevent="run('commit')" @keydown.meta.enter.prevent="run('commit')"  data-git-commit-message class="w-full border border-[#e5e7eb] rounded-md pt-[7px] pr-9.5 pb-[7px] pl-2.5 bg-transparent text-inherit text-[13px] dark:border-[#374151] min-h-9 max-h-60 leading-5 resize-none overflow-y-hidden block focus:outline-2 focus:outline-solid focus:outline-blue-500 focus:outline-offset-1"></textarea>
                <button type="button" @click="generate" :disabled="!canGenerate" :aria-busy="generating" aria-label="Generate commit message" :title="generating ? 'Generating commit message…' : data.canGenerateCommit === false ? 'Commit message generation is disabled' : !data.stagedChanges?.length ? 'Stage changes to generate a commit message' : 'Generate commit message from staged changes'"  data-git-generate-button class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-blue-500 focus-visible:outline-offset-1 absolute top-1 right-1 w-7 h-7 flex items-center justify-center rounded text-slate-500 dark:text-slate-400 hover:enabled:text-blue-600 hover:enabled:bg-[rgb(127_127_127_/_.1)] dark:hover:enabled:text-blue-300 disabled:opacity-40 disabled:cursor-default">
                    <svg v-if="generating"  width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true" data-workspace-refreshing class="animate-spin motion-reduce:animate-none"><path d="M20 12a8 8 0 1 1-8-8"/></svg>
                    <svg v-else width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linejoin="round" aria-hidden="true"><path d="m12 3 2.3 6.7L21 12l-6.7 2.3L12 21l-2.3-6.7L3 12l6.7-2.3L12 3zM20 2v4m-2-2h4"/></svg>
                </button>
            </div>
            <p v-if="generating" role="status" class="text-xs opacity-60 mt-1">Summarizing staged changes…</p>
            <fieldset v-if="needsIdentity && !sync?.visible" :disabled="busy"  data-git-author class="grid gap-2 my-2.5 mx-0">
                <legend class="text-xs opacity-70">Git author for this commit</legend>
                <label class="sr-only" for="git-author-name">Author name</label><input id="git-author-name" v-model="name" placeholder="Your name" autocomplete="name"  :class="$styles.chromeBorder"  data-git-author-input class="w-full border border-[#e5e7eb] rounded-md py-2 px-2.5 bg-transparent text-inherit text-[13px] dark:border-[#374151] focus:outline-2 focus:outline-solid focus:outline-blue-500 focus:outline-offset-1"/>
                <label class="sr-only" for="git-author-email">Author email</label><input id="git-author-email" type="email" v-model="email" placeholder="Email address" autocomplete="email"  :class="$styles.chromeBorder"  data-git-author-input class="w-full border border-[#e5e7eb] rounded-md py-2 px-2.5 bg-transparent text-inherit text-[13px] dark:border-[#374151] focus:outline-2 focus:outline-solid focus:outline-blue-500 focus:outline-offset-1"/>
                <label class="flex items-center gap-2 text-xs opacity-80"><input type="checkbox" v-model="save" /> Remember for this repository</label>
            </fieldset>
            <button type="submit" :disabled="sync?.visible ? busy || !!sync.reason : !canCommit" :title="sync?.visible ? sync.title : undefined" :aria-busy="working || sync?.busy || false"  data-git-commit-button class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-blue-500 focus-visible:outline-offset-0.5 w-full flex items-center justify-center gap-1.5 mt-2 rounded-md bg-blue-600 text-[white] py-1.75 px-2.5 text-[13px] font-medium hover:enabled:bg-blue-700 disabled:opacity-45 disabled:cursor-default">
                <svg v-if="sync?.visible" xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 24 24" aria-hidden="true" :class="sync.busy ? 'animate-spin motion-reduce:animate-none' : ''"><path d="M0 0h24v24H0z" fill="none"/><path fill="currentColor" d="M18.43 4.25a.76.76 0 0 0-.75.75v2.43l-.84-.84a7.24 7.24 0 0 0-12 2.78a.74.74 0 0 0 .46 1a.7.7 0 0 0 .25 0a.76.76 0 0 0 .71-.51a5.6 5.6 0 0 1 1.37-2.2a5.76 5.76 0 0 1 8.13 0l.84.84h-2.41a.75.75 0 0 0 0 1.5h4.24a.74.74 0 0 0 .75-.75V5a.75.75 0 0 0-.75-.75m.25 9.43a.76.76 0 0 0-1 .47a5.6 5.6 0 0 1-1.37 2.2a5.76 5.76 0 0 1-8.13 0l-.84-.84h2.47a.75.75 0 0 0 0-1.5H5.57a.74.74 0 0 0-.75.75V19a.75.75 0 0 0 1.5 0v-2.43l.84.84a7.24 7.24 0 0 0 12-2.78a.74.74 0 0 0-.48-.95"/></svg>
                <svg v-else width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true"><path d="m5 12 4 4L19 6"/></svg>
                <template v-if="sync?.visible">{{sync.busy ? 'Syncing Changes…' : 'Sync Changes'}}<span v-if="sync.behind">{{sync.behind}}↓</span><span v-if="sync.ahead" class="inline-flex items-center gap-1" :aria-label="sync.ahead + ' outgoing commits'">{{sync.ahead}}<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 16 16" aria-hidden="true"><path d="M0 0h16v16H0z" fill="none"/><path fill="currentColor" fill-rule="evenodd" d="M8 15a.5.5 0 0 0 .5-.5V2.707l3.146 3.147a.5.5 0 0 0 .708-.708l-4-4a.5.5 0 0 0-.708 0l-4 4a.5.5 0 1 0 .708.708L7.5 2.707V14.5a.5.5 0 0 0 .5.5"/></svg></span></template>
                <template v-else>{{working ? 'Working…' : 'Commit' + (data.stagedChanges?.length ? ' (' + data.stagedChanges.length + ')' : '')}}</template>
            </button>
            <p v-if="!needsIdentity && !sync?.visible" class="text-xs opacity-60 mt-1 truncate" :title="data.identity.name + ' <' + data.identity.email + '>'">Commit as {{data.identity.name}}</p>
            <p v-if="!data.stagedChanges?.length && !sync?.visible" class="text-xs opacity-60 mt-1">Stage changes below to include them in a commit.</p>
            <p v-if="error" role="alert" class="text-xs text-red-600 dark:text-red-400 mt-2">{{error}}</p>
            <p v-if="notice" role="status" class="text-xs mt-2 text-green-700 dark:text-green-400">{{notice}}</p>
        </form>
        <section v-for="section in sections" :key="section.action" :aria-label="section.action === 'stage' ? 'Unstaged changes' : 'Staged changes'">
            <div data-git-change-heading class="flex items-center gap-1.5 py-1 px-2 mt-2 text-[13px] font-medium"><button type="button"  @click="expanded[section.action] = !expanded[section.action]" :aria-expanded="expanded[section.action]" data-git-change-toggle class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-[var(--ring)] focus-visible:-outline-offset-0.5 flex items-center gap-2 flex-1 min-w-0 text-left"><svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true" :style="{transform: expanded[section.action] ? '' : 'rotate(-90deg)'}"><path d="m5 9 7 7 7-7"/></svg>{{section.title}}<span data-git-change-count class="rounded-xl bg-[rgb(0_0_0_/_.07)] py-0 px-1.5 text-[11px] font-normal dark:bg-[rgb(255_255_255_/_.08)] ml-auto">{{section.files.length}}</span></button><button v-if="section.files.length" type="button" :disabled="busy" @click="run(section.action, section.files.map(file => file.relativePath))"  :aria-label="section.action === 'stage' ? 'Stage all changes' : 'Unstage all changes'" :title="section.action === 'stage' ? 'Stage all changes' : 'Unstage all changes'" data-git-change-action class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-blue-500 focus-visible:outline-offset-0.5 shrink-0 w-6 h-6 mr-1 rounded text-lg leading-[1] hover:enabled:bg-[rgb(127_127_127_/_.18)] disabled:opacity-35">{{section.action === 'stage' ? '+' : '−'}}</button></div>
            <div v-if="expanded[section.action]">
            <p v-if="!section.files.length" class="px-2 py-1 text-xs opacity-60">{{section.action === 'stage' ? 'No unstaged changes' : 'No staged changes'}}</p>
            <div v-for="change in section.files" :key="change.relativePath" @contextmenu="openMenu($event, change, section)" @keydown="contextKey($event, change, section)"  :class="selectedPreview === section.preview && selectedFile === change.path ? $styles.threadItemActive : $styles.threadItemHover" data-git-change-row class="flex items-center min-w-0 rounded">
                <button type="button" :aria-haspopup="section.action === 'stage' ? 'menu' : undefined" @click="$emit('file', { ...change, preview: section.preview })" :title="change.relativePath" :aria-pressed="selectedPreview === section.preview && selectedFile === change.path"  data-git-change-file class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-[var(--ring)] focus-visible:-outline-offset-0.5 flex flex-1 items-center gap-2 py-1 px-2 min-w-0 text-left text-[13px]">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" class="shrink-0 opacity-70" aria-hidden="true"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8zM14 2v6h6"/></svg>
                    <span class="truncate">{{change.name}}</span><span class="truncate flex-1 text-xs opacity-60">{{change.relativePath.includes('/') ? change.relativePath.slice(0, change.relativePath.lastIndexOf('/')) : ''}}</span><span class="shrink-0 text-xs font-mono" :class="change.status === 'D' ? 'text-red-500' : ['?', 'A'].includes(change.status) ? 'text-green-600 dark:text-green-400' : 'text-amber-600 dark:text-amber-400'">{{change.status === '?' ? 'U' : change.status}}</span>
                </button>
                <button type="button" :disabled="busy" @click="run(section.action, [change.relativePath])" :aria-label="(section.action === 'stage' ? 'Stage ' : 'Unstage ') + change.relativePath" :title="section.action === 'stage' ? 'Stage change' : 'Unstage change'"  data-git-change-action class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-blue-500 focus-visible:outline-offset-0.5 shrink-0 w-6 h-6 mr-1 rounded text-lg leading-[1] hover:enabled:bg-[rgb(127_127_127_/_.18)] disabled:opacity-35">{{section.action === 'stage' ? '+' : '−'}}</button>
            </div>
            </div>
        </section>
        <GitContextMenu :menu="menu" @close="menu = null" @action="discard" />
    </div>`
}
