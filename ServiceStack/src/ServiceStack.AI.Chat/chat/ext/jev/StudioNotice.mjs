import StudioIcon from './StudioIcon.mjs'

export default {
    components: { StudioIcon },
    props: {
        message: String,
        tone: { type: String, default: 'info' },
        dismissible: Boolean,
    },
    emits: ['dismiss'],
    computed: {
        icon() {
            return this.tone === 'success' ? 'check' : ['error', 'warning'].includes(this.tone) ? 'warning' : 'info'
        },
    },
    template: `<div data-jev-message :data-jev-alert="tone === 'error' || undefined" :data-jev-notice="tone !== 'error' || undefined" :data-tone="tone" class="grid grid-cols-[20px_minmax(0,1fr)_auto] items-start gap-3 border rounded-[10px] px-4 py-3.5 my-3 text-[13px] leading-[1.6] text-gray-800 dark:text-gray-200 wrap-anywhere max-sm:px-3.5 max-sm:py-3 max-sm:gap-2.5" :class="{ 'bg-slate-50 border-gray-200 dark:bg-gray-800 dark:border-gray-700': tone === 'info', 'bg-amber-50 border-amber-200 dark:bg-amber-950/30 dark:border-amber-800': tone === 'warning', 'bg-red-50 border-red-200 dark:bg-red-950/30 dark:border-red-800': tone === 'error', 'bg-green-50 border-green-200 dark:bg-green-950/30 dark:border-green-800': tone === 'success' }" :role="tone==='error'?'alert':'status'">
      <StudioIcon :name="icon" data-jev-message-icon class="size-5 shrink-0 mt-px" :class="{ 'text-slate-500 dark:text-slate-400': tone === 'info', 'text-amber-700 dark:text-amber-300': tone === 'warning', 'text-red-700 dark:text-red-300': tone === 'error', 'text-green-700 dark:text-green-300': tone === 'success' }"/>
      <div data-jev-message-body class="min-w-0"><slot>{{message}}</slot></div>
      <div v-if="$slots.actions||dismissible" data-jev-message-actions class="flex items-center gap-2.5 max-sm:has-[button:not([aria-label])]:col-[2/-1] max-sm:flex-wrap [&_a]:font-medium [&_a]:text-inherit"><slot name="actions"/><button v-if="dismissible" type="button" :aria-label="tone==='error'?'Dismiss error':'Dismiss notice'" @click="$emit('dismiss')" data-jev-message-dismiss class="inline-flex items-center justify-center size-6 rounded-md p-0.5 text-slate-500 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 hover:text-gray-800 dark:hover:text-gray-200 focus-visible:outline-2 focus-visible:outline-indigo-600 focus-visible:outline-offset-3 max-sm:size-8 pointer-coarse:size-11"><StudioIcon name="close" class="size-4"/></button></div>
    </div>`,
}
