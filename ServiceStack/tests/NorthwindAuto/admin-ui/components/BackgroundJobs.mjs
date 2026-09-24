import { ref, computed, watch, onMounted, onUnmounted, provide, inject, nextTick } from "vue"
import { humanize,  toDate, timeFmt12, leftPart, rightPart, pick, omit, EventBus } from "@servicestack/client"
import { useClient, useUtils, useFormatters } from "@servicestack/vue"
import { AdminJobInfo, AdminGetJob, AdminGetJobProgress, AdminCancelJobs, AdminRequeueFailedJobs, AdminJobDashboard,
    AdminUpdateScheduledTask, AdminGetJobQueues, AdminUpdateJobQueue, AdminReplayJob, AdminGetJobBatch,
    AdminGetJobNodes, AdminUpdateJobNode, AdminGetJobAttempts } from "dtos"
import { Chart, registerables } from 'chart.js'
Chart.register(...registerables)

const bus = new EventBus()

const { formatDate, time, prettyJson, humanifyNumber, humanifyMs, relativeTime } = useFormatters()
const { swrApi, swrCacheKey, fromCache } = useUtils()

function getPrefs() {
    return JSON.parse(localStorage.getItem('jobs.prefs') ?? "{}")
}
function setPrefs(args) {
    let prefs = getPrefs()
    Object.assign(prefs, args)
    localStorage.setItem('jobs.prefs', JSON.stringify(prefs))
}

// Ensure only a single loop is running at a time
window.onInfo = null
let lastStats = null
let updateStatsTimeout = null

function getStats() {
    return lastStats ?? fromCache(swrCacheKey(new AdminJobInfo()));
}

async function updateStats() {
    //console.debug('updateStats', !!window.client)
    if (window.client) {
        const prefs = getPrefs()
        const request = new AdminJobInfo({ month:prefs.monthDb }) //var needed by safari
        swrApi(window.client, request, r => {
            if (!lastStats || JSON.stringify(lastStats) !== JSON.stringify(r.response)) {
                lastStats = r.response
                bus.publish('stats:changed', r.response)
            }
        })
    }
    updateStatsTimeout = setTimeout(updateStats,3000)
}

function delay(time) {
    return new Promise(resolve => setTimeout(resolve, time))
}

function hasItems(obj) {
    return !obj ? false : typeof obj === 'object'
        ? Object.keys(obj).length > 0
        : obj.length
}

// TimeSpans arrive as an XSD duration (PT1M30S) or as [d.]hh:mm:ss[.fff]
function timeSpanMs(ts) {
    if (!ts) return null
    const xsd = /^-?P(?:(\d+)D)?(?:T(?:(\d+)H)?(?:(\d+)M)?(?:([\d.]+)S)?)?$/.exec(ts)
    if (xsd) {
        const [, d, h, m, s] = xsd
        return ((+(d||0) * 24 + +(h||0)) * 60 + +(m||0)) * 60000 + Math.round(parseFloat(s||0) * 1000)
    }
    const hms = /^(?:(\d+)\.)?(\d+):(\d+):([\d.]+)$/.exec(ts)
    if (hms) {
        const [, d, h, m, s] = hms
        return ((+(d||0) * 24 + +h) * 60 + +m) * 60000 + Math.round(parseFloat(s) * 1000)
    }
    return null
}
function formatTimeSpan(ts) {
    const ms = timeSpanMs(ts)
    return ms == null ? (ts ?? '') : humanifyMs(ms)
}

const stateStyles = {
    Queued:    'bg-gray-100 text-gray-700 ring-gray-500/20 dark:bg-gray-800 dark:text-gray-300',
    Started:   'bg-sky-50 text-sky-700 ring-sky-600/20 dark:bg-sky-900/40 dark:text-sky-300',
    Executed:  'bg-indigo-50 text-indigo-700 ring-indigo-600/20 dark:bg-indigo-900/40 dark:text-indigo-300',
    Completed: 'bg-green-50 text-green-700 ring-green-600/20 dark:bg-green-900/40 dark:text-green-300',
    Failed:    'bg-red-50 text-red-700 ring-red-600/20 dark:bg-red-900/40 dark:text-red-300',
    Cancelled: 'bg-amber-50 text-amber-800 ring-amber-600/20 dark:bg-amber-900/40 dark:text-amber-300',
}

// Friendlier explanations of the error codes the Jobs runtime assigns itself
const jobErrorHelp = {
    JobExpired: 'The Job was not started before its ExpiresAt deadline, so it was cancelled instead of running late',
    LeaseExpired: 'The Job was abandoned mid-execution (its server stopped or it ignored its timeout) more times than its RetryLimit allows',
    QueueClearedOnUpgrade: 'The Job was still queued when the database was upgraded to the new Background Jobs schema',
}

const Markup = {
    template: `
        <mark v-if="title" class="border-b-2 cursor-help border-dotted border-gray-500 hover:border-gray-600 text-gray-500 bg-transparent hover:text-gray-600" :title="title">
            <slot></slot>
        </mark>
        <span v-else :title="title">
            <slot></slot>
        </span>
    `,
    props: {title:String},
    setup() {
        return {}
    }
}

const DateTime = {
    template: `<div v-if="dateValue" :title="formatDate(dateValue) + ' ' + time(dateValue)">
        {{sameDay ? time(dateValue) : formatDate(dateValue) }}
    </div>`,
    props:['value'],
    setup(props) {
        const dateValue = computed(() => props.value ? toDate(props.value) : null)
        function hasTime(date) {
            const d = toDate(date)
            return date && d.getTime() !== new Date(d.toDateString()).getTime()
        }
        const sameDay = computed(() =>  {
            const d = dateValue.value
            const now = new Date()
            return d.getFullYear() === now.getFullYear() && d.getMonth() === now.getMonth() && d.getDate() === now.getDate()
        })
        return { formatDate, time, toDate, dateValue, sameDay }
    }
}

const Duration = {
    template: `<div>{{humanifyMs(value)}}</div>`,
    props:['value'],
    setup() {
        return { humanifyMs }
    }
}

const JobState = {
    template:`
        <div class="flex items-center">
            <svg v-if="state=='Completed'" class="text-green-700 w-5 h-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 2048 2048"><path fill="currentColor" d="M1024 0q141 0 272 36t244 104t207 160t161 207t103 245t37 272q0 141-36 272t-104 244t-160 207t-207 161t-245 103t-272 37q-141 0-272-36t-244-104t-207-160t-161-207t-103-245t-37-272q0-141 36-272t104-244t160-207t207-161T752 37t272-37m603 685l-136-136l-659 659l-275-275l-136 136l411 411z"></path></svg>
            <svg v-else-if="state=='Executed'" class="text-gray-700 w-5 h-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="m9.55 18l-5.7-5.7l1.425-1.425L9.55 15.15l9.175-9.175L20.15 7.4z"/></svg>
            <svg v-else-if="state=='Queued'" class="text-gray-700 w-5 h-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32"><path fill="currentColor" d="M10.293 5.293L7 8.586L5.707 7.293L4.293 8.707L7 11.414l4.707-4.707zM14 7v2h14V7zm0 8v2h14v-2zm0 8v2h14v-2z"></path></svg>
            <svg v-else-if="state=='Started'" class="text-gray-700 w-5 h-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="m18.364 8.05l-.707-.707a8 8 0 1 0 2.28 4.658m-1.573-3.95h-4.243m4.243 0V3.807"/></svg>
            <svg v-else-if="state=='Cancelled' || state=='Failed'" class="text-red-700 w-5 h-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 2048 2048"><path fill="currentColor" d="M1024 0q141 0 272 36t244 104t207 160t161 207t103 245t37 272q0 141-36 272t-104 244t-160 207t-207 161t-245 103t-272 37q-141 0-272-36t-244-104t-207-160t-161-207t-103-245t-37-272q0-141 36-272t104-244t160-207t207-161T752 37t272-37m113 1024l342-342l-113-113l-342 342l-342-342l-113 113l342 342l-342 342l113 113l342-342l342 342l113-113z"></path></svg>
            <div :class="[textColor,'ml-1.5 text-xl']">{{state}}</div>
        </div>
    `,
    props: { state:String },
    setup(props) {
        const textColor = computed(() => props.state==='Cancelled' || props.state==='Failed' 
            ? 'text-red-700' 
            : props.state==='Completed'
                ? 'text-green-700'
                : 'text-gray-700')
        
        return { textColor }
    }
}

const Request = {
    components: { Markup },
    template: `
        <div v-if="job.request" class="flex items-center">
            <div class="flex items-center">
                <svg v-if="job.requestType=='API'" class="w-4 h-4 mr-1" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><title>API</title><g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"><path d="M16 3h5v5M8 3H3v5"/><path d="m21 3l-7.536 7.536A5 5 0 0 0 12 14.07V21M3 3l7.536 7.536A5 5 0 0 1 12 14.07V15"/></g></svg>
                <svg v-else-if="job.requestType=='CMD'" class="w-4 h-4 mr-1" xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 24 24"><title>Command</title><path fill="currentColor" d="M10 8h4V6.5a3.5 3.5 0 1 1 3.5 3.5H16v4h1.5a3.5 3.5 0 1 1-3.5 3.5V16h-4v1.5A3.5 3.5 0 1 1 6.5 14H8v-4H6.5A3.5 3.5 0 1 1 10 6.5zM8 8V6.5A1.5 1.5 0 1 0 6.5 8zm0 8H6.5A1.5 1.5 0 1 0 8 17.5zm8-8h1.5A1.5 1.5 0 1 0 16 6.5zm0 8v1.5a1.5 1.5 0 1 0 1.5-1.5zm-6-6v4h4v-4z"/></svg>                
                <Markup :title="job.requestBody??''">
                    <span v-if="job.request == 'NoArgs'" class="text-gray-400">None</span>
                    <span v-else>{{job.request}}</span>
                </Markup>
            </div>
        </div>
    `,
    props:['job'],
    setup(props) {
        const requestBody = computed(() => 'Request: ' + (props.job?.request ?? '')
            + '\n' + (props.job.requestBody??''))
        return { requestBody }
    }
}
const Command = {
    components: { Markup },
    template: `
        <div v-if="job.requestType=='CMD'" class="flex items-center">
            <span :title="requestBody">{{job.command.replace('Command','')}}</span>
        </div>
    `,
    props:['job'],
    setup(props) {
        const requestBody = computed(() => 'Request: ' + (props.job?.request ?? '')
            + '\n' + (props.job.requestBody??''))
        return { requestBody }
    }
}
const Response = {
    components: { Markup },
    template: `
        <div class="flex items-center">
            <Markup v-if="job.response || job.callback" class="flex items-center" :title="job.responseBody??''">
                {{job.response}}
            </Markup>
            <div v-if="job.callback || job.replyTo" class="flex items-center">
                <svg class="w-4 h-4 mx-1" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32"><path fill="currentColor" d="M2.078 3.965c-.407-1.265.91-2.395 2.099-1.801l24.994 12.495c1.106.553 1.106 2.13 0 2.684L4.177 29.838c-1.188.594-2.506-.536-2.099-1.801L5.95 16.001zm5.65 13.036L4.347 27.517l23.037-11.516L4.346 4.485L7.73 15H19a1 1 0 1 1 0 2z"/></svg>
                <span>{{job.callback || job.replyTo}}</span>
            </div>
        </div>
    `,
    props:['job'],
    setup(props) {
        return { }
    }
}

