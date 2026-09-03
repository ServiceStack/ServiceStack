import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { MetadataInput, MetadataDialog, IMPORT_FIELDS, summariseMetadata, loadFacets } from './metadata.mjs'
import { Popover, RootsPanel, PathText, CheckBox } from './explorer.mjs'

let ext = null
export function initImport(extScope) {
    ext = extScope
}

// Vue wraps form values in reactive proxies, which structuredClone() rejects in browsers.
// Crawl rules are deliberately JSON-only because they are persisted in import.json.
const cloneJson = value => JSON.parse(JSON.stringify(value ?? null))

// One entry per import option. `fields` drives the form, so a tab only ever shows what that
// option actually needs - the folder tab has no upload zone, the upload tab has no path.
export const IMPORT_TABS = [
    {
        id: 'upload', label: 'Upload files',
        blurb: 'Drop files in, including a .zip archive - which are expanded and individually imported with its folder structure becoming the category.',
        recurring: false, fields: [],
    },
    {
        id: 'folder', label: 'Folder', sourceType: 'folder',
        blurb: 'Index a folder on this machine and keep it in sync.',
        recurring: true,
        // A `pair` shares one grid cell, so the two settings that both shape the category sit
        // together and the glob fields get a row of their own.
        fields: [
            { key: 'path', label: 'Folder path', placeholder: '/Users/me/src/docs', mono: true, required: true,
              hint: 'Must be inside an allowed folder', roots: true },
            { pair: [
                // Condensed, with the full explanation on hover: the long form was the field's
                // whole second line and still didn't say what it meant to anyone new.
                { key: 'root', label: 'Category root', placeholder: 'docs', mono: true,
                  hint: 'Trimmed from each category',
                  title: 'Only files under this subfolder are imported, and this prefix is removed '
                       + 'from every category. With a root of "docs", the file docs/guides/auth.md '
                       + 'lands in the category guides.' },
                { key: 'maxDepth', label: 'Max depth', type: 'number', min: 0, step: 1,
                  placeholder: 'unlimited', width: 'w-28 shrink-0', hint: '0 = this folder only' },
            ] },
            { key: 'include', label: 'Include only', placeholder: '**/*.md', mono: true },
            { key: 'exclude', label: 'Exclude', placeholder: '**/drafts/**', mono: true },
        ],
    },
    {
        id: 'crawl', label: 'Web crawl',
        blurb: 'Fetch a website into an inspectable Markdown folder, transform it, then import it.',
        recurring: false, fields: [],
    },
]

