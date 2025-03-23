import {computed, inject, onMounted, onUnmounted, ref, watch} from "vue"
import {useClient, useFiles, useFormatters, useUtils, css} from "@servicestack/vue";
import {ApiResult, apiValueFmt, humanify, mapGet,leftPart,pick} from "@servicestack/client"
import {GetAnalyticsReports, AdminGetUser} from "dtos"
import {Chart, registerables} from 'chart.js'
Chart.register(...registerables)
const { humanifyMs, humanifyNumber } = useFormatters()
const { delay } = useUtils()
const resultLimits = [5,10,25,50,100]
export const colors = [
    { background: 'rgba(54, 162, 235, 0.2)',  border: 'rgb(54, 162, 235)' }, //blue
    { background: 'rgba(255, 99, 132, 0.2)',  border: 'rgb(255, 99, 132)' },
    { background: 'rgba(153, 102, 255, 0.2)', border: 'rgb(153, 102, 255)' },
    { background: 'rgba(54, 162, 235, 0.2)',  border: 'rgb(54, 162, 235)' },
    { background: 'rgba(255, 159, 64, 0.2)',  border: 'rgb(255, 159, 64)' },
    { background: 'rgba(67, 56, 202, 0.2)',   border: 'rgb(67, 56, 202)' },
    { background: 'rgba(255, 99, 132, 0.2)',  border: 'rgb(255, 99, 132)' },
    { background: 'rgba(14, 116, 144, 0.2)',  border: 'rgb(14, 116, 144)' },
    { background: 'rgba(162, 28, 175, 0.2)',  border: 'rgb(162, 28, 175)' },
    { background: 'rgba(201, 203, 207, 0.2)', border: 'rgb(201, 203, 207)' },
]
function httpStatusColor(status) {
    const code = parseInt(status)
    if (code < 300) return 'rgba(75, 192, 192, 0.2)'  // 2xx - success - green
    if (code < 400) return 'rgba(54, 162, 235, 0.2)'  // 3xx - redirect - blue
    if (code < 500) return 'rgba(255, 159, 64, 0.2)'  // 4xx - client error - orange
    return 'rgba(255, 99, 132, 0.2)'                  // 5xx - server error - red
}
function httpStatusGroup(status) {
    const code = parseInt(status)
    if (code < 300) return '200'
    if (code < 400) return '300'
    if (code < 500) return '400'
    return '500'
}
function substringWithEllipsis(s, len) {
    return s.length > len ? s.substring(0, len - 3) + '...' : s
}
function getUserLabel(analytics,userIdOrName) {
    const userId = userIdOrName in analytics.users ? userIdOrName : Object.keys(analytics.users)
        .find(key => analytics.users[key].name === userIdOrName)
    const user = userId && analytics.users[userId]
    return user
        ? user.name ? `${user.name} (${substringWithEllipsis(userId,8)})` : `${userId}`
        : userIdOrName
}
function chartHeight(recordCount, minHeight = 150, heightPerRecord = 22) {
    // Validate input
    const count = Math.min(Math.max(1, recordCount), 100);
    // Base height plus additional height per record
    // More records need more vertical space for readability
    return minHeight + (count * heightPerRecord);
}
/** Create a vertical bar chart for duration ranges */
function createDurationRangesChart(durations, chart, elRef) {
    if (!durations || !elRef.value) return
    // Convert durations object to arrays for Chart.js
    const labels = Object.keys(durations)
    const data = Object.values(durations)
    // Format labels for better display
    const formattedLabels = labels.map(label => {
        if (label.startsWith('>')) return `>${humanifyMs(parseInt(label.substring(1)))}`;
        const num = parseInt(label)
        return `<${humanifyMs(num)}`
    });
    // Destroy existing chart if it exists
    chart?.destroy()
    // Create chart
    return new Chart(elRef.value, {
        type: 'bar',
        data: {
            labels: formattedLabels,
            datasets: [{
                label: 'Requests',
                data: data,
                backgroundColor: 'rgba(54, 162, 235, 0.2)',
                borderColor: 'rgb(54, 162, 235)',
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            indexAxis: 'x', // vertical bars
            plugins: {
                legend: {
                    position: 'top',
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return `Requests: ${context.raw}`;
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Number of Requests'
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Duration Ranges'
                    }
                }
            }
        }
    })
}
function createStatusCodesChart(opt) {
    const { requests, chart, refEl, onClick, formatY } = opt
    if (!Object.keys(requests ?? {}).length || !refEl.value) return chart
    // Group requests by status code
    const statusCounts = {}
    // Process each API entry to count status codes
    Object.entries(requests).forEach(([status, count]) => {
        statusCounts[status] = (statusCounts[status] || 0) + count
    })
    // Create labels and data for the chart
    const labels = Object.keys(statusCounts)
    const data = Object.values(statusCounts)
    // Generate colors based on status code ranges
    const backgroundColor = labels.map(httpStatusColor)
    chart?.destroy()
    return new Chart(refEl.value, {
        type: 'pie',
        data: {
            labels,
            datasets: [{
                data,
                backgroundColor,
                borderColor: backgroundColor.map(color => color.replace('0.2', '1')),
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'right',
                    title: {
                        display: true,
                        text: 'HTTP Status',
                        font: {
                            weight: 'bold'
                        }
                    }
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            const label = context.label || '';
                            const value = context.raw || 0;
                            const total = context.dataset.data.reduce((a, b) => a + b, 0);
                            const percentage = Math.round((value / total) * 100);
                            return `${label}: ${value} (${percentage}%)`;
                        }
                    }
                }
            }
        }
    })
}
function sortedSummaryRequests(requestsMap, limit= 11) {
    if (!requestsMap || !Object.keys(requestsMap).length) return []
    // Sort APIs by request count in descending order
    return Object.entries(requestsMap)
        .sort((a, b) => b[1].totalRequests - a[1].totalRequests)
        .slice(0, limit) // Limit for better visualization
}
function sortedDetailRequests(requestsMap, limit= 11) {
    if (!requestsMap || !Object.keys(requestsMap).length) return []
    // Sort APIs by request count in descending order
    return Object.entries(requestsMap)
        .sort((a, b) => b[1] - a[1])
        .slice(0, limit) // Limit for better visualization
}
function createRequestsChart(opt) {
    const { requests, chart, refEl, onClick, formatY } = opt
    //console.log('createRequestsChart', !!onClick, requests?.length ?? 0)
    if (!requests?.length || !refEl.value) return chart
    const opRequestsChart = typeof requests[0][1] == 'number'
    const labels = requests.map(([api]) => api)
    const data = requests.map(([_, stats]) => opRequestsChart ? stats : stats.totalRequests)
    const avgResponseTimes = opRequestsChart ? [] : requests.map(([_, stats]) =>
        stats.totalRequests > 0 ? Math.round(stats.totalDuration / stats.totalRequests) : 0)
    const ctx = {
        requests,
        labels,
        data,
        avgResponseTimes,
    }
    chart?.destroy()
    const datasets = []
    if (opRequestsChart) {
        datasets.push({
            label: 'Requests',
            data,
            backgroundColor: 'rgba(54, 162, 235, 0.2)',
            borderColor: 'rgb(54, 162, 235)',
            borderWidth: 1
        })
    } else {
        const statuses = [200, 300, 400, 500]
        statuses.forEach(status => {
            datasets.push({
                label: `${status}`,
                data: requests.map(x => {
                    const ret = {}
                    Object.keys(x[1].status).forEach((status) => {
                        const group = httpStatusGroup(status)
                        ret[group] = (ret[group] || 0) + x[1].status[status]
                    })
                    return ret[status]
                }),
                backgroundColor: httpStatusColor(status),
                borderColor: httpStatusColor(status).replace('0.2','1'),
                borderWidth: 1
            })
        })
    }
    
    const scales = {
        x: {
            stacked: true,
                beginAtZero: true,
                title: {
                display: true,
                    text: 'Number of Requests'
            }
        },
        y: {
            stacked: true,
        }
    }
    if (typeof formatY === 'function') {
        scales.y.ticks = {
            callback: function (value, index, values) {
                return formatY(ctx, value, index, values)
            }
        } 
    }
    return new Chart(refEl.value, {
        type: 'bar',
        data: {
            labels,
            datasets,
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            indexAxis: 'y',
            plugins: {
                legend: {
                    position: 'top'
                },
                tooltip: {
                    callbacks: {
                        afterLabel: function(context) {
                            const index = context.dataIndex;
                            const suffix = avgResponseTimes.length ? `, Avg Response Time: ${avgResponseTimes[index]}ms` : `` 
                            return `Total ${data[index]} requests${suffix}`
                        }
                    }
                }
            },
            scales,
            onClick
        }
    })
}
function onClick(analytics, routes, type) {
    //console.log('onClick', type)
    if (type === "api") {
        return function(e, elements, chart) {
            const points = chart.getElementsAtEventForMode(e, 'nearest', { intersect: true }, false)
            if (points.length) {
                const firstPoint = points[0]
                const op = chart.data.labels[firstPoint.index]
                routes.to({ tab:'', op })
            }
        }
    }
    if (type === "user") {
        return function(e, elements, chart) {
            const points = chart.getElementsAtEventForMode(e, 'nearest', { intersect: true }, false);
            if (points.length) {
                const firstPoint = points[0]
                const userId = chart.data.labels[firstPoint.index]
                if (userId in analytics.users) {
                    routes.to({ tab:'users', userId })
                }
            }
        }
    }
    if (type === "ip") {
        return function(e, elements, chart) {
            const points = chart.getElementsAtEventForMode(e, 'nearest', { intersect: true }, false);
            if (points.length) {
                const firstPoint = points[0]
                const ip = chart.data.labels[firstPoint.index]
                if (ip in analytics.ips) {
                    routes.to({ tab:'ips', ip })
                }
            }
        }
    }
    if (type === "apiKey") {
        return function(e, elements, chart) {
            const points = chart.getElementsAtEventForMode(e, 'nearest', { intersect: true }, false);
            if (points.length) {
                const firstPoint = points[0]
                const apiKey = chart.data.labels[firstPoint.index]
                if (apiKey in analytics.apiKeys) {
                    routes.to({ tab:'apiKeys', apiKey })
                }
            }
        }
    }
    throw new Error(`Unknown type: ${type}`)
}
const numFmt = new Intl.NumberFormat('en-US', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
})
function round(n) {
    return n.toString().indexOf(".") === -1 ? n : numFmt.format(n);
}
const userSrc = 'data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIzMiIgaGVpZ2h0PSIzMiIgdmlld0JveD0iMCAwIDMyIDMyIj48cGF0aCBmaWxsPSIjNGE1NTY1IiBkPSJNMTYgOGE1IDUgMCAxIDAgNSA1YTUgNSAwIDAgMC01LTUiLz48cGF0aCBmaWxsPSIjNGE1NTY1IiBkPSJNMTYgMmExNCAxNCAwIDEgMCAxNCAxNEExNC4wMTYgMTQuMDE2IDAgMCAwIDE2IDJtNy45OTMgMjIuOTI2QTUgNSAwIDAgMCAxOSAyMGgtNmE1IDUgMCAwIDAtNC45OTIgNC45MjZhMTIgMTIgMCAxIDEgMTUuOTg1IDAiLz48L3N2Zz4='
const ApiAnalytics = {
    template:`
      <div class="my-4 mx-auto max-w-lg">
        <div class="flex">
          <div class="flex-grow">
            <Autocomplete ref="cboApis" id="op" label="" placeholder="Select API"
                          :match="(x, value) => x.key.toLowerCase().includes(value.toLowerCase())"
                          v-model="opEntry" :options="opEntries">
              <template #item="{ key, value }">
                <div v-if="value" class="truncate flex justify-between mr-8">
                  <span>{{ key }}</span>
                  <span class="text-gray-500">({{ humanifyNumber(value.totalRequests) }})</span>
                </div>
              </template>
            </Autocomplete>
          </div>
          <div class="relative ml-12 -mt-1">
            <CloseButton v-if="routes.op" @click="routes.to({ op: undefined })" title="Close API" />
          </div>
        </div>
      </div>
      <div v-if="routes.op && apiAnalytics && analytics.apis?.[routes.op]" class="mb-8 pb-8 relative border-b">
        <div class="mb-2">
          <div class="flex flex-wrap lg:flex-nowrap">
            <div class="lg:w-1/2">
              <div>
                <HtmlFormat :value="{ 'Total Requests': humanifyNumber(analytics.apis[routes.op].totalRequests) }" />
              </div>
              <div class="ml-2">
                <nav class="-mb-px flex space-x-4 flex-wrap">
                  <a v-href="{ $page:'logging', op:routes.op, month:routes.month, $clear:true }" :title="routes.op + ' Request Logs'"
                     class="group flex whitespace-nowrap px-1 py-4 text-sm font-medium text-gray-500 hover:text-indigo-600">
                    <svg class=" text-gray-400 group-hover:text-indigo-500 mr-3 h-5 w-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                      <path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13V5a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v13c0 1-.6 3-3 3m0 0H6c-1 0-3-.6-3-3v-2h12v2c0 2.4 2 3 3 3zM9 7h8m-8 4h4"></path>
                    </svg>
                    <span>All</span>
                  </a>
                  <a v-for="link in apiLinks" :href="link.href" :title="link.label + ' Request Logs'"
                     class="group flex whitespace-nowrap px-1 py-4 text-sm font-medium text-gray-500 hover:text-indigo-600">
                    {{link.label}}
                    <span class="ml-2 hidden rounded-full bg-gray-100 group-hover:bg-indigo-100 px-2.5 py-0.5 text-xs font-medium text-gray-900 hover:text-indigo-600 md:inline-block">{{link.count}}</span>
                  </a>
                </nav>
              </div>
            </div>
            <div class="lg:w-1/2">
              <HtmlFormat :value="apiAnalytics" />
            </div>
          </div>
        </div>
        <div class="flex flex-wrap lg:flex-nowrap w-full gap-x-2">
          <div class="lg:w-1/2">
            <div class="bg-white rounded shadow p-4" style="height:300px">
              <canvas ref="refOpStatusCodes"></canvas>
            </div>
          </div>
          <div class="lg:w-1/2">
            <div class="bg-white rounded shadow p-4 mb-8" style="height:300px">
              <canvas ref="refOpDurationRanges"></canvas>
            </div>
          </div>
        </div>
        <div class="flex flex-wrap lg:flex-nowrap w-full gap-x-2">
          <div v-if="Object.keys(analytics.apis[routes.op].users ?? {}).length"
               :class="Object.keys(analytics.apis[routes.op].apiKeys ?? {}).length ? 'lg:w-1/3' : 'lg:w-1/2'">
            Top Users
            <div class="mt-1 bg-white rounded shadow p-4" style="height:300px">
              <canvas ref="refOpTopUsers"></canvas>
            </div>
          </div>
          <div v-if="Object.keys(analytics.apis[routes.op].apiKeys ?? {}).length"
               :class="Object.keys(analytics.apis[routes.op].apiKeys ?? {}).length ? 'lg:w-1/3' : 'lg:w-1/2'">
            Top API Keys
            <div class="mt-1 bg-white rounded shadow p-4 mb-8" style="height:300px">
              <canvas ref="refOpTopApiKeys"></canvas>
            </div>
          </div>
          <div v-if="Object.keys(analytics.apis[routes.op].ips ?? {}).length"
              :class="Object.keys(analytics.apis[routes.op].apiKeys ?? {}).length ? 'lg:w-1/3' : 'lg:w-1/2'">
            Top IP Addresses
            <div class="mt-1 bg-white rounded shadow p-4 mb-8" style="height:300px">
              <canvas ref="refOpTopIps"></canvas>
            </div>
          </div>
        </div>
      </div>
      <div :class="{ hidden:!!routes.op }">
        <div class="mb-2 flex justify-between">
          <div>
            Overview
          </div>
        </div>
        <div class="flex flex-wrap lg:flex-nowrap w-full gap-x-2">
          <div class="lg:w-1/3 bg-white rounded shadow p-4 mb-8" style="height:300px">
            <canvas ref="refBrowsers"></canvas>
          </div>
          <div class="lg:w-1/3 bg-white rounded shadow p-4 mb-8" style="height:300px">
            <canvas ref="refDevices"></canvas>
          </div>
          <div class="lg:w-1/3 bg-white rounded shadow p-4 mb-8" style="height:300px">
            <canvas ref="refBots"></canvas>
          </div>
        </div>
        <div class="flex flex-wrap lg:flex-nowrap w-full gap-x-2">
          <div class="lg:w-1/2">
            <div class="mb-2">
              Requests per day
            </div>
            <div class="bg-white rounded shadow p-4 mb-8" style="height:300px">
              <canvas ref="refWeeklyRequests"></canvas>
            </div>
          </div>
          <div class="lg:w-1/2">
            <div class="mb-2">
              API tag groups
            </div>
            <div class="bg-white rounded shadow p-4 mb-8" style="height:300px">
              <canvas ref="refTags"></canvas>
            </div>
          </div>
        </div>
      </div>
      <div>
        <div class="mb-1 flex justify-between">
          <div>
            API Requests
          </div>
          <div>
            <SelectInput id="apiLimit" label="" v-model="limits.api" :values="resultLimits" />
          </div>
        </div>
        <div class="bg-white rounded shadow p-4 mb-8" :style="{height:chartHeight(Math.min(Object.keys(analytics?.apis ?? {}).length, limits.api)) + 'px'}">
          <canvas ref="refApiRequests"></canvas>
        </div>
      </div>
      <div>
        <div class="mb-1 flex justify-between">
          <div>
            API Duration
          </div>
          <div>
            <SelectInput id="apiLimit" label="" v-model="limits.duration" :values="resultLimits" />
          </div>
        </div>
        <div class="flex flex-wrap lg:flex-nowrap w-full gap-x-2">
          <div class="lg:w-1/2 bg-white rounded shadow p-4 mb-8" :style="{height:chartHeight(Math.min(Object.keys(analytics?.apis ?? {}).length, limits.duration)) + 'px'}">
            <canvas ref="refApiDurations"></canvas>
          </div>
          <div class="lg:w-1/2 bg-white rounded shadow p-4 mb-8" style="height:300px">
            <canvas ref="refApiDurationRanges"></canvas>
          </div>
        </div>
      </div>
    `,
    props: {
        analytics: Object,
    },
    setup(props) {
        const routes = inject('routes')
        const refBrowsers = ref(null)
        const refDevices = ref(null)
        const refBots = ref(null)
        const refWeeklyRequests = ref(null)
        const refTags = ref(null)
        const refApiRequests = ref(null)
        const refApiDurations = ref(null)
        const refApiDurationRanges = ref(null)
        const refOpStatusCodes = ref(null)
        const refOpDurationRanges = ref(null)
        const refOpTopUsers = ref(null)
        const refOpTopApiKeys = ref(null)
        const refOpTopIps = ref(null)
        const opEntry = ref()
        const opEntries = ref([])
        const limits = ref({
            api: 10,
            duration: 10
        })
        const { formatBytes } = useFiles()
        const apiAnalytics = computed(() => {
            const api = props.analytics?.apis?.[routes.op]
            if (api) {
                let ret = []
                if (api.totalDuration) {
                    ret.push({ name: 'Duration',
                        Total: humanifyMs(round(api.totalDuration)),
                        Min: humanifyMs(round(api.minDuration)),
                        Max: humanifyMs(round(api.maxDuration)),
                    })
                }
                if (api.totalRequestLength) {
                    ret.push({ name: 'Request Body', 
                        Total: formatBytes(api.totalRequestLength), 
                        Min: formatBytes(api.minRequestLength), 
                        Max: formatBytes(api.maxRequestLength), 
                    })
                }
                return ret
            }
            return []
        })
        const apiLinks = computed(() => {
            const api = props.analytics?.apis?.[routes.op]
            if (api) {
                let linkBase = `./logging?op=${routes.op}`
                if (routes.month) {
                    linkBase += `&month=${routes.month}`
                }
                const ret = []
                Object.entries(api.status).forEach(([status, count]) => {
                    ret.push({ label:status, href:`${linkBase}&status=${status}`, count })
                })
                return ret
            }
            return []
        })
        let userStatusCodesChart = null
        let browsersChart = null
        
        const createBrowsersChart = () => {
            if (!props.analytics || !refBrowsers.value) return
            const browsers = props.analytics.browsers || {}
            // Create labels and data for the chart
            const labels = Object.keys(browsers)
            const data = Object.values(browsers).map(b => b.totalRequests)
            // Ensure enough colors by cycling through the array
            const backgroundColor = labels.map((_, i) => colors[i % colors.length].background)
            const borderColor = labels.map((_, i) => colors[i % colors.length].border)
            browsersChart?.destroy()
            browsersChart = new Chart(refBrowsers.value, {
                type: 'pie',
                data: {
                    labels,
                    datasets: [{
                        data,
                        backgroundColor,
                        borderColor,
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            position: 'right',
                            title: {
                                display: true,
                                text: 'Browsers',
                                font: {
                                    weight: 'bold'
                                }
                            }
                        },
                        tooltip: {
                            callbacks: {
                                label: function(context) {
                                    const label = context.label || '';
                                    const value = context.raw || 0;
                                    const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                    const percentage = Math.round((value / total) * 100);
                                    return `${label}: ${value} (${percentage}%)`;
                                }
                            }
                        }
                    }
                }
            })
        }
        let devicesChart = null
        const createDevicesChart = () => {
            if (!props.analytics || !refDevices.value) return
            const devices = props.analytics.devices || {}
            // Create labels and data for the chart
            const labels = Object.keys(devices)
            const data = Object.values(devices).map(b => b.totalRequests)
            // Ensure enough colors by cycling through the array
            const backgroundColor = labels.map((_, i) => colors[i % colors.length].background)
            const borderColor = labels.map((_, i) => colors[i % colors.length].border)
            devicesChart?.destroy()
            devicesChart = new Chart(refDevices.value, {
                type: 'pie',
                data: {
                    labels,
                    datasets: [{
                        data,
                        backgroundColor,
                        borderColor,
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            position: 'right',
                            title: {
                                display: true,
                                text: 'Devices',
                                font: {
                                    weight: 'bold'
                                }
                            }
                        },
                        tooltip: {
                            callbacks: {
                                label: function(context) {
                                    const label = context.label || '';
                                    const value = context.raw || 0;
                                    const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                    const percentage = Math.round((value / total) * 100);
                                    return `${label}: ${value} (${percentage}%)`;
                                }
                            }
                        }
                    }
                }
            })
        }
        let botsChart = null
        const createBotsChart = () => {
            if (!props.analytics || !refBots.value) return
            let bots = props.analytics.bots || {}
            if (!Object.keys(bots).length) {
                bots = { None: 0 }
            }
            // Create labels and data for the chart
            const sortedBots = Object.entries(bots)
                .sort((a, b) => b[1].totalRequests - a[1].totalRequests)
                .slice(0, 11) // Limit to top 11 for better visualization
            const labels = sortedBots.map(x => x[0])
            const data = sortedBots.map(x => x[1].totalRequests)
            // Ensure enough colors by cycling through the array
            const backgroundColor = labels.map((_, i) => colors[i % colors.length].background)
            const borderColor = labels.map((_, i) => colors[i % colors.length].border)
            botsChart?.destroy()
            botsChart = new Chart(refBots.value, {
                type: 'pie',
                data: {
                    labels,
                    datasets: [{
                        data,
                        backgroundColor,
                        borderColor,
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            position: 'right',
                            title: {
                                display: true,
                                text: 'Bots',
                                font: {
                                    weight: 'bold'
                                }
                            }
                        },
                        tooltip: {
                            callbacks: {
                                label: function(context) {
                                    const label = context.label || '';
                                    const value = context.raw || 0;
                                    const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                    const percentage = Math.round((value / total) * 100);
                                    return `${label}: ${value} (${percentage}%)`;
                                }
                            }
                        }
                    }
                }
            })
        }
        let weeklyRequestsChart = null;
        const createWeeklyRequestsChart = () => {
            if (!props.analytics || !refWeeklyRequests.value) return
            // Sample data provided
            const days = props.analytics.days
            
            const allDays = Object.keys(days)
            const last10Days = allDays.slice(Math.max(allDays.length - 10, 0))
            // Map numeric days to day names
            const labels = last10Days.map(day => new Date(routes.month
                ? `${routes.month}-${day.padStart(2, '0')}`
                : new Date().toISOString().slice(0, 7) + '-' + day.padStart(2, '0')).toLocaleDateString('en-US', { weekday: 'short' }) + ` ${day}`);
            // Extract request data
            const requestData = last10Days.map(day => days[day].totalRequests);
            // Calculate average response time data
            const avgResponseTimeData = last10Days.map(day =>
                Math.round(days[day].totalDuration / days[day].totalRequests)
            );
            weeklyRequestsChart?.destroy();
            weeklyRequestsChart = new Chart(refWeeklyRequests.value, {
                type: 'line',
                data: {
                    labels,
                    datasets: [
                        {
                            label: 'Requests',
                            data: requestData,
                            borderColor: 'rgb(54, 162, 235)',
                            backgroundColor: 'rgba(54, 162, 235, 0.1)',
                            borderWidth: 2,
                            fill: true,
                            tension: 0.2,
                            yAxisID: 'y'
                        },
                        {
                            label: 'Avg Response Time (ms)',
                            data: avgResponseTimeData,
                            borderColor: 'rgb(255, 99, 132)',
                            backgroundColor: 'rgba(255, 99, 132, 0.1)',
                            borderWidth: 2,
                            fill: true,
                            tension: 0.2,
                            yAxisID: 'y1'
                        }
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            position: 'top',
                        },
                        tooltip: {
                            callbacks: {
                                label: function(context) {
                                    const label = context.dataset.label || '';
                                    const value = context.raw || 0;
                                    return `${label}: ${value.toLocaleString()}`;
                                }
                            }
                        }
                    },
                    scales: {
                        y: {
                            beginAtZero: true,
                            position: 'left',
                            title: {
                                display: true,
                                text: 'Number of Requests'
                            }
                        },
                        y1: {
                            beginAtZero: true,
                            position: 'right',
                            grid: {
                                drawOnChartArea: false
                            },
                            title: {
                                display: true,
                                text: 'Avg Response Time (ms)'
                            }
                        }
                    }
                }
            });
        };
        let tagsChart = null
        const createTagsChart = () => {
            if (!props.analytics || !refTags.value) return
            let tags = props.analytics.tags || {}
            if (!Object.keys(tags).length) {
                tags = { None: 0 }
            }
            // Create labels and data for the chart
            const sortedTags = Object.entries(tags)
                .sort((a, b) => b[1].totalRequests - a[1].totalRequests)
                .slice(0, 11) // Limit to top 11 for better visualization
            const labels = sortedTags.map(x => x[0])
            const data = sortedTags.map(x => x[1].totalRequests)
            // Ensure enough colors by cycling through the array
            const backgroundColor = labels.map((_, i) => colors[i % colors.length].background)
            const borderColor = labels.map((_, i) => colors[i % colors.length].border)
            tagsChart?.destroy()
            tagsChart = new Chart(refTags.value, {
                type: 'pie',
                data: {
                    labels,
                    datasets: [{
                        data,
                        backgroundColor,
                        borderColor,
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            position: 'right',
                            title: {
                                display: true,
                                text: 'Tags',
                                font: {
                                    weight: 'bold'
                                }
                            }
                        },
                        tooltip: {
                            callbacks: {
                                label: function(context) {
                                    const label = context.label || '';
                                    const value = context.raw || 0;
                                    const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                    const percentage = Math.round((value / total) * 100);
                                    return `${label}: ${value} (${percentage}%)`;
                                }
                            }
                        }
                    }
                }
            })
        }
        
        let apiRequestsChart = null
        let apiDurationsChart = null
        const createApiDurationsChart = () => {
            if (!props.analytics || !refApiDurations.value) return
            // Sort APIs by request count in descending order
            const sortedApis = Object.entries(props.analytics.apis)
                .sort((a, b) =>
                    Math.floor(b[1].totalDuration / b[1].totalRequests) - Math.floor(a[1].totalDuration / a[1].totalRequests))
                .slice(0, limits.value.duration) // Limit for better visualization
            const labels = sortedApis.map(([api]) => api)
            const data = sortedApis.map(([_, stats]) => Math.floor(stats.totalDuration / stats.totalRequests))
            const avgRequestLengths = sortedApis.map(([_, stats]) =>
                stats.totalRequests > 0 ? Math.round(stats.requestLength / stats.totalRequests) : 0)
            apiDurationsChart?.destroy()
            apiDurationsChart = new Chart(refApiDurations.value, {
                type: 'bar',
                data: {
                    labels,
                    datasets: [
                        {
                            label: 'Average Duration (ms)',
                            data,
                            backgroundColor: 'rgba(54, 162, 235, 0.2)',
                            borderColor: 'rgb(54, 162, 235)',
                            borderWidth: 1
                        }
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    indexAxis: 'y',
                    plugins: {
                        legend: {
                            position: 'top',
                        },
                        tooltip: {
                            callbacks: {
                                afterLabel: function(context) {
                                    const index = context.dataIndex;
                                    return avgRequestLengths[index] > 0
                                        ? `Avg Request Body: ${avgRequestLengths[index]} bytes`
                                        : '';
                                }
                            }
                        }
                    },
                    scales: {
                        x: {
                            beginAtZero: true,
                            title: {
                                display: true,
                                text: 'Duration (ms)'
                            }
                        }
                    },
                    onClick: onClick(props.analytics, routes, 'api')
                }
            })
        }
        let opDurationRangesChart = null
        let apiDurationRangesChart = null
        let opTopUsersChart = null
        let opTopApiKeysChart = null
        let opTopIpsChart = null
        onMounted(() => {
            update()
        })
        onUnmounted(() => {
            [
                browsersChart,
                botsChart,
                weeklyRequestsChart,
                tagsChart,
                apiRequestsChart,
                apiDurationsChart,
                apiDurationRangesChart,
                userStatusCodesChart,
                opTopUsersChart,
                opTopApiKeysChart,
                opTopIpsChart,
            ].forEach(chart => chart?.destroy())
        })
        
        function update() {
            opEntries.value = Object.keys(props.analytics?.apis ?? {})
                .map(key => ({ key, value:props.analytics.apis[key] }))
                .sort((a,b) => b.value.totalRequests - a.value.totalRequests)
            opEntry.value = routes.op ? opEntries.value.find(x => x.key === routes.op) : null
            createBrowsersChart()
            createDevicesChart()
            createBotsChart()
            createWeeklyRequestsChart()
            createTagsChart()
            apiRequestsChart = createRequestsChart({
                requests: sortedSummaryRequests(props.analytics?.apis,limits.value.api), 
                chart: apiRequestsChart, 
                refEl: refApiRequests,
                onClick: onClick(props.analytics, routes, 'api')
            })
            createApiDurationsChart()
            apiDurationRangesChart = createDurationRangesChart(props.analytics.durations, apiDurationRangesChart, refApiDurationRanges)
            updateApi()
        }
        
        function updateApi() {
            userStatusCodesChart = createStatusCodesChart({
                requests: props.analytics.apis[routes.op]?.status,
                chart: userStatusCodesChart,
                refEl: refOpStatusCodes,
            })
            opDurationRangesChart = createDurationRangesChart(props.analytics.apis[routes.op]?.durations, opDurationRangesChart, refOpDurationRanges)
            opTopUsersChart = createRequestsChart({
                requests: sortedDetailRequests(props.analytics.apis[routes.op]?.users),
                chart: opTopUsersChart, 
                refEl: refOpTopUsers,
                formatY: function(ctx, value, index, values) {
                    const userId = ctx.labels[index]
                    const user = userId && props.analytics.users[userId]
                    return user?.name ?? substringWithEllipsis(userId,16)
                },
                onClick: onClick(props.analytics, routes, 'user'),
            })
            opTopApiKeysChart = createRequestsChart({
                requests: sortedDetailRequests(props.analytics.apis[routes.op]?.apiKeys),
                chart: opTopApiKeysChart,
                refEl: refOpTopApiKeys,
                onClick: onClick(props.analytics, routes, 'apiKey'),
            })
            opTopIpsChart = createRequestsChart({
                requests: sortedDetailRequests(props.analytics.apis[routes.op]?.ips),
                chart: opTopIpsChart,
                refEl: refOpTopIps,
                onClick: onClick(props.analytics, routes, 'ip'),
            })
        }
        
        watch(() => [routes.month], update)
        watch(() => [routes.op], () => {
            opEntry.value = routes.op ? opEntries.value.find(x => x.key === routes.op) : null
            setTimeout(() => {
                updateApi()
            }, 0)
        })
        watch(() => [opEntry.value], () => {
            const op = opEntry.value?.key
            if (op !== routes.op) {
                routes.to({ op })
            }
        })
        watch(() => [props.analytics, limits.value.api], () => {
            setTimeout(() => {
                apiRequestsChart = createRequestsChart(
                    sortedDetailRequests(props.analytics?.apis,limits.value.api), 
                    refApiRequests, 
                    apiRequestsChart,
                    onClick(props.analytics, routes, 'api'))
            }, 0)
        })
        watch(() => [props.analytics, limits.value.duration], () => {
            setTimeout(() => {
                createApiDurationsChart()
            }, 0)
        })
        return {
            routes,
            limits,
            resultLimits,
            apiAnalytics,
            apiLinks,
            opEntry,
            opEntries,
            refBrowsers,
            refDevices,
            refBots,
            refWeeklyRequests,
            refTags,
            refApiRequests,
            refApiDurations,
            refApiDurationRanges,
            refOpStatusCodes,
            refOpDurationRanges,
            refOpTopUsers,
            refOpTopApiKeys,
            refOpTopIps,
            chartHeight,
            humanifyNumber,
        }
    }
}
const UserAnalytics = {
    template:`
      <div class="my-4 mx-auto max-w-lg">
        <div class="flex">
          <div class="flex-grow">
            <Autocomplete ref="cboUsers" id="op" label="" placeholder="Select User"
                          :match="(x, value) => getUserLabel(analytics,x.key).toLowerCase().includes(value.toLowerCase())"
                          v-model="opEntry" :options="opEntries">
              <template #item="{ key, value }">
                <div v-if="value" class="truncate flex justify-between mr-8">
                  <span>{{ getUserLabel(analytics,key) }}</span>
                  <span class="text-gray-500">({{ humanifyNumber(value.totalRequests) }})</span>
                </div>
              </template>
            </Autocomplete>
          </div>
          <div class="relative ml-12 -mt-1">
            <CloseButton v-if="routes.userId" @click="routes.to({ userId: undefined })" title="Close User" />
          </div>
        </div>
      </div>
      <div v-if="routes.userId && userAnalytics && analytics.users?.[routes.userId]" class="mb-8 pb-8 relative border-b">
        <div class="mb-2">
          <div class="flex flex-wrap lg:flex-nowrap">
            <div class="lg:w-1/2">
              <div class="flex justify-between">
                <table class="text-sm">
                  <template v-if="userInfo">
                    <tr>
                      <td>Id</td>
                      <td class="pl-2">
                        <a :href="'./users?edit=' + userInfo.id" class="text-indigo-700 hover:text-indigo-600">
                          {{userInfo.id}}
                        </a>
                      </td>
                    </tr>
                    <tr>
                      <td>Name</td>
                      <td class="pl-2">
                        {{ userInfo.result.DisplayName ?? (userInfo.result.FirstName ? (userInfo.FirstName + ' ' + userInfo.result.LastName) : userInfo.result.Email) }}
                      </td>
                    </tr>
                  </template>
                  <tr>
                    <td>Total Requests</td>
                    <td class="pl-2">{{humanifyNumber(analytics.users[routes.userId].totalRequests)}}</td>
                  </tr>
                </table>
                <a :href="'./users?edit=' + routes.userId">
                  <img :src="userInfo?.result.ProfileUrl || userSrc" class="m-2 h-16 w-16 rounded-full text-gray-500" alt="User Profile" :onerror="'this.src=' + JSON.stringify(userSrc)">
                </a>
              </div>
              <div class="ml-2">
                <nav class="-mb-px flex space-x-4 flex-wrap">
                  <a v-href="{ $page:'logging', userId:routes.userId, month:routes.month, $clear:true }" title="User Request Logs"
                     class="group flex whitespace-nowrap px-1 py-4 text-sm font-medium text-gray-500 hover:text-indigo-600">
                    <svg class=" text-gray-400 group-hover:text-indigo-500 mr-3 h-5 w-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                      <path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13V5a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v13c0 1-.6 3-3 3m0 0H6c-1 0-3-.6-3-3v-2h12v2c0 2.4 2 3 3 3zM9 7h8m-8 4h4"></path>
                    </svg>
                    <span>All</span>
                  </a>
                  <a v-for="link in userLinks" :href="link.href" :title="link.label + ' Request Logs'"
                     class="group flex whitespace-nowrap px-1 py-4 text-sm font-medium text-gray-500 hover:text-indigo-600">
                    {{link.label}}
                    <span class="ml-2 hidden rounded-full bg-gray-100 group-hover:bg-indigo-100 px-2.5 py-0.5 text-xs font-medium text-gray-900 hover:text-indigo-600 md:inline-block">{{link.count}}</span>
                  </a>
                </nav>
              </div>
            </div>
            <div class="lg:w-1/2">
              <HtmlFormat :value="userAnalytics" />
            </div>
          </div>
        </div>
        <div class="flex flex-wrap lg:flex-nowrap w-full gap-x-2">
          <div class="lg:w-1/2">
            <div class="bg-white rounded shadow p-4" style="height:300px">
              <canvas ref="refUserStatusCodes"></canvas>
            </div>
          </div>
          <div class="lg:w-1/2">
            <div class="bg-white rounded shadow p-4 mb-8" style="height:300px">
              <canvas ref="refUserDurationRanges"></canvas>
            </div>
          </div>
        </div>
        <div class="flex flex-wrap lg:flex-nowrap w-full gap-x-2">
          <div v-if="Object.keys(analytics.users[routes.userId].apis ?? {}).length"
               :class="Object.keys(analytics.users[routes.userId].apiKeys ?? {}).length ? 'lg:w-1/3' : 'lg:w-1/2'">
            Top APIs
            <div class="mt-1 bg-white rounded shadow p-4" style="height:300px">
              <canvas ref="refUserTopApis"></canvas>
            </div>
          </div>
          <div v-if="Object.keys(analytics.users[routes.userId].apiKeys ?? {}).length"
               :class="Object.keys(analytics.users[routes.userId].apiKeys ?? {}).length ? 'lg:w-1/3' : 'lg:w-1/2'">
            Top API Keys
            <div class="mt-1 bg-white rounded shadow p-4 mb-8" style="height:300px">
              <canvas ref="refUserTopApiKeys"></canvas>
            </div>
          </div>
          <div v-if="Object.keys(analytics.users[routes.userId].ips ?? {}).length"
               :class="Object.keys(analytics.users[routes.userId].apiKeys ?? {}).length ? 'lg:w-1/3' : 'lg:w-1/2'">
            Top IP Addresses
            <div class="mt-1 bg-white rounded shadow p-4 mb-8" style="height:300px">
              <canvas ref="refUserTopIps"></canvas>
            </div>
          </div>
        </div>
      </div>
      <div>
        <div class="mb-1 flex justify-between">
          <div>
            User Requests
          </div>
          <div>
            <SelectInput id="apiLimit" label="" v-model="limits.user" :values="resultLimits" />
          </div>
        </div>
        <div class="bg-white rounded shadow p-4 mb-8" :style="{height:chartHeight(Math.min(Object.keys(analytics?.users ?? {}).length, limits.user)) + 'px'}">
          <canvas ref="refUserRequests"></canvas>
        </div>
      </div>
    `,
    props: {
        analytics: Object
    },
    setup(props) {
        const routes = inject('routes')
        const refUserRequests = ref(null)
        const opEntry = ref()
        const opEntries = ref([])
        const refUsersDurations = ref(null)
        const refUsersDurationRanges = ref(null)
        const refUserStatusCodes = ref(null)
        const refUserDurationRanges = ref(null)
        const refUserTopApis = ref(null)
        const refUserTopApiKeys = ref(null)
        const refUserTopIps = ref(null)
        const userInfo = ref()
        const limits = ref({
            user: 50,
        })
        const { formatBytes } = useFiles()
        const userAnalytics = computed(() => {
            const user = props.analytics?.users?.[routes.userId]
            if (user) {
                let ret = []
                if (user.totalDuration) {
                    ret.push({ name: 'Duration',
                        Total: humanifyMs(round(user.totalDuration)),
                        Min: humanifyMs(round(user.minDuration)),
                        Max: humanifyMs(round(user.maxDuration)),
                    })
                }
                if (user.totalRequestLength) {
                    ret.push({ name: 'Request Body',
                        Total: formatBytes(user.totalRequestLength),
                        Min: formatBytes(user.minRequestLength),
                        Max: formatBytes(user.maxRequestLength),
                    })
                }
                return ret
            }
            return []
        })
        const userLinks = computed(() => {
            const api = props.analytics?.users?.[routes.userId]
            if (api) {
                let linkBase = `./logging?userId=${routes.userId}`
                if (routes.month) {
                    linkBase += `&month=${routes.month}`
                }
                const ret = []
                Object.entries(api.status).forEach(([status, count]) => {
                    ret.push({ label:status, href:`${linkBase}&status=${status}`, count })
                })
                return ret
            }
            return []
        })
        let userStatusCodesChart = null
        let userRequestsChart = null
        let userDurationRangesChart = null
        let userTopApisChart = null
        let userTopApiKeysChart = null
        let userTopIpsChart = null
        onMounted(() => {
            update()
        })
        onUnmounted(() => {
            [
                userStatusCodesChart,
                userRequestsChart,
                userDurationRangesChart,
                userTopApisChart,
                userTopApiKeysChart,
                userTopIpsChart,
            ].forEach(chart => chart?.destroy())
        })
        
        function createUserRequestsChart() {
            userRequestsChart = createRequestsChart({
                requests: sortedSummaryRequests(props.analytics?.users,limits.value.user),
                chart: userRequestsChart,
                refEl: refUserRequests,
                formatY: function(ctx, value, index, values) {
                    const userId = ctx.labels[index]
                    const user = userId && props.analytics.users[userId]
                    return user?.name ?? substringWithEllipsis(userId,16)
                },
                onClick: onClick(props.analytics, routes, 'user'),
            })
        }
        function update() {
            createUserRequestsChart()
            updateUser()
        }
        
        function updateUser() {
            userInfo.value = null
            if (routes.userId) {
                client.api(new AdminGetUser({ id: routes.userId }))
                    .then(r => {
                        if (r.succeeded) {
                            userInfo.value = r.response
                        }
                    })
            }
            opEntries.value = Object.keys(props.analytics?.users ?? {})
                .map(key => ({ key, value:props.analytics.users[key] }))
                .sort((a,b) => b.value.totalRequests - a.value.totalRequests)
            opEntry.value = routes.userId ? opEntries.value.find(x => x.key === routes.userId) : null
            userStatusCodesChart = createStatusCodesChart({
                requests: props.analytics.users[routes.userId]?.status,
                chart: userStatusCodesChart,
                refEl: refUserStatusCodes,
            })
            
            userDurationRangesChart = createDurationRangesChart(
                props.analytics.users[routes.userId]?.durations, userDurationRangesChart, refUserDurationRanges)
            userTopApisChart = createRequestsChart({
                requests: sortedDetailRequests(props.analytics.users[routes.userId]?.apis),
                chart: userTopApisChart,
                refEl: refUserTopApis,
                formatY: function(ctx, value, index, values) {
                    return ctx.labels[index]
                },
                onClick: onClick(props.analytics, routes, 'api'),
            })
            userTopApiKeysChart = createRequestsChart({
                requests: sortedDetailRequests(props.analytics.users[routes.userId]?.apiKeys),
                chart: userTopApiKeysChart,
                refEl: refUserTopApiKeys,
                onClick: onClick(props.analytics, routes, 'apiKey'),
            })
            userTopIpsChart = createRequestsChart({
                requests: sortedDetailRequests(props.analytics.users[routes.userId]?.ips),
                chart: userTopIpsChart,
                refEl: refUserTopIps,
                onClick: onClick(props.analytics, routes, 'ip'),
            })
        }
        watch(() => [routes.month], update)
        watch(() => [routes.userId], () => {
            opEntry.value = routes.userId ? opEntries.value.find(x => x.key === routes.userId) : null
            setTimeout(() => {
                updateUser()
            }, 0)
        })
        watch(() => [opEntry.value], () => {
            const userId = opEntry.value?.key
            if (userId !== routes.userId) {
                routes.to({ userId })
            }
        })
        watch(() => [props.analytics, limits.value.user], () => {
            setTimeout(() => {
                createUserRequestsChart()
            }, 0)
        })
        return {
            routes,
            limits,
            userSrc,
            userAnalytics,
            userLinks,
            userInfo,
            opEntry,
            opEntries,
            resultLimits,
            refUserRequests,
            refUsersDurations,
            refUsersDurationRanges,
            refUserStatusCodes,
            refUserDurationRanges,
            refUserTopApis,
            refUserTopApiKeys,
            refUserTopIps,
            getUserLabel,
            chartHeight,
            humanifyNumber,
        }
    }
}
const ApiKeyAnalytics = {
    template:`
      <div class="my-4 mx-auto max-w-lg">
        <div class="flex">
          <div class="flex-grow">
            <Autocomplete ref="cboUsers" id="op" label="" placeholder="Select API Key"
                          :match="(x, value) => getUserLabel(analytics,x.key).toLowerCase().includes(value.toLowerCase())"
                          v-model="opEntry" :options="opEntries">
              <template #item="{ key, value }">
                <div v-if="value" class="truncate flex justify-between mr-8">
                  <span>{{ getUserLabel(analytics,key) }}</span>
                  <span class="text-gray-500">({{ humanifyNumber(value.totalRequests) }})</span>
                </div>
              </template>
            </Autocomplete>
          </div>
          <div class="relative ml-12 -mt-1">
            <CloseButton v-if="routes.apiKey" @click="routes.to({ apiKey: undefined })" title="Close IP Address" />
          </div>
        </div>
      </div>
      <div v-if="routes.apiKey && apiKeyAnalytics && analytics.apiKeys?.[routes.apiKey]" class="mb-8 pb-8 relative border-b">
        <div class="mb-2">
          <div class="flex flex-wrap lg:flex-nowrap">
            <div class="lg:w-1/2">
              <div class="flex justify-between">
                <table class="text-sm">
                  <tr>
                    <td>IP</td>
                    <td class="pl-2">
                      {{routes.apiKey}}
                    </td>
                  </tr>
                  <tr>
                    <td>Total Requests</td>
                    <td class="pl-2">{{humanifyNumber(analytics.apiKeys[routes.apiKey].totalRequests)}}</td>
                  </tr>
                </table>
              </div>
              <div class="ml-2">
                <nav class="-mb-px flex space-x-4 flex-wrap">
                  <a v-href="{ $page:'logging', apiKey:routes.apiKey, month:routes.month, $clear:true }" title="IP Address Request Logs"
                     class="group flex whitespace-nowrap px-1 py-4 text-sm font-medium text-gray-500 hover:text-indigo-600">
                    <svg class=" text-gray-400 group-hover:text-indigo-500 mr-3 h-5 w-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                      <path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13V5a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v13c0 1-.6 3-3 3m0 0H6c-1 0-3-.6-3-3v-2h12v2c0 2.4 2 3 3 3zM9 7h8m-8 4h4"></path>
                    </svg>
                    <span>All</span>
                  </a>
                  <a v-for="link in apiKeyLinks" :href="link.href" :title="link.label + ' Request Logs'"
                     class="group flex whitespace-nowrap px-1 py-4 text-sm font-medium text-gray-500 hover:text-indigo-600">
                    {{link.label}}
                    <span class="ml-2 hidden rounded-full bg-gray-100 group-hover:bg-indigo-100 px-2.5 py-0.5 text-xs font-medium text-gray-900 hover:text-indigo-600 md:inline-block">{{link.count}}</span>
                  </a>
                </nav>
              </div>
            </div>
            <div class="lg:w-1/2">
              <HtmlFormat :value="apiKeyAnalytics" />
            </div>
          </div>
        </div>
        <div class="flex flex-wrap lg:flex-nowrap w-full gap-x-2">
          <div class="lg:w-1/2">
            <div class="bg-white rounded shadow p-4" style="height:300px">
              <canvas ref="refApiKeyStatusCodes"></canvas>
            </div>
          </div>
          <div class="lg:w-1/2">
            <div class="bg-white rounded shadow p-4 mb-8" style="height:300px">
              <canvas ref="refApiKeysDurationRanges"></canvas>
            </div>
          </div>
        </div>
        <div class="flex flex-wrap lg:flex-nowrap w-full gap-x-2">
          <div v-if="Object.keys(analytics.apiKeys[routes.apiKey].apis ?? {}).length"
               :class="Object.keys(analytics.apiKeys[routes.apiKey].apis ?? {}).length ? 'lg:w-1/3' : 'lg:w-1/2'">
            Top APIs
            <div class="mt-1 bg-white rounded shadow p-4" style="height:300px">
              <canvas ref="refApiKeyTopApis"></canvas>
            </div>
          </div>
          <div v-if="Object.keys(analytics.apiKeys[routes.apiKey].users ?? {}).length"
               :class="Object.keys(analytics.apiKeys[routes.apiKey].users ?? {}).length ? 'lg:w-1/3' : 'lg:w-1/2'">
            Top Users
            <div class="mt-1 bg-white rounded shadow p-4 mb-8" style="height:300px">
              <canvas ref="refApiKeyTopIps"></canvas>
            </div>
          </div>
          <div v-if="Object.keys(analytics.apiKeys[routes.apiKey].ips ?? {}).length"
               :class="Object.keys(analytics.apiKeys[routes.apiKey].ips ?? {}).length ? 'lg:w-1/3' : 'lg:w-1/2'">
            Top IPs
            <div class="mt-1 bg-white rounded shadow p-4 mb-8" style="height:300px">
              <canvas ref="refApiKeyTopUsers"></canvas>
            </div>
          </div>
        </div>
      </div>
      <div>
        <div class="mb-1 flex justify-between">
          <div>
            API Keys
          </div>
          <div>
            <SelectInput id="apiLimit" label="" v-model="limits.apiKey" :values="resultLimits" />
          </div>
        </div>
        <div class="bg-white rounded shadow p-4 mb-8" :style="{height:chartHeight(Math.min(Object.keys(analytics?.apiKeys ?? {}).length, limits.apiKey)) + 'px'}">
          <canvas ref="refApiKeyRequests"></canvas>
        </div>
      </div>
    `,
    props: {
        analytics: Object,
    },
    setup(props) {
        const routes = inject('routes')
        const refApiKeyRequests = ref(null)
        const opEntry = ref()
        const opEntries = ref([])
        const refApiKeysDurations = ref(null)
        const refApiKeysDurationRanges = ref(null)
        const refApiKeyStatusCodes = ref(null)
        const refApiKeyDurationRanges = ref(null)
        const refApiKeyTopApis = ref(null)
        const refApiKeyTopIps = ref(null)
        const refApiKeyTopUsers = ref(null)
        const limits = ref({
            apiKey: 50,
        })
        const { formatBytes } = useFiles()
        const apiKeyAnalytics = computed(() => {
            const user = props.analytics?.apiKeys?.[routes.apiKey]
            if (user) {
                let ret = []
                if (user.totalDuration) {
                    ret.push({ name: 'Duration',
                        Total: humanifyMs(round(user.totalDuration)),
                        Min: humanifyMs(round(user.minDuration)),
                        Max: humanifyMs(round(user.maxDuration)),
                    })
                }
                if (user.totalRequestLength) {
                    ret.push({ name: 'Request Body',
                        Total: formatBytes(user.totalRequestLength),
                        Min: formatBytes(user.minRequestLength),
                        Max: formatBytes(user.maxRequestLength),
                    })
                }
                return ret
            }
            return []
        })
        const apiKeyLinks = computed(() => {
            const api = props.analytics?.apiKeys?.[routes.apiKey]
            if (api) {
                let linkBase = `./logging?apiKey=${routes.apiKey}`
                if (routes.month) {
                    linkBase += `&month=${routes.month}`
                }
                const ret = []
                Object.entries(api.status).forEach(([status, count]) => {
                    ret.push({ label:status, href:`${linkBase}&status=${status}`, count })
                })
                return ret
            }
            return []
        })
        let apiKeyStatusCodesChart = null
        let apiKeyRequestsChart = null
        let apiKeyDurationRangesChart = null
        let apiKeyTopApisChart = null
        let apiKeyTopApiKeysChart = null
        let apiKeyTopUsersChart = null
        onMounted(() => {
            update()
        })
        onUnmounted(() => {
            [
                apiKeyStatusCodesChart,
                apiKeyRequestsChart,
                apiKeyDurationRangesChart,
                apiKeyTopApisChart,
                apiKeyTopApiKeysChart,
                apiKeyTopUsersChart,
            ].forEach(chart => chart?.destroy())
        })
        function createApiKeyRequestsChart() {
            apiKeyRequestsChart = createRequestsChart({
                requests: sortedSummaryRequests(props.analytics?.apiKeys,limits.value.apiKey),
                chart: apiKeyRequestsChart,
                refEl: refApiKeyRequests,
                onClick: onClick(props.analytics, routes, 'apiKey')
            })
        }
        function update() {
            createApiKeyRequestsChart()
            updateApiKey()
        }
        function updateApiKey() {
            opEntries.value = Object.keys(props.analytics?.apiKeys ?? {})
                .map(key => ({ key, value:props.analytics.apiKeys[key] }))
                .sort((a,b) => b.value.totalRequests - a.value.totalRequests)
            opEntry.value = routes.apiKey ? opEntries.value.find(x => x.key === routes.apiKey) : null
            apiKeyStatusCodesChart = createStatusCodesChart({
                requests: props.analytics.apiKeys[routes.apiKey]?.status,
                chart: apiKeyStatusCodesChart,
                refEl: refApiKeyStatusCodes,
            })
            apiKeyDurationRangesChart = createDurationRangesChart(props.analytics.apiKeys[routes.apiKey]?.durations, apiKeyDurationRangesChart, refApiKeyDurationRanges)
            apiKeyTopApisChart = createRequestsChart({
                requests: sortedDetailRequests(props.analytics.apiKeys[routes.apiKey]?.apis),
                chart: apiKeyTopApisChart,
                refEl: refApiKeyTopApis,
                formatY: function(ctx, value, index, values) {
                    return ctx.labels[index]
                },
                onClick: onClick(props.analytics, routes, 'api'),
            })
            apiKeyTopApiKeysChart = createRequestsChart({
                requests: sortedDetailRequests(props.analytics.apiKeys[routes.apiKey]?.apiKeys),
                chart: apiKeyTopApiKeysChart,
                refEl: refApiKeyTopIps,
                onClick: onClick(props.analytics, routes, 'apiKey'),
            })
            apiKeyTopUsersChart = createRequestsChart({
                requests: sortedDetailRequests(props.analytics.apiKeys[routes.apiKey]?.users),
                chart: apiKeyTopUsersChart,
                refEl: refApiKeyTopUsers,
                formatY: function(ctx, value, index, values) {
                    const userId = ctx.labels[index]
                    const user = userId && props.analytics.users[userId]
                    return user?.name ?? substringWithEllipsis(userId,16)
                },
                onClick: onClick(props.analytics, routes, 'user'),
            })
        }
        watch(() => [routes.month], update)
        watch(() => [routes.apiKey], () => {
            opEntry.value = routes.apiKey ? opEntries.value.find(x => x.key === routes.apiKey) : null
            setTimeout(() => {
                updateApiKey()
            }, 0)
        })
        watch(() => [opEntry.value], () => {
            const apiKey = opEntry.value?.key
            if (apiKey !== routes.apiKey) {
                routes.to({ apiKey })
            }
        })
        watch(() => [props.analytics, limits.value.apiKey], () => {
            setTimeout(() => {
                createApiKeyRequestsChart()
            }, 0)
        })
        return {
            routes,
            limits,
            apiKeyAnalytics,
            apiKeyLinks,
            opEntry,
            opEntries,
            resultLimits,
            refApiKeyRequests,
            refApiKeysDurations,
            refApiKeysDurationRanges,
            refApiKeyStatusCodes,
            refApiKeyDurationRanges,
            refApiKeyTopApis,
            refApiKeyTopIps,
            refApiKeyTopUsers,
            getUserLabel,
            chartHeight,
            humanifyNumber,
        }
    }
}
const IpAnalytics = {
    template:`
      <div class="my-4 mx-auto max-w-lg">
        <div class="flex">
          <div class="flex-grow">
            <Autocomplete ref="cboUsers" id="op" label="" placeholder="Select IP Address"
                          :match="(x, value) => getUserLabel(analytics,x.key).toLowerCase().includes(value.toLowerCase())"
                          v-model="opEntry" :options="opEntries">
              <template #item="{ key, value }">
                <div v-if="value" class="truncate flex justify-between mr-8">
                  <span>{{ getUserLabel(analytics,key) }}</span>
                  <span class="text-gray-500">({{ humanifyNumber(value.totalRequests) }})</span>
                </div>
              </template>
            </Autocomplete>
          </div>
          <div class="relative ml-12 -mt-1">
            <CloseButton v-if="routes.ip" @click="routes.to({ ip: undefined })" title="Close IP Address" />
          </div>
        </div>
      </div>
      <div v-if="routes.ip && ipAnalytics && analytics.ips?.[routes.ip]" class="mb-8 pb-8 relative border-b">
        <div class="mb-2">
          <div class="flex flex-wrap lg:flex-nowrap">
            <div class="lg:w-1/2">
              <div class="flex justify-between">
                <table class="text-sm">
                  <tr>
                    <td>IP</td>
                    <td class="pl-2">
                      {{routes.ip}}
                    </td>
                  </tr>
                  <tr>
                    <td>Total Requests</td>
                    <td class="pl-2">{{humanifyNumber(analytics.ips[routes.ip].totalRequests)}}</td>
                  </tr>
                </table>
              </div>
              <div class="ml-2">
                <nav class="-mb-px flex space-x-4 flex-wrap">
                  <a v-href="{ $page:'logging', ip:routes.ip, month:routes.month, $clear:true }" title="IP Address Request Logs"
                     class="group flex whitespace-nowrap px-1 py-4 text-sm font-medium text-gray-500 hover:text-indigo-600">
                    <svg class=" text-gray-400 group-hover:text-indigo-500 mr-3 h-5 w-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                      <path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13V5a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v13c0 1-.6 3-3 3m0 0H6c-1 0-3-.6-3-3v-2h12v2c0 2.4 2 3 3 3zM9 7h8m-8 4h4"></path>
                    </svg>
                    <span>All</span>
                  </a>
                  <a v-for="link in ipLinks" :href="link.href" :title="link.label + ' Request Logs'"
                     class="group flex whitespace-nowrap px-1 py-4 text-sm font-medium text-gray-500 hover:text-indigo-600">
                    {{link.label}}
                    <span class="ml-2 hidden rounded-full bg-gray-100 group-hover:bg-indigo-100 px-2.5 py-0.5 text-xs font-medium text-gray-900 hover:text-indigo-600 md:inline-block">{{link.count}}</span>
                  </a>
                </nav>
              </div>
            </div>
            <div class="lg:w-1/2">
              <HtmlFormat :value="ipAnalytics" />
            </div>
          </div>
        </div>
        <div class="flex flex-wrap lg:flex-nowrap w-full gap-x-2">
          <div class="lg:w-1/2">
            <div class="bg-white rounded shadow p-4" style="height:300px">
              <canvas ref="refIpStatusCodes"></canvas>
            </div>
          </div>
          <div class="lg:w-1/2">
            <div class="bg-white rounded shadow p-4 mb-8" style="height:300px">
              <canvas ref="refIpDurationRanges"></canvas>
            </div>
          </div>
        </div>
        <div class="flex flex-wrap lg:flex-nowrap w-full gap-x-2">
          <div v-if="Object.keys(analytics.ips[routes.ip].apis ?? {}).length"
               :class="Object.keys(analytics.ips[routes.ip].apis ?? {}).length ? 'lg:w-1/3' : 'lg:w-1/2'">
            Top APIs
            <div class="mt-1 bg-white rounded shadow p-4" style="height:300px">
              <canvas ref="refIpTopApis"></canvas>
            </div>
          </div>
          <div v-if="Object.keys(analytics.ips[routes.ip].users ?? {}).length"
               :class="Object.keys(analytics.ips[routes.ip].users ?? {}).length ? 'lg:w-1/3' : 'lg:w-1/2'">
            Top Users
            <div class="mt-1 bg-white rounded shadow p-4 mb-8" style="height:300px">
              <canvas ref="refIpTopUsers"></canvas>
            </div>
          </div>
          <div v-if="Object.keys(analytics.ips[routes.ip].apiKeys ?? {}).length"
               :class="Object.keys(analytics.ips[routes.ip].apiKeys ?? {}).length ? 'lg:w-1/3' : 'lg:w-1/2'">
            Top API Keys
            <div class="mt-1 bg-white rounded shadow p-4 mb-8" style="height:300px">
              <canvas ref="refIpTopApiKeys"></canvas>
            </div>
          </div>
        </div>
      </div>
      <div>
        <div class="mb-1 flex justify-between">
          <div>
            IP Requests
          </div>
          <div>
            <SelectInput id="apiLimit" label="" v-model="limits.ip" :values="resultLimits" />
          </div>
        </div>
        <div class="bg-white rounded shadow p-4 mb-8" :style="{height:chartHeight(Math.min(Object.keys(analytics?.ips ?? {}).length, limits.ip)) + 'px'}">
          <canvas ref="refIpRequests"></canvas>
        </div>
      </div>
    `,
    props: {
        analytics: Object,
    },
    setup(props) {
        const routes = inject('routes')
        const refIpRequests = ref(null)
        const opEntry = ref()
        const opEntries = ref([])
        const refIpsDurations = ref(null)
        const refIpsDurationRanges = ref(null)
        const refIpStatusCodes = ref(null)
        const refIpDurationRanges = ref(null)
        const refIpTopApis = ref(null)
        const refIpTopApiKeys = ref(null)
        const refIpTopUsers = ref(null)
        const limits = ref({
            ip: 50,
        })
        const { formatBytes } = useFiles()
        const ipAnalytics = computed(() => {
            const user = props.analytics?.ips?.[routes.ip]
            if (user) {
                let ret = []
                if (user.totalDuration) {
                    ret.push({ name: 'Duration',
                        Total: humanifyMs(round(user.totalDuration)),
                        Min: humanifyMs(round(user.minDuration)),
                        Max: humanifyMs(round(user.maxDuration)),
                    })
                }
                if (user.totalRequestLength) {
                    ret.push({ name: 'Request Body',
                        Total: formatBytes(user.totalRequestLength),
                        Min: formatBytes(user.minRequestLength),
                        Max: formatBytes(user.maxRequestLength),
                    })
                }
                return ret
            }
            return []
        })
        const ipLinks = computed(() => {
            const api = props.analytics?.ips?.[routes.ip]
            if (api) {
                let linkBase = `./logging?ip=${routes.ip}`
                if (routes.month) {
                    linkBase += `&month=${routes.month}`
                }
                const ret = []
                Object.entries(api.status).forEach(([status, count]) => {
                    ret.push({ label:status, href:`${linkBase}&status=${status}`, count })
                })
                return ret
            }
            return []
        })
        let ipStatusCodesChart = null
        let ipRequestsChart = null
        let ipDurationRangesChart = null
        let ipTopApisChart = null
        let ipTopApiKeysChart = null
        let ipTopUsersChart = null
        onMounted(() => {
            update()
        })
        onUnmounted(() => {
            [
                ipStatusCodesChart,
                ipRequestsChart,
                ipDurationRangesChart,
                ipTopApisChart,
                ipTopApiKeysChart,
                ipTopUsersChart,
            ].forEach(chart => chart?.destroy())
        })
        
        function createIpRequestsChart() {
            ipRequestsChart = createRequestsChart({
                requests: sortedSummaryRequests(props.analytics?.ips,limits.value.ip),
                chart: ipRequestsChart,
                refEl: refIpRequests,
                onClick: onClick(props.analytics, routes, 'ip')
            })
        }
        function update() {
            createIpRequestsChart()
            updateIp()
        }
        function updateIp() {
            opEntries.value = Object.keys(props.analytics?.ips ?? {})
                .map(key => ({ key, value:props.analytics.ips[key] }))
                .sort((a,b) => b.value.totalRequests - a.value.totalRequests)
            opEntry.value = routes.ip ? opEntries.value.find(x => x.key === routes.ip) : null
            ipStatusCodesChart = createStatusCodesChart({
                requests: props.analytics.ips[routes.ip]?.status,
                chart: ipStatusCodesChart,
                refEl: refIpStatusCodes,
            })
            ipDurationRangesChart = createDurationRangesChart(props.analytics.ips[routes.ip]?.durations, ipDurationRangesChart, refIpDurationRanges)
            
            ipTopApisChart = createRequestsChart({
                requests: sortedDetailRequests(props.analytics.ips[routes.ip]?.apis),
                chart: ipTopApisChart,
                refEl: refIpTopApis,
                formatY: function(ctx, value, index, values) {
                    return ctx.labels[index]
                },
                onClick: onClick(props.analytics, routes, 'api'),
            })
            ipTopApiKeysChart = createRequestsChart({
                requests: sortedDetailRequests(props.analytics.ips[routes.ip]?.apiKeys),
                chart: ipTopApiKeysChart,
                refEl: refIpTopApiKeys,
                onClick: onClick(props.analytics, routes, 'apiKey'),
            })
            ipTopUsersChart = createRequestsChart({
                requests: sortedDetailRequests(props.analytics.ips[routes.ip]?.users),
                chart: ipTopUsersChart,
                refEl: refIpTopUsers,
                formatY: function(ctx, value, index, values) {
                    const userId = ctx.labels[index]
                    const user = userId && props.analytics.users[userId]
                    return user?.name ?? substringWithEllipsis(userId,16)
                },
                onClick: onClick(props.analytics, routes, 'user'),
            })
        }
        watch(() => [routes.month], update)
        watch(() => [routes.ip], () => {
            opEntry.value = routes.ip ? opEntries.value.find(x => x.key === routes.ip) : null
            setTimeout(() => {
                updateIp()
            }, 0)
        })
        watch(() => [opEntry.value], () => {
            const ip = opEntry.value?.key
            if (ip !== routes.ip) {
                routes.to({ ip })
            }
        })
        watch(() => [props.analytics, limits.value.ip], () => {
            setTimeout(() => {
                createIpRequestsChart()
            }, 0)
        })        
        
        return {
            routes,
            limits,
            ipAnalytics,
            ipLinks,
            opEntry,
            opEntries,
            resultLimits,
            refIpRequests,
            refIpsDurations,
            refIpsDurationRanges,
            refIpStatusCodes,
            refIpDurationRanges,
            refIpTopApis,
            refIpTopApiKeys,
            refIpTopUsers,
            getUserLabel,
            chartHeight,
            humanifyNumber,
        }
    }
}
export const Analytics = {
    components: {
        ApiAnalytics,
        UserAnalytics,
        ApiKeyAnalytics,
        IpAnalytics,
    },
    template: `
      <div class="container mx-auto">
        <ErrorSummary v-if="api.error" :status="api.error" />
        <div>
            <div class="relative">
              <nav class="absolute flex space-x-4" aria-label="Tabs">
                <a v-for="(tab,label) in tabs" v-href="{ tab }"
                   :class="['rounded-md px-3 py-2 text-sm font-medium', routes.tab === tab ? 'bg-indigo-100 text-indigo-700' : 'text-gray-500 hover:text-gray-700']" aria-current="page">{{ label }}</a>
              </nav>
            </div>
            
            <div v-if="months.length" class="mb-2 flex flex-wrap justify-center">
              <template v-for="year in years">
                <b v-if="year === (routes.year || new Date().getFullYear().toString())" class="ml-3 text-sm font-semibold">
                  {{ year }}
                </b>
                <a v-else v-href="{ year }" class="ml-3 text-sm text-indigo-700 font-semibold hover:underline">
                  {{ year }}
                </a>
              </template>
            </div>
            <div v-if="months.length" class="flex flex-wrap justify-center">
              <template v-for="month in months.filter(x => x.startsWith(routes.year || new Date().getFullYear().toString()))">
               <span v-if="month === (routes.month || (new Date().getFullYear() + '-' + (new Date().getMonth() + 1).toString().padStart(2,'0')))" class="mr-2 mb-2 text-xs leading-5 font-semibold bg-indigo-600 text-white rounded-full py-1 px-3 flex items-center space-x-2">
               {{ new Date(month + '-01').toLocaleString('default', { month: 'long' }) }}
               </span>
                <a v-else v-href="{ month }" class="mr-2 mb-2 text-xs leading-5 font-semibold bg-slate-400/10 rounded-full py-1 px-3 flex items-center space-x-2 hover:bg-slate-400/20 dark:highlight-white/5">
                  {{ new Date(month + '-01').toLocaleString('default', { month: 'short' }) }}
                </a>
              </template>
            </div>
          
        </div>
        <div v-if="loading" class="flex justify-center p-4">
          <Loading v-if="loading">generating...</Loading>
        </div>
        <div v-else-if="analytics">
          <UserAnalytics v-if="routes.tab === 'users'" :analytics="analytics"/>
          <ApiKeyAnalytics v-else-if="routes.tab === 'apiKeys'" :analytics="analytics"/>
          <IpAnalytics v-else-if="routes.tab === 'ips'" :analytics="analytics"/>
          <ApiAnalytics v-else :analytics="analytics" />
        </div>
      </div>
    `,
    setup(props) {
        const routes = inject('routes')
        const client = useClient()
        const analytics = ref(null)
        const loading = ref(false)
        const error = ref(null)
        const api = ref(new ApiResult())
        const tabs = ref({ APIs:'' })
        const months = ref([])
        const years = computed(() => 
            Array.from(new Set(months.value.map(x => leftPart(x,'-')))).toReversed())
        
        async function update() {
            loading.value = true
            
            client.api(new GetAnalyticsReports({ filter:'info' })).then(r => {
                months.value = r.response?.months || []
            })
            
            //await delay(2000); // Pauses for 2 seconds
            
            api.value = await client.api(new GetAnalyticsReports({
                month: routes.month ? `${routes.month}-01` : undefined
            }))
            if (api.value.succeeded) {
                analytics.value = api.value.response.results
                months.value = api.value.response.months
                loading.value = false
                const newTabs = { APIs:'' }
                if (Object.keys(analytics.value.users ?? {}).length) {
                    newTabs.Users = 'users'
                }
                if (Object.keys(analytics.value.apiKeys ?? {}).length) {
                    newTabs['API Keys'] = 'apiKeys'
                }
                if (Object.keys(analytics.value.ips ?? {}).length) {
                    newTabs['IP Addresses'] = 'ips'
                }
                tabs.value = newTabs
            }
            loading.value = false
        }
        
        onMounted(async () => {
            await update()
        })
        watch(() => [routes.month], () => {
            setTimeout(() => {
                update()
            }, 0)
        })
        return {
            routes,
            api,
            analytics,
            loading,
            error,
            tabs,
            months,
            years,
        }
    }
}
