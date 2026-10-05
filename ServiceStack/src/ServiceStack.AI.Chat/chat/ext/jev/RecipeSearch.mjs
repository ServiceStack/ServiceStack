import StudioIcon from './StudioIcon.mjs'

export default {
    components: { StudioIcon },
    props: {
        modelValue: { type: String, default: '' },
        label: { type: String, default: 'Search recipes' },
        placeholder: { type: String, default: 'Find a recipe…' },
    },
    emits: ['update:modelValue', 'search'],
    methods: {
        focus() {
            this.$refs.field.focus()
        },
        clear() {
            this.$emit('update:modelValue', '')
            this.$emit('search')
            this.focus()
        },
    },
    template: `<div role="search" :aria-label="label.replace(/^Search /,'')" data-jev-search class="llms-input-frame flex shrink-0 items-center gap-2 h-10 px-2.5 bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg pointer-coarse:h-11">
      <StudioIcon name="search" class="size-4 shrink-0 text-slate-500 dark:text-slate-400"/>
      <input ref="field" :value="modelValue" @input="$emit('update:modelValue',$event.target.value)" @keydown.enter.prevent="$emit('search')" :aria-label="label" :placeholder="placeholder" type="search" class="llms-input-unframed block flex-1 min-w-0 w-full appearance-none border-0 outline-none shadow-none bg-transparent text-gray-800 dark:text-gray-200 p-0 text-[13px] leading-5 max-[620px]:text-base [&::-webkit-search-cancel-button]:appearance-none [&::-webkit-search-decoration]:appearance-none"/>
      <button type="button" :aria-label="'Clear '+label.toLowerCase()" :disabled="!modelValue" @click="clear" class="grid place-items-center size-6 shrink-0 rounded cursor-pointer outline-none focus:outline-none text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 hover:text-gray-800 dark:hover:text-gray-200 focus-visible:bg-slate-100 dark:focus-visible:bg-slate-800 disabled:opacity-0 disabled:pointer-events-none"><StudioIcon name="close" class="size-3.5"/></button>
    </div>`,
}
