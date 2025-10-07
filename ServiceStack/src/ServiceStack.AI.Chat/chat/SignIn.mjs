import { inject, ref } from "vue"
import { toJsonObject } from "./utils.mjs"

export default {
    template: `
    <div class="min-h-full -mt-12 flex flex-col justify-center py-12 sm:px-6 lg:px-8">
        <div class="sm:mx-auto sm:w-full sm:max-w-md">
            <h2 class="mt-6 text-center text-3xl font-extrabold text-gray-900 dark:text-gray-50">
                Sign In
            </h2>
        </div>
        <div class="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
            <ErrorSummary v-if="errorSummary" class="mb-3" :status="errorSummary" />
            <div class="bg-white dark:bg-black py-8 px-4 shadow sm:rounded-lg sm:px-10">
                <form @submit.prevent="submit">
                    <div class="flex flex-1 flex-col justify-between">
                        <div class="space-y-6">
                            <fieldset class="grid grid-cols-12 gap-6">
                                <div class="w-full col-span-12">
                                    <TextInput id="apiKey" name="apiKey" label="API Key" v-model="apiKey" />
                                </div>
                            </fieldset>
                        </div>
                    </div>
                    <div class="mt-8">
                        <PrimaryButton class="w-full">Sign In</PrimaryButton>
                    </div>
                </form>
            </div>
        </div>
    </div>     
    `,
    emits: ['done'],
    setup(props, { emit }) {
        const ai = inject('ai')
        const apiKey = ref('')
        const errorSummary = ref()
        async function submit() {
            const r = await ai.get('/auth', {
                headers: { 
                    'Authorization': `Bearer ${apiKey.value}`
                },
            })
            const txt = await r.text()
            const json = toJsonObject(txt)
            // console.log('json', json)
            if (r.ok) {
                json.apiKey = apiKey.value
                emit('done', json)
            } else {
                errorSummary.value = json.responseStatus || { 
                    errorCode: "Unauthorized", 
                    message: 'Invalid API Key' 
                }
            }
        }
        
        return {
            apiKey,
            submit,
            errorSummary,
        }
    }
}