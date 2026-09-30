import { ref, computed, inject, onMounted, onUnmounted, watch, nextTick } from 'vue'
export default {
  components: {
    ProjectFolderIcon: {
      template: `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 8V6a2 2 0 0 1 2-2h5l2 2h7a2 2 0 0 1 2 2v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8Z"/><path d="M3 9h18"/></svg>`
    }
  },
  template: `<div ref="root" class="relative min-w-0 max-w-full" @keydown.esc="close">
      <div class="group/project inline-flex items-center max-w-full rounded-[18px] bg-[rgb(128_128_128/0.08)] py-[5px] px-2.5 gap-[7px] text-sm" :class="projectId ? '' : 'text-gray-500 dark:text-gray-400'">
        <button v-if="projectId" type="button" class="group/remove grid place-items-center cursor-pointer shrink-0 disabled:opacity-45 disabled:cursor-not-allowed outline-none! focus:outline-none! focus:ring-0! focus:[box-shadow:none]! focus-visible:bg-gray-100 dark:focus-visible:bg-gray-800" :disabled="busy" @click="removeProject" aria-label="Remove project from this chat" title="Remove project from this chat">
          <ProjectFolderIcon class="group-hover/project:hidden group-focus-visible/remove:hidden" />
          <svg class="hidden group-hover/project:block group-focus-visible/remove:block" width="18" height="18" viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="10" fill="currentColor" opacity=".65"/><path d="m9 9 6 6m0-6-6 6" stroke="white" stroke-width="1.7" stroke-linecap="round"/></svg>
        </button>
        <button ref="trigger" type="button" class="flex text-sm items-center gap-2 min-w-0 text-left cursor-pointer [&_svg]:shrink-0 outline-none! focus:outline-none! focus:ring-0! focus:[box-shadow:none]! focus-visible:bg-gray-100 dark:focus-visible:bg-gray-800" @click="toggle" :aria-expanded="open" aria-haspopup="dialog" title="Change the project for this chat">
          <ProjectFolderIcon v-if="!projectId" />
          <span class="truncate">{{label}}</span>
        </button>
      </div>
      <div v-if="open" role="dialog" aria-label="Choose project" class="absolute bottom-full left-0 mb-2 w-[328px] max-w-[calc(100vw-48px)] rounded-[22px] p-[7px] shadow-[0_8px_28px_rgb(0_0_0/0.12)] z-50 border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900" @keydown.down.prevent="moveFocus($event, 1)" @keydown.up.prevent="moveFocus($event, -1)">
        <div class="flex items-center gap-2 mx-2 pt-2.5 px-px pb-3 text-gray-400 dark:text-gray-500 border-b border-gray-200 dark:border-gray-700">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true"><circle cx="10.5" cy="10.5" r="6.5"/><path d="m16 16 4 4"/></svg>
          <input class="min-w-0 w-full bg-transparent border-0 outline-none text-sm text-inherit focus:text-gray-700 dark:focus:text-gray-200" ref="search" v-model="query" aria-label="Search projects" placeholder="Search projects" />
        </div>
        <p v-if="busy" class="text-xs px-3 py-2 text-gray-500 dark:text-gray-400">Projects can't change while this chat has an active run.</p>
        <p v-else-if="thread" class="text-xs px-3 pt-2 text-gray-500 dark:text-gray-400">changes the workspace for future messages</p>
        <div class="max-h-[188px] overflow-y-auto py-[5px] [scrollbar-width:thin]">
          <button v-for="p in filtered" :key="p.id" type="button" class="flex items-center gap-2.5 w-full text-left py-2 px-2.5 rounded-xl text-sm cursor-pointer [&_svg]:shrink-0 [&_svg]:opacity-70 disabled:opacity-45 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-blue-500 focus-visible:outline-offset-2 hover:bg-gray-100 dark:hover:bg-gray-800" :disabled="busy" :aria-pressed="p.id === projectId" @click="select(p.id)">
            <ProjectFolderIcon /><span class="truncate flex-1">{{p.name}}</span>
            <svg v-if="p.id === projectId" class="size-4 opacity-100! text-blue-600 dark:text-blue-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m5 12 5 5L20 7"/></svg>
          </button>
          <p v-if="!filtered.length" class="text-sm px-3 py-2 opacity-60">{{query ? 'No projects found' : 'No projects yet'}}</p>
        </div>
        <div class="pt-[5px] mt-1 border-t border-gray-200 dark:border-gray-700">
          <button type="button" :disabled="busy" class="flex items-center gap-2.5 w-full text-left py-2 px-2.5 rounded-xl text-sm cursor-pointer [&_svg]:shrink-0 [&_svg]:opacity-70 disabled:opacity-45 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-blue-500 focus-visible:outline-offset-2 hover:bg-gray-100 dark:hover:bg-gray-800" :aria-pressed="!projectId" @click="select(null)">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" aria-hidden="true"><circle cx="12" cy="12" r="8.5"/><path d="m6 6 12 12"/></svg>
            <span class="flex-1">Don't work in a project</span>
            <svg v-if="!projectId" class="size-4 opacity-100! text-blue-600 dark:text-blue-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m5 12 5 5L20 7"/></svg>
          </button>
          <button type="button" :disabled="busy" class="flex items-center gap-2.5 w-full text-left py-2 px-2.5 rounded-xl text-sm cursor-pointer [&_svg]:shrink-0 [&_svg]:opacity-70 disabled:opacity-45 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-blue-500 focus-visible:outline-offset-2 hover:bg-gray-100 dark:hover:bg-gray-800" @click="create">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" aria-hidden="true"><path d="M12 4v16M4 12h16"/></svg>
            <span>New project</span>
          </button>
        </div>
        <p v-if="error" role="alert" class="text-xs text-red-500 px-3 py-2">{{error}}</p>
      </div>
      <p v-if="error && !open" role="alert" class="text-xs text-red-500">{{error}}</p>
    </div>`,
  setup() {
    const ctx = inject('ctx'), open = ref(false), query = ref(''), projects = computed(() => ctx.state.projects || []), error = ref(''), trigger = ref(null)
    const root = ref(null), search = ref(null)
    const thread = computed(() => String(ctx.threads?.currentThread.value?.id) === ctx.chat.drafts.state.key ? ctx.threads.currentThread.value : null)
    const projectId = computed(() => thread.value ? thread.value.projectId : ctx.chat.drafts.get().projectId)
    const label = computed(() => projects.value.find(p => p.id === projectId.value)?.name || 'No project')
    const filtered = computed(() => projects.value.filter(p => p.name.toLowerCase().includes(query.value.toLowerCase())))
    const busy = computed(() => ['queued', 'running', 'waiting_approval'].includes(thread.value?.run?.status))
    let origin
    async function load() {
      const previous = ctx.state.projects
      const api = await ctx.getJson('/ext/projects/projects.json')
      if (api.response && ctx.state.projects === previous) ctx.setState({ projects: api.response })
    }
    function close() { open.value = false; trigger.value?.focus() }
    async function toggle() { if (open.value) return close(); origin = { draft: ctx.chat.drafts.get(), thread: thread.value }; query.value = ''; open.value = true; error.value = ''; await nextTick(); search.value?.focus(); await load() }
    async function select(id, target = origin) {
      if (!target || busy.value) return
      error.value = ''
      if (target.thread) {
        const api = await ctx.scope('app').patchJson('/threads/' + target.thread.id, { projectId: id, membershipVersion: target.thread.membershipVersion || 0 })
        if (!api.response) { error.value = api.error?.message || 'Unable to move chat'; return }
        ctx.threads.replaceThread(api.response)
      }
      target.draft.projectId = id; ctx.chat.drafts.touch(target.draft); close()
    }
    function removeProject() {
      return select(null, { draft: ctx.chat.drafts.get(), thread: thread.value })
    }
    function create() {
      // Keep the initiating draft/thread: the new project applies to it, not to a fresh chat.
      const target = origin
      close()
      ctx.projects.createForChat(async project => {
        await load()
        if (target) await select(project.id, target)
      })
    }
    function outside(event) { if (open.value && !root.value?.contains(event.target)) close() }
    function moveFocus(event, direction) {
      const buttons = Array.from(root.value?.querySelector('[role=dialog]')?.querySelectorAll('button:not(:disabled),input') || [])
      const index = buttons.indexOf(event.target)
      buttons[(index + direction + buttons.length) % buttons.length]?.focus()
    }
    watch(() => ctx.chat.drafts.state.key, close)
    onMounted(() => { document.addEventListener('pointerdown', outside); if (ctx.state.config.extensions.includes('projects')) load() })
    onUnmounted(() => document.removeEventListener('pointerdown', outside))
    return { root, search, removeProject, moveFocus, open, query, filtered, label, projectId, busy, thread, error, trigger, toggle, close, select, create }
  }
}