const Truncate = {
    template:`<div :class="['text-ellipsis overflow-hidden']" :title="value">{{value}}</div>`,
    props:['value']
}
const EditLink = {
    template:`<span @click.prevent.stop="$emit('selected',id)" class="cursor-pointer text-indigo-700 hover:text-indigo-600">{{id}}</span>`,
    emits: ['selected'],
    props: { id:Number }
}

const StateBadge = {
    template:`<span v-if="state" :class="[cls, 'inline-flex items-center gap-x-1 rounded-md px-2 py-0.5 text-xs font-medium ring-1 ring-inset whitespace-nowrap']" :title="title">
        {{label ?? state}}
    </span>`,
    props: { state:String, label:String, title:String },
    setup(props) {
        const cls = computed(() => stateStyles[props.state] ?? stateStyles.Queued)
        return { cls }
    }
}

// Segmented bar of how a group of Jobs finished, e.g. a Batch
const BatchProgress = {
    template:`
        <div>
            <div class="flex h-2 w-full overflow-hidden rounded-full bg-gray-200 dark:bg-gray-700" :title="summary">
                <div v-for="s in segments" :key="s.key" :class="s.cls" :style="{ width: s.pct + '%' }"></div>
            </div>
            <div class="mt-1.5 flex flex-wrap gap-x-4 gap-y-1 text-xs text-gray-600 dark:text-gray-400">
                <span v-for="s in legend" :key="s.key" class="inline-flex items-center gap-x-1.5">
                    <span :class="[s.cls, 'h-2 w-2 rounded-full']"></span>{{s.count}} {{s.key}}
                </span>
            </div>
        </div>`,
    props: { counts:Object, total:Number },
    setup(props) {
        const colors = {
            completed: 'bg-green-500',
            failed:    'bg-red-500',
            cancelled: 'bg-amber-400',
            running:   'bg-sky-400',
            queued:    'bg-gray-400',
        }
        const all = computed(() => Object.keys(colors).map(key => ({ key, cls:colors[key], count: props.counts?.[key] ?? 0 })))
        const sum = computed(() => Math.max(props.total ?? 0, all.value.reduce((acc, s) => acc + s.count, 0)))
        const segments = computed(() => sum.value
            ? all.value.filter(s => s.count).map(s => ({ ...s, pct: s.count / sum.value * 100 }))
            : [])
        const legend = computed(() => all.value.filter(s => s.count))
        const summary = computed(() => legend.value.map(s => `${s.count} ${s.key}`).join(', '))
        return { segments, legend, summary }
    }
}

const JobProgress = {
    template:`
        <div v-if="!isNaN(job.durationMs) && job.progress" class="w-56 flex items-center">
            <div class="w-full bg-gray-200 rounded-full dark:bg-gray-700">
                <div class="bg-green-600 text-xs font-medium text-green-100 text-center p-0.5 leading-none rounded-full" :style="{width:percent}">{{percent}}</div>
            </div>
            <div class="ml-2 w-16">{{humanifyMs(job.durationMs)}}</div>
        </div>`,
    props:['job'],
    setup(props) {
        const percent = computed(() => (props.job.progress * 100).toFixed(0) + '%')
        return { percent, humanifyMs }
    }
}

