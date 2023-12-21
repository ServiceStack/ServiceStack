import { ref } from "vue"
import { map } from "@servicestack/client"

export default {
    template:`<div class="lang relative bg-gray-700 text-gray-300 pl-5 py-3 sm:rounded flex">
    <div class="flex ml-2 w-full justify-between cursor-pointer" @click="copy">
      <div>
          <span>$ </span>
          <label class="cursor-pointer">
            <slot>{{text}}</slot>
          </label>
      </div>
      <small class="text-xs text-gray-400 px-3 -mt-1">sh</small>
    </div>

    <div v-if="successText" class="absolute right-0 -mr-28 -mt-3 rounded-md bg-green-50 p-3">
        <div class="flex">
            <div class="flex-shrink-0">
                <svg class="h-5 w-5 text-green-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                    <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.857-9.809a.75.75 0 00-1.214-.882l-3.483 4.79-1.88-1.88a.75.75 0 10-1.06 1.061l2.5 2.5a.75.75 0 001.137-.089l4-5.5z" clip-rule="evenodd" />
                </svg>
            </div>
            <div class="ml-3">
                <p class="text-sm font-medium text-green-800">
                    {{ successText }}
                </p>
            </div>
        </div>
    </div>

  </div>`,
    props:['text'],
    setup(props) {
        let successText = ref('')
        /** @param {MouseEvent} e */
        function copy(e) {
            let $el = document.createElement("input")
            let $lbl = e.target.parentElement.querySelector('label')
            $el.setAttribute("value", $lbl.innerText)
            document.body.appendChild($el)
            $el.select()
            document.execCommand("copy")
            document.body.removeChild($el)
            if (typeof window.getSelection == "function") {
                const range = document.createRange()
                range.selectNodeContents($lbl)
                map(window.getSelection(), sel => {
                    sel.removeAllRanges()
                    sel.addRange(range)
                })
            }
            successText.value = 'copied'
            setTimeout(() => successText.value = '', 3000)
        }
        return { successText, copy }
    }
}
