import {
    ref,
    inject,
    computed,
    watch,
    nextTick,
    onUnmounted,
    defineAsyncComponent,
} from 'vue'
import StudioNotice from './StudioNotice.mjs'
import ShareSnapshotPreview, { formatRunDate } from './ShareSnapshotPreview.mjs'
import StudioIcon from './StudioIcon.mjs'
export default {
    components: {
        StudioNotice,
        ShareSnapshotPreview,
        StudioIcon,
        PublisherAccount: defineAsyncComponent(
            () => import('../share_llmspy/PublisherAccount.mjs'),
        ),
    },
    props: { open: Boolean, recipeId: String, dirty: Boolean, api: Function },
    emits: ['close', 'run', 'save-run', 'shared'],
    template: `<dialog ref="dialog" @cancel="$emit('close')" @click="backdrop" aria-labelledby="jev-share-title" aria-describedby="jev-share-description" data-jev-import-dialog data-jev-sharing-dialog class="fixed inset-0 m-auto w-[min(960px,_calc(100%_-_32px))] h-[90dvh] max-h-[90dvh] p-0 overflow-hidden border border-gray-200 dark:border-gray-700 rounded-2xl bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-sm shadow-xl backdrop:bg-[#10162680] backdrop:[backdrop-filter:blur(3px)] open:flex open:flex-col focus-visible:outline-2 focus-visible:outline-indigo-500 max-[620px]:w-[calc(100%_-_20px)] max-[620px]:h-[94dvh] max-[620px]:max-h-[94dvh]">
  <header class="relative flex shrink-0 items-center gap-4 pl-6 pr-16 py-5 border-b border-gray-200 dark:border-gray-700 max-[620px]:pl-4 max-[620px]:gap-3">
    <div class="min-w-0 flex-1"><h2 id="jev-share-title" class="text-xl font-semibold tracking-tight">Share recipe</h2><p id="jev-share-description" class="mt-1 text-xs text-slate-500 dark:text-slate-400">Review the snapshot before publishing to the Jev Recipes gallery.</p></div>
    <div v-if="state" class="flex justify-end shrink-0 max-w-[40%]">
      <span class="inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium" :class="state.publication&&!state.savedChangesNotShared?'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300':'bg-slate-200/70 text-slate-600 dark:bg-gray-800 dark:text-slate-300'"><StudioIcon v-if="state.publication&&!state.savedChangesNotShared" name="check" class="size-3.5 shrink-0"/>{{state.publication?(state.savedChangesNotShared?'Saved changes not shared':'Shared'):'Not shared'}}</span>
    </div>
    <button type="button" aria-label="Close recipe sharing" @click="$emit('close')" data-jev-dialog-close class="absolute top-2 right-2 grid place-items-center size-8 rounded-lg cursor-pointer text-slate-400 hover:bg-slate-100 dark:hover:bg-gray-800 hover:text-gray-800 dark:hover:text-gray-200 focus-visible:outline-2 focus-visible:outline-indigo-500 pointer-coarse:size-11"><StudioIcon name="close" class="size-5"/></button>
  </header>
  <div data-jev-sharing-body class="flex-1 min-h-0 overflow-y-auto overscroll-contain [scrollbar-width:thin] [scrollbar-color:var(--scrollbar-thumb-bg,_#d1d5db)_transparent] [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar]:bg-transparent [&::-webkit-scrollbar-thumb]:bg-[var(--scrollbar-thumb-bg,_#d1d5db)] [&::-webkit-scrollbar-thumb]:rounded-full">
    <div v-if="error" class="px-6 max-[620px]:px-4"><StudioNotice tone="error" :message="error"/></div>
    <template v-if="state">
      <div v-if="dirty||(!state.runs.length&&!state.pendingPublication)||state.pendingPublication||state.publication?.remoteChanged" class="px-6 max-[620px]:px-4">
        <StudioNotice v-if="dirty" tone="warning" message="Your editor has unsaved changes. This preview shows the saved version."><template #actions><button type="button" @click="$emit('save-run')" class="cursor-pointer rounded-lg border border-amber-300 dark:border-amber-800 px-3 py-2 text-xs font-medium hover:bg-amber-100 dark:hover:bg-amber-900 focus-visible:outline-2 focus-visible:outline-indigo-500">Save and run</button></template></StudioNotice>
        <StudioNotice v-if="!state.runs.length&&!state.pendingPublication" tone="warning" message="Run this recipe successfully before sharing it."><template #actions><button type="button" @click="$emit('run')" class="cursor-pointer rounded-lg border border-amber-300 dark:border-amber-800 px-3 py-2 text-xs font-medium hover:bg-amber-100 dark:hover:bg-amber-900 focus-visible:outline-2 focus-visible:outline-indigo-500">Return to Run</button></template></StudioNotice>
        <StudioNotice v-if="state.pendingPublication" tone="warning" message="A publication is awaiting confirmation. This is its captured snapshot; retry to recover it."/>
        <StudioNotice v-if="state.publication?.remoteChanged" tone="warning" message="The public snapshot changed. Review this preview before updating it."/>
      </div>
      <div v-if="!state.account.apiKey" class="px-6 py-4 border-b border-gray-200 dark:border-gray-700 max-[620px]:px-4"><PublisherAccount :account="state.account" @connected="load"/></div>
      <div class="grid min-h-full grid-cols-[260px_minmax(0,_1fr)] max-[760px]:grid-cols-1">
        <aside aria-label="Publishing options" class="min-w-0 space-y-6 p-6 bg-slate-50/70 dark:bg-gray-950/30 border-r border-gray-200 dark:border-gray-700 max-[760px]:border-r-0 max-[760px]:border-b max-[760px]:grid max-[760px]:grid-cols-2 max-[760px]:gap-4 max-[760px]:space-y-0 max-[620px]:p-4">
          <div>
            <h3 class="text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">Publishing</h3>
            <p class="mt-3 font-medium wrap-anywhere">{{state.recipe.filename}}</p>
          </div>
          <div v-if="state.account.apiKey" class="min-w-0"><p class="text-xs font-medium text-slate-500 dark:text-slate-400">Publisher</p><p class="mt-1 font-medium wrap-anywhere">{{state.account.userName}}</p><p class="mt-1 text-xs text-slate-500 dark:text-slate-400 wrap-anywhere">{{publisherHost}}</p></div>
          <div v-if="execution||state.runs.length" class="min-w-0 max-[760px]:col-span-2">
            <label v-if="state.runs.length" for="jev-share-run" class="block text-xs font-medium">Successful execution</label>
            <select v-if="state.runs.length" id="jev-share-run" v-model="runId" :disabled="busy||!!state.pendingPublication" class="mt-2 block w-full min-w-0 rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 px-3 py-2.5 text-xs cursor-pointer disabled:opacity-50 focus-visible:outline-2 focus-visible:outline-indigo-500 max-[620px]:text-base"><option v-for="run in state.runs" :key="run.id" :value="run.id" :title="run.execution.model">{{formatRunDate(run.execution.completedAt)}}</option></select>
            <p v-if="execution" class="mt-2 text-xs leading-relaxed text-slate-500 dark:text-slate-400 wrap-anywhere">{{execution.model}}</p>
          </div>
          <p v-if="document?.examples?.length" class="max-[760px]:col-span-2 text-xs leading-relaxed text-slate-500 dark:text-slate-400">{{document.examples.length}} usage {{document.examples.length===1?'example':'examples'}} included</p>
          <div class="flex items-center gap-2.5 rounded-xl border border-indigo-100 dark:border-indigo-900 bg-indigo-50/60 dark:bg-indigo-950/30 p-3 max-[760px]:col-span-2 text-xs leading-relaxed text-slate-600 dark:text-slate-300"><StudioIcon name="info" class="size-4 shrink-0 text-indigo-500 dark:text-indigo-300"/><p>This recipe will be public</p></div>
          <p v-if="usage?.publisherStarred" class="max-[760px]:col-span-2 flex items-center gap-2 text-xs text-amber-700 dark:text-amber-300"><StudioIcon name="star" class="size-4"/>Publisher favourite</p>
        </aside>
        <div class="flex flex-col min-w-0 p-6 max-[620px]:p-4"><ShareSnapshotPreview :document="document" :execution="execution" :snapshot="snapshot"/></div>
      </div>
    </template>
    <p v-else-if="!error" role="status" class="px-6 py-10 text-slate-500 dark:text-slate-400">Loading saved recipe…</p>
  </div>
  <footer v-if="state" class="shrink-0 border-t border-gray-200 dark:border-gray-700 px-6 py-4 bg-white dark:bg-gray-900 max-[620px]:px-4">
    <div v-if="url" class="min-w-0 mb-3 text-xs"><a :href="url" target="_blank" rel="noopener" class="block truncate text-indigo-600 dark:text-indigo-300 hover:underline focus-visible:outline-2 focus-visible:outline-indigo-500" :title="url">{{url}}</a></div>
    <div data-jev-actions class="flex justify-end gap-3">
      <div class="flex flex-wrap items-center justify-end gap-2">
        <button v-if="url" type="button" @click="copy" class="shrink-0 cursor-pointer py-2.5 text-xs font-medium text-slate-600 dark:text-slate-300 hover:text-indigo-600 dark:hover:text-indigo-300 focus-visible:outline-2 focus-visible:outline-indigo-500 pointer-coarse:min-h-11">{{copied?'Copied':'Copy link'}}</button><span class="sr-only" role="status">{{copied?'Link copied':''}}</span>
        <button v-if="state.publication" type="button" :disabled="busy" @click="remove" data-jev-button data-jev-danger class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed rounded-lg border border-gray-200 dark:border-gray-700 px-3 py-2.5 text-xs font-medium text-red-700 dark:text-red-300 hover:bg-red-50 dark:hover:bg-red-950/40 focus-visible:outline-2 focus-visible:outline-indigo-500 pointer-coarse:min-h-11">Stop sharing</button>
        <button type="button" :disabled="busy||!execution||!state.account.apiKey" @click="publish" data-jev-button data-jev-primary class="inline-flex items-center justify-center gap-2 cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed rounded-lg bg-indigo-600 px-4 py-2.5 text-xs font-medium text-white hover:bg-indigo-700 focus-visible:outline-2 focus-visible:outline-indigo-500 focus-visible:outline-offset-2 pointer-coarse:min-h-11"><StudioIcon name="share" class="size-4"/>{{busy?'Publishing…':state.pendingPublication?'Recover captured publication':state.publication?'Update public recipe':'Publish recipe'}}</button>
      </div>
    </div>
  </footer>
</dialog>`,
    setup(props, { emit }) {
        const ctx = inject('ctx')
        const dialog = ref(null),
            state = ref(null),
            runId = ref(''),
            error = ref(''),
            busy = ref(false),
            url = ref(''),
            copied = ref(false)
        let session = 0,
            disposed = false
        const execution = computed(
            () =>
                state.value?.pendingPublication?.payload?.execution ||
                state.value?.runs.find((r) => r.id === runId.value)?.execution,
        )
        const usage = computed(() => {
            const captured = state.value?.pendingPublication?.payload
            return captured
                ? Number.isInteger(captured.publisherRunCount)
                    ? captured
                    : null
                : state.value?.usage
        })
        const document = computed(() => {
            if (!state.value) return null
            if (state.value.pendingPublication?.payload)
                return state.value.pendingPublication.payload.document
            const d = JSON.parse(JSON.stringify(state.value.recipe.document))
            return d
        })
        const snapshot = computed(() => {
            if (!state.value) return null
            return state.value.pendingPublication?.payload || {
                filename: state.value.recipe.filename,
                document: document.value,
                execution: execution.value,
                ...state.value.usage,
            }
        })
        const publisherHost = computed(() => {
            try {
                return new URL(state.value?.account.baseUrl).host
            } catch {
                return state.value?.account.baseUrl || ''
            }
        })
        async function load() {
            const token = session,
                id = props.recipeId
            error.value = ''
            try {
                const result = await props.api(
                    '/recipes/' + encodeURIComponent(id) + '/share',
                )
                if (disposed || token !== session || id !== props.recipeId)
                    return
                state.value = result
                runId.value =
                    result.publication?.sourceRunId || result.runs[0]?.id || ''
                url.value = result.publication?.publishedUrl || ''
            } catch (e) {
                if (!disposed && token === session) error.value = e.message
            }
        }
        watch(
            () => [props.open, props.recipeId],
            async () => {
                session++
                state.value = null
                error.value = ''
                busy.value = false
                copied.value = false
                if (!props.open) {
                    dialog.value?.close()
                    return
                }
                await nextTick()
                if (!dialog.value.open) dialog.value.showModal()
                load()
            },
        )
        async function mutate(method, body) {
            const token = session,
                id = props.recipeId
            busy.value = true
            error.value = ''
            try {
                const r = await props.api(
                    '/recipes/' + encodeURIComponent(id) + '/share',
                    method,
                    body,
                )
                if (disposed || token !== session) return
                url.value = r.publishedUrl || ''
                emit('shared', id, r)
                if (method === 'POST') {
                    emit('close')
                    ctx.toast('Recipe shared')
                } else {
                    await load()
                }
            } catch (e) {
                if (!disposed && token === session) {
                    if (e.status === 409) await load()
                    error.value = e.message
                }
            } finally {
                if (!disposed && token === session) busy.value = false
            }
        }
        const publish = () =>
            mutate('POST', {
                revision: state.value.recipe.revision,
                runId: runId.value,
                publishedRevision: state.value.publication?.publicRevision,
            })
        const remove = () =>
            mutate('DELETE', {
                publishedRevision: state.value.publication.publicRevision,
            })
        async function copy() {
            try {
                await navigator.clipboard.writeText(url.value)
                copied.value = true
            } catch {
                error.value =
                    'Could not copy the link. Select it and copy manually.'
            }
        }
        function backdrop(event) {
            if (event.target !== dialog.value) return
            const rect = dialog.value.getBoundingClientRect()
            if (
                event.clientX < rect.left ||
                event.clientX > rect.right ||
                event.clientY < rect.top ||
                event.clientY > rect.bottom
            )
                emit('close')
        }
        onUnmounted(() => {
            disposed = true
            dialog.value?.close()
        })
        return {
            dialog,
            state,
            runId,
            error,
            busy,
            url,
            copied,
            usage,
            publisherHost,
            formatRunDate,
            document,
            snapshot,
            execution,
            load,
            publish,
            remove,
            copy,
            backdrop,
        }
    },
}
