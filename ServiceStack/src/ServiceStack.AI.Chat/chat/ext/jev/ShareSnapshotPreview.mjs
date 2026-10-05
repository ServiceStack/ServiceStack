import JsonBlock from './JsonBlock.mjs'
import RecordedAnswers from './RecordedAnswers.mjs'

export function formatRunDate(value) {
    const date = new Date(value)
    return value && !Number.isNaN(date.getTime())
        ? new Intl.DateTimeFormat(undefined, {
              dateStyle: 'medium',
              timeStyle: 'medium',
          }).format(date)
        : 'Unknown date'
}

export default {
    components: { JsonBlock, RecordedAnswers },
    props: { document: Object, execution: Object, snapshot: Object },
    data() {
        return { tab: 'preview' }
    },
    methods: {
        formatRunDate,
        label(key) {
            return (
                this.document.inputSchema.properties?.[key]?.title ||
                key.replace(/_/g, ' ')
            )
        },
        display(value) {
            return typeof value === 'string'
                ? value
                : JSON.stringify(value, null, 2)
        },
        count(value, noun) {
            const length = Object.keys(value || {}).length
            return `${length} ${noun}${length === 1 ? '' : 's'}`
        },
    },
    template: `<section v-if="document" data-jev-share-preview class="flex flex-col flex-1 min-w-0">
      <div class="flex shrink-0 items-center justify-between gap-3 mb-5">
        <h3 class="text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">Public preview</h3>
        <nav aria-label="Publication preview" class="inline-flex gap-1 p-1 rounded-lg bg-slate-100 dark:bg-gray-800">
          <button v-for="item in ['preview','json']" :key="item" type="button" :aria-pressed="tab===item" @click="tab=item" class="cursor-pointer rounded-md px-3 py-1.5 text-xs font-medium focus-visible:outline-2 focus-visible:outline-indigo-500" :class="tab===item?'bg-white dark:bg-gray-700 text-gray-900 dark:text-white shadow-sm':'text-slate-500 dark:text-slate-400 hover:text-gray-900 dark:hover:text-white'">{{item==='preview'?'Preview':'JSON'}}</button>
        </nav>
      </div>
      <div v-if="tab==='preview'">
        <h3 class="text-xl font-semibold tracking-tight wrap-anywhere">{{document.name}}</h3>
        <p v-if="document.description" class="mt-2 text-sm leading-relaxed text-slate-600 dark:text-slate-300 wrap-anywhere">{{document.description}}</p>
        <p class="mt-3 text-xs text-slate-500 dark:text-slate-400">{{count(document.inputSchema.properties,'field')}} · {{count(document.questions,'question')}}</p>
        <p v-if="document.content" class="mt-3 text-xs text-slate-500 dark:text-slate-400">Content: {{document.content}}</p><div v-if="document.tags?.length" class="flex flex-wrap gap-1.5 mt-3"><span v-for="tag in document.tags" :key="tag" class="rounded-md bg-indigo-50 dark:bg-indigo-950/50 px-2 py-1 text-xs text-indigo-700 dark:text-indigo-300 wrap-anywhere">{{tag}}</span></div>
        <template v-if="execution">
          <section class="mt-6 pt-5 border-t border-gray-200 dark:border-gray-700">
            <h4 class="font-semibold text-sm">Recorded example result</h4>
            <p class="mt-1 text-xs text-slate-500 dark:text-slate-400 leading-relaxed wrap-anywhere">{{execution.model}} · {{formatRunDate(execution.completedAt)}}</p>
            <RecordedAnswers :recipe="document" :execution="execution"/>
          </section>
          <section class="mt-5">
            <h4 class="text-sm font-semibold">Recorded input</h4>
            <dl class="mt-3 space-y-3 rounded-xl bg-slate-50 dark:bg-gray-800/60 border border-gray-200 dark:border-gray-700 p-4">
              <div v-for="(value,key) in execution.input" :key="key"><dt class="text-xs font-medium text-slate-500 dark:text-slate-400">{{label(key)}}</dt><dd class="mt-1 whitespace-pre-wrap wrap-anywhere text-sm leading-relaxed">{{display(value)}}</dd></div>
            </dl>
          </section>
          <details class="mt-5 text-xs text-slate-500 dark:text-slate-400"><summary class="cursor-pointer py-2 focus-visible:outline-2 focus-visible:outline-indigo-500">Recorded prompt / state</summary><JsonBlock :text="display(execution.prompt)"/></details>
        </template>
        <details v-if="document.examples?.length" class="mt-2 text-xs text-slate-500 dark:text-slate-400"><summary class="cursor-pointer py-2 focus-visible:outline-2 focus-visible:outline-indigo-500">Usage examples · {{document.examples.length}}</summary><article v-for="example in document.examples" :key="example.id" class="my-3 p-4 border border-gray-200 dark:border-gray-700 rounded-xl"><h4 class="text-sm font-semibold text-gray-800 dark:text-gray-200">{{example.label}}</h4><p v-if="example.notes" class="mt-2 leading-relaxed whitespace-pre-wrap wrap-anywhere">{{example.notes}}</p><dl class="mt-3 space-y-3"><div v-for="(value,key) in example.input" :key="key"><dt class="font-medium">{{label(key)}}</dt><dd class="mt-1 whitespace-pre-wrap wrap-anywhere text-sm leading-relaxed text-gray-800 dark:text-gray-200">{{display(value)}}</dd></div></dl><RecordedAnswers v-if="example.execution" :recipe="document" :execution="example.execution"/><div v-if="Object.keys(example.expected||{}).length" class="mt-3"><p class="font-medium">Expected answers</p><JsonBlock :text="JSON.stringify(example.expected,null,2)"/></div></article></details>
        <details class="mt-2 text-xs text-slate-500 dark:text-slate-400"><summary class="cursor-pointer py-2 focus-visible:outline-2 focus-visible:outline-indigo-500">Recipe fields and questions</summary><JsonBlock :text="JSON.stringify(document,null,2)"/></details>
      </div>
      <div v-else data-jev-share-json class="relative flex-1 min-h-64"><JsonBlock fill class="absolute inset-0" :text="JSON.stringify(snapshot,null,2)"/></div>
    </section>`,
}