const JobDialog = {
    components: {
        JobState,
        StateBadge,
        BatchProgress,
    },
    template: `
        <SlideOver v-if="job" @done="$emit('done')"
            contentClass="relative flex-1">
            <template #title>
                <h2 class="flex items-center text-lg font-medium text-gray-900 dark:text-gray-50">
                    <svg v-if="job.requestType=='CMD'" class="w-5 h-5 mr-1" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><title>Command</title><path fill="currentColor" d="M10 8h4V6.5a3.5 3.5 0 1 1 3.5 3.5H16v4h1.5a3.5 3.5 0 1 1-3.5 3.5V16h-4v1.5A3.5 3.5 0 1 1 6.5 14H8v-4H6.5A3.5 3.5 0 1 1 10 6.5zM8 8V6.5A1.5 1.5 0 1 0 6.5 8zm0 8H6.5A1.5 1.5 0 1 0 8 17.5zm8-8h1.5A1.5 1.5 0 1 0 16 6.5zm0 8v1.5a1.5 1.5 0 1 0 1.5-1.5zm-6-6v4h4v-4z"/></svg>
                    <svg v-else-if="job.requestType=='API'" class="w-5 h-5 mr-1" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><title>API</title><g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"><path d="M16 3h5v5M8 3H3v5"/><path d="m21 3l-7.536 7.536A5 5 0 0 0 12 14.07V21M3 3l7.536 7.536A5 5 0 0 1 12 14.07V15"/></g></svg>
                    {{job.command ?? job.request}} Job {{job.id}}
                </h2>
            </template>
            <ErrorSummary :status="errorStatus" />
            <div class="mt-2 flex justify-between">
                <div>
                    <JobState class="pl-2" :state="state" />
                    <HtmlFormat :value="basic" class="py-2 not-prose" />
                </div>
                <div class="pr-3 flex flex-col gap-y-3 items-end">
                    <div v-if="job.parentId" class="flex items-center" title="Parent Job">
                        <span @click="routes.to({edit:job.parentId})" class="cursor-pointer text-sm text-indigo-600 hover:text-indigo-700">{{job.parentId}}</span>
                        <svg class="w-4 h-4 text-gray-600" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32"><path fill="currentColor" d="m21 4.094l-.72.687l-6 6l1.44 1.44L20 7.936V25H5v2h17V7.937l4.28 4.282l1.44-1.44l-6-6z"/></svg>
                    </div>
                    <div v-if="state=='Cancelled' || state=='Failed'">
                        <SecondaryButton @click="requeueJob" :disabled="loading">Requeue</SecondaryButton>
                    </div>
                    <div v-if="state=='Completed'">
                        <SecondaryButton @click="replayJob" :disabled="loading" title="Queue this Job again with the same arguments">Replay</SecondaryButton>
                    </div>
                    <div v-if="state=='Queued' || state=='Started'">
                        <PrimaryButton color="red" @click="cancelJob" :disabled="loading">Cancel</PrimaryButton>
                    </div>
                    <div>
                        <Loading v-if="loading" class="text-sm font-normal" />
                    </div>
                </div>            
            </div>
            <div v-if="job.requestType=='CMD'">
                <div v-if="job.command" class="bg-indigo-700 text-white px-3 py-3">
                  <div class="flex items-center">
                    <h2 class="font-medium text-white">{{job.command}}</h2>
                  </div>
                </div>
            </div>
            <div v-else-if="job.requestType=='API'">
                <div v-if="job.request" class="bg-indigo-700 text-white px-3 py-3">
                  <div class="flex items-center">
                    <h2 class="font-medium text-white">{{job.request}}</h2>
                  </div>
                </div>
            </div>
            <div v-if="job.requestBody" class="relative flex overflow-auto">
                <CopyIcon class="absolute top-1 right-1" :text="job.requestBody" />
                <HtmlFormat :value="JSON.parse(job.requestBody)" class="not-prose" />
            </div>
            <div v-if="job.response" class="bg-indigo-700 text-white px-3 py-3">
              <div class="flex items-start justify-between space-x-3">
                <h2 class="font-medium text-white">{{job.response}}</h2>
              </div>
            </div>
            <div v-if="job.responseBody" class="relative flex overflow-auto">
                <CopyIcon class="absolute top-1 right-1" :text="job.responseBody" />
                <HtmlFormat :value="JSON.parse(job.responseBody)" class="not-prose" />
            </div>
            <div v-if="showError" class="bg-red-700 text-white px-3 py-3">
              <div class="flex items-start justify-between space-x-3">
                <h2 class="font-medium text-white">Error</h2>
              </div>
            </div>
            <div v-if="showError" class="relative flex overflow-auto">
              <CopyIcon class="absolute top-1 right-1" :text="prettyJson(error)" />
              <table class="border-separate border-spacing-2 text-sm">
              <tbody>
                <tr>
                  <th class="text-left font-medium align-top pr-2">Code</th>
                  <td>{{ error.errorCode }}</td>
                </tr>
                <tr>
                  <th class="text-left font-medium align-top pr-2">Message</th>
                  <td>{{ error.message }}</td>
                </tr>
                <tr v-if="errorHelp">
                  <th></th>
                  <td class="text-gray-500 dark:text-gray-400">{{ errorHelp }}</td>
                </tr>
                <tr v-if="error.stackTrace">
                  <th class="text-left font-medium align-top pr-2">StackTrace</th>
                  <td>
                    <div class="whitespace-pre">{{error.stackTrace }}</div>
                  </td>
                </tr>
                <tr v-if="hasItems(error.errors)">
                  <th class="text-left font-medium align-top pr-2">Errors</th>
                  <td>
                    <HtmlFormat :value="error.errors" />
                  </td>
                </tr>
              </tbody>
              </table>
            </div>
            <div v-if="attempts.length" class="bg-gray-100 dark:bg-gray-800 text-gray-900 dark:text-gray-100 px-3 py-3">
              <div class="flex items-start justify-between space-x-3">
                <h2 class="font-medium">Failed Attempts</h2>
                <span class="text-xs text-gray-600 dark:text-gray-400">{{attempts.length}}</span>
              </div>
            </div>
            <div v-if="attempts.length" class="px-3 py-2">
              <ol class="relative border-s border-gray-200 dark:border-gray-700 ml-2">
                <li v-for="a in attempts" :key="a.id" class="mb-3 ms-4">
                  <div class="absolute w-2.5 h-2.5 bg-red-500 rounded-full -start-[5.5px] mt-1.5 ring-4 ring-white dark:ring-gray-900"></div>
                  <div class="flex flex-wrap items-center gap-x-2 text-sm">
                    <span class="font-medium">Attempt {{a.attempt}}</span>
                    <span v-if="a.errorCode" class="text-red-700 dark:text-red-400" :title="jobErrorHelp[a.errorCode]">{{a.errorCode}}</span>
                    <span class="text-xs text-gray-500">{{humanifyMs(a.durationMs)}}</span>
                    <span v-if="a.serverId" class="text-xs text-gray-500" :title="a.serverId">on {{a.serverId.split(':').slice(0,2).join(':')}}</span>
                    <span class="text-xs text-gray-400" :title="a.startedDate">{{relativeTime(a.startedDate || a.createdDate)}}</span>
                  </div>
                  <div v-if="a.error?.message" class="mt-0.5 text-sm text-gray-600 dark:text-gray-400 break-words">{{a.error.message}}</div>
                </li>
              </ol>
            </div>
            <div v-if="batch" class="bg-gray-100 dark:bg-gray-800 text-gray-900 dark:text-gray-100 px-3 py-3">
              <div class="flex items-start justify-between space-x-3">
                <div>
                  <h2 class="font-medium">Batch <span class="font-mono text-sm">{{batch.id}}</span></h2>
                  <p v-if="batch.description" class="text-sm text-gray-600 dark:text-gray-400">{{batch.description}}</p>
                </div>
                <span class="text-sm text-gray-600 dark:text-gray-400 whitespace-nowrap">
                  {{batchFinished}}<span v-if="batch.total"> / {{batch.total}}</span> finished
                </span>
              </div>
            </div>
            <div v-if="batch" class="px-3 py-3 space-y-3">
              <BatchProgress :counts="batchCounts" :total="batch.total" />
              <div class="text-sm text-gray-600 dark:text-gray-400 space-y-0.5">
                <div v-if="batch.callback" title="Runs when every Job in the batch has finished">Callback: {{batch.callback}}</div>
                <div v-if="batch.onSuccess" title="Runs only if every Job in the batch completed">On Success: {{batch.onSuccess}}</div>
                <div v-if="batch.parentBatchId">Parent Batch: {{batch.parentBatchId}}</div>
                <div v-if="batch.cancelledDate" title="No more Jobs can be added to this batch">Cancelled {{relativeTime(batch.cancelledDate)}}</div>
                <div v-else-if="batch.completedDate">Finished {{relativeTime(batch.completedDate)}}</div>
              </div>
              <div v-if="batchActive || batchCounts.failed" class="flex gap-2">
                <SecondaryButton v-if="batchCounts.failed" @click="requeueBatch" :disabled="loading" title="Requeue every failed Job in this batch">
                  Requeue {{batchCounts.failed}} failed
                </SecondaryButton>
                <SecondaryButton v-if="batchActive" @click="cancelBatch" :disabled="loading" class="!text-red-700" title="Cancel every active Job in this batch and stop more being added">
                  Cancel batch
                </SecondaryButton>
              </div>
            </div>
            <div v-if="hasItems(job.meta)" class="bg-gray-100 dark:bg-gray-800 text-gray-900 dark:text-gray-100 px-3 py-3">
              <h2 class="font-medium">Meta</h2>
            </div>
            <div v-if="hasItems(job.meta)" class="px-1">
              <HtmlFormat :value="job.meta" class="not-prose" />
            </div>
            <div v-if="logs || logsTruncated" class="bg-gray-100 dark:bg-gray-800 text-gray-900 dark:text-gray-100 px-3 py-3">
              <div class="flex items-start justify-between space-x-3">
                <h2 class="font-medium">Logs</h2>
                <span v-if="logsTruncated" class="text-xs text-amber-700" title="The configured per-job log limit was reached">Truncated</span>
              </div>
            </div>
            <div v-if="logs" class="flex overflow-auto">
              <div class="pt-2 px-2 relative w-full">
                <pre class="m-0 text-sm rounded py-2 px-3 bg-gray-800 text-gray-100">{{ logs }}</pre>                
              </div>
            </div>
            <div v-if="isRunning(state)" class="flex items-center">
                <Loading class="m-2" imageClass="w-5 h-5"><div class="text-sm font-normal">Running... {{duration}}</div></Loading>
            </div>
            <div ref="bottom" class="bottom"></div>
        </SlideOver>
    `,
    emits:['done','updated'],
    props:['job'],
    setup(props, { emit }) {
        const routes = inject('routes')
        const client = useClient()
        const error = computed(() => props.job && props.job.error ||
            (props.job.errorCode ? {errorCode:props.job.errorCode,message:props.job.errorMessage} : null))

        const bottom = ref()
        const duration = ref(humanifyMs(props.job.durationMs))
        const errorStatus = ref()
        const loading = ref(false)
        const isRunning = state => state === 'Started' || state === 'Executed'
        const logs = ref(props.job.logs || '')
        const logsTruncated = ref(props.job.logsTruncated === true)
        const batch = ref()
        const batchStateCounts = ref({})
        const attempts = ref([])
        const state = ref(props.job.state)
        const errorHelp = computed(() => jobErrorHelp[error.value?.errorCode])
        // A Job that completed after retrying keeps its last error, which its attempt history already shows
        const showError = computed(() => !!error.value && !(state.value === 'Completed' && attempts.value.length))

        function formatArgs(args) {
            Object.keys(args).forEach(key => {
                const val = args[key]
                if (val == null) {
                    delete args[key]
                } else if (key.endsWith('Date') || key === 'runAfter' || key.endsWith('At')) {
                    args[key] = formatDate(val) + ' ' + timeFmt12(toDate(val))
                } else if (key === 'durationMs') {
                    args['duration'] = duration.value
                } else if (key === 'retryDelayMs' || key === 'maxRetryDelayMs') {
                    args[key.replace('Ms','')] = humanifyMs(val)
                    delete args[key]
                } else if (key === 'timeoutSecs') {
                    args['timeout'] = humanifyMs(val * 1000)
                    delete args[key]
                }
            })
            if (args.dependsOnPolicy && !args.dependsOn && !args.dependsOnBatch) delete args.dependsOnPolicy
            return omit(args, ['state', 'durationMs'])
        }
        const basic = computed(() => formatArgs(pick(props.job || {},
            'id,refId,tag,queue,priority,tenantId,concurrencyKey,singletonKey,batchId,dependsOn,dependsOnBatch,dependsOnPolicy,runAfter,expiresAt,createdDate,createdBy,userId,startedDate,' +
            'worker,state,durationMs,completedDate,cancelRequestedDate,attempts,retryLimit,retryBackoff,retryDelayMs,maxRetryDelayMs,' +
            'timeoutSecs,leaseOwner,leaseExpiresAt,traceId,callback,replyTo')))

        const batchCounts = computed(() => {
            const counts = batchStateCounts.value
            if (hasItems(counts)) {
                return {
                    completed: counts.Completed ?? 0,
                    failed:    counts.Failed ?? 0,
                    cancelled: counts.Cancelled ?? 0,
                    running:  (counts.Started ?? 0) + (counts.Executed ?? 0),
                    queued:    counts.Queued ?? 0,
                }
            }
            const b = batch.value
            return b ? { completed:b.completed, failed:b.failed, cancelled:b.cancelled, queued:Math.max(0, b.queued - b.completed - b.failed - b.cancelled) } : {}
        })
        const batchFinished = computed(() => (batchCounts.value.completed ?? 0) + (batchCounts.value.failed ?? 0) + (batchCounts.value.cancelled ?? 0))
        const batchActive = computed(() => !batch.value?.cancelledDate && ((batchCounts.value.queued ?? 0) + (batchCounts.value.running ?? 0)) > 0)
        function updated(job) {
            loading.value = false
            logs.value = job.logs || ''
            logsTruncated.value = job.logsTruncated === true
            state.value = job.state
            duration.value = humanifyMs(job.durationMs)
            console.debug('updated', job, state.value)
            emit('updated', job)
        }
        
        async function replayJob() {
            errorStatus.value = null
            loading.value = true
            const api = await client.api(new AdminReplayJob({ id:props.job.id }))
            loading.value = false
            if (api.succeeded) {
                routes.to({ edit: api.response.jobId })
            } else {
                errorStatus.value = api.error
            }
        }
        async function loadBatch() {
            if (!props.job.batchId) return
            const api = await client.api(new AdminGetJobBatch({ batchId:props.job.batchId }))
            if (api.succeeded) {
                batch.value = api.response.result
                batchStateCounts.value = api.response.stateCounts ?? {}
            }
        }
        async function loadAttempts() {
            // Only Jobs that have been retried or failed have attempt history
            if (!(props.job.attempts > 1 || props.job.state === 'Failed')) return
            const api = await client.api(new AdminGetJobAttempts({ id:props.job.id }))
            if (api.succeeded) attempts.value = api.response.results ?? []
        }
        async function cancelBatch() {
            if (!confirm(`Cancel every active Job in batch '${props.job.batchId}'? No more Jobs can be added to it afterwards.`)) return
            errorStatus.value = null
            loading.value = true
            const api = await client.api(new AdminCancelJobs({ batchId:props.job.batchId }))
            loading.value = false
            if (api.succeeded) await loadBatch()
            else errorStatus.value = api.error
        }
        async function requeueBatch() {
            errorStatus.value = null
            loading.value = true
            const api = await client.api(new AdminRequeueFailedJobs({ batchId:props.job.batchId }))
            loading.value = false
            if (api.succeeded) await loadBatch()
            else errorStatus.value = api.error
        }
        async function requeueJob() {
            errorStatus.value = null
            const api = await client.api(new AdminRequeueFailedJobs({ ids:[props.job.id] }))
            if (api.response) {
                const errorKeys = Object.keys(api.response.errors ?? {}) 
                if (errorKeys.length) {
                    errorStatus.value = api.response.errors[errorKeys[0]]
                    console.debug('errors', api.response.errors)
                } else {
                    while (true) {
                        loading.value = true
                        const apiRefresh = await client.api(new AdminGetJob({ id: props.job.id }))
                        const r = apiRefresh.response
                        const job = r.completed ?? r.failed ?? r.queued ?? r.result
                        console.debug('requeue', job?.state, r.result.state)
                        if (job?.state === 'Queued' || job?.state === 'Started' || job?.state === 'Executed') {
                            updated(job)
                            clearTimeout(updateTimer)
                            refresh()
                            return
                        }
                        await delay(500)
                    }
                }
            } else {
                console.log('api.error', api.error)
                errorStatus.value = api.error
            }
        }
        async function cancelJob() {
            errorStatus.value = null
            const api = await client.api(new AdminCancelJobs({ ids:[props.job.id] }))
            if (api.response) {
                loading.value = true
                const apiRefresh = await client.api(new AdminGetJob({ id: props.job.id }))
                const r = apiRefresh.response
                const job = r.completed ?? r.failed ?? r.queued ?? r.result
                if (job) {
                    updated(job)
                }
            } else {
                errorStatus.value = api.error
            }
        }
        
        function scrollToBottom() {
            if (bottom.value) {
                nextTick(() => {
                    bottom.value.scrollIntoView({ behavior: "smooth", block: "end", inline: "nearest" })
                })
            }
        }
        let updateTimer = null
        async function refresh() {
            const running = isRunning(state.value)
            console.debug('refresh', running)
            if (running) {
                //if (!logs.value) logs.value = props.job.logs || ''
                const api = await client.api(new AdminGetJobProgress({ 
                    id: props.job.id,
                    logStart: logs.value.length
                }))
                if (api.response) {
                    const newLogs = logs.value + (api.response.logs || '')
                    const newDuration = humanifyMs(api.response.durationMs ?? 0)

                    logs.value = newLogs
                    logsTruncated.value = api.response.logsTruncated === true
                    state.value = api.response.state
                    duration.value = newDuration

                    if (!isRunning(api.response.state)) {
                        const apiRefresh = await client.api(new AdminGetJob({ id: props.job.id }))
                        const r = apiRefresh.response
                        const job = r.completed ?? r.failed ?? r.queued ?? r.result
                        // console.log('apiRefresh',job)
                        if (job) {
                            updated(job)
                            if (api.response.logs) {
                                scrollToBottom()
                            }
                            return
                        }
                    }
                }
            }
            updateTimer = setTimeout(refresh, 500)
        }
        onMounted(() => {
            refresh()
            loadBatch()
            loadAttempts()
        })
        onUnmounted(() => clearTimeout(updateTimer))

        return {
            routes, bottom, error, errorHelp, showError, basic, logs, logsTruncated, batch, batchCounts, batchFinished, batchActive,
            attempts, state, duration, errorStatus, loading, jobErrorHelp,
            hasItems, prettyJson, isRunning, requeueJob, replayJob, cancelJob, cancelBatch, requeueBatch,
            humanifyMs, relativeTime,
        }
    }
}

