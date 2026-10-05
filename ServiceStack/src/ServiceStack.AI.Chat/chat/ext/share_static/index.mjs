import { computed, inject, onMounted, onUnmounted, ref, watch } from 'vue'
import { publicationAge, publicationDestination, publicationUrl, publishProjectOutput } from '../../ui/modules/shareProject.mjs'

let ext
const resolveActiveProject = ctx => {
    const thread = ctx.threads?.currentThread?.value
    return thread ? (ctx.state.projects || []).find(p => p.id === thread.projectId)
        : ctx.projects?.getProject(ctx.state.prefs.project)
}
const StaticSharePanel = {
    template: `
    <div>
        <div v-if="publishError" role="alert" class="relative mb-4 rounded-lg border border-red-300 dark:border-red-800 bg-red-50 dark:bg-red-950/30 p-3 pr-10 text-sm text-red-800 dark:text-red-200">
            <button type="button" @click="clearPublishError" aria-label="Dismiss publishing error" class="absolute top-2 right-2 p-1 rounded hover:bg-red-100 dark:hover:bg-red-900 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-red-500">×</button>
            <p class="font-semibold">{{ publishError.title }}</p>
            <p class="mt-1 whitespace-pre-wrap break-words">{{ publishError.message }}</p>
            <p v-if="publishError.details" class="mt-2 text-xs whitespace-pre-line break-all">{{ publishError.details }}</p>
        </div>
        <div v-if="activeProjectName" class="space-y-4">
            <div>
                <label class="block text-xs font-bold uppercase tracking-wider mb-1.5" :class="$styles.muted">Build Directory (dist)</label>
                <div class="flex items-stretch gap-2">
                    <input type="text" v-model="overrideDistPath" :disabled="isPublishing" placeholder="deploy root project folder" spellcheck="false"
                        class="flex-1 min-w-0 rounded-lg px-3.5 py-2 text-sm border font-mono bg-white dark:bg-gray-900" :class="[$styles.textInput, $styles.borderInput]"/>
                    <button type="button" @click="openFolderBrowser" :disabled="isPublishing" class="px-3 rounded-lg border text-xs font-semibold" :class="[$styles.dropdownButton, $styles.borderInput]">Browse</button>
                </div>
                <span class="text-xs mt-1 block" :class="$styles.muted">Relative path from project folder to publish (e.g. dist, build, or leave empty for project root).</span>
            </div>
            <div class="space-y-1 text-xs" :class="$styles.muted">
                <div v-if="publication"><span :title="new Date(publication.publishedAt).toLocaleString()">Published {{ publishedAge(publication.publishedAt) }}</span> to <code class="break-all select-all">{{ publicationDestination(publication.publishedPath) }}</code></div>
                <div v-else>Publish to <code class="break-all select-all">{{ destination }}</code></div>
                <a v-if="publishedUrl" :href="publishedUrl" target="_blank" rel="noopener noreferrer" class="block text-blue-600 dark:text-blue-400 hover:underline break-all">{{ publishedUrl }}</a>
                <div v-else-if="publication">URL path: <code class="select-all">{{ publication.urlPath }}</code></div>
            </div>
            <div class="flex items-center justify-between pt-2">
                <span class="text-xs" :class="$styles.muted">Publishing project: <strong>{{ activeProjectName }}</strong></span>
                <button type="button" @click="publishProject" :disabled="isPublishing || isDetectingDist" class="px-4 py-2 text-xs font-bold" :class="$styles.primaryButton">{{ isPublishing ? 'Publishing...' : publication ? 'Update folder' : 'Publish folder' }}</button>
            </div>
        </div>
            <transition enter-active-class="transition duration-200 ease-out"
                        enter-from-class="opacity-0"
                        enter-to-class="opacity-100"
                        leave-active-class="transition duration-150 ease-in"
                        leave-from-class="opacity-100"
                        leave-to-class="opacity-0">
                <div v-if="showFolderBrowser" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs">
                    <div class="w-full max-w-lg rounded-xl border shadow-2xl flex flex-col max-h-[500px]"
                         :class="$styles.bgPopover || 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700'">
                        
                        <!-- Modal Header -->
                        <div class="px-4 py-3 border-b flex items-center justify-between" :class="$styles.chromeBorder">
                            <h4 class="text-sm font-bold text-gray-950 dark:text-white flex items-center gap-1.5">
                                <svg class="w-4 h-4 text-blue-500" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24">
                                    <path stroke-linecap="round" stroke-linejoin="round" d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z" />
                                </svg>
                                Select Build Folder
                            </h4>
                            <button type="button" @click="closeFolderBrowser" class="p-1 rounded hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors">
                                <svg class="w-4 h-4 text-gray-500" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                                    <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
                                </svg>
                            </button>
                        </div>

                        <!-- Current path header / navigation -->
                        <div class="px-4 py-2 border-b bg-gray-50/50 dark:bg-gray-950/20 flex items-center gap-2" :class="$styles.chromeBorder">
                            <button type="button" @click="goUpFolder" :disabled="browserParentPath === null || browserParentPath === undefined"
                                    class="p-1 rounded hover:bg-gray-200 dark:hover:bg-gray-800 disabled:opacity-40 transition-colors">
                                <svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24">
                                    <path stroke-linecap="round" stroke-linejoin="round" d="M15 19l-7-7 7-7" />
                                </svg>
                            </button>
                            <span class="text-[11px] font-mono text-gray-600 dark:text-gray-400 break-all select-all flex-1">
                                {{ browserDisplayPath }}
                            </span>
                        </div>

                        <!-- Directory list -->
                        <div class="flex-1 overflow-y-auto p-2 min-h-[240px]">
                            <div v-if="isBrowsing" class="flex flex-col items-center justify-center py-12 text-gray-400">
                                <svg class="w-6 h-6 animate-spin mb-2" fill="none" viewBox="0 0 24 24">
                                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"></path>
                                </svg>
                                <span class="text-xs">Loading subdirectories...</span>
                            </div>
                            <div v-else-if="browserSubdirs.length === 0" class="flex items-center justify-center py-12 text-xs italic text-gray-500">
                                No subdirectories found in this folder
                            </div>
                            <div v-else class="space-y-0.5">
                                <button v-for="dir in browserSubdirs" :key="dir.path"
                                        @click="navigateToFolder(dir.path)"
                                        type="button"
                                        class="w-full text-left px-3 py-2 rounded-lg text-xs font-medium hover:bg-gray-100 dark:hover:bg-gray-800/80 transition-colors flex items-center gap-2">
                                    <svg class="w-4 h-4 text-yellow-500 fill-yellow-500/20" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                                        <path stroke-linecap="round" stroke-linejoin="round" d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z" />
                                    </svg>
                                    <span class="truncate">{{ dir.name }}</span>
                                </button>
                            </div>
                        </div>

                        <!-- Actions footer -->
                        <div class="px-4 py-3 border-t bg-gray-50/50 dark:bg-gray-950/20 flex items-center justify-between" :class="$styles.chromeBorder">
                            <span class="text-xs" :class=[$styles.muted]>Select any sub-folder to set it as target.</span>
                            <div class="flex items-center gap-2">
                                <button type="button" @click="closeFolderBrowser" class="px-3 py-1.5 text-xs font-semibold hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors rounded">
                                    Cancel
                                </button>
                                <button type="button" @click="selectCurrentFolder" class="px-3.5 py-1.5 text-xs font-bold rounded" :class="$styles.primaryButton">
                                    Select Folder
                                </button>
                            </div>
                        </div>

                    </div>
                </div>
            </transition>
    </div>`,
    setup() {
        const ctx = inject('ctx')
        const publishError = ref(null)
        const clearPublishError = () => { publishError.value = null }
        const setPublishError = (error, title = 'Publishing failed', details = '') => {
            const status = error?.error || error?.responseStatus || error
            let message = typeof status === 'string' ? status : status?.message || status?.errorCode || 'The server returned no error details. Please check the server logs.'
            const root = config.value.directory?.replace(/[/\\]+$/, '')
            if (root) {
                for (const prefix of new Set([root, root.replace(/\\/g, '/')]))
                    message = message.replaceAll(prefix + '/', '~/').replaceAll(prefix + '\\', '~/')
            }
            publishError.value = {
                title,
                message,
                details,
            }
        }
        const config = computed(() => ext.state.config || {})
        const activeProject = computed(() => resolveActiveProject(ctx))
        const activeProjectName = computed(() => activeProject.value?.name || null)
        const activeProjectFolder = computed(() => activeProject.value?.folder || ctx.utils.toKebabCase(activeProjectName.value || ''))
        const publication = computed(() => activeProject.value?.staticPublication)
        const publishedUrl = computed(() => publicationUrl(config.value, publication.value, path => ctx.ai.resolveStaticPublishUrl?.(path)))
        const destination = computed(() => '~/' + (ctx.ai.auth?.userName || 'default') + '/' + activeProjectFolder.value)
        const now = ref(Date.now())
        const publishedAge = timestamp => publicationAge(timestamp, now.value)
        const isPublishing = ref(false)
        let clock
        const isDetectingDist = ref(false)
        const overrideDistPath = ref('')

        function sanitizePublishPath(path, folderName) {
            if (!path) return ''
            path = path.trim()
            if (folderName) {
                const marker = `projects/${folderName}/`
                const idx = path.indexOf(marker)
                if (idx !== -1) {
                    path = path.substring(idx + marker.length)
                } else if (path === `projects/${folderName}` || path.endsWith(`/${folderName}`) || path === folderName) {
                    return ''
                } else if (path.startsWith(`${folderName}/`)) {
                    path = path.substring(folderName.length + 1)
                }
            }
            path = path.replace(/^[/\\]+/, '')
            const parts = path.split(/[/\\]+/).filter(p => p && p !== '.' && p !== '..')
            return parts.join('/')
        }

        let distVersion = 0
        const detectDistFolder = async () => {
            const version = ++distVersion
            isDetectingDist.value = true
            try {
                const project = activeProjectName.value ? ctx.projects?.getProject(activeProjectName.value) : null
                const folder = project?.folder
                if (project && project.publish !== undefined && project.publish !== null) {
                    overrideDistPath.value = sanitizePublishPath(project.publish, folder)
                    return
                }
                const api = await ext.getJson('/detect-dist' + (ctx.threads?.currentThread.value?.id ? '?threadId=' + encodeURIComponent(ctx.threads.currentThread.value.id) : ''))
                if (version !== distVersion) return
                if (api.response && api.response.dist !== undefined) {
                    overrideDistPath.value = sanitizePublishPath(api.response.dist, folder)
                } else {
                    overrideDistPath.value = ''
                }
            } catch (e) {
                console.warn('Failed to auto-detect dist folder', e)
            } finally {
                if (version === distVersion) isDetectingDist.value = false
            }
        }



        const showFolderBrowser = ref(false)
        const isBrowsing = ref(false)
        const browserCurrentPath = ref('')
        const browserDisplayPath = ref('')
        const browserParentPath = ref(null)
        const browserSubdirs = ref([])

        const openFolderBrowser = async () => {
            showFolderBrowser.value = true
            await fetchSubdirs(overrideDistPath.value || '')
        }

        const closeFolderBrowser = () => {
            showFolderBrowser.value = false
        }

        const fetchSubdirs = async (path) => {
            isBrowsing.value = true
            try {
                const projParam = activeProjectName.value ? `&project=${encodeURIComponent(activeProjectName.value)}` : ''
                const api = await ext.getJson(`/list-subdirs?path=${encodeURIComponent(path || '')}${projParam}`)
                if (api.response) {
                    browserCurrentPath.value = api.response.currentPath || ''
                    const folderName = activeProjectFolder.value || 'project'
                    const rel = api.response.currentPath
                    browserDisplayPath.value = api.response.displayPath || (`~/${folderName}` + (rel ? `/${rel}` : ''))
                    browserParentPath.value = api.response.parentPath
                    browserSubdirs.value = api.response.subdirs || []
                } else if (path) {
                    await fetchSubdirs('')
                }
            } catch (e) {
                console.warn('Failed to load subdirectories', e)
                if (path) {
                    await fetchSubdirs('')
                }
            } finally {
                isBrowsing.value = false
            }
        }

        const navigateToFolder = async (path) => {
            await fetchSubdirs(path)
        }

        const goUpFolder = async () => {
            if (browserParentPath.value !== null && browserParentPath.value !== undefined) {
                await fetchSubdirs(browserParentPath.value)
            }
        }

        const selectCurrentFolder = () => {
            overrideDistPath.value = browserCurrentPath.value
            closeFolderBrowser()
        }


        watch(() => [activeProject.value?.id, activeProject.value?.publish], () => { clearPublishError(); closeFolderBrowser(); detectDistFolder() })
        onMounted(() => { clock = setInterval(() => { now.value = Date.now() }, 1000); detectDistFolder() })
        onUnmounted(() => clearInterval(clock))
        const publishProject = async () => {
            if (!activeProject.value || isPublishing.value) return
            const target = { ...activeProject.value }
            const owner = ctx.ai.auth?.userName || 'default'
            const source = sanitizePublishPath(overrideDistPath.value, target.folder)
            const details = `Project: ${owner}/${target.folder || target.name}\nBuild directory: ${source || '(project root)'}\nDestination: ${destination.value}`
            clearPublishError()
            isPublishing.value = true
            try {
                const api = await publishProjectOutput(ctx, ext, target, source, 'folder')
                if ((ctx.ai.auth?.userName || 'default') !== owner) return
                if (api.error) { setPublishError(api.error, 'Failed to publish project', details); return }
                if (!api.response?.publishedPath) { setPublishError('The server did not return a publication result.', 'Failed to publish project', details); return }
                const project = (ctx.state.projects || []).find(p => p.id === target.id)
                if (project) project.staticPublication = api.response
                ext.toast('Project published to folder')
            } catch (e) { setPublishError(e, 'Failed to publish project', details) }
            finally { isPublishing.value = false }
        }
        return { publishError, clearPublishError, config, activeProjectName, activeProjectFolder, publication, publishedUrl, destination, publishedAge, publicationDestination,
            isPublishing, overrideDistPath, isDetectingDist, publishProject, showFolderBrowser, isBrowsing,
            browserCurrentPath, browserDisplayPath, browserParentPath, browserSubdirs, openFolderBrowser,
            closeFolderBrowser, navigateToFolder, goUpFolder, selectCurrentFolder }
    },
}

const option = { name: 'Folder', order: 10, component: StaticSharePanel, isVisible: ctx => !!resolveActiveProject(ctx) }
export default {
    order: 100,
    install(ctx) {
        ext = ctx.scope('share_static')
    },
    async load(ctx) {
        const api = await ext.getJson('/config.json')
        if (api.error) { ext.setError(api.error, 'Failed to load static sharing configuration'); return }
        ext.setState({ config: api.response })
        ctx.setShareOptions({ share_static: api.response?.enabled === false ? null : option })
    },
}
