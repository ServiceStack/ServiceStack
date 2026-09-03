import { ref, computed, nextTick, onMounted, onUnmounted, watch } from 'vue'
import { CheckBox } from './explorer.mjs'

let ext

export function initSearches(scope) { ext = scope }

const defaults = () => ({
  identity: { title: 'Search documentation', placeholder: 'Search docs', emptyText: 'No matching documents found.' },
  scope: {},
  behavior: { commandKShortcut: true, slashShortcut: true, minChars: 2, maxResults: 30, groupLimit: 8 },
  appearance: { theme: 'auto', highlightColor: '', width: 420, dialogWidth: 760 },
  hosting: { allowedOrigins: [], requestsPerMinute: 120 },
})
const clone = value => JSON.parse(JSON.stringify(value))
const SCOPE_FIELDS = ['category', 'docType', 'status', 'locale', 'product', 'versions', 'tags']
const palettes = {
  light: { bg: '#fff', surface: '#f8fafc', text: '#1f2937', muted: '#64748b', border: '#d1d5db' },
  dark: { bg: '#111827', surface: '#1f2937', text: '#f3f4f6', muted: '#9ca3af', border: '#374151' },
  nord: { bg: '#2e3440', surface: '#3b4252', text: '#eceff4', muted: '#d8dee9', border: '#4c566a' },
  matrix: { bg: '#000', surface: '#020a04', text: '#4ade80', muted: '#15803d', border: '#166534' },
  'soft-pink': { bg: '#fff', surface: '#fdf2f8', text: '#831843', muted: '#9d174d', border: '#fbcfe8' },
}

