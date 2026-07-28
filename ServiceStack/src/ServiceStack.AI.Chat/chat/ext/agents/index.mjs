import { ref, computed, onMounted, onUnmounted, inject, nextTick } from "vue"

let ext

function useAgents(ext) {
    const ctx = ext.ctx
    let agents = {}
    ctx.setState({ agents })

    function getAgent(id) {
        return Object.values(ctx.state.agents).find(a => a.id === id) || null
    }

    function selectAgent(id) {
        console.log('selectAgent', id)
        ext.setPrefs({
            selectedAgent: id
        })
        const profile = id ? getAgent(id) : null
        ctx.changeProfile(profile)
    }
    function getAvatarUrl(agent) {
        return `${ext.baseUrl}/${agent}/avatar`
    }

    async function load() {
        const api = await ext.getJson(``)
        const agentDefs = api.response || {}
        agents = Object.entries(agentDefs).map(([id, def]) => ({
            id,
            name: def.name || ctx.utils.idToName(id),
            theme: def.theme,
            avatar: getAvatarUrl(id),
            model: def.model,
            onlyTools: def.onlyTools,
            onlySkills: def.onlySkills,
            actions: def.actions ?? {},
            injectPrompt: def.injectPrompt ?? true,
            prompt: ''
        }))

        // foreach agent, load its config
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

        if (ext.prefs.selectedAgent) {
            selectAgent(ext.prefs.selectedAgent)
        }
    }

    return {
        get all() { return Object.values(ctx.state.agents) },
        get selectedAgent() { return ext.prefs.selectedAgent },
        get selected() { return getAgent(ext.prefs.selectedAgent) },
        getAgent,
        load,
        selectAgent,
        getAvatarUrl,
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
                    <path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clip-rule="evenodd" />
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
                    <path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clip-rule="evenodd" />
                </svg>
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

export default {
    order: 20 - 100,

    install(ctx) {

        ext = ctx.scope('agents')

        ctx.components({ AgentSelector })

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
            if (thread.messages.length < 2) return false

            const lastMessage = thread.messages[thread.messages.length - 1]
            // only show if the last message is from the assistant
            if (lastMessage.role != "assistant") return false

            // Actions are attached to thread when loaded in threads.
            const actions = ctx.threads.getThreadActions(thread.id)
            if (actions) {
                return actions
            }

            // it has a skill tool call
            const hasSkillToolCall = thread.messages.some(m =>
                m.tool_calls?.some(tc => tc.type == "function" && tc.function.name == "skill"))

            // or the last message has no content but has reasoning
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
                                // Switch to profile
                                const agent = ctx.agents.getAgent(action.profile)
                                if (!agent) {
                                    console.error('Agent not found', action.profile)
                                    return
                                }

                                ctx.agents.selectAgent(action.profile)
                                // start new thread with the same messages but with agent system prompt
                                const thread = ctx.threads.currentThread.value

                                // filter out system prompt from the last agent
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