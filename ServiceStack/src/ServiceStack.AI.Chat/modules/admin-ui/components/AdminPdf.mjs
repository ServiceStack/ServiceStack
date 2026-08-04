// Keep this C#-owned copy in sync with src/ServiceStack.AI.Chat/modules/admin-ui/components/AdminPdf.mjs.
import { computed, inject, nextTick, onMounted, onUnmounted, ref, shallowRef, watch } from 'vue'
import { useClient } from '@servicestack/vue'

class AdminPdfTemplates { getTypeName() { return 'AdminPdfTemplates' }; getMethod() { return 'GET' }; createResponse() { return {} } }
class AdminGetPdfTemplate { constructor(init) { Object.assign(this, init) }; getTypeName() { return 'AdminGetPdfTemplate' }; getMethod() { return 'GET' }; createResponse() { return {} } }
class AdminDeletePdfTemplate { constructor(init) { Object.assign(this, init) }; getTypeName() { return 'AdminDeletePdfTemplate' }; getMethod() { return 'POST' }; createResponse() { return {} } }
class AdminEditPdfTemplate { constructor(init) { Object.assign(this, init) }; getTypeName() { return 'AdminEditPdfTemplate' }; getMethod() { return 'POST' }; createResponse() { return {} } }

const json = value => {
    try { return value ? JSON.parse(value) : {} } catch { return {} }
}

