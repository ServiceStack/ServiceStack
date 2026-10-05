import { ref, computed, inject, onMounted, onUnmounted, nextTick, watch } from "vue"

import { CheckBox } from '../../ui/components/CheckBox.mjs'
import ProjectCreateForm from './ProjectCreateForm.mjs'
import { useProjectOrganization } from './projectOrganization.mjs'
import { publicationDestination } from '../../ui/modules/shareProject.mjs'

let ext

function useProjects(ext) {
    const ctx = ext.ctx

    function getProject(name) {
        return (ctx.state.projects || []).find(p => p.name === name)
    }

    async function saveProject(originalName, updatedProject, { reportError = true } = {}) {
        const api = await ext.postJson(`/save/${encodeURIComponent(originalName)}`, updatedProject)
        if (api.error) {
            if (reportError) ctx.setError(api.error, "Failed to save project")
        } else {
            const projects = api.response
            ctx.setState({ projects })
            // Update active project if needed
            const active = ctx.state.prefs.project
            if (active) {
                if (active === originalName && updatedProject.name !== originalName) {
                    ctx.state.prefs.project = updatedProject.name
                }
            }
        }
        return api
    }

    return {
        get all() { return (ctx.state.projects || []).filter(p => !p.archived) },
        get archived() { return (ctx.state.projects || []).filter(p => p.archived) },
        get active() { return ctx.ctx.state.prefs.project },
        getProject,
        publicationDestination,
        publicationUrl(project) {
            const publication = project?.staticPublication
            return publication?.publishedUrl || ctx.ai.resolveStaticPublishUrl?.(publication?.urlPath) || null
        },
        saveProject,
        openNewProject() {
            ctx.projectCreationRequest = { startNew: true }
            ctx.openModal('projects-manager')
        },
        // Open the project's most recent unsent draft, or start one, so the folder
        // appears in the sidebar with a prompt ready for that project.
        openDraft(projectId) {
            const drafts = ctx.chat.drafts
            const existing = drafts.list()
                .filter(d => d.projectId === projectId)
                .sort((a, b) => (b.updatedAt || 0) - (a.updatedAt || 0))[0]
            if (existing) drafts.bind(existing.key)
            else drafts.fresh(projectId)
            ctx.threads.clearCurrentThread()
            ctx.to('/')
            setTimeout(() => document.getElementById('messageText')?.focus(), 0)
        },
        editProject(id) {
            ctx.projectCreationRequest = { editId: id }
            ctx.openModal('projects-manager')
        },
        createForChat(onCreated) {
            ctx.projectCreationRequest = { startNew: true, onCreated }
            ctx.openModal('projects-manager')
        },
    }
}

