import { ref, computed, nextTick, onMounted, onUnmounted, watch } from 'vue'
import { CheckBox } from './explorer.mjs'
import { Chart, registerables } from 'chart.js'
Chart.register(...registerables)

export const colors = [
  { background: 'rgba(54, 162, 235, 0.2)', border: 'rgb(54, 162, 235)' },
  { background: 'rgba(255, 99, 132, 0.2)', border: 'rgb(255, 99, 132)' },
  { background: 'rgba(153, 102, 255, 0.2)', border: 'rgb(153, 102, 255)' },
  { background: 'rgba(255, 206, 86, 0.2)', border: 'rgb(255, 206, 86)' },
  { background: 'rgba(255, 159, 64, 0.2)', border: 'rgb(255, 159, 64)' },
  { background: 'rgba(67, 56, 202, 0.2)', border: 'rgb(67, 56, 202)' },
  { background: 'rgba(14, 116, 144, 0.2)', border: 'rgb(14, 116, 144)' },
  { background: 'rgba(162, 28, 175, 0.2)', border: 'rgb(162, 28, 175)' },
  { background: 'rgba(75, 192, 192, 0.2)', border: 'rgb(75, 192, 192)' },
  { background: 'rgba(201, 203, 207, 0.2)', border: 'rgb(201, 203, 207)' },
]

let ext
let ctx

export function initSearches(scope, context) { ext = scope; ctx = context }

const defaults = () => ({
  identity: { title: 'Search documentation', placeholder: 'Search docs', emptyText: 'No matching documents found.', tooltip: '' },
  scope: {},
  ranking: { titleWeight: 8, headingWeight: 5, contentWeight: 1, phraseBoost: 4, exactTitleBoost: 6, freshnessWeight: 20, freshnessHalfLifeDays: 365, nativeWeight: 2, docTypeWeights: {} },
  behavior: { commandKShortcut: true, slashShortcut: true, minChars: 2, maxResults: 30, groupLimit: 8 },
  analytics: { enabled: false, retentionDays: 90, anonymizeIp: true, respectDoNotTrack: true, excludeBots: true, requireConsent: false, deniedUserAgents: [...DEFAULT_DENIED_USER_AGENTS], deniedIpRanges: [], excludedPaths: [] },
  appearance: { theme: 'auto', highlightColor: '', fontFamily: '', position: 'bottom-right', launcherStyle: 'flat', mount: '', offset: { top: 20, right: 20, bottom: 20, left: 20 }, width: 420, dialogWidth: 760 },
  hosting: { allowedOrigins: [], requestsPerMinute: 120 },
})
const clone = value => JSON.parse(JSON.stringify(value))
const DEFAULT_DENIED_USER_AGENTS = ['bytespider', 'gptbot', 'claudebot', 'amazonbot', 'imagesiftbot', 'semrushbot', 'dotbot', 'dataforseobot', 'whatsapp bot', 'petalbot']
const SCOPE_FIELDS = ['category', 'docType', 'status', 'locale', 'product', 'versions', 'tags']
const SYSTEM_FONT = "Inter, 'Inter Fallback', system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', 'Noto Sans', Arial, sans-serif, 'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol', 'Noto Color Emoji'"
const palettes = {
  light: { bg: '#fff', surface: '#f8fafc', text: '#1f2937', muted: '#64748b', border: '#d1d5db' },
  dark: { bg: '#111827', surface: '#1f2937', text: '#f3f4f6', muted: '#9ca3af', border: '#374151' },
  nord: { bg: '#2e3440', surface: '#3b4252', text: '#eceff4', muted: '#d8dee9', border: '#4c566a' },
  matrix: { bg: '#000', surface: '#020a04', text: '#4ade80', muted: '#15803d', border: '#166534' },
  'soft-pink': { bg: '#fff', surface: '#fdf2f8', text: '#831843', muted: '#9d174d', border: '#fbcfe8' },
}

const DeleteSearchDialog = {
  props: { open: Boolean, busy: Boolean, item: Object, modelValue: String },
  emits: ['close', 'confirm', 'update:modelValue'],
  template: `
    <Teleport to="body">
      <div v-if="open" class="fixed inset-0 flex items-center justify-center p-4" style="z-index:220">
        <div class="fixed inset-0 bg-black/60" @click="close"></div>
        <div class="relative flex max-h-[calc(100vh-2rem)] w-full max-w-xl flex-col overflow-hidden rounded-xl border bg-white shadow-2xl dark:bg-gray-900"
          :class="$styles.chromeBorder" role="dialog" aria-modal="true" aria-labelledby="delete-search-title">
          <div class="flex items-start gap-3 border-b px-5 py-4" :class="$styles.chromeBorder">
            <div class="mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-full bg-red-100 text-red-600 dark:bg-red-950 dark:text-red-400">
              <svg class="size-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 9v4m0 4h.01M10.3 3.7 2.4 17.4A2 2 0 0 0 4.1 20h15.8a2 2 0 0 0 1.7-3L13.7 3.7a2 2 0 0 0-3.4 0Z"/></svg>
            </div>
            <div>
              <h3 id="delete-search-title" class="font-semibold" :class="$styles.heading">Permanently delete {{ item?.name || 'this Search widget' }}?</h3>
              <p class="mt-1 text-sm" :class="$styles.muted">Its deployment, configuration, and public widget ID will be removed. This cannot be undone.</p>
            </div>
          </div>
          <div class="overflow-y-auto px-5 py-4">
            <div class="overflow-hidden rounded-lg border text-sm" :class="$styles.chromeBorder">
              <div class="flex items-center justify-between gap-4 border-b px-3 py-2.5" :class="$styles.chromeBorder"><span>Search configuration and widget ID</span><b>1</b></div>
              <div class="flex items-center justify-between gap-4 border-b px-3 py-2.5" :class="$styles.chromeBorder"><span>Published widget deployment</span><b>{{ item?.published ? 1 : 0 }}</b></div>
              <div class="flex items-center justify-between gap-4 border-b px-3 py-2.5" :class="$styles.chromeBorder"><span>Retained customer searches</span><b>{{ Number(item?.searchCount || 0).toLocaleString() }}</b></div>
              <div class="flex items-center justify-between gap-4 px-3 py-2.5"><span>Retained page views</span><b>{{ Number(item?.pageViewCount || 0).toLocaleString() }}</b></div>
            </div>
            <label for="delete-search-confirmation" class="mt-5 block text-sm font-medium">Type <strong>{{ item?.name }}</strong> to confirm</label>
            <input id="delete-search-confirmation" type="text" :value="modelValue" @input="$emit('update:modelValue', $event.target.value)" :disabled="busy"
              autocomplete="off" spellcheck="false" class="mt-2 block w-full rounded-md px-3 py-2 text-sm" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]">
          </div>
          <div class="flex justify-end gap-2 border-t px-5 py-3" :class="$styles.chromeBorder">
            <button type="button" @click="close" :disabled="busy" class="rounded-md border px-3 py-1.5 text-sm transition-colors disabled:opacity-50" :class="$styles.secondaryButton">Cancel</button>
            <button type="button" @click="$emit('confirm')" :disabled="busy || !item || modelValue !== item.name"
              class="rounded-md border border-red-600 bg-red-600 px-4 py-1.5 text-sm font-semibold text-white transition-colors hover:border-red-700 hover:bg-red-700 disabled:cursor-not-allowed disabled:opacity-40">
              {{ busy ? 'Deleting everything…' : 'Delete Search' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>`,
  setup(props, { emit }) {
    function close() { if (!props.busy) emit('close') }
    function onKey(event) { if (event.key === 'Escape' && props.open) close() }
    onMounted(() => document.addEventListener('keydown', onKey))
    onUnmounted(() => document.removeEventListener('keydown', onKey))
    return { close }
  },
}