export const AdminPdf = {
    template: `
    <div class="max-w-screen-2xl mx-auto p-4 sm:p-6">
        <div class="flex flex-wrap items-center gap-3 border-b border-gray-200 dark:border-gray-700 pb-4 mb-4">
            <div class="flex-1 min-w-48">
                <h1 class="text-xl font-semibold text-gray-900 dark:text-gray-100">{{ selected?.name || 'PDF Templates' }}</h1>
                <p v-if="selected?.publishedBy" class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">Published by {{ selected.publishedBy }}{{ selected.publishedAt ? ' · ' + formatDate(selected.publishedAt) : '' }}</p>
                <p v-else class="text-sm text-gray-500 dark:text-gray-400">Published PDF templates</p>
            </div>
            <button type="button" @click="openDialog = true" class="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium rounded-md border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-200 bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-700 shadow-sm transition-colors" title="Open published template">
                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M3.75 9.776c.112-.017.227-.026.344-.026h15.812c.117 0 .232.009.344.026m-16.5 0a2.25 2.25 0 0 0-1.883 2.542l.857 6a2.25 2.25 0 0 0 2.227 1.932H19.05a2.25 2.25 0 0 0 2.227-1.932l.857-6a2.25 2.25 0 0 0-1.883-2.542m-16.5 0V6A2.25 2.25 0 0 1 6 3.75h3.879a1.5 1.5 0 0 1 1.06.44l2.122 2.12a1.5 1.5 0 0 0 1.06.44H18A2.25 2.25 0 0 1 20.25 9v.776"/></svg>
                Open
            </button>
            <button v-if="selected?.editUrl" type="button" @click="edit" :disabled="editing" class="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium rounded-md border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-200 bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-700 shadow-sm transition-colors disabled:opacity-50">
                <svg v-if="editing" class="w-4 h-4 animate-spin" viewBox="0 0 24 24" fill="none"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"/></svg>
                <svg v-else class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="m16.862 4.487 1.687-1.688a1.875 1.875 0 1 1 2.652 2.652L10.582 16.07a4.5 4.5 0 0 1-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 0 1 1.13-1.897l8.932-8.931Zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0 1 15.75 21H5.25A2.25 2.25 0 0 1 3 18.75V8.25A2.25 2.25 0 0 1 5.25 6H10"/></svg>
                {{ editing ? 'Opening…' : 'Edit' }}
            </button>
            <button v-if="selected" type="button" @click="unpublish" class="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium rounded-md text-red-600 dark:text-red-400 border border-red-300 dark:border-red-700 hover:bg-red-50 dark:hover:bg-red-950/40 shadow-sm transition-colors">
                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="m14.74 9-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 0 1-2.244 2.077H8.084a2.25 2.25 0 0 1-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 0 0-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 0 1 3.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 0 0-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 0 0-7.5 0"/></svg>
                Unpublish
            </button>
        </div>

        <div v-if="error" class="mb-4 rounded-lg border border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-950/30 p-3 text-sm text-red-700 dark:text-red-300">
            {{ error.message || error }}
            <details v-if="error.stackTrace" class="mt-2"><summary class="cursor-pointer text-red-500 hover:text-red-700">Details</summary><pre class="mt-2 whitespace-pre-wrap text-xs text-red-600 dark:text-red-400">{{ error.stackTrace }}</pre></details>
        </div>
        <div v-if="!typstAvailable" class="mb-4 rounded-lg border border-amber-200 dark:border-amber-800 bg-amber-50 dark:bg-amber-950/30 p-3 text-sm text-amber-700 dark:text-amber-300">
            <span class="font-medium">typst not found.</span> Templates can be browsed and unpublished, but rendering requires typst on PATH.
        </div>
        <section v-if="!selected && !loading" class="rounded-lg border border-gray-200 dark:border-gray-700 p-4 sm:p-5">
            <div class="flex flex-col sm:flex-row sm:items-center gap-3 mb-5">
                <div class="flex-1"><h2 class="font-medium text-gray-900 dark:text-gray-100">Select a template</h2><p class="text-sm text-gray-500 dark:text-gray-400">Search published templates or <a :href="chatBase + '/pdf'" class="text-indigo-600 dark:text-indigo-400 hover:underline">create one in PDF Studio</a>.</p></div>
                <div class="flex gap-2"><input v-model="search" class="w-full sm:w-64 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 px-3 py-2 text-sm focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 dark:focus:border-indigo-400 dark:focus:ring-indigo-400 outline-none transition-shadow" placeholder="Search templates…"><select v-model="sort" class="rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 px-2 py-2 text-sm"><option value="name">Name</option><option value="modified">Modified</option><option value="size">Size</option></select></div>
            </div>
            <div v-if="!filtered.length" class="py-12 text-center"><svg class="mx-auto h-10 w-10 text-gray-300 dark:text-gray-600" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 0 0-3.375-3.375h-1.5A1.125 1.125 0 0 1 13.5 7.125v-1.5a3.375 3.375 0 0 0-3.375-3.375H8.25m3.75 9v6m3-3H9m1.5-12H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 0 0-9-9Z"/></svg><p class="mt-2 text-sm text-gray-500 dark:text-gray-400">No published templates match your search.</p></div>
            <div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                <button type="button" v-for="t in filtered" :key="t.name" @click="select(t.name)" class="group relative aspect-[4/3] overflow-hidden rounded-lg bg-gray-100 dark:bg-gray-800 text-left shadow-sm ring-1 ring-gray-200 dark:ring-gray-700 hover:ring-2 hover:ring-indigo-500 dark:hover:ring-indigo-400 transition-all">
                    <img v-if="t.previewUrl" :src="t.previewUrl" loading="lazy" class="h-full w-full object-contain transition-transform duration-200 group-hover:scale-[1.02]"><span v-else class="grid h-full place-items-center"><svg class="w-12 h-12 text-gray-300 dark:text-gray-600" fill="none" viewBox="0 0 24 24" stroke-width="1" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 0 0-3.375-3.375h-1.5A1.125 1.125 0 0 1 13.5 7.125v-1.5a3.375 3.375 0 0 0-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 0 0-9-9Z"/></svg></span>
                    <span class="absolute inset-x-0 bottom-0 bg-gradient-to-t from-gray-950/80 to-transparent px-3 pt-6 pb-2 text-white"><span class="block truncate text-sm font-medium">{{ t.name }}</span><span class="block text-xs text-gray-300">{{ humanifyBytes(t.size) }} · {{ formatDate(t.modified) }}</span></span>
                </button>
            </div>
        </section>

        <div v-if="selected" class="grid grid-cols-1 xl:grid-cols-2 gap-3">
            <section class="rounded-lg border border-gray-200 dark:border-gray-700 min-w-0 overflow-hidden">
                <div class="flex items-center justify-between px-4 py-2.5 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/60">
                    <h2 class="text-sm font-medium text-gray-700 dark:text-gray-200">Data</h2>
                    <div class="flex items-center gap-2">
                        <div class="inline-flex rounded-md overflow-hidden border border-gray-300 dark:border-gray-600 divide-x divide-gray-300 dark:divide-gray-600">
                            <button type="button" @click="raw = true" class="px-3 py-1 text-xs font-medium transition-colors" :class="raw ? 'bg-indigo-600 text-white' : 'bg-white dark:bg-gray-900 text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800'">Code</button>
                            <button type="button" @click="raw = false" class="px-3 py-1 text-xs font-medium transition-colors" :class="!raw ? 'bg-indigo-600 text-white' : 'bg-white dark:bg-gray-900 text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800'">Form</button>
                        </div>
                        <button type="button" class="text-xs text-indigo-600 dark:text-indigo-400 hover:text-indigo-800 dark:hover:text-indigo-300 font-medium transition-colors" @click="resetData">Reset</button>
                    </div>
                </div>
                <div class="w-full">
                    <textarea v-if="raw" ref="textareaEl" v-model="dataText" @input="onCodeInput" class="block w-full p-4 font-mono text-xs leading-relaxed text-gray-800 dark:text-gray-200 bg-transparent outline-none resize-none overflow-hidden" spellcheck="false"></textarea>
                    <JsonSchemaForm v-else :schema="schema" :data="data" :show-title="false" class="block w-full p-3" @change="onFormChange" />
                </div>
            </section>
            <section class="rounded-lg border border-gray-200 dark:border-gray-700 min-w-0 flex flex-col overflow-hidden">
                <div class="flex justify-between items-center px-4 py-2.5 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/60">
                    <div class="flex flex-wrap items-center gap-1.5">
                        <button type="button" @click="zoom(-.15)" class="px-2 py-1 text-xs font-medium border border-gray-300 dark:border-gray-600 rounded hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors">−</button><span class="text-xs w-10 text-center text-gray-600 dark:text-gray-300 tabular-nums">{{ Math.round(scale * 100) }}%</span><button type="button" @click="zoom(.15)" class="px-2 py-1 text-xs font-medium border border-gray-300 dark:border-gray-600 rounded hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors">+</button>
                        <button type="button" @click="fit" class="px-2.5 py-1 text-xs font-medium border border-gray-300 dark:border-gray-600 rounded hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors">Fit</button><span v-if="pages" class="text-xs text-gray-500 dark:text-gray-400 ml-1">{{ pages }} page{{ pages === 1 ? '' : 's' }}</span>
                    </div>
                    <div class="flex flex-wrap items-center gap-2">
                        <button type="button" @click="render" :disabled="rendering || !typstAvailable" class="inline-flex items-center gap-1.5 px-3 py-1 text-xs font-medium rounded-md bg-indigo-600 text-white hover:bg-indigo-700 disabled:opacity-40 disabled:hover:bg-indigo-600 shadow-sm transition-colors">
                            <svg v-if="rendering" class="w-3 h-3 animate-spin" viewBox="0 0 24 24" fill="none"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"/></svg>
                            {{ rendering ? 'Rendering…' : 'Render' }}
                        </button>
                        <button type="button" @click="download" :disabled="!pdfBytes || !typstAvailable" class="px-3 py-1 text-xs font-medium rounded-md border border-gray-300 dark:border-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700 disabled:opacity-40 shadow-sm transition-colors">Download</button>
                    </div>
                </div>
                <div ref="previewEl" class="min-h-96 flex-1 overflow-auto p-4 bg-gray-100 dark:bg-gray-900">
                    <div v-if="!pages && !rendering" class="h-80 grid place-items-center">
                        <div class="text-center">
                            <svg class="mx-auto h-10 w-10 text-gray-300 dark:text-gray-600" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 0 0-3.375-3.375h-1.5A1.125 1.125 0 0 1 13.5 7.125v-1.5a3.375 3.375 0 0 0-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 0 0-9-9Z"/></svg>
                            <p class="mt-2 text-sm text-gray-400 dark:text-gray-500">Click <span class="font-medium text-gray-500 dark:text-gray-400">Render</span> to preview this template.</p>
                        </div>
                    </div>
                    <div v-else class="flex flex-col items-center gap-4"><canvas v-for="n in pages" :key="n" :ref="el => canvases[n - 1] = el" class="bg-white shadow-md rounded-sm"></canvas></div>
                </div>
            </section>
        </div>

        <ModalDialog v-if="openDialog" size-class="w-full max-w-5xl" @done="openDialog = false">
            <div class="p-5 sm:p-6">
                <div class="mb-5 pr-8"><h2 class="text-lg font-semibold text-gray-900 dark:text-gray-100">Open published template</h2><p class="text-sm text-gray-500 dark:text-gray-400">Choose a PDF template to render.</p></div>
                <div class="flex flex-col sm:flex-row gap-2 mb-5"><input v-model="search" class="flex-1 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 px-3 py-2 text-sm focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 outline-none transition-shadow" placeholder="Search templates…"><select v-model="sort" class="rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 px-3 py-2 text-sm"><option value="name">Name</option><option value="modified">Modified</option><option value="size">Size</option></select></div>
                <div v-if="!filtered.length" class="py-12 text-center"><svg class="mx-auto h-10 w-10 text-gray-300 dark:text-gray-600" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 0 0-3.375-3.375h-1.5A1.125 1.125 0 0 1 13.5 7.125v-1.5a3.375 3.375 0 0 0-3.375-3.375H8.25m3.75 9v6m3-3H9m1.5-12H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 0 0-9-9Z"/></svg><p class="mt-2 text-sm text-gray-500 dark:text-gray-400">No published templates. <a :href="chatBase + '/pdf'" class="text-indigo-600 dark:text-indigo-400 hover:underline">Open PDF Studio</a>.</p></div>
                <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4"><button type="button" v-for="t in filtered" :key="t.name" @click="select(t.name)" class="group relative aspect-[4/3] overflow-hidden rounded-lg bg-gray-100 dark:bg-gray-800 text-left shadow-sm ring-1 ring-gray-200 dark:ring-gray-700 hover:ring-2 hover:ring-indigo-500 dark:hover:ring-indigo-400 transition-all"><img v-if="t.previewUrl" :src="t.previewUrl" loading="lazy" class="h-full w-full object-contain transition-transform duration-200 group-hover:scale-[1.02]"><span v-else class="grid h-full place-items-center"><svg class="w-12 h-12 text-gray-300 dark:text-gray-600" fill="none" viewBox="0 0 24 24" stroke-width="1" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 0 0-3.375-3.375h-1.5A1.125 1.125 0 0 1 13.5 7.125v-1.5a3.375 3.375 0 0 0-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 0 0-9-9Z"/></svg></span><span class="absolute inset-x-0 bottom-0 bg-gradient-to-t from-gray-950/80 to-transparent px-3 pt-6 pb-2 text-white"><span class="block truncate text-sm font-medium">{{ t.name }}</span><span class="block text-xs text-gray-300">{{ humanifyBytes(t.size) }} · {{ formatDate(t.modified) }}</span></span></button></div>
            </div>
        </ModalDialog>
    </div>`,
    setup() {
        const client = useClient(), routes = inject('routes')
        const humanifyBytes = bytes => bytes == null ? '—' : bytes < 1024 ? `${bytes} B` : bytes < 1048576 ? `${(bytes / 1024).toFixed(1)} KB` : `${(bytes / 1048576).toFixed(1)} MB`
        const templates = ref([]), selected = ref(null), pdfPath = ref(''), chatBase = ref('/chat'), typstAvailable = ref(false)
        const loading = ref(false), rendering = ref(false), editing = ref(false), openDialog = ref(false), search = ref(''), sort = ref('name'), raw = ref(false), error = ref(null)
        const data = ref({}), initialData = ref({}), dataText = ref('{}'), schema = ref({}), pdfView = shallowRef(null), pages = ref(0), scale = ref(1), pdfBytes = ref(null), previewEl = ref(null), textareaEl = ref(null), canvases = []
        const autoResize = () => nextTick(() => { const el = textareaEl.value; if (el) { el.style.height = 'auto'; el.style.height = el.scrollHeight + 'px' } })
        watch([dataText, raw], autoResize)
        const filtered = computed(() => templates.value.filter(x => x.name.toLowerCase().includes(search.value.toLowerCase())).sort((a,b) => sort.value === 'name' ? a.name.localeCompare(b.name) : sort.value === 'size' ? b.size - a.size : new Date(b.modified) - new Date(a.modified)))
        const formatDate = value => value ? new Date(value).toLocaleString() : '—'
        let renderTimer = null
        const cloneData = value => JSON.parse(JSON.stringify(value ?? {}))
        const syncText = () => dataText.value = JSON.stringify(data.value, null, 2)
        const readData = () => {
            try { data.value = JSON.parse(dataText.value); error.value = null; return true }
            catch (e) { error.value = { message: 'Data must be valid JSON: ' + e.message }; return false }
        }
        const scheduleRender = () => {
            clearTimeout(renderTimer)
            renderTimer = setTimeout(() => { if (selected.value && typstAvailable.value && !error.value) render() }, 350)
        }
        const onCodeInput = () => { if (readData()) scheduleRender() }
        const onFormChange = value => { data.value = value; error.value = null; syncText(); scheduleRender() }
        const resetData = () => { data.value = cloneData(initialData.value); error.value = null; syncText(); scheduleRender() }
        const loadPreviewModule = async () => {
            try {
                const preview = await import(`${chatBase.value}/ext/pdf/pdf-preview.mjs`)
                pdfView.value = new preview.PdfView(chatBase.value + '/ext/pdf')
            } catch { /* typst-disabled chat returns the SPA HTML; textarea/iframe-free canvas fallback remains usable */ }
        }
        const refresh = async () => {
            loading.value = true
            const api = await client.api(new AdminPdfTemplates())
            loading.value = false
            if (api.error) { error.value = api.error; return }
            const r = api.response; templates.value = r.results || []; pdfPath.value = r.pdfPath; chatBase.value = r.chatBaseUrl || '/chat'; typstAvailable.value = r.typstAvailable
            await loadPreviewModule()
            if (routes.template) await select(routes.template, false)
        }
        const select = async (name, close = true) => {
            error.value = null
            const api = await client.api(new AdminGetPdfTemplate({ name }))
            if (api.error) {
                if (routes.origin === 'chat' || new URLSearchParams(location.search).get('origin') === 'chat') {
                    selected.value = null
                    error.value = null
                    routes.to({ template: undefined })
                    if (location.search.includes('origin=')) {
                        const url = new URL(location.href)
                        url.searchParams.delete('origin')
                        url.searchParams.delete('template')
                        history.replaceState(null, '', url.pathname + (url.search ? url.search : ''))
                    }
                    return
                }
                error.value = api.error; return
            }
            const r = api.response; selected.value = r.template; initialData.value = json(r.data); data.value = cloneData(initialData.value); dataText.value = r.data || '{}'; schema.value = json(r.schema); pages.value = 0; pdfBytes.value = null
            if (close) openDialog.value = false
            // Keep the selected template addressable without adding a duplicate history entry
            // when this selection was itself caused by back/forward navigation.
            if (routes.template !== name) routes.to({ template: name })
            scheduleRender()
        }
        const renderCanvases = async () => { if (!pdfView.value) return; await nextTick(); for (let n = 1; n <= pages.value; n++) await pdfView.value.renderPage(n, canvases[n - 1], scale.value) }
        const render = async () => {
            readData(); if (error.value || !selected.value) return
            rendering.value = true; error.value = null
            try {
                const res = await fetch('/api/AdminRenderPdfTemplate', { method: 'POST', credentials: 'same-origin', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ name: selected.value.name, data: JSON.stringify(data.value) }) })
                if (!res.ok) { const body = await res.json().catch(() => null); throw body?.responseStatus || { message: res.statusText } }
                pdfBytes.value = await res.arrayBuffer()
                if (pdfView.value) { pages.value = await pdfView.value.load(pdfBytes.value); await renderCanvases() }
                else { error.value = { message: 'PDF preview module could not load. You can still download the PDF.' } }
            } catch (e) { error.value = e } finally { rendering.value = false }
        }
        const zoom = async amount => { scale.value = Math.max(.25, Math.min(2.5, scale.value + amount)); await renderCanvases() }
        const fit = async () => { const size = await pdfView.value?.pageSize(1); if (size && previewEl.value) { scale.value = Math.max(.25, (previewEl.value.clientWidth - 32) / size.width); await renderCanvases() } }
        const download = () => { if (!pdfBytes.value) return; const url = URL.createObjectURL(new Blob([pdfBytes.value], { type: 'application/pdf' })); const a = Object.assign(document.createElement('a'), { href: url, download: selected.value.name + '.pdf' }); a.click(); setTimeout(() => URL.revokeObjectURL(url), 1000) }
        const unpublish = async () => { if (!selected.value || !confirm(`Unpublish ${selected.value.name}?`)) return; const api = await client.api(new AdminDeletePdfTemplate({ name: selected.value.name })); if (api.error) return error.value = api.error; selected.value = null; routes.to({ template: undefined }); await refresh() }
        const edit = async () => { if (!selected.value) return; editing.value = true; error.value = null; try { const api = await client.api(new AdminEditPdfTemplate({ name: selected.value.name })); if (api.error) { error.value = api.error; return } location.href = api.response.editUrl } catch (e) { error.value = e } finally { editing.value = false } }
        watch(() => routes.template, name => {
            if (name && name !== selected.value?.name) {
                select(name, false)
            } else if (!name && selected.value) {
                // Back to /admin-ui/pdf removes the selection as well as the query parameter.
                selected.value = null
                pages.value = 0
                pdfBytes.value = null
                error.value = null
            }
        })
        onMounted(refresh); onUnmounted(() => { clearTimeout(renderTimer); pdfView.value?.destroy() })
        return { templates, selected, pdfPath, chatBase, typstAvailable, loading, rendering, editing, openDialog, search, sort, raw, error, data, dataText, schema, pages, scale, pdfBytes, previewEl, textareaEl, canvases, filtered, humanifyBytes, formatDate, onCodeInput, onFormChange, resetData, select, render, zoom, fit, download, unpublish, edit }
    },
}
