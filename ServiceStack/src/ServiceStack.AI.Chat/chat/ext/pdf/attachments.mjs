/**
 * Turns whatever the user drops on the AI box - a screenshot, a photo, a PDF - into image data URLs a
 * vision model can read. PDFs are rasterised page by page with the same vendored pdf.js the preview uses,
 * so this works with any model that accepts images, not just the few that take PDFs directly.
 */
import { loadPdfjs } from './pdf-preview.mjs'

export const MAX_ATTACHMENTS = 8
export const MAX_PDF_PAGES = 4
// what models see is downscaled anyway - sending more just costs tokens and upload time
const MAX_EDGE = 1568
const JPEG_QUALITY = 0.85

export const isPdfFile = file => file.type === 'application/pdf' || /\.pdf$/i.test(file.name ?? '')
export const isImageFile = file => (file.type || '').startsWith('image/')

const readDataUrl = file =>
    new Promise((resolve, reject) => {
        const reader = new FileReader()
        reader.onload = () => resolve(reader.result)
        reader.onerror = () => reject(reader.error ?? new Error(`Could not read ${file.name}`))
        reader.readAsDataURL(file)
    })

const loadImage = src =>
    new Promise((resolve, reject) => {
        const img = new Image()
        img.onload = () => resolve(img)
        img.onerror = () => reject(new Error('Not a readable image'))
        img.src = src
    })

/** PNG keeps screenshots of text crisp; photos are far smaller as JPEG */
function encode(canvas, preferPng) {
    return preferPng ? canvas.toDataURL('image/png') : canvas.toDataURL('image/jpeg', JPEG_QUALITY)
}

function scaleOf(width, height) {
    return Math.min(1, MAX_EDGE / Math.max(width, height))
}

async function fromImage(file) {
    const source = await readDataUrl(file)
    const img = await loadImage(source)
    const scale = scaleOf(img.naturalWidth, img.naturalHeight)
    if (scale === 1 && /^data:image\/(png|jpeg);/.test(source)) {
        return [{ name: file.name || 'image', url: source, width: img.naturalWidth, height: img.naturalHeight }]
    }
    const canvas = document.createElement('canvas')
    canvas.width = Math.round(img.naturalWidth * scale)
    canvas.height = Math.round(img.naturalHeight * scale)
    canvas.getContext('2d').drawImage(img, 0, 0, canvas.width, canvas.height)
    return [
        {
            name: file.name || 'image',
            url: encode(canvas, /^data:image\/png;/.test(source)),
            width: canvas.width,
            height: canvas.height,
        },
    ]
}

async function fromPdf(file, baseUrl) {
    const lib = await loadPdfjs(baseUrl)
    const task = lib.getDocument({ data: new Uint8Array(await file.arrayBuffer()) })
    const doc = await task.promise
    try {
        const pages = Math.min(doc.numPages, MAX_PDF_PAGES)
        const out = []
        for (let n = 1; n <= pages; n++) {
            const page = await doc.getPage(n)
            const base = page.getViewport({ scale: 1 })
            // a PDF point is 1/72", so rendering at scale 1 gives text too small to read - scale up to MAX_EDGE
            const scale = Math.min(3, MAX_EDGE / Math.max(base.width, base.height))
            const viewport = page.getViewport({ scale })
            const canvas = document.createElement('canvas')
            canvas.width = Math.ceil(viewport.width)
            canvas.height = Math.ceil(viewport.height)
            await page.render({ canvas, viewport }).promise
            out.push({
                name: `${file.name || 'document.pdf'} p${n}`,
                url: encode(canvas, true),
                width: canvas.width,
                height: canvas.height,
            })
        }
        return out
    } finally {
        task.destroy().catch(() => {})
    }
}

/**
 * @returns {Promise<{ attachments: Array<{name,url,width,height}>, errors: string[] }>}
 */
export async function toAttachments(files, baseUrl) {
    const attachments = []
    const errors = []
    for (const file of files) {
        try {
            if (isPdfFile(file)) attachments.push(...(await fromPdf(file, baseUrl)))
            else if (isImageFile(file)) attachments.push(...(await fromImage(file)))
            else errors.push(`${file.name || 'file'} isn't an image or a PDF`)
        } catch (e) {
            errors.push(`${file.name || 'file'}: ${e.message ?? e}`)
        }
    }
    return { attachments, errors }
}
