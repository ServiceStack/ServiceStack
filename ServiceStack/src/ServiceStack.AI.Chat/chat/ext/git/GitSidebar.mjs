import { inject, ref, watch, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { workspaceQuery } from '../../../ui/modules/explorerState.mjs'
import GitHistory from './GitHistory.mjs'

export default {
    components: { GitHistory },
    props: ['workspace', 'projectId', 'refreshKey'],
    emits: ['file', 'busy'],
    setup(props, { emit }) {
        const ctx = inject('ctx'), data = ref(null), error = ref(''), loading = ref(false)
        const completionRefresh = ref(0)
        const route = useRoute(), router = useRouter()
        let generation = 0
        onUnmounted(() => { generation++; emit('busy', false) })
        async function load() {
            const current = ++generation
            if (!props.workspace?.path) { data.value = null; loading.value = false; emit('busy', false); return }
            error.value = ''; loading.value = true; emit('busy', true)
            const query = new URLSearchParams({ path: props.workspace.path })
            if (props.projectId) query.set('projectId', props.projectId)
            try {
                const api = await ctx.getJson('/ext/git/workspace?' + query)
                if (current !== generation) return
                if (!api.response) throw new Error(api.error?.message || 'Unable to load Git information')
                data.value = api.response
            } catch (e) { if (current === generation) error.value = e.message }
            finally { if (current === generation) { loading.value = false; emit('busy', false) } }
        }
        watch(() => [props.workspace?.path, props.projectId], () => { data.value = null; load() }, { immediate: true })
        watch(() => props.refreshKey, load)
        // This component exists only while the Git view is open. Both SSE and long-poll
        // update currentThread, so completion needs no additional subscription or polling.
        watch(() => {
            const thread = ctx.threads?.currentThread?.value
            return [thread?.id, thread?.projectId || null, !!(thread?.completedAt || thread?.error)]
        }, ([id, projectId, completed], [previousId, previousProject, wasCompleted]) => {
            if (id && id === previousId && projectId === previousProject && projectId === (props.projectId || null)
                    && completed && !wasCompleted) {
                completionRefresh.value++
                load()
            }
        }, { flush: 'sync' })
        async function changed(action) {
            const path = props.workspace.path, projectId = props.projectId
            await load()
            if (props.workspace.path !== path || props.projectId !== projectId || route.query.workspaceCommit) return
            if (!['git', 'git-staged'].includes(route.query.workspacePreview)) return
            const file = route.query.workspaceFile
            const staged = data.value?.stagedChanges?.some(entry => entry.path === file)
            const working = data.value?.changes?.some(entry => entry.path === file)
            const preview = action === 'stage' && staged ? 'git-staged' : working ? 'git' : staged ? 'git-staged' : null
            router.replace({ query: workspaceQuery(route.query, { workspacePreview: preview, workspaceFile: preview ? file : null }) })
        }
        return { data, error, loading, load, route, changed, completionRefresh }
    },
    template: `<div :aria-busy="loading">
        <p v-if="loading && !data" role="status"  data-workspace-message class="p-2 text-xs opacity-80">Loading Git information…</p>
        <div v-if="error" role="alert"  data-workspace-message class="p-2 text-xs opacity-80">{{error}} <button type="button" @click="load"   class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-[var(--ring)] focus-visible:-outline-offset-0.5 underline">Retry</button></div>
        <GitHistory v-else-if="data?.repository" :data="data" :refreshing="loading" :selected-file="route.query.workspaceFile" :selected-commit="route.query.workspaceCommit" :selected-preview="route.query.workspacePreview" :project-id="projectId" :refresh-key="(refreshKey || 0) + completionRefresh" @changed="changed" @busy="$emit('busy', $event)" @file="$emit('file', { ...$event, preview: $event.preview || 'git', directoryPath: workspace.path })" />
        <div v-else-if="data && !loading"  data-workspace-empty class="py-6 px-3 text-center">
            <p class="font-medium">No Git repository here</p>
            <p class="mt-1 text-xs opacity-70">Open a repository folder in Files to see its changes and history.</p>
        </div>
    </div>`
}
