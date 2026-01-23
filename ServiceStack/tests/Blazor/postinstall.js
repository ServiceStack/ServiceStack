// Usage: npm install

const path = require('path')
const fs = require('fs')
const { pipeline } = require('stream')
const { promisify } = require('util')
const { execSync } = require('child_process')
const pipe = promisify(pipeline)

const writeTo = './wwwroot'
const files = {
    tailwind: {
        'ServiceStack.Blazor.html' : 'https://raw.githubusercontent.com/ServiceStack/ServiceStack/refs/heads/main/ServiceStack.Blazor/src/ServiceStack.Blazor/dist/tailwind.html'
    }
}

;(async () => {
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

    await Promise.all(requests)
    await downloadTailwindBinary()
})()

async function fetchDownload(url, toFile, retries) {
    const toDir = path.dirname(toFile)
    fs.mkdirSync(toDir, { recursive: true })
    for (let i=retries; i>=0; --i) {
        try {
            let r = await fetch(url)
            if (!r.ok) {
                throw new Error(`${r.status} ${r.statusText}`);
            }
            let txt = await r.text()
            console.log(`writing ${url} to ${toFile}`)
            await fs.writeFileSync(toFile, txt)
            return
        } catch (e) {
            console.log(`get ${url} failed: ${e}${i > 0 ? `, ${i} retries remaining...` : ''}`)
        }
    }
}

