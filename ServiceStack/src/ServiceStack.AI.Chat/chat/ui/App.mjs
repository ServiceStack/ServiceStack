import { ref, computed, watch, inject, onMounted, onUnmounted } from "vue"
import { useRouter, useRoute } from "vue-router"
import { AppContext } from "./ctx.mjs"

import WorkspaceFileView from "./modules/WorkspaceFileView.mjs"
import { workspaceQuery, installWorkspaceNavigation } from "./modules/explorerState.mjs"
import WorkspaceSidebar from "./modules/WorkspaceSidebar.mjs"

// Vertical Sidebar Icons
const LeftBar = {
    template: `
        <div class="select-none flex flex-col justify-between h-full">
            <!-- top icons -->
            <div class="flex flex-col space-y-2 pt-2.5 px-1">
                <div v-for="(icon, id) in $ctx.visibleComponents($ctx.left)" :key="id" class="relative flex items-center justify-center">
                    <component :is="icon.component"
                        :class="[icon.isActive({ ...$layout }) ? $styles.iconActive : $styles.icon, $styles.iconHover, icon.iconClass ?? 'size-7 p-1 cursor-pointer block rounded']"
                        @mouseenter="tooltip = icon.id"
                        @mouseleave="tooltip = ''"
                        />
                    <div v-if="tooltip === icon.id && !icon.isActive({ ...$layout })"
                        class="absolute left-full top-1/2 -translate-y-1/2 ml-2 px-2 py-1 text-xs text-white bg-gray-900 dark:bg-gray-800 rounded shadow-md z-50 whitespace-nowrap pointer-events-none" style="z-index: 60">
                        {{icon.title ?? icon.name}}
                    </div>
                </div>
            </div>
            <!-- bottom icons -->
            <div>
                <div class="pb-1 relative flex items-center justify-center" title="Settings" @click="$ctx.to($ctx.matchesPath($route.path,'/settings') ? '/' : '/settings')">
                    <svg class="size-7 p-1 cursor-pointer rounded block"
                        :class="[$ctx.matchesPath($route.path,'/settings') ? $styles.iconActive : $styles.icon, $styles.iconHover]"
                        xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="M19 7.5h-7.628a2.251 2.251 0 0 0-4.244 0H5V9h2.128a2.25 2.25 0 0 0 4.244 0H19zm0 7.5h-2.128a2.251 2.251 0 0 0-4.244 0H5v1.5h7.628a2.251 2.251 0 0 0 4.244 0H19z"/></svg>
                </div>
                <div class="flex items-center justify-center pt-1 pb-2">
                    <Avatar />
                </div>
            </div>
        </div>
    `,
    setup() {
        const tooltip = ref('')
        return {
            tooltip,
        }
    }
}

const LeftPanel = {
    template: `
        <div v-if="component" class="flex flex-col h-full border-r" :class="$styles.chromeBorder">
            <button type="button" @click="$ctx.toggleLayout('left',false)" class="absolute top-2 right-2 p-1 rounded-md lg:hidden z-20">
                <svg class="size-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor"><path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/></svg>
            </button>
            <component :is="component" />
        </div>
    `,
    setup() {
        /**@type {AppContext} */
        const ctx = inject('ctx')
        const component = computed(() => ctx.component(ctx.layout.left))
        return {
            component,
        }
    }
}

const TopBar = {
    template: `
        <div class="select-none flex space-x-1">
            <div v-for="(icon, id) in $ctx.visibleComponents($ctx.top)" :key="id" class="relative flex items-center justify-center">
                <component :is="icon.component"
                    class="size-7 p-1 cursor-pointer block border border-transparent rounded"
                    :class="[icon.isActive({ ...$layout }) ? $styles.iconActive : $styles.icon, $styles.iconHover]"
                    @mouseenter="tooltip = icon.id"
                    @mouseleave="tooltip = ''"
                    />
                <div v-if="tooltip === icon.id && !icon.isActive({ ...$layout })"
                    class="absolute top-full mt-2 px-2 py-1 text-xs text-white bg-gray-900 dark:bg-gray-800 rounded shadow-md z-50 whitespace-nowrap pointer-events-none"
                    :class="last2.includes(id) ? 'right-0' : 'left-1/2 -translate-x-1/2'">
                    {{icon.title ?? icon.name}}
                </div>
            </div>
        </div>
    `,
    setup() {
        const tooltip = ref('')
        const last2 = computed(() => Object.keys($ctx.top).slice(-2))
        return {
            tooltip,
            last2,
        }
    }
}

const TopPanel = {
    template: `
        <component v-if="component" :is="component" class="mb-2" />
    `,
    setup() {
        /**@type {AppContext} */
        const ctx = inject('ctx')
        const component = computed(() => ctx.component(ctx.layout.top))
        return {
            component,
        }
    }
}