export const SearchesPanel = {
  components: { CheckBox, DeleteSearchDialog },
  props: { storeId: [String, Number], facets: Object, routeSearch: String },
  emits: ['count', 'navigate'],
  template: `
      <div data-tag="SearchesPanel" class="space-y-5 pb-8">
        <div class="flex flex-wrap items-start justify-between gap-3">
          <div><h2 class="text-lg font-semibold">Website Search</h2><p class="mt-1 text-sm" :class="$styles.muted">Publish a model-free documentation Search widget backed by this File Store's local index.</p></div>
          <div v-if="!editing" class="flex gap-2">
            <button type="button" @click="rebuild" :disabled="rebuilding" class="rounded-md px-3 py-1.5 text-sm font-medium" :class="$styles.secondaryButton">{{ rebuilding ? 'Queueing…' : 'Rebuild index' }}</button>
            <button type="button" @click="newWidget" class="rounded-md px-3 py-1.5 text-sm font-semibold" :class="$styles.primaryButton">New Search</button>
          </div>
        </div>
        <DeleteSearchDialog :open="deleteOpen" :busy="deleteBusy" :item="draft" v-model="deleteConfirmation" @close="closeDelete" @confirm="deletePermanently" />

        <div v-if="editing" class="flex items-end justify-between gap-4 border-b" :class="$styles.chromeBorder">
          <nav class="flex min-w-0 gap-5" aria-label="Search sections">
            <button v-for="section in searchSections" :key="section.value" type="button" @click="selectSearchSection(section.value)" :disabled="section.value !== 'edit' && !draft?.id" class="-mb-px border-b-2 px-0.5 pb-2 text-sm font-medium disabled:cursor-not-allowed disabled:opacity-40" :class="activeSection === section.value ? 'border-blue-600 text-blue-600 dark:border-blue-400 dark:text-blue-400' : ['border-transparent',$styles.muted]">{{section.label}}</button>
          </nav>
          <button type="button" @click="close" class="shrink-0 pb-2 text-sm font-medium" :class="$styles.muted">Close Search</button>
        </div>

        <section v-if="activeSection !== 'edit' && draft?.id" class="overflow-hidden rounded-xl border" :class="$styles.chromeBorder">
          <div class="flex flex-wrap items-center justify-between gap-3 border-b px-5 py-4" :class="$styles.chromeBorder">
            <p class="text-xs" :class="$styles.muted">{{activeSection === 'analytics' ? 'Analyze first-party website traffic collected by this Search widget.' : 'Related wording is grouped into search intents so demand and missing content are easier to measure.'}}</p>
            <div class="flex items-center gap-2">
              <button v-if="activeSection === 'analytics' && analyticsEnabled" type="button" @click="setTrafficAnalytics(false)" :disabled="analyticsBusy || saving" class="rounded-md border border-red-600 px-3 py-1.5 text-sm font-medium text-red-600 hover:bg-red-50 disabled:opacity-50 dark:hover:bg-red-950/30">{{analyticsBusy ? 'Disabling…' : 'Disable Analytics'}}</button>
              <button type="button" @click="loadSearchAnalytics" :disabled="searchesLoading" title="Refresh searches" aria-label="Refresh searches" class="rounded p-1 transition-colors hover:bg-gray-100 disabled:opacity-50 dark:hover:bg-gray-800" :class="$styles.muted"><svg class="size-4" :class="{'animate-spin':searchesLoading}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M20 11a8.1 8.1 0 0 0-15.5-2M4 4v5h5M4 13a8.1 8.1 0 0 0 15.5 2M20 20v-5h-5" /></svg></button>
            </div>
          </div>
          <div v-if="searchesLoading && !searchAnalytics" class="p-8 text-center text-sm" :class="$styles.muted">Loading {{activeSection === 'analytics' ? 'website analytics' : 'customer searches'}}…</div>
          <template v-else-if="searchAnalytics">
            <div v-if="activeSection === 'analytics'" class="p-5">
              <div class="flex flex-wrap items-start justify-between gap-3">
                <div><h4 class="font-semibold">Website traffic</h4><p class="mt-1 text-xs" :class="$styles.muted">Optional page-view analytics captured by this Search widget on every page where it is embedded.</p></div>
                <div class="flex rounded-md border p-0.5" :class="$styles.chromeBorder"><button v-for="range in trafficRanges" :key="range.value" type="button" @click="selectTrafficPeriod(range.value)" class="rounded px-2.5 py-1 text-xs font-medium" :class="trafficPeriod === range.value ? $styles.primaryButton : $styles.muted">{{range.label}}</button></div>
              </div>
              <div v-if="!analyticsEnabled" class="mt-4 rounded-lg border border-dashed p-5 text-center"><p class="text-sm" :class="$styles.muted">Traffic analytics is off. Search queries and result clicks are still measured independently.</p><button type="button" @click="setTrafficAnalytics(true)" :disabled="analyticsBusy || saving" class="mt-3 rounded-md px-3 py-1.5 text-sm font-semibold disabled:opacity-50" :class="$styles.primaryButton">{{analyticsBusy ? 'Enabling…' : 'Capture Analytics'}}</button></div>
              <template v-if="searchAnalytics.traffic">
                <div class="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-3 xl:grid-cols-7"><div v-for="metric in trafficMetrics" :key="metric.label" class="rounded-lg border p-3" :class="$styles.chromeBorder"><div class="text-xs uppercase tracking-wide" :class="$styles.muted">{{metric.label}}</div><div class="mt-1 text-xl font-semibold tabular-nums">{{metric.value}}</div></div></div>
                <div class="mt-4 h-72 rounded-lg border p-3" :class="$styles.chromeBorder"><canvas ref="trafficChart"></canvas></div>
                <div class="mt-4 grid items-stretch gap-4 lg:h-[32rem] lg:grid-cols-3">
                  <div class="flex min-h-0 flex-col overflow-hidden rounded-lg border" :class="$styles.chromeBorder"><div class="border-b px-3 py-2 text-xs font-semibold uppercase tracking-wide" :class="[$styles.chromeBorder,$styles.muted]">Top pages</div><div class="min-h-0 flex-1 overflow-y-auto"><div v-for="item in searchAnalytics.traffic.topPages" :key="item.path" class="flex items-start justify-between gap-3 border-b px-3 py-2.5 last:border-b-0" :class="$styles.chromeBorder"><div class="min-w-0"><a v-if="item.url" :href="item.url" target="_blank" rel="noopener noreferrer" class="block truncate text-sm font-medium hover:underline" :title="item.title || item.path">{{item.title || item.path}}</a><div v-else class="truncate text-sm font-medium">{{item.title || item.path}}</div><div class="truncate text-xs" :class="$styles.muted">{{item.path}} · {{item.visitors}} visitors</div></div><b class="text-sm tabular-nums">{{item.views}}</b></div><p v-if="!searchAnalytics.traffic.topPages?.length" class="p-5 text-center text-sm" :class="$styles.muted">No page views yet.</p></div></div>
                  <div class="flex min-h-0 flex-col overflow-hidden rounded-lg border" :class="$styles.chromeBorder"><div class="border-b px-3 py-2 text-xs font-semibold uppercase tracking-wide" :class="[$styles.chromeBorder,$styles.muted]">Top referrers</div><div class="min-h-0 flex-1 overflow-y-auto"><div v-for="item in searchAnalytics.traffic.topReferrers" :key="item.value" class="flex items-center justify-between gap-3 border-b px-3 py-2.5 text-sm last:border-b-0" :class="$styles.chromeBorder"><span class="min-w-0 truncate" :title="item.value">{{trafficLabel(item.value)}}</span><b class="tabular-nums">{{item.count}}</b></div><p v-if="!searchAnalytics.traffic.topReferrers?.length" class="p-5 text-center text-sm" :class="$styles.muted">No external referrers yet.</p></div></div>
                  <div class="flex min-h-0 flex-col overflow-hidden rounded-lg border" :class="$styles.chromeBorder"><div class="border-b px-3 py-2 text-xs font-semibold uppercase tracking-wide" :class="[$styles.chromeBorder,$styles.muted]">Audience</div><div class="min-h-0 flex-1 overflow-y-auto"><div v-for="group in audienceGroups" :key="group.label" class="border-b p-3 last:border-b-0" :class="$styles.chromeBorder"><div class="mb-2 text-xs font-semibold uppercase tracking-wide" :class="$styles.muted">{{group.label}}</div><div class="flex flex-wrap gap-1.5"><span v-for="item in group.items.slice(0,6)" :key="item.value" class="rounded-full border px-2 py-0.5 text-xs" :class="$styles.chromeBorder">{{item.value}} <b>{{item.count}}</b></span><span v-if="!group.items.length" class="text-xs" :class="$styles.muted">No data</span></div></div></div></div>
                </div>
                <div class="mt-4 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
                  <div v-for="group in geographyGroups" :key="group.label" class="overflow-hidden rounded-lg border" :class="$styles.chromeBorder">
                    <div class="border-b px-3 py-2 text-xs font-semibold uppercase tracking-wide" :class="[$styles.chromeBorder,$styles.muted]">{{group.label}}</div>
                    <div class="max-h-56 overflow-y-auto"><div v-for="item in group.items" :key="item.value" class="flex items-center justify-between gap-3 border-b px-3 py-2.5 text-sm last:border-b-0" :class="$styles.chromeBorder"><span class="min-w-0 truncate" :title="item.value">{{item.value}}</span><b class="tabular-nums">{{item.count}}</b></div><p v-if="!group.items.length" class="p-5 text-center text-sm" :class="$styles.muted">No resolved data.</p></div>
                  </div>
                </div>
                <div class="mt-4 overflow-hidden rounded-lg border" :class="$styles.chromeBorder">
                  <div class="flex items-center justify-between gap-3 border-b px-3 py-2" :class="$styles.chromeBorder">
                    <div class="text-xs font-semibold uppercase tracking-wide" :class="$styles.muted">Recent visitors</div>
                    <div v-if="visitorTotal" class="flex items-center gap-2 text-xs" :class="$styles.muted">
                      <span class="tabular-nums">{{visitorStart}}–{{visitorEnd}} of {{visitorTotal.toLocaleString()}}</span>
                      <button type="button" @click="previousVisitors" :disabled="!canPreviousVisitors || searchesLoading" title="Previous visitors" aria-label="Previous visitors" class="rounded p-1 transition-colors hover:bg-gray-100 disabled:cursor-not-allowed disabled:opacity-30 dark:hover:bg-gray-800"><svg class="size-4" viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m12.5 15-5-5 5-5" /></svg></button>
                      <button type="button" @click="nextVisitors" :disabled="!canNextVisitors || searchesLoading" title="Next visitors" aria-label="Next visitors" class="rounded p-1 transition-colors hover:bg-gray-100 disabled:cursor-not-allowed disabled:opacity-30 dark:hover:bg-gray-800"><svg class="size-4" viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m7.5 5 5 5-5 5" /></svg></button>
                    </div>
                  </div>
                  <div class="overflow-x-auto">
                    <table class="min-w-full border-separate border-spacing-0 text-left text-sm">
                      <thead><tr class="text-xs uppercase tracking-wide" :class="$styles.muted"><th class="whitespace-nowrap border-b px-3 py-2 font-semibold" :class="$styles.chromeBorder">IP address</th><th class="border-b px-3 py-2 font-semibold" :class="$styles.chromeBorder">Location</th><th class="border-b px-3 py-2 font-semibold" :class="$styles.chromeBorder">Network</th><th class="border-b px-3 py-2 font-semibold" :class="$styles.chromeBorder">Page</th><th class="whitespace-nowrap border-b px-3 py-2 text-right font-semibold" :class="$styles.chromeBorder">Seen</th></tr></thead>
                      <tbody><tr v-for="item in searchAnalytics.traffic.recentPageViews" :key="item.createdAt + ':' + item.ipAddress + ':' + item.pagePath"><td class="whitespace-nowrap border-b px-3 py-2.5 align-top font-mono text-xs" :class="$styles.chromeBorder">{{item.ipAddress || ''}}</td><td class="min-w-48 border-b px-3 py-2.5 align-top" :class="$styles.chromeBorder"><div v-if="geoLocation(item)" class="font-medium">{{geoLocation(item)}}</div><div v-if="geoDetails(item)" class="mt-0.5 text-xs" :class="$styles.muted">{{geoDetails(item)}}</div></td><td class="min-w-44 border-b px-3 py-2.5 align-top" :class="$styles.chromeBorder"><div v-if="item.organization" class="font-medium">{{item.organization}}</div><div v-if="item.asn" class="mt-0.5 text-xs" :class="$styles.muted">AS{{item.asn}}</div></td><td class="min-w-56 max-w-md border-b px-3 py-2.5 align-top" :class="$styles.chromeBorder"><a v-if="item.pageUrl" :href="item.pageUrl" target="_blank" rel="noopener noreferrer" class="block truncate font-medium hover:underline" :title="item.pageTitle || item.pagePath">{{item.pageTitle || item.pagePath || item.pageUrl}}</a><div v-if="item.pagePath" class="mt-0.5 truncate text-xs" :class="$styles.muted">{{item.pagePath}}</div></td><td class="whitespace-nowrap border-b px-3 py-2.5 text-right align-top text-xs" :class="[$styles.chromeBorder,$styles.muted]"><time :datetime="item.createdAt" :title="formatSearchDate(item.createdAt)">{{formatRelativeTime(item.createdAt)}}</time></td></tr></tbody>
                    </table>
                    <p v-if="!searchAnalytics.traffic.recentPageViews?.length" class="p-6 text-center text-sm" :class="$styles.muted">No page views have been recorded for this period.</p>
                  </div>
                </div>
              </template>
            </div>
            <template v-if="activeSection === 'searches'">
            <div class="grid grid-cols-2 gap-3 border-b p-5 sm:grid-cols-3 xl:grid-cols-6" :class="$styles.chromeBorder">
              <div v-for="metric in searchMetrics" :key="metric.label" class="rounded-lg border p-3" :class="$styles.chromeBorder"><div class="text-xs uppercase tracking-wide" :class="$styles.muted">{{ metric.label }}</div><div class="mt-1 text-xl font-semibold tabular-nums">{{ metric.value }}</div></div>
            </div>
            <div class="border-b" :class="$styles.chromeBorder">
              <div class="border-b bg-gray-50 px-4 py-2 text-xs font-semibold uppercase tracking-wide dark:bg-gray-900" :class="[$styles.chromeBorder,$styles.muted]">Popular documents</div>
              <div v-if="searchAnalytics.popularDocuments?.length" class="grid max-h-72 overflow-y-auto md:grid-cols-2">
                <div v-for="document in searchAnalytics.popularDocuments" :key="document.documentId" class="flex min-w-0 items-start justify-between gap-4 border-b px-4 py-3 odd:md:border-r" :class="$styles.chromeBorder">
                  <div class="min-w-0"><a v-if="document.sourceUrl" :href="document.sourceUrl" target="_blank" rel="noopener noreferrer" class="block truncate text-sm font-semibold hover:underline" :title="document.title">{{document.title || 'Document'}}</a><div v-else class="truncate text-sm font-semibold" :title="document.title">{{document.title || 'Document'}}</div><div class="mt-1 text-xs" :class="$styles.muted">{{Number(document.uniqueSearches || 0).toLocaleString()}} search{{Number(document.uniqueSearches || 0) === 1 ? '' : 'es'}} · avg. position {{Number(document.averagePosition || 0).toLocaleString()}} · last clicked {{formatSearchDate(document.lastClickedAt)}}</div></div>
                  <span class="shrink-0 rounded-full bg-gray-100 px-2.5 py-1 text-sm font-semibold tabular-nums dark:bg-gray-800">{{Number(document.clickCount || 0).toLocaleString()}} click{{Number(document.clickCount || 0) === 1 ? '' : 's'}}</span>
                </div>
              </div>
              <p v-else class="p-6 text-center text-sm" :class="$styles.muted">No Search result clicks have been recorded yet.</p>
            </div>
            <div class="grid min-h-72 lg:grid-cols-[minmax(0,1.2fr)_minmax(18rem,0.8fr)]">
              <div class="min-w-0 border-b lg:border-b-0 lg:border-r" :class="$styles.chromeBorder">
                <div class="border-b bg-gray-50 px-4 py-2 text-xs font-semibold uppercase tracking-wide dark:bg-gray-900" :class="[$styles.chromeBorder,$styles.muted]">Related searches by frequency</div>
                <div class="max-h-[32rem] overflow-y-auto">
                  <div v-for="group in searchAnalytics.groups" :key="group.key" class="border-b px-4 py-3 last:border-b-0" :class="$styles.chromeBorder">
                    <div class="flex items-start justify-between gap-4"><div class="min-w-0"><div class="break-words text-sm font-semibold">{{ group.query }}</div><div class="mt-1 text-xs" :class="$styles.muted">Last searched {{ formatSearchDate(group.lastSearchedAt) }} · {{ group.averageResults }} avg. results · {{group.clickCount || 0}} clicks · {{group.clickThroughRate || 0}}% CTR</div></div><span class="shrink-0 rounded-full bg-gray-100 px-2.5 py-1 text-sm font-semibold tabular-nums dark:bg-gray-800">{{ Number(group.count || 0).toLocaleString() }}</span></div>
                    <div v-if="group.variants?.length > 1" class="mt-2 flex flex-wrap gap-1.5"><span v-for="variant in group.variants" :key="variant.query" class="rounded border px-2 py-0.5 text-xs" :class="$styles.chromeBorder">{{ variant.query }} <b class="ml-1">{{ variant.count }}</b></span></div>
                    <div v-if="group.noResultCount" class="mt-2 text-xs font-medium text-amber-700 dark:text-amber-400">{{ Number(group.noResultCount).toLocaleString() }} returned no results</div>
                  </div>
                  <p v-if="!searchAnalytics.groups?.length" class="p-8 text-center text-sm" :class="$styles.muted">No customer searches have been recorded yet.</p>
                </div>
              </div>
              <div class="min-w-0">
                <div class="border-b bg-gray-50 px-4 py-2 text-xs font-semibold uppercase tracking-wide dark:bg-gray-900" :class="[$styles.chromeBorder,$styles.muted]">Latest searches</div>
                <div class="max-h-[32rem] overflow-y-auto">
                  <div v-for="item in searchAnalytics.recent" :key="item.id" class="border-b px-4 py-3 last:border-b-0" :class="$styles.chromeBorder"><div class="break-words text-sm font-medium">{{ item.query }}</div><div class="mt-1 flex flex-wrap justify-between gap-x-3 gap-y-1 text-xs" :class="$styles.muted"><span>{{ item.resultCount }} result{{ item.resultCount === 1 ? '' : 's' }} in {{ item.documentCount }} document{{ item.documentCount === 1 ? '' : 's' }}</span><time :datetime="item.createdAt">{{ formatSearchDate(item.createdAt) }}</time></div><div v-if="searchSource(item)" class="mt-1 truncate text-xs" :class="$styles.muted" :title="item.pageUrl || item.origin">{{ searchSource(item) }}</div></div>
                  <p v-if="!searchAnalytics.recent?.length" class="p-8 text-center text-sm" :class="$styles.muted">No recent searches.</p>
                </div>
              </div>
            </div>
            </template>
          </template>
        </section>

        <div v-if="!editing" class="rounded-lg border px-4 py-3" :class="[statusCards[0].health.tone,$styles.chromeBorder]"><div class="flex items-center justify-between gap-3"><div><b>{{statusCards[0].health.label}}</b><p class="mt-0.5 text-xs" :class="$styles.muted">{{statusCards[0].health.detail}}</p></div><span class="size-2.5 shrink-0 rounded-full" :class="statusCards[0].health.dot"></span></div><div v-if="index.errors?.length" class="mt-3 space-y-1 border-t pt-3 text-xs" :class="$styles.chromeBorder"><div v-for="error in index.errors" :key="error.documentId" class="truncate" :title="error.error"><b>{{error.name || ('Document ' + error.documentId)}}</b>: {{error.error}}</div></div></div>
        <div v-if="!editing" class="grid gap-3 sm:grid-cols-3 xl:grid-cols-6">
          <div v-for="item in statusCards" :key="item.label" class="rounded-lg border p-3" :class="$styles.chromeBorder"><div class="text-xs uppercase tracking-wide" :class="$styles.muted">{{item.label}}</div><div class="mt-1 text-xl font-semibold tabular-nums">{{item.value}}</div></div>
        </div>

        <div v-if="!editing" class="space-y-3">
          <button v-for="widget in widgets" :key="widget.id" type="button" @click="edit(widget)" class="flex w-full items-center justify-between gap-4 rounded-lg border p-4 text-left hover:bg-gray-50 dark:hover:bg-gray-900" :class="$styles.chromeBorder">
            <div><div class="font-semibold">{{widget.name}}</div><div class="mt-1 text-xs" :class="$styles.muted">{{widget.enabled === 0 ? 'Archived' : widget.published ? 'Published' : 'Draft'}} · {{ Number(widget.searchCount || 0).toLocaleString() }} search{{ Number(widget.searchCount || 0) === 1 ? '' : 'es' }}</div></div>
            <span class="flex shrink-0 items-center gap-1.5 text-xs" :class="$styles.muted"><span class="size-1.5 rounded-full" :class="widget.enabled === 0 ? 'bg-orange-400' : widget.published ? 'bg-green-500' : 'bg-gray-400'"></span>{{widget.enabled === 0 ? 'Archived' : widget.published ? 'Published' : 'Draft'}}</span>
          </button>
          <p v-if="!widgets.length" class="rounded-lg border p-8 text-center text-sm" :class="[$styles.chromeBorder,$styles.muted]">No Search widgets yet. The local index is still maintained automatically.</p>
        </div>

        <div v-if="editing && activeSection === 'edit'" class="space-y-5">
          <div class="grid gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(340px,0.8fr)]">
          <form @submit.prevent class="space-y-5 rounded-xl border p-5" :class="$styles.chromeBorder">
            <div class="flex flex-wrap items-end justify-between gap-3">
              <label class="block min-w-0 flex-1 text-sm font-medium">Name<input type="text" v-model="draft.name" :disabled="archived" required maxlength="200" class="mt-1 block w-full rounded-md px-3 py-2 disabled:opacity-60" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"></label>
              <div class="flex flex-wrap gap-2">
                <button v-if="!archived && !draft.published" type="button" @click="save(false)" :disabled="saving || !canSaveDraft" class="rounded-md px-3 py-1.5 text-sm disabled:opacity-50" :class="$styles.secondaryButton">Save draft</button>
                <button v-if="!archived" type="button" @click="save(true)" :disabled="saving || !canPublish" class="rounded-md px-3 py-1.5 text-sm font-semibold disabled:opacity-50" :class="$styles.primaryButton">{{draft.published ? 'Update published' : 'Publish'}}</button>
              </div>
            </div>
            <div v-if="archived" class="rounded-lg border border-amber-300 bg-amber-50 px-4 py-3 text-sm text-amber-900 dark:border-amber-800 dark:bg-amber-950/30 dark:text-amber-200">This Search widget is archived, offline, and read-only. Restore it to an unpublished draft before editing or publishing it.</div>
            <fieldset :disabled="archived" class="space-y-5 disabled:opacity-60">
            <div class="grid gap-4 sm:grid-cols-2">
              <label class="block text-sm font-medium">Title<input type="text" v-model="draft.config.identity.title" class="mt-1 block w-full rounded-md px-3 py-2" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"></label>
              <label class="block text-sm font-medium">Input placeholder<input type="text" v-model="draft.config.identity.placeholder" class="mt-1 block w-full rounded-md px-3 py-2" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"></label>
            </div>
            <label class="block text-sm font-medium">Button tooltip <span class="font-normal" :class="$styles.muted">(optional)</span><input type="text" v-model.trim="draft.config.identity.tooltip" maxlength="200" placeholder="No tooltip" class="mt-1 block w-full rounded-md px-3 py-2" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"></label>
            <label class="block text-sm font-medium">No results message<input type="text" v-model="draft.config.identity.emptyText" class="mt-1 block w-full rounded-md px-3 py-2" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"></label>
            <section class="rounded-lg border p-4 space-y-4" :class="$styles.chromeBorder">
              <div><h3 class="font-semibold">Appearance</h3><p class="text-xs" :class="$styles.muted">Customize the Search dialog and floating launcher.</p></div>
              <div class="grid gap-4 sm:grid-cols-2">
                <label class="block text-sm font-medium">Theme<select v-model="draft.config.appearance.theme" class="mt-1 block w-full rounded-md px-3 py-2" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"><option v-for="v in themes" :key="v" :value="v">{{v}}</option></select><span v-if="draft.config.appearance.theme === 'auto'" class="mt-1 block text-xs" :class="$styles.muted">Uses the host page's saved <code>color-scheme</code>.</span></label>
                <label class="block text-sm font-medium">Button corner<select v-model="draft.config.appearance.position" class="mt-1 block w-full rounded-md px-3 py-2" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"><option value="top-left">Top left</option><option value="top-right">Top right</option><option value="bottom-left">Bottom left</option><option value="bottom-right">Bottom right</option></select></label>
                <label class="block text-sm font-medium">Button style<select v-model="draft.config.appearance.launcherStyle" class="mt-1 block w-full rounded-md px-3 py-2" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"><option value="flat">Flat</option><option value="raised">Raised</option><option value="inset">Inset</option></select></label>
              </div>
              <div><label class="block text-sm font-medium">Mount element <span class="font-normal" :class="$styles.muted">(optional CSS selector)</span><input type="text" v-model.trim="draft.config.appearance.mount" maxlength="300" placeholder="#search-slot" spellcheck="false" class="mt-1 block w-full rounded-md px-3 py-2 font-mono text-xs" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"></label><span class="mt-1 block text-xs" :class="$styles.muted">Renders the launcher inside this element (e.g. a nav bar) instead of a floating corner button. The search dialog still overlays the page. The host page can override it with <code>data-mount</code> on the script tag.</span></div>
              <div><div class="flex items-center justify-between gap-3"><label for="search-font-family" class="text-sm font-medium">Font family</label><button v-if="hasFontFamily" type="button" @click="resetFontFamily" class="text-xs underline" :class="$styles.muted">reset</button></div><input id="search-font-family" type="text" :value="fontFamily" @change="setFontFamily($event.target.value)" class="mt-1 block w-full rounded-md px-3 py-2" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"></div>
              <div><div class="text-sm font-medium">Button offsets</div><div class="mt-1 grid grid-cols-2 gap-3 sm:grid-cols-4"><label v-for="side in offsetSides" :key="side" class="text-xs capitalize">{{side}}<input v-model.number="draft.config.appearance.offset[side]" type="number" min="0" max="400" class="mt-1 block w-full rounded-md px-3 py-2" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"></label></div><span class="mt-1 block text-xs" :class="$styles.muted">Pixels from each viewport edge; the selected corner uses its two corresponding values.</span></div>
              <div><label class="block text-sm font-medium">Highlight color</label><div class="mt-1 flex items-center gap-2"><input type="color" :value="highlightColorValue" @input="setHighlightColor($event.target.value)" aria-label="Choose highlight color" class="size-9 shrink-0 cursor-pointer rounded border" :class="$styles.chromeBorder"><input type="text" :value="highlightColorValue" @change="setHighlightColorText" maxlength="7" pattern="#[0-9a-fA-F]{6}" spellcheck="false" class="w-24 rounded-md px-2 py-1.5 font-mono text-xs" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"><button v-if="hasHighlightColor" type="button" @click="resetHighlightColor" class="text-xs underline" :class="$styles.muted">reset</button></div><span class="mt-1 block text-xs" :class="$styles.muted">Defaults to blue with an underline in light themes, and bold white in dark themes.</span></div>
            </section>
            <label class="block text-sm font-medium">Results per page<input v-model.number="draft.config.behavior.maxResults" type="number" min="5" max="100" class="mt-1 block w-full rounded-md px-3 py-2" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"></label>
            <section class="rounded-lg border p-4 space-y-3" :class="$styles.chromeBorder"><div><h3 class="font-semibold">Document scope</h3><p class="text-xs" :class="$styles.muted">These filters are enforced by the server and cannot be changed by the host website.</p></div><div class="grid sm:grid-cols-2 gap-3"><div v-for="field in scopeFields" :key="field"><label class="block text-xs font-semibold">{{field}}</label><select v-model="draft.config.scope[field]" class="mt-1 w-full rounded-md" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"><option value="">Any value</option><option v-for="x in facetOptions(field)" :key="x.value" :value="x.value">{{x.value}} ({{x.count}})</option></select></div></div><p class="text-xs font-mono break-all" :class="$styles.muted">{{scopeSummary}}</p></section>
            <section class="space-y-4 rounded-lg border p-4" :class="$styles.chromeBorder">
              <div><h3 class="font-semibold">Analytics & privacy</h3><p class="text-xs" :class="$styles.muted">Controls optional website traffic capture. Traffic exclusions also omit matching customer searches and result clicks.</p></div>
              <label class="block text-sm font-medium">Retention<select v-model.number="draft.config.analytics.retentionDays" class="mt-1 block w-full rounded-md px-3 py-2" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"><option :value="30">30 days</option><option :value="90">90 days</option><option :value="180">180 days</option><option :value="365">1 year</option><option :value="730">2 years</option></select></label>
              <div class="grid gap-2 sm:grid-cols-2"><label class="inline-flex items-center gap-2 text-sm"><CheckBox v-model="draft.config.analytics.anonymizeIp"/> Anonymize IP addresses</label><label class="inline-flex items-center gap-2 text-sm"><CheckBox v-model="draft.config.analytics.respectDoNotTrack"/> Respect Do Not Track</label><label class="inline-flex items-center gap-2 text-sm"><CheckBox v-model="draft.config.analytics.excludeBots"/> Exclude known bots</label><label class="inline-flex items-center gap-2 text-sm"><CheckBox v-model="draft.config.analytics.requireConsent"/> Require consent callback</label></div>
              <div class="grid gap-4 lg:grid-cols-2">
                <label class="block text-sm font-medium">Denied user agents <span class="font-normal" :class="$styles.muted">(one substring per line)</span><textarea v-model.lazy="deniedUserAgents" rows="6" spellcheck="false" class="mt-1 block w-full rounded-md px-3 py-2 font-mono text-xs" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"></textarea><span class="mt-1 block text-xs" :class="$styles.muted">Case-insensitive. Disable known-bot detection to rely only on this editable list.</span></label>
                <div><label class="block text-sm font-medium">Denied IP ranges <span class="font-normal" :class="$styles.muted">(one per line)</span><textarea v-model.lazy="deniedIpRanges" rows="6" spellcheck="false" placeholder="203.0.113.8&#10;114.119.*&#10;2001:db8::/32" class="mt-1 block w-full rounded-md px-3 py-2 font-mono text-xs" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"></textarea></label><div class="mt-1 flex items-center justify-between gap-3"><span class="text-xs" :class="$styles.muted">Exact IPv4/IPv6, CIDR, or trailing IPv4 wildcards.</span><button v-if="draft.requestIp" type="button" @click="excludeCurrentIp" class="shrink-0 text-xs font-medium underline" :class="$styles.muted">Exclude {{draft.requestIp}}</button></div></div>
              </div>
              <label class="block text-sm font-medium">Excluded page paths <span class="font-normal" :class="$styles.muted">(one glob per line)</span><textarea v-model.lazy="excludedPaths" rows="4" spellcheck="false" placeholder="/admin/*&#10;/health&#10;/preview/*" class="mt-1 block w-full rounded-md px-3 py-2 font-mono text-xs" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"></textarea><span class="mt-1 block text-xs" :class="$styles.muted"><code>*</code> matches any text and <code>?</code> matches one character. Query strings are ignored.</span></label>
              <button v-if="draft.id" type="button" @click="purgeAnalytics" :disabled="analyticsBusy" class="rounded-md border border-red-600 px-3 py-1.5 text-sm font-medium text-red-600 hover:bg-red-50 disabled:opacity-50 dark:hover:bg-red-950/30">Clear retained analytics</button>
            </section>
            <label class="block text-sm font-medium">Allowed origins <span class="font-normal" :class="$styles.muted">(one per line; empty allows all)</span><textarea v-model="origins" rows="3" class="mt-1 block w-full rounded-md px-3 py-2 font-mono text-xs" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"></textarea></label>
            <div class="flex flex-wrap items-center justify-between gap-3 border-t pt-4" :class="$styles.chromeBorder">
              <div class="flex flex-wrap gap-x-5 gap-y-2"><label class="inline-flex items-center gap-2 text-sm"><CheckBox v-model="draft.config.behavior.commandKShortcut"/> Open with <kbd>Ctrl/⌘ K</kbd></label><label class="inline-flex items-center gap-2 text-sm"><CheckBox v-model="draft.config.behavior.slashShortcut"/> Open with <kbd>/</kbd></label></div>
            </div>
            </fieldset>
            <section v-if="draft.id" class="rounded-lg border p-4 space-y-3" :class="$styles.chromeBorder">
              <div class="flex items-center justify-between"><div><h3 class="font-semibold">Deployment</h3><p class="text-xs" :class="$styles.muted">Embed this Search widget using its generated script.</p></div><span class="text-xs font-medium" :class="archived ? 'text-orange-600 dark:text-orange-400' : draft.published ? 'text-green-600' : $styles.muted">{{archived ? 'Archived' : draft.published ? 'Published' : 'Draft'}}</span></div>
              <template v-if="draft.published">
                <div class="relative"><textarea readonly rows="3" :value="draft.embedCode" class="w-full rounded-md border bg-gray-50 px-2.5 py-1.5 pr-9 font-mono text-xs font-normal dark:bg-gray-950" :class="$styles.chromeBorder"></textarea><button type="button" @click="copyEmbed" class="absolute right-2 top-2 rounded p-1 text-gray-500 hover:bg-black/5 hover:text-gray-900 dark:text-gray-400 dark:hover:bg-white/10 dark:hover:text-gray-200" :title="copiedEmbed ? 'Copied to clipboard' : 'Copy embed code'"><svg v-if="copiedEmbed" class="size-4 text-green-600 dark:text-green-500" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="m9.55 18l-5.7-5.7l1.425-1.425L9.55 15.15l9.175-9.175L20.15 7.4z"/></svg><svg v-else xmlns="http://www.w3.org/2000/svg" class="size-4" viewBox="0 0 24 24"><path fill="currentColor" d="M16 1H4c-1.1 0-2 .9-2 2v14h2V3h12zm3 4H8c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h11c1.1 0 2-.9 2-2V7c0-1.1-.9-2-2-2m0 16H8V7h11z"/></svg></button></div>
                <div class="flex gap-2"><button type="button" @click="save(false)" :disabled="saving" class="rounded-md border px-3 py-1.5 text-sm hover:bg-gray-50 disabled:opacity-50 dark:hover:bg-gray-800" :class="$styles.secondaryButton">Unpublish</button><button type="button" @click="regenerate" :disabled="saving" class="rounded-md border border-red-600 px-3 py-1.5 text-sm font-medium text-red-600 hover:bg-red-50 disabled:opacity-50 dark:hover:bg-red-950/30">Regenerate ID</button></div>
              </template>
              <button type="button" @click="copyEmbed(true)" class="rounded-md border px-3 py-1.5 text-sm" :class="$styles.secondaryButton">Run diagnostics</button>
              <div v-if="draft.diagnostics" class="overflow-hidden rounded-md border text-xs" :class="$styles.chromeBorder"><div v-for="check in draft.diagnostics.checks" :key="check.name" class="flex gap-2 border-b px-3 py-2 last:border-b-0" :class="$styles.chromeBorder"><span class="mt-1 size-2 shrink-0 rounded-full" :class="check.status === 'pass' ? 'bg-green-500' : check.status === 'warn' ? 'bg-amber-500' : 'bg-red-500'"></span><div><b>{{check.name}}</b><div :class="$styles.muted">{{check.message}}</div></div></div></div>
              <div class="flex flex-wrap gap-2"><button v-if="archived" type="button" @click="restore" :disabled="saving" class="rounded-md px-3 py-1.5 text-sm font-medium disabled:opacity-50" :class="$styles.secondaryButton">Restore Search</button><button v-else type="button" @click="archive" :disabled="saving" class="rounded-md border border-red-600 px-3 py-1.5 text-sm font-medium text-red-600 hover:bg-red-50 disabled:opacity-50 dark:hover:bg-red-950/30">Archive search</button><button type="button" @click="openDelete" :disabled="saving" class="rounded-md border border-red-600 bg-red-600 px-3 py-1.5 text-sm font-semibold text-white hover:border-red-700 hover:bg-red-700 disabled:opacity-50">Delete permanently</button></div>
            </section>
            <div class="flex flex-wrap items-center justify-end gap-2 border-t pt-4" :class="$styles.chromeBorder">
              <button v-if="!archived && !draft.published" type="button" @click="save(false)" :disabled="saving || !canSaveDraft" class="rounded-md border px-4 py-2 text-sm font-medium hover:bg-gray-50 disabled:opacity-50 dark:hover:bg-gray-800" :class="$styles.secondaryButton">Save draft</button>
              <button v-if="!archived" type="button" @click="save(true)" :disabled="saving || !canPublish" class="rounded-md px-4 py-2 text-sm font-semibold disabled:opacity-50" :class="$styles.primaryButton">{{draft.published ? 'Update published' : 'Publish'}}</button>
            </div>
          </form>

          <div class="space-y-4">
            <div class="rounded-xl border p-5" :class="$styles.chromeBorder"><div class="mb-3 text-xs font-semibold uppercase tracking-wide" :class="$styles.muted">Live preview</div><button type="button" @click="openPreview" :aria-label="draft.config.identity.placeholder" :style="previewLauncherStyle" class="inline-flex items-center gap-1.5 rounded-full px-2.5 py-1.5 transition-shadow"><svg viewBox="0 0 16 16" class="-ml-0.5 size-4 fill-current" :style="previewLauncherIconStyle"><path fill-rule="evenodd" d="M9.965 11.026a5 5 0 1 1 1.06-1.06l2.755 2.754a.75.75 0 1 1-1.06 1.06l-2.755-2.754ZM10.5 7a3.5 3.5 0 1 1-7 0 3.5 3.5 0 0 1 7 0Z" clip-rule="evenodd"/></svg><kbd v-if="shortcutLabel" class="text-[13px]/4">{{shortcutLabel}}</kbd></button></div>
            <fieldset :disabled="archived" class="rounded-xl border p-5 space-y-4 disabled:opacity-60" :class="$styles.chromeBorder">
              <div class="flex items-start justify-between gap-3"><div><h3 class="font-semibold">Result ranking</h3><p class="text-xs" :class="$styles.muted">Tune relevance and see the test results below refresh automatically.</p></div><button type="button" @click="resetRanking" class="shrink-0 text-xs underline" :class="$styles.muted">reset defaults</button></div>
              <div class="grid gap-4 sm:grid-cols-2">
                <div v-for="field in rankingFields" :key="field.key">
                  <div class="flex items-center justify-between gap-2"><span class="text-xs font-semibold">{{field.label}}</span><input v-model.number="draft.config.ranking[field.key]" type="number" :min="field.min" :max="field.max" :step="field.step" class="w-16 shrink-0 rounded-md px-2 py-0.5 text-right text-xs" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"></div>
                  <input v-model.number="draft.config.ranking[field.key]" type="range" :min="field.min" :max="field.max" :step="field.step" :aria-label="field.label" class="mt-2 h-1 w-full cursor-pointer appearance-none rounded-lg bg-gray-200 accent-blue-600 dark:bg-gray-700 dark:accent-blue-500">
                  <div class="mt-1 flex items-baseline justify-between gap-2 text-xs" :class="$styles.muted"><span>{{field.hint}}</span><span class="shrink-0 tabular-nums opacity-70">{{field.min}}-{{field.max}}</span></div>
                </div>
              </div>
              <div v-if="rankingDocTypes.length" class="space-y-2"><div class="text-xs font-semibold">Document type preference</div><div class="grid gap-2 sm:grid-cols-2"><div v-for="docType in rankingDocTypes" :key="docType" class="rounded-md border px-3 py-2 text-xs" :class="$styles.chromeBorder"><div class="flex items-center justify-between gap-3"><span class="min-w-0 truncate font-medium" :title="docType">{{docType}}</span><input type="number" :value="docTypeWeight(docType)" @input="setDocTypeWeight(docType,$event.target.value)" min="-20" max="50" step="0.5" class="w-16 shrink-0 rounded-md px-2 py-0.5 text-right text-xs" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"></div><input type="range" :value="docTypeWeight(docType)" @input="setDocTypeWeight(docType,$event.target.value)" min="-20" max="50" step="0.5" :aria-label="docType + ' weight'" class="mt-2 h-1 w-full cursor-pointer appearance-none rounded-lg bg-gray-200 accent-blue-600 dark:bg-gray-700 dark:accent-blue-500"></div></div><p class="text-xs" :class="$styles.muted">0 is neutral; positive values promote a type and negative values demote it.</p></div>
            </fieldset>
            <div class="rounded-xl border p-5" :class="$styles.chromeBorder"><div class="flex items-baseline justify-between gap-3"><label class="text-sm font-medium">Test the local index</label><span v-if="testQuery.trim()" class="text-xs tabular-nums" :class="$styles.muted">{{testResultCount}} {{testResultCount === 1 ? 'result' : 'results'}}</span></div><div class="mt-2 flex gap-2"><input type="search" v-model="testQuery" @keyup.enter="testSearch()" class="min-w-0 flex-1 rounded-md px-3 py-2" :class="[$styles.bgInput,$styles.textInput,$styles.borderInput]"><button type="button" @click="testSearch()" class="rounded-md px-3" :class="$styles.secondaryButton">Search</button></div><div v-for="group in testGroups" :key="group.documentId" class="mt-3"><div class="text-sm font-semibold">{{group.title}}</div><button v-for="item in group.items" :key="item.id" type="button" @click="openResult(item,group)" class="mt-1 flex w-full items-baseline gap-2 rounded bg-gray-50 px-2 py-1 text-left text-xs outline-offset-[-2px] hover:outline-2 hover:outline-gray-400 focus-visible:outline-2 focus-visible:outline-gray-400 dark:bg-gray-900 dark:hover:outline-gray-500 dark:focus-visible:outline-gray-500"><span class="w-5 shrink-0 text-right tabular-nums" :class="$styles.muted">{{resultIndex(item) + 1}}</span><span class="min-w-0 flex-1 truncate"><span v-for="(part,i) in resultParts(item,'snippet')" :key="i" :style="part.match ? adminMatchStyle : null">{{part.text}}</span></span><span v-if="item.score != null" class="shrink-0 font-semibold tabular-nums" :class="$styles.muted" title="Ranking score">{{formatScore(item.score)}}</span></button></div></div>
          </div>
          </div>
        </div>
        <Teleport to="body">
          <div v-if="previewOpen" class="fixed inset-0 z-[220] flex items-start justify-center bg-black/60 px-4 pt-[8vh]" @click.self="previewOpen=false" @keydown.esc.stop.prevent="escapePreview">
            <div class="flex max-h-[78vh] w-full max-w-3xl flex-col overflow-hidden rounded-2xl border shadow-2xl" :style="previewDialogStyle">
              <div class="flex items-center gap-3 border-b px-5 py-4" :style="previewBorderStyle"><svg class="size-6" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="m20 20-4-4"/></svg><input type="text" ref="previewInput" v-model="testQuery" @keydown="onPreviewInputKeydown" :placeholder="draft.config.identity.placeholder" :style="{color:previewPalette.text,borderColor:'transparent',boxShadow:'none'}" class="min-w-0 flex-1 !border-0 bg-transparent text-xl !outline-none !ring-0 placeholder:opacity-60 focus:!border-0 focus:!outline-none focus:!ring-0"><button type="button" @click="previewOpen=false" aria-label="Close search" class="rounded-md border px-2 py-1 font-sans text-xs leading-4" :style="[previewMutedStyle,previewBorderStyle]">esc</button></div>
              <div ref="previewResults" @scroll.passive="onPreviewResultsScroll" class="overflow-y-auto p-3"><div v-for="group in testGroups" :key="group.documentId" class="mb-3"><h3 class="mb-1 px-1 text-lg" :style="previewMutedStyle">{{group.title}}</h3><button v-for="item in group.items" :key="item.id" type="button" :data-result-index="resultIndex(item)" @mouseenter="selectResult(resultIndex(item))" @click="openResult(item,group)" :style="[previewResultStyle,isSelected(item) ? previewSelectedStyle : null]" class="mb-1 flex w-full gap-3 rounded-lg p-3 text-left text-sm"><span class="grid size-5 shrink-0 place-items-center text-xl" :style="previewMutedStyle"><svg v-if="item.type === 'doc'" width="20" height="20" viewBox="0 0 20 20"><path d="M17 6v12c0 .52-.2 1-1 1H4c-.7 0-1-.33-1-1V2c0-.55.42-1 1-1h8l5 5zM14 8h-3.13c-.51 0-.87-.34-.87-.87V4" stroke="currentColor" fill="none" fill-rule="evenodd" stroke-linejoin="round"></path></svg><svg v-else-if="item.type === 'heading'" width="20" height="20" viewBox="0 0 20 20"><path d="M13 13h4-4V8H7v5h6v4-4H7V8H3h4V3v5h6V3v5h4-4v5zm-6 0v4-4H3h4z" stroke="currentColor" fill="none" fill-rule="evenodd" stroke-linecap="round" stroke-linejoin="round"></path></svg><svg v-else xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 512 512"><path d="M0 0h512v512H0z" fill="none"></path><path fill="currentColor" d="M80 96h352v32H80zm0 144h352v32H80zm0 144h352v32H80z"></path></svg></span><span class="min-w-0"><span class="block truncate"><span v-for="(part,i) in resultParts(item,'snippet')" :key="i" :style="part.match ? previewMatchStyle : null">{{part.text}}</span></span><span class="block truncate text-xs" :style="previewMutedStyle"><span v-for="(part,i) in resultParts(item,'title')" :key="i" :style="part.match ? previewMatchStyle : null">{{part.text}}</span></span></span></button></div><div v-if="testLoadingMore" class="p-3 text-center text-xs" :style="previewMutedStyle">Loading more…</div><div v-if="!testGroups.length" class="p-10 text-center text-sm" :style="previewMutedStyle">{{testQuery ? draft.config.identity.emptyText : draft.config.identity.title}}</div></div>
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
    const testHasMore = ref(false), testNextSkip = ref(0), testLoadingMore = ref(false)
    const documentPreview = ref(null), previewLoading = ref(false)
    const previewInput = ref(null), previewResults = ref(null), previewDocumentBody = ref(null), selectedResult = ref(-1)
    const copiedEmbed = ref(false), deleteOpen = ref(false), deleteBusy = ref(false), deleteConfirmation = ref('')
    const activeSection = ref('edit'), searchesLoading = ref(false), searchAnalytics = ref(null)
    const searchSections = computed(() => [
      { value: 'edit', label: draft.value?.id ? 'Edit Search' : 'New Search' },
      { value: 'searches', label: 'Customer searches' },
      { value: 'analytics', label: 'Website Analytics' }])
    const analyticsBusy = ref(false), trafficPeriod = ref('30d'), trafficChart = ref(null)
    const visitorPage = ref(0), visitorPageSize = 10
    const trafficRanges = [{ value: '1d', label: '24 hours' }, { value: '7d', label: '7 days' }, { value: '30d', label: '30 days' }, { value: '90d', label: '90 days' }]
    let trafficChartInstance = null
    const savedSnapshot = ref('')
    const formSnapshot = computed(() => JSON.stringify({ name: String(draft.value?.name || '').trim(), config: draft.value?.config || null }))
    const dirty = computed(() => formSnapshot.value !== savedSnapshot.value)
    const archived = computed(() => !!draft.value?.id && Number(draft.value.enabled) === 0)
    const canSaveDraft = computed(() => !archived.value && !!String(draft.value?.name || '').trim() && (dirty.value || !draft.value?.id))
    const canPublish = computed(() => !archived.value && !!String(draft.value?.name || '').trim() && (dirty.value || !draft.value?.published))
    const themes = ['auto', 'light', 'dark', 'nord', 'matrix', 'soft-pink']
    const isMac = /Mac|iPhone|iPad|iPod/i.test(navigator.userAgentData?.platform || navigator.platform || '')
    const colorSchemeMedia = matchMedia('(prefers-color-scheme: dark)')
    const savedColorScheme = () => { try { const value = localStorage.getItem('color-scheme'); return value === 'dark' || value === 'light' ? value : null } catch (_) { return null } }
    const pageDark = ref((savedColorScheme() || (colorSchemeMedia.matches ? 'dark' : 'light')) === 'dark')
    const previewTheme = computed(() => draft.value?.config?.appearance?.theme === 'auto' ? (pageDark.value ? 'dark' : 'light') : (draft.value?.config?.appearance?.theme || 'light'))
    const previewPalette = computed(() => palettes[previewTheme.value] || palettes.light)
    const previewIsDark = computed(() => ['dark', 'nord', 'matrix'].includes(previewTheme.value))
    const fontFamily = computed(() => draft.value?.config?.appearance?.fontFamily || SYSTEM_FONT)
    const hasFontFamily = computed(() => !!draft.value?.config?.appearance?.fontFamily)
    const previewDialogStyle = computed(() => ({ backgroundColor: previewPalette.value.bg, color: previewPalette.value.text, borderColor: previewPalette.value.border, colorScheme: previewIsDark.value ? 'dark' : 'light', fontFamily: fontFamily.value }))
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
    const previewIsFlat = computed(() => (draft.value?.config?.appearance?.launcherStyle || 'flat') === 'flat')
    const previewLauncherShadow = computed(() => {
      const ring = `inset 0 0 0 1px ${previewPalette.value.border}`
      const style = draft.value?.config?.appearance?.launcherStyle
      if (style === 'raised') return `0 8px 24px #0000001f, ${ring}`
      if (style === 'inset') return previewIsDark.value ? `inset 0 2px 5px #00000080, ${ring}` : `inset 0 2px 4px #0f172a29, ${ring}`
      return ring
    })
    const previewLauncherStyle = computed(() => ({ backgroundColor: previewIsFlat.value ? previewPalette.value.bg : previewPalette.value.surface, color: previewPalette.value.muted, boxShadow: previewLauncherShadow.value, paddingRight: slashOnly.value ? '14px' : '10px', fontFamily: fontFamily.value }))
    const previewLauncherIconStyle = computed(() => ({ color: `color-mix(in srgb, ${previewPalette.value.text} 45%, ${previewPalette.value.muted})` }))
    const previewItems = computed(() => testGroups.value.flatMap(group => (group.items || []).map(item => ({ item, group }))))
    const testResultCount = computed(() => previewItems.value.length)
    const shortcutLabel = computed(() => commandKEnabled.value ? (isMac ? '⌘K' : 'Ctrl K') : slashOnly.value ? '/' : '')
    const rankingFields = [
      { key: 'titleWeight', label: 'Title weight', hint: 'Matches in the document title.', min: 0, max: 50, step: 0.5 },
      { key: 'headingWeight', label: 'Heading weight', hint: 'Matches in section headings.', min: 0, max: 50, step: 0.5 },
      { key: 'contentWeight', label: 'Content weight', hint: 'Matches in body text.', min: 0, max: 50, step: 0.5 },
      { key: 'phraseBoost', label: 'Exact phrase boost', hint: 'Query words found together.', min: 0, max: 50, step: 0.5 },
      { key: 'exactTitleBoost', label: 'Exact title boost', hint: 'The whole title equals the query.', min: 0, max: 50, step: 0.5 },
      { key: 'freshnessWeight', label: 'Freshness weight', hint: 'Preference for recently updated sources.', min: 0, max: 50, step: 0.5 },
      { key: 'freshnessHalfLifeDays', label: 'Freshness half-life (days)', hint: 'The freshness boost halves after this many days.', min: 1, max: 3650, step: 1 },
      { key: 'nativeWeight', label: 'Database relevance weight', hint: "Retains a small preference for the provider's native FTS order.", min: 0, max: 20, step: 0.5 },
    ]
    const rankingDocTypes = computed(() => [...new Set([
      ...facetOptions('docType').map(x => String(x.value)),
      ...Object.keys(draft.value?.config?.ranking?.docTypeWeights || {}),
    ])].filter(Boolean).sort((a, b) => a.localeCompare(b)))
    const scopeSummary = computed(() => Object.entries(draft.value?.config?.scope || {}).filter(([, value]) => value).map(([key, value]) => `${key} = ${value}`).join(' · ') || 'All documents in this File Store')
    const origins = computed({ get: () => draft.value?.config.hosting.allowedOrigins.join('\n') || '', set: v => { if (draft.value) draft.value.config.hosting.allowedOrigins = String(v).split(/[,\n]/).map(x => x.trim()).filter(Boolean) } })
    const analyticsList = key => computed({ get: () => draft.value?.config?.analytics?.[key]?.join('\n') || '', set: v => { if (draft.value) draft.value.config.analytics[key] = [...new Set(String(v).split(/\r?\n/).map(x => x.trim()).filter(Boolean))] } })
    const deniedUserAgents = analyticsList('deniedUserAgents')
    const deniedIpRanges = analyticsList('deniedIpRanges')
    const excludedPaths = analyticsList('excludedPaths')
    const statusCards = computed(() => [
      { label: 'Documents', value: Number(index.value.documents || 0).toLocaleString(), health: indexHealth.value },
      { label: 'Indexed', value: Number(index.value.indexed || 0).toLocaleString() },
      { label: 'Pending', value: Number(index.value.pending || 0).toLocaleString() },
      { label: 'Failed', value: Number(index.value.failed || 0).toLocaleString() },
      { label: 'Sections', value: Number(index.value.sections || 0).toLocaleString() },
      { label: 'Provider', value: index.value.provider || '—' },
    ])
    const indexHealth = computed(() => {
      const failed = Number(index.value.failed || 0), pending = Number(index.value.pending || 0)
      if (failed) return { label: 'Index needs attention', detail: `${failed} document${failed === 1 ? '' : 's'} failed indexing. Rebuild after correcting the source or parser error.`, dot: 'bg-red-500', tone: 'border-red-300 bg-red-50 dark:border-red-900 dark:bg-red-950/20' }
      if (pending) return { label: 'Index is updating', detail: `${pending} document${pending === 1 ? '' : 's'} queued${index.value.oldestPendingAt ? ` since ${formatRelativeTime(index.value.oldestPendingAt)}` : ''}.`, dot: 'bg-amber-500', tone: 'border-amber-300 bg-amber-50 dark:border-amber-900 dark:bg-amber-950/20' }
      return { label: 'Index healthy', detail: index.value.lastIndexedAt ? `Last indexed ${formatRelativeTime(index.value.lastIndexedAt)}.` : 'Ready for imported documents.', dot: 'bg-green-500', tone: 'border-green-300 bg-green-50 dark:border-green-900 dark:bg-green-950/20' }
    })
    const searchMetrics = computed(() => [
      { label: 'Total searches', value: Number(searchAnalytics.value?.total || 0).toLocaleString() },
      { label: 'Result clicks', value: Number(searchAnalytics.value?.totalClicks || 0).toLocaleString() },
      { label: 'Search CTR', value: `${Number(searchAnalytics.value?.clickThroughRate || 0).toLocaleString()}%` },
      { label: 'Related intents', value: Number(searchAnalytics.value?.relatedGroups || 0).toLocaleString() },
      { label: 'No results', value: Number(searchAnalytics.value?.noResults || 0).toLocaleString() },
      { label: 'Avg. results', value: Number(searchAnalytics.value?.averageResults || 0).toLocaleString() },
    ])
    const analyticsEnabled = computed(() => draft.value?.config?.analytics?.enabled === true)
    const trafficMetrics = computed(() => {
      const value = searchAnalytics.value?.traffic || {}
      return [
        { label: 'Page views', value: Number(value.pageViews || 0).toLocaleString() },
        { label: 'Visitors', value: Number(value.visitors || 0).toLocaleString() },
        { label: 'New visitors', value: Number(value.newVisitors || 0).toLocaleString() },
        { label: 'Sessions', value: Number(value.sessions || 0).toLocaleString() },
        { label: 'Pages/session', value: Number(value.pagesPerSession || 0).toLocaleString() },
        { label: 'Bounce rate', value: `${Number(value.bounceRate || 0).toLocaleString()}%` },
        { label: 'Avg. load', value: value.averageLoadMs ? `${Number(value.averageLoadMs).toLocaleString()} ms` : '—' },
      ]
    })
    const visitorTotal = computed(() => Number(searchAnalytics.value?.traffic?.recentTotal ?? searchAnalytics.value?.traffic?.pageViews ?? 0))
    const visitorRows = computed(() => searchAnalytics.value?.traffic?.recentPageViews || [])
    const visitorStart = computed(() => visitorRows.value.length ? visitorPage.value * visitorPageSize + 1 : 0)
    const visitorEnd = computed(() => visitorPage.value * visitorPageSize + visitorRows.value.length)
    const canPreviousVisitors = computed(() => visitorPage.value > 0)
    const canNextVisitors = computed(() => visitorEnd.value < visitorTotal.value)
    const audienceGroups = computed(() => {
      const value = searchAnalytics.value?.traffic || {}
      return [
        { label: 'Devices', items: value.devices || [] },
        { label: 'Languages', items: value.languages || [] },
        { label: 'Timezones', items: value.timezones || [] },
        { label: 'Platforms', items: value.platforms || [] },
        { label: 'Connections', items: value.connections || [] },
        { label: 'Campaigns', items: value.campaigns || [] },
      ]
    })
    const geographyGroups = computed(() => {
      const value = searchAnalytics.value?.traffic || {}
      return [
        { label: 'Countries', items: value.countries || [] },
        { label: 'Regions', items: value.regions || [] },
        { label: 'Cities', items: value.cities || [] },
        { label: 'Networks', items: value.organizations || [] },
      ]
    })
    const geoLocation = item => [...new Set([item?.city, item?.region, item?.country].filter(Boolean))].join(', ')
    const geoDetails = item => {
      const coordinates = item?.latitude != null && item?.longitude != null ? `${item.latitude}, ${item.longitude}` : ''
      return [item?.countryCode, item?.postalCode, item?.timeZone, coordinates].filter(Boolean).join(' · ')
    }
    async function load() { const [list, status] = await Promise.all([ext.getJson(`/filestores/${props.storeId}/searches`), ext.getJson(`/filestores/${props.storeId}/search-index`)]); if (!list.error) { widgets.value = list.response || []; emit('count', widgets.value.filter(x => x.enabled !== 0).length) } if (!status.error) index.value = status.response || {}; if (props.routeSearch && String(draft.value?.id || '') !== String(props.routeSearch)) { const found = widgets.value.find(x => String(x.id) === String(props.routeSearch)); if (found) edit(found) } }
    function markClean() { savedSnapshot.value = formSnapshot.value }
    function destroyTrafficChart() { trafficChartInstance?.destroy(); trafficChartInstance = null }
    function clearSearchAnalytics() { searchAnalytics.value = null; visitorPage.value = 0; destroyTrafficChart() }
    function edit(widget, section = 'edit') { if (draft.value?.id !== widget.id) clearSearchAnalytics(); activeSection.value = section; draft.value = clone(widget); draft.value.config = { ...defaults(), ...draft.value.config, identity: { ...defaults().identity, ...draft.value.config?.identity }, scope: { ...draft.value.config?.scope }, ranking: { ...defaults().ranking, ...draft.value.config?.ranking, docTypeWeights: { ...draft.value.config?.ranking?.docTypeWeights } }, behavior: { ...defaults().behavior, ...draft.value.config?.behavior }, analytics: { ...defaults().analytics, ...draft.value.config?.analytics }, appearance: { ...defaults().appearance, ...draft.value.config?.appearance, offset: { ...defaults().appearance.offset, ...draft.value.config?.appearance?.offset } }, hosting: { ...defaults().hosting, ...draft.value.config?.hosting } }; markClean(); deleteOpen.value = false; deleteConfirmation.value = ''; emit('navigate', { search: widget.id }) }
    function newWidget() { clearSearchAnalytics(); activeSection.value = 'edit'; draft.value = { name: 'Documentation Search', published: false, enabled: 1, config: defaults() }; markClean(); deleteOpen.value = false; deleteConfirmation.value = ''; emit('navigate', { search: null }) }
    function close() { clearSearchAnalytics(); activeSection.value = 'edit'; draft.value = null; deleteOpen.value = false; deleteConfirmation.value = ''; emit('navigate', { search: null }) }
    async function loadSearchAnalytics() {
      if (!draft.value?.id) return
      searchesLoading.value = true
      try {
        const visitorSkip = visitorPage.value * visitorPageSize
        const api = await ext.getJson(`/searches/${draft.value.id}/analytics?groupTake=50&recentTake=100&period=${trafficPeriod.value}&visitorSkip=${visitorSkip}&visitorTake=${visitorPageSize}`)
        if (api.error) return ext.setError(api.error)
        searchAnalytics.value = api.response || {}
        const count = Number(searchAnalytics.value.total || 0)
        draft.value.searchCount = count
        widgets.value = widgets.value.map(x => x.id === draft.value.id ? { ...x, searchCount: count } : x)
        await nextTick()
        if (activeSection.value === 'analytics') renderTrafficChart()
      } finally { searchesLoading.value = false }
    }
    async function selectSearchSection(section) {
      if (section !== 'edit' && !draft.value?.id) return
      activeSection.value = section
      destroyTrafficChart()
      if (section === 'analytics') visitorPage.value = 0
      if (section !== 'edit') await loadSearchAnalytics()
    }
    function formatSearchDate(value) { if (!value) return 'unknown'; const date = new Date(value); return Number.isNaN(date.getTime()) ? String(value) : date.toLocaleString() }
    function formatRelativeTime(value) {
      if (!value) return ''
      const date = new Date(value)
      if (Number.isNaN(date.getTime())) return String(value)
      const seconds = Math.round((date.getTime() - Date.now()) / 1000)
      const relative = new Intl.RelativeTimeFormat(undefined, { numeric: 'auto' })
      const ranges = [[60, 'second', 1], [60, 'minute', 60], [24, 'hour', 3600], [30, 'day', 86400], [12, 'month', 2592000], [Infinity, 'year', 31536000]]
      const absolute = Math.abs(seconds)
      for (const [limit, unit, divisor] of ranges) {
        const value = Math.round(seconds / divisor)
        if (absolute < limit * divisor) return relative.format(value, unit)
      }
      return ''
    }
    function searchSource(item) { const value = item?.pageUrl || item?.origin; if (!value) return ''; try { const url = new URL(value); return `${url.host}${url.pathname === '/' ? '' : url.pathname}` } catch (_) { return String(value) } }
    function trafficLabel(value) { if (!value) return 'Direct'; try { const url = new URL(value); return url.host + (url.pathname === '/' ? '' : url.pathname) } catch (_) { return String(value) } }
    function trafficBucketLabel(value) {
      if (!value) return ''
      const hourly = searchAnalytics.value?.traffic?.bucket === 'hour'
      const date = new Date(hourly ? `${value}:00:00` : `${value}T00:00:00`)
      return hourly ? date.toLocaleTimeString([], { hour: 'numeric' }) : date.toLocaleDateString([], { month: 'short', day: 'numeric' })
    }
    function renderTrafficChart() {
      destroyTrafficChart()
      const timeline = searchAnalytics.value?.traffic?.timeline || []
      if (!trafficChart.value || !timeline.length) return
      const text = pageDark.value ? '#d1d5db' : '#4b5563'
      const grid = pageDark.value ? 'rgba(156,163,175,.16)' : 'rgba(107,114,128,.14)'
      trafficChartInstance = new Chart(trafficChart.value.getContext('2d'), {
        type: 'line',
        data: {
          labels: timeline.map(x => trafficBucketLabel(x.bucket)), datasets: [
            { label: 'Page views', data: timeline.map(x => x.pageViews), borderColor: colors[0].border, backgroundColor: colors[0].background, borderWidth: 1, fill: true, tension: .25, pointRadius: 2 },
            { label: 'Visitors', data: timeline.map(x => x.visitors), borderColor: colors[1].border, backgroundColor: colors[1].background, borderWidth: 1, tension: .25, pointRadius: 2 },
          ]
        },
        options: { responsive: true, maintainAspectRatio: false, interaction: { mode: 'index', intersect: false }, plugins: { legend: { labels: { color: text } } }, scales: { x: { ticks: { color: text, maxTicksLimit: 12 }, grid: { color: grid } }, y: { beginAtZero: true, ticks: { color: text, precision: 0 }, grid: { color: grid } } } },
      })
    }
    async function selectTrafficPeriod(value) { if (trafficPeriod.value === value) return; trafficPeriod.value = value; visitorPage.value = 0; await loadSearchAnalytics() }
    async function previousVisitors() { if (!canPreviousVisitors.value || searchesLoading.value) return; visitorPage.value--; await loadSearchAnalytics() }
    async function nextVisitors() { if (!canNextVisitors.value || searchesLoading.value) return; visitorPage.value++; await loadSearchAnalytics() }
    async function setTrafficAnalytics(enabled) {
      if (!draft.value?.id || archived.value) return
      const previous = analyticsEnabled.value
      draft.value.config.analytics.enabled = enabled
      analyticsBusy.value = true
      try {
        const saved = await save(!!draft.value.published, { successMessage: enabled ? 'Website analytics enabled' : 'Website analytics disabled' })
        if (!saved) draft.value.config.analytics.enabled = previous
      } finally { analyticsBusy.value = false }
    }
    async function purgeAnalytics() {
      if (!draft.value?.id || !confirm('Permanently clear all retained searches, clicks, and page views for this Search widget?')) return
      analyticsBusy.value = true
      try {
        const api = await ext.postJson(`/searches/${draft.value.id}/analytics/clear`, {})
        if (api.error) return ext.setError(api.error)
        clearSearchAnalytics(); await loadSearchAnalytics(); await load()
        ctx?.toast?.('Search analytics cleared')
      } finally { analyticsBusy.value = false }
    }
    function excludeCurrentIp() {
      const ip = String(draft.value?.requestIp || '').trim()
      if (!ip) return
      const values = draft.value.config.analytics.deniedIpRanges || (draft.value.config.analytics.deniedIpRanges = [])
      if (!values.some(x => String(x).toLowerCase() === ip.toLowerCase())) values.push(ip)
      ctx?.toast?.(`${ip} added to denied IP ranges`)
    }
    async function save(published, extra = {}) {
      if (archived.value) return ext.setError({ message: 'Restore this Search widget before editing or publishing it' })
      const name = String(draft.value?.name || '').trim()
      if (!name) { ext.setError({ message: 'Name is required' }); return false }
      const existed = !!draft.value.id, wasPublished = !!draft.value.published
      saving.value = true
      try {
        const { successMessage, ...requestExtra } = extra
        const body = { name, published, config: clone(draft.value.config), ...requestExtra }
        const api = draft.value.id ? await ext.putJson(`/searches/${draft.value.id}`, body) : await ext.postJson(`/filestores/${props.storeId}/searches`, body)
        if (api.error) { ext.setError(api.error); return false }
        const section = activeSection.value
        await load()
        const fresh = widgets.value.find(x => x.id === api.response.id) || api.response
        edit(fresh, section)
        const message = successMessage || (requestExtra.regeneratePublicId ? 'Search ID regenerated' : published ? (wasPublished ? 'Published Search updated' : 'Search published') : wasPublished ? 'Search unpublished' : existed ? 'Draft saved' : 'Draft created')
        ctx?.toast?.(message)
        return true
      } finally { saving.value = false }
    }
    async function archive() {
      if (!draft.value?.id || archived.value || !confirm('Archive this Search widget? Its public widget will stop working.')) return
      const widgetId = draft.value.id
      saving.value = true
      let api
      try { api = await ext.deleteJson(`/searches/${widgetId}`) } finally { saving.value = false }
      if (api.error) return ext.setError(api.error)
      await load()
      const fresh = widgets.value.find(x => x.id === widgetId)
      if (fresh) edit(fresh)
      ctx?.toast?.('Search archived')
    }
    async function restore() {
      if (!draft.value?.id || !archived.value) return
      const widgetId = draft.value.id
      saving.value = true
      let api
      try { api = await ext.postJson(`/searches/${widgetId}/restore`, {}) } finally { saving.value = false }
      if (api.error) return ext.setError(api.error)
      await load()
      const fresh = widgets.value.find(x => x.id === widgetId) || api.response
      edit(fresh)
      ctx?.toast?.('Search restored as a draft')
    }
    async function rebuild() { rebuilding.value = true; try { const api = await ext.postJson(`/filestores/${props.storeId}/search-index/rebuild`, {}); if (api.error) return ext.setError(api.error); await load() } finally { rebuilding.value = false } }
    let testTimer = 0, testRequest = 0
    function mergeTestGroups(incoming) {
      const groups = testGroups.value.map(group => ({ ...group, items: [...(group.items || [])] }))
      const groupLimit = Number(draft.value?.config?.behavior?.groupLimit || 8)
      for (const incomingGroup of incoming || []) {
        let group = groups.find(x => String(x.documentId) === String(incomingGroup.documentId))
        if (!group) { group = { ...incomingGroup, items: [] }; groups.push(group) }
        const ids = new Set(group.items.map(x => String(x.id)))
        for (const item of incomingGroup.items || []) {
          if (group.items.length >= groupLimit) break
          if (!ids.has(String(item.id))) { group.items.push(item); ids.add(String(item.id)) }
        }
      }
      return groups
    }
    async function testSearch(append = false) {
      append = append === true
      const query = testQuery.value.trim()
      if (append && (!testHasMore.value || testLoadingMore.value)) return
      const request = append ? testRequest : ++testRequest
      const skip = append ? testNextSkip.value : 0
      if (!append) { selectedResult.value = -1; testHasMore.value = false; testNextSkip.value = 0 }
      if (!query) { testGroups.value = []; return }
      testLoadingMore.value = append
      const ranking = encodeURIComponent(JSON.stringify(draft.value?.config?.ranking || {}))
      const take = Number(draft.value?.config?.behavior?.maxResults || 30)
      const api = await ext.getJson(`/filestores/${props.storeId}/search?q=${encodeURIComponent(query)}&ranking=${ranking}&take=${take}&skip=${skip}`)
      testLoadingMore.value = false
      if (request !== testRequest || query !== testQuery.value.trim()) return
      if (api.error) return ext.setError(api.error)
      testGroups.value = append ? mergeTestGroups(api.response?.groups) : (api.response?.groups || [])
      testHasMore.value = api.response?.hasMore === true
      testNextSkip.value = Number(api.response?.nextSkip || 0)
      if (!append) selectResult(previewItems.value.length ? 0 : -1)
      nextTick(maybeLoadMoreTestResults)
    }
    function maybeLoadMoreTestResults() {
      const element = previewResults.value
      if (previewOpen.value && element && element.scrollTop + element.clientHeight >= element.scrollHeight - 120) testSearch(true)
    }
    function onPreviewResultsScroll() { maybeLoadMoreTestResults() }
    async function openPreview() { previewOpen.value = true; await nextTick(); previewInput.value?.focus(); if (previewItems.value.length && selectedResult.value < 0) selectResult(0); if (testQuery.value.trim()) testSearch() }
    async function closeDocumentPreview() { documentPreview.value = null; previewLoading.value = false; await nextTick(); if (previewOpen.value) previewInput.value?.focus() }
    function escapePreview() { if (documentPreview.value || previewLoading.value) closeDocumentPreview(); else previewOpen.value = false }
    function openExternalUrl(value) { try { const url = new URL(value, location.href); if (url.protocol === 'http:' || url.protocol === 'https:') window.open(url.href, '_blank', 'noopener,noreferrer')?.focus() } catch (_) { } }
    async function openResult(item, group) {
      if (item.url) { openExternalUrl(item.url); return }
      if (!item.previewUrl) return
      previewLoading.value = true; documentPreview.value = null
      try { const response = await fetch(item.previewUrl, { headers: { Accept: 'application/json' } }); if (!response.ok) throw new Error(`Document preview failed (${response.status})`); const data = await response.json(); documentPreview.value = { title: data.title || group.title, markdown: data.markdown || '' } }
      catch (error) { ext.setError(error) } finally { previewLoading.value = false; await nextTick(); previewDocumentBody.value?.focus({ preventScroll: true }) }
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
        if (deleteOpen.value) return
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
    async function copyEmbed(diagnostics = false) { if (diagnostics === true) { const api = await ext.getJson(`/searches/${draft.value.id}/diagnostics`); if (api.error) return ext.setError(api.error); draft.value.diagnostics = api.response; return } await navigator.clipboard.writeText(draft.value.embedCode); copiedEmbed.value = true; setTimeout(() => copiedEmbed.value = false, 2000) }
    async function regenerate() { if (confirm('Regenerate the public ID? Existing embed codes will stop working.')) await save(true, { regeneratePublicId: true }) }
    function openDelete() { if (!draft.value?.id) return; deleteConfirmation.value = ''; deleteOpen.value = true }
    function closeDelete() { if (deleteBusy.value) return; deleteOpen.value = false; deleteConfirmation.value = '' }
    async function deletePermanently() {
      if (!draft.value?.id || deleteConfirmation.value !== draft.value.name) return
      deleteBusy.value = true
      let api
      try {
        api = await ext.deleteJson(`/searches/${draft.value.id}/permanent`, { headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ confirm: deleteConfirmation.value }) })
      } finally { deleteBusy.value = false }
      if (api.error) return ext.setError(api.error)
      close()
      await load()
      ctx?.toast?.('Search deleted')
    }
    function setHighlightColor(value) { if (validColor(value)) draft.value.config.appearance.highlightColor = value }
    function setHighlightColorText(event) { const value = String(event.target.value || '').trim(); if (validColor(value)) setHighlightColor(value); else event.target.value = highlightColorValue.value }
    function resetHighlightColor() { draft.value.config.appearance.highlightColor = '' }
    function setFontFamily(value) { draft.value.config.appearance.fontFamily = String(value || '').replace(/[\x00-\x1f{};]/g, '').trim().slice(0, 300) }
    function resetFontFamily() { draft.value.config.appearance.fontFamily = '' }
    function facetOptions(field) { return (props.facets?.[field]?.values || []).map(x => typeof x === 'object' ? x : { value: x, count: '' }) }
    function docTypeWeight(docType) { return Number(draft.value?.config?.ranking?.docTypeWeights?.[docType] || 0) }
    function setDocTypeWeight(docType, value) { const weight = Math.min(50, Math.max(-20, Number(value) || 0)); const weights = draft.value.config.ranking.docTypeWeights; if (weight) weights[docType] = weight; else delete weights[docType] }
    function resetRanking() { draft.value.config.ranking = clone(defaults().ranking) }
    function formatScore(score) { const value = Number(score); return Number.isFinite(value) ? value.toFixed(1) : '' }
    watch(testQuery, () => { clearTimeout(testTimer); selectedResult.value = -1; testTimer = setTimeout(testSearch, 180) })
    watch(() => draft.value?.config?.ranking, () => { if (testQuery.value.trim()) { clearTimeout(testTimer); testTimer = setTimeout(testSearch, 180) } }, { deep: true })
    watch(pageDark, () => nextTick(renderTrafficChart))
    const syncPageTheme = () => pageDark.value = (savedColorScheme() || (colorSchemeMedia.matches ? 'dark' : 'light')) === 'dark'
    const onStorage = event => { if (event.key === 'color-scheme') { syncPageTheme(); nextTick(renderTrafficChart) } }
    const themeObserver = new MutationObserver(syncPageTheme)
    watch(() => props.storeId, load); onMounted(() => { load(); window.addEventListener('keydown', onKeydown, true); window.addEventListener('storage', onStorage); colorSchemeMedia.addEventListener?.('change', syncPageTheme); document.addEventListener('visibilitychange', syncPageTheme); themeObserver.observe(document.documentElement, { attributes: true, attributeFilter: ['class', 'style'] }) }); onUnmounted(() => { clearTimeout(testTimer); destroyTrafficChart(); window.removeEventListener('keydown', onKeydown, true); window.removeEventListener('storage', onStorage); colorSchemeMedia.removeEventListener?.('change', syncPageTheme); document.removeEventListener('visibilitychange', syncPageTheme); themeObserver.disconnect() })
    return { widgets, draft, editing, activeSection, searchSections, selectSearchSection, saving, copiedEmbed, dirty, archived, canSaveDraft, canPublish, deleteOpen, deleteBusy, deleteConfirmation, index, rebuilding, testQuery, testGroups, testLoadingMore, previewOpen, documentPreview, previewLoading, previewInput, previewResults, previewDocumentBody, selectedResult, themes, isMac, origins, deniedUserAgents, deniedIpRanges, excludedPaths, excludeCurrentIp, statusCards, searchesLoading, searchAnalytics, searchMetrics, analyticsBusy, analyticsEnabled, trafficPeriod, trafficRanges, trafficChart, trafficMetrics, visitorTotal, visitorStart, visitorEnd, canPreviousVisitors, canNextVisitors, previousVisitors, nextVisitors, audienceGroups, geographyGroups, geoLocation, geoDetails, trafficLabel, selectTrafficPeriod, setTrafficAnalytics, purgeAnalytics, previewPalette, previewIsDark, previewDialogStyle, previewBorderStyle, previewMutedStyle, adminMatchStyle, previewMatchStyle, highlightColorValue, hasHighlightColor, fontFamily, hasFontFamily, previewResultStyle, previewSelectedStyle, previewLauncherStyle, previewLauncherIconStyle, shortcutLabel, testResultCount, formatScore, rankingFields, rankingDocTypes, docTypeWeight, setDocTypeWeight, resetRanking, offsetSides: ['top', 'right', 'bottom', 'left'], scopeFields: SCOPE_FIELDS, scopeSummary, facetOptions, edit, newWidget, close, save, archive, restore, rebuild, loadSearchAnalytics, formatSearchDate, formatRelativeTime, searchSource, testSearch, onPreviewResultsScroll, openPreview, openResult, resultParts, resultIndex, isSelected, selectResult, onPreviewInputKeydown, closeDocumentPreview, escapePreview, copyEmbed, regenerate, openDelete, closeDelete, deletePermanently, setHighlightColor, setHighlightColorText, resetHighlightColor, setFontFamily, resetFontFamily }
  }
}
