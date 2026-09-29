import { computed } from 'vue'

export const CheckBox = {
    template: `
        <input type="checkbox" :checked="modelValue" @change="$emit('update:modelValue', $event.target.checked)"
            class="appearance-none size-4 shrink-0 rounded border transition-colors cursor-pointer bg-no-repeat bg-center
                   border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900
                   checked:bg-blue-600 checked:border-blue-600
                   focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500/40"
            :style="modelValue ? checkedStyle : null">
    `,
    props: { modelValue: Boolean },
    emits: ['update:modelValue'],
    setup(props) {
        // The tick as a data URI rather than a ::after glyph: it scales crisply and needs no font.
        const TICK = "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 20' "
            + "fill='white'%3E%3Cpath fill-rule='evenodd' d='M16.7 5.3a1 1 0 0 1 0 1.4l-7.5 7.5a1 1 0 0 1-1.4 0"
            + "L3.3 9.7a1 1 0 0 1 1.4-1.4l3.8 3.8 6.8-6.8a1 1 0 0 1 1.4 0z' clip-rule='evenodd'/%3E%3C/svg%3E"
        // Keep the checked fill inline with the tick. In light mode the generated bg-white
        // utility can otherwise win the cascade over checked:bg-blue-600, leaving a white tick
        // on a white box. The checked utility classes remain for hover/theme consistency.
        const checkedStyle = computed(() => ({
            backgroundColor: '#2563eb',
            backgroundImage: `url("${TICK}")`,
        }))
        return { checkedStyle }
    },
}