const ProjectsSelector = {
    template: `
        <div class="relative" ref="triggerRef">
            <button type="button" @click="togglePopover"
                class="select-none flex items-center space-x-2 px-3 py-2 rounded-md text-sm w-full md:w-auto md:min-w-48 max-w-96 transition-colors"
                :class="$styles.dropdownButton">
                <!-- Folder Icon -->
                <svg xmlns="http://www.w3.org/2000/svg" class="size-4 flex-shrink-0" :class="$styles.mutedIcon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"></path>
                </svg>
                <span class="truncate flex-1 text-left font-medium">{{ $state.prefs.project || 'Default Workspace' }}</span>
                <svg class="size-4 flex-shrink-0" :class="[$styles.mutedIcon]" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd" />
                </svg>
            </button>

            <!-- Dropdown Popover -->
            <div v-if="showPopover" ref="popoverRef"
                class="absolute left-0 mt-1.5 w-80 rounded-lg shadow-xl z-50 py-1"
                :class="$styles.bgPopover">
                <div class="px-3 py-1.5 text-xs font-semibold uppercase tracking-wider" :class="$styles.muted">
                    Workspaces & Projects
                </div>

                <!-- Default Workspace -->
                <button type="button" @click="selectProject(null)"
                    class="w-full text-left px-3 py-2 flex items-start space-x-3 transition-colors text-sm"
                    :class="[$state.prefs.project === null ? $styles.popoverButtonActive : $styles.popoverButton]">
                    <svg xmlns="http://www.w3.org/2000/svg" class="size-5 mt-0.5 flex-shrink-0" :class="$state.prefs.project === null ? 'text-blue-500' : $styles.mutedIcon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path>
                        <polyline points="9 22 9 12 15 12 15 22"></polyline>
                    </svg>
                    <div class="flex-1 min-w-0">
                        <div class="font-medium text-gray-900 dark:text-gray-100">
                            <span>Default Workspace</span>
                        </div>
                        <div class="text-[10px] font-mono truncate mt-0.5" :class="$styles.muted">
                            Default workspace (no project selected)
                        </div>
                    </div>
                </button>

                <!-- Project List -->
                <div v-if="projects.length === 0" class="px-4 py-3 text-xs italic border-t" :class="[$styles.muted, $styles.chromeBorder]">
                    No other projects registered.
                </div>
                <div v-else class="max-h-60 overflow-y-auto border-t" :class="$styles.chromeBorder">
                    <div v-for="project in projects" :key="project.name"
                        @click="selectProject(project)"
                        class="w-full text-left px-3 py-2 flex items-start space-x-3 transition-colors text-sm cursor-pointer select-none"
                        :class="[$state.prefs.project === project.name ? $styles.popoverButtonActive : $styles.popoverButton]">
                        <svg xmlns="http://www.w3.org/2000/svg" class="size-5 mt-0.5 flex-shrink-0" :class="$state.prefs.project === project.name ? 'text-blue-500' : $styles.mutedIcon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"></path>
                        </svg>
                        <div class="flex-1 min-w-0">
                            <div class="font-medium text-gray-900 dark:text-gray-100 flex items-center justify-between">
                                <span class="truncate font-semibold">{{ project.name }}</span>
                                <a v-if="$projects.publicationUrl(project)" :href="$projects.publicationUrl(project)" target="_blank" rel="noopener noreferrer" @click.stop
                                   class="text-[10px] text-blue-600 dark:text-blue-400 hover:underline shrink-0 ml-1">folder</a>
                                <span v-else-if="project.staticPublication" class="text-[10px] shrink-0 ml-1" :class="$styles.muted" :title="$projects.publicationDestination(project.staticPublication.publishedPath)">folder published</span>
                                <a v-if="project.publishedUrl" :href="project.publishedUrl" target="_blank" rel="noopener noreferrer" @click.stop
                                   class="inline-flex items-center gap-0.5 text-[10px] text-blue-600 dark:text-blue-400 hover:underline shrink-0 ml-1">
                                    <svg class="size-2.5 shrink-0" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                                        <path stroke-linecap="round" stroke-linejoin="round" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
                                    </svg>
                                    <span>published</span>
                                </a>
                            </div>
                            <div v-if="project.description" class="text-xs truncate mt-0.5" :class="$styles.muted">
                                {{ project.description }}
                            </div>
                            <div class="mt-1.5 text-[10px] font-mono truncate" :class="$styles.muted" :title="project.folder || $utils.toKebabCase(project.name)">
                                ~/{{ project.folder || $utils.toKebabCase(project.name) }}
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Manage Projects Button -->
                <button type="button" @click="manageProjects"
                    class="w-full text-left px-3 py-2 flex items-center space-x-2 transition-colors text-sm border-t"
                    :class="[$styles.popoverButton, $styles.chromeBorder]">
                    <svg xmlns="http://www.w3.org/2000/svg" class="size-4 flex-shrink-0" :class="$styles.mutedIcon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M12 20h9M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z"></path>
                    </svg>
                    <span class="font-medium">Manage Projects</span>
                </button>
            </div>
        </div>
    `,
    setup(props) {
        const ctx = inject('ctx')
        const showPopover = ref(false)
        const triggerRef = ref(null)
        const popoverRef = ref(null)

        const projects = computed(() => (ctx.state.projects || []).filter(p => !p.archived))

        const togglePopover = () => showPopover.value = !showPopover.value

        async function selectProject(project) {
            const name = project?.name || null
            const api = await ext.postJson(`/active`, { name })
            if (api.error) {
                ctx.setError(api.error, "Failed to switch project")
            } else {
                ctx.state.prefs.project = name
                ctx.toast(`Switched to project: ${name || 'default'}`)
            }
            showPopover.value = false
        }

        function manageProjects() {
            showPopover.value = false
            ctx.openModal('projects-manager')
        }

        const onDocClick = (e) => {
            const t = e.target
            if (triggerRef.value?.contains(t)) return
            if (popoverRef.value?.contains(t)) return
            showPopover.value = false
        }

        onMounted(() => document.addEventListener('click', onDocClick))
        onUnmounted(() => document.removeEventListener('click', onDocClick))

        return {
            ext,
            showPopover,
            triggerRef,
            popoverRef,
            projects,
            togglePopover,
            selectProject,
            manageProjects,
        }
    }
}

