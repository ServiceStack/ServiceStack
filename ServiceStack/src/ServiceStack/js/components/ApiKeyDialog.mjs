import { ref, inject, onMounted, computed } from 'vue'
import { ApiResult } from "@servicestack/client"
import { Authenticate } from "dtos"
export const ApiKeyDialog = {
    template: `
      <ModalDialog size-class="w-96" @done="$emit('done')">
        <div class="bg-white dark:bg-black px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
          <div class="">
            <div class="mt-3 text-center sm:mt-0 sm:mx-4 sm:text-left">
              <h3 class="text-lg leading-6 font-medium text-gray-900 dark:text-gray-100">{{title ?? 'API Key'}}</h3>
              
              <ErrorSummary v-if="errorSummary" class="mb-3" :errorSummary="errorSummary" />
              <div class="pb-4">
                <form @submit.prevent="submit">
                  <div class="space-y-6 pt-6 pb-5">
                    <TextInput id="apikey" type="password" autocomplete="new-password" v-model="apikey" label="" />
                  </div>
                  <div>
                    <PrimaryButton class="w-full">Save</PrimaryButton>
                  </div>
                </form>
              </div>
            </div>
          </div>
        </div>
      </ModalDialog>
    `,
    emits: ['done'],
    props: {
        title: String
    },
    setup(props, { emit }) {
        const store = inject('store')
        const server = inject('server')
        const routes = inject('routes')
        const apikey = ref(store.apikey ?? '')
        
        const modelValue = ref(new Authenticate())
        const api = ref(new ApiResult())
        const formLayout = computed(() => server.plugins.apiKey.formLayout)
        const errorSummary = computed(() => api.value.summaryMessage())
        
        async function submit() {
            store.apikey = apikey.value
            emit('done')
        }
        
        return {
            store, apikey, routes, api, modelValue, errorSummary, formLayout, submit
        }
    }
}
export default ApiKeyDialog
