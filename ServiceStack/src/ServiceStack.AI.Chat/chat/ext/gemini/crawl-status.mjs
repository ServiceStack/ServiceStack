import { computed } from 'vue'

// Shared by new crawls, editor recrawls, and saved import runs.
export const CrawlStatus = {
    props: { result: Object, progress: Object, loading: Boolean, label: { type: String, default: 'Crawling website…' } },
    template: `<div v-if="loading || result" class="space-y-2" :aria-busy="loading">
        <p v-if="loading" role="status" class="text-sm flex items-center gap-2" :class="[$styles.muted]">
            <svg class="size-4 animate-spin motion-reduce:animate-none shrink-0" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                <circle cx="12" cy="12" r="9" stroke="currentColor" stroke-width="3" opacity="0.25"/>
                <path d="M12 3a9 9 0 0 1 9 9" stroke="currentColor" stroke-width="3" stroke-linecap="round"/>
            </svg>
            <span>{{ label }} This can take several minutes.
                <span v-if="progress" class="block tabular-nums">{{ Number(progress.pages || 0).toLocaleString() }} {{ progress.pages === 1 ? 'page' : 'pages' }} crawled<span v-if="progress.failed"> · {{ Number(progress.failed).toLocaleString() }} {{ progress.failed === 1 ? 'URL' : 'URLs' }} failed</span></span>
            </span>
        </p>
        <p v-if="result" role="status" class="text-sm rounded-md border px-3 py-2"
            :class="result.failed > 0 || (result.remaining > 0 && ['maxPages', 'maxRequests'].includes(result.stopReason))
                ? 'border-amber-500 text-amber-700 dark:text-amber-400 bg-amber-50 dark:bg-amber-900/20'
                : 'border-emerald-500 text-emerald-700 dark:text-emerald-400 bg-emerald-50 dark:bg-emerald-900/20'">{{ message }}</p>
        <details v-if="result?.errors?.length" class="text-sm">
            <summary class="cursor-pointer text-amber-700 dark:text-amber-400">View crawl errors ({{ result.errors.length }})</summary>
            <ul class="mt-2 space-y-2 max-h-64 overflow-y-auto">
                <li v-for="(error, index) in result.errors" :key="index" class="break-all">
                    <span class="font-mono">{{ error.url }}</span>: {{ error.message }}
                </li>
            </ul>
        </details>
</div>`,
    setup(props) {
        const message = computed(() => {
            const result = props.result
            if (!result) return ''
            const count = Number(result.pages || 0)
            let summary = `Crawled ${count.toLocaleString()} ${count === 1 ? 'page' : 'pages'}.`
            if (result.failed) summary += ` ${Number(result.failed).toLocaleString()} ${result.failed === 1 ? 'URL' : 'URLs'} failed.`
            if (result.retainedPages) summary += ` ${Number(result.retainedPages).toLocaleString()} previously crawled ${result.retainedPages === 1 ? 'page' : 'pages'} retained.`
            const limit = { maxPages: 'Max pages', maxRequests: 'Max requests' }[result.stopReason]
            if (!limit || !result.remaining) return summary
            return `${summary} Stopped at the ${limit} limit (${Number(result[result.stopReason]).toLocaleString()})`
                + ` with ${Number(result.remaining).toLocaleString()} queued ${result.remaining === 1 ? 'URL' : 'URLs'} not yet visited.`
                + ` Increase ${limit} to crawl more.`
        })
        return { message }
    },
}

// Poll independently of the long-running crawl request, never overlapping polls.
// Stop guards also ignore an in-flight response after completion or component unmount.
export function pollCrawlProgress(ext, onProgress) {
    const progressId = crypto.randomUUID()
    let stopped = false
    let timer = null
    onProgress({pages:0, failed:0, requests:0})
    async function poll() {
        try {
            const api = await ext.getJson(`/imports/crawl/progress?progressId=${encodeURIComponent(progressId)}`)
            if (!stopped && !api.error && api.response?.running) onProgress(api.response)
        } catch { /* A transient polling error must not interrupt the crawl. */ }
        finally {
            if (!stopped) timer = setTimeout(poll, 2000)
        }
    }
    void poll()
    return { progressId, stop() { stopped = true; clearTimeout(timer) } }
}
