import { computed, inject, ref, watch, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { directoryBreadcrumbs, workspaceQuery } from '../../../ui/modules/explorerState.mjs'
import { parseUnifiedDiff } from './diff.mjs'

export default {
    setup() {
        const ctx = inject('ctx'), route = useRoute(), router = useRouter()
        const data = ref(null), error = ref(''), loading = ref(false)
        let generation = 0
        onUnmounted(() => generation++)
        const rows = computed(() => parseUnifiedDiff(data.value?.patch))
        const added = computed(() => rows.value.filter(row => row.type === 'added').length)
        const removed = computed(() => rows.value.filter(row => row.type === 'removed').length)
        const breadcrumbs = computed(() => directoryBreadcrumbs(data.value?.repository, data.value?.roots.find(root =>
            data.value.repository === root || data.value.repository.startsWith(root.replace(/[\\/]+$/, '') + (root.includes('\\') ? '\\' : '/')))))
        const filename = computed(() => data.value?.file?.relativePath || String(route.query.workspaceFile || '').split(/[\\/]/).pop())
        const isCommit = computed(() => !!route.query.workspaceCommit)
        const isStaged = computed(() => route.query.workspacePreview === 'git-staged')
        const comparison = computed(() => isCommit.value
            ? data.value ? `${data.value.parent ? data.value.parent.slice(0, 7) : 'Empty tree'} → ${data.value.commit.slice(0, 7)}`
                : `Commit ${String(route.query.workspaceCommit).slice(0, 7)}`
            : isStaged.value ? 'Staged changes · HEAD → Index' : 'Unstaged changes · Index → Working tree')
        function navigate(path) {
            router.push({ query: workspaceQuery(route.query, { workspacePath: path, workspaceFile: null, workspacePreview: null, workspaceCommit: null }) })
        }
        function close() {
            router.push({ query: workspaceQuery(route.query, { workspaceFile: null, workspacePreview: null, workspaceCommit: null }) })
        }
        async function load() {
            const current = ++generation
            data.value = null; error.value = ''; loading.value = true
            const query = new URLSearchParams({ file: route.query.workspaceFile })
            if (route.query.workspacePath) query.set('path', route.query.workspacePath)
            if (route.query.workspaceProject) query.set('projectId', route.query.workspaceProject)
            if (route.query.workspaceCommit) query.set('commit', route.query.workspaceCommit)
            if (isStaged.value) query.set('staged', '1')
            try {
                const api = await ctx.getJson('/ext/git/diff?' + query)
                if (generation !== current) return
                if (!api.response) throw new Error(api.error?.message || 'Unable to load diff')
                data.value = api.response
            } catch (e) { if (generation === current) error.value = e.message }
            finally { if (generation === current) loading.value = false }
        }
        watch(() => [route.query.workspaceFile, route.query.workspacePath, route.query.workspaceProject, route.query.workspaceCommit, route.query.workspacePreview], load, { immediate: true })
        return { data, error, loading, rows, added, removed, breadcrumbs, filename, isCommit, isStaged, comparison, navigate, close, load }
    },
    template: `<section aria-label="Git diff" :aria-busy="loading"  :class="$styles.textBlock" data-git-diff class="h-full flex flex-col min-w-0">
        <div :class="[$styles.chromeBorder, $styles.bgSidebar]" data-git-diff-header class="pt-2 pr-4 pb-3 pl-4 shrink-0 border-b">
            <div class="flex items-center gap-2 min-w-0">
                <nav aria-label="Diff breadcrumbs" class="flex items-center gap-1 min-w-0 text-xs">
                    <template v-for="crumb in breadcrumbs" :key="crumb.path"><button type="button" @click="navigate(crumb.path)" :title="crumb.path"  :class="[$styles.muted, $styles.iconHover]"  class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-[var(--ring)] focus-visible:-outline-offset-0.5 truncate">{{crumb.name}}</button><span class="opacity-40">/</span></template>
                    <span class="font-mono truncate" :title="data?.file?.relativePath">{{filename}}</span>
                </nav>
                <div class="ml-auto flex items-center gap-1 shrink-0">
                    <button type="button" @click="load" :disabled="loading" aria-label="Refresh diff" title="Refresh diff"  :class="[$styles.icon, $styles.iconHover]" data-workspace-icon class="flex items-center justify-center w-7 h-7 p-1 border border-transparent rounded shrink-0 cursor-pointer disabled:opacity-45 disabled:cursor-default focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-[var(--ring)] focus-visible:-outline-offset-0.5"><svg viewBox="0 0 24 24" aria-hidden="true"  class="w-4.5 h-4.5"><path fill="currentColor" fill-rule="evenodd" d="M10.546 5.132L8.828 3.414L10.243 2l3.889 3.89a.5.5 0 0 1 0 .706l-3.89 3.89L8.829 9.07l1.946-1.946a6 6 0 1 0 5.468 1.632l1.415-1.414a8 8 0 1 1-7.11-2.211z"/></svg></button>
                    <button type="button" @click="close" aria-label="Close diff" title="Close diff"  :class="[$styles.icon, $styles.iconHover]" data-workspace-icon class="flex items-center justify-center w-7 h-7 p-1 border border-transparent rounded shrink-0 cursor-pointer disabled:opacity-45 disabled:cursor-default focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-[var(--ring)] focus-visible:-outline-offset-0.5"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true"  class="w-4.5 h-4.5"><path d="m6 6 12 12M6 18 18 6"/></svg></button>
                </div>
            </div>
            <div class="flex items-center gap-3 text-xs mt-1">
                <span :class="$styles.muted">{{comparison}}</span>
                <span v-if="data?.patch"  :aria-label="added + ' lines added'" data-git-diff-added-count class="text-[#1a7f37] dark:text-[#86efac]">+{{added}}</span>
                <span v-if="data?.patch"  :aria-label="removed + ' lines removed'" data-git-diff-removed-count class="text-[#b42318] dark:text-[#fca5a5]">−{{removed}}</span>
            </div>
        </div>
        <div tabindex="0" aria-label="Diff lines"  class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-[var(--ring)] focus-visible:-outline-offset-0.5 flex-1 overflow-auto min-h-0">
            <p v-if="loading" role="status" class="p-4 text-sm">Loading diff…</p>
            <p v-else-if="error" role="alert" class="p-4 text-sm">{{error}} <button type="button" @click="load"   class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-[var(--ring)] focus-visible:-outline-offset-0.5 underline">Retry</button></p>
            <p v-else-if="data?.message" class="p-4 text-sm" :class="$styles.muted">{{data.message}}</p>
            <table v-else-if="rows.length"  :aria-label="isCommit ? 'Committed file diff' : isStaged ? 'Staged file diff' : 'Unstaged file diff'" data-git-diff-table class="w-full border-collapse [font-family:var(--font-mono)] text-[13px] leading-5.5">
                <tbody>
                    <tr v-for="(row, index) in rows" :key="index"  :data-diff-type="row.type" :class="{ 'bg-[#e6ffec] text-[#1a7f37] dark:bg-[#122d23] dark:text-green-300': row.type === 'added', 'bg-[#ffebe9] text-[#b42318] dark:bg-[#3c2025] dark:text-red-300': row.type === 'removed', 'bg-blue-50 text-slate-600 dark:bg-slate-800 dark:text-slate-400': row.type === 'hunk', 'text-gray-500 dark:text-slate-400 italic': row.type === 'meta' }">
                        <template v-if="row.type === 'hunk' || row.type === 'meta'"><td colspan="4"  data-git-diff-note class="border-0 align-top py-1 px-4 whitespace-pre">{{row.text}}</td></template>
                        <template v-else>
                            <td :aria-label="isCommit ? 'Parent line' : isStaged ? 'HEAD line' : 'Index line'" data-git-diff-number class="border-0 align-top w-[1%] min-w-12 py-0 px-2.5 text-right text-[#6b7280] select-none dark:text-slate-400">{{row.oldLine}}</td>
                            <td :aria-label="isCommit ? 'Commit line' : isStaged ? 'Index line' : 'Working tree line'" data-git-diff-number class="border-0 align-top w-[1%] min-w-12 py-0 px-2.5 text-right text-[#6b7280] select-none dark:text-slate-400">{{row.newLine}}</td>
                            <td :aria-label="row.type === 'added' ? 'Added' : row.type === 'removed' ? 'Removed' : 'Unchanged'" data-git-diff-sign class="border-0 align-top w-[1%] min-w-6 py-0 px-1 text-center select-none">{{row.prefix}}</td>
                            <td data-git-diff-source class="border-0 align-top pt-0 pr-4 pb-0 pl-1 whitespace-pre [tab-size:4]">{{row.text}}</td>
                        </template>
                    </tr>
                </tbody>
            </table>
        </div>
    </section>`
}
