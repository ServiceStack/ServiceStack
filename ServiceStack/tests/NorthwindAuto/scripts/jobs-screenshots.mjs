#!/usr/bin/env node
/*
 * Populates the Background Jobs Admin UI with the example Jobs in ServiceInterface/BackgroundJobServices.cs,
 * then captures screenshots of it at 1280x720 (16:9 720p) for videos and Release Notes.
 *
 * Usage (with the App running):
 *   npm install                  # installs playwright-core
 *   npm run screenshots:jobs
 *   npm run screenshots:jobs -- --scale 1.5 --out ../../screenshots
 *
 * Options:
 *   --url <url>          App URL (default https://localhost:5001)
 *   --authsecret <pwd>   Admin Auth Secret (default p@55wOrd)
 *   --out <dir>          Output directory (default screenshots/background-jobs)
 *   --scale <n>          Device scale factor, e.g. 1.5 for 1920x1080 or 2 for 2560x1440 images of the same 720p layout (default 1)
 *   --chrome <path>      Chromium/Chrome executable, if Playwright's own browser isn't installed (or CHROME_PATH)
 *   --no-seed            Skip queueing Jobs and screenshot the latest existing ones
 */
import fs from 'node:fs'
import path from 'node:path'
import { parseArgs } from 'node:util'
import { chromium } from 'playwright-core'

const { values: args } = parseArgs({ options: {
    url:          { type: 'string', default: 'https://localhost:5001' },
    authsecret:   { type: 'string', default: 'p@55wOrd' },
    out:          { type: 'string', default: 'screenshots/background-jobs' },
    scale:        { type: 'string', default: '1' },
    chrome:       { type: 'string', default: process.env.CHROME_PATH },
    'no-seed':    { type: 'boolean', default: false },
}})
const BASE_URL = args.url.replace(/\/$/, '')
const OUT = args.out
const SCALE = parseFloat(args.scale)
const ADMIN_URL = `${BASE_URL}/admin-ui/backgroundjobs`

async function launch() {
    if (args.chrome) return await chromium.launch({ executablePath: args.chrome })
    // Playwright's own browser, then common system installs
    for (const executablePath of [undefined, '/usr/bin/chromium', '/usr/bin/google-chrome',
        '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome']) {
        try {
            return await chromium.launch({ executablePath })
        } catch {}
    }
    throw new Error('No browser found, run `npx playwright install chromium` or set CHROME_PATH')
}

const browser = await launch()
const context = await browser.newContext({
    viewport: { width: 1280, height: 720 },
    deviceScaleFactor: SCALE,
    ignoreHTTPSErrors: true,
    extraHTTPHeaders: { authsecret: args.authsecret },
})
const api = context.request

async function post(op, body = {}) {
    const res = await api.post(`${BASE_URL}/api/${op}`, { data: body })
    const json = await res.json().catch(() => ({}))
    if (!res.ok()) throw new Error(`${op} failed: ${res.status()} ${json?.responseStatus?.message ?? ''}`)
    console.log(`  ${op}${json.message ? ` - ${json.message}` : ''}`)
    return json
}

async function getJob(id) {
    const res = await api.get(`${BASE_URL}/api/AdminGetJob?id=${id}`)
    const r = await res.json()
    if (!res.ok()) throw new Error(`AdminGetJob ${id} failed: ${res.status()} ${r?.responseStatus?.message ?? ''}`)
    return r.completed ?? r.failed ?? r.queued ?? r.result
}

/** Polls a Job until it reaches one of the states, returning the Job */
async function waitForJob(id, states, timeoutMs = 60_000) {
    const until = Date.now() + timeoutMs
    let job
    while (Date.now() < until) {
        job = await getJob(id)
        if (states.includes(job?.state)) return job
        await new Promise(r => setTimeout(r, 1000))
    }
    console.warn(`  Job ${id} is ${job?.state} after ${timeoutMs / 1000}s, expected ${states.join('/')}`)
    return job
}

fs.mkdirSync(OUT, { recursive: true })
const page = await context.newPage()
page.on('pageerror', e => console.warn(`  page error: ${e.message}`))

async function screenshot({ name, path: urlPath, click, wait, scrollTo }) {
    if (urlPath.includes('edit=undefined')) {
        console.warn(`  skipped ${name}: no matching Job`)
        return
    }
    await page.goto(ADMIN_URL + urlPath)
    await page.waitForLoadState('networkidle', { timeout: 5000 }).catch(() => {})
    if (click) await page.click(click)
    if (wait) await page.waitForSelector(wait, { timeout: 10_000 }).catch(() => console.warn(`  ${name}: '${wait}' not found`))
    // let grids load their results and polling refresh
    await page.waitForTimeout(1500)
    if (scrollTo) await page.locator(scrollTo).last().scrollIntoViewIfNeeded().catch(() => {})
    const file = path.join(OUT, `${name}.png`)
    await page.screenshot({ path: file })
    console.log(`  ${file}`)
}

