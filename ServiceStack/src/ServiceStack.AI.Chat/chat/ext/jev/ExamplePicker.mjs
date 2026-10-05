import { ref, computed, nextTick, onMounted, onUnmounted } from 'vue'
import StudioIcon from './StudioIcon.mjs'

let sequence = 0

export default {
    components: { StudioIcon },
    props: { examples: Array, selectedId: String },
    emits: ['select'],
    setup(props, { emit }) {
        const anchor = ref(null),
            trigger = ref(null),
            popup = ref(null),
            open = ref(false),
            position = ref({}),
            id = 'jev-example-picker-' + ++sequence
        const selectedIndex = computed(() => props.examples.findIndex(e => e.id === props.selectedId))
        const nextExample = computed(() => props.examples[(selectedIndex.value + 1) % props.examples.length])

        function place() {
            if (!anchor.value) return
            const rect = anchor.value.getBoundingClientRect(),
                width = Math.min(320, innerWidth - 32),
                top = rect.bottom + 6
            position.value = {
                left: Math.max(16, Math.min(rect.right - width, innerWidth - width - 16)) + 'px',
                top: top + 'px',
                width: width + 'px',
                maxHeight: Math.max(0, Math.min(320, innerHeight - top - 16)) + 'px',
            }
        }
        async function show(focus = false) {
            place()
            if (!popup.value.matches(':popover-open')) popup.value.showPopover()
            open.value = true
            if (focus) {
                await nextTick()
                if (!popup.value?.matches(':popover-open')) return
                const options = popup.value.querySelectorAll('button')
                options[Math.max(0, selectedIndex.value)]?.focus()
            }
        }
        function close(restoreFocus = false) {
            if (popup.value?.matches(':popover-open')) popup.value.hidePopover()
            open.value = false
            if (restoreFocus) trigger.value?.focus()
        }
        function cycle() {
            close()
            if (nextExample.value) emit('select', nextExample.value)
        }
        function choose(example) {
            emit('select', example)
            close(true)
        }
        function toggle() {
            if (open.value) close()
            else show(true)
        }
        function navigate(event) {
            const options = Array.from(popup.value.querySelectorAll('button')),
                index = options.indexOf(document.activeElement)
            let next
            if (event.key === 'ArrowDown') next = (index + 1) % options.length
            else if (event.key === 'ArrowUp') next = (index - 1 + options.length) % options.length
            else if (event.key === 'Home') next = 0
            else if (event.key === 'End') next = options.length - 1
            else return
            event.preventDefault()
            options[next]?.focus()
        }
        function focusOut(event) {
            if (open.value && !anchor.value.contains(event.relatedTarget)) close()
        }
        function reposition() {
            if (!open.value) return
            const rect = anchor.value.getBoundingClientRect()
            if (rect.bottom < 0 || rect.top > innerHeight) close()
            else place()
        }
        onMounted(() => {
            window.addEventListener('resize', reposition)
            window.addEventListener('scroll', reposition, true)
        })
        onUnmounted(() => {
            close()
            window.removeEventListener('resize', reposition)
            window.removeEventListener('scroll', reposition, true)
        })
        return { anchor, trigger, popup, open, position, id, nextExample, show, close, cycle, choose, toggle, navigate, focusOut }
    },
    template: `<div ref="anchor" data-jev-example-picker @focusout="focusOut" @keydown.esc.prevent.stop="close(true)" class="inline-flex shrink-0 rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900">
      <button ref="trigger" type="button" @click="cycle" :title="nextExample?'Try '+nextExample.label:undefined" data-jev-button class="cursor-pointer rounded-l-lg px-3 py-2 text-xs font-medium outline-none focus:outline-none hover:bg-slate-100 dark:hover:bg-slate-800 focus-visible:bg-slate-100 dark:focus-visible:bg-slate-800 pointer-coarse:min-h-11">Try example</button>
      <button type="button" aria-label="Choose example" @click="toggle" @keydown.down.prevent="show(true)" :aria-expanded="open" :aria-controls="id" class="grid place-items-center cursor-pointer rounded-r-lg border-l border-gray-200 dark:border-gray-700 px-2 outline-none focus:outline-none hover:bg-slate-100 dark:hover:bg-slate-800 focus-visible:bg-slate-100 dark:focus-visible:bg-slate-800 pointer-coarse:min-w-11"><StudioIcon name="chevron" class="size-3.5"/></button>
      <div ref="popup" :id="id" popover="auto" role="group" aria-label="Choose an example" data-jev-example-popover :style="position" @toggle="open=$event.newState==='open'" @keydown="navigate" class="fixed m-0 p-1.5 overflow-y-auto overscroll-contain rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 shadow-lg">
        <button v-for="(example,index) in examples" :key="example.id" type="button" :aria-pressed="selectedId===example.id" :data-jev-example-option="example.id" @click="choose(example)" class="flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-left text-xs cursor-pointer outline-none focus:outline-none hover:bg-slate-100 dark:hover:bg-gray-800 focus-visible:bg-slate-100 dark:focus-visible:bg-gray-800 aria-pressed:bg-indigo-50 dark:aria-pressed:bg-indigo-950/50 aria-pressed:text-indigo-700 dark:aria-pressed:text-indigo-300 pointer-coarse:min-h-11"><span class="min-w-0 flex-1 wrap-anywhere">{{example.label||'Example '+(index+1)}}</span><StudioIcon v-if="selectedId===example.id" name="check" class="size-3.5"/></button>
      </div>
    </div>`,
}
