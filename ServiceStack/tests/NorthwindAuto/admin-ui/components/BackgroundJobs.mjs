import { ref, computed, watch, onMounted, onUnmounted, provide, inject, nextTick } from "vue"
import { humanize,  toDate, timeFmt12, leftPart, rightPart, pick, omit, EventBus } from "@servicestack/client"
import { useClient, useUtils, useFormatters } from "@servicestack/vue"
import { AdminJobInfo, AdminGetJob, AdminGetJobProgress, AdminCancelJobs, AdminRequeueFailedJobs, AdminJobDashboard } from "dtos"
import { Chart, registerables } from 'chart.js'
Chart.register(...registerables)

const bus = new EventBus()

const { formatDate, time, prettyJson, humanifyNumber, humanifyMs } = useFormatters()
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
            if (lastStats?.pageStats == null ||
                lastStats.pageStats.find(x => x.label === 'JobSummary').total !==
                r.response.pageStats.find(x => x.label === 'JobSummary').total) {
                bus.publish('stats:changed', r.response)
            }
            lastStats = r.response
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
            <div v-if="error" class="bg-red-700 text-white px-3 py-3">
              <div class="flex items-start justify-between space-x-3">
                <h2 class="font-medium text-white">Error</h2>
              </div>
            </div>
            <div v-if="error" class="relative flex overflow-auto">
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
            <div v-if="job.logs" class="bg-gray-100 text-gray-900 px-3 py-3">
              <div class="flex items-start justify-between space-x-3">
                <h2 class="font-medium">Logs</h2>
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
        const state = ref(props.job.state)
        function formatArgs(args) {
            Object.keys(args).forEach(key => {
                const val = args[key]
                if (key.endsWith('Date') || key === 'runAfter') {
                    args[key] = formatDate(val) + ' ' + timeFmt12(toDate(val))
                } else if (key === 'durationMs') {
                    args['duration'] = duration.value
                }
            })
            return omit(args, ['state', 'durationMs'])
        }
        const basic = computed(() => formatArgs(pick(props.job || {},
            'id,refId,tag,runAfter,createdDate,worker,state,durationMs,completedDate,attempts,callback,replyTo')))

        function updated(job) {
            loading.value = false
            logs.value = job.logs || ''
            state.value = job.state
            duration.value = humanifyMs(job.durationMs)
            console.debug('updated', job, state.value)
            emit('updated', job)
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

        onMounted(refresh)
        onUnmounted(() => clearTimeout(updateTimer))
        
        return {
            routes, bottom, error, basic, logs, state, duration, errorStatus, loading,
            hasItems, prettyJson, isRunning, requeueJob, cancelJob, 
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
                </div>
              </fieldset>

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
        
        function done() {
            emit('done')
        }
        
        async function cancelJobs() {
            const tasks = []
            const stateKeys = Object.keys(states.value).filter(k => states.value[k])
            stateKeys.forEach(state => {
                tasks.push(client.api(new AdminCancelJobs({ state: state })))
            })
            const workerKeys = Object.keys(workers.value).filter(k => workers.value[k])
            workerKeys.forEach(worker => {
                tasks.push(client.api(new AdminCancelJobs({ worker: worker })))
            })
            await Promise.all(tasks)
            
            if (stateKeys.length || workerKeys.length) {
                console.log('cancelJobs', stateKeys, workerKeys)
            }
            done()
        }
        
        return { info, done, cancelJobs, states, workers }
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
}

const Queue = {
    components,
    template: `
        <AutoQueryGrid ref="grid" type="BackgroundJob" hide="downloadCsv,copyApiUrl,forms"
            selectedColumns="progress,durationMs,worker,id,parentId,refId,tag,requestType,request,requestBody,command,runAfter,userId,dependsOn,batchId,callback,replyTo,createdDate,state,status,lastActivityDate,attempts"
            :headerTitles="{parentId:'Parent',batchId:'Batch',requestType:'Type',createdDate:'Created',startedDate:'Started',completedDate:'Completed',notifiedDate:'Notified',lastActivityDate:'Last Activity',timeoutSecs:'Timeout'}"
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
            <template #response="job"><Response :job="job" /></template>
            <template #createdDate="{createdDate}"><DateTime :value="createdDate"/></template>
            <template #worker="{worker}">{{worker}}</template>
            <template #state="{state}">{{state}}</template>
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

        return { routes, grid, edit, show }
    }
}

const Summary = {
    components,
    template: `
        <AutoQueryGrid ref="grid" type="JobSummary" hide="copyApiUrl,forms" 
            selectedColumns="id,parentId,refId,tag,requestType,request,command,response,callback,createdDate,worker,state,durationMs,completedDate,attempts,errorCode,errorMessage"
            :visibleFrom="{requestType:'never',callback:'never',errorMessage:'never'}"
            :headerTitles="{parentId:'Parent',createdDate:'Created',completedDate:'Completed',durationMs:'Duration',errorCode:'Error'}"
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
            <template #state="{state}">{{state}}</template>
            <template #durationMs="{durationMs}"><Duration :value="durationMs" /></template>
            <template #completedDate="{completedDate}"><DateTime :value="completedDate"/></template>
            <template #attempts="{attempts}">{{attempts}}</template>
            <template #errorCode="{errorCode,errorMessage}"><Markup :title="errorMessage">{{errorCode}}</Markup></template>
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
        
        return { routes, grid, formatDate, time, toDate, humanifyMs, edit }
    }
}
const Completed = {
    components,
    props:['month'],
    template: `
        <AutoQueryGrid ref="grid" type="CompletedJob" hide="copyApiUrl,forms"
            selectedColumns="id,parentId,refId,tag,requestType,request,command,userId,dependsOn,batchId,response,callback,replyTo,createdDate,worker,startedDate,state,status,durationMs,completedDate,notifiedDate,attempts,lastActivityDate"
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
            <template #state="{state}">{{state}}</template>
            <template #durationMs="{durationMs}"><Duration :value="durationMs" /></template>
            <template #completedDate="{completedDate}"><DateTime :value="completedDate"/></template>
            <template #notifiedDate="{notifiedDate}"><DateTime :value="notifiedDate"/></template>
            <template #lastActivityDate="{lastActivityDate}"><DateTime :value="lastActivityDate"/></template>
        </AutoQueryGrid>
        <JobDialog v-if="edit" :job="edit" @done="routes.edit=null" @updated="job => edit=job" />
    `,
    setup(props) {
        const routes = inject('routes')
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
            selectedColumns="id,parentId,refId,tag,dependsOn,batchId,requestType,request,command,userId,response,callback,replyTo,createdDate,worker,startedDate,state,status,durationMs,completedDate,notifiedDate,lastActivityDate,attempts,retryLimit,timeoutSecs,errorCode,error"
            :visibleFrom="{error:'never'}"
            :headerTitles="{parentId:'Parent',batchId:'Batch',requestType:'Type',createdDate:'Created',startedDate:'Started',completedDate:'Completed',notifiedDate:'Notified',lastActivityDate:'Last Activity',timeoutSecs:'Timeout'}"
            @rowSelected="routes.edit = routes.edit == $event.id ? null : $event.id" :isSelected="(row) => routes.edit == row.id"
            :filters="{month}">
            <template #parentId="{parentId}"><EditLink :id="parentId" @selected="routes.edit = $event" /></template>
        </AutoQueryGrid>
        <JobDialog v-if="edit" :job="edit" @done="routes.edit=null" @updated="job => edit=job" />
    `,
    setup(props) {
        const routes = inject('routes')
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
        const monthDb = ref(getPrefs().monthDb ?? info.value.monthDbs[0])
        const monthDbEntries = computed(() => {
            return info.value.monthDbs?.map(x => ({ 
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
            selectedColumns="id,name,lastJobId,lastRun,interval,cronExpression,requestType,command,request,requestBody,options"
            :headerTitles="{lastJobId:'Last Job'}"
            @rowSelected="routes.edit = routes.edit === $event.id ? null : $event.id" :isSelected="(row) => routes.edit === row.id">
            <template #lastJobId="{lastJobId}"><EditLink :id="lastJobId" @selected="routes.edit = $event" /></template>
            <template #lastRun="{lastRun}"><DateTime :value="lastRun"/></template>
        </AutoQueryGrid>
        <JobDialog v-if="edit" :job="edit" @done="routes.edit=null" @updated="job => edit = job" />
    `,
    setup() {
        const routes = inject('routes')
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
        onMounted(update)
        
        return { routes, grid, edit }
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
        const results = ref({ commands:[], apis:[], workers:[], today:[] })
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
            routes, periodLabels, periodLabel, periods, results, elChart, isToday
        }
    }
}

export const BackgroundJobs = {
    components: {
        Queue,
        History,
        ScheduledTasks,
    },
    template: `
        <Tabs :tabs="tabs" :label="tabLabel" :clearQuery="true" />
    `,
    setup() {
        const client = useClient()
        
        const tabs = {
            Dashboard,
            Queue,
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
            info,
            tabs,
            tabLabel,
        };
    }
}
