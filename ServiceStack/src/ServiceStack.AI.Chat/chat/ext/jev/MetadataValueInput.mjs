import { ref, computed, watch } from 'vue'
import StudioIcon from './StudioIcon.mjs'
import { uid } from './recipeModel.mjs'
import { normalizeTags, tagKey } from './decisionTags.mjs'

// Search, select, or create a value; tags commit on Enter, comma, or blur like Gemini metadata.
export default {
    components: { StudioIcon },
    props: {
        label: String,
        values: Array,
        suggestions: Array,
        max: Number,
        replace: Boolean,
        hint: String,
    },
    emits: ['change'],
    setup(props, { emit }) {
        const id = 'jev-metadata-' + uid()
        const text = ref(''),
            open = ref(false),
            active = ref(-1),
            error = ref('')
        const norm = (value) => tagKey(String(value))
        const full = computed(
            () => !props.replace && (props.values?.length || 0) >= props.max,
        )
        const exact = computed(() =>
            (props.suggestions || []).find(
                (value) =>
                    norm(value.label) === norm(text.value),
            ),
        )
        const options = computed(() => {
            const query = text.value.trim().toLowerCase()
            const matches = (props.suggestions || [])
                .filter(
                    (value) =>
                        !(props.values || []).some(v => norm(v) === norm(value.label)) &&
                        (!query ||
                            value.label.toLowerCase().includes(query)),
                )
                .slice(0, 30)
                .map((value) => ({ ...value, create: false }))
            if (text.value.trim() && !exact.value && !text.value.includes(','))
                matches.push({
                    label: text.value.trim().replace(/\s+/g, ' '),
                    create: true,
                })
            return matches
        })
        watch(text, () => {
            active.value = -1
        })
        watch(full, () => {
            if (full.value) open.value = false
        })
        function commit(raw = text.value) {
            const values = normalizeTags(raw).map(
                (value) =>
                    (props.suggestions || []).find(
                        (s) =>
                            norm(s.label) === norm(value),
                    )?.label || value,
            )
            if (!values.length) return
            if (values.some((value) => value.length > 40)) {
                error.value = 'Use values of up to 40 characters.'
                return
            }
            if (props.replace && values.length !== 1) {
                error.value = 'Choose one content type.'
                return
            }
            const next = props.replace
                ? values
                : normalizeTags([...(props.values || []), ...values].join(','))
            if (next.length > props.max) {
                error.value = `Use up to ${props.max} tags. Remove one before adding another.`
                return
            }
            error.value = ''
            emit('change', next)
            text.value = ''
            open.value = false
            active.value = -1
        }
        function onInput() {
            error.value = ''
            open.value = true
            if (!props.replace && text.value.includes(',')) {
                const parts = text.value.split(','),
                    rest = parts.pop()
                commit(parts.join(','))
                if (!error.value) text.value = rest
            }
        }
        function onKey(event) {
            if (event.isComposing) return
            if (event.key === 'Escape') {
                open.value = false
                return
            }
            if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
                event.preventDefault()
                open.value = true
                const count = options.value.length
                if (count)
                    active.value =
                        (active.value +
                            (event.key === 'ArrowDown' ? 1 : -1) +
                            count) %
                        count
            }
            if (event.key === 'Enter') {
                event.preventDefault()
                if (open.value && active.value >= 0)
                    commit(options.value[active.value]?.label)
                else commit(exact.value?.label || text.value)
            }
            if (
                event.key === 'Backspace' &&
                !text.value &&
                props.values?.length
            )
                emit('change', props.values.slice(0, -1))
        }
        function blur() {
            commit()
            open.value = false
        }
        function remove(value) {
            error.value = ''
            emit(
                'change',
                (props.values || []).filter((v) => v !== value),
            )
        }
        return {
            id,
            text,
            open,
            active,
            error,
            full,
            options,
            commit,
            onInput,
            onKey,
            blur,
            remove,
        }
    },
    template: `<div data-jev-metadata-input class="min-w-0">
      <label :for="id" class="block text-xs font-medium mb-1.5 text-gray-800 dark:text-gray-200">{{label}}</label>
      <div class="relative">
        <input :id="id" v-model="text" :aria-label="'Search or create '+label.toLowerCase()" role="combobox" aria-autocomplete="list" :aria-expanded="open&&!full" :aria-controls="id+'-options'" :aria-activedescendant="open&&active>=0?id+'-option-'+active:undefined" :aria-describedby="id+'-hint'" autocomplete="off" :disabled="full" :placeholder="full?'3 tags selected':'Search or create…'" @input="onInput" @focus="open=true" @blur="blur" @keydown="onKey" class="block w-full min-w-0 bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 border border-gray-300 dark:border-gray-600 rounded-md px-2.5 py-2 text-sm disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-indigo-500"/>
        <div v-if="open&&!full" :id="id+'-options'" role="listbox" :aria-label="label+' suggestions'" class="absolute z-30 left-0 right-0 mt-1 max-h-52 overflow-auto rounded-md border border-gray-200 dark:border-gray-700 shadow-lg bg-white dark:bg-gray-800 p-1">
          <button v-for="(option,index) in options" :key="option.label" :id="id+'-option-'+index" type="button" role="option" :aria-selected="active===index" @pointerdown.prevent @click="commit(option.label)" class="w-full cursor-pointer px-2.5 py-2 rounded text-left text-sm wrap-anywhere hover:bg-slate-100 dark:hover:bg-gray-700 aria-selected:bg-indigo-50 dark:aria-selected:bg-indigo-950/50" :class="option.create?'text-amber-700 dark:text-amber-300':'text-gray-800 dark:text-gray-200'">{{option.create?'Create “'+option.label+'”':option.label}}</button>
          <p v-if="!options.length" class="px-2.5 py-2 text-xs text-slate-500 dark:text-slate-400">Type a new value to create it.</p>
        </div>
      </div>
      <div v-if="values?.length" class="flex flex-wrap gap-1.5 mt-2">
        <span v-for="value in values" :key="value" data-jev-metadata-chip class="inline-flex items-center gap-1 pl-2 pr-1 py-1 rounded border text-xs text-emerald-700 dark:text-emerald-300 border-emerald-500/40 bg-emerald-50 dark:bg-emerald-950/30"><span class="wrap-anywhere">{{value}}</span><button type="button" :aria-label="'Remove '+label.toLowerCase()+' '+value" @click="remove(value)" class="cursor-pointer rounded p-0.5 hover:bg-emerald-100 dark:hover:bg-emerald-900 focus-visible:outline-2 focus-visible:outline-indigo-500"><StudioIcon name="close" class="size-3.5"/></button></span>
      </div>
      <p :id="id+'-hint'" class="mt-1.5 text-xs leading-relaxed" :class="error?'text-red-600 dark:text-red-300':'text-slate-500 dark:text-slate-400'" :role="error?'alert':undefined">{{error||hint}}</p>
    </div>`,
}
