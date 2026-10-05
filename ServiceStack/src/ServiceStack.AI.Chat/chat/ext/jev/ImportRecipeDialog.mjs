import { loadDecisionTags } from './decisionTags.mjs'
import StudioNotice from './StudioNotice.mjs'
import { ref, computed, watch, nextTick, onUnmounted } from 'vue'
import StudioIcon from './StudioIcon.mjs'
import RecipeSearch from './RecipeSearch.mjs'

export default {
    components: { StudioNotice, StudioIcon, RecipeSearch },
    props: { open: Boolean, api: Function, cacheScope: String },
    emits: ['close', 'imported', 'file'],
    template: `<dialog ref="dialog"  :class="tab==='From JSON'||replacement ? 'h-fit overflow-auto open:block' : 'h-[min(720px,85vh)] overflow-hidden open:flex open:flex-col'" aria-labelledby="jev-import-title" @cancel="$emit('close')" @click="backdrop" data-jev-import-dialog class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 fixed inset-0 m-auto border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 rounded-2xl shadow-xl p-6.5 max-h-[85vh] w-[min(720px,_calc(100%_-_32px))] text-sm backdrop:bg-[#10162680] backdrop:[backdrop-filter:blur(3px)] max-[620px]:p-4.5">
      <header data-jev-section-heading class="flex items-start justify-between gap-3 flex-nowrap mb-4.5 max-[620px]:gap-2.5 shrink-0"><div class="flex-1 min-w-0 pr-8"><h2 id="jev-import-title"  class="mb-3 text-[21px] font-semibold tracking-[-0.6px] max-[620px]:text-[23px]">Import recipe</h2><p data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.7] mt-1">Choose a recipe to add to your library.</p></div><button type="button"  aria-label="Close recipe import" @click="$emit('close')" data-jev-dialog-close class="absolute top-2 right-2 grid place-items-center size-8 rounded-lg cursor-pointer text-slate-400 hover:bg-slate-100 dark:hover:bg-gray-800 hover:text-gray-800 dark:hover:text-gray-200 focus-visible:outline-2 focus-visible:outline-indigo-500 pointer-coarse:size-11"><StudioIcon name="close" class="size-5"/></button></header>
      <nav data-jev-tabs class="flex gap-[21px] max-[1100px]:gap-[15px] max-[620px]:gap-4.5 max-[620px]:w-full max-[620px]:justify-between"><button v-for="name in ['Collection','From JSON']" type="button" :aria-current="tab===name?'page':undefined" @click="switchTab(name)"  class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 flex items-center gap-1.75 py-3 px-0 border-0 border-b-2 border-b-transparent bg-transparent text-[13px] text-slate-500 dark:text-slate-400 aria-[current=page]:border-b-indigo-600 aria-[current=page]:dark:border-b-indigo-300 aria-[current=page]:text-indigo-600 aria-[current=page]:dark:text-indigo-300 aria-[current=page]:font-medium max-[620px]:text-xs max-[620px]:gap-1.25 pointer-coarse:min-h-11">{{name}}</button></nav>
      <form v-if="tab==='From JSON'&&!replacement" @submit.prevent="importUrl()" class="mt-4">
        <label for="jev-import-url" class="block text-xs font-medium mb-1.5 text-gray-800 dark:text-gray-200">JSON URL</label>
        <div class="flex items-stretch gap-2">
          <input id="jev-import-url" v-model="link" type="url" required placeholder="Recipe JSON URL or share link" class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 flex-1 min-w-0 bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 border border-gray-200 dark:border-gray-700 rounded-[7px] py-[9px] px-2.5 text-[13px] font-normal max-[620px]:text-base"/>
          <button type="submit" :disabled="!!busy" data-jev-url-import data-jev-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 shrink-0 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-xs font-medium py-2 px-3 whitespace-nowrap text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 pointer-coarse:min-h-11">{{busy?'Importing…':'Import'}}</button>
        </div>
        <a v-if="openUrl" :href="openUrl" target="_blank" rel="noopener" class="inline-block mt-2 text-xs text-indigo-600 dark:text-indigo-300 no-underline hover:text-indigo-800 dark:hover:text-indigo-200 focus-visible:outline-2 focus-visible:outline-indigo-500 focus-visible:outline-offset-2">Open URL</a>
      </form>
      <section v-if="replacement" data-jev-import-conflict class="mt-4"><StudioNotice tone="warning" :message="'Replacing '+replacement.id+' clears its old history. Its existing public share remains available and can be removed separately in My recipes.'"/><label class="block text-xs font-medium mb-3.5 text-gray-800 dark:text-gray-200">Import filename<input v-model="filename"  class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 block w-full min-w-0 mt-1.5 bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 border border-gray-200 dark:border-gray-700 rounded-[7px] py-[9px] px-2.5 text-[13px] font-normal max-[620px]:text-base"/></label><div data-jev-actions class="flex items-center gap-2.5 flex-wrap mt-5"><button type="button" :disabled="!!busy" @click="importSaved(true)" data-jev-button data-jev-primary class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-indigo-500 focus-visible:outline-offset-2 inline-flex items-center justify-center gap-1.5 rounded-lg text-xs font-medium py-2 px-3 pointer-coarse:min-h-11 bg-indigo-600 text-white hover:bg-indigo-700">Replace &amp; clear history</button><button type="button" :disabled="!!busy" @click="importCopy" data-jev-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-indigo-500 focus-visible:outline-offset-2 inline-flex items-center justify-center gap-1.5 rounded-lg text-xs font-medium py-2 px-3 pointer-coarse:min-h-11 border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 hover:bg-slate-100 dark:hover:bg-slate-800">Import as a copy</button><button type="button" :disabled="!!busy" @click="cancelReplacement" data-jev-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-indigo-500 focus-visible:outline-offset-2 inline-flex items-center justify-center gap-1.5 rounded-lg text-xs font-medium py-2 px-3 pointer-coarse:min-h-11 border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 hover:bg-slate-100 dark:hover:bg-slate-800">Cancel</button></div></section>
      <RecipeSearch v-if="tab!=='From JSON'&&!replacement" ref="searchField" v-model="search" label="Search recipe collection" @search="load()" class="mt-4 mb-3.5 max-[620px]:my-2"/>
      <div v-if="tab==='Collection'&&!replacement"  data-jev-field-pair class="grid grid-cols-2 gap-4 max-[850px]:grid-cols-1"><label class="block text-xs font-medium mb-3.5 text-gray-800 dark:text-gray-200">Tag<input v-model="tag" list="jev-collection-tags" placeholder="All tags" @change="filterChanged"  class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 block w-full min-w-0 mt-1.5 bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 border border-gray-200 dark:border-gray-700 rounded-[7px] py-[9px] px-2.5 text-[13px] font-normal max-[620px]:text-base"/><datalist id="jev-collection-tags"><option v-for="item in tags" :key="item.label" :value="item.label"></option></datalist></label><label class="block text-xs font-medium mb-3.5 text-gray-800 dark:text-gray-200">Sort<select v-model="order" @change="filterChanged"  class="cursor-pointer focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 block w-full min-w-0 mt-1.5 bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 border border-gray-200 dark:border-gray-700 rounded-[7px] py-[9px] px-2.5 text-[13px] font-normal max-[620px]:text-base"><option value="recommended">Recommended</option><option value="most-run">Most run</option><option value="newest">Newest</option><option value="name">Name</option></select></label></div>
      <StudioNotice v-if="error" tone="error" :message="error"><template #actions><button v-if="!items.length" type="button"  @click="tab==='From JSON'?importUrl():load()" data-jev-text-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 text-indigo-600 dark:text-indigo-300 bg-transparent border-0 text-xs text-left underline underline-offset-3">Try again</button></template></StudioNotice>
      <div v-if="tab!=='From JSON'&&!replacement"  :aria-busy="loading||!!busy" data-jev-import-list class="min-h-0 overflow-y-auto flex flex-col gap-2.5 p-0.75 m-[-3px]"><p v-if="loading"  role="status" data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.7]">Loading recipes…</p>
        <article v-for="item in filtered" :key="item.externalRef||item.id" data-jev-import-card class="flex items-center justify-between gap-5 p-4 border border-gray-200 dark:border-gray-700 rounded-[10px] bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-left hover:bg-slate-50 hover:dark:bg-gray-800 hover:border-indigo-600 hover:dark:border-indigo-300 max-[620px]:items-start max-[620px]:flex-col max-[620px]:gap-2.5 max-[620px]:p-[13px]"><span data-jev-import-card-content class="flex flex-col gap-1.5 min-w-0"><strong class="text-sm font-semibold">{{item.name}}</strong><span data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.6]">{{item.description}}</span><span data-jev-recipe-meta class="block text-[11px] text-slate-500 dark:text-slate-400 mt-2 max-[620px]:mt-1">{{item.questionCount}} {{item.questionCount===1?'question':'questions'}} · {{item.inputCount??item.fieldCount}} {{item.inputCount===1?'input':'inputs'}}<span v-if="item.content"> · {{item.content}}</span><span v-if="item.tags?.length"> · {{item.tags.join(' · ')}}</span></span></span><span class="flex items-center gap-3 shrink-0"><button type="button" :disabled="!!starring||!!busy" @click="toggleStar(item)" data-jev-collection-star :aria-pressed="!!item.starred" :aria-label="item.starred?'Remove your star':'Star this recipe'" :title="item.starred?'Remove your star':'Star this recipe'" class="inline-flex items-center gap-1.5 rounded px-2 py-1 text-xs cursor-pointer disabled:opacity-50 disabled:cursor-wait text-slate-500 dark:text-slate-400 aria-pressed:text-amber-700 dark:aria-pressed:text-amber-300 hover:bg-slate-100 dark:hover:bg-slate-800 focus-visible:outline-2 focus-visible:outline-indigo-500 focus-visible:outline-offset-2"><StudioIcon name="star" class="size-4" :class="item.starred?'fill-current':''"/>{{item.starCount??(item.publisherStarred?1:0)}}</button><a :href="item.publishedUrl" target="_blank" rel="noopener" data-jev-import-view class="text-xs text-indigo-600 dark:text-indigo-300 no-underline hover:text-indigo-800 dark:hover:text-indigo-200 focus-visible:outline-2 focus-visible:outline-indigo-500 focus-visible:outline-offset-2">View</a><button type="button" :disabled="!!busy" @click="choose(item)" data-jev-import-card-action class="inline-flex items-center gap-1.5 shrink-0 cursor-pointer disabled:opacity-60 disabled:cursor-wait text-indigo-600 dark:text-indigo-300 text-xs whitespace-nowrap rounded px-2 py-1 hover:bg-indigo-50 dark:hover:bg-indigo-950 focus-visible:outline-2 focus-visible:outline-indigo-500 focus-visible:outline-offset-2"><span v-if="busy===(item.externalRef||item.id)" data-jev-spinner class="inline-block size-4 border-2 border-gray-200 dark:border-gray-700 border-t-indigo-600 dark:border-t-indigo-300 rounded-full animate-spin shrink-0 motion-reduce:animate-none"/><StudioIcon v-else name="plus" class="size-4.5"/><span>{{busy===(item.externalRef||item.id)?'Importing…':'Import'}}</span></button></span></article>
        <button v-if="tab==='Collection'&&hasMore" :disabled="loading||!!busy" @click="load(true)" data-jev-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 pointer-coarse:min-h-11">More recipes</button><p v-if="!loading&&!filtered.length&&!error"  data-jev-help data-jev-library-empty class="text-xs text-slate-500 dark:text-slate-400 leading-[1.7] py-5 px-3">No recipes match your search.</p>
      </div>
      <footer v-if="tab==='From JSON'&&!replacement" class="flex items-center justify-between gap-3 pt-4 mt-4 border-t border-t-gray-200 dark:border-t-gray-700 shrink-0 max-[620px]:items-start max-[620px]:flex-col"><span data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.6]">Have a recipe JSON file?</span><button type="button" :disabled="!!busy" @click="uploadFile" data-jev-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 pointer-coarse:min-h-11"><StudioIcon name="import"  class="size-4 shrink-0"/>Import from file</button></footer>
    </dialog>`,
    setup(props, { emit }) {
        const dialog = ref(null),
            searchField = ref(null),
            search = ref(''),
            items = ref([]),
            loading = ref(false),
            busy = ref(''),
            error = ref(''),
            starring = ref('')
        const tab = ref('Collection'),
            link = ref(''),
            source = ref(null),
            filename = ref(''),
            replacement = ref(null),
            hasMore = ref(false)
        const tags = ref([]),
            tag = ref(''),
            order = ref('recommended')
        let loadGeneration = 0,
            session = 0,
            disposed = false
        const filtered = computed(() =>
            items.value.filter((item) =>
                [
                    item.name,
                    item.description,
                    item.content || '',
                    ...(item.tags || []),
                ]
                    .join(' ')
                    .toLowerCase()
                    .includes(search.value.trim().toLowerCase()),
            ),
        )
        const openUrl = computed(() => {
            try {
                const url = new URL(link.value.trim())
                if (!['https:', 'http:'].includes(url.protocol)) return ''
                return url.href
            } catch { return '' }
        })
        async function load(more = false) {
            const token = session,
                request = ++loadGeneration
            loading.value = true
            error.value = ''
            try {
                const result = await props.api(
                    '/shared-recipes?' +
                    new URLSearchParams({
                        q: search.value,
                        tag: tag.value,
                        orderBy: order.value,
                        skip: more ? items.value.length : 0,
                        take: 20,
                    }),
                )
                if (
                    !disposed &&
                    token === session &&
                    request === loadGeneration
                ) {
                    items.value = more
                        ? [...items.value, ...result.items]
                        : result.items
                    hasMore.value = !!result.hasMore
                }
            } catch (e) {
                if (
                    !disposed &&
                    token === session &&
                    request === loadGeneration
                )
                    error.value = e.message
            } finally {
                if (
                    !disposed &&
                    token === session &&
                    request === loadGeneration
                )
                    loading.value = false
            }
        }
        watch(
            () => props.open,
            async (open) => {
                session++
                if (!open) {
                    dialog.value?.close()
                    return
                }
                search.value = ''
                link.value = ''
                error.value = ''
                tag.value = ''
                order.value = 'recommended'
                source.value = null
                replacement.value = null
                tab.value = 'Collection'
                items.value = []
                busy.value = ''
                await nextTick()
                dialog.value?.showModal()
                searchField.value?.focus()
                loadTags()
                load()
            },
        )
        async function toggleStar(item) {
            if (starring.value || busy.value) return
            const reference = item.externalRef || item.id, token = session
            starring.value = reference
            error.value = ''
            try {
                const state = await props.api('/shared-recipes/' + encodeURIComponent(reference) + '/star', 'PUT', { starred: !item.starred })
                if (!disposed && token === session) Object.assign(item, state)
            } catch (e) {
                if (!disposed && token === session) error.value = e.message
            } finally { starring.value = '' }
        }
        async function choose(item) {
            if (busy.value) return
            link.value = item.publishedUrl
            await importUrl(item.externalRef || item.id)
        }
        function loadTags() {
            const token = session
            tags.value = []
            loadDecisionTags(props.api, props.cacheScope).then((catalog) => {
                if (!disposed && token === session) tags.value = catalog.tags
            })
        }
        function switchTab(name) {
            session++
            tab.value = name
            source.value = null
            replacement.value = null
            items.value = []
            error.value = ''
            busy.value = ''
            search.value = ''
            if (name === 'Collection') loadTags()
            if (name !== 'From JSON') load()
        }
        function filterChanged() {
            session++
            items.value = []
            hasMore.value = false
            load()
        }
        async function importUrl(identity = 'url') {
            if (busy.value) return
            const token = session
            busy.value = identity
            error.value = ''
            replacement.value = null
            try {
                const detail = await props.api('/recipes/preview-url', 'POST', { url: link.value })
                if (disposed || token !== session || !props.open) return
                source.value = detail
                filename.value = detail.filename
                await saveSource(token)
            } catch (e) {
                if (!disposed && token === session) error.value = e.message
            } finally {
                if (!disposed && token === session) busy.value = ''
            }
        }
        async function saveSource(token, replace = false) {
            const detail = source.value
            try {
                const row = await props.api(detail.externalRef ? '/shared-recipes/import' : '/recipes', 'POST', {
                    ...(detail.externalRef ? {
                        externalRef: detail.externalRef,
                        publishedRevision: detail.revision,
                        contentHash: detail.contentHash,
                    } : { document: detail.document }),
                    filename: filename.value,
                    ...(replace ? { replaceRevision: replacement.value.revision } : {}),
                })
                if (!disposed) {
                    const activate = token === session && props.open
                    emit('imported', row, activate)
                    if (activate) emit('close')
                }
            } catch (e) {
                if (!disposed && token === session) {
                    if (e.code === 'RecipeExistsError' && e.existingRecipe)
                        replacement.value = e.existingRecipe
                    else {
                        replacement.value = null
                        error.value = e.message
                    }
                }
            }
        }
        async function importSaved(replace = false) {
            if (busy.value || !source.value) return
            const token = session
            busy.value = 'import'
            error.value = ''
            try { await saveSource(token, replace) }
            finally { if (!disposed && token === session) busy.value = '' }
        }
        function importCopy() {
            filename.value = filename.value.replace(/\.json$/i, '') + ' (copy).json'
            replacement.value = null
            importSaved()
        }
        function cancelReplacement() {
            source.value = null
            replacement.value = null
            error.value = ''
        }
        function uploadFile() {
            emit('close')
            emit('file')
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
            tags,
            tag,
            order,
            filterChanged,
            tab,
            link,
            filename,
            replacement,
            hasMore,
            switchTab,
            openUrl,
            importUrl,
            importSaved,
            importCopy,
            cancelReplacement,
            dialog,
            searchField,
            search,
            filtered,
            items,
            loading,
            busy,
            error,
            load,
            choose,
            toggleStar,
            starring,
            backdrop,
            uploadFile,
        }
    },
}
