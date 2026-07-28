import { ref, inject, computed, nextTick } from "vue"
import { leftPart } from "@servicestack/client"

let ext

const SkillSelector = {
    template: `
        <div class="px-4 py-4 max-h-[80vh] overflow-y-auto border-b" :class="$styles.panel">
            
            <!-- Global Controls -->
            <div class="flex items-center justify-between mb-4">
                <span class="text-xs font-bold uppercase tracking-wider" :class="$styles.heading">Include Skills</span>
                <div class="flex items-center gap-2">
                    <button type="button" v-if="!$ctx.tools?.isToolEnabled('skill')"
                        class="px-3 py-1 rounded-md text-xs font-medium border transition-colors select-none cursor-pointer bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-400 border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600"
                        @click="$ctx.tools?.enableTool('skill')"
                        title="'skill' tool needs to be enabled to use Skills"
                        >
                        <span class="text-xs font-semibold text-red-700 dark:text-red-300">⚠️ Enable skill tool</span>
                    </button>
                    <button type="button" @click="$ctx.setPrefs({ onlySkills: null })"
                        class="px-3 py-1 rounded-md text-xs font-medium border transition-colors select-none"
                        :class="$prefs.onlySkills == null
                            ? 'bg-green-100 dark:bg-green-900/40 text-green-800 dark:text-green-300 border-green-300 dark:border-green-800' 
                            : 'cursor-pointer bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-400 border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600'">
                        All Skills
                    </button>
                    <button type="button" @click="$ctx.setPrefs({ onlySkills:[] })"
                        class="px-3 py-1 rounded-md text-xs font-medium border transition-colors select-none"
                        :class="$prefs.onlySkills?.length === 0
                            ? 'bg-fuchsia-100 dark:bg-fuchsia-900/40 text-fuchsia-800 dark:text-fuchsia-300 border-fuchsia-200 dark:border-fuchsia-800' 
                            : 'cursor-pointer bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-400 border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600'">
                        No Skills
                    </button>
                </div>
            </div>

            <!-- Groups -->
            <div class="space-y-3">
                <div v-for="group in skillGroups" :key="group.name" 
                     class="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700 overflow-hidden">
                     
                     <!-- Group Header -->
                     <div class="flex items-center justify-between px-3 py-2 bg-gray-50/50 dark:bg-gray-800/50 cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
                          @click="toggleCollapse(group.name)">
                        
                        <div class="flex items-center gap-2 min-w-0">
                             <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4 text-gray-400 transition-transform duration-200" :class="{ '-rotate-90': isCollapsed(group.name) }">
                                <path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd" />
                             </svg>
                             <span class="font-semibold text-sm text-gray-700 dark:text-gray-200 truncate">
                                {{ group.name || 'Other Skills' }}
                             </span>
                             <span class="text-xs text-gray-400 font-mono">
                                {{ getActiveCount(group) }}/{{ group.skills.length }}
                             </span>
                        </div>

                        <div class="flex items-center gap-2" @click.stop>
                             <button @click="setGroupSkills(group, true)" type="button"
                                title="Include All in Group"
                                class="px-2 py-0.5 rounded text-xs font-medium border transition-colors select-none"
                                :class="getActiveCount(group) === group.skills.length
                                    ? 'bg-green-50 dark:bg-green-900/20 text-green-700 dark:text-green-300 border-green-300 dark:border-green-800 hover:bg-green-100 dark:hover:bg-green-900/40'
                                    : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-400 border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600'">
                                all
                             </button>
                             <button @click="setGroupSkills(group, false)" type="button"
                                title="Include None in Group"
                                class="px-2 py-0.5 rounded text-xs font-medium border transition-colors select-none"
                                :class="getActiveCount(group) === 0
                                    ? 'bg-fuchsia-50 dark:bg-fuchsia-900/20 text-fuchsia-700 dark:text-fuchsia-300 border-fuchsia-200 dark:border-fuchsia-800 hover:bg-fuchsia-100 dark:hover:bg-fuchsia-900/40'
                                    : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-400 border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600'">
                                none
                             </button>
                        </div>
                     </div>
                     
                     <!-- Group Body -->
                     <div v-show="!isCollapsed(group.name)" class="p-3 bg-white dark:bg-gray-900 border-t border-gray-100 dark:border-gray-800">
                         <div class="flex flex-wrap gap-2">
                            <button v-for="skill in group.skills" :key="skill.name" type="button"
                                @click="toggleSkill(skill.name)"
                                :title="skill.description"
                                class="px-2.5 py-1 rounded-full text-xs font-medium border transition-colors select-none text-left truncate max-w-[200px]"
                                :class="isSkillActive(skill.name)
                                    ? 'bg-blue-100 dark:bg-blue-900/40 text-blue-800 dark:text-blue-300 border-blue-200 dark:border-blue-800' 
                                    : 'bg-gray-50 dark:bg-gray-800 text-gray-600 dark:text-gray-400 border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600'">
                                {{ skill.name }}
                            </button>
                         </div>
                     </div>
                </div>
            </div>
        </div>
    `,
    setup() {
        const ctx = inject('ctx')
        const collapsedState = ref({})

        const availableSkills = computed(() => Object.values(ctx.state.skills || {}))

        const skillGroups = computed(() => {
            const skills = availableSkills.value
            const groupsMap = {}
            const otherSkills = []

            skills.forEach(skill => {
                if (skill.group) {
                    if (!groupsMap[skill.group]) groupsMap[skill.group] = []
                    groupsMap[skill.group].push(skill)
                } else {
                    otherSkills.push(skill)
                }
            })

            const definedGroups = Object.entries(groupsMap).map(([name, skills]) => ({
                name,
                skills
            }))

            // Sort groups: writable first, then alphabetically
            definedGroups.sort((a, b) => {
                const aEditable = a.skills.some(s => s.writable)
                const bEditable = b.skills.some(s => s.writable)
                if (aEditable !== bEditable) return aEditable ? -1 : 1
                return a.name.localeCompare(b.name)
            })

            if (otherSkills.length > 0) {
                definedGroups.push({ name: '', skills: otherSkills })
            }

            return definedGroups
        })

        function isSkillActive(name) {
            const only = ctx.prefs.onlySkills
            if (only == null) return true
            if (Array.isArray(only)) {
                return only.includes(name)
            }
            return false
        }

        function toggleSkill(name) {
            let onlySkills = ctx.prefs.onlySkills

            if (onlySkills == null) {
                // If currently 'All', clicking a skill means we enter custom mode with all OTHER skills selected (deselecting clicked)
                // Wait, logic in ToolSelector:
                // if (onlyTools == null) { onlyTools = availableTools.value.map(t => t.function.name).filter(t => t !== name) }
                // This means deselecting one tool switches to "custom" with all but that one.

                onlySkills = availableSkills.value.map(s => s.name).filter(s => s !== name)
            } else {
                if (onlySkills.includes(name)) {
                    onlySkills = onlySkills.filter(s => s !== name)
                } else {
                    onlySkills = [...onlySkills, name]
                    // If has all skills set to 'All' (null)
                    if (onlySkills.length === availableSkills.value.length) {
                        onlySkills = null
                    }
                }
            }

            ctx.setPrefs({ onlySkills })
        }

        function toggleCollapse(groupName) {
            const key = groupName || '_other_'
            collapsedState.value[key] = !collapsedState.value[key]
        }

        function isCollapsed(groupName) {
            const key = groupName || '_other_'
            return !!collapsedState.value[key]
        }

        function setGroupSkills(group, enable) {
            const groupSkillNames = group.skills.map(s => s.name)
            let onlySkills = ctx.prefs.onlySkills

            if (enable) {
                if (onlySkills == null) return
                const newSet = new Set(onlySkills)
                groupSkillNames.forEach(n => newSet.add(n))
                onlySkills = Array.from(newSet)
                if (onlySkills.length === availableSkills.value.length) {
                    onlySkills = null
                }
            } else {
                if (onlySkills == null) {
                    onlySkills = availableSkills.value
                        .map(s => s.name)
                        .filter(n => !groupSkillNames.includes(n))
                } else {
                    onlySkills = onlySkills.filter(n => !groupSkillNames.includes(n))
                }
            }

            ctx.setPrefs({ onlySkills })
        }

        function getActiveCount(group) {
            const onlySkills = ctx.prefs.onlySkills
            if (onlySkills == null) return group.skills.length
            return group.skills.filter(s => onlySkills.includes(s.name)).length
        }

        return {
            availableSkills,
            skillGroups,
            isSkillActive,
            toggleSkill,
            toggleCollapse,
            isCollapsed,
            setGroupSkills,
            getActiveCount
        }
    }
}

