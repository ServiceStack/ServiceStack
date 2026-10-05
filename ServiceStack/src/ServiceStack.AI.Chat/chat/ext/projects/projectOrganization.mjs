import { computed, nextTick, onUnmounted, ref } from 'vue'

export function useProjectOrganization(ctx, projects) {
    const api = ctx.scope('projects')
    const activeProjects = computed(() => projects.value.filter(p => !p.archived))
    const archivedProjects = computed(() => projects.value.filter(p => p.archived))
    const reorderMode = ref(false), savingOrder = ref(false), organizationError = ref(''), orderStatus = ref('')
    const projectRows = ref(null), draggingId = ref(null), dropIndex = ref(null), dragPreview = ref(null)
    let gesture, frame, handle, pointerY

    function updateProjects(result) {
        projects.value = result
        ctx.setState({ projects: result })
    }
    function toggleReorder() {
        cancelDrag()
        reorderMode.value = !reorderMode.value
        organizationError.value = ''; orderStatus.value = ''
    }
    async function persistOrder(ids) {
        if (savingOrder.value) return
        const previous = projects.value
        const byId = new Map(activeProjects.value.map(p => [p.id, p]))
        if (ids.some(id => !byId.has(id))) return
        projects.value = [...ids.map(id => byId.get(id)), ...archivedProjects.value]
        savingOrder.value = true; organizationError.value = ''; orderStatus.value = 'Saving order…'
        try {
            const result = await api.postJson('/order', { ids })
            if (!result.response) throw new Error(result.error?.message || 'Unable to save project order.')
            updateProjects(result.response)
            orderStatus.value = 'Order saved'
        } catch (error) {
            projects.value = previous
            organizationError.value = error.message
            orderStatus.value = ''
            const current = await api.getJson('/projects.json').catch(() => null)
            if (current?.response) updateProjects(current.response)
        } finally { savingOrder.value = false }
    }
    async function moveProject(id, direction) {
        if (savingOrder.value) return
        const ids = activeProjects.value.map(p => p.id), index = ids.indexOf(id), target = index + direction
        if (index < 0 || target < 0 || target >= ids.length) return
        ids.splice(index, 1); ids.splice(target, 0, id)
        await persistOrder(ids)
        await nextTick()
        projectRows.value?.querySelectorAll('[data-project-drag-handle]').forEach(el => {
            if (el.dataset.id === id) el.focus()
        })
    }
    function updateDrop(x, y) {
        const list = projectRows.value, rect = list.getBoundingClientRect()
        gesture.allowed = x >= rect.left && x <= rect.right && y >= rect.top && y <= rect.bottom
        if (!gesture.allowed) return
        const center = y - gesture.offsetY + gesture.height / 2
        const scrolled = list.scrollTop - gesture.scrollTop
        // Compare against the original slots, so animated rows don't change the
        // hit testing or oscillate back and forth beneath the pointer.
        dropIndex.value = gesture.slots.filter((slot, index) => index !== gesture.index
            && center > slot.top + slot.height / 2 - scrolled).length
    }
    function rowStyle(id) {
        if (!draggingId.value || !gesture) return
        const index = gesture.slots.findIndex(slot => slot.id === id), from = gesture.index, to = dropIndex.value
        if (index < 0 || to == null) return
        let destination = index
        if (index === from) destination = to
        else if (from < to && index > from && index <= to) destination = index - 1
        else if (from > to && index >= to && index < from) destination = index + 1
        return { transform: `translateY(${gesture.slots[destination].top - gesture.slots[index].top}px)` }
    }
    function scroll() {
        if (!gesture || !draggingId.value) return
        const list = projectRows.value, rect = list.getBoundingClientRect()
        if (pointerY < rect.top + 30) list.scrollTop -= 7
        else if (pointerY > rect.bottom - 30) list.scrollTop += 7
        updateDrop(gesture.x, pointerY)
        frame = requestAnimationFrame(scroll)
    }
    function startDrag(event, id) {
        if (!reorderMode.value || savingOrder.value || gesture || event.button !== 0) return
        event.preventDefault()
        handle = event.currentTarget
        const control = event.target.closest('button') || handle.querySelector('button')
        control?.focus()
        const rect = handle.getBoundingClientRect()
        const slots = [...projectRows.value.querySelectorAll('[data-project-id]')].map(row => {
            const bounds = row.getBoundingClientRect()
            return { id: row.dataset.projectId, top: bounds.top, height: bounds.height }
        })
        gesture = { id, pointer: event.pointerId, startX: event.clientX, startY: event.clientY, x: event.clientX,
            offsetX: event.clientX - rect.left, offsetY: event.clientY - rect.top, height: rect.height, width: rect.width,
            slots, index: slots.findIndex(slot => slot.id === id), scrollTop: projectRows.value.scrollTop, allowed: true,
            project: activeProjects.value.find(p => p.id === id) }
        handle.setPointerCapture(event.pointerId)
    }
    function drag(event) {
        if (!gesture || event.pointerId !== gesture.pointer) return
        gesture.x = event.clientX; pointerY = event.clientY
        if (!draggingId.value && Math.hypot(event.clientX - gesture.startX, event.clientY - gesture.startY) < 5) return
        if (!draggingId.value) {
            draggingId.value = gesture.id; dropIndex.value = gesture.index
            frame = requestAnimationFrame(scroll)
        }
        dragPreview.value = { project: gesture.project,
            style: { width: gesture.width + 'px', height: gesture.height + 'px',
                transform: `translate3d(${event.clientX - gesture.offsetX}px, ${event.clientY - gesture.offsetY}px, 0)` } }
        updateDrop(event.clientX, event.clientY)
    }
    function cancelDrag() {
        cancelAnimationFrame(frame)
        const pointer = gesture?.pointer, element = handle
        gesture = null; handle = null; draggingId.value = null; dropIndex.value = null; dragPreview.value = null
        if (element?.hasPointerCapture(pointer)) element.releasePointerCapture(pointer)
    }
    async function endDrag(event) {
        if (!gesture || gesture.pointer !== event.pointerId) return
        if (draggingId.value) updateDrop(event.clientX, event.clientY)
        const from = draggingId.value, index = dropIndex.value, allowed = gesture.allowed
        cancelDrag()
        if (!from || index == null || !allowed) return
        const current = activeProjects.value.map(p => p.id)
        const ids = current.filter(id => id !== from)
        ids.splice(index, 0, from)
        if (ids.some((id, index) => id !== current[index])) await persistOrder(ids)
    }
    onUnmounted(cancelDrag)
    return { activeProjects, archivedProjects, reorderMode, savingOrder, organizationError, orderStatus,
        projectRows, draggingId, dragPreview, rowStyle, updateProjects, toggleReorder, moveProject, startDrag, drag, endDrag, cancelDrag }
}
