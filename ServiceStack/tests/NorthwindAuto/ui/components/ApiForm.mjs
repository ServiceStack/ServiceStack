import { computed, inject, provide, nextTick, onMounted, onUnmounted, ref, watch } from "vue"
import { prettyJson } from "core"
import { ApiResult, HttpMethods, parseCookie, map, humanify, humanize } from "@servicestack/client"
import { useMetadata, useClient, useUtils, css } from "@servicestack/vue"

const { createDto } = useMetadata()

let OP_STATE = {}

const ApiResponse = {
    template: `
    <div class="mt-2 border-t">
        <div class="border-b border-gray-200">
            <nav class="-mb-px flex space-x-4 pl-2" aria-label="Tabs">
                <a v-for="(tab,name) in tabs" @click="routes.response = tab" 
                   :class="[routes.response === tab ? 'border-indigo-500 text-indigo-600' : 'text-gray-500 border-transparent hover:text-gray-700 hover:border-gray-300', 'cursor-pointer whitespace-nowrap py-1 px-1 border-b-2 font-medium text-sm']">
                    {{name}}
                </a>
            </nav>
        </div>
    
        <div v-if="routes.response === ''" class="p-2">
            <span class="relative z-0 inline-flex shadow-sm rounded-md">
              <a v-for="(tab,name) in {Pretty:'',Preview:'preview'}" @click="routes.body = tab"
                  :class="[{ Pretty:'rounded-l-md',Preview:'rounded-r-md -ml-px' }[name], routes.body === tab ? 'z-10 outline-none ring-1 ring-indigo-500 border-indigo-500' : '', 'cursor-pointer relative inline-flex items-center px-4 py-1 border border-gray-300 bg-white text-sm font-medium text-gray-700 hover:bg-gray-50']">
                {{name}}
              </a>
            </span>
            <div v-if="routes.body === ''" class="pt-2">
                <CopyIcon v-if="json" :text="json" class="absolute right-4" />
                <pre class="whitespace-pre-wrap"><code lang="json" v-highlightjs="json"></code></pre>
            </div>
            <div v-else-if="routes.body === 'preview'" class="body-preview flex pt-2 overflow-x-auto">
              <HtmlFormat :value="api.response || api.error" :fieldAttrs="fieldAttrs" />  
            </div>
        </div>
        <div v-else-if="routes.response === 'cookies'" class="md:px-4">
            <HtmlFormat v-if="Object.keys(cookies).length" class="my-2" :value="asKvps(cookies)" />
            <Alert>HttpOnly Cookies are not shown</Alert>
        </div>
    </div>`,
    props:['api'],
    setup(props) {
        const { asKvps } = useMetadata()
        const json = computed(() => JSON.stringify(props.api.response, null, 4))
        const response = computed(() => props.api.response || props.api.error)
        const routes = ref({
            body: '',
            response: '',
        })

        function fieldAttrs(id) {
            let useId = id.replace(/\s+/g,'').toLowerCase()
            return useId === 'stacktrace'
                ? { 'class': 'whitespace-pre overflow-x-auto' }
                : {}
        }

        let cookies = parseCookie(document.cookie) || {}

        const cookiesLabel = computed(() =>  `Cookies (${Object.keys(cookies).length})`)
        const tabs = computed(() => {
            let tabs = { Body:'' }
            tabs[cookiesLabel.value] = 'cookies'
            return tabs
        })

        return {
            routes,
            json,
            response,
            cookies,
            asKvps,
            cookiesLabel,
            tabs,
            fieldAttrs,
        }
    }
}

