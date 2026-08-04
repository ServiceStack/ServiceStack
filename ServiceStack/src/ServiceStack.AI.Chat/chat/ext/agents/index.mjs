import { ref, computed, onMounted, onUnmounted, inject, nextTick, watch } from "vue"

let ext

function useAgents(ext) {
    const ctx = ext.ctx
    let agents = {}
    ctx.setState({ agents })

    function getAgent(id) {
        return Object.values(ctx.state.agents).find(a => a.id === id) || null
    }

    function getProfileOverride(id) {
        const overrides = ext.prefs.overrides || {}
        return overrides[id] || null
    }

    function updateProfileOverrides(id, override) {
        const overrides = Object.assign({}, ext.prefs.overrides || {})
        if (override && (override.model || override.theme)) {
            overrides[id] = {
                model: override.model || null,
                theme: override.theme || null,
            }
        } else {
            delete overrides[id]
        }
        ext.setPrefs({ overrides })

        const agent = getAgent(id)
        if (agent) {
            agent.overrideModel = overrides[id]?.model || null
            agent.overrideTheme = overrides[id]?.theme || null
        }

        const activeProfileId = ext.prefs.selectedAgent || 'default'
        if (activeProfileId === id) {
            const activeProfile = id === 'default' ? null : getAgent(id)
            ctx.changeProfile(activeProfile, true)
        }
    }

    function selectAgent(id) {
        console.log('selectAgent', id)
        ext.setPrefs({
            selectedAgent: id
        })
        const profile = id ? getAgent(id) : null
        ctx.changeProfile(profile, true)
    }

    function getAvatarUrl(agent, timestamp) {
        const agentId = typeof agent === 'object' ? agent?.id : agent
        const ts = timestamp ? `?t=${timestamp}` : ''
        return `${ext.baseUrl}/${agentId}/avatar${ts}`
    }

    async function saveProfileConfig(id, config) {
        const res = await ext.postJson(`${id}/config`, config)
        if (res?.error) {
            throw new Error(res.error.message || 'Failed to save profile config')
        }
        await load()
    }

    async function getProfileFiles(id) {
        const res = await ext.getJson(`${id}/files`)
        const data = res?.response !== undefined ? res.response : res
        return Array.isArray(data) ? data : []
    }

    async function getFileContent(id, filename) {
        const url = `${ext.baseUrl}/${id}/files/${filename}`
        const res = await fetch(url)
        if (!res.ok) {
            throw new Error(`Failed to load file (${res.status})`)
        }
        return await res.text()
    }

    async function saveFileContent(id, filename, content) {
        const url = `${ext.baseUrl}/${id}/files/${filename}`
        const res = await fetch(url, {
            method: 'PUT',
            headers: { 'Content-Type': 'text/plain' },
            body: content
        })
        if (!res.ok) {
            const text = await res.text()
            throw new Error(text || `Failed to save file (${res.status})`)
        }
        await load()
    }

    async function createFile(id, filename, content) {
        const res = await ext.postJson(`${id}/files`, { filename, content })
        if (res?.error) {
            throw new Error(res.error.message || 'Failed to create file')
        }
        await load()
        return res?.response !== undefined ? res.response : res
    }

    async function deleteFile(id, filename) {
        const url = `${ext.baseUrl}/${id}/files/${filename}`
        const res = await fetch(url, { method: 'DELETE' })
        if (!res.ok) {
            throw new Error(`Failed to delete file`)
        }
        await load()
    }

    async function uploadAvatar(id, file) {
        const formData = new FormData()
        formData.append('file', file)
        const url = `${ext.baseUrl}/${id}/avatar`
        const res = await fetch(url, {
            method: 'POST',
            body: formData
        })
        if (!res.ok) {
            throw new Error('Failed to upload avatar')
        }
        await load()
    }

    async function fetchToolsAndSkills() {
        const res = await ext.getJson(`tools-skills`)
        const data = res?.response !== undefined ? res.response : res
        return data || {}
    }

    async function load() {
        const api = await ext.getJson(``)
        const agentDefs = (api?.response !== undefined ? api.response : api) || {}
        const overrides = ext.prefs.overrides || {}

        agents = Object.entries(agentDefs).map(([id, def]) => ({
            id,
            name: def.name || ctx.utils.idToName(id),
            theme: def.theme,
            avatar: getAvatarUrl(id, Date.now()),
            model: def.model,
            overrideModel: overrides[id]?.model || null,
            overrideTheme: overrides[id]?.theme || null,
            onlyTools: def.onlyTools,
            onlySkills: def.onlySkills,
            actions: def.actions ?? {},
            injectPrompt: def.injectPrompt ?? true,
            isBuiltIn: !!def.isBuiltIn,
            files: def.files || [],
            prompt: ''
        }))

        const tasks = []
        for (const agent of agents) {
            const id = agent.id

            tasks.push(ext.get(`${id}/system`)
                .then(r => r.text())
                .then(text => {
                    agent.prompt = text
                }))
        }

        await Promise.all(tasks)
        ctx.setState({ agents })

        const initialSelected = ext.prefs.selectedAgent ?? (localStorage.getItem('llms.profile') !== 'default' ? localStorage.getItem('llms.profile') : null)
        if (initialSelected) {
            selectAgent(initialSelected)
        } else {
            ctx.changeProfile(null, true)
        }
    }

    async function createProfile(name) {
        const res = await ext.postJson('', { name })
        if (res?.error) {
            throw new Error(res.error.message || 'Failed to create profile')
        }
        const data = res?.response !== undefined ? res.response : res
        await load()
        return data
    }

    async function deleteProfile(id) {
        const res = await ext.deleteJson(`${id}`)
        if (res?.error) {
            throw new Error(res.error.message || 'Failed to delete profile')
        }
        await load()
    }

    return {
        get all() { return Object.values(ctx.state.agents) },
        get selectedAgent() { return ext.prefs.selectedAgent },
        get selected() { return getAgent(ext.prefs.selectedAgent) },
        getAgent,
        getProfileOverride,
        updateProfileOverrides,
        load,
        selectAgent,
        getAvatarUrl,
        saveProfileConfig,
        createProfile,
        deleteProfile,
        getProfileFiles,
        getFileContent,
        saveFileContent,
        createFile,
        deleteFile,
        uploadAvatar,
        fetchToolsAndSkills,
    }
}

const AgentSelector = {
    template: `
    <div class="agent-selector relative inline-block text-left">
        <button 
            @click="toggleDropdown" 
            class="agent-trigger inline-flex items-center gap-2 px-3 py-2 rounded-md cursor-pointer text-sm text-gray-700 dark:text-gray-300 whitespace-nowrap h-[38px] box-border transition-all duration-150 ease-out transition-colors"
            :class="[{ 'border-blue-500 dark:border-blue-500': isOpen }, $styles.dropdownButton]"
        >
            <img 
                v-if="$ctx.agents.selected"
                :src="$ctx.agents.getAvatarUrl($ctx.agents.selected.id)" 
                :alt="$ctx.agents.selected.id"
                class="w-5 h-5 min-w-[20px] max-w-[20px] rounded-full object-cover shrink-0"
            />
            <img v-else
                :src="$ctx.getDefaultAgentAvatar()" 
                alt="Default Agent"
                class="w-6 h-6 min-w-[24px] max-w-[24px] rounded-full object-cover shrink-0"
            />
            <span class="whitespace-nowrap overflow-hidden text-ellipsis max-w-[100px] text-gray-700 dark:text-gray-300">{{ $ctx.agents.selected?.name || 'Default' }}</span>
            <svg class="w-4 h-4 text-gray-400 shrink-0 transition-transform duration-150 ease-in-out" :class="{ 'rotate-180': isOpen }" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd" />
            </svg>
        </button>
        
        <div v-show="isOpen" class="absolute top-[calc(100%+4px)] left-0 min-w-full w-max max-w-[220px] rounded-lg shadow-lg z-50 overflow-hidden" :class="$styles.bgPopover">
            <button 
                @click="selectAgent(null)"
                class="flex items-center gap-2 w-full px-3 py-2 border-none cursor-pointer text-left transition-colors duration-100 ease-in-out"
                :class="[$styles.popoverButton, !ext.prefs.selectedAgent ? $styles.popoverButtonActive : 'bg-transparent']"
            >
                <img 
                    :src="$ctx.getDefaultAgentAvatar()" 
                    alt="Default Agent"
                    class="w-6 h-6 min-w-[24px] max-w-[24px] rounded-full object-cover shrink-0"
                />
                <div class="flex-1 min-w-0 flex flex-col gap-[1px]">
                    <span class="text-[13px] font-medium text-gray-900 dark:text-gray-100 whitespace-nowrap overflow-hidden text-ellipsis">Default</span>
                </div>
                <svg v-if="!ext.prefs.selectedAgent" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4 text-blue-500 shrink-0">
                    <path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 011.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clip-rule="evenodd" />
                </svg>
            </button>
            <button 
                v-for="agent in $ctx.agents.all" 
                :key="agent.id"
                @click="selectAgent(agent.id)"
                class="flex items-center gap-2 w-full px-3 py-2 border-none cursor-pointer text-left transition-colors duration-100 ease-in-out"
                :class="[$styles.popoverButton, ext.prefs.selectedAgent === agent.id ? $styles.popoverButtonActive : 'bg-transparent']"
            >
                <img 
                    :src="agent.avatar" 
                    :alt="agent.name"
                    class="w-6 h-6 min-w-[24px] max-w-[24px] rounded-full object-cover shrink-0"
                />
                <div class="flex-1 min-w-0 flex flex-col gap-[1px]">
                    <span class="text-[13px] font-medium text-gray-900 dark:text-gray-100 whitespace-nowrap overflow-hidden text-ellipsis">{{ agent.name }}</span>
                </div>
                <svg v-if="ext.prefs.selectedAgent === agent.id" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4 text-blue-500 shrink-0">
                    <path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 011.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clip-rule="evenodd" />
                </svg>
            </button>

            <!-- Manage Profiles Button -->
            <button type="button" @click="manageProfiles"
                class="w-full text-left px-3 py-2 flex items-center space-x-2 transition-colors text-sm border-t cursor-pointer"
                :class="[$styles.popoverButton, $styles.chromeBorder]">
                <svg xmlns="http://www.w3.org/2000/svg" class="size-4 flex-shrink-0" :class="$styles.mutedIcon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M12 20h9M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z"></path>
                </svg>
                <span class="font-medium">Manage Profiles</span>
            </button>
        </div>
    </div>
    `,
    setup() {
        const ctx = inject('ctx')
        const isOpen = ref(false)

        const toggleDropdown = () => {
            isOpen.value = !isOpen.value
        }

        const selectAgent = (id) => {
            if (ext.prefs.selectedAgent === id) {
                ctx.agents.selectAgent(null)
            } else {
                ctx.agents.selectAgent(id)
            }
            isOpen.value = false
        }

        const manageProfiles = () => {
            isOpen.value = false
            ctx.openModal('ProfilesManagerModal')
        }

        const handleClickOutside = (e) => {
            const selector = document.querySelector('.agent-selector')
            if (selector && !selector.contains(e.target)) {
                isOpen.value = false
            }
        }

        onMounted(() => {
            document.addEventListener('click', handleClickOutside)
        })

        onUnmounted(() => {
            document.removeEventListener('click', handleClickOutside)
        })

        return {
            ext,
            isOpen,
            toggleDropdown,
            selectAgent,
            manageProfiles,
        }
    }
}