// Skills Page Component - Full management interface
const SkillPage = {
    template: `
        <div class="h-full flex flex-col">
            <div class="px-4 py-3 flex items-center justify-between flex-shrink-0 border-b" :class="[$styles.chromeBorder]">
                <div>
                    <h1 class="text-xl font-bold" :class="[$styles.heading]">Manage Skills</h1>
                    <p class="text-sm" :class=[$styles.muted]>{{ Object.keys(skills).length }} skills available</p>
                </div>
                <div class="flex items-center gap-2">
                    <button @click="showCreateDialog = true" type="button" class="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium rounded-md transition-colors" :class="[$styles.primaryButton]">
                        <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" /></svg>
                        Create Skill
                    </button>
                    <button @click="$ctx.togglePath('/skills/store', { left:false })" type="button" class="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium rounded-md transition-colors" :class="[$styles.secondaryButton]">
                        <svg xmlns="http://www.w3.org/2000/svg" class="size-4" viewBox="0 0 24 24"><path fill="currentColor" fill-rule="evenodd" d="M18.319 14.433A8.001 8.001 0 0 0 6.343 3.868a8 8 0 0 0 10.564 11.976l.043.045l4.242 4.243a1 1 0 1 0 1.415-1.415l-4.243-4.242zm-2.076-9.15a6 6 0 1 1-8.485 8.485a6 6 0 0 1 8.485-8.485" clip-rule="evenodd"/></svg>
                        Discover Skills
                    </button>
                </div>
            </div>
            <div class="flex-1 flex min-h-0">
                <div class="w-72 flex flex-col flex-shrink-0 border-r" :class="[$styles.chromeBorder, $styles.bgSidebar]">
                    <div class="p-2 border-b" :class="[$styles.chromeBorder]">
                        <input v-model="searchQuery" type="text" placeholder="Search installed skills..." class="w-full px-3 py-1.5 text-sm rounded-md" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]" />
                    </div>
                    <div class="flex-1 overflow-y-auto">
                        <div v-for="group in skillGroups" :key="group.name" class="border-b last:border-b-0" :class="[$styles.chromeBorder]">
                            <div class="flex items-center justify-between px-3 py-2 text-xs font-semibold uppercase tracking-wider" :class="[$styles.bgSidebar]">
                                <span :class="[$styles.heading]">{{ group.name || 'Other' }}</span>
                                <svg v-if="!isGroupEditable(group.name)" xmlns="http://www.w3.org/2000/svg" class="h-3.5 w-3.5 text-gray-400" viewBox="0 0 20 20" fill="currentColor" title="Read-only">
                                    <path fill-rule="evenodd" d="M5 9V7a5 5 0 0110 0v2a2 2 0 012 2v5a2 2 0 01-2 2H5a2 2 0 01-2-2v-5a2 2 0 012-2zm8-2v2H7V7a3 3 0 016 0z" clip-rule="evenodd"/>
                                </svg>
                            </div>
                            <div class="py-1">
                                <div v-for="skill in group.skills" :key="skill.name">
                                    <div @click="toggleSkillExpand(skill)" class="select-none w-full px-3 py-2 text-left text-sm transition-colors flex items-center gap-2 cursor-pointer" 
                                        :class="selectedSkill?.name === skill.name ? $styles.threadItemActive : $styles.threadItemHover">
                                        <svg xmlns="http://www.w3.org/2000/svg" class="h-3.5 w-3.5 text-gray-400 transition-transform flex-shrink-0" :class="{ '-rotate-90': !isSkillExpanded(skill.name) }" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd" /></svg>
                                        <span class="truncate font-medium flex-1" :class="selectedSkill?.name === skill.name ? 'text-blue-800 dark:text-blue-200' : 'text-gray-700 dark:text-gray-300'">{{ skill.name }}</span>
                                        <span v-if="skill.files?.length" class="text-[10px] px-1.5 py-0.5 rounded-full font-medium" :class="[$styles.icon,$styles.bgBody]">{{ skill.files.length }}</span>
                                    </div>
                                    <div v-show="isSkillExpanded(skill.name)" class="pl-4" :class="[$styles.chromeBorder, $styles.bgBody]">
                                        <div v-if="isEditable(skill)" class="px-3 py-1 flex items-center gap-1 border-b" :class="[$styles.chromeBorder]">
                                            <button @click.stop="selectSkill(skill); showAddFileDialog = true" type="button" title="Add File" class="p-1 rounded text-xs" :class="[$styles.muted, $styles.threadItemHover]">+ file</button>
                                            <button @click.stop="selectSkill(skill); confirmDeleteSkill()" type="button" title="Delete Skill" class="p-1 rounded hover:bg-red-50 dark:hover:bg-red-900/20 text-xs ml-auto" :class="[$styles.muted, $styles.threadItemHover]">delete</button>
                                        </div>
                                        <div v-for="node in getFileTree(skill)" :key="node.path">
                                            <SkillFileNode :node="node" :skill="skill" :selected-file="selectedSkill?.name === skill.name ? selectedFile : null" :is-editable="isEditable(skill)" @select="onFileSelect(skill, $event)" @delete="onFileDelete(skill, $event)" />
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="flex-1 flex flex-col min-w-0">
                    <template v-if="selectedFile">
                        <div class="px-4 py-2 flex items-center justify-between border-b" :class="[$styles.chromeBorder, $styles.cardTitleActive]">
                            <div class="flex items-center gap-2 min-w-0">
                                <span class="text-xs text-gray-500 dark:text-gray-400">{{ selectedSkill?.name }} /</span>
                                <span class="text-sm font-mono text-gray-700 dark:text-gray-300 truncate">{{ selectedFile }}</span>
                                <span v-if="isEditing" class="text-xs px-1.5 py-0.5 rounded bg-yellow-100 dark:bg-yellow-900/40 text-yellow-700 dark:text-yellow-300">editing</span>
                                <span v-if="hasUnsavedChanges" class="text-xs text-orange-500">•</span>
                            </div>
                            <div class="flex items-center gap-2">
                                <template v-if="isEditing">
                                    <button @click="saveFile" :disabled="saving" type="button" class="px-3 py-1 text-xs font-medium rounded" :class="[$styles.primaryButton]">{{ saving ? 'Saving...' : 'Save' }}</button>
                                    <button @click="cancelEdit" type="button" class="px-3 py-1 text-xs font-medium rounded" :class="[$styles.secondaryButton]">Cancel</button>
                                </template>
                                <template v-else-if="isEditable(selectedSkill)">
                                    <button @click="startEdit" type="button" class="px-3 py-1 text-xs font-medium rounded" :class="[$styles.secondaryButton]">Edit</button>
                                </template>
                            </div>
                        </div>
                        <div class="flex-1 overflow-auto">
                            <div v-if="loadingFile" class="flex items-center justify-center h-full text-gray-500">Loading...</div>
                            <textarea v-else-if="isEditing" ref="editorRef" v-model="editContent" class="w-full h-full p-4 font-mono text-sm resize-none focus:outline-none" spellcheck="false"></textarea>
                            <div v-else class="p-4 font-mono text-sm whitespace-pre-wrap break-words" :class="[$styles.textBlock]">{{ fileContent }}</div>
                        </div>
                    </template>
                    <template v-else-if="selectedSkill">
                        <div class="p-6">
                            <h2 class="text-2xl font-bold mb-2" :class="[$styles.heading]">{{ selectedSkill.name }}</h2>
                            <p class="mb-4" :class="[$styles.muted]">{{ selectedSkill.description }}</p>
                            <div class="grid grid-cols-2 gap-4 text-sm">
                                <div><span :class="[$styles.muted]">Group:</span><span class="ml-2">{{ selectedSkill.group }}</span></div>
                                <div><span :class="[$styles.muted]">Files:</span><span class="ml-2">{{ selectedSkill.files?.length || 0 }}</span></div>
                                <div class="col-span-2"><span :class="[$styles.muted]">Location:</span><span class="ml-2 font-mono text-xs break-all">{{ selectedSkill.location }}</span></div>
                            </div>
                            <div class="mt-6"><p class="text-sm" :class="[$styles.muted]">Select a file from the tree to view or edit its contents.</p></div>
                        </div>
                    </template>
                    <template v-else>
                        <div class="flex items-center justify-center h-full" :class="[$styles.muted]">
                            <div class="text-center">
                                <svg xmlns="http://www.w3.org/2000/svg" class="h-12 w-12 mx-auto mb-4" :class="[$styles.mutedIcon]" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4" /></svg>
                                <p>Select a skill to view its files</p>
                            </div>
                        </div>
                    </template>
                </div>
            </div>
            <div v-if="showCreateDialog" class="fixed inset-0 z-100 flex items-center justify-center bg-black/50" @click.self="showCreateDialog = false">
                <div class="rounded-lg shadow-xl w-full max-w-md mx-4 bg-white dark:bg-gray-800">
                    <div class="px-4 py-3 border-b border-gray-200 dark:border-gray-700"><h3 class="text-lg font-semibold">Create New Skill</h3></div>
                    <div class="p-4 space-y-4">
                        <div>
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Skill Name</label>
                            <input :value="newSkillName" @input="onSkillNameInput" type="text" placeholder="my-new-skill" class="w-full px-3 py-2 text-sm rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 focus:ring-2 focus:ring-blue-500" @keyup.enter="createSkill" maxlength="40" />
                            <p class="mt-1 text-xs text-gray-500">Lowercase letters, numbers, and hyphens only. Max 40 characters.</p>
                        </div>
                        <div v-if="createError" class="p-3 rounded-md bg-red-50 dark:bg-red-900/20 text-red-700 dark:text-red-300 text-sm">{{ createError }}</div>
                    </div>
                    <div class="px-4 py-3 border-t border-gray-200 dark:border-gray-700 flex justify-end gap-2">
                        <button @click="showCreateDialog = false" type="button" class="px-4 py-2 text-sm font-medium rounded-md border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700">Cancel</button>
                        <button @click="createSkill" :disabled="creating || !newSkillName.trim()" type="button" class="px-4 py-2 text-sm font-medium rounded-md bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-50">{{ creating ? 'Creating...' : 'Create' }}</button>
                    </div>
                </div>
            </div>
            <div v-if="showAddFileDialog" class="fixed inset-0 z-100 flex items-center justify-center bg-black/50" @click.self="showAddFileDialog = false">
                <div class="rounded-lg shadow-xl w-full max-w-md mx-4 bg-white dark:bg-gray-800">
                    <div class="px-4 py-3 border-b border-gray-200 dark:border-gray-700"><h3 class="text-lg font-semibold">Add New File</h3></div>
                    <div class="p-4 space-y-4">
                        <div>
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">File Path</label>
                            <input v-model="newFilePath" type="text" placeholder="scripts/my-script.py" class="w-full px-3 py-2 text-sm rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 focus:ring-2 focus:ring-blue-500" @keyup.enter="addFile" />
                            <p class="mt-1 text-xs text-gray-500">Relative path from skill root (e.g., scripts/helper.py)</p>
                        </div>
                        <div v-if="addFileError" class="p-3 rounded-md bg-red-50 dark:bg-red-900/20 text-red-700 dark:text-red-300 text-sm">{{ addFileError }}</div>
                    </div>
                    <div class="px-4 py-3 border-t border-gray-200 dark:border-gray-700 flex justify-end gap-2">
                        <button @click="showAddFileDialog = false" type="button" class="px-4 py-2 text-sm font-medium rounded-md border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700">Cancel</button>
                        <button @click="addFile" :disabled="addingFile || !newFilePath.trim()" type="button" class="px-4 py-2 text-sm font-medium rounded-md bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-50">{{ addingFile ? 'Adding...' : 'Add' }}</button>
                    </div>
                </div>
            </div>
            <div v-if="deleteConfirm" class="fixed inset-0 z-100 flex items-center justify-center bg-black/50" @click.self="deleteConfirm = null">
                <div class="rounded-lg shadow-xl w-full max-w-sm mx-4 bg-white dark:bg-gray-800">
                    <div class="p-4">
                        <h3 class="text-lg font-semibold mb-2">Confirm Delete</h3>
                        <p class="text-gray-600 dark:text-gray-400 text-sm">{{ deleteConfirm.type === 'skill' ? 'Delete skill "' + deleteConfirm.name + '"? This cannot be undone.' : 'Delete "' + deleteConfirm.path + '"?' }}</p>
                    </div>
                    <div class="px-4 py-3 border-t border-gray-200 dark:border-gray-700 flex justify-end gap-2">
                        <button @click="deleteConfirm = null" type="button" class="px-4 py-2 text-sm font-medium rounded-md border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700">Cancel</button>
                        <button @click="executeDelete" :disabled="deleting" type="button" class="px-4 py-2 text-sm font-medium rounded-md bg-red-600 text-white hover:bg-red-700 disabled:opacity-50">{{ deleting ? 'Deleting...' : 'Delete' }}</button>
                    </div>
                </div>
            </div>
        </div>
    `,
    setup() {
        const ctx = inject('ctx')
        const searchQuery = ref('')
        const selectedSkill = ref(null)
        const selectedFile = ref(null)
        const fileContent = ref('')
        const editContent = ref('')
        const isEditing = ref(false)
        const loadingFile = ref(false)
        const saving = ref(false)
        const showCreateDialog = ref(false)
        const showAddFileDialog = ref(false)
        const deleteConfirm = ref(null)
        const newSkillName = ref('')
        const creating = ref(false)
        const createError = ref('')
        const newFilePath = ref('')
        const addingFile = ref(false)
        const addFileError = ref('')
        const deleting = ref(false)
        const editorRef = ref(null)
        const expandedSkills = ref({})
        const skills = computed(() => ctx.state.skills || {})

        const skillGroups = computed(() => {
            const grouped = {}
            const query = searchQuery.value.toLowerCase()
            Object.values(skills.value).forEach(skill => {
                if (query && !skill.name.toLowerCase().includes(query) && !skill.description?.toLowerCase().includes(query)) return
                const group = skill.group || 'Other'
                if (!grouped[group]) grouped[group] = []
                grouped[group].push(skill)
            })
            return Object.entries(grouped).sort((a, b) => {
                const aEditable = a[1].some(s => s.writable)
                const bEditable = b[1].some(s => s.writable)
                if (aEditable !== bEditable) return aEditable ? -1 : 1
                return a[0].localeCompare(b[0])
            }).map(([name, skills]) => ({ name, skills: skills.sort((a, b) => a.name.localeCompare(b.name)) }))
        })
        function getFileTree(skill) {
            if (!skill?.files) return []
            const files = [...skill.files].sort()
            const tree = []
            const dirs = {}
            files.forEach(filePath => {
                const parts = filePath.split('/')
                if (parts.length === 1) {
                    tree.push({ name: filePath, path: filePath, isFile: true })
                } else {
                    const dirName = parts[0]
                    if (!dirs[dirName]) { dirs[dirName] = { name: dirName, path: dirName, isFile: false, children: [] }; tree.push(dirs[dirName]) }
                    dirs[dirName].children.push({ name: parts.slice(1).join('/'), path: filePath, isFile: true })
                }
            })
            return tree.sort((a, b) => { if (a.isFile !== b.isFile) return a.isFile ? 1 : -1; return a.name.localeCompare(b.name) })
        }
        const hasUnsavedChanges = computed(() => isEditing.value && editContent.value !== fileContent.value)
        function isGroupEditable(groupName) { return Object.values(skills.value).some(s => s.group === groupName && s.writable) }
        function isEditable(skill) { return skill?.writable }
        function isSkillExpanded(name) { return !!expandedSkills.value[name] }
        function toggleSkillExpand(skill) {
            expandedSkills.value[skill.name] = !expandedSkills.value[skill.name]
            if (expandedSkills.value[skill.name]) {
                selectedSkill.value = skill
                selectedFile.value = null
                fileContent.value = ''
                isEditing.value = false
            }
        }
        function selectSkill(skill) {
            if (hasUnsavedChanges.value && !confirm('Discard unsaved changes?')) return
            selectedSkill.value = skill; selectedFile.value = null; fileContent.value = ''; isEditing.value = false
            expandedSkills.value[skill.name] = true
        }
        async function selectFile(filePath) {
            if (hasUnsavedChanges.value && !confirm('Discard unsaved changes?')) return
            selectedFile.value = filePath; isEditing.value = false; loadingFile.value = true
            try {
                const res = await ext.getJson(`/file/${selectedSkill.value.name}/${filePath}`)
                fileContent.value = res.response ? res.response.content : `Error: ${res.error?.message || 'Failed to load'}`
            } catch (e) { fileContent.value = `Error: ${e.message}` }
            finally { loadingFile.value = false }
        }
        function onFileSelect(skill, filePath) {
            if (hasUnsavedChanges.value && !confirm('Discard unsaved changes?')) return
            selectedSkill.value = skill
            selectFile(filePath)
        }
        function onFileDelete(skill, filePath) {
            selectedSkill.value = skill
            confirmDeleteFile(filePath)
        }
        function startEdit() { editContent.value = fileContent.value; isEditing.value = true; nextTick(() => editorRef.value?.focus()) }
        function cancelEdit() { if (hasUnsavedChanges.value && !confirm('Discard changes?')) return; isEditing.value = false; editContent.value = '' }
        async function saveFile() {
            saving.value = true
            try {
                const res = await ext.postJson(`/file/${selectedSkill.value.name}`, { path: selectedFile.value, content: editContent.value })
                if (res.response) { fileContent.value = editContent.value; isEditing.value = false; if (res.response.skill) { ctx.setState({ skills: { ...skills.value, [res.response.skill.name]: res.response.skill } }); selectedSkill.value = res.response.skill } }
                else { alert(`Error: ${res.error?.message || 'Unknown'}`) }
            } catch (e) { alert(`Error: ${e.message}`) }
            finally { saving.value = false }
        }
        function onSkillNameInput(e) {
            // Sanitize to lowercase letters, numbers, and hyphens only
            const sanitized = e.target.value.toLowerCase().replace(/[^a-z0-9-\s]/g, '').replace(/\s+/g, '-')
            newSkillName.value = sanitized
            // Update input value if sanitization changed it
            if (e.target.value !== sanitized) {
                e.target.value = sanitized
            }
        }
        async function createSkill() {
            createError.value = ''; creating.value = true
            try {
                const res = await ext.postJson('/create', { name: newSkillName.value.trim() })
                if (res.response) {
                    ctx.setState({ skills: { ...skills.value, [res.response.skill.name]: res.response.skill } })
                    selectedSkill.value = res.response.skill
                    expandedSkills.value[res.response.skill.name] = true
                    showCreateDialog.value = false
                    newSkillName.value = ''
                }
                else { createError.value = res.error?.message || 'Failed' }
            } catch (e) { createError.value = e.message }
            finally { creating.value = false }
        }
        async function addFile() {
            addFileError.value = ''; addingFile.value = true
            try {
                const res = await ext.postJson(`/file/${selectedSkill.value.name}`, { path: newFilePath.value.trim(), content: '' })
                if (res.response) { if (res.response.skill) { ctx.setState({ skills: { ...skills.value, [res.response.skill.name]: res.response.skill } }); selectedSkill.value = res.response.skill }; selectedFile.value = newFilePath.value.trim(); fileContent.value = ''; showAddFileDialog.value = false; newFilePath.value = ''; startEdit() }
                else { addFileError.value = res.error?.message || 'Failed' }
            } catch (e) { addFileError.value = e.message }
            finally { addingFile.value = false }
        }
        function confirmDeleteSkill() { deleteConfirm.value = { type: 'skill', name: selectedSkill.value.name } }
        function confirmDeleteFile(filePath) { deleteConfirm.value = { type: 'file', path: filePath, skillName: selectedSkill.value.name } }
        async function executeDelete() {
            deleting.value = true
            try {
                if (deleteConfirm.value.type === 'skill') {
                    const res = await ext.deleteJson(`/skill/${deleteConfirm.value.name}`)
                    if (res.response?.deleted) { const s = { ...skills.value }; delete s[deleteConfirm.value.name]; ctx.setState({ skills: s }); selectedSkill.value = null; selectedFile.value = null; delete expandedSkills.value[deleteConfirm.value.name] }
                    else { alert(`Error: ${res.error?.message || 'Failed'}`) }
                } else {
                    const res = await ext.deleteJson(`/file/${deleteConfirm.value.skillName}?path=${encodeURIComponent(deleteConfirm.value.path)}`)
                    if (res.response) { if (res.response.skill) { ctx.setState({ skills: { ...skills.value, [res.response.skill.name]: res.response.skill } }); selectedSkill.value = res.response.skill }; if (selectedFile.value === deleteConfirm.value.path) { selectedFile.value = null; fileContent.value = '' } }
                    else { alert(`Error: ${res.error?.message || 'Failed'}`) }
                }
            } catch (e) { alert(`Error: ${e.message}`) }
            finally { deleting.value = false; deleteConfirm.value = null }
        }
        return { skills, searchQuery, skillGroups, selectedSkill, selectedFile, fileContent, editContent, isEditing, loadingFile, saving, hasUnsavedChanges, editorRef, showCreateDialog, showAddFileDialog, deleteConfirm, newSkillName, creating, createError, newFilePath, addingFile, addFileError, deleting, isEditable, isGroupEditable, selectSkill, selectFile, startEdit, cancelEdit, saveFile, createSkill, addFile, confirmDeleteSkill, confirmDeleteFile, executeDelete, expandedSkills, isSkillExpanded, toggleSkillExpand, getFileTree, onFileSelect, onFileDelete, onSkillNameInput }
    }
}

