import {computed, inject, onMounted, onUnmounted, ref, watch} from "vue"
import {useClient, useFormatters, css} from "@servicestack/vue";
import {ApiResult, apiValueFmt, humanify, mapGet} from "@servicestack/client"
import {GetAnalyticsReports} from "dtos"
import {Chart, registerables} from 'chart.js'
Chart.register(...registerables)
export const Analytics = {
    template: `
      <div class="container mx-auto">
        <div v-if="loading" class="flex justify-center p-8">
          <div class="spinner"></div>
        </div>
        <ErrorSummary v-else-if="api.error" :status="api.error" />
        
        <div v-else>
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
            <div class="bg-white rounded shadow p-4 mb-8" :style="{height:chartHeight(Math.min(Object.keys(analytics?.apis ?? {}).length, limits.duration)) + 'px'}">
              <canvas ref="refApiDurations"></canvas>
            </div>
          </div>
        </div>
      </div>
    `,
    setup(props) {
        const client = useClient()
        const refApiRequests = ref(null)
        const refApiDurations = ref(null)
        const analytics = ref(null)
        const loading = ref(true)
        const error = ref(null)
        const api = ref(new ApiResult())
        const resultLimits = [5,10,25,50,100]
        function chartHeight(recordCount, minHeight = 300, heightPerRecord = 15) {
            // Validate input
            const count = Math.min(Math.max(1, recordCount), 100);
            // Base height plus additional height per record
            // More records need more vertical space for readability
            return minHeight + (count * heightPerRecord);
        }
        
        const limits = ref({
            api: 10,
            duration: 10
        })
        
        function apiOnClick(e, elements, chart) {
            console.log('onClick', e)
            const points = chart.getElementsAtEventForMode(e, 'nearest', { intersect: true }, false);
            if (points.length) {
                const firstPoint = points[0];
                const apiName = chart.data.labels[firstPoint.index];
                const apiStats = analytics.value.apis[apiName];
                // Do something with the clicked API data
                // For example, emit an event:
                // emit('apiSelected', { name: apiName, stats: apiStats });
                console.log('API clicked:', apiName, apiStats);
                // You could also implement a drill-down view or show details panel
            }
        }
        let apiRequestsChart = null
        const createApiRequestsChart = () => {
            if (!analytics.value || !refApiRequests.value) return
            
            // Sort APIs by request count in descending order
            const sortedApis = Object.entries(analytics.value.apis)
                .sort((a, b) => b[1].requests - a[1].requests)
                .slice(0, limits.value.api) // Limit to top 15 for better visualization
            
            const labels = sortedApis.map(([api]) => api)
            const data = sortedApis.map(([_, stats]) => stats.requests)
            const avgResponseTimes = sortedApis.map(([_, stats]) => 
                stats.requests > 0 ? Math.round(stats.duration / stats.requests) : 0)
            
            // Destroy existing chart if it exists
            if (apiRequestsChart) {
                apiRequestsChart.destroy()
            }
            
            apiRequestsChart = new Chart(refApiRequests.value, {
                type: 'bar',
                data: {
                    labels,
                    datasets: [
                        {
                            label: 'Requests',
                            data,
                            backgroundColor: 'rgba(54, 162, 235, 0.7)',
                            borderColor: 'rgba(54, 162, 235, 1)',
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
                                    return `Avg Response Time: ${avgResponseTimes[index]}ms`;
                                }
                            }
                        }
                    },
                    scales: {
                        x: {
                            beginAtZero: true,
                            title: {
                                display: true,
                                text: 'Number of Requests'
                            }
                        }
                    },
                    onClick: apiOnClick
                }
            })
        }
        let apiDurationsChart = null
        const createApiDurationsChart = () => {
            if (!analytics.value || !refApiDurations.value) return
            // Sort APIs by request count in descending order
            const sortedApis = Object.entries(analytics.value.apis)
                .sort((a, b) =>
                    Math.floor(b[1].duration / b[1].requests) - Math.floor(a[1].duration / a[1].requests))
                .slice(0, limits.value.duration) // Limit to top 15 for better visualization
            const labels = sortedApis.map(([api]) => api)
            const data = sortedApis.map(([_, stats]) => Math.floor(stats.duration / stats.requests))
            const avgRequestLengths = sortedApis.map(([_, stats]) =>
                stats.requests > 0 ? Math.round(stats.requestLength / stats.requests) : 0)
            // Destroy existing chart if it exists
            if (apiDurationsChart) {
                apiDurationsChart.destroy()
            }
            apiDurationsChart = new Chart(refApiDurations.value, {
                type: 'bar',
                data: {
                    labels,
                    datasets: [
                        {
                            label: 'Average Duration (ms)',
                            data,
                            backgroundColor: 'rgba(54, 162, 235, 0.7)',
                            borderColor: 'rgba(54, 162, 235, 1)',
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
                    onClick: apiOnClick
                }
            })
        }
        
        onMounted(async () => {
            api.value = await client.api(new GetAnalyticsReports())
            if (api.value.succeeded) {
                analytics.value = api.value.response
                loading.value = false
                // Wait for the DOM to update
                setTimeout(() => {
                    createApiRequestsChart()
                    createApiDurationsChart()
                }, 0)
            }
            loading.value = false
        })
        
        onUnmounted(() => {
            if (apiRequestsChart) {
                apiRequestsChart.destroy()
            }
            if (apiDurationsChart) {
                apiDurationsChart.destroy()
            }
        })
        // If the component resizes, update the chart
        watch(() => [analytics.value, limits.value.api], () => {
            setTimeout(() => {
                createApiRequestsChart()
            }, 0)
        })
        watch(() => [analytics.value, limits.value.duration], () => {
            setTimeout(() => {
                createApiDurationsChart()
            }, 0)
        })
        return {
            api,
            limits,
            resultLimits,
            analytics,
            loading,
            error,
            refApiRequests,
            refApiDurations,
            chartHeight,
        }
    }
}
