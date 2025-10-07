import { ref, computed, inject, onMounted, onUnmounted } from "vue"

export default {
    template:`
        <div v-if="$ai.isAdmin" ref="triggerRef" class="relative" :key="renderKey">
            <button type="button" @click="togglePopover" 
                class="mt-1 flex space-x-2 items-center text-sm font-semibold select-none rounded-sm py-2 px-3 border border-transparent hover:bg-gray-50 hover:shadow hover:border-gray-200">
                <span class="text-gray-600" :title="models.length + ' models from ' + (config.status.enabled||[]).length + ' enabled providers'">{{models.length}}</span>
                <div class="cursor-pointer flex items-center" :title="'Enabled:\\n' + (config.status.enabled||[]).map(x => '  ' + x).join('\\n')">
                    <svg class="size-4 text-green-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" fill="currentColor"/></svg>
                    <span class="text-green-700">{{(config.status.enabled||[]).length}}</span>
                </div>
                <div class="cursor-pointer flex items-center" :title="'Disabled:\\n' + (config.status.disabled||[]).map(x => '  ' + x).join('\\n')">
                    <svg class="size-4 text-red-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><circle cx="12" cy="12" r="9" fill="currentColor"/></svg>
                    <span class="text-red-700">{{(config.status.disabled||[]).length}}</span>
                </div>
            </button>
            <div v-if="showPopover" ref="popoverRef" class="absolute right-0 mt-2 w-72 max-h-120 overflow-y-auto bg-white border border-gray-200 rounded-md shadow-lg z-10">
                <div class="divide-y divide-gray-100">
                    <div v-for="p in allProviders" :key="p" class="flex items-center justify-between px-3 py-2">
                        <label :for="'chk_' + p" class="cursor-pointer text-sm text-gray-900 truncate mr-2" :title="p">{{ p }}</label>
                        <div @click="onToggle(p, !isEnabled(p))" class="cursor-pointer group relative inline-flex h-5 w-10 shrink-0 items-center justify-center rounded-full outline-offset-2 outline-green-600 has-focus-visible:outline-2">
                            <span class="absolute mx-auto h-4 w-9 rounded-full bg-gray-200 inset-ring inset-ring-gray-900/5 transition-colors duration-200 ease-in-out group-has-checked:bg-green-600" />
                            <span class="absolute left-0 size-5 rounded-full border border-gray-300 bg-white shadow-xs transition-transform duration-200 ease-in-out group-has-checked:translate-x-5" />
                            <input :id="'chk_' + p" type="checkbox" :checked="isEnabled(p)" class="switch cursor-pointer absolute inset-0 appearance-none focus:outline-hidden" aria-label="Use setting" name="setting" />
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `,
    emits: ['updated'],
    setup(props, { emit }) {
        const ai = inject('ai')
        const config = inject('config')
        const models = inject('models')
        const showPopover = ref(false)
        const triggerRef = ref(null)
        const popoverRef = ref(null)
        const pending = ref({})
        const renderKey = ref(0)
        const allProviders = computed(() => config.status?.all)
        const isEnabled = (p) => config.status.enabled.includes(p)
        const togglePopover = () => showPopover.value = !showPopover.value

        const onToggle = async (provider, enable) => {
            pending.value = { ...pending.value, [provider]: true }
            try {
                const res = await ai.post(`/providers/${encodeURIComponent(provider)}`, {
                    body: JSON.stringify(enable ? { enable: true } : { disable: true })
                })
                if (!res.ok) throw new Error(`HTTP ${res.status} ${res.statusText}`)
                const json = await res.json()
                config.status.enabled = json.enabled || []
                config.status.disabled = json.disabled || []
                if (json.feedback) {
                    alert(json.feedback)
                }

                try {
                    const [configRes, modelsRes] = await Promise.all([
                        ai.getConfig(),
                        ai.getModels(),
                    ])
                    const newConfig = await configRes.json()
                    const newModels = await modelsRes.json()
                    Object.assign(config, newConfig)
                    models.length = 0
                    newModels.forEach(m => models.push(m))
                    emit('updated')
                    renderKey.value++
                } catch (e) {
                    alert(`Failed to reload config: ${e.message}`)
                }

            } catch (e) {
                alert(`Failed to ${enable ? 'enable' : 'disable'} ${provider}: ${e.message}`)
            } finally {
                pending.value = { ...pending.value, [provider]: false }
            }
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
            renderKey,
            config,
            models,
            showPopover,
            triggerRef,
            popoverRef,
            allProviders,
            isEnabled,
            togglePopover,
            onToggle,
            pending,
        }
    }
}