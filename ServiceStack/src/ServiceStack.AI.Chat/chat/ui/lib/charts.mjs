import { Chart, registerables } from "chart.js"
Chart.register(...registerables)

export const resultLimits = [5, 10, 25, 50, 100]
export const colors = [
    { background: 'rgba(54, 162, 235, 0.2)', border: 'rgb(54, 162, 235)' }, //blue
    { background: 'rgba(255, 99, 132, 0.2)', border: 'rgb(255, 99, 132)' },
    { background: 'rgba(153, 102, 255, 0.2)', border: 'rgb(153, 102, 255)' },
    { background: 'rgba(54, 162, 235, 0.2)', border: 'rgb(54, 162, 235)' },
    { background: 'rgba(255, 159, 64, 0.2)', border: 'rgb(255, 159, 64)' },
    { background: 'rgba(67, 56, 202, 0.2)', border: 'rgb(67, 56, 202)' },
    { background: 'rgba(255, 99, 132, 0.2)', border: 'rgb(255, 99, 132)' },
    { background: 'rgba(14, 116, 144, 0.2)', border: 'rgb(14, 116, 144)' },
    { background: 'rgba(162, 28, 175, 0.2)', border: 'rgb(162, 28, 175)' },
    { background: 'rgba(201, 203, 207, 0.2)', border: 'rgb(201, 203, 207)' },
]
