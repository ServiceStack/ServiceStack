// This file is intentionally C#-owned: sync.sh does not replace chat/custom/**.
import { ref } from 'vue'
let ext

export default {
    install(ctx) {
        ext = ctx.scope('custom')
        ctx.pdf.setPreviewActions({
            adminPdf: {
                isVisible: c => ctx.ai.isAdmin && c.entry?.endsWith('.typ') && !/^lib(?:\.preview)?\.typ$/i.test(c.entry),
                component: {
                    props: ['entry', 'buffers', 'rendering', 'save'],
                    template: `
                        <button type="button" @click="publish" :disabled="!entry || rendering || publishing"
                            title="Publish this template to App_Data/pdf"
                            class="inline-flex items-center gap-1.5 px-2.5 py-1 text-xs disabled:opacity-40 mr-1 text-gray-700 bg-white border border-gray-300 hover:bg-gray-50 rounded-md">
                            {{ publishing ? 'Publishing…' : 'Publish' }}
                        </button>`,
                    setup(props) {
                        const publishing = ref(false)
                        const hasUnsavedChanges = () => Object.values(props.buffers || {})
                            .some(x => x.content !== x.saved)
                        const post = async body => {
                            const res = await fetch('/api/AdminPublishPdfTemplate', {
                                method: 'POST', credentials: 'same-origin',
                                headers: Object.assign({ 'Content-Type': 'application/json' }, ctx.ai.headers),
                                body: JSON.stringify(body),
                            })
                            return await ctx.ai.createJsonResult(res)
                        }
                        const publish = async () => {
                            if (!props.entry || publishing.value) return
                            if (hasUnsavedChanges()) {
                                if (!confirm('Save your changes before publishing? Publishing uses the files on disk.')) return
                                await props.save()
                                if (hasUnsavedChanges()) return
                            }
                            publishing.value = true
                            try {
                                let api = await post({ path: props.entry })
                                if (api.error?.errorCode === 'AlreadyExists') {
                                    const owner = api.error.meta || {}
                                    if (confirm(`${owner.name || props.entry} was published by ${owner.user || 'another user'} from ${owner.source || 'another template'}. Overwrite it?`))
                                        api = await post({ path: props.entry, overwrite: true })
                                }
                                if (api.error) return ext.setError(api.error)
                                ext.toast(`Published ${api.response.template.name}`)
                                if (api.response.libUpdated) ext.toast('lib.typ was updated and may affect other published templates')
                            } catch (e) {
                                ext.setError(ctx.ai.createErrorStatus({ message: e.message || String(e) }))
                            } finally {
                                publishing.value = false
                            }
                        }
                        return { publishing, publish }
                    },
                }
            }
        })
    },
}