const ProjectsManagerModal = {
    components: { CheckBox, ProjectCreateForm },
    template: `
        <!-- Dialog Overlay -->
        <div class="fixed inset-0 z-50 overflow-hidden text-gray-900 dark:text-gray-100" @keydown.escape="closeDialog">
            <!-- Backdrop -->
            <div class="fixed inset-0 bg-black/50 transition-opacity" @click="closeDialog"></div>

            <!-- Dialog -->
            <div class="fixed inset-4 md:inset-8 lg:inset-12 flex items-center justify-center" @click.self="closeDialog">
                <div role="dialog" aria-modal="true" aria-labelledby="projects-manager-title" @keydown.tab="trapFocus" data-project-manager class="relative bg-white dark:bg-gray-800 rounded-xl shadow-2xl w-full h-full max-w-4xl max-h-[90vh] flex flex-col overflow-hidden">
                    <!-- Header -->
                    <div class="flex-shrink-0 px-6 py-4 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
                        <h2 id="projects-manager-title" class="text-xl font-semibold">{{isNewProject ? 'New project' : 'Manage Projects'}}</h2>
                        <button type="button" aria-label="Close project manager" @click="closeDialog" class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-blue-500 focus-visible:outline-offset-0.5 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 transition-colors">
                            <svg class="size-6" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                                <path fill="currentColor" d="M19 6.41L17.59 5L12 10.59L6.41 5L5 6.41L10.59 12L5 17.59L6.41 19L12 13.41L17.59 19L19 17.59L13.41 12z"/>
                            </svg>
                        </button>
                    </div>

                    <!-- Main Body Split Pane -->
                    <div class="flex-1 flex overflow-hidden">
                        <!-- Left pane: Projects List -->
                        <div :class="{ 'max-sm:hidden': isNewProject }" data-project-manager-list class="max-sm:[&>div:first-child]:p-2 max-sm:[&>div:first-child_button]:px-1 max-sm:[&>div:first-child_button]:text-xs max-sm:[&>div:nth-child(2)]:pl-2.5 max-sm:[&>div:nth-child(2)]:pr-2 w-1/3 border-r border-gray-200 dark:border-gray-700 flex flex-col bg-gray-50 dark:bg-gray-800/40">
                            <div class="max-sm:p-2 p-4 border-b border-gray-200 dark:border-gray-700">
                                <button type="button" @click="createNewProject"
                                    :class="[$styles.primaryButton]" class="max-sm:pl-1 max-sm:pr-1 max-sm:text-xs w-full py-2 px-3 flex items-center justify-center space-x-1 text-sm font-medium rounded-lg transition-colors">
                                    <svg class="size-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                                        <line x1="12" y1="5" x2="12" y2="19"></line>
                                        <line x1="5" y1="12" x2="19" y2="12"></line>
                                    </svg>
                                    <span>New Project</span>
                                </button>
                            </div>
                            <div class="max-sm:pl-2.5 max-sm:pr-2 flex items-center justify-between px-4 pt-3 pb-2">
                                <span class="text-xs font-medium" :class="$styles.muted">Projects</span>
                                <button v-if="activeProjects.length > 1" type="button" @click="toggleReorder" :aria-pressed="reorderMode" :disabled="savingOrder || actionBusy" :class="reorderMode ? 'text-blue-600 dark:text-blue-400' : $styles.muted" data-project-order-toggle class="disabled:opacity-35 disabled:cursor-wait text-xs py-0.75 px-1 rounded focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-blue-500 focus-visible:outline-offset-0.5">{{reorderMode ? 'Done' : 'Reorder'}}</button>
                            </div>
                            <p v-if="reorderMode" id="project-order-help" class="px-4 pb-2 text-xs" :class="$styles.muted">Drag a folder to reorder. Or use ↑ ↓ on its handle.</p>
                            <div ref="projectRows" :class="{ '[&>[data-project-id]]:transition-transform [&>[data-project-id]]:duration-150 motion-reduce:[&>[data-project-id]]:transition-none': draggingId }" aria-label="Active projects" class="flex-1 min-h-0 overflow-y-auto p-2 space-y-1">
                                <div v-if="activeProjects.length === 0" class="text-center py-8 text-xs" :class="[$styles.muted]">
                                    No active projects.
                                </div>
                                <div v-for="p in activeProjects" :key="p.id" :data-project-id="p.id" :class="[selectedId === p.id && !showArchived ? 'bg-blue-50 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 font-semibold ring-1 ring-blue-500/20' : 'hover:bg-gray-100 dark:hover:bg-gray-700/50 text-gray-700 dark:text-gray-300', { 'touch-none select-none cursor-grab active:cursor-grabbing [&>button]:cursor-inherit': reorderMode, 'bg-blue-500/6 ring-1 ring-inset ring-blue-500/14 [&>button]:invisible': draggingId === p.id }]" :style="rowStyle(p.id)" @pointerdown="!actionBusy && startDrag($event, p.id)" @pointermove="drag" @pointerup="endDrag" @pointercancel="cancelDrag" @lostpointercapture="cancelDrag" data-project-manager-row class="relative has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-blue-500 has-[:focus-visible]:outline-offset-2 [&>button]:focus-visible:outline-none flex items-center rounded-lg">
                                <button type="button" @click="!reorderMode && selectEditProject(p.id)" :aria-current="selectedId === p.id && !showArchived ? 'true' : undefined"
                                      class="max-sm:pl-1.5 max-sm:pr-1.5 flex-1 min-w-0 text-left px-3 py-2 rounded-lg flex items-center justify-between text-sm">
                                    <div class="flex items-center space-x-2 min-w-0">
                                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="max-sm:hidden size-4 flex-shrink-0 opacity-60">
                                            <path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"></path>
                                        </svg>
                                        <span class="truncate">{{ p.name }}</span>
                                    </div>
                                    <span v-if="p.showInSidebar === false" class="max-sm:hidden ml-2 shrink-0 text-[10px] opacity-60">Hidden</span>
                                </button>
                                <button v-if="reorderMode" type="button" :data-id="p.id" :disabled="savingOrder || actionBusy" :aria-label="'Move ' + p.name" aria-describedby="project-order-help" @keydown.up.prevent="moveProject(p.id, -1)" @keydown.down.prevent="moveProject(p.id, 1)" data-project-drag-handle class="grid place-items-center shrink-0 w-7.5 h-8 mr-1 rounded-md opacity-55 touch-none cursor-grab hover:bg-[rgb(128_128_128_/_10%)] hover:opacity-100 active:cursor-grabbing disabled:opacity-35 disabled:cursor-wait max-sm:w-5.5 max-sm:mr-0">
                                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" aria-hidden="true" class="w-4.5 h-4.5"><path d="M5 8h14M5 12h14M5 16h14"/></svg>
                                </button>
                                </div>
                            </div>
                            <p v-if="organizationError" role="alert" class="px-4 pb-3 text-xs text-red-600 dark:text-red-400">{{organizationError}}</p>
                            <p class="sr-only" role="status">{{orderStatus}}</p>
                            <div :class="$styles.chromeBorder" data-project-manager-sidebar-footer class="shrink-0 py-3 px-2 max-sm:p-2 border-t">
                                <button type="button" @click="openArchived" :aria-current="showArchived ? 'page' : undefined" :class="showArchived ? 'bg-blue-50 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300' : 'hover:bg-gray-100 dark:hover:bg-gray-700/50'" data-project-archives-link class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-blue-500 focus-visible:outline-offset-0.5 min-h-9.5 max-sm:flex-wrap max-sm:gap-1.5 max-sm:text-xs max-sm:p-2 w-full flex items-center gap-2 rounded-lg text-left px-3 py-2 text-sm">
                                    <svg class="size-4 shrink-0 opacity-70" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linejoin="round" aria-hidden="true"><path d="M3 4h18v4H3Zm2 4v12h14V8M9 12h6"/></svg>
                                    <span class="max-sm:basis-[calc(100%-24px)] flex-1">Archived Projects</span><span class="text-xs tabular-nums" :class="$styles.muted">{{archivedProjects.length}}</span>
                                </button>
                            </div>
                        </div>

                        <!-- Right pane: Project Edit Form -->
                        <div data-project-manager-form class="max-sm:flex-1 max-sm:min-w-0 w-2/3 flex flex-col bg-white dark:bg-gray-800">
                            <div v-if="showArchived" data-project-archives-page class="max-w-140 w-full my-0 mx-auto flex flex-col flex-1 min-h-0 overflow-y-auto p-6">
                                <h3 class="text-lg font-semibold tracking-tight">Archived Projects</h3>
                                <p class="text-sm mt-1 mb-5" :class="$styles.muted">Your files and conversations stay with each project.</p>
                                <div :class="[$styles.bgInput, $styles.borderInput]" data-project-archive-search class="flex items-center gap-2 py-2.5 px-3 border rounded-lg">
                                    <svg class="size-4 shrink-0 opacity-50" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true"><circle cx="10.5" cy="10.5" r="6.5"/><path d="m16 16 4 4"/></svg>
                                    <input ref="archiveSearchInput" v-model="archiveSearch" type="search" aria-label="Search archived projects" placeholder="Search archived projects" class="llms-input-unframed p-0 bg-transparent text-sm min-w-0 flex-1"/>
                                </div>
                                <p v-if="managerError" role="alert" class="text-sm mt-4 text-red-600 dark:text-red-400">{{managerError}}</p>
                                <p class="sr-only" role="status">{{archiveStatus}}</p>
                                <div v-if="filteredArchives.length" class="mt-4">
                                    <div v-for="p in filteredArchives" :key="p.id" :data-archived-id="p.id" data-project-archive-row class="flex items-center gap-3 py-4 px-0 border-b border-b-[var(--border)] max-sm:flex-wrap max-sm:gap-2">
                                        <svg class="size-5 shrink-0 opacity-50" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true"><path d="M3 4h18v4H3Zm2 4v12h14V8M9 12h6"/></svg>
                                        <div class="max-sm:basis-[calc(100%-28px)] min-w-0 flex-1">
                                            <div data-project-archive-title class="flex items-baseline gap-2"><h4 class="min-w-0 truncate text-sm font-medium" :title="p.name">{{p.name}}</h4><span :class="$styles.muted" :title="p.folder" data-project-archive-folder class="min-w-0 max-w-[45%] py-0.5 px-1.5 rounded bg-[rgb(128_128_128_/_8%)] text-[10px] leading-4 font-mono truncate">{{p.folder}}</span></div>
                                            <p v-if="p.description" class="text-xs mt-1 truncate" :class="$styles.muted" :title="p.description">{{p.description}}</p>
                                        </div>
                                        <button type="button" @click="setArchived(p, false)" :disabled="!!actionBusy" :aria-label="'Unarchive ' + p.name" :class="$styles.secondaryButton" data-project-create-button class="py-[9px] px-4 rounded-lg text-[13px] font-semibold transition-colors disabled:opacity-45 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-blue-500 focus-visible:outline-offset-0.5 max-sm:ml-7 shrink-0">{{actionBusy === p.id ? 'Restoring…' : 'Unarchive'}}</button>
                                    </div>
                                </div>
                                <div v-else data-project-archive-empty class="flex flex-col items-center justify-center text-center min-h-60 py-6 px-0"><svg class="size-10 mb-4 opacity-35" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.2" aria-hidden="true"><path d="M3 4h18v4H3Zm2 4v12h14V8M9 12h6"/></svg><p class="text-sm font-medium">{{archiveSearch.trim() ? 'No matching projects' : 'No archived projects'}}</p><p class="text-xs mt-2" :class="$styles.muted">{{archiveSearch.trim() ? 'Try a different name, folder or description.' : 'Archive a project to keep your active list tidy.'}}</p></div>
                            </div>
                            <div v-else-if="isNewProject" class="flex-1 min-h-0 overflow-y-auto p-6 flex flex-col">
                                <ProjectCreateForm @done="closeDialog" @created="localProjects = $event" />
                            </div>
                            <div v-else-if="selectedIdx === null" class="flex-1 flex flex-col items-center justify-center p-6 text-gray-400 dark:text-gray-500">
                                <svg xmlns="http://www.w3.org/2000/svg" class="size-16 mb-4 opacity-40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                                    <path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"></path>
                                </svg>
                                <p class="text-sm">Select a project to edit, or create a new one.</p>
                                <button type="button" @click="closeDialog"
                                    :class="[$styles.secondaryButton]" class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-blue-500 focus-visible:outline-offset-0.5 mt-4 px-4 py-1 text-sm font-medium transition-colors">
                                    close
                                </button>
                            </div>
                            <div v-else class="flex-1 min-h-0 flex flex-col">
                                <div class="flex-1 min-h-0 overflow-y-auto p-6 space-y-6">
                                    <div>
                                        <h3 class="text-base font-semibold mb-4">
                                            {{ isNewProject ? 'Create Project' : 'Edit Project' }}
                                        </h3>
                                        <div class="space-y-4">
                                            <!-- Project Name -->
                                            <div>
                                                <label class="block text-sm font-medium mb-1" :class="[$styles.labelInput]">Project Name *</label>
                                                <input type="text" v-model="editForm.name" @input="onNameInput"
                                                    placeholder="e.g. My Awesome App"
                                                    :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]" data-project-create-input class="block w-full border rounded-lg py-[9px] px-3 text-sm outline-none"/>
                                            </div>

                                            <!-- Project Description -->
                                            <div>
                                                <label class="block text-sm font-medium mb-1" :class="[$styles.labelInput]">Description</label>
                                                <input type="text" v-model="editForm.description"
                                                    placeholder="Short summary of this project"
                                                    :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]" data-project-create-input class="block w-full border rounded-lg py-[9px] px-3 text-sm outline-none"/>
                                            </div>

                                            <!-- Folder Name -->
                                            <div>
                                                <label class="block text-sm font-medium mb-1" :class="[$styles.labelInput]">Folder Name *</label>
                                                <input type="text" v-model="editForm.folder" @input="isFolderManuallyEdited = true"
                                                    placeholder="e.g. my-awesome-app"
                                                    :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]" data-project-create-input class="block w-full border rounded-lg py-[9px] px-3 text-sm outline-none font-mono"/>
                                                <span class="text-[10px] text-gray-400 dark:text-gray-500 mt-1 block">
                                                    User Projects folder path: <code class="font-mono">~/{{ editForm.folder || 'folder-name' }}</code>
                                                </span>
                                            </div>

                                            <div v-if="repositoryUrl">
                                                <label for="project-repository-url" class="block text-sm font-medium mb-1" :class="$styles.labelInput">Repository URL</label>
                                                <div class="flex items-center gap-1 min-w-0">
                                                    <input id="project-repository-url" type="text" :value="repositoryUrl" :title="repositoryUrl" readonly @focus="$event.target.select()" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]" data-project-create-input class="block w-full border rounded-lg py-[9px] px-3 text-sm outline-none min-w-0 flex-1 font-mono text-xs"/>
                                                    <button type="button" @click="copyRepositoryUrl" aria-label="Copy repository URL" :title="repositoryCopied ? 'Copied' : 'Copy repository URL'" :class="[$styles.icon, $styles.iconHover]" class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-blue-500 focus-visible:outline-offset-0.5 shrink-0 p-2 rounded-md">
                                                        <svg class="size-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path v-if="repositoryCopied" d="m5 12 4 4L19 6"/><template v-else><rect x="9" y="9" width="12" height="12" rx="2"/><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/></template></svg>
                                                    </button>
                                                    <a v-if="repositoryWebUrl" :href="repositoryWebUrl" target="_blank" rel="noopener noreferrer" aria-label="Open repository" title="Open repository" class="shrink-0 p-2 rounded-md" :class="[$styles.icon, $styles.iconHover]">
                                                        <svg class="size-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M15 3h6v6M10 14 21 3M21 14v5a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5"/></svg>
                                                    </a>
                                                </div>
                                                <span class="sr-only" role="status">{{repositoryCopied ? 'Repository URL copied' : ''}}</span>
                                            </div>

                                            <label class="flex items-start gap-2 text-sm cursor-pointer">
                                                <CheckBox v-model="editForm.showInSidebar" class="mt-0.5" />
                                                <span>Show folder in sidebar <span class="block text-xs opacity-60">Folders appear after their first chat message.</span></span>
                                            </label>

                                            <!-- Publish Build Directory -->
                                            <div>
                                                <label class="block text-sm font-medium mb-1" :class="[$styles.labelInput]">Publish Build Directory</label>
                                                <input type="text" v-model="editForm.publish"
                                                    placeholder="deploy root project folder"
                                                    :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]" data-project-create-input class="block w-full border rounded-lg py-[9px] px-3 text-sm outline-none font-mono placeholder:text-gray-500"/>
                                                <span class="text-[10px] text-gray-400 dark:text-gray-500 mt-1 block">
                                                    Relative path from project folder to publish (e.g. dist, build, or leave empty for project root)
                                                </span>
                                            </div>

                                            <div v-if="localProjects[selectedIdx]?.staticPublication" class="text-xs space-y-1">
                                                <label class="block text-sm font-medium" :class="$styles.labelInput">Published folder</label>
                                                <code class="break-all select-all">{{ $projects.publicationDestination(localProjects[selectedIdx].staticPublication.publishedPath) }}</code>
                                                <a v-if="$projects.publicationUrl(localProjects[selectedIdx])" :href="$projects.publicationUrl(localProjects[selectedIdx])" target="_blank" rel="noopener noreferrer"
                                                   class="block text-blue-600 dark:text-blue-400 hover:underline">Open site</a>
                                            </div>
                                            <!-- Published URL -->
                                            <div v-if="editForm.publishedUrl">
                                                <label class="block text-sm font-medium mb-1" :class="[$styles.labelInput]">ai.llmspy.org URL</label>
                                                <div class="flex items-center gap-2">
                                                    <a :href="editForm.publishedUrl" target="_blank" rel="noopener noreferrer"
                                                       class="text-xs text-blue-600 dark:text-blue-400 hover:underline truncate">
                                                        {{ editForm.publishedUrl }}
                                                    </a>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <!-- Form Actions -->
                                <p v-if="managerError" role="alert" class="shrink-0 px-6 pb-2 text-sm text-red-600 dark:text-red-400">{{managerError}}</p>
                                <div data-project-manager-actions class="shrink-0 px-6 py-3 border-t border-gray-200 dark:border-gray-700 flex flex-wrap gap-3 items-center justify-between">
                                    <div class="flex items-center gap-1">
                                    <button type="button" @click="setArchived(localProjects[selectedIdx], true)" :disabled="!!actionBusy || savingOrder" :class="$styles.secondaryButton" class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-blue-500 focus-visible:outline-offset-0.5 min-h-9.5 px-4 py-2 text-sm font-semibold rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed">{{actionBusy ? 'Archiving…' : 'Archive project'}}</button>
                                    <button type="button" v-if="!isNewProject" @click="deleteProject"
                                        :disabled="!!actionBusy || savingOrder" aria-label="Delete project" title="Delete project" class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-blue-500 focus-visible:outline-offset-0.5 min-h-9.5 p-2 text-sm text-gray-500 dark:text-gray-400 hover:text-red-600 dark:hover:text-red-400 hover:bg-red-50 dark:hover:bg-red-950/20 rounded-lg transition-colors">
                                        <svg class="size-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                                            <polyline points="3 6 5 6 21 6"></polyline>
                                            <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
                                        </svg>
                                    </button>
                                    </div>
                                    <div class="flex items-center space-x-3">
                                        <button type="button" @click="closeDialog"
                                              class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-blue-500 focus-visible:outline-offset-0.5 min-h-9.5 px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg transition-colors">
                                            Close
                                        </button>
                                        <button type="button" @click="saveForm"
                                            :disabled="!editForm.name.trim() || !!actionBusy || savingOrder"
                                            :class="[$styles.primaryButton]" class="min-h-9.5 px-4 py-2 text-sm font-semibold rounded-lg transition-colors">
                                            {{ isDirty ? 'Save Project' : 'Select Project' }}
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            <Teleport to="body">
                <div v-if="dragPreview" :style="dragPreview.style" aria-hidden="true" data-project-drag-preview class="fixed top-0 left-0 z-200 flex items-center gap-2 py-2 px-3 border border-[rgb(59_130_246_/_25%)] rounded-lg bg-white shadow-[0_12px_28px_rgb(0_0_0_/_16%),_0_3px_8px_rgb(0_0_0_/_8%)] pointer-events-none select-none [will-change:transform] dark:bg-[#1f2937] dark:shadow-[0_12px_28px_rgb(0_0_0_/_40%),_0_3px_8px_rgb(0_0_0_/_20%)] max-sm:py-2 max-sm:px-1.5 max-sm:gap-1 text-gray-700 dark:text-gray-200">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="max-sm:hidden size-4 shrink-0 opacity-60"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/></svg>
                    <span class="min-w-0 flex-1 truncate text-sm font-medium">{{dragPreview.project.name}}</span>
                    <span v-if="dragPreview.project.showInSidebar === false" data-project-drag-hidden class="max-sm:hidden text-[10px] opacity-60">Hidden</span>
                    <svg class="size-4 shrink-0 opacity-50" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"><path d="M5 8h14M5 12h14M5 16h14"/></svg>
                </div>
            </Teleport>
        </div>
    `,
    emits: ['done'],
    setup(props, { emit }) {
        const ctx = inject('ctx')
        const creationRequest = ctx.projectCreationRequest
        const focusOrigin = document.activeElement
        const localProjects = ref([])
        const selectedId = ref(null)
        const isNewProject = ref(false)
        const selectedIdx = computed(() => {
            if (isNewProject.value) return -1
            const idx = localProjects.value.findIndex(p => p.id === selectedId.value && !p.archived)
            return idx < 0 ? null : idx
        })
        const repositoryUrl = computed(() => localProjects.value[selectedIdx.value]?.gitSource?.url || '')
        const copiedRepositoryUrl = ref('')
        const repositoryCopied = computed(() => !!repositoryUrl.value && copiedRepositoryUrl.value === repositoryUrl.value)
        const repositoryWebUrl = computed(() => {
            const source = repositoryUrl.value
            if (!source) return null
            const scp = source.match(/^([\w.-]+)@([\w.-]+):(.+)$/)
            try {
                const url = new URL(scp ? `ssh://${scp[1]}@${scp[2]}/${scp[3]}` : source)
                const knownHost = ['github.com', 'gitlab.com', 'bitbucket.org'].includes(url.hostname)
                if (url.protocol === 'https:' && !url.username && !url.password) {
                    if (knownHost) url.pathname = url.pathname.replace(/\.git$/, '')
                    return url.href
                }
                if (url.protocol === 'ssh:' && knownHost) {
                    return `https://${url.hostname}${url.pathname.replace(/\.git$/, '')}`
                }
            } catch { /* The original clone URL remains visible and copyable. */ }
            return null
        })
        let repositoryCopyTimer
        async function copyRepositoryUrl() {
            const url = repositoryUrl.value
            try {
                await navigator.clipboard.writeText(url)
                copiedRepositoryUrl.value = url
                clearTimeout(repositoryCopyTimer)
                repositoryCopyTimer = setTimeout(() => { copiedRepositoryUrl.value = '' }, 2000)
            } catch { managerError.value = 'Unable to copy. Select the repository URL and copy it manually.' }
        }
        const organization = useProjectOrganization(ctx, localProjects)
        const showArchived = ref(false), archiveSearch = ref(''), archiveSearchInput = ref(null)
        const managerError = ref(''), archiveStatus = ref(''), actionBusy = ref(null)
        const filteredArchives = computed(() => {
            const query = archiveSearch.value.trim().toLocaleLowerCase()
            return organization.archivedProjects.value.filter(p =>
                [p.name, p.folder, p.description].some(value => (value || '').toLocaleLowerCase().includes(query)))
        })
        const isFolderManuallyEdited = ref(false)

        const editForm = ref({
            name: '',
            folder: '',
            description: '',
            publish: '',
            publishedUrl: '',
            showInSidebar: true
        })

        // Load project data
        onMounted(async () => {
            localProjects.value = JSON.parse(JSON.stringify(ctx.state.projects || []))
            if (creationRequest?.startNew) createNewProject()
            else if (creationRequest?.editId) {
                selectEditProject(creationRequest.editId)
            }
            const previous = ctx.state.projects
            const api = await ext.getJson('/projects.json').catch(() => null)
            if (api?.response && ctx.state.projects === previous) organization.updateProjects(api.response)
        })
        watch(() => ctx.state.projects, projects => {
            localProjects.value = JSON.parse(JSON.stringify(projects || []))
        })
        onUnmounted(() => {
            clearTimeout(repositoryCopyTimer)
            if (ctx.projectCreationRequest === creationRequest) ctx.projectCreationRequest = null
            if (focusOrigin?.isConnected) focusOrigin.focus()
        })

        function trapFocus(event) {
            const dialog = event.currentTarget
            const controls = [...dialog.querySelectorAll('button:not(:disabled),input:not(:disabled),summary,a[href],[tabindex="0"]')]
                .filter(el => el.getClientRects().length)
            const first = controls[0], last = controls.at(-1)
            if (event.shiftKey && event.target === first) { event.preventDefault(); last?.focus() }
            else if (!event.shiftKey && event.target === last) { event.preventDefault(); first?.focus() }
        }

        function onNameInput() {
            if (!isFolderManuallyEdited.value) {
                editForm.value.folder = ctx.utils.toKebabCase(editForm.value.name)
            }
        }

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

        function selectEditProject(id) {
            const proj = localProjects.value.find(p => p.id === id && !p.archived)
            if (!proj) return
            isNewProject.value = false
            showArchived.value = false
            managerError.value = ''
            selectedId.value = id
            const folder = proj.folder || ctx.utils.toKebabCase(proj.name)
            editForm.value = {
                name: proj.name,
                folder: folder,
                description: proj.description || '',
                publish: sanitizePublishPath(proj.publish || '', folder),
                publishedUrl: proj.publishedUrl || '',
                showInSidebar: proj.showInSidebar !== false
            }
            isFolderManuallyEdited.value = true
        }

        function createNewProject() {
            isNewProject.value = true
            selectedId.value = null
            showArchived.value = false
            managerError.value = ''
            organization.reorderMode.value = false
            editForm.value = {
                name: '',
                folder: '',
                description: '',
                publish: '',
                publishedUrl: '',
                showInSidebar: true
            }
            isFolderManuallyEdited.value = false
        }

        function cancelEdit() {
            selectedId.value = null
            isNewProject.value = false
        }

        async function openArchived() {
            cancelEdit()
            showArchived.value = true
            organization.reorderMode.value = false
            managerError.value = ''
            await nextTick()
            archiveSearchInput.value?.focus()
        }

        async function setArchived(project, archived) {
            if (!project || actionBusy.value || organization.savingOrder.value) return
            actionBusy.value = project.id
            managerError.value = ''; archiveStatus.value = ''
            try {
                const api = await ext.patchJson(`/archive/${encodeURIComponent(project.id)}`, { archived })
                if (!api.response) throw new Error(api.error?.message || 'Unable to update project archive.')
                organization.updateProjects(api.response)
                if (archived) {
                    if (ctx.state.prefs.project === project.name) ctx.state.prefs.project = null
                    archiveSearch.value = ''
                    await openArchived()
                }
                archiveStatus.value = archived ? `${project.name} archived` : `${project.name} restored to active projects`
            } catch (error) {
                managerError.value = error.message
            } finally { actionBusy.value = null }
        }

        async function deleteProject() {
            if (selectedIdx.value === null || isNewProject.value) return
            const projName = localProjects.value[selectedIdx.value].name
            if (!confirm(`Are you sure you want to delete the project "${projName}"?`)) return

            const projects = localProjects.value.filter(p => p.id !== selectedId.value)
            if (!await persistProjects(projects)) return
            ctx.toast(`Deleted project: ${projName}`)
            cancelEdit()
        }

        async function saveForm() {
            if (!editForm.value.name.trim()) {
                ctx.setError('Project name is required')
                return
            }

            const folder = (editForm.value.folder.trim() || ctx.utils.toKebabCase(editForm.value.name)).trim()

            const updatedProject = {
                name: editForm.value.name.trim(),
                folder: folder,
                description: editForm.value.description.trim(),
                publish: sanitizePublishPath(editForm.value.publish, folder),
                publishedUrl: editForm.value.publishedUrl ? editForm.value.publishedUrl.trim() : '',
                showInSidebar: editForm.value.showInSidebar
            }

            // Check duplicate project name
            const isDuplicate = localProjects.value.some((p, idx) => {
                if (isNewProject.value) {
                    return p.name === updatedProject.name
                } else {
                    return p.name === updatedProject.name && idx !== selectedIdx.value
                }
            })

            if (isDuplicate) {
                ctx.setError('A project with this name already exists')
                return
            }

            const originalName = isNewProject.value
                ? updatedProject.name
                : localProjects.value[selectedIdx.value].name

            const creating = isNewProject.value
            const selecting = !creating && !isDirty.value
            // Selecting a hidden project brings its folder back to the sidebar.
            if (selecting) updatedProject.showInSidebar = true
            const success = await persistProject(updatedProject, originalName)
            if (!success) return

            const name = updatedProject.name
            if (creationRequest?.onCreated) {
                const project = ctx.state.projects.find(p => p.name === name)
                if (project) await creationRequest.onCreated(project)
                closeDialog()
                return
            }
            if (creating || selecting) {
                const project = ctx.state.projects.find(p => p.name === name)
                if (project) ctx.projects.openDraft(project.id)
            }
            const api = await ext.postJson('/active', { name })
            if (api.error) {
                ctx.setError(api.error, "Failed to switch project")
            } else {
                ctx.state.prefs.project = name
                ctx.toast(selecting ? `Switched to project: ${name}` : `Saved project: ${name}`)
                closeDialog()
            }
        }

        async function persistProject(updatedProject, originalName) {
            const api = await ctx.projects.saveProject(originalName, updatedProject)
            if (api.response) {
                localProjects.value = api.response

                // Update active project if needed
                const active = ctx.state.prefs.project
                if (active) {
                    if (active === originalName && updatedProject.name !== originalName) {
                        ctx.state.prefs.project = updatedProject.name
                    } else if (!localProjects.value.some(p => p.name === active)) {
                        ctx.state.prefs.project = null
                    }
                }
                return true
            }
        }

        async function persistProjects(projects) {
            // Save localProjects to backend
            const api = await ext.postJson(`/projects.json`, projects)
            if (api.error) {
                ctx.setError(api.error, "Failed to save projects")
            } else {
                organization.updateProjects(api.response)
                // Update active project if needed
                const active = ctx.state.prefs.project
                if (active && !localProjects.value.some(p => p.name === active)) {
                    ctx.state.prefs.project = null
                }
                return true
            }
        }

        const isDirty = computed(() => {
            if (selectedIdx.value === null) return false

            const orig = isNewProject.value
                ? { name: '', folder: '', description: '', publish: '' }
                : localProjects.value[selectedIdx.value]

            // Check if name, folder, description, or publish changed
            if ((editForm.value.name || '').trim() !== (orig?.name || '').trim()) return true
            if ((editForm.value.folder || '').trim() !== (orig?.folder || ctx.utils.toKebabCase(orig?.name || '')).trim()) return true
            if ((editForm.value.description || '').trim() !== (orig?.description || '').trim()) return true
            if ((editForm.value.publish || '').trim() !== (orig?.publish || '').trim()) return true
            if (editForm.value.showInSidebar !== (orig?.showInSidebar !== false)) return true

            return false
        })

        function closeDialog() {
            emit('done')
        }

        return {
            ...organization,
            repositoryUrl,
            repositoryWebUrl,
            repositoryCopied,
            copyRepositoryUrl,
            localProjects,
            selectedId,
            selectedIdx,
            showArchived,
            archiveSearch,
            archiveSearchInput,
            filteredArchives,
            managerError,
            archiveStatus,
            actionBusy,
            openArchived,
            setArchived,
            isNewProject,
            editForm,
            isFolderManuallyEdited,
            onNameInput,
            selectEditProject,
            createNewProject,
            cancelEdit,
            deleteProject,
            saveForm,
            closeDialog,
            isDirty,
            trapFocus,
        }
    }
}

export default {
    order: 30 - 100,

    install(ctx) {
        ext = ctx.scope('projects')

        ctx.components({ ProjectsSelector, ProjectsManagerModal })

        ctx.modals({
            'projects-manager': ProjectsManagerModal
        })

        ctx.setGlobals({
            projects: useProjects(ext)
        })
    },

    async load(ctx) {
        const api = await ext.getJson(`/projects.json`)
        const projects = api.response || []
        ctx.setState({ projects })
        console.log('project.state', JSON.stringify(ext.state, undefined, 2))
    }
}
