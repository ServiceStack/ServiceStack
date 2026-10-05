import { computed, inject, nextTick, onMounted, onUnmounted, reactive, ref, watch } from 'vue'
import { CheckBox } from '../../ui/components/CheckBox.mjs'

const activeStates = new Set(['queued', 'running', 'finalizing'])
// Shared field and action styles. TextInput.css owns the border and single focus indicator.
const inputClass = 'block w-full border rounded-lg py-[9px] px-3 text-sm outline-none'
const buttonClass = 'py-[9px] px-4 rounded-lg text-[13px] font-semibold transition-colors disabled:opacity-45 disabled:cursor-not-allowed'
const focusOutline = 'focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-blue-500 focus-visible:outline-offset-3'

export default {
    components: { CheckBox },
    emits: ['done', 'created'],
    setup(props, { emit }) {
        const ctx = inject('ctx'), api = ctx.scope('projects')
        const request = ctx.projectCreationRequest
        const account = ctx.ai?.auth?.userName || 'default'
        if (ctx.projectCreationSession?.account !== account) ctx.projectCreationSession = null
        const origin = ctx.chat?.drafts?.state.key
        const options = ref(null), error = ref(''), loading = ref(true), submitting = ref(false)
        const urlInput = ref(null), nameInput = ref(null), advanced = ref(false)
        const session = ctx.projectCreationSession ||= reactive({
            account,
            kind: 'new', url: '', branch: '', name: '', folder: '', description: '', publish: '',
            showInSidebar: true, initializeGit: true, nameEdited: false, folderEdited: false,
            requestId: null, operation: null,
        })
        const busy = computed(() => submitting.value || activeStates.has(session.operation?.state))
        const ready = computed(() => session.operation?.state === 'succeeded')
        const canCreate = computed(() => !loading.value && !busy.value && session.name.trim()
            && session.folder.trim() && (session.kind !== 'clone' || session.url.trim()))
        const destination = computed(() => (options.value?.root || '') + '/' + (session.folder || 'project-name'))
        const phase = computed(() => session.operation?.state === 'finalizing' ? 2
            : session.operation?.state === 'succeeded' ? 3 : session.operation?.state === 'queued' ? 0 : 1)
        let mounted = true, controller, applying = false

        function source(kind) {
            session.kind = kind; error.value = ''
            nextTick(() => (kind === 'clone' ? urlInput.value : nameInput.value)?.focus())
        }
        function deriveFolder() {
            if (!session.folderEdited) session.folder = ctx.utils.toKebabCase(session.name)
        }
        function deriveRepository() {
            const match = session.url.trim().match(/(?:\/|:)([^/]+?)\/?$/)
            const name = match?.[1]?.replace(/\.git$/i, '') || ''
            if (!session.nameEdited && name && !/[?#]/.test(name)) session.name = name
            deriveFolder()
        }
        function fresh() {
            Object.assign(session, { kind: 'new', url: '', branch: '', name: '', folder: '',
                description: '', publish: '', showInSidebar: true, initializeGit: true,
                nameEdited: false, folderEdited: false, requestId: null, operation: null })
            error.value = ''; advanced.value = false
            nextTick(() => nameInput.value?.focus())
        }
        function editDetails() {
            session.operation = null; session.requestId = null; error.value = ''
            nextTick(() => (session.kind === 'clone' ? urlInput.value : nameInput.value)?.focus())
        }
        function adopt(operation) {
            session.operation = operation
            Object.assign(session, operation.project, { kind: operation.source.kind,
                url: operation.source.url || '', branch: operation.source.branch || '',
                initializeGit: operation.source.initializeGit ?? true })
        }
        async function complete(auto = false) {
            if (applying || !ready.value) return
            applying = true
            const { project } = session.operation.result
            try {
                const refreshed = await api.getJson('/projects.json')
                if (refreshed.response) {
                    ctx.setState({ projects: refreshed.response })
                    emit('created', refreshed.response)
                    if (!refreshed.response.some(p => p.id === project.id)) {
                        error.value = 'This project was removed. Create a new project to continue.'
                        return
                    }
                }
                // Never move the chat selected after this asynchronous operation began.
                if (auto && (!mounted || ctx.chat?.drafts?.state.key !== origin
                    || ctx.projectCreationRequest !== request)) return
                if (auto && request?.onCreated) await request.onCreated(project)
                else ctx.projects.openDraft(project.id)
                ctx.toast(`Created ${project.name}`)
                fresh()
                emit('done')
            } catch (e) { error.value = e.message || 'Your project is ready. Open it from the project list.' }
            finally { applying = false }
        }
        async function follow(auto) {
            controller?.abort()
            const current = new AbortController()
            controller = current
            while (mounted && activeStates.has(session.operation?.state)) {
                const operation = session.operation
                const result = await api.getJson(`/creation/operations/${operation.id}?revision=${operation.revision}`,
                    { signal: current.signal })
                if (!mounted || current.signal.aborted || current !== controller) return
                if (!result.response) {
                    error.value = result.error?.message || 'Connection lost. Reconnect to check progress.'
                    return
                }
                if (result.response.revision >= session.operation.revision) session.operation = result.response
            }
            if (ready.value) {
                if (auto) await complete(true)
                else {
                    const refreshed = await api.getJson('/projects.json')
                    if (refreshed.response) { ctx.setState({ projects: refreshed.response }); emit('created', refreshed.response) }
                }
            }
        }
        async function create() {
            if (!canCreate.value) return
            error.value = ''; submitting.value = true
            session.requestId ||= crypto.randomUUID()
            try {
                const payload = {
                    requestId: session.requestId,
                    project: { name: session.name.trim(), folder: session.folder.trim(),
                        description: session.description.trim(), publish: session.publish.trim(),
                        showInSidebar: session.showInSidebar },
                    source: session.kind === 'clone'
                        ? { kind: 'clone', url: session.url.trim(), branch: session.branch.trim() || null }
                        : { kind: 'new', initializeGit: !!options.value?.initializeGit && session.initializeGit },
                }
                // Shared UI can be served by hosts that have not implemented the
                // creation endpoint yet; keep their existing plain-project workflow.
                const result = options.value?.create
                    ? await api.postJson('/create', payload)
                    : await ctx.projects.saveProject(payload.project.name, payload.project)
                if (!result.response) { error.value = result.error?.message || 'Unable to create project.'; return }
                if (!options.value?.create) {
                    const project = result.response.find(p => p.name === payload.project.name)
                    session.operation = { state: 'succeeded', result: { project, projects: result.response } }
                    await complete(true)
                    return
                }
                session.operation = result.response
                follow(true).catch(e => { if (mounted && e.name !== 'AbortError') error.value = 'Connection lost. Check progress to reconnect.' })
            } finally { submitting.value = false }
        }
        async function cancel() {
            const result = await api.postJson(`/creation/operations/${session.operation.id}/cancel`, {})
            if (result.response) session.operation = result.response
            else error.value = result.error?.message || 'Unable to cancel. Check progress.'
        }
        async function retry() {
            error.value = ''; submitting.value = true
            try {
                const result = await api.postJson(`/creation/operations/${session.operation.id}/retry`, {})
                if (!result.response) { error.value = result.error?.message || 'Unable to retry.'; return }
                session.operation = result.response
                follow(true).catch(() => { if (mounted) error.value = 'Connection lost. Check progress to reconnect.' })
            } finally { submitting.value = false }
        }
        async function reconnect() {
            error.value = ''
            await follow(false)
        }
        onMounted(async () => {
            try {
                const result = await api.getJson('/creation/options')
                options.value = result.response || null
                if (!options.value?.create && !session.operation) session.kind = 'new'
                if (!session.operation) {
                    const pending = result.response?.operations.find(op => activeStates.has(op.state)
                        || op.state === 'interrupted')
                    if (pending) adopt(pending)
                }
                if (session.operation) await follow(false)
            } catch { error.value = 'Unable to load project creation options.' }
            finally { loading.value = false; await nextTick(); nameInput.value?.focus() }
        })
        onUnmounted(() => { mounted = false; controller?.abort() })
        watch(() => ctx.ai?.auth?.userName || 'default', value => {
            if (value !== account) { controller?.abort(); ctx.projectCreationSession = null; emit('done') }
        })
        // A changed form is a new request; retries after a lost response retain the
        // original ID until the user edits it, avoiding accidental duplicate projects.
        function edited() { if (!session.operation) session.requestId = null }
        return { session, options, error, loading, submitting, busy, ready, canCreate, destination,
            phase, urlInput, nameInput, advanced, source, deriveRepository, deriveFolder,
            create, cancel, retry, fresh, editDetails, complete, reconnect, edited,
            inputClass, buttonClass, focusOutline }
    },
    template: `<div @input="edited" data-project-create class="max-w-140 w-full my-0 mx-auto flex flex-col h-full min-h-0">
        <div class="mb-6"><h3 class="text-lg font-semibold tracking-tight">Create a project</h3>
            <p class="text-sm mt-1" :class="$styles.muted">A workspace for your files, ideas and conversations.</p></div>
        <div v-if="session.operation" class="flex-1 flex flex-col">
            <div data-project-create-status class="border border-[var(--border)] rounded-[14px] py-7 px-6 bg-[rgb(128_128_128_/_3%)]">
                <svg v-if="ready" class="size-8 text-green-600 dark:text-green-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><circle cx="12" cy="12" r="9"/><path d="m8 12 3 3 5-6"/></svg>
                <svg v-else class="size-8" :class="busy ? 'animate-pulse text-blue-500' : $styles.muted" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true"><path d="M3 7V5h6l2 2h10v12H3Z"/><path d="M9 13h6m-3-3v6"/></svg>
                <h4 class="font-semibold mt-4 break-all">{{session.name}}</h4>
                <p role="status" aria-live="polite" class="text-sm mt-1" :class="$styles.muted">{{session.operation.message}}</p>
                <div v-if="busy || ready" aria-label="Project creation steps" data-project-create-steps class="flex gap-5 mt-6 text-[11px] text-gray-400">
                    <span :class="{ 'text-blue-500': phase >= 0 }">Prepare</span><span :class="{ 'text-blue-500': phase >= 1 }">{{session.kind === 'clone' ? 'Clone' : 'Create'}}</span><span :class="{ 'text-blue-500': phase >= 2 }">Open</span>
                </div>
                <div v-if="busy" role="progressbar" aria-label="Creation progress" aria-valuemin="0" aria-valuemax="100" :aria-valuenow="session.operation.percent ?? undefined" data-project-create-progress class="h-[3px] rounded-[3px] mt-3 overflow-hidden bg-[rgb(128_128_128_/_12%)]"><span :class="{ 'w-full animate-pulse motion-reduce:animate-none opacity-50': session.operation.percent == null }" :style="session.operation.percent == null ? null : { width: session.operation.percent + '%' }" class="block h-full bg-blue-500 rounded-[3px] transition-[width]"></span></div>
                <p v-if="session.operation.error" role="alert" class="text-sm mt-4 text-red-600 dark:text-red-400">{{session.operation.error}}</p>
                <p v-if="busy" class="text-xs mt-4" :class="$styles.muted">You can close this window. Creation will continue.</p>
            </div>
            <p class="text-xs font-mono break-all mt-3" :class="$styles.muted">{{destination}}</p>
            <p v-if="error" role="alert" class="text-sm mt-4 text-red-600 dark:text-red-400">{{error}}</p>
            <div class="mt-auto pt-6 flex flex-wrap justify-end gap-3">
                <button v-if="error && busy" type="button" @click="reconnect" :class="[$styles.secondaryButton, buttonClass, focusOutline]" data-project-create-button>Check progress</button>
                <button v-if="session.operation.cancellable" type="button" @click="cancel" :class="[$styles.secondaryButton, buttonClass, focusOutline]" data-project-create-button>Cancel creation</button>
                <button v-if="!busy && !ready" type="button" @click="editDetails" :class="[$styles.secondaryButton, buttonClass, focusOutline]" data-project-create-button>Edit details</button>
                <button v-if="!busy && !ready" type="button" @click="retry" :disabled="submitting" :class="[$styles.primaryButton, buttonClass]" data-project-create-button>Retry</button>
                <button v-if="ready" type="button" @click="complete()" :class="[$styles.primaryButton, buttonClass]" data-project-create-button>Open project</button>
            </div>
        </div>
        <form v-else @submit.prevent="create" class="flex-1 flex flex-col gap-5">
            <div v-if="options?.clone" role="group" aria-label="Project source" data-project-create-sources class="grid grid-cols-2 gap-2.5 max-sm:grid-cols-1">
                <button type="button" @click="source('new')" :aria-pressed="session.kind === 'new'" data-project-create-source class="flex items-start gap-2.5 py-3.5 px-3 border border-[var(--border)] rounded-xl text-left transition-colors hover:bg-[rgb(128_128_128_/_5%)] aria-pressed:border-blue-500 aria-pressed:bg-[rgb(59_130_246_/_5%)] focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-blue-500 focus-visible:outline-offset-3 group">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true" class="w-5 h-5 shrink-0 mt-0.5 opacity-75 group-aria-pressed:text-blue-500 group-aria-pressed:opacity-100"><path d="M3 7V5h6l2 2h10v12H3Z"/><path d="M9 13h6m-3-3v6"/></svg><span><strong class="block text-[13px] font-semibold">New project</strong><small class="block text-[11px] mt-1 opacity-60">Start with an empty folder</small></span></button>
                <button type="button" @click="source('clone')" :aria-pressed="session.kind === 'clone'" data-project-create-source class="flex items-start gap-2.5 py-3.5 px-3 border border-[var(--border)] rounded-xl text-left transition-colors hover:bg-[rgb(128_128_128_/_5%)] aria-pressed:border-blue-500 aria-pressed:bg-[rgb(59_130_246_/_5%)] focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-blue-500 focus-visible:outline-offset-3 group">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true" class="w-5 h-5 shrink-0 mt-0.5 opacity-75 group-aria-pressed:text-blue-500 group-aria-pressed:opacity-100"><circle cx="6" cy="5" r="2"/><circle cx="6" cy="19" r="2"/><circle cx="18" cy="5" r="2"/><path d="M6 7v10m12-10c0 6-12 3-12 8"/></svg><span><strong class="block text-[13px] font-semibold">Clone repository</strong><small class="block text-[11px] mt-1 opacity-60">Bring an existing project</small></span></button>
            </div>
            <div v-if="session.kind === 'clone'">
                <label for="project-repository" data-project-create-label class="block text-[13px] font-medium mb-[7px]">Repository URL</label>
                <input id="project-repository" ref="urlInput" v-model="session.url" @input="deriveRepository" type="text" required autocomplete="off" spellcheck="false" placeholder="https://github.com/owner/repository" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput, inputClass, 'font-mono']" data-project-create-input/>
                <p class="text-xs mt-2" :class="$styles.muted">GitHub, GitLab or another Git host. Your history comes with it.</p>
            </div>
            <div><label for="project-create-name" data-project-create-label class="block text-[13px] font-medium mb-[7px]">Project name</label>
                <input id="project-create-name" ref="nameInput" v-model="session.name" @input="session.nameEdited = true; deriveFolder()" type="text" maxlength="200" required placeholder="My project" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput, inputClass]" data-project-create-input/></div>
            <div><label for="project-create-folder" data-project-create-label class="block text-[13px] font-medium mb-[7px]">Folder name</label>
                <input id="project-create-folder" v-model="session.folder" @input="session.folderEdited = true" type="text" maxlength="150" required spellcheck="false" placeholder="my-project" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput, inputClass, 'font-mono']" data-project-create-input/>
                <p v-if="options?.root" class="text-xs mt-2 break-all font-mono" :class="$styles.muted">{{destination}}</p></div>
            <label v-if="session.kind === 'new' && options?.initializeGit" class="flex items-start gap-3 text-sm cursor-pointer">
                <CheckBox v-model="session.initializeGit" class="mt-0.5"/><span>Initialize Git repository<small class="block text-xs mt-1" :class="$styles.muted">Keep local version history as your project grows.</small></span></label>
            <details @toggle="advanced = $event.target.open" data-project-create-details class="border-t border-t-[var(--border)] pt-4">
                <summary :class="$styles.muted" class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-blue-500 focus-visible:outline-offset-3 text-sm cursor-pointer">More options</summary>
                <div class="grid gap-4 pt-4">
                    <div v-if="session.kind === 'clone'"><label for="project-create-branch" data-project-create-label class="block text-[13px] font-medium mb-[7px]">Branch <span :class="$styles.muted">(optional)</span></label><input id="project-create-branch" v-model="session.branch" placeholder="Default branch" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput, inputClass, 'font-mono']" data-project-create-input/></div>
                    <div><label for="project-create-description" data-project-create-label class="block text-[13px] font-medium mb-[7px]">Description</label><input id="project-create-description" v-model="session.description" placeholder="What are you working on?" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput, inputClass]" data-project-create-input/></div>
                    <label class="flex items-center gap-3 text-sm cursor-pointer"><CheckBox v-model="session.showInSidebar"/>Show folder in sidebar</label>
                    <div><label for="project-create-publish" data-project-create-label class="block text-[13px] font-medium mb-[7px]">Publish build directory <span :class="$styles.muted">(optional)</span></label><input id="project-create-publish" v-model="session.publish" placeholder="e.g. dist" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput, inputClass, 'font-mono']" data-project-create-input/></div>
                </div>
            </details>
            <p v-if="error" role="alert" class="text-sm text-red-600 dark:text-red-400">{{error}}</p>
            <div class="mt-auto pt-5 flex justify-end gap-3">
                <button type="button" @click="$emit('done')" :class="[$styles.secondaryButton, buttonClass, focusOutline]" data-project-create-button>Close</button>
                <button type="submit" :disabled="!canCreate" :class="[$styles.primaryButton, buttonClass]" data-project-create-button>{{submitting ? 'Preparing…' : session.kind === 'clone' ? 'Clone & create project' : 'Create project'}}</button>
            </div>
        </form>
    </div>`,
}