export default {
    components: {
        WorkspaceSidebar,
        WorkspaceFileView,
        LeftBar,
        LeftPanel,
        TopBar,
        TopPanel,
    },
    setup() {
        const router = useRouter()
        const route = useRoute()

        /**@type {AppContext} */
        const ctx = inject('ctx')
        const ai = ctx.ai
        const removeWorkspaceNavigation = installWorkspaceNavigation(router)
        onUnmounted(removeWorkspaceNavigation)
        const workspaceOpen = computed(() => route.query.workspace === '1')
        const workspacePreview = computed(() => ctx.right[route.query.workspacePreview]?.preview || WorkspaceFileView)
        // Views that own scrolling can raise their header without the page viewport clipping it.
        const compactHeader = computed(() => route.meta.pageScroll === false && !(workspaceOpen.value && route.query.workspaceFile))
        function toggleWorkspace() {
            router.push({ query: workspaceQuery(route.query, workspaceOpen.value
                ? { workspace: '0', workspacePath: null, workspaceFile: null, workspacePreview: null, workspaceCommit: null, workspaceView: null, workspaceProject: null }
                : { workspace: '1' }) })
        }
        const isMobile = ref(false)
        const modal = ref()

        const checkMobile = () => {
            //const wasMobile = isMobile.value
            isMobile.value = window.innerWidth < 640 // sm breakpoint

            //console.log('checkMobile', wasMobile, isMobile.value)
            // Only auto-adjust sidebar state when transitioning between mobile/desktop
            if (isMobile.value) {
                ctx.toggleLayout('left', false)
            }
        }

        onMounted(() => {
            checkMobile()
            window.addEventListener('resize', checkMobile)
        })

        onUnmounted(() => {
            window.removeEventListener('resize', checkMobile)
        })

        function closeModal() {
            ctx.closeModal(route.query.open)
        }

        watch([() => ctx.state.startupReady, () => route.query.open], ([ready, name]) => {
            if (ready) modal.value = name ? ctx.openModal(name) : undefined
        })

        watch(() => ctx.state.selectedModel, (newVal) => {
            ctx.chat.setSelectedModel(ctx.chat.getModel(newVal))
        })

        const toastMessage = ref('')
        const showToast = ref(false)
        let toastTimeout = null

        watch(() => ctx.state.message, (newMsg) => {
            if (newMsg) {
                toastMessage.value = newMsg
                showToast.value = true
                if (toastTimeout) {
                    clearTimeout(toastTimeout)
                }
                toastTimeout = setTimeout(() => {
                    showToast.value = false
                }, 3000)
            } else {
                showToast.value = false
            }
        })

        watch(showToast, (val) => {
            if (!val) {
                ctx.state.message = null
                if (toastTimeout) {
                    clearTimeout(toastTimeout)
                    toastTimeout = null
                }
            }
        })

        return { ai, modal, workspacePreview, workspaceOpen, compactHeader, toggleWorkspace, isMobile, closeModal, toastMessage, showToast }
    },
    template: `
        <div class="flex h-screen" :class="$styles.app">
            <div v-if="$state.startupReady" class="flex w-full h-full" :class="$styles.appInner">
                <!-- Mobile Overlay -->
                <div v-if="isMobile && $ctx.layoutVisible('left') && $ai.hasAccess"
                    @click="$ctx.toggleLayout('left')"
                    :class="$ctx.cls('mobile-overlay', 'fixed inset-0 bg-black/50 z-40 lg:hidden')"
                ></div>

                <div v-if="$ai.hasAccess" id="sidebar" :class="$ctx.cls('sidebar', 'z-100 relative flex ' + $styles.bgSidebar)">
                    <LeftBar id="left-bar" class="relative z-60 shadow-[1px_0_4px_-2px_rgb(0_0_0_/_10%)] dark:shadow-[1px_0_4px_-2px_rgb(0_0_0_/_22%)]" />
                    <LeftPanel id="left-panel"
                        v-if="$ai.hasAccess && $ctx.layoutVisible('left')"
                        :class="[
                            'transition-transform duration-300 ease-in-out z-50',
                            'w-72 xl:w-80 flex-shrink-0',
                            'lg:relative',
                            'fixed inset-y-0 left-[2.25rem] lg:left-0',
                            $styles.bgSidebar
                        ]"
                    />
                </div>

                <!-- Main Area -->
                <div id="main" :class="$ctx.cls('main', 'flex-1 min-w-0 flex flex-col')">
                    <div id="main-inner" :class="$ctx.cls('main-inner', 'flex flex-col h-full w-full overflow-hidden')">
                        <div v-if="$ai.hasAccess && $route.meta.header !== false && $ctx.layoutVisible('header')" id="header" :class="[$ctx.cls('header', 'min-h-9 py-1 pr-1 flex items-center justify-between shrink-0'), compactHeader ? 'relative z-10 pointer-events-none bg-transparent!' : '']">
                            <div class="flex items-center gap-2" :class="compactHeader ? 'pointer-events-auto bg-[var(--background)]' : ''">
                                <component v-for="(c, id) in $ctx.visibleComponents($ctx.leftTop)" :is="c.component" />
                                <!--ThemeSelector /-->
                            </div>
                            <div class="flex items-center gap-2" :class="compactHeader ? 'pointer-events-auto bg-[var(--background)]' : ''">
                                <TopBar id="top-bar" />
                                <button v-if="$ctx.state.projects" type="button" @click="toggleWorkspace" :aria-expanded="workspaceOpen" aria-controls="workspace-sidebar" aria-label="Toggle workspace explorer" title="Workspace explorer" :class="[workspaceOpen ? $styles.iconActive : $styles.icon, $styles.iconHover]" data-workspace-icon class="flex items-center justify-center w-7 h-7 p-1 border border-transparent rounded shrink-0 cursor-pointer">
                                    <svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 16 16" aria-hidden="true" class="w-4.5 h-4.5"><path d="M0 0h16v16H0z" fill="none"/><path fill="currentColor" d="M12.5 1A2.5 2.5 0 0 1 15 3.5v9a2.5 2.5 0 0 1-2.5 2.5h-9A2.5 2.5 0 0 1 1 12.5v-9A2.5 2.5 0 0 1 3.5 1zM9 14V2H3.5A1.5 1.5 0 0 0 2 3.5v9A1.5 1.5 0 0 0 3.5 14z"/></svg>
                                </button>
                            </div>
                        </div>
                        <TopPanel v-if="$ai.hasAccess && (($route.meta.header !== false && $ctx.layoutVisible('header')) || $ctx.layout.top)" id="top-panel" :class="$ctx.cls('top-panel', 'shrink-0')" />
                        <div id="page" :class="$ctx.cls('page', 'flex-1 min-h-0 flex flex-col ' + (compactHeader ? 'overflow-visible' : 'overflow-y-auto'))">
                            <RouterView v-show="!workspaceOpen || !$route.query.workspaceFile" class="h-full" />
                            <component v-if="workspaceOpen && $route.query.workspaceFile" :is="workspacePreview" />
                        </div>
                    </div>
                </div>

                <WorkspaceSidebar v-if="workspaceOpen && $ai.hasAccess" @close="toggleWorkspace" @keydown.esc="toggleWorkspace" style="width: min(340px, 100vw); flex-shrink: 0" :style="isMobile ? {position:'fixed', right:0, top:0, bottom:0, zIndex:110} : {}" />

                <component v-if="modal" :is="modal" :class="$ctx.cls('modal', '!z-[200]')" :data-chat-model-selector="$route.query.open === 'models' ? '' : undefined" @done="closeModal" />

                <!-- Toast Popup -->
                <Transition name="toast">
                    <div v-if="showToast"
                        class="fixed bottom-5 right-5 z-[300] flex items-center gap-3 max-w-sm p-4 bg-white/80 dark:bg-gray-900/80 backdrop-blur-md border border-gray-200/50 dark:border-gray-800/50 rounded-xl shadow-xl transition-all duration-300">
                        <div class="flex-shrink-0 w-8 h-8 rounded-lg bg-emerald-500/10 dark:bg-emerald-400/10 flex items-center justify-center text-emerald-600 dark:text-emerald-400">
                            <svg class="w-5 h-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                            </svg>
                        </div>
                        <div class="flex-1 text-sm font-medium text-gray-800 dark:text-gray-200 leading-snug">
                            {{ toastMessage }}
                        </div>
                        <button type="button" @click="showToast = false" class="flex-shrink-0 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 transition-colors p-1 rounded-lg hover:bg-gray-100/50 dark:hover:bg-gray-800/50">
                            <svg class="w-4 h-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                            </svg>
                        </button>
                    </div>
                </Transition>

                <component :is="'style'">
                    .toast-enter-from, .toast-leave-to {
                        opacity: 0;
                        transform: translateY(1rem) scale(0.95);
                    }
                    .toast-enter-active, .toast-leave-active {
                        transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
                    }
                </component>
            </div>
            <div v-else role="status" aria-live="polite" aria-label="Loading llms.py" class="flex w-full h-full items-center justify-center" :class="$styles.appInner">
                <svg xmlns="http://www.w3.org/2000/svg" width="96" height="96" viewBox="0 0 96 96" fill="none" aria-hidden="true" focusable="false">
                <use href="#llms-loading-sprite" />
            </svg>
            </div>
        </div>
    `,
}
