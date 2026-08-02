/**
 * Renders compiled PDF bytes to <canvas> using the vendored pdf.js (ESM build).
 */

let pdfjsLib = null

export async function loadPdfjs(baseUrl) {
    if (!pdfjsLib) {
        pdfjsLib = await import(`${baseUrl}/pdfjs/pdf.min.mjs`)
        pdfjsLib.GlobalWorkerOptions.workerSrc = `${baseUrl}/pdfjs/pdf.worker.min.mjs`
    }
    return pdfjsLib
}

export class PdfView {
    constructor(baseUrl) {
        this.baseUrl = baseUrl
        this.doc = null
        this.loadingTask = null
        this.tasks = new Map()
    }

    /** Load PDF bytes, keeping the previous document alive until the new one is ready */
    async load(bytes) {
        const lib = await loadPdfjs(this.baseUrl)
        const loadingTask = lib.getDocument({ data: bytes })
        const doc = await loadingTask.promise
        const previous = this.loadingTask
        this.doc = doc
        this.loadingTask = loadingTask
        if (previous) {
            this.cancelAll()
            previous.destroy().catch(() => {})
        }
        return doc.numPages
    }

    get numPages() {
        return this.doc?.numPages ?? 0
    }

    /** Unscaled page size in CSS px, used for fit-to-width */
    async pageSize(pageNo = 1) {
        if (!this.doc) return null
        const page = await this.doc.getPage(pageNo)
        const viewport = page.getViewport({ scale: 1 })
        return { width: viewport.width, height: viewport.height }
    }

    cancelAll() {
        for (const task of this.tasks.values()) {
            try {
                task.cancel()
            } catch {
                /* already settled */
            }
        }
        this.tasks.clear()
    }

    async renderPage(pageNo, canvas, scale) {
        if (!this.doc || !canvas) return
        const doc = this.doc
        const existing = this.tasks.get(pageNo)
        if (existing) {
            try {
                existing.cancel()
            } catch {
                /* already settled */
            }
        }

        const page = await doc.getPage(pageNo)
        if (this.doc !== doc) return // superseded while awaiting

        const viewport = page.getViewport({ scale })
        const outputScale = window.devicePixelRatio || 1
        canvas.width = Math.floor(viewport.width * outputScale)
        canvas.height = Math.floor(viewport.height * outputScale)
        canvas.style.width = `${Math.floor(viewport.width)}px`
        canvas.style.height = `${Math.floor(viewport.height)}px`

        const task = page.render({
            canvas,
            viewport,
            transform: outputScale !== 1 ? [outputScale, 0, 0, outputScale, 0, 0] : null,
        })
        this.tasks.set(pageNo, task)
        try {
            await task.promise
        } catch (e) {
            if (e?.name !== 'RenderingCancelledException') throw e
        } finally {
            if (this.tasks.get(pageNo) === task) this.tasks.delete(pageNo)
        }
    }

    destroy() {
        this.cancelAll()
        if (this.loadingTask) {
            this.loadingTask.destroy().catch(() => {})
            this.loadingTask = null
        }
        this.doc = null
    }
}
