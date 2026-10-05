import JsonBlock from './JsonBlock.mjs'

// Visual bands describe probability concentration, not measured accuracy.
export function confidenceStrength(value) {
    if (value == null) return null
    return value < 0.5 ? 'weak' : value < 0.7 ? 'moderate' : value < 0.8 ? 'high' : 'strong'
}

const confidenceThemes = {
    weak: {
        pill: 'bg-red-50 text-red-900 border-red-600/30 dark:bg-red-950/40 dark:text-red-200 dark:border-red-400/40',
        fill: 'from-red-200 to-red-300 dark:from-red-800 dark:to-red-900',
    },
    moderate: {
        pill: 'bg-amber-50 text-amber-900 border-amber-600/30 dark:bg-amber-950/40 dark:text-amber-200 dark:border-amber-400/40',
        fill: 'from-amber-200 to-amber-300 dark:from-amber-800 dark:to-amber-900',
    },
    high: {
        pill: 'bg-yellow-50 text-yellow-900 border-yellow-600/30 dark:bg-yellow-950/40 dark:text-yellow-200 dark:border-yellow-400/40',
        fill: 'from-yellow-200 to-yellow-300 dark:from-yellow-800 dark:to-yellow-900',
    },
    strong: {
        pill: 'bg-emerald-50 text-emerald-900 border-emerald-600/30 dark:bg-emerald-950/40 dark:text-emerald-200 dark:border-emerald-400/40',
        fill: 'from-emerald-200 to-emerald-300 dark:from-emerald-800 dark:to-emerald-900',
    },
    unavailable: {
        pill: 'bg-slate-50 text-slate-500 border-slate-300 dark:bg-slate-800 dark:text-slate-400 dark:border-slate-600',
    },
}

