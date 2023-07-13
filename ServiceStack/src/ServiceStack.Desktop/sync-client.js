const writeTo = './lib'
const defaultPrefix = 'https://unpkg.com'
const files = {
    js: {
        '@servicestack/client/servicestack-client.min.js': 'https://unpkg.com/@servicestack/client@2/dist/servicestack-client.min.js',
    }
}

const path = require('path')
const fs = require('fs')
const http = require('http')
const https = require('https')

Object.keys(files).forEach(dir => {
    const dirFiles = files[dir]
    Object.keys(dirFiles).forEach(name => {
        let url = dirFiles[name]
        if (url.startsWith('/'))
            url = defaultPrefix + url
        const toFile = path.join(writeTo, dir, name)
        const toDir = path.dirname(toFile)
        if (!fs.existsSync(toDir)) {
            fs.mkdirSync(toDir, { recursive: true })
        }
        //httpDownload(url, toFile, 5) //started hanging
        fetchDownload(url, toFile, 5)
    })
})

function httpDownload(url, toFile, retries) {
    const client = url.startsWith('https') ? https : http
    const retry = (e) => {
        console.log(`get ${url} failed: ${e}${retries > 0 ? `, ${retries-1} retries remaining...` : ''}`)
        if (retries > 0) httpDownload(url, toFile, retries-1)
    }

    client.get(url, res => {
        if (res.statusCode === 301 || res.statusCode === 302) {
            let redirectTo = res.headers.location;
            if (redirectTo.startsWith('/'))
                redirectTo = new URL(res.headers.location, new URL(url).origin).href
            return httpDownload(redirectTo, toFile, retries)
        } else if (res.statusCode >= 400) {
            retry(`${res.statusCode} ${res.statusText || ''}`.trimEnd())
        }
        else {
            console.log(`writing ${url} to ${toFile}`)
            const file = fs.createWriteStream(toFile)
            res.pipe(file);
            file.on('finish', () => file.close())
        }
    }).on('error', retry)
}

function fetchDownload(url, toFile, retries) {
    (async () => {
        for (let i=retries; i>=0; --i) {
            try {
                let r = await fetch(url)
                if (!r.ok) throw new Error(`${r.status} ${r.statusText}`);
                let txt = await r.text()
                console.log(`writing ${url} to ${toFile}`)
                fs.writeFileSync(toFile, txt)
                return
            } catch (e) {
                console.log(`get ${url} failed: ${e}${i > 0 ? `, ${i} retries remaining...` : ''}`)
            }
        }
    })()
}