const ThreadProfile = {
    template: `
    <span v-if="thread.metadata?.profile" @click="$ctx.agents.selectAgent(thread.metadata?.profile)"
        class="flex items-center cursor-pointer px-1.5 py-0.5 text-xs rounded transition-colors space-x-2" :class="[$styles.tagLabel, $styles.tagLabelHover]">
        <img 
            v-if="thread.metadata.profile"
            :src="$ctx.agents.getAvatarUrl(thread.metadata.profile)" 
            :alt="thread.metadata.profile"
            class="w-4 h-4 min-w-[20px] max-w-[20px] rounded-full object-cover shrink-0"
        />
        <img v-else
            :src="$ctx.getDefaultAgentAvatar()" 
            alt="Default Agent"
            class="w-4 h-4 min-w-[24px] max-w-[24px] rounded-full object-cover shrink-0"
        />
        <span class="whitespace-nowrap overflow-hidden text-ellipsis max-w-[120px]">{{thread.metadata?.profile}}</span>
    </span>
    `,
    props: { thread: Object },
}

const ProfilesManagerModal = {
    template: `
        <!-- Dialog Overlay -->
        <div class="fixed inset-0 z-50 overflow-hidden text-gray-900 dark:text-gray-100" @keydown.escape.stop="handleEscape">
            <!-- Backdrop -->
            <div class="fixed inset-0 bg-black/50 transition-opacity" @click="closeDialog"></div>
            
            <!-- Dialog -->
            <div class="fixed inset-4 md:inset-8 lg:inset-12 flex items-center justify-center">
                <div class="relative bg-white dark:bg-gray-800 rounded-xl shadow-2xl w-full h-full max-w-5xl max-h-[90vh] flex flex-col overflow-hidden">
                    <!-- Header -->
                    <div class="flex-shrink-0 px-6 py-4 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
                        <h2 class="text-xl font-semibold">Profile Manager</h2>
                        <button type="button" @click="closeDialog" class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 transition-colors">
                            <svg class="size-6" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                                <path fill="currentColor" d="M19 6.41L17.59 5L12 10.59L6.41 5L5 6.41L10.59 12L5 17.59L6.41 19L12 13.41L17.59 19L19 17.59L13.41 12z"/>
                            </svg>
                        </button>
                    </div>
                    
                    <!-- Main Body Split Pane -->
                    <div class="flex-1 flex overflow-hidden">
                        <!-- Left pane: Profiles List -->
                        <div class="w-56 shrink-0 border-r border-gray-200 dark:border-gray-700 flex flex-col bg-gray-50 dark:bg-gray-800/40">
                            <div class="px-3.5 py-2.5 text-xs font-semibold uppercase tracking-wider flex justify-between items-center border-b border-gray-200 dark:border-gray-700" :class="$styles.muted">
                                <span>Select Profile</span>
                                <button type="button" @click="isNewProfileDialogOpen = true"
                                    class="p-1 rounded hover:bg-gray-200 dark:hover:bg-gray-700 text-gray-600 dark:text-gray-300 transition-colors cursor-pointer"
                                    title="Create New Profile">
                                    <svg class="size-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                                        <path stroke-linecap="round" stroke-linejoin="round" d="M12 4v16m8-8H4" />
                                    </svg>
                                </button>
                            </div>
                            <div class="flex-1 overflow-y-auto p-2 space-y-1">
                                <!-- Default Profile -->
                                <button type="button" @click="selectProfile(defaultProfileItem)"
                                    class="w-full text-left px-3 py-2 rounded-lg flex items-center justify-between text-sm transition-all cursor-pointer select-none"
                                    :class="[selectedId === 'default' ? 'bg-blue-50 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 font-semibold ring-1 ring-blue-500/20' : 'hover:bg-gray-100 dark:hover:bg-gray-700/50 text-gray-700 dark:text-gray-300']">
                                    <div class="flex items-center space-x-2.5 min-w-0">
                                        <img :src="$ctx.getDefaultAgentAvatar()" alt="Default" class="size-6 rounded-full object-cover shrink-0" />
                                        <span class="truncate">Default</span>
                                    </div>
                                    <span v-if="hasOverride('default')" class="size-2 rounded-full bg-blue-500 shrink-0" title="Has overridden preferences"></span>
                                </button>

                                <!-- Agent Profiles -->
                                <button v-for="agent in agentProfiles" :key="agent.id"
                                    type="button"
                                    @click="selectProfile(agent)"
                                    class="w-full text-left px-3 py-2 rounded-lg flex items-center justify-between text-sm transition-all cursor-pointer select-none"
                                    :class="[selectedId === agent.id ? 'bg-blue-50 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 font-semibold ring-1 ring-blue-500/20' : 'hover:bg-gray-100 dark:hover:bg-gray-700/50 text-gray-700 dark:text-gray-300']">
                                    <div class="flex items-center space-x-2.5 min-w-0">
                                        <img :src="agent.avatar" :alt="agent.name" class="size-6 rounded-full object-cover shrink-0" />
                                        <span class="truncate">{{ agent.name }}</span>
                                        <svg v-if="agent.isBuiltIn" class="size-3.5 text-gray-400 shrink-0" title="Built-in profile (read-only)" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                                            <path fill-rule="evenodd" d="M10 1a4.5 4.5 0 00-4.5 4.5V9H5a2 2 0 00-2 2v6a2 2 0 002 2h10a2 2 0 002-2v-6a2 2 0 00-2-2h-.5V5.5A4.5 4.5 0 0010 1zm3 8V5.5a3 3 0 10-6 0V9h6z" clip-rule="evenodd" />
                                        </svg>
                                    </div>
                                    <span v-if="hasOverride(agent.id)" class="size-2 rounded-full bg-blue-500 shrink-0" title="Has overridden preferences"></span>
                                </button>
                            </div>
                        </div>

                        <!-- Right pane: Profile Detail / Editor -->
                        <div class="flex-1 overflow-y-auto p-6 flex flex-col bg-white dark:bg-gray-800">
                            <div v-if="!selectedProfile" class="flex-1 flex flex-col items-center justify-center text-gray-400 dark:text-gray-500">
                                <svg xmlns="http://www.w3.org/2000/svg" class="size-16 mb-4 opacity-40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                                    <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
                                    <circle cx="12" cy="7" r="4"></circle>
                                </svg>
                                <p class="text-sm">Select a profile to customize settings.</p>
                            </div>
                            <div v-else class="flex-1 flex flex-col justify-between h-full space-y-4">
                                <div class="space-y-4 flex-1 flex flex-col min-h-0">
                                    <!-- Profile Header Info -->
                                    <div class="flex items-start space-x-4 pb-4 border-b border-gray-200 dark:border-gray-700">
                                        <div class="relative group shrink-0">
                                            <img :src="selectedProfile.avatar" :alt="selectedProfile.name" class="size-14 rounded-full object-cover shadow-sm border border-gray-200 dark:border-gray-700" />
                                            <button v-if="!selectedProfile.isBuiltIn && selectedId !== 'default'" type="button" @click="triggerAvatarUpload"
                                                class="absolute inset-0 bg-black/50 rounded-full flex items-center justify-center text-white opacity-0 group-hover:opacity-100 transition-opacity cursor-pointer"
                                                title="Upload new avatar">
                                                <svg class="size-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                                                    <path stroke-linecap="round" stroke-linejoin="round" d="M3 9a2 2 0 012-2h.93a2 2 0 001.664-.89l.812-1.22A2 2 0 0110.07 4h3.86a2 2 0 011.664.89l.812 1.22A2 2 0 0018.07 7H19a2 2 0 012 2v9a2 2 0 01-2 2H5a2 2 0 01-2-2V9z" />
                                                    <path stroke-linecap="round" stroke-linejoin="round" d="M15 13a3 3 0 11-6 0 3 3 0 016 0z" />
                                                </svg>
                                            </button>
                                            <input ref="avatarInputRef" type="file" accept="image/*" class="hidden" @change="handleAvatarUpload" />
                                        </div>
                                        <div class="flex-1 min-w-0">
                                            <div v-if="!selectedProfile.isBuiltIn && selectedId !== 'default'" class="mb-1">
                                                <input type="text" v-model="editForm.name" placeholder="Profile Name"
                                                    class="text-lg font-semibold bg-transparent border-b border-dashed border-gray-300 dark:border-gray-600 focus:border-blue-500 focus:outline-none w-full py-0.5" />
                                            </div>
                                            <div v-else class="flex items-center space-x-2">
                                                <h3 class="text-lg font-semibold truncate">{{ selectedProfile.name }}</h3>
                                                <span v-if="selectedProfile.isBuiltIn" class="inline-flex items-center space-x-1 px-2 py-0.5 rounded text-[11px] font-medium bg-amber-100 dark:bg-amber-900/40 text-amber-800 dark:text-amber-300 shrink-0">
                                                    <svg class="size-3" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                                                        <path fill-rule="evenodd" d="M10 1a4.5 4.5 0 00-4.5 4.5V9H5a2 2 0 00-2 2v6a2 2 0 002 2h10a2 2 0 002-2v-6a2 2 0 00-2-2h-.5V5.5A4.5 4.5 0 0010 1zm3 8V5.5a3 3 0 10-6 0V9h6z" clip-rule="evenodd" />
                                                    </svg>
                                                    <span>Built-in Profile (Read-only)</span>
                                                </span>
                                            </div>
                                            <div class="text-xs font-mono" :class="$styles.muted">ID: {{ selectedProfile.id }}</div>
                                        </div>

                                        <button v-if="!selectedProfile.isBuiltIn && selectedId !== 'default'" type="button" @click="deleteCurrentProfile"
                                            class="px-2.5 py-1.5 text-xs font-medium text-red-600 hover:text-red-700 hover:bg-red-50 dark:text-red-400 dark:hover:bg-red-950/40 border border-red-200 dark:border-red-800 rounded-lg transition-colors cursor-pointer shrink-0 flex items-center space-x-1"
                                            title="Delete profile">
                                            <svg class="size-3.5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                                                <path stroke-linecap="round" stroke-linejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                                            </svg>
                                            <span>Delete Profile</span>
                                        </button>
                                    </div>

                                    <!-- Sub-Navigation Tabs Bar -->
                                    <div v-if="selectedId !== 'default'" class="flex border-b border-gray-200 dark:border-gray-700">
                                        <button type="button" @click="activeTab = 'settings'"
                                            :class="['px-4 py-2 text-sm font-medium border-b-2 cursor-pointer transition-colors', activeTab === 'settings' ? 'border-blue-600 text-blue-600 dark:text-blue-400' : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400']">
                                            Settings
                                        </button>
                                        <button type="button" @click="activeTab = 'skills'"
                                            :class="['px-4 py-2 text-sm font-medium border-b-2 cursor-pointer transition-colors', activeTab === 'skills' ? 'border-blue-600 text-blue-600 dark:text-blue-400' : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400']">
                                            Skills ({{ getActiveSkillsSummary() }})
                                        </button>
                                        <button type="button" @click="activeTab = 'tools'"
                                            :class="['px-4 py-2 text-sm font-medium border-b-2 cursor-pointer transition-colors', activeTab === 'tools' ? 'border-blue-600 text-blue-600 dark:text-blue-400' : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400']">
                                            Tools ({{ getActiveToolsSummary() }})
                                        </button>
                                        <button type="button" @click="activeTab = 'files'"
                                            :class="['px-4 py-2 text-sm font-medium border-b-2 cursor-pointer transition-colors', activeTab === 'files' ? 'border-blue-600 text-blue-600 dark:text-blue-400' : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400']">
                                            Files ({{ profileFiles.length }})
                                        </button>
                                    </div>

                                    <!-- 1. SETTINGS TAB -->
                                    <div v-if="activeTab === 'settings' || selectedId === 'default'" class="space-y-5">
                                        <!-- Selected Model Override -->
                                        <div>
                                            <label class="block text-sm font-medium mb-1.5" :class="[$styles.labelInput]">
                                                Selected Model
                                            </label>
                                            <div class="flex items-center space-x-2">
                                                <button type="button" @click="isModelPickerOpen = true"
                                                    class="flex flex-1 items-center justify-between rounded-lg px-3.5 py-2 border shadow-sm transition-colors text-sm cursor-pointer"
                                                    :class="[$styles.dropdownButton, $styles.chromeBorder]">
                                                    <span class="flex items-center space-x-2 truncate">
                                                        <ProviderIcon v-if="selectedModelObj?.provider" :provider="selectedModelObj.provider" class="size-4 shrink-0" />
                                                        <span class="font-medium truncate">
                                                            {{ editForm.model ? editForm.model : ('Server Default (' + serverDefaultModelDisplay + ')') }}
                                                        </span>
                                                    </span>
                                                    <svg class="size-4 opacity-70 shrink-0" :class="$styles.icon" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                                                        <path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd" />
                                                    </svg>
                                                </button>
                                                <button v-if="editForm.model" type="button" @click="editForm.model = ''"
                                                    class="px-2.5 py-2 text-xs font-medium text-gray-600 hover:text-red-600 dark:text-gray-400 dark:hover:text-red-400 border border-gray-300 dark:border-gray-600 rounded-md transition-colors cursor-pointer shrink-0"
                                                    title="Clear model override">
                                                    Clear Override
                                                </button>
                                            </div>
                                            <span class="text-[11px] mt-1 block" :class="$styles.muted">
                                                <template v-if="editForm.model">
                                                    Selected model: <strong>{{ editForm.model }}</strong>
                                                </template>
                                                <template v-else>
                                                    Using Server Default: <strong>{{ serverDefaultModelDisplay }}</strong>
                                                </template>
                                            </span>
                                        </div>

                                        <!-- Theme Override -->
                                        <div>
                                            <label class="block text-sm font-medium mb-1.5" :class="[$styles.labelInput]">
                                                Theme
                                            </label>
                                            <div class="relative text-left select-none" ref="themeMenuContainer">
                                                <div class="flex items-center space-x-2">
                                                    <button type="button" @click.stop="isThemeMenuOpen = !isThemeMenuOpen"
                                                        class="flex flex-1 items-center justify-between rounded-lg px-3.5 py-2 border shadow-sm transition-colors text-sm cursor-pointer"
                                                        :class="[$styles.dropdownButton, $styles.chromeBorder]">
                                                        <span class="flex items-center space-x-2">
                                                            <svg class="size-4" :class="$styles.icon" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="M17.5 12a1.5 1.5 0 0 1-1.5-1.5A1.5 1.5 0 0 1 17.5 9a1.5 1.5 0 0 1 1.5 1.5a1.5 1.5 0 0 1-1.5 1.5m-3-4A1.5 1.5 0 0 1 13 6.5A1.5 1.5 0 0 1 14.5 5A1.5 1.5 0 0 1 16 6.5A1.5 1.5 0 0 1 14.5 8m-5 0A1.5 1.5 0 0 1 8 6.5A1.5 1.5 0 0 1 9.5 5A1.5 1.5 0 0 1 11 6.5A1.5 1.5 0 0 1 9.5 8m-3 4A1.5 1.5 0 0 1 5 10.5A1.5 1.5 0 0 1 6.5 9A1.5 1.5 0 0 1 8 10.5A1.5 1.5 0 0 1 6.5 12M12 3a9 9 0 0 0-9 9a9 9 0 0 0 9 9a1.5 1.5 0 0 0 1.5-1.5c0-.39-.15-.74-.39-1c-.23-.27-.38-.62-.38-1a1.5 1.5 0 0 1 1.5-1.5H16a5 5 0 0 0 5-5c0-4.42-4.03-8-9-8"/></svg>
                                                            <span class="font-medium">
                                                                {{ editForm.theme ? ($utils.idToName(editForm.theme) || editForm.theme) : ('Server Default (' + serverDefaultThemeDisplay + ')') }}
                                                            </span>
                                                        </span>
                                                        <svg class="size-4 opacity-70" :class="$styles.icon" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                                                            <path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd" />
                                                        </svg>
                                                    </button>
                                                    <button v-if="editForm.theme" type="button" @click="editForm.theme = ''"
                                                        class="px-2.5 py-2 text-xs font-medium text-gray-600 hover:text-red-600 dark:text-gray-400 dark:hover:text-red-400 border border-gray-300 dark:border-gray-600 rounded-md transition-colors cursor-pointer shrink-0"
                                                        title="Clear theme override">
                                                        Clear Override
                                                    </button>
                                                </div>

                                                <!-- Theme Popover Dropdown -->
                                                <div v-if="isThemeMenuOpen"
                                                    @click.stop
                                                    class="absolute left-0 z-50 mt-2 w-[32rem] max-w-[90vw] origin-top-left rounded-lg focus:outline-none shadow-2xl">
                                                    <div class="max-h-80 overflow-y-auto w-full p-4 bg-white/95 dark:bg-gray-800/95 backdrop-blur-md rounded-xl border border-gray-200 dark:border-gray-700 space-y-4">
                                                        <div class="grid grid-cols-2 gap-4 w-full">
                                                            <!-- Light Themes Column -->
                                                            <div class="flex flex-col space-y-2.5">
                                                                <div class="text-[11px] font-bold tracking-wider uppercase px-1" :class="$styles.muted">Light Themes</div>
                                                                <div v-for="(theme, id) in lightThemes" :key="id"
                                                                    @click="selectThemeOverride(id)"
                                                                    class="cursor-pointer">
                                                                    <ThemeButton :id="id" :theme="theme" />
                                                                </div>
                                                            </div>

                                                            <!-- Dark Themes Column -->
                                                            <div class="flex flex-col space-y-2.5">
                                                                <div class="text-[11px] font-bold tracking-wider uppercase px-1" :class="$styles.muted">Dark Themes</div>
                                                                <div v-for="(theme, id) in darkThemes" :key="id"
                                                                    @click="selectThemeOverride(id)"
                                                                    class="cursor-pointer">
                                                                    <ThemeButton :id="id" :theme="theme" />
                                                                </div>
                                                            </div>
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>
                                            <span class="text-[11px] mt-1 block" :class="$styles.muted">
                                                <template v-if="editForm.theme">
                                                    Selected theme: <strong>{{ $utils.idToName(editForm.theme) || editForm.theme }}</strong>
                                                </template>
                                                <template v-else>
                                                    Using Server Default: <strong>{{ serverDefaultThemeDisplay }}</strong>
                                                </template>
                                            </span>
                                        </div>

                                        <!-- Server Defaults Summary Box -->
                                        <div v-if="selectedProfile.isBuiltIn || selectedId === 'default'" class="p-3.5 rounded-lg border text-xs space-y-1.5" :class="[$styles.panel, $styles.chromeBorder]">
                                            <div class="font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wider text-[10px]">
                                                Server Configuration Defaults
                                            </div>
                                            <div class="flex items-center justify-between">
                                                <span :class="$styles.muted">Default Model:</span>
                                                <span class="font-mono font-medium">{{ selectedProfile.serverModel || 'Not configured' }}</span>
                                            </div>
                                            <div class="flex items-center justify-between">
                                                <span :class="$styles.muted">Default Theme:</span>
                                                <span class="font-mono font-medium">{{ selectedProfile.serverTheme || 'Not configured' }}</span>
                                            </div>
                                        </div>
                                    </div>

                                    <!-- 2. SKILLS TAB -->
                                    <div v-if="activeTab === 'skills' && selectedId !== 'default'" class="space-y-4">
                                        <div class="flex items-center justify-between">
                                            <div>
                                                <h4 class="text-sm font-semibold">Allowed Skills</h4>
                                                <p class="text-xs" :class="$styles.muted">Select which skills this profile can invoke</p>
                                            </div>
                                            <div v-if="!selectedProfile.isBuiltIn" class="flex items-center space-x-2">
                                                <button type="button" @click="editForm.skillsMode = 'all'"
                                                    :class="['px-3 py-1 rounded-md text-xs font-medium border transition-colors cursor-pointer', editForm.skillsMode === 'all' ? 'bg-green-100 dark:bg-green-900/40 text-green-800 dark:text-green-300 border-green-300 dark:border-green-800 font-semibold' : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-400 border-gray-200 dark:border-gray-700']">
                                                    All Skills
                                                </button>
                                                <button type="button" @click="editForm.skillsMode = 'none'"
                                                    :class="['px-3 py-1 rounded-md text-xs font-medium border transition-colors cursor-pointer', editForm.skillsMode === 'none' ? 'bg-fuchsia-100 dark:bg-fuchsia-900/40 text-fuchsia-800 dark:text-fuchsia-300 border-fuchsia-200 dark:border-fuchsia-800 font-semibold' : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-400 border-gray-200 dark:border-gray-700']">
                                                    No Skills
                                                </button>
                                                <button type="button" @click="editForm.skillsMode = 'custom'"
                                                    :class="['px-3 py-1 rounded-md text-xs font-medium border transition-colors cursor-pointer', editForm.skillsMode === 'custom' ? 'bg-blue-100 dark:bg-blue-900/40 text-blue-800 dark:text-blue-300 border-blue-300 dark:border-blue-800 font-semibold' : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-400 border-gray-200 dark:border-gray-700']">
                                                    Custom
                                                </button>
                                            </div>
                                        </div>

                                        <div v-if="skillGroups.length === 0" class="text-center py-12 text-gray-400 text-xs italic">
                                            No skills installed or available
                                        </div>
                                        <div v-else class="space-y-3">
                                            <div v-for="group in skillGroups" :key="group.name"
                                                class="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700 overflow-hidden">
                                                <!-- Group Header -->
                                                <div class="flex items-center justify-between px-3.5 py-2 bg-gray-50/70 dark:bg-gray-800/50">
                                                    <div class="flex items-center space-x-2 min-w-0">
                                                        <span class="font-semibold text-xs text-gray-800 dark:text-gray-200 truncate">
                                                            {{ group.name || 'General Skills' }}
                                                        </span>
                                                        <span class="text-[11px] text-gray-400 font-mono">
                                                            ({{ getGroupSkillsActiveCount(group) }}/{{ group.skills.length }})
                                                        </span>
                                                    </div>
                                                    <div v-if="!selectedProfile.isBuiltIn" class="flex items-center space-x-1.5">
                                                        <button @click="setGroupSkills(group, true)" type="button"
                                                            class="px-2 py-0.5 rounded text-[10px] font-medium border transition-colors cursor-pointer"
                                                            :class="getGroupSkillsActiveCount(group) === group.skills.length
                                                                ? 'bg-green-50 dark:bg-green-900/30 text-green-700 dark:text-green-300 border-green-300 dark:border-green-800'
                                                                : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-400 border-gray-200 dark:border-gray-700 hover:border-gray-300'">
                                                            all
                                                        </button>
                                                        <button @click="setGroupSkills(group, false)" type="button"
                                                            class="px-2 py-0.5 rounded text-[10px] font-medium border transition-colors cursor-pointer"
                                                            :class="getGroupSkillsActiveCount(group) === 0
                                                                ? 'bg-fuchsia-50 dark:bg-fuchsia-900/30 text-fuchsia-700 dark:text-fuchsia-300 border-fuchsia-200 dark:border-fuchsia-800'
                                                                : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-400 border-gray-200 dark:border-gray-700 hover:border-gray-300'">
                                                            none
                                                        </button>
                                                    </div>
                                                </div>

                                                <!-- Group Body -->
                                                <div class="p-3 border-t border-gray-100 dark:border-gray-800 flex flex-wrap gap-2">
                                                    <button v-for="sk in group.skills" :key="sk" type="button"
                                                        :disabled="selectedProfile.isBuiltIn"
                                                        @click="toggleCustomSkill(sk)"
                                                        :class="[
                                                            'px-2.5 py-1 rounded-full text-xs font-medium border transition-colors cursor-pointer select-none flex items-center space-x-1.5',
                                                            isCustomSkillSelected(sk)
                                                                ? 'bg-blue-100 dark:bg-blue-900/40 text-blue-800 dark:text-blue-300 border-blue-300 dark:border-blue-700'
                                                                : 'bg-gray-50 dark:bg-gray-800 text-gray-500 dark:text-gray-400 border-gray-200 dark:border-gray-700 opacity-60 hover:opacity-100'
                                                        ]">
                                                        <svg v-if="isCustomSkillSelected(sk)" class="size-3 text-blue-600 dark:text-blue-400 shrink-0" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                                                            <path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 011.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clip-rule="evenodd" />
                                                        </svg>
                                                        <span>{{ sk }}</span>
                                                    </button>
                                                </div>
                                            </div>
                                        </div>
                                    </div>

                                    <!-- 3. TOOLS TAB -->
                                    <div v-if="activeTab === 'tools' && selectedId !== 'default'" class="space-y-4">
                                        <div class="flex items-center justify-between">
                                            <div>
                                                <h4 class="text-sm font-semibold">Allowed Tools</h4>
                                                <p class="text-xs" :class="$styles.muted">Select which system and extension tools this profile can execute</p>
                                            </div>
                                            <div v-if="!selectedProfile.isBuiltIn" class="flex items-center space-x-2">
                                                <button type="button" @click="editForm.toolsMode = 'all'"
                                                    :class="['px-3 py-1 rounded-md text-xs font-medium border transition-colors cursor-pointer', editForm.toolsMode === 'all' ? 'bg-green-100 dark:bg-green-900/40 text-green-800 dark:text-green-300 border-green-300 dark:border-green-800 font-semibold' : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-400 border-gray-200 dark:border-gray-700']">
                                                    All Tools
                                                </button>
                                                <button type="button" @click="editForm.toolsMode = 'none'"
                                                    :class="['px-3 py-1 rounded-md text-xs font-medium border transition-colors cursor-pointer', editForm.toolsMode === 'none' ? 'bg-fuchsia-100 dark:bg-fuchsia-900/40 text-fuchsia-800 dark:text-fuchsia-300 border-fuchsia-200 dark:border-fuchsia-800 font-semibold' : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-400 border-gray-200 dark:border-gray-700']">
                                                    No Tools
                                                </button>
                                                <button type="button" @click="editForm.toolsMode = 'custom'"
                                                    :class="['px-3 py-1 rounded-md text-xs font-medium border transition-colors cursor-pointer', editForm.toolsMode === 'custom' ? 'bg-blue-100 dark:bg-blue-900/40 text-blue-800 dark:text-blue-300 border-blue-300 dark:border-blue-800 font-semibold' : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-400 border-gray-200 dark:border-gray-700']">
                                                    Custom
                                                </button>
                                            </div>
                                        </div>

                                        <div v-if="toolGroups.length === 0" class="text-center py-12 text-gray-400 text-xs italic">
                                            No tools registered or available
                                        </div>
                                        <div v-else class="space-y-3">
                                            <div v-for="group in toolGroups" :key="group.name"
                                                class="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700 overflow-hidden">
                                                <!-- Group Header -->
                                                <div class="flex items-center justify-between px-3.5 py-2 bg-gray-50/70 dark:bg-gray-800/50">
                                                    <div class="flex items-center space-x-2 min-w-0">
                                                        <span class="font-semibold text-xs text-gray-800 dark:text-gray-200 truncate">
                                                            {{ group.name || 'Other Tools' }}
                                                        </span>
                                                        <span class="text-[11px] text-gray-400 font-mono">
                                                            ({{ getGroupToolsActiveCount(group) }}/{{ group.tools.length }})
                                                        </span>
                                                    </div>
                                                    <div v-if="!selectedProfile.isBuiltIn" class="flex items-center space-x-1.5">
                                                        <button @click="setGroupTools(group, true)" type="button"
                                                            class="px-2 py-0.5 rounded text-[10px] font-medium border transition-colors cursor-pointer"
                                                            :class="getGroupToolsActiveCount(group) === group.tools.length
                                                                ? 'bg-green-50 dark:bg-green-900/30 text-green-700 dark:text-green-300 border-green-300 dark:border-green-800'
                                                                : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-400 border-gray-200 dark:border-gray-700 hover:border-gray-300'">
                                                            all
                                                        </button>
                                                        <button @click="setGroupTools(group, false)" type="button"
                                                            class="px-2 py-0.5 rounded text-[10px] font-medium border transition-colors cursor-pointer"
                                                            :class="getGroupToolsActiveCount(group) === 0
                                                                ? 'bg-fuchsia-50 dark:bg-fuchsia-900/30 text-fuchsia-700 dark:text-fuchsia-300 border-fuchsia-200 dark:border-fuchsia-800'
                                                                : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-400 border-gray-200 dark:border-gray-700 hover:border-gray-300'">
                                                            none
                                                        </button>
                                                    </div>
                                                </div>

                                                <!-- Group Body -->
                                                <div class="p-3 border-t border-gray-100 dark:border-gray-800 flex flex-wrap gap-2">
                                                    <button v-for="tl in group.tools" :key="tl" type="button"
                                                        :disabled="selectedProfile.isBuiltIn"
                                                        @click="toggleCustomTool(tl)"
                                                        :class="[
                                                            'px-2.5 py-1 rounded-full text-xs font-mono font-medium border transition-colors cursor-pointer select-none flex items-center space-x-1.5',
                                                            isCustomToolSelected(tl)
                                                                ? 'bg-blue-100 dark:bg-blue-900/40 text-blue-800 dark:text-blue-300 border-blue-300 dark:border-blue-700'
                                                                : 'bg-gray-50 dark:bg-gray-800 text-gray-500 dark:text-gray-400 border-gray-200 dark:border-gray-700 opacity-60 hover:opacity-100'
                                                        ]">
                                                        <svg v-if="isCustomToolSelected(tl)" class="size-3 text-blue-600 dark:text-blue-400 shrink-0" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                                                            <path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 011.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clip-rule="evenodd" />
                                                        </svg>
                                                        <span>{{ tl }}</span>
                                                    </button>
                                                </div>
                                            </div>
                                        </div>
                                    </div>

                                    <!-- 4. FILES TAB -->
                                    <div v-if="activeTab === 'files' && selectedId !== 'default'" class="flex-1 flex flex-col space-y-3 min-h-[350px]">
                                        <div class="flex items-center justify-between gap-2 border-b border-gray-200 dark:border-gray-700 pb-2">
                                            <div class="flex items-center space-x-1 overflow-x-auto flex-1">
                                                <button v-for="f in profileFiles" :key="f" type="button"
                                                    @click="selectFile(f)"
                                                    :class="[
                                                        'px-2 py-0.5 rounded text-[11px] font-mono transition-all cursor-pointer whitespace-nowrap border pt-1',
                                                        selectedFilename === f
                                                            ? 'bg-blue-50 dark:bg-blue-900/40 text-blue-600 dark:text-blue-300 border-blue-300 dark:border-blue-700 font-semibold shadow-sm'
                                                            : 'bg-gray-50 dark:bg-gray-800/60 text-gray-600 dark:text-gray-400 border-gray-200 dark:border-gray-700 hover:bg-gray-100 dark:hover:bg-gray-700'
                                                    ]">
                                                    {{ f }}
                                                </button>
                                            </div>
                                            <button v-if="!selectedProfile.isBuiltIn" type="button" @click="isNewFileDialogOpen = true"
                                                class="p-1.5 rounded-lg text-xs font-medium bg-gray-100 hover:bg-gray-200 dark:bg-gray-700 dark:hover:bg-gray-600 text-gray-700 dark:text-gray-200 transition-colors cursor-pointer shrink-0"
                                                title="Create new file">
                                                <svg class="size-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                                                    <path stroke-linecap="round" stroke-linejoin="round" d="M12 4v16m8-8H4" />
                                                </svg>
                                            </button>
                                        </div>

                                        <div v-if="selectedFilename" class="flex-1 flex flex-col space-y-2 min-h-0">
                                            <div class="flex items-center justify-between text-xs">
                                                <span class="font-mono text-gray-600 dark:text-gray-400">{{ selectedFilename }}</span>
                                                <div class="flex items-center space-x-2">
                                                    <span v-if="selectedProfile.isBuiltIn" class="text-amber-600 dark:text-amber-400 text-[11px] font-medium">Read-Only File</span>
                                                    <button v-else-if="selectedFilename.endsWith('.md') && selectedFilename !== 'SYSTEM.md'" type="button" @click="deleteSelectedFile(selectedFilename)"
                                                        class="text-red-600 hover:text-red-700 text-xs cursor-pointer">
                                                        Delete
                                                    </button>
                                                    <button v-if="!selectedProfile.isBuiltIn" type="button" @click="saveSelectedFile" :disabled="isFileSaving"
                                                        class="px-3 py-1 rounded bg-blue-600 text-white text-xs font-medium hover:bg-blue-700 disabled:opacity-50 cursor-pointer">
                                                        {{ isFileSaving ? 'Saving...' : 'Save File' }}
                                                    </button>
                                                </div>
                                            </div>
                                            <textarea v-model="fileContent" :readonly="selectedProfile.isBuiltIn"
                                                @keydown.ctrl.s.prevent="saveSelectedFile"
                                                @keydown.meta.s.prevent="saveSelectedFile"
                                                placeholder="File content..."
                                                class="flex-1 w-full p-3 font-mono text-xs rounded-lg border border-gray-300 dark:border-gray-600 bg-gray-900 text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none min-h-[300px]" />
                                        </div>
                                    </div>
                                </div>

                                <!-- Form Actions -->
                                <div class="mt-6 pt-4 border-t border-gray-200 dark:border-gray-700 flex items-center justify-between">
                                    <button type="button" @click="resetToDefaults"
                                        :disabled="!hasOverride(selectedId) && !editForm.model && !editForm.theme"
                                        class="px-3.5 py-2 text-sm font-medium rounded-lg transition-colors flex items-center space-x-1.5 disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer"
                                        :class="[$styles.secondaryButton]">
                                        <svg class="size-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                                            <path stroke-linecap="round" stroke-linejoin="round" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                                        </svg>
                                        <span>Reset Preferences</span>
                                    </button>

                                    <div class="flex items-center space-x-3">
                                        <button type="button" @click="closeDialog"
                                            class="px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg transition-colors cursor-pointer">
                                            Close
                                        </button>
                                        <button type="button" @click="saveForm"
                                            class="px-4 py-2 text-sm font-semibold rounded-lg transition-colors cursor-pointer"
                                            :class="[$styles.primaryButton]">
                                            Save Profile
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- New Profile Modal Dialog -->
            <div v-if="isNewProfileDialogOpen" class="fixed inset-0 z-[70] bg-black/60 flex items-center justify-center p-4" @keydown.escape.stop="isNewProfileDialogOpen = false">
                <div class="bg-white dark:bg-gray-800 rounded-xl p-5 w-full max-w-sm space-y-4 shadow-2xl border border-gray-200 dark:border-gray-700">
                    <h4 class="text-base font-semibold">Create New Profile</h4>
                    <div>
                        <label class="block text-xs font-medium mb-1" :class="$styles.muted">Profile Name</label>
                        <input type="text" ref="newProfileInputRef" v-model="newProfileName" placeholder="e.g. Code Reviewer"
                            @keydown.enter="createNewProfile"
                            class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500" />
                    </div>
                    <div class="flex justify-end space-x-2 text-xs">
                        <button type="button" @click="isNewProfileDialogOpen = false" class="px-3.5 py-1.5 rounded-lg border border-gray-300 dark:border-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700 cursor-pointer">Cancel</button>
                        <button type="button" @click="createNewProfile" class="px-3.5 py-1.5 rounded-lg bg-blue-600 text-white font-medium hover:bg-blue-700 cursor-pointer">Create Profile</button>
                    </div>
                </div>
            </div>

            <!-- New File Modal Dialog -->
            <div v-if="isNewFileDialogOpen" class="fixed inset-0 z-[70] bg-black/60 flex items-center justify-center p-4" @keydown.escape.stop="isNewFileDialogOpen = false">
                <div class="bg-white dark:bg-gray-800 rounded-xl p-5 w-full max-w-md space-y-4 shadow-2xl border border-gray-200 dark:border-gray-700">
                    <h4 class="text-base font-semibold">Create New File</h4>
                    <input type="text" ref="newFileInputRef" v-model="newFileName" placeholder="filename.md"
                        @keydown.enter="createNewFile"
                        class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 font-mono" />
                    <div class="flex justify-end space-x-2 text-xs">
                        <button type="button" @click="isNewFileDialogOpen = false" class="px-3.5 py-1.5 rounded-lg border border-gray-300 dark:border-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700 cursor-pointer">Cancel</button>
                        <button type="button" @click="createNewFile" class="px-3.5 py-1.5 rounded-lg bg-blue-600 text-white font-medium hover:bg-blue-700 cursor-pointer">Create</button>
                    </div>
                </div>
            </div>

            <!-- Inner Model Selection Sub-Dialog -->
            <div v-if="isModelPickerOpen" class="fixed inset-0 z-[60] overflow-hidden text-gray-900 dark:text-gray-100" @keydown.escape.stop="isModelPickerOpen = false">
                <div class="fixed inset-0 bg-black/60 transition-opacity" @click="isModelPickerOpen = false"></div>
                <div class="fixed inset-4 md:inset-10 lg:inset-16 flex items-center justify-center">
                    <div class="relative bg-white dark:bg-gray-800 rounded-xl shadow-2xl w-full h-full max-w-5xl max-h-[85vh] flex flex-col overflow-hidden border border-gray-200 dark:border-gray-700">
                        <div class="flex-shrink-0 px-6 py-4 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
                            <div>
                                <h3 class="text-lg font-semibold">Select Model Override</h3>
                                <p class="text-xs" :class="$styles.muted">Select a model for profile <strong>{{ selectedProfile?.name }}</strong></p>
                            </div>
                            <button type="button" @click="isModelPickerOpen = false" class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 transition-colors">
                                <svg class="size-6" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                                    <path fill="currentColor" d="M19 6.41L17.59 5L12 10.59L6.41 5L5 6.41L10.59 12L5 17.59L6.41 19L12 13.41L17.59 19L19 17.59L13.41 12z"/>
                                </svg>
                            </button>
                        </div>
                        <div class="p-4 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50 space-y-3">
                            <div class="flex flex-col sm:flex-row gap-3">
                                <div class="relative flex-1">
                                    <svg class="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-gray-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                                        <path fill-rule="evenodd" d="M9 3.5a5.5 5.5 0 100 11 5.5 5.5 0 000-11zM2 9a7 7 0 1112.452 4.391l3.328 3.329a.75.75 0 11-1.06 1.06l-3.329-3.328A7 7 0 012 9z" clip-rule="evenodd" />
                                    </svg>
                                    <input type="text" v-model="modelSearchQuery" placeholder="Search models by name, ID, or provider..."
                                        class="w-full pl-10 pr-8 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm" />
                                    <button v-if="modelSearchQuery" type="button" @click="modelSearchQuery = ''"
                                        class="absolute right-2.5 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 transition-colors p-0.5 rounded-full cursor-pointer"
                                        title="Clear search">
                                        <svg class="size-4" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                            <line x1="18" y1="6" x2="6" y2="18"></line>
                                            <line x1="6" y1="6" x2="18" y2="18"></line>
                                        </svg>
                                    </button>
                                </div>
                                <div class="flex items-center space-x-2">
                                    <label class="text-xs text-gray-500 dark:text-gray-400 whitespace-nowrap">Sort by:</label>
                                    <select v-model="modelSortBy"
                                        class="px-3 py-2 pr-8 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 cursor-pointer">
                                        <option v-for="opt in modelSortOptions" :key="opt.id" :value="opt.id">{{ opt.label }}</option>
                                    </select>
                                    <button type="button" @click="modelSortAsc = !modelSortAsc"
                                        class="p-2 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors cursor-pointer"
                                        :title="modelSortAsc ? 'Ascending' : 'Descending'">
                                        <svg v-if="modelSortAsc" class="size-5 text-gray-600 dark:text-gray-400" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24">
                                            <path fill="currentColor" d="M19 7h3l-4-4l-4 4h3v14h2M2 17h10v2H2M6 5v2H2V5m0 6h7v2H2z"/>
                                        </svg>
                                        <svg v-else class="size-5 text-gray-600 dark:text-gray-400" style="transform: scaleY(-1)" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24">
                                            <path fill="currentColor" d="M19 7h3l-4-4l-4 4h3v14h2M2 17h10v2H2M6 5v2H2V5m0 6h7v2H2z"/>
                                        </svg>
                                    </button>
                                </div>
                            </div>
                            <div class="flex flex-wrap gap-2">
                                <button type="button" @click="selectedProviderFilter = ''"
                                    :class="[
                                        'px-3 py-1 rounded-lg text-xs font-medium transition-colors cursor-pointer',
                                        !selectedProviderFilter
                                            ? 'bg-blue-600 text-white'
                                            : 'bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-300 dark:hover:bg-gray-600'
                                    ]">
                                    All
                                </button>
                                <button v-for="prov in uniqueProviders" :key="prov" type="button"
                                    @click="selectedProviderFilter = prov === selectedProviderFilter ? '' : prov"
                                    :class="[
                                        'flex items-center space-x-1.5 px-3 py-1 rounded-lg text-xs font-medium transition-colors cursor-pointer',
                                        selectedProviderFilter === prov
                                            ? 'bg-blue-600 text-white'
                                            : 'bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-300 dark:hover:bg-gray-600'
                                    ]">
                                    <ProviderIcon :provider="prov" class="size-3.5" />
                                    <span>{{ prov }}</span>
                                </button>
                            </div>
                        </div>
                        <div class="flex-1 overflow-y-auto p-4">
                            <div v-if="filteredModelList.length === 0" class="text-center py-12 text-gray-500 dark:text-gray-400 text-sm">
                                No models found matching your search.
                            </div>
                            <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
                                <button v-for="m in filteredModelList" :key="m.id + '-' + m.provider"
                                    type="button"
                                    @click="selectModelOverride(m.name || m.id)"
                                    :class="[
                                        'text-left p-3.5 rounded-lg border transition-all cursor-pointer group hover:scale-[1.01]',
                                        editForm.model === (m.name || m.id)
                                            ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/30 ring-2 ring-blue-500/50'
                                            : 'border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-700/50'
                                    ]">
                                    <div class="flex items-start justify-between mb-1.5">
                                        <div class="flex items-center space-x-2 min-w-0">
                                            <ProviderIcon :provider="m.provider" class="size-4 shrink-0" />
                                            <span class="font-medium text-sm text-gray-900 dark:text-gray-100 truncate">{{ m.name }}</span>
                                        </div>
                                    </div>
                                    <div class="text-[11px] text-gray-500 dark:text-gray-400 font-mono truncate mb-2">{{ m.id }}</div>
                                    <div class="flex flex-wrap gap-1 text-[10px] text-gray-600 dark:text-gray-400">
                                        <span v-if="m.limit?.context" class="px-1.5 py-0.5 rounded bg-gray-100 dark:bg-gray-700 font-mono" :title="(m.limit.context ? m.limit.context.toLocaleString() : '') + ' token context limit'">
                                            {{ formatShortNumber(m.limit.context) }}
                                        </span>
                                        <span v-if="m.release_date" class="px-1.5 py-0.5 rounded bg-gray-100 dark:bg-gray-700 font-mono">
                                            {{ m.release_date }}
                                        </span>
                                        <span v-if="isFreeModel(m)" class="px-1.5 py-0.5 rounded bg-green-100 dark:bg-green-900/50 text-green-700 dark:text-green-300 font-medium">
                                            Free
                                        </span>
                                        <span v-else-if="m.cost && (m.cost.input != null || m.cost.output != null)" class="px-1.5 py-0.5 rounded bg-gray-100 dark:bg-gray-700 font-mono" :title="'Input: $' + formatCostNum(m.cost.input) + ' / Output: $' + formatCostNum(m.cost.output) + ' per 1M tokens'">
                                            {{ formatCostNum(m.cost.input) }}/{{ formatCostNum(m.cost.output) }}
                                        </span>
                                        <span v-if="m.reasoning" class="px-1.5 py-0.5 rounded bg-purple-100 dark:bg-purple-900/50 text-purple-700 dark:text-purple-300">reasoning</span>
                                        <span v-if="m.tool_call" class="px-1.5 py-0.5 rounded bg-blue-100 dark:bg-blue-900/50 text-blue-700 dark:text-blue-300">tools</span>
                                    </div>
                                </button>
                            </div>
                        </div>
                        <div class="px-6 py-3 border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50 flex justify-between items-center text-xs">
                            <span :class="$styles.muted">{{ filteredModelList.length }} models</span>
                            <button type="button" @click="isModelPickerOpen = false"
                                class="px-4 py-1.5 font-medium rounded-md hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors cursor-pointer">
                                Cancel
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `,
    emits: ['done'],
    setup(props, { emit }) {
        const ctx = inject('ctx')
        const activeTab = ref('settings')
        const selectedId = ref('default')
        const isThemeMenuOpen = ref(false)
        const themeMenuContainer = ref(null)

        const isModelPickerOpen = ref(false)
        const modelSearchQuery = ref('')
        const selectedProviderFilter = ref('')
        const modelSortBy = ref('release_date')
        const modelSortAsc = ref(false)
        const modelSortOptions = [
            { id: 'release_date', label: 'Release Date' },
            { id: 'name', label: 'Name' },
            { id: 'knowledge', label: 'Knowledge Cutoff' },
            { id: 'last_updated', label: 'Last Updated' },
            { id: 'cost_input', label: 'Cost (Input)' },
            { id: 'cost_output', label: 'Cost (Output)' },
            { id: 'context', label: 'Context Limit' },
        ]

        const availableTools = ref([])
        const availableSkills = ref([])

        const profileFiles = ref([])
        const selectedFilename = ref('')
        const fileContent = ref('')
        const isFileLoading = ref(false)
        const isFileSaving = ref(false)
        const isNewFileDialogOpen = ref(false)
        const newFileName = ref('')
        const newFileInputRef = ref(null)

        const isNewProfileDialogOpen = ref(false)
        const newProfileName = ref('')
        const newProfileInputRef = ref(null)

        watch(isNewProfileDialogOpen, (open) => {
            if (open) {
                nextTick(() => {
                    newProfileInputRef.value?.focus()
                })
            }
        })

        watch(isNewFileDialogOpen, (open) => {
            if (open) {
                nextTick(() => {
                    newFileInputRef.value?.focus()
                })
            }
        })

        const avatarInputRef = ref(null)

        const editForm = ref({
            name: '',
            model: '',
            theme: '',
            skillsMode: 'all',
            customSkills: [],
            toolsMode: 'all',
            customTools: [],
        })

        const defaultProfileItem = computed(() => ({
            id: 'default',
            name: 'Default Workspace',
            avatar: ctx.getDefaultAgentAvatar(),
            serverModel: ctx.state.config?.defaultModel || null,
            serverTheme: null,
            isBuiltIn: true,
            files: [],
        }))

        const agentProfiles = computed(() => {
            return (ctx.agents?.all || []).map(a => ({
                id: a.id,
                name: a.name,
                avatar: a.avatar,
                serverModel: a.model || null,
                serverTheme: a.theme || null,
                isBuiltIn: !!a.isBuiltIn,
                files: a.files || [],
                onlyTools: a.onlyTools,
                onlySkills: a.onlySkills,
            }))
        })

        const allAvailableTools = computed(() => {
            if (availableTools.value && availableTools.value.length > 0) {
                return availableTools.value
            }
            const defs = ctx.state?.tool?.definitions || []
            return defs.map(d => d.function?.name || d.name).filter(Boolean)
        })

        const allAvailableSkills = computed(() => {
            if (availableSkills.value && availableSkills.value.length > 0) {
                return availableSkills.value
            }
            const skills = ctx.state?.skills || []
            return skills.map(s => s.name || s).filter(Boolean)
        })

        const toolGroups = computed(() => {
            const groups = ctx.state?.tool?.groups || {}
            const defs = ctx.state?.tool?.definitions || []
            const definedGroups = []
            const usedTools = new Set()

            for (const [groupName, toolNames] of Object.entries(groups)) {
                if (!Array.isArray(toolNames)) continue
                const tools = toolNames.map(name => {
                    const match = allAvailableTools.value.find(t => (typeof t === 'string' ? t : t.name) === name)
                    return match ? (typeof match === 'string' ? match : match.name) : null
                }).filter(Boolean)
                if (tools.length) {
                    tools.forEach(t => usedTools.add(t))
                    definedGroups.push({ name: groupName, tools })
                }
            }

            const otherTools = allAvailableTools.value.filter(t => !usedTools.has(typeof t === 'string' ? t : t.name))
            if (otherTools.length) {
                definedGroups.push({ name: 'Other Tools', tools: otherTools.map(t => typeof t === 'string' ? t : t.name) })
            }

            return definedGroups.length ? definedGroups : [{ name: 'Tools', tools: allAvailableTools.value }]
        })

        const skillGroups = computed(() => {
            const rawSkills = Object.values(ctx.state?.skills || {})
            const groupsMap = {}
            const usedSkills = new Set()

            rawSkills.forEach(sk => {
                const name = typeof sk === 'string' ? sk : (sk.name || sk.id)
                const grp = sk.group || ''
                if (name) {
                    usedSkills.add(name)
                    if (!groupsMap[grp]) groupsMap[grp] = []
                    groupsMap[grp].push(name)
                }
            })

            allAvailableSkills.value.forEach(s => {
                const name = typeof s === 'string' ? s : (s.name || s)
                if (name && !usedSkills.has(name)) {
                    if (!groupsMap['']) groupsMap[''] = []
                    groupsMap[''].push(name)
                }
            })

            return Object.entries(groupsMap).map(([name, skills]) => ({
                name: name || 'General Skills',
                skills
            }))
        })

        const availableModels = computed(() => ctx.state.models || [])

        const uniqueProviders = computed(() => {
            if (!availableModels.value) return []
            return [...new Set(availableModels.value.map(m => m.provider))].filter(Boolean).sort()
        })

        const selectedModelObj = computed(() => {
            if (!editForm.value.model || !availableModels.value) return null
            return availableModels.value.find(m => m.name === editForm.value.model || m.id === editForm.value.model) || null
        })

        const filteredModelList = computed(() => {
            if (!availableModels.value) return []
            let res = [...availableModels.value]
            if (selectedProviderFilter.value) {
                res = res.filter(m => m.provider === selectedProviderFilter.value)
            }
            if (modelSearchQuery.value.trim()) {
                const q = modelSearchQuery.value.toLowerCase()
                res = res.filter(m =>
                    m.name?.toLowerCase().includes(q) ||
                    m.id?.toLowerCase().includes(q) ||
                    m.provider?.toLowerCase().includes(q)
                )
            }
            res.sort((a, b) => {
                let cmp = 0
                switch (modelSortBy.value) {
                    case 'release_date':
                        cmp = (a.release_date || '').localeCompare(b.release_date || '')
                        break
                    case 'name':
                        cmp = (a.name || a.id || '').localeCompare(b.name || b.id || '')
                        break
                    case 'knowledge':
                        cmp = (a.knowledge || '').localeCompare(b.knowledge || '')
                        break
                    case 'last_updated':
                        cmp = (a.last_updated || '').localeCompare(b.last_updated || '')
                        break
                    case 'cost_input':
                        cmp = (parseFloat(a.cost?.input) || 0) - (parseFloat(b.cost?.input) || 0)
                        break
                    case 'cost_output':
                        cmp = (parseFloat(a.cost?.output) || 0) - (parseFloat(b.cost?.output) || 0)
                        break
                    case 'context':
                        cmp = (a.limit?.context || 0) - (b.limit?.context || 0)
                        break
                    default:
                        cmp = 0
                }
                if (cmp === 0) {
                    cmp = (a.name || a.id || '').localeCompare(b.name || b.id || '')
                }
                return modelSortAsc.value ? cmp : -cmp
            })
            return res
        })

        function formatCostNum(cost) {
            if (cost == null) return '-'
            const val = parseFloat(cost)
            if (val === 0) return '0'
            if (val < 0.01) return val.toFixed(4)
            return val.toFixed(2)
        }

        function isFreeModel(m) {
            return m.cost && parseFloat(m.cost.input) === 0 && parseFloat(m.cost.output) === 0
        }

        function formatShortNumber(num) {
            if (num == null) return '-'
            if (num >= 1000000) return (num / 1000000).toFixed(1) + 'M'
            if (num >= 1000) return (num / 1000).toFixed(0) + 'K'
            return num
        }

        const fullThemes = computed(() => ctx.resolveThemes(ctx.state.themes) || {})

        const lightThemes = computed(() => {
            const themes = {}
            const sortedEntries = Object.entries(fullThemes.value).sort((a, b) => {
                const idA = a[0], idB = b[0]
                if (idA === 'light') return -1
                if (idB === 'light') return 1
                return (ctx.utils.idToName(idA) || '').localeCompare(ctx.utils.idToName(idB) || '')
            })
            for (const [id, theme] of sortedEntries) {
                if (theme.vars.colorScheme !== 'dark') themes[id] = theme
            }
            return themes
        })

        const darkThemes = computed(() => {
            const themes = {}
            const sortedEntries = Object.entries(fullThemes.value).sort((a, b) => {
                const idA = a[0], idB = b[0]
                if (idA === 'dark') return -1
                if (idB === 'dark') return 1
                return (ctx.utils.idToName(idA) || '').localeCompare(ctx.utils.idToName(idB) || '')
            })
            for (const [id, theme] of sortedEntries) {
                if (theme.vars.colorScheme === 'dark') themes[id] = theme
            }
            return themes
        })

        const selectedProfile = computed(() => {
            if (selectedId.value === 'default') return defaultProfileItem.value
            return agentProfiles.value.find(p => p.id === selectedId.value) || null
        })

        const serverDefaultModelDisplay = computed(() => {
            return selectedProfile.value?.serverModel || 'None'
        })

        const serverDefaultThemeDisplay = computed(() => {
            return selectedProfile.value?.serverTheme || 'None'
        })

        function hasOverride(id) {
            const override = ctx.agents?.getProfileOverride ? ctx.agents.getProfileOverride(id) : null
            return !!(override && (override.model || override.theme))
        }

        function sortProfileFiles(files) {
            if (!Array.isArray(files)) return []
            const sorted = [...files]
            sorted.sort((a, b) => {
                const getRank = (f) => {
                    if (f === 'SYSTEM.template') return 0
                    if (f === 'SYSTEM.md') return 1
                    return 2
                }
                const rankA = getRank(a)
                const rankB = getRank(b)
                if (rankA !== rankB) return rankA - rankB
                return a.localeCompare(b)
            })
            return sorted
        }

        async function selectProfile(profile) {
            selectedId.value = profile.id
            isThemeMenuOpen.value = false
            isModelPickerOpen.value = false
            activeTab.value = 'settings'

            const agent = ctx.agents?.getAgent ? ctx.agents.getAgent(profile.id) : null
            const override = ctx.agents?.getProfileOverride ? ctx.agents.getProfileOverride(profile.id) : null

            let skillsMode = 'all'
            let customSkills = []
            if (agent && agent.onlySkills !== undefined && agent.onlySkills !== null) {
                if (Array.isArray(agent.onlySkills) && agent.onlySkills.length === 0) {
                    skillsMode = 'none'
                } else if (Array.isArray(agent.onlySkills)) {
                    skillsMode = 'custom'
                    customSkills = [...agent.onlySkills]
                }
            }

            let toolsMode = 'all'
            let customTools = []
            if (agent && agent.onlyTools !== undefined && agent.onlyTools !== null) {
                if (Array.isArray(agent.onlyTools) && agent.onlyTools.length === 0) {
                    toolsMode = 'none'
                } else if (Array.isArray(agent.onlyTools)) {
                    toolsMode = 'custom'
                    customTools = [...agent.onlyTools]
                }
            }

            editForm.value = {
                name: agent ? agent.name : profile.name,
                model: agent?.isBuiltIn ? (override?.model || '') : (agent?.model || override?.model || ''),
                theme: agent?.isBuiltIn ? (override?.theme || '') : (agent?.theme || override?.theme || ''),
                skillsMode,
                customSkills,
                toolsMode,
                customTools,
            }

            if (profile.id !== 'default') {
                try {
                    const files = await ctx.agents.getProfileFiles(profile.id)
                    profileFiles.value = sortProfileFiles(files)
                    if (profileFiles.value.length > 0) {
                        selectFile(profileFiles.value[0])
                    } else {
                        selectedFilename.value = ''
                        fileContent.value = ''
                    }
                } catch (e) {
                    profileFiles.value = sortProfileFiles(profile.files)
                }
            } else {
                profileFiles.value = []
                selectedFilename.value = ''
                fileContent.value = ''
            }
        }

        async function selectFile(filename) {
            selectedFilename.value = filename
            fileContent.value = ''
            isFileLoading.value = true
            try {
                fileContent.value = await ctx.agents.getFileContent(selectedId.value, filename)
            } catch (e) {
                ctx.toast(`Failed to load ${filename}: ${e.message}`)
            } finally {
                isFileLoading.value = false
            }
        }

        async function saveSelectedFile() {
            if (!selectedProfile.value || selectedProfile.value.isBuiltIn || !selectedFilename.value) return
            isFileSaving.value = true
            try {
                await ctx.agents.saveFileContent(selectedId.value, selectedFilename.value, fileContent.value)
                ctx.toast(`Saved file ${selectedFilename.value}`)
            } catch (e) {
                ctx.toast(`Failed to save file: ${e.message}`)
            } finally {
                isFileSaving.value = false
            }
        }

        async function createNewFile() {
            if (!newFileName.value || !selectedProfile.value || selectedProfile.value.isBuiltIn) return
            try {
                const res = await ctx.agents.createFile(selectedId.value, newFileName.value, '# New File\n\n')
                const createdFilename = res?.filename || (newFileName.value.startsWith('SYSTEM.template') ? 'SYSTEM.template' : (newFileName.value.endsWith('.md') ? newFileName.value : newFileName.value + '.md'))
                ctx.toast(`Created file ${createdFilename}`)
                isNewFileDialogOpen.value = false
                const files = await ctx.agents.getProfileFiles(selectedId.value)
                profileFiles.value = sortProfileFiles(files)
                selectFile(createdFilename)
                newFileName.value = ''
            } catch (e) {
                ctx.toast(`Failed to create file: ${e.message}`)
            }
        }

        async function deleteSelectedFile(filename) {
            if (!selectedProfile.value || selectedProfile.value.isBuiltIn) return
            if (!confirm(`Are you sure you want to delete ${filename}?`)) return
            try {
                await ctx.agents.deleteFile(selectedId.value, filename)
                ctx.toast(`Deleted ${filename}`)
                const files = await ctx.agents.getProfileFiles(selectedId.value)
                profileFiles.value = sortProfileFiles(files)
                if (profileFiles.value.length > 0) {
                    selectFile(profileFiles.value[0])
                } else {
                    selectedFilename.value = ''
                    fileContent.value = ''
                }
            } catch (e) {
                ctx.toast(`Failed to delete file: ${e.message}`)
            }
        }

        async function createNewProfile() {
            const name = newProfileName.value.trim()
            if (!name) return
            try {
                const created = await ctx.agents.createProfile(name)
                ctx.toast(`Created profile '${name}'`)
                isNewProfileDialogOpen.value = false
                newProfileName.value = ''
                await nextTick()
                const newProfileObj = agentProfiles.value.find(p => p.id === created.id)
                if (newProfileObj) {
                    selectProfile(newProfileObj)
                }
            } catch (e) {
                ctx.toast(`Failed to create profile: ${e.message}`)
            }
        }

        async function deleteCurrentProfile() {
            if (!selectedProfile.value || selectedProfile.value.isBuiltIn || selectedId.value === 'default') return
            const name = selectedProfile.value.name
            if (!confirm(`Are you sure you want to delete profile '${name}'? This action cannot be undone.`)) return
            try {
                await ctx.agents.deleteProfile(selectedId.value)
                ctx.toast(`Deleted profile '${name}'`)
                selectProfile(defaultProfileItem.value)
            } catch (e) {
                ctx.toast(`Failed to delete profile: ${e.message}`)
            }
        }

        function toggleCustomSkill(skillName) {
            if (editForm.value.skillsMode !== 'custom') {
                editForm.value.skillsMode = 'custom'
                editForm.value.customSkills = [...allAvailableSkills.value]
            }
            const idx = editForm.value.customSkills.indexOf(skillName)
            if (idx >= 0) {
                editForm.value.customSkills.splice(idx, 1)
            } else {
                editForm.value.customSkills.push(skillName)
            }
        }

        function isCustomSkillSelected(skillName) {
            if (editForm.value.skillsMode === 'all') return true
            if (editForm.value.skillsMode === 'none') return false
            return editForm.value.customSkills.includes(skillName)
        }

        function setGroupSkills(group, enable) {
            if (editForm.value.skillsMode !== 'custom') {
                editForm.value.skillsMode = 'custom'
                editForm.value.customSkills = enable ? [...allAvailableSkills.value] : []
            }
            const set = new Set(editForm.value.customSkills)
            if (enable) {
                group.skills.forEach(s => set.add(s))
            } else {
                group.skills.forEach(s => set.delete(s))
            }
            editForm.value.customSkills = Array.from(set)
        }

        function getGroupSkillsActiveCount(group) {
            if (editForm.value.skillsMode === 'all') return group.skills.length
            if (editForm.value.skillsMode === 'none') return 0
            return group.skills.filter(s => editForm.value.customSkills.includes(s)).length
        }

        function toggleCustomTool(toolName) {
            if (editForm.value.toolsMode !== 'custom') {
                editForm.value.toolsMode = 'custom'
                editForm.value.customTools = [...allAvailableTools.value]
            }
            const idx = editForm.value.customTools.indexOf(toolName)
            if (idx >= 0) {
                editForm.value.customTools.splice(idx, 1)
            } else {
                editForm.value.customTools.push(toolName)
            }
        }

        function isCustomToolSelected(toolName) {
            if (editForm.value.toolsMode === 'all') return true
            if (editForm.value.toolsMode === 'none') return false
            return editForm.value.customTools.includes(toolName)
        }

        function setGroupTools(group, enable) {
            if (editForm.value.toolsMode !== 'custom') {
                editForm.value.toolsMode = 'custom'
                editForm.value.customTools = enable ? [...allAvailableTools.value] : []
            }
            const set = new Set(editForm.value.customTools)
            if (enable) {
                group.tools.forEach(t => set.add(t))
            } else {
                group.tools.forEach(t => set.delete(t))
            }
            editForm.value.customTools = Array.from(set)
        }

        function getGroupToolsActiveCount(group) {
            if (editForm.value.toolsMode === 'all') return group.tools.length
            if (editForm.value.toolsMode === 'none') return 0
            return group.tools.filter(t => editForm.value.customTools.includes(t)).length
        }

        function getActiveSkillsSummary() {
            if (editForm.value.skillsMode === 'all') return 'All'
            if (editForm.value.skillsMode === 'none') return 'None'
            return `${editForm.value.customSkills.length}/${allAvailableSkills.value.length}`
        }

        function getActiveToolsSummary() {
            if (editForm.value.toolsMode === 'all') return 'All'
            if (editForm.value.toolsMode === 'none') return 'None'
            return `${editForm.value.customTools.length}/${allAvailableTools.value.length}`
        }

        function triggerAvatarUpload() {
            avatarInputRef.value?.click()
        }

        async function handleAvatarUpload(e) {
            const file = e.target.files?.[0]
            if (!file || !selectedProfile.value || selectedProfile.value.isBuiltIn) return
            try {
                await ctx.agents.uploadAvatar(selectedId.value, file)
                ctx.toast('Avatar uploaded successfully')
            } catch (err) {
                ctx.toast(`Avatar upload failed: ${err.message}`)
            }
        }

        function selectModelOverride(modelNameOrId) {
            editForm.value.model = modelNameOrId
            isModelPickerOpen.value = false
        }

        function selectThemeOverride(themeId) {
            editForm.value.theme = themeId
            isThemeMenuOpen.value = false
        }

        const handleThemeClickOutside = (e) => {
            if (isThemeMenuOpen.value && themeMenuContainer.value && !themeMenuContainer.value.contains(e.target)) {
                isThemeMenuOpen.value = false
            }
        }

        onMounted(async () => {
            selectProfile(defaultProfileItem.value)
            document.addEventListener('click', handleThemeClickOutside)
            try {
                const ts = await ctx.agents.fetchToolsAndSkills()
                const toolsList = (ts.tools || ts.response?.tools || []).map(t => typeof t === 'string' ? t : (t.name || t.function?.name || t))
                const skillsList = (ts.skills || ts.response?.skills || []).map(s => typeof s === 'string' ? s : (s.name || s))
                availableTools.value = toolsList.filter(Boolean)
                availableSkills.value = skillsList.filter(Boolean)
            } catch (e) {
                console.error('Failed to fetch tools and skills', e)
            }
        })

        onUnmounted(() => {
            document.removeEventListener('click', handleThemeClickOutside)
        })

        function resetToDefaults() {
            if (!selectedProfile.value) return
            const id = selectedProfile.value.id
            editForm.value.model = ''
            editForm.value.theme = ''
            isThemeMenuOpen.value = false
            isModelPickerOpen.value = false
            ctx.agents.updateProfileOverrides(id, null)
            ctx.toast(`Reset preferences to defaults for profile: ${selectedProfile.value.name}`)
        }

        async function saveForm() {
            if (!selectedProfile.value) return
            const id = selectedProfile.value.id

            if (selectedProfile.value.isBuiltIn || id === 'default') {
                const override = {
                    model: editForm.value.model || null,
                    theme: editForm.value.theme || null,
                }
                ctx.agents.updateProfileOverrides(id, override)
                ctx.toast(`Saved preferences for profile: ${selectedProfile.value.name}`)
                closeDialog()
            } else {
                let onlySkills = null
                if (editForm.value.skillsMode === 'none') onlySkills = []
                else if (editForm.value.skillsMode === 'custom') onlySkills = editForm.value.customSkills

                let onlyTools = null
                if (editForm.value.toolsMode === 'none') onlyTools = []
                else if (editForm.value.toolsMode === 'custom') onlyTools = editForm.value.customTools

                const config = {
                    name: editForm.value.name,
                    model: editForm.value.model || null,
                    theme: editForm.value.theme || null,
                    onlySkills,
                    onlyTools,
                }
                try {
                    await ctx.agents.saveProfileConfig(id, config)
                    ctx.toast(`Saved profile settings for ${editForm.value.name}`)
                    closeDialog()
                } catch (e) {
                    ctx.toast(`Failed to save profile: ${e.message}`)
                }
            }
        }

        function closeDialog() {
            emit('done')
        }

        function handleEscape(e) {
            if (e) {
                e.stopPropagation()
            }
            if (isModelPickerOpen.value) {
                isModelPickerOpen.value = false
                return
            }
            if (isThemeMenuOpen.value) {
                isThemeMenuOpen.value = false
                return
            }
            if (isNewProfileDialogOpen.value) {
                isNewProfileDialogOpen.value = false
                return
            }
            if (isNewFileDialogOpen.value) {
                isNewFileDialogOpen.value = false
                return
            }
            closeDialog()
        }

        return {
            activeTab,
            selectedId,
            editForm,
            defaultProfileItem,
            agentProfiles,
            availableModels,
            uniqueProviders,
            selectedModelObj,
            filteredModelList,
            formatShortNumber,
            formatCostNum,
            isFreeModel,
            isModelPickerOpen,
            modelSearchQuery,
            selectedProviderFilter,
            modelSortBy,
            modelSortAsc,
            modelSortOptions,
            selectModelOverride,
            lightThemes,
            darkThemes,
            selectedProfile,
            serverDefaultModelDisplay,
            serverDefaultThemeDisplay,
            hasOverride,
            selectProfile,
            isThemeMenuOpen,
            themeMenuContainer,
            selectThemeOverride,
            resetToDefaults,
            saveForm,
            closeDialog,
            handleEscape,
            availableTools,
            availableSkills,
            allAvailableTools,
            allAvailableSkills,
            toolGroups,
            skillGroups,
            profileFiles,
            selectedFilename,
            fileContent,
            isFileLoading,
            isFileSaving,
            isNewFileDialogOpen,
            newFileName,
            newFileInputRef,
            isNewProfileDialogOpen,
            newProfileName,
            newProfileInputRef,
            createNewProfile,
            deleteCurrentProfile,
            selectFile,
            saveSelectedFile,
            createNewFile,
            deleteSelectedFile,
            toggleCustomSkill,
            isCustomSkillSelected,
            setGroupSkills,
            getGroupSkillsActiveCount,
            toggleCustomTool,
            isCustomToolSelected,
            setGroupTools,
            getGroupToolsActiveCount,
            getActiveSkillsSummary,
            getActiveToolsSummary,
            avatarInputRef,
            triggerAvatarUpload,
            handleAvatarUpload,
        }
    }
}

