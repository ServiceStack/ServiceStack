import { ref, watch, nextTick, onUnmounted } from 'vue'
import StudioIcon from './StudioIcon.mjs'
import StudioNotice from './StudioNotice.mjs'

export default {
    components: { StudioIcon, StudioNotice },
    props: { draft: Object, api: Function },
    emits: ['close', 'save'],
    setup(props, { emit }) {
        const dialog = ref(null), field = ref(null), name = ref(''), busy = ref(false), warning = ref('')
        let session = 0, edited = false, previousFocus
        watch(() => props.draft, async (draft) => {
            const token = ++session
            if (!draft) {
                dialog.value?.close()
                previousFocus?.focus?.()
                return
            }
            previousFocus = document.activeElement
            name.value = ''
            warning.value = ''
            edited = false
            busy.value = true
            await nextTick()
            if (token !== session) return
            dialog.value?.showModal()
            field.value?.focus()
            try {
                const suggestion = await props.api('/runs/' + encodeURIComponent(draft.run.id) + '/example-name', 'POST', {})
                if (token === session && !edited) name.value = suggestion.label
            } catch (e) {
                if (token === session) {
                    warning.value = e.message || 'Could not suggest a name. Enter a name yourself.'
                    if (!edited) name.value = (draft.recipe.name + ' example').slice(0, 120)
                }
            } finally {
                if (token === session) busy.value = false
            }
        }, { immediate: true })
        onUnmounted(() => { session++ })
        function save() {
            const label = name.value.trim()
            if (label && label.length <= 120) emit('save', label)
        }
        function backdrop(event) {
            if (event.target !== dialog.value) return
            const box = dialog.value.getBoundingClientRect()
            if (event.clientX < box.left || event.clientX > box.right || event.clientY < box.top || event.clientY > box.bottom) emit('close')
        }
        return { dialog, field, name, busy, warning, save, backdrop, edit: () => { edited = true } }
    },
    template: `<dialog ref="dialog" @cancel="$emit('close')" @click="backdrop" aria-labelledby="jev-save-example-title" data-jev-save-example-dialog class="fixed inset-0 m-auto border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 rounded-2xl shadow-xl p-6 max-h-[85vh] overflow-auto w-[min(540px,_calc(100%_-_32px))] text-sm backdrop:bg-[#10162680] backdrop:[backdrop-filter:blur(3px)]">
      <div class="flex items-center justify-between gap-4 mb-4 pr-8"><h2 id="jev-save-example-title" class="text-xl font-semibold">Save run as example</h2><button type="button" aria-label="Close save example" @click="$emit('close')" class="absolute top-2 right-2 grid place-items-center size-8 rounded-lg cursor-pointer text-slate-400 hover:bg-slate-100 dark:hover:bg-gray-800 hover:text-gray-800 dark:hover:text-gray-200 focus-visible:outline-2 focus-visible:outline-indigo-500 pointer-coarse:size-11"><StudioIcon name="close" class="size-5"/></button></div>
      <p class="text-xs text-slate-500 dark:text-slate-400 mb-5 leading-relaxed">Keep this run's original input and actual results. The text summarization model suggests a name using those inputs and results; you can change it.</p>
      <form @submit.prevent="save"><label for="jev-example-name" class="block text-xs font-medium">Example name</label><input id="jev-example-name" ref="field" v-model="name" @input="edit" required maxlength="120" placeholder="Describe this example" class="block w-full mt-2 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 px-3 py-2.5 text-sm max-sm:text-base focus-visible:outline-2 focus-visible:outline-indigo-600"/>
        <p v-if="busy" role="status" class="text-xs text-slate-500 dark:text-slate-400 mt-3">Suggesting a name… You can enter your own while you wait.</p><StudioNotice v-if="warning" tone="warning" :message="warning"/>
        <div class="flex items-center justify-end gap-3 mt-5"><button type="button" @click="$emit('close')" class="border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2 text-xs hover:bg-slate-100 dark:hover:bg-slate-800 focus-visible:outline-2 focus-visible:outline-indigo-600">Cancel</button><button type="submit" :disabled="!name.trim()" class="bg-indigo-600 text-white rounded-lg px-3 py-2 text-xs hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-indigo-600">Save example</button></div>
      </form>
    </dialog>`,
}
