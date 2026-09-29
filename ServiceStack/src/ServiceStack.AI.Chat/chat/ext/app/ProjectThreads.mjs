import { ref, inject, onMounted, onUnmounted, computed, nextTick } from 'vue'

export default {
    directives: { focus: { mounted: el => el.focus() } },
    template: `<div class="flex flex-col h-full"><Brand>
      <button type="button" class="p-1.5 rounded-md hover:bg-black/5 dark:hover:bg-white/5 cursor-pointer" @click="$ctx.to('/chat/recents')" aria-label="Search chats" title="Search chats">
        <svg class="size-5" fill="currentColor" viewBox="0 0 16 16" aria-hidden="true"><path fill-rule="evenodd" d="M9.965 11.026a5 5 0 1 1 1.06-1.06l2.755 2.754a.75.75 0 1 1-1.06 1.06l-2.755-2.754ZM10.5 7a3.5 3.5 0 1 1-7 0 3.5 3.5 0 0 1 7 0Z" clip-rule="evenodd"></path></svg>
      </button>
    </Brand>
      <div class="flex-1 overflow-y-auto px-2 pt-2 pb-6">
        <button type="button" class="w-full rounded-md px-2 py-1.5 mb-2 text-sm inline-flex items-center gap-2 hover:bg-black/5 dark:hover:bg-white/5 cursor-pointer" :class="{'bg-black/[0.07] dark:bg-white/10': isNewChatActive}" @click="newChat(null)"><svg xmlns="http://www.w3.org/2000/svg" class="size-4 shrink-0" viewBox="0 0 24 24" aria-hidden="true"><g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"><path d="M12 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.375 2.625a1 1 0 0 1 3 3l-9.013 9.014a2 2 0 0 1-.853.505l-2.873.84a.5.5 0 0 1-.62-.62l.84-2.873a2 2 0 0 1 .506-.852z"></path></g></svg>New chat</button>
        <p v-if="error" role="alert" class="text-xs text-red-500 px-2 py-1">{{ error }} <button type="button" class="underline cursor-pointer" @click="refresh">Retry</button></p>
        <div class="group/header relative flex items-center rounded-md">
          <button type="button" class="sidebar-row-title flex-1 min-w-0 text-left text-xs font-medium text-gray-500 dark:text-gray-400 px-2 py-1.5 flex items-center gap-1 cursor-pointer hover:text-gray-700 dark:hover:text-gray-200" @click="toggleProjects" :aria-expanded="projectsExpanded">
            <span>Projects</span>
            <svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 24 24" aria-hidden="true" class="transition-transform opacity-0 group-hover/header:opacity-100 group-focus-within/header:opacity-100" :class="{'-rotate-90': !projectsExpanded}"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m6 10l6 6l6-6"/></svg>
          </button>
          <div class="folder-row-action absolute right-0 inset-y-0 flex items-center opacity-0 group-hover/header:opacity-100 group-focus-within/header:opacity-100">
            <button type="button" class="p-1.5 rounded-md text-gray-500 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-100 hover:bg-black/5 dark:hover:bg-white/5 cursor-pointer" @click.stop="openProjectManager" aria-label="Manage projects" title="Manage projects"><svg xmlns="http://www.w3.org/2000/svg" class="size-3.5" viewBox="0 0 24 24" aria-hidden="true"><path fill="currentColor" fill-rule="evenodd" d="M4.5 12a1.5 1.5 0 1 1 3 0a1.5 1.5 0 0 1-3 0m6 0a1.5 1.5 0 1 1 3 0a1.5 1.5 0 0 1-3 0m6 0a1.5 1.5 0 1 1 3 0a1.5 1.5 0 0 1-3 0" clip-rule="evenodd"/></svg></button>
            <button type="button" class="p-1.5 rounded-md text-gray-500 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-100 hover:bg-black/5 dark:hover:bg-white/5 cursor-pointer" @click.stop="manageProjects" aria-label="New project" title="New project"><svg xmlns="http://www.w3.org/2000/svg" class="size-3.5" viewBox="0 0 24 24" aria-hidden="true"><path fill="currentColor" d="M19 11h-6V5h-2v6H5v2h6v6h2v-6h6z"/></svg></button>
          </div>
        </div>
        <p v-if="projectsExpanded && !visibleGroups.some(g => g.id)" class="text-xs px-2 py-1 text-gray-500 dark:text-gray-400">No project chats yet</p>
        <section v-for="group in visibleGroups" :key="group.id || 'unassigned'" v-show="!group.id || projectsExpanded" :class="group.id ? 'mb-1' : 'mt-4'">
          <div class="folder-row group/folder relative flex items-center rounded-md" :class="group.id ? 'hover:bg-black/5 dark:hover:bg-white/5' : ''">
            <button type="button" class="sidebar-row-title flex-1 min-w-0 text-left px-2 py-1.5 flex items-center gap-2 cursor-pointer" :class="group.id ? 'text-sm font-medium folder-title group-hover/folder:pr-16 group-focus-within/folder:pr-16' : 'text-xs font-medium text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200'" @click="toggle(group)" @contextmenu="openProjectMenu($event, group)" :aria-expanded="group.expanded" :title="group.name">
              <svg v-if="group.id" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 640 640" class="size-4 shrink-0 text-gray-500 dark:text-gray-400" aria-hidden="true">
                <path v-if="group.expanded" fill="currentColor" d="m129.5 464l50-160h379.4l-50 160zm190.7 48H509c21 0 39.6-13.6 45.8-33.7l50-160c9.7-30.9-13.4-62.3-45.8-62.3H179.6c-21 0-39.6 13.6-45.8 33.7l-21.6 68.7V160c0-8.8 7.2-16 16-16h138.7c3.5 0 6.8 1.1 9.6 3.2l38.4 28.8c13.8 10.4 30.7 16 48 16h117.3c8.8 0 16 7.2 16 16h48c0-35.3-28.7-64-64-64H362.9c-6.9 0-13.7-2.2-19.2-6.4l-38.4-28.8c-11.1-8.3-24.5-12.8-38.4-12.8H128.2c-35.3 0-64 28.7-64 64v288c0 35.3 28.7 64 64 64z"/>
                <path v-else fill="currentColor" d="M512 464H128c-8.8 0-16-7.2-16-16V304h416v144c0 8.8-7.2 16-16 16m16-208H112v-96c0-8.8 7.2-16 16-16h138.7c3.5 0 6.8 1.1 9.6 3.2l38.4 28.8c13.8 10.4 30.7 16 48 16H512c8.8 0 16 7.2 16 16zM128 512h384c35.3 0 64-28.7 64-64V208c0-35.3-28.7-64-64-64H362.7c-6.9 0-13.7-2.2-19.2-6.4l-38.4-28.8C294 100.5 280.5 96 266.7 96H128c-35.3 0-64 28.7-64 64v288c0 35.3 28.7 64 64 64"/>
              </svg>
              <span class="truncate">{{ group.name }}</span>
              <svg v-if="!group.id" xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 24 24" aria-hidden="true" class="transition-transform opacity-0 group-hover/folder:opacity-100 group-focus-within/folder:opacity-100" :class="{'-rotate-90': !group.expanded}"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m6 10l6 6l6-6"/></svg>
            </button>
            <div v-if="group.id" class="folder-row-action absolute right-0.5 inset-y-0 flex items-center opacity-0 group-hover/folder:opacity-100 group-focus-within/folder:opacity-100" :class="{'opacity-100!': menu === group.id}">
              <button type="button" class="p-1.5 rounded-md text-gray-500 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-100 hover:bg-black/5 dark:hover:bg-white/10 cursor-pointer" @click.stop="menu = menu === group.id ? null : group.id" :aria-label="'More options for ' + group.name" :aria-expanded="menu === group.id" aria-haspopup="menu" title="More options"><svg xmlns="http://www.w3.org/2000/svg" class="size-3.5" viewBox="0 0 24 24" aria-hidden="true"><path fill="currentColor" fill-rule="evenodd" d="M4.5 12a1.5 1.5 0 1 1 3 0a1.5 1.5 0 0 1-3 0m6 0a1.5 1.5 0 1 1 3 0a1.5 1.5 0 0 1-3 0m6 0a1.5 1.5 0 1 1 3 0a1.5 1.5 0 0 1-3 0" clip-rule="evenodd"/></svg></button>
              <button type="button" class="p-1.5 rounded-md text-gray-500 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-100 hover:bg-black/5 dark:hover:bg-white/10 cursor-pointer" @click.stop="newChat(group.id)" :aria-label="'New chat in ' + group.name" title="New chat in project"><svg xmlns="http://www.w3.org/2000/svg" class="size-3.5" viewBox="0 0 24 24" aria-hidden="true"><g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"><path d="M12 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.375 2.625a1 1 0 0 1 3 3l-9.013 9.014a2 2 0 0 1-.853.505l-2.873.84a.5.5 0 0 1-.62-.62l.84-2.873a2 2 0 0 1 .506-.852z"></path></g></svg></button>
            </div>
            <div v-if="group.id && menu === group.id" role="menu" class="absolute right-0 top-full mt-1 z-20 min-w-44 rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-lg p-1" @click.stop>
              <button type="button" role="menuitem" class="w-full text-left text-sm px-2 py-1.5 rounded-md hover:bg-gray-100 dark:hover:bg-gray-700 cursor-pointer" @click="menu = null; newChat(group.id)">New chat</button>
              <button type="button" role="menuitem" class="w-full text-left text-sm px-2 py-1.5 rounded-md hover:bg-gray-100 dark:hover:bg-gray-700 cursor-pointer" @click="menu = null; $ctx.projects.editProject(group.id)">Edit project…</button>
              <button type="button" role="menuitem" class="w-full text-left text-sm px-2 py-1.5 rounded-md hover:bg-gray-100 dark:hover:bg-gray-700 cursor-pointer" @click="hideFolder(group)">Hide from sidebar</button>
            </div>
          </div>
          <div v-if="group.expanded">
            <div v-for="draft in draftsIn(group)" :key="draft.key" @mouseenter="showDraftPreview($event, draft)" @mouseleave="preview = null" @focusin="showDraftPreview($event, draft)" @focusout="preview = null" class="group/row relative rounded-md text-sm" :class="isActiveDraft(draft) ? 'bg-black/[0.07] dark:bg-white/10' : 'hover:bg-black/5 dark:hover:bg-white/5'">
              <button type="button" class="thread-row-title w-full min-w-0 text-left py-1.5 pr-2 flex items-center gap-2 cursor-pointer group-hover/row:pr-8 group-focus-within/row:pr-8" :class="rowIndent(group)" @click="selectDraft(draft)" :aria-label="'Draft: ' + (draft.text || 'New chat')">
                <span class="truncate flex-1" :class="draft.text ? '' : 'text-gray-500 dark:text-gray-400'">{{draft.text || (draft.attachments.length ? plural(draft.attachments.length, 'attachment') : 'New chat')}}</span>
                <span class="shrink-0 inline-flex items-center gap-1 rounded-full px-1.5 py-px text-[10px] font-medium leading-4 bg-amber-500/15 text-amber-700 dark:text-amber-300 group-hover/row:hidden group-focus-within/row:hidden" aria-hidden="true"><svg class="size-2.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M12 20h9"/><path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4Z"/></svg>Draft</span>
              </button>
              <div class="thread-row-action absolute right-1 inset-y-0 flex items-center opacity-0 group-hover/row:opacity-100 group-focus-within/row:opacity-100">
                <button type="button" class="p-1 rounded text-gray-500 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-100 cursor-pointer disabled:opacity-40" :disabled="draft.sending" @click.stop="removeDraft(draft)" aria-label="Discard draft" title="Discard draft"><svg xmlns="http://www.w3.org/2000/svg" class="size-3.5" viewBox="0 0 24 24" aria-hidden="true"><path fill="currentColor" d="m18.3 7.1-1.4-1.4L12 10.6 7.1 5.7 5.7 7.1l4.9 4.9-4.9 4.9 1.4 1.4 4.9-4.9 4.9 4.9 1.4-1.4-4.9-4.9z"/></svg></button>
              </div>
            </div>
            <div v-for="thread in rowsIn(group)" :key="thread.id" @mouseenter="showPreview($event, thread)" @mouseleave="preview = null" @focusin="showPreview($event, thread)" @focusout="onRowFocusOut($event, thread)" class="group/row relative rounded-md text-sm" :class="$threads.currentThread.value?.id === thread.id ? 'bg-black/[0.07] dark:bg-white/10 font-medium' : 'hover:bg-black/5 dark:hover:bg-white/5'">
              <button type="button" class="thread-row-title w-full min-w-0 text-left py-1.5 pr-2 flex items-center gap-2 cursor-pointer group-hover/row:pr-8 group-focus-within/row:pr-8" :class="[rowIndent(group), {'pr-16!': confirming === thread.id}]" @click="$ctx.to('/c/' + thread.id)" :aria-current="$threads.currentThread.value?.id === thread.id ? 'page' : null">
                <span class="truncate flex-1">{{ thread.title || 'New Chat' }}</span>
                <span v-if="thread.runStatus" class="shrink-0 size-1.5 rounded-full group-hover/row:hidden group-focus-within/row:hidden" :class="thread.runStatus === 'waiting_approval' ? 'bg-amber-500' : 'bg-emerald-500 animate-pulse'" :aria-label="runLabel(thread.runStatus)" :title="runLabel(thread.runStatus)"></span>
              </button>
              <div class="thread-row-action absolute right-1 inset-y-0 flex items-center opacity-0 group-hover/row:opacity-100 group-focus-within/row:opacity-100" :class="{'opacity-100!': confirming === thread.id}">
                <button v-if="confirming !== thread.id" type="button" class="p-1 rounded text-gray-500 dark:text-gray-400 hover:text-red-600 dark:hover:text-red-400 cursor-pointer" @click.stop="confirming = thread.id" :aria-label="'Delete ' + (thread.title || 'chat')" title="Delete chat"><svg xmlns="http://www.w3.org/2000/svg" class="size-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 7h16M10 11v6M14 11v6M5 7l1 12a2 2 0 0 0 2 2h8a2 2 0 0 0 2-2l1-12M9 7V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v3"/></svg></button>
                <button v-else type="button" v-focus class="px-2 py-0.5 rounded text-xs font-medium bg-red-600 hover:bg-red-700 text-white cursor-pointer" @click.stop="remove(thread)" @keydown.esc.stop="confirming = null" :aria-label="'Confirm delete ' + (thread.title || 'chat')">Delete</button>
              </div>
            </div>
            <p v-if="!group.items.length && !draftsIn(group).length" class="text-xs py-1 text-gray-500 dark:text-gray-400" :class="rowIndent(group)">No chats yet</p>
            <button v-if="group.hasMore" type="button" class="w-full text-left rounded-md py-1 text-xs text-gray-500 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-100 cursor-pointer disabled:cursor-wait" :class="rowIndent(group)" :disabled="group.loading" @click="more(group)">{{group.loading ? 'Loading…' : 'Show more'}}</button>
            <p v-if="group.error" class="text-xs text-red-500 py-1" :class="rowIndent(group)" role="alert">{{group.error}} <button type="button" class="underline cursor-pointer" @click="more(group)">Retry</button></p>
          </div>
        </section>
      </div>
      <Teleport to="body">
        <div v-if="preview" ref="previewElement" role="tooltip" class="fixed z-[300] pointer-events-none w-72 max-w-[calc(100vw-16px)] rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 shadow-xl p-3" :style="previewPosition">
          <div class="text-sm font-semibold break-words">{{preview.title || 'New Chat'}}</div>
          <div class="mt-2 text-xs text-gray-500 dark:text-gray-400"><span v-if="preview.lastActivityAt">{{$fmt.relativeTime(preview.lastActivityAt)}} · </span><span v-if="preview.isDraft">Draft · {{plural(preview.attachmentCount, 'attachment')}}</span><span v-else-if="preview.messageCount != null">{{plural(preview.messageCount, 'message')}}</span><span v-else>Loading…</span></div>
          <div v-if="preview.model" class="mt-1 text-xs break-words text-blue-600 dark:text-blue-400">{{preview.model}}</div>
          <div v-if="previewStats.inputTokens || previewStats.outputTokens" class="mt-2 text-xs">
            {{$fmt.humanifyNumber(previewStats.inputTokens || 0)}} input · {{$fmt.humanifyNumber(previewStats.outputTokens || 0)}} output tokens
          </div>
          <div v-if="previewStats.cost != null" class="mt-1 text-xs">{{$fmt.cost(previewStats.cost) || '$0.00'}}</div>
          <div v-if="preview.runStatus" class="mt-2 text-xs text-emerald-600 dark:text-emerald-400">{{runLabel(preview.runStatus)}}</div>
          <div v-else-if="preview.error" class="mt-2 text-xs break-words text-red-600 dark:text-red-400 line-clamp-3">{{preview.error}}</div>
        </div>
      </Teleport>
    </div>`,
    setup() {
        const ctx = inject('ctx'), ext = ctx.scope('app')
        const groups = ref([]), error = ref(''), menu = ref(null), projectsExpanded = ref(true)
        let generation = 0, timer, source, disposed = false, refreshing = false, revision = null
        const preview = ref(null), previewElement = ref(null), previewPosition = ref({})
        const previewStats = computed(() => {
            let stats = preview.value?.stats
            if (typeof stats === 'string') { try { stats = JSON.parse(stats) } catch { stats = null } }
            return {...preview.value, ...(stats || {})}
        })
        function showDraftPreview(event, draft) {
            return showPreview(event, {
                id: draft.key, title: draft.text || 'New chat', isDraft: true,
                lastActivityAt: draft.updatedAt, attachmentCount: draft.attachments.length
            })
        }
        async function showPreview(event, thread) {
            const rect = event.currentTarget.getBoundingClientRect()
            preview.value = thread
            previewPosition.value = {left: Math.min(rect.right + 10, Math.max(8, window.innerWidth - 296)) + 'px', top: rect.top + 'px'}
            if (!thread.isDraft && (thread.messageCount == null || !('model' in thread))) {
                const query = new URLSearchParams({id: String(thread.id), fields: 'id,title,model,stats,inputTokens,outputTokens,cost,lastActivityAt'})
                try {
                    const api = await ext.getJson('/threads?' + query)
                    const summary = api.response?.find(row => row.id === thread.id)
                    if (summary) Object.assign(thread, summary)
                    if (preview.value?.id === thread.id) preview.value = {...thread}
                } catch { /* Keep the title available if the summary cannot load. */ }
            }
            await nextTick()
            if (preview.value?.id !== thread.id || !previewElement.value) return
            previewPosition.value.top = Math.max(8, Math.min(rect.top, window.innerHeight - previewElement.value.offsetHeight - 8)) + 'px'
        }
        function hidePreview() { preview.value = null }
        const selected = computed(() => ctx.threads.currentThread.value)
        const confirming = ref(null)
        // Drafts whose project no longer exists belong in Recents, matching their "No project" chip
        const draftProject = d => d.projectId && (!Array.isArray(ctx.state.projects)
            || ctx.state.projects.some(p => p.id === d.projectId)) ? d.projectId : null
        const draftsIn = group => ctx.chat.drafts.list().filter(d => draftProject(d) === group.id)
        // The selected chat may be beyond the loaded page; show it last instead of faking cursor order.
        const rowsIn = group => {
            const current = selected.value
            return current && (current.projectId || null) === group.id && !group.items.some(t => t.id === current.id)
                ? [...group.items, current] : group.items
        }
        const rowIndent = group => group.id ? 'pl-8' : 'pl-2'
        const isActiveDraft = draft => !selected.value && ctx.chat.drafts.state.key === draft.key
        const isNewChatActive = computed(() => !selected.value && !ctx.chat.drafts.list().some(isActiveDraft))
        const runLabels = { queued: 'Queued', running: 'Working', waiting_approval: 'Waiting for approval' }
        const runLabel = status => runLabels[status] || status
        const plural = (n, word) => `${n ?? 0} ${word}${n === 1 ? '' : 's'}`
        // Only cancel a pending delete when focus moves to another element outside the row.
        // A null relatedTarget also happens when the focused trash button is swapped for the
        // confirm button, which must not cancel the confirmation it just opened.
        function onRowFocusOut(event, thread) {
            preview.value = null
            const next = event.relatedTarget
            if (confirming.value === thread.id && next && !event.currentTarget.contains(next)) confirming.value = null
        }
        const prefKey = () => `chat-project-folders:${ctx.ai.base || ''}:${ctx.ai.auth?.userName || ctx.ai.auth?.username || ctx.ai.auth?.id || 'default'}`
        const draftGroups = ref({})
        const visibleGroups = computed(() => {
            const ids = new Set(ctx.chat.drafts.list().map(d => d.projectId).filter(Boolean))
            const existing = new Set(groups.value.map(g => g.id))
            const extra = (ctx.state.projects || []).filter(p => ids.has(p.id) && !existing.has(p.id) && p.showInSidebar !== false)
                .map(p => {
                    const key = prefKey() + ':' + p.id
                    if (!draftGroups.value[key]) draftGroups.value[key] = {
                        id: p.id, items: [], hasMore: false, expanded: expansion()[p.id] ?? true
                    }
                    draftGroups.value[key].name = p.name
                    return draftGroups.value[key]
                })
            return [...groups.value.filter(g => g.id), ...extra, ...groups.value.filter(g => !g.id)]
        })
        function expansion() { try { return JSON.parse(localStorage.getItem(prefKey()) || '{}') } catch { return {} } }
        function toggle(group) {
            group.expanded = !group.expanded
            try { localStorage.setItem(prefKey(), JSON.stringify(Object.fromEntries(visibleGroups.value.map(g => [g.id || 'unassigned', g.expanded])))) } catch {}
        }
        function toggleProjects() {
            projectsExpanded.value = !projectsExpanded.value
            menu.value = null
            try { localStorage.setItem(prefKey() + ':projects', JSON.stringify(projectsExpanded.value)) } catch {}
        }
        function openProjectManager() {
            ctx.projectCreationRequest = null
            ctx.openModal('projects-manager')
        }
        function manageProjects() { ctx.projects.openNewProject() }
        let refreshQueued = false
        async function refresh() {
            // A change signalled mid-refresh must not be lost: run once more afterwards
            if (refreshing) { refreshQueued = true; return }
            refreshing = true
            const version = ++generation
            try {
                const api = await ext.getJson('/thread-sidebar')
                if (disposed || version !== generation) return
                if (!api.response) { error.value = api.error?.message || 'Unable to load chats'; return }
                error.value = ''
                revision = api.response.revision
                const current = ctx.threads.currentThread.value
                if (current) {
                    const summary = [...api.response.projects.flatMap(g => g.items), ...api.response.unassigned.items].find(t => t.id === current.id)
                    if (summary) for (const field of ['title','projectId','metadataVersion','membershipVersion']) current[field] = summary[field]
                }
                const old = new Map(groups.value.map(g => [g.id, g]))
                groups.value = [...api.response.projects, { ...api.response.unassigned, id: null, name: 'Recents' }]
                    .map(g => ({...g, expanded: old.get(g.id)?.expanded ?? expansion()[g.id || 'unassigned'] ?? true, loading: false, generation: version}))
                // Keep rows revealed with "Show more": refill each such group to its previous length
                for (const group of groups.value) {
                    const shown = old.get(group.id)?.items?.length || 0
                    if (group.hasMore && shown > group.items.length) more(group, Math.min(100, shown - group.items.length))
                }
            } finally {
                refreshing = false
                if (refreshQueued) { refreshQueued = false; refresh() }
            }
        }
        async function more(group, limit = 10) {
            if (group.loading) return
            group.loading = true; group.error = ''
            const query = new URLSearchParams({limit: String(limit), cursor: group.nextCursor})
            if (group.id) query.set('projectId', group.id); else query.set('scope', 'unassigned')
            try {
                const api = await ext.getJson('/thread-sidebar/threads?' + query)
                if (group.generation !== generation) return
                if (!api.response) { group.error = api.error?.message || 'Unable to load chats'; return }
                const known = new Set(group.items.map(t => t.id))
                group.items.push(...api.response.items.filter(t => !known.has(t.id)))
                group.nextCursor = api.response.nextCursor; group.hasMore = api.response.hasMore
            } finally { group.loading = false }
        }
        async function removeDraft(draft) {
            if (draft.sending) return
            if (ctx.chat.drafts.state.key === draft.key) {
                ctx.chat.drafts.fresh()
                ctx.threads.clearCurrentThread()
                ctx.to('/')
            }
            await ctx.chat.drafts.discard(draft.key)
        }
        function selectDraft(draft) {
            ctx.chat.drafts.bind(draft.key)
            ctx.threads.clearCurrentThread()
            ctx.to('/')
        }
        function newChat(projectId) {
            ctx.chat.drafts.fresh(projectId)
            ctx.threads.clearCurrentThread()
            ctx.to('/')
        }
        async function remove(thread) {
            confirming.value = null
            preview.value = null
            const wasCurrent = selected.value?.id === thread.id
            try {
                await ctx.threads.deleteThread(thread.id)
            } catch (e) {
                error.value = e.message || 'Unable to delete chat'
                return
            }
            if (wasCurrent) ctx.to('/')
            await refresh()
        }
        async function hideFolder(group) {
            menu.value = null
            const api = await ctx.scope('projects').patchJson('/sidebar/' + encodeURIComponent(group.id), {showInSidebar:false})
            if (api.error) { error.value = api.error.message || 'Unable to hide folder'; return }
            ctx.setState({projects: api.response})
            await refresh()
        }
        function openProjectMenu(event, group) {
            if (!group.id) return
            event.preventDefault()
            event.stopPropagation()
            menu.value = menu.value === group.id ? null : group.id
        }
        function dismissMenu(event) {
            if (!event.target.closest('.folder-row')) menu.value = null
            if (!event.target.closest('.thread-row-action')) confirming.value = null
        }
        function dismissMenuKey(event) { if (event.key === 'Escape') { menu.value = null; confirming.value = null } }
        async function poll() {
            if (disposed) return
            try {
                const api = await ext.getJson('/thread-sidebar/updates?sig=' + encodeURIComponent(revision || ''))
                if (api.response && api.response.revision !== revision) await refresh()
            } finally { if (!disposed) timer = setTimeout(poll, 1000) }
        }
        function subscribe() {
            const transport = ctx.state.config.defaults?.events?.transport || 'auto'
            if (transport === 'long-poll' || !globalThis.EventSource) { poll(); return }
            source = new EventSource(ctx.resolveUrl('/ext/app/thread-sidebar/updates/stream?sig=' + encodeURIComponent(revision || '')), {withCredentials:true})
            source.onmessage = event => {
                try { if (JSON.parse(event.data).revision !== revision) refresh() } catch {}
            }
            source.onerror = () => {
                source.close()
                if (!disposed) timer = setTimeout(transport === 'sse' ? subscribe : poll, 1000)
            }
        }
        onMounted(async () => { window.addEventListener('scroll', hidePreview, true); window.addEventListener('resize', hidePreview); try { projectsExpanded.value = JSON.parse(localStorage.getItem(prefKey() + ':projects') || 'true') } catch {} await refresh(); if (!disposed) subscribe(); globalThis.addEventListener('focus', refresh); document.addEventListener('click', dismissMenu); document.addEventListener('keydown', dismissMenuKey) })
        onUnmounted(() => { window.removeEventListener('scroll', hidePreview, true); window.removeEventListener('resize', hidePreview); disposed = true; source?.close(); clearTimeout(timer); globalThis.removeEventListener('focus', refresh); document.removeEventListener('click', dismissMenu); document.removeEventListener('keydown', dismissMenuKey) })
        return { confirming, onRowFocusOut, draftsIn, rowsIn, rowIndent, isActiveDraft, isNewChatActive, runLabel, plural, preview, previewElement, previewPosition, previewStats, showPreview, showDraftPreview, groups, visibleGroups, selected, toggle, toggleProjects, projectsExpanded, manageProjects, openProjectManager, error, refresh, more, newChat, selectDraft, removeDraft, remove, hideFolder, openProjectMenu, menu }
    }
}
