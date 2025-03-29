import { useFormatters, useUtils } from "@servicestack/vue"
import { Chart, registerables } from "chart.js"
Chart.register(...registerables)

const { humanifyMs, humanifyNumber, formatDate } = useFormatters()
const { delay } = useUtils()

export const resultLimits = [5,10,25,50,100]
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
export function substringWithEllipsis(s, len) {
    return s.length > len ? s.substring(0, len - 3) + '...' : s
}
export function getUserLabel(analytics,userIdOrName) {
    const userId = userIdOrName in analytics.users ? userIdOrName : Object.keys(analytics.users)
        .find(key => analytics.users[key].name === userIdOrName)

    const user = userId && analytics.users[userId]
    return user
        ? user.name ? `${user.name} (${substringWithEllipsis(userId,8)})` : `${userId}`
        : userIdOrName
}
export function hiddenApiKey(apiKey) {
    return apiKey.substring(0,3) + '***' + apiKey.substring(apiKey.length-3)
}
export function getApiKeyLabel(analytics,apiKey) {
    const label = hiddenApiKey(apiKey)
    const info = apiKey && analytics.apiKeys[apiKey]
    return info
        ? info.name ? `${info.name} (${label})` : `${label}`
        : label
}
export function mapCounts(summary, keys) {
    return (Array.isArray(keys) ? keys : [keys])
        .reduce((acc,key) => acc + (summary && summary[key] && Object.keys(summary[key] ?? {})?.length ? 1 : 0), 0)
}

export function chartHeight(recordCount, minHeight = 150, heightPerRecord = 22) {
    // Validate input
    const count = Math.min(Math.max(1, recordCount), 100);
    // Base height plus additional height per record
    // More records need more vertical space for readability
    return minHeight + (count * heightPerRecord);
}

export function sortedSummaryRequests(requestsMap, limit= 11) {
    if (!requestsMap || !Object.keys(requestsMap).length) return []
    // Sort APIs by request count in descending order
    return Object.entries(requestsMap)
        .sort((a, b) => b[1].totalRequests - a[1].totalRequests)
        .slice(0, limit) // Limit for better visualization
}
export function sortedDetailRequests(requestsMap, limit= 11) {
    if (!requestsMap || !Object.keys(requestsMap).length) return []
    // Sort APIs by request count in descending order
    return Object.entries(requestsMap)
        .sort((a, b) => b[1] - a[1])
        .slice(0, limit) // Limit for better visualization
}


export function onClick(analytics, routes, type) {
    //console.log('onClick', type)

    if (type === "api") {
        return function(e, elements, chart) {
            const points = chart.getElementsAtEventForMode(e, 'nearest', { intersect: true }, false)
            if (points.length) {
                const firstPoint = points[0]
                const op = chart.data.labels[firstPoint.index]
                routes.to({ $page:'analytics', tab:'', op, $clear:true })
            }
        }
    }
    if (type === "user") {
        return function(e, elements, chart) {
            const points = chart.getElementsAtEventForMode(e, 'nearest', { intersect: true }, false);
            if (points.length) {
                const firstPoint = points[0]
                const userId = chart.data.labels[firstPoint.index]
                routes.to({ $page:'analytics', tab:'users', userId, $clear:true })
            }
        }
    }
    if (type === "ip") {
        return function(e, elements, chart) {
            const points = chart.getElementsAtEventForMode(e, 'nearest', { intersect: true }, false);
            if (points.length) {
                const firstPoint = points[0]
                const ip = chart.data.labels[firstPoint.index]
                routes.to({ $page:'analytics', tab:'ips', ip, $clear:true })
            }
        }
    }
    if (type === "apiKey") {
        return function(e, elements, chart) {
            const points = chart.getElementsAtEventForMode(e, 'nearest', { intersect: true }, false);
            if (points.length) {
                const firstPoint = points[0]
                const apiKey = chart.data.labels[firstPoint.index]
                routes.to({ $page:'analytics', tab:'apiKeys', apiKey, $clear:true })
            }
        }
    }

    throw new Error(`Unknown type: ${type}`)
}

/** Create a vertical bar chart for duration ranges */
export function createDurationRangesChart(durations, chart, elRef) {
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

export function createStatusCodesChart(opt) {
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

export function createRequestsChart(opt) {
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
