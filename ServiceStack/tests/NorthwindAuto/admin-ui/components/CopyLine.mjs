import { ref } from "vue"
import { useUtils } from "@servicestack/vue"

const CopyLine = {
    template:`<div class="group flex items-center cursor-pointer rounded-md bg-gray-900 pl-4 pr-1.5 py-1.5 hover:bg-gray-800" @click="copy(text)" title="Copy to clipboard">
        <div class="flex-grow font-mono text-sm text-gray-100 select-none"><span class="text-gray-500">$ </span>{{prefix||''}}{{text}}</div>
        <div :class="['ml-3 rounded p-1.5', copied ? 'text-green-400' : 'text-gray-400 group-hover:text-white']">
            <svg v-if="copied" class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg>
            <svg v-else class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7v8a2 2 0 002 2h6M8 7V5a2 2 0 012-2h4.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V15a2 2 0 01-2 2h-2M8 7H6a2 2 0 00-2 2v10a2 2 0 002 2h8a2 2 0 002-2v-2"></path>
            </svg>
        </div>
    </div>`,
    props:['text','prefix'],
    setup(props) {
        const { copyText } = useUtils()
        const copied = ref(false)
        function copy(text) {
            copied.value = true
            copyText(text)
            setTimeout(() => copied.value = false, 3000)
        }

        return { copied, copy, }
    }
}
export default CopyLine