export const SearchesPanel = {
  components: { CheckBox },
  props: { storeId: [String, Number], facets: Object, routeSearch: String },
  emits: ['count', 'navigate'],
  template: `
      <div data-tag="SearchesPanel" class="space-y-5 pb-8">
        <div class="flex flex-wrap items-start justify-between gap-3">
          <div><h2 class="text-lg font-semibold">Website Search</h2><p class="mt-1 text-sm" :class="$styles.muted">Publish a model-free documentation Search widget backed by this File Store's local index.</p></div>
          <div class="flex gap-2">
            <button type="button" @click="rebuild" :disabled="rebuilding" class="rounded-md px-3 py-1.5 text-sm font-medium" :class="$styles.secondaryButton">{{ rebuilding ? 'Queueing…' : 'Rebuild index' }}</button>
            <button type="button" @click="newWidget" class="rounded-md px-3 py-1.5 text-sm font-semibold" :class="$styles.primaryButton">New Search</button>
          </div>
        </div>

        <div class="grid gap-3 sm:grid-cols-4">
          <div v-for="item in statusCards" :key="item.label" class="rounded-lg border p-3" :class="$styles.chromeBorder"><div class="text-xs uppercase tracking-wide" :class="$styles.muted">{{item.label}}</div><div class="mt-1 text-xl font-semibold tabular-nums">{{item.value}}</div></div>
        </div>

        <div v-if="!editing" class="space-y-3">
          <button v-for="widget in widgets" :key="widget.id" type="button" @click="edit(widget)" class="flex w-full items-center justify-between gap-4 rounded-lg border p-4 text-left hover:bg-gray-50 dark:hover:bg-gray-900" :class="$styles.chromeBorder">
            <div><div class="font-semibold">{{widget.name}}</div><div class="mt-1 text-xs" :class="$styles.muted">{{widget.enabled === 0 ? 'Archived' : widget.published ? 'Published' : 'Draft'}} · {{widget.config?.identity?.placeholder || 'Search docs'}}</div></div>
            <span class="rounded-full px-2 py-0.5 text-xs" :class="widget.published ? 'bg-green-100 text-green-800 dark:bg-green-950 dark:text-green-300' : $styles.tagLabel">{{widget.published ? 'Live' : widget.enabled === 0 ? 'Archived' : 'Draft'}}</span>
          </button>
          <p v-if="!widgets.length" class="rounded-lg border p-8 text-center text-sm" :class="[$styles.chromeBorder,$styles.muted]">No Search widgets yet. The local index is still maintained automatically.</p>
        </div>

        <div v-else class="grid gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(340px,0.8fr)]">
          <form @submit.prevent="save" class="space-y-5 rounded-xl border p-5" :class="$styles.chromeBorder">
            <div class="flex items-center justify-between"><h3 class="font-semibold">{{draft.id ? 'Edit Search' : 'New Search'}}</h3><button type="button" @click="close" class="text-sm" :class="$styles.muted">Close</button></div>
            <label class="block text-sm font-medium">Name<input type="text" v-model="draft.name" required maxlength="200" class="mt-1 block w-full rounded-md px-3 py-2" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"></label>
            <div class="grid gap-4 sm:grid-cols-2">
              <label class="block text-sm font-medium">Title<input type="text" v-model="draft.config.identity.title" class="mt-1 block w-full rounded-md px-3 py-2" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"></label>
              <label class="block text-sm font-medium">Input placeholder<input type="text" v-model="draft.config.identity.placeholder" class="mt-1 block w-full rounded-md px-3 py-2" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"></label>
            </div>
            <label class="block text-sm font-medium">No results message<input type="text" v-model="draft.config.identity.emptyText" class="mt-1 block w-full rounded-md px-3 py-2" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"></label>
            <div class="grid gap-4 sm:grid-cols-2">
              <label class="block text-sm font-medium">Theme<select v-model="draft.config.appearance.theme" class="mt-1 block w-full rounded-md px-3 py-2" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"><option v-for="v in themes" :key="v" :value="v">{{v}}</option></select></label>
              <label class="block text-sm font-medium">Max results<input v-model.number="draft.config.behavior.maxResults" type="number" min="5" max="100" class="mt-1 block w-full rounded-md px-3 py-2" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"></label>
            </div>
            <div><label class="block text-sm font-medium">Highlight color</label><div class="mt-1 flex items-center gap-2"><input type="color" :value="highlightColorValue" @input="setHighlightColor($event.target.value)" aria-label="Choose highlight color" class="size-9 shrink-0 cursor-pointer rounded border" :class="$styles.chromeBorder"><input type="text" :value="highlightColorValue" @change="setHighlightColorText" maxlength="7" pattern="#[0-9a-fA-F]{6}" spellcheck="false" class="w-24 rounded-md px-2 py-1.5 font-mono text-xs" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"><button v-if="hasHighlightColor" type="button" @click="resetHighlightColor" class="text-xs underline" :class="$styles.muted">reset</button></div><span class="mt-1 block text-xs" :class="$styles.muted">Defaults to blue with an underline in light themes, and bold white in dark themes.</span></div>
            <section class="rounded-lg border p-4 space-y-3" :class="$styles.chromeBorder"><div><h3 class="font-semibold">Document scope</h3><p class="text-xs" :class="$styles.muted">These filters are enforced by the server and cannot be changed by the host website.</p></div><div class="grid sm:grid-cols-2 gap-3"><div v-for="field in scopeFields" :key="field"><label class="block text-xs font-semibold">{{field}}</label><select v-model="draft.config.scope[field]" class="mt-1 w-full rounded-md" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"><option value="">Any value</option><option v-for="x in facetOptions(field)" :key="x.value" :value="x.value">{{x.value}} ({{x.count}})</option></select></div></div><p class="text-xs font-mono break-all" :class="$styles.muted">{{scopeSummary}}</p></section>
            <label class="block text-sm font-medium">Allowed origins <span class="font-normal" :class="$styles.muted">(one per line; empty allows all)</span><textarea v-model="origins" rows="3" class="mt-1 block w-full rounded-md px-3 py-2 font-mono text-xs" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"></textarea></label>
            <div class="flex flex-wrap items-center justify-between gap-3 border-t pt-4" :class="$styles.chromeBorder">
              <div class="flex flex-wrap gap-x-5 gap-y-2"><label class="inline-flex items-center gap-2 text-sm"><CheckBox v-model="draft.config.behavior.commandKShortcut"/> Open with <kbd>Ctrl/⌘ K</kbd></label><label class="inline-flex items-center gap-2 text-sm"><CheckBox v-model="draft.config.behavior.slashShortcut"/> Open with <kbd>/</kbd></label></div>
              <label class="inline-flex items-center gap-2 text-sm"><CheckBox v-model="draft.published"/> Published</label>
            </div>
            <div v-if="draft.embedCode" class="rounded-lg border p-3" :class="$styles.chromeBorder"><div class="mb-2 text-xs font-semibold uppercase tracking-wide" :class="$styles.muted">Embed script</div><div class="flex gap-2"><input type="text" :value="draft.embedCode" readonly class="min-w-0 flex-1 rounded px-2 py-1 font-mono text-xs" :class="[$styles.bgInput,$styles.borderInput]"><button type="button" @click="copyEmbed" class="rounded px-3 py-1 text-xs" :class="$styles.secondaryButton">Copy</button></div></div>
            <div class="flex flex-wrap justify-between gap-2">
              <div><button v-if="draft.id && draft.enabled !== 0" type="button" @click="archive" class="rounded-md px-3 py-1.5 text-sm text-red-600">Archive</button><button v-if="draft.id && draft.enabled === 0" type="button" @click="restore" class="rounded-md px-3 py-1.5 text-sm" :class="$styles.secondaryButton">Restore</button></div>
              <button type="submit" :disabled="saving || draft.enabled === 0" class="rounded-md px-4 py-1.5 text-sm font-semibold" :class="$styles.primaryButton">{{saving ? 'Saving…' : 'Save Search'}}</button>
            </div>
          </form>

          <div class="space-y-4">
            <div class="rounded-xl border p-5" :class="$styles.chromeBorder"><div class="mb-3 text-xs font-semibold uppercase tracking-wide" :class="$styles.muted">Live preview</div><button type="button" @click="openPreview" :aria-label="draft.config.identity.placeholder" :style="previewLauncherStyle" class="inline-flex items-center gap-1 rounded-full px-2 py-1"><svg viewBox="0 0 16 16" class="-ml-0.5 size-4 fill-current"><path fill-rule="evenodd" d="M9.965 11.026a5 5 0 1 1 1.06-1.06l2.755 2.754a.75.75 0 1 1-1.06 1.06l-2.755-2.754ZM10.5 7a3.5 3.5 0 1 1-7 0 3.5 3.5 0 0 1 7 0Z" clip-rule="evenodd"/></svg><kbd v-if="shortcutLabel" class="font-sans text-xs/4">{{shortcutLabel}}</kbd></button></div>
            <div class="rounded-xl border p-5" :class="$styles.chromeBorder"><label class="text-sm font-medium">Test the local index</label><div class="mt-2 flex gap-2"><input type="search" v-model="testQuery" @keyup.enter="testSearch" class="min-w-0 flex-1 rounded-md px-3 py-2" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"><button type="button" @click="testSearch" class="rounded-md px-3" :class="$styles.secondaryButton">Search</button></div><div v-for="group in testGroups" :key="group.documentId" class="mt-3"><div class="text-sm font-semibold">{{group.title}}</div><button v-for="item in group.items" :key="item.id" type="button" @click="openResult(item,group)" class="mt-1 block w-full truncate rounded bg-gray-50 px-2 py-1 text-left text-xs outline-offset-[-2px] hover:outline-2 hover:outline-gray-400 focus-visible:outline-2 focus-visible:outline-gray-400 dark:bg-gray-900 dark:hover:outline-gray-500 dark:focus-visible:outline-gray-500"><span v-for="(part,i) in resultParts(item,'snippet')" :key="i" :style="part.match ? adminMatchStyle : null">{{part.text}}</span></button></div></div>
          </div>
        </div>
        <Teleport to="body">
          <div v-if="previewOpen" class="fixed inset-0 z-[220] flex items-start justify-center bg-black/60 px-4 pt-[8vh]" @click.self="previewOpen=false" @keydown.esc.stop.prevent="escapePreview">
            <div class="flex max-h-[78vh] w-full max-w-3xl flex-col overflow-hidden rounded-2xl border shadow-2xl" :style="previewDialogStyle">
              <div class="flex items-center gap-3 border-b px-5 py-4" :style="previewBorderStyle"><svg class="size-6" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="m20 20-4-4"/></svg><input type="text" ref="previewInput" v-model="testQuery" @keydown="onPreviewInputKeydown" :placeholder="draft.config.identity.placeholder" :style="{color:previewPalette.text,borderColor:'transparent',boxShadow:'none'}" class="min-w-0 flex-1 !border-0 bg-transparent text-xl !outline-none !ring-0 placeholder:opacity-60 focus:!border-0 focus:!outline-none focus:!ring-0"><button type="button" @click="previewOpen=false" aria-label="Close search" class="rounded-md border px-2 py-1 font-sans text-xs leading-4" :style="[previewMutedStyle,previewBorderStyle]">esc</button></div>
              <div ref="previewResults" class="overflow-y-auto p-3"><div v-for="group in testGroups" :key="group.documentId" class="mb-3"><h3 class="mb-1 px-1 text-lg" :style="previewMutedStyle">{{group.title}}</h3><button v-for="item in group.items" :key="item.id" type="button" :data-result-index="resultIndex(item)" @mouseenter="selectResult(resultIndex(item))" @click="openResult(item,group)" :style="[previewResultStyle,isSelected(item) ? previewSelectedStyle : null]" class="mb-1 flex w-full gap-3 rounded-lg p-3 text-left text-sm"><span class="grid size-5 shrink-0 place-items-center text-xl" :style="previewMutedStyle"><svg v-if="item.type === 'doc'" width="20" height="20" viewBox="0 0 20 20"><path d="M17 6v12c0 .52-.2 1-1 1H4c-.7 0-1-.33-1-1V2c0-.55.42-1 1-1h8l5 5zM14 8h-3.13c-.51 0-.87-.34-.87-.87V4" stroke="currentColor" fill="none" fill-rule="evenodd" stroke-linejoin="round"></path></svg><svg v-else-if="item.type === 'heading'" width="20" height="20" viewBox="0 0 20 20"><path d="M13 13h4-4V8H7v5h6v4-4H7V8H3h4V3v5h6V3v5h4-4v5zm-6 0v4-4H3h4z" stroke="currentColor" fill="none" fill-rule="evenodd" stroke-linecap="round" stroke-linejoin="round"></path></svg><svg v-else xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 512 512"><path d="M0 0h512v512H0z" fill="none"></path><path fill="currentColor" d="M80 96h352v32H80zm0 144h352v32H80zm0 144h352v32H80z"></path></svg></span><span class="min-w-0"><span class="block truncate"><span v-for="(part,i) in resultParts(item,'snippet')" :key="i" :style="part.match ? previewMatchStyle : null">{{part.text}}</span></span><span class="block truncate text-xs" :style="previewMutedStyle"><span v-for="(part,i) in resultParts(item,'title')" :key="i" :style="part.match ? previewMatchStyle : null">{{part.text}}</span></span></span></button></div><div v-if="!testGroups.length" class="p-10 text-center text-sm" :style="previewMutedStyle">{{testQuery ? draft.config.identity.emptyText : draft.config.identity.title}}</div></div>
            </div>
          </div>
          <div v-if="documentPreview || previewLoading" class="fixed inset-0 z-[230] flex items-start justify-center bg-black/70 px-4 pt-[5vh]" @click.self="closeDocumentPreview">
            <div class="flex h-[88vh] w-full max-w-5xl flex-col overflow-hidden rounded-2xl border shadow-2xl" :style="previewDialogStyle">
              <div class="flex items-center gap-3 border-b px-5 py-3" :style="previewBorderStyle"><button type="button" @click="closeDocumentPreview" aria-label="Back to search results" class="grid size-8 shrink-0 place-items-center rounded-md" :style="previewMutedStyle"><svg viewBox="0 0 24 24" class="size-5" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M19 12H5M12 19l-7-7 7-7"/></svg></button><h3 class="min-w-0 flex-1 truncate font-semibold">{{documentPreview?.title || 'Loading…'}}</h3><button type="button" @click="closeDocumentPreview" class="text-2xl" :style="previewMutedStyle">×</button></div>
              <div v-if="previewLoading" class="grid flex-1 place-items-center text-sm" :style="previewMutedStyle">Loading document…</div>
              <article v-else ref="previewDocumentBody" tabindex="-1" class="prose max-w-none flex-1 overflow-y-auto p-6 outline-none" :class="{'prose-invert':previewIsDark}" v-html="$fmt.markdown(documentPreview?.markdown || '')"></article>
            </div>
          </div>
        </Teleport>
      </div>`,
  setup(props, { emit }) {
    const widgets = ref([]), draft = ref(null), editing = computed(() => !!draft.value), saving = ref(false)
    const index = ref({}), rebuilding = ref(false), testQuery = ref(''), testGroups = ref([]), previewOpen = ref(false)
    const documentPreview = ref(null), previewLoading = ref(false)
    const previewInput = ref(null), previewResults = ref(null), previewDocumentBody = ref(null), selectedResult = ref(-1)
    const themes = ['auto', 'light', 'dark', 'nord', 'matrix', 'soft-pink']
    const isMac = /Mac|iPhone|iPad|iPod/i.test(navigator.userAgentData?.platform || navigator.platform || '')
    const pageDark = ref(document.documentElement.classList.contains('dark'))
    const previewTheme = computed(() => draft.value?.config?.appearance?.theme === 'auto' ? (pageDark.value ? 'dark' : 'light') : (draft.value?.config?.appearance?.theme || 'light'))
    const previewPalette = computed(() => palettes[previewTheme.value] || palettes.light)
    const previewIsDark = computed(() => ['dark', 'nord', 'matrix'].includes(previewTheme.value))
    const previewDialogStyle = computed(() => ({ backgroundColor: previewPalette.value.bg, color: previewPalette.value.text, borderColor: previewPalette.value.border, colorScheme: previewIsDark.value ? 'dark' : 'light' }))
    const previewBorderStyle = computed(() => ({ borderColor: previewPalette.value.border }))
    const previewMutedStyle = computed(() => ({ color: previewPalette.value.muted }))
    const validColor = value => /^#[0-9a-f]{6}$/i.test(value || '')
    const hasHighlightColor = computed(() => validColor(draft.value?.config?.appearance?.highlightColor))
    const highlightColorValue = computed(() => hasHighlightColor.value ? draft.value.config.appearance.highlightColor : previewIsDark.value ? '#ffffff' : '#0ea5e9')
    const matchDecoration = (color, dark) => ({ color, fontWeight: '700', textDecoration: dark ? 'none' : 'underline', textDecorationColor: color, textDecorationThickness: '2px', textUnderlineOffset: '2px' })
    const adminMatchStyle = computed(() => matchDecoration(hasHighlightColor.value ? highlightColorValue.value : pageDark.value ? '#ffffff' : '#0ea5e9', pageDark.value))
    const previewMatchStyle = computed(() => matchDecoration(highlightColorValue.value, previewIsDark.value))
    const previewResultStyle = computed(() => ({ backgroundColor: previewPalette.value.surface, color: previewPalette.value.text }))
    const previewSelectedStyle = computed(() => ({ outline: `2px solid ${previewPalette.value.muted}`, outlineOffset: '-2px' }))
    const commandKEnabled = computed(() => draft.value?.config?.behavior?.commandKShortcut !== false)
    const slashOnly = computed(() => !commandKEnabled.value && draft.value?.config?.behavior?.slashShortcut !== false)
    const previewLauncherStyle = computed(() => ({ backgroundColor: previewPalette.value.surface, color: previewPalette.value.muted, boxShadow: `inset 0 0 0 1px ${previewPalette.value.border}`, paddingRight: slashOnly.value ? '12px' : '8px' }))
    const previewItems = computed(() => testGroups.value.flatMap(group => (group.items || []).map(item => ({ item, group }))))
    const shortcutLabel = computed(() => commandKEnabled.value ? (isMac ? '⌘K' : 'Ctrl K') : slashOnly.value ? '/' : '')
    const scopeSummary = computed(() => Object.entries(draft.value?.config?.scope || {}).filter(([,value]) => value).map(([key,value]) => `${key} = ${value}`).join(' · ') || 'All documents in this File Store')
    const origins = computed({ get: () => draft.value?.config.hosting.allowedOrigins.join('\n') || '', set: v => { if (draft.value) draft.value.config.hosting.allowedOrigins = String(v).split(/[,\n]/).map(x => x.trim()).filter(Boolean) } })
    const statusCards = computed(() => [
      { label: 'Documents', value: Number(index.value.documents || 0).toLocaleString() },
      { label: 'Indexed', value: Number(index.value.indexed || 0).toLocaleString() },
      { label: 'Sections', value: Number(index.value.sections || 0).toLocaleString() },
      { label: 'Provider', value: index.value.provider || '—' },
    ])
    async function load() { const [list, status] = await Promise.all([ext.getJson(`/filestores/${props.storeId}/searches`), ext.getJson(`/filestores/${props.storeId}/search-index`)]); if (!list.error) { widgets.value = list.response || []; emit('count', widgets.value.filter(x => x.enabled !== 0).length) } if (!status.error) index.value = status.response || {}; if (props.routeSearch) { const found = widgets.value.find(x => String(x.id) === String(props.routeSearch)); if (found) edit(found) } }
    function edit(widget) { draft.value = clone(widget); draft.value.config = { ...defaults(), ...draft.value.config, identity: { ...defaults().identity, ...draft.value.config?.identity }, scope: { ...draft.value.config?.scope }, behavior: { ...defaults().behavior, ...draft.value.config?.behavior }, appearance: { ...defaults().appearance, ...draft.value.config?.appearance }, hosting: { ...defaults().hosting, ...draft.value.config?.hosting } }; emit('navigate', { search: widget.id }) }
    function newWidget() { draft.value = { name: 'Documentation Search', published: false, enabled: 1, config: defaults() }; emit('navigate', { search: null }) }
    function close() { draft.value = null; emit('navigate', { search: null }) }
    async function save() { saving.value = true; try { const body = { name: draft.value.name, published: draft.value.published, config: draft.value.config }; const api = draft.value.id ? await ext.putJson(`/searches/${draft.value.id}`, body) : await ext.postJson(`/filestores/${props.storeId}/searches`, body); if (api.error) return ext.setError(api.error); await load(); edit(api.response) } finally { saving.value = false } }
    async function archive() { if (!confirm(`Archive "${draft.value.name}"? Its embed script will stop working.`)) return; const api = await ext.deleteJson(`/searches/${draft.value.id}`); if (api.error) return ext.setError(api.error); close(); await load() }
    async function restore() { const api = await ext.postJson(`/searches/${draft.value.id}/restore`, {}); if (api.error) return ext.setError(api.error); await load(); edit(api.response) }
    async function rebuild() { rebuilding.value = true; try { const api = await ext.postJson(`/filestores/${props.storeId}/search-index/rebuild`, {}); if (api.error) return ext.setError(api.error); await load() } finally { rebuilding.value = false } }
    let testTimer = 0, testRequest = 0
    async function testSearch() { const query = testQuery.value.trim(), request = ++testRequest; selectedResult.value = -1; if (!query) return testGroups.value = []; const api = await ext.getJson(`/filestores/${props.storeId}/search?q=${encodeURIComponent(query)}`); if (request !== testRequest) return; if (api.error) return ext.setError(api.error); testGroups.value = api.response?.groups || []; selectResult(previewItems.value.length ? 0 : -1) }
    async function openPreview() { previewOpen.value = true; await nextTick(); previewInput.value?.focus(); if (previewItems.value.length && selectedResult.value < 0) selectResult(0); if (testQuery.value.trim()) testSearch() }
    async function closeDocumentPreview() { documentPreview.value = null; previewLoading.value = false; await nextTick(); if (previewOpen.value) previewInput.value?.focus() }
    function escapePreview() { if (documentPreview.value || previewLoading.value) closeDocumentPreview(); else previewOpen.value = false }
    function openExternalUrl(value) { try { const url = new URL(value, location.href); if (url.protocol === 'http:' || url.protocol === 'https:') window.open(url.href, '_blank', 'noopener,noreferrer')?.focus() } catch (_) { } }
    async function openResult(item, group) {
      if (item.url) { openExternalUrl(item.url); return }
      if (!item.previewUrl) return
      previewLoading.value = true; documentPreview.value = null
      try { const response = await fetch(item.previewUrl, { headers: { Accept: 'application/json' } }); if (!response.ok) throw new Error(`Document preview failed (${response.status})`); const data = await response.json(); documentPreview.value = { title: data.title || group.title, markdown: data.markdown || '' } }
      catch (error) { ext.setError(error) } finally { previewLoading.value = false; await nextTick(); previewDocumentBody.value?.focus({ preventScroll:true }) }
    }
    function resultParts(item, field) {
      const parts = item?.[`${field}Parts`]
      if (Array.isArray(parts) && parts.length) return parts
      return [{ text: field === 'snippet' ? (item?.snippet || item?.title || '') : (item?.title || ''), match: false }]
    }
    function resultIndex(item) { return previewItems.value.findIndex(x => x.item.id === item.id) }
    function isSelected(item) { return resultIndex(item) === selectedResult.value }
    function selectResult(index) {
      const count = previewItems.value.length
      selectedResult.value = count && index >= 0 ? (index + count) % count : -1
      if (selectedResult.value >= 0) nextTick(() => previewResults.value?.querySelector(`[data-result-index="${selectedResult.value}"]`)?.scrollIntoView({ block: 'nearest' }))
    }
    function onPreviewInputKeydown(event) {
      if (event.key === 'ArrowDown' && previewItems.value.length) { selectResult(selectedResult.value < 0 ? 0 : selectedResult.value + 1); event.preventDefault() }
      else if (event.key === 'ArrowUp' && previewItems.value.length) { selectResult(selectedResult.value < 0 ? previewItems.value.length - 1 : selectedResult.value - 1); event.preventDefault() }
      else if (event.key === 'Enter' && previewItems.value[selectedResult.value]) { const selected = previewItems.value[selectedResult.value]; openResult(selected.item, selected.group); event.preventDefault() }
    }
    function onKeydown(event) {
      if (event.key === 'Escape') {
        let handled = false
        if (documentPreview.value || previewLoading.value) { closeDocumentPreview(); handled = true }
        else if (previewOpen.value) { previewOpen.value = false; handled = true }
        if (handled) { event.preventDefault(); event.stopPropagation() }
        return
      }
      if (!draft.value || event.repeat) return
      const behavior = draft.value.config?.behavior || {}
      const commandK = behavior.commandKShortcut !== false && event.key.toLowerCase() === 'k' && (event.ctrlKey || event.metaKey) && !event.altKey && !event.shiftKey
      const slash = behavior.slashShortcut !== false && event.key === '/' && !event.metaKey && !event.ctrlKey && !event.altKey && !event.shiftKey
        && !/^(INPUT|TEXTAREA|SELECT)$/.test(event.target?.tagName || '') && !event.target?.isContentEditable
      if (commandK || slash) { openPreview(); event.preventDefault(); event.stopPropagation() }
    }
    async function copyEmbed() { await navigator.clipboard.writeText(draft.value.embedCode) }
    function setHighlightColor(value) { if (validColor(value)) draft.value.config.appearance.highlightColor = value }
    function setHighlightColorText(event) { const value = String(event.target.value || '').trim(); if (validColor(value)) setHighlightColor(value); else event.target.value = highlightColorValue.value }
    function resetHighlightColor() { draft.value.config.appearance.highlightColor = '' }
    function facetOptions(field) { return (props.facets?.[field]?.values || []).map(x => typeof x === 'object' ? x : { value:x, count:'' }) }
    watch(testQuery, () => { clearTimeout(testTimer); selectedResult.value = -1; testTimer = setTimeout(testSearch, 180) })
    const themeObserver = new MutationObserver(() => pageDark.value = document.documentElement.classList.contains('dark'))
    watch(() => props.storeId, load); onMounted(() => { load(); window.addEventListener('keydown', onKeydown, true); themeObserver.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] }) }); onUnmounted(() => { clearTimeout(testTimer); window.removeEventListener('keydown', onKeydown, true); themeObserver.disconnect() })
    return { widgets, draft, editing, saving, index, rebuilding, testQuery, testGroups, previewOpen, documentPreview, previewLoading, previewInput, previewResults, previewDocumentBody, selectedResult, themes, isMac, origins, statusCards, previewPalette, previewIsDark, previewDialogStyle, previewBorderStyle, previewMutedStyle, adminMatchStyle, previewMatchStyle, highlightColorValue, hasHighlightColor, previewResultStyle, previewSelectedStyle, previewLauncherStyle, shortcutLabel, scopeFields:SCOPE_FIELDS, scopeSummary, facetOptions, edit, newWidget, close, save, archive, restore, rebuild, testSearch, openPreview, openResult, resultParts, resultIndex, isSelected, selectResult, onPreviewInputKeydown, closeDocumentPreview, escapePreview, copyEmbed, setHighlightColor, setHighlightColorText, resetHighlightColor }
  }
}