const CancelJobs = {
    template:`
      <ModalDialog id="cancelJobs" size-class="w-full sm:max-w-prose" @done="done">
        <div class="bg-white dark:bg-black px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
          <div class="">
            <div class="mt-3 text-center sm:mt-0 sm:mx-4 sm:text-left">
              <h3 class="text-lg leading-6 font-medium text-gray-900 dark:text-gray-100">Cancel Jobs</h3>

              <fieldset class="mt-4">
                <div class="grid grid-cols-6 gap-6">
                  <div v-if="Object.keys(info?.stateCounts ?? {}).length" class="col-span-6">
                    <div class="mb-2">
                      <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">States</label>
                    </div>
                    <div class="grid grid-cols-3 xl:grid-cols-4 gap-4">
                      <CheckboxInput v-for="(count, state) in info.stateCounts" :id="state" :label="state + ' (' + count + ')'" v-model="states[state]" />
                    </div>
                  </div>

                  <div v-if="Object.keys(info?.workerCounts ?? {}).length" class="col-span-6">
                    <div class="mb-2">
                      <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Workers</label>
                    </div>
                    <div class="grid grid-cols-3 xl:grid-cols-4 gap-4">
                      <CheckboxInput v-for="(count, worker) in info.workerCounts" :id="worker" :label="worker + ' (' + count + ')'" v-model="workers[worker]" />
                    </div>
                  </div>

                  <div v-if="queues.length" class="col-span-6">
                    <div class="mb-2">
                      <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Queues</label>
                    </div>
                    <div class="grid grid-cols-3 xl:grid-cols-4 gap-4">
                      <CheckboxInput v-for="q in queues" :id="'queue-' + q.name" :label="q.name + ' (' + (q.queued + q.running) + ')'" v-model="queueNames[q.name]" />
                    </div>
                  </div>

                  <div class="col-span-6 sm:col-span-3">
                    <TextInput id="cancelTag" label="Tag" v-model="tag" placeholder="Every Job with this tag" />
                  </div>
                  <div class="col-span-6 sm:col-span-3">
                    <TextInput id="cancelBatchId" label="Batch Id" v-model="batchId" placeholder="Every Job in this batch"
                        help="Cancelling a batch also stops more Jobs being added to it" />
                  </div>
                </div>
              </fieldset>
              <p v-if="result" class="mt-4 text-sm text-gray-600 dark:text-gray-400">{{result}}</p>

            </div>
          </div>
        </div>

        <div class="bg-gray-50 dark:bg-gray-900 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
          <PrimaryButton color="red" class="ml-2" @click="cancelJobs">Cancel Jobs</PrimaryButton>
          <SecondaryButton @click="done">
            Close
          </SecondaryButton>
        </div>
      </ModalDialog>
    `,
    emits:['done'],
    setup(props, { emit }) {
        const info = inject('info')
        const client = useClient()
        
        const states = ref({})
        const workers = ref({})
        const queueNames = ref({})
        const queues = ref([])
        const tag = ref('')
        const batchId = ref('')
        const result = ref('')

        function done() {
            emit('done')
        }

        onMounted(async () => {
            const api = await client.api(new AdminGetJobQueues())
            if (api.succeeded) queues.value = (api.response.results ?? []).filter(q => q.queued + q.running > 0)
        })

        async function cancelJobs() {
            const selected = map => Object.keys(map).filter(k => map[k])
            const requests = [
                ...selected(states.value).map(state => new AdminCancelJobs({ state })),
                ...selected(workers.value).map(worker => new AdminCancelJobs({ worker })),
                ...selected(queueNames.value).map(queue => new AdminCancelJobs({ queue })),
            ]
            if (tag.value.trim()) requests.push(new AdminCancelJobs({ tag:tag.value.trim() }))
            if (batchId.value.trim()) requests.push(new AdminCancelJobs({ batchId:batchId.value.trim() }))
            if (!requests.length) return done()

            const apis = await Promise.all(requests.map(request => client.api(request)))
            const cancelled = new Set(apis.flatMap(api => api.response?.results ?? []))
            const failed = apis.find(api => !api.succeeded)
            if (failed || !cancelled.size) {
                result.value = failed?.error?.message ?? 'No matching Jobs were cancelled'
                return
            }
            done()
        }

        return { info, done, cancelJobs, states, workers, queues, queueNames, tag, batchId, result }
    }
}

const RequeueJobs = {
    template:`
      <ModalDialog id="requeueJobs" size-class="w-full sm:max-w-prose" @done="done">
        <div class="bg-white dark:bg-black px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
          <div class="mt-3 text-center sm:mt-0 sm:mx-4 sm:text-left">
            <h3 class="text-lg leading-6 font-medium text-gray-900 dark:text-gray-100">Requeue Failed Jobs</h3>
            <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">
              Requeue every failed Job matching a Tag or Batch, with its previous run state cleared.
            </p>
            <ErrorSummary class="mt-2" :status="error" />
            <fieldset class="mt-4 grid grid-cols-6 gap-6">
              <div class="col-span-6 sm:col-span-3">
                <TextInput id="requeueTag" label="Tag" v-model="tag" />
              </div>
              <div class="col-span-6 sm:col-span-3">
                <TextInput id="requeueBatchId" label="Batch Id" v-model="batchId" />
              </div>
              <div class="col-span-6 sm:col-span-3">
                <TextInput id="requeueFrom" type="date" label="Created on or after" v-model="from" />
              </div>
            </fieldset>
            <p v-if="result" class="mt-4 text-sm text-gray-600 dark:text-gray-400">{{result}}</p>
          </div>
        </div>
        <div class="bg-gray-50 dark:bg-gray-900 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
          <PrimaryButton class="ml-2" @click="requeue" :disabled="!tag.trim() && !batchId.trim() || loading">Requeue</PrimaryButton>
          <SecondaryButton @click="done">Close</SecondaryButton>
        </div>
      </ModalDialog>
    `,
    emits:['done'],
    setup(props, { emit }) {
        const client = useClient()
        const tag = ref('')
        const batchId = ref('')
        const from = ref('')
        const error = ref()
        const result = ref('')
        const loading = ref(false)

        const done = () => emit('done')

        async function requeue() {
            error.value = null
            loading.value = true
            const api = await client.api(new AdminRequeueFailedJobs({
                tag: tag.value.trim() || undefined,
                batchId: batchId.value.trim() || undefined,
                from: from.value || undefined,
            }))
            loading.value = false
            if (!api.succeeded) {
                error.value = api.error
                return
            }
            const errors = Object.keys(api.response.errors ?? {}).length
            result.value = errors
                ? `${errors} Job${errors === 1 ? '' : 's'} could not be requeued`
                : 'Matching failed Jobs have been requeued'
        }

        return { tag, batchId, from, error, result, loading, done, requeue }
    }
}

const components = {
    Markup,
    Truncate,
    DateTime,
    EditLink,
    Request,
    Command,
    Response,
    Duration,
    JobProgress,
    JobDialog,
    CancelJobs,
    RequeueJobs,
    StateBadge,
    BatchProgress,
}

const Queue = {
    components,
    template: `
        <AutoQueryGrid ref="grid" type="BackgroundJob" hide="downloadCsv,copyApiUrl,forms"
            :selectedColumns="selectedColumns"
            :headerTitles="{parentId:'Parent',batchId:'Batch',requestType:'Type',createdDate:'Created',startedDate:'Started',completedDate:'Completed',notifiedDate:'Notified',lastActivityDate:'Last Activity',timeoutSecs:'Timeout',concurrencyKey:'Concurrency Key',tenantId:'Tenant',dependsOnBatch:'Depends On Batch',logsTruncated:'Logs Truncated'}"
            :visibleFrom="{durationMs:'never',requestBody:'never'}"
            @rowSelected="routes.edit = routes.edit == $event.id ? null : $event.id" :isSelected="(row) => routes.edit == row.id">
            <template #progress="job"><JobProgress :job="job" /></template>
            <template #id="{id}">{{id}}</template>
            <template #parentId="{parentId}"><EditLink :id="parentId" @selected="routes.edit=$event" /></template>
            <template #refId="{ refId }"><Truncate class="w-16" :value="refId" /></template>
            <template #tag="{tag}">{{tag}}</template>
            <template #request="job"><Request :job="job" /></template>
            <template #command="job"><Command :job="job" /></template>
            <template #runAfter="{runAfter}"><DateTime :value="runAfter"/></template>
            <template #expiresAt="{expiresAt}"><DateTime :value="expiresAt"/></template>
            <template #leaseExpiresAt="{leaseExpiresAt}"><DateTime :value="leaseExpiresAt"/></template>
            <template #cancelRequestedDate="{cancelRequestedDate}"><DateTime :value="cancelRequestedDate"/></template>
            <template #response="job"><Response :job="job" /></template>
            <template #createdDate="{createdDate}"><DateTime :value="createdDate"/></template>
            <template #worker="{worker}">{{worker}}</template>
            <template #state="{state,cancelRequestedDate}"><StateBadge :state="state" :label="cancelRequestedDate && state !== 'Cancelled' ? 'Cancelling' : null" /></template>
            <template #completedDate="{completedDate}"><DateTime :value="completedDate"/></template>
            <template #attempts="{attempts}">{{attempts}}</template>
            <template #errorCode="{errorCode,errorMessage}"><Markup :title="errorMessage">{{errorCode}}</Markup></template>
            <template #toolbarbuttons="{toolbarButtonClass}">
              <div class="pl-2 mt-1">
                <button type="button" @click="show='cancel'" title="Cancel Jobs" :class="toolbarButtonClass">
                  <svg class="w-5 h-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="M12 2c5.5 0 10 4.5 10 10s-4.5 10-10 10S2 17.5 2 12S6.5 2 12 2m0 2c-1.9 0-3.6.6-4.9 1.7l11.2 11.2c1-1.4 1.7-3.1 1.7-4.9c0-4.4-3.6-8-8-8m4.9 14.3L5.7 7.1C4.6 8.4 4 10.1 4 12c0 4.4 3.6 8 8 8c1.9 0 3.6-.6 4.9-1.7"/></svg>
                </button>
              </div>
            </template>
        </AutoQueryGrid>
        <JobDialog v-if="edit" :job="edit" @done="routes.edit=null" @updated="job => edit=job" />
        <CancelJobs v-if="show=='cancel'" :info="info" @done="show=''" />
    `,
    setup(props) {
        const routes = inject('routes')
        const info = inject('info')
        const client = useClient()
        const grid = ref()
        const edit = ref()
        const show = ref('')
        const selectedColumns = computed(() => {
            const common = 'progress,durationMs,state,queue,priority,worker,id,parentId,refId,singletonKey,concurrencyKey,tenantId,tag,requestType,request,requestBody,command,runAfter,expiresAt,userId,dependsOn,dependsOnBatch,batchId,callback,replyTo,createdDate,status,lastActivityDate,cancelRequestedDate,attempts,logsTruncated'
            return info.value?.capabilities?.includes('leases')
                ? common + ',leaseOwner,leaseExpiresAt'
                : common
        })
        async function update() {
            if (routes.edit) {
                const api = await client.api(new AdminGetJob({ id: routes.edit }))
                if (api.succeeded) {
                    const r = api.response
                    edit.value = r.completed ?? r.failed ?? r.queued ?? r.result
                    return
                }
            }
            edit.value = null
            grid.value?.editDone()
        }
        
        watch(() => routes.edit, update)

        let updateTimer = null
        async function updateGrid(){
            if (grid.value) {
                const searchArgs = grid.value.createRequestArgs()
                searchArgs.take = grid.value.apiPrefs?.take ?? 25
                searchArgs.include = 'total'
                delete searchArgs.fields
                await grid.value.search(searchArgs)
            }
            updateTimer = setTimeout(updateGrid, 1000)
        }

        onMounted(() => {
            update()
            updateGrid()
        })
        onUnmounted(() => clearTimeout(updateTimer))
        return { routes, info, grid, edit, show, selectedColumns }
    }
}