const FileTreeNode = {
    name: 'FileTreeNode',
    template: `
        <div>
            <div v-if="node.isFile" @click="$emit('select', node.path)" class="group flex items-center gap-2 px-3 py-1 text-sm cursor-pointer transition-colors" :class="selectedFile === node.path ? 'bg-blue-100 dark:bg-blue-900/40 text-blue-800 dark:text-blue-200' : 'text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700'">
                <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4 text-gray-400 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" /></svg>
                <span class="truncate flex-1">{{ node.name }}</span>
                <button v-if="isEditable && node.path.toLowerCase() !== 'skill.md'" @click.stop="$emit('delete', node.path)" type="button" class="opacity-0 group-hover:opacity-100 p-0.5 rounded hover:bg-red-100 dark:hover:bg-red-900/30 text-gray-400 hover:text-red-600">
                    <svg xmlns="http://www.w3.org/2000/svg" class="h-3 w-3" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" /></svg>
                </button>
            </div>
            <div v-else>
                <div @click="expanded = !expanded" class="flex items-center gap-2 px-3 py-1 text-sm cursor-pointer text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700">
                    <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4 text-gray-400 transition-transform" :class="{ '-rotate-90': !expanded }" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" /></svg>
                    <span class="font-medium">{{ node.name }}/</span>
                </div>
                <div v-show="expanded" class="pl-4">
                    <FileTreeNode v-for="child in node.children" :key="child.path" :node="child" :selected-file="selectedFile" :is-editable="isEditable" @select="$emit('select', $event)" @delete="$emit('delete', $event)" />
                </div>
            </div>
        </div>
    `,
    props: { node: { type: Object, required: true }, selectedFile: { type: String, default: null }, isEditable: { type: Boolean, default: false } },
    emits: ['select', 'delete'],
    setup() { return { expanded: ref(true) } }
}