export const ApiForm = {
    components: { ApiResponse },
    template:/*html*/`
    <div v-if="!showForm" class="fixed w-body md:w-body top-top-nav h-top-nav overflow-auto z-10">
        <div class="md:p-4">
          <Alert v-html="store.invalidAccess()" />
          <SignIn v-if="server.plugins.auth" title="Sign in to your account" :provider="routes.provider" @login="login" />
        </div>
    </div>
    <div v-else-if="state?.op">
        <nav class="w-body md:w-body fixed flex space-x-4 pl-2 py-2.5 border-b bg-white" aria-label="Tabs">
          <div>
            <a v-for="(tab,name) in tabs" v-href="{ form:tab, $on:formNav }"
               :class="[routes.form==tab ? 'bg-gray-100 text-gray-700' : 'uppercase text-gray-500 hover:text-gray-700', 'cursor-pointer px-3 py-1 font-medium text-sm rounded-md']">
              {{ name }}
            </a>
          </div>
        </nav>
        <div class="w-body md:w-body fixed top-sub-nav h-sub-nav overflow-auto md:pb-scroll">
          <Alert v-if="store.invalidAccess()" class="pt-4 px-4" v-html="store.invalidAccess()" />
          <Alert v-else-if="store.op.requiresApiKey && !(store.apikey || ['apikey','authsecret'].includes(store.auth?.authProvider))" class="pt-4 px-4">
            This API Requires an <a v-href="{ dialog:'apikey' }" target="_blank" class="underline">API Key</a>
          </Alert>
          <AutoForm v-if="showAutoForm && routes.form===''" :type="routes.op" v-model="state.model" 
                    class="sm:m-4 max-w-4xl" :jsconfig="server?.ui?.explorer?.jsConfig"
                    @success="state.apiResult.response=$event" @error="state.apiResult.error=$event" />
          <div v-if="routes.form==='json'" class="sm:p-4">
        
            <form @submit.prevent="onJsonFormSubmit" autocomplete="off" class="shadow sm:rounded-md">
              <div class="relative px-4 py-5 bg-white sm:p-6">
                <fieldset>
                  <legend class="text-lg text-gray-900 text-center mb-4">{{ state.title }}</legend>
                  <div class="flex flex-col">
                    <textarea ref="txtJson" class="flex-1" :rows="Math.max(state.apiRequestJson.split('\\n').length,10)"
                              v-model="state.formJson" @input="onJsonFormInput"></textarea>
                    <div v-if="state.formJson" class="mt-1">
                      <div v-if="state.jsonError" class="1 flex items-end">
                        <svg class="text-red-600 w-5 h-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                          <g fill="none"><path d="M20 20L4 4m16 0L4 20" stroke="currentColor" stroke-width="2" stroke-linecap="round"/></g>
                        </svg>
                        <p class="ml-1 text-md text-red-500">
                          {{ (state.jsonError || {}).type === 'submit' ? state.jsonError.message : 'invalid' }}</p>
                      </div>
                      <div v-else-if="state.apiRequestJson">
                        <svg class="text-green-500 w-6 h-6" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                          <g fill="none"><path d="M4 12l6 6L20 6" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></g>
                        </svg>
                      </div>
                    </div>
                  </div>
                </fieldset>
              </div>
              <div class="mt-4 px-4 py-3 bg-gray-50 text-right sm:px-6">
                <div class="flex justify-end">
                  <PrimaryButton :disabled="state.jsonError">
                    Submit
                  </PrimaryButton>
                </div>
              </div>
            </form>
            
          </div>
          <Loading v-if="apiLoading" />
          <ApiResponse v-else-if="state.apiResult?.response" class="mb-8 pb-8" :api="state.apiResult" />
        </div>
    </div>
    `,
    setup() {
        const { createFormLayout, formValues } = useMetadata()
        const client = useClient()

        const app = inject("app")
        const store = inject("store")
        const routes = inject("routes")
        const server = inject("server")
        
        const state = ref({})
        
        function createState(op) {
            let opName = op && op.request && op.request.name
            if (!opName) {
                console.warn('!state.opName') /*debug*/
                return null
            }

            const formLayout = createFormLayout(op.request)
            const apiResult = new ApiResult()

            const requestDto = createDto(opName) 
            let model = requestDto
            formLayout.forEach(input => {
                if (typeof model[input.id] == 'undefined') {
                    model[input.id] = null
                }
            })
            const formJson = ''
            const jsonError = ''

            let ret = {
                op,
                opName,
                formJson,
                jsonError,
                requestDto,
                model,
                apiResult,
                get error(){ return apiResult.error },
                get errorSummary() {
                    let except = formLayout.map(input => input.id).filter(x => x)
                    return apiResult.summaryMessage(except)
                },
                get title() { return op.request.description || humanify(op.request.name).replace(/^Patch/,'Update') },
                get apiRequestJson() { return this.requestDto && prettyJson(this.requestDto) || '' },
                formLayout,
            }
            return ret
        }

        const apiLoading = computed(() => client.loading.value)

        const txtJson = ref()
        const jsonForm = ref()
        function formNav(args) {
            if (!args.form || args.form === '') {
                updateRequestDto(state.value.formJson)
            } else if (args.form === 'json') {
                saveForm()
                state.value.jsonError = null
            }
        }
        const showAutoForm = ref(true)
        
        const showForm = computed(() => {
            if (!state.value?.op) return false
            return !state.value.op.requiresAuth || store.auth
        })

        function saveForm() {
            if (!state.value?.op) return
            const jsonModel = Object.keys(state.value.model).reduce((acc,x) => {
                const value = state.value.model[x]
                if (value != null) acc[x] = value
                return acc
            }, {})
            state.value.formJson = prettyJson(jsonModel)
        }
        
        function updateRequestDto(json) {
            if (!state.value?.op) return
            state.value.jsonError = null
            try {
                let obj = JSON.parse(json)
                state.value.model = state.value.requestDto = createDto(state.value.opName, obj)
            } catch (e) {
                state.value.jsonError = { type:'input', message: `${e}` }
            }
        }
        
        function onAjaxFormInput(e) {
            if (!state.value.op) return
            state.value.formJson = prettyJson(formValues(e.target.tagName === 'FORM' ? e.target : e.target.form))
        }

        function onJsonFormInput(e) {
            updateRequestDto(e.target.value)
        }
        
        async function onAjaxFormSubmit(e) {
            saveForm(e.target)
            if (HttpMethods.hasRequestBody(state.value.op.method)) {
                /**: Use apiForm for POST/PATCH requests using Ajax Forms to support multi-part/file requests */
                await apiForm(e.target)
            } else {
                await apiSend()
            }
        }
        
        async function onJsonFormSubmit(e) {
            if (!state.value?.op) return
            try {
                let json = txtJson.value?.value
                let obj = JSON.parse(json)
                state.value.requestDto = createDto(state.value.opName, obj)
                await apiSend()
            } catch (e) {
                state.value.jsonError = { type:'submit', message: `${e}` }
            }
        }
        
        async function apiSend() {
            if (!state.value?.op) return
            if (!state.value.requestDto) return
            state.value.jsonError = null
            state.value.apiResult = await client.api(state.value.requestDto)
        }
        
        async function apiForm(form) {
            if (!state.value?.op) return
            let formData = new FormData(form)
            state.value.requestDto = createDto(state.value.opName,{})
            state.value.jsonError = null
            state.value.apiResult = await client.apiForm(state.value.requestDto, formData)
        }
        
        //watch(state.value.model, () => )
        
        function login(auth) {
            globalThis.AUTH = store.auth = auth
        }
        
        const tabs = {'FORM':'','JSON':'json'}

        function update() {
            state.value = OP_STATE[routes.op] || (OP_STATE[routes.op] = createState(store.op))
            client.error = null
            // Force reloading of AutoForm
            showAutoForm.value = false
            nextTick(() => showAutoForm.value = true)
        }

        let sub = null
        onMounted(async () => {
            sub = app.subscribe('route:nav', update)
            update()
        })
        onUnmounted(() => app.unsubscribe(sub))

        return {
            store, routes, server, client, login,
            tabs,
            state,
            apiLoading,
            jsonForm,
            txtJson,
            showForm,
            showAutoForm,
            formNav,
            saveForm,
            updateRequestDto,
            onAjaxFormInput,
            onJsonFormInput,
            onAjaxFormSubmit,
            onJsonFormSubmit,
            apiSend,
            apiForm,
        }        
    }
}
