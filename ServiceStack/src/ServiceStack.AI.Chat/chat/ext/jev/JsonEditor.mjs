import { ref, inject, onMounted, onUnmounted, watch } from 'vue'
import { loadCodeEditor } from '/ui/lazy.mjs'

export default {
    props: { modelValue: String, readonly: Boolean, label: { type: String, default: 'Recipe JSON' } },
    emits: ['update:modelValue'],
    template: `<div data-jev-json-editor class="[&_.CodeMirror]:h-[440px] [&_.CodeMirror]:text-xs [&_.CodeMirror]:border [&_.CodeMirror]:border-gray-200 dark:[&_.CodeMirror]:border-gray-700 [&_.CodeMirror]:rounded-lg mt-4.5 min-w-0"><textarea ref="textarea" :aria-label="label" :value="modelValue" :readonly="readonly" @input="$emit('update:modelValue',$event.target.value)" spellcheck="false"  class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 block w-full min-w-0 mt-1.5 bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 border border-gray-200 dark:border-gray-700 rounded-[7px] py-[9px] px-2.5 font-mono text-[12px] leading-[1.8] text-[13px] font-normal resize-y min-h-110 [tab-size:2] max-[620px]:text-base"></textarea></div>`,
    setup(props, { emit }) {
        const ctx = inject('ctx'),
            textarea = ref(null)
        let cm,
            disposed = false,
            observer
        const theme = () => (document.documentElement.classList.contains('dark') ? 'mocha' : 'default')
        onMounted(async () => {
            try {
                await loadCodeEditor(ctx)
            } catch {
                /* A usable native editor is always available. */
            }
            if (disposed || !globalThis.CodeMirror || !textarea.value) return
            cm = CodeMirror.fromTextArea(textarea.value, {
                mode: { name: 'javascript', json: true },
                lineNumbers: true,
                lineWrapping: true,
                matchBrackets: true,
                theme: theme(),
                readOnly: props.readonly || false,
            })
            cm.on('change', () => {
                const value = cm.getValue()
                if (value !== props.modelValue) emit('update:modelValue', value)
            })
            observer = new MutationObserver(() => cm?.setOption('theme', theme()))
            observer.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] })
        })
        watch(
            () => props.modelValue,
            (value) => {
                if (cm && value !== cm.getValue()) cm.setValue(value || '')
            },
        )
        onUnmounted(() => {
            disposed = true
            observer?.disconnect()
            cm?.toTextArea()
            cm = null
        })
        return { textarea }
    },
}