const SkillFileNode = {
    name: 'SkillFileNode',
    template: `
        <div>
            <div v-if="node.isFile" @click="$emit('select', node.path)" class="group flex items-center gap-1.5 px-2 py-0.5 text-xs cursor-pointer border-l border-t border-b transition-colors" :class="selectedFile === node.path ? ($styles.threadItemActive + ' ' + $styles.threadItemActiveBorder) : 'border-transparent ' + $styles.threadItemHover">
                <svg xmlns="http://www.w3.org/2000/svg" class="h-3 w-3 flex-shrink-0" :class="[$styles.icon]" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" /></svg>
                <span class="select-none truncate flex-1">{{ node.name }}</span>
                <button v-if="isEditable && node.path.toLowerCase() !== 'skill.md'" @click.stop="$emit('delete', node.path)" type="button" class="opacity-0 group-hover:opacity-100 p-0.5 rounded hover:bg-red-100 dark:hover:bg-red-900/30 text-gray-400 hover:text-red-500">
                    <svg xmlns="http://www.w3.org/2000/svg" class="h-2.5 w-2.5" :class="[$styles.icon]" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" /></svg>
                </button>
            </div>
            <div v-else>
                <div @click="expanded = !expanded" class="flex items-center gap-1.5 px-2 py-0.5 text-xs cursor-pointer text-gray-500 dark:text-gray-400" :class="[$styles.threadItemHover]">
                    <svg xmlns="http://www.w3.org/2000/svg" class="h-3 w-3 text-gray-400 transition-transform" :class="{ '-rotate-90': !expanded }" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" /></svg>
                    <span class="select-none font-medium">{{ node.name }}/</span>
                </div>
                <div v-show="expanded" class="pl-3">
                    <SkillFileNode v-for="child in node.children" :key="child.path" :node="child" :skill="skill" :selected-file="selectedFile" :is-editable="isEditable" @select="$emit('select', $event)" @delete="$emit('delete', $event)" />
                </div>
            </div>
        </div>
    `,
    props: { node: { type: Object, required: true }, skill: { type: Object, required: true }, selectedFile: { type: String, default: null }, isEditable: { type: Boolean, default: false } },
    emits: ['select', 'delete'],
    setup() { return { expanded: ref(true) } }
}

