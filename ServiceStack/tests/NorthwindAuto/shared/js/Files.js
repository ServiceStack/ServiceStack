import { lastRightPart, leftPart } from "@servicestack/client"
import { Files } from "../../lib/types"
import { toAppUrl } from "./core"

/*minify:*/

/** Wrapper around SVG icons for File Types 
 * @type {Files} */
export const Files = (function () {
    let web = 'png,jpg,jpeg,gif,svg,webp'.split(',')
    const Ext = {
        img:'png,jpg,jpeg,gif,svg,webp,png,jpg,jpeg,gif,bmp,tif,tiff,webp,ai,psd,ps'.split(','),
        vid:'avi,m4v,mov,mp4,mpg,mpeg,wmv,webm'.split(','),
        aud:'mp3,mpa,ogg,wav,wma,mid,webm'.split(','),
        ppt:'key,odp,pps,ppt,pptx'.split(','),
        xls:'xls,xlsm,xlsx,ods,csv,tsv'.split(','),
        doc:'doc,docx,pdf,rtf,tex,txt,md,rst,xls,xlsm,xlsx,ods,key,odp,pps,ppt,pptx'.split(','),
        zip:'zip,tar,gz,7z,rar,gzip,deflate,br,iso,dmg,z,lz,lz4,lzh,s7z,apl,arg,jar,war'.split(','),
        exe:'exe,bat,sh,cmd,com,app,msi,run,vb,vbs,js,ws,wsh'.split(','),
        att:'bin,oct,dat'.split(','), //attachment
    }
    const ExtKeys = Object.keys(Ext)
    let S = (viewBox,body) => `<svg xmlns='http://www.w3.org/2000/svg' aria-hidden='true' role='img' preserveAspectRatio='xMidYMid meet' viewBox='${viewBox}'>${body}</svg>`
    const Icons = {
        img: S("0 0 24 24","<path fill='currentColor' d='M20 6v12a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2zm-2 0H6v6.38l2.19-2.19l5.23 5.23l1-1a1.59 1.59 0 0 1 2.11.11L18 16V6zm-5 3.5a1.5 1.5 0 1 1 3 0a1.5 1.5 0 0 1-3 0z'/>"),
        vid: S("0 0 24 24","<path fill='currentColor' d='m14 2l6 6v12a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h8m4 18V9h-5V4H6v16h12m-2-2l-2.5-1.7V18H8v-5h5.5v1.7L16 13v5Z'/>"),
        aud: S("0 0 24 24","<path fill='currentColor' d='M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V8l-6-6zM6 20V4h7v5h5v11H6zm10-9h-4v3.88a2.247 2.247 0 0 0-3.5 1.87c0 1.24 1.01 2.25 2.25 2.25S13 17.99 13 16.75V13h3v-2z'/>"),
        ppt: S("0 0 48 48","<g fill='none' stroke='currentColor' stroke-linecap='round' stroke-linejoin='round' stroke-width='4'><path d='M4 8h40'/><path d='M8 8h32v26H8V8Z' clip-rule='evenodd'/><path d='m22 16l5 5l-5 5m-6 16l8-8l8 8'/></g>"),
        xls: S("0 0 256 256","<path fill='currentColor' d='M200 26H72a14 14 0 0 0-14 14v26H40a14 14 0 0 0-14 14v96a14 14 0 0 0 14 14h18v26a14 14 0 0 0 14 14h128a14 14 0 0 0 14-14V40a14 14 0 0 0-14-14Zm-42 76h44v52h-44Zm44-62v50h-44V80a14 14 0 0 0-14-14h-2V38h58a2 2 0 0 1 2 2ZM70 40a2 2 0 0 1 2-2h58v28H70ZM38 176V80a2 2 0 0 1 2-2h104a2 2 0 0 1 2 2v96a2 2 0 0 1-2 2H40a2 2 0 0 1-2-2Zm32 40v-26h60v28H72a2 2 0 0 1-2-2Zm130 2h-58v-28h2a14 14 0 0 0 14-14v-10h44v50a2 2 0 0 1-2 2ZM69.2 148.4L84.5 128l-15.3-20.4a6 6 0 1 1 9.6-7.2L92 118l13.2-17.6a6 6 0 0 1 9.6 7.2L99.5 128l15.3 20.4a6 6 0 0 1-9.6 7.2L92 138l-13.2 17.6a6 6 0 1 1-9.6-7.2Z'/>"),
        doc: S("0 0 32 32","<path fill='currentColor' d='M26 30H11a2.002 2.002 0 0 1-2-2v-6h2v6h15V6h-9V4h9a2.002 2.002 0 0 1 2 2v22a2.002 2.002 0 0 1-2 2Z'/><path fill='currentColor' d='M17 10h7v2h-7zm-1 5h8v2h-8zm-1 5h9v2h-9zm-6-1a5.005 5.005 0 0 1-5-5V3h2v11a3 3 0 0 0 6 0V5a1 1 0 0 0-2 0v10H8V5a3 3 0 0 1 6 0v9a5.005 5.005 0 0 1-5 5z'/>"),
        zip: S("0 0 16 16","<g fill='currentColor'><path d='M6.5 7.5a1 1 0 0 1 1-1h1a1 1 0 0 1 1 1v.938l.4 1.599a1 1 0 0 1-.416 1.074l-.93.62a1 1 0 0 1-1.109 0l-.93-.62a1 1 0 0 1-.415-1.074l.4-1.599V7.5zm2 0h-1v.938a1 1 0 0 1-.03.243l-.4 1.598l.93.62l.93-.62l-.4-1.598a1 1 0 0 1-.03-.243V7.5z'/><path d='M2 2a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V2zm5.5-1H4a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1V2a1 1 0 0 0-1-1H9v1H8v1h1v1H8v1h1v1H7.5V5h-1V4h1V3h-1V2h1V1z'/></g>"),
        exe: S("0 0 16 16","<path fill='currentColor' fill-rule='evenodd' d='M14 4.5V14a2 2 0 0 1-2 2h-1v-1h1a1 1 0 0 0 1-1V4.5h-2A1.5 1.5 0 0 1 9.5 3V1H4a1 1 0 0 0-1 1v9H2V2a2 2 0 0 1 2-2h5.5L14 4.5ZM2.575 15.202H.785v-1.073H2.47v-.606H.785v-1.025h1.79v-.648H0v3.999h2.575v-.647ZM6.31 11.85h-.893l-.823 1.439h-.036l-.832-1.439h-.931l1.227 1.983l-1.239 2.016h.861l.853-1.415h.035l.85 1.415h.908l-1.254-1.992L6.31 11.85Zm1.025 3.352h1.79v.647H6.548V11.85h2.576v.648h-1.79v1.025h1.684v.606H7.334v1.073Z'/>"),
        att: S("0 0 24 24","<path fill='currentColor' d='M14 0a5 5 0 0 1 5 5v12a7 7 0 1 1-14 0V9h2v8a5 5 0 0 0 10 0V5a3 3 0 1 0-6 0v12a1 1 0 1 0 2 0V6h2v11a3 3 0 1 1-6 0V5a5 5 0 0 1 5-5Z'/>"),
    }
    const symbols = /[\r\n%#()<>?[\\\]^`{|}]/g

    /** Encode SVG for embedding in Data URI 
     * @param {string} s 
     * @return {string} */
    function encodeSvg(s) {
        s = s.replace(/"/g, `'`)
        s = s.replace(/>\s+</g, `><`)
        s = s.replace(/\s{2,}/g, ` `)
        return s.replace(symbols, encodeURIComponent)
    }
    /** Convert SVG to Data URI 
     * @param {string} svg 
     * @return {string} */
    function svgToDataUri(svg) {
        return "data:image/svg+xml;utf8," + encodeSvg(svg)
    }
    let Track = []

    /**@param {MediaSource} file
     * @return {string} */
    function objectUrl(file) {
        let ret = URL.createObjectURL(file)
        Track.push(ret)
        return ret
    }

    /** Clear all remaining Image URIs of INPUT file attachments */
    function flush() {
        Track.forEach(x => {
            try {
                URL.revokeObjectURL(x)
            } catch (e) {
                console.error('URL.revokeObjectURL', e)
            }
        })
        Track = []
    }

    /** Return file name part of URI or file path
     * @param {string} path 
     * @return {string|null} */
    function getFileName(path) {
        if (!path) return null
        let noQs = leftPart(path,'?')
        return lastRightPart(noQs,'/')
    }

    /** Return file extension (without '.; prefix) of path or URI  
     * @param {string} path 
     * @return {string|null} */
    function getExt(path) {
        let fileName = getFileName(path)
        if (fileName == null || fileName.indexOf('.') === -1)
            return null
        return lastRightPart(fileName,'.').toLowerCase()
    }

    /** Return Image URI of uploaded file
     * @param {File|MediaSource} file 
     * @return {string} */
    function fileImageUri(file) {
        let ext = getExt(file.name)
        if (web.indexOf(ext) >= 0)
            return objectUrl(file)
        return filePathUri(file.name)
    }

    /** Return whether path is a URI to a previewable image 
     * @param {string} path */
    function canPreview(path) {
        if (!path) return false
        if (path.startsWith('blob:') || path.startsWith('data:'))
            return true
        let ext = getExt(path)
        return ext && web.indexOf(ext) >= 0;
    }

    /** Get the Icon src for a file path or URI, previewable resources will return self, otherwise returns SVG URI of .ext 
     * @param {string} path 
     * @return {string|null} */
    function filePathUri(path) {
        if (!path) return null
        let ext = getExt(path)
        if (ext == null || canPreview(path))
            return toAppUrl(path)
        return extSrc(ext) || svgToDataUri(Icons.doc)
    }

    /** Get Icon src for .ext
     * @param {string} ext
     * @return {string|null} */
    function extSrc(ext) {
        return map(extSvg(ext), svg => svgToDataUri(svg))
    }

    /** Get Icon SVG for .ext
     * @param {string} ext
     * @return {string|null} */
    function extSvg(ext) {
        if (Icons[ext])
            return Icons[ext]
        for (let i=0; i<ExtKeys.length; i++) {
            let k = ExtKeys[i]
            if (Ext[k].indexOf(ext) >= 0)
                return Icons[k]
        }
        return null
    }

    const k = 1024
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB']
    /** @param {number} bytes
     *  @param {number} [d=2] 
     *  @return {string} */
    function formatBytes(bytes, d = 2) {
        if (bytes === 0) return '0 Bytes'
        const dm = d < 0 ? 0 : d
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i]
    }
        
    return {
        extSvg,
        extSrc,
        getExt,
        encodeSvg,
        canPreview,
        getFileName,
        formatBytes,
        filePathUri,
        svgToDataUri,
        fileImageUri,
        flush,
    }
})()

/*:minify*/