async function downloadTailwindBinary() {
    const platform = process.platform // e.g., 'darwin', 'linux', 'win32'
    const arch = process.arch         // e.g., 'arm64', 'x64'

    // Check if tailwindcss is already in PATH
    try {
        const command = platform === 'win32' ? 'where tailwindcss' : 'which tailwindcss'
        const result = execSync(command, { stdio: 'pipe' })
        if (result) {
            // Check version of tailwindcss by looking for 'tailwindcss v4' in `taildwindcss --help`
            const helpResult = execSync('tailwindcss --help', { stdio: 'pipe' })
            const helpOutput = helpResult.toString()
            if (helpOutput.includes('tailwindcss v1') || helpOutput.includes('tailwindcss v2') || helpOutput.includes('tailwindcss v3')) {
                console.log('old version of tailwindcss detected, please uninstall and rerun this script.')
            } else {
                console.log('tailwindcss is already installed.')
            }
            return
        }
    } catch (e) {
        // Command failed, tailwindcss not in PATH
    }
    
    // if file already exists, exit
    const tailwindcssPath = path.join(process.cwd(), 'tailwindcss')
    if (fs.existsSync(tailwindcssPath)) {
        console.log(`${tailwindcssPath} already exists, skipping download.`)
        return
    }

    console.log()
    function getBinaryFileName() {
        // Determine the correct binary file name based on the current OS and architecture
        if (platform === 'darwin') { // macOS
            if (arch === 'arm64') {
                return 'tailwindcss-macos-arm64'
            } else if (arch === 'x64') {
                return 'tailwindcss-macos-x64'
            }
        } else if (platform === 'linux') { // Linux
            if (arch === 'arm64') {
                return 'tailwindcss-linux-arm64'
            } else if (arch === 'x64') {
                return 'tailwindcss-linux-x64'
            }
        } else if (platform === 'win32') { // Windows
            if (arch === 'arm64') {
                return 'arm64-windows'
            } else if (arch === 'x64') {
                return 'tailwindcss-windows-x64.exe'
            }
        }
    }

    let binaryFileName = getBinaryFileName()

    // If no matching binary is found, exit with an error
    if (!binaryFileName) {
        console.error(`Error: Unsupported platform/architecture combination: ${platform}/${arch}`)
        console.error(`Please ensure your system is one of the following:`)
        console.error(`  macOS (arm64, x64)`)
        console.error(`  Linux (arm64, x64)`)
        console.error(`  Windows (arm64, x64)`)
        process.exit(1)
    }

    // Base URL for Tailwind CSS latest release downloads
    const downloadTailwindBaseUrl = `https://github.com/tailwindlabs/tailwindcss/releases/latest/download/`
    const downloadUrl = `${downloadTailwindBaseUrl}${binaryFileName}`
    // Set the output file name. On Windows, it should have a .exe extension.
    const outputFileName = (platform === 'win32' || platform === 'cygwin' || platform === 'msys') ? 'tailwindcss.exe' : 'tailwindcss'
    const outputPath = path.join(process.cwd(), outputFileName)

    console.log(`Attempting to download the latest Tailwind CSS binary for ${platform}/${arch}...`)
    console.log(`Downloading ${downloadUrl}...`)

    try {
        const response = await fetch(downloadUrl)

        // Check if the response status is not OK (e.g., 404, 500).
        // Fetch automatically handles redirects (3xx status codes).
        if (!response.ok) {
            console.error(`Failed to download: HTTP Status Code ${response.status} - ${response.statusText}`)
            return
        }

        // Ensure there's a readable stream body
        if (!response.body) {
            console.error('No response body received from the download URL.')
            return
        }

        const fileStream = fs.createWriteStream(outputPath)
        // Pipe the readable stream from the fetch response body directly to the file stream
        await pipe(response.body, fileStream)

        // Set executable permissions for non-Windows platforms
        if (platform !== 'win32' && platform !== 'cygwin' && platform !== 'msys') {
            console.log(`Setting executable permissions (+x) on ${outputPath}...`)
            // '755' means: owner can read, write, execute; group and others can read and execute.
            fs.chmodSync(outputPath, '755')
            // console.log('Permissions set successfully.')

            const tryFolders = [
                `${process.env.HOME}/.local/bin`,
                `${process.env.HOME}/.npm-global/bin`,
                '/usr/local/bin', 
                '/usr/bin', 
                '/usr/sbin'
            ]

            // Move the binary to a common location in PATH
            for (const folder of tryFolders) {
                if (!fs.existsSync(folder)) {
                    // console.log(`Folder ${folder} does not exist, skipping...`);
                    continue
                }
                const targetPath = path.join(folder, outputFileName)
                if (fs.accessSync(folder, fs.constants.W_OK)) {
                    try {
                        fs.renameSync(outputPath, targetPath)
                        console.log(`Saved to ${targetPath}`)
                        break
                    }
                    catch (err) {
                        console.error(`Failed to move ${outputPath} to ${targetPath}: ${err.message}`)
                    }
                }

                try {
                    // try using sudo with process exec
                    execSync(`sudo mv ${outputPath} ${targetPath}`)
                    console.log(`Saved to ${targetPath}`)
                    break
                }
                catch (err) {
                    console.log(`Manually move tailwindcss to ${targetPath} by running:`)
                    console.log(`sudo mv ${outputPath} ${targetPath}`)
                    break
                }
            }
        } else if (platform === 'win32') {
            let moved = false
            // Move the binary to a common location in PATH for .NET Devs
            const tryFolders = [
                `${process.env.APPDATA}/npm`,
                `${process.env.USERPROFILE}/.dotnet/tools`,
            ]
            for (const folder of tryFolders) {
                if (!fs.existsSync(folder)) {
                    continue
                }
                const targetPath = path.join(folder, outputFileName)
                try {
                    fs.renameSync(outputPath, targetPath)
                    console.log(`Saved to ${targetPath}`)
                    moved = true
                    break
                }
                catch (err) {
                }
            }
            if (!moved) {
                console.log()
                console.log(`Saved to ${outputPath}`)
                console.log(`Tip: Make ${outputFileName} globally accessible by moving it to a folder in your PATH`)
            }
        }

        console.log()
        console.log(`You can now run it from your terminal using:`)
        console.log(outputFileName === 'tailwindcss.exe' ? `${outputFileName} --help` : `${outputFileName} --help`)

    } catch (error) {
        console.error(`\nError during download or permission setting:`)
        console.error(error.message)
        process.exit(1)
    }
}
