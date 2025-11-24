import { computed, inject, onMounted, onUnmounted, ref, watch, nextTick } from "vue"
import { useClient, useFiles, useFormatters, useUtils, css } from "@servicestack/vue";
import { ApiResult, apiValueFmt, humanify, mapGet, leftPart, pick } from "@servicestack/client"
import { GetAnalyticsReports, GetAnalyticsInfo, AdminGetUser, AdminQueryApiKeys } from "dtos"
import { Chart, registerables } from 'chart.js'

Chart.register(...registerables)

import {
    resultLimits,
    colors,
    substringWithEllipsis,
    getUserLabel,
    hiddenApiKey,
    getApiKeyLabel,
    mapCounts,
    chartHeight,
    sortedSummaryRequests,
    sortedDetailRequests,
    onClick,

    createDurationRangesChart,
    createStatusCodesChart,
    createRequestsChart,
} from "charts"

const {humanifyMs, humanifyNumber, formatDate} = useFormatters()
const {delay} = useUtils()

const numFmt = new Intl.NumberFormat('en-US', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
})

function round(n) {
    return n.toString().indexOf(".") === -1 ? n : numFmt.format(n);
}

const ApiAnalytics = {
    template: `
      <div class="mt-2 mb-4 mx-auto max-w-sm">
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
                <HtmlFormat class="not-prose" :value="{ 'Total Requests': humanifyNumber(analytics.apis[routes.op].totalRequests) }" />
              </div>
              <LogLinks :title="routes.op" :links="apiLinks" :filter="{ op:routes.op }" />
            </div>
            <div class="lg:w-1/2">
              <HtmlFormat class="not-prose" :value="apiAnalytics" />
            </div>
          </div>
        </div>
        <div class="grid grid-cols-1 lg:grid-cols-2 w-full gap-2">
          <div>
            <div class="bg-white rounded shadow p-4" style="height:300px">
              <canvas ref="refOpStatusCodes"></canvas>
            </div>
          </div>
          <div>
            <div class="bg-white rounded shadow p-4 mb-8" style="height:300px">
              <canvas ref="refOpDurationRanges"></canvas>
            </div>
          </div>
        </div>
        <div :class="['mt-8 grid grid-cols-1 md:grid-cols-2 w-full gap-2', mapCounts(analytics.apis[routes.op],['users','apiKeys','ips']) === 3 ? 'lg:grid-cols-3' : '']">
          <div v-if="mapCounts(analytics.apis[routes.op],'users')">
            Top Users
            <div class="mt-1 bg-white rounded shadow p-4" style="height:300px">
              <canvas ref="refOpTopUsers"></canvas>
            </div>
          </div>
          <div v-if="mapCounts(analytics.apis[routes.op],'apiKeys')">
            Top API Keys
            <div class="mt-1 bg-white rounded shadow p-4 mb-8" style="height:300px">
              <canvas ref="refOpTopApiKeys"></canvas>
            </div>
          </div>
          <div v-if="mapCounts(analytics.apis[routes.op],'ips')">
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
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 w-full gap-2">
          <div class="bg-white rounded shadow p-4 mb-8" style="height:300px">
            <canvas ref="refBrowsers"></canvas>
          </div>
          <div class="bg-white rounded shadow p-4 mb-8" style="height:300px">
            <canvas ref="refDevices"></canvas>
          </div>
          <div class="bg-white rounded shadow p-4 mb-8" style="height:300px">
            <canvas ref="refBots"></canvas>
          </div>
        </div>
        <div class="grid grid-cols-1 lg:grid-cols-2 w-full gap-2">
          <div>
            <div class="mb-2">
              Requests per day
            </div>
            <div class="bg-white rounded shadow p-4 mb-8" style="height:300px">
              <canvas ref="refWeeklyRequests"></canvas>
            </div>
          </div>
          <div>
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
        <div class="grid grid-cols-1 lg:grid-cols-2 w-full gap-2">
          <div class="bg-white rounded shadow p-2" :style="{height:chartHeight(Math.min(Object.keys(analytics?.apis ?? {}).length, limits.duration)) + 'px'}">
            <canvas ref="refApiTotalDurations"></canvas>
          </div>
          <div class="bg-white rounded shadow p-2" :style="{height:chartHeight(Math.min(Object.keys(analytics?.apis ?? {}).length, limits.duration)) + 'px'}">
            <canvas ref="refApiAverageDurations"></canvas>
          </div>
          <div class="bg-white rounded shadow p-2" style="height:300px">
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
        const refApiAverageDurations = ref(null)
        const refApiTotalDurations = ref(null)
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

        const {formatBytes} = useFiles()
        const apiAnalytics = computed(() => {
            const api = props.analytics?.apis?.[routes.op]
            if (api) {
                let ret = []
                if (api.totalDuration) {
                    ret.push({
                        name: 'Duration',
                        Total: humanifyMs(round(api.totalDuration)),
                        Min: humanifyMs(round(api.minDuration)),
                        Max: humanifyMs(round(api.maxDuration)),
                    })
                }
                if (api.totalRequestLength) {
                    ret.push({
                        name: 'Request Body',
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
                    ret.push({label: status, href: `${linkBase}&status=${status}`, count})
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
                                label: function (context) {
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
                                label: function (context) {
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
                bots = {None: 0}
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
                                label: function (context) {
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
                : new Date().toISOString().slice(0, 7) + '-' + day.padStart(2, '0')).toLocaleDateString('en-US', {weekday: 'short'}) + ` ${day}`);

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
                                label: function (context) {
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
                tags = {None: 0}
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
                                label: function (context) {
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
        let apiAverageDurationsChart = null
        const createApiAverageDurationsChart = () => {
            if (!props.analytics || !refApiAverageDurations.value) return

            // Sort APIs by request count in descending order
            const sortedApis = Object.entries(props.analytics.apis)
                .sort((a, b) =>
                    Math.floor(b[1].totalDuration / b[1].totalRequests) - Math.floor(a[1].totalDuration / a[1].totalRequests))
                .slice(0, limits.value.duration) // Limit for better visualization

            const labels = sortedApis.map(([api]) => api)
            const data = sortedApis.map(([_, stats]) => Math.floor(stats.totalDuration / stats.totalRequests))
            const avgRequestLengths = sortedApis.map(([_, stats]) =>
                stats.totalRequests > 0 ? Math.round(stats.requestLength / stats.totalRequests) : 0)

            apiAverageDurationsChart?.destroy()
            apiAverageDurationsChart = new Chart(refApiAverageDurations.value, {
                type: 'bar',
                data: {
                    labels,
                    datasets: [
                        {
                            label: 'Average Duration',
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
                                label: function (context) {
                                    return `Average Duration: ${humanifyMs(context.raw)}`;
                                },
                                afterLabel: function (context) {
                                    const index = context.dataIndex;
                                    const api = sortedApis[index][1];
                                    let lines = [];
                                    if (avgRequestLengths[index] > 0) {
                                        lines.push(`Avg Request Body: ${formatBytes(avgRequestLengths[index])}`);
                                    }
                                    lines.push(`Total Duration: ${humanifyMs(api.totalDuration)}`);
                                    return lines;
                                }
                            }
                        }
                    },
                    scales: {
                        x: {
                            beginAtZero: true,
                            ticks: {
                                callback: function (value) {
                                    return humanifyMs(value);
                                }
                            }
                        }
                    },
                    onClick: onClick(props.analytics, routes, 'api')
                }
            })
        }

        let apiTotalDurationsChart = null

        function createApiTotalDurationsChart() {
            if (!props.analytics || !refApiTotalDurations.value) return

            // Sort APIs by total duration in descending order
            const sortedApis = Object.entries(props.analytics.apis)
                .sort((a, b) => b[1].totalDuration - a[1].totalDuration)
                .slice(0, limits.value.duration) // Limit for better visualization

            const labels = sortedApis.map(([api]) => api)
            const data = sortedApis.map(([_, stats]) => stats.totalDuration)
            const avgDurations = sortedApis.map(([_, stats]) =>
                stats.totalRequests > 0 ? Math.floor(stats.totalDuration / stats.totalRequests) : 0)
            const avgRequestLengths = sortedApis.map(([_, stats]) =>
                stats.totalRequests > 0 ? Math.round(stats.requestLength / stats.totalRequests) : 0)

            apiTotalDurationsChart?.destroy()
            apiTotalDurationsChart = new Chart(refApiTotalDurations.value, {
                type: 'bar',
                data: {
                    labels,
                    datasets: [
                        {
                            label: 'Total Duration',
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
                                label: function (context) {
                                    return `Total Duration: ${humanifyMs(context.raw)}`;
                                },
                                afterLabel: function (context) {
                                    const index = context.dataIndex;
                                    const api = sortedApis[index][1];
                                    let lines = [];
                                    lines.push(`Average Duration: ${humanifyMs(avgDurations[index])}`);
                                    if (avgRequestLengths[index] > 0) {
                                        lines.push(`Avg Request Body: ${formatBytes(avgRequestLengths[index])}`);
                                    }
                                    return lines;
                                }
                            }
                        }
                    },
                    scales: {
                        x: {
                            beginAtZero: true,
                            ticks: {
                                callback: function (value) {
                                    return humanifyMs(value);
                                }
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
                apiAverageDurationsChart,
                apiDurationRangesChart,
                userStatusCodesChart,
                opTopUsersChart,
                opTopApiKeysChart,
                opTopIpsChart,
            ].forEach(chart => chart?.destroy())
        })

        function update() {
            opEntries.value = Object.keys(props.analytics?.apis ?? {})
                .map(key => ({key, value: props.analytics.apis[key]}))
                .sort((a, b) => b.value.totalRequests - a.value.totalRequests)
            opEntry.value = routes.op ? opEntries.value.find(x => x.key === routes.op) : null
            createBrowsersChart()
            createDevicesChart()
            createBotsChart()
            createWeeklyRequestsChart()
            createTagsChart()
            apiRequestsChart = createRequestsChart({
                requests: sortedSummaryRequests(props.analytics?.apis, limits.value.api),
                chart: apiRequestsChart,
                refEl: refApiRequests,
                onClick: onClick(props.analytics, routes, 'api')
            })
            createApiAverageDurationsChart()
            createApiTotalDurationsChart()
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
                formatY: function (ctx, value, index, values) {
                    const userId = ctx.labels[index]
                    const user = userId && props.analytics.users[userId]
                    return user?.name ?? substringWithEllipsis(userId, 16)
                },
                onClick: onClick(props.analytics, routes, 'user'),
            })
            opTopApiKeysChart = createRequestsChart({
                requests: sortedDetailRequests(props.analytics.apis[routes.op]?.apiKeys),
                chart: opTopApiKeysChart,
                refEl: refOpTopApiKeys,
                formatY: function (ctx, value, index, values) {
                    return hiddenApiKey(ctx.labels[index])
                },
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
            nextTick(updateApi)
        })
        watch(() => [opEntry.value], () => {
            const op = opEntry.value?.key
            if (op !== routes.op) {
                routes.to({op})
            }
        })
        watch(() => [props.analytics, limits.value.api], () => {
            nextTick(() => {
                apiRequestsChart = createRequestsChart(
                    sortedDetailRequests(props.analytics?.apis, limits.value.api),
                    refApiRequests,
                    apiRequestsChart,
                    onClick(props.analytics, routes, 'api'))
            })
        })
        watch(() => [props.analytics, limits.value.duration], () => {
            nextTick(() => {
                createApiAverageDurationsChart()
                createApiTotalDurationsChart()
            })
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
            refApiAverageDurations,
            refApiTotalDurations,
            refApiDurationRanges,
            refOpStatusCodes,
            refOpDurationRanges,
            refOpTopUsers,
            refOpTopApiKeys,
            refOpTopIps,
            chartHeight,
            humanifyNumber,
            mapCounts,
        }
    }
}

const UserAnalytics = {
    template: `
      <div class="mt-2 mb-4 mx-auto max-w-sm">
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
                  <tbody>
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
                        <a :href="'./users?edit=' + userInfo.id" class="text-indigo-700 hover:text-indigo-600">
                          {{ userInfo.result.DisplayName ?? (userInfo.result.FirstName ? (userInfo.result.FirstName + ' ' + userInfo.result.LastName) : userInfo.result.UserName) ?? userInfo.result.Email }}
                        </a>
                      </td>
                    </tr>
                  </template>
                  <tr>
                    <td>Total Requests</td>
                    <td class="pl-2">{{humanifyNumber(analytics.users[routes.userId].totalRequests)}}</td>
                  </tr>
                  </tbody>
                </table>
                <a :href="'./users?edit=' + routes.userId">
                  <img :src="userInfo?.result.ProfileUrl || store.userIconUri" class="m-2 h-16 w-16 rounded-full text-gray-500" alt="User Profile" :onerror="'this.src=' + JSON.stringify(store.userIconUri)">
                </a>
              </div>
              <LogLinks title="User" :links="userLinks" :filter="{ userId:routes.userId }" />
            </div>
            <div class="lg:w-1/2">
              <HtmlFormat class="not-prose" :value="userAnalytics" />
            </div>
          </div>
        </div>
        <div class="grid grid-cols-1 lg:grid-cols-2 w-full gap-2">
          <div class="bg-white rounded shadow p-4" style="height:300px">
            <canvas ref="refUserStatusCodes"></canvas>
          </div>
          <div class="bg-white rounded shadow p-4 mb-8" style="height:300px">
            <canvas ref="refUserDurationRanges"></canvas>
          </div>
        </div>
        <div :class="['mt-8 grid grid-cols-1 md:grid-cols-2 w-full gap-2', mapCounts(analytics.users[routes.userId],['apis','apiKeys','ips']) === 3 ? 'lg:grid-cols-3' : '']">
          <div v-if="mapCounts(analytics.users[routes.userId],'apis')">
            Top APIs
            <div class="mt-1 bg-white rounded shadow p-4" style="height:300px">
              <canvas ref="refUserTopApis"></canvas>
            </div>
          </div>
          <div v-if="mapCounts(analytics.users[routes.userId],'apiKeys')">
            Top API Keys
            <div class="mt-1 bg-white rounded shadow p-4" style="height:300px">
              <canvas ref="refUserTopApiKeys"></canvas>
            </div>
          </div>
          <div v-if="mapCounts(analytics.users[routes.userId],'ips')">
            Top IP Addresses
            <div class="mt-1 bg-white rounded shadow p-4" style="height:300px">
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
        const store = inject('store')
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
            user: 25,
        })

        const {formatBytes} = useFiles()
        const userAnalytics = computed(() => {
            const user = props.analytics?.users?.[routes.userId]
            if (user) {
                let ret = []
                if (user.totalDuration) {
                    ret.push({
                        name: 'Duration',
                        Total: humanifyMs(round(user.totalDuration)),
                        Min: humanifyMs(round(user.minDuration)),
                        Max: humanifyMs(round(user.maxDuration)),
                    })
                }
                if (user.totalRequestLength) {
                    ret.push({
                        name: 'Request Body',
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
                    ret.push({label: status, href: `${linkBase}&status=${status}`, count})
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

        onMounted(async () => {
            await loadUserIfMissing()
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
                requests: sortedSummaryRequests(props.analytics?.users, limits.value.user),
                chart: userRequestsChart,
                refEl: refUserRequests,
                formatY: function (ctx, value, index, values) {
                    const userId = ctx.labels[index]
                    const user = userId && props.analytics.users[userId]
                    return user?.name ?? substringWithEllipsis(userId, 16)
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
                client.api(new AdminGetUser({id: routes.userId}))
                    .then(r => {
                        if (r.succeeded) {
                            userInfo.value = r.response
                        }
                    })
            }
            opEntries.value = Object.keys(props.analytics?.users ?? {})
                .map(key => ({key, value: props.analytics.users[key]}))
                .sort((a, b) => b.value.totalRequests - a.value.totalRequests)
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
                formatY: function (ctx, value, index, values) {
                    return ctx.labels[index]
                },
                onClick: onClick(props.analytics, routes, 'api'),
            })
            userTopApiKeysChart = createRequestsChart({
                requests: sortedDetailRequests(props.analytics.users[routes.userId]?.apiKeys),
                chart: userTopApiKeysChart,
                refEl: refUserTopApiKeys,
                formatY: function (ctx, value, index, values) {
                    return hiddenApiKey(ctx.labels[index])
                },
                onClick: onClick(props.analytics, routes, 'apiKey'),
            })
            userTopIpsChart = createRequestsChart({
                requests: sortedDetailRequests(props.analytics.users[routes.userId]?.ips),
                chart: userTopIpsChart,
                refEl: refUserTopIps,
                onClick: onClick(props.analytics, routes, 'ip'),
            })
        }

        async function loadUserIfMissing() {
            if (routes.userId && props.analytics?.users && !props.analytics.users[routes.userId]) {
                const r = await client.api(new GetAnalyticsReports({
                    filter: 'user',
                    value: routes.userId
                }))
                if (r.response) {
                    const user = Object.values(r.response.result?.users ?? {})[0]
                    props.analytics.users[routes.userId] = user
                }
            }
        }

        watch(() => [routes.month], update)
        watch(() => [routes.userId], async () => {
            opEntry.value = routes.userId ? opEntries.value.find(x => x.key === routes.userId) : null
            await loadUserIfMissing()
            nextTick(updateUser)
        })
        watch(() => [opEntry.value], () => {
            const userId = opEntry.value?.key
            if (userId !== routes.userId) {
                routes.to({userId})
            }
        })
        watch(() => [props.analytics, limits.value.user], () => {
            nextTick(createUserRequestsChart)
        })

        return {
            routes,
            store,
            limits,
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
            mapCounts,
        }
    }
}

const ApiKeyAnalytics = {
    template: `
      <div class="mt-2 mb-4 mx-auto max-w-sm">
        <div class="flex">
          <div class="flex-grow">
            <Autocomplete ref="cboUsers" id="op" label="" placeholder="Select API Key"
                          :match="(x, value) => getApiKeyLabel(analytics,x.key).toLowerCase().includes(value.toLowerCase())"
                          v-model="opEntry" :options="opEntries">
              <template #item="{ key, value }">
                <div v-if="value" class="truncate flex justify-between mr-8">
                  <span>{{ getApiKeyLabel(analytics,key) }}</span>
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
                    <td>API Key</td>
                    <td class="pl-2">
                      <span @click="showApiKey=true" v-if="!showApiKey" class="border border-dashed hover:border-gray-500 px-1 rounded-sm cursor-pointer" title="Show full API Key">
                        {{hiddenApiKey(routes.apiKey)}}
                      </span>
                      <span v-else>
                        {{routes.apiKey}}
                      </span>
                    </td>
                  </tr>
                  <template v-if="apiKeyInfo">
                    <tr>
                      <td>Name</td>
                      <td class="pl-2">
                        <a :href="'./apikeys?edit=' + apiKeyInfo.id" class="text-indigo-700 hover:text-indigo-600">
                          {{apiKeyInfo.name}} ({{apiKeyInfo.id}})
                        </a>
                      </td>
                    </tr>
                    <tr>
                      <td>Created</td>
                      <td class="pl-2">
                        {{formatDate(apiKeyInfo.createdDate)}}
                      </td>
                    </tr>
                    <tr v-if="apiKeyInfo.userName">
                      <td>User</td>
                      <td class="pl-2">
                        {{formatDate(apiKeyInfo.userName)}}
                      </td>
                    </tr>
                    <tr v-if="apiKeyInfo.expiryDate">
                      <td>Expires</td>
                      <td class="pl-2">
                        {{formatDate(apiKeyInfo.expiryDate)}}
                      </td>
                    </tr>
                    <tr v-if="apiKeyInfo.cancelledDate">
                      <td>Cancelled</td>
                      <td class="pl-2">
                        {{formatDate(apiKeyInfo.cancelledDate)}}
                      </td>
                    </tr>
                  </template>
                  <tr>
                    <td>Total Requests</td>
                    <td class="pl-2">{{humanifyNumber(analytics.apiKeys[routes.apiKey].totalRequests)}}</td>
                  </tr>
                </table>
              </div>
              <LogLinks title="API Key" :links="apiKeyLinks" :filter="{ apiKey:routes.apiKey }" />
            </div>
            <div class="lg:w-1/2">
              <HtmlFormat class="not-prose" :value="apiKeyAnalytics" />
            </div>
          </div>
        </div>
        <div class="grid grid-cols-1 lg:grid-cols-2 w-full gap-2">
          <div>
            <div class="bg-white rounded shadow p-4" style="height:300px">
              <canvas ref="refApiKeyStatusCodes"></canvas>
            </div>
          </div>
          <div>
            <div class="bg-white rounded shadow p-4 mb-8" style="height:300px">
              <canvas ref="refApiKeyDurationRanges"></canvas>
            </div>
          </div>
        </div>
        <div :class="['mt-8 grid grid-cols-1 md:grid-cols-2 w-full gap-2', mapCounts(analytics.apiKeys[routes.apiKey],['apis','users','ips']) === 3 ? 'lg:grid-cols-3' : '']">
          <div v-if="mapCounts(analytics.apiKeys[routes.apiKey],'apis')">
            Top APIs
            <div class="mt-1 bg-white rounded shadow p-4" style="height:300px">
              <canvas ref="refApiKeyTopApis"></canvas>
            </div>
          </div>
          <div v-if="mapCounts(analytics.apiKeys[routes.apiKey],'users')">
            Top Users
            <div class="mt-1 bg-white rounded shadow p-4 mb-8" style="height:300px">
              <canvas ref="refApiKeyTopUsers"></canvas>
            </div>
          </div>
          <div v-if="mapCounts(analytics.apiKeys[routes.apiKey],'ips')">
            Top IPs
            <div class="mt-1 bg-white rounded shadow p-4 mb-8" style="height:300px">
              <canvas ref="refApiKeyTopIps"></canvas>
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
        const apiKeyInfo = ref()
        const showApiKey = ref(false)

        const limits = ref({
            apiKey: 25,
        })

        const {formatBytes} = useFiles()
        const apiKeyAnalytics = computed(() => {
            const user = props.analytics?.apiKeys?.[routes.apiKey]
            if (user) {
                let ret = []
                if (user.totalDuration) {
                    ret.push({
                        name: 'Duration',
                        Total: humanifyMs(round(user.totalDuration)),
                        Min: humanifyMs(round(user.minDuration)),
                        Max: humanifyMs(round(user.maxDuration)),
                    })
                }
                if (user.totalRequestLength) {
                    ret.push({
                        name: 'Request Body',
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
                    ret.push({label: status, href: `${linkBase}&status=${status}`, count})
                })
                return ret
            }
            return []
        })

        let apiKeyStatusCodesChart = null
        let apiKeyRequestsChart = null
        let apiKeyDurationRangesChart = null

        let apiKeyTopApisChart = null
        let apiKeyTopIpsChart = null
        let apiKeyTopUsersChart = null

        onMounted(async () => {
            await loadApiKeyIfMissing()
            update()
        })

        onUnmounted(() => {
            [
                apiKeyStatusCodesChart,
                apiKeyRequestsChart,
                apiKeyDurationRangesChart,
                apiKeyTopApisChart,
                apiKeyTopUsersChart,
            ].forEach(chart => chart?.destroy())
        })

        function createApiKeyRequestsChart() {
            apiKeyRequestsChart = createRequestsChart({
                requests: sortedSummaryRequests(props.analytics?.apiKeys, limits.value.apiKey),
                chart: apiKeyRequestsChart,
                refEl: refApiKeyRequests,
                formatY: function (ctx, value, index, values) {
                    return hiddenApiKey(ctx.labels[index])
                },
                onClick: onClick(props.analytics, routes, 'apiKey')
            })
        }

        function update() {
            createApiKeyRequestsChart()
            updateApiKey()
        }

        function updateApiKey() {
            apiKeyInfo.value = null
            client.api(new AdminQueryApiKeys({apiKey: routes.apiKey}))
                .then(r => {
                    if (r.succeeded) {
                        apiKeyInfo.value = r.response?.results?.[0]
                    }
                })

            opEntries.value = Object.keys(props.analytics?.apiKeys ?? {})
                .map(key => ({key, value: props.analytics.apiKeys[key]}))
                .sort((a, b) => b.value.totalRequests - a.value.totalRequests)
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
                formatY: function (ctx, value, index, values) {
                    return ctx.labels[index]
                },
                onClick: onClick(props.analytics, routes, 'api'),
            })
            apiKeyTopIpsChart = createRequestsChart({
                requests: sortedDetailRequests(props.analytics.apiKeys[routes.apiKey]?.ips),
                chart: apiKeyTopIpsChart,
                refEl: refApiKeyTopIps,
                onClick: onClick(props.analytics, routes, 'ip'),
            })
            apiKeyTopUsersChart = createRequestsChart({
                requests: sortedDetailRequests(props.analytics.apiKeys[routes.apiKey]?.users),
                chart: apiKeyTopUsersChart,
                refEl: refApiKeyTopUsers,
                formatY: function (ctx, value, index, values) {
                    const userId = ctx.labels[index]
                    const user = userId && props.analytics.users[userId]
                    return user?.name ?? substringWithEllipsis(userId, 16)
                },
                onClick: onClick(props.analytics, routes, 'user'),
            })
        }

        async function loadApiKeyIfMissing() {
            const requestArgs = routes.apiKey && props.analytics?.apiKeys && !props.analytics.apiKeys[routes.apiKey]
                ? {filter: 'apiKey', value: routes.apiKey}
                : routes.apiKeyId
                    ? {filter: 'apiKeyId', value: routes.apiKeyId}
                    : null
            if (requestArgs) {
                const r = await client.api(new GetAnalyticsReports(requestArgs))
                if (r.response) {
                    const apiKey = Object.keys(r.response.result?.apiKeys ?? {})[0]
                    const apiKeyInfo = apiKey && r.response.result.apiKeys[apiKey]
                    if (apiKeyInfo) {
                        props.analytics.apiKeys[apiKey] = apiKeyInfo
                        // from ManageUserApiKeys
                        if (routes.apiKeyId) {
                            routes.to({apiKey, apiKeyId: undefined})
                        }
                    }
                }
            }
        }

        watch(() => [routes.month], update)
        watch(() => [routes.apiKey], async () => {
            await loadApiKeyIfMissing()
            nextTick(updateApiKey)
        })
        watch(() => [opEntry.value], () => {
            const apiKey = opEntry.value?.key
            if (apiKey !== routes.apiKey) {
                routes.to({apiKey})
            }
        })
        watch(() => [props.analytics, limits.value.apiKey], () => {
            nextTick(createApiKeyRequestsChart)
        })

        return {
            routes,
            limits,
            apiKeyAnalytics,
            apiKeyLinks,
            opEntry,
            opEntries,
            apiKeyInfo,
            resultLimits,
            refApiKeyRequests,
            refApiKeysDurations,
            refApiKeysDurationRanges,
            refApiKeyStatusCodes,
            refApiKeyDurationRanges,
            refApiKeyTopApis,
            refApiKeyTopIps,
            refApiKeyTopUsers,
            getApiKeyLabel,
            chartHeight,
            humanifyNumber,
            formatDate,
            hiddenApiKey,
            mapCounts,
            showApiKey,
        }
    }
}

const IpAnalytics = {
    template: `
      <div class="mt-2 mb-4 mx-auto max-w-sm">
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
              <LogLinks :title="routes.ip" :links="ipLinks" :filter="{ ip:routes.ip }" />
            </div>
            <div class="lg:w-1/2">
              <HtmlFormat class="not-prose" :value="ipAnalytics" />
            </div>
          </div>
        </div>
        <div class="grid grid-cols-1 lg:grid-cols-2 w-full gap-2">
          <div>
            <div class="bg-white rounded shadow p-4" style="height:300px">
              <canvas ref="refIpStatusCodes"></canvas>
            </div>
          </div>
          <div>
            <div class="bg-white rounded shadow p-4 mb-8" style="height:300px">
              <canvas ref="refIpDurationRanges"></canvas>
            </div>
          </div>
        </div>
        <div :class="['mt-8 grid grid-cols-1 md:grid-cols-2 w-full gap-2', mapCounts(analytics.ips[routes.ip],['apis','users','apiKeys']) === 3 ? 'lg:grid-cols-3' : '']">
          <div v-if="mapCounts(analytics.ips[routes.ip],'apis')">
            Top APIs
            <div class="mt-1 bg-white rounded shadow p-4" style="height:300px">
              <canvas ref="refIpTopApis"></canvas>
            </div>
          </div>
          <div v-if="mapCounts(analytics.ips[routes.ip],'users')">
            Top Users
            <div class="mt-1 bg-white rounded shadow p-4 mb-8" style="height:300px">
              <canvas ref="refIpTopUsers"></canvas>
            </div>
          </div>
          <div v-if="mapCounts(analytics.ips[routes.ip],'apiKeys')">
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
            ip: 25,
        })

        const {formatBytes} = useFiles()
        const ipAnalytics = computed(() => {
            const user = props.analytics?.ips?.[routes.ip]
            if (user) {
                let ret = []
                if (user.totalDuration) {
                    ret.push({
                        name: 'Duration',
                        Total: humanifyMs(round(user.totalDuration)),
                        Min: humanifyMs(round(user.minDuration)),
                        Max: humanifyMs(round(user.maxDuration)),
                    })
                }
                if (user.totalRequestLength) {
                    ret.push({
                        name: 'Request Body',
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
                    ret.push({label: status, href: `${linkBase}&status=${status}`, count})
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

        onMounted(async () => {
            await loadIpIfMissing()
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
                requests: sortedSummaryRequests(props.analytics?.ips, limits.value.ip),
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
                .map(key => ({key, value: props.analytics.ips[key]}))
                .sort((a, b) => b.value.totalRequests - a.value.totalRequests)
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
                formatY: function (ctx, value, index, values) {
                    return ctx.labels[index]
                },
                onClick: onClick(props.analytics, routes, 'api'),
            })
            ipTopApiKeysChart = createRequestsChart({
                requests: sortedDetailRequests(props.analytics.ips[routes.ip]?.apiKeys),
                chart: ipTopApiKeysChart,
                refEl: refIpTopApiKeys,
                formatY: function (ctx, value, index, values) {
                    return hiddenApiKey(ctx.labels[index])
                },
                onClick: onClick(props.analytics, routes, 'apiKey'),
            })
            ipTopUsersChart = createRequestsChart({
                requests: sortedDetailRequests(props.analytics.ips[routes.ip]?.users),
                chart: ipTopUsersChart,
                refEl: refIpTopUsers,
                formatY: function (ctx, value, index, values) {
                    const userId = ctx.labels[index]
                    const user = userId && props.analytics.users[userId]
                    return user?.name ?? substringWithEllipsis(userId, 16)
                },
                onClick: onClick(props.analytics, routes, 'user'),
            })
        }

        async function loadIpIfMissing() {
            if (routes.ip && props.analytics?.ips && !props.analytics.ips[routes.ip]) {
                const r = await client.api(new GetAnalyticsReports({
                    filter: 'ip',
                    value: routes.ip
                }))
                if (r.response) {
                    const ip = Object.values(r.response.result?.ips ?? {})[0]
                    props.analytics.ips[routes.ip] = ip
                }
            }
        }

        watch(() => [routes.month], update)
        watch(() => [routes.ip], async () => {
            await loadIpIfMissing()
            nextTick(updateIp)
        })
        watch(() => [opEntry.value], () => {
            const ip = opEntry.value?.key
            if (ip !== routes.ip) {
                routes.to({ip})
            }
        })
        watch(() => [props.analytics, limits.value.ip], () => {
            nextTick(createIpRequestsChart)
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
            mapCounts,
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
      <section v-if="!plugin">
          <div class="p-4 max-w-3xl">
            <Alert type="info">Admin Analytics UI is not enabled</Alert>
            <div class="my-4">
              <div>
                <p>
                    The <b>RequestLogsFeature</b> plugin needs to be configured with your App's RDBMS
                    <a href="https://docs.servicestack.net/admin-ui-rdbms-analytics" class="ml-2 whitespace-nowrap font-medium text-blue-700 hover:text-blue-600" target="_blank">
                       Learn more <span aria-hidden="true">&rarr;</span>
                    </a>
                </p>
              </div>
            </div>
            <div>
                <p class="text-sm text-gray-700 my-2">For ASP.NET Identity Auth Projects:</p>
                <CopyLine text="x mix db-identity" />
                <p class="text-sm text-gray-700 my-2">For other ASP.NET Core Projects:</p>
                <CopyLine text="x mix db-requestlogs" />
            </div>
          </div>
      </section>
      <div v-else class="container mx-auto">
        <ErrorSummary v-if="api.error" :status="api.error" />
        <div>
            <div class="relative">
              <nav class="-mt-2 absolute flex space-x-4" aria-label="Tabs">
                <a v-for="(tab,label) in tabs" v-href="{ tab }"
                   :class="['rounded-md px-3 py-2 text-sm font-medium', routes.tab === tab ? 'bg-indigo-100 text-indigo-700' : 'text-gray-500 hover:text-gray-700']" aria-current="page">{{ label }}</a>
              </nav>
            </div>
            
            <div v-if="months.length" class="my-2 flex flex-wrap justify-center">
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
        const server = inject('server')
        const plugin = server.plugins.requestLogs
        const client = useClient()
        const analytics = ref(null)
        const loading = ref(false)
        const error = ref(null)
        const api = ref(new ApiResult())
        const tabs = ref(plugin?.analytics?.tabs ?? {APIs: ''})
        const months = ref(plugin?.analytics?.months ?? [])
        const years = computed(() =>
            Array.from(new Set(months.value.map(x => leftPart(x, '-')))).toReversed())

        async function update() {
            loading.value = true

            client.api(new GetAnalyticsInfo({type: 'info'})).then(r => {
                months.value = r.response?.months || []
            })

            //await delay(2000); // Pauses for 2 seconds

            api.value = await client.api(new GetAnalyticsReports({
                month: routes.month ? `${routes.month}-01` : undefined
            }))
            if (api.value.succeeded) {
                const result = api.value.response.result
                analytics.value = result
                loading.value = false
                const newTabs = {APIs: ''}
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
            nextTick(update)
        })

        return {
            plugin,
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