const latest = async (query) => {
    const res = await api.get(`${BASE_URL}/api/AdminQueryJobSummary?orderByDesc=Id&take=1&fields=id&${query}`)
    return (await res.json()).results?.[0]?.id
}

// Jobs the screenshots link to
const ids = {}

// Phase 1: Jobs that finish, for the History, Job details and Scheduled Tasks screenshots
if (!args['no-seed']) {
    console.log(`Queueing example Jobs on ${BASE_URL}...`)
    await post('ScheduleRecurringCleanup', { intervalSecs: 30, maxRuns: 5 })
    ids.retried = (await post('QueueWelcomeEmail', { email: 'ada@example.org', failAttempts: 2, retryLimit: 3 })).jobs[0].id
    ids.failed = (await post('QueueWelcomeEmail', { email: 'bounced@example.org', failAttempts: 10, retryLimit: 2 })).jobs[0].id
    ids.batch = (await post('QueueResizeImages', { images: 16, failImages: 3 })).jobs[0].id
    await post('QueueResizeImages', { images: 12 })
    await post('QueueOrderFulfillment', { orderId: 1001 })
    await post('QueueOrderFulfillment', { orderId: 1002, failStep: 'reserve' })
    await post('QueueTenantSync', { jobsPerTenant: 4 })
    await post('QueueDeduplicatedJobs')
    await post('QueuePlaceOrder')
    await post('QueueScheduledReport', { reportName: 'Monthly Sales', delaySecs: 10 })
    await post('QueueScheduledReport', { reportName: 'Stale Inventory', delaySecs: 10, expiresInSecs: 2 })
    await post('RunReportAndWait', { reportName: 'Daily Orders' })

    console.log('Waiting for Jobs to run...')
    await waitForJob(ids.retried, ['Completed', 'Failed'])
    await waitForJob(ids.failed, ['Failed'], 90_000)
    await waitForJob(ids.batch, ['Completed', 'Failed'])
} else {
    console.log('--no-seed: using the latest existing Jobs')
    ids.retried = await latest('command=SendWelcomeEmailCommand&state=Completed&attemptsGreaterThan=1')
    ids.failed = await latest('command=SendWelcomeEmailCommand&state=Failed')
    ids.batch = await latest('command=ResizeImageCommand&state=Failed')
}

console.log(`Saving screenshots to ${OUT}...`)
for (const shot of [
    { name: '06-history',          path: '?tab=History' },
    { name: '07-job-attempts',     path: `?tab=History&edit=${ids.retried}`, wait: 'text=Failed Attempts', scrollTo: 'text=Failed Attempts' },
    { name: '08-job-failed',       path: `?tab=History&edit=${ids.failed}`, wait: 'text=Failed Attempts', scrollTo: 'text=Failed Attempts' },
    { name: '09-job-batch',        path: `?tab=History&edit=${ids.batch}`, wait: 'text=Batch', scrollTo: 'text=Requeue' },
    { name: '10-history-failed',   path: '?tab=History&page=failed' },
    { name: '11-scheduled-tasks',  path: '?tab=ScheduledTasks', wait: 'table' },
]) await screenshot(shot)

// Phase 2: a long-running import and a rate limited backlog, for the live Queue screenshots
if (!args['no-seed']) {
    ids.import = (await post('QueueImportProducts', { products: 400, msPerProduct: 250, timeoutSecs: 300 })).jobs[0].id
    await post('QueueExternalApiCalls', { calls: 30, rateLimit: 2, windowSecs: 10 })
    await post('QueueTenantSync', { jobsPerTenant: 6 })
    await waitForJob(ids.import, ['Started'])
    await new Promise(r => setTimeout(r, 5000))
} else {
    ids.import = await latest('command=ImportProductsCommand')
}

for (const shot of [
    { name: '01-dashboard',        path: '?tab=Dashboard' },
    { name: '02-queue',            path: '?tab=Queue' },
    { name: '03-job-running',      path: `?tab=Queue&edit=${ids.import}`, wait: 'text=Logs', scrollTo: 'text=Logs' },
    { name: '04-queues',           path: '?tab=Queues', wait: 'table' },
    { name: '05-nodes',            path: '?tab=Nodes', wait: 'table' },
    { name: '12-cancel-jobs',      path: '?tab=Queue', click: "button[title='Cancel Jobs']", wait: 'text=Batch Id' },
]) await screenshot(shot)

await browser.close()