// Shared read-only result presentation used by Jev and the public viewer.
export default {
    components: { JsonBlock },
    props: { recipe: Object, execution: Object },
    setup() {
        return {
            confidenceStrength,
            confidenceTheme: (value) => confidenceThemes[confidenceStrength(value) || 'unavailable'],
            label: (value) =>
                String(value)
                    .replace(/_/g, ' ')
                    .replace(/^./, (c) => c.toUpperCase()),
            percent: (value) =>
                new Intl.NumberFormat(undefined, {
                    style: 'percent',
                    maximumFractionDigits: 1,
                }).format(value),
            score: (value) => Number(value).toFixed(2),
            display: (value) =>
                typeof value === 'string' ? value : JSON.stringify(value),
        }
    },
    template: `<article v-for="(answer,key) in execution.answers" :key="key"  data-jev-answer class="py-5.5 px-0 border-b border-b-gray-200 dark:border-b-gray-700">
          <div data-jev-section-heading class="flex items-center justify-between gap-3 flex-wrap mb-[7px] max-[620px]:gap-2.5"><h4 class="text-sm font-semibold">{{recipe.presentation?.questions?.[key]?.label || label(key)}}</h4><span data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.6] mt-1">{{answer.type==='choice'?'Choose one':answer.type==='score'?'Ordered scale':'Yes / no'}}</span></div>
          <template v-if="answer.type==='noul'"><div data-jev-answer-value class="text-[28px] leading-[1.4] font-medium tracking-[-0.5px] mb-[17px] wrap-anywhere">{{percent(answer.noul)}}<span data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.6] font-normal tracking-[0]"> probability of yes</span></div>
            <div v-for="row in [{key:'Yes',value:answer.noul},{key:'No',value:1-answer.noul}]" :key="row.key"  data-jev-probability class="mt-3 text-xs"><div data-jev-row class="flex items-start gap-4 flex-nowrap justify-between"><span class="min-w-0 wrap-anywhere">{{row.key}}</span><span class="whitespace-nowrap tabular-nums">{{percent(row.value)}}</span></div><div data-jev-track class="h-[5px] bg-slate-100 dark:bg-slate-800 mt-1.5 rounded-[3px]"><div :style="{width:(row.value*100)+'%'}"  class="h-full bg-indigo-600 dark:bg-indigo-300 rounded-[3px] transition-[width] motion-reduce:transition-none"/></div></div>
          </template>
          <template v-else>
            <div data-jev-answer-value class="text-[28px] leading-[1.4] font-medium tracking-[-0.5px] mb-[17px] wrap-anywhere">{{answer.type==='choice' ? (recipe.presentation?.questions?.[key]?.optionLabels?.[answer.choice] || label(answer.choice)) : score(answer.score)}}<span v-if="answer.type==='score'"  data-jev-help class="text-xs text-slate-500 dark:text-slate-400 leading-[1.6] font-normal tracking-[0]"> / {{recipe.questions[key].criteria.length-1}}</span></div>
            <div v-if="answer.type==='score'"  :aria-label="'Score '+score(answer.score)+' on a scale from 0 to '+(recipe.questions[key].criteria.length-1)" data-jev-score-meter class="mt-2 mb-6"><div data-jev-score-line class="relative h-[5px] bg-slate-100 dark:bg-slate-800 mt-2 mr-[5px] mb-[9px] ml-[5px]"><span :style="{left:(answer.score/(recipe.questions[key].criteria.length-1)*100)+'%'}"  class="absolute -top-1 h-[13px] w-1 rounded-[2px] bg-indigo-600 dark:bg-indigo-300 -translate-x-1/2"/></div><div data-jev-score-ends class="flex justify-between gap-5 text-[10px] text-slate-500 dark:text-slate-400"><span class="max-w-[45%]">{{display(recipe.questions[key].criteria[0])}}</span><span class="max-w-[45%] text-right">{{display(recipe.questions[key].criteria.at(-1))}}</span></div></div>
            <div v-for="(value,option) in answer.probabilities" :key="option"  data-jev-probability class="mt-3 text-xs"><div data-jev-row class="flex items-start gap-4 flex-nowrap justify-between"><span class="min-w-0 wrap-anywhere">{{answer.type==='score' ? display(recipe.questions[key].criteria[Number(option)]) : recipe.presentation?.questions?.[key]?.optionLabels?.[option] || label(option)}}</span><span class="whitespace-nowrap tabular-nums">{{percent(value)}}</span></div><div data-jev-track class="h-[5px] bg-slate-100 dark:bg-slate-800 mt-1.5 rounded-[3px]"><div :style="{width:(value*100)+'%'}"  class="h-full bg-indigo-600 dark:bg-indigo-300 rounded-[3px] transition-[width] motion-reduce:transition-none"/></div></div>
            <details data-jev-confidence class="text-[11px] text-slate-500 dark:text-slate-400 mt-[15px]">
              <summary :data-strength="confidenceStrength(answer.confidence)" :class="confidenceTheme(answer.confidence).pill" :title="answer.confidence==null?'Confidence unavailable':'Confidence: '+percent(answer.confidence)" class="relative inline-flex items-center overflow-hidden rounded-full border px-3 py-1 text-xs font-medium cursor-pointer hover:border-current focus-visible:outline-2 focus-visible:outline-indigo-600 dark:focus-visible:outline-indigo-300 focus-visible:outline-offset-3 list-none [&::-webkit-details-marker]:hidden">
                <span v-if="answer.confidence!=null" data-jev-confidence-fill aria-hidden="true" :style="{width:(answer.confidence*100)+'%'}" :class="confidenceTheme(answer.confidence).fill" class="pointer-events-none absolute inset-y-0 left-0 bg-linear-to-r"/>
                <span class="relative z-10">{{answer.confidence==null?'Confidence unavailable':'Confidence '+percent(answer.confidence)+' · '+label(confidenceStrength(answer.confidence))}}</span>
              </summary>
              <p class="pt-[7px] leading-[1.7]">Confidence describes how concentrated the probabilities are across the alternatives. It is not measured accuracy. Strength: weak below 0.50, moderate below 0.70, high below 0.80, strong from 0.80.</p>
            </details>
          </template>
          <details data-jev-criteria class="text-xs text-slate-500 dark:text-slate-400 mt-4"><summary class="cursor-pointer focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3">Question &amp; criteria</summary><p class="mt-2 leading-[1.7]">{{display(recipe.questions[key].instructions)}}</p><JsonBlock :text="JSON.stringify(recipe.questions[key].criteria,null,2)"/></details>
        </article>`,
}