export default {
    order: 20 - 100,

    install(ctx) {
        ext = ctx.scope('agents')

        ctx.components({ AgentSelector, ProfilesManagerModal })
        ctx.modals({ ProfilesManagerModal })

        ctx.setLeftTop({
            agents: {
                component: AgentSelector,
            }
        })

        ctx.setGlobals({
            agents: useAgents(ext)
        })

        ctx.setThreadHeaders({
            agents: {
                component: ThreadProfile,
                show({ thread }) { return thread.metadata?.profile }
            },
        })

        ctx.chatRequestFilters.push(({ request, thread, context, model }) => {
            const agent = ctx.agents.selected
            if (!agent?.prompt) return

            // Inject agent system prompt as a required prompt (always prepended)
            if (agent.injectPrompt !== false) {
                context.requiredSystemPrompts.unshift(agent.prompt)
            }

            // Override tool selection if agent specifies it
            if (agent.tools !== undefined) {
                request.metadata.tools = agent.tools
            }

            // Override skill selection if agent specifies it
            if (agent.skills !== undefined) {
                request.metadata.skills = Array.isArray(agent.skills)
                    ? agent.skills.join(',')
                    : agent.skills
            }

            // include profile info
            if (!request.metadata.profile) {
                request.metadata.profile = agent.id
            }

            console.log('agents.chatRequestFilter', agent.id, {
                onlyTools: agent.onlyTools,
                onlySkills: agent.onlySkills,
                promptLength: agent.prompt?.length
            })
        })

        function getActions(thread) {
            if (!thread.messages || thread.messages.length < 2) return false

            const lastMessage = thread.messages[thread.messages.length - 1]
            if (lastMessage.role != "assistant") return false

            const actions = ctx.threads.getThreadActions(thread.id)
            if (actions) {
                return actions
            }

            const hasSkillToolCall = thread.messages.some(m =>
                m.tool_calls?.some(tc => tc.type == "function" && tc.function.name == "skill"))

            const hasOnlyThinking = !lastMessage.content?.trim() && lastMessage.reasoning?.trim()
            if (hasSkillToolCall || hasOnlyThinking) {
                return {
                    Proceed: { message: 'Proceed' }
                }
            }
            return {}
        }

        ctx.setThreadFooters({
            agents: {
                component: {
                    template: `
                        <div class="mt-2 w-full flex justify-center gap-2">
                            <button type="button" v-for="(props, name) in actions" @click="runAction(name, props)"
                                class="px-3 py-1 rounded-md text-xs font-medium transition-colors select-none" :class="[$styles.secondaryButton]">
                                {{ name }}
                            </button>
                        </div>
                    `,
                    setup(props) {
                        const ctx = inject('ctx')
                        const actions = computed(() =>
                            getActions(ctx.threads?.currentThread?.value))

                        async function runAction(name, action) {
                            console.log('runAction', name, action)
                            if (action.profile) {
                                const agent = ctx.agents.getAgent(action.profile)
                                if (!agent) {
                                    console.error('Agent not found', action.profile)
                                    return
                                }

                                ctx.agents.selectAgent(action.profile)
                                const thread = ctx.threads.currentThread.value
                                const messages = thread.messages.filter(x => x.role !== 'system')

                                if (agent.prompt) {
                                    messages.unshift({
                                        role: 'system',
                                        content: agent.prompt
                                    })
                                }
                                messages.push({
                                    role: 'user',
                                    content: name,
                                })

                                const newThread = await ctx.threads.startNewThread({
                                    title: `Execute Plan ${thread.title}`,
                                    model: ctx.chat.getSelectedModel(),
                                    messages,
                                    redirect: true,
                                })

                                console.log('runAction.profile', newThread)
                            } else if (action.message) {
                                ctx.chat.sendUserMessage(action.message, { target: action.target })
                            }
                            ctx.threads.loadThreadActions(ctx.threads?.currentThread?.value?.id, { force: true })
                        }

                        return {
                            actions,
                            runAction,
                        }
                    }
                },
                show({ thread }) {
                    const actions = getActions(thread)
                    return Object.keys(actions).length > 0
                },
            }
        })
    },

    async load(ctx) {
        ctx.agents.load()
    }
}