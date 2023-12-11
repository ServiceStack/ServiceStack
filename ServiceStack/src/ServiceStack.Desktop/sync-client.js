const writeTo = './lib'
const defaultPrefix = 'https://unpkg.com'
const files = {
    js: {
        '@servicestack/client/servicestack-client.min.js': 'https://unpkg.com/@servicestack/client@2/dist/servicestack-client.min.js',
    }
}

const path = require('path')
const fs = require('fs').promises
const http = require('http')
const https = require('https')

const requests = []
Object.keys(files).forEach(dir => {
    const dirFiles = files[dir]
    Object.keys(dirFiles).forEach(name => {
        let url = dirFiles[name]
        if (url.startsWith('/'))
            url = defaultPrefix + url
        const toFile = path.join(writeTo, dir, name)
        requests.push(fetchDownload(url, toFile, 5))
    })
})

;(async () => {
    await Promise.all(requests)
})()

async function fetchDownload(url, toFile, retries) {
    const toDir = path.dirname(toFile)
    await fs.mkdir(toDir, { recursive: true })
    for (let i=retries; i>=0; --i) {
        try {
            let r = await fetch(url)
            if (!r.ok) {
                throw new Error(`${r.status} ${r.statusText}`);
            }
            let txt = await r.text()
            console.log(`writing ${url} to ${toFile}`)
            await fs.writeFile(toFile, txt)
            return
        } catch (e) {
            console.log(`get ${url} failed: ${e}${i > 0 ? `, ${i} retries remaining...` : ''}`)
        }
    }
}