import { ref, watch, nextTick, onMounted, onUnmounted } from 'vue'

export default {
    props: ['menu'],
    emits: ['close', 'action'],
    setup(props, { emit }) {
        const element = ref(null), position = ref({}), submenu = ref(null), submenuElement = ref(null), submenuPosition = ref({}), submenuOrigin = ref(null), overlay = ref(false)
        function close(restore = false) {
            if (!props.menu) return
            if (restore) props.menu.origin?.focus()
            emit('close')
        }
        function buttons(scope = element.value) { return [...(scope?.querySelectorAll('[role="menuitem"]:not(:disabled)') || [])].filter(item => item.closest('[role="menu"]') === scope) }
        function keydown(event) {
            const scope = event.target.closest('[role="menu"]'), items = buttons(scope), index = items.indexOf(document.activeElement)
            if (event.key === 'ArrowLeft' && scope === submenuElement.value) { event.preventDefault(); back(); return }
            if (event.key === 'ArrowRight' && scope === element.value) {
                const item = props.menu.items.find(item => item.action === document.activeElement?.dataset.action)
                if (item?.children) { event.preventDefault(); openSubmenu(item, document.activeElement, true) }
                return
            }
            if (event.key === 'Escape') { event.preventDefault(); event.stopPropagation(); close(true) }
            else if (event.key === 'Tab') close()
            else if (['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)) {
                event.preventDefault()
                const next = event.key === 'Home' ? 0 : event.key === 'End' ? items.length - 1 :
                    (index + (event.key === 'ArrowDown' ? 1 : -1) + items.length) % items.length
                items[next]?.focus()
            }
        }
        watch(() => props.menu, async menu => {
            submenu.value = null
            if (!menu) return
            position.value = { left: menu.x + 'px', top: menu.y + 'px', visibility: 'hidden' }
            await nextTick()
            if (props.menu !== menu || !element.value) return
            const rect = element.value.getBoundingClientRect()
            position.value = { left: Math.max(8, Math.min(menu.x, window.innerWidth - rect.width - 8)) + 'px',
                top: Math.max(8, Math.min(menu.y, window.innerHeight - rect.height - 8)) + 'px' }
            await nextTick()
            if (props.menu === menu) buttons()[0]?.focus()
        }, { immediate: true })
        function outside(event) { if (!element.value?.contains(event.target)) close() }
        function reposition() { close() }
        function scroll(event) { if (!element.value?.contains(event.target)) close() }
        function focusout(event) { if (!element.value?.contains(event.relatedTarget)) close() }
        async function openSubmenu(item, origin, focus = false) {
            if (item.disabled) return
            if (submenu.value === item) { if (focus) buttons(submenuElement.value)[0]?.focus(); return }
            submenu.value = item; submenuOrigin.value = origin
            submenuPosition.value = { left: '0px', top: '0px', visibility: 'hidden' }
            await nextTick()
            if (submenu.value !== item || !submenuElement.value) return
            const anchor = origin.getBoundingClientRect(), parent = element.value.getBoundingClientRect(), rect = submenuElement.value.getBoundingClientRect()
            overlay.value = parent.right + rect.width > window.innerWidth - 8 && parent.left - rect.width < 8
            const left = overlay.value ? parent.left : parent.right + rect.width <= window.innerWidth - 8 ? parent.right - 4 : parent.left - rect.width + 4
            submenuPosition.value = { left: Math.max(8, left) + 'px', top: Math.max(8, Math.min(anchor.top, window.innerHeight - rect.height - 8)) + 'px' }
            await nextTick()
            if (focus && submenu.value === item) buttons(submenuElement.value)[0]?.focus()
        }
        function back() { submenu.value = null; submenuOrigin.value?.focus() }
        function hover(item, event) { if (item.children) openSubmenu(item, event.currentTarget); else submenu.value = null }
        function action(item, event) {
            if (item.children) { openSubmenu(item, event.currentTarget, true); return }
            const target = props.menu.target; close(true); emit('action', { action: item.action, target })
        }
        onMounted(() => {
            document.addEventListener('pointerdown', outside)
            window.addEventListener('resize', reposition)
            window.addEventListener('scroll', scroll, true)
        })
        onUnmounted(() => {
            document.removeEventListener('pointerdown', outside)
            window.removeEventListener('resize', reposition)
            window.removeEventListener('scroll', scroll, true)
        })
        return { element, position, keydown, action, focusout, submenu, submenuElement, submenuPosition, overlay, back, hover }
    },
    template: `<Teleport to="body"><div v-if="menu" ref="element" role="menu" :aria-label="menu.label" @keydown="keydown" @focusout="focusout" @contextmenu.prevent :style="position"  class="w-57.5 max-w-[calc(100vw-16px)] max-h-[calc(100vh-16px)] overflow-y-auto fixed z-[300] rounded-md border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 shadow-xl p-1">
        <template v-for="item in menu.items" :key="item.action">
            <div v-if="item.separator" role="separator" class="border-t border-gray-200 dark:border-gray-700 my-1"></div>
            <button type="button" role="menuitem" :disabled="item.disabled" :title="item.title" :data-action="item.action" :aria-haspopup="item.children ? 'menu' : undefined" :aria-expanded="item.children ? submenu === item : undefined" @pointerenter="hover(item, $event)" @click="action(item, $event)"  :class="item.danger ? 'text-red-600 dark:text-red-400' : ''"  class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-[var(--ring)] focus-visible:-outline-offset-0.5 w-full rounded px-3 py-1 text-left text-sm disabled:opacity-40 disabled:cursor-not-allowed hover:bg-gray-100 dark:hover:bg-gray-800 focus:bg-gray-100 dark:focus:bg-gray-800">{{item.label}}<span v-if="item.children" class="float-right" aria-hidden="true">›</span></button>
        </template>
        <div v-if="submenu" ref="submenuElement" role="menu" :aria-label="submenu.label + ' actions'"  :style="submenuPosition"  class="w-57.5 max-w-[calc(100vw-16px)] max-h-[calc(100vh-16px)] overflow-y-auto fixed rounded-md border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 shadow-xl p-1">
            <button v-if="overlay" type="button" @click="back" aria-label="Back to repository actions"   class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-[var(--ring)] focus-visible:-outline-offset-0.5 w-full rounded px-3 py-1 text-left text-sm">‹ {{submenu.label}}</button>
            <template v-for="item in submenu.children" :key="item.action">
                <div v-if="item.separator" role="separator" class="border-t border-gray-200 dark:border-gray-700 my-1"></div>
                <button type="button" role="menuitem" :disabled="item.disabled" :title="item.title" @click="action(item, $event)"  :class="item.danger ? 'text-red-600 dark:text-red-400' : ''"  class="focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-[var(--ring)] focus-visible:-outline-offset-0.5 w-full rounded px-3 py-1 text-left text-sm disabled:opacity-40 disabled:cursor-not-allowed hover:bg-gray-100 dark:hover:bg-gray-800 focus:bg-gray-100 dark:focus:bg-gray-800">{{item.label}}</button>
            </template>
        </div>
    </div></Teleport>`
}