// Skill Store Component - Search and install available skills
const SkillStore = {
    template: `
        <div class="h-full flex flex-col">
            <div class="px-4 py-3 flex items-center justify-between flex-shrink-0 border-b" :class="[$styles.chromeBorder]">
                <div>
                    <h1 class="text-xl font-bold">Discover Skills</h1>
                    <p class="text-sm text-gray-500 dark:text-gray-400">{{ total.toLocaleString() }} skills available</p>
                </div>
                <div class="flex items-center gap-2">
                    <button @click="$ctx.togglePath('/skills', { left:false })" type="button" class="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium rounded-md transition-colors" :class="[$styles.secondaryButton]">
                        <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M9.707 16.707a1 1 0 01-1.414 0l-6-6a1 1 0 010-1.414l6-6a1 1 0 011.414 1.414L5.414 9H17a1 1 0 110 2H5.414l4.293 4.293a1 1 0 010 1.414z" clip-rule="evenodd" /></svg>
                        Installed Skills
                    </button>
                </div>
            </div>
            <div class="p-4 border-b" :class="[$styles.chromeBorder]">
                <div class="relative">
                    <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                    </svg>
                    <input v-model="searchQuery" @input="onSearchInput" type="text" placeholder="Search available skills..." 
                        class="w-full pl-10 pr-4 py-2.5 text-sm rounded-lg" :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]" />
                    <div v-if="searching" class="absolute right-3 top-1/2 -translate-y-1/2">
                        <svg class="animate-spin h-5 w-5 text-blue-500" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                        </svg>
                    </div>
                </div>
            </div>
            <div class="flex-1 overflow-y-auto">
                <div v-if="results.length === 0 && !searching" class="flex items-center justify-center h-full text-gray-500 dark:text-gray-400">
                    <div class="text-center">
                        <svg xmlns="http://www.w3.org/2000/svg" class="h-12 w-12 mx-auto mb-4 text-gray-300 dark:text-gray-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                        </svg>
                        <p>{{ searchQuery ? 'No skills found' : 'Search for skills to install' }}</p>
                    </div>
                </div>
                <div v-else class="divide-y divide-gray-200 dark:divide-gray-700">
                    <div v-for="skill in results" :key="skill.id" class="p-4 hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                        <div class="flex items-start justify-between gap-4">
                            <div class="min-w-0 flex-1">
                                <h3 class="text-sm font-semibold truncate">{{ skill.name }}</h3>
                                <div class="mt-1 flex items-center gap-3 text-xs text-gray-500 dark:text-gray-400">
                                    <span class="inline-flex items-center gap-1">
                                        <svg xmlns="http://www.w3.org/2000/svg" class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
                                            <path fill-rule="evenodd" d="M3 17a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zm3.293-7.707a1 1 0 011.414 0L9 10.586V3a1 1 0 112 0v7.586l1.293-1.293a1 1 0 111.414 1.414l-3 3a1 1 0 01-1.414 0l-3-3a1 1 0 010-1.414z" clip-rule="evenodd" />
                                        </svg>
                                        {{ formatInstalls(skill.installs) }}
                                    </span>
                                    <span class="inline-flex items-center gap-1 truncate" :title="skill.topSource">
                                        <svg xmlns="http://www.w3.org/2000/svg" class="h-3.5 w-3.5 flex-shrink-0" viewBox="0 0 20 20" fill="currentColor">
                                            <path fill-rule="evenodd" d="M10 0C4.477 0 0 4.484 0 10.017c0 4.425 2.865 8.18 6.839 9.504.5.092.682-.217.682-.483 0-.237-.008-.868-.013-1.703-2.782.605-3.369-1.343-3.369-1.343-.454-1.158-1.11-1.466-1.11-1.466-.908-.62.069-.608.069-.608 1.003.07 1.531 1.032 1.531 1.032.892 1.53 2.341 1.088 2.91.832.092-.647.35-1.088.636-1.338-2.22-.253-4.555-1.113-4.555-4.951 0-1.093.39-1.988 1.029-2.688-.103-.253-.446-1.272.098-2.65 0 0 .84-.27 2.75 1.026A9.564 9.564 0 0110 4.844c.85.004 1.705.115 2.504.337 1.909-1.296 2.747-1.027 2.747-1.027.546 1.379.203 2.398.1 2.651.64.7 1.028 1.595 1.028 2.688 0 3.848-2.339 4.695-4.566 4.942.359.31.678.921.678 1.856 0 1.338-.012 2.419-.012 2.747 0 .268.18.58.688.482A10.019 10.019 0 0020 10.017C20 4.484 15.522 0 10 0z" clip-rule="evenodd" />
                                        </svg>
                                        {{ skill.topSource }}
                                    </span>
                                </div>
                            </div>
                            <div class="flex-shrink-0">
                                <button v-if="isInstalled(skill.id)" disabled type="button"
                                    class="px-3 py-1.5 text-xs font-medium rounded-md bg-gray-100 dark:bg-gray-700 text-gray-500 dark:text-gray-400 cursor-not-allowed">
                                    Installed
                                </button>
                                <button v-else-if="installing.has(skill.id)" disabled type="button"
                                    class="px-3 py-1.5 text-xs font-medium rounded-md bg-blue-100 dark:bg-blue-900/40 text-blue-700 dark:text-blue-300 cursor-wait inline-flex items-center gap-1.5">
                                    <svg class="animate-spin h-3.5 w-3.5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                                    </svg>
                                    Installing...
                                </button>
                                <button v-else @click="installSkill(skill)" type="button"
                                    class="px-3 py-1.5 text-xs font-medium rounded-md bg-blue-600 text-white hover:bg-blue-700 transition-colors">
                                    Install
                                </button>
                            </div>
                        </div>
                        <div v-if="installError[skill.id]" class="mt-2 text-xs text-red-600 dark:text-red-400">
                            {{ installError[skill.id] }}
                        </div>
                    </div>
                </div>
                <div v-if="results.length > 0 && results.length < total" class="p-4 flex justify-center">
                    <button @click="loadMore" :disabled="searching" type="button"
                        class="px-4 py-2 text-sm font-medium rounded-md border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors disabled:opacity-50">
                        {{ searching ? 'Loading...' : 'Load More' }}
                    </button>
                </div>
            </div>
        </div>
    `,
    setup() {
        const ctx = inject('ctx')
        const searchQuery = ref('')
        const results = ref([])
        const total = ref(0)
        const searching = ref(false)
        const installing = ref(new Set())
        const installError = ref({})
        const offset = ref(0)
        const limit = 50
        let searchTimeout = null

        const installedSkills = computed(() => ctx.state.skills || {})

        function isInstalled(skillId) {
            // Check if skill is already installed by comparing id/name
            return Object.values(installedSkills.value).some(s =>
                s.name === skillId || s.name === skillId.replace(/-/g, ' ')
            )
        }

        function formatInstalls(count) {
            if (count >= 1000000) return (count / 1000000).toFixed(1) + 'M'
            if (count >= 1000) return (count / 1000).toFixed(1) + 'k'
            return count.toString()
        }

        async function search(append = false) {
            searching.value = true
            try {
                const params = new URLSearchParams({
                    q: searchQuery.value,
                    limit: limit.toString(),
                    offset: (append ? offset.value : 0).toString()
                })
                const res = await ext.getJson(`/search?${params}`)
                if (res.response) {
                    if (append) {
                        results.value = [...results.value, ...res.response.results]
                    } else {
                        results.value = res.response.results
                        offset.value = 0
                    }
                    total.value = res.response.total
                    offset.value = results.value.length
                }
            } catch (e) {
                console.error('Search failed:', e)
            } finally {
                searching.value = false
            }
        }

        function onSearchInput() {
            if (searchTimeout) clearTimeout(searchTimeout)
            searchTimeout = setTimeout(() => search(false), 300)
        }

        function loadMore() {
            search(true)
        }

        async function installSkill(skill) {
            installing.value = new Set([...installing.value, skill.id])
            delete installError.value[skill.id]

            try {
                const res = await ext.postJson(`/install/${skill.id}`)
                if (res.error) {
                    installError.value[skill.id] = res.error.message || 'Installation failed'
                } else {
                    // Refresh installed skills
                    const api = await ext.getJson('/')
                    if (api.response) {
                        ctx.setState({ skills: api.response })
                    }
                }
            } catch (e) {
                installError.value[skill.id] = e.message || 'Installation failed'
            } finally {
                const newSet = new Set(installing.value)
                newSet.delete(skill.id)
                installing.value = newSet
            }
        }

        // Initial load - show popular skills
        search(false)

        return {
            searchQuery,
            results,
            total,
            searching,
            installing,
            installError,
            isInstalled,
            formatInstalls,
            onSearchInput,
            loadMore,
            installSkill
        }
    }
}

