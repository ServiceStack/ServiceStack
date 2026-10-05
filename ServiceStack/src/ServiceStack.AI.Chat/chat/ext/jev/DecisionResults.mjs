import JsonBlock from './JsonBlock.mjs'
import RecordedAnswers from './RecordedAnswers.mjs'
import StudioNotice from './StudioNotice.mjs'
import { cost, label, percent, activeRun } from './recipeModel.mjs'
import StudioIcon from './StudioIcon.mjs'
export default {
    components: { JsonBlock, StudioNotice, StudioIcon, RecordedAnswers },
    props: {
        run: Object,
        stale: Boolean,
        pending: Object,
        canSaveExample: { type: Boolean, default: undefined },
    },
    emits: ['cancel', 'export', 'save-example'],
    setup() {
        return {
            cost,
            label,
            percent,
            activeRun,
            display: (value) =>
                typeof value === 'string' ? value : JSON.stringify(value),
            score: (value) => Number(value).toFixed(2),
        }
    },
    template: `<section aria-label="Decision results" aria-live="polite" data-jev-results class="min-w-0 border-l border-l-gray-200 dark:border-l-gray-700 pl-8 max-[1100px]:pl-5.5 max-[850px]:pl-0 max-[850px]:border-l-0 max-[850px]:border-t max-[850px]:border-t-gray-200 max-[850px]:dark:border-t-gray-700 max-[850px]:pt-5.5">
      <div data-jev-section-heading class="flex items-center justify-between gap-3 flex-wrap mb-4.5 max-[620px]:gap-2.5"><h3 class="text-base font-semibold">Results</h3><span v-if="run"  data-jev-badge class="inline-flex bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 text-[11px] rounded-[5px] py-0.75 px-[7px] items-center font-normal">{{pending?.id&&pending.id!==run.id?'Last successful result':run.status==='succeeded'?'Completed':run.status}}</span></div>
      <div v-if="activeRun(pending)"  role="status" data-jev-running class="flex items-center gap-2.5 text-xs p-3 bg-indigo-50 dark:bg-indigo-950/50 rounded-lg text-gray-800 dark:text-gray-200 flex-wrap"><span data-jev-spinner class="inline-block w-4 h-4 border-2 border-gray-200 dark:border-gray-700 border-t-indigo-600 dark:border-t-indigo-300 rounded-full animate-spin shrink-0 motion-reduce:animate-none"/>Evaluating your questions…<button type="button" :disabled="!pending.id" @click="$emit('cancel',pending)" data-jev-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 pointer-coarse:min-h-11">Stop</button></div>
      <StudioNotice v-if="pending?.error" tone="error" :message="pending.error"/>
      <div v-if="!run?.answers"  data-jev-result-empty class="py-13.5 px-3 text-center text-slate-500 dark:text-slate-400"><div data-jev-empty-symbol class="grid place-items-center text-indigo-600 dark:text-indigo-300 bg-indigo-50 dark:bg-indigo-950/50 w-14.5 h-14.5 rounded-[18px] mr-auto mb-4.5 ml-auto"><StudioIcon name="branch"  class="w-7 h-7 shrink-0"/></div><h3 class="text-base font-semibold text-gray-800 dark:text-gray-200 mb-2.5">Turn context into a decision</h3><p class="my-0 mx-auto text-[13px] leading-[1.8] max-w-75">Fill in the recipe and run Jev. Your answers will appear here with probabilities you can inspect.</p><div data-jev-primitive-legend class="flex flex-wrap justify-center gap-3 text-[10px] mt-6"><span class="py-0.75 px-[7px] border border-gray-200 dark:border-gray-700 rounded-[5px]">Choose one</span><span class="py-0.75 px-[7px] border border-gray-200 dark:border-gray-700 rounded-[5px]">Yes / no</span><span class="py-0.75 px-[7px] border border-gray-200 dark:border-gray-700 rounded-[5px]">Rate a scale</span></div></div>
      <template v-else>
        <StudioNotice v-if="stale" tone="warning" message="From an earlier input or recipe. Run again to evaluate your changes."/>
        <RecordedAnswers :recipe="run.recipe" :execution="run"/>
        <div data-jev-call-details class="flex flex-col gap-1.25 text-slate-500 dark:text-slate-400 text-[11px] my-4.5 mx-0 wrap-anywhere"><span>{{run.durationMs==null?'':(run.durationMs/1000).toFixed(2)+'s · '}}{{cost(run.usage?.cost)}}</span><span v-if="run.usage">{{run.usage.input_tokens ?? '—'}} input · {{run.usage.output_tokens ?? '—'}} output tokens</span><span>{{run.model}}</span></div>
        <div data-jev-actions class="flex items-center gap-2.5 flex-wrap"><button type="button"  v-if="canSaveExample!==undefined" :disabled="!canSaveExample" @click="$emit('save-example',run)" data-jev-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 pointer-coarse:min-h-11">Save as example</button><button type="button"  @click="$emit('export',run)" data-jev-button class="cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3 inline-flex items-center justify-center gap-1.75 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 text-gray-800 dark:text-gray-200 text-xs font-medium py-2 px-3 whitespace-normal text-center leading-[1.5] no-underline hover:bg-slate-100 hover:dark:bg-slate-800 pointer-coarse:min-h-11">Export result</button><details data-jev-raw class="text-xs text-slate-500 dark:text-slate-400 w-full mt-2.5"><summary class="cursor-pointer focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3">Raw response</summary><JsonBlock :text="JSON.stringify(run.response,null,2)"/></details></div>
      </template>
      <details v-if="(pending?.response&&pending.id!==run?.id)||(!run?.answers&&run?.response)"  data-jev-raw class="text-xs text-slate-500 dark:text-slate-400 w-full mt-2.5"><summary class="cursor-pointer focus-visible:outline-2 focus-visible:outline-solid focus-visible:outline-indigo-600 focus-visible:dark:outline-indigo-300 focus-visible:outline-offset-3">Inspect provider response</summary><JsonBlock :text="JSON.stringify(pending?.response||run?.response,null,2)"/></details>
    </section>`,
}
