import { ref } from "vue"
import { useUtils } from "@servicestack/vue"

const CopyIcon = {
    template:`
      <div @click="copy(text)">
          <div class="cursor-pointer select-none p-1 rounded-md border block border-gray-200 bg-white hover:bg-gray-50">
            <svg v-if="copied" class="w-6 h-6 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg>
            <svg v-else xmlns="http://www.w3.org/2000/svg" class="w-6 h-6 text-gray-500" viewBox="0 0 24 24"><g fill="none"><path d="M8 5H6a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2v-1M8 5a2 2 0 0 0 2 2h2a2 2 0 0 0 2-2M8 5a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2m0 0h2a2 2 0 0 1 2 2v3m2 4H10m0 0l3-3m-3 3l3 3" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></g></svg>
          </div>
      </div>
    `,
    props:['text'],
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
export default CopyIcon
