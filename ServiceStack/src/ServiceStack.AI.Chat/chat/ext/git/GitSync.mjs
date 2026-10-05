import { computed, inject, onUnmounted, ref, watch } from 'vue'

const circle = 'M9.95 13h2.55c.28 0 .5.22.5.5s-.22.5-.5.5H9.95a2.5 2.5 0 0 1-4.9 0H2.5c-.28 0-.5-.22-.5-.5s.22-.5.5-.5h2.55a2.5 2.5 0 0 1 4.9 0m-3.86 1a1.495 1.495 0 0 0 2.82 0c.06-.16.09-.32.09-.5s-.03-.34-.09-.5a1.495 1.495 0 0 0-2.82 0c-.06.16-.09.32-.09.5s.03.34.09.5'
const arrows = {
    push: 'M4.85 4.85a.48.48 0 0 1-.7 0a.48.48 0 0 1 0-.7l3-3a.48.48 0 0 1 .7 0l3 3a.48.48 0 0 1 0 .7a.48.48 0 0 1-.7 0L8 2.71V9.5c0 .28-.22.5-.5.5S7 9.78 7 9.5V2.71z',
    pull: 'M4.85 6.15a.48.48 0 0 0-.7 0a.48.48 0 0 0 0 .7l3 3a.48.48 0 0 0 .7 0l3-3a.48.48 0 0 0 0-.7a.48.48 0 0 0-.7 0L8 8.29V1.5c0-.28-.22-.5-.5-.5s-.5.22-.5.5v6.79z'
}

export default {
    props: ['data', 'projectId', 'refreshing'],
    emits: ['busy', 'changed'],
    setup(props, { emit }) {
        const ctx = inject('ctx'), selected = ref(''), busy = ref(''), error = ref(''), message = ref('')
        let generation = 0, alive = true
        watch([() => props.data.repository, () => props.projectId], () => { generation++; error.value = ''; message.value = '' })
        watch(() => props.data.remotes, remotes => {
            if (!remotes?.some(remote => remote.name === selected.value)) selected.value = props.data.remote || remotes?.[0]?.name || ''
        }, { immediate: true })
        const remote = computed(() => props.data.remotes?.find(remote => remote.name === selected.value))
        const primary = computed(() => ({
            visible: !props.data.changes?.length && !props.data.stagedChanges?.length && !!remote.value && props.data.canSync &&
                (remote.value.ahead == null || remote.value.ahead > 0 || remote.value.behind > 0 || busy.value === 'sync'),
            busy: busy.value === 'sync', reason: reason('sync'), title: title('sync'),
            ahead: remote.value?.ahead, behind: remote.value?.behind,
        }))
        function reason(action) {
            if (busy.value || props.refreshing) return 'A Git operation is in progress'
            if (!remote.value) return 'Configure a Git remote to sync this repository'
            if (!props.data.canSync) return 'Create a commit and check out a branch before syncing'
            if (action !== 'pull' && !props.data.canPush) return 'Push requires local mode with your Git credentials'
            if (action !== 'push' && (props.data.changes?.length || props.data.stagedChanges?.length)) return 'Commit or stash your working changes before pulling'
            return ''
        }
        function title(action) {
            return reason(action) || (action === 'sync' ? 'Pull updates, then push commits to ' : action === 'pull' ? 'Pull updates from ' : 'Push commits to ') + remote.value.name + '/' + remote.value.branch
        }
        async function sync(action) {
            if (reason(action)) return
            const current = generation, destination = remote.value
            const body = { path: props.data.path, projectId: props.projectId || null, head: props.data.head,
                branch: props.data.branch, remote: destination.name, remoteBranch: destination.branch }
            busy.value = action; error.value = ''; message.value = ''; emit('busy', true)
            try {
                const api = await ctx.postJson('/ext/git/repositories/' + action, { body: JSON.stringify(body) })
                if (current !== generation) return
                if (!api.response) throw new Error(api.error?.message || 'Unable to ' + action + ' this repository')
                message.value = (action === 'sync' ? 'Synced with ' : action === 'push' ? 'Pushed to ' : 'Pulled from ') + destination.name + '/' + destination.branch
                emit('changed', action)
            } catch (e) { if (current === generation) { error.value = e.message; emit('changed', action) } }
            finally { if (alive) { busy.value = ''; emit('busy', false) } }
        }
        onUnmounted(() => { alive = false; generation++; emit('busy', false) })
        return { arrows, circle, selected, remote, busy, error, message, reason, title, sync, primary }
    },
    template: `<div class="mt-2" aria-label="Repository sync">
        <div class="flex items-center justify-between gap-1 min-w-0 w-full">
            <span v-if="!data.remotes?.length" class="truncate text-xs opacity-60">No remote configured</span>
            <select v-else-if="data.remotes?.length > 1" v-model="selected" :disabled="!!busy || refreshing" aria-label="Git remote" class="min-w-0 max-w-[120px] bg-transparent text-xs rounded py-1" :title="remote?.name + '/' + remote?.branch"><option v-for="entry in data.remotes" :value="entry.name">{{entry.name}}</option></select>
            <span v-else-if="remote" class="truncate text-xs opacity-60" :title="remote.name + '/' + remote.branch">{{remote.name}}</span>
            <div class="ml-auto flex items-center gap-1 shrink-0">
                <button v-for="action in ['pull', 'push']" :key="action" type="button" :aria-label="action === 'pull' ? 'Pull repository' : 'Push repository'" :title="title(action)" :disabled="!!reason(action)" :aria-busy="busy === action" @click="sync(action)"  :class="!reason(action) ? 'cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-800 hover:text-gray-900 dark:hover:text-white' : ''" data-git-sync-button class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-blue-500 focus-visible:outline-offset-1 cursor-pointer transition-colors hover:enabled:bg-[rgb(127_127_127_/_.15)] disabled:cursor-not-allowed disabled:opacity-40 shrink-0 flex items-center gap-1 rounded px-2 py-1 text-xs">
                    <svg v-if="busy === action" width="16" height="16" viewBox="0 0 24 24" class="animate-spin" aria-hidden="true"><circle cx="12" cy="12" r="9" fill="none" stroke="currentColor" stroke-width="2" opacity=".2"/><path d="M12 3a9 9 0 0 1 9 9" fill="none" stroke="currentColor" stroke-width="2"/></svg>
                    <svg v-else width="16" height="16" viewBox="0 0 16 16" aria-hidden="true"><path d="M0 0h16v16H0z" fill="none"/><g fill="currentColor"><path :d="arrows[action]"/><path :d="circle" fill-rule="evenodd" clip-rule="evenodd"/></g></svg>
                    <span>{{action === 'pull' ? 'Pull' : 'Push'}}</span><span v-if="remote && (action === 'pull' ? remote.behind : remote.ahead)" class="opacity-60">{{action === 'pull' ? remote.behind : remote.ahead}}</span>
                </button>
            </div>
        </div>
        <p v-if="busy" role="status" class="mt-1 text-xs opacity-70">{{busy === 'sync' ? 'Pulling updates and pushing commits…' : busy === 'push' ? 'Pushing commits…' : 'Pulling updates…'}}</p>
        <p v-else-if="error" role="alert" class="mt-1 text-xs text-red-600 dark:text-red-400 break-words">{{error}}</p>
        <p v-else-if="message" role="status" class="mt-1 text-xs opacity-70">{{message}}</p>
    </div>`
}
