import { computed, inject, ref, watch } from 'vue'

export const ShareIcon = {
    template: `<svg @click="$ctx.toggleTop('SharePanel')" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path d="M0 0h24v24H0z" fill="none"/><path fill="currentColor" d="m11 11.85l-1.875 1.875q-.3.3-.712.288T7.7 13.7q-.275-.3-.288-.7t.288-.7l3.6-3.6q.15-.15.325-.212T12 8.425t.375.063t.325.212l3.6 3.6q.3.3.288.7t-.288.7q-.3.3-.712.313t-.713-.288L13 11.85V19q0 .425-.288.713T12 20t-.712-.288T11 19zM4 8V6q0-.825.588-1.412T6 4h12q.825 0 1.413.588T20 6v2q0 .425-.288.713T19 9t-.712-.288T18 8V6H6v2q0 .425-.288.713T5 9t-.712-.288T4 8"/></svg>`,
}

export default {
    template: `
    <div class="relative px-4 py-3 overflow-y-auto border-b" :class="$styles.panel">
        <button type="button" @click="$ctx.toggleTop('SharePanel', false)" aria-label="Close sharing panel" title="Close"
                class="absolute top-2 right-4 p-1.5 rounded text-gray-400 hover:text-gray-600 focus-visible:outline-2 focus-visible:outline-blue-500 focus-visible:outline-offset-2">
            <svg class="size-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true"><path stroke-linecap="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
        </button>
        <div class="max-w-4xl mx-auto pt-8">
            <div v-if="options.length" class="flex border-b mb-6" :class="$styles.chromeBorder" role="tablist" aria-label="Sharing destination">
                <button v-for="option in options" :key="option.id" type="button" role="tab"
                        :aria-selected="selected?.id === option.id" :tabindex="selected?.id === option.id ? 0 : -1"
                        @click="activeId = option.id" @keydown="moveTab($event, option.id)"
                        class="px-4 py-2.5 text-sm font-semibold border-b-2 -mb-px"
                        :class="selected?.id === option.id ? 'border-blue-500 text-blue-600 dark:text-blue-400' : 'border-transparent text-gray-500'">
                    {{ option.name }}
                </button>
            </div>
            <component v-if="selected" :is="selected.component" :key="selected.id" v-bind="selected.props || {}"/>
        </div>
    </div>`,
    setup() {
        const ctx = inject('ctx')
        const activeId = ref(null)
        const options = computed(() => Object.values(ctx.visibleComponents(ctx.shareOptions, ctx))
            .sort((a, b) => (a.order ?? 100) - (b.order ?? 100)))
        const selected = computed(() => options.value.find(o => o.id === activeId.value) || options.value[0])
        watch(options, list => {
            if (!list.length) ctx.toggleTop('SharePanel', false)
        }, { immediate: true })
        const moveTab = (event, id) => {
            const keys = ['ArrowLeft', 'ArrowRight', 'Home', 'End']
            if (!keys.includes(event.key)) return
            event.preventDefault()
            const index = options.value.findIndex(o => o.id === id)
            const next = event.key === 'Home' ? 0 : event.key === 'End' ? options.value.length - 1
                : (index + (event.key === 'ArrowRight' ? 1 : -1) + options.value.length) % options.value.length
            activeId.value = options.value[next].id
            event.currentTarget.parentElement.children[next].focus()
        }
        return { options, selected, activeId, moveTab }
    },
}
