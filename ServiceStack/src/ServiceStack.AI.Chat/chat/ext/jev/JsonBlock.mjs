import { highlightJson } from '/ui/utils.mjs'

// Match the app's JSON inspector without inheriting global pre/code-block styles.
export default {
    props: { text: String, fill: Boolean },
    computed: {
        highlighted() {
            return highlightJson(this.text || '')
        },
    },
    template: `<div data-jev-json-block class="min-w-0 max-w-full overflow-auto rounded-lg border border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900 p-3 text-gray-800 dark:text-gray-200" :class="fill?'h-full max-h-none my-0':'max-h-100 my-3'"><code class="block whitespace-pre-wrap wrap-anywhere font-mono text-xs leading-relaxed" v-html="highlighted"></code></div>`,
}