const Summary = {
    components,
    template: `
        <AutoQueryGrid ref="grid" type="JobSummary" hide="copyApiUrl,forms" 
            selectedColumns="id,state,parentId,refId,tag,batchId,requestType,request,command,response,callback,createdDate,worker,queue,priority,tenantId,runAfter,expiresAt,durationMs,completedDate,attempts,errorCode,errorMessage,logsTruncated"
            :visibleFrom="{requestType:'never',callback:'never',errorMessage:'never'}"
            :headerTitles="{parentId:'Parent',batchId:'Batch',createdDate:'Created',completedDate:'Completed',durationMs:'Duration',errorCode:'Error',tenantId:'Tenant',concurrencyKey:'Concurrency Key',logsTruncated:'Logs Truncated'}"
            @rowSelected="routes.edit = routes.edit == $event.id ? null : $event.id" :isSelected="(row) => routes.edit == row.id">
            <template #id="{id}">{{id}}</template>
            <template #parentId="{parentId}"><EditLink :id="parentId" @selected="routes.edit=$event" /></template>
            <template #refId="{ refId }"><Truncate class="w-16" :value="refId" /></template>
            <template #tag="{tag}">{{tag}}</template>
            <template #request="job"><Request :job="job" /></template>
            <template #command="job"><Command :job="job" /></template>
            <template #response="job"><Response :job="job" /></template>
            <template #createdDate="{createdDate}"><DateTime :value="createdDate"/></template>
            <template #worker="{worker}">{{worker}}</template>
            <template #state="{state}"><StateBadge :state="state" /></template>
            <template #durationMs="{durationMs}"><Duration :value="durationMs" /></template>
            <template #completedDate="{completedDate}"><DateTime :value="completedDate"/></template>
            <template #runAfter="{runAfter}"><DateTime :value="runAfter"/></template>
            <template #expiresAt="{expiresAt}"><DateTime :value="expiresAt"/></template>
            <template #attempts="{attempts}">{{attempts}}</template>
            <template #errorCode="{errorCode,errorMessage}"><Markup :title="jobErrorHelp[errorCode] ?? errorMessage" class="text-red-700 dark:text-red-400">{{errorCode}}</Markup></template>
        </AutoQueryGrid>
        <JobDialog v-if="edit" :job="edit" @done="routes.edit=null" @updated="job => edit=job" />
    `,
    setup() {
        const routes = inject('routes')
        const client = useClient()
        const grid = ref()
        const edit = ref()

        async function update() {
            if (routes.edit) {
                const api = await client.api(new AdminGetJob({ id: routes.edit }))
                if (api.succeeded) {
                    console.debug('api.response', api.response.result)
                    const r = api.response
                    edit.value = r.completed ?? r.failed ?? r.queued ?? r.result
                    return
                }
            }
            edit.value = null
            grid.value?.editDone()
        }
        
        watch(() => routes.edit, update)

        onMounted(update)

        return { routes, grid, formatDate, time, toDate, humanifyMs, edit, jobErrorHelp }
    }
}
const Completed = {
    components,
    props:['month'],
    template: `
        <AutoQueryGrid ref="grid" type="CompletedJob" hide="copyApiUrl,forms"
            selectedColumns="id,state,parentId,refId,tag,queue,requestType,request,command,userId,dependsOn,batchId,response,callback,replyTo,createdDate,worker,startedDate,status,durationMs,completedDate,notifiedDate,attempts,lastActivityDate"
            :headerTitles="{parentId:'Parent',batchId:'Batch',requestType:'Type',createdDate:'Created',startedDate:'Started',completedDate:'Completed',notifiedDate:'Notified',lastActivityDate:'Last Activity',timeoutSecs:'Timeout'}"
            @rowSelected="routes.edit = routes.edit == $event.id ? null : $event.id" :isSelected="(row) => routes.edit == row.id"
            :filters="{month}">
            <template #parentId="{parentId}"><EditLink :id="parentId" @selected="routes.edit = $event" /></template>
            <template #refId="{ refId }"><Truncate class="w-16" :value="refId" /></template>
            <template #tag="{tag}">{{tag}}</template>
            <template #request="job"><Request :job="job" /></template>
            <template #command="job"><Command :job="job" /></template>
            <template #response="job"><Response :job="job" /></template>
            <template #createdDate="{createdDate}"><DateTime :value="createdDate"/></template>
            <template #startedDate="{startedDate}"><DateTime :value="startedDate"/></template>
            <template #worker="{worker}">{{worker}}</template>
            <template #state="{state}"><StateBadge :state="state" /></template>
            <template #durationMs="{durationMs}"><Duration :value="durationMs" /></template>
            <template #completedDate="{completedDate}"><DateTime :value="completedDate"/></template>
            <template #notifiedDate="{notifiedDate}"><DateTime :value="notifiedDate"/></template>
            <template #lastActivityDate="{lastActivityDate}"><DateTime :value="lastActivityDate"/></template>
        </AutoQueryGrid>
        <JobDialog v-if="edit" :job="edit" @done="routes.edit=null" @updated="job => edit=job" />
    `,
    setup(props) {
        const routes = inject('routes')
        const client = useClient()
        const grid = ref()
        const edit = ref()

        async function update() {
            if (routes.edit) {
                const api = await client.api(new AdminGetJob({ id: routes.edit }))
                if (api.succeeded) {
                    const r = api.response
                    edit.value = r.completed ?? r.failed ?? r.queued ?? r.result
                    return
                }
            }
            edit.value = null
            grid.value?.editDone()
        }
        
        watch(() => routes.edit, update)
        watch(() => props.month, (newValue,oldValue) => {
            nextTick(() => grid.value?.update())
        })
        onMounted(update)
        return { routes, grid, edit }
    }
}
const Failed = {
    components,
    props:['month'],
    template: `
        <AutoQueryGrid ref="grid" type="FailedJob" hide="copyApiUrl,forms"
            selectedColumns="id,state,parentId,refId,tag,queue,dependsOn,batchId,requestType,request,command,userId,response,callback,replyTo,createdDate,worker,startedDate,status,durationMs,completedDate,notifiedDate,lastActivityDate,attempts,retryLimit,timeoutSecs,errorCode,error"
            :visibleFrom="{error:'never'}"
            :headerTitles="{parentId:'Parent',batchId:'Batch',requestType:'Type',createdDate:'Created',startedDate:'Started',completedDate:'Completed',notifiedDate:'Notified',lastActivityDate:'Last Activity',timeoutSecs:'Timeout',errorCode:'Error'}"
            @rowSelected="routes.edit = routes.edit == $event.id ? null : $event.id" :isSelected="(row) => routes.edit == row.id"
            :filters="{month}">
            <template #parentId="{parentId}"><EditLink :id="parentId" @selected="routes.edit = $event" /></template>
            <template #state="{state}"><StateBadge :state="state" /></template>
            <template #durationMs="{durationMs}"><Duration :value="durationMs" /></template>
            <template #createdDate="{createdDate}"><DateTime :value="createdDate"/></template>
            <template #startedDate="{startedDate}"><DateTime :value="startedDate"/></template>
            <template #completedDate="{completedDate}"><DateTime :value="completedDate"/></template>
            <template #errorCode="{errorCode,error}"><Markup :title="jobErrorHelp[errorCode] ?? error?.message" class="text-red-700 dark:text-red-400">{{errorCode}}</Markup></template>
            <template #toolbarbuttons="{toolbarButtonClass}">
              <div class="pl-2 mt-1">
                <button type="button" @click="show='requeue'" title="Requeue Failed Jobs by Tag or Batch" :class="toolbarButtonClass">
                  <svg class="w-5 h-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12a9 9 0 1 0 9-9a9.75 9.75 0 0 0-6.74 2.74L3 8m0-5v5h5"/></svg>
                </button>
              </div>
            </template>
        </AutoQueryGrid>
        <JobDialog v-if="edit" :job="edit" @done="routes.edit=null" @updated="job => edit=job" />
        <RequeueJobs v-if="show=='requeue'" @done="show=''; grid?.update()" />
    `,
    setup(props) {
        const routes = inject('routes')
        const client = useClient()
        const grid = ref()
        const edit = ref()
        const show = ref('')

        async function update() {
            if (routes.edit) {
                const api = await client.api(new AdminGetJob({ id: routes.edit }))
                if (api.succeeded) {
                    const r = api.response
                    edit.value = r.completed ?? r.failed ?? r.queued ?? r.result
                    return
                }
            }
            edit.value = null
            grid.value?.editDone()
        }
        watch(() => routes.edit, update)
        watch(() => props.month, (newValue,oldValue) => {
            nextTick(() => grid.value?.update())
        })
        onMounted(update)

        return { routes, grid, edit, show, jobErrorHelp }
    }
}
const History = {
    components: {
        Summary,
        Completed,
        Failed,
    },
    template: `
    <div class="border-b border-gray-200">
      <nav class="-mb-px flex space-x-8" aria-label="Tabs">
        <span v-for="tab in tabs" :key="tabs" @click="routes.to({page:tab.page,skip:undefined})" 
            :class="['cursor-pointer flex whitespace-nowrap border-b-2 px-1 py-4 text-sm font-medium', 
            tab.page===routes.page ? 'border-indigo-500 text-indigo-600' : 'border-transparent text-gray-500 hover:border-gray-200 hover:text-gray-700']">
          {{ tab.name }}
          <span v-if="info?.tableCounts[tab.table] != null" 
            :class="['ml-3 hidden rounded-full px-2.5 py-0.5 text-xs font-medium md:inline-block',
                tab.page===routes.page ? 'bg-indigo-100 text-indigo-600' : 'bg-gray-100 text-gray-900']">
            {{ info?.tableCounts[tab.table] }}
          </span>
        </span>
      </nav>
    </div>
    <div v-if="monthDbEntries.length && (routes.page==='completed'||routes.page==='failed')" class="relative">
      <div class="absolute right-0 -mt-12">
        <SelectInput id="month" label="" v-model="monthDb" :entries="monthDbEntries" />
      </div>
    </div>
    <Completed v-if="routes.page==='completed'" :month="monthDb" />
    <Failed v-else-if="routes.page==='failed'" :month="monthDb" />
    <Summary v-else />
    `,
    props:[],
    setup() {
        const tabs = [
            { name: 'Summary',   page: '',   table:'JobSummary' },
            { name: 'Completed', page: 'completed', table:'CompletedJob' },
            { name: 'Failed',    page: 'failed',    table:'FailedJob' },
        ]
        const routes = inject('routes')
        const info = inject('info')
        // info is null until the first AdminJobInfo response when linked to directly
        const monthDb = ref(getPrefs().monthDb ?? info.value?.monthDbs?.[0])
        const monthDbEntries = computed(() => {
            return info.value?.monthDbs?.map(x => ({
                key:x, 
                value: toDate(x).toLocaleString('default', { month: 'long' }) + ' ' + toDate(x).getFullYear() 
            })) ?? []
        })
        
        watch(() => monthDb.value, () => setPrefs({monthDb:monthDb.value}))

        return { routes, tabs, info, monthDb, monthDbEntries }
    }
}
const ScheduledTasks = {
    components,
    template: `
        <AutoQueryGrid ref="grid" type="ScheduledTask" hide="copyApiUrl,forms"
            selectedColumns="id,name,enabled,interval,nextRun,lastJobId,lastRun,lastRunState,lastRunDurationMs,runCount,startDate,timeZoneId,misfirePolicy,overlapPolicy,requestType,command,request,lastErrorCode,cronExpression,maxRuns,endDate,lastErrorMessage"
            :visibleFrom="{cronExpression:'never',maxRuns:'never',endDate:'never',lastErrorMessage:'never'}"
            :headerTitles="{enabled:'Status',interval:'Schedule',lastJobId:'Last Job',nextRun:'Next Run',lastRun:'Last Run',lastRunState:'Result',lastRunDurationMs:'Duration',runCount:'Runs',startDate:'Active',timeZoneId:'Time Zone',misfirePolicy:'Misfire',overlapPolicy:'Overlap',lastErrorCode:'Last Error'}">
            <template #enabled="task">
                <div class="flex items-center gap-x-2">
                    <button type="button" @click.stop="setEnabled(task)" :disabled="updating === task.id"
                        :class="[task.enabled ? 'text-green-700 bg-green-50 ring-green-600/20 hover:bg-green-100' : 'text-gray-600 bg-gray-100 ring-gray-500/20 hover:bg-gray-200', 'rounded-md px-2 py-0.5 text-xs font-medium ring-1 ring-inset disabled:opacity-50 whitespace-nowrap']"
                        :title="task.enabled ? 'Pause this schedule' : (stoppedReason(task) ? stoppedReason(task) + ', click to resume' : 'Resume this schedule')">
                        {{ task.enabled ? 'Enabled' : stoppedReason(task) ? 'Finished' : 'Paused' }}
                    </button>
                    <button type="button" v-if="task.enabled" @click.stop="runNow(task)" :disabled="updating === task.id"
                        class="inline-flex items-center gap-x-1 rounded-md px-2 py-0.5 text-xs font-medium text-indigo-700 bg-indigo-50 hover:bg-indigo-100 dark:bg-indigo-900/40 dark:text-indigo-300 disabled:opacity-50 whitespace-nowrap"
                        title="Run this task now, without changing its schedule">
                        Run Now
                    </button>
                </div>
            </template>
            <template #interval="{interval,cronExpression}">
                <span v-if="cronExpression" class="font-mono text-xs" :title="'Cron: ' + cronExpression">{{cronExpression}}</span>
                <span v-else-if="interval" :title="interval">every {{formatTimeSpan(interval)}}</span>
            </template>
            <template #lastJobId="{lastJobId}"><EditLink :id="lastJobId" @selected="routes.edit = $event" /></template>
            <template #lastRun="{lastRun}"><DateTime :value="lastRun"/></template>
            <template #lastRunState="{lastRunState}"><StateBadge :state="lastRunState" /></template>
            <template #lastRunDurationMs="{lastRunDurationMs}">{{ lastRunDurationMs == null ? '' : humanifyMs(lastRunDurationMs) }}</template>
            <template #nextRun="{nextRun,enabled}"><span v-if="enabled && nextRun" :title="nextRun">{{relativeTime(nextRun)}}</span></template>
            <template #runCount="{runCount,maxRuns}">{{runCount}}<span v-if="maxRuns" class="text-gray-400"> / {{maxRuns}}</span></template>
            <template #startDate="{startDate,endDate}">
                <span v-if="startDate || endDate" class="text-xs whitespace-nowrap">
                    <DateTime v-if="startDate" :value="startDate" class="inline" /><span v-else>&hellip;</span>
                    &rarr;
                    <DateTime v-if="endDate" :value="endDate" class="inline" /><span v-else>&hellip;</span>
                </span>
            </template>
            <template #lastErrorCode="{lastErrorCode,lastErrorMessage}"><Markup :title="lastErrorMessage" class="text-red-700 dark:text-red-400">{{lastErrorCode}}</Markup></template>
        </AutoQueryGrid>
        <JobDialog v-if="edit" :job="edit" @done="routes.edit=null" @updated="job => edit = job" />
    `,
    setup() {
        const routes = inject('routes')
        const client = useClient()
        const grid = ref()
        const edit = ref()
        const updating = ref()

        // A disabled task that reached its bounds was stopped by its schedule, not paused by an operator
        function stoppedReason(task) {
            if (task.enabled) return null
            if (task.maxRuns && task.runCount >= task.maxRuns) return `Ran ${task.runCount} of MaxRuns ${task.maxRuns}`
            if (task.endDate && toDate(task.endDate) <= new Date()) return `Passed its EndDate`
            return null
        }

        async function setEnabled(task) {
            await updateTask(task, new AdminUpdateScheduledTask({ id:task.id, enabled:!task.enabled }))
        }

        async function runNow(task) {
            await updateTask(task, new AdminUpdateScheduledTask({ id:task.id, runNow:true }))
        }

        async function updateTask(task, request) {
            updating.value = task.id
            const api = await client.api(request)
            updating.value = null
            if (api.succeeded) {
                Object.assign(task, api.response.result)
                grid.value?.editDone()
            }
        }
        
        async function update() {
            if (routes.edit) {
                const api = await client.api(new AdminGetJob({ id: routes.edit }))
                if (api.succeeded) {
                    const r = api.response
                    edit.value = r.completed ?? r.failed ?? r.queued ?? r.result
                    return
                }
            }
            edit.value = null
            grid.value?.editDone()
        }
        
        watch(() => routes.edit, update)
        onMounted(update)
        
        return { routes, grid, edit, updating, setEnabled, runNow, stoppedReason, humanifyMs, relativeTime, formatTimeSpan }
    }
}
const Queues = {
    components,
    template: `
        <div class="py-4">
          <div v-if="error" class="mb-4"><ErrorSummary :status="error" /></div>
          <div class="mb-4 flex flex-wrap items-end justify-between gap-4">
            <p class="text-sm text-gray-500 dark:text-gray-400 max-w-3xl">
              Pause a queue to stop dispatching its Jobs without losing them, change how many Jobs it runs at once,
              or cap how many it may start per interval. Changes take effect on every node.
            </p>
          </div>
          <div class="overflow-x-auto rounded-lg ring-1 ring-gray-200 dark:ring-gray-700">
          <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead class="bg-gray-50 dark:bg-gray-800">
              <tr>
                <th class="px-2 py-2.5 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">Queue</th>
                <th class="px-2 py-2.5 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">Status</th>
                <th class="px-2 py-2.5 text-right text-xs font-medium text-gray-500 uppercase tracking-wide">Queued</th>
                <th class="px-2 py-2.5 text-right text-xs font-medium text-gray-500 uppercase tracking-wide">Running</th>
                <th class="px-2 py-2.5 text-right text-xs font-medium text-gray-500 uppercase tracking-wide" title="How long the oldest Job has been waiting">Oldest</th>
                <th class="px-2 py-2.5 text-right text-xs font-medium text-gray-500 uppercase tracking-wide" title="Max Jobs this queue runs at once">Concurrency</th>
                <th class="px-2 py-2.5 text-left text-xs font-medium text-gray-500 uppercase tracking-wide" title="Max Jobs this queue may start per interval, across every node">Rate Limit</th>
                <th class="px-2 py-2.5"><span class="sr-only">Actions</span></th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 dark:divide-gray-800 bg-white dark:bg-black">
              <tr v-for="q in results" :key="q.name" :class="q.paused ? 'bg-amber-50/40 dark:bg-amber-900/10' : ''">
                <td class="px-2 py-2.5 text-sm font-medium text-gray-900 dark:text-gray-100 whitespace-nowrap">
                  {{q.name}}
                  <div v-if="q.modifiedDate" class="max-w-40 truncate text-xs font-normal text-gray-400" :title="changedBy(q)">{{changedBy(q)}}</div>
                </td>
                <td class="px-2 py-2.5 text-sm">
                  <StateBadge :state="q.paused ? 'Cancelled' : q.running ? 'Started' : q.queued ? 'Queued' : 'Completed'"
                    :label="q.paused ? 'Paused' : q.running ? 'Processing' : q.queued ? 'Waiting' : 'Idle'" />
                </td>
                <td class="px-2 py-2.5 text-sm text-right tabular-nums">{{humanifyNumber(q.queued)}}</td>
                <td class="px-2 py-2.5 text-sm text-right tabular-nums">{{q.running}}</td>
                <td class="px-2 py-2.5 text-sm text-right tabular-nums whitespace-nowrap" :class="q.queued > 0 && isBacklogged(q) ? 'text-amber-700 font-medium' : 'text-gray-600 dark:text-gray-400'"
                    :title="q.oldestQueued">
                  {{ q.queued > 0 ? formatTimeSpan(q.oldestQueued) : '' }}
                </td>
                <td class="px-2 py-2.5 text-sm text-right whitespace-nowrap">
                  <input type="number" min="1" :value="q.concurrency" @change="setConcurrency(q, $event.target.value)"
                         :disabled="updating === q.name" class="w-14 py-1 text-right rounded-md border-gray-300 dark:border-gray-600 dark:bg-gray-900 text-sm" />
                  <span class="inline-block w-3 ml-1 text-xs text-indigo-500" :title="q.concurrencyOverridden ? 'Overridden at runtime' : ''">{{ q.concurrencyOverridden ? '*' : '' }}</span>
                </td>
                <td class="px-2 py-2.5 text-sm whitespace-nowrap">
                  <div class="flex items-center gap-x-1 text-gray-500">
                    <input type="number" min="0" :value="q.rateLimit ?? ''" placeholder="&infin;" @change="setRateLimit(q, $event.target.value, q.rateLimitSecs)"
                         :disabled="updating === q.name" class="w-14 py-1 text-right rounded-md border-gray-300 dark:border-gray-600 dark:bg-gray-900 text-sm"
                         title="Jobs per interval, empty or 0 for no limit" />
                    <span class="text-xs">/</span>
                    <select :value="q.rateLimitSecs ?? 60" @change="setRateLimit(q, q.rateLimit, $event.target.value)" :disabled="updating === q.name || !q.rateLimit"
                        class="py-1 pl-2 pr-7 rounded-md border-gray-300 dark:border-gray-600 dark:bg-gray-900 text-sm disabled:opacity-50">
                      <option v-for="w in windowOptions(q.rateLimitSecs)" :value="w.secs">{{w.label}}</option>
                    </select>
                  </div>
                </td>
                <td class="px-2 py-2.5 text-sm text-right whitespace-nowrap">
                  <button type="button" @click="togglePaused(q)" :disabled="updating === q.name"
                          :class="[q.paused ? 'text-green-700 bg-green-50 hover:bg-green-100' : 'text-amber-800 bg-amber-50 hover:bg-amber-100', 'rounded-md px-2 py-1 text-xs font-medium disabled:opacity-50']">
                    {{ q.paused ? 'Resume' : 'Pause' }}
                  </button>
                  <button type="button" v-if="q.queued || q.running" @click="cancelQueued(q)" :disabled="updating === q.name"
                          class="ml-2 rounded-md px-2 py-1 text-xs font-medium text-red-700 hover:bg-red-50 disabled:opacity-50"
                          title="Cancel every Job on this queue">
                    Cancel all
                  </button>
                </td>
              </tr>
              <tr v-if="!results.length">
                <td colspan="8" class="px-3 py-8 text-center text-sm text-gray-500">No queues</td>
              </tr>
            </tbody>
          </table>
          </div>
        </div>
    `,
    setup() {
        const client = useClient()
        const results = ref([])
        const error = ref()
        const updating = ref()

        const windows = [{ secs:1, label:'sec' }, { secs:60, label:'min' }, { secs:3600, label:'hour' }, { secs:86400, label:'day' }]
        const windowOptions = secs => secs && !windows.some(w => w.secs === secs)
            ? [...windows, { secs, label: `${secs}s` }]
            : windows

        // Waiting longer than a minute is worth drawing attention to
        const isBacklogged = q => (timeSpanMs(q.oldestQueued) ?? 0) > 60_000

        async function refresh() {
            const api = await client.api(new AdminGetJobQueues())
            error.value = api.error
            if (api.succeeded) results.value = api.response.results
        }

        async function update(q, request) {
            updating.value = q.name
            const api = await client.api(request)
            updating.value = null
            error.value = api.error
            if (api.succeeded) await refresh()
        }

        const togglePaused = q => update(q, new AdminUpdateJobQueue({ name:q.name, paused:!q.paused }))
        const setConcurrency = (q, value) => {
            const concurrency = parseInt(value)
            if (!(concurrency > 0) || concurrency === q.concurrency) return
            return update(q, new AdminUpdateJobQueue({ name:q.name, concurrency }))
        }
        const setRateLimit = (q, limit, secs) => {
            const rateLimit = parseInt(limit) || 0
            const rateLimitSecs = parseInt(secs) || 60
            if (rateLimit === (q.rateLimit ?? 0) && rateLimitSecs === (q.rateLimitSecs ?? 60)) return
            return update(q, new AdminUpdateJobQueue({ name:q.name, rateLimit, rateLimitSecs }))
        }
        const changedBy = q => 'changed ' + relativeTime(q.modifiedDate) + (q.modifiedBy ? ' by ' + q.modifiedBy : '')
        async function cancelQueued(q) {
            if (!confirm(`Cancel all ${q.queued + q.running} Jobs on the '${q.name}' queue?`)) return
            await update(q, new AdminCancelJobs({ queue:q.name }))
        }

        let timer = null
        onMounted(async () => {
            await refresh()
            timer = setInterval(refresh, 5000)
        })
        onUnmounted(() => clearInterval(timer))

        return { results, error, updating, togglePaused, setConcurrency, setRateLimit, cancelQueued,
            windowOptions, isBacklogged, changedBy, formatTimeSpan, relativeTime, humanifyNumber }
    }
}
const Nodes = {
    components,
    template: `
        <div class="py-4 sm:px-4">
          <div v-if="error" class="mb-4"><ErrorSummary :status="error" /></div>
          <div class="mb-4 flex flex-wrap items-end justify-between gap-4">
            <p class="text-sm text-gray-500 dark:text-gray-400 max-w-3xl">
              App Servers processing Jobs and when they last reported in. Drain a server before taking it out of
              service to have it finish the Jobs it has without taking any more.
            </p>
            <CheckboxInput v-if="stoppedCount" id="showStopped" :label="'Show ' + stoppedCount + ' stopped'" v-model="showStopped" />
          </div>
          <div class="overflow-x-auto rounded-lg ring-1 ring-gray-200 dark:ring-gray-700">
          <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead class="bg-gray-50 dark:bg-gray-800">
              <tr>
                <th class="px-3 py-2.5 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">Server</th>
                <th class="px-3 py-2.5 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">Status</th>
                <th class="px-3 py-2.5 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">Running</th>
                <th class="px-3 py-2.5 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">Queues</th>
                <th class="px-3 py-2.5 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">Last Heartbeat</th>
                <th class="px-3 py-2.5 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">Started</th>
                <th class="px-3 py-2.5 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">Version</th>
                <th class="px-3 py-2.5"><span class="sr-only">Actions</span></th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 dark:divide-gray-800 bg-white dark:bg-black">
              <tr v-for="node in visible" :key="node.serverId" :class="node.status === 'stopped' ? 'opacity-60' : ''">
                <td class="px-3 py-2.5 text-sm">
                  <div class="font-medium text-gray-900 dark:text-gray-100">{{node.machineName ?? node.serverId}}</div>
                  <div class="text-xs text-gray-500 font-mono" :title="node.serverId">pid {{node.processId}} &middot; {{shortId(node.serverId)}}</div>
                </td>
                <td class="px-3 py-2.5 text-sm">
                  <span :class="[statusStyle[node.status].cls, 'rounded-md px-2 py-0.5 text-xs font-medium ring-1 ring-inset whitespace-nowrap']" :title="statusStyle[node.status].title">
                    {{statusStyle[node.status].label}}
                  </span>
                </td>
                <td class="px-3 py-2.5 text-sm tabular-nums whitespace-nowrap">{{node.runningJobs}}<span class="text-gray-400"> / {{node.concurrency}}</span></td>
                <td class="px-3 py-2.5 text-sm">
                  <span v-if="node.queues?.length">{{node.queues.join(', ')}}</span>
                  <span v-else class="text-xs text-gray-400">All</span>
                </td>
                <td class="px-3 py-2.5 text-sm text-gray-600 dark:text-gray-400 whitespace-nowrap" :title="node.lastHeartbeat">{{relativeTime(node.lastHeartbeat)}}</td>
                <td class="px-3 py-2.5 text-sm text-gray-600 dark:text-gray-400 whitespace-nowrap" :title="node.startedDate">
                  {{relativeTime(node.startedDate)}}
                  <div v-if="node.stoppedDate" class="text-xs text-gray-400" :title="node.stoppedDate">stopped {{relativeTime(node.stoppedDate)}}</div>
                </td>
                <td class="px-3 py-2.5 text-sm text-gray-600 dark:text-gray-400">{{node.version}}</td>
                <td class="px-3 py-2.5 text-sm text-right whitespace-nowrap">
                  <button v-if="node.status !== 'stopped'" type="button" @click="toggleDraining(node)" :disabled="updating === node.serverId"
                    :class="[node.draining ? 'text-green-700 bg-green-50 hover:bg-green-100' : 'text-amber-800 bg-amber-50 hover:bg-amber-100', 'rounded-md px-2 py-1 text-xs font-medium disabled:opacity-50']"
                    :title="node.draining ? 'Resume taking new Jobs' : 'Finish running Jobs without taking any more'">
                    {{ node.draining ? 'Undrain' : 'Drain' }}
                  </button>
                </td>
              </tr>
              <tr v-if="!visible.length">
                <td colspan="8" class="px-3 py-8 text-center text-sm text-gray-500">No App Servers have reported in</td>
              </tr>
            </tbody>
          </table>
          </div>
        </div>
    `,
    setup() {
        const client = useClient()
        const results = ref([])
        const error = ref()
        const updating = ref()
        const showStopped = ref(false)
        // Matches the default NodeTimeoutSecs used by the server's health check
        const nodeTimeoutMs = 60_000

        const statusStyle = {
            alive:    { label:'Alive',         cls:'bg-green-50 text-green-700 ring-green-600/20', title:'Reported in within the last minute' },
            draining: { label:'Draining',      cls:'bg-amber-50 text-amber-800 ring-amber-600/20', title:'Finishing its Jobs without taking any more' },
            stale:    { label:'Not Reporting', cls:'bg-red-50 text-red-700 ring-red-600/20',       title:'Has not reported in for over a minute without shutting down cleanly' },
            stopped:  { label:'Stopped',       cls:'bg-gray-100 text-gray-600 ring-gray-500/20',   title:'Shut down cleanly' },
        }

        function statusOf(node) {
            if (node.stoppedDate) return 'stopped'
            if (Date.now() - toDate(node.lastHeartbeat).getTime() > nodeTimeoutMs) return 'stale'
            return node.draining ? 'draining' : 'alive'
        }

        const order = { alive:0, draining:1, stale:2, stopped:3 }
        const nodes = computed(() => results.value
            .map(node => ({ ...node, status:statusOf(node) }))
            .sort((a,b) => order[a.status] - order[b.status] || (b.lastHeartbeat > a.lastHeartbeat ? 1 : -1)))
        const visible = computed(() => showStopped.value ? nodes.value : nodes.value.filter(x => x.status !== 'stopped'))
        const stoppedCount = computed(() => nodes.value.filter(x => x.status === 'stopped').length)

        const shortId = serverId => (serverId ?? '').split(':').pop().substring(0, 8)

        async function refresh() {
            const api = await client.api(new AdminGetJobNodes())
            error.value = api.error
            if (api.succeeded) results.value = api.response.results ?? []
        }

        async function toggleDraining(node) {
            if (!node.draining && !confirm(`Drain ${node.machineName ?? node.serverId}? It will finish its running Jobs without taking any more.`)) return
            updating.value = node.serverId
            const api = await client.api(new AdminUpdateJobNode({ serverId:node.serverId, draining:!node.draining }))
            updating.value = null
            error.value = api.error
            if (api.succeeded) await refresh()
        }

        let timer = null
        onMounted(async () => {
            await refresh()
            timer = setInterval(refresh, 5000)
        })
        onUnmounted(() => clearInterval(timer))

        return { visible, stoppedCount, error, updating, showStopped, statusStyle, shortId, toggleDraining, relativeTime }
    }
}
const Dashboard = {
    components,
    template:`
        <div>
            <div>
              <div class="sm:hidden">
                <label for="tabs" class="sr-only">Select a tab</label>
                <select @change="routes.to({tab:undefined,period:$event.target.value})" class="block w-full rounded-md border-gray-300 focus:border-indigo-500 focus:ring-indigo-500">
                  <option v-for="(period,name) in periods" :value="period">{{name}}</option>
                </select>
              </div>
              <div class="hidden sm:block">
                <nav class="flex space-x-4" aria-label="Tabs">
                  <span v-for="(period,name) in periods" @click="routes.to({tab:undefined,period})" 
                    :class="[period === routes.period ? 'bg-indigo-100 text-indigo-700':'text-gray-500 hover:text-gray-700', 'cursor-pointer select-none rounded-md px-3 py-2 text-sm font-medium']" :title="name">{{periodLabels[name]}}</span>
                </nav>
              </div>
            </div>
            <h2 class="lg:block pt-4 mb-2 text-3xl font-bold leading-tight tracking-tight text-gray-900">{{periodLabel}}</h2>
        </div>
        
        <div v-if="results.waitTimes?.count" class="mb-8">
            <h4 class="mt-4 font-semibold text-gray-500" title="Time between a Job being queued and starting">Wait Times</h4>
            <dl class="mt-2 grid grid-cols-2 gap-4 sm:grid-cols-4 max-w-2xl">
                <div class="rounded-lg bg-gray-50 px-4 py-3">
                    <dt class="text-xs font-medium text-gray-500">Jobs</dt>
                    <dd class="mt-1 text-xl font-semibold">{{humanifyNumber(results.waitTimes.count)}}</dd>
                </div>
                <div class="rounded-lg bg-gray-50 px-4 py-3">
                    <dt class="text-xs font-medium text-gray-500">Average</dt>
                    <dd class="mt-1 text-xl font-semibold">{{humanifyMs(results.waitTimes.avgMs)}}</dd>
                </div>
                <div class="rounded-lg bg-gray-50 px-4 py-3">
                    <dt class="text-xs font-medium text-gray-500">Longest</dt>
                    <dd class="mt-1 text-xl font-semibold">{{humanifyMs(results.waitTimes.maxMs)}}</dd>
                </div>
                <div class="rounded-lg px-4 py-3" :class="results.waitTimes.waitingMs ? 'bg-amber-50' : 'bg-gray-50'">
                    <dt class="text-xs font-medium text-gray-500" title="Longest a Job is still waiting right now">Waiting Now</dt>
                    <dd class="mt-1 text-xl font-semibold">{{results.waitTimes.waitingMs ? humanifyMs(results.waitTimes.waitingMs) : '-'}}</dd>
                </div>
            </dl>
        </div>
        <div v-if="results.queues.length">
            <h4 class="mt-4 font-semibold text-gray-500">Queue Stats</h4>
            <DataGrid :items="results.queues" selectedColumns="name,total,completed,retries,failed,cancelled">
                <template #name="{ name }"><Truncate class="w-40 sm:w-80" :value="name" /></template>
            </DataGrid>
        </div>
        <div v-if="isToday && results.today.length" class="mb-8">
            <h4 class="mt-4 font-semibold text-gray-500">24 hour activity</h4>
            <div style="max-width:1024px;max-height:512px">
                <canvas ref="elChart"></canvas>
            </div>
        </div>
        <div v-if="results.commands.length">
            <h4 class="mt-4 font-semibold text-gray-500">Commands Stats</h4>
            <DataGrid :items="results.commands" selectedColumns="name,total,completed,retries,failed,cancelled">
                <template #name="{ name }"><Truncate class="w-40 sm:w-80" :value="name" /></template>
            </DataGrid>
        </div>
        <div v-if="results.apis.length">
            <h4 class="mt-4 font-semibold text-gray-500">API Stats</h4>
            <DataGrid :items="results.apis" selectedColumns="name,total,completed,retries,failed,cancelled">
                <template #name="{ name }"><Truncate class="w-40 sm:w-80" :value="name" /></template>
            </DataGrid>
        </div>
        <div v-if="results.workers.length">
            <h4 class="mt-4 font-semibold text-gray-500">Worker Stats</h4>
            <DataGrid :items="results.workers" selectedColumns="name,total,completed,retries,failed,cancelled">
                <template #name="{ name }"><Truncate class="w-40 sm:w-80" :value="name" /></template>
            </DataGrid>
        </div>
    `,
    setup() {
        const client = useClient()
        const results = ref({ commands:[], apis:[], workers:[], queues:[], today:[], waitTimes:null })
        const routes = inject('routes')

        const dayMs = 24 * 60 * 60 * 1000
        const now = new Date()
        const periodToday = periodArg(day(now))
        const periods = {
            Today:             '',
            Yesterday:         periodArg(day(new Date(now.getTime() - dayMs)), day(now)),
            'Last 7 days':     periodArg(day(new Date(now.getTime() - 7 * dayMs))),
            'Last 4 weeks':    periodArg(day(new Date(now.getTime() - 4 * 7 * dayMs))),
            'Last 3 months':   periodArg(day(setMonth(now.getMonth() - 3))),
            'Last 12 months':  periodArg(day(setMonth(now.getMonth() - 12))),
            'Month to date':   periodArg(day(new Date(now.getFullYear(), now.getMonth(), 1))),
            'Quarter to date': periodArg(day(startOfCurrentQuarter())),
            'Year to date':    periodArg(day(new Date(now.getFullYear(), 0, 1))),
            'Last year':       periodArg(day(new Date(now.getFullYear()-1, 0, 1)), day(new Date(now.getFullYear(), 0, 1))),
            'All time':        periodArg(),
        }
        const abbr = 'T,Y,1W,4W,3M,1Y,MTD,QTD,YTD,LY,ALL'.split(',')
        const periodLabels = {}
        Object.keys(periods).forEach((period,i) => {
            periodLabels[period] = abbr[i]
        })
        
        const periodLabel = computed(() => Object.keys(periods).find(x => periods[x] === routes.period))
        const isToday = computed(() => !routes.period || routes.period === periods.Today)

        function periodArg(from,to) {
            return `${from||''},${to||''}`
        }

        function day(date) {
            const year = date.getFullYear();
            const month = String(date.getMonth() + 1).padStart(2, '0');
            const day = String(date.getDate()).padStart(2, '0');
            return `${year}-${month}-${day}`;
        }
        function setMonth(month) {
            const date = new Date()
            date.setMonth(month)
            return date
        }
        function startOfCurrentQuarter() {
            const now = new Date();
            const currentMonth = now.getMonth();
            const quarterStartMonth = Math.floor(currentMonth / 3) * 3;
            return new Date(now.getFullYear(), quarterStartMonth, 1);
        }

        const elChart = ref()
        let chart = null
        function createChart(today) {
            if (!today || !elChart.value) return
            
            if (chart) {
                chart.destroy()
                chart = null
            }

            const borderWidth = 1
            const fill = true
            const pointStyle = false
            const colors = [
                { background: 'rgba(148,163,184, 0.2)',   border: 'rgb(148,163,184)' }, //gray-400
                { background: 'rgba(22,163,74, 0.2)',     border: 'rgb(22,163,74)' },   //green-600
                { background: 'rgba(185,28,28, 0.2)',     border: 'rgb(185,28,28)' },   //red-700
                { background: 'rgba(67,56,202, 0.2)',     border: 'rgb(67,56,202)' },   //indigo-700
            ]

            const data = {
                labels: today.map(x => x.hour),
                datasets: [{
                    label: 'total',
                    data: today.map(x => x.total),
                    backgroundColor: colors[0].background,
                    borderColor: colors[0].border,
                    borderWidth, fill, pointStyle,
                },{
                    label: 'completed',
                    data: today.map(x => x.completed),
                    backgroundColor: colors[1].background,
                    borderColor: colors[1].border,
                    borderWidth, fill, pointStyle,
                },{
                    label: 'failed',
                    data: today.map(x => x.failed),
                    backgroundColor: colors[2].background,
                    borderColor: colors[2].border,
                    borderWidth, fill, pointStyle,
                },{
                    label: 'cancelled',
                    data: today.map(x => x.cancelled),
                    backgroundColor: colors[3].background,
                    borderColor: colors[3].border,
                    borderWidth, fill, pointStyle,
                }]
            }
            const suggestedMax = Math.floor(Math.max(...today.map(x => x.total)) * 1.1)
            chart = new Chart(elChart.value, {
                type: 'line',
                data,
                options: {
                    responsive: true,
                    scales: {
                        y: {
                            suggestedMax
                        }
                    },
                    plugins: {
                        legend: {
                            position: 'top',
                        },
                    }
                },                
            })
        }
        
        async function refresh() {
            const period = routes.period || periodToday
            const from = leftPart(period,',') || undefined
            const to = rightPart(period,',') || undefined
            const api = await client.api(new AdminJobDashboard({ from, to }))
            if (api.succeeded) {
                results.value = api.response
                nextTick(() => createChart(results.value.today))
            }
        }
        
        watch(() => routes.period, refresh)
        
        onMounted(async () => {
            refresh()
        })
        
        return { 
            routes, periodLabels, periodLabel, periods, results, elChart, isToday,
            humanifyMs, humanifyNumber
        }
    }
}

