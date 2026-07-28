import { ref, computed, inject, onMounted, onUnmounted, watch } from "vue"

let ext

const SharePanel = {
    template: `
    <div class="px-4 py-3 overflow-y-auto border-b transition-all duration-300" :class="$styles.panel">
        <div class="max-w-4xl mx-auto">

            <!-- Content Area with Transition -->
            <transition enter-active-class="transition duration-300 ease-out"
                        enter-from-class="transform scale-95 opacity-0"
                        enter-to-class="transform scale-100 opacity-100"
                        leave-active-class="transition duration-200 ease-in"
                        leave-from-class="transform scale-100 opacity-100"
                        leave-to-class="transform scale-95 opacity-0"
                        mode="out-in">
                
                <!-- 1. NOT CONFIGURED: Show register iframe and info -->
                <div v-if="!isConfigured" key="register" class="flex flex-col lg:flex-row gap-6 items-stretch">
                    <!-- Benefits & Info -->
                    <div class="flex-1 flex flex-col justify-between p-6 rounded-xl border border-gray-200 dark:border-gray-700/80 bg-white dark:bg-gray-900/60 shadow-sm">
                        <div class="space-y-4">
                            <h4 class="text-base font-semibold text-gray-900 dark:text-gray-100">Publish &amp; Share Your Conversations</h4>
                            <p class="text-xs leading-relaxed" :class=[$styles.muted]>
                                Connect a publisher account to generate public, read-only links for your chat sessions. This makes it easy to share styled conversations with colleagues or friends.
                            </p>
                            <ul class="space-y-2.5 text-xs text-gray-600 dark:text-gray-300">
                                <li class="flex items-start gap-2">
                                    <span class="text-green-500 font-bold">✓</span>
                                    <span>Generate beautiful public read-only links</span>
                                </li>
                                <li class="flex items-start gap-2">
                                    <span class="text-green-500 font-bold">✓</span>
                                    <span>Retain full formatting of code, tables, and prompts</span>
                                </li>
                                <li class="flex items-start gap-2">
                                    <span class="text-green-500 font-bold">✓</span>
                                    <span>Revoke links or disconnect your account at any time</span>
                                </li>
                            </ul>
                        </div>
                        <div class="mt-6 pt-4 border-t border-gray-100 dark:border-gray-800 text-xs text-gray-400 dark:text-gray-500">
                            Registration is free and takes less than a minute.
                        </div>
                    </div>
                    
                    <!-- Iframe Container -->
                    <div class="w-full lg:w-[500px] shrink-0 rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 overflow-hidden shadow-sm flex flex-col">
                        <div class="flex-1 bg-white dark:bg-gray-900 min-h-[460px] flex items-center justify-center">
                            <iframe :src="registerUrl" class="w-full h-[550px] border-none" allow="clipboard-write"></iframe>
                        </div>
                    </div>
                </div>

                <!-- 2. CONFIGURED: Show account dashboard -->
                <div v-else key="dashboard" class="relative text-left">
                    
                    <!-- Collapsible Account Connected Widget in Top Right -->
                    <div class="absolute top-0 right-0 z-40">
                        <div class="relative" ref="accountMenuContainer">
                            <button type="button" @click="toggleAccountMenu"
                                    class="flex items-center gap-2 px-3 py-1.5 rounded-lg border text-xs font-semibold shadow-sm transition-colors"
                                    :class="$styles.dropdownButton">
                                <span class="size-2 rounded-full bg-green-500 animate-pulse"></span>
                                <span>Connected: @{{ publish.userName }}</span>
                                <svg class="size-4 opacity-60" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                                    <path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd" />
                                </svg>
                            </button>

                            <!-- Account Details Dropdown/Menu -->
                            <div v-if="showAccountMenu" 
                                 class="absolute right-0 mt-1.5 w-72 rounded-lg shadow-xl z-50 p-4 border text-sm"
                                 :class="$styles.bgPopover || 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700'">
                                <div class="flex items-center gap-3.5 mb-4 pb-3 border-b" :class="$styles.chromeBorder">
                                    <div class="p-2 rounded-lg bg-green-50 dark:bg-green-950/40 text-green-600 dark:text-green-400 border border-green-400/40 dark:border-green-800/40">
                                        <svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24">
                                            <path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                                        </svg>
                                    </div>
                                    <div>
                                        <h4 class="text-xs font-bold text-gray-950 dark:text-white">Account Connected</h4>
                                        <span class="text-xs" :class=[$styles.muted]>Linked and ready to publish</span>
                                    </div>
                                </div>
                                <div class="space-y-3.5">
                                    <div>
                                        <label class="text-[9px] font-bold text-gray-400 dark:text-gray-500 uppercase tracking-wider">API Key</label>
                                        <div class="mt-0.5 text-xs font-mono text-gray-600 dark:text-gray-300 bg-gray-50 dark:bg-gray-950 px-2 py-1 rounded border break-all select-all" :class="$styles.borderInput">
                                            {{ publish.apiKey }}
                                        </div>
                                    </div>
                                    <div class="pt-3 border-t flex justify-between items-center" :class="$styles.chromeBorder">
                                        <span class="text-xs" :class="$styles.muted" :title="'UserId: ' + publish.userId">@{{ publish.userName }}</span>
                                        <button @click="disconnect" type="button" class="text-xs font-bold text-red-600 hover:text-red-500 dark:text-red-400 dark:hover:text-red-300 transition-colors">
                                            Disconnect
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- Main Section: Publishing & Sharing -->
                    <div class="mr-48 pr-4">
                        <h4 class="text-base font-bold text-gray-950 dark:text-white mb-2">Publishing & Sharing</h4>
                        <p class="text-xs mb-6" :class=[$styles.muted]>
                            Configure options below to share your conversations or projects directly to the public web.
                        </p>

                        <!-- Tabs -->
                        <div class="flex border-b mb-6" :class="$styles.chromeBorder">
                            <!-- Thread Tab -->
                            <button type="button" @click="publishType = 'thread'"
                                    class="px-4 py-2.5 text-sm font-semibold border-b-2 transition-colors -mb-px"
                                    :class="publishType === 'thread' 
                                        ? 'border-blue-500 text-blue-600 dark:text-blue-400' 
                                        : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300'">
                                Publish Chat Thread
                            </button>
                            <!-- Project Tab -->
                            <button v-if="activeProjectName" type="button" @click="publishType = 'project'"
                                    class="px-4 py-2.5 text-sm font-semibold border-b-2 transition-colors -mb-px"
                                    :class="publishType === 'project' 
                                        ? 'border-blue-500 text-blue-600 dark:text-blue-400' 
                                        : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300'">
                                Publish Project ({{ activeProjectName }})
                            </button>
                        </div>

                        <ErrorSummary v-if="$state.error" class="mb-3" :status="$state.error" />

                        <!-- Details Section -->
                        <div class="p-4 rounded-xl border bg-gray-50/50 dark:bg-gray-950/10 mb-6" :class="$styles.borderInput">
                            
                            <!-- Case A: Thread Publishing -->
                            <div v-if="publishType === 'thread'" class="space-y-4">
                                <div v-if="!currentThread" class="text-sm italic" :class=[$styles.muted]>
                                    No active conversation thread selected. Go back to chat or start a new thread to share.
                                </div>
                                <div v-else class="space-y-4">
                                    <div>
                                        <label class="block text-xs font-bold uppercase tracking-wider mb-1.5" :class=[$styles.muted]>Sharing Thread</label>
                                        <div class="p-3.5 rounded-lg border text-sm bg-white dark:bg-gray-900 flex flex-col gap-2" :class="$styles.borderInput">
                                            <span class="font-semibold text-gray-955 dark:text-white break-words">{{ currentThread.title || 'Untitled Thread' }}</span>
                                            <div class="flex">
                                                <span class="text-xs px-2 py-0.5 rounded bg-gray-100 dark:bg-gray-800 font-mono" :class=[$styles.muted]>
                                                    Model: {{ currentThread.model || 'unknown' }}
                                                </span>
                                            </div>
                                        </div>
                                    </div>

                                    <!-- Status / Published Info UI -->
                                    <div class="mt-4 p-4 rounded-xl border text-left" :class="currentThread.publishedUrl ? 'border-green-400 dark:border-green-800/60 bg-green-50/10 dark:bg-green-950/5' : 'border-gray-200 dark:border-gray-800 bg-gray-50/20 dark:bg-gray-900/10'">
                                        <div class="flex items-center justify-between mb-2">
                                            <div class="flex items-center gap-2">
                                                <span class="size-2 rounded-full" :class="currentThread.publishedUrl ? 'bg-green-500 animate-pulse' : 'bg-gray-400'"></span>
                                                <span class="text-xs font-bold" :class="currentThread.publishedUrl ? 'text-green-800 dark:text-green-300' : 'text-gray-600 dark:text-gray-400'">
                                                    {{ currentThread.publishedUrl ? 'Published' : 'Not Published' }}
                                                </span>
                                            </div>
                                            <span v-if="currentThread.publishedAt" class="text-xs text-gray-400 dark:text-gray-500">
                                                Published at: {{ new Date(currentThread.publishedAt).toLocaleString() }}
                                            </span>
                                        </div>
                                        <div v-if="currentThread.publishedUrl" @click="copyThreadUrl" 
                                             class="flex items-center justify-between gap-3 text-xs px-2.5 py-1.5 rounded-lg border font-mono bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-800 cursor-pointer select-none group/link relative transition-all hover:border-gray-400 dark:hover:border-gray-600"
                                             :class="$styles.borderInput">
                                            <!-- Left side: link + external icon -->
                                            <div class="flex items-center gap-1.5 min-w-0 flex-1">
                                                <a :href="currentThread.publishedUrl" target="_blank" rel="noopener noreferrer" 
                                                   @click.stop
                                                   class="text-blue-600 hover:text-blue-500 dark:text-blue-400 dark:hover:text-blue-300 hover:underline truncate flex items-center gap-1.5">
                                                    <svg class="size-3.5 shrink-0 opacity-70" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                                                        <path stroke-linecap="round" stroke-linejoin="round" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
                                                    </svg>
                                                    <span class="truncate">{{ currentThread.publishedUrl }}</span>
                                                </a>
                                            </div>
                                            <!-- Right side: copy/check icon -->
                                            <div class="shrink-0 flex items-center text-gray-400 dark:text-gray-500 group-hover/link:text-gray-800 dark:group-hover/link:text-gray-200 opacity-60 group-hover/link:opacity-100 transition-all duration-200">
                                                <svg v-if="copiedThread" class="size-4 text-green-500 dark:text-green-400 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path>
                                                </svg>
                                                <svg v-else class="size-4 shrink-0" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                                    <rect width="14" height="14" x="8" y="8" rx="2" ry="2"/>
                                                    <path d="M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2"/>
                                                </svg>
                                            </div>
                                        </div>
                                    </div>

                                    <div class="flex items-center justify-between pt-2">
                                        <span class="text-xs" :class=[$styles.muted]>Thread contents will be made public via a secure link.</span>
                                        <button type="button" @click="publishThread" :disabled="isPublishing"
                                                class="px-4 py-2 text-xs font-bold transition-all shadow-sm"
                                                :class="$styles.primaryButton">
                                            <span v-if="isPublishing">Publishing...</span>
                                            <span v-else>{{ currentThread.publishedUrl ? 'Update Thread' : 'Publish Thread' }}</span>
                                        </button>
                                    </div>
                                </div>
                            </div>

                            <!-- Case B: Project Publishing -->
                            <div v-else-if="publishType === 'project'" class="space-y-4">
                                <div>
                                    <label class="block text-xs font-bold uppercase tracking-wider mb-1.5" :class=[$styles.muted]>Build Directory (dist)</label>
                                    <div class="flex items-stretch gap-2">
                                        <div class="relative flex-1">
                                            <input type="text" v-model="overrideDistPath" placeholder="deploy root project folder"
                                                   class="block w-full rounded-lg px-3.5 py-2 text-sm focus:ring-2 focus:ring-blue-500 outline-none border font-mono bg-white dark:bg-gray-900 placeholder:text-gray-400"
                                                   :class="[$styles.textInput, $styles.borderInput]" spellcheck="false" />
                                            <span v-if="isDetectingDist" class="absolute right-3.5 top-1/2 -translate-y-1/2 text-xs text-gray-400">
                                                Detecting...
                                            </span>
                                        </div>
                                        <button type="button" @click="openFolderBrowser"
                                                class="px-3 rounded-lg border text-xs font-semibold hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors flex items-center gap-1.5"
                                                :class="[$styles.dropdownButton, $styles.borderInput]">
                                            <svg class="w-4 h-4 text-gray-500" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                                                <path stroke-linecap="round" stroke-linejoin="round" d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z" />
                                            </svg>
                                            <span>Browse</span>
                                        </button>
                                    </div>
                                    <span class="text-xs mt-1 block" :class=[$styles.muted]>
                                        Relative path from project folder to publish (e.g. dist, build, or leave empty for project root).
                                    </span>
                                </div>

                                <div class="flex items-center justify-between pt-2">
                                    <span class="text-xs" :class=[$styles.muted]>
                                        Publishing project: <span class="font-semibold text-gray-900 dark:text-gray-100">{{ activeProjectName }}</span>
                                    </span>
                                    <button type="button" @click="publishProject" :disabled="isPublishing"
                                            class="px-4 py-2 text-xs font-bold transition-all shadow-sm"
                                            :class="$styles.primaryButton">
                                        <span v-if="isPublishing">Publishing...</span>
                                        <span v-else>Publish Project</span>
                                    </button>
                                </div>

                                <div v-if="publishedProjectUrl" class="p-4 rounded-xl border border-green-400 dark:border-green-800 bg-green-50/30 dark:bg-green-950/10 text-left transition-all duration-300">
                                    <h5 class="text-xs font-bold text-green-800 dark:text-green-300 flex items-center gap-1.5 mb-2">
                                        <svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24">
                                            <path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                                        </svg>
                                        Successfully Published Project!
                                    </h5>
                                    <div @click="copyPublishedProjectUrl" 
                                         class="flex items-center justify-between gap-3 text-xs px-2.5 py-1.5 rounded-lg border font-mono bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-800 cursor-pointer select-none group/link relative transition-all hover:border-gray-400 dark:hover:border-gray-600"
                                         :class="$styles.borderInput">
                                        <!-- Left side: link + external icon -->
                                        <div class="flex items-center gap-1.5 min-w-0 flex-1">
                                            <a :href="publishedProjectUrl" target="_blank" rel="noopener noreferrer" 
                                               @click.stop
                                               class="text-blue-600 hover:text-blue-500 dark:text-blue-400 dark:hover:text-blue-300 hover:underline truncate flex items-center gap-1.5">
                                                <svg class="size-3.5 shrink-0 opacity-70" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                                                    <path stroke-linecap="round" stroke-linejoin="round" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
                                                </svg>
                                                <span class="truncate">{{ publishedProjectUrl }}</span>
                                            </a>
                                        </div>
                                        <!-- Right side: copy/check icon -->
                                        <div class="shrink-0 flex items-center text-gray-400 dark:text-gray-500 group-hover/link:text-gray-800 dark:group-hover/link:text-gray-200 opacity-60 group-hover/link:opacity-100 transition-all duration-200">
                                            <svg v-if="copiedProject" class="size-4 text-green-500 dark:text-green-400 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path>
                                            </svg>
                                            <svg v-else class="size-4 shrink-0" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                                <rect width="14" height="14" x="8" y="8" rx="2" ry="2"/>
                                                <path d="M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2"/>
                                            </svg>
                                        </div>
                                    </div>
                                </div>
                            </div>

                        </div>
                    </div>

                </div>

            </transition>

            <!-- Folder Browser Modal -->
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
        </div>
    </div>
    `,
    setup() {
        const ctx = inject('ctx')
        const publish = computed(() => ext.state.publish || {})
        const isConfigured = computed(() => !!(publish.value.userName && publish.value.apiKey))

        const activeProjectName = computed(() => ctx.state.prefs.project || null)
        const activeProjectFolder = computed(() => {
            if (!activeProjectName.value) return ''
            const p = ctx.projects?.getProject ? ctx.projects.getProject(activeProjectName.value) : null
            return p?.folder || ctx.utils.toKebabCase(activeProjectName.value)
        })

        const publishType = ref(activeProjectName.value ? 'project' : 'thread')
        const currentThread = computed(() => ctx.threads?.currentThread?.value || null)

        const isPublishing = ref(false)
        const publishedProjectUrl = ref('')
        const copiedThread = ref(false)
        const copiedProject = ref(false)

        const showAccountMenu = ref(false)
        const accountMenuContainer = ref(null)

        const toggleAccountMenu = () => {
            showAccountMenu.value = !showAccountMenu.value
        }

        const onDocClick = (e) => {
            const t = e.target
            if (accountMenuContainer.value && !accountMenuContainer.value.contains(t)) {
                showAccountMenu.value = false
            }
        }

        const handleMessage = async (event) => {
            const data = event.data
            if (data && data.type === 'register-success') {
                const { apiKey, userName, userId } = data
                try {
                    const api = await ext.postJson('/config.json', { apiKey, userName, userId })
                    if (api.response) {
                        ext.setState({ publish: api.response })
                        showAccountMenu.value = false
                        ext.toast('API Key was generated successfully!')
                    } else if (api.error) {
                        ext.setError(api.error, 'Failed to save configuration')
                    }
                } catch (e) {
                    ext.setError(e, 'Error linking account')
                }
            }
        }

        const disconnect = async () => {
            try {
                const api = await ext.postJson('/disconnect')
                if (api.response) {
                    ext.setState({ publish: api.response })
                    ext.toast('Account disconnected successfully')
                } else if (api.error) {
                    ext.setError(api.error, 'Failed to disconnect account')
                }
            } catch (e) {
                ext.setError(e, 'Error disconnecting account')
            }
        }

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

        const detectDistFolder = async () => {
            isDetectingDist.value = true
            try {
                const project = activeProjectName.value ? ctx.projects?.getProject(activeProjectName.value) : null
                const folder = project?.folder
                if (project && project.publish !== undefined && project.publish !== null) {
                    overrideDistPath.value = sanitizePublishPath(project.publish, folder)
                    return
                }
                const api = await ext.getJson('/detect-dist')
                if (api.response && api.response.dist !== undefined) {
                    overrideDistPath.value = sanitizePublishPath(api.response.dist, folder)
                } else {
                    overrideDistPath.value = ''
                }
            } catch (e) {
                console.warn('Failed to auto-detect dist folder', e)
            } finally {
                isDetectingDist.value = false
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

        const updatePublishedProjectUrl = () => {
            if (activeProjectName.value) {
                const project = ctx.projects.getProject(activeProjectName.value)
                publishedProjectUrl.value = project?.publishedUrl || ''
            } else {
                publishedProjectUrl.value = ''
            }
        }

        watch(activeProjectName, async (newProj) => {
            if (newProj) {
                publishType.value = 'project'
            } else {
                publishType.value = 'thread'
            }
            updatePublishedProjectUrl()
            await detectDistFolder()
        })

        onMounted(async () => {
            window.addEventListener('message', handleMessage)
            document.addEventListener('click', onDocClick)
            updatePublishedProjectUrl()
            await detectDistFolder()
        })

        onUnmounted(() => {
            window.removeEventListener('message', handleMessage)
            document.removeEventListener('click', onDocClick)
        })

        const registerUrl = computed(() => {
            const args = {}
            if (ctx.ai.auth.userName) {
                args['username'] = ctx.ai.auth.userName
            }
            let url = ext.state.publish.registerUrl || ''
            if (Object.keys(args).length) {
                url = `${url}${url.includes('?') ? '&' : '?'}${new URLSearchParams(args)}`
            }
            return url
        })

        const publishThread = async () => {
            if (!currentThread.value) return
            isPublishing.value = true

            const api = await ext.postJson(`/thread/${currentThread.value.id}`)
            if (api.response) {
                console.log(`/thread/${currentThread.value.id}`, api.response)
                const data = api.response
                if (data.publishedUrl) {
                    currentThread.value.publishedAt = data.publishedAt
                    currentThread.value.publishedUrl = data.publishedUrl
                    ext.toast('Thread published successfully!')
                    const thread = await ctx.threads.getThread(currentThread.value.id)
                    if (thread) {
                        ctx.threads.replaceThread(thread)
                    }
                }
            } else {
                console.log(api.error)
                ctx.setError(api.error, 'Failed to publish to remote platform')
            }
            isPublishing.value = false
        }

        const publishProject = async () => {
            if (!activeProjectName.value) return
            isPublishing.value = true
            publishedProjectUrl.value = ''

            const project = ctx.projects.getProject(activeProjectName.value)
            if (!project) {
                ext.setError('Project not found', 'Failed to publish project')
                isPublishing.value = false
                return
            }

            const cleanPublish = sanitizePublishPath(overrideDistPath.value, project.folder)
            overrideDistPath.value = cleanPublish

            if (cleanPublish != project.publish) {
                project.publish = cleanPublish
                const api = await ctx.projects.saveProject(project.name, project)
                if (api.error) {
                    ext.setError(api.error, 'Failed to save project publish path')
                    isPublishing.value = false
                    return
                }
            }

            try {
                const api = await ext.postJson(`/project/${encodeURIComponent(activeProjectName.value)}`)
                if (api.response) {
                    const data = api.response
                    if (data.publishedUrl) {
                        publishedProjectUrl.value = data.publishedUrl
                        ext.toast('Project published successfully!')
                        const proj = ctx.projects.getProject(activeProjectName.value)
                        if (proj) {
                            proj.publishedUrl = data.publishedUrl
                        }
                    } else {
                        ext.setError('Missing publishedUrl in response', 'Failed to publish project')
                    }
                } else if (api.error) {
                    ext.setError(api.error, 'Failed to publish project')
                }
            } catch (e) {
                ext.setError(e, 'Error publishing project')
            } finally {
                isPublishing.value = false
            }
        }

        const copyPublishedProjectUrl = () => {
            if (!publishedProjectUrl.value) return
            navigator.clipboard.writeText(publishedProjectUrl.value)
            ext.toast('Link copied to clipboard!')
            copiedProject.value = true
            setTimeout(() => {
                copiedProject.value = false
            }, 2000)
        }

        const copyThreadUrl = () => {
            if (!currentThread.value?.publishedUrl) return
            navigator.clipboard.writeText(currentThread.value.publishedUrl)
            ext.toast('Link copied to clipboard!')
            copiedThread.value = true
            setTimeout(() => {
                copiedThread.value = false
            }, 2000)
        }

        return {
            ext,
            publish,
            isConfigured,
            registerUrl,
            disconnect,
            publishType,
            activeProjectName,
            activeProjectFolder,
            currentThread,
            isPublishing,
            publishedProjectUrl,
            copiedThread,
            copiedProject,
            overrideDistPath,
            isDetectingDist,
            publishThread,
            publishProject,
            copyPublishedProjectUrl,
            copyThreadUrl,
            showAccountMenu,
            accountMenuContainer,
            toggleAccountMenu,
            showFolderBrowser,
            isBrowsing,
            browserCurrentPath,
            browserDisplayPath,
            browserParentPath,
            browserSubdirs,
            openFolderBrowser,
            closeFolderBrowser,
            navigateToFolder,
            goUpFolder,
            selectCurrentFolder,
        }
    }
}

const GalleryPublish = {
    template: `
    <div v-if="isConfigured" class="bg-white dark:bg-gray-800/20 p-3 rounded-lg border border-gray-200 dark:border-white/5 mb-4">
        <div v-if="item.publishedUrl" class="space-y-2">
            <div class="flex items-center gap-1.5 text-xs text-green-600 dark:text-green-400 font-semibold">
                <span class="size-2 rounded-full bg-green-500 animate-pulse"></span>
                Published
            </div>
            <a :href="item.publishedUrl" target="_blank" rel="noopener noreferrer" 
               class="flex items-center gap-1.5 text-xs text-blue-600 hover:text-blue-500 dark:text-blue-400 dark:hover:text-blue-300 hover:underline">
                <svg class="size-3.5 shrink-0 opacity-70" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
                </svg>
                Open Published Link
            </a>
        </div>
        <div v-else>
            <button type="button" @click="publishMedia" :disabled="isPublishing"
                    class="w-full flex items-center justify-center gap-1.5 px-3 py-2 text-xs font-bold transition-all shadow-sm rounded-lg"
                    :class="$styles.primaryButton">
                <span v-if="isPublishing">Publishing...</span>
                <span v-else class="flex items-center gap-1.5">
                    <svg class="size-3.5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" />
                    </svg>
                    Share Image
                </span>
            </button>
        </div>
    </div>
    `,
    props: {
        item: Object
    },
    setup(props) {
        const publish = computed(() => ext.state.publish || {})
        const isConfigured = computed(() => !!(publish.value.userName && publish.value.apiKey))
        const isPublishing = ref(false)

        const publishMedia = async () => {
            if (isPublishing.value) return
            isPublishing.value = true
            try {
                const api = await ext.postJson(`/media/${props.item.id}`)
                if (api.error) {
                    ext.setError(api.error, 'Failed to publish image')
                    return
                }
                const data = api.response
                if (data && data.publishedUrl) {
                    props.item.publishedUrl = data.publishedUrl
                    props.item.publishedAt = data.publishedAt
                    ext.toast('Image published successfully!')
                }
            } catch (e) {
                console.error(e)
                ext.setError(e, 'Failed to publish image')
            } finally {
                isPublishing.value = false
            }
        }

        return {
            publish,
            isConfigured,
            isPublishing,
            publishMedia,
        }
    }
}

const AudioPublish = {
    template: `
    <div v-if="isConfigured" class="inline-flex items-center">
        <a v-if="item.publishedUrl" :href="item.publishedUrl" target="_blank" rel="noopener noreferrer"
           class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold border transition-all text-green-600 dark:text-green-400 bg-green-50/50 dark:bg-green-950/20 border-green-200 dark:border-green-800/40 hover:bg-green-100 dark:hover:bg-green-900/30 shadow-sm">
            <span class="size-1.5 rounded-full bg-green-500 animate-pulse"></span>
            <span>open link</span>
            <svg class="w-3 h-3 shrink-0 opacity-70" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M13.5 6H5.25A2.25 2.25 0 003 8.25v10.5A2.25 2.25 0 005.25 21h10.5A2.25 2.25 0 0018 18.75V10.5m-10.5 6L21 3m0 0h-5.25M21 3v5.25" />
            </svg>
        </a>
        <div v-else>
            <span v-if="isPublishing" class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold border text-gray-500 border-gray-200 dark:border-gray-800/40 bg-gray-50 dark:bg-gray-950/20">
                <svg class="animate-spin h-3.5 w-3.5 text-gray-500 mr-1" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                <span>publishing...</span>
            </span>
            <button v-else type="button" @click="publishMedia"
                    class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold border transition-all text-blue-600 dark:text-blue-400 bg-blue-50/50 dark:bg-blue-950/20 border-blue-200 dark:border-blue-800/40 hover:bg-blue-100 dark:hover:bg-blue-900/30 shadow-sm">
                <svg class="w-3 h-3 shrink-0 opacity-70" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" />
                </svg>
                <span>share</span>
            </button>
        </div>
    </div>
    `,
    props: {
        item: Object
    },
    setup(props) {
        const publish = computed(() => ext.state.publish || {})
        const isConfigured = computed(() => !!(publish.value.userName && publish.value.apiKey))
        const isPublishing = ref(false)

        const publishMedia = async () => {
            if (isPublishing.value) return
            isPublishing.value = true
            try {
                const api = await ext.postJson(`/media/${props.item.id}`)
                if (api.error) {
                    ext.setError(api.error, 'Failed to publish audio')
                    return
                }
                const data = api.response
                if (data && data.publishedUrl) {
                    props.item.publishedUrl = data.publishedUrl
                    props.item.publishedAt = data.publishedAt
                    ext.toast('Audio published successfully!')
                }
            } catch (e) {
                console.error(e)
                ext.setError(e, 'Failed to publish audio')
            } finally {
                isPublishing.value = false
            }
        }

        return {
            publish,
            isConfigured,
            isPublishing,
            publishMedia,
        }
    }
}

export default {
    order: 100,

    install(ctx) {
        ext = ctx.scope('publish')

        ctx.components({
            SharePanel,
        })

        ctx.setTopIcons({
            publish: {
                component: {
                    template: `
                    <svg @click="$ctx.toggleTop('SharePanel')" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path d="M0 0h24v24H0z" fill="none" /><path fill="currentColor" d="m11 11.85l-1.875 1.875q-.3.3-.712.288T7.7 13.7q-.275-.3-.288-.7t.288-.7l3.6-3.6q.15-.15.325-.212T12 8.425t.375.063t.325.212l3.6 3.6q.3.3.288.7t-.288.7q-.3.3-.712.313t-.713-.288L13 11.85V19q0 .425-.288.713T12 20t-.712-.288T11 19zM4 8V6q0-.825.588-1.412T6 4h12q.825 0 1.413.588T20 6v2q0 .425-.288.713T19 9t-.712-.288T18 8V6H6v2q0 .425-.288.713T5 9t-.712-.288T4 8" /></svg>`,
                },
                isActive({ top }) { return top === 'SharePanel' },
                get title() { return 'Share' }
            }
        })

        ctx.gallery.setLightboxFooters({
            GalleryPublish,
        })
        ctx.gallery.setAudioActions({
            AudioPublish,
        })
    },

    async load(ctx) {
        const api = await ext.getJson(`/config.json`)
        const publish = api.response || {}
        ext.setState({ publish })
        console.log('publish', publish)
    }
}