function codeFragment(s) {
    return "`" + s + "`"
}
function codeBlock(s) {
    return "```\n" + s + "\n```\n"
}

const SkillInstructions = `
You have access to specialized skills that extend your capabilities with domain-specific knowledge, workflows, and tools. 
Skills are modular packages containing instructions, scripts, references, and assets for particular tasks.

## Using Skills

Use the skill tool to read a skill's main instructions and guidance, e.g:
${codeBlock("skill({ name: \"skill-name\" })")}

To read a specific file within a skill (scripts, references, assets):
${codeBlock("skill({ name: \"skill-name\", file: \"relative/path/to/file\" })")}

Examples:
- ${codeFragment("skill({ name: \"create-plan\" })")} - Read the create-plan skill's SKILL.md instructions
- ${codeFragment("skill({ name: \"web-artifacts-builder\", file: \"scripts/init-artifact.sh\" })")} - Read a specific script

## When to Use Skills

You should read the appropriate skill BEFORE starting work on relevant tasks. Skills contain best practices, scripts, and reference materials that significantly improve output quality.

**Skill Selection Guidelines:**
- Match the task to available skill descriptions
- Multiple skills may be relevant - read all that apply
- Read the skill first, then follow its instructions

## Available Skills
$$AVAILABLE_SKILLS$$

## Important Notes

- Always read the skill BEFORE starting implementation
- Skills may contain scripts that can be executed directly without loading into context
- Multiple skills can and should be combined when tasks span multiple domains
- If a skill references additional files (references/, scripts/, assets/), read those as needed during execution
`