export const BackgroundJobs = {
    components: {
        Queue,
        Queues,
        Nodes,
        History,
        ScheduledTasks,
    },
    template: `
        <section v-if="!plugin">
          <div class="p-4 max-w-3xl">
            <Alert type="info">Background Jobs Admin UI is not enabled</Alert>
            <div class="my-4">
              <div>
                <p>
                    The <b>DatabaseJobFeature</b> plugin needs to be configured with your App
                    <a href="https://docs.servicestack.net/background-jobs-rdbms" class="ml-2 whitespace-nowrap font-medium text-blue-700 hover:text-blue-600" target="_blank">
                       Learn more <span aria-hidden="true">&rarr;</span>
                    </a>
                </p>
              </div>
            </div>
            <div>
                <p class="text-sm text-gray-700 mb-2">Quick start:</p>
                <CopyLine text="npx add-in db-jobs" />
            </div>
          </div>
        </section>
        <Tabs v-else :tabs="tabs" :label="tabLabel" :clearQuery="true" />
    `,
    setup() {
        const client = useClient()
        const server = inject('server')
        const plugin = server.plugins.loaded.includes('backgroundjobs')
        
        const tabs = {
            Dashboard,
            Queue,
            Queues,
            Nodes,
            History,
            ScheduledTasks,
        }

        const info = ref(getStats())
        provide('info', info)

        function tabLabel(tab) {
            const count = tab === 'Queue'
                ? info.value?.tableCounts['BackgroundJob']
                : tab === 'History'
                    ? info.value?.tableCounts['JobSummary']
                    : tab === 'ScheduledTasks'
                        ? info.value?.tableCounts['ScheduledTask']
                        : null
            return humanize(tab) + (count != null ? `  (${humanifyNumber(count)})` : '')
        }

        let sub = bus.subscribe('stats:changed', () => info.value = getStats())
        onMounted(async () => {
            console.debug('onMounted')
            updateStats()

            'JobSummary,CompletedJob,FailedJob'.split(',').forEach(table => {
                const prefix = `Column/AutoQueryGrid:${table}.`
                const key = `${prefix}Id`
                if (!localStorage.getItem(key)) {
                    const anySorts = Object.keys(localStorage)
                        .filter(x => x.startsWith(prefix))
                        .some(x => JSON.parse(localStorage.getItem(x)).sort)
                    if (!anySorts) {
                        localStorage.setItem(key, `{"filters":[],"sort":"DESC"}`)
                    }
                }
            })
            
        })
        onUnmounted(() => {
            sub.unsubscribe()
            clearTimeout(updateStatsTimeout)
        })

        return {
            plugin,
            info,
            tabs,
            tabLabel,
        };
    }
}
