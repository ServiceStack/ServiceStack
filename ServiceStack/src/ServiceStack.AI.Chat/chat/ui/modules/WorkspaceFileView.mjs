import { computed, inject, ref, watch, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { directoryBreadcrumbs, workspaceQuery } from './explorerState.mjs'
import { highlightSource } from './sourceHighlight.mjs'

export default {
    setup() {
        const ctx = inject('ctx'), route = useRoute(), router = useRouter()
        const data = ref(null), error = ref(''), loading = ref(false)
        const renderSvg = ref(true), imageError = ref(false)
        try { renderSvg.value = localStorage.getItem('llms.workspace.svgView') !== 'text' } catch {}
        function toggleSvg() {
            renderSvg.value = !renderSvg.value
            try { localStorage.setItem('llms.workspace.svgView', renderSvg.value ? 'render' : 'text') } catch {}
        }
        const isSvg = computed(() => data.value?.file?.mimeType === 'image/svg+xml')
        const showingImage = computed(() => !!data.value?.file?.image && (!isSvg.value || renderSvg.value))
        watch(showingImage, () => { imageError.value = false })
        const lineWrap = ref(true)
        try { lineWrap.value = localStorage.getItem('llms.workspace.lineWrap') !== 'false' } catch {}
        function toggleWrap() {
            lineWrap.value = !lineWrap.value
            try { localStorage.setItem('llms.workspace.lineWrap', String(lineWrap.value)) } catch {}
        }
        const highlighted = computed(() => highlightSource(data.value?.file?.content, data.value?.file?.name))
        const xml = computed(() => /\.(?:html?|xml|svg|vue|xaml|csproj|fsproj|props|targets)$/i.test(data.value?.file?.name || ''))
        const copied = ref(false)
        let copyTimer
        let generation = 0
        function resetCopy() { clearTimeout(copyTimer); copied.value = false }
        onUnmounted(() => { generation++; resetCopy() })
        async function copyFileContents() {
            const content = data.value?.file?.content, current = generation
            if (loading.value || content == null) return
            resetCopy()
            try {
                try {
                    await navigator.clipboard.writeText(content)
                } catch {
                    // Match message copying's fallback for browsers without clipboard access.
                    const textArea = document.createElement('textarea'), focused = document.activeElement
                    textArea.value = content
                    textArea.style.cssText = 'position:fixed;opacity:0;pointer-events:none'
                    document.body.appendChild(textArea)
                    try {
                        textArea.select()
                        if (!document.execCommand('copy')) throw new Error('Clipboard access is unavailable')
                    } finally { textArea.remove(); focused?.focus({ preventScroll: true }) }
                }
                if (current !== generation) return
                clearTimeout(copyTimer)
                copied.value = true
                copyTimer = setTimeout(() => { copied.value = false }, 2000)
            } catch {
                if (current === generation) ctx.toast('Unable to copy file contents')
            }
        }
        const breadcrumbs = computed(() => directoryBreadcrumbs(data.value?.path, data.value?.roots.find(r => data.value.path === r || data.value.path.startsWith(r.replace(/[\\/]+$/, '') + (r.includes('\\') ? '\\' : '/')))))
        function navigate(path) { router.push({ query: workspaceQuery(route.query, { workspacePath: path, workspaceFile: null, workspacePreview: null, workspaceCommit: null, workspaceView: 'files' }) }) }
        watch(() => [route.query.workspaceFile, route.query.workspacePath, route.query.workspaceProject], async () => {
            const current = ++generation
            resetCopy()
            data.value = null; error.value = ''; imageError.value = false; loading.value = true
            const query = new URLSearchParams({ file: route.query.workspaceFile, path: route.query.workspacePath })
            if (route.query.workspaceProject) query.set('projectId', route.query.workspaceProject)
            try {
                const api = await ctx.getJson('/ext/projects/explorer?' + query)
                if (generation !== current) return
                if (!api.response) throw new Error(api.error?.message || 'Unable to read file')
                data.value = api.response
            } catch (e) { if (generation === current) error.value = e.message }
            finally { if (generation === current) loading.value = false }
        }, { immediate: true })
        return { data, error, loading, breadcrumbs, navigate, highlighted, xml, lineWrap, toggleWrap, renderSvg, toggleSvg, isSvg, showingImage, imageError, copied, copyFileContents }
    },
    template: `<section aria-label="Workspace file" class="h-full flex flex-col min-w-0">
        <div :class="[$styles.chromeBorder, $styles.cardTitleActive]" data-workspace-location data-workspace-file-location class="flex items-center h-[35px] py-0 px-2 shrink-0 pl-4 pr-1 border-b gap-2">
            <nav aria-label="File breadcrumbs" class="flex items-center gap-2 text-sm flex-1 min-w-0 overflow-hidden">
                <template v-for="crumb in breadcrumbs" :key="crumb.path"><button type="button" @click="navigate(crumb.path)" :title="crumb.path" class="max-w-full truncate text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200 transition-colors">{{crumb.name}}</button><span>/</span></template>
                <span class="font-mono truncate" :title="data?.file?.name">{{data?.file?.name}}</span>
            </nav>
            <button v-if="isSvg" type="button" @click="toggleSvg" aria-label="Toggle SVG render view" :aria-pressed="renderSvg" :title="renderSvg ? 'View SVG source' : 'Render SVG'"  :class="[renderSvg ? $styles.iconActive : $styles.icon, $styles.iconHover]" data-workspace-icon class="flex items-center justify-center w-7 h-7 p-1 border border-transparent rounded shrink-0 cursor-pointer disabled:opacity-45 disabled:cursor-default">
                <svg v-if="renderSvg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"  class="w-4.5 h-4.5"><path d="m8 7-5 5 5 5m8-10 5 5-5 5m-3-13-2 16"/></svg>
                <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"  class="w-4.5 h-4.5"><rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="8" cy="8" r="1.5"/><path d="m21 15-5-5L5 21"/></svg>
            </button>
            <button v-if="!showingImage" type="button" @click="toggleWrap" aria-label="Toggle line wrap" :aria-pressed="lineWrap" :title="lineWrap ? 'Disable line wrap' : 'Enable line wrap'" :disabled="loading || data?.file?.content == null"  :class="[lineWrap ? $styles.iconActive : $styles.icon, $styles.iconHover]" data-workspace-icon class="flex items-center justify-center w-7 h-7 p-1 border border-transparent rounded shrink-0 cursor-pointer disabled:opacity-45 disabled:cursor-default">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"  class="w-4.5 h-4.5"><path d="M3 5h18M3 10h13a4 4 0 0 1 0 8h-4m3-3-3 3 3 3M3 15h5M3 20h5"/></svg>
            </button>
            <button v-if="!showingImage" type="button" @click="copyFileContents" aria-label="Copy file contents" :title="copied ? 'Copied' : 'Copy file contents'" :disabled="loading || data?.file?.content == null"  :class="[$styles.icon, $styles.iconHover]" data-workspace-icon class="flex items-center justify-center w-7 h-7 p-1 border border-transparent rounded shrink-0 cursor-pointer disabled:opacity-45 disabled:cursor-default">
                <svg v-if="copied"  fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg" aria-hidden="true"  class="w-4.5 h-4.5 text-green-500 dark:text-green-400"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/></svg>
                <svg v-else xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true"  class="w-4.5 h-4.5"><rect width="14" height="14" x="8" y="8" rx="2" ry="2"/><path d="M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2"/></svg>
            </button>
        </div>
        <div class="flex-1 min-h-0 overflow-auto" :class="{ 'p-4': !showingImage }">
            <p v-if="loading" role="status">Loading…</p><p v-else-if="error" role="alert">{{error}}</p>
            <p v-else-if="data?.file?.message">{{data.file.message}}</p>
            <div v-else-if="showingImage"  aria-label="Image preview" data-workspace-image-preview class="flex items-center justify-center h-full min-h-40">
                <p v-if="imageError" role="alert" class="text-sm" :class="$styles.textBlock">Unable to display this image. <button type="button" @click="imageError = false" class="underline">Retry</button></p>
                <img v-else :key="data.file.path || data.file.name" :src="data.file.image" :alt="data.file.name" @error="imageError = true"   class="max-w-full max-h-full object-contain"/>
            </div>
            <div v-else :class="[$styles.textBlock, lineWrap ? 'whitespace-pre-wrap break-words' : 'whitespace-pre']" v-html="highlighted" data-workspace-source :data-workspace-xml="xml || undefined" class="text-black [tab-size:4] dark:text-gray-300 font-mono text-sm"></div>
        </div>
    </section>`
}