export default {
    order: 15 - 100,

    install(ctx) {
        ext = ctx.scope("skills")

        ctx.components({ SkillSelector, SkillPage, SkillStore, FileTreeNode, SkillFileNode })

        const svg = (attrs, title) => `<svg ${attrs} xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">${title ? "<title>" + title + "</title>" : ''}<path fill="currentColor" d="M20 17a2 2 0 0 0 2-2V4a2 2 0 0 0-2-2H9.46c.35.61.54 1.3.54 2h10v11h-9v2m4-10v2H9v13H7v-6H5v6H3v-8H1.5V9a2 2 0 0 1 2-2zM8 4a2 2 0 0 1-2 2a2 2 0 0 1-2-2a2 2 0 0 1 2-2a2 2 0 0 1 2 2"/></svg>`

        ctx.setLeftIcons({
            skills: {
                component: { template: svg([`@click="$ctx.togglePath('/skills', { left:false })"`].join(' ')) },
                isActive({ path }) {
                    return ctx.matchesPath(path, '/skills')
                }
            }
        })

        ctx.routes.push({ path: '/skills', component: SkillPage, meta: { title: 'Manage Skills' } })
        ctx.routes.push({ path: '/skills/store', component: SkillStore, meta: { title: 'Skill Store' } })

        ctx.setTopIcons({
            skills: {
                component: {
                    template: svg([
                        `@click="$ctx.toggleTop('SkillSelector')"`,
                        `:class="!$tools?.isToolEnabled('skill') ? '' : $prefs.onlySkills == null ? $styles.iconFull : $prefs.onlySkills.length ? $styles.iconPartial : ''"`
                    ].join(' ')),
                },
                isActive({ top }) {
                    return top === 'SkillSelector'
                },
                get title() {
                    return !ctx.tools?.isToolEnabled('skill')
                        ? `skill tool not enabled`
                        : ctx.prefs.onlySkills == null
                            ? `All Skills Included`
                            : ctx.prefs.onlySkills.length
                                ? `${ctx.prefs.onlySkills.length} ${ctx.utils.pluralize('Skill', ctx.prefs.onlySkills.length)} Included`
                                : 'No Skills Included'
                }
            }
        })

        ctx.chatRequestFilters.push(({ request, thread, context, model }) => {

            if (!ctx.tools?.isToolEnabled('skill')) {
                console.log(`skills.chatRequestFilters: 'skill' tool is not enabled`)
                return
            }

            if (!model) {
                console.log(`WARN skills.chatRequestFilters: no model`)
            } else if (model.tool_call === false) {
                console.log(`skills.chatRequestFilters: model ${model.id} ${model.name} tool_call:false`,
                    JSON.stringify(model, null, 2)
                )
                return
            }

            // console.log('[skills.chatRequestFilters]',
            //     JSON.stringify({ request, thread, context, model }, null, 2)
            // )

            const prefs = ctx.prefs
            if (prefs.onlySkills != null) {
                if (Array.isArray(prefs.onlySkills)) {
                    request.metadata.skills = prefs.onlySkills.length > 0
                        ? prefs.onlySkills.join(',')
                        : 'none'
                }
            } else {
                request.metadata.skills = 'all'
            }

            console.log('skills.chatRequestFilters', prefs.onlySkills, Object.keys(ctx.state.skills || {}))
            const skills = ctx.state.skills
            if (!skills) return

            const includeSkills = []
            for (const skill of Object.values(skills)) {
                if (prefs.onlySkills == null || prefs.onlySkills.includes(skill.name)) {
                    includeSkills.push(skill)
                }
            }
            if (!includeSkills.length) return

            const sb = []
            sb.push("<available_skills>")
            for (const skill of includeSkills) {
                sb.push(" <skill>")
                sb.push("  <name>" + ctx.utils.encodeHtml(skill.name) + "</name>")
                sb.push("  <description>" + ctx.utils.encodeHtml(skill.description) + "</description>")
                sb.push("  <location>" + ctx.utils.encodeHtml(skill.location) + "</location>")
                sb.push(" </skill>")
            }
            sb.push("</available_skills>")

            const skillsPrompt = SkillInstructions.replace('$$AVAILABLE_SKILLS$$', sb.join('\n')).trim()
            context.requiredSystemPrompts.push(skillsPrompt)
        })

        ctx.setState({
            skills: {}
        })
    },

    async load(ctx) {
        const api = await ext.getJson('/')
        if (api.response) {
            ctx.setState({ skills: api.response })
        } else {
            ctx.setError(api.error)
        }
    }
}