export const ImportPanel = {
    components: { MetadataDialog, MetadataInput, Popover, RootsPanel, PathText, CheckBox },
    template: `
        <div class="rounded-lg border overflow-hidden" :class="[$styles.chromeBorder]">
            <div class="flex border-b" :class="[$styles.chromeBorder]">
                <button v-for="t in tabs" :key="t.id" type="button" @click="select(t.id)"
                    class="px-4 py-2.5 text-sm font-medium whitespace-nowrap border-b-2 -mb-px inline-flex items-center gap-1.5"
                    :class="tab === t.id
                        ? 'border-blue-500 text-blue-600 dark:text-blue-400'
                        : 'border-transparent hover:bg-gray-100 dark:hover:bg-gray-800'">
                    <!-- SVGs rather than emoji: crisper, themeable, and consistent with the
                         chevrons and folder icons elsewhere in the extension. -->
                    <svg v-if="t.id === 'upload'" class="size-4 opacity-70" viewBox="0 0 24 24" fill="none"
                        stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                        <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                        <polyline points="14 2 14 8 20 8"/>
                        <line x1="12" y1="18" x2="12" y2="12"/>
                        <polyline points="9 15 12 12 15 15"/>
                    </svg>
                    <svg v-else-if="t.id === 'folder'" class="size-4 opacity-70" viewBox="0 0 24 24" fill="none"
                        stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                        <path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/>
                    </svg>
                    <svg v-else class="size-4 opacity-70" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                        stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                        <circle cx="12" cy="12" r="9"/><path d="M3 12h18M12 3a15 15 0 0 1 0 18M12 3a15 15 0 0 0 0 18"/>
                    </svg>
                    {{ t.label }}
                    <span v-if="t.unavailable" class="ml-1 text-xs" :class="[$styles.muted]">· unavailable</span>
                </button>
            </div>

            <div class="p-5 space-y-4">
                <p class="text-sm" :class="[$styles.muted]">{{ active.blurb }}</p>

                <div v-if="active.unavailable" class="px-3 py-2 rounded border text-sm border-amber-500 text-amber-700 dark:text-amber-400 bg-amber-50 dark:bg-amber-900/20">
                    This import option isn't available on this machine{{ active.reason ? ': ' + active.reason : '' }}.
                </div>

                <template v-else>
                    <div v-if="tab === 'crawl'" class="space-y-4">
                        <div class="grid sm:grid-cols-[1fr_14rem_7rem] gap-3">
                            <div><label class="block text-xs font-semibold mb-1">Start URL</label>
                                <input type="url" v-model="crawlForm.url" @input="deriveCrawlName" placeholder="https://docs.example.org/"
                                    class="w-full px-2.5 py-1.5 rounded-md text-sm border-2 bg-white dark:bg-gray-900" :class="[$styles.chromeBorder]"></div>
                            <div><label class="block text-xs font-semibold mb-1">Import folder</label>
                                <input type="text" v-model="crawlForm.name" @input="crawlNameEdited = true" placeholder="docs.example.org"
                                    class="w-full px-2.5 py-1.5 rounded-md text-sm border-2 bg-white dark:bg-gray-900" :class="[$styles.chromeBorder]"></div>
                            <div><label class="block text-xs font-semibold mb-1">Max pages</label>
                                <input v-model.number="crawlForm.maxPages" type="number" min="1" max="10000"
                                    class="w-full px-2.5 py-1.5 rounded-md text-sm border-2 bg-white dark:bg-gray-900" :class="[$styles.chromeBorder]"></div>
                        </div>
                        <div class="grid sm:grid-cols-2 gap-3">
                            <div><label class="block text-xs font-semibold mb-1">Include paths</label>
                                <input type="text" v-model="crawlForm.includeText" placeholder="/** — comma or newline separated"
                                    class="w-full px-2.5 py-1.5 rounded-md text-sm font-mono border-2 bg-white dark:bg-gray-900" :class="[$styles.chromeBorder]"></div>
                            <div><label class="block text-xs font-semibold mb-1">Exclude paths</label>
                                <input type="text" v-model="crawlForm.excludeText" placeholder="e.g. /archives/**, /account/**"
                                    class="w-full px-2.5 py-1.5 rounded-md text-sm font-mono border-2 bg-white dark:bg-gray-900" :class="[$styles.chromeBorder]"></div>
                            <div class="grid grid-cols-[10rem_1fr] gap-2">
                                <div><label class="block text-xs font-semibold mb-1">Query strings</label>
                                    <select v-model="crawlForm.queryMode" class="w-full px-2.5 py-1.5 rounded-md text-sm border-2 bg-white dark:bg-gray-900" :class="[$styles.chromeBorder]">
                                        <option value="ignore">Ignore</option><option value="allow">Allow selected</option><option value="all">Include all</option>
                                    </select></div>
                                <div><label class="block text-xs font-semibold mb-1">Allowed parameters</label>
                                    <input type="text" v-model="crawlForm.queryAllowText" :disabled="crawlForm.queryMode !== 'allow'" placeholder="version, lang"
                                        class="w-full px-2.5 py-1.5 rounded-md text-sm font-mono border-2 bg-white dark:bg-gray-900 disabled:opacity-50" :class="[$styles.chromeBorder]"></div>
                            </div>
                            <div class="grid grid-cols-[7rem_1fr] gap-2">
                                <div><label class="block text-xs font-semibold mb-1">Max depth</label>
                                    <input v-model.number="crawlForm.maxDepth" type="number" min="0" max="100"
                                        class="w-full px-2.5 py-1.5 rounded-md text-sm border-2 bg-white dark:bg-gray-900" :class="[$styles.chromeBorder]"></div>
                                <div><label class="block text-xs font-semibold mb-1">Additional hosts</label>
                                    <input type="text" v-model="crawlForm.allowedHostsText" placeholder="cdn.example.org"
                                        class="w-full px-2.5 py-1.5 rounded-md text-sm font-mono border-2 bg-white dark:bg-gray-900" :class="[$styles.chromeBorder]"></div>
                            </div>
                        </div>
                        <div class="flex flex-wrap gap-x-5 gap-y-2 text-sm">
                            <label class="inline-flex items-center gap-2"><CheckBox v-model="crawlForm.respectRobots"/> Respect robots.txt</label>
                            <label class="inline-flex items-center gap-2"><CheckBox v-model="crawlForm.respectNoIndex"/> Respect noindex</label>
                            <label class="inline-flex items-center gap-2"><CheckBox v-model="crawlForm.followNoFollow"/> Follow nofollow links</label>
                            <label class="inline-flex items-center gap-2"><CheckBox v-model="crawlForm.useCanonical"/> Use canonical URLs</label>
                            <label class="inline-flex items-center gap-2"><CheckBox v-model="crawlForm.dedupeContent"/> Remove duplicate content</label>
                        </div>
                        <div>
                            <div class="text-xs font-semibold mb-2">Additional crawl rules</div>
                            <JsonSchemaForm :schema="crawlRuleSchema" :data="crawlRules"
                                :show-title="false" @change="setCrawlRules" />
                        </div>
                        <p v-if="crawlError" class="text-xs text-red-600">{{ crawlError }}</p>
                        <button type="button" @click="startCrawl" :disabled="busy || !crawlForm.url"
                            class="px-4 py-2 rounded-md text-sm font-semibold text-white bg-blue-600 hover:bg-blue-700 disabled:opacity-40">
                            {{ busy ? 'Crawling…' : 'Crawl website' }}
                        </button>

                        <div class="border-t pt-4" :class="[$styles.chromeBorder]">
                            <div class="flex items-center justify-between mb-2"><h3 class="text-sm font-semibold">Saved crawl imports</h3>
                                <button type="button" @click="loadImports" class="text-xs underline" :class="[$styles.muted]">refresh</button></div>
                            <p v-if="!crawlImports.length" class="text-sm" :class="[$styles.muted]">No crawled imports yet.</p>
                            <div v-else class="grid sm:grid-cols-[14rem_1fr] gap-4">
                                <div class="space-y-1">
                                    <button v-for="item in crawlImports" :key="item.name" type="button" @click="toggleImport(item)"
                                        class="w-full text-left rounded-md px-3 py-2" :class="[$styles.secondaryButton, selectedImport?.name === item.name ? 'bg-blue-50 dark:bg-blue-900/20' : '']">
                                        <span class="block text-sm font-medium truncate">{{ item.name }}</span>
                                        <span class="text-xs" :class="[$styles.muted]">{{ item.pages }} page{{ item.pages === 1 ? '' : 's' }}</span>
                                    </button>
                                </div>
                                <div v-if="selectedImport" class="space-y-3 min-w-0">
                                    <div class="text-xs font-mono break-all" :class="[$styles.muted]">{{ selectedImport.path }}</div>
                                    <label class="block text-xs font-semibold">Regex transforms</label>
                                    <JsonSchemaForm :schema="transformSchema" :data="transforms"
                                        :show-title="false" @change="setTransforms" />
                                    <p class="text-xs" :class="[$styles.muted]">Applying transforms also saves them to this crawl's import.json.</p>
                                    <ErrorSummary v-if="transformError" :status="transformError" />
                                    <p v-else-if="transformMessage" class="text-xs text-green-600 dark:text-green-400">{{ transformMessage }}</p>
                                    <div class="flex gap-2 flex-wrap">
                                        <button type="button" @click="viewCrawledPages" class="px-3 py-1.5 rounded-md text-sm border font-medium inline-flex items-center gap-1.5" :class="[$styles.secondaryButton]">
                                            <svg class="size-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                                                <path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12z"/><circle cx="12" cy="12" r="3"/>
                                            </svg>
                                            View crawled pages
                                        </button>
                                        <button type="button" @click="applyTransforms" class="px-3 py-1.5 rounded-md text-sm" :class="[$styles.secondaryButton]">Apply &amp; save transforms</button>
                                        <button type="button" @click="importCrawlFolder" class="px-3 py-1.5 rounded-md text-sm" :class="[$styles.primaryButton]">Import this folder</button>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- Upload: the drop zone lives in its own tab now -->
                    <div v-if="tab === 'upload'">
                        <input type="file" ref="fileInput" class="hidden" multiple
                            accept=".zip,.pdf,.md,.mdx,.markdown,.txt,.html,.htm,.rst,.adoc,.csv,.json,.yaml,.yml"
                            @change="onFiles">
                        <div @click="fileInput?.click()" @dragover.prevent="dragover = true"
                            @dragleave.prevent="dragover = false" @drop.prevent="onDrop"
                            class="border-2 border-dashed rounded-lg p-8 text-center cursor-pointer transition"
                            :class="dragover ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20' : [$styles.chromeBorder]">
                            <svg class="size-10 mx-auto mb-2 text-gray-300 dark:text-gray-600" viewBox="0 0 24 24" fill="none"
                                stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                                <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                                <polyline points="14 2 14 8 20 8"/>
                                <line x1="12" y1="18" x2="12" y2="12"/>
                                <polyline points="9 15 12 12 15 15"/>
                            </svg>
                            <p class="text-sm font-medium" :class="[$styles.heading]">
                                <span :class="[$styles.linkHover]">Choose files</span> or drag and drop
                            </p>
                            <p class="text-xs mt-1" :class="[$styles.muted]">PDFs, text, Markdown, HTML — or a .zip of them</p>
                            <p v-if="files.length" class="text-xs mt-2 text-blue-600 dark:text-blue-400">
                                {{ files.length }} file{{ files.length === 1 ? '' : 's' }} ready<span v-if="hasArchive"> — archives are expanded on upload</span>
                            </p>
                        </div>
                    </div>

                    <!-- Everything else is driven by the tab's own field list -->
                    <div v-else-if="tab !== 'crawl'" class="grid sm:grid-cols-2 gap-4">
                        <div v-for="cell in formCells" :key="cell.id" class="flex items-start gap-3"
                            :class="cell.wide ? 'sm:col-span-2' : ''">
                            <div v-for="f in cell.fields" :key="f.key" :class="f.width || 'grow min-w-0'">
                                <label class="block text-xs font-semibold mb-1">
                                    {{ f.label }}<span v-if="f.required" class="text-red-500">*</span>
                                </label>
                                <div class="flex items-center gap-1.5">
                                    <input v-model="config[f.key]" :type="f.type || 'text'" :placeholder="f.placeholder"
                                        :min="f.min" :step="f.step"
                                        class="grow min-w-0 px-2.5 py-1.5 rounded-md text-sm border-2 bg-white dark:bg-gray-900"
                                        :class="[$styles.chromeBorder, f.mono ? 'font-mono' : '']">
                                    <!-- A path you can only satisfy by guessing is a broken field.
                                         The allowed folders are the answer, but a dumped list ate
                                         the form - so they live one click away instead. -->
                                    <Popover v-if="f.roots" icon wide title="Where can I import from?" @open="loadRoots">
                                        <template #label>
                                            <svg class="size-[18px] opacity-70" viewBox="0 0 24 24" fill="none"
                                                stroke="currentColor" stroke-width="2" stroke-linecap="round"
                                                stroke-linejoin="round" aria-hidden="true">
                                                <path d="m6 14 1.5-2.9A2 2 0 0 1 9.24 10H20a2 2 0 0 1 1.94 2.5l-1.54 6a2 2 0 0 1-1.95 1.5H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h7a2 2 0 0 1 2 2v1"/>
                                            </svg>
                                        </template>
                                        <template #default="{ close }">
                                            <RootsPanel :roots="folderRoots" :unrestricted="rootsUnrestricted"
                                                @pick="p => { config[f.key] = p; close() }" />
                                        </template>
                                    </Popover>
                                </div>
                                <p v-if="f.roots" class="mt-0.5 text-xs" :class="[$styles.muted]">
                                    {{ rootsUnrestricted ? 'Any folder on this machine' : f.hint }}
                                </p>
                                <p v-else-if="f.hint" class="mt-0.5 text-xs" :class="[$styles.muted]"
                                    :title="f.title">{{ f.hint }}</p>
                            </div>
                        </div>
                    </div>

                    <!-- Import is where categories come from. Browsing one in Explore is a
                         read-only view of what was ingested; typing a new name here is how one
                         starts existing, by having documents put in it. -->
                    <div v-if="tab !== 'crawl'" class="sm:w-2/3">
                        <label class="block text-xs font-semibold mb-1">
                            Destination category
                            <span class="font-normal" :class="[$styles.muted]">— optional</span>
                        </label>
                        <MetadataInput :model-value="landingCategory || ''"
                            @update:modelValue="setLanding" :values="categoryValues"
                            placeholder="e.g. guides/auth — or leave empty" />
                        <p class="mt-0.5 text-xs" :class="[$styles.muted]">
                            <span v-if="tab === 'upload'">Where these files will live. Leave empty and a .zip keeps its own folder structure.</span>
                            <span v-else>The folder's own structure nests underneath this.</span>
                        </p>
                    </div>

                    <!-- Metadata: one button, then a read-optimised summary -->
                    <div v-if="tab !== 'crawl'" class="rounded-lg border p-3" :class="[$styles.chromeBorder]">
                        <div class="flex items-center justify-between gap-3 flex-wrap">
                            <div class="min-w-0">
                                <div class="text-xs font-semibold">Metadata</div>
                                <div v-if="!summary.length && !(metadata.rules || []).length" class="text-xs" :class="[$styles.muted]">
                                    None set — documents will import unlabelled
                                </div>
                                <div v-else class="flex flex-wrap gap-1.5 mt-1">
                                    <span v-for="s in summary" :key="s.key"
                                        class="px-2 py-0.5 rounded-full text-xs border border-emerald-500 text-emerald-600 dark:text-emerald-400 bg-emerald-50 dark:bg-emerald-900/20">
                                        {{ s.label }}: <b>{{ s.value }}</b>
                                    </span>
                                    <span v-if="(metadata.rules || []).length"
                                        class="px-2 py-0.5 rounded-full text-xs border" :class="[$styles.chromeBorder]">
                                        +{{ metadata.rules.length }} rule{{ metadata.rules.length === 1 ? '' : 's' }} by path
                                    </span>
                                </div>
                            </div>
                            <button type="button" @click="dialogOpen = true"
                                class="px-3 py-1.5 rounded-md text-sm font-medium shrink-0" :class="[$styles.secondaryButton]">
                                {{ summary.length || (metadata.rules || []).length ? 'Edit metadata' : 'Add metadata' }}
                            </button>
                        </div>
                    </div>

                    <!-- Only offered where re-running actually means something -->
                    <label v-if="active.recurring" class="flex items-start gap-2.5 text-sm cursor-pointer">
                        <CheckBox v-model="saveSource" class="mt-0.5" />
                        <span>
                            <span class="font-medium">Save as a recurring import</span>
                            <span class="block text-xs" :class="[$styles.muted]">
                                Keep it so you can re-sync later and pick up changes. Unchecked, this is a one-off.
                            </span>
                        </span>
                    </label>
                    <div v-if="active.recurring && saveSource" class="sm:w-1/2">
                        <label class="block text-xs font-semibold mb-1">Name</label>
                        <input type="text" v-model="name" placeholder="Product docs"
                            class="w-full px-2.5 py-1.5 rounded-md" :class="[$styles.textInput, $styles.bgInput, $styles.borderInput]">
                    </div>

                    <div v-if="tab !== 'crawl'" class="flex items-center gap-3 pt-1">
                        <button type="button" @click="submit" :disabled="!canSubmit || busy"
                            class="px-4 py-2 rounded-md text-sm font-semibold text-white bg-blue-600 hover:bg-blue-700 disabled:opacity-40 disabled:cursor-not-allowed">
                            {{ busy ? busyLabel : submitLabel }}
                        </button>
                        <span v-if="tab !== 'upload'" class="text-xs" :class="[$styles.muted]">
                            Preview costs nothing — you confirm before anything is indexed.
                        </span>
                    </div>
                </template>
            </div>

            <MetadataDialog v-if="dialogOpen" v-model="metadata" :facets="facets" :fields="importFields"
                :show-rules="tab !== 'upload'" @close="dialogOpen = false" />

            <Teleport to="body">
            <div v-if="pageBrowserOpen"
                class="fixed inset-0 flex items-center justify-center p-4 md:p-8 lg:p-12 overflow-hidden bg-black/50 text-gray-900 dark:text-gray-100"
                style="z-index:200" @click.self="closePageBrowser">
                    <div class="relative bg-white dark:bg-gray-900 rounded-xl shadow-2xl w-full h-full max-w-7xl max-h-[92vh] flex flex-col overflow-hidden">
                        <div class="shrink-0 px-5 py-3 border-b flex items-center justify-between" :class="[$styles.chromeBorder]">
                            <div class="min-w-0">
                                <h2 class="text-lg font-semibold">Crawled pages</h2>
                                <p class="text-xs truncate" :class="[$styles.muted]">{{ selectedImport?.name }} · {{ pagePaths.length }} page{{ pagePaths.length === 1 ? '' : 's' }}</p>
                            </div>
                            <button type="button" @click="closePageBrowser" class="p-1.5 rounded-md text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800" title="Close">
                                <svg class="size-6" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true"><path d="M19 6.41 17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/></svg>
                            </button>
                        </div>
                        <div class="flex-1 grid grid-cols-[minmax(13rem,22rem)_1fr] min-h-0">
                            <div class="border-r overflow-y-auto py-2 bg-gray-50 dark:bg-gray-950" :class="[$styles.chromeBorder]">
                                <p v-if="pageBrowserBusy" class="px-4 py-3 text-sm" :class="[$styles.muted]">Loading pages…</p>
                                <p v-else-if="pageBrowserError" class="px-4 py-3 text-sm text-red-600">{{ pageBrowserError }}</p>
                                <p v-else-if="!pageEntries.length" class="px-4 py-3 text-sm" :class="[$styles.muted]">No crawled pages.</p>
                                <button v-for="entry in pageEntries" :key="entry.path" type="button"
                                    @click="entry.directory ? togglePageDirectory(entry.path) : selectCrawledPage(entry.path)"
                                    class="w-full flex items-center gap-1.5 pr-3 py-1.5 text-left text-sm hover:bg-gray-100 dark:hover:bg-gray-800"
                                    :class="!entry.directory && selectedPagePath === entry.path ? 'bg-blue-50 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300' : ''"
                                    :style="{ paddingLeft: (entry.depth * 16 + 12) + 'px' }">
                                    <svg v-if="entry.directory" class="size-4 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                                        <path d="M9 18l6-6-6-6" :class="pageDirectoriesClosed[entry.path] ? '' : 'rotate-90 origin-center'"/>
                                    </svg>
                                    <svg v-else class="size-4 shrink-0 opacity-60" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><path d="M14 2v6h6"/></svg>
                                    <span class="truncate">{{ entry.name }}</span>
                                </button>
                            </div>
                            <div class="min-w-0 min-h-0 flex flex-col bg-white dark:bg-gray-900">
                                <div v-if="selectedPagePath" class="shrink-0 px-4 py-2 border-b text-xs font-mono truncate" :class="[$styles.chromeBorder, $styles.muted]">{{ selectedPagePath }}</div>
                                <div v-if="pageContentBusy" class="p-6 text-sm" :class="[$styles.muted]">Loading page…</div>
                                <div v-else-if="selectedPagePath" class="flex-1 min-h-0 overflow-y-auto overflow-x-hidden p-5 text-sm leading-6 font-mono whitespace-pre-wrap break-words select-text">{{ selectedPageContent }}</div>
                                <div v-else class="flex-1 flex items-center justify-center text-sm" :class="[$styles.muted]">Select a page to view its contents.</div>
                            </div>
                        </div>
                    </div>
            </div>
            </Teleport>
        </div>
    `,
    props: { storeId: [String, Number], facets: Object, presetCategory: String,
        routeTab: String, routeCrawl: String },
    emits: ['previewing', 'preview', 'imported', 'navigate'],
    setup(props, { emit }) {
        const tabs = ref(IMPORT_TABS.map(t => ({ ...t })))
        // The URL is authoritative for reload/back/forward; preferences are only a fallback for
        // old links that predate deep-linking.
        const saved = props.routeCrawl ? 'crawl' : IMPORT_TABS.some(t => t.id === props.routeTab) ? props.routeTab
            : IMPORT_TABS.some(t => t.id === ext.prefs.importTab) ? ext.prefs.importTab : 'upload'
        const tab = ref(saved)
        const config = ref({})
        const metadata = ref({ defaults: {}, rules: [] })
        const saveSource = ref(false)
        const name = ref('')
        const files = ref([])
        const dragover = ref(false)
        const dialogOpen = ref(false)
        const busy = ref(false)
        const fileInput = ref(null)
        const crawlForm = ref({ url: '', name: '', maxPages: 500, maxDepth: 10,
            includeText: '/**', excludeText: '',
            queryMode: 'ignore', queryAllowText: '', allowedHostsText: '', respectRobots: true,
            respectNoIndex: true, followNoFollow: false, useCanonical: true, dedupeContent: true })
        const crawlNameEdited = ref(false)
        const crawlRuleSchema = ref({ type: 'array', items: { type: 'object' } })
        const crawlRules = ref([])
        const crawlError = ref('')
        const crawlImports = ref([])
        const selectedImport = ref(null)
        const transformSchema = ref({ type: 'array', items: { type: 'object' } })
        const transforms = ref([])
        const transformError = ref(null)
        const transformMessage = ref('')
        const pageBrowserOpen = ref(false)
        const pageBrowserBusy = ref(false)
        const pageBrowserError = ref('')
        const pagePaths = ref([])
        const pageDirectoriesClosed = ref({})
        const selectedPagePath = ref('')
        const selectedPageContent = ref('')
        const pageContentBusy = ref(false)

        const active = computed(() => tabs.value.find(t => t.id === tab.value) || tabs.value[0])
        const pageEntries = computed(() => {
            const tree = {}
            for (const path of pagePaths.value) {
                let node = tree
                for (const part of path.split('/')) node = node[part] ||= {}
            }
            const entries = []
            const visit = (node, parent = '', depth = 0) => {
                for (const name of Object.keys(node).sort((a, b) => {
                    const ad = Object.keys(node[a]).length > 0, bd = Object.keys(node[b]).length > 0
                    return ad === bd ? a.localeCompare(b) : ad ? -1 : 1
                })) {
                    const path = parent ? `${parent}/${name}` : name
                    const directory = Object.keys(node[name]).length > 0
                    entries.push({ name, path, depth, directory })
                    if (directory && !pageDirectoriesClosed.value[path]) visit(node[name], path, depth + 1)
                }
            }
            visit(tree)
            return entries
        })

        // Arriving from "Import into guides/auth": for an upload that's the category itself, for a
        // folder import it's a prefix the discovered structure nests under - build_plan derives
        // category from the path, so a plain default would just be overwritten.
        watch(() => props.presetCategory, cat => {
            const defaults = { ...(metadata.value.defaults || {}) }
            if (cat) defaults.category = cat
            else delete defaults.category
            metadata.value = {
                ...metadata.value,
                defaults,
            }
        }, { immediate: true })
        const hasArchive = computed(() => files.value.some(f => f.name.toLowerCase().endsWith('.zip')))
        // One entry per grid cell. A plain field is a cell of one; a `pair` puts two side by side.
        const formCells = computed(() => (active.value.fields || []).map(f => f.pair
            ? { id: f.pair.map(p => p.key).join('+'), fields: f.pair }
            : { id: f.key, fields: [f], wide: f.wide }))
        const summary = computed(() => summariseMetadata(metadata.value)
            .filter(s => !(s.key === 'category' && s.value === landingCategory.value)))
        const landingCategory = computed(() => metadata.value?.defaults?.category || null)
        function setLanding(value) {
            const defaults = { ...(metadata.value.defaults || {}) }
            if (value) defaults.category = value
            else delete defaults.category
            metadata.value = { ...metadata.value, defaults }
        }
        // Existing categories with their counts, so importing into one that already exists is
        // obvious - and a genuinely new name is flagged as new rather than typed blind.
        const categoryValues = computed(() => props.facets?.category?.values || [])
        const folderRoots = ref({})
        const rootsUnrestricted = ref(false)

        // Re-fetched every time the folder picker opens: trusted folders are editable in the
        // Trusted import folders section (and the config file), and the list here must show
        // those changes without a page refresh.
        async function loadRoots() {
            const api = await ext.getJson('/source-types')
            if (api.error) return {}
            const byType = Object.fromEntries((api.response || []).map(t => [t.type, t]))
            folderRoots.value = byType.folder?.roots || {}
            rootsUnrestricted.value = !!byType.folder?.unrestricted
            return byType
        }

        onMounted(async () => {
            // A source type that can't run here is shown disabled with the reason, rather than
            // being offered and failing on first use.
            const byType = await loadRoots()
            tabs.value = tabs.value.map(t => t.sourceType && byType[t.sourceType] && !byType[t.sourceType].available
                ? { ...t, unavailable: true, reason: byType[t.sourceType].reason }
                : t)
            await loadImports()
            const schema = await ext.getJson('/imports/schema')
            if (!schema.error) {
                if (schema.response?.rules) crawlRuleSchema.value = schema.response.rules
                if (schema.response?.transforms) transformSchema.value = schema.response.transforms
            }
            syncRouteState()
        })

        function onWindowKeydown(e) {
            if (e.key === 'Escape' && pageBrowserOpen.value) closePageBrowser()
        }
        window.addEventListener('keydown', onWindowKeydown)
        onUnmounted(() => window.removeEventListener('keydown', onWindowKeydown))

        function select(id) {
            tab.value = id
            config.value = {}
            ext.setPrefs({ importTab: id })
            emit('navigate', { import:id, crawl:id === 'crawl' ? selectedImport.value?.name || null : null })
        }

        function defaultCrawlName(url) {
            try { return new URL(url).host.toLowerCase().replace(/:/g, '-').replace(/[^a-z0-9._-]+/g, '-') }
            catch { return '' }
        }
        function deriveCrawlName() {
            if (!crawlNameEdited.value) crawlForm.value.name = defaultCrawlName(crawlForm.value.url)
        }
        async function loadImports() {
            const api = await ext.getJson('/imports')
            if (!api.error) {
                crawlImports.value = api.response || []
                syncRouteState()
            }
        }
        function showImport(item, navigate=false) {
            selectedImport.value = item
            transforms.value = cloneJson(item.config?.transforms || [])
            const saved = item.config?.crawl || {}
            if (saved.url) {
                crawlForm.value = { ...crawlForm.value, ...saved,
                    includeText: (saved.include || []).join(', '), excludeText: (saved.exclude || []).join(', '),
                    queryMode: saved.query?.mode || 'ignore', queryAllowText: (saved.query?.allow || []).join(', '),
                    allowedHostsText: (saved.allowedHosts || []).join(', ') }
                crawlRules.value = cloneJson(saved.rules || [])
                crawlNameEdited.value = true
            }
            transformError.value = null
            transformMessage.value = ''
            if (navigate) emit('navigate', { import:'crawl', crawl:item.name })
        }
        function openImport(item) { showImport(item, true) }
        function toggleImport(item) {
            if (selectedImport.value?.name === item.name) {
                selectedImport.value = null
                transforms.value = []
                transformError.value = null
                transformMessage.value = ''
                emit('navigate', { import:'crawl', crawl:null })
            } else showImport(item, true)
        }
        function syncRouteState() {
            const nextTab = props.routeCrawl ? 'crawl'
                : IMPORT_TABS.some(t => t.id === props.routeTab) ? props.routeTab : tab.value
            if (tab.value !== nextTab) {
                tab.value = nextTab
                config.value = {}
                ext.setPrefs({ importTab:nextTab })
            }
            if (nextTab !== 'crawl' || !props.routeCrawl) {
                selectedImport.value = null
                return
            }
            const item = crawlImports.value.find(x => x.name === props.routeCrawl)
            if (item && selectedImport.value?.name !== item.name) showImport(item)
            else if (!item) selectedImport.value = null
        }
        watch([() => props.routeTab, () => props.routeCrawl], syncRouteState)
        const splitValues = value => String(value || '').split(/[\n,]/).map(x => x.trim()).filter(Boolean)
        function setCrawlRules(value) { crawlRules.value = value }
        function setTransforms(value) {
            transforms.value = value
            transformError.value = null
            transformMessage.value = ''
        }
        async function startCrawl() {
            busy.value = true
            crawlError.value = ''
            try {
                const body = { ...crawlForm.value,
                    include: splitValues(crawlForm.value.includeText), exclude: splitValues(crawlForm.value.excludeText),
                    allowedHosts: splitValues(crawlForm.value.allowedHostsText), rules: cloneJson(crawlRules.value),
                    query: { mode: crawlForm.value.queryMode, allow: splitValues(crawlForm.value.queryAllowText),
                        exclude: ['utm_*', 'fbclid', 'gclid', 'ref', 'session', 'token'], maxVariantsPerPath: 5 } }
                for (const key of ['includeText', 'excludeText', 'allowedHostsText', 'queryMode', 'queryAllowText']) delete body[key]
                const api = await ext.postJson('/imports/crawl', body)
                if (api.error) return ext.setError(api.error)
                await loadImports()
                openImport(crawlImports.value.find(x => x.name === api.response.name) || api.response)
            } catch (e) { crawlError.value = e.message }
            finally { busy.value = false }
        }
        async function applyTransforms() {
            transformError.value = null
            transformMessage.value = ''
            try {
                const api = await ext.postJson(`/imports/${encodeURIComponent(selectedImport.value.name)}/transform`, {
                    transforms: cloneJson(transforms.value),
                })
                if (api.error) {
                    transformError.value = api.error
                    return
                }
                selectedImport.value = { ...selectedImport.value, config: api.response.config }
                transformMessage.value = `${api.response.changed} page${api.response.changed === 1 ? '' : 's'} updated. Transforms saved to import.json.`
            } catch (e) {
                transformError.value = { errorCode: 'Error', message: e.message || String(e) }
            }
        }
        function importCrawlFolder() {
            const item = selectedImport.value
            if (!item) return
            tab.value = 'folder'
            ext.setPrefs({ importTab: 'folder' })
            emit('navigate', { import:'folder', crawl:null })
            config.value = { path: item.path }
            metadata.value = item.config?.metadata || { defaults: {}, rules: [] }
        }
        async function viewCrawledPages() {
            if (!selectedImport.value) return
            pageBrowserOpen.value = true
            pageBrowserBusy.value = true
            pageBrowserError.value = ''
            pagePaths.value = []
            selectedPagePath.value = ''
            selectedPageContent.value = ''
            const api = await ext.getJson(`/imports/${encodeURIComponent(selectedImport.value.name)}/pages`)
            pageBrowserBusy.value = false
            if (api.error) {
                pageBrowserError.value = api.error.message || String(api.error)
                return
            }
            pagePaths.value = api.response?.pages || []
            if (pagePaths.value.length) await selectCrawledPage(pagePaths.value[0])
        }
        function closePageBrowser() { pageBrowserOpen.value = false }
        function togglePageDirectory(path) {
            pageDirectoriesClosed.value = { ...pageDirectoriesClosed.value, [path]: !pageDirectoriesClosed.value[path] }
        }
        async function selectCrawledPage(path) {
            selectedPagePath.value = path
            selectedPageContent.value = ''
            pageContentBusy.value = true
            const api = await ext.getJson(`/imports/${encodeURIComponent(selectedImport.value.name)}/page?path=${encodeURIComponent(path)}`)
            pageContentBusy.value = false
            if (api.error) {
                selectedPageContent.value = api.error.message || String(api.error)
                return
            }
            if (selectedPagePath.value === path) selectedPageContent.value = api.response?.content || ''
        }

        const canSubmit = computed(() => {
            if (active.value.unavailable) return false
            if (tab.value === 'upload') return files.value.length > 0
            return (active.value.fields || []).every(f => !f.required || String(config.value[f.key] || '').trim())
        })
        const submitLabel = computed(() => tab.value === 'upload'
            ? `Upload ${files.value.length || ''} file${files.value.length === 1 ? '' : 's'}`.replace('  ', ' ')
            : 'Preview import')
        const busyLabel = computed(() => tab.value === 'upload' ? 'Uploading…' : 'Scanning…')

        function onFiles(e) { files.value = [...e.target.files] }
        function onDrop(e) { dragover.value = false; files.value = [...e.dataTransfer.files] }

        /** Rule rows from the dialog -> the shape build_plan() expects. */
        function rulesPayload() {
            const defaults = { ...(metadata.value.defaults || {}) }
            // On a source import the category comes from the path (prefixed above), so leaving it
            // in defaults would be a value that silently never applies.
            if (tab.value !== 'upload') delete defaults.category
            return {
                defaults,
                rules: (metadata.value.rules || []).map(r => r.field
                    ? { match: r.match, set: { [r.field]: ['versions', 'tags'].includes(r.field)
                        ? String(r.value || '').split(',').map(s => s.trim()).filter(Boolean) : r.value } }
                    : { match: r.match, skip: true }),
            }
        }

        async function submit() {
            busy.value = true
            try {
                if (tab.value === 'upload') return await uploadFiles()
                await previewSource()
            } finally { busy.value = false }
        }

        async function uploadFiles() {
            const form = new FormData()
            for (const [k, v] of Object.entries(metadata.value.defaults || {})) {
                if (v === '' || v == null) continue
                form.append(k, Array.isArray(v) ? v.join(',') : v)
            }
            for (const f of files.value) form.append('file', f)
            // Use the extension scope so uploads respect the host's configured route prefix
            // (e.g. /chat/ext/gemini instead of assuming /ext/gemini is mounted at the root).
            const res = await ext.postForm(`/filestores/${props.storeId}/upload`, { body: form })
            const api = await ext.createJsonResult(res)
            if (api.error) return ext.setError(api.error)
            const payload = api.response
            files.value = []
            if (fileInput.value) fileInput.value.value = ''
            emit('imported', { queued: Array.isArray(payload) ? payload.length : 0, category: landingCategory.value || null })
        }

        async function previewSource() {
            const cfg = config.value
            emit('previewing')
            const defaultName = cfg.path?.split('/').filter(Boolean).pop() || active.value.label
            // Provisional one-off sources are deleted after the confirmed run. Give them an
            // internal unique name so they can preview the same folder as an existing saved
            // import without weakening saved-import name uniqueness.
            const sourceName = saveSource.value
                ? name.value || defaultName
                : `${defaultName} (one-off ${Date.now().toString(36)})`
            const body = {
                filestoreId: Number(props.storeId),
                name: sourceName,
                type: active.value.sourceType,
                config: {
                    path: cfg.path,
                    include: cfg.include ? [cfg.include] : null,
                    exclude: cfg.exclude ? [cfg.exclude] : null,
                    metadataSpecified: !!(Object.keys(metadata.value.defaults || {}).length
                        || (metadata.value.rules || []).length),
                },
                category: {
                    root: cfg.root || null,
                    maxDepth: cfg.maxDepth !== '' && cfg.maxDepth != null ? Number(cfg.maxDepth) : null,
                    prefix: landingCategory.value || null,
                },
                rules: rulesPayload(),
                // A one-off is still a source row - it's just deleted once it has run, which keeps
                // one code path instead of two.
                // Whether this is kept is decided when the import is confirmed, not here - the
                // source row exists either way, because running the pipeline needs one.
            }
            const created = await ext.postJson('/sources', body)
            if (created.error) return ext.setError(created.error)
            const run = await ext.postJson(`/sources/${created.response.id}/run`, { dryRun: true })
            if (run.error) return ext.setError(run.error)
            emit('preview', { source: created.response, run: run.response, keep: saveSource.value })
        }

        function resetAfterImport() {
            saveSource.value = false
            name.value = ''
        }

        return {
            tabs, tab, active, config, metadata, saveSource, name, files, dragover, dialogOpen,
            busy, fileInput, hasArchive, formCells, summary, canSubmit,
            crawlForm, crawlNameEdited, crawlRuleSchema, crawlRules, crawlError, crawlImports, selectedImport,
            transformSchema, transforms, transformError, transformMessage,
            pageBrowserOpen, pageBrowserBusy, pageBrowserError, pagePaths, pageDirectoriesClosed,
            pageEntries, selectedPagePath, selectedPageContent, pageContentBusy,
            deriveCrawlName, loadImports, openImport, toggleImport, setCrawlRules, setTransforms, startCrawl, applyTransforms, importCrawlFolder,
            viewCrawledPages, closePageBrowser, togglePageDirectory, selectCrawledPage,
            importFields: IMPORT_FIELDS,
            landingCategory, setLanding, categoryValues, folderRoots, rootsUnrestricted, loadRoots,
            submitLabel, busyLabel, select, onFiles, onDrop, submit, resetAfterImport,
        }
    },